using GuessWord.Shared.Enums;

namespace GuessWord.Api.Models
{
    public class GamePlayer
    {
        public int Id { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public GamePlayerResult Result { get; set; } = GamePlayerResult.Playing;

        public int AttemptsCount { get; set; } = 0;

        public bool IsActiveSingleGame { get; set; } = false;
    }
}
