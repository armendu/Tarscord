using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluentValidation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence;
using Tarscord.Core.Services;

namespace Tarscord.Core;

public class Startup
{
    public IConfigurationRoot Configuration { get; }

    public Startup()
    {
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

    private static IChatClient CreateChatClient(IConfiguration configuration)
    {
        var address = configuration.OllamaUrl();
        string? model = configuration.OllamaModel();

        // Unusable config means the model is off and Generate skips it, so this only has to resolve.
        return address is null || model is null
            ? new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.1")
            : new OllamaApiClient(address, model);
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
                DefaultRunMode = RunMode.Sync,
            }))
            .AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>())
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<ReminderDispatcher>()
            .AddSingleton<RestrictionExpirySweeper>()
            .AddSingleton<BotPersonality>()
            .AddSingleton<GenerationCooldown>()
            .AddSingleton(CreateChatClient(configuration))
            .AddLogging(builder => builder.AddSimpleConsole(options => options.TimestampFormat = "HH:mm:ss "))
            .AddSingleton(configuration)
            .AddDatabase(configuration)
            .AddSingleton(TimeProvider.System)
            .AddScoped<IValidator<Features.Events.Create.Command>, Features.Events.Create.CommandValidator>()
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
