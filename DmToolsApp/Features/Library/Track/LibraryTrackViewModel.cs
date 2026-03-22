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
    public partial class LibraryTrackViewModel : ObservableObject
    {
        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly AudioMixerService _audioMixerService;
        private readonly FileService _fileService;

        [ObservableProperty]
        public bool isBusy = false;

        [ObservableProperty]
        public ObservableCollection<Track> trackItems = new();

        [ObservableProperty]
        private Track? selectedTrackItem;

        public LibraryTrackViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService, AudioMixerService audioMixerService, FileService fileService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            _audioMixerService = audioMixerService;
            _fileService = fileService;
            WeakReferenceMessenger.Default.Register<LibraryUpdatedMessage>(this,
            async (r, m) =>
            {
                await MainThread.InvokeOnMainThreadAsync(LoadData);
            });
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                await LoadData();
            }
            finally
            {
                IsBusy = false;
            }
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

            bool confirm = await Shell.Current.DisplayAlertAsync(
               "Delete",
               $"{item.Title} will be deleted permanentaly. Are you sure to proccessed ?",
               "Yes",
               "No");

            if (!confirm)
            {
                _fileService.DeleteTrackFromLocal(item.FilePath);
                await _libraryDataService.DeleteLibraryItem(item);

                TrackItems.Remove(item);
                SelectedTrackItem = null;
            }          
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
    }
}
