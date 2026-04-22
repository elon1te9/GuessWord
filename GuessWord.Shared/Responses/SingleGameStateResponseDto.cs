using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWord.Shared.Responses
{
    public class SingleGameStateResponseDto
    {
        public int GameId { get; set; }
        public string Status { get; set; } = null!;
        public int AttemptsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GameAttemptItemDto> Attempts { get; set; } = new();
    }

    public class GameAttemptItemDto
    {
        public string Word { get; set; } = null!;
        public int? Rank { get; set; }
        public bool IsValid { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
