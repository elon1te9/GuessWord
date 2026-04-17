using GuessWord.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GuessWord.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DictionaryController : ControllerBase
{
    private readonly DictionaryService _dictionaryService;

    public DictionaryController(DictionaryService dictionaryService)
    {
        _dictionaryService = dictionaryService;
    }

    [HttpGet("exists")]
    public IActionResult Exists([FromQuery] string word)
    {
        var exists = _dictionaryService.WordExists(word);
        return Ok(exists);
    }

    [HttpGet("random-secret")]
    public IActionResult GetRandomSecretWord()
    {
        var word = _dictionaryService.GetRandomSecretWord();
        return Ok(word);
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var result = new
        {
            AllWordsCount = _dictionaryService.GetAllWordsCount(),
            SecretWordsCount = _dictionaryService.GetSecretWordsCount()
        };

        return Ok(result);
    }
}