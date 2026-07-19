namespace DmToolsApp.Components.Tutorial;

public partial class TutorialArrowView : ContentView
{
    public TutorialArrowView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty IsActiveProperty =
        BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(TutorialArrowView), false,
            propertyChanged: OnIsActiveChanged);

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (TutorialArrowView)bindable;
        if ((bool)newValue)
            _ = view.BounceLoopAsync();
    }

    // Boucle tant que IsActive reste vrai : se relance à chaque étape du tutoriel qui réutilise
    // ce même contrôle plutôt que de tourner en continu pour rien une fois l'étape passée.
    private async Task BounceLoopAsync()
    {
        while (IsActive)
        {
            await ArrowLabel.TranslateToAsync(0, 8, 450, Easing.CubicInOut);
            if (!IsActive) break;
            await ArrowLabel.TranslateToAsync(0, 0, 450, Easing.CubicInOut);
        }
        ArrowLabel.TranslationY = 0;
    }
}
