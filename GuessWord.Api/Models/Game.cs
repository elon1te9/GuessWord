using GuessWord.Shared.Enums;

namespace GuessWord.Api.Models
{
    public class Game
    {
        public int Id { get; set; }

        public GameMode Mode { get; set; }
        public GameStatus Status { get; set; } = GameStatus.InProgress;

        public int SecretWordId { get; set; }
        public Word SecretWord { get; set; } = null!;

        public int? WinnerUserId { get; set; }
        public User? WinnerUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }

        public List<GamePlayer> Players { get; set; } = new();
        public List<GameAttempt> Attempts { get; set; } = new();
    }
}
