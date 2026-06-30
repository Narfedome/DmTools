using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.Library
{
    public partial class LibrarySpellEditViewModel
    : BaseViewModel, IQueryAttributable
    {
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;

        public LibrarySpellEditViewModel(ILibraryDataService libraryDataService,
                                        FileService fileService)
        {
            _libraryDataService = libraryDataService;
            _fileService = fileService;
        }

        [ObservableProperty]
        private Spell? item;


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Item", out var value) &&
           value is Spell item)
            {
                Item = item;
            }
        }
        [RelayCommand]
        public async Task PickFile()
        {
            try
            {
                var result = await _fileService.PickAudioFileAsync(LocalizationService.Instance["TrackSelectFile"]);

                if (result != null)
                {
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
