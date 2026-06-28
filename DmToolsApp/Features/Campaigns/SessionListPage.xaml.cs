namespace DmToolsApp.Features.Campaigns;

public partial class SessionListPage : ContentPage
{
    public SessionListPage(SessionListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
