using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;

namespace dark_mode_toggle.Services
{
    internal sealed class TrayIcon : IDisposable
    {
        private const int MenuToggleId = 1;
        private const int MenuExitId = 2;
        private const uint WmTrayIcon = NativeMethods.WmApp + 1;
        private const uint WmRButtonUp = 0x0205;
        private const uint WmLButtonUp = 0x0202;

        private readonly NativeMethods.WindowProc _windowProc;
        private readonly IntPtr _menuHandle;
        private NotifyIconData _notifyIconData;
        private readonly MessageWindow _messageWindow;
        private readonly Action _toggleAction;
        private readonly Action _exitAction;
        private bool _disposed;

        public TrayIcon(Action toggleAction, Action exitAction)
        {
            _toggleAction = toggleAction ?? throw new ArgumentNullException(nameof(toggleAction));
            _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));

            _menuHandle = NativeMethods.CreatePopupMenu();
            NativeMethods.AppendMenu(_menuHandle, NativeMethods.MfString, MenuToggleId, "Toggle Dark/Light");
            NativeMethods.AppendMenu(_menuHandle, NativeMethods.MfSeparator, 0, string.Empty);
            NativeMethods.AppendMenu(_menuHandle, NativeMethods.MfString, MenuExitId, "Exit");

            _windowProc = WndProc;
            _messageWindow = MessageWindow.Create(_windowProc);

            var iconPath = Path.Combine(Package.Current.InstalledLocation.Path, "Assets", "TrayIcon.ico");
            
            _notifyIconData = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = _messageWindow.Handle,
                uID = 1,
                uFlags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip,
                uCallbackMessage = WmTrayIcon,
                hIcon = NativeMethods.LoadImage(IntPtr.Zero, iconPath, NativeMethods.IMAGE_ICON, 0, 0, NativeMethods.LR_LOADFROMFILE),
                szTip = "Dark Mode Toggle"
            };

            NativeMethods.Shell_NotifyIcon(NativeMethods.NimAdd, ref _notifyIconData);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WmTrayIcon && ((uint)lParam == WmRButtonUp || (uint)lParam == WmLButtonUp))
            {
                ShowContextMenu();
                return IntPtr.Zero;
            }

            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            var cursor = NativeMethods.GetCursorPosition();
            NativeMethods.SetForegroundWindow(_messageWindow.Handle);
            var command = NativeMethods.TrackPopupMenuEx(
                _menuHandle,
                NativeMethods.TpmReturndCmd | NativeMethods.TpmRightButton,
                cursor.X,
                cursor.Y,
                _messageWindow.Handle,
                IntPtr.Zero);

            NativeMethods.PostMessage(_messageWindow.Handle, NativeMethods.WmNull, IntPtr.Zero, IntPtr.Zero);

            if (command == MenuToggleId)
            {
                _toggleAction();
            }
            else if (command == MenuExitId)
            {
                _exitAction();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            NativeMethods.Shell_NotifyIcon(NativeMethods.NimDelete, ref _notifyIconData);
            if (_menuHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyMenu(_menuHandle);
            }

            _messageWindow.Dispose();
            if (_notifyIconData.hIcon != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(_notifyIconData.hIcon);
            }
        }

        private sealed class MessageWindow : IDisposable
        {
            private readonly string _className;
            private readonly IntPtr _windowHandle;
            private readonly NativeMethods.WindowProc _windowProc;
            private bool _disposed;

            private MessageWindow(string className, IntPtr windowHandle, NativeMethods.WindowProc windowProc)
            {
                _className = className;
                _windowHandle = windowHandle;
                _windowProc = windowProc;
            }

            public IntPtr Handle => _windowHandle;

            public static MessageWindow Create(NativeMethods.WindowProc windowProc)
            {
                var className = $"TrayMessageWindow_{Guid.NewGuid():N}";
                var wndClass = new NativeMethods.WndClassEx
                {
                    cbSize = (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),
                    style = 0,
                    lpfnWndProc = windowProc,
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = NativeMethods.GetModuleHandle(IntPtr.Zero),
                    hIcon = IntPtr.Zero,
                    hCursor = IntPtr.Zero,
                    hbrBackground = IntPtr.Zero,
                    lpszMenuName = null,
                    lpszClassName = className,
                    hIconSm = IntPtr.Zero
                };

                if (NativeMethods.RegisterClassEx(ref wndClass) == 0)
                {
                    throw new InvalidOperationException("Unable to register tray message window class.");
                }

                var hwnd = NativeMethods.CreateWindowEx(
                    0,
                    className,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    0,
                    NativeMethods.HwndMessage,
                    IntPtr.Zero,
                    wndClass.hInstance,
                    IntPtr.Zero);

                if (hwnd == IntPtr.Zero)
                {
                    NativeMethods.UnregisterClass(className, NativeMethods.GetModuleHandle(IntPtr.Zero));
                    throw new InvalidOperationException("Unable to create tray message window.");
                }

                return new MessageWindow(className, hwnd, windowProc);
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                NativeMethods.DestroyWindow(_windowHandle);
                NativeMethods.UnregisterClass(_className, NativeMethods.GetModuleHandle(IntPtr.Zero));
                _disposed = true;
            }
        }

        private struct NotifyIconData
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        private static class NativeMethods
        {
            public const uint WmApp = 0x8000;
            public const uint WmNull = 0x0000;
            public static readonly IntPtr HwndMessage = new IntPtr(-3);
            public const int WmCommand = 0x0111;
            public const int MfString = 0x0000;
            public const int MfSeparator = 0x0800;
            public const uint NimAdd = 0x00000000;
            public const uint NimModify = 0x00000001;
            public const uint NimDelete = 0x00000002;
            public const uint NifMessage = 0x00000001;
            public const uint NifIcon = 0x00000002;
            public const uint NifTip = 0x00000004;
            public const uint IMAGE_ICON = 1;
            public const uint LR_LOADFROMFILE = 0x00000010;
            public const uint TpmReturndCmd = 0x0100;
            public const uint TpmRightButton = 0x0002;
            public static readonly IntPtr IdiApplication = new IntPtr(0x7f00);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr CreateWindowEx(
                uint dwExStyle,
                string lpClassName,
                string lpWindowName,
                uint dwStyle,
                int x,
                int y,
                int nWidth,
                int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool DestroyWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr CreatePopupMenu();

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool DestroyMenu(IntPtr hMenu);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint TrackPopupMenuEx(
                IntPtr hMenu,
                uint uFlags,
                int x,
                int y,
                IntPtr hWnd,
                IntPtr lpTPMParams);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetCursorPos(out Point lpPoint);

            [DllImport("shell32.dll", SetLastError = true)]
            public static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

            public static Point GetCursorPosition()
            {
                return GetCursorPos(out var point) ? point : default;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct Point
            {
                public int X;
                public int Y;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct WndClassEx
            {
                public uint cbSize;
                public uint style;
                public WindowProc lpfnWndProc;
                public int cbClsExtra;
                public int cbWndExtra;
                public IntPtr hInstance;
                public IntPtr hIcon;
                public IntPtr hCursor;
                public IntPtr hbrBackground;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string? lpszMenuName;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string lpszClassName;
                public IntPtr hIconSm;
            }

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        }
    }
}

