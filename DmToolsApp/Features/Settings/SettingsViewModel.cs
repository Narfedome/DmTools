using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly LocalizationService _loc = LocalizationService.Instance;
        public LocalizationService Loc => _loc;

        public ObservableCollection<string> Languages { get; } = new() { "fr", "en" };

        [ObservableProperty]
        private string selectedLanguage;

        public SettingsViewModel()
        {
            selectedLanguage = _loc.Language;
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value != null)
                _loc.Language = value;
        }
    }
}
