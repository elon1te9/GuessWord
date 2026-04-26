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
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GamePlayer> GamePlayers { get; set; }
        public DbSet<GameAttempt> GameAttempts { get; set; }
        public DbSet<Word> Words { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Login)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .Property(x => x.Status)
                .HasDefaultValue(GuessWord.Shared.Enums.RoomStatus.Waiting);

            modelBuilder.Entity<Word>()
                .HasIndex(x => x.Text)
                .IsUnique();

            modelBuilder.Entity<Word>()
                .Property(x => x.Embedding)
                .HasColumnType("vector(384)");

            modelBuilder.Entity<GamePlayer>()
                .HasIndex(x => new { x.UserId, x.IsActiveSingleGame })
                .IsUnique()
                .HasFilter("\"IsActiveSingleGame\" = true");

            modelBuilder.Entity<Game>()
                .HasOne(g => g.SecretWord)
                .WithMany(w => w.SecretWordGames)
                .HasForeignKey(g => g.SecretWordId)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Game)
                .WithMany()
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
