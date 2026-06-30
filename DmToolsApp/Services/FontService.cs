namespace DmToolsApp.Services
{
    public enum AppFont
    {
        Default = 0,
        PirataOne = 1
    }

    public class FontService
    {
        public static readonly FontService Instance = new();

        private const string FontKey = "app_font";
        private AppFont _font;

        private FontService()
        {
            _font = (AppFont)Preferences.Default.Get(FontKey, (int)AppFont.Default);
        }

        public AppFont Font
        {
            get => _font;
            set
            {
                if (_font == value) return;
                _font = value;
                Preferences.Default.Set(FontKey, (int)value);
                Apply();
            }
        }

        public void Initialize() => Apply();

        public void Apply()
        {
            if (Application.Current?.Resources is null) return;
            Application.Current.Resources["AppFontFamily"] = _font switch
            {
                AppFont.PirataOne => "PirataOne",
                _                 => "OpenSansRegular"
            };
        }
    }
}
