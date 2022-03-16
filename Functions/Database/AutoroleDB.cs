using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using SlickRentals_Discord_Bot.Models;

namespace SlickRentals_Discord_Bot.Functions.Database
{
    public class AutoroleDB
    {
        public static long AddAutoRoleEntry(ulong discordId, ulong roleId, ulong channelId, DateTime startTime, DateTime endTime)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var cmd = new MySqlCommand(
                    "INSERT INTO auto_role(discord_id, role_id, channel_id, start_time, end_time) values (@discord_id, @role_id, @channel_id, @start_time, @end_time)",
                    conn);
                cmd.Parameters.AddWithValue("@discord_id", discordId);
                cmd.Parameters.AddWithValue("@role_id", roleId);
                cmd.Parameters.AddWithValue("@start_time", startTime);
                cmd.Parameters.AddWithValue("@channel_id", channelId);
                cmd.Parameters.AddWithValue("@end_time", endTime);

                cmd.Prepare();

                cmd.ExecuteNonQuery();

                return cmd.LastInsertedId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -1;
            }
        }

        public static string UpdateAutoRoleStatus(int id, string status)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("UPDATE auto_role SET status=@status WHERE id=@id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                checkCmd.Parameters.AddWithValue("@status", status);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows) return null;

                while (reader.Read()) return reader["stripe_id"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                conn.Close();
            }

            return null;
        }

        public static List<AutoRoleDetails> GetOverdueRoleRevokes()
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM auto_role WHERE end_time<@now AND status='granted'", conn);

                checkCmd.Parameters.AddWithValue("@now", DateTime.Now);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    return null;
                }

                var result = new List<AutoRoleDetails>();

                while (reader.Read())
                    result.Add(new AutoRoleDetails
                    {
                        Id = (int) reader["id"],
                        Status = (string) reader["status"],
                        DiscordId = Convert.ToUInt64(reader["discord_id"].ToString()),
                        RoleId = Convert.ToUInt64(reader["role_id"].ToString()),
                        ChannelId = Convert.ToUInt64(reader["channel_id"].ToString()),
                        StartTime = Convert.ToDateTime(reader["start_time"].ToString()),
                        EndTime = Convert.ToDateTime(reader["end_time"].ToString())
                    });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                conn.Close();
            }

            return null;
        }

        public static RoleDetails GetRoleInformation(ulong roleId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM bot_roles WHERE role_id=@role_id", conn);
                checkCmd.Parameters.AddWithValue("@role_id", roleId);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                reader.Read();

                return new RoleDetails
                {
                    RoleId = Convert.ToUInt64(reader["role_id"].ToString()),
                    BotName = (string) reader["bot_name"],
                    GuideChannelId = Convert.ToUInt64(reader["guide_channel_id"]?.ToString()),
                    DownloadChannelId = Convert.ToUInt64(reader["download_channel_id"]?.ToString())
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static RoleDetails CheckIfUserNeedsRoleLonger(ulong customerId, ulong roleId)
        {
            var config = DiscordFunctions.GetConfig();
            var connectionStr = config["db_connection"].ToString();

            using var conn = new MySqlConnection(connectionStr);
            try
            {
                conn.Open();

                var checkCmd = new MySqlCommand("SELECT * FROM bot_roles WHERE role_id=@role_id", conn);
                checkCmd.Parameters.AddWithValue("@role_id", roleId);
                checkCmd.Prepare();

                using var reader = checkCmd.ExecuteReader();

                reader.Read();

                return new RoleDetails
                {
                    RoleId = Convert.ToUInt64(reader["role_id"].ToString()),
                    BotName = (string)reader["bot_name"],
                    GuideChannelId = Convert.ToUInt64(reader["guide_channel_id"]?.ToString()),
                    DownloadChannelId = Convert.ToUInt64(reader["download_channel_id"]?.ToString())
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }
    }
}