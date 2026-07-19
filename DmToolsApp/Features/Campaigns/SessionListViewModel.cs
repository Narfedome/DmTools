using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Extensions;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SessionListViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly TutorialService _tutorial;

        public SessionListViewModel(ISceneDataService sceneDataService, TutorialService tutorial)
        {
            _sceneDataService = sceneDataService;
            _tutorial = tutorial;
            WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this,
                (r, m) => ((SessionListViewModel)r).OnPropertyChanged(nameof(PageTitle)));
        }

        public bool ShowTutorialHint => _tutorial.CurrentStep == TutorialService.StepCreateChapter;
        public string TutorialHintTitle => Loc["TutorialCreateChapterTitle"];
        public string TutorialHintDescription => Loc["TutorialCreateChapterDesc"];

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

        [ObservableProperty] private Campaign? campaign;
        [ObservableProperty] private ObservableCollection<Session> sessions = new();
        [ObservableProperty] private Session? selectedSession;

        public string PageTitle => Campaign != null
            ? $"{Loc["NavCampaign"]} · {Campaign.Title}"
            : Loc["ChaptersHeader"];

        partial void OnCampaignChanged(Campaign? value) => OnPropertyChanged(nameof(PageTitle));

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Ne recharge pas ici : OnNavigatedTo (SessionListPage) s'en charge à chaque navigation
            // réelle. Shell ré-applique les query attributes retenus à la fermeture d'une popup
            // (modale) : un rechargement ici écrasait la liste avec l'état de la BD lu AVANT que le
            // rename/create en cours n'ait fini d'être persisté (liste figée sur l'ancien titre),
            // alors que CampaignPage (sans query attributes) ne souffrait pas de ce problème.
            if (query.TryGetValue("Campaign", out var value) && value is Campaign c)
                Campaign = c;
        }

        public async Task ReloadAsync()
        {
            if (Campaign == null) return;
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetSessionsAsync(Campaign.Id);
                Sessions = new ObservableCollection<Session>(list);
            });
            RefreshTutorialHint();
        }

        [RelayCommand]
        public async Task Create()
        {
            if (Campaign == null) return;
            string? name = await ShowPromptAsync(Loc["DialogNewChapter"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var session = new Session { CampaignId = Campaign.Id, Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveSessionAsync(session);
            Sessions.Add(session);

            _tutorial.Complete(TutorialService.StepCreateChapter);
            RefreshTutorialHint();
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedSession == null) return;
            string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedSession.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedSession.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveSessionAsync(SelectedSession);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedSession == null) return;
            if (!await ConfirmDeleteAsync(SelectedSession.Title)) return;

            await _sceneDataService.DeleteSessionAsync(SelectedSession);
            Sessions.Remove(SelectedSession);
            SelectedSession = null;
        }

        [RelayCommand]
        public async Task Navigate(Session session)
        {
            await Shell.Current.GoToAsync(nameof(SceneListPage),
                new Dictionary<string, object>
                {
                    { "Campaign", Campaign! },
                    { "Session", session }
                });
        }
    }
}
