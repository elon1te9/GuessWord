namespace GuessWord.Client.Services
{
    public class UserStateService
    {
        public int? UserId { get; private set; }
        public string? Token { get; private set; }
        public string? Login { get; private set; }
        public string? Name { get; private set; }
        public bool IsInitialized { get; private set; }

        public bool IsAuthorized => !string.IsNullOrWhiteSpace(Token);

        public event Action? OnChange;

        public void SetUser(int userId, string token, string login, string? name)
        {
            UserId = userId;
            Token = token;
            Login = login;
            Name = name;
            NotifyStateChanged();
        }

        public void ClearUser()
        {
            UserId = null;
            Token = null;
            Login = null;
            Name = null;
            NotifyStateChanged();
        }

        public void MarkInitialized()
        {
            IsInitialized = true;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
