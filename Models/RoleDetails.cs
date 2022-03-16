namespace SlickRentals_Discord_Bot.Models
{
    public class RoleDetails
    {
        public ulong RoleId { get; set; }
        public string BotName { get; set; }
        public ulong GuideChannelId { get; set; }
        public ulong DownloadChannelId { get; set; }
    }
}