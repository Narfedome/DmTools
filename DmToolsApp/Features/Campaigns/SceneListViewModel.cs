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
        private readonly TutorialService _tutorial;
        private Campaign? _campaign;

        public SceneListViewModel(
            ISceneDataService sceneDataService,
            AudioMixerViewModel audioMixerViewModel,
            SessionStateService sessionStateService,
            TutorialService tutorial)
        {
            _sceneDataService = sceneDataService;
            _audioMixerViewModel = audioMixerViewModel;
            _sessionStateService = sessionStateService;
            _tutorial = tutorial;
            WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this,
                (r, m) => ((SceneListViewModel)r).OnPropertyChanged(nameof(PageTitle)));
        }

        public bool ShowTutorialHint => _tutorial.CurrentStep is TutorialService.StepCreateScene or TutorialService.StepLaunchScene;

        public string TutorialHintTitle => _tutorial.CurrentStep switch
        {
            TutorialService.StepCreateScene => Loc["TutorialCreateSceneTitle"],
            TutorialService.StepLaunchScene => Loc["TutorialLaunchSceneTitle"],
            _ => string.Empty
        };

        public string TutorialHintDescription => _tutorial.CurrentStep switch
        {
            TutorialService.StepCreateScene => Loc["TutorialCreateSceneDesc"],
            TutorialService.StepLaunchScene => Loc["TutorialLaunchSceneDesc"],
            _ => string.Empty
        };

        private void RefreshTutorialHint()
        {
            OnPropertyChanged(nameof(ShowTutorialHint));
            OnPropertyChanged(nameof(TutorialHintTitle));
            OnPropertyChanged(nameof(TutorialHintDescription));
        }

        [RelayCommand]
        public void SkipTutorial()
        {
            _tutorial.Skip();
            RefreshTutorialHint();
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
            RefreshTutorialHint();
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

            _tutorial.Complete(TutorialService.StepCreateScene);
            RefreshTutorialHint();
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
            _tutorial.Complete(TutorialService.StepLaunchScene);
            await _audioMixerViewModel.LoadFromPlayAsync(_campaign, Session, scene);
            _sessionStateService.SetActive(true);
            await Shell.Current.GoToAsync("//AudioMixerPage");
        }
    }
}
