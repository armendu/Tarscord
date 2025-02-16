using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddYamlFile("Resources/config.yml", optional: false, reloadOnChange: true);
        Configuration = builder.Build();
    }

    public static async Task RunAsync(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var startup = new Startup(args);
                startup.ConfigureServices(services);
                startup.SetupGlobalMessages();
            })
            .Build();

        await host.RunAsync();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000,
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<TimerService>()
            .AddLogging()
            .AddSingleton(Configuration)
            .AddAutoMapper(typeof(Startup))
            .AddScoped<IEventRepository, EventRepository>()
            .AddScoped<IEventAttendeesRepository, EventAttendeesRepository>()
            .AddScoped<ILoanRepository, LoanRepository>()
            .AddSingleton<IDatabaseConnection, DatabaseConnection>()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
    }

    private void SetupGlobalMessages()
    {
        GlobalMessages.EuroSign = Configuration["messages:euro_sign"];
    }
}