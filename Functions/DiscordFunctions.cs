using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlickRentals_Discord_Bot.Functions
{
    public static class DiscordFunctions
    {
        public static async Task SetBotStatusAsync(DiscordSocketClient client)
        {
            var config = GetConfig();

            var currently = config["currently"]?.Value<string>().ToLower();
            var statusText = config["playing_status"]?.Value<string>();
            var onlineStatus = config["status"]?.Value<string>().ToLower();

            // Set the online status
            if (!string.IsNullOrEmpty(onlineStatus))
            {
                var userStatus = onlineStatus switch
                {
                    "dnd" => UserStatus.DoNotDisturb,
                    "idle" => UserStatus.Idle,
                    "offline" => UserStatus.Invisible,
                    _ => UserStatus.Online
                };

                await client.SetStatusAsync(userStatus);
                Console.WriteLine($"{DateTime.Now.TimeOfDay:hh\\:mm\\:ss} | Online status set | {userStatus}");
            }

            // Set the playing status
            if (!string.IsNullOrEmpty(currently) && !string.IsNullOrEmpty(statusText))
            {
                var activity = currently switch
                {
                    "listening" => ActivityType.Listening,
                    "watching" => ActivityType.Watching,
                    "streaming" => ActivityType.Streaming,
                    _ => ActivityType.Playing
                };

                await client.SetGameAsync(statusText, type: activity);
                Console.WriteLine(
                    $"{DateTime.Now.TimeOfDay:hh\\:mm\\:ss} | Playing status set | {activity}: {statusText}");
            }
        }

        public static JObject GetConfig()
        {
            // Get the config file.
            using var configJson = new StreamReader(Directory.GetCurrentDirectory() + @"/config.json");
            return (JObject) JsonConvert.DeserializeObject(configJson.ReadToEnd());
        }
    }
}