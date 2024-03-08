using DiscordWordGameDotNetCore.dataAccess.models;
using Microsoft.EntityFrameworkCore;

namespace DiscordWordGameDotNetCore.dataAccess
{
    public static class PlayingRoomManager
    {
        public static async Task<PlayingChannelsDataModel> AddRoom(PlayingChannelsDataModel playingChannel)
        {
            using Context context = new();

            var room = await context.PlayingRooms.FirstOrDefaultAsync(x => x.ServerId == playingChannel.ServerId);

            if (room != null)
            {
                room.ChannelId = playingChannel.ServerId;
                room.PlayWordCount = playingChannel.PlayWordCount;

                context.PlayingRooms.Update(room);
                return playingChannel;
            }
            else
            {
                context.PlayingRooms.Add(playingChannel);
            }

            await context.SaveChangesAsync();

            return null;
        }

        public static async Task<List<PlayingChannelsDataModel>> GetChannelsAsync()
        {
            using Context context = new();
            return await context.PlayingRooms.AsNoTracking().ToListAsync();
        }
    }
}
