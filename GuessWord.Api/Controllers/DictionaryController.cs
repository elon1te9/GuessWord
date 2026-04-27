using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DictionaryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRankService _rankService;

    public DictionaryController(AppDbContext context, IRankService rankService)
    {
        _context = context;
        _rankService = rankService;
    }

    [HttpGet("exists")]
    public async Task<IActionResult> Exists([FromQuery] string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return Ok(false);

        var normalizedWord = word.Trim().ToLower();
        var exists = await _context.Words
            .AsNoTracking()
            .AnyAsync(x => x.Text == normalizedWord);

        return Ok(exists);
    }

    [HttpGet("random-secret")]
    public async Task<IActionResult> GetRandomSecretWord()
    {
        var word = await _context.Words
            .AsNoTracking()
            .Where(x => x.CanBeSecret)
            .OrderBy(x => Guid.NewGuid())
            .Select(x => x.Text)
            .FirstOrDefaultAsync();

        if (word is null)
            return NotFound("Секретные слова не найдены.");

        return Ok(word);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var allWordsCount = await _context.Words.AsNoTracking().CountAsync();
        var secretWordsCount = await _context.Words.AsNoTracking().CountAsync(x => x.CanBeSecret);
        var wordsWithoutEmbeddingsCount = await _context.Words.AsNoTracking().CountAsync(x => x.Embedding == null);

        var result = new
        {
            AllWordsCount = allWordsCount,
            SecretWordsCount = secretWordsCount,
            WordsWithoutEmbeddingsCount = wordsWithoutEmbeddingsCount,
            DictionaryReady = allWordsCount >= 20_000 && wordsWithoutEmbeddingsCount == 0
        };

        return Ok(result);
    }

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking([FromQuery] string secretWord, [FromQuery] int take = 100)
    {
        if (string.IsNullOrWhiteSpace(secretWord))
            return BadRequest("Нужно передать secretWord.");

        var normalizedWord = secretWord.Trim().ToLower();

        var secret = await _context.Words
            .AsNoTracking()
            .Where(x => x.Text == normalizedWord)
            .Select(x => new
            {
                x.Id,
                x.Text,
                x.CanBeSecret,
                HasEmbedding = x.Embedding != null
            })
            .FirstOrDefaultAsync();

        if (secret is null)
            return NotFound("Слово не найдено в словаре.");

        if (!secret.HasEmbedding)
            return BadRequest("Для этого слова ещё нет embedding.");

        var preview = await _rankService.GetRankingPreviewAsync(secret.Id, take);
        var wordIds = preview.Select(x => x.WordId).ToList();

        var wordsById = await _context.Words
            .AsNoTracking()
            .Where(x => wordIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Text })
            .ToDictionaryAsync(x => x.Id, x => x.Text);

        var result = new
        {
            SecretWord = secret.Text,
            secret.CanBeSecret,
            RequestedTake = take,
            ReturnedCount = preview.Count,
            Items = preview.Select(item => new
            {
                Word = wordsById.GetValueOrDefault(item.WordId, $"#{item.WordId}"),
                item.Rank
            })
        };

        return Ok(result);
    }
}
