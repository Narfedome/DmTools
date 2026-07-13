using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using TagLib.Matroska;
using Track = DmToolsApp.Models.Library.Track;

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

        [ObservableProperty]
        public ObservableCollection<Track> trackItems = new();

        public bool HasTrackItems => TrackItems.Count > 0;

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

            _ = ReloadAsync();
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
            foreach (var c in existing)
                Categories.Add(c);

            SelectedCategory = Categories.Contains(previousSelection) ? previousSelection : AllCategoriesLabel;

            _suppressCategoryReload = false;
        }

        private async Task ReloadAsync()
        {
            _categoryFilter = SelectedCategory == AllCategoriesLabel ? null : SelectedCategory;

            ClearTrackItems();
            _loadedCount = 0;
            _hasMoreItems = true;
            SelectedTrackItem = null;

            // Changer de catégorie change le contexte de sélection multiple : on repart de zéro
            // pour éviter de supprimer par erreur des pistes qu'on ne voit plus.
            Selection.Clear();

            await LoadNextPageAsync();
        }

        private void ClearTrackItems()
        {
            foreach (var t in TrackItems)
                Selection.Untrack(t);

            TrackItems.Clear();
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
                    TrackItems.Add(track);

                    // Laisse la main au thread UI entre chaque tuile (traitement des messages Windows -
                    // dont les taps sur les autres onglets) plutôt que d'enchaîner les 12 ajouts et
                    // réalisations visuelles en un seul bloc synchrone ininterrompu.
                    await Task.Yield();
                }

                _loadedCount += items.Count;
                _hasMoreItems = items.Count == PageSize;

                if (SelectedTrackItem == null)
                    SelectedTrackItem = TrackItems.FirstOrDefault();
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

            // Bascule : si tout est déjà sélectionné, un nouvel appui tout désélectionne.
            if (ids.Count > 0 && Selection.ContainsAll(ids))
                Selection.DeselectAll(TrackItems);
            else
                Selection.SelectIds(ids, TrackItems);
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
                ? string.Format(Loc["DialogDeleteTrackConfirm"], (TrackItems.FirstOrDefault(t => t.Id == ids[0]) ?? SelectedTrackItem)?.Title)
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

            foreach (var item in TrackItems.Where(t => ids.Contains(t.Id)).ToList())
            {
                Selection.Untrack(item);
                TrackItems.Remove(item);
                _loadedCount--;
            }

            Selection.Clear();
            SelectedTrackItem = TrackItems.FirstOrDefault();

            await RefreshCategoriesAsync();
        }

        [RelayCommand]
        public async Task EditItem()
        {
            if (SelectedTrackItem == null)
                return;

            var copy = SelectedTrackItem.Clone();
            await Shell.Current.GoToAsync(nameof(LibraryTrackEditPage),
            new Dictionary<string, object>
            {
                 { "Item", copy }
            });
        }

        [RelayCommand]
        public async Task CreateItem()
        {
            var newItem = new Track();

            await Shell.Current.GoToAsync(nameof(LibraryTrackEditPage),
                new Dictionary<string, object>
                {
                       { "Item", newItem }
                });
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

            try
            {
                foreach (var file in files)
                {
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
                            TrackItems.Add(track);
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
            await _libraryDataService.EnsureCategoryAsync(typeof(Track), trimmed);
            return trimmed;
        }
    }
}
