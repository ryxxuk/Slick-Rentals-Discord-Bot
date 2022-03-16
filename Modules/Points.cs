using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SlickRentals_Discord_Bot.Modules
{
    public class Points : ModuleBase<SocketCommandContext>
    {
        [Command("points check")]
        public async Task GetPoints(SocketGuildUser user = null)
        {
            if (user is null) await ReplyAsync($"You have {Functions.Points.GetPoints(Context.User.Id)} points!");
            else await ReplyAsync($"{user.Username} has {Functions.Points.GetPoints(user.Id)} points!");
        }

        [Command("points add")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task AddPoints(SocketGuildUser user, int points)
        {
            var successful = Functions.Points.AddPoints(user.Id, points);

            if (successful) await ReplyAsync($"Successfully added {points} points to {user.Username}!");
            else await ReplyAsync($"Failed to add points to {user.Username}!");
        }

        [Command("points remove")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task RemovePoints(SocketGuildUser user, int points)
        {
            var resultPoints = Functions.Points.RemovePoints(user.Id, points);

            if (resultPoints != -1)
                await ReplyAsync($"Removed {points} points from {user.Username}. They now have {resultPoints} points!");
            else await ReplyAsync($"Failed to remove points to {user.Username}!");
        }
    }
}