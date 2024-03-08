using System;

namespace DiscordWordGame.models
{
    public class PlayingUser
    {
        public ulong ServerId { get; set; }
        public ulong PlayerId { get; set; }
        public DateTime PlayingDate { get; set; }
    }
}
