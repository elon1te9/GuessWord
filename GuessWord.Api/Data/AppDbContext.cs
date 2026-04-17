using GuessWord.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<WordDictionary> WordDictionaries { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GamePlayer> GamePlayers { get; set; }
        public DbSet<GameAttempt> GameAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.HostUser)
                .WithMany(u => u.HostedRooms)
                .HasForeignKey(r => r.HostUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.GuestUser)
                .WithMany(u => u.GuestRooms)
                .HasForeignKey(r => r.GuestUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
