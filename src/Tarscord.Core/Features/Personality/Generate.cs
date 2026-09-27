using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

/// <summary>Asks the local model for something to say.</summary>
/// <remarks>No FailureResponse arm: the caller's fallback is used when the model is down.</remarks>
internal static class Generate
{
    public record Command(string Prompt, string Fallback, string PerformedByUser)
        : IRequest<GeneratedMessageEnvelope>, IPerformedByUser;

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        IChatClient chatClient,
        BotPersonality personality) : IRequestHandler<Command, GeneratedMessageEnvelope>
    {
        // Past this a Discord reply reads as broken, and the canned line is better.
        private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(20);

        public async Task<GeneratedMessageEnvelope> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Generate), command.PerformedByUser);

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
            catch (Exception exception) when (exception is HttpRequestException
                                                 or OperationCanceledException
                                                 or IOException
                                                 or InvalidOperationException)
            {
                // Ollama being down is normal, so warn and fall back rather than fail.
                logger.LogWarning(exception, "Asking the model failed; using a canned reply instead");
            }

            return new GeneratedMessageEnvelope(command.Fallback, FromModel: false);
        }
    }
}
