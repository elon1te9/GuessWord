namespace GuessWord.Api.Models
{
    public class WordDictionary
    {
        public int Id { get; set; }
        public string Word { get; set; } = null!;

        public List<Game> SecretWordGames { get; set; } = new();
    }
}
