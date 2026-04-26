using GuessWord.Shared.Enums;

namespace GuessWord.Api.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;

        public int HostUserId { get; set; }
        public User HostUser { get; set; } = null!;

        public int? GuestUserId { get; set; }
        public User? GuestUser { get; set; }

        public RoomStatus Status { get; set; } = RoomStatus.Waiting;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? GameId { get; set; }

        public Game? Game { get; set; }
    }
}
