using DmToolsApp.Services;

namespace DmToolsApp.Features.Onboarding
{
    public partial class OnboardingPage : ContentPage
    {
        public OnboardingPage(OnboardingViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            var vm = (OnboardingViewModel)BindingContext;
            var labels = LocalizationService.SupportedLanguages.Values.ToArray();
            var result = await DisplayActionSheet(vm.Loc.OnboardingLanguage, vm.Loc.BtnCancel, null, labels);
            if (result is null || result == vm.Loc.BtnCancel) return;
            var code = LocalizationService.SupportedLanguages.First(kv => kv.Value == result).Key;
            vm.SelectedLanguage = code;
        }
    }
}
