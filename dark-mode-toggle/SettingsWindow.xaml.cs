using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using System.IO;
using Windows.ApplicationModel;
using Windows.Graphics;
using WinRT.Interop;

namespace dark_mode_toggle
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly Services.SettingsService _settingsService;
        private readonly Services.SchedulerService _schedulerService;
        private readonly AppWindow _appWindow;

        internal SettingsWindow(Services.SettingsService settingsService, Services.SchedulerService schedulerService)
        {
            _settingsService = settingsService;
            _schedulerService = schedulerService;
            InitializeComponent();

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Resize(new SizeInt32(360, 360));
            _appWindow.Closing += OnCloseRequested;
            var iconPath = Path.Combine(Package.Current.InstalledLocation.Path, "Assets", "TrayIcon.ico");
            _appWindow.SetIcon(iconPath);

            ScheduleToggleSwitch.IsOn = _settingsService.IsScheduleEnabled;
            StartTimePicker.Time = _settingsService.LightModeStart;
            EndTimePicker.Time = _settingsService.LightModeEnd;
            UpdateTimePickerState();
        }

        private void OnScheduleToggle(object sender, RoutedEventArgs e)
        {
            _settingsService.SetScheduleEnabled(ScheduleToggleSwitch.IsOn);
            UpdateTimePickerState();
            _schedulerService.Refresh();
        }

        private void OnStartTimeChanged(object sender, TimePickerValueChangedEventArgs args)
        {
            _settingsService.SetLightModeStart(StartTimePicker.Time);
            _schedulerService.Refresh();
        }

        private void OnEndTimeChanged(object sender, TimePickerValueChangedEventArgs args)
        {
            _settingsService.SetLightModeEnd(EndTimePicker.Time);
            _schedulerService.Refresh();
        }

        private void UpdateTimePickerState()
        {
            var enabled = ScheduleToggleSwitch.IsOn;
            StartTimePicker.IsEnabled = enabled;
            EndTimePicker.IsEnabled = enabled;
        }

        private void OnCloseRequested(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            sender.Hide();
        }
    }
}

