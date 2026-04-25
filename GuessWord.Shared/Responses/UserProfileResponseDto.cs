namespace GuessWord.Shared.Responses
{
    public class UserProfileResponseDto
    {
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public int WinsCount { get; set; }
        public int LossesCount { get; set; }
        public int WinRate { get; set; }
    }
}
