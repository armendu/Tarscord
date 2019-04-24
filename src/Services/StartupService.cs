using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordRandomNumber.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands,
            IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            // Get the discord token from the config file
            string discordToken = _config["tokens:discord"];
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception(
                    "Please enter your bots token into the `config.yml` file found in the applications root directory.");

            // Login to discord
            await _discord.LoginAsync(TokenType.Bot, discordToken);

            // Connect to the websocket
            await _discord.StartAsync();

            // Load commands and modules into the command service
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),
                _provider);
        }
    }
}