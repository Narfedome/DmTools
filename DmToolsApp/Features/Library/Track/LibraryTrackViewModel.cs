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
        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;
        private readonly AudioPlayerService _audioPlayerService;

        [ObservableProperty]
        public ObservableCollection<Track> trackItems = new();

        [ObservableProperty]
        private Track? selectedTrackItem;

        public LibraryTrackViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService, AudioPlayerService audioPlayerService, FileService fileService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            _audioPlayerService  = audioPlayerService;
            _fileService = fileService;
            WeakReferenceMessenger.Default.Register<LibraryUpdatedMessage>(this,
            async (r, m) =>
            {
                await MainThread.InvokeOnMainThreadAsync(LoadData);
            });
        }

        // Stop any playing audio when leaving the view
        public void StopAudio()
        {
            _audioPlayerService?.Stop();
        }

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(LoadData);
        }

        private async Task LoadData()
        {
            var items = await _libraryDataService.GetAllItemsTypeAsync(typeof(Track));

            TrackItems.Clear();

            foreach (var item in items)
                TrackItems.Add((Track)item);

            SelectedTrackItem = TrackItems.FirstOrDefault();
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
            _fileService.DeleteTrackFromLocal(item.FilePath);
            TrackItems.Remove(item);
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
    }
}
