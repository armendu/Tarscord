# Tarscord 🤖

A Discord bot named after TARS from *Interstellar*. It mutes people, tracks events, remembers
who owes whom money, and rolls dice, with a dry remark where it fits.

Built on [Discord.Net](https://discordnet.dev) 3.20, .NET 10 and PostgreSQL.

> **Tarscord uses text commands, not slash commands.** Every command starts with the prefix
> from your config file, which is `?` out of the box, so it's `?help`, not `/help`.

## Commands

`?help` lists everything the bot will let you run, and `?help <command>` explains one of them.

| Command | What it does |
|---|---|
| `?help` / `?help <command>` | List commands, or explain one |
| `?random <min> <max>` (`?r`) | A random number between two bounds |
| `?dare <@user>` | Dares someone, in the bot's current voice |
| `@Tarscord <anything>` | Mention it with no command and it answers, once every 20s per person |
| `?event list` | Every event on record |
| `?event show <id>` (`info`, `get`, `display`, `details`) | One event in full |
| `?event create <name>, <when>, <description>` (`add`, `make`, `generate`) | Add an event |
| `?event remove <name or id>` (`delete`) | Cancel an event you organized |
| `?event confirm <name or id> [@users]` | Confirm attendance, yours or someone else's |
| `?event cancel <name or id> [@users]` (`unattend`) | Withdraw attendance; someone else's is the organizer's to do |
| `?event confirmed <name or id>` | Who has confirmed for an event |
| `?loan list` (`show`) | Loans you're on either side of |
| `?loan to <@user> <amount> <why>` | Record that you lent someone money |
| `?loan payback <@user> <amount>` (`return`, `payloan`, …) | Pay back money you owe |
| `?remindme <minutes> <message>` | Reminds you in the channel you asked in |

The three parts of `?event create` are separated by commas, because a name and a date are both
usually several words:

```
?event create Release party, next friday, in the usual place
```

Anywhere an event is named you can give its name instead of its id, and the latest active event with
that name is used. `confirm` and `cancel` take mentions after it, so a multi-word name needs quotes
there: `?event confirm "Release party" @bob`.

The description is optional. Dates can be written the way you'd say them — `today`, `tomorrow`,
`in 3 days`, `in 2 weeks`, `next friday` — or given outright as `2026-05-01 18:30`.

Owner-only:

| Command | What it does |
|---|---|
| `?sarcasm-level <0-10>` | How sarcastic it is |
| `?humor-level <0-10>` (`humour-level`) | How funny it tries to be |

Owner-only, and the bot needs Manage Roles in the channel to apply them:

| Command | What it does |
|---|---|
| `?mute <@user> [minutes]` | Deny Send Messages in this channel |
| `?unmute <@user>` | Give it back |
| `?denyreacting <@user> [minutes]` | Deny Add Reactions in this channel |
| `?allowreacting <@user>` (`allowreactions`) | Give that back |

Leave `[minutes]` out and the restriction stays until you lift it; give a number and the bot lifts
it for you when the time is up. Expiry is checked every 15 seconds, so it is approximate.

Every command `?help` offers does what it says. If one misbehaves, that's a bug — the
[CLAUDE.md](CLAUDE.md) list of known-broken commands is history now.

## Running it

### You'll need

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker or Rancher Desktop, for the bundled PostgreSQL — or your own Postgres if you'd rather
- A Discord application — [Developer Portal](https://discord.com/developers/applications) →
  **New Application** → **Bot** → **Reset Token**, and keep the token

### 1. Clone and configure

```sh
git clone https://github.com/armendu/Tarscord.git
cd Tarscord
cp src/Tarscord.Core/Resources/config.example.yml src/Tarscord.Core/Resources/config.yml
```

Open `src/Tarscord.Core/Resources/config.yml` and set `tokens.discord` and
`tarscord-context.connection-string`. That file is gitignored, so your token stays on your machine.

`config.example.yml` is read first and supplies a default for every key except the connection
string, which has none on purpose — the bot refuses to start rather than pointing itself at a
database you did not choose. For the bundled compose stack use
`Host=localhost;Username=root;Password=password;Database=tarscord_db`.

### 2. Turn on the Message Content intent

Back in the Developer Portal, under **Bot → Privileged Gateway Intents**, enable
**Message Content**.

Discord made this one privileged in 2022, and Tarscord asks for it because text commands can't
work without reading what people type. Leave it off and the bot logs in, reports itself online,
and then ignores every single command with no error anywhere. It is the usual reason a first run
looks dead.

### 3. Start PostgreSQL

```sh
docker compose up -d
```

That brings up `tarscord-postgres` with the `tarscord_db` database on port 5432.

### 4. Create the schema

The migrator applies the SQL in `src/Tarscord.DbMigrator/Migrations/` with
[DbUp](https://dbup.readthedocs.io). It takes the connection string as its first argument:

```sh
dotnet run --project src/Tarscord.DbMigrator -- "Host=localhost;Username=root;Password=password;Database=tarscord_db"
```

Run with no argument and it falls back to exactly that localhost string.

### 5. Start the bot

```sh
dotnet run --project src/Tarscord.Core
```

`dotnet build` from the repository root works too — the solution is a `Tarscord.slnx`, which
every project in the repo belongs to.

### 6. Invite it

In the Developer Portal under **OAuth2 → URL Generator**, tick `bot`, pick the permissions you
want it to have (Manage Roles and Manage Messages for the moderation commands), then open the
generated URL.

## Configuration

Three keys in `src/Tarscord.Core/Resources/config.yml` actually do something:

| Key | What it's for |
|---|---|
| `tokens.discord` | Your bot token. Required |
| `tarscord-context.connection-string` | PostgreSQL connection string. Required |
| `prefix` | The character that starts a command. Default `?` |
| `sarcasm-level`, `humor-level` | 0–10, the starting values for the bot's voice |
| `ollama.url`, `ollama.model` | Where the local model lives, and which one |

### The local model

`?dare`, `?random`, the heading of `?event list` and replies to a mention are written by a local LLM
through [Ollama](https://ollama.com), shaped by `sarcasm-level` and `humor-level`:

```sh
ollama pull llama3.1
ollama serve
```

It is optional. If nothing answers on `ollama.url`, the bot logs a warning and replies with a fixed
line instead, so no command breaks because the model is down, and a blank `ollama.url` or
`ollama.model` skips the call altogether. The model only writes the wrapper: `?random` still draws
its number with `Random.Shared`, and `?event list` still lists the same events. Every other reply — loans, events,
help — is deterministic and never goes near the model.

`?sarcasm-level 8` and `?humor-level 3` change the voice while the bot is running. They are held in
memory, so a restart goes back to the values in `config.yml`.

## Layout

```
src/Tarscord.Core/        the bot: Discord modules, features, EF Core persistence
src/Tarscord.DbMigrator/  DbUp console app, applies Migrations/*.sql
tests/Tarscord.Core.Tests/        unit tests, no infrastructure
tests/Tarscord.IntegrationTests/  runs the real migrations against a throwaway Postgres
```

## Tests

```sh
dotnet test tests/Tarscord.Core.Tests          # unit, fast, no Docker needed
dotnet test tests/Tarscord.IntegrationTests    # starts its own Postgres via Testcontainers
```

The integration suite needs a Docker socket. It finds one automatically for Docker Desktop,
Rancher Desktop and Colima; set `DOCKER_HOST` if yours lives somewhere else.

## Contributing

Pull requests are welcome. Branch off `master` as `feat/<topic>`, keep commits in
[Conventional Commits](https://www.conventionalcommits.org) form, and open a PR.

[CLAUDE.md](CLAUDE.md) is the real contributor guide — the architecture, the coding standard and
its sources, and an honest list of what's currently broken. Read it before writing code.

## License

MIT. See [LICENSE](LICENSE).
