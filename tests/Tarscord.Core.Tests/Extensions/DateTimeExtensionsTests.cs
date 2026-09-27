using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using System.Globalization;
using Tarscord.Core.Extensions;
using Xunit;

namespace Tarscord.Core.Tests.Extensions;

/// <summary>What the parser rejects matters as much as what it accepts.</summary>
public class DateTimeExtensionsTests
{
    // A Friday, so "next friday" has to roll forward a whole week rather than return today.
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("today", "2026-05-01T00:00:00")]
    [InlineData("tomorrow", "2026-05-02T00:00:00")]
    [InlineData("in 5 minutes", "2026-05-01T12:05:00")]
    [InlineData("in 2 hours", "2026-05-01T14:00:00")]
    [InlineData("in 3 days", "2026-05-04T12:00:00")]
    [InlineData("in 2 weeks", "2026-05-15T12:00:00")]
    [InlineData("in 1 month", "2026-06-01T12:00:00")]
    [InlineData("in 1 year", "2027-05-01T12:00:00")]
    [InlineData("next monday", "2026-05-04T00:00:00")]
    [InlineData("next friday", "2026-05-08T00:00:00")]
    public void FromTextToDate_WithARelativeDate_ResolvesItAgainstTheProvidedClock(
        string input, string expected)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        var result = input.FromTextToDate(timeProvider);

        // Assert
        result.Should().Be(Utc(expected));
    }

    [Theory]
    [InlineData("2026-05-01 18:30", "2026-05-01T18:30:00")]
    [InlineData("2026-12-24", "2026-12-24T00:00:00")]
    public void FromTextToDate_WithAnAbsoluteDate_ParsesIt(string input, string expected)
    {
        // None of these worked before: the parser understood four phrasings and nothing else.

        // Arrange
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        var result = input.FromTextToDate(timeProvider);

        // Assert
        result.Should().Be(Utc(expected));
    }

    [Theory]
    [InlineData("IN 3 DAYS", "2026-05-04T12:00:00")]
    [InlineData("  tomorrow  ", "2026-05-02T00:00:00")]
    public void FromTextToDate_WithOddCasingOrPadding_StillParses(string input, string expected)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        var result = input.FromTextToDate(timeProvider);

        // Assert
        result.Should().Be(Utc(expected));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("whenever")]
    [InlineData("in 5 minutes tomorrow")]
    [InlineData("blah in 5 days blah")]
    [InlineData("friday")]
    [InlineData("in 99999999999 days")]
    [InlineData("in 2000000 years")]
    [InlineData("in -3 days")]
    public void FromTextToDate_WithTextItCannotRead_ReturnsNull(string input)
    {
        // Unanchored patterns matched mid-sentence, and a large number threw out of the handler.

        // Arrange
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        var result = input.FromTextToDate(timeProvider);

        // Assert
        result.Should().BeNull();
    }

    private static DateTime Utc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    [Fact]
    public void FromTextToDate_ForARelativeDate_ReturnsUtc()
    {
        // The value goes straight into a timestamptz column.

        // Arrange
        var timeProvider = new FakeTimeProvider(Now);

        // Act
        var result = "in 1 hour".FromTextToDate(timeProvider);

        // Assert
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
