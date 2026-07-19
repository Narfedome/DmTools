using System.Windows.Input;

namespace DmToolsApp.Components.Tutorial;

/// <summary>
/// Enveloppe un bouton/lien ciblé par une étape du tutoriel : anneau de mise en évidence, bulle de
/// texte et flèche animée regroupés au même endroit, juste au-dessus de l'élément réel. Utilisation :
/// &lt;tutorial:TutorialCoachMark IsActive="{Binding ShowXHint}" HintTitle="..." HintDescription="..."
///                              SkipCommand="{Binding SkipTutorialCommand}"&gt;
///     &lt;components:FaIconButtonView .../&gt;
/// &lt;/tutorial:TutorialCoachMark&gt;
/// </summary>
[ContentProperty(nameof(TargetContent))]
public partial class TutorialCoachMark : Grid
{
    public TutorialCoachMark()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TargetContentProperty =
        BindableProperty.Create(nameof(TargetContent), typeof(View), typeof(TutorialCoachMark),
            propertyChanged: OnTargetContentChanged);

    public View TargetContent
    {
        get => (View)GetValue(TargetContentProperty);
        set => SetValue(TargetContentProperty, value);
    }

    public static readonly BindableProperty IsActiveProperty =
        BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(TutorialCoachMark), false,
            propertyChanged: OnIsActiveChanged);

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly BindableProperty HintTitleProperty =
        BindableProperty.Create(nameof(HintTitle), typeof(string), typeof(TutorialCoachMark));

    public string HintTitle
    {
        get => (string)GetValue(HintTitleProperty);
        set => SetValue(HintTitleProperty, value);
    }

    public static readonly BindableProperty HintDescriptionProperty =
        BindableProperty.Create(nameof(HintDescription), typeof(string), typeof(TutorialCoachMark));

    public string HintDescription
    {
        get => (string)GetValue(HintDescriptionProperty);
        set => SetValue(HintDescriptionProperty, value);
    }

    public static readonly BindableProperty SkipCommandProperty =
        BindableProperty.Create(nameof(SkipCommand), typeof(ICommand), typeof(TutorialCoachMark));

    public ICommand SkipCommand
    {
        get => (ICommand)GetValue(SkipCommandProperty);
        set => SetValue(SkipCommandProperty, value);
    }

    private static void OnTargetContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var coachMark = (TutorialCoachMark)bindable;
        if (coachMark.TargetHost == null) return; // pas encore InitializeComponent()

        if (oldValue is View oldView)
            coachMark.TargetHost.Children.Remove(oldView);
        if (newValue is View newView)
            coachMark.TargetHost.Children.Insert(0, newView);
    }

    private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var coachMark = (TutorialCoachMark)bindable;
        if ((bool)newValue)
            _ = coachMark.BounceLoopAsync();
    }

    // Boucle tant que IsActive reste vrai : se relance à chaque étape qui réutilise ce même
    // contrôle plutôt que de tourner en continu pour rien une fois l'étape passée.
    private async Task BounceLoopAsync()
    {
        while (IsActive)
        {
            await ArrowLabel.TranslateToAsync(0, 6, 450, Easing.CubicInOut);
            if (!IsActive) break;
            await ArrowLabel.TranslateToAsync(0, 0, 450, Easing.CubicInOut);
        }
        ArrowLabel.TranslationY = 0;
    }
}
