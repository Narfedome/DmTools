using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryTrackEditViewModel
    : ObservableObject, IQueryAttributable
    {
        private readonly AudioPlayerService _audioPlayerService;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _trackFileService;


        public LibraryTrackEditViewModel(AudioPlayerService audioPlayerService, ILibraryDataService libraryDataService,
                                        FileService trackFileService)
        {
            _audioPlayerService = audioPlayerService;
            _libraryDataService = libraryDataService;
            _trackFileService = trackFileService;
        }

        [ObservableProperty]
        private Track item = new Track();

        [ObservableProperty]
        private string importedFilePath = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Item", out var value) &&
           value is Track item)
            {
                Item = item;
                if(Item.Id != 0)
                {
                    Title = "Edit Track";
                }
                else
                {
                    Title = "Create Track";
                }

            }
        }
        public void StopAudio()
        {
            _audioPlayerService?.Stop();
        }

        [RelayCommand]
        public async Task PickFile()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select audio file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.iOS, new[] { "public.audio" } },
                            { DevicePlatform.Android, new[] { "audio/*" } },
                            { DevicePlatform.WinUI, new[] { ".mp3", ".wav", ".m4a" } },
                            { DevicePlatform.MacCatalyst, new[] { "public.audio" } }
                        })
                });

                if (result != null)
                {
                    var tagfile = TagLib.File.Create(result.FullPath);
                    Item.FilePath = result.FullPath;
                    Item.Title = string.IsNullOrEmpty(tagfile.Name) ? result.FileName : $"{tagfile.Tag.FirstAlbumArtist} - {tagfile.Tag.Title} ";
                    Item.Duration = tagfile.Properties.Duration;
                    ImportedFilePath = result.FullPath;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        [RelayCommand]
        public async Task Save()
        {
            if (Item == null)
                return;

            // Si c'est un Track et que le fichier a été choisi par l'utilisateur
            if (!string.IsNullOrEmpty(ImportedFilePath))
            {
                // Copie le fichier dans le dossier privé
                Item.FilePath = _trackFileService.CopyTrackToLocal(ImportedFilePath);
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
