using Microsoft.Toolkit.Uwp.Notifications;

namespace ToastFish.Services.Notifications
{
    public class NotificationService
    {
        public const string AppTitle = "Nihongo ToastFish";

        public void ShowMessage(string message, string buttonText = "")
        {
            ToastContentBuilder builder = new ToastContentBuilder()
                .AddText(AppTitle)
                .AddText(message);

            if (!string.IsNullOrEmpty(buttonText))
            {
                builder.AddButton(new ToastButton()
                    .SetContent(buttonText)
                    .AddArgument("action", NotificationAction.Succeed)
                    .SetBackgroundActivation());
            }

            builder.Show();
        }
    }
}
