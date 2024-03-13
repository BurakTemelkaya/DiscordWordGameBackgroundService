using DiscordWordGameDotNetCore.dataAccess.models;
using DiscordWordGameDotNetCore.dataAccess;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Text;

namespace DiscordWordGame.commands
{
    public class WordSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("start", "Kelime oyununu başlatılacağı odayı ve tur başına kelime sayısını seçmek için kullanılır.")]
        public async Task SlashCommandAdd(InteractionContext interactionContext, [Option("WordCount",
                                        "Tur başına kelime sayısı (10 ile 1000 arasında bir sayı giriniz.)")] long playTotalWord = 100)
        {
            var desiredRoleId = interactionContext.Guild.Roles.FirstOrDefault(r => r.Value.Name == "kelime-oyunu-yönetici").Key;

            var isAdmin = interactionContext.Member.Roles.Any(r => r.Permissions.HasPermission(Permissions.Administrator));

            if (!isAdmin && desiredRoleId == 0)
            {
                await interactionContext.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("yönetici veya kelime-oyunu-yönetici rolü bulunamadı. " +
                    "Oyunu başlatmak için yönetici veya kelime-oyunu-yönetici rolüne sahip olmalısın.")
                    );
                return;
            }
            else if (!isAdmin && !interactionContext.Member.Roles.Select(role => role.Id).Contains(desiredRoleId))
            {
                await interactionContext.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Bu komutu sadece yönetici veya kelime-oyunu-yönetici rolüne sahip kişiler çalıştırabilir.")
                    );
                return;
            }

            ulong roomId = interactionContext.Channel.Id;
            ulong serverId = interactionContext.Guild.Id;

            if (playTotalWord < 10 || playTotalWord > 1000)
            {
                await interactionContext.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Toplam oynanacak kelime sayısı 10 ile 1000 arasında olmalıdır.")
                    );
                return;
            }

            var dataRoom = await PlayingRoomManager.AddRoom(new PlayingChannelsDataModel
            {
                ServerId = serverId,
                ChannelId = roomId,
                PlayWordCount = (int)playTotalWord
            });

            if (dataRoom != null)
            {
                StringBuilder stringBuilder = new StringBuilder();

                var room = WordManager.PlayingChannels.FirstOrDefault(x => x.ServerId == serverId);

                if (room.ChannelId != roomId)
                {
                    room.ChannelId = roomId;

                    stringBuilder.Append($"Kelime oyunu odası {interactionContext.Channel.Mention} olarak güncellenmiştir.");
                }
                if (room.PlayTotalWord != playTotalWord)
                {
                    room.PlayTotalWord = (int)playTotalWord;

                    if(stringBuilder.Length > 0)
                    {
                        stringBuilder.Append('\n');
                    }

                    stringBuilder.Append($"Kelime oyunu tur başına kelime sayısı {playTotalWord} olarak güncellenmiştir.");
                }

                await interactionContext.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent(stringBuilder.ToString())
                    );
            }
            else
            {
                WordManager.PlayingChannels.Add(new models.PlayingChannel
                {
                    ChannelId = roomId,
                    ServerId = serverId,
                    PlayTotalWord = (int)playTotalWord
                });

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

                await interactionContext.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Kelime oyunu yeni başlamıştır ilk kelime: {firstWord}")
                    );
            }
        }
    }
}
