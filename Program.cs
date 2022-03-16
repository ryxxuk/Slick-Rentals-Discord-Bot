using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SlickRentals_Discord_Bot.Functions;

namespace SlickRentals_Discord_Bot
{
    public class Program
    {
        private static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            var services = ConfigureServices();

            var client = services.GetRequiredService<DiscordSocketClient>();
            services.GetRequiredService<CommandService>();
            services.GetRequiredService<LoggingService>();

            // Get the bot token from the Config.json file.
            var config = DiscordFunctions.GetConfig();
            var token = config["token"].Value<string>();

            // Log in to Discord and start the bot.
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            var autoRole = new Autorole();
            autoRole.InitiateAutoRole(client);

            // Run the bot forever.
            await Task.Delay(-1);
        }

        public static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 500,
                    LogLevel = LogSeverity.Info
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = RunMode.Async,
                    CaseSensitiveCommands = false
                }))
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}