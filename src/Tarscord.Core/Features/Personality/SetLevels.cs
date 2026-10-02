using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

public static class SetLevels
{
    public enum Trait
    {
        Sarcasm,
        Humor
    }

    public sealed record Command(Trait Which, int Level, string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Level)
                .InclusiveBetween(BotPersonality.MinimumLevel, BotPersonality.MaximumLevel)
                .WithMessage(
                    $"Pick a level between {BotPersonality.MinimumLevel} and {BotPersonality.MaximumLevel}.");
        }
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        BotPersonality personality,
        IValidator<Command> validator)
    {
        public async Task<OneOf<LevelsEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(SetLevels), command.PerformedByUser);

            if (await validator.FailureAsync(command, cancellationToken) is { } failure)
            {
                return failure;
            }

            switch (command.Which)
            {
                case Trait.Sarcasm:
                    personality.SetSarcasmLevel(command.Level);
                    break;

                case Trait.Humor:
                    personality.SetHumorLevel(command.Level);
                    break;

                default:
                    return new FailureResponse($"I do not know what '{command.Which}' is.");
            }

            return new LevelsEnvelope(personality.SarcasmLevel, personality.HumorLevel);
        }
    }
}
