using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class CategoryListViewModel : BaseViewModel
    {
        private readonly ILibraryDataService _libraryDataService;

        public CategoryListViewModel(ILibraryDataService libraryDataService)
        {
            _libraryDataService = libraryDataService;
        }

        [ObservableProperty] private ObservableCollection<string> categoryNames = new();
        [ObservableProperty] private string? selectedCategoryName;

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(LoadAsync);
        }

        private async Task LoadAsync()
        {
            var names = await _libraryDataService.GetCategoryNamesAsync();
            CategoryNames = new ObservableCollection<string>(names);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedCategoryName == null) return;

            var newName = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedCategoryName);
            if (string.IsNullOrWhiteSpace(newName) || newName == SelectedCategoryName) return;

            await _libraryDataService.RenameCategoryAsync(SelectedCategoryName, newName.Trim());
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await LoadAsync();
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedCategoryName == null) return;
            if (!await ConfirmDeleteAsync(SelectedCategoryName)) return;

            await _libraryDataService.DeleteCategoryAsync(SelectedCategoryName);
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await LoadAsync();
            SelectedCategoryName = null;
        }
    }
}
