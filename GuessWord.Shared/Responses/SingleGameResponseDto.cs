using GuessWord.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWord.Shared.Responses
{
    public class SingleGameResponseDto
    {
        public int GameId { get; set; }

        public int AttemptsCount { get; set; }

        public GameStatus Status { get; set; }

        public GamePlayerResult PlayerResult { get; set; }

        public string? SecretWord { get; set; }

        public List<GameAttemptResponseDto> Attempts { get; set; } = new();
    }
}
