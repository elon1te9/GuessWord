using GuessWord.Api.Data;
using GuessWord.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class DictionaryImportService
    {
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

            var secretWords = File.ReadAllLines(secretWordsPath)
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToHashSet();

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