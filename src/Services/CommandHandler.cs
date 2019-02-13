using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordRandomNumber.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage message)) return;

            // Ignore self when checking commands
            if (message.Author.Id == _discord.CurrentUser.Id)
                return;

            var context = new SocketCommandContext(_discord, message); // Create the command context

            int argPos = 0;

            // Check if the message has a valid command prefix
            if (message.HasStringPrefix(_config["prefix"], ref argPos) ||
                message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                // Execute the command
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                // If not successful, reply with the error
                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}