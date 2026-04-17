namespace GuessWord.Client.Services
{
    public class UserStateService
    {
        public string? Token { get; private set; }
        public string? Login { get; private set; }
        public string? Name { get; private set; }

        public bool IsAuthorized => !string.IsNullOrWhiteSpace(Token);

        public event Action? OnChange;

        public void SetUser(string token, string login, string? name)
        {
            Token = token;
            Login = login;
            Name = name;
            NotifyStateChanged();
        }

        public void ClearUser()
        {
            Token = null;
            Login = null;
            Name = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
