using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class LibraryPickerNavigationService : ILibraryPickerNavigationService
    {
        private readonly TaskCompletionSource<LibraryItem?> _tcs;

        public LibraryPickerNavigationService(TaskCompletionSource<LibraryItem?> tcs)
        {
            _tcs = tcs;
        }

        public async Task ClosePickerAsync(LibraryItem? result)
        {
            _tcs.TrySetResult(result);
            await Shell.Current.Navigation.PopModalAsync();
        }
    }
    public interface ILibraryPickerNavigationService
    {
        Task ClosePickerAsync(LibraryItem? result);
    }
}
