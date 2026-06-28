using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SceneListViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly AudioMixerViewModel _audioMixerViewModel;
        private readonly SessionStateService _sessionStateService;
        private Campaign? _campaign;

        public SceneListViewModel(
            ISceneDataService sceneDataService,
            AudioMixerViewModel audioMixerViewModel,
            SessionStateService sessionStateService)
        {
            _sceneDataService = sceneDataService;
            _audioMixerViewModel = audioMixerViewModel;
            _sessionStateService = sessionStateService;
        }

        [ObservableProperty] private Session? session;
        [ObservableProperty] private ObservableCollection<Scene> scenes = new();
        [ObservableProperty] private Scene? selectedScene;
        [ObservableProperty] private bool isBusy;

        public string PageTitle => Session != null ? $"Chapitre · {Session.Title}" : "Scènes";
        partial void OnSessionChanged(Session? value) => OnPropertyChanged(nameof(PageTitle));

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Campaign", out var c) && c is Campaign campaign)
                _campaign = campaign;
            if (query.TryGetValue("Session", out var s) && s is Session session)
            {
                Session = session;
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

        [RelayCommand]
        public async Task Launch(Scene scene)
        {
            if (_campaign == null || Session == null) return;
            await _audioMixerViewModel.LoadFromPlayAsync(_campaign, Session, scene);
            _sessionStateService.SetActive(true);
            await Shell.Current.GoToAsync("//AudioMixerPage");
        }
    }
}
