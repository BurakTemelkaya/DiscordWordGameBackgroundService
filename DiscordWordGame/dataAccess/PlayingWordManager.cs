using DiscordWordGame.dataAccess.models;
using DiscordWordGame.models;
using DiscordWordGameDotNetCore.dataAccess;
using Microsoft.EntityFrameworkCore;

namespace DiscordWordGame.dataAccess
{
    public static class PlayingWordManager
    {
        public static async Task AddAsync(List<PlayingWord> playingUsers, ulong serverId)
        {
            List<PlayerPointDataModel> endPlayingUsers = new();

            foreach (var item in playingUsers)
            {
                //botun eklediği mesajları geç
                if (item.PlayerId == 0)
                {
                    continue;
                }
                else if (item.ServerId != serverId)
                {
                    continue;
                }
                else if (endPlayingUsers.Any(x => x.PlayerId == item.PlayerId))
                {
                    continue;
                }

                var playingUser = playingUsers.Where(x => x.ServerId == serverId && x.PlayerId == item.PlayerId);

                var point = playingUser.Sum(x => x.Word.Length);
                var wordCount = playingUser.Count();

                endPlayingUsers.Add(new PlayerPointDataModel
                {
                    PlayerId = item.PlayerId,
                    Point = point,
                    WordCount = wordCount,
                    ServerId = item.ServerId,
                });
            }

            using Context context = new();

            var lastPlays = await context.PlayerPoints.Where(x => x.ServerId == serverId).AsNoTracking().ToListAsync();

            var updatedData = new List<PlayerPointDataModel>();

            foreach (var item in lastPlays)
            {
                var lastPlay = endPlayingUsers.FirstOrDefault(x => x.PlayerId == item.PlayerId);

                if (lastPlay != null)
                {
                    var playingUser = playingUsers.Where(x => x.ServerId == serverId && x.PlayerId == item.PlayerId);

                    var point = playingUser.Sum(x => x.Word.Length);
                    var wordCount = playingUser.Count();

                    item.Point += point;
                    item.WordCount += wordCount;
                    updatedData.Add(item);

                    var removedUser = endPlayingUsers.First(x => x.PlayerId == item.PlayerId);

                    endPlayingUsers.Remove(removedUser);
                }
            }

            if (updatedData.Count > 0)
            {
                context.PlayerPoints.UpdateRange(updatedData);
            }

            var addedUser = new List<PlayerPointDataModel>();

            foreach (var item in endPlayingUsers)
            {
                addedUser.Add(new PlayerPointDataModel
                {
                    PlayerId = item.PlayerId,
                    Point = item.Point,
                    ServerId = item.ServerId,
                    WordCount = item.WordCount,
                });
            }

            if (addedUser.Count > 0)
            {
                context.PlayerPoints.AddRange(addedUser);
            }

            if (addedUser.Count > 0 || updatedData.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        public static async Task<List<PlayerPointDataModel>> GetAsync(ulong serverId)
        {
            using Context context = new();

            var data = await context.PlayerPoints.Where(x => x.ServerId == serverId).OrderByDescending(x => x.Point).Take(10).ToListAsync();

            return data;
        }
    }
}
