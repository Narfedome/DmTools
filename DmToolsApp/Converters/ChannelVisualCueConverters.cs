using DmToolsApp.Services;
using System;
using System.Globalization;

namespace DmToolsApp.Converters
{
    // Convertit Volume (0-1) en opacité pour le fond "en lecture" d'un channel strip (cf.
    // MixerChannelCardView) : plancher à 0.15 plutôt qu'un mapping direct 0-1, pour qu'un canal à
    // très faible volume reste visible comme "en lecture, discret" plutôt que de se confondre avec
    // un canal désactivé/grisé (IsEnabled) - toujours appliqué à un calque de fond dédié, jamais à
    // la carte entière, donc les boutons/le texte restent lisibles quel que soit le volume.
    public class VolumeToOpacityConverter : IValueConverter
    {
        private const double MinOpacity = 0.15;
        private const double MaxOpacity = 0.75;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var volume = value is double d ? Math.Clamp(d, 0, 1) : 0;
            return MinOpacity + volume * (MaxOpacity - MinOpacity);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Bande de dégradé signalant un fade in/out actif (cf. IsFadeIn/IsFadeOut sur
    // ChannelStripViewModel) : ConverterParameter="Bottom" pour la bande de fade out (dégradé
    // partant du bas), sinon dégradé partant du haut (fade in). Couleur alignée sur
    // BoolToActiveStrokeConverter (ThemeService.Instance.CurrentAccentSecondary) pour rester le
    // même signal "accent" que le fond de lecture, plutôt qu'une couleur figée qui ignorerait le
    // thème/la palette actifs.
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
                    new GradientStop { Color = color.WithAlpha(0.65f), Offset = 0f },
                    new GradientStop { Color = Colors.Transparent, Offset = 1f }
                }
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
