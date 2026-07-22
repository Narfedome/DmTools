namespace DmToolsApp.Components;

public partial class RotatedLabelView : ContentView
{
    public RotatedLabelView()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateSize();
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(RotatedLabelView),
            string.Empty,
            propertyChanged: (bindable, _, _) => ((RotatedLabelView)bindable).UpdateSize());

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(RotatedLabelView),
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
            typeof(RotatedLabelView),
            14.0,
            propertyChanged: (bindable, _, _) => ((RotatedLabelView)bindable).UpdateSize());

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty LineBreakModeProperty =
        BindableProperty.Create(
            nameof(LineBreakMode),
            typeof(LineBreakMode),
            typeof(RotatedLabelView),
            Microsoft.Maui.LineBreakMode.NoWrap);

    public LineBreakMode LineBreakMode
    {
        get => (LineBreakMode)GetValue(LineBreakModeProperty);
        set => SetValue(LineBreakModeProperty, value);
    }

    /// <summary>Angle de rotation du texte, en degrés (par défaut vertical, de bas en haut).</summary>
    public static readonly BindableProperty RotationAngleProperty =
        BindableProperty.Create(
            nameof(RotationAngle),
            typeof(double),
            typeof(RotatedLabelView),
            -90.0);

    public double RotationAngle
    {
        get => (double)GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    /// <summary>
    /// Plafond au-delà duquel le texte est tronqué (LineBreakMode) plutôt que de continuer à
    /// grandir : sans ça, un nom de piste très long (ex. un nom de fichier complet) réclame toute
    /// la hauteur disponible dans la ligne "Auto" et écrase le slider voisin dans sa ligne "*".
    /// Abaissé à 90 (au lieu de 140) pour laisser plus de place au slider sur mobile.
    /// </summary>
    public static readonly BindableProperty MaximumLengthProperty =
        BindableProperty.Create(
            nameof(MaximumLength),
            typeof(double),
            typeof(RotatedLabelView),
            90.0,
            propertyChanged: (bindable, _, _) => ((RotatedLabelView)bindable).UpdateSize());

    public double MaximumLength
    {
        get => (double)GetValue(MaximumLengthProperty);
        set => SetValue(MaximumLengthProperty, value);
    }

    /// <summary>
    /// Mesure la largeur naturelle du texte (sans contrainte) et la reporte comme hauteur du
    /// composant une fois pivoté : c'est ce qui rend le label "responsive" plutôt que figé sur une
    /// valeur en pixels devinée à la main. Le Measure legacy est nécessaire ici car il n'y a pas
    /// d'équivalent moderne pour interroger la taille intrinsèque d'une vue en dehors d'un passage
    /// de layout.
    /// </summary>
    private void UpdateSize()
    {
        if (InnerLabel.Handler == null)
            return;

        var naturalLength = InnerLabel.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        if (naturalLength <= 0)
            return;

        var length = Math.Min(naturalLength, MaximumLength);
        InnerLabel.WidthRequest = length;
        RootGrid.HeightRequest = length;
    }
}
