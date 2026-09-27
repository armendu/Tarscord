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
| `?dare <@user>` | Dares someone to say it out loud |
| `?event list` | Every event on record |
| `?event show <id>` (`info`, `get`, `display`, `details`) | One event in full |
| `?event create <name> <when>` (`add`, `make`, `generate`) | Add an event |
| `?loan list` (`show`) | Loans you're on either side of |
| `?loan to <@user> <amount> <why>` | Record that you lent someone money |

Event dates are written the way you'd say them: `today`, `tomorrow`, `in 3 days`,
`in 2 weeks`, `next friday`.

Owner-only, and the bot needs Manage Roles in the channel to apply them:

| Command | What it does |
|---|---|
| `?mute <@user> [minutes]` | Deny Send Messages in the channel |
| `?unmute <@user>` | Give it back |
| `?denyreacting <@user> [minutes]` | Deny Add Reactions in the channel |

### Not working yet

These are registered, so `?help` will offer them, but they don't do what they claim. See
[CLAUDE.md](CLAUDE.md) for the details:

- `?remindme <minutes> <message>` — throws before it reaches the reminder
- `?loan payback <@user> <amount>` (`return`, `payloan`, …) — throws on the database query
- `?event remove | confirm | cancel | confirmed` — the bodies are commented out
- `?sarcasm-level <n>` — accepts your number and discards it

The `[minutes]` argument on `mute` and `denyreacting` is also accepted and ignored; the mute
stays until you lift it by hand.

## Running it

### You'll need

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL, reachable and with an empty database ready
- A Discord application — [Developer Portal](https://discord.com/developers/applications) →
  **New Application** → **Bot** → **Reset Token**, and keep the token

### 1. Clone and configure

```sh
git clone https://github.com/armendu/Tarscord.git
cd Tarscord
cp src/Tarscord.Core/Resources/config.example.yml src/Tarscord.Core/Resources/config.yml
```

Open `src/Tarscord.Core/Resources/config.yml` and put your token in `tokens.discord`. That file
is gitignored, so your token stays on your machine.

**Don't skip this step.** The bot loads `config.yml` as a required file and throws on startup if
it isn't there.

### 2. Turn on the Message Content intent

Back in the Developer Portal, under **Bot → Privileged Gateway Intents**, enable
**Message Content**.

Discord made this one privileged in 2022, and Tarscord asks for it because text commands can't
work without reading what people type. Leave it off and the bot logs in, reports itself online,
and then ignores every single command with no error anywhere. It is the usual reason a first run
looks dead.

### 3. Create the schema

The migrator applies the SQL in `src/Tarscord.DbMigrator/Migrations/` with
[DbUp](https://dbup.readthedocs.io). It takes the connection string as its first argument:

```sh
dotnet run --project src/Tarscord.DbMigrator -- "Host=localhost;Username=root;Password=password;Database=tarscord_db"
```

Run with no argument and it falls back to exactly that localhost string.

### 4. Start the bot

```sh
dotnet run --project src/Tarscord.Core
```

`dotnet build` from the repository root works too — the solution is a `Tarscord.slnx`, which
every project in the repo belongs to.

### 5. Invite it

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

The rest of the file — `sarcasm-level`, `humor-level`, `messages.euro_sign` — is never read by
any code. Loan output hard-codes its `€`. Changing those values does nothing today.

## Layout

```
src/Tarscord.Core/        the bot: Discord modules, features, EF Core persistence
src/Tarscord.DbMigrator/  DbUp console app, applies Migrations/*.sql
tests/                    xUnit
```

## Contributing

Pull requests are welcome. Branch off `master` as `feat/<topic>`, keep commits in
[Conventional Commits](https://www.conventionalcommits.org) form, and open a PR.

[CLAUDE.md](CLAUDE.md) is the real contributor guide — the architecture, the coding standard and
its sources, and an honest list of what's currently broken. Read it before writing code.

## License

MIT. See [LICENSE](LICENSE).
