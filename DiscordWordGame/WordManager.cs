using DiscordWordGame.models;

namespace DiscordWordGame
{
    public static class WordManager
    {
        public static List<string> Words = new();
        public static List<PlayingChannel> PlayingChannels = new();
        public static List<PlayingWord> PlayingWords = new();

        public static async Task AddWords()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/DiscordWordGame/kelime-listesi.txt");

            using (StreamReader sr = new(path))
            {
                string satir;
                while ((satir = await sr.ReadLineAsync()) != null)
                {
                    Words.Add(satir);
                }
            }
        }
    }
}
