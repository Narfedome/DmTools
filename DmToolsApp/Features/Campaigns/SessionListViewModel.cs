using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Extensions;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SessionListViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly LocalizationService _loc = LocalizationService.Instance;
        public LocalizationService Loc => _loc;

        public SessionListViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
            _loc.LanguageChanged += () => OnPropertyChanged(nameof(PageTitle));
        }

        [ObservableProperty] private Campaign? campaign;
        [ObservableProperty] private ObservableCollection<Session> sessions = new();
        [ObservableProperty] private Session? selectedSession;
        [ObservableProperty] private bool isBusy;

        public string PageTitle => Campaign != null
            ? $"{_loc.NavCampaign} · {Campaign.Title}"
            : _loc.ChaptersHeader;

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
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetSessionsAsync(Campaign.Id);
                Sessions = new ObservableCollection<Session>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task Create()
        {
            if (Campaign == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(_loc.DialogNewChapter, _loc.PromptName);
            if (string.IsNullOrWhiteSpace(name)) return;

            var session = new Session { CampaignId = Campaign.Id, Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveSessionAsync(session);
            Sessions.Add(session);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedSession == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(_loc.DialogRename, _loc.PromptName, initialValue: SelectedSession.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedSession.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveSessionAsync(SelectedSession);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedSession == null) return;
            bool ok = await Shell.Current.DisplayAlertAsync(
                _loc.DialogDelete,
                string.Format(_loc.DialogDeleteConfirm, SelectedSession.Title),
                _loc.DialogYes,
                _loc.DialogNo);
            if (!ok) return;

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
