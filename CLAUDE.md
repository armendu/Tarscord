# Working on Tarscord

A Discord bot in C# on .NET 10, using Discord.Net for the gateway, MediatR for a vertical-slice
command/query layer, EF Core + Npgsql for storage, DbUp for schema migrations, and a local LLM
through Ollama for the bot's own voice. See [README.md](README.md) for how to run it.

Every command in `?help` works, and there are tests. That was not true until recently — most of
what this file used to warn about has been fixed — so treat any pre-2026 commit's context with
suspicion. *Things that will bite you* is still worth reading; it is just much shorter now.

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

**CI builds with `-warnaserror` and runs both test suites.** The integration suite starts its own
PostgreSQL through Testcontainers, so a pull request that breaks a query fails on the runner. See
*CI* below.

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
docker compose up -d                           # PostgreSQL on port 5433
dotnet build                                   # all four projects, via Tarscord.slnx
dotnet test                                    # unit + integration
dotnet test tests/Tarscord.Core.Tests          # unit only, no Docker needed
dotnet run --project src/Tarscord.Core         # start the bot
dotnet run --project src/Tarscord.DbMigrator -- "<connection string>"
```

The integration suite needs a Docker socket; it finds one for Docker Desktop, Rancher Desktop and
Colima, and `DOCKER_HOST` overrides that. It does not use the compose database — it starts a
throwaway one and applies the real migrations to it.

The solution is [`Tarscord.slnx`](Tarscord.slnx), the XML solution format. It has no GUIDs and
no configuration matrix — adding a project is one `<Project Path="..." />` line. Don't
reintroduce a `.sln`; having both in one directory makes every `dotnet` command ambiguous.

## Layout

```
src/Tarscord.Core/
    Program.cs, Startup.cs     composition root: config, DI, gateway client
    Setup/                     InitializeBot, ProcessMessage — MediatR handlers for gateway wiring
    Services/                  singletons: gateway event hooks, BackgroundServices, BotPersonality
    Modules/                   Discord.Net command modules — the user-facing surface
    Features/<Area>/           one file per operation: the request record and its handler
    Persistence/               TarscordContext, Entities/, AddDatabase()
    Extensions/                EmbedMessage, FromTextToDate, CommandPrefix, ToEmbeddedMessage
    Resources/config.yml       gitignored; config.example.yml is the defaults layer under it
src/Tarscord.DbMigrator/       DbUp console app, Migrations/*.sql embedded as resources
                               DatabaseMigrator is public so tests apply the real scripts
tests/Tarscord.Core.Tests/         unit tests: no database, no gateway, no model
tests/Tarscord.IntegrationTests/   Testcontainers PostgreSQL with the real migrations applied
docker-compose.yml             PostgreSQL for running the bot, on host port 5433
```

`Tarscord.Core` is the executable (`OutputType` is `Exe`), not a library, despite the name.

## Architecture

The shape to copy when adding anything. These are the repo's own patterns, read off the code.

**A feature is a file.** One `internal static class` per operation under `Features/<Area>/`,
holding the request record and its handler together: `Features/Events/Create.cs`,
`Features/Loans/List.cs`. Adding an operation means adding a file, never widening a service
class. Name the file after the verb — `Create`, `Update`, `List`, `Details`.

**Modules stay thin.** A module parses Discord input, guards what Discord's parser cannot
(a missing mention), sends one MediatR message, and replies. No EF Core, no business rules, no
`DbContext`. `Modules/LoanModule.cs` and `Modules/AdminModule.cs` are the shape to copy.

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

**Reuse what's there.** `EmbedMessage()` in `Extensions/Extensions.cs` builds every embed, and
`FromTextToDate()` in `Extensions/DateTimeExtensions.cs` parses whatever a person typed as a date.
An envelope implements `IEmbeddedMessage` and a module renders a whole result with
`response.ToEmbeddedMessage()` from `Extensions/ResponseExtensions.cs`, rather than repeating the
same two-arm `Match`. Don't hand-roll an `EmbedBuilder` in a module. `HelpModule` is the one
exception: it enumerates `CommandService` rather than rendering a feature, so it builds its own.

**Take time from `TimeProvider`.** It's registered in `Startup` and injected into every handler
and extension that needs a clock, which is what makes them testable with `FakeTimeProvider`. Never
`DateTime.Now` anywhere but a test.

**Validators are wired one at a time.** There is no validation pipeline behavior. A validator runs
only if its handler injects `IValidator<T>` *and* `Startup` registers it — both, or it is dead code.
The registrations sit together in `ConfigureServices` so the set is visible in one place. A
`ValidationBehavior<,>` would remove the footgun and remains the better long-term answer; it was
deliberately not introduced.

**Background work is a `BackgroundService` with a scope per tick.** `ReminderDispatcher` and
`RestrictionExpirySweeper` are the two. `TarscordContext` is scoped, so each tick opens an
`IServiceScope` and resolves `IMediator` from it. Both catch and log rather than letting an
exception escape, because a loop that dies takes its whole feature with it silently.

**Depend on `IDiscordClient`, not `DiscordSocketClient`, in a handler.** The concrete client's
members are not virtual, so a handler that takes it cannot be tested at all. Both are registered.

## The build

All four projects target `net10.0`. `global.json` pins the SDK to `8.0.0` with
`"rollForward": "latestMajor"`, which is why a machine with 10.0.201 installed builds fine — the
pin is a floor, not a ceiling.

| Package | Version | What it's for |
|---|---|---|
| Discord.Net | 3.20.1 | Gateway client and the text-command framework |
| MediatR | 14.2.0 | The request/handler layer every feature is built on |
| FluentValidation | 12.1.1 | Validators, registered one at a time in `Startup` |
| OneOf | 3.0.271 | `OneOf<TEnvelope, FailureResponse>` result type |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | EF Core against PostgreSQL |
| NetEscapades.Configuration.Yaml | 3.1.0 | Lets `IConfiguration` read `config.yml` |
| Microsoft.Extensions.AI.Abstractions | 10.10.1 | `IChatClient`, the seam the LLM sits behind |
| OllamaSharp | 5.3.1 | `IChatClient` over a local Ollama — pinned, see below |
| dbup-postgresql | 7.0.1 | Migrations, in `Tarscord.DbMigrator` only |
| xunit / FluentAssertions | 2.9.3 / 8.11.0 | Tests |
| NSubstitute | 6.2.0 | Faking Discord.Net interfaces and `IChatClient` |
| Microsoft.Extensions.TimeProvider.Testing | 10.10.0 | `FakeTimeProvider` |
| Testcontainers.PostgreSql | 4.15.0 | The integration suite's own database |
| Roslynator.Analyzers | 5.0.0 everywhere | Analyzers |

**OllamaSharp is pinned to 5.3.1 on purpose.** 5.4.x ships a source generator built against a
newer Roslyn than the 10.0.201 SDK's compiler, which raises `CS9057` in every project that sees it
— and CI builds with `-warnaserror`. `ExcludeAssets="analyzers"` does not suppress it. Bump it only
after checking the build is still clean.

The schema is **not** EF migrations. It's hand-written SQL in
`src/Tarscord.DbMigrator/Migrations/`, embedded as resources and applied by DbUp in filename
order. A model change needs a new numbered `.sql` file; EF will not generate one for you and
`EnsureCreated` is never called.

## Things that will bite you

Every item here was verified in the repository, not inferred. The long list this section used to
hold is gone — those commands are fixed and have tests. What is left is what still surprises people.

**Discord's Message Content intent is privileged.** `Startup` requests
`GatewayIntents.MessageContent` because text commands cannot work without it. If it isn't enabled in
the Developer Portal the bot connects, logs nothing unusual, and ignores every command. A "the bot
does nothing" report is this until proven otherwise.

**A command runs in its own DI scope, and that is load-bearing.** `TarscordContext` is scoped, and
`ProcessMessage` creates a scope per message and hands it to `commands.ExecuteAsync`. Resolve
anything scoped from the root provider and `ValidateScopes` fails at startup. The bot ran for years
sharing one `DbContext` across the whole process; don't reintroduce that.

**`DefaultRunMode` is `Sync`, deliberately.** Under `RunMode.Async`, `ExecuteAsync` returns success
before the command body runs, so an exception inside a module is reported as success and vanishes.
Sync is what makes a failure reach the user and the log. It does mean a slow command occupies the
gateway callback; that is the trade made.

**The schema is hand-written SQL, and the model must match it.** A property without a `[Column]`
attribute maps to a quoted PascalCase column that does not exist. `ulong` maps to `numeric(20,0)`
unless `OnModelCreating` converts it to `long` — every Discord id needs that conversion. Money needs
`HasPrecision`. All of this failed silently before, as wrong values rather than errors.

**Migrations are applied by filename order and never edited afterwards.** DbUp journals what it has
run in `schemaversions`, so changing an applied script does nothing on any database that already has
it. Add a new numbered file. `v1.03` had to be corrected by `v1.05` for exactly this reason.

**Source is UTF-8 without a BOM, declared in `.editorconfig`, and every `.cs` file is pure ASCII.**
The tree used to be mixed — 16 files with a BOM, the rest without — and an editor reading a non-BOM
file containing a non-ASCII character guesses wrong and reports it as loaded in the wrong encoding.
Don't reintroduce a BOM, and write a symbol like the euro as `\u20AC`.

**`config.yml` is optional; `config.example.yml` is not.** The example file is the defaults layer and
is loaded with `optional: false`; `config.yml` sits on top with `optional: true`. Add a new key to
the example with a placeholder or nothing will read it. Both are copied to the output directory.

**Ollama is optional at runtime and must stay that way.** `Features/Personality/Generate.cs` has no
`FailureResponse` arm: on any transport failure it logs a warning and returns the caller's fixed
line. If you add something that talks to the model, it degrades the same way. Nothing that people
rely on reading — loans, events, help — goes near it.

**`?loan payback` picks the most recent open loan.** Not the oldest. That is what the original
`LastOrDefault` was reaching for, and it is a behaviour choice, not an accident.

**The `confirmed` column on `loans` is written once, as false, and never read.** Its intended meaning
is unclear — probably "the borrower acknowledged the loan", for which there is no command. Whether a
loan is settled is `amount_payed < amount_loaned`. Don't repurpose `confirmed` without deciding what
it means.

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

> No bare `throw new Exception` remains. The four that did — in `InteractionModule`,
> `ReminderModule`, `RandomNumberModule` and `Setup/InitializeBot.cs` — became replies the user can
> read, except the token check, which is a configuration failure and throws
> `InvalidOperationException`. In a module, a `FailureResponse` is almost always what you want.
>
> The two `BackgroundService` loops do catch `Exception`, which the rule above forbids. That is
> deliberate and commented at both sites: a background loop that lets an exception escape stops
> running, and the feature goes silent with nothing in the log. They rethrow
> `OperationCanceledException` so shutdown still works.

**Async** — [Asynchronous programming scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios).
`async void` only for event handlers. Suffix async methods with `Async`. Never `.Result` or
`.Wait()`; `await` instead. Don't mark a method `async` if it has nothing to await — there are no
`await Task.CompletedTask` bodies left, and one appearing again means a stub got committed.

> The deleted `TimerService.StartTimerAsync` passed `async void (_) => await mediator.Send(...)` as a
> `System.Threading.Timer` callback, where an exception is unobservable and takes the process down.
> That is the canonical `async void` trap and the reason background work here is a
> `BackgroundService` with a `PeriodicTimer` instead.

**Analyzers** — [Code analysis in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).
`EnableNETAnalyzers` is on by default for .NET 5+, so `CAxxxx` rules already run, and CI builds
with `-warnaserror`, so any of them failing fails the build. `IDExxxx` code-style rules still do
run too, because `.editorconfig` is the template `dotnet new editorconfig` generates and
`Directory.Build.props` sets `EnforceCodeStyleInBuild`. The template ships every rule as a
suggestion, which a build ignores, so two are raised to warnings: `IDE0055` (formatting) and
`IDE0011` (braces on every `if`). `dotnet format` fixes both automatically. Raise more rather than
lowering these.

## Testing

Two projects, and both must stay green:

- `tests/Tarscord.Core.Tests` — unit tests. No database, no gateway, no model. Fast enough to run on
  every save.
- `tests/Tarscord.IntegrationTests` — one throwaway PostgreSQL per run, with the **real DbUp
  migrations** applied to it, shared by every test in the collection. `PostgresFixture.ResetAsync()`
  truncates before a test that makes assertions about whole tables.

Handler logic belongs in the unit project. A handler's *query* belongs in the integration project:
the defect that made `?loan payback` throw for its entire existence was LINQ that EF Core could not
translate, which no in-memory provider would have caught. If a handler touches `TarscordContext`,
its test runs against PostgreSQL.

Three tests are worth knowing about because they guard whole classes of mistake:
`StartupTests.ConfigureServices_ForAMediatRHandler_ResolvesItWithAllItsDependencies` resolves every
handler in the assembly and so catches a validator nobody registered;
`CommandSurfaceTests` walks the discovered command surface and fails if anything lacks a summary;
`SchemaRoundTripTests` writes and reads every entity, which is what keeps the model and the SQL
honest.

The standard is
[Microsoft's unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices):

- Name them `Method_Scenario_ExpectedBehavior` — `Handle_AmountExceedsBalance_ReturnsFailure`.
- Arrange / Act / Assert, marked with comments, in that order.
- No `if`, `for` or string-building inside a test. Use `[Theory]` with `[InlineData]` instead.
- One Act per test. Assign hard-coded values to named constants rather than inlining them.
- No infrastructure. A unit test touches no database and no gateway.
- Test through public behaviour, not private methods.
- Inject seams for anything ambient. `TimeProvider` is already injected into the handlers for
  exactly this reason — use it rather than freezing the clock globally.

Handlers are the natural unit here: they take injected dependencies and return a value. Use
`FakeTimeProvider` for the clock, `NullLogger<T>.Instance` for logging, and `NSubstitute` for
Discord.Net's interfaces and `IChatClient`.

Nothing in either suite talks to a real Discord gateway or a real language model. Verifying
generation against Ollama is a manual step, because a test that needs a model running is neither a
unit test nor deterministic.

## CI

`.github/workflows/dotnet.yml` runs `dotnet restore`, `dotnet build -warnaserror` and
`dotnet test` in Release on every push and pull request targeting `master`, on `ubuntu-latest` with
the 10.0.x SDK.

There is no service container for PostgreSQL: the integration suite starts its own through
Testcontainers, using the Docker daemon the runner already provides. There is no Ollama on the
runner either, which is fine — the model is behind `IChatClient` and every test fakes it.

It installs the SDK with an explicit `dotnet-version: 10.0.x` rather than
`global-json-file: global.json`, deliberately: `global.json` pins `8.0.0` and leans on
`rollForward: latestMajor` to reach a newer SDK, which works on a developer machine that has one
but would leave a runner holding only 8.0.0 unable to build `net10.0`.

It passes `-warnaserror`. The tree is warning-clean and the point is to keep it that way; the
three `CS8604` warnings that used to be the baseline came from `AdminModule` handing a nullable
`IUser` to a non-nullable parameter and are gone.

## Style

Explain *why* in comments, not *what* — the code already says what. A comment that survives is
one that records a decision or a trap, not one that narrates the line below it. `// Create the
command context` above `new SocketCommandContext(...)` is the kind this repo has too many of.

Don't add a third-party dependency without a reason you can state in the commit body.

Delete code you replace. This repo used to be full of commented-out bodies — `EventModule` was
roughly half of them — and working out whether each was disabled deliberately cost real time. None
remain. Don't start again.

## Working on this repo as an agent

**Verify before you claim.** Run the build and both suites and report what they actually said. The
baseline is zero warnings and zero failures, and `dotnet build` on an up-to-date tree prints
`0 Warning(s)` without recompiling anything — use `--no-incremental` when the count is the thing you
are checking. Don't call something fixed until you've run it.

**Check whether it ever worked.** Less true than it was, but still worth doing: establish what a
command does today before assuming the bug is the one described.

**Stay inside the task.** This file lists a lot of known problems. They are context, not a
backlog to work through. Fixing an unrelated one in the same change makes the diff unreviewable —
mention it instead.

**Prefer the existing pattern.** Feature file, envelope, `OneOf`, thin module. If a change seems
to need a new architectural concept, say so and ask rather than introducing a second way of doing
things alongside the first.
