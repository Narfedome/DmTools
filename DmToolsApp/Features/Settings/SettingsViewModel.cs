using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ThemeService _theme = ThemeService.Instance;

        public string AppVersion => AppInfo.Current.VersionString;

        [RelayCommand]
        public async Task SelectLanguage()
        {
            var labels = LanguageLabels.Values.ToArray();
            var result = await ShowActionSheetAsync(Loc["SettingsLanguage"], labels);
            if (result == null) return;
            SelectedLanguage = LanguageLabels.First(kv => kv.Value == result).Key;
        }

        [RelayCommand]
        public async Task SelectTheme()
        {
            var result = await ShowActionSheetAsync(Loc["SettingsTheme"], ThemeOptions.ToArray());
            if (result != null) SelectedThemeOption = result;
        }

        [RelayCommand]
        public async Task OpenCoffeeLink() =>
            await Launcher.OpenAsync(new Uri("https://buymeacoffee.com/narfedome"));

        public static Dictionary<string, string> LanguageLabels => LocalizationService.SupportedLanguages;

        public ObservableCollection<string> Languages { get; } = new(LocalizationService.SupportedLanguages.Keys);
        public ObservableCollection<string> ThemeOptions { get; } = new();

        [ObservableProperty]
        private string selectedLanguage;

        public string? SelectedLanguageLabel =>
            LanguageLabels.TryGetValue(SelectedLanguage ?? "", out var label) ? label : SelectedLanguage;

        [ObservableProperty]
        private AppPalette selectedPalette;

        [ObservableProperty]
        private string selectedThemeOption;

        public SettingsViewModel()
        {
            selectedLanguage = Loc.Language;
            selectedPalette  = _theme.Palette;
            RebuildThemeOptions();
            selectedThemeOption = ThemeOptions[(int)_theme.ThemePreference];

            Loc.LanguageChanged += RebuildThemeOptions;
        }

        private void RebuildThemeOptions()
        {
            var current = _theme.ThemePreference;

            string[] labels = [Loc["SettingsThemeSystem"], Loc["SettingsThemeLight"], Loc["SettingsThemeDark"]];
            for (int i = 0; i < labels.Length; i++)
            {
                if (i < ThemeOptions.Count) ThemeOptions[i] = labels[i];
                else ThemeOptions.Add(labels[i]);
            }

            _rebuildingOptions = true;
            SelectedThemeOption = ThemeOptions[(int)current];
            _rebuildingOptions = false;
        }

        private bool _rebuildingOptions;

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value != null) Loc.Language = value;
            OnPropertyChanged(nameof(SelectedLanguageLabel));
        }

        partial void OnSelectedPaletteChanged(AppPalette value)
        {
            _theme.Palette = value;
        }

        partial void OnSelectedThemeOptionChanged(string value)
        {
            if (value is null || _rebuildingOptions) return;
            int idx = ThemeOptions.IndexOf(value);
            if (idx >= 0) _theme.ThemePreference = (AppThemePreference)idx;
        }
    }
}
