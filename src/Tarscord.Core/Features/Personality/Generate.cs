using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

/// <summary>
/// Asks the local model for something to say.
/// </summary>
/// <remarks>
/// There is no FailureResponse arm: a reply the user can read is always produced, because a model
/// that is not running is not the user's problem. The caller supplies the line to fall back to.
/// </remarks>
internal static class Generate
{
    public record Command(string Prompt, string Fallback, string PerformedByUser)
        : IRequest<GeneratedMessageEnvelope>, IPerformedByUser;

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        IChatClient chatClient,
        BotPersonality personality) : IRequestHandler<Command, GeneratedMessageEnvelope>
    {
        // Ollama on a laptop is not fast, but a Discord reply that takes longer than this reads as
        // broken, and the canned line is better than nothing.
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
                    return new GeneratedMessageEnvelope(generated, FromModel: true);

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
                // Ollama not running is the normal case on a fresh machine, so this is a warning and a
                // canned line rather than a failure the user has to read about.
                logger.LogWarning(exception, "Asking the model failed; using a canned reply instead");
            }

            return new GeneratedMessageEnvelope(command.Fallback, FromModel: false);
        }
    }
}
