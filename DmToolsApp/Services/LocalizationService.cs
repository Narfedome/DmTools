using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace DmToolsApp.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static readonly LocalizationService Instance = new();

        private static readonly ResourceManager _rm = new(
            "DmToolsApp.Strings.AppStrings",
            typeof(LocalizationService).Assembly);

        private CultureInfo _culture;

        private LocalizationService()
        {
            _culture = new CultureInfo(Preferences.Default.Get("app_lang", "fr"));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                LanguageChanged?.Invoke();
            }
        }

        public event Action? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
