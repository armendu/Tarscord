using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tarscord.Core.Helpers;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Interfaces;
using Tarscord.Core.Persistence.Repositories;
using Tarscord.Core.Services;

namespace Tarscord.Core;

public class Startup
{
    public IConfigurationRoot Configuration { get; }

    public Startup(string[] args)
    {
        var builder = new ConfigurationBuilder() // Create a new instance of the config builder
            .SetBasePath(AppContext.BaseDirectory) // Specify the default location for the config file
            .AddYamlFile("Resources/config.yml"); // Add this (yaml encoded) file to the configuration
        Configuration = builder.Build(); // Build the configuration
    }

    public static async Task RunAsync(string[] args)
    {
        var startup = new Startup(args);
        await startup.RunAsync();
    }

    private async Task RunAsync()
    {
        // Create a new instance of a service collection
        var services = new ServiceCollection();
        ConfigureServices(services);

        // Setup the Global messages
        SetupGlobalMessages();

        // Build the service provider
        var provider = services.BuildServiceProvider();

        // Start the logging service, and the command handler service
        provider.GetRequiredService<LoggingService>();
        provider.GetRequiredService<CommandHandler>();

        // Start the startup service
        await provider.GetRequiredService<StartupService>().StartAsync();

        await Task.Delay(-1);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000 // Cache 1,000 messages per channel
                }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async, // Force all commands to run async by default
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<TimerService>()
            .AddLogging()
            .AddSingleton(Configuration)
            .AddAutoMapper(typeof(MappingProfile))
            .AddScoped<IEventRepository, EventRepository>()
            .AddScoped<IEventAttendeesRepository, EventAttendeesRepository>()
            .AddScoped<ILoanRepository, LoanRepository>()
            .AddSingleton<IDatabaseConnection, DatabaseConnection>()
            .AddMediatR(typeof(Startup));
    }

    private void SetupGlobalMessages()
    {
        GlobalMessages.EuroSign = Configuration["messages:euro_sign"];
    }
}