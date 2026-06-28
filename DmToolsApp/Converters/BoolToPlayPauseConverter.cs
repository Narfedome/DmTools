using DmToolsApp.Resources.Icons;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DmToolsApp.Converters
{
    internal class BoolToPlayPauseConverter : IValueConverter
    {
        // Convert bool → string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
                return isPlaying ? SolidFont.CirclePause : SolidFont.CirclePlay;

            return RegularFont.CirclePlay;
        }

        // Convert back si jamais nécessaire (pas obligatoire ici)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
                return str.Equals(SolidFont.CirclePause, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }
    internal class BoolToTooltipPlayPauseConverter : IValueConverter
    {
        // Convert bool → string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
                return isPlaying ? "Pause" : "Play";

            return "Play";
        }

        // Convert back si jamais nécessaire (pas obligatoire ici)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
                return str.Equals("Pause", StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    internal class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? 1.0 : 0.35;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    internal class BoolToActiveStrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b
                ? new SolidColorBrush(Color.FromArgb("#D600AA"))
                : new SolidColorBrush(Colors.Transparent);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

