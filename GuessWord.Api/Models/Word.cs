using Pgvector;

namespace GuessWord.Api.Models
{
    public class Word
    {
        public int Id { get; set; }
        public string Text { get; set; } = null!;
        public bool CanBeSecret { get; set; }
        public Vector? Embedding { get; set; }
        public List<Game> SecretWordGames { get; set; } = new();
    }
}
