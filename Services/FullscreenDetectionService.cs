using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DynamicIslandMedia.Services
{
    public class FullscreenDetectionService : IDisposable
    {
        private readonly DispatcherTimer _pollTimer;
        private readonly uint _currentProcessId;
        private bool _isFullscreen;
        private bool _disposed;

        public event EventHandler<bool>? FullscreenChanged;

        public bool IsFullscreen => _isFullscreen;

        public FullscreenDetectionService()
        {
            _currentProcessId = (uint)Environment.ProcessId;

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            _pollTimer.Tick += OnPollTick;
        }

        public void Start()
        {
            if (!_pollTimer.IsEnabled)
            {
                _pollTimer.Start();
                CheckFullscreen();
            }
        }

        public void Stop()
        {
            if (_pollTimer.IsEnabled)
            {
                _pollTimer.Stop();
            }
        }

        private void OnPollTick(object? sender, EventArgs e)
        {
            CheckFullscreen();
        }

        public void CheckFullscreen()
        {
            try
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero)
                    return;

                // Ignore windows owned by this process
                GetWindowThreadProcessId(foreground, out uint pid);
                if (pid == _currentProcessId)
                    return;

                // Ignore desktop / shell windows
                IntPtr shell = GetShellWindow();
                IntPtr desktop = GetDesktopWindow();
                if (foreground == shell || foreground == desktop)
                {
                    UpdateFullscreenState(false);
                    return;
                }

                // Check class name
                var sb = new StringBuilder(256);
                GetClassName(foreground, sb, sb.Capacity);
                string className = sb.ToString();

                if (string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(className, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(className, "Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateFullscreenState(false);
                    return;
                }

                // Check window visibility and minimized state
                if (!IsWindowVisible(foreground) || IsIconic(foreground))
                {
                    UpdateFullscreenState(false);
                    return;
                }

                // Get window bounds
                if (!GetWindowRect(foreground, out RECT rect))
                {
                    UpdateFullscreenState(false);
                    return;
                }

                // Compare against primary screen bounds
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen == null)
                {
                    UpdateFullscreenState(false);
                    return;
                }

                Rectangle bounds = primaryScreen.Bounds;

                // Tolerant check (±2px) to handle borderless window and multi-monitor edge alignments
                bool isCoveringPrimary = rect.Left <= bounds.Left + 2 &&
                                         rect.Top <= bounds.Top + 2 &&
                                         rect.Right >= bounds.Right - 2 &&
                                         rect.Bottom >= bounds.Bottom - 2;

                UpdateFullscreenState(isCoveringPrimary);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FullscreenDetectionService] Check failed: {ex.Message}");
            }
        }

        private void UpdateFullscreenState(bool isFullscreen)
        {
            if (_isFullscreen != isFullscreen)
            {
                _isFullscreen = isFullscreen;
                FullscreenChanged?.Invoke(this, _isFullscreen);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pollTimer.Stop();
        }

        #region Win32 P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion
    }
}
