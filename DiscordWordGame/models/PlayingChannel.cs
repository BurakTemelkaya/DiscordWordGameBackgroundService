using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWordGame.models
{
    public class PlayingChannel
    {
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public int PlayTotalWord { get; set; }
    }
}
