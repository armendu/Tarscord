# Working on Tarscord

A Discord bot in C# on .NET 10, using Discord.Net for the gateway, MediatR for a vertical-slice
command/query layer, EF Core + Npgsql for storage, and DbUp for schema migrations. See
[README.md](README.md) for how to run it.

Read *Things that will bite you* before you believe anything works. A fair amount of this
codebase is registered-but-broken: the command shows up in `?help` and then throws. Knowing
which parts those are saves you from "fixing" code that was never reached.

## How work happens here

Work on a branch, merge to `master` through a pull request.

```
feat/<topic>  →  pull request  →  master
```

`master` is the default branch and the only long-lived one. Two things about that are worth
knowing rather than discovering:

**`master` is not protected.** Verified against the API, not assumed — the branch-protection
endpoint answers 404 *Branch not protected*. A direct push to `master` succeeds silently. The
pull-request flow here is a convention people keep, not a gate that stops them. Keep it anyway:
every feature in the history since #2 arrived that way.

**CI is thin.** It restores, builds and tests on every pull request, and the test suite is
empty, so in practice the only thing it proves is that the tree compiles. See *CI* below.

Branch names in the history use both `feat/` and `feature/`. Prefer `feat/`, matching the most
recent ones.

### Commit messages

[Conventional Commits](https://www.conventionalcommits.org), which most of the recent history
already follows:

```
type(scope): summary in the imperative, lower case, no full stop
```

`type` is one of **feat, fix, docs, test, build, ci, refactor, perf, chore, revert**. `scope` is
the part of the repo it lands in — **core, modules, features, persistence, migrator, tests,
build, ci** — and is left off when a change spans everything. Keep the subject under about 72
characters.

```
feat(features): add loan payback partial payments
fix(modules): stop event list throwing on an empty database
chore(build): upgrade to .NET 10 and upgrade packages
```

**A body is for *why*, and only when why is not obvious.** A one-line change does not need one;
a decision, a trap, or a bug whose cause is not visible in the diff does. That is the same rule
the code comments follow, see *Style* at the bottom of this file. What the change does is
already in the diff.

Older commits don't follow this and are left alone. Rewriting shared history to tidy up message
formatting is a bad trade.

**Nothing is enforced by a hook.** `.git/hooks` holds only the stock samples. And although
`commit.gpgsign=true` with `gpg.format=ssh` is set globally, `git log --format=%G?` reports `N`
for every commit in this repo — commits land unsigned. Don't add `--no-verify` or
`-c commit.gpgsign=false` incantations to work around problems this repo doesn't have.

### Secrets

`src/Tarscord.Core/Resources/config.yml` is **gitignored and must stay that way.** It holds the
bot token and the database password. `config.example.yml` beside it is the tracked template;
when you add a config key, add it there with a placeholder value.

The token committed on `master` has been the empty string since `4ece70d`, and the working copy
carried the real one only on disk. Keep it that way.

Five *older* tokens for this bot are still reachable from `master` in early history, across
commits cheerfully titled `Removed API Key`, `Removed token` and `Removed API key :facepalm:`.
They're spent, but they make the point: removing a secret in a later commit does not remove it
from history. Anything pushed is burned and has to be rotated in the Developer Portal — that is
the fix, not a follow-up commit.

A trap worth knowing, because it nearly landed a live token in this very change: the file used
to carry a `skip-worktree` bit, which hides local edits from `git status` entirely. Clearing
that bit makes your real token look like an ordinary unstaged modification, and `.gitignore`
does **not** protect a file that is still tracked. If you ever untrack it again, verify with
`git diff --cached` before committing, not after.

## Commands

```sh
dotnet build                                   # all three projects, via Tarscord.slnx
dotnet test                                    # currently discovers 0 tests, exits 0
dotnet run --project src/Tarscord.Core         # start the bot
dotnet run --project src/Tarscord.DbMigrator -- "<connection string>"
```

The solution is [`Tarscord.slnx`](Tarscord.slnx), the XML solution format. It has no GUIDs and
no configuration matrix — adding a project is one `<Project Path="..." />` line. Don't
reintroduce a `.sln`; having both in one directory makes every `dotnet` command ambiguous.

## Layout

```
src/Tarscord.Core/
    Program.cs, Startup.cs     composition root: config, DI, gateway client
    Setup/                     InitializeBot, ProcessMessage — MediatR handlers for gateway wiring
    Services/                  long-lived singletons hooked to Discord.Net events
    Modules/                   Discord.Net command modules — the user-facing surface
    Features/<Area>/           one file per operation: the request record and its handler
    Persistence/               TarscordContext, Entities/, AddDatabase()
    Extensions/                EmbedMessage, ToCommonUser, FromTextToDate
    Resources/config.yml       gitignored; config.example.yml is the template
src/Tarscord.DbMigrator/       DbUp console app, Migrations/*.sql embedded as resources
tests/                         xUnit + FluentAssertions
```

`Tarscord.Core` is the executable (`OutputType` is `Exe`), not a library, despite the name.

## Architecture

The shape to copy when adding anything. These are the repo's own patterns, read off the code.

**A feature is a file.** One `internal static class` per operation under `Features/<Area>/`,
holding the request record and its handler together: `Features/Events/Create.cs`,
`Features/Loans/List.cs`. Adding an operation means adding a file, never widening a service
class. Name the file after the verb — `Create`, `Update`, `List`, `Details`.

**Modules stay thin.** A module parses Discord input, sends one MediatR message, and replies
with an `Embed`. No EF Core, no business rules, no `DbContext`. `Modules/LoanGroupModule.cs` is
close to the intended shape; `Modules/EventGroupModule.cs` is the same shape buried under dead
code.

**Entities never reach Discord.** Each feature owns an envelope with a static `FromEntity` and
an instance `ToEmbeddedMessage` — see `Features/Events/EventInfoEnvelope.cs`. A handler returns
the envelope; the module renders it.

**Expected failures are values, not exceptions.** Handlers that can fail in a way a user causes
return `OneOf<TEnvelope, FailureResponse>`, and the module `.Match(…)`es both arms. "No such
event", "you'd be overpaying", "that isn't a user mention" are all values. Exceptions are for
bugs. This is also what the Framework Design Guidelines ask for — see *Code quality*.

**User-triggered requests are auditable.** Implement `IPerformedByUser`
(`Features/Common/IPerformedByUser.cs`) and open the handler with
`logger.LogInformation("{Command} executed by {PerformedByUser}", …)`, structured, not
interpolated.

**Reuse what's there.** `EmbedMessage()` in `Extensions/Extensions.cs` builds every embed;
`ToCommonUser()` in `Extensions/UserExtensions.cs` maps a Discord user to the `User` entity.
Don't hand-roll an `EmbedBuilder` in a module.

**Take time from `TimeProvider`.** It's registered in `Startup` and injected into the handlers
that need it, which is what makes them testable. Never `DateTime.Now` in a handler.
`Extensions/DateTimeExtensions.cs` does use `DateTime.Now` directly — that's an inconsistency to
fix, not the pattern to follow.

## The build

All three projects target `net10.0`. `global.json` pins the SDK to `8.0.0` with
`"rollForward": "latestMajor"`, which is why a machine with 10.0.201 installed builds fine — the
pin is a floor, not a ceiling.

| Package | Version | What it's for |
|---|---|---|
| Discord.Net | 3.20.1 | Gateway client and the text-command framework |
| MediatR | 14.2.0 | The request/handler layer every feature is built on |
| FluentValidation | 12.1.1 | Validators — mostly unwired, see below |
| OneOf | 3.0.271 | `OneOf<TEnvelope, FailureResponse>` result type |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | EF Core against PostgreSQL |
| NetEscapades.Configuration.Yaml | 3.1.0 | Lets `IConfiguration` read `config.yml` |
| dbup-postgresql | 7.0.1 | Migrations, in `Tarscord.DbMigrator` only |
| xunit / FluentAssertions | 2.9.3 / 8.11.0 | Tests |
| Roslynator.Analyzers | 5.0.0 core, 4.14.1 tests | Analyzers — versions disagree, see below |

The schema is **not** EF migrations. It's hand-written SQL in
`src/Tarscord.DbMigrator/Migrations/`, embedded as resources and applied by DbUp in filename
order. A model change needs a new numbered `.sql` file; EF will not generate one for you and
`EnsureCreated` is never called.

## Things that will bite you

Every item here was verified in the repository, not inferred. They're roughly in order of how
early they'll stop you.

**`dotnet test` reports success while running zero tests.** The only test file,
`tests/ServicesTests/TimerServiceTests.cs`, is commented out in its entirety, and it references
`Moq`, which is not a `PackageReference` in the test project. The runner prints "No test is
available" and the suite is vacuously green. Adding `Moq` is a prerequisite to reviving it.

**`?remindme` throws before it reaches the reminder.** `TimerService`'s constructor takes an
`ITimer`, and `Startup.ConfigureServices` never registers one, so resolving `TimerService`
fails. `StartTimerAsync` is never called by anything either, and `ReminderInfos` is a `static`
`SortedList` shared across every instance. The reminder path needs designing, not patching.

**`?loan payback` throws on the database query.** `Features/Loans/Update.cs` calls
`LastOrDefaultAsync` on an unordered `IQueryable`. EF Core cannot translate "last" without an
ordering and throws `InvalidOperationException` telling you to add an `OrderBy`. The fix is
`OrderByDescending(x => x.Created)` — or `FirstOrDefaultAsync` on a descending order, which is
what actually runs on the database.

**`?event list` throws on an empty database.** `EventModule.ListEvents` indexes
`eventInfoList.EventInfos[0]` with no emptiness check. It also only ever shows the first event's
name, so it is wrong even when it doesn't throw.

**`?loan list` sends two messages when you have no loans.** `LoanModule.ShowLoans` replies "No
active loans were found" and then falls through to the "Here are all the loans" reply, because
there's no `return`.

**Event organizers are stored as the description.** `Features/Events/Create.cs` sets
`EventOrganizer = command.EventDescription`. The organizer's name never reaches the database;
`EventOrganizerId` is correct. Every row created so far has this wrong.

**Most validators never run.** `Startup.ConfigureServices` registers exactly one:
`IValidator<Features.Events.Details.Query>`. There is no MediatR pipeline behavior doing
validation, so a validator only executes if its handler injects `IValidator<T>` itself — which
only `Events.Details` does. `Loans.Create.CommandValidator` and `Loans.Update.CommandValidator`
compile, are never constructed, and enforce nothing. Writing a new `AbstractValidator` without
wiring it is writing dead code. The real fix is a `ValidationBehavior<,>` in the MediatR
pipeline; until that exists, wire validators explicitly and know you've done it.

**`Events.Details` reports a validation failure as "Event does not exist".** Both the invalid
input branch and the not-found branch return the same `FailureResponse`, so a malformed ID looks
identical to a missing event.

**The C# types and the SQL columns disagree.** `EntityBase.Id` is `ulong`; the columns are
`SERIAL`, which is a signed 32-bit `int`. `Loan.AmountLoaned` and `AmountPayed` are `decimal`;
`amount_loaned` and `amount_payed` are `BIGINT`, so every fractional amount is truncated on the
way in. `EventAttendee.EventInfoId` is a `string` against an `INTEGER` foreign key. None of this
fails loudly.

**Discord's Message Content intent is privileged.** `Startup` requests
`GatewayIntents.MessageContent` because text commands cannot work without it. If it isn't
enabled in the Developer Portal the bot connects, logs nothing unusual, and ignores every
command. A "the bot does nothing" report is this until proven otherwise.

**`config.yml` is loaded as required, from the output directory.** `Startup` reads
`AppContext.BaseDirectory` + `Resources/config.yml` with `optional: false`, and the csproj copies
it with `PreserveNewest`. A fresh clone has no `config.yml` at all (it's gitignored) and throws
on startup before any logging is set up.

**`Roslynator.Analyzers` is 5.0.0 in `Tarscord.Core` and 4.14.1 in the test project.** The two
projects are analyzed against different rule sets. Bump them together.

**The build is not warning-clean.** `Tarscord.Core` emits three `CS8604` warnings, all in
`Modules/AdminModule.cs`, where an `IUser? user = null` parameter is passed to `Mute.Command`'s
non-nullable `IUser`. If you're checking whether you introduced a warning, that's the baseline:
three, all in that file.

**Commented-out code is everywhere and some of it is load-bearing context.**
`Modules/EventGroupModule.cs` is roughly half commented-out bodies — `remove`, `confirm`,
`cancel` and `confirmed` all `await Task.CompletedTask` and return. `Features/EventAttendees/`
exists in full and nothing calls it. Delete what you replace; git remembers it.

## Code quality

This project follows the mainstream .NET conventions rather than a house style. Each rule below
links to where it comes from, so you can check it instead of taking this file's word for it.

**Naming** — [Framework Design Guidelines: Capitalization Conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions).
PascalCase for namespaces, types, methods, properties, events and constants; camelCase for
parameters. No underscores in public identifiers. Two-letter acronyms keep both letters
capitalised (`IOStream`), longer ones don't (`HtmlTag`). Names never differ by case alone.

**Layout and fields** — [dotnet/runtime C# Coding Style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md),
which is what this codebase's existing `_mediator`-style fields already follow. Private and
internal fields are `_camelCase`, statics are `s_`. Allman braces, four spaces, no tabs.
`using` directives outside the namespace, sorted, `System.*` first. Always state visibility, and
put it first among the modifiers. Prefer `nameof(...)` to a string literal. Make internal and
private types `static` or `sealed` unless something derives from them. Use `var` only when the
type is named on the right-hand side.

**Language use** — [.NET/C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
File-scoped namespaces. Language keywords (`string`, `int`) over BCL names (`String`, `Int32`).
String interpolation for short concatenation, `StringBuilder` in loops. Object and collection
initializers. Don't catch `System.Exception` without a filter; catch only what you can handle.

**Exceptions** — [Framework Design Guidelines: Exception Throwing](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exception-throwing).
Report execution failures by throwing, but **don't use exceptions for normal control flow** —
that's what `OneOf<TEnvelope, FailureResponse>` is for here. Never throw bare
`new Exception(...)`; throw a specific type with a message that says what went wrong.
`throw;` to rethrow, never `throw ex;`, which resets the stack trace.

> Four places currently violate this: `InteractionModule`, `ReminderModule`,
> `RandomNumberModule` and `Setup/InitializeBot.cs` all throw bare `Exception`. In a module,
> what you almost always want instead is a `FailureResponse` and a reply the user can read.
> `RandomNumberModule` is the worst of them — it catches `Exception` and rethrows a new one,
> discarding the original.

**Async** — [Asynchronous programming scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios).
`async void` only for event handlers. Suffix async methods with `Async`. Never `.Result` or
`.Wait()`; `await` instead. Don't mark a method `async` if it has nothing to await — six
methods here do `await Task.CompletedTask` purely to satisfy the signature (the
`EventGroupModule` stubs, `BotConfigModule`, `TimerService.AddReminder`), which is a smell that
the method should be synchronous.

> `TimerService.StartTimerAsync` passes `async void (_) => await mediator.Send(...)` as a
> `System.Threading.Timer` callback. An exception thrown in there is unobservable and takes the
> process down. This is the canonical `async void` trap, not an event handler.

**Analyzers** — [Code analysis in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).
`EnableNETAnalyzers` is on by default for .NET 5+, so `CAxxxx` rules already run. `IDExxxx`
code-style rules do **not** run on a command-line build unless `EnforceCodeStyleInBuild` is set,
which is why style drift in this repo never shows up in a build or in CI. There is no
`.editorconfig`, so nothing is machine-enforced today; adding one is the right way to make the
rules above real rather than aspirational.

## Testing

Zero tests run today. Start from that fact rather than from the green `dotnet test` output.

When you add tests, the standard is
[Microsoft's unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices):

- Name them `Method_Scenario_ExpectedBehavior` — `Handle_AmountExceedsBalance_ReturnsFailure`.
- Arrange / Act / Assert, marked with comments, in that order.
- No `if`, `for` or string-building inside a test. Use `[Theory]` with `[InlineData]` instead.
- One Act per test. Assign hard-coded values to named constants rather than inlining them.
- No infrastructure. A unit test touches no database and no gateway.
- Test through public behaviour, not private methods.
- Inject seams for anything ambient. `TimeProvider` is already injected into the handlers for
  exactly this reason — use it rather than freezing the clock globally.

Handlers are the natural unit here: they take injected dependencies and return a value.
`Features/Loans/Update.cs`'s overpayment rule is the clearest example of logic that should have
had a test and doesn't.

## CI

`.github/workflows/dotnet.yml` runs `dotnet restore`, `dotnet build` and `dotnet test` in
Release on every push and pull request targeting `master`, on `ubuntu-latest` with the 10.0.x
SDK. No database, no gateway, no device.

It installs the SDK with an explicit `dotnet-version: 10.0.x` rather than
`global-json-file: global.json`, deliberately: `global.json` pins `8.0.0` and leans on
`rollForward: latestMajor` to reach a newer SDK, which works on a developer machine that has one
but would leave a runner holding only 8.0.0 unable to build `net10.0`.

It does **not** pass `-warnaserror`, because the tree has three pre-existing `CS8604` warnings
(see above) and a build that fails on day one gets ignored or removed. Clear those three and
turning it on is a two-word change worth making.

## Style

Explain *why* in comments, not *what* — the code already says what. A comment that survives is
one that records a decision or a trap, not one that narrates the line below it. `// Create the
command context` above `new SocketCommandContext(...)` is the kind this repo has too many of.

Don't add a third-party dependency without a reason you can state in the commit body.

Delete code you replace. There is a lot of commented-out code here and it has already cost
someone the time it takes to work out whether it was disabled deliberately.

## Working on this repo as an agent

**Verify before you claim.** Run the per-project build and report what it actually said. The
baseline is three `CS8604` warnings in `AdminModule.cs` — if you report "build succeeded" without
mentioning warnings, you haven't looked. Don't call something fixed until you've run it.

**Check whether it ever worked.** Several commands in this repo throw on their first line. If
you're asked to change behaviour in one, establish what it does today before assuming the bug is
the one described.

**Stay inside the task.** This file lists a lot of known problems. They are context, not a
backlog to work through. Fixing an unrelated one in the same change makes the diff unreviewable —
mention it instead.

**Prefer the existing pattern.** Feature file, envelope, `OneOf`, thin module. If a change seems
to need a new architectural concept, say so and ask rather than introducing a second way of doing
things alongside the first.
