using GuessWord.Shared.Responses;

namespace GuessWord.Api.Services
{
    public class AuthOperationResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public AuthResponseDto? Data { get; init; }
    }
}
