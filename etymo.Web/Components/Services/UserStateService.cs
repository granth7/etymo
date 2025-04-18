namespace etymo.Web.Components.Services
{
    public class UserStateService
    {
        private string? userId;
        private string? userName;
        private bool isAuthenticated;

        public bool IsAuthenticated
        {
            get => isAuthenticated;
            set
            {
                isAuthenticated = value;
                NotifyStateChanged();
            }
        }

        public string? UserId
        {
            get => userId;
            set
            {
                userId = value;
                NotifyStateChanged();
            }
        }

        public string? UserName
        {
            get => userName;
            set
            {
                userName = value;
                NotifyStateChanged();
            }
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
