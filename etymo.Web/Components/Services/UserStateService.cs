namespace etymo.Web.Components.Services
{
    public class UserStateService
    {
        private string? userId;
        private string? userName;
        private bool isAuthenticated;
        private bool isAdmin;

        public bool IsAuthenticated
        {
            get => isAuthenticated;
            set
            {
                isAuthenticated = value;
                NotifyStateChanged();
            }
        }

        public bool IsAdmin
        {
            get => isAdmin;
            set
            {
                isAdmin = value;
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
