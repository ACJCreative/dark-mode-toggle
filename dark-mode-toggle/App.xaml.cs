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
        private readonly Services.SettingsService _settingsService = new();
        private Services.SchedulerService? _schedulerService;
        private TrayIcon? _trayIcon;
        private SettingsWindow? _settingsWindow;
        private bool _isExiting;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (_trayIcon is null)
            {
                _schedulerService = new Services.SchedulerService(_settingsService, _themeService);
                _trayIcon = new TrayIcon(ToggleThemeAndRecordOverride, ShowSettingsWindow, RequestExit);
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
            DisposeSchedulerService();
            Current.Exit();
        }

        private void DisposeTrayIcon()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }

        private void DisposeSchedulerService()
        {
            _schedulerService?.Dispose();
            _schedulerService = null;
        }

        private void ToggleThemeAndRecordOverride()
        {
            _themeService.ToggleTheme();
            _schedulerService?.NotifyManualToggle();
        }

        private void ShowSettingsWindow()
        {
            if (_schedulerService is null)
            {
                return;
            }

            if (_settingsWindow is not null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(_settingsService, _schedulerService);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Activate();
        }
    }
}
