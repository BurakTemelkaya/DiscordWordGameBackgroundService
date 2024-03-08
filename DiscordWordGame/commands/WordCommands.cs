using DiscordWordGameDotNetCore.dataAccess;
using DiscordWordGameDotNetCore.dataAccess.models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DiscordWordGame.commands
{
    public class WordCommands : BaseCommandModule
    {
        [Command("Start")]
        public async Task Add(CommandContext commandContext, int playTotalWord = 100)
        {
            ulong roomId = commandContext.Channel.Id;
            ulong serverId = commandContext.Guild.Id;

            if (playTotalWord < 10 || playTotalWord > 1000)
            {
                await commandContext.RespondAsync($"Toplam oynanacak kelime sayısı 10 ile 1000 arasında olmalıdır.");
                return;
            }

            Random r = new();

            string firstWord;
            do
            {
                firstWord = WordManager.Words[r.Next(WordManager.Words.Count)];
            } while (firstWord[^1] == 'ğ');

            WordManager.PlayingWords.Add(
                new models.PlayingWord
                {
                    Word = firstWord,
                    ServerId = serverId,
                    PlayingDate = DateTime.Now,
                });

            var dataRoom = await PlayingRoomManager.AddRoom(new PlayingChannelsDataModel
            {
                ServerId = serverId,
                ChannelId = roomId,
                PlayWordCount = playTotalWord
            });

            if (dataRoom != null)
            {
                var room = WordManager.PlayingChannels.FirstOrDefault(x => x.ServerId == serverId);

                if(room.ChannelId != roomId)
                {
                    room.ChannelId = roomId;
                    await commandContext.Client.SendMessageAsync(commandContext.Channel, $"Kelime oyunu odası {commandContext.Channel.Mention} olarak güncellenmiştir.");              
                }
                if(room.PlayTotalWord != playTotalWord)
                {
                    room.PlayTotalWord = playTotalWord;
                    await commandContext.Client.SendMessageAsync(commandContext.Channel, $"Kelime oyunu tur başına kelime sayısı {playTotalWord} olarak güncellenmiştir.");
                }
            }
            else
            {
                WordManager.PlayingChannels.Add(new models.PlayingChannel
                {
                    ChannelId = roomId,
                    ServerId = serverId,
                    PlayTotalWord = playTotalWord
                });
                await commandContext.Client.SendMessageAsync(commandContext.Channel, $"Kelime oyunu yeni başlamıştır ilk kelime: {firstWord}");
            }
        }
    }
}
