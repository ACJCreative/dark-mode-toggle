using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace dark_mode_toggle.Services
{
    internal sealed class ThemeService
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightTheme = "AppsUseLightTheme";
        private const string SystemUsesLightTheme = "SystemUsesLightTheme";

        public bool IsDarkModeEnabled => ReadValue(AppsUseLightTheme) == 0;

        public void ToggleTheme()
        {
            SetTheme(!IsDarkModeEnabled);
        }

        public void SetTheme(bool isDark)
        {
            var newValue = isDark ? 0 : 1;
            ApplyTheme(newValue);
        }

        private static void ApplyTheme(int value)
        {
            using var personalizeKey = Registry.CurrentUser.CreateSubKey(PersonalizeKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            personalizeKey?.SetValue(AppsUseLightTheme, value, RegistryValueKind.DWord);
            personalizeKey?.SetValue(SystemUsesLightTheme, value, RegistryValueKind.DWord);

            NativeMethods.BroadcastSettingChange("ImmersiveColorSet");
        }

        private static int ReadValue(string valueName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            var storedValue = key?.GetValue(valueName, 1);
            return storedValue is int value ? value : 1;
        }

        private static class NativeMethods
        {
            public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
            public const int WM_SETTINGCHANGE = 0x001A;
            public const int SMTO_ABORTIFHUNG = 0x0002;

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam,
                string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

            public static void BroadcastSettingChange(string settingName)
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, settingName,
                    SMTO_ABORTIFHUNG, 100, out _);
            }
        }
    }
}



