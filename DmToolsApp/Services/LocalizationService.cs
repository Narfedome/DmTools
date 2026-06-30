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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                LanguageChanged?.Invoke();
            }
        }

        // --- Typed properties (one per resx key) ---
        public string TabCampaigns        => this["TabCampaigns"];
        public string TabAudiomixer       => this["TabAudiomixer"];
        public string TabLibrary          => this["TabLibrary"];
        public string TabTracks           => this["TabTracks"];
        public string TabSpells           => this["TabSpells"];
        public string TabSettings         => this["TabSettings"];
        public string MainTitle           => this["MainTitle"];
        public string MainChooseCampaign  => this["MainChooseCampaign"];
        public string BtnPlay             => this["BtnPlay"];
        public string NavNext             => this["NavNext"];
        public string CampaignsHeader     => this["CampaignsHeader"];
        public string ChaptersHeader      => this["ChaptersHeader"];
        public string ScenesHeader        => this["ScenesHeader"];
        public string TracksHeader        => this["TracksHeader"];
        public string NavCampaign         => this["NavCampaign"];
        public string NavChapter          => this["NavChapter"];
        public string BtnNewCampaign      => this["BtnNewCampaign"];
        public string BtnNewChapter       => this["BtnNewChapter"];
        public string BtnNewScene         => this["BtnNewScene"];
        public string BtnRename           => this["BtnRename"];
        public string BtnDelete           => this["BtnDelete"];
        public string BtnSave             => this["BtnSave"];
        public string BtnCancel           => this["BtnCancel"];
        public string BtnLaunch           => this["BtnLaunch"];
        public string BtnAddTrack         => this["BtnAddTrack"];
        public string BtnAddChannel       => this["BtnAddChannel"];
        public string BtnSaveTrack        => this["BtnSaveTrack"];
        public string BtnRemoveSceneTrack => this["BtnRemoveSceneTrack"];
        public string MixerChapter        => this["MixerChapter"];
        public string MixerScene          => this["MixerScene"];
        public string MixerPrevScene      => this["MixerPrevScene"];
        public string MixerNextScene      => this["MixerNextScene"];
        public string MixerLoadScene      => this["MixerLoadScene"];
        public string MixerGlobal         => this["MixerGlobal"];
        public string MixerPlayAll        => this["MixerPlayAll"];
        public string MixerFadeOutAll     => this["MixerFadeOutAll"];
        public string MixerStopAll        => this["MixerStopAll"];
        public string ChannelFadeOut      => this["ChannelFadeOut"];
        public string ChannelLoop         => this["ChannelLoop"];
        public string ChannelPlay         => this["ChannelPlay"];
        public string ChannelPause        => this["ChannelPause"];
        public string ChannelNew          => this["ChannelNew"];
        public string SceneAuto           => this["SceneAuto"];
        public string LibAdd              => this["LibAdd"];
        public string LibEdit             => this["LibEdit"];
        public string LibDelete           => this["LibDelete"];
        public string LibConfirm          => this["LibConfirm"];
        public string LibCancel           => this["LibCancel"];
        public string LibSelect           => this["LibSelect"];
        public string TrackTitlePh        => this["TrackTitlePh"];
        public string TrackEditTitle      => this["TrackEditTitle"];
        public string TrackCreateTitle    => this["TrackCreateTitle"];
        public string TrackImport         => this["TrackImport"];
        public string TrackSelectFile     => this["TrackSelectFile"];
        public string SpellTitlePh        => this["SpellTitlePh"];
        public string SpellDescPh         => this["SpellDescPh"];
        public string SpellEditTitle      => this["SpellEditTitle"];
        public string SettingsTitle         => this["SettingsTitle"];
        public string SettingsLanguage      => this["SettingsLanguage"];
        public string SettingsPalette       => this["SettingsPalette"];
        public string SettingsTheme         => this["SettingsTheme"];
        public string SettingsThemeSystem   => this["SettingsThemeSystem"];
        public string SettingsThemeLight    => this["SettingsThemeLight"];
        public string SettingsThemeDark     => this["SettingsThemeDark"];
        public string SettingsSupportTitle  => this["SettingsSupportTitle"];
        public string SettingsSupportDesc   => this["SettingsSupportDesc"];
        public string SettingsVersion       => this["SettingsVersion"];
        public string OnboardingTitle       => this["OnboardingTitle"];
        public string OnboardingDesc        => this["OnboardingDesc"];
        public string OnboardingPalette     => this["OnboardingPalette"];
        public string OnboardingLanguage    => this["OnboardingLanguage"];
        public string OnboardingStart       => this["OnboardingStart"];
        public string PromptName          => this["PromptName"];
        public string DialogRename        => this["DialogRename"];
        public string DialogDelete        => this["DialogDelete"];
        public string DialogYes           => this["DialogYes"];
        public string DialogNo            => this["DialogNo"];
        public string DialogDeleteConfirm      => this["DialogDeleteConfirm"];
        public string DialogRemoveChannel      => this["DialogRemoveChannel"];
        public string DialogDeleteTrackConfirm => this["DialogDeleteTrackConfirm"];
        public string DialogNewCampaign   => this["DialogNewCampaign"];
        public string DialogNewChapter    => this["DialogNewChapter"];
        public string DialogNewScene      => this["DialogNewScene"];

        public event Action? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
