using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tarscord.Core.Setup;

public static class InitializeBot
{
    public delegate Task Handle();

    public static void AddSlice(IServiceCollection services) =>
        services.AddSingleton<Handle>(provider =>
        {
            var discord = provider.GetRequiredService<DiscordSocketClient>();
            var commands = provider.GetRequiredService<CommandService>();
            var config = provider.GetRequiredService<IConfigurationRoot>();

            return () => HandleAsync(provider, discord, commands, config);
        });

    public static async Task HandleAsync(
        IServiceProvider provider,
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config)
    {
        string? discordToken = config["tokens:discord"];

        if (string.IsNullOrWhiteSpace(discordToken))
        {
            throw new InvalidOperationException(
                "No Discord bot token is configured. Copy Resources/config.example.yml to " +
                "Resources/config.yml and put your token in tokens.discord.");
        }

        await discord.LoginAsync(TokenType.Bot, discordToken);
        await discord.StartAsync();
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
    }
}
