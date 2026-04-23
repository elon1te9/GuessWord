using GuessWord.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DictionaryController : ControllerBase
{
    private readonly AppDbContext _context;

    public DictionaryController(AppDbContext context)
    {
        _context = context;
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
        var result = new
        {
            AllWordsCount = await _context.Words.AsNoTracking().CountAsync(),
            SecretWordsCount = await _context.Words.AsNoTracking().CountAsync(x => x.CanBeSecret)
        };

        return Ok(result);
    }
}
