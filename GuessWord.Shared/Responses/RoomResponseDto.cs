using GuessWord.Shared.Enums;

namespace GuessWord.Shared.Responses
{
    public class RoomResponseDto
    {
        public string Code { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string? GuestName { get; set; }
        public RoomStatus Status { get; set; }
        public int? GameId { get; set; }
        public bool IsFull { get; set; }
        public bool CanStartGame { get; set; }
        public bool IsHost { get; set; }
    }
}
