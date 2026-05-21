using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace smartest_desktop.Helpers
{
    /// <summary>Visible si la valeur entière (niveau) égale le ConverterParameter (ex. 1, 2, 3).</summary>
    public sealed class IntLevelToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int level = ParseInt(value);
            int target = ParseInt(parameter);
            return level == target ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static int ParseInt(object? raw)
        {
            if (raw is int i) return i;
            if (raw is long l) return (int)l;
            if (raw != null && int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return n;
            return 0;
        }
    }
}
