using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryViewModel : ObservableObject
    {
        private readonly ILibraryPickerNavigationService _navigation;

        public Type CurrentLibraryType { get; set; } = typeof(LibraryItem);

        [ObservableProperty]
        public ObservableCollection<LibraryItem> libraryItems = new();

        [ObservableProperty]
        private LibraryItem? selectedLibraryItem;

        public LibraryViewModel(ILibraryPickerNavigationService navigation)
        {
            _navigation = navigation;

            LoadData();

            if(LibraryItems.Count >0)
            {
                SelectedLibraryItem = LibraryItems.FirstOrDefault();
            }
        }

        private void LoadData()
        {
            LibraryItems.Add(new Track
            {
                Id = Guid.NewGuid(),
                Title = "Demo Track",
                FilePath = "E:\\tab_music\\Exhausted\\Maquette MP3\\Instru\\4_Rebirth_instru.mp3",
                ImagePath = "E:\\JDR\\Dnd\\Ressources\\Token\\dragonnet bronze - token.png"

            });
            LibraryItems.Add(new Spell
            {
                Id = Guid.NewGuid(),
                Title = "Demo Spell 2",
               // FilePath = "E:\\tab_music\\Exhausted\\Maquette MP3\\Instru\\2_Impurity_instru.mp3",
                ImagePath = "E:\\JDR\\Dnd\\Ressources\\Token\\Guivre follette - token.png"

            });
            LibraryItems.Add(new Spell
            {
                Id = Guid.NewGuid(),
                Title = "Demo Spell 3",
               // FilePath = "E:\\tab_music\\Exhausted\\Maquette MP3\\Instru\\5_Fall_instru.mp3",
                ImagePath = "E:\\JDR\\Dnd\\Ressources\\Token\\kobold ailé - token.png"

            });
            LibraryItems.Add(new Track
            {
                Id = Guid.NewGuid(),
                Title = "Demo Track 44",
                FilePath = "E:\\tab_music\\Exhausted\\Maquette MP3\\Instru\\1_Silent_instru.mp3",
                ImagePath = "E:\\JDR\\Dnd\\Ressources\\Token\\dragonnet bleu - token.png"

            });
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
        public void DeleteItem()
        {
            if (SelectedLibraryItem != null)
            {
                LibraryItems.Remove(SelectedLibraryItem);
                SelectedLibraryItem = null;
            }
        }

        [RelayCommand]
        public async Task EditItem()
        {
            if (SelectedLibraryItem == null)
                return;

            await Shell.Current.GoToAsync(nameof(LibraryItemEditPage),
                new Dictionary<string, object>
                {
            { "Item", SelectedLibraryItem }
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
}
