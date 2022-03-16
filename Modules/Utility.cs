using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions;
using SlickRentals_Discord_Bot.Functions.Database;

namespace SlickRentals_Discord_Bot.Modules
{
    public class Utility : ModuleBase<SocketCommandContext>
    {
        [Command("oos")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Oos(string bot, [Remainder] string drop)
        {
            var config = DiscordFunctions.GetConfig();
            var oosChannel = Convert.ToUInt64(config["oos_channel"].ToString());

            if (!(Context.Client.GetChannel(oosChannel) is IMessageChannel channel)) return;

            await channel.SendMessageAsync(
                $"We are unfortunately out of stock for {bot} for the {drop} rental period. Please ensure you rent early to secure a bot!");
            await Context.Message.DeleteAsync();
        }

        [Command("help")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Help()
        {
            var embedBuilder = new EmbedBuilder();

            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithFooter("Slick Rentals")
                .WithThumbnailUrl("")
                .WithColor(Color.Blue)
                .WithTitle("Slick Rentals Bot Command Help")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Commands",
                    Value =
                        "**~rental** {@botrole} {price in USD} {start date} {Length in days} {@customer} {@renter} {drop}\n*Records a new rental. Commission is paid to user who executes the command*\n" +
                        "**~points add** {@user} {points}\n*Adds points to a user*\n" +
                        "**~points remove** {@user} {points}\n*Removes points to a user*\n" +
                        "**~points check** {@user}\n*Checks points to a user*\n" +
                        "**~check**\n *Checks if the invoice has been paid.*\n" +
                        "**~paid** {rentalId}\n*Marks a manual rental as paid and sends a commission embed*\n" +
                        "**~cancel** {rentalId}\n*Cancels a rental and voids the invoice*\n" +
                        "**~addstripe** {@user} {staff/renter} {stripe id}\n*Adds a user to the stripe database*\n" +
                        "**~commission** {@user} {USD amount} {notes (optional)}\n *Creates a commissions invoice. Notes default to \"commissions\" *\n"
                })
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync("", false, embed);
        }

        [Command("addstripe")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task AddStripe(SocketGuildUser user, string role, string stripeId)
        {
            var successful = RentalsDB.AddStripeAccount(stripeId, role, user);

            await ReplyAsync(successful
                ? $":white_check_mark: Successfully added <@{user.Id}> to the Stripe database!"
                : $":x: Could not add <@{user.Id}> to the Stripe database!");
        }
    }
}