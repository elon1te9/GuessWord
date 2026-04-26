using GuessWord.Shared.Enums;

namespace GuessWord.Shared.Responses
{
    public class MultiplayerGameResponseDto
    {
        public int GameId { get; set; }
        public GameStatus Status { get; set; }
        public GamePlayerResult PlayerResult { get; set; }
        public string? SecretWord { get; set; }
        public int AttemptsCount { get; set; }
        public List<GameAttemptResponseDto> Attempts { get; set; } = new();
        public GameAttemptResponseDto? LastAttempt { get; set; }
        public bool LastAttemptWasRepeated { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public int OpponentAttemptsCount { get; set; }
        public int? OpponentBestRank { get; set; }
        public bool IsWinner { get; set; }
    }
}
