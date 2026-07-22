using Microsoft.Maui.Controls.Shapes;

namespace DmToolsApp.Components;

public partial class ScrollingLabelView : ContentView
{
    public ScrollingLabelView()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
    }


    #region BindableProperty 
    public static readonly BindableProperty TextColorProperty =
    BindableProperty.Create(
        nameof(TextColor),
        typeof(Color),
        typeof(ScrollingLabelView),
        Color.FromArgb("#FFFFFF"));

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(
            nameof(IsPlaying),
            typeof(bool),
            typeof(ScrollingLabelView),
            false,
            propertyChanged: OnTextOrPlayingChanged);
 
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(ScrollingLabelView),
            // Cf. ChannelVolumeSliderView.xaml.cs : Resources["AppFontSizeBase"] renverrait l'objet
            // OnIdiom<double> brut (pas resolu) via un simple acces dictionnaire C#.
            defaultValueCreator: _ => DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ? 12.0 : 14.0);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ScrollingLabelView),
            string.Empty,
            propertyChanged: OnTextOrPlayingChanged);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly BindableProperty LineBreakModeProperty =
        BindableProperty.Create(
            nameof(LineBreakMode),
            typeof(string),
            typeof(ScrollingLabelView),
            string.Empty);

    public string LineBreakMode
    {
        get => (string)GetValue(LineBreakModeProperty);
        set => SetValue(LineBreakModeProperty, value);
    }

    private static void OnTextOrPlayingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ScrollingLabelView)bindable;
        control.UpdateScrolling();
    }
    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Width > 0 && Height > 0)
            RootGrid.Clip = new RectangleGeometry(new Rect(0, 0, Width, Height));
        UpdateScrolling();
    }
    #endregion

    private bool _isScrolling = false;
    #region Animation

    private void StopScrolling()
    {
        _isScrolling = false;
        ScrollingLabel.TranslationX = 0;
    }
    private void UpdateScrolling()
    {
        if (!IsPlaying || string.IsNullOrEmpty(Text))
        {
            StopScrolling();
            return;
        }

        if (!_isScrolling)
        {
            _isScrolling = true;
            StartScrollingLoop();
        }
    }
    private async void StartScrollingLoop()
    {
        try
        {
            while (_isScrolling && IsPlaying)
            {
                // Repositionne le texte au départ
                ScrollingLabel.TranslationX = RootGrid.Width;

                // Défilement linéaire jusqu'à la fin
                await ScrollingLabel.TranslateToAsync(-ScrollingLabel.Width, 0,
                    (uint)(5000 + ScrollingLabel.Width * 10), Easing.Linear);
            }

            // Si on sort de la boucle, repositionner le texte à gauche
            ScrollingLabel.TranslationX = 0;
        }
        catch
        {
            // Ignore les erreurs d'annulation
        }
    }
    #endregion
}

