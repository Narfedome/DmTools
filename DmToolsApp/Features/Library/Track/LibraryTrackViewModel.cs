using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Components.Dialogs;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryTrackViewModel : BaseViewModel
    {
        private const int PageSize = 15;

        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;
        private readonly AudioPlayerService _audioPlayerService;
        private readonly CoverArtService _coverArtService;

        private int _loadedCount;
        private bool _hasMoreItems = true;
        private bool _isLoadingMore;
        private bool _suppressCategoryReload;
        private string? _categoryFilter;

        public LibraryMultiSelection<Track> Selection { get; } = new();

        public bool HasSelection => Selection.HasSelection;

        public string SelectedCountLabel => string.Format(Loc["LibSelectedCount"], Selection.SelectedCount);

        public bool CanDelete => HasSelection || SelectedTrackItem != null;

        public string DeleteTooltip => HasSelection ? SelectedCountLabel : Loc["LibDelete"];

        private string AllCategoriesLabel => Loc["LibAllCategories"];

        // Réutilise le même libellé que le choix "Aucun dossier" du dialogue d'import (PickImportCategoryAsync) :
        // catégorie native représentant Track.Category vide, non stockée en CategoryEntity, non
        // renommable/supprimable (cf. CategoryListViewModel).
        private string NoFolderLabel => Loc["LibImportNoCategory"];

        private string CategoryDisplayName(string category) => string.IsNullOrEmpty(category) ? NoFolderLabel : category;

        [ObservableProperty]
        public ObservableCollection<TrackGroup> trackItems = new();

        public bool HasTrackItems => TrackItems.Count > 0;

        // TrackItems.Count compte les groupes (catégories), pas les pistes - utilisé par le code-behind
        // (OnCollectionViewScrolled/FillViewportAsync) pour ses heuristiques de pagination infinie.
        public int LoadedTrackCount => TrackItems.Sum(g => g.Count);

        // LoadMoreTracksCommand n'a pas de CanExecute (toujours exécutable) : c'est ce booléen, pas la
        // commande, qui indique s'il reste réellement des pistes à charger. FillViewportAsync doit le
        // vérifier explicitement, sinon sa boucle continue indéfiniment (LoadNextPageAsync devient un
        // no-op silencieux une fois _hasMoreItems à false, sans jamais faire progresser LoadedTrackCount)
        // dès qu'une catégorie contient moins de pistes que nécessaire pour remplir le viewport.
        public bool HasMoreItems => _hasMoreItems;

        // Branché par la vue (View) sur FillViewportAsync (Desktop uniquement) : seule la vue connaît la
        // taille réelle du viewport. Appelé explicitement une fois à la fin de ReloadAsync plutôt que
        // réagi via HasTrackItems.PropertyChanged - ce dernier se déclenchait dès ClearTrackItems(),
        // avant même que ReloadAsync ait fini de remettre à zéro _loadedCount/_hasMoreItems, provoquant
        // un rechargement concurrent avec un offset (_loadedCount) périmé : page vide pour la nouvelle
        // catégorie, puis boucle infinie au changement suivant (état de pagination corrompu par la course).
        public Func<Task>? FillViewportRequested { get; set; }

        [ObservableProperty]
        private Track? selectedTrackItem;

        partial void OnSelectedTrackItemChanged(Track? value)
        {
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(DeleteTooltip));
        }

        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private ObservableCollection<string> categories = new();

        [ObservableProperty]
        private string selectedCategory = string.Empty;

        partial void OnSelectedCategoryChanged(string value)
        {
            if (_suppressCategoryReload)
                return;

            _ = ReloadAfterCategoryChangeAsync();
        }

        // Le handler de changement de propriete est void (impose par le generateur de source) : pas
        // moyen d'y faire un vrai await. Sans ce wrapper, une erreur DB dans ReloadAsync disparaissait
        // silencieusement (tache fire-and-forget jamais observee) au lieu de remonter a l'utilisateur.
        private async Task ReloadAfterCategoryChangeAsync()
        {
            try
            {
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
            }
        }

        public LibraryTrackViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService, AudioPlayerService audioPlayerService, FileService fileService, CoverArtService coverArtService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            _audioPlayerService = audioPlayerService;
            _fileService = fileService;
            _coverArtService = coverArtService;
            TrackItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTrackItems));
            Selection.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Selection.SelectedCount))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedCountLabel));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(DeleteTooltip));
                }
            };
            WeakReferenceMessenger.Default.Register<LibraryUpdatedMessage>(this,
            async (r, m) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await RefreshCategoriesAsync();
                    await ReloadAsync();
                });
            });
        }

        // Stop any playing audio when leaving the view
        public void StopAudio()
        {
            _audioPlayerService?.Stop();
        }

        private bool _isInitializing;
        private bool _hasLoadedOnce;

        public async Task InitializeAsync()
        {
            // Le premier chargement (requête catégories + page de pistes + extraction des pochettes)
            // peut être long : pas la peine de le refaire à chaque retour sur l'onglet, alors que
            // LibraryUpdatedMessage (édition, catégories...) recharge déjà la liste quand les données
            // changent réellement. Ça évite aussi de revivre ce chargement long à chaque changement
            // d'onglet.
            if (_hasLoadedOnce)
                return;

            // Évite un rechargement concurrent si l'utilisateur quitte puis revient sur l'onglet avant
            // la fin du chargement précédent (navigation restant possible pendant le chargement) - on
            // laisse simplement le chargement déjà en cours se terminer plutôt que d'en démarrer un
            // second en parallèle, qui corromprait l'état de la liste (doublons, sélection incohérente).
            if (_isInitializing)
                return;

            _isInitializing = true;
            try
            {
                await Loading.RunAsync(async () =>
                {
                    await RefreshCategoriesAsync();
                    await ReloadAsync();
                });
                _hasLoadedOnce = true;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task RefreshCategoriesAsync()
        {
            var existing = await _libraryDataService.GetCategoryNamesAsync(typeof(Track));
            var previousSelection = string.IsNullOrEmpty(SelectedCategory) ? AllCategoriesLabel : SelectedCategory;

            _suppressCategoryReload = true;

            Categories.Clear();
            Categories.Add(AllCategoriesLabel);
            Categories.Add(NoFolderLabel);
            foreach (var c in existing)
                Categories.Add(c);

            SelectedCategory = Categories.Contains(previousSelection) ? previousSelection : AllCategoriesLabel;

            _suppressCategoryReload = false;
        }

        private async Task ReloadAsync()
        {
            // null = pas de filtre (Tout), "" = uniquement les pistes sans catégorie (Aucun dossier),
            // sinon le nom exact de la catégorie choisie - cf. LibraryDataService.GetItemsPageAsync.
            _categoryFilter = SelectedCategory == AllCategoriesLabel ? null
                : SelectedCategory == NoFolderLabel ? string.Empty
                : SelectedCategory;

            ClearTrackItems();
            _loadedCount = 0;
            _hasMoreItems = true;
            SelectedTrackItem = null;

            // Changer de catégorie change le contexte de sélection multiple : on repart de zéro
            // pour éviter de supprimer par erreur des pistes qu'on ne voit plus.
            Selection.Clear();

            await LoadNextPageAsync();

            if (FillViewportRequested != null)
                await FillViewportRequested();
        }

        private void ClearTrackItems()
        {
            foreach (var group in TrackItems)
                foreach (var t in group)
                    Selection.Untrack(t);

            TrackItems.Clear();
        }

        /// <summary>Ajoute une piste à la fin du groupe (catégorie) correspondant dans TrackItems,
        /// en créant ce groupe s'il n'existe pas encore.</summary>
        private void AddTrackToGroup(Track track)
        {
            var groupName = CategoryDisplayName(track.Category);
            var group = TrackItems.FirstOrDefault(g => g.Name == groupName);
            if (group == null)
            {
                group = new TrackGroup(groupName);
                TrackItems.Add(group);
            }

            group.Add(track);
        }

        private async Task LoadNextPageAsync()
        {
            if (_isLoadingMore || !_hasMoreItems)
                return;

            // Le spinner du footer (IsLoadingMore) ne doit apparaître qu'en pagination (scroll) - la
            // toute première page est déjà couverte par le spinner de la liste (Loading.IsLoading),
            // sans quoi les deux s'affichaient en même temps au premier chargement.
            var isPagination = _loadedCount > 0;

            _isLoadingMore = true;
            if (isPagination)
                IsLoadingMore = true;

            try
            {
                var items = await _libraryDataService.GetItemsPageAsync(typeof(Track), _loadedCount, PageSize, _categoryFilter);

                // Précharge les pochettes de la page avant de révéler les tuiles : l'indicateur de
                // chargement de la liste (spinner du haut ou du footer selon le cas) couvre alors tout
                // le temps de chargement réel (DB + lecture des tags audio), au lieu de disparaître
                // avant que les tuiles n'aient fini d'apparaître avec leur pochette.
                await Task.WhenAll(items.Select(i => _coverArtService.GetCoverAsync((Track)i)));

                foreach (var item in items)
                {
                    var track = (Track)item;
                    Selection.Track(track);

                    // La requête (GetItemsPageAsync) trie par catégorie : les pistes d'un même groupe
                    // sortent toujours consécutives, y compris à cheval sur deux pages successives - pas
                    // besoin de rechercher le groupe ailleurs que dans AddTrackToGroup.
                    AddTrackToGroup(track);

                    // Laisse la main au thread UI entre chaque tuile (traitement des messages Windows -
                    // dont les taps sur les autres onglets) plutôt que d'enchaîner les 12 ajouts et
                    // réalisations visuelles en un seul bloc synchrone ininterrompu.
                    await Task.Yield();
                }

                _loadedCount += items.Count;
                _hasMoreItems = items.Count == PageSize;

                if (SelectedTrackItem == null)
                    SelectedTrackItem = TrackItems.FirstOrDefault()?.FirstOrDefault();
            }
            finally
            {
                _isLoadingMore = false;
                if (isPagination)
                    IsLoadingMore = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreTracks()
        {
            await LoadNextPageAsync();
        }

        [RelayCommand]
        public async Task ConfirmSelection()
        {
            if (SelectedTrackItem is Track trackItem)
                await _navigation.ClosePickerAsync(trackItem);
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await _navigation.ClosePickerAsync(null);
        }

        [RelayCommand]
        public async Task SelectCategory()
        {
            var result = await ShowActionSheetAsync(Loc["LibImportCategoryTitle"], Categories.ToArray());
            if (result != null)
                SelectedCategory = result;
        }

        [RelayCommand]
        public async Task ManageCategories()
        {
            await Shell.Current.GoToAsync(nameof(CategoryListPage),
                new Dictionary<string, object> { { "LibraryType", typeof(Track) } });
        }

        [RelayCommand]
        public async Task SelectAll()
        {
            var ids = await _libraryDataService.GetItemIdsAsync(typeof(Track), _categoryFilter);

            var allTracks = TrackItems.SelectMany(g => g);

            // Bascule : si tout est déjà sélectionné, un nouvel appui tout désélectionne.
            if (ids.Count > 0 && Selection.ContainsAll(ids))
                Selection.DeselectAll(allTracks);
            else
                Selection.SelectIds(ids, allTracks);
        }

        /// <summary>
        /// Supprime la sélection multiple (cases cochées) si elle existe, sinon la piste actuellement
        /// tapée - un seul bouton couvre donc la suppression individuelle et la suppression multiple.
        /// </summary>
        [RelayCommand]
        public async Task DeleteSelectedItems()
        {
            var ids = Selection.HasSelection
                ? Selection.SelectedIds.ToList()
                : SelectedTrackItem != null ? new List<int> { SelectedTrackItem.Id } : new List<int>();

            if (ids.Count == 0)
                return;

            // Une seule piste ciblée (que ce soit via une case cochée ou l'item tapé) : même message
            // que la suppression individuelle, avec son titre plutôt qu'un simple compteur.
            var message = ids.Count == 1
                ? string.Format(Loc["DialogDeleteTrackConfirm"], (TrackItems.SelectMany(g => g).FirstOrDefault(t => t.Id == ids[0]) ?? SelectedTrackItem)?.Title)
                : string.Format(Loc["LibDeleteSelectedConfirm"], ids.Count);

            if (!await ConfirmAsync(Loc["DialogDelete"], message))
                return;

            StopAudio();

            var deleted = await _libraryDataService.DeleteItemsAsync(typeof(Track), ids);

            // Ne supprime les fichiers physiques (audio ET vignette de pochette) que s'ils ne sont
            // plus référencés par aucune autre track (dédup : plusieurs tracks peuvent partager le
            // même fichier et la même pochette).
            var referenced = await _libraryDataService.GetAllReferencedFilePathsAsync();
            foreach (var track in deleted.OfType<Track>())
            {
                if (!referenced.Contains(track.FilePath))
                    _fileService.DeleteTrackFromLocal(track.FilePath);
                if (!string.IsNullOrEmpty(track.ImagePath) && !referenced.Contains(track.ImagePath))
                    _fileService.DeleteTrackFromLocal(track.ImagePath);
            }

            foreach (var group in TrackItems.ToList())
            {
                foreach (var item in group.Where(t => ids.Contains(t.Id)).ToList())
                {
                    Selection.Untrack(item);
                    group.Remove(item);
                    _loadedCount--;
                }

                // Un groupe vidé de toutes ses pistes ne doit pas laisser un en-tête de catégorie
                // sans rien en dessous.
                if (group.Count == 0)
                    TrackItems.Remove(group);
            }

            Selection.Clear();
            SelectedTrackItem = TrackItems.FirstOrDefault()?.FirstOrDefault();

            await RefreshCategoriesAsync();
        }

        [RelayCommand]
        public async Task Edit()
        {
            if (SelectedTrackItem == null)
                return;

            await ShowTrackEditDialogAsync((Track)SelectedTrackItem.Clone());
        }

        [RelayCommand]
        public async Task Create()
        {
            await ShowTrackEditDialogAsync(new Track());
        }

        // La persistance (dédup par hash, copie locale, sauvegarde) vit dans
        // TrackEditDialogViewModel.Save, pas ici : ce ViewModel n'a plus qu'à afficher le dialogue.
        private async Task ShowTrackEditDialogAsync(Track item)
        {
            var categories = await _libraryDataService.GetCategoryNamesAsync(typeof(Track));
            var dialogViewModel = new TrackEditDialogViewModel(item, categories, _libraryDataService, _fileService, _audioPlayerService);
            await ShowDialogAsync(new TrackEditDialog(dialogViewModel));
        }

        [RelayCommand]
        public void SelectItem(Track track)
        {
            SelectedTrackItem = track;
        }

        [RelayCommand]
        public async Task ImportMultipleTracks()
        {
            var files = (await _fileService.PickAudioFilesAsync(Loc["TrackSelectFiles"]))?.ToList();
            if (files == null || files.Count == 0)
                return;

            var category = await PickImportCategoryAsync();
            if (category == null)
                return;

            int imported = 0;
            int duplicates = 0;
            int failed = 0;

            var popupView = new ImportProgressPopupView();
            popupView.ViewModel.Title = Loc["LibImportInProgress"];
            popupView.ViewModel.TotalCount = files.Count;

            var page = Shell.Current.CurrentPage;
            page.ShowPopup(popupView, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = false });

            using var cts = new CancellationTokenSource();
            popupView.ViewModel.CancelRequested += cts.Cancel;

            try
            {
                foreach (var file in files)
                {
                    // Vérifié entre deux fichiers plutôt qu'au milieu d'un traitement (hash, copie...) :
                    // les fichiers audio importés ici restent individuellement petits/rapides à traiter,
                    // pas besoin d'un CancellationToken jusque dans TrackTagHelper/CopyTrackToLocal.
                    if (cts.IsCancellationRequested)
                        break;

                    popupView.ViewModel.CurrentFileName = file.FileName;

                    try
                    {
                        var hash = await Task.Run(() => TrackTagHelper.ComputeSha256(file.FullPath));
                        var existing = await _libraryDataService.FindTrackByHashAsync(hash, 0);

                        string filePath;
                        if (existing != null)
                        {
                            filePath = existing.FilePath;
                            duplicates++;
                        }
                        else
                        {
                            filePath = await Task.Run(() => _fileService.CopyTrackToLocal(file.FullPath));
                        }

                        var title = file.FileName;
                        var duration = TimeSpan.Zero;
                        byte[]? coverBytes = null;
                        try
                        {
                            var (tagTitle, tagDuration, cover) = await Task.Run(() =>
                            {
                                var tagfile = TagLib.File.Create(file.FullPath);
                                var t = TrackTagHelper.ExtractTitle(tagfile.Tag, file.FileName);
                                var c = CoverArtService.ExtractCoverThumbnailBytes(tagfile.Tag);
                                return (t, tagfile.Properties.Duration, c);
                            });
                            title = tagTitle;
                            duration = tagDuration;
                            coverBytes = cover;
                        }
                        catch { /* tags illisibles, on garde le nom de fichier */ }

                        // Dédup : réutilise la pochette déjà extraite pour ce fichier plutôt que
                        // d'en écrire une seconde copie identique sur disque.
                        var imagePath = existing != null && !string.IsNullOrEmpty(existing.ImagePath)
                            ? existing.ImagePath
                            : coverBytes != null ? _fileService.SaveCoverThumbnail(coverBytes) : string.Empty;

                        var track = new Track
                        {
                            Title = title,
                            FilePath = filePath,
                            ImagePath = imagePath,
                            Duration = duration,
                            Hash = hash,
                            Category = category
                        };
                        await _libraryDataService.SaveLibraryItemAsync(track);

                        // N'affiche la nouvelle track dans la liste que si elle correspond au filtre actif
                        if (_categoryFilter == null || string.Equals(track.Category, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            Selection.Track(track);
                            AddTrackToGroup(track);
                            _loadedCount++;
                        }

                        imported++;
                    }
                    catch { failed++; /* fichier invalide, on passe au suivant */ }
                    finally
                    {
                        // Nettoie la copie laissée par FilePicker dans le cache Android une fois le fichier
                        // traité (copié en local ou dédupliqué) - sinon elle reste orpheline indéfiniment.
                        _fileService.DeleteIfCached(file.FullPath);
                    }

                    popupView.ViewModel.ProcessedCount++;
                }
            }
            finally
            {
                await page.ClosePopupAsync();
            }

            await RefreshCategoriesAsync();

            // Les fichiers en échec (illisibles, corrompus...) sont signalés plutôt que passés
            // sous silence : sans ça l'utilisateur ne savait pas pourquoi il manquait des pistes.
            var resultMessage = string.Format(Loc["LibImportResult"], imported, duplicates);
            if (failed > 0)
                resultMessage += "\n" + string.Format(Loc["LibImportResultErrors"], failed);
            await ShowInfoAsync(Loc["LibImport"], resultMessage);
        }

        /// <summary>
        /// Retourne null si l'utilisateur annule (l'import complet doit alors être abandonné),
        /// ou la catégorie choisie (chaîne vide si "Aucun dossier").
        /// </summary>
        private async Task<string?> PickImportCategoryAsync()
        {
            var existing = await _libraryDataService.GetCategoryNamesAsync(typeof(Track));
            var options = new[] { Loc["LibImportNoCategory"] }
                .Concat(existing)
                .Append(Loc["LibImportNewCategory"])
                .ToArray();

            var choice = await ShowActionSheetAsync(Loc["LibImportCategoryTitle"], options);
            if (choice == null)
                return null;

            if (choice == Loc["LibImportNoCategory"])
                return string.Empty;

            if (choice != Loc["LibImportNewCategory"])
                return choice;

            var newCategory = await ShowPromptAsync(Loc["LibImportNewCategory"], Loc["LibImportNewCategoryPrompt"]);
            if (string.IsNullOrWhiteSpace(newCategory))
                return string.Empty;

            var trimmed = newCategory.Trim();

            // "Aucun dossier" est un pseudo-dossier natif (Category vide), pas une vraie CategoryEntity :
            // évite de créer une catégorie réelle du même nom, qui apparaîtrait en double et ambiguë
            // partout où les catégories sont listées (sélecteur, gestion des catégories...).
            if (string.Equals(trimmed, NoFolderLabel, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            await _libraryDataService.EnsureCategoryAsync(typeof(Track), trimmed);
            return trimmed;
        }
    }
}
