using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions.Database;
using Stripe;
using Stripe.Checkout;

namespace SlickRentals_Discord_Bot.Functions
{
    internal class Stripe
    {
        static Stripe()
        {
            var config = DiscordFunctions.GetConfig();
            var privKey = config["stripe_private_key"].ToString();

            StripeConfiguration.ApiKey = privKey;
        }

        public static async Task<Embed> CreateStripeInvoice(int rentalId, ulong bot, string drop, double gbpPrice,
            double usdPrice, double finalGbpPrice, double destinationFee, SocketGuildUser customer,
            SocketGuildUser renter, int id, SocketCommandContext context)
        {
            var renterConnectedAccount = RentalsDB.GetStripeAccountId(renter.Id);

            if (renterConnectedAccount is null) return null;

            var botName = context.Guild.GetRole(bot).Name;

            if (botName is null)
            {
                await context.Message.Channel.SendMessageAsync($"Failed to get Database details for {bot}");
                return null;
            }

            var session = await CreateCheckoutSession(rentalId, drop, botName, finalGbpPrice, destinationFee,
                renterConnectedAccount);

            var embed = CustomEmbedBuilder.BuildInvoiceEmbed(id, usdPrice, gbpPrice,
                Math.Round(finalGbpPrice - gbpPrice, 2), $"<@&{bot}> for {drop}",
                session.Id);

            RentalsDB.SetInvoiceId(id, session.Id);
            return embed;
        }

        public static async Task<Session> CreateCheckoutSession(int rentalId, string rental, string bot, double price,
            double applicationFee, string destinationStripeId)
        {
            var capabilityService = new CapabilityService();
            var capability = await capabilityService.GetAsync(destinationStripeId, "card_payments");

            var applicationFeeInPence = Convert.ToInt64(applicationFee * 100);
            var priceInPence = Convert.ToInt64(price * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Name = $"{bot} for {rental}. RENTAL#{rentalId}",
                        Amount = priceInPence,
                        Currency = "gbp",
                        Quantity = 1
                    }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = applicationFeeInPence,
                    TransferData = new SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = destinationStripeId
                    },
                    OnBehalfOf = capability.Status == "inactive" || capability.Status == "unrequested"
                        ? null
                        : destinationStripeId,
                    StatementDescriptor = "Slick Rentals",
                    StatementDescriptorSuffix = $"SR-{bot}#{rentalId}"
                },
                SuccessUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/success.html",
                CancelUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/failure.html",
                Mode = "payment"
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public static async Task<Session> CreateCustomChargeSessionAsync(string note, double price)
        {
            var priceInPence = Convert.ToInt64(price * 100);
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Name = $"{note}",
                        Amount = priceInPence,
                        Currency = "gbp",
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/success.html",
                CancelUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/failure.html"
            };
            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public static async Task<bool> CheckIfPaid(string sessionId)
        {
            var service = new SessionService();
            try
            {
                var session = await service.GetAsync(sessionId);
                if (session.PaymentStatus == "paid" || session.PaymentStatus == "no_payment_required") return true;
            }
            catch (Exception)

            {
                // ignored or couldn't find session
            }

            return false;
        }
    }
}