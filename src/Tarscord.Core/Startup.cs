using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var services = new ServiceCollection();
        ConfigureServices(services, Configuration);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        // Resolved only for their constructors, which subscribe to the gateway's events.
        provider.GetRequiredService<LoggingService>();
        provider.GetRequiredService<CommandHandler>();

        await provider.GetRequiredService<StartupService>().StartAsync();

        var logger = provider.GetRequiredService<ILogger<Startup>>();
        _ = StartLoopAsync(provider.GetRequiredService<ReminderDispatcher>(), logger);
        _ = StartLoopAsync(provider.GetRequiredService<RestrictionExpirySweeper>(), logger);

        await Task.Delay(-1);
    }

    internal static async Task StartLoopAsync(BackgroundService loop, ILogger<Startup> logger)
    {
        try
        {
            await loop.StartAsync(CancellationToken.None);
            await loop.ExecuteTask!;
        }
        catch (Exception exception)
        {
            // Nothing else awaits the loop, so without this a loop that dies would die silently.
            logger.LogError(exception, "{Loop} stopped and will not run again until the bot restarts",
                loop.GetType().Name);
        }
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
            .AddSingleton(TimeProvider.System);

        Setup.InitializeBot.AddSlice(services);
        Setup.ProcessMessage.AddSlice(services);
        Features.Logging.ProcessLog.AddSlice(services);

        Features.Events.Create.AddSlice(services);
        Features.Events.Delete.AddSlice(services);
        Features.Events.Details.AddSlice(services);
        Features.Events.List.AddSlice(services);

        Features.EventAttendees.Cancel.AddSlice(services);
        Features.EventAttendees.Confirm.AddSlice(services);
        Features.EventAttendees.List.AddSlice(services);

        Features.Loans.Confirm.AddSlice(services);
        Features.Loans.Create.AddSlice(services);
        Features.Loans.List.AddSlice(services);
        Features.Loans.Update.AddSlice(services);

        Features.Personality.Generate.AddSlice(services);
        Features.Personality.SetLevels.AddSlice(services);

        Features.Reminders.Complete.AddSlice(services);
        Features.Reminders.Create.AddSlice(services);
        Features.Reminders.List.AddSlice(services);

        Features.Restrictions.Apply.AddSlice(services);
        Features.Restrictions.Lift.AddSlice(services);
        Features.Restrictions.List.AddSlice(services);
    }
}
