using DmToolsApp.Features.Library;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class LibraryPickerService : ILibraryPickerService
    {
        private TaskCompletionSource<LibraryItem?>? _tcs;

        public async Task<LibraryItem?> PickTrackAsync()
        {
            _tcs = new TaskCompletionSource<LibraryItem?>();

            var page = new NavigationPage(
                new LibrarySelectorPage(_tcs));

            await Shell.Current.Navigation.PushModalAsync(page);

            return await _tcs.Task;
        }


    }
    public interface ILibraryPickerService { Task<LibraryItem?> PickTrackAsync(); }
}
