using System;
using Discord;

namespace SlickRentals_Discord_Bot.Functions
{
    public class CustomEmbedBuilder
    {
        public static Embed BuildRentalEmbed(int id, string customerName, string renterName, string staffName,
            string drop, double price)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithFooter("Slick RentalsDB")
                .WithThumbnailUrl("")
                .WithColor(Color.Blue)
                .WithTitle($"New Rental #{id} Created Successfully")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Customer",
                    Value = customerName,
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Renter",
                    Value = renterName,
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Ticket Handler",
                    Value = staffName,
                    IsInline = false
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Period",
                    Value = drop,
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Price",
                    Value = $"${price}",
                    IsInline = true
                })
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildInvoiceEmbed(int id, double usdPrice, double gbpPrice, double transactionFee,
            string items, string sessionId)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Blue)
                .WithTitle($"Rental #{id} Payment Session Created")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Items",
                    Value = items,
                    IsInline = false
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Cost",
                    Value = $"£{gbpPrice:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Transaction Fee",
                    Value = $"£{transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Total Cost",
                    Value = $"£{gbpPrice + transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Payment Link",
                    Value =
                        $"[CLICK HERE TO PAY NOW](https://slickrentals.s3.eu-west-2.amazonaws.com/pay.html?sessionid={sessionId}) :white_check_mark:",
                    IsInline = true
                })
                .WithDescription($"USD TO GBP Conversion:\n{usdPrice:0.00} USD -> {gbpPrice:0.00} GBP")
                .WithFooter("Thanks for using Slick RentalsDB!")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildPaidInvoiceEmbed(bool paid, int id)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl =
                        "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(paid ? Color.Green : Color.Red)
                .WithTitle(paid
                    ? $"Rental #{id} Paid! :white_check_mark:"
                    : $"Rental #{id} has not been paid yet! :no_entry_sign:")
                .WithFooter("Payment Checking")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        internal static Embed BuildOutstandingCommissionEmbed(int rentalId, string bot, string drop, ulong discordId,
            double usdPrice, double usdCommission)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl =
                        "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Green)
                .WithTitle($"Outstanding Commission for Rental #{rentalId}")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Cost",
                    Value = $"${usdPrice:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Commission",
                    Value = $"${usdCommission:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Reason",
                    Value = $"<@&{bot}> for {drop}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "User",
                    Value = $"<@{discordId}>",
                    IsInline = true
                })
                .WithFooter("Outstanding Commission")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildCancelInvoiceEmbed(bool cancelled, int id)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl =
                        "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(cancelled ? Color.Green : Color.Red)
                .WithTitle(cancelled
                    ? $"Rental #{id} Cancelled! :white_check_mark:"
                    : $"Rental #{id} failed to cancel! :no_entry_sign:")
                .WithFooter("Rental Status Update")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildCustomerReceiptEmbed(decimal gbpPrice, decimal transactionFee, string items, int id)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Blue)
                .WithTitle($"Rental Receipt #{id}")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Items",
                    Value = items,
                    IsInline = false
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Cost",
                    Value = $"£{gbpPrice}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Transaction Fee",
                    Value = $"£{transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Total Cost",
                    Value = $"£{gbpPrice + transactionFee:0.00}",
                    IsInline = true
                })
                .WithDescription("Thanks for using Slick RentalsDB, don't forget to post your success!")
                .WithFooter("Thanks for using Slick RentalsDB!")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildRenterReceiptEmbed(decimal gbpPrice, decimal commissions, string items, int id)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Blue)
                .WithTitle($"Congratulations on the rental! #{id}")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Items",
                    Value = items,
                    IsInline = false
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Price",
                    Value = $"£{gbpPrice:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Commission",
                    Value = $"£{commissions:0.00}",
                    IsInline = true
                })
                .WithDescription(
                    "Please keep this for your records! If you have any questions, talk to a member of staff.")
                .WithFooter("Thanks for using Slick RentalsDB!")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildCommissionInvoiceEmbed(double usdPrice, double gbpPrice, double transactionFee,
            string note, string sessionId)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {author.IconUrl = "https://i.imgur.com/mKyyEe4.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Blue)
                .WithTitle("Commissions Payment Request")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Note",
                    Value = note,
                    IsInline = false
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Rental Cost",
                    Value = $"£{gbpPrice:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Transaction Fee",
                    Value = $"£{transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Total Cost",
                    Value = $"£{gbpPrice + transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Payment Link",
                    Value =
                        $"[CLICK HERE TO PAY NOW](https://slickrentals.s3.eu-west-2.amazonaws.com/pay.html?sessionid={sessionId}) :white_check_mark:",
                    IsInline = true
                })
                .WithDescription($"USD TO GBP Conversion:\n{usdPrice:0.00} USD -> {gbpPrice:0.00} GBP")
                .WithFooter("Thanks for using Slick RentalsDB!")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }

        public static Embed BuildAddedRoleEmbed(ulong roleId, ulong discordId, DateTime endTime)
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl =
                        "https://media.discordapp.net/attachments/707336856475271199/802233836989448252/THIS_ONE.png";
                    author.Name = "Slick Rentals";
                })
                .WithColor(Color.Green)
                .WithTitle($"Guide Access Given! Expires: {endTime}")
                .WithDescription($"Granted <@&{roleId}> role to <@{discordId}> ")
                .WithFooter("Slick Rentals Auto Role")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }


        public static Embed BuildFinishedRentalEmbed()
        {
            var embedBuilder = new EmbedBuilder();
            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl =
                        "https://media.discordapp.net/attachments/707336856475271199/802233836989448252/THIS_ONE.png";
                    author.Name = "Slick Rentals";
                })
                .WithThumbnailUrl("https://media.discordapp.net/attachments/707336856475271199/802233836989448252/THIS_ONE.png")
                .WithColor(Color.Blue)
                .WithTitle($"Thank you for your rental!")
                .WithDescription($"Hopefully your rental went well and you copped, be sure to share in <#745754156001919186>!\nIf you have any feedback on how to improve in the future please let us know.\n\nThanks for renting through Slick Rentals!")
                .WithFooter("Slick Rentals Ticket End")
                .WithCurrentTimestamp()
                .Build();
            return embed;
        }


    }
}