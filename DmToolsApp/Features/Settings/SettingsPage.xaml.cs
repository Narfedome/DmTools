namespace DmToolsApp.Features.Settings
{
    public partial class SettingsPage : ContentPage
    {
        // TODO: replace with your actual buymeacoffee URL
        private const string CoffeeUrl = "https://buymeacoffee.com/narfedome";

        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnCoffeeTapped(object sender, TappedEventArgs e)
        {
            await Launcher.OpenAsync(new Uri(CoffeeUrl));
        }
    }
}
