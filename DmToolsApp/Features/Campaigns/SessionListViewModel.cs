using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Extensions;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SessionListViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;

        public SessionListViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
            Loc.LanguageChanged += () => OnPropertyChanged(nameof(PageTitle));
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
            if (query.TryGetValue("Campaign", out var value) && value is Campaign c)
            {
                Campaign = c;
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            if (Campaign == null) return;
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetSessionsAsync(Campaign.Id);
                Sessions = new ObservableCollection<Session>(list);
            });
        }

        [RelayCommand]
        public async Task Create()
        {
            if (Campaign == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(Loc["DialogNewChapter"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var session = new Session { CampaignId = Campaign.Id, Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveSessionAsync(session);
            Sessions.Add(session);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedSession == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedSession.Title);
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
