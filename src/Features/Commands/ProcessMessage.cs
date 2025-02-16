using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Features.Commands;

public abstract class ProcessMessage
{
    public record Command : IRequest<bool>
    {
        public SocketMessage Message { get; init; }
    }

    public class Handler(
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config,
        IServiceProvider provider)
        : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Message is not SocketUserMessage message) 
                return false;

            // Ignore self when checking commands
            if (message.Author.Id == discord.CurrentUser.Id)
                return false;

            // Create the command context
            var context = new SocketCommandContext(discord, message);

            int argPos = 0;

            // Check if the message has a valid command prefix
            if (message.HasStringPrefix(config["prefix"], ref argPos) ||
                message.HasMentionPrefix(discord.CurrentUser, ref argPos))
            {
                // Execute the command
                var result = await commands.ExecuteAsync(context, argPos, provider);

                // If not successful, reply with the error
                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }

            return true;
        }
    }
}