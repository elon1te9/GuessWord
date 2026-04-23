using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GuessWord.Api.Services
{
    public class RankService : IRankService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public RankService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<int> GetRankAsync(int secretWordId, int guessWordId)
        {
            var cacheKey = $"ranking_{secretWordId}";

            if (!_cache.TryGetValue(cacheKey, out Dictionary<int, int>? rankMap))
            {
                rankMap = await BuildRankingAsync(secretWordId);

                _cache.Set(cacheKey, rankMap, TimeSpan.FromHours(3));
            }

            return rankMap.TryGetValue(guessWordId, out var rank)
                ? rank
                : int.MaxValue;
        }

        private async Task<Dictionary<int, int>> BuildRankingAsync(int secretWordId)
        {
            var secret = await _context.Words
                .AsNoTracking()
                .Where(w => w.Id == secretWordId)
                .Select(w => new
                {
                    w.Id,
                    w.Embedding
                })
                .FirstOrDefaultAsync();

            if (secret is null)
                throw new Exception("Секретное слово не найдено.");

            if (secret.Embedding is null)
                throw new Exception("У секретного слова отсутствует embedding.");

            var words = await _context.Words
                .AsNoTracking()
                .Where(w => w.Embedding != null)
                .Select(w => new
                {
                    w.Id,
                    w.Embedding
                })
                .ToListAsync();

            var secretVector = secret.Embedding.ToArray();

            var similarities = words
                .Select(w => new
                {
                    w.Id,
                    Score = Dot(secretVector, w.Embedding!.ToArray())
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var rankMap = new Dictionary<int, int>();

            for (int i = 0; i < similarities.Count; i++)
            {
                rankMap[similarities[i].Id] = i;
            }

            return rankMap;
        }

        private float Dot(float[] a, float[] b)
        {
            float sum = 0;

            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }

            return sum;
        }
    }
}