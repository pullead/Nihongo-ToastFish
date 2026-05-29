using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;
using ToastFish.ViewModel;

namespace ToastFish
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.Exception);
            if (IsNotificationAccessDenied(e.Exception))
            {
                e.Handled = true;
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.ExceptionObject as Exception);
        }

        private static void WriteCrashLog(Exception exception)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = Path.Combine(baseDirectory, "Log");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, "crash.log");
                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    (exception == null ? "Unknown unhandled exception." : exception.ToString()) +
                    Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // Do not throw while handling a crash.
            }
        }

        private static bool IsNotificationAccessDenied(Exception exception)
        {
            Exception current = exception;
            while (current != null)
            {
                if (current is UnauthorizedAccessException)
                {
                    string details = current.ToString();
                    if (details.IndexOf("Windows.UI.Notifications", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        details.IndexOf("Microsoft.Toolkit.Uwp.Notifications", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}
