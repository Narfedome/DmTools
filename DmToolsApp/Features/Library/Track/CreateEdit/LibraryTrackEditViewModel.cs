using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class LibraryTrackEditViewModel
    : BaseViewModel, IQueryAttributable
    {
        private readonly AudioPlayerService _audioPlayerService;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _trackFileService;

        // Pochette extraite au moment du choix du fichier (PickFile), persistée sur disque à la
        // sauvegarde (Save) une fois le chemin final de la track connu (copie locale ou dédup).
        private byte[]? _pendingCoverBytes;

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

        [ObservableProperty]
        private ObservableCollection<string> categories = new();

        public string SelectedCategory
        {
            get => string.IsNullOrEmpty(Item.Category) ? Loc["LibImportNoCategory"] : Item.Category;
            set => Item.Category = value == Loc["LibImportNoCategory"] ? string.Empty : value;
        }

        public async Task InitializeAsync()
        {
            var names = await _libraryDataService.GetCategoryNamesAsync(typeof(Track));
            Categories = new ObservableCollection<string>(new[] { Loc["LibImportNoCategory"] }.Concat(names));
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Item", out var value) &&
           value is Track item)
            {
                Item = item;
                var loc = LocalizationService.Instance;
                Title = Item.Id != 0 ? loc["TrackEditTitle"] : loc["TrackCreateTitle"];
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }
        public void StopAudio()
        {
            _audioPlayerService?.Stop();
        }

        [RelayCommand]
        public async Task SelectCategory()
        {
            var result = await ShowActionSheetAsync(Loc["LibImportCategoryTitle"], Categories.ToArray());
            if (result == null)
                return;

            SelectedCategory = result;
            OnPropertyChanged(nameof(SelectedCategory));
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
                    Item.Title = TrackTagHelper.ExtractTitle(tagfile.Tag, result.FileName);
                    Item.Duration = tagfile.Properties.Duration;
                    _pendingCoverBytes = CoverArtService.ExtractCoverThumbnailBytes(tagfile.Tag);
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
                var hash = TrackTagHelper.ComputeSha256(ImportedFilePath);
                var existing = await _libraryDataService.FindTrackByHashAsync(hash, Item.Id);

                Item.FilePath = existing != null
                    ? existing.FilePath
                    : _trackFileService.CopyTrackToLocal(ImportedFilePath);
                Item.Hash = hash;

                // Dédup : réutilise la pochette déjà extraite pour ce fichier plutôt que d'en
                // écrire une seconde copie identique sur disque.
                Item.ImagePath = existing != null && !string.IsNullOrEmpty(existing.ImagePath)
                    ? existing.ImagePath
                    : _pendingCoverBytes != null ? _trackFileService.SaveCoverThumbnail(_pendingCoverBytes) : string.Empty;

                // Nettoie la copie laissée par FilePicker dans le cache Android maintenant que le fichier
                // est traité (copié en local ou dédupliqué) - sinon elle reste orpheline indéfiniment.
                _trackFileService.DeleteIfCached(ImportedFilePath);
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
