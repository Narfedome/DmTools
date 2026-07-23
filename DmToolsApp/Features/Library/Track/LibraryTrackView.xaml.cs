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
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Desktop && BindingContext is LibraryTrackViewModel vm)
            _ = FillViewportAsync(vm);
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

        while (vm.TrackItems.Count < neededItems && vm.LoadMoreTracksCommand.CanExecute(null))
        {
            await vm.LoadMoreTracksCommand.ExecuteAsync(null);
        }
    }
}