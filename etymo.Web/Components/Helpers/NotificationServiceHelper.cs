namespace etymo.Web.Components.Helpers
{
    using System;

    public class NotificationServiceHelper
    {
        public event Action<string, NotificationType>? OnShowNotification;
        public event Action? OnClearNotification;

        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        public void ShowSuccess(string message)
        {
            OnShowNotification?.Invoke(message, NotificationType.Success);
        }

        public void ShowError(string message)
        {
            OnShowNotification?.Invoke(message, NotificationType.Error);
        }

        public void ShowWarning(string message)
        {
            OnShowNotification?.Invoke(message, NotificationType.Warning);
        }

        public void ShowInfo(string message)
        {
            OnShowNotification?.Invoke(message, NotificationType.Info);
        }

        public void ClearNotification()
        {
            OnClearNotification?.Invoke();
        }
    }
}
