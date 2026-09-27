using Discord.Commands;
using Discord.WebSocket;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core;
using Tarscord.Core.Persistence;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Setup;

/// <summary>
/// Guards the composition root. Every command in the bot is resolved through this container, so a
/// missing registration shows up here rather than as a silent failure on one command.
/// </summary>
public class StartupTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Username=root;Password=password;Database=tarscord_db";

    [Theory]
    [InlineData(typeof(DiscordSocketClient))]
    [InlineData(typeof(CommandService))]
    [InlineData(typeof(CommandHandler))]
    [InlineData(typeof(LoggingService))]
    [InlineData(typeof(StartupService))]
    [InlineData(typeof(IMediator))]
    [InlineData(typeof(TimeProvider))]
    public void ConfigureServices_ForAGatewayService_ResolvesIt(Type serviceType)
    {
        // Arrange
        using var provider = BuildProvider();

        // Act
        object? service = provider.GetService(serviceType);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ForTheDbContext_ResolvesItInsideAScope()
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        var context = scope.ServiceProvider.GetService<TarscordContext>();

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ForTheDbContext_RefusesToResolveFromTheRoot()
    {
        // A scoped DbContext resolved from the root provider is shared by the whole process, which is
        // what made concurrent commands collide on one change tracker.

        // Arrange
        using var provider = BuildProvider();

        // Act
        Action act = () => provider.GetService<TarscordContext>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["prefix"] = "?",
                ["tarscord-context:connection-string"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        Startup.ConfigureServices(services, configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
