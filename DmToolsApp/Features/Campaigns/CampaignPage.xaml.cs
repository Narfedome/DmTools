namespace DmToolsApp.Features.Campaigns;

public partial class CampaignPage : ContentPage
{
    private readonly CampaignViewModel _vm;

    public CampaignPage(CampaignViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
