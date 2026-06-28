namespace DmToolsApp.Features.Campaigns;

public partial class SceneTracksPage : ContentPage
{
    public SceneTracksPage(SceneTracksViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
