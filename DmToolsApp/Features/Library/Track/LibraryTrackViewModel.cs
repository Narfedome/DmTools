using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
        private const int PageSize = 9;
        private readonly string[] DefaultCategories;

        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;
        private readonly AudioPlayerService _audioPlayerService;

        private int _loadedCount;
        private bool _hasMoreItems = true;
        private bool _isLoadingMore;
        private bool _suppressCategoryReload;
        private string? _categoryFilter;

        private string AllCategoriesLabel => Loc["LibAllCategories"];

        [ObservableProperty]
        public ObservableCollection<Track> trackItems = new();

        [ObservableProperty]
        private Track? selectedTrackItem;

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
            DefaultCategories =  new string[] { LocalizationService.Instance["LibCategoryMusic"], LocalizationService.Instance["LibCategoryAmbience"], LocalizationService.Instance  ["LibCategorySFX"] };  
            _libraryDataService = libraryDataService;     
            _audioPlayerService = audioPlayerService;
            _fileService = fileService;
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
            var existing = await _libraryDataService.GetDistinctTrackCategoriesAsync();
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

            TrackItems.Clear();
            _loadedCount = 0;
            _hasMoreItems = true;
            SelectedTrackItem = null;

            await LoadNextPageAsync();
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
                    TrackItems.Add((Track)item);

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
        public async Task DeleteItem()
        {
            if (SelectedTrackItem == null)
                return;

            var item = SelectedTrackItem;

            if (!await ConfirmAsync(Loc["DialogDelete"], string.Format(Loc["DialogDeleteTrackConfirm"], item.Title))) return;

            StopAudio();
            await _libraryDataService.DeleteLibraryItem(item);

            // Ne supprime le fichier physique que si aucune autre track ne le référence encore (dédup)
            var remainingRefs = await _libraryDataService.CountTracksWithFilePathAsync(item.FilePath, item.Id);
            if (remainingRefs == 0)
                _fileService.DeleteTrackFromLocal(item.FilePath);

            TrackItems.Remove(item);
            _loadedCount--;
            SelectedTrackItem = null;
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

        private async Task<string> PickImportCategoryAsync()
        {
            var existing = await _libraryDataService.GetDistinctTrackCategoriesAsync();
            var options = DefaultCategories
                .Union(existing, StringComparer.OrdinalIgnoreCase)
                .Append(Loc["LibImportNewCategory"])
                .ToArray();

            var choice = await ShowActionSheetAsync(Loc["LibImportCategoryTitle"], options);
            if (choice == null)
                return string.Empty;

            if (choice != Loc["LibImportNewCategory"])
                return choice;

            var newCategory = await ShowPromptAsync(Loc["LibImportNewCategory"], Loc["LibImportNewCategoryPrompt"]);
            return string.IsNullOrWhiteSpace(newCategory) ? string.Empty : newCategory.Trim();
        }
    }
}
