namespace GuessWord.Shared.Responses
{
    public class LeaderboardEntryResponseDto
    {
        public int Place { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int WinsCount { get; set; }
        public int GamesCount { get; set; }
        public int WinRate { get; set; }
    }
}
