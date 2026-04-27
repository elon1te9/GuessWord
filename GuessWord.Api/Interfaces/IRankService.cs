namespace GuessWord.Api.Interfaces
{
    public interface IRankService
    {
        Task PrepareRankingAsync(int secretWordId);
        Task<int> GetRankAsync(int secretWordId, int guessWordId);
        Task<IReadOnlyList<(int WordId, int Rank)>> GetRankingPreviewAsync(int secretWordId, int take);
    }
}
