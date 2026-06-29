using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Onboarding
{
    public partial class OnboardingViewModel : ObservableObject
    {
        private readonly LocalizationService _loc = LocalizationService.Instance;
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly AppShell _shell;

        public LocalizationService Loc => _loc;

        public ObservableCollection<string> Languages { get; } = new() { "fr", "en" };

        [ObservableProperty]
        private string selectedLanguage;

        [ObservableProperty]
        private AppPalette selectedPalette;

        public OnboardingViewModel(AppShell shell)
        {
            _shell = shell;
            selectedLanguage = _loc.Language;
            selectedPalette  = _theme.Palette;
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value != null) _loc.Language = value;
        }

        partial void OnSelectedPaletteChanged(AppPalette value)
        {
            _theme.Palette = value;
        }

        [RelayCommand]
        private void Start()
        {
            Preferences.Default.Set("has_launched", true);
            Application.Current!.Windows[0].Page = _shell;
        }
    }
}
