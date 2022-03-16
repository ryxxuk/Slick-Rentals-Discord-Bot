using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SlickRentals_Discord_Bot.Functions
{
    public class LoggingService
    {
        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            discord.Log += OnLogAsync;
            commands.Log += OnLogAsync;
            commands.CommandExecuted += CommandExecutedAsync;
        }

        private string LogDirectory { get; }
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        private Task OnLogAsync(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory)) // Create the log directory if it doesn't exist
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile)) // Create today's log file if it doesn't exist
                File.Create(LogFile).Dispose();

            var logText =
                $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            File.AppendAllText(LogFile, logText + "\n"); // Write the log text to a file

            return Console.Out.WriteLineAsync(logText); // Write the log text to the console
        }

        public Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!Directory.Exists(LogDirectory)) // Create the log directory if it doesn't exist
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile)) // Create today's log file if it doesn't exist
                File.Create(LogFile).Dispose();

            string logText;

            if (command.IsSpecified && result.IsSuccess)
                logText =
                    $"{DateTime.UtcNow:hh:mm:ss} [Msg] Successful command '{command.Value.Name}' executed by user: '{context.User.Username}'.";
            else
                logText =
                    $"{DateTime.UtcNow:hh:mm:ss} [Msg] Invalid command attempted by user:'{context.User.Username}' {result.ErrorReason}";

            File.AppendAllTextAsync(LogFile, logText + "\n"); // Write the log text to a file

            return Console.Out.WriteLineAsync(logText); // Write the log text to the console
        }
    }
}