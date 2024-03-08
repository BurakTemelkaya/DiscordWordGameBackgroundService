using DiscordWordGame.dataAccess.models;
using DiscordWordGameDotNetCore.dataAccess.models;
using Microsoft.EntityFrameworkCore;

namespace DiscordWordGameDotNetCore.dataAccess
{
    public class Context : DbContext
    {
        public DbSet<PlayerPointDataModel> PlayerPoints { get; set; }
        public DbSet<PlayingChannelsDataModel> PlayingRooms { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/DiscordWordGame/words.db");
            optionsBuilder.UseSqlite($"Data Source={path}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerPointDataModel>(a =>
            {
                a.ToTable("PlayerPoints");
                a.HasKey(g => g.Id).HasName("Id");
                a.Property(g => g.ServerId).HasColumnName("ServerId");
                a.Property(g => g.PlayerId).HasColumnName("PlayerId");
                a.Property(g => g.Point).HasColumnName("Point");
                a.Property(g => g.WordCount).HasColumnName("WordCount");
            });

            modelBuilder.Entity<PlayingChannelsDataModel>(a =>
            {
                a.ToTable("PlayingChannels");
                a.HasKey(g => g.ServerId).HasName("ServerId");
                a.Property(g => g.ChannelId).HasColumnName("ChannelId");
                a.Property(g => g.PlayWordCount).HasColumnName("PlayWordCount");
            });
        }
    }
}
