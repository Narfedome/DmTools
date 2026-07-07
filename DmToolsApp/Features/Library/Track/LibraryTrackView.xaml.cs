using DmToolsApp.Components;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibraryTrackView : ContentView
{
    public LibraryTrackView()
	{
		InitializeComponent();
    }
        
    public static readonly BindableProperty IsCrudProperty =
    BindableProperty.Create(nameof(IsCrud), typeof(bool), typeof(LibraryTrackView), default(bool));

    public bool IsCrud
    {
        get => (bool)GetValue(IsCrudProperty);
        set => SetValue(IsCrudProperty, value);
    }

    // RemainingItemsThresholdReachedCommand est peu fiable sur certaines plateformes (notamment quand la vue
    // est imbriquée dans un ControlTemplate comme WatermarkedLayout) : on détecte la fin de liste manuellement.
    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (e.LastVisibleItemIndex < 0 || BindingContext is not LibraryTrackViewModel vm)
            return;

        if (e.LastVisibleItemIndex >= vm.TrackItems.Count - 3 && vm.LoadMoreTracksCommand.CanExecute(null))
        {
            vm.LoadMoreTracksCommand.Execute(null);
        }
    }
}