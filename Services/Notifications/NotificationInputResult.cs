namespace ToastFish.Services.Notifications
{
    public class NotificationInputResult
    {
        public NotificationInputResult(string action, string inputValue)
        {
            Action = action;
            InputValue = inputValue;
        }

        public string Action { get; }

        public string InputValue { get; }
    }
}
