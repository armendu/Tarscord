using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordRandomNumber
{
    class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;

        static void Main() => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            // Add this for logging.
            _client.Log += Log;

            _services = new ServiceCollection()
                .BuildServiceProvider();

            await InstallCommands();

            await _client.LoginAsync(TokenType.Bot, Token.Value);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommand;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
                return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            // Create a Command Context
            var context = new CommandContext(_client, message);

            await Log(
                new LogMessage(LogSeverity.Info, this.GetType().FullName,
                    $"User {message.Author} @ {DateTime.UtcNow} wrote: {message}"));

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess)
            {
                if (result.Error is CommandError.ParseFailed)
                    await context.Channel.SendMessageAsync("Please provide a positive number.");
                else
                    await context.Channel.SendMessageAsync(result.ErrorReason);

                await Log(new LogMessage(LogSeverity.Error, " ",
                    $"The following error occurred: {result.ErrorReason}"));
            }
        }
    }
}