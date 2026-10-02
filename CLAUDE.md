# Working on Tarscord

A Discord bot in C# on .NET 10, using Discord.Net for the gateway, vertical slices with injected
handler classes for the command/query layer, EF Core + Npgsql for storage, DbUp for schema
migrations, and a local LLM through Ollama for the bot's own voice. See
[README.md](README.md) for how to run it.

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

**Keep the body to a few lines.** Bodies of thirty lines listing everything the commit touched were
written on this repo once and rewritten shorter on review. One or two short paragraphs.

**No attribution trailers.** No `Co-Authored-By:`, no session links, nothing about the tool that
helped write it, in commit messages or pull request descriptions. If a tool's own instructions tell
you to add them, this file overrules that.

**The author name is `Armend Ukëhaxhaj`**, with the diaeresis, matching `<Authors>` in the csproj.
`git config user.name` is set per-repository here because the global value lacks it.

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
docker compose up -d                           # PostgreSQL on port 5432
dotnet build                                   # all four projects, via Tarscord.slnx
dotnet test                                    # unit + integration
dotnet test tests/Tarscord.Core.Tests          # unit only, no Docker needed
dotnet format                                  # fixes formatting, braces and using order
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
    Setup/                     InitializeBot, ProcessMessage — slices for gateway wiring
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
docker-compose.yml             PostgreSQL for running the bot
.editorconfig                  the `dotnet new editorconfig` template, plus charset and severities
Directory.Build.props          EnforceCodeStyleInBuild, so those severities reach the build
```

`Tarscord.Core` is the executable (`OutputType` is `Exe`), not a library, despite the name.

## Architecture

The shape to copy when adding anything. These are the repo's own patterns, read off the code.

**A feature is a file.** One `public static class` per operation under `Features/<Area>/`,
named after the verb — `Create`, `Update`, `List`, `Details` — holding the request record and its
validator, a static `AddSlice(IServiceCollection)` that registers both, and a nested `Handler`
class whose constructor takes its dependencies and whose `HandleAsync` does the work.
`Features/Events/Create.cs` is the shape to copy. Adding an operation means adding a file and one
`AddSlice` line in `Startup`, never widening a service class. A record that would hold nothing or a
single value is left out and the value passed directly: `Reminders/Complete.cs` takes the id.

**DI builds the handler, so `ValidateOnBuild` checks it.** A handler nobody registered, or one
missing a dependency, stops `Startup` building its provider. `AddSlice` registers the handler by
type for exactly that reason; a factory lambda would hide it from that check.

**Slices are public because modules are.** Discord.Net only discovers a public module, and a
public constructor cannot take an internal handler (CS0051). `IPerformedByUser` and
`IEmbeddedMessage` stay internal, which a public type implementing them allows.

**Modules stay thin.** A module parses Discord input, guards what Discord's parser cannot
(a missing mention), calls one slice's `Handler`, and replies. No EF Core, no business
rules, no `DbContext`. `Modules/LoanModule.cs` and `Modules/AdminModule.cs` are the shape to copy.

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

**Every reply is an embed**, generated ones included, so the bot never answers in two styles. The
reminder's ping is the only plain text, because an embed cannot mention anyone.

**Take time from `TimeProvider`.** It's registered in `Startup` and injected into every handler
and extension that needs a clock, which is what makes them testable with `FakeTimeProvider`. Never
`DateTime.Now` anywhere but a test. `Features/Logging/ProcessLog.cs` is the one place still reading
the clock ambiently, for a log filename and a timestamp; it predates the rule and nothing asserts on
it.

**Validators are wired one at a time.** There is no validation pipeline. A validator runs only if
its `Handler` takes an `IValidator<T>` *and* calls it; the slice's own `AddSlice` registers it
beside the handler that needs it, so a validator nobody registered fails `ValidateOnBuild`. What nothing catches is a validator that is registered and passed in but never called.

**Background work is a `BackgroundService` with a scope per tick.** `ReminderDispatcher` and
`RestrictionExpirySweeper` are the two. `TarscordContext` is scoped, so each tick opens an
`IServiceScope` and resolves the handlers it needs from it. Both catch and log rather
than letting an exception escape, because a loop that dies takes its whole feature with it silently.
There is no host: `Startup.StartLoopAsync` starts each one and logs if it ever stops, and nothing
ever cancels them, so the bot keeps answering commands with that loop gone until it is restarted.

**Depend on `IDiscordClient`, not `DiscordSocketClient`, in a handler.** The concrete client's
members are not virtual, so a handler that takes it cannot be tested at all. Both are registered.

## The build

All four projects target `net10.0`, and `global.json` asks for `10.0.100` with
`"rollForward": "latestFeature"` — so any 10.0.x SDK works (10.0.201 resolves here) but an SDK from a
future major does not, which makes a bump deliberate rather than silent.

| Package | Version | What it's for |
|---|---|---|
| Discord.Net | 3.20.1 | Gateway client and the text-command framework |
| FluentValidation | 12.1.1 | Validators, each registered by its slice's `AddSlice` |
| OneOf | 3.0.271 | `OneOf<TEnvelope, FailureResponse>` result type |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | EF Core against PostgreSQL |
| NetEscapades.Configuration.Yaml | 3.1.0 | Lets `IConfiguration` read `config.yml` |
| Microsoft.Extensions.Configuration | 10.0.12 | `IConfigurationRoot`, injected into handlers |
| Microsoft.Extensions.Hosting | 10.0.12 | `BackgroundService` for the two loops |
| Microsoft.Extensions.AI.Abstractions | 10.10.1 | `IChatClient`, the seam the LLM sits behind |
| OllamaSharp | 5.3.1 | `IChatClient` over a local Ollama — pinned, see below |
| dbup-postgresql | 7.0.1 | Migrations, in `Tarscord.DbMigrator` only |
| xunit / FluentAssertions | 2.9.3 / 8.11.0 | Tests |
| NSubstitute | 6.2.0 | Faking Discord.Net interfaces and `IChatClient` |
| Microsoft.Extensions.TimeProvider.Testing | 10.10.0 | `FakeTimeProvider` |
| Testcontainers.PostgreSql | 4.15.0 | The integration suite's own database |
| Roslynator.Analyzers | 5.0.0 | Analyzers, in every project but `Tarscord.DbMigrator` |

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

**Discord.Net builds every module once at registration, not only per command.** `AddModulesAsync`
constructs each module from the provider it is given, and modules take scoped handlers,
so `InitializeBot.AddModulesAsync` hands it a scope. Given the root provider, `ValidateScopes`
throws and the bot never starts. The first draft of the MediatR removal did exactly that, with
every test green, because `CommandSurfaceTests` built its provider without `ValidateScopes`.

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
Don't reintroduce a BOM, and write a symbol like the euro as `"\u20AC"`.

**A list command shows `max-listed` rows and then says there are more.** The key defaults to 10 in
`config.example.yml`, and `ConfigurationExtensions.MaxListed()` falls back to 10 for a missing or
unusable value and caps the rest at 100, because `TakeListedAsync` reads one row past the limit and
`max-listed: 2147483647` overflowed that into `LIMIT must not be negative`. A hundred rows is already
more than an embed description can show. The two `List` slices the background services use are deliberately
uncapped: they are work queues, and a cap would mean the eleventh due reminder never fires.

**Text limits come from Discord, not from taste.** `Persistence/TextLengths.cs` holds them - `Name` is
256 because that is all an embed title shows, `FreeText` is 2000 because that is all Discord lets
someone type, and `Organizer` is the 200 that `event_organizer` already had - and the validators and
the `VARCHAR` widths follow them. A
validator that disagrees with its column turns a readable reply into an unhandled Postgres error, so
change the migration and the constant together; `MigrationsTests` fails if a validated column's width
drifts from its constant.

**Nothing is scoped to a guild, so the bot is single-guild by assumption.** No table has a
`guild_id` column and no query filters on one, and `event_infos.id` is `SERIAL`. In a second guild,
`?event list` and `?event confirmed` would show the first guild's events and attendee names, and
`?event confirm` would write into them. Deleting and cancelling are safe, they check
`EventOrganizerId`. Loans filter by user id; reminders and restrictions are channel-scoped. Adding a
column and a predicate is the fix and it is its own change.

**An event can be named instead of numbered.** `EventInfos.MatchAsync` takes the text as an id when
it parses as one, otherwise as a name, and by name it takes the latest **active** event. So by id you
can still reach a cancelled event and be told it is cancelled; by name you cannot see it at all.

**The connection string is the one key with no default.** Every other key falls back to
`config.example.yml`, so forgetting one is silent; `tarscord-context:connection-string` is empty
there and `AddDatabase` throws, because the alternative is a deployment quietly running against
`localhost` with the development password. A test pins it.

**`config.yml` is optional; `config.example.yml` is not.** The example file is the defaults layer and
is loaded with `optional: false`; `config.yml` sits on top with `optional: true`. Add a new key to
the example with a placeholder or nothing will read it. Both are copied to the output directory.

**A generated reply is on a 5 second timeout and a 20 second per-user cooldown.** Both are about the
gateway, not the model: `DefaultRunMode` is `Sync` and `CommandHandler` returns the task it gets, so
the command body runs on the gateway callback and a slow model delays every other event. `Generate`
owns both, and spends the cooldown only once it is about to call the model, so an unconfigured bot
never uses it up; on cooldown it returns the caller's fallback. It enters the typing state at that
same point, in the channel the caller passes, so a fallback never shows typing for nothing. Callers
don't enter it themselves. A mention wants silence on cooldown instead, so
`ProcessMessage` peeks with `GenerationCooldown.IsCoolingDown` first and never spends it itself;
unconfigured, then, every mention gets the canned line, which is cheap and stalls nothing. Measured
on this machine, a warm `llama3.1` answers one of these prompts in about half a second and a cold
load took 4.5 seconds, so the first call after Ollama idles can lose the race and fall back.

**Ollama is optional at runtime and must stay that way.** `Features/Personality/Generate.cs` has no
`FailureResponse` arm: on any transport failure it logs a warning and returns the caller's fixed
line. It also skips the request outright when `ollama:url` or `ollama:model` is blank, so an
unconfigured bot takes the fallback without waiting on a call that cannot work. If you add something
that talks to the model, it degrades the same way.

What the model is allowed to touch is the *voice*, never the data: `?dare` and a reply to a mention
are entirely generated, and `?random` and `?event list` generate only the line around a number and a
list that were both produced deterministically. Never route the numbers or the rows through it.

**`Apply` saves before calling Discord; `Lift` calls Discord before saving.** Opposite orders, for
the same reason — leave the row as the thing that still needs doing. If `Apply` denied in Discord
first and then failed to save, someone would be muted with no row, so nothing would ever expire it.
If `Lift` marked the row lifted first and then failed in Discord, the person would stay denied with
nothing left to find them, because the sweeper only looks at rows where `Lifted` is false. This was
the wrong way round for `Lift` until a review caught it.

**`dotnet format` will make every `DbSet` nullable.** It rewrites
`public DbSet<Loan> Loans { get; set; }` to `DbSet<Loan>?`, which is wrong — EF assigns them — and
produces about sixty `CS8604` and `CS8602` warnings in the handlers. They are `= null!;` for this
reason. Check `TarscordContext` after running it.

**`dotnet format` also separates import groups if you let it.**
`dotnet_separate_import_directive_groups` is `false` in `.editorconfig` on purpose; with it `true`,
the formatter puts a blank line between `using System...`, `using Discord...` and the rest, and
setting it back to `false` does not remove the blanks it already inserted.

**`using SomeNamespace;` imports types, not nested namespaces.** `using Tarscord.Core.Features;` does
not make `EventAttendees.Confirm` resolvable — the nested namespace is not in scope. That is what
sent an earlier version of `EventModule` reaching for a `using` alias.

**A private `const` can supply its own class's attribute.** `[Name(ModuleName)]` on `HelpModule`
compiles with `private const string ModuleName` inside it, which is why the name the self-exclusion
check compares against is not duplicated.

**Two modules can share a `[Group]`.** `EventModule` and `EventAttendanceModule` are both
`[Group("event")]` and Discord.Net merges them. `?help` groups its sections by `[Name]`, so give both
the same one or the same group appears twice.

**Testcontainers hands back an address that may not connect.** Rancher Desktop publishes container
ports on IPv4 only while `localhost` resolves to `::1` first, so `PostgresFixture` pins the host to
`127.0.0.1`. It also waits for the database to answer *from the host*: the container reports itself
ready as soon as `pg_isready` succeeds inside it, but a desktop runtime forwards the port through a VM
and that forward lags. Without both, the suite fails intermittently.

**NSubstitute refuses a substitute created inside `Returns()`.** `discord.GetUserAsync(id).Returns(
Task.FromResult(NewUser()))` throws `CouldNotSetReturnDueToNoLastCallException`. Build the inner
substitute first, then pass it.

**`?loan payback` picks the most recent open loan.** Not the oldest. That is what the original
`LastOrDefault` was reaching for, and it is a behaviour choice, not an accident.

**`confirmed` on `loans` means the borrower acknowledged it, not that it is settled.** Anyone can
record that someone owes them money; only that borrower can `?loan confirm` it. Whether a loan is
settled is `amount_payed < amount_loaned`.

## Code quality

This project follows the mainstream .NET conventions rather than a house style. Each rule below
links to where it comes from, so you can check it instead of taking this file's word for it.

**Naming** — [Framework Design Guidelines: Capitalization Conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions).
PascalCase for namespaces, types, methods, properties, events and constants; camelCase for
parameters. No underscores in public identifiers. Two-letter acronyms keep both letters
capitalised (`IOStream`), longer ones don't (`HtmlTag`). Names never differ by case alone.

**Layout and fields** — [dotnet/runtime C# Coding Style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md),
which is what this codebase's existing `_mediator`-style fields already follow. Private and
internal instance fields are `_camelCase`. Note that `s_` for statics, which that guide asks for, is
**not** used here: `.editorconfig` enforces PascalCase for a `private static readonly` field and the
build checks it, so the enforced rule wins. Allman braces, four spaces, no tabs.
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
> Four places catch `Exception`, which the rule above forbids, and all four are deliberate and
> commented. The two `BackgroundService` loops do it because a loop that lets an exception escape
> stops running and the feature goes silent with nothing in the log; both rethrow
> `OperationCanceledException`. `Startup.StartLoopAsync` does it to log whatever still escapes them. `Features/Personality/Generate.cs` does it because the model is
> optional and a closed list of transport exceptions kept missing cases.

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
run too, because `.editorconfig` is the template `dotnet new editorconfig` generates — the .NET
recommendations, unedited apart from `charset`, line endings and the import-group setting — and
`Directory.Build.props` sets `EnforceCodeStyleInBuild`.

The template ships every rule as a **suggestion**, which a build ignores, so `EnforceCodeStyleInBuild`
on its own reports nothing. Three groups are raised to `warning` here:

| Rule | What it caught when first enabled |
|---|---|
| `IDE0055` | formatting, mostly using-directive order — **232** violations |
| `IDE0011` | braces on every `if`, including single-line ones — **84** violations |
| `IDE1006` | naming; the template wants PascalCase for a `private static readonly` field, **not** an `s_` prefix, which is the opposite of what the dotnet/runtime style above says — the template wins here because it is what the build checks |

`dotnet format` fixes all three automatically, with the two caveats in *Things that will bite you*.

**Fix the code, not the config.** If a recommended rule fires, the answer is to change the code.
Raise more rules rather than lowering these; the only settings deliberately altered from the template
are encoding and the import-group blank line, both because they were wrong for this repo rather than
inconvenient.

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

Four tests are worth knowing about because they guard whole classes of mistake:
`StartupTests.ConfigureServices_ForASlice_ResolvesItWithAllItsDependencies` resolves every
`Handler` in the assembly and so catches a slice or a validator nobody registered;
`StartupTests.ConfigureServices_ForAModule_BuildsItWithAllItsDependencies` builds every module
from a scope, the way each command does;
`CommandSurfaceTests` registers the modules through `InitializeBot.AddModulesAsync`, as startup does,
and fails if anything lacks a summary;
`SchemaRoundTripTests` writes and reads every entity, which is what keeps the model and the SQL
honest. Add a case to it when you add an entity, or the guard quietly stops covering everything.

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
the SDK from `global-json-file: global.json`. It used to name `10.0.x` explicitly because
`global.json` pinned `8.0.0` and a runner holding only that could not build `net10.0`; the pin says
10 now, so the workflow and the repo agree from one place.

There is no service container for PostgreSQL: the integration suite starts its own through
Testcontainers, using the Docker daemon the runner already provides. There is no Ollama on the
runner either, which is fine — the model is behind `IChatClient` and every test fakes it.

It passes `-warnaserror`. The tree is warning-clean and the point is to keep it that way; the
three `CS8604` warnings that used to be the baseline came from `AdminModule` handing a nullable
`IUser` to a non-nullable parameter and are gone.

## Style

**Explain *why* in comments, not *what*** — the code already says what. A comment that survives is
one that records a decision or a trap, not one that narrates the line below it. `// Create the
command context` above `new SocketCommandContext(...)` is the kind this repo has too many of.

**One line per comment.** Not a paragraph, and not a `<remarks>` block that restates the summary in
longer words. If it needs three lines, the code probably needs the change instead. This was the most
frequent review comment on the branch that fixed the commands, by a distance.

**Prefer the simpler construct.** Things that were written and then removed on review, all of them
replaced by something shorter: a source-generated `[GeneratedRegex]` for two patterns matched once
per command (two `static readonly Regex` fields do it, and the class stops needing to be `partial`);
a shared `EventMessages` class for two strings (inlined); a currency symbol threaded from
configuration through three handlers, an envelope and a module so it could be configured (a `const`
on the envelope). Generality nobody asked for reads as complexity to the person reviewing it.

**Name a constant for what it holds, not for what it does.** `MentionSomeone` held a message and
read like a command; the message is inlined at its two call sites now.

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

**Don't open an issue for what an open pull request turns up.** A problem found while a pull
request is unmerged, by review or while working on it, is fixed on that pull request. That is not
the known-problems list above: it is the change's own unfinished business, and an issue filed for it
is a promise that gets dropped once the pull request merges. #30 was opened during #29 and folded
back into it for this reason.

**Prefer the existing pattern.** Feature file, envelope, `OneOf`, thin module. If a change seems
to need a new architectural concept, say so and ask rather than introducing a second way of doing
things alongside the first. Three things were added on one branch and removed on review for exactly
this: a messages class no sibling feature had, `ListDue` and `ListExpired` when every other slice is
a single verb, and a `using` alias hiding a type collision.

**Fix a collision, don't alias it.** `EventModule` once aliased
`using Attendees = Tarscord.Core.Features.EventAttendees;` because its own `Confirm` method and
`Events.List` collided with the attendee slices of the same name. The attendance commands moved to
`EventAttendanceModule` instead. Renaming or splitting beats hiding.

**Replacing commented-out code means the command still has to work.** Deleting the commented body of
a registered command is only acceptable when the command is reimplemented and tested in the same
change. `?event remove`, `confirm`, `cancel` and `confirmed` were four such bodies; they could not
be uncommented, because they called an `IEventAttendeesRepository` and an `IMapper` that do not exist
in this repo, so they were rewritten against `TarscordContext`.

**In a handler that writes the database and calls Discord, order the two so the row is what still
needs doing.** That is not the same order in both directions — see *`Apply` saves before calling
Discord; `Lift` calls Discord before saving* under *Things that will bite you*.

**Read the file before answering a question about it.** Every claim in this document that says
"verified" was checked by running something. Answering from memory about this codebase has produced
wrong answers.
