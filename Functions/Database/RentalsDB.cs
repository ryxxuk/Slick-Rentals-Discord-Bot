using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using SlickRentals_Discord_Bot.Models;

namespace SlickRentals_Discord_Bot.Functions.Database
{
    public class RentalsDB
    {
        public static string RecordNewRental(ulong botRoleId, double price, string drop, string renterId,
            string customerId, ulong channelId, DateTime startDate, int rentalLength)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var cmd = new MySqlCommand(
                    "INSERT INTO rental_history(bot, customer_id, renter_id, channel_id, price, rental_period, start_date, rental_length) values (@bot, @customer_id, @renter_id, @channel_id, @price, @rental_period, @start_date, @rental_length)",
                    conn);
                cmd.Parameters.AddWithValue("@bot", botRoleId);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@rental_period", drop);
                cmd.Parameters.AddWithValue("@customer_id", customerId);
                cmd.Parameters.AddWithValue("@renter_id", renterId);
                cmd.Parameters.AddWithValue("@channel_id", channelId);
                cmd.Parameters.AddWithValue("@start_date", startDate);
                cmd.Parameters.AddWithValue("@rental_length", rentalLength);

                cmd.Prepare();

                cmd.ExecuteNonQuery();

                return cmd.LastInsertedId.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return "Failed logging new rental!";
            }
        }

        public static string GetStripeAccountId(ulong discordId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM connected_accounts WHERE discord_id=@discord_id", conn);
                checkCmd.Parameters.AddWithValue("@discord_id", discordId);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return null;

                while (reader.Read()) return reader["stripe_id"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static bool UpdateStatus(int id, string status)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand("UPDATE rental_history SET rental_status=@status WHERE rental_id=@rental_id",
                        conn);
                checkCmd.Parameters.AddWithValue("@rental_id", id);
                checkCmd.Parameters.AddWithValue("@status", status);
                checkCmd.Prepare();

                checkCmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static bool SetInvoiceId(int id, string stripeId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand("UPDATE rental_history SET stripe_id=@stripe_id WHERE rental_id=@rental_id", conn);
                checkCmd.Parameters.AddWithValue("@rental_id", id);
                checkCmd.Parameters.AddWithValue("@stripe_id", stripeId);
                checkCmd.Prepare();

                checkCmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static List<DataRow> GetUnpaidCommissions(SocketGuildUser renter)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand(
                        "SELECT * FROM rental_history WHERE renter_id=@renter_id AND rental_status='COMMISSION_DUE'", conn);
                checkCmd.Parameters.AddWithValue("@renter_id", renter.Id);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return null;

                var dt = new DataTable();
                dt.Load(reader);

                return dt.AsEnumerable().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        internal static RentalDetails GetRentalDetails(int rentalId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM rental_history WHERE rental_id=@rental_id", conn);
                checkCmd.Parameters.AddWithValue("@rental_id", rentalId);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return null;

                while (reader.Read())
                    return new RentalDetails
                    {
                        Bot = (string) reader["bot"],
                        Price = (int) reader["price"],
                        RentalId = (int) reader["rental_id"],
                        RentalPeriod = (string) reader["rental_period"],
                        Status = (string) reader["rental_status"],
                        SessionId = reader["stripe_id"]?.ToString(),
                        GbpCommission = (decimal) reader["gbp_commission"],
                        GbpPayout = (decimal) reader["gbp_payout"],
                        GbpTransactionFee = (decimal) reader["gbp_transaction_fee"],
                        ChannelId = Convert.ToUInt64(reader["channel_id"]?.ToString()),
                        CustomerId = Convert.ToUInt64(reader["customer_id"]?.ToString()),
                        RenterId = Convert.ToUInt64(reader["renter_id"]?.ToString()),
                        StartDateTime = Convert.ToDateTime(reader["start_date"]?.ToString()),
                        RentalLength = (int) reader["rental_length"],
                        Date = Convert.ToDateTime(reader["date"]?.ToString())
                    };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static bool AddStripeAccount(string stripeId, string role, SocketGuildUser user)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand(
                        "INSERT connected_accounts(stripe_id, discord_id, role, nickname) VALUES (@stripe_id, @discord_id, @role, @nickname)",
                        conn);
                checkCmd.Parameters.AddWithValue("@stripe_id", stripeId);
                checkCmd.Parameters.AddWithValue("@discord_id", user.Id);
                checkCmd.Parameters.AddWithValue("@role", role);
                checkCmd.Parameters.AddWithValue("@nickname", user.Username);
                checkCmd.Prepare();

                checkCmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static List<DataRow> GetUnpaidInvoicesInChannel(ulong channelId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand(
                        "SELECT * FROM rental_history WHERE channel_id=@channel_id AND rental_status='NEW'", conn);
                checkCmd.Parameters.AddWithValue("@channel_id", channelId);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return null;

                var dt = new DataTable();
                dt.Load(reader);

                return dt.AsEnumerable().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public static bool UpdateRental(int id, double gbpPayout, double gbpCommission, double gbpTransactionFee)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd =
                    new MySqlCommand(
                        "UPDATE rental_history SET gbp_payout=@gbp_payout, gbp_commission=@gbp_commission, gbp_transaction_fee=@gbp_transaction_fee WHERE rental_id=@rental_id",
                        conn);
                checkCmd.Parameters.AddWithValue("@rental_id", id);
                checkCmd.Parameters.AddWithValue("@gbp_payout", gbpPayout);
                checkCmd.Parameters.AddWithValue("@gbp_commission", gbpCommission);
                checkCmd.Parameters.AddWithValue("@gbp_transaction_fee", gbpTransactionFee);
                checkCmd.Prepare();

                checkCmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return false;
        }

        public static bool CheckIfStaff(ulong discordId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT role FROM connected_accounts WHERE discord_id=@discord_id",
                    conn);
                checkCmd.Parameters.AddWithValue("@discord_id", discordId);

                checkCmd.Prepare();

                var role = checkCmd.ExecuteScalar();

                return role.ToString()?.ToLower() == "staff";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed for {discordId} \n {ex}");
            }
            return false;
        }
    }
}