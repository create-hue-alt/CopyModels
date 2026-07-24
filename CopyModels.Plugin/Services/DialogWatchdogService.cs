using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    /// Win32/UI Automation вотчдог для системных диалогов, которые не ловит EventService
    /// (не через RevitAPI, а через сторонние окна - например, зависший экспортер NWC)
    /// </summary>
    internal class DialogWatchdogService : IDisposable
    {
        private const string TargetWindowTitle = "Navisworks NWC Exporter";
        private const string OkButtonAutomationId = "2";
        private const int PollIntervalMs = 1000;
        
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logDebug;
        private Timer _timer;

        public DialogWatchdogService(Action<string> logWarning, Action<string> logDebug)
        {
            _logWarning = logWarning ?? (_ => { });
            _logDebug = logDebug ?? (_ => { });
        }

        public void Start()
        { _timer = new Timer(Tick, null, 0, PollIntervalMs); }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose() => Stop();

        private void Tick(object state)
        {
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                if (ReadWindowText(hWnd) == TargetWindowTitle)
                {
                    _logWarning($"Watchdog: found dialog '{TargetWindowTitle}', attempting to click OK");
                    ClickOkButton(hWnd);
                }

                return true;
            }, IntPtr.Zero);
        }

        private void ClickOkButton(IntPtr parentHwnd)
        {
            var dialogElement = AutomationElement.FromHandle(parentHwnd);
            if (dialogElement == null)
            {
                _logWarning("Watchdog: dialog found but AutomationElement.FromHandle returned null");
                return;
            }

            // вспомогательный метод для просмотра наполнения всплывающего окна
            //var children = dialogElement.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            //_logWarning($"Watchdog: dialog has {children.Count} descendant elements");
            //foreach (AutomationElement child  in children)
            //{
            //    _logWarning($"Watchdog: element Name='{child.Current.Name}' " +
            //        $"ControlType='{child.Current.ControlType.ProgrammaticName}' " +
            //        $"AutomationId='{child.Current.AutomationId}'");
            //}

            var buttonElement = dialogElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, OkButtonAutomationId));

            if (buttonElement == null)
            {
                _logWarning("Watchdog: dialog found but OK button not located");
                return;
            }

            if (buttonElement.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
            {
                ((InvokePattern)pattern).Invoke();
                _logWarning("Watchdog: OK button clicked");
            }
            else
            {
                _logWarning("Watchdog: OK button found but has no InvokePattern");
            }
        }

        private static string ReadWindowText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;

            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr iParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
    }
}
