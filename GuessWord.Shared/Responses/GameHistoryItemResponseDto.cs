namespace GuessWord.Shared.Responses
{
    public class GameHistoryItemResponseDto
    {
        public DateTime Date { get; set; }
        public string GameType { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int AttemptsCount { get; set; }
        public string SecretWord { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
    }
}
