namespace GuessWord.Shared.Responses
{
    public class LeaderboardResponseDto
    {
        public List<LeaderboardEntryResponseDto> Entries { get; set; } = [];
        public int TotalPlayers { get; set; }
    }
}
