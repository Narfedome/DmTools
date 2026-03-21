using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryItemEditViewModel
    : ObservableObject, IQueryAttributable
    {
        private readonly ILibraryDataService _libraryDataService;
        private readonly TrackFileService _trackFileService;

        public LibraryItemEditViewModel(ILibraryDataService libraryDataService,
                                        TrackFileService trackFileService)
        {
            _libraryDataService = libraryDataService;
            _trackFileService = trackFileService;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTrack))]
        [NotifyPropertyChangedFor(nameof(IsSpell))]
        private LibraryItem? item;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Item", out var value) &&
           value is LibraryItem item)
            {
                Item = item;
            }
        }

        public bool IsTrack => Item is Track;
        public bool IsSpell => Item is Spell;

        [RelayCommand]
        public async Task Save()
        {
            if (Item == null)
                return;

            // Si c'est un Track et que le fichier a été choisi par l'utilisateur
            if (Item is Track track && !string.IsNullOrEmpty(track.FilePath))
            {
                // Copie le fichier dans le dossier privé
                track.FilePath = _trackFileService.CopyToLocal(track.FilePath);
            }

            await _libraryDataService.SaveLibraryItemAsync(Item);
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
