using Discord.Commands;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core;
using Xunit;

namespace Tarscord.Core.Tests.Modules;

public class CommandSurfaceTests
{
    [Fact]
    public async Task ModuleDiscovery_FindsTheWholeCommandSurface()
    {
        // Arrange
        using var commands = await BuildCommandServiceAsync();

        // Act
        int commandCount = commands.Commands.Count();

        // Assert
        commandCount.Should().BeGreaterThan(15);
    }

    [Fact]
    public async Task EveryCommand_HasASummary()
    {
        // Arrange
        using var commands = await BuildCommandServiceAsync();

        // Act
        var withoutSummary = commands.Commands
            .Where(command => string.IsNullOrWhiteSpace(command.Summary))
            .Select(command => command.Aliases.First())
            .ToList();

        // Assert
        withoutSummary.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryModule_HasAName()
    {
        // Arrange
        using var commands = await BuildCommandServiceAsync();

        // Act
        var namedAfterTheirType = commands.Modules
            .Where(module => module.Name.EndsWith("Module", StringComparison.Ordinal))
            .Select(module => module.Name)
            .ToList();

        // Assert
        namedAfterTheirType.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryCommandParameter_HasASummary()
    {
        // Arrange
        using var commands = await BuildCommandServiceAsync();

        // Act
        var withoutSummary = commands.Commands
            .SelectMany(command => command.Parameters.Select(
                parameter => new { Command = command.Aliases.First(), parameter.Name, parameter.Summary }))
            .Where(parameter => string.IsNullOrWhiteSpace(parameter.Summary))
            .Select(parameter => $"{parameter.Command}:{parameter.Name}")
            .ToList();

        // Assert
        withoutSummary.Should().BeEmpty();
    }

    private static async Task<CommandService> BuildCommandServiceAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["prefix"] = "?",
                ["tarscord-context:connection-string"] =
                    "Host=localhost;Username=root;Password=password;Database=tarscord_db"
            })
            .Build();

        var services = new ServiceCollection();
        Startup.ConfigureServices(services, configuration);

        using var provider = services.BuildServiceProvider();
        var commands = provider.GetRequiredService<CommandService>();

        await commands.AddModulesAsync(typeof(Startup).Assembly, provider);

        return commands;
    }
}
