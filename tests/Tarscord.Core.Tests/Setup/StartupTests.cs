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
        // Arrange
        using var provider = BuildProvider();

        // Act
        Action act = () => provider.GetService<TarscordContext>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(RequestHandlers))]
    public void ConfigureServices_ForAMediatRHandler_ResolvesItWithAllItsDependencies(Type handlerService)
    {
        // Arrange
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        object? handler = scope.ServiceProvider.GetService(handlerService);

        // Assert
        handler.Should().NotBeNull();
    }

    public static TheoryData<Type> RequestHandlers()
    {
        Type[] handlerInterfaces = [typeof(IRequestHandler<,>), typeof(IRequestHandler<>)];

        var services = typeof(Startup).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType
                               && handlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
            .Distinct();

        var data = new TheoryData<Type>();

        foreach (var service in services)
        {
            data.Add(service);
        }

        return data;
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
