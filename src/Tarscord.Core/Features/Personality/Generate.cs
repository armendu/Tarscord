using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

/// <summary>Falls back to the caller's fixed line when the model is down.</summary>
internal static class Generate
{
    public record Command(string Prompt, string Fallback, string PerformedByUser)
        : IRequest<GeneratedMessageEnvelope>, IPerformedByUser;

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        IChatClient chatClient,
        IConfigurationRoot configuration,
        BotPersonality personality) : IRequestHandler<Command, GeneratedMessageEnvelope>
    {
        // The gateway callback waits on this, so it is a stall budget rather than a model budget.
        private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(5);

        public async Task<GeneratedMessageEnvelope> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Generate), command.PerformedByUser);

            if (!IsConfigured())
            {
                return new GeneratedMessageEnvelope(command.Fallback, FromModel: false);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(GenerationTimeout);

            try
            {
                List<ChatMessage> messages =
                [
                    new(ChatRole.System, personality.SystemPrompt),
                    new(ChatRole.User, command.Prompt)
                ];

                var options = new ChatOptions { Temperature = personality.Temperature };

                var response = await chatClient.GetResponseAsync(messages, options, timeout.Token);

                string generated = response.Text.Trim();

                if (generated.Length > 0)
                {
                    return new GeneratedMessageEnvelope(generated, FromModel: true);
                }

                logger.LogWarning("The model returned an empty reply");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Everything but the caller's cancellation: a closed list kept missing cases.
                logger.LogWarning(exception, "Asking the model failed; using a canned reply instead");
            }

            return new GeneratedMessageEnvelope(command.Fallback, FromModel: false);
        }

        /// <summary>Nothing callable configured means there is nothing to ask, so don't try.</summary>
        private bool IsConfigured() =>
            configuration.OllamaUrl() is not null && configuration.OllamaModel() is not null;
    }
}
