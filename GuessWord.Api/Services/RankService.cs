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

        public async Task PrepareRankingAsync(int secretWordId)
        {
            await GetOrBuildRankingAsync(secretWordId);
        }

        public async Task<int> GetRankAsync(int secretWordId, int guessWordId)
        {
            var rankMap = await GetOrBuildRankingAsync(secretWordId);

            return rankMap.TryGetValue(guessWordId, out var rank)
                ? rank
                : int.MaxValue;
        }

        private async Task<Dictionary<int, int>> GetOrBuildRankingAsync(int secretWordId)
        {
            var cacheKey = $"ranking_{secretWordId}";

            if (_cache.TryGetValue(cacheKey, out Dictionary<int, int>? rankMap))
                return rankMap!;

            rankMap = await BuildRankingAsync(secretWordId);
            SetRankingCache(cacheKey, rankMap);

            return rankMap;
        }

        private void SetRankingCache(string cacheKey, Dictionary<int, int> rankMap)
        {
            _cache.Set(cacheKey, rankMap, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3),
                Size = 1
            });
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
                    Score = CosineSimilarity(secretVector, w.Embedding!.ToArray())
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

        private float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA <= 0 || normB <= 0)
                return float.MinValue;

            return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}
