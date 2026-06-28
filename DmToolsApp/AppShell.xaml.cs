using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Features.Play;

namespace DmToolsApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            GoToAsync("//InitialPage");
            Routing.RegisterRoute(nameof(LibrarySpellEditPage), typeof(LibrarySpellEditPage));
            Routing.RegisterRoute(nameof(LibraryTrackEditPage), typeof(LibraryTrackEditPage));
            Routing.RegisterRoute(nameof(SessionListPage), typeof(SessionListPage));
            Routing.RegisterRoute(nameof(SceneListPage), typeof(SceneListPage));
            Routing.RegisterRoute(nameof(SceneTracksPage), typeof(SceneTracksPage));
            Routing.RegisterRoute(nameof(PlaySessionPage), typeof(PlaySessionPage));
            Routing.RegisterRoute(nameof(PlayScenePage), typeof(PlayScenePage));
        }
    }
}
