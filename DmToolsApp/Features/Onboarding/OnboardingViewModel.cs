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
        private readonly FontService _font = FontService.Instance;
        private readonly AppShell _shell;

        public LocalizationService Loc => _loc;

        public ObservableCollection<string> Languages   { get; } = new() { "fr", "en" };
        public ObservableCollection<string> FontOptions { get; } = new();

        [ObservableProperty]
        private string selectedLanguage;

        [ObservableProperty]
        private AppPalette selectedPalette;

        [ObservableProperty]
        private string selectedFontOption;

        public OnboardingViewModel(AppShell shell)
        {
            _shell = shell;
            selectedLanguage = _loc.Language;
            selectedPalette  = _theme.Palette;
            FontOptions.Add(_loc.SettingsFontDefault);
            FontOptions.Add(_loc.SettingsFontPirata);
            selectedFontOption = FontOptions[(int)_font.Font];
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value != null) _loc.Language = value;
        }

        partial void OnSelectedFontOptionChanged(string value)
        {
            if (value is null) return;
            int idx = FontOptions.IndexOf(value);
            if (idx >= 0) _font.Font = (AppFont)idx;
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
