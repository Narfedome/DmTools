using System.Windows.Input;

namespace DmToolsApp.Components.Tutorial;

public partial class TutorialHintView : ContentView
{
    public TutorialHintView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(TutorialHintView), default(string));

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public static readonly BindableProperty DescriptionTextProperty =
        BindableProperty.Create(nameof(DescriptionText), typeof(string), typeof(TutorialHintView), default(string));

    public string DescriptionText
    {
        get => (string)GetValue(DescriptionTextProperty);
        set => SetValue(DescriptionTextProperty, value);
    }

    public static readonly BindableProperty SkipCommandProperty =
        BindableProperty.Create(nameof(SkipCommand), typeof(ICommand), typeof(TutorialHintView));

    public ICommand SkipCommand
    {
        get => (ICommand)GetValue(SkipCommandProperty);
        set => SetValue(SkipCommandProperty, value);
    }
}
