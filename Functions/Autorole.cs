using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions.Database;
using SlickRentals_Discord_Bot.Models;

namespace SlickRentals_Discord_Bot.Functions
{
    internal class Autorole
    {
        private DiscordSocketClient _client;
        private ulong guildId;
        private List<ulong> channelIds;

        public void InitiateAutoRole(DiscordSocketClient client)
        {
            channelIds = new List<ulong>();
            _client = client;
            var config = DiscordFunctions.GetConfig();
            guildId = Convert.ToUInt64(config["guild"].ToString());

            var task = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(15));

                while (true)
                {
                    try
                    {
                        await CheckAllUserRoles();
                    }
                    catch
                    {
                        // ignored
                    }

                    await Task.Delay(TimeSpan.FromMinutes(30));
                }
            });
        }

        public static async Task<bool> AddUserToRoleAsync(ulong discordId, ulong roleId, DateTime endTime,
            SocketCommandContext context)
        {
            try
            {
                AutoroleDB.AddAutoRoleEntry(discordId, roleId, context.Channel.Id, DateTime.Now, endTime);

                var roleDetails = AutoroleDB.GetRoleInformation(roleId);

                await context.Guild.GetUser(discordId).AddRoleAsync(context.Guild.GetRole(roleId));
                
                var embed = CustomEmbedBuilder.BuildAddedRoleEmbed(roleId, discordId, endTime);

                await context.Channel.SendMessageAsync("", false, embed);

                await context.Channel.SendMessageAsync(
                    $"Hey! You can download {roleDetails.BotName} from here: <#{roleDetails.DownloadChannelId}> and you can view the guides here: <#{roleDetails.GuideChannelId}>", false);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task CheckAllUserRoles()
        {
            var rolesToBeRemoved = AutoroleDB.GetOverdueRoleRevokes();

            if (rolesToBeRemoved is null || rolesToBeRemoved.Count == 0) return;

            foreach (var role in rolesToBeRemoved) await RemoveUserFromRole(role);

            channelIds.Clear();

            Console.WriteLine($"{DateTime.Now} [Msg] {rolesToBeRemoved.Count} roles revoked!");
        }

        public async Task RemoveUserFromRole(AutoRoleDetails roleToBeRemoved)
        {
            try
            {
                var guild = _client.GetGuild(guildId);
                var user = guild.GetUser(roleToBeRemoved.DiscordId);

                if (user is not null)
                {
                    var role = guild.GetRole(roleToBeRemoved.RoleId);

                    if (role is not null && user.Roles.Contains(role))
                    {
                        await user.RemoveRoleAsync(roleToBeRemoved.RoleId);

                        var dmChannel = await _client.GetUser(roleToBeRemoved.DiscordId).GetOrCreateDMChannelAsync();

                        await dmChannel.SendMessageAsync(
                            $"The {guild.GetRole(roleToBeRemoved.RoleId).Name} role has now been removed from you! Thank you for the rental, We hope you consider using Slick Rentals again!");

                    }
                }

                if (!channelIds.Contains(roleToBeRemoved.ChannelId))
                {
                    var channel = guild.GetChannel(roleToBeRemoved.ChannelId);
                    channelIds.Add(roleToBeRemoved.ChannelId);

                    if (channel is not null)
                    {
                        await channel.ModifyAsync(prop => prop.CategoryId = 811882030535147522);

                        var messageChannel = (ISocketMessageChannel)channel;

                        await messageChannel.SendMessageAsync("<@&745662263922262191>", false, CustomEmbedBuilder.BuildFinishedRentalEmbed());
                    }
                }

                AutoroleDB.UpdateAutoRoleStatus(roleToBeRemoved.Id, "revoked");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed removing role for: {roleToBeRemoved.Id}\n {e}");
                AutoroleDB.UpdateAutoRoleStatus(roleToBeRemoved.Id, "error");
            }
        }
    }
}