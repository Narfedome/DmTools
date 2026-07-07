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
        private const int PageSize = 12;

        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;
        private readonly AudioPlayerService _audioPlayerService;

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

        public LibraryTrackViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService, AudioPlayerService audioPlayerService, FileService fileService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            _audioPlayerService = audioPlayerService;
            _fileService = fileService;
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

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(async () =>
            {
                await RefreshCategoriesAsync();
                await ReloadAsync();
            });
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

            _isLoadingMore = true;
            IsLoadingMore = true;

            try
            {
                var items = await _libraryDataService.GetItemsPageAsync(typeof(Track), _loadedCount, PageSize, _categoryFilter);

                foreach (var item in items)
                {
                    var track = (Track)item;
                    Selection.Track(track);
                    TrackItems.Add(track);
                }

                _loadedCount += items.Count;
                _hasMoreItems = items.Count == PageSize;

                if (SelectedTrackItem == null)
                    SelectedTrackItem = TrackItems.FirstOrDefault();
            }
            finally
            {
                _isLoadingMore = false;
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

            foreach (var track in deleted.OfType<Track>())
            {
                // Ne supprime le fichier physique que si aucune autre track ne le référence encore (dédup)
                var remainingRefs = await _libraryDataService.CountTracksWithFilePathAsync(track.FilePath, track.Id);
                if (remainingRefs == 0)
                    _fileService.DeleteTrackFromLocal(track.FilePath);
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
                        var hash = await Task.Run(() => FileService.ComputeSha256(file.FullPath));
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
                        try
                        {
                            var (tagTitle, tagDuration) = await Task.Run(() =>
                            {
                                var tagfile = TagLib.File.Create(file.FullPath);
                                var t = FileService.ExtractTitle(tagfile.Tag, file.FileName);
                                return (t, tagfile.Properties.Duration);
                            });
                            title = tagTitle;
                            duration = tagDuration;
                        }
                        catch { /* tags illisibles, on garde le nom de fichier */ }

                        var track = new Track
                        {
                            Title = title,
                            FilePath = filePath,
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
                    catch { /* fichier invalide, on passe au suivant */ }

                    popupView.ViewModel.ProcessedCount++;
                }
            }
            finally
            {
                await page.ClosePopupAsync();
            }

            await RefreshCategoriesAsync();
            await ShowInfoAsync(Loc["LibImport"], string.Format(Loc["LibImportResult"], imported, duplicates));
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
