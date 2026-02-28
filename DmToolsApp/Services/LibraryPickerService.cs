using DmToolsApp.Features.Library;
using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class LibraryPickerService : ILibraryPickerService
    {
        private readonly IServiceProvider _provider;

        public LibraryPickerService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<LibraryItem?> PickTrackAsync(Type libraryType)
        {
            var tcs = new TaskCompletionSource<LibraryItem?>();

            var navigationService =
                _provider.GetRequiredService<ILibraryPickerNavigationService>();

            navigationService.RegisterTaskSource(tcs);

            var page =
                _provider.GetRequiredService<LibrarySelectorPage>();


            page.LibraryType = libraryType;

            await Shell.Current.Navigation.PushModalAsync(new NavigationPage(page));

            return await tcs.Task;
        }

    }
    public interface ILibraryPickerService { Task<LibraryItem?> PickTrackAsync(Type type); }
}
