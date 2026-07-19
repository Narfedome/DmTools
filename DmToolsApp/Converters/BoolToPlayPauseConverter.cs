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
                return isPlaying ? loc["ChannelPause"] : loc["ChannelPlay"];

            return loc["ChannelPlay"];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var loc = DmToolsApp.Services.LocalizationService.Instance;
            if (value is string str)
                return str.Equals(loc["ChannelPause"], StringComparison.OrdinalIgnoreCase);

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

    // Met en évidence (couleur accent) une icône ciblée par une bulle d'aide du tutoriel, ex. les
    // chevrons de navigation Campagne -> Chapitre -> Scène : le texte seul de la bulle ne suffit
    // pas à indiquer QUEL élément taper parmi plusieurs lignes de la liste.
    internal class BoolToTutorialHighlightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b
                ? DmToolsApp.Services.ThemeService.Instance.CurrentAccent
                : DmToolsApp.Services.ThemeService.Instance.CurrentTextMuted;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Espace de la bulle de tutoriel : toujours réservé en layout (voir TutorialCoachMark), seule
    // l'opacité bascule 0/1 pour ne jamais décaler l'élément ciblé en dessous.
    internal class BoolToShowOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? 1.0 : 0.0;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Atténue un élément voisin du bouton ciblé par le tutoriel (ex: crayon/poubelle à côté du +),
    // pour renforcer visuellement le focus sur la cible sans avoir besoin d'un vrai voile plein écran.
    internal class BoolToDimOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? 0.3 : 1.0;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
