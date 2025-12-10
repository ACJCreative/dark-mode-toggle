using System;
using System.Threading.Tasks;
using dark_mode_toggle.Services;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace dark_mode_toggle
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const string StartupTaskId = "DarkModeToggleStartupTask";
        private readonly ThemeService _themeService = new();
        private TrayIcon? _trayIcon;
        private bool _isExiting;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (_trayIcon is null)
            {
                _trayIcon = new TrayIcon(_themeService.ToggleTheme, RequestExit);
            }

            await EnsureStartupTaskEnabledAsync().ConfigureAwait(false);
        }

        private async Task EnsureStartupTaskEnabledAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                if (startupTask.State == StartupTaskState.Disabled)
                {
                    await startupTask.RequestEnableAsync();
                }
            }
            catch
            {
                // ignore failures to avoid crashing on environment differences
            }
        }

        private void RequestExit()
        {
            if (_isExiting)
            {
                return;
            }

            _isExiting = true;
            DisposeTrayIcon();
            Current.Exit();
        }

        private void DisposeTrayIcon()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
    }
}
