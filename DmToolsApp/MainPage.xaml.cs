using DmToolsApp.Features.Play;

namespace DmToolsApp
{
    public partial class MainPage : ContentPage
    {
        private readonly PlayCampaignViewModel _vm;

        public MainPage(PlayCampaignViewModel vm)
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
}
