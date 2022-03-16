using System;
using Discord.Commands;
using Discord.WebSocket;
using SlickRentals_Discord_Bot.Functions.Database;

namespace SlickRentals_Discord_Bot.Functions
{
    public class RentalsFunc
    {
        public static int LogRental(SocketCommandContext context, SocketGuildUser renter, SocketGuildUser customer,
            ulong botRoleId, DateTime startDate, int rentalLength, string drop, double usdPrice)
        {
            var result = RentalsDB.RecordNewRental(botRoleId, usdPrice, drop, renter.Id.ToString(),
                customer.Id.ToString(), context.Channel.Id, startDate, rentalLength);

            var successful = int.TryParse(result, out var rentalId);

            if (!successful) return -1;

            return rentalId; // Reply with the rental embed
        }
    }
}