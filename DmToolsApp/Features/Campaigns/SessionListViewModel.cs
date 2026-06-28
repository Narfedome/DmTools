using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SessionListViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;

        public SessionListViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private Campaign? campaign;
        [ObservableProperty] private ObservableCollection<Session> sessions = new();
        [ObservableProperty] private Session? selectedSession;
        [ObservableProperty] private bool isBusy;

        public string PageTitle => Campaign != null ? $"Campagne · {Campaign.Title}" : "Chapitres";
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
            string? name = await Shell.Current.DisplayPromptAsync("Nouveau chapitre", "Nom :");
            if (string.IsNullOrWhiteSpace(name)) return;

            var session = new Session { CampaignId = Campaign.Id, Title = name };
            await _sceneDataService.SaveSessionAsync(session);
            Sessions.Add(session);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedSession == null) return;
            string? name = await Shell.Current.DisplayPromptAsync("Renommer", "Nom :", initialValue: SelectedSession.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedSession.Title = name;
            await _sceneDataService.SaveSessionAsync(SelectedSession);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedSession == null) return;
            bool ok = await Shell.Current.DisplayAlertAsync("Supprimer", $"Supprimer \"{SelectedSession.Title}\" ?", "Oui", "Non");
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
