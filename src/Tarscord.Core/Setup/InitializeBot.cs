using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Setup;

public static class InitializeBot
{
    public record Command : IRequest<Unit>;

    public class Handler : IRequestHandler<Command, Unit>
    {
        private const string PlaceholderToken = "YOUR_DISCORD_BOT_TOKEN";

        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public Handler(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _config = config;
        }

        public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            string? discordToken = _config["tokens:discord"];

            // A configuration failure, not something a user can act on, so it throws.
            if (string.IsNullOrWhiteSpace(discordToken) || discordToken == PlaceholderToken)
            {
                throw new InvalidOperationException(
                    "No Discord bot token is configured. Copy Resources/config.example.yml to " +
                    "Resources/config.yml and put your token in tokens.discord.");
            }

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            return Unit.Value;
        }
    }
}