using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tarscord.Core.Setup;

public static class InitializeBot
{
    public static void AddSlice(IServiceCollection services) =>
        services.AddSingleton<Handler>();

    internal static async Task AddModulesAsync(CommandService commands, IServiceProvider provider)
    {
        // Discord.Net builds every module once here, and modules take scoped slices.
        using var scope = provider.CreateScope();

        await commands.AddModulesAsync(typeof(InitializeBot).Assembly, scope.ServiceProvider);
    }

    public sealed class Handler
    {
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

        public async Task HandleAsync()
        {
            string? discordToken = _config["tokens:discord"];

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new InvalidOperationException(
                    "No Discord bot token is configured. Copy Resources/config.example.yml to " +
                    "Resources/config.yml and put your token in tokens.discord.");
            }

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();
            await AddModulesAsync(_commands, _provider);
        }
    }
}
