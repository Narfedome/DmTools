using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryViewModel : ObservableObject
    {
        private readonly ILibraryPickerNavigationService _navigation;
        private readonly ILibraryDataService _libraryDataService;


        public Type CurrentLibraryType { get; set; } = typeof(LibraryItem);

        [ObservableProperty]
        public ObservableCollection<LibraryItem> libraryItems = new();

        [ObservableProperty]
        private LibraryItem? selectedLibraryItem;

        public LibraryViewModel(ILibraryPickerNavigationService navigation, ILibraryDataService libraryDataService)
        {
            _navigation = navigation;
            _libraryDataService = libraryDataService;
            WeakReferenceMessenger.Default.Register<LibraryUpdatedMessage>(this,
            async (r, m) =>
            {
                await MainThread.InvokeOnMainThreadAsync(LoadData);
            });
        }

        public async Task InitializeAsync()
        {
            await LoadData();
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
            if (SelectedLibraryItem != null)
            {
                LibraryItems.Remove(SelectedLibraryItem);
              await  _libraryDataService.DeleteLibraryItem(SelectedLibraryItem);
                SelectedLibraryItem = null;
            }
        }

        [RelayCommand]
        public async Task EditItem()
        {
            if (SelectedLibraryItem == null)
                return;

            var copy = SelectedLibraryItem.Clone();
            await Shell.Current.GoToAsync(nameof(LibraryItemEditPage),
                new Dictionary<string, object>
                {
            { "Item", copy }
                });
        }

        [RelayCommand]
        public async Task CreateItem()
        {
            var newItem = Activator.CreateInstance(CurrentLibraryType)!;

            await Shell.Current.GoToAsync(nameof(LibraryItemEditPage),
                new Dictionary<string, object>
                {
            { "Item", newItem }
                });
        }
    }

    public class LibraryUpdatedMessage
    {
    }
}
