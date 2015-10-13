namespace Sentinel.Support.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    using Common.Logging;

    using NodaTime;

    using Interfaces;

    public class TimePreferenceConverter : IValueConverter
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private IUserPreferences Preferences { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Preferences = parameter as IUserPreferences;

            if (Preferences == null)
            {
                Log.Error("Parameter must be an IUserPreferences");
                throw new ArgumentException("Parameter must be an instance of IUserPreferences", nameof(parameter));
            }

            var message = value as ILogEntry;

            if (message == null)
            {
                Log.Warn("Not supplied an ILogEntry as the value parameter");
                return string.Empty;
            }

            object displayDateTime = null;
            if (Preferences.UseArrivalDateTime)
            {
                message.MetaData.TryGetValue("ReceivedTime", out displayDateTime);
            }

            // Fallback if message does not contain meta-data.
            var dt = (DateTime)(displayDateTime ?? message.DateTime);
            var isUtc = dt.Kind == DateTimeKind.Utc;
            if (isUtc && Preferences.ConvertUtcTimesToLocalTimezone)
            {
                var defaultTimeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
                var global = new ZonedDateTime(Instant.FromDateTimeUtc(dt), defaultTimeZone);
                return
                    global.ToString(
                        GetDateDisplayFormat(Preferences.SelectedTimeFormatOption, Preferences.TimeFormatOptions, true),
                        CultureInfo.CurrentCulture);
            }

            var local = LocalDateTime.FromDateTime(dt);
            var time =
                local.ToString(
                    GetDateDisplayFormat(Preferences.SelectedTimeFormatOption, Preferences.TimeFormatOptions, false),
                    CultureInfo.CurrentCulture);

            return isUtc ? time + " [UTC]" : time;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string GetDateDisplayFormat(int setting, IEnumerable<string> settings, bool convertToLocalIfUtc)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var dateFormatSource = settings.ElementAt(setting);
            var dateFormat = dateFormatSource.Replace("-", "'-'").Replace(":", "':'");
            
            // Need to quote special characters, this will only happen when changing formats, so don't need to be too clever.
            return convertToLocalIfUtc ? dateFormat + " x" : dateFormat;
        }
    }
}