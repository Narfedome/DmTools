using DmToolsApp.Services;
using System;
using System.Globalization;

namespace DmToolsApp.Converters
{
    // Convertit Volume (0-1) en hauteur de ligne "star" pour un remplissage bas-vers-haut (cf.
    // MixerChannelCardView) : deux lignes de Grid, l'une "vide" (ConverterParameter="Invert",
    // 1-Volume) au-dessus de l'autre "remplie" (Volume) en dessous - le rapport des deux stars
    // donne visuellement une jauge, sans jamais recalculer une hauteur en pixels. Étoile jamais à
    // 0 (Math.Max avec un epsilon) : MAUI ignore une RowDefinition à 0 star au lieu de la
    // collapser proprement, ce qui déséquilibrerait le partage avec l'autre ligne.
    public class VolumeToRowStarConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var volume = value is double d ? Math.Clamp(d, 0, 1) : 0;
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            var star = invert ? 1 - volume : volume;
            return new GridLength(Math.Max(star, 0.0001), GridUnitType.Star);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Bande de dégradé signalant un fade in/out actif (cf. IsFadeIn/IsFadeOut sur
    // ChannelStripViewModel) : ConverterParameter="Bottom" pour la bande de fade out (dégradé
    // partant du bas), sinon dégradé partant du haut (fade in). Couleur alignée sur
    // BoolToActiveStrokeConverter (ThemeService.Instance.CurrentAccentSecondary) pour rester le
    // même signal "accent" que le contour de lecture, plutôt qu'une couleur figée qui ignorerait
    // le thème/la palette actifs.
    public class FadeEdgeBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool active || !active)
                return new SolidColorBrush(Colors.Transparent);

            var color = ThemeService.Instance.CurrentAccentSecondary;
            var fromBottom = string.Equals(parameter as string, "Bottom", StringComparison.OrdinalIgnoreCase);

            return new LinearGradientBrush
            {
                StartPoint = fromBottom ? new Point(0, 1) : new Point(0, 0),
                EndPoint = fromBottom ? new Point(0, 0) : new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = color.WithAlpha(0.55f), Offset = 0f },
                    new GradientStop { Color = Colors.Transparent, Offset = 1f }
                }
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
