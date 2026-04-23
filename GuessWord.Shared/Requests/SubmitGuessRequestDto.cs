namespace GuessWord.Shared.Requests
{
    public class SubmitGuessRequestDto
    {
        public int GameId { get; set; }
        public string Word { get; set; } = null!;
    }
}
