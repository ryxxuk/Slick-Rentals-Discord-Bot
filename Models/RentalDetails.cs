using System;

namespace SlickRentals_Discord_Bot.Models
{
    public class RentalDetails
    {
        public int RentalId { get; set; }
        public ulong CustomerId { get; set; }
        public ulong RenterId { get; set; }
        public ulong ChannelId { get; set; }
        public string SessionId { get; set; }
        public string Bot { get; set; }
        public string RentalPeriod { get; set; }
        public int Price { get; set; }
        public string Status { get; set; }
        public decimal GbpCommission { get; set; }
        public decimal GbpPayout { get; set; }
        public decimal GbpTransactionFee { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartDateTime { get; set; }
        public int RentalLength { get; set; }
    }
}