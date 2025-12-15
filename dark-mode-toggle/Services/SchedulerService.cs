using System;
using Microsoft.UI.Dispatching;

namespace dark_mode_toggle.Services
{
    internal sealed class SchedulerService : IDisposable
    {
        private readonly SettingsService _settingsService;
        private readonly ThemeService _themeService;
        private readonly DispatcherQueueTimer _timer;
        private bool _disposed;
        private bool _hasPreviousTarget;
        private bool _previousShouldBeLight;

        public SchedulerService(SettingsService settingsService, ThemeService themeService)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            var dispatcher = DispatcherQueue.GetForCurrentThread();
            if (dispatcher is null)
            {
                throw new InvalidOperationException("Unable to obtain dispatcher queue for scheduler.");
            }

            _timer = dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += OnTimerTick;
            _timer.Start();
            Evaluate(force: true);
        }

        public void NotifyManualToggle()
        {
            _settingsService.RecordManualOverride(DateTime.UtcNow);
        }

        public void Refresh()
        {
            Evaluate(force: true);
        }

        private void OnTimerTick(object? sender, object? args)
        {
            Evaluate();
        }

        private void Evaluate(bool force = false)
        {
            if (_disposed)
            {
                return;
            }

            if (!_settingsService.IsScheduleEnabled)
            {
                _hasPreviousTarget = false;
                return;
            }

            var shouldBeLight = IsWithinLightModeWindow();
            if (!_hasPreviousTarget || force)
            {
                _hasPreviousTarget = true;
                _previousShouldBeLight = shouldBeLight;

                if (!_settingsService.SkipNextTransition)
                {
                    ApplyTheme(shouldBeLight);
                }

                return;
            }

            if (_settingsService.SkipNextTransition && shouldBeLight != _previousShouldBeLight)
            {
                _settingsService.ClearSkipNextTransition();
                _previousShouldBeLight = shouldBeLight;
                return;
            }

            if (shouldBeLight != _previousShouldBeLight)
            {
                ApplyTheme(shouldBeLight);
                _previousShouldBeLight = shouldBeLight;
            }
        }

        private bool IsWithinLightModeWindow()
        {
            var now = DateTime.Now.TimeOfDay;
            var start = _settingsService.LightModeStart;
            var end = _settingsService.LightModeEnd;

            if (start <= end)
            {
                return now >= start && now < end;
            }

            return now >= start || now < end;
        }

        private void ApplyTheme(bool shouldBeLight)
        {
            _themeService.SetTheme(!shouldBeLight);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _disposed = true;
        }
    }
}

