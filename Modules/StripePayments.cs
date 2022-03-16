using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions;
using SlickRentals_Discord_Bot.Functions.Database;
using SlickRentals_Discord_Bot.Models;

namespace SlickRentals_Discord_Bot.Modules
{
    public class StripePayments : ModuleBase<SocketCommandContext>
    {
        [Command("check")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Check()
        {
            try
            {
                var rentals = RentalsDB.GetUnpaidInvoicesInChannel(Context.Channel.Id);

                if (rentals == null)
                {
                    await ReplyAsync("There are no unpaid rental sessions in this channel!");
                    return;
                }

                foreach (var rental in rentals)
                {
                    var rentalId = Convert.ToInt32(rental["rental_id"]);
                    var rentalDetails = RentalsDB.GetRentalDetails(rentalId);
                    var endTime = rentalDetails.StartDateTime.AddDays(rentalDetails.RentalLength);
                    var bot = Context.Guild.GetRole(Convert.ToUInt64(rentalDetails.Bot)).Name;

                    if (bot is null)
                    {
                        await ReplyAsync($"Failed to get Database details for <&{rentalDetails.Bot}>");
                        return;
                    }

                    var invoicePaid = await Functions.Stripe.CheckIfPaid(rentalDetails.SessionId);

                    if (invoicePaid)
                    {
                        RentalsDB.UpdateStatus(rentalId, "PAID");

                        var message = await ReplyAsync("", false, CustomEmbedBuilder.BuildPaidInvoiceEmbed(true, rentalId));

                        var customer = Context.Guild.GetUser(rentalDetails.CustomerId);
                        var customerReceipt = CustomEmbedBuilder.BuildCustomerReceiptEmbed(
                            rentalDetails.GbpPayout + rentalDetails.GbpCommission,
                            rentalDetails.GbpTransactionFee, $"{bot} for {rentalDetails.RentalPeriod}", rentalId);
                        var customerDms = await customer.GetOrCreateDMChannelAsync();
                        await customerDms.SendMessageAsync($"Click here for the ticket -> {message.GetJumpUrl()}",
                            embed: customerReceipt);

                        var renter = Context.Guild.GetUser(rentalDetails.RenterId);
                        var renterReceipt = CustomEmbedBuilder.BuildRenterReceiptEmbed(
                            rentalDetails.GbpPayout + rentalDetails.GbpCommission,
                            rentalDetails.GbpCommission, $"{bot} for {rentalDetails.RentalPeriod}", rentalId);
                        var renterDm = await renter.GetOrCreateDMChannelAsync();
                        await renterDm.SendMessageAsync($"Click here for the ticket -> {message.GetJumpUrl()}",
                            embed: renterReceipt);

                        Functions.Points.AddPoints(rentalDetails.CustomerId, rentalDetails.Price);
                        await ReplyAsync(
                            $"<@{rentalDetails.CustomerId}> you now have {Functions.Points.GetPoints(rentalDetails.CustomerId)} points!"); // Reply with how many points they have
                        
                        await Autorole.AddUserToRoleAsync(rentalDetails.CustomerId, Convert.ToUInt64(rentalDetails.Bot),
                            endTime, Context);

                        await Context.Message.DeleteAsync();
                    }
                    else
                    {
                        await ReplyAsync("", false, CustomEmbedBuilder.BuildPaidInvoiceEmbed(false, rentalId));
                    }
                }
            }
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }
           
        }

        [Command("commissions")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task CollateCommissions(SocketGuildUser renter)
        {
            try
            {
                var config = DiscordFunctions.GetConfig();

                var outstandingCommissions = RentalsDB.GetUnpaidCommissions(renter);

                double total = 0;
                var note = "";

                foreach (var rental in outstandingCommissions)
                {
                    total += Convert.ToDouble(rental["price"]) * Convert.ToDouble(config["non_stripe_percent"]);
                    note += $"{rental["price"]} - {rental["price"]} ({rental["price"]}) \n";


                }



                var stripeFixedFee = Convert.ToDouble(config["stripe_fixed_fee"]);
                var stripePercentFee = Convert.ToDouble(config["stripe_percent_fee"]);

                var gbpPrice = Math.Round(ExchangeRate.ConvertCurrency(usdPrice), 2);
                var finalGbpPrice = Math.Round((gbpPrice + stripeFixedFee) / (1 - stripePercentFee), 2);
                var destinationFee = Math.Round(finalGbpPrice - gbpPrice, 2);

                var message = await ReplyAsync("Generating Invoice...");

                var invoiceEmbed =
                    await CreateCommissionInvoice(note, gbpPrice, usdPrice, finalGbpPrice, destinationFee, renter);

                if (invoiceEmbed is null)
                {
                    await ReplyAsync(
                        "Failed creating invoice! Ensure renter is setup with stripe and added to the database!");
                    return;
                }

                await ReplyAsync("", false, invoiceEmbed); // Reply with the invoice embed
                await message.DeleteAsync();
            }
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }

        }












        [Command("charge")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task CustomCharge(SocketGuildUser renter, double usdPrice, [Remainder] string note)
        {
            try
            {
                var config = DiscordFunctions.GetConfig();

                var stripeFixedFee = Convert.ToDouble(config["stripe_fixed_fee"]);
                var stripePercentFee = Convert.ToDouble(config["stripe_percent_fee"]);

                var gbpPrice = Math.Round(ExchangeRate.ConvertCurrency(usdPrice), 2);
                var finalGbpPrice = Math.Round((gbpPrice + stripeFixedFee) / (1 - stripePercentFee), 2);
                var destinationFee = Math.Round(finalGbpPrice - gbpPrice, 2);

                var message = await ReplyAsync("Generating Invoice...");

                var invoiceEmbed =
                    await CreateCommissionInvoice(note, gbpPrice, usdPrice, finalGbpPrice, destinationFee, renter);

                if (invoiceEmbed is null)
                {
                    await ReplyAsync(
                        "Failed creating invoice! Ensure renter is setup with stripe and added to the database!");
                    return;
                }

                await ReplyAsync("", false, invoiceEmbed); // Reply with the invoice embed
                await message.DeleteAsync();
            } 
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }
           
        }

        private static async Task<Embed> CreateCommissionInvoice(string note, double gbpPrice, double usdPrice,
            double finalGbpPrice, double destinationFee, SocketGuildUser renter)
        {
            var session = await Functions.Stripe.CreateCustomChargeSessionAsync(note, finalGbpPrice);

            var embed = CustomEmbedBuilder.BuildCommissionInvoiceEmbed(usdPrice, gbpPrice,
                Math.Round(finalGbpPrice - gbpPrice, 2), note,
                session.Id);

            return embed;
        }
    }
}