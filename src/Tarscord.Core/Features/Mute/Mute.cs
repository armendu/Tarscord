using Discord;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Features.Mute;

internal static class Mute
{
    public record Command(
        IMessageChannel ContextChannel,
        IUser User,
        CommandType Action,
        string PerformedByUser,
        int Minutes = 0)
        : IRequest<string>, IPerformedByUser;

    internal sealed class CommandHandler(ILogger<CommandHandler> logger) : IRequestHandler<Command, string>
    {
        public async Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(Mute.Command), request.PerformedByUser);

            // TODO: After the specified minutes, the user should be un muted.
            if (request.ContextChannel is not IGuildChannel channel) return "";

            OverwritePermissions? possiblePermissions = channel.GetPermissionOverwrite(request.User);
            OverwritePermissions overwritePermissions = new OverwritePermissions();

            string messageToBeShownByBot = "No action taken.";

            switch (request.Action)
            {
                case CommandType.Mute:
                    overwritePermissions = possiblePermissions?.Modify(sendMessages: PermValue.Deny) ??
                                           new OverwritePermissions(sendMessages: PermValue.Deny);
                    messageToBeShownByBot = $"The user '{request.User.Username}' was muted.";
                    break;

                case CommandType.Unmute:
                    if (possiblePermissions is OverwritePermissions permissions)
                        overwritePermissions = permissions.Modify(sendMessages: PermValue.Allow);

                    messageToBeShownByBot = $"The user '{request.User.Username}' was unmuted.";
                    break;

                case CommandType.DenyReacting:
                    overwritePermissions = possiblePermissions?.Modify(addReactions: PermValue.Deny)
                                           ?? new OverwritePermissions(addReactions: PermValue.Deny);

                    messageToBeShownByBot = $"The user '{request.User.Username}' has been stopped from reacting.";
                    break;
            }

            await channel.AddPermissionOverwriteAsync(request.User, overwritePermissions);
            return messageToBeShownByBot;
        }
    }

    internal enum CommandType
    {
        Mute,
        Unmute,
        DenyReacting
    }
}