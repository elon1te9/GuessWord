using GuessWord.Api.Data;
using GuessWord.Api.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using System.Globalization;

namespace GuessWord.Api.Services
{
    public class DictionaryImportService
    {
        private const int MinWordsCount = 20_000;
        private const int ExpectedVectorDimensions = 800;
        private const int BatchSize = 500;

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public DictionaryImportService(
            AppDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task ImportAsync()
        {
            if (await _context.Words.AnyAsync())
                return;

            var allWordsPath = ResolveDictionaryPath(
                _configuration["Dictionary:AllWordsPath"],
                Path.Combine("Resources", "all-words.txt"),
                "Файл полного словаря не найден.");

            var secretWordsPath = ResolveDictionaryPath(
                _configuration["Dictionary:SecretWordsPath"],
                Path.Combine("Resources", "secret-words.txt"),
                "Файл слов для загадывания не найден.");

            var sociationVecPath = ResolveDictionaryPath(
                _configuration["Dictionary:SociationVecPath"],
                Path.Combine("Resources", "Data", "sociation2vec800.vec"),
                "Файл sociation2vec800.vec не найден.");

            var allWords = LoadWords(allWordsPath);
            var secretWords = LoadWords(secretWordsPath).ToHashSet(StringComparer.Ordinal);

            ValidateWordSets(allWords, secretWords);

            await ImportWordsWithEmbeddingsAsync(
                sociationVecPath,
                allWords,
                secretWords);
        }

        private string ResolveDictionaryPath(
            string? configuredPath,
            string defaultRelativePath,
            string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                var resolvedConfiguredPath = Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(_environment.ContentRootPath, configuredPath);

                if (File.Exists(resolvedConfiguredPath))
                    return resolvedConfiguredPath;
            }

            var defaultPath = Path.Combine(_environment.ContentRootPath, defaultRelativePath);
            if (File.Exists(defaultPath))
                return defaultPath;

            throw new FileNotFoundException(
                $"{errorMessage} Укажи корректный путь в конфиге.",
                defaultPath);
        }

        private static List<string> LoadWords(string path) =>
            File.ReadLines(path)
                .Select(NormalizeWord)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static void ValidateWordSets(
            List<string> allWords,
            HashSet<string> secretWords)
        {
            if (allWords.Count < MinWordsCount)
            {
                throw new InvalidOperationException(
                    $"Полный словарь слишком маленький. Требуется не менее {MinWordsCount} слов, найдено {allWords.Count}.");
            }

            if (secretWords.Count == 0)
            {
                throw new InvalidOperationException("Список secret-words.txt пустой.");
            }

            var allWordsSet = allWords.ToHashSet(StringComparer.Ordinal);
            var missingSecretWords = secretWords
                .Where(word => !allWordsSet.Contains(word))
                .Take(20)
                .ToList();

            if (missingSecretWords.Count > 0)
            {
                throw new InvalidOperationException(
                    "В полном словаре отсутствуют слова из secret-words.txt: "
                    + string.Join(", ", missingSecretWords));
            }
        }

        private async Task ImportWordsWithEmbeddingsAsync(
            string sociationVecPath,
            List<string> allWords,
            HashSet<string> secretWords)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            using var reader = new StreamReader(sociationVecPath);
            var header = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(header))
                throw new InvalidOperationException("Файл sociation2vec800.vec пустой.");

            ValidateVecHeader(header);

            var pendingWords = allWords.ToHashSet(StringComparer.Ordinal);
            var importedWords = new HashSet<string>(StringComparer.Ordinal);
            var batch = new List<Word>(BatchSize);

            while (!reader.EndOfStream && pendingWords.Count > 0)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!TryParseVecLine(line, out var word, out var vector))
                    continue;

                var normalizedWord = NormalizeWord(word);
                if (!pendingWords.Contains(normalizedWord) || importedWords.Contains(normalizedWord))
                    continue;

                batch.Add(new Word
                {
                    Text = normalizedWord,
                    CanBeSecret = secretWords.Contains(normalizedWord),
                    Embedding = new Vector(vector)
                });

                importedWords.Add(normalizedWord);
                pendingWords.Remove(normalizedWord);

                if (batch.Count >= BatchSize)
                {
                    _context.Words.AddRange(batch);
                    await _context.SaveChangesAsync();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                _context.Words.AddRange(batch);
                await _context.SaveChangesAsync();
            }

            if (pendingWords.Count > 0)
            {
                throw new InvalidOperationException(
                    "Не удалось импортировать слова из sociation2vec800.vec: "
                    + string.Join(", ", pendingWords.Take(20)));
            }

            await transaction.CommitAsync();
        }

        private static void ValidateVecHeader(string header)
        {
            var headerParts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (headerParts.Length != 2
                || !int.TryParse(headerParts[1], out var dimensions)
                || dimensions != ExpectedVectorDimensions)
            {
                throw new InvalidOperationException(
                    $"Ожидался vec-файл с размерностью {ExpectedVectorDimensions}, получено: '{header}'.");
            }
        }

        private static bool TryParseVecLine(
            string line,
            out string word,
            out float[] vector)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != ExpectedVectorDimensions + 1)
            {
                word = string.Empty;
                vector = [];
                return false;
            }

            word = parts[0];
            vector = new float[ExpectedVectorDimensions];

            for (var i = 0; i < ExpectedVectorDimensions; i++)
            {
                if (!float.TryParse(
                        parts[i + 1],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out vector[i]))
                {
                    word = string.Empty;
                    vector = [];
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeWord(string value) =>
            value.Trim().ToLowerInvariant().Replace('ё', 'е');
    }
}
