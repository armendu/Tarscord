using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace Tarscord.Core.Features.Logging;

public static class ProcessLog
{
    public static void AddSlice(IServiceCollection services) =>
        services.AddSingleton<Handler>();

    public sealed class Handler
    {
        private string LogDirectory => Path.Combine(AppContext.BaseDirectory, "logs");
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public Task HandleAsync(LogMessage logMessage)
        {
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            // Create today's log file if it doesn't exist
            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            string logText = $"{DateTime.UtcNow:HH:mm:ss} [{logMessage.Severity}] {logMessage.Source}: " +
                             $"{logMessage.Exception?.ToString() ?? logMessage.Message}";

            // Write the log text to a file
            File.AppendAllText(LogFile, logText + "\n");

            // Write the log text to the console
            Console.WriteLine(logText);

            return Task.CompletedTask;
        }
    }
}
