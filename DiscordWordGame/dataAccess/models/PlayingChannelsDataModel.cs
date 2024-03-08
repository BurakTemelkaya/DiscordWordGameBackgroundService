namespace DiscordWordGameDotNetCore.dataAccess.models
{
    public class PlayingChannelsDataModel
    {
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public int PlayWordCount { get; set; }
    }
}
