using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace DiscordWordGame.configuration
{
    public class JSONReader
    {
        public string Token { get; set; }
        public string Prefix { get; set; }

        public async Task ReadJson()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/DiscordWordGame/config.json");
            using (StreamReader sr = new StreamReader(path))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

                this.Token = data.Token;
                this.Prefix = data.Prefix;
            }
        }
    }

    public class JSONStructure
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
    }
}
