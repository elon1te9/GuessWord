namespace GuessWord.Shared.Responses
{
    public class RoomResponseDto
    {
        public string Code { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string? GuestName { get; set; }
        public bool IsFull { get; set; }
        public bool CanStartGame { get; set; }
        public bool IsHost { get; set; }
    }
}
