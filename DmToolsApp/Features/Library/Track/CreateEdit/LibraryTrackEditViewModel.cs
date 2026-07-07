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
                    Item.Title = FileService.ExtractTitle(tagfile.Tag, result.FileName);
                    Item.Duration = tagfile.Properties.Duration;
                    ImportedFilePath = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
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
                // Déduplication par hash : réutilise le fichier existant si le contenu est déjà en librairie
                var hash = FileService.ComputeSha256(ImportedFilePath);
                var existing = await _libraryDataService.FindTrackByHashAsync(hash, Item.Id);

                Item.FilePath = existing != null
                    ? existing.FilePath
                    : _trackFileService.CopyTrackToLocal(ImportedFilePath);
                Item.Hash = hash;
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
