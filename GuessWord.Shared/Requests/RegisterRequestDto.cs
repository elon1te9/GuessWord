namespace GuessWord.Shared.Requests
{
    public class RegisterRequestDto
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
