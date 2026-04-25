using GuessWord.Shared.Enums;

namespace GuessWord.Shared.Responses
{
    public class SingleGameResponseDto
    {
        public int GameId { get; set; }

        public int AttemptsCount { get; set; }

        public GameStatus Status { get; set; }

        public GamePlayerResult PlayerResult { get; set; }

        public string? SecretWord { get; set; }

        public GameAttemptResponseDto? LastAttempt { get; set; }

        public bool LastAttemptWasRepeated { get; set; }

        public List<GameAttemptResponseDto> Attempts { get; set; } = new();
    }
}
