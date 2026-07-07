using CommunityToolkit.Maui.Extensions;

namespace DmToolsApp.Features.Library;

public partial class LibraryTrackPage : ContentPage
{
	public LibraryTrackPage(LibraryTrackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Fermer une popup thémée (confirmation, saisie...) redéclenche aussi la navigation sur cette
        // page : dans ce cas le ViewModel a déjà mis à jour TrackItems localement, un rechargement complet
        // ferait doublon et pourrait entrer en collision avec cette mise à jour (liste qui clignote/se perd).
        if (args.WasPreviousPageACommunityToolkitPopupPage())
            return;

        if (BindingContext is LibraryTrackViewModel vm)
            await vm.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop any playing audio when leaving the library page via the ViewModel (use DI)
        if (BindingContext is LibraryTrackViewModel vm)
        {
            vm.StopAudio();
        }
    }
}