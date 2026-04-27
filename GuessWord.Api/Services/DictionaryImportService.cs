using GuessWord.Api.Data;
using GuessWord.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class DictionaryImportService
    {
        private const int MinWordsCount = 20_000;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DictionaryImportService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task ImportAsync()
        {
            if (await _context.Words.AnyAsync())
                return;

            var allWordsPath = Path.Combine(_environment.ContentRootPath, "Resources", "all-words.txt");
            var secretWordsPath = Path.Combine(_environment.ContentRootPath, "Resources", "secret-words.txt");

            if (!File.Exists(allWordsPath))
                throw new FileNotFoundException("Файл полного словаря не найден.", allWordsPath);

            if (!File.Exists(secretWordsPath))
                throw new FileNotFoundException("Файл слов для загадывания не найден.", secretWordsPath);

            var allWords = File.ReadAllLines(allWordsPath)
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var allWordsSet = allWords.ToHashSet();

            var secretWords = File.ReadAllLines(secretWordsPath)
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToHashSet();

            if (allWords.Count < MinWordsCount)
                throw new InvalidOperationException(
                    $"Полный словарь слишком маленький. Требуется не менее {MinWordsCount} слов, найдено {allWords.Count}.");

            var missingSecretWords = secretWords
                .Where(word => !allWordsSet.Contains(word))
                .Take(20)
                .ToList();

            if (missingSecretWords.Count > 0)
                throw new InvalidOperationException(
                    "В полном словаре отсутствуют слова из secret-words.txt: "
                    + string.Join(", ", missingSecretWords));

            var words = allWords.Select(word => new Word
            {
                Text = word,
                CanBeSecret = secretWords.Contains(word)
            }).ToList();

            _context.Words.AddRange(words);
            await _context.SaveChangesAsync();
        }
    }
}
