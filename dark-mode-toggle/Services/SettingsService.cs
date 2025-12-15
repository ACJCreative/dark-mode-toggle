using System;
using Windows.Storage;

namespace dark_mode_toggle.Services
{
    internal sealed class SettingsService
    {
        private const string ScheduleEnabledKey = "ScheduleEnabled";
        private const string LightModeStartHourKey = "LightModeStartHour";
        private const string LightModeStartMinuteKey = "LightModeStartMinute";
        private const string LightModeEndHourKey = "LightModeEndHour";
        private const string LightModeEndMinuteKey = "LightModeEndMinute";
        private const string LastManualToggleTimeKey = "LastManualToggleTime";
        private const string SkipNextTransitionKey = "SkipNextTransition";

        private static readonly TimeSpan DefaultLightModeStart = TimeSpan.FromHours(9);
        private static readonly TimeSpan DefaultLightModeEnd = TimeSpan.FromHours(17);

        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        public bool IsScheduleEnabled { get; private set; }
        public TimeSpan LightModeStart { get; private set; }
        public TimeSpan LightModeEnd { get; private set; }
        public DateTime? LastManualToggleTime { get; private set; }
        public bool SkipNextTransition { get; private set; }

        public SettingsService()
        {
            IsScheduleEnabled = ReadBool(ScheduleEnabledKey, false);
            LightModeStart = ReadTime(LightModeStartHourKey, LightModeStartMinuteKey, DefaultLightModeStart);
            LightModeEnd = ReadTime(LightModeEndHourKey, LightModeEndMinuteKey, DefaultLightModeEnd);
            var lastToggleTicks = ReadLong(LastManualToggleTimeKey, null);
            LastManualToggleTime = lastToggleTicks.HasValue ? new DateTime(lastToggleTicks.Value, DateTimeKind.Utc) : null;
            SkipNextTransition = ReadBool(SkipNextTransitionKey, false);
        }

        public void SetScheduleEnabled(bool isEnabled)
        {
            IsScheduleEnabled = isEnabled;
            WriteValue(ScheduleEnabledKey, isEnabled);
        }

        public void SetLightModeStart(TimeSpan time)
        {
            LightModeStart = NormalizeTime(time);
            WriteValue(LightModeStartHourKey, LightModeStart.Hours);
            WriteValue(LightModeStartMinuteKey, LightModeStart.Minutes);
        }

        public void SetLightModeEnd(TimeSpan time)
        {
            LightModeEnd = NormalizeTime(time);
            WriteValue(LightModeEndHourKey, LightModeEnd.Hours);
            WriteValue(LightModeEndMinuteKey, LightModeEnd.Minutes);
        }

        public void RecordManualOverride(DateTime utcTime)
        {
            LastManualToggleTime = utcTime.ToUniversalTime();
            WriteValue(LastManualToggleTimeKey, LastManualToggleTime.Value.Ticks);
            SkipNextTransition = true;
            WriteValue(SkipNextTransitionKey, true);
        }

        public void ClearSkipNextTransition()
        {
            SkipNextTransition = false;
            WriteValue(SkipNextTransitionKey, false);
        }

        private TimeSpan ReadTime(string hourKey, string minuteKey, TimeSpan defaultValue)
        {
            var hours = ReadInt(hourKey, defaultValue.Hours);
            var minutes = ReadInt(minuteKey, defaultValue.Minutes);
            return NormalizeTime(new TimeSpan(hours, minutes, 0));
        }

        private int ReadInt(string key, int defaultValue)
        {
            if (_localSettings.Values.TryGetValue(key, out var storedValue) && storedValue is int intValue)
            {
                return intValue;
            }

            return defaultValue;
        }

        private bool ReadBool(string key, bool defaultValue)
        {
            if (_localSettings.Values.TryGetValue(key, out var storedValue) && storedValue is bool boolValue)
            {
                return boolValue;
            }

            return defaultValue;
        }

        private long? ReadLong(string key, long? defaultValue)
        {
            if (_localSettings.Values.TryGetValue(key, out var storedValue) && storedValue is long longValue)
            {
                return longValue;
            }

            return defaultValue;
        }

        private void WriteValue(string key, object value)
        {
            _localSettings.Values[key] = value;
        }

        private static TimeSpan NormalizeTime(TimeSpan time)
        {
            var hours = (time.Hours % 24 + 24) % 24;
            var minutes = (time.Minutes % 60 + 60) % 60;
            return new TimeSpan(hours, minutes, 0);
        }
    }
}
