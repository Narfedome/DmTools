namespace DmToolsApp.Features.Campaigns;

public partial class SceneListPage : ContentPage
{
    public SceneListPage(SceneListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
