using DmToolsApp.Features.Library;

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

        }
    }
}
