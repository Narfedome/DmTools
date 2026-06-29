using DmToolsApp.Resources.Icons;
using System;
using System.Globalization;

namespace DmToolsApp.Converters
{
    internal class BoolToPlayPauseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
                return isPlaying ? SolidFont.CirclePause : SolidFont.CirclePlay;

            return RegularFont.CirclePlay;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
                return str.Equals(SolidFont.CirclePause, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    internal class BoolToTooltipPlayPauseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var loc = DmToolsApp.Services.LocalizationService.Instance;
            if (value is bool isPlaying)
                return isPlaying ? loc.ChannelPause : loc.ChannelPlay;

            return loc.ChannelPlay;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var loc = DmToolsApp.Services.LocalizationService.Instance;
            if (value is string str)
                return str.Equals(loc.ChannelPause, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    internal class BoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? 1.0 : 0.35;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    internal class BoolToActiveStrokeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b
                ? new SolidColorBrush(DmToolsApp.Services.ThemeService.Instance.CurrentAccentSecondary)
                : new SolidColorBrush(Colors.Transparent);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
