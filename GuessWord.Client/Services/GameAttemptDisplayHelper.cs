using GuessWord.Shared.Responses;

namespace GuessWord.Client.Services;

public static class GameAttemptDisplayHelper
{
    public static List<GameAttemptResponseDto> BuildSingleGameAttempts(
        IEnumerable<GameAttemptResponseDto>? attempts)
    {
        if (attempts is null)
            return [];

        return attempts
            .Where(attempt => attempt.IsValid && attempt.Rank.HasValue)
            .GroupBy(attempt => attempt.Word.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(attempt => attempt.Rank).ThenBy(attempt => attempt.CreatedAt).First())
            .OrderBy(attempt => attempt.Rank)
            .ThenBy(attempt => attempt.Word)
            .ToList();
    }

    public static List<GameAttemptResponseDto> BuildMultiplayerAttempts(
        IEnumerable<GameAttemptDto>? attempts)
    {
        if (attempts is null)
            return [];

        return attempts
            .Where(attempt => attempt.Rank.HasValue)
            .GroupBy(attempt => attempt.Word.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(attempt => attempt.Rank).ThenBy(attempt => attempt.CreatedAt).First())
            .OrderBy(attempt => attempt.Rank)
            .ThenBy(attempt => attempt.Word)
            .Select(attempt => new GameAttemptResponseDto
            {
                Word = attempt.Word,
                Rank = attempt.Rank,
                IsValid = true,
                CreatedAt = attempt.CreatedAt
            })
            .ToList();
    }

    public static string BuildAttemptKey(string word, int? rank, DateTime createdAt)
    {
        return $"{word}|{rank}|{createdAt:O}";
    }

    public static string BuildAttemptKey(GameAttemptResponseDto attempt)
    {
        return BuildAttemptKey(attempt.Word, attempt.Rank, attempt.CreatedAt);
    }
}
