using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace Tarscord.IntegrationTests.Services;

/// <summary>Runs a background loop until it has finished one tick, which ends when its scope is disposed.</summary>
internal sealed class OneTick(IServiceScopeFactory inner) : IServiceScopeFactory
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IServiceScope CreateScope() => new SignallingScope(inner.CreateScope(), _finished);

    public async Task RunAsync(BackgroundService loop, FakeTimeProvider timeProvider)
    {
        await loop.StartAsync(CancellationToken.None);

        // ExecuteAsync starts on the thread pool, so its timer may not exist yet when time first moves.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        while (!_finished.Task.IsCompleted)
        {
            deadline.Token.ThrowIfCancellationRequested();
            timeProvider.Advance(PollInterval);
            await Task.WhenAny(_finished.Task, Task.Delay(50));
        }

        await loop.StopAsync(CancellationToken.None);
    }

    private sealed class SignallingScope(IServiceScope inner, TaskCompletionSource finished) : IServiceScope
    {
        public IServiceProvider ServiceProvider => inner.ServiceProvider;

        public void Dispose()
        {
            inner.Dispose();
            finished.TrySetResult();
        }
    }
}
