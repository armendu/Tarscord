using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Features.Personality;

public class SetLevelsTests
{
    [Fact]
    public async Task Handle_ForAValidSarcasmLevel_ChangesIt()
    {
        // Arrange
        var personality = NewPersonality();
        var handler = NewHandler(personality);

        // Act
        var response = await handler.Handle(
            new SetLevels.Command(SetLevels.Trait.Sarcasm, 8, "alice"), CancellationToken.None);

        // Assert
        response.AsT0.SarcasmLevel.Should().Be(8);
        personality.SarcasmLevel.Should().Be(8);
    }

    [Fact]
    public async Task Handle_ForAValidHumorLevel_ChangesIt()
    {
        // Arrange
        var personality = NewPersonality();
        var handler = NewHandler(personality);

        // Act
        var response = await handler.Handle(
            new SetLevels.Command(SetLevels.Trait.Humor, 3, "alice"), CancellationToken.None);

        // Assert
        response.AsT0.HumorLevel.Should().Be(3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Handle_ForALevelOutOfRange_ReturnsFailureAndChangesNothing(int level)
    {
        // Arrange
        var personality = NewPersonality();
        var handler = NewHandler(personality);

        // Act
        var response = await handler.Handle(
            new SetLevels.Command(SetLevels.Trait.Sarcasm, level, "alice"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("between 0 and 10");
        personality.SarcasmLevel.Should().Be(5);
    }

    private static BotPersonality NewPersonality() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["sarcasm-level"] = "5",
                ["humor-level"] = "5"
            })
            .Build());

    private static SetLevels.CommandHandler NewHandler(BotPersonality personality) =>
        new(NullLogger<SetLevels.CommandHandler>.Instance, personality, new SetLevels.CommandValidator());
}
