using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace DmToolsApp.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static readonly Dictionary<string, string> SupportedLanguages = new()
        {
            { "fr", "Français" },
            { "en", "English" },
        };

        private static readonly ResourceManager _rm = new(
            "DmToolsApp.Strings.AppStrings",
            typeof(LocalizationService).Assembly);

        private CultureInfo _culture;

        public static readonly LocalizationService Instance = new();

        private LocalizationService()
        {
            var deviceLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var defaultLang = SupportedLanguages.ContainsKey(deviceLang) ? deviceLang : "fr";
            _culture = new CultureInfo(Preferences.Default.Get("app_lang", defaultLang));
        }

        public string this[string key] => _rm.GetString(key, _culture) ?? key;

        public string Language
        {
            get => _culture.Name;
            set
            {
                if (_culture.Name == value) return;
                _culture = new CultureInfo(value);
                Preferences.Default.Set("app_lang", value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                LanguageChanged?.Invoke();
            }
        }

        public event Action? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
