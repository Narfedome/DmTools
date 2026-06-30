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

        private async void OnThemeClicked(object sender, EventArgs e)
        {
            var vm = (SettingsViewModel)BindingContext;
            var options = vm.ThemeOptions.ToArray();
            var result = await DisplayActionSheet(vm.Loc["SettingsTheme"], vm.Loc["BtnCancel"], null, options);
            if (result is null || result == vm.Loc["BtnCancel"]) return;
            vm.SelectedThemeOption = result;
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            var vm = (SettingsViewModel)BindingContext;
            var labels = SettingsViewModel.LanguageLabels.Values.ToArray();
            var result = await DisplayActionSheet(vm.Loc["SettingsLanguage"], vm.Loc["BtnCancel"], null, labels);
            if (result is null || result == vm.Loc["BtnCancel"]) return;
            var code = SettingsViewModel.LanguageLabels.First(kv => kv.Value == result).Key;
            vm.SelectedLanguage = code;
        }

        private async void OnCoffeeTapped(object sender, TappedEventArgs e)
        {
            await Launcher.OpenAsync(new Uri(CoffeeUrl));
        }
    }
}
