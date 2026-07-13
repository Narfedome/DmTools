using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Extensions;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SceneListViewModel : BaseViewModel, IQueryAttributable
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
            WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this,
                (r, m) => ((SceneListViewModel)r).OnPropertyChanged(nameof(PageTitle)));
        }

        [ObservableProperty] private Session? session;
        [ObservableProperty] private ObservableCollection<Scene> scenes = new();
        [ObservableProperty] private Scene? selectedScene;

        public string PageTitle => Session != null
            ? $"{Loc["NavChapter"]} · {Session.Title}"
            : Loc["ScenesHeader"];

        partial void OnSessionChanged(Session? value) => OnPropertyChanged(nameof(PageTitle));

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Ne recharge pas ici : OnNavigatedTo (SceneListPage) s'en charge à chaque navigation
            // réelle. Shell ré-applique les query attributes retenus à la fermeture d'une popup
            // (modale) : un rechargement ici écrasait la liste avec l'état de la BD lu AVANT que le
            // rename/create en cours n'ait fini d'être persisté (liste figée sur l'ancien titre),
            // alors que CampaignPage (sans query attributes) ne souffrait pas de ce problème.
            if (query.TryGetValue("Campaign", out var c) && c is Campaign campaign)
                _campaign = campaign;
            if (query.TryGetValue("Session", out var s) && s is Session session)
                Session = session;
        }

        public async Task ReloadAsync()
        {
            if (Session == null) return;
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetScenesAsync(Session.Id);
                Scenes = new ObservableCollection<Scene>(list);
            });
        }

        [RelayCommand]
        public async Task Create()
        {
            if (Session == null) return;
            string? name = await ShowPromptAsync(Loc["DialogNewScene"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var scene = new Scene { SessionId = Session.Id, Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveSceneAsync(scene);
            Scenes.Add(scene);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedScene == null) return;
            string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedScene.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedScene.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveSceneAsync(SelectedScene);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedScene == null) return;
            if (!await ConfirmDeleteAsync(SelectedScene.Title)) return;

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
