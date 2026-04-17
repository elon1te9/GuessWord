namespace GuessWord.Api.Models
{
    public class GameAttempt
    {
        public int Id { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Word { get; set; } = null!;
        public int? Rank { get; set; }
        public bool IsValid { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
