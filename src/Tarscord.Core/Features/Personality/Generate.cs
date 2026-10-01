using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

public static class Generate
{
    // The gateway callback waits on this, so it is a stall budget rather than a model budget.
    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(5);

    public sealed record Command(string Prompt, string Fallback, ulong UserId, string PerformedByUser)
        : IPerformedByUser;

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        IChatClient chatClient,
        IConfigurationRoot configuration,
        BotPersonality personality,
        GenerationCooldown cooldown)
    {
        public async Task<GeneratedMessageEnvelope> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Generate), command.PerformedByUser);

            if (!IsConfigured(configuration))
            {
                return new GeneratedMessageEnvelope(command.Fallback, FromModel: false);
            }

            // Spent only here, after the configured check, so a call that never happens costs nothing.
            if (!cooldown.TryGenerate(command.UserId))
            {
                logger.LogInformation("{PerformedByUser} is on cooldown; using a canned reply instead",
                    command.PerformedByUser);

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
    }

    private static bool IsConfigured(IConfiguration configuration) =>
        configuration.OllamaUrl() is not null && configuration.OllamaModel() is not null;
}
