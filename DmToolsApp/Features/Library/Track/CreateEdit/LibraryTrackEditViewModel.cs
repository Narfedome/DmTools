using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryTrackEditViewModel
    : BaseViewModel, IQueryAttributable
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
                var loc = LocalizationService.Instance;
                Title = Item.Id != 0 ? loc["TrackEditTitle"] : loc["TrackCreateTitle"];

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
                var result = await _trackFileService.PickAudioFileAsync(LocalizationService.Instance["TrackSelectFile"]);

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
                await Shell.Current.DisplayAlertAsync(Loc["ErrorTitle"], ex.Message, "OK");
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
