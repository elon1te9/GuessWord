using System.ComponentModel.DataAnnotations.Schema;

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public int? GameId { get; set; }

        public Game? Game { get; set; }
    }
}
