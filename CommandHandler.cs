using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SlickRentals_Discord_Bot.Functions;

namespace SlickRentals_Discord_Bot
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Event handlers
            _client.Ready += ClientReadyAsync;
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage rawMessage)
        {
            if (rawMessage.Author.IsBot || !(rawMessage is SocketUserMessage message) || message.Channel is IDMChannel)
                return;

            var context = new SocketCommandContext(_client, message);

            var argPos = 0;

            var config = DiscordFunctions.GetConfig();
            var prefixes = JsonConvert.DeserializeObject<string[]>(config["prefixes"].ToString());

            // Check if message has any of the prefixes or mentions the bot.
            if (prefixes.Any(x => message.HasStringPrefix(x, ref argPos))
            ) //|| message.HasMentionPrefix(_client.CurrentUser, ref argPos)
            {
                // Execute the command.
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess && result.Error.HasValue)
                    await context.Channel.SendMessageAsync($":x: {result.ErrorReason}");
            }
        }

        private async Task ClientReadyAsync()
        {
            await DiscordFunctions.SetBotStatusAsync(_client);
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}