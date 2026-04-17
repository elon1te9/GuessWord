namespace GuessWord.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Login { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Room> HostedRooms { get; set; } = new();
        public List<Room> GuestRooms { get; set; } = new();
        public List<GamePlayer> GamePlayers { get; set; } = new();
        public List<GameAttempt> GameAttempts { get; set; } = new();
    }
}
