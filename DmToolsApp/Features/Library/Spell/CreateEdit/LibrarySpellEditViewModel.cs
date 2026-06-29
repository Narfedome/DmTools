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
    public partial class LibrarySpellEditViewModel
    : ObservableObject, IQueryAttributable
    {
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;

        public DmToolsApp.Services.LocalizationService Loc => DmToolsApp.Services.LocalizationService.Instance;

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
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = DmToolsApp.Services.LocalizationService.Instance.TrackSelectFile,
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
