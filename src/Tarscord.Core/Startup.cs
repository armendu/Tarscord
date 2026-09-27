using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;
using Tarscord.Core.Services;

namespace Tarscord.Core;

public class Startup
{
    public IConfigurationRoot Configuration { get; }

    public Startup()
    {
        // config.example.yml supplies the defaults for every key and is always present, so a clone
        // with no config.yml of its own still starts and fails with a readable message about the
        // token rather than a FileNotFoundException before logging exists.
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddYamlFile("Resources/config.example.yml", optional: false, reloadOnChange: true)
            .AddYamlFile("Resources/config.yml", optional: true, reloadOnChange: true);
        Configuration = builder.Build();
    }

    public static Task RunAsync()
        => new Startup().RunBotAsync();

    private async Task RunBotAsync()
    {
        // Create a new instance of a service collection
        var services = new ServiceCollection();
        ConfigureServices(services, Configuration);

        // ValidateScopes turns "scoped service resolved from the root provider" into a startup
        // failure instead of a shared-DbContext bug that only shows under concurrent commands.
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        // Start the logging service, and the command handler service
        provider.GetRequiredService<LoggingService>();
        provider.GetRequiredService<CommandHandler>();

        // Start the startup service
        await provider.GetRequiredService<StartupService>().StartAsync();

        await provider.GetRequiredService<ReminderDispatcher>().StartAsync(CancellationToken.None);
        await provider.GetRequiredService<RestrictionExpirySweeper>().StartAsync(CancellationToken.None);

        await Task.Delay(-1);
    }

    internal static void ConfigureServices(IServiceCollection services, IConfigurationRoot configuration)
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
                // Sync, not Async: with Async, ExecuteAsync returns success before the command body
                // runs, so every exception a module throws was swallowed and the user saw silence.
                DefaultRunMode = RunMode.Sync,
            }))
            // Also as IDiscordClient, so handlers can depend on the interface and be tested.
            .AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>())
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<ReminderDispatcher>()
            .AddSingleton<RestrictionExpirySweeper>()
            .AddLogging(builder => builder.AddSimpleConsole(options => options.TimestampFormat = "HH:mm:ss "))
            .AddSingleton(configuration)
            .AddDatabase(configuration)
            .AddSingleton(TimeProvider.System)
            // Validators are wired one by one and injected by the handler that uses them. There is
            // no validation pipeline behavior, so a validator that is not listed here does nothing.
            .AddScoped<IValidator<Features.Events.Details.Query>, Features.Events.Details.QueryValidator>()
            .AddScoped<IValidator<Features.Events.Delete.Command>, Features.Events.Delete.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.Confirm.Command>, Features.EventAttendees.Confirm.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.Cancel.Command>, Features.EventAttendees.Cancel.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.List.Query>, Features.EventAttendees.List.QueryValidator>()
            .AddScoped<IValidator<Features.Loans.Create.Command>, Features.Loans.Create.CommandValidator>()
            .AddScoped<IValidator<Features.Loans.Update.Command>, Features.Loans.Update.CommandValidator>()
            .AddScoped<IValidator<Features.Reminders.Create.Command>, Features.Reminders.Create.CommandValidator>()
            .AddScoped<IValidator<Features.Restrictions.Apply.Command>, Features.Restrictions.Apply.CommandValidator>()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
    }
}