using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Personality;

internal static class SetLevels
{
    public enum Trait
    {
        Sarcasm,
        Humor
    }

    public record Command(Trait Which, int Level, string PerformedByUser)
        : IRequest<OneOf<LevelsEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Level)
                .InclusiveBetween(BotPersonality.MinimumLevel, BotPersonality.MaximumLevel)
                .WithMessage(
                    $"Pick a level between {BotPersonality.MinimumLevel} and {BotPersonality.MaximumLevel}.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        BotPersonality personality,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<LevelsEnvelope, FailureResponse>>
    {
        public async Task<OneOf<LevelsEnvelope, FailureResponse>> Handle(
            Command command,
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
}
