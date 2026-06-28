namespace DmToolsApp.Features.Play;

public partial class PlayScenePage : ContentPage
{
    public PlayScenePage(PlaySceneViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
