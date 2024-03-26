using DiscordWordGame.models;

namespace DiscordWordGame
{
    public static class WordManager
    {
        public static List<string> Words = new();
        public static List<PlayingChannel> PlayingChannels = new();
        public static List<PlayingWord> PlayingWords = new();

        /// <summary>
        /// Reads all the words from the file line by line and adds them to the Words list.
        /// </summary>
        /// <returns></returns>
        public static async Task AddAllWords()
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

        /// <summary>
        /// It checks if the last letter of the generated random word is 'ğ' and if there is a word that has been played before and generates words accordingly.
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns>Random word</returns>
        public static string AddRandomWord(ulong serverId)
        {
            Random r = new();

            string firstWord;
            do
            {
                firstWord = Words[r.Next(Words.Count)];
            } while (firstWord[^1] == 'ğ' || PlayingWords.Any(x => x.ServerId == serverId && x.Word == firstWord));

            PlayingWords.Add(
                new PlayingWord
                {
                    Word = firstWord,
                    ServerId = serverId,
                    PlayingDate = DateTime.Now,
                });

            return firstWord;
        }
    }
}
