using DmToolsApp.Components;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibraryTrackView : ContentView
{
    public LibraryTrackView()
	{
		InitializeComponent();

        // Changer de catégorie (ou revenir sur "Tout") recharge la liste (ReloadAsync) sans jamais
        // redéclencher SizeChanged, puisque la fenêtre ne change pas de taille : sur Desktop plein
        // écran, la seule page rechargée (PageSize) ne suffit alors plus forcément à remplir tout le
        // viewport, sans qu'aucun Scrolled ne se déclenche jamais pour poursuivre la pagination. On
        // branche donc FillViewportAsync directement sur ReloadAsync (appelé une seule fois, à la toute
        // fin) plutôt que de réagir à un évènement de la collection (TrackItems.CollectionChanged/
        // HasTrackItems se déclenchait dès ClearTrackItems(), avant que ReloadAsync ait fini de remettre
        // _loadedCount/_hasMoreItems à zéro - rechargement concurrent avec un offset périmé, page vide
        // pour la nouvelle catégorie puis blocage au changement suivant).
        BindingContextChanged += (_, _) =>
        {
            if (BindingContext is LibraryTrackViewModel vm)
                vm.FillViewportRequested = () => FillViewportIfDesktopAsync(vm);
        };
    }

    private Task FillViewportIfDesktopAsync(LibraryTrackViewModel vm) =>
        DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ? FillViewportAsync(vm) : Task.CompletedTask;

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

        // ItemsViewScrolledEventArgs n'expose pas d'index par groupe (pas de LastVisibleItemGroupIndex
        // dans cette version de .NET MAUI) : LastVisibleItemIndex reste comparé au nombre total de
        // pistes chargées tous groupes confondus (LoadedTrackCount), pas au nombre de groupes.
        if (e.LastVisibleItemIndex >= vm.LoadedTrackCount - 3 && vm.LoadMoreTracksCommand.CanExecute(null))
        {
            vm.LoadMoreTracksCommand.Execute(null);
        }
    }

    // Si la page chargée tient déjà entièrement dans le viewport (ex. plein écran Desktop sans
    // barre de défilement), aucun Scrolled ne se déclenche jamais et la pagination reste bloquée
    // indéfiniment, même s'il reste des pistes à charger - un seul chargement de page (PageSize)
    // est d'ailleurs très insuffisant pour couvrir un grand écran (ex. ~70 tuiles visibles en plein
    // écran 1080p contre 15 chargées par page). SizeChanged se déclenche au premier affichage et à
    // chaque redimensionnement (bascule plein écran comprise) : on boucle alors les chargements
    // jusqu'à avoir de quoi couvrir l'espace visible estimé.
    // Desktop uniquement (meme garde que ResponsiveGridSpanBehavior, meme raison) : sur Android/iOS
    // un ecran de telephone est de toute facon toujours plus petit qu'une page (PageSize=15), le
    // probleme d'origine n'existe pas la-bas. Pire : SizeChanged peut s'y declencher PENDANT une
    // passe de layout du CollectionView natif (RecyclerView), et muter TrackItems immediatement
    // dedans plantait l'appli ("Cannot call this method while RecyclerView is computing a layout").
    private void OnCollectionViewSizeChanged(object? sender, EventArgs e)
    {
        if (BindingContext is LibraryTrackViewModel vm)
            _ = FillViewportIfDesktopAsync(vm);
    }

    // Meme largeur de tuile que ResponsiveGridSpanBehavior (qui calcule le nombre de colonnes a
    // partir d'elle) : les tuiles sont approximativement carrees (image 120 + marge 5 de chaque
    // cote + la ligne de titre en dessous), donc la meme valeur sert d'estimation de hauteur.
    // +2 lignes de marge pour garder un vrai potentiel de scroll (sinon la derniere ligne visible
    // colle exactement au bord, sans espace pour re-declencher Scrolled plus tard).
    private const double EstimatedTileSize = 150;

    private async Task FillViewportAsync(LibraryTrackViewModel vm)
    {
        if (ItemsCollection.Width <= 0 || ItemsCollection.Height <= 0)
            return;

        var columns = Math.Max(1, (int)(ItemsCollection.Width / EstimatedTileSize));
        var rows = (int)(ItemsCollection.Height / EstimatedTileSize) + 2;
        var neededItems = columns * rows;

        while (vm.HasMoreItems && vm.LoadedTrackCount < neededItems && vm.LoadMoreTracksCommand.CanExecute(null))
        {
            await vm.LoadMoreTracksCommand.ExecuteAsync(null);
        }
    }
}