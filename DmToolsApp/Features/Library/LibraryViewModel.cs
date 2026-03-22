using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components.AudioButton;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryViewModel : ObservableObject
    {
        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;
        private readonly AudioMixerService _audioMixerService;

        [ObservableProperty]
        public bool isBusy = false;


        public Type CurrentLibraryType { get; set; } = typeof(LibraryItem);

        [ObservableProperty]
        public ObservableCollection<LibraryItem> libraryItems = new();

        [ObservableProperty]
        private LibraryItem? selectedLibraryItem;

        public LibraryViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService, AudioMixerService audioMixerService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            _audioMixerService = audioMixerService;
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
            var items = await _libraryDataService.GetAllItemsTypeAsync(CurrentLibraryType);

            LibraryItems.Clear();

            foreach (var item in items)
                LibraryItems.Add(item);

            SelectedLibraryItem = LibraryItems.FirstOrDefault();
        }

        [RelayCommand]
        public async Task ConfirmSelection()
        {
            if (SelectedLibraryItem is LibraryItem libraryItem)
                await _navigation.ClosePickerAsync(libraryItem);
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await _navigation.ClosePickerAsync(null);
        }

        [RelayCommand]
        public async Task DeleteItem()
        {
            if (SelectedLibraryItem == null)
                return;

            var item = SelectedLibraryItem;

            await _libraryDataService.DeleteLibraryItem(item);

            LibraryItems.Remove(item);

            SelectedLibraryItem = null;
        }

        [RelayCommand]
        public async Task EditItem()
        {
            if (SelectedLibraryItem == null)
                return;

            var copy = SelectedLibraryItem.Clone();
            if (CurrentLibraryType == typeof(Track))
            {
                await Shell.Current.GoToAsync(nameof(LibraryTrackEditPage),
                new Dictionary<string, object>
                {
                 { "Item", (Track)copy }
                });
            }
            if (CurrentLibraryType == typeof(Spell))
            {
                await Shell.Current.GoToAsync(nameof(LibrarySpellEditPage),
                new Dictionary<string, object>
                {
                 { "Item", (Spell)copy }
                });
            }


        }

        [RelayCommand]
        public async Task CreateItem()
        {
            var newItem = Activator.CreateInstance(CurrentLibraryType)!;

            if (CurrentLibraryType == typeof(Track))
            {
                await Shell.Current.GoToAsync(nameof(LibraryTrackEditPage),
                    new Dictionary<string, object>
                    {
                       { "Item", (Track)newItem }
                    });
            }
            if (CurrentLibraryType == typeof(Spell))
            {
                await Shell.Current.GoToAsync(nameof(LibrarySpellEditPage),
                    new Dictionary<string, object>
                    {
                       { "Item", (Spell)newItem }
                    });
            }
        }
    }

    public class LibraryUpdatedMessage
    {
    }
}
