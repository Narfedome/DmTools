using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace DmToolsApp.Components;

public partial class FaIconButtonView : ContentView
{
    public FaIconButtonView()
    {
        InitializeComponent();
    }
    public static readonly BindableProperty BackgroundImageProperty =
    BindableProperty.Create(nameof(BackgroundImage), typeof(ImageSource), typeof(FaIconButtonView), default(ImageSource));

    public ImageSource BackgroundImage
    {
        get => (ImageSource)GetValue(BackgroundImageProperty);
        set => SetValue(BackgroundImageProperty, value);
    }

    // ICON (glyph)
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(FaIconButtonView), default(string));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // COMMAND
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FaIconButtonView));
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    // TOOLTIP
    public static readonly BindableProperty TooltipProperty =
        BindableProperty.Create(nameof(Tooltip), typeof(string), typeof(FaIconButtonView));
    public string Tooltip
    {
        get => (string)GetValue(TooltipProperty);
        set => SetValue(TooltipProperty, value);
    }


    // COMMAND PARAM
    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(FaIconButtonView));

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    // ICON SIZE
    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(FaIconButtonView),
            // Cf. ChannelVolumeSliderView.xaml.cs pour la raison du calcul direct via DeviceInfo
            // plutot qu'un lookup dans Resources (renverrait l'objet OnIdiom brut, pas resolu).
            defaultValueCreator: _ => DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ? 14.0 : 16.0);

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }
    // ICON COLOR
    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(FaIconButtonView), Colors.White);

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    // ICON FONT
    public static readonly BindableProperty IconFontProperty =
        BindableProperty.Create(nameof(IconFont), typeof(string), typeof(FaIconButtonView), "FontSolid");

    public string IconFont
    {
        get => (string)GetValue(IconFontProperty);
        set => SetValue(IconFontProperty, value);
    }

    // CORNER RADIUS
    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(int), typeof(FaIconButtonView), 8);

    public int CornerRadius
    {
        get => (int)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}