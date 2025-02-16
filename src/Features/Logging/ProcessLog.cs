using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;

namespace Tarscord.Core.Features.Logging;

public class ProcessLog
{
    public record Command : IRequest<Unit>
    {
        public LogMessage LogMessage { get; init; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private string LogDirectory => Path.Combine(AppContext.BaseDirectory, "logs");
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            // Create today's log file if it doesn't exist
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();

            string logText = $"{DateTime.UtcNow:hh:mm:ss} [{request.LogMessage.Severity}] {request.LogMessage.Source}: " +
                           $"{request.LogMessage.Exception?.ToString() ?? request.LogMessage.Message}";

            // Write the log text to a file
            File.AppendAllText(LogFile, logText + "\n");

            // Write the log text to the console
            Console.WriteLine(logText);

            return Task.FromResult(Unit.Value);
        }
    }
} 