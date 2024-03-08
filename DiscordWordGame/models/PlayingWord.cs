using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWordGame.models
{
    public class PlayingWord
    {
        public ulong ServerId { get; set; }
        public ulong PlayerId { get; set; }
        public string Word { get; set; }
        public DateTime PlayingDate { get; set; }
    }
}
