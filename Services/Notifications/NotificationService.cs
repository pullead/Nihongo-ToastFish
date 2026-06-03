using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToastFish.Model.SqliteControl;
using ToastFish.Services.Japanese;
using ToastFish.Services.Study;
using Windows.UI.Notifications;

namespace ToastFish.Services.Notifications
{
    public class NotificationService
    {
        public const string AppTitle = "Nihongo ToastFish";
        private readonly FuriganaFormatter furiganaFormatter = new FuriganaFormatter();
        private readonly StudyCardNotificationFormatter studyCardFormatter = new StudyCardNotificationFormatter();

        public void ShowMessage(string message, string buttonText = "")
        {
            ToastContentBuilder builder = new ToastContentBuilder()
                .AddText(message);

            if (!string.IsNullOrEmpty(buttonText))
            {
                builder.AddButton(new ToastButton()
                    .SetContent(buttonText)
                    .AddArgument("action", NotificationAction.Succeed)
                    .SetBackgroundActivation());
            }

            try
            {
                ApplyExtendedToastDuration(builder);
                builder.Show();
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteNotificationLog(ex);
            }
            catch (Exception ex)
            {
                WriteNotificationLog(ex);
            }
        }

        public void ShowFuriganaMessage(string plainText, string furiganaJson, string buttonText = "")
        {
            string message = furiganaFormatter.ToInlineText(furiganaJson, plainText);
            ShowMessage(message, buttonText);
        }

        public void ShowStudyCard(StudyCard card, string buttonText = "")
        {
            ShowMessage(studyCardFormatter.FormatSummary(card), buttonText);
        }

        public Task<string> ShowStudyCardNavigationAndWaitAsync(
            StudyCard card,
            IObservable<string> hotKeyObservable = null,
            Func<string, string> hotKeyActionMapper = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string message = UseCustomPopup()
                ? studyCardFormatter.Format(card)
                : studyCardFormatter.FormatSummary(card);
            if (UseCustomPopup())
            {
                return ShowCustomWindowAndWaitAsync(
                    message,
                    CreateStudyButtons(),
                    hotKeyObservable,
                    hotKeyActionMapper,
                    cancellationToken,
                    card.Kind == StudyCardKind.Vocabulary || card.Kind == StudyCardKind.Grammar ? card.PrimaryText : null);
            }

            ToastContentBuilder builder = new ToastContentBuilder()
                .SetToastDuration(ToastDuration.Long)
                .AddText(message)
                .AddButton(new ToastButton()
                    .SetContent("上一个")
                    .AddArgument("action", NotificationAction.Previous)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("下一个")
                    .AddArgument("action", NotificationAction.Next)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("详情")
                    .AddArgument("action", NotificationAction.Details)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("笔记")
                    .AddArgument("action", NotificationAction.AddNote)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("暂停")
                    .AddArgument("action", NotificationAction.Cancel)
                    .SetBackgroundActivation());

            return ShowAndWaitForDismissibleActionAsync(
                builder,
                hotKeyObservable,
                hotKeyActionMapper,
                cancellationToken);
        }

        public Task<string> ShowQuestionAndWaitAsync(
            string title,
            string question,
            IList<string> answerButtons,
            IObservable<string> hotKeyObservable = null,
            Func<string, string> hotKeyActionMapper = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (UseCustomPopup())
            {
                return ShowCustomWindowAndWaitAsync(
                    FormatQuestionMessage(title, question),
                    CreateQuestionButtons(answerButtons),
                    hotKeyObservable,
                    hotKeyActionMapper,
                    cancellationToken);
            }

            ToastContentBuilder builder = new ToastContentBuilder()
                .SetToastDuration(ToastDuration.Long);

            if (!string.IsNullOrWhiteSpace(title))
                builder.AddText(title);
            if (!string.IsNullOrWhiteSpace(question))
                builder.AddText(question);

            if (answerButtons != null)
            {
                for (int index = 0; index < answerButtons.Count; index++)
                {
                    builder.AddButton(new ToastButton()
                        .SetContent(answerButtons[index])
                        .AddArgument("action", index.ToString())
                        .SetBackgroundActivation());
                }
            }

            builder.AddButton(new ToastButton()
                    .SetContent("上一个")
                    .AddArgument("action", NotificationAction.Previous)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("下一个")
                    .AddArgument("action", NotificationAction.Next)
                    .SetBackgroundActivation());

            return ShowAndWaitForDismissibleActionAsync(
                builder,
                hotKeyObservable,
                hotKeyActionMapper,
                cancellationToken);
        }

        public Task<string> WaitForActionAsync(
            IObservable<string> hotKeyObservable = null,
            Func<string, string> hotKeyActionMapper = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(NotificationAction.Cancel);

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            IDisposable hotKeySubscription = null;
            CancellationTokenRegistration cancellationRegistration = default(CancellationTokenRegistration);

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
            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(
                    () => completion.TrySetResult(NotificationAction.Cancel));
            }

            return completion.Task.ContinueWith(task =>
            {
                ToastNotificationManagerCompat.OnActivated -= toastHandler;
                hotKeySubscription?.Dispose();
                cancellationRegistration.Dispose();
                return task.Result;
            });
        }

        private Task<string> ShowAndWaitForDismissibleActionAsync(
            ToastContentBuilder builder,
            IObservable<string> hotKeyObservable,
            Func<string, string> hotKeyActionMapper,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(NotificationAction.Cancel);

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            IDisposable hotKeySubscription = null;
            CancellationTokenRegistration cancellationRegistration = default(CancellationTokenRegistration);

            OnActivated toastHandler = toastArgs =>
            {
                string action = GetActionArgument(toastArgs);
                completion.TrySetResult(string.IsNullOrEmpty(action) ? NotificationAction.Next : action);
            };

            ToastNotification notification = new ToastNotification(builder.GetXml());
            notification.Dismissed += (sender, args) =>
            {
                completion.TrySetResult(NotificationAction.Cancel);
            };
            notification.Failed += (sender, args) =>
            {
                completion.TrySetResult(NotificationAction.Cancel);
            };

            ToastNotificationManagerCompat.OnActivated += toastHandler;
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

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(
                    () => completion.TrySetResult(NotificationAction.Cancel));
            }

            try
            {
                ToastNotificationManagerCompat.History.Clear();
                ToastNotificationManagerCompat.CreateToastNotifier().Show(notification);
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteNotificationLog(ex);
                completion.TrySetResult(NotificationAction.Cancel);
            }
            catch (Exception ex)
            {
                WriteNotificationLog(ex);
                completion.TrySetResult(NotificationAction.Cancel);
            }

            return completion.Task.ContinueWith(task =>
            {
                ToastNotificationManagerCompat.OnActivated -= toastHandler;
                hotKeySubscription?.Dispose();
                cancellationRegistration.Dispose();
                return task.Result;
            });
        }

        private Task<string> ShowCustomWindowAndWaitAsync(
            string message,
            IList<Tuple<string, string>> buttons,
            IObservable<string> hotKeyObservable,
            Func<string, string> hotKeyActionMapper,
            CancellationToken cancellationToken,
            string highlightText = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(NotificationAction.Cancel);

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            IDisposable hotKeySubscription = null;
            CancellationTokenRegistration cancellationRegistration = default(CancellationTokenRegistration);
            CustomToastWindow window = null;

            Action<string> complete = action =>
            {
                string result = string.IsNullOrEmpty(action) ? NotificationAction.Next : action;
                if (!completion.TrySetResult(result))
                    return;

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (window != null && window.IsVisible)
                            window.Close();
                    }));
                }
            };

            if (hotKeyObservable != null)
            {
                hotKeySubscription = hotKeyObservable.Subscribe(events =>
                {
                    string action = hotKeyActionMapper == null ? events : hotKeyActionMapper(events);
                    if (!string.IsNullOrEmpty(action))
                        complete(action);
                });
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(
                    () => complete(NotificationAction.Cancel));
            }

            if (Application.Current == null)
            {
                complete(NotificationAction.Cancel);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (completion.Task.IsCompleted)
                        return;

                    window = new CustomToastWindow(message, buttons, highlightText);
                    window.ActionSelected += action => complete(action);
                    window.Show();
                }));
            }

            return completion.Task.ContinueWith(task =>
            {
                hotKeySubscription?.Dispose();
                cancellationRegistration.Dispose();
                return task.Result;
            });
        }

        private void ApplyExtendedToastDuration(ToastContentBuilder builder)
        {
            builder.SetToastDuration(ToastDuration.Long);
        }

        public Task<NotificationInputResult> WaitForInputAsync(
            string inputKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(new NotificationInputResult(NotificationAction.Cancel, string.Empty));

            TaskCompletionSource<NotificationInputResult> completion = new TaskCompletionSource<NotificationInputResult>();
            CancellationTokenRegistration cancellationRegistration = default(CancellationTokenRegistration);

            OnActivated toastHandler = toastArgs =>
            {
                string action = GetActionArgument(toastArgs);
                string inputValue = GetUserInputValue(toastArgs, inputKey);
                completion.TrySetResult(new NotificationInputResult(action, inputValue));
            };

            ToastNotificationManagerCompat.OnActivated += toastHandler;
            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(
                    () => completion.TrySetResult(new NotificationInputResult(NotificationAction.Cancel, string.Empty)));
            }

            return completion.Task.ContinueWith(task =>
            {
                ToastNotificationManagerCompat.OnActivated -= toastHandler;
                cancellationRegistration.Dispose();
                return task.Result;
            });
        }

        private bool UseCustomPopup()
        {
            return Select.NOTIFICATION_MODE == 1;
        }

        private IList<Tuple<string, string>> CreateStudyButtons()
        {
            return new List<Tuple<string, string>>
            {
                Tuple.Create("上一个", NotificationAction.Previous),
                Tuple.Create("下一个", NotificationAction.Next),
                Tuple.Create("详情", NotificationAction.Details),
                Tuple.Create("笔记", NotificationAction.AddNote),
                Tuple.Create("暂停", NotificationAction.Cancel)
            };
        }

        private IList<Tuple<string, string>> CreateQuestionButtons(IList<string> answerButtons)
        {
            List<Tuple<string, string>> buttons = new List<Tuple<string, string>>();
            if (answerButtons != null)
            {
                for (int index = 0; index < answerButtons.Count; index++)
                {
                    buttons.Add(Tuple.Create(answerButtons[index], index.ToString()));
                }
            }

            buttons.Add(Tuple.Create("上一个", NotificationAction.Previous));
            buttons.Add(Tuple.Create("下一个", NotificationAction.Next));
            return buttons;
        }

        private string FormatQuestionMessage(string title, string question)
        {
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(title))
                lines.Add(title.Trim());
            if (!string.IsNullOrWhiteSpace(question))
                lines.Add(question.Trim());
            return string.Join(Environment.NewLine, lines);
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

        private void WriteNotificationLog(Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(
                    Path.Combine(logDirectory, "notification.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    exception + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // Notification logging must never become another startup failure.
            }
        }
    }
}
