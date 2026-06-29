using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Services;

namespace DmToolsApp
{
    public partial class AppShell : Shell
    {
        private readonly LocalizationService _loc = LocalizationService.Instance;

        public AppShell(SessionStateService sessionStateService)
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LibrarySpellEditPage), typeof(LibrarySpellEditPage));
            Routing.RegisterRoute(nameof(LibraryTrackEditPage), typeof(LibraryTrackEditPage));
            Routing.RegisterRoute(nameof(SessionListPage), typeof(SessionListPage));
            Routing.RegisterRoute(nameof(SceneListPage), typeof(SceneListPage));
            Routing.RegisterRoute(nameof(SceneTracksPage), typeof(SceneTracksPage));

            sessionStateService.StateChanged += () =>
            {
                AudioMixerTab.IsVisible = sessionStateService.IsSessionActive;
            };

            _loc.LanguageChanged += UpdateTabTitles;
            UpdateTabTitles();
        }

        private void UpdateTabTitles()
        {
            CampaignsTab.Title    = _loc["tab_campaigns"];
            AudioMixerTab.Title   = _loc["tab_audiomixer"];
            LibraryTab.Title      = _loc["tab_library"];
            TracksContent.Title   = _loc["tab_tracks"];
            SpellsContent.Title   = _loc["tab_spells"];
            SettingsTab.Title     = _loc["tab_settings"];
        }
    }
}
