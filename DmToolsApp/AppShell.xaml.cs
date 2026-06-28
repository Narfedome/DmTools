using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Services;

namespace DmToolsApp
{
    public partial class AppShell : Shell
    {
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
        }
    }
}
