using DiscordWordGame.configuration;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DiscordWordGame.dataAccess;
using DiscordWordGame.models;
using DiscordWordGame;
using DiscordWordGameDotNetCore.dataAccess;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Text;
using DiscordWordGame.commands;
using DSharpPlus.SlashCommands;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CvProjectUI
{
    public class DiscordWordGameWorker : BackgroundService
    {
        public DiscordClient Client { get; set; }

        private CommandsNextExtension Commands { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jsonReader = new JSONReader();

            await jsonReader.ReadJson();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            Client.Ready += Client_Ready;

            Client.MessageCreated += Message_Created;

            var commandConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[]
                {
                    jsonReader.Prefix
                },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = true,
            };

            var slashCommand = Client.UseSlashCommands();

            slashCommand.RegisterCommands<WordSlashCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1, stoppingToken);
        }

        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            if (WordManager.Words.Count == 0)
            {
                await WordManager.AddAllWords();
            }

            var playingRooms = await PlayingRoomManager.GetChannelsAsync();

            playingRooms.ForEach(x =>
            {
                WordManager.PlayingChannels.Add(new PlayingChannel
                {
                    ChannelId = x.ChannelId,
                    PlayTotalWord = x.PlayWordCount,
                    ServerId = x.ServerId,
                });
            });

            await Task.CompletedTask;
        }

        private async Task Message_Created(DiscordClient sender, MessageCreateEventArgs args)
        {
            ulong serverId = args.Guild.Id;
            var lastUser = PlayerUsers.LastOrDefault(x => x.ServerId == serverId);
            //var mentionUser = sender.CurrentUser.Mention; kullanıcıyı etiketlemek için kullanılabilir.
            if (args.Message.Author.IsBot)
            {
                return;
            }

            else if (!WordManager.PlayingChannels.Any(x => x.ServerId == serverId && x.ChannelId == args.Channel.Id))
            {
                return;
            }
            else if (lastUser != null && lastUser.PlayerId == args.Message.Author.Id)
            {
                await ReactDeniedMessageAsync(args);
                await args.Message.RespondAsync("Bir kullanıcı arka arkaya 2 kez oynayamaz.");
                return;
            }

            if (!WordManager.PlayingWords.Any(p => p.ServerId == serverId))
            {
                var messages = await args.Channel.GetMessagesAsync();
                string lastWord = string.Empty;

                foreach (var message in messages)
                {
                    if (message.Reactions.Any(x => x.IsMe && x.Emoji == "✅"))
                    {
                        lastUser = new()
                        {
                            PlayerId = message.Author.Id,
                            PlayingDate = message.CreationTimestamp.Date,
                            ServerId = serverId,
                        };

                        lastWord = message.Content;
                        WordManager.PlayingWords.Add(new PlayingWord
                        {
                            PlayerId = message.Author.Id,
                            PlayingDate = message.CreationTimestamp.Date,
                            ServerId = serverId,
                            Word = lastWord
                        });

                        if (lastUser.PlayerId == args.Author.Id)
                        {
                            await ReactDeniedMessageAsync(args);
                            await args.Message.RespondAsync("Bir kullanıcı arka arkaya 2 kez oynayamaz.");
                            return;
                        }
                        
                        break;
                    }
                }

                if (string.IsNullOrEmpty(lastWord))
                {
                    string randomWord = WordManager.AddRandomWord(serverId);
                    await args.Message.RespondAsync($"Son kelime bulunamadı rastgele yeni bir kelime oluşturuldu. Kelime: {randomWord}");
                    return;
                }
            }

            if (await AddWord(args.Message.Content, args))
            {
                PlayUser(args.Message.Author.Id, serverId);
            }

            await Task.CompletedTask;
        }


        static List<PlayingUser> PlayerUsers = new List<PlayingUser>();

        private async Task<bool> AddWord(string word, MessageCreateEventArgs args)
        {
            var user = args.Message.Author;
            ulong serverId = args.Guild.Id;
            word = word.ToLower();

            if (word.Length < 2)
            {
                await args.Message.RespondAsync($"Lütfen kelimeyi en az 2 harfli giriniz. @{user.Username}");
                await ReactDeniedMessageAsync(args);
                return false;
            }
            else
            {
                var lastWord = WordManager.PlayingWords.Last(x => x.ServerId == serverId).Word;
                if (word[0] != lastWord[^1])
                {
                    await args.Message.RespondAsync($"Lütfen {lastWord[lastWord.Length - 1]} harfi ile başlayan bir kelime yazınız. Son kelime {lastWord}");
                    await ReactDeniedMessageAsync(args);
                    return false;
                }
                else if (WordManager.PlayingWords.Any(x => x.ServerId == serverId && x.Word == word))
                {
                    await args.Message.RespondAsync($"Bu kelime daha önce yazıldı. Son kelime {lastWord}");

                    await ReactDeniedMessageAsync(args);

                    return false;

                }
                else if (WordManager.Words.Any(x => x == word))
                {
                    if (word[0] == lastWord[lastWord.Length - 1])
                    {
                        PlayingWord playingWord = new()
                        {
                            ServerId = serverId,
                            PlayerId = user.Id,
                            Word = word,
                            PlayingDate = DateTime.Now,
                        };

                        WordManager.PlayingWords.Add(playingWord);

                        lastWord = word;

                        await ReactApproveMessageAsync(args);

                        if (lastWord[lastWord.Length - 1] == 'ğ')
                        {
                            lastWord = WordManager.AddRandomWord(serverId);

                            await args.Message.Channel.SendMessageAsync(lastWord);
                        }

                        int playTotalWordCount = WordManager.PlayingChannels.First(x => x.ServerId == serverId).PlayTotalWord;

                        if (WordManager.PlayingWords.Count >= playTotalWordCount)
                        {
                            await WritePoints(serverId, args);
                            await RestartGame(serverId, args);
                        }

                        return true;
                    }
                }
                else
                {
                    await args.Message.RespondAsync($"Böyle bir kelime bulunmamaktadır. Son kelime {lastWord}");
                    await ReactDeniedMessageAsync(args);
                    return false;
                }
            }
            return false;
        }

        private void PlayUser(ulong userId, ulong serverId)
        {
            var lastUser = PlayerUsers.LastOrDefault(x => x.ServerId == serverId);

            if (lastUser != null)
            {
                lastUser.PlayerId = userId;
            }
            else
            {
                PlayerUsers.Add(new PlayingUser
                {
                    ServerId = serverId,
                    PlayerId = userId,
                    PlayingDate = DateTime.Now,
                });
            }

        }

        private async Task ReactApproveMessageAsync(MessageCreateEventArgs args)
        {
            var emote = DiscordEmoji.FromName(Client, ":white_check_mark:");
            await args.Message.CreateReactionAsync(emote);
        }

        private async Task ReactDeniedMessageAsync(MessageCreateEventArgs args)
        {
            var emote = DiscordEmoji.FromName(Client, ":negative_squared_cross_mark:");
            await args.Message.CreateReactionAsync(emote);
        }

        private async Task RestartGame(ulong serverId, MessageCreateEventArgs args)
        {
            WordManager.PlayingWords.RemoveAll(x => x.ServerId == serverId);
            PlayerUsers.RemoveAll(x => x.ServerId == serverId);

            string firstWord = WordManager.AddRandomWord(serverId);

            await args.Channel.SendMessageAsync($"Kelime oyunu sıfırlanmıştır ilk kelime {firstWord}");
        }

        private async Task CalculateGamePoints(ulong serverId)
        {
            await PlayingWordManager.AddAsync(WordManager.PlayingWords, serverId);
        }

        private async Task WritePoints(ulong serverId, MessageCreateEventArgs args)
        {
            await CalculateGamePoints(serverId);

            var data = await PlayingWordManager.GetAsync(serverId);

            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.WithColor(DiscordColor.Cyan);
            embedBuilder.WithTitle($"# {args.Guild.Name} Sunucusunda En Çok Puana Sahip 10 Kişi");

            var stringBuilder = new StringBuilder();
            int queue = 1;
            foreach (var item in data)
            {
                var user = await Client.GetUserAsync(item.PlayerId);
                DiscordEmoji? emote = queue switch
                {
                    1 => DiscordEmoji.FromName(Client, ":first_place:"),
                    2 => DiscordEmoji.FromName(Client, ":second_place:"),
                    3 => DiscordEmoji.FromName(Client, ":third_place:"),
                    4 => DiscordEmoji.FromName(Client, ":four:"),
                    5 => DiscordEmoji.FromName(Client, ":five:"),
                    6 => DiscordEmoji.FromName(Client, ":six:"),
                    7 => DiscordEmoji.FromName(Client, ":seven:"),
                    8 => DiscordEmoji.FromName(Client, ":eight:"),
                    9 => DiscordEmoji.FromName(Client, ":nine:"),
                    10 => DiscordEmoji.FromName(Client, ":keycap_ten:"),
                    _ => null,
                };

                stringBuilder.AppendLine($"{emote} - Kullanıcı: **{user.Username}** \n Puan: **{item.Point}** Toplam Kelime Sayısı: **{item.WordCount}** \n");

                queue++;
            }

            embedBuilder.WithDescription(stringBuilder.ToString());

            await args.Channel.SendMessageAsync(embed: embedBuilder);
        }
    }
}
