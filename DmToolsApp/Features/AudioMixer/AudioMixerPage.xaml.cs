using DmToolsApp.Components;

namespace DmToolsApp.Features.AudioMixer;

public partial class AudioMixerPage : ContentPage
{
    private readonly AudioMixerViewModel _vm;

    // MAUI ne réordonne pas les autres items en direct pendant le drag (pas d'équivalent au
    // reorder animé natif d'un RecyclerView/UICollectionView) : ce feedback minimal (opacité +
    // surlignage de la cible survolée) est ce qu'on peut faire sans réécrire le layout à la main.
    private VisualElement? _draggingElement;

    private static Color HighlightColor =>
        ((Color)Application.Current!.Resources["AppAccent"]).WithAlpha(0.25f);

    // Auto-scroll pendant le drag : CollectionView ne le fait pas nativement quand on approche
    // un bord alors que la liste horizontale déborde (scrollbar visible). ScrollTo n'accepte
    // qu'un index (pas un offset en pixels) : on avance/recule d'un item par tick tant que le
    // pointeur reste près du bord.
    private const double AutoScrollEdgeThreshold = 60;
    private IDispatcherTimer? _autoScrollTimer;
    private int _autoScrollDirection;
    private int _autoScrollIndex;

    public AudioMixerPage(AudioMixerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    // Cf. SceneDataService.ReorderSceneTracksAsync : ne stocke que l'item glissé, pas un index,
    // pour rester valide même si l'ordre a changé entre le début du drag et le drop.
    private void OnChannelDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is not VisualElement { BindingContext: ChannelStripViewModel channel } element)
            return;

        e.Data.Properties["Channel"] = channel;

        // Le sender est la poignée (seul point d'accroche du drag, cf. XAML), mais l'estompage
        // doit viser toute la carte : on remonte jusqu'à l'item racine (le Grid qui porte le
        // DropGestureRecognizer).
        _draggingElement = FindItemCard(element);
        _draggingElement.Opacity = 0.5;
    }

    private static VisualElement FindItemCard(VisualElement start)
    {
        Element? current = start;
        while (current != null && current is not Grid { GestureRecognizers.Count: > 0 })
            current = current.Parent;
        return current as VisualElement ?? start;
    }

    // Se déclenche sur la source une fois le geste terminé (succès ou non). Sur Android ce
    // n'est pas toujours fiable (bug connu dotnet/maui#17554) : OnChannelDrop réinitialise
    // aussi l'opacité par sécurité si cet event ne se déclenche pas.
    private void OnChannelDropCompleted(object sender, DropCompletedEventArgs e)
    {
        ResetDraggingOpacity();
        StopAutoScroll();
    }

    private void OnChannelDragOver(object sender, DragEventArgs e)
    {
        if (sender is not VisualElement { BindingContext: ChannelStripViewModel hovered } element)
            return;

        element.BackgroundColor = HighlightColor;
        UpdateAutoScroll(e, hovered);
    }

    private void OnChannelDragLeave(object sender, DragEventArgs e)
    {
        if (sender is VisualElement element)
            element.BackgroundColor = Colors.Transparent;
        // L'auto-scroll n'est PAS arrêté ici : ce handler se déclenche aussi en passant d'une
        // carte à sa voisine (juste avant le DragOver de la suivante), l'arrêter à chaque fois
        // ferait repartir la progression de zéro à chaque item traversé.
    }

    private async void OnChannelDrop(object sender, DropEventArgs e)
    {
        if (sender is VisualElement targetElement)
            targetElement.BackgroundColor = Colors.Transparent;
        ResetDraggingOpacity();
        StopAutoScroll();

        if (sender is not Element { BindingContext: ChannelStripViewModel target })
            return;
        if (!e.Data.Properties.TryGetValue("Channel", out var draggedObj) || draggedObj is not ChannelStripViewModel dragged)
            return;

        await _vm.ReorderChannelsAsync(dragged, target);
    }

    private void UpdateAutoScroll(DragEventArgs e, ChannelStripViewModel hovered)
    {
        var position = e.GetPosition(ChannelsCollectionView);
        if (position is null || ChannelsCollectionView.Width <= 0)
        {
            StopAutoScroll();
            return;
        }

        double x = position.Value.X;
        int direction = x < AutoScrollEdgeThreshold ? -1
            : x > ChannelsCollectionView.Width - AutoScrollEdgeThreshold ? 1
            : 0;

        if (direction == 0)
        {
            StopAutoScroll();
            return;
        }

        // Déjà en cours dans la même direction : on laisse le timer progresser tout seul plutôt
        // que de repartir de l'item actuellement survolé (cf. commentaire sur OnChannelDragLeave).
        if (_autoScrollTimer != null && _autoScrollDirection == direction)
            return;

        StopAutoScroll();
        _autoScrollDirection = direction;
        _autoScrollIndex = _vm.CurrentChannels.IndexOf(hovered);

        _autoScrollTimer = Dispatcher.CreateTimer();
        _autoScrollTimer.Interval = TimeSpan.FromMilliseconds(400);
        _autoScrollTimer.Tick += (_, _) => AdvanceAutoScroll();
        _autoScrollTimer.Start();
    }

    private void AdvanceAutoScroll()
    {
        if (_vm.CurrentChannels.Count == 0)
        {
            StopAutoScroll();
            return;
        }

        _autoScrollIndex = Math.Clamp(_autoScrollIndex + _autoScrollDirection, 0, _vm.CurrentChannels.Count - 1);
        ChannelsCollectionView.ScrollTo(_autoScrollIndex, position: ScrollToPosition.MakeVisible, animate: true);
    }

    private void StopAutoScroll()
    {
        if (_autoScrollTimer == null) return;
        _autoScrollTimer.Stop();
        _autoScrollTimer = null;
        _autoScrollDirection = 0;
    }

    private void ResetDraggingOpacity()
    {
        if (_draggingElement == null) return;
        _draggingElement.Opacity = 1;
        _draggingElement = null;
    }
}
