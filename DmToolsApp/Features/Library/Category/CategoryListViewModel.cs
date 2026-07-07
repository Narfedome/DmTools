using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Library
{
    public partial class CategoryListViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly ILibraryDataService _libraryDataService;

        // Type de bibliothèque géré par cette instance de la page (Track, Spell...) - transmis par le
        // bouton "Gérer les catégories" de la vue appelante, les catégories sont scopées par type.
        private Type _libraryType = typeof(Track);

        public CategoryListViewModel(ILibraryDataService libraryDataService)
        {
            _libraryDataService = libraryDataService;
        }

        [ObservableProperty] private ObservableCollection<string> categoryNames = new();
        [ObservableProperty] private string? selectedCategoryName;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("LibraryType", out var value) && value is Type type)
                _libraryType = type;
        }

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(LoadAsync);
        }

        private async Task LoadAsync()
        {
            var names = await _libraryDataService.GetCategoryNamesAsync(_libraryType);
            CategoryNames = new ObservableCollection<string>(names);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedCategoryName == null) return;

            var newName = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedCategoryName);
            if (string.IsNullOrWhiteSpace(newName) || newName == SelectedCategoryName) return;

            await _libraryDataService.RenameCategoryAsync(_libraryType, SelectedCategoryName, newName.Trim());
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await LoadAsync();
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedCategoryName == null) return;
            if (!await ConfirmDeleteAsync(SelectedCategoryName)) return;

            await _libraryDataService.DeleteCategoryAsync(_libraryType, SelectedCategoryName);
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await LoadAsync();
            SelectedCategoryName = null;
        }
    }
}
