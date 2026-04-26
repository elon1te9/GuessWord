namespace GuessWord.Shared.Responses
{
    public class GamePlayerStateDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<GameAttemptDto> Attempts { get; set; } = new();
    }
}
