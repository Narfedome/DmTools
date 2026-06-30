using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Extensions;
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
        private readonly LocalizationService _loc = LocalizationService.Instance;
        public LocalizationService Loc => _loc;
        private Campaign? _campaign;

        public SceneListViewModel(
            ISceneDataService sceneDataService,
            AudioMixerViewModel audioMixerViewModel,
            SessionStateService sessionStateService)
        {
            _sceneDataService = sceneDataService;
            _audioMixerViewModel = audioMixerViewModel;
            _sessionStateService = sessionStateService;
            _loc.LanguageChanged += () => OnPropertyChanged(nameof(PageTitle));
        }

        [ObservableProperty] private Session? session;
        [ObservableProperty] private ObservableCollection<Scene> scenes = new();
        [ObservableProperty] private Scene? selectedScene;
        [ObservableProperty] private bool isBusy;

        public string PageTitle => Session != null
            ? $"{_loc["NavChapter"]} · {Session.Title}"
            : _loc["ScenesHeader"];

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
            string? name = await Shell.Current.DisplayPromptAsync(_loc["DialogNewScene"], _loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var scene = new Scene { SessionId = Session.Id, Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveSceneAsync(scene);
            Scenes.Add(scene);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedScene == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(_loc["DialogRename"], _loc["PromptName"], initialValue: SelectedScene.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedScene.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveSceneAsync(SelectedScene);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedScene == null) return;
            bool ok = await Shell.Current.DisplayAlertAsync(
                _loc["DialogDelete"],
                string.Format(_loc["DialogDeleteConfirm"], SelectedScene.Title),
                _loc["DialogYes"],
                _loc["DialogNo"]);
            if (!ok) return;

            await _sceneDataService.DeleteSceneAsync(SelectedScene);
            Scenes.Remove(SelectedScene);
            SelectedScene = null;
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
