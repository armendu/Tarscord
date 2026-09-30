using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
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

    public delegate Task<OneOf<LevelsEnvelope, FailureResponse>> Handle(
        Command command,
        CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handle>(provider =>
            {
                var personality = provider.GetRequiredService<BotPersonality>();
                var validator = provider.GetRequiredService<IValidator<Command>>();
                var logger = provider.GetRequiredService<ILogger<Command>>();

                return (command, cancellationToken) =>
                    HandleAsync(command, personality, validator, logger, cancellationToken);
            });

    public static async Task<OneOf<LevelsEnvelope, FailureResponse>> HandleAsync(
        Command command,
        BotPersonality personality,
        IValidator<Command> validator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Command {Command} executed by {PerformedByUser}",
            nameof(SetLevels), command.PerformedByUser);

        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new FailureResponse(
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
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
