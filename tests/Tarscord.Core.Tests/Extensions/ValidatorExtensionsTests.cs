using FluentAssertions;
using FluentValidation;
using Tarscord.Core.Extensions;
using Xunit;

namespace Tarscord.Core.Tests.Extensions;

public class ValidatorExtensionsTests
{
    private const string TooSmall = "Pick a bigger number.";
    private const string TooOdd = "Pick an even number.";

    [Fact]
    public async Task FailureAsync_ForAValidRequest_ReturnsNull()
    {
        // Arrange
        var validator = NewValidator();

        // Act
        var failure = await validator.FailureAsync(4, CancellationToken.None);

        // Assert
        failure.Should().BeNull();
    }

    [Fact]
    public async Task FailureAsync_ForAnInvalidRequest_JoinsEveryMessage()
    {
        // Arrange
        var validator = NewValidator();

        // Act
        var failure = await validator.FailureAsync(-1, CancellationToken.None);

        // Assert
        failure!.ErrorMessage.Should().Be($"{TooSmall} {TooOdd}");
    }

    private static InlineValidator<int> NewValidator()
    {
        var validator = new InlineValidator<int>();
        validator.RuleFor(number => number).GreaterThan(0).WithMessage(TooSmall);
        validator.RuleFor(number => number).Must(number => number % 2 == 0).WithMessage(TooOdd);

        return validator;
    }
}
