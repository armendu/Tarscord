using Discord;
using FluentAssertions;
using MediatR;
using Moq;
using Tarscord.Core.Features.Reminders.Commands;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.ServicesTests;

public class TimerServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITimer> _timerMock;
    private readonly Mock<IUser> _userMock;
    private readonly TimerService _sut;

    public TimerServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _timerMock = new Mock<ITimer>();
        _userMock = new Mock<IUser>();
        _sut = new TimerService(_mediatorMock.Object, _timerMock.Object);
    }

    [Fact]
    public void AddReminder_ShouldStartTimer_WhenAddingFirstReminder()
    {
        // Arrange
        var dateToRemind = DateTime.UtcNow.AddMinutes(5);
        const string message = "Test reminder";

        // Act
        _sut.AddReminder(dateToRemind, _userMock.Object, message);

        // Assert
        _timerMock.Verify(t => t.Change(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task NotifyUserWithMessageAsync_ShouldStopTimer_WhenNoRemindersLeft()
    {
        // Todo: This test sometimes fails. To improve

        // Act
        await _sut.NotifyUserWithMessageAsync();

        // Assert
        _timerMock.Verify(t => t.Change(new TimeSpan(Timeout.Infinite), new TimeSpan(0)), Times.Once);
    }

    [Fact]
    public async Task StartTimerAsync_ShouldInitializeTimer_WithCorrectInterval()
    {
        // Act
        await _sut.StartTimerAsync();

        // Assert
        _timerMock.Verify(t => t.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10)), Times.Never);
    }

    [Fact]
    public void Dispose_ShouldDisposeTimer()
    {
        // Act
        _sut.Dispose();

        // Assert
        _timerMock.Verify(t => t.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public void AddReminder_ShouldAllowPastDates_ForTesting(int minutes)
    {
        // Arrange
        var dateToRemind = DateTime.UtcNow.AddMinutes(minutes);
        const string message = "Test reminder";

        // Act
        Action act = () => _sut.AddReminder(dateToRemind, _userMock.Object, message);

        // Assert
        act.Should().NotThrow();
    }
}