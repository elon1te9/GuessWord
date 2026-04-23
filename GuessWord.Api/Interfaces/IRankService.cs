namespace GuessWord.Api.Interfaces
{
    public interface IRankService
    {
        Task<int> GetRankAsync(int secretWordId, int guessWordId);
    }
}
