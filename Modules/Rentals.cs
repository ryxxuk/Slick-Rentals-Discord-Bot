using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions;
using SlickRentals_Discord_Bot.Functions.Database;

namespace SlickRentals_Discord_Bot.Modules
{
    public class Rentals : ModuleBase<SocketCommandContext>
    {
        [Command("rental")]
        [Alias("rent")]
        [Summary("Logs a new rental")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Rental(SocketRole bot, int usdPrice, string startDateString, int rentalLength,
            SocketGuildUser customer, SocketGuildUser renter,
            [Remainder] string drop)
        {
            try
            {
                // VALIDATION
                if (!DateTime.TryParse(startDateString, out var startDate))
                {
                    await ReplyAsync(":x: Date in incorrect format! Use dd/mm/yyyy");
                    return;
                }

                if (!renter.Guild.Roles.Contains(Context.Guild.Roles.First(x => x.Id == 775082785240776724)))
                {
                    await ReplyAsync($"<@{renter.Id}> is not a renter!");
                    return;
                }

                if (startDate < DateTime.Today)
                {
                    await ReplyAsync($"Cannot create a rental in the past!");
                    return;
                }

                if (bot.Name.Contains("Seller"))
                {
                    await ReplyAsync($"Make sure you select the right bot role. Not the @bot Seller role!");
                    return;
                }

                rentalLength++; // Adds an extra day for good measure!

                var config = DiscordFunctions.GetConfig();
                var gbpPrice = Math.Round(ExchangeRate.ConvertCurrency(usdPrice), 2);

                // Log a new rental in the database

                var rentalId = RentalsFunc.LogRental(Context, renter, customer, bot.Id, startDate, rentalLength, drop,
                    usdPrice);

                if (rentalId == -1)
                {
                    await ReplyAsync("Failed logging new rental!");
                    return;
                }

                var rentalEmbed = CustomEmbedBuilder.BuildRentalEmbed(rentalId, customer.Username, renter.Username,
                    Context.User.Username, $"<@&{bot.Id}> for {drop}", usdPrice);

                await ReplyAsync("", false, rentalEmbed);

                double commissionPercent = 0;
                var isStaff = RentalsDB.CheckIfStaff(renter.Id);

                var renterConnectedAccount = RentalsDB.GetStripeAccountId(renter.Id);
                switch (renterConnectedAccount)
                {
                    // Is not on stripe
                    case null:
                        if (!isStaff) commissionPercent = Convert.ToDouble(config["non_stripe_percent"]);

                        var usdCommission = usdPrice * commissionPercent;
                        var gbpCommission = Math.Round(ExchangeRate.ConvertCurrency(usdCommission), 2);

                        RentalsDB.UpdateStatus(rentalId, "MANUAL_NEW");
                        RentalsDB.UpdateRental(rentalId, gbpPrice - gbpCommission, gbpCommission, 0);

                        await ReplyAsync(
                            "No Stripe account detected! Rental in manual mode. Please use ~paid {rental id} when rental has been paid. "); // Reply with the invoice embed
                        break;
                    // Is on stripe
                    default:
                    {
                        var message = await ReplyAsync("Generating Session...");

                        if (!isStaff) commissionPercent = Convert.ToDouble(config["commission_percent"]);

                        var stripeFixedFee = Convert.ToDouble(config["stripe_fixed_fee"]);
                        var stripePercentFee = Convert.ToDouble(config["stripe_percent_fee"]);


                        var finalGbpPrice = Math.Round((gbpPrice + stripeFixedFee) / (1 - stripePercentFee), 2);
                        var destinationFee = Math.Round(finalGbpPrice - gbpPrice + gbpPrice * commissionPercent, 2);

                        RentalsDB.UpdateRental(rentalId, gbpPrice * (1 - commissionPercent),
                            gbpPrice * commissionPercent,
                            destinationFee);

                        // Create Stripe invoice

                        var invoiceEmbed = await Functions.Stripe.CreateStripeInvoice(rentalId, bot.Id, drop, gbpPrice,
                            usdPrice, finalGbpPrice, destinationFee, customer, renter, rentalId, Context);

                        if (invoiceEmbed is null)
                        {
                            await ReplyAsync(
                                "Failed creating invoice! Ensure renter is setup with stripe and added to the database!");
                            return;
                        }

                        await ReplyAsync("", false, invoiceEmbed); // Reply with the invoice embed
                        await message.DeleteAsync();
                        break;
                    }
                }

                var logChannel =
                    (IMessageChannel) Context.Client.GetChannel(Convert.ToUInt64(config["log_channel"].ToString()));
                if (!(logChannel is null)) await logChannel.SendMessageAsync("", false, rentalEmbed);

                await Context.Message.DeleteAsync();
            }
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }
        }

        [Command("paid")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MarkPaid(int rentalId)
        {
            try
            {
                var config = DiscordFunctions.GetConfig();
                var rentalDetails = RentalsDB.GetRentalDetails(rentalId);
                var bot = Context.Guild.GetRole(Convert.ToUInt64(rentalDetails.Bot))?.Name;
                var endTime = rentalDetails.StartDateTime.AddDays(rentalDetails.RentalLength);

                if (bot is null)
                {
                    await ReplyAsync($"Failed to get Database details for <&{rentalDetails.Bot}>");
                    return;
                }

                Functions.Points.AddPoints(rentalDetails.CustomerId, rentalDetails.Price);
                var message =
                    await ReplyAsync(
                        $"<@{rentalDetails.CustomerId}> you now have {Functions.Points.GetPoints(rentalDetails.CustomerId)} points!"); // Reply with how many points they have

                // Send Receipt to Customer
                var customer = Context.Guild.GetUser(rentalDetails.CustomerId);
                var customerDms = await customer.GetOrCreateDMChannelAsync();
                var customerReceipt = CustomEmbedBuilder.BuildCustomerReceiptEmbed(
                    rentalDetails.GbpPayout + rentalDetails.GbpCommission,
                    rentalDetails.GbpTransactionFee, $"{bot} for {rentalDetails.RentalPeriod}", rentalId);
                await customerDms.SendMessageAsync($"Click here for the ticket -> {message.GetJumpUrl()}",
                    embed: customerReceipt);

                // Send Receipt to Renter
                var renter = Context.Guild.GetUser(rentalDetails.RenterId);
                var renterDm = await renter.GetOrCreateDMChannelAsync();
                var renterReceipt = CustomEmbedBuilder.BuildRenterReceiptEmbed(
                    rentalDetails.GbpPayout + rentalDetails.GbpCommission,
                    rentalDetails.GbpCommission, $"{bot} for {rentalDetails.RentalPeriod}", rentalId);
                await renterDm.SendMessageAsync($"Click here for the ticket -> {message.GetJumpUrl()}",
                    embed: renterReceipt);


                var usdCommission = rentalDetails.Price * (double)config["non_stripe_percent"];

                RentalsDB.UpdateStatus(rentalId, "COMMISSION_DUE");

                var commissionEmbed = CustomEmbedBuilder.BuildOutstandingCommissionEmbed(rentalId, rentalDetails.Bot,
                    rentalDetails.RentalPeriod, rentalDetails.RenterId, rentalDetails.Price, usdCommission);

                var commissionChannel =
                    (IMessageChannel)Context.Guild.GetChannel(Convert.ToUInt64(config["commission_channel"]?.ToString()));
                await commissionChannel.SendMessageAsync("", false, commissionEmbed);

                await ReplyAsync("", false, CustomEmbedBuilder.BuildPaidInvoiceEmbed(true, rentalId)); // success

                await Autorole.AddUserToRoleAsync(rentalDetails.CustomerId, Convert.ToUInt64(rentalDetails.Bot), endTime,
                    Context);
            }
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }

            
        }

        [Command("cancel")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Cancel(int rentalId)
        {
            try
            {
                var sessionId = RentalsDB.GetRentalDetails(rentalId);

                var successful = false;

                if (sessionId.Status is not null)
                {
                    RentalsDB.UpdateStatus(rentalId, "VOID");
                    successful = true;
                }

                await ReplyAsync("", false, CustomEmbedBuilder.BuildCancelInvoiceEmbed(successful, rentalId)); // success
            }
            catch
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
            }
            
        }
    }
}