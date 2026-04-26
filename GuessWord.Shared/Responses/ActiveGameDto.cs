using GuessWord.Shared.Enums;

namespace GuessWord.Shared.Responses
{
    public class ActiveGameDto
    {
        public int GameId { get; set; }
        public GameMode GameMode { get; set; }
        public GameStatus Status { get; set; }
        public string? RoomCode { get; set; }
    }
}
