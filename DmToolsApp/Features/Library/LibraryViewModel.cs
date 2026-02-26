using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace DmToolsApp.Features.Library
{
   public partial class LibraryViewModel : ObservableObject
    {
        private readonly ILibraryPickerNavigationService _navigation;

        [ObservableProperty]
        public ObservableCollection<LibraryItem> libraryItems =new();

        [ObservableProperty]
        private LibraryItem? selectedLibraryItem;

        public LibraryViewModel(ILibraryPickerNavigationService navigation)
        {
            _navigation = navigation;

            LoadData();
        }

        private void LoadData()
        {
            LibraryItems.Add(new Track
            {
                Id = Guid.NewGuid(),
                Title = "Demo Track",
                FilePath = "E:\\tab_music\\Exhausted\\Maquette MP3\\Instru\\4_Rebirth_instru.mp3",

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
        public void AddItem()
        {
            LibraryItems.Add(new LibraryItem
            {
                Id = Guid.NewGuid(),
                Title = $"Item {LibraryItems.Count + 1}"
            });
        }
    }
}
