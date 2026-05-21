using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;
using ToastFish.Services.Japanese;

namespace ToastFish.Services.Notifications
{
    public class NotificationService
    {
        public const string AppTitle = "Nihongo ToastFish";
        private readonly FuriganaFormatter furiganaFormatter = new FuriganaFormatter();

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

        public void ShowFuriganaMessage(string plainText, string furiganaJson, string buttonText = "")
        {
            string message = furiganaFormatter.ToInlineText(furiganaJson, plainText);
            ShowMessage(message, buttonText);
        }

        public Task<string> WaitForActionAsync(
            IObservable<string> hotKeyObservable = null,
            Func<string, string> hotKeyActionMapper = null)
        {
            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            IDisposable hotKeySubscription = null;

            OnActivated toastHandler = toastArgs =>
            {
                string action = GetActionArgument(toastArgs);
                completion.TrySetResult(action);
            };

            if (hotKeyObservable != null)
            {
                hotKeySubscription = hotKeyObservable.Subscribe(events =>
                {
                    string action = hotKeyActionMapper == null ? events : hotKeyActionMapper(events);
                    if (!string.IsNullOrEmpty(action))
                    {
                        completion.TrySetResult(action);
                    }
                });
            }

            ToastNotificationManagerCompat.OnActivated += toastHandler;

            return completion.Task.ContinueWith(task =>
            {
                ToastNotificationManagerCompat.OnActivated -= toastHandler;
                hotKeySubscription?.Dispose();
                return task.Result;
            });
        }

        public Task<NotificationInputResult> WaitForInputAsync(string inputKey)
        {
            TaskCompletionSource<NotificationInputResult> completion = new TaskCompletionSource<NotificationInputResult>();

            OnActivated toastHandler = toastArgs =>
            {
                string action = GetActionArgument(toastArgs);
                string inputValue = GetUserInputValue(toastArgs, inputKey);
                completion.TrySetResult(new NotificationInputResult(action, inputValue));
            };

            ToastNotificationManagerCompat.OnActivated += toastHandler;

            return completion.Task.ContinueWith(task =>
            {
                ToastNotificationManagerCompat.OnActivated -= toastHandler;
                return task.Result;
            });
        }

        private string GetActionArgument(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            try
            {
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                return args["action"];
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetUserInputValue(ToastNotificationActivatedEventArgsCompat toastArgs, string inputKey)
        {
            try
            {
                return toastArgs.UserInput[inputKey] as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
