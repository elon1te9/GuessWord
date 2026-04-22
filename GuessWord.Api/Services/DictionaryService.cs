namespace GuessWord.Api.Services;

public class IDictionaryService
{
    private readonly HashSet<string> _allWords;
    private readonly List<string> _secretWords;
    private readonly Random _random = new();

    public IDictionaryService(IWebHostEnvironment environment)
    {
        var allWordsPath = Path.Combine(environment.ContentRootPath, "Resources", "all-words.txt");
        var secretWordsPath = Path.Combine(environment.ContentRootPath, "Resources", "secret-words.txt");

        if (!File.Exists(allWordsPath))
        {
            throw new FileNotFoundException("Файл полного словаря не найден.", allWordsPath);
        }

        if (!File.Exists(secretWordsPath))
        {
            throw new FileNotFoundException("Файл слов для загадывания не найден.", secretWordsPath);
        }

        var allWords = File.ReadAllLines(allWordsPath)
            .Select(word => word.Trim().ToLower())
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct()
            .ToList();

        _allWords = new HashSet<string>(allWords);

        var secretWords = File.ReadAllLines(secretWordsPath)
            .Select(word => word.Trim().ToLower())
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct()
            .ToList();

        var invalidSecretWords = secretWords
            .Where(word => !_allWords.Contains(word))
            .ToList();

        if (invalidSecretWords.Count > 0)
        {
            throw new Exception("Некоторые слова из secret-words.txt отсутствуют в all-words.txt.");
        }

        _secretWords = secretWords;
    }

    public bool WordExists(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        return _allWords.Contains(word.Trim().ToLower());
    }

    public string GetRandomSecretWord()
    {
        if (_secretWords.Count == 0)
        {
            throw new InvalidOperationException("Список слов для загадывания пуст.");
        }

        var index = _random.Next(_secretWords.Count);
        return _secretWords[index];
    }

    public int GetAllWordsCount()
    {
        return _allWords.Count;
    }

    public int GetSecretWordsCount()
    {
        return _secretWords.Count;
    }
}