﻿using DiscordWordGame.configuration;
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

            Commands = Client.UseCommandsNext(commandConfig);

            Commands.RegisterCommands<WordCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            if (WordManager.Words.Count == 0)
            {
                await WordManager.AddWords();
            }

            var playingRooms = await PlayingRoomManager.GetChannelsAsync();

            playingRooms.ForEach(async x =>
            {
                WordManager.PlayingChannels.Add(new PlayingChannel
                {
                    ChannelId = x.ChannelId,
                    PlayTotalWord = x.PlayWordCount,
                    ServerId = x.ServerId,
                });

                string firstWord = AddRandomWord(x.ServerId);

                //var discordChannel = await sender.GetChannelAsync(x.ChannelId);

                //await sender.SendMessageAsync(discordChannel, $"Kelime oyunu yeni başlamıştır ilk kelime {firstWord} ");
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
                await args.Message.RespondAsync($"Bir kullanıcı arka arkaya 2 kez oynayamaz.");
                return;
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
                if (word[0] != lastWord[lastWord.Length - 1])
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
                            lastWord = AddRandomWord(serverId);

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

            string firstWord = AddRandomWord(serverId);

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

            embedBuilder.WithColor(DiscordColor.Azure);
            embedBuilder.WithTitle("Bu Sunucuda En Çok Puana Sahip 10 Kişi");

            var stringBuilder = new StringBuilder();
            int queue = 1;
            foreach (var item in data)
            {
                var user = await Client.GetUserAsync(item.PlayerId);

                stringBuilder.AppendLine($"{queue}. Kullanıcı: {user.Username}\n Puan: {item.Point} Toplam Kelime Sayısı: {item.WordCount}");

                queue++;
            }

            embedBuilder.WithDescription(stringBuilder.ToString());

            await args.Channel.SendMessageAsync(embed: embedBuilder);
        }

        private string AddRandomWord(ulong serverId)
        {
            Random r = new();

            string firstWord;
            do
            {
                firstWord = WordManager.Words[r.Next(WordManager.Words.Count)];
            } while (firstWord[firstWord.Length - 1] == 'ğ');

            WordManager.PlayingWords.Add(
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