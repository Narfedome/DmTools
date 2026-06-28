using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SceneListViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;

        public SceneListViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private Session? session;
        [ObservableProperty] private ObservableCollection<Scene> scenes = new();
        [ObservableProperty] private Scene? selectedScene;
        [ObservableProperty] private bool isBusy;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Session", out var value) && value is Session s)
            {
                Session = s;
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            if (Session == null) return;
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetScenesAsync(Session.Id);
                Scenes = new ObservableCollection<Scene>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task Create()
        {
            if (Session == null) return;
            string? name = await Shell.Current.DisplayPromptAsync("Nouvelle scène", "Nom :");
            if (string.IsNullOrWhiteSpace(name)) return;

            var scene = new Scene { SessionId = Session.Id, Title = name };
            await _sceneDataService.SaveSceneAsync(scene);
            Scenes.Add(scene);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedScene == null) return;
            string? name = await Shell.Current.DisplayPromptAsync("Renommer", "Nom :", initialValue: SelectedScene.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedScene.Title = name;
            await _sceneDataService.SaveSceneAsync(SelectedScene);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedScene == null) return;
            bool ok = await Shell.Current.DisplayAlertAsync("Supprimer", $"Supprimer \"{SelectedScene.Title}\" ?", "Oui", "Non");
            if (!ok) return;

            await _sceneDataService.DeleteSceneAsync(SelectedScene);
            Scenes.Remove(SelectedScene);
            SelectedScene = null;
        }

        [RelayCommand]
        public async Task Navigate(Scene scene)
        {
            await Shell.Current.GoToAsync(nameof(SceneTracksPage),
                new Dictionary<string, object> { { "Scene", scene } });
        }
    }
}
