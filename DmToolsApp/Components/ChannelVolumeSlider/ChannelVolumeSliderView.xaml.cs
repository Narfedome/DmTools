namespace DmToolsApp.Components;

public partial class ChannelVolumeSliderView : ContentView
{
    public ChannelVolumeSliderView()
    {
        InitializeComponent();
        TrackHost.SizeChanged += (_, _) => UpdateTrackLength();

        // À l'intérieur du CollectionView (item template), le tout premier passage de layout
        // mesure souvent la ligne "*" avec une contrainte non résolue (elle reste à 0) - le
        // slider n'apparaît qu'après un évènement de resize qui force un nouveau passage. On
        // force donc nous-même un remeasure une fois la vue montée, une fois le dispatcher UI
        // libre (le layout de la CollectionView est alors retombé sur ses pieds).
        Loaded += (_, _) => Dispatcher.Dispatch(() => TrackHost.InvalidateMeasure());
    }

    /// <summary>
    /// TrackHost (ligne "*") reçoit la hauteur que le Grid parent lui accorde ; on la reporte
    /// comme WidthRequest du slider avant rotation pour qu'il remplisse exactement cet espace,
    /// au lieu d'une longueur fixe qui déborde ou laisse un vide selon la taille de la fenêtre.
    /// </summary>
    private void UpdateTrackLength()
    {
        if (TrackHost.Height > 0)
            InnerSlider.WidthRequest = TrackHost.Height;
    }

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            0.0,
            BindingMode.TwoWay);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(
            nameof(Minimum),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            0.0);

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(
            nameof(Maximum),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            1.0);

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Texte affiché au-dessus du slider (ex. valeur formatée en pourcentage).</summary>
    public static readonly BindableProperty ValueTextProperty =
        BindableProperty.Create(
            nameof(ValueText),
            typeof(string),
            typeof(ChannelVolumeSliderView),
            string.Empty);

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(ChannelVolumeSliderView),
            defaultValueCreator: _ => (Color)Application.Current!.Resources["AppText"]);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            14.0);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>Angle de rotation du slider, en degrés (par défaut vertical, de bas en haut).</summary>
    public static readonly BindableProperty RotationAngleProperty =
        BindableProperty.Create(
            nameof(RotationAngle),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            -90.0);

    public double RotationAngle
    {
        get => (double)GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    /// <summary>Largeur de la colonne (label de valeur + slider).</summary>
    public static readonly BindableProperty ThicknessProperty =
        BindableProperty.Create(
            nameof(Thickness),
            typeof(double),
            typeof(ChannelVolumeSliderView),
            50.0);

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }
}
