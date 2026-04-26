namespace GuessWord.Shared.Responses
{
    public class GameAttemptDto
    {
        public string Word { get; set; } = null!;
        public int? Rank { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
