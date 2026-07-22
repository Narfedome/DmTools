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

        [ObservableProperty] private ObservableCollection<CategoryRowItem> categoryNames = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCategoryName))]
        private CategoryRowItem? selectedCategory;

        // Exposée en lecture seule pour ne pas toucher au reste de la classe (Rename/Delete, bindings
        // IsEnabled dans CategoryListPage.xaml) : la sélection réelle passe par SelectedCategory
        // (CollectionView.SelectedItem), CategoryRowItem porte en plus IsFirst (cf. son commentaire).
        public string? SelectedCategoryName => SelectedCategory?.Name;

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
            CategoryNames = new ObservableCollection<CategoryRowItem>(
                names.Select((name, index) => new CategoryRowItem(name, IsFirst: index == 0)));
        }

        [RelayCommand]
        public async Task Create()
        {
            var name = await ShowPromptAsync(Loc["DialogNewCategory"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            await _libraryDataService.EnsureCategoryAsync(_libraryType, name.Trim());
            WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());
            await LoadAsync();
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
            SelectedCategory = null;
        }
    }

    /// <summary>
    /// IsFirst (vrai seulement pour le tout premier élément de CategoryNames) : la toute première
    /// ligne de la liste n'a pas besoin de l'espace ajouté au-dessus de chaque ligne pour les séparer
    /// (cf. CategoryListPage.xaml), sans quoi ça pousse toute la liste vers le bas inutilement - même
    /// principe que CampaignRow.IsFirstCampaign dans Features/Campaigns.
    /// </summary>
    public record CategoryRowItem(string Name, bool IsFirst)
    {
        public override string ToString() => Name;
    }
}
