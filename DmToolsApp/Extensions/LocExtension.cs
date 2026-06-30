using DmToolsApp.Services;
using System.ComponentModel;

namespace DmToolsApp.Extensions
{
    [ContentProperty(nameof(Key))]
    public class LocExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
            => new Binding
            {
                Mode = BindingMode.OneWay,
                Path = nameof(LocalizedString.Value),
                Source = new LocalizedString(Key)
            };

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }

    public class LocalizedString : INotifyPropertyChanged
    {
        private readonly string _key;
        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizedString(string key)
        {
            _key = key;
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

        public string Value => LocalizationService.Instance[_key];
    }
}
