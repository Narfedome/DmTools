using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly LocalizationService _loc = LocalizationService.Instance;
        private readonly ThemeService _theme = ThemeService.Instance;

        public LocalizationService Loc => _loc;

        public ObservableCollection<string> Languages { get; } = new() { "fr", "en" };

        public ObservableCollection<string> ThemeOptions { get; } = new();

        [ObservableProperty]
        private string selectedLanguage;

        [ObservableProperty]
        private AppPalette selectedPalette;

        [ObservableProperty]
        private string selectedThemeOption;

        public SettingsViewModel()
        {
            selectedLanguage = _loc.Language;
            selectedPalette  = _theme.Palette;
            RebuildThemeOptions();
            selectedThemeOption = ThemeOptions[(int)_theme.ThemePreference];

            _loc.LanguageChanged += RebuildThemeOptions;
        }

        private void RebuildThemeOptions()
        {
            var current = _theme.ThemePreference;

            // Update in place to avoid null SelectedItem during Clear()
            string[] labels = [_loc.SettingsThemeSystem, _loc.SettingsThemeLight, _loc.SettingsThemeDark];
            for (int i = 0; i < labels.Length; i++)
            {
                if (i < ThemeOptions.Count) ThemeOptions[i] = labels[i];
                else ThemeOptions.Add(labels[i]);
            }

            // Suppress theme-change side effect — only updating display labels
            _rebuildingOptions = true;
            SelectedThemeOption = ThemeOptions[(int)current];
            _rebuildingOptions = false;
        }

        private bool _rebuildingOptions;

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value != null) _loc.Language = value;
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
