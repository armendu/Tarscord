using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using Tarscord.Core.Persistence;
using Tarscord.Core.Services;

namespace Tarscord.Core;

public class Startup
{
    public IConfigurationRoot Configuration { get; }

    public Startup()
    {
        // The example file is the defaults layer, so a clone without config.yml still starts.
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

        // Makes a scoped service resolved from the root a startup failure, not a concurrency bug.
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

    /// <summary>The local model, behind Microsoft.Extensions.AI's abstraction.</summary>
    /// <remarks>Nothing connects here; a handler falls back if the model is down.</remarks>
    private static IChatClient CreateChatClient(IConfiguration configuration)
    {
        string url = configuration["ollama:url"] ?? "http://localhost:11434";
        string model = configuration["ollama:model"] ?? "llama3.1";

        return new OllamaApiClient(new Uri(url), model);
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
                // Sync: with Async, ExecuteAsync returns success before the body runs.
                DefaultRunMode = RunMode.Sync,
            }))
            // Also as IDiscordClient, so handlers can depend on the interface and be tested.
            .AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>())
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<ReminderDispatcher>()
            .AddSingleton<RestrictionExpirySweeper>()
            .AddSingleton<BotPersonality>()
            .AddSingleton(CreateChatClient(configuration))
            .AddLogging(builder => builder.AddSimpleConsole(options => options.TimestampFormat = "HH:mm:ss "))
            .AddSingleton(configuration)
            .AddDatabase(configuration)
            .AddSingleton(TimeProvider.System)
            // No validation pipeline: a validator missing from this list does nothing.
            .AddScoped<IValidator<Features.Events.Details.Query>, Features.Events.Details.QueryValidator>()
            .AddScoped<IValidator<Features.Events.Delete.Command>, Features.Events.Delete.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.Confirm.Command>, Features.EventAttendees.Confirm.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.Cancel.Command>, Features.EventAttendees.Cancel.CommandValidator>()
            .AddScoped<IValidator<Features.EventAttendees.List.Query>, Features.EventAttendees.List.QueryValidator>()
            .AddScoped<IValidator<Features.Loans.Create.Command>, Features.Loans.Create.CommandValidator>()
            .AddScoped<IValidator<Features.Loans.Update.Command>, Features.Loans.Update.CommandValidator>()
            .AddScoped<IValidator<Features.Reminders.Create.Command>, Features.Reminders.Create.CommandValidator>()
            .AddScoped<IValidator<Features.Restrictions.Apply.Command>, Features.Restrictions.Apply.CommandValidator>()
            .AddScoped<IValidator<Features.Personality.SetLevels.Command>, Features.Personality.SetLevels.CommandValidator>()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
    }
}