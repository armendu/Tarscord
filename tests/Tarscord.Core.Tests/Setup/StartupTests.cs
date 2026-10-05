using Discord.Commands;
using Discord.WebSocket;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tarscord.Core;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Persistence;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Setup;

public class StartupTests
{
    private const string ConnectionString =
        "Host=localhost;Username=root;Password=password;Database=tarscord_db";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfigureServices_WithNoConnectionString_RefusesToStart(string? configured)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["prefix"] = "?",
                ["tarscord-context:connection-string"] = configured
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        Action act = () => Startup.ConfigureServices(services, configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*");
    }

    [Theory]
    [InlineData(typeof(DiscordSocketClient))]
    [InlineData(typeof(CommandService))]
    [InlineData(typeof(CommandHandler))]
    [InlineData(typeof(LoggingService))]
    [InlineData(typeof(StartupService))]
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
        // Arrange
        using var provider = BuildProvider();

        // Act
        Action act = () => provider.GetService<TarscordContext>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(Slices))]
    public void ConfigureServices_ForASlice_ResolvesItWithAllItsDependencies(Type slice)
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        object? handler = scope.ServiceProvider.GetService(slice);

        // Assert
        handler.Should().NotBeNull();
    }

    public static TheoryData<Type> Slices()
    {
        var slices = typeof(Startup).Assembly.GetTypes()
            .Where(type => type.IsNested && type.Name == "Handler");

        var data = new TheoryData<Type>();

        foreach (var slice in slices)
        {
            data.Add(slice);
        }

        return data;
    }

    // Modules are not registered in DI; each command builds one from its scope, as this does.
    [Theory]
    [MemberData(nameof(Modules))]
    public void ConfigureServices_ForAModule_BuildsItWithAllItsDependencies(Type module)
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        object instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, module);

        // Assert
        instance.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void EveryModule_TakesTheVoice_SoNoReplyIsLeftUnvoiced(Type module)
    {
        // Arrange
        var constructor = module.GetConstructors().Single();

        // Act
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType);

        // Assert
        parameterTypes.Should().Contain(typeof(Voice.Handler));
    }

    public static TheoryData<Type> Modules()
    {
        var modules = typeof(Startup).Assembly.GetTypes()
            .Where(type => typeof(IModuleBase).IsAssignableFrom(type) && !type.IsAbstract);

        var data = new TheoryData<Type>();

        foreach (var module in modules)
        {
            data.Add(module);
        }

        return data;
    }

    [Fact]
    public async Task StartLoopAsync_WhenTheLoopFails_LogsIt()
    {
        // Arrange
        var logger = Substitute.For<ILogger<Startup>>();

        // Act
        await Startup.StartLoopAsync(new FailingLoop(), logger);

        // Assert
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    private sealed class FailingLoop : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            throw new InvalidOperationException("The loop broke");
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
