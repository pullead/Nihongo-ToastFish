using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ToastFish.Model.StartWithWindows
{
    public class KeyboardHookHotKey : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;
        private const int VkAlt = 0x12;
        private const int VkControl = 0x11;
        private const int VkShift = 0x10;
        private const int VkLWin = 0x5B;
        private const int VkRWin = 0x5C;

        private readonly LowLevelKeyboardProc hookProc;
        private readonly Action<HotKey> action;
        private readonly HashSet<string> excludedProcessNames;
        private IntPtr hookId = IntPtr.Zero;
        private bool disposed;

        public KeyboardHookHotKey(Action<HotKey> action, IEnumerable<string> excludedProcessNames)
        {
            this.action = action;
            this.excludedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (excludedProcessNames != null)
            {
                foreach (string processName in excludedProcessNames)
                {
                    if (!string.IsNullOrWhiteSpace(processName))
                        this.excludedProcessNames.Add(processName.Trim());
                }
            }

            hookProc = HookCallback;
            hookId = SetHook(hookProc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(currentModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode < 0)
                    return CallNextHookEx(hookId, nCode, wParam, lParam);

                int message = wParam.ToInt32();
                if (message != WmKeyDown && message != WmSysKeyDown)
                    return CallNextHookEx(hookId, nCode, wParam, lParam);

                KeyboardHookStruct hookData = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                Key key = KeyInterop.KeyFromVirtualKey(hookData.vkCode);
                KeyModifier modifiers = GetCurrentModifiers();
                if (!IsSupportedHotKey(key, modifiers))
                    return CallNextHookEx(hookId, nCode, wParam, lParam);

                if (IsForegroundProcessExcluded())
                    return CallNextHookEx(hookId, nCode, wParam, lParam);

                RaiseHotKeyAsync(key, modifiers);
                return new IntPtr(1);
            }
            catch (Exception ex)
            {
                WriteHookLog(ex);
                return CallNextHookEx(hookId, nCode, wParam, lParam);
            }
        }

        private void RaiseHotKeyAsync(Key key, KeyModifier modifiers)
        {
            Application application = Application.Current;
            if (application == null || application.Dispatcher == null)
                return;

            application.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        action?.Invoke(new HotKey(key, modifiers, null, false));
                    }
                    catch (Exception ex)
                    {
                        WriteHookLog(ex);
                    }
                }),
                DispatcherPriority.Background);
        }

        private bool IsSupportedHotKey(Key key, KeyModifier modifiers)
        {
            if (modifiers == KeyModifier.Alt)
            {
                return key == Key.A ||
                       key == Key.D ||
                       key == Key.W ||
                       key == Key.S ||
                       key == Key.E ||
                       key == Key.Q ||
                       key == Key.D1 ||
                       key == Key.D2 ||
                       key == Key.D3 ||
                       key == Key.D4 ||
                       key == Key.Oem3;
            }

            if (modifiers == (KeyModifier.Ctrl | KeyModifier.Alt))
            {
                return key == Key.J ||
                       key == Key.V ||
                       key == Key.G ||
                       key == Key.E ||
                       key == Key.P ||
                       key == Key.O;
            }

            return false;
        }

        private KeyModifier GetCurrentModifiers()
        {
            KeyModifier modifiers = KeyModifier.None;
            if (IsKeyDown(VkAlt))
                modifiers |= KeyModifier.Alt;
            if (IsKeyDown(VkControl))
                modifiers |= KeyModifier.Ctrl;
            if (IsKeyDown(VkShift))
                modifiers |= KeyModifier.Shift;
            if (IsKeyDown(VkLWin) || IsKeyDown(VkRWin))
                modifiers |= KeyModifier.Win;
            return modifiers;
        }

        private bool IsKeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private bool IsForegroundProcessExcluded()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return false;

            int processId;
            GetWindowThreadProcessId(foregroundWindow, out processId);
            if (processId <= 0)
                return false;

            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    return excludedProcessNames.Contains(process.ProcessName);
                }
            }
            catch
            {
                return false;
            }
        }

        private void WriteHookLog(Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(
                    Path.Combine(logDirectory, "keyboard-hook.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    exception + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // Keyboard hook failures must never crash the app.
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
