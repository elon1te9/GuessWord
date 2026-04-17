namespace GuessWord.Shared.Responses
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string Login { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
