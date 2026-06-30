using DmToolsApp.Extensions;
using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Resources.Icons;
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
                bool active = sessionStateService.IsSessionActive;
                AudioMixerTab.IsVisible = active;
                // Force icon re-render — MAUI doesn't redraw FontImageSource on IsVisible toggle
                if (active)
                    AudioMixerTab.Icon = new FaIconExtension { Glyph = SolidFont.Sliders }
                        .ProvideValue(null!);
            };

            _loc.LanguageChanged += UpdateTabTitles;
            UpdateTabTitles();
        }

        private void UpdateTabTitles()
        {
            CampaignsTab.Title    = _loc["TabCampaigns"];
            AudioMixerTab.Title   = _loc["TabAudiomixer"];
            LibraryTab.Title      = _loc["TabLibrary"];
            TracksContent.Title   = _loc["TabTracks"];
            SpellsContent.Title   = _loc["TabSpells"];
            SettingsTab.Title     = _loc["TabSettings"];
        }
    }
}
