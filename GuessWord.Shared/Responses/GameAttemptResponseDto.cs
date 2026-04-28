namespace GuessWord.Shared.Responses
{
    public class GameAttemptResponseDto
    {
        public string Word { get; set; } = null!;
        public int? Rank { get; set; }
        public bool IsValid { get; set; }
        public bool IsRepeated { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
