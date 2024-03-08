namespace DiscordWordGame.dataAccess.models
{
    public class PlayerPointDataModel
    {
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong PlayerId { get; set; }
        public int Point { get; set; }
        public int WordCount { get; set; }
    }
}
