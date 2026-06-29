using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Play
{
    public partial class PlaySessionViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        public LocalizationService Loc => LocalizationService.Instance;

        public PlaySessionViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private Campaign? campaign;
        [ObservableProperty] private ObservableCollection<Session> sessions = new();
        [ObservableProperty] private bool isBusy;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Campaign", out var c) && c is Campaign campaign)
            {
                Campaign = campaign;
                _ = LoadAsync(campaign.Id);
            }
        }

        private async Task LoadAsync(int campaignId)
        {
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetSessionsAsync(campaignId);
                Sessions = new ObservableCollection<Session>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task Navigate(Session session)
        {
            await Shell.Current.GoToAsync(nameof(PlayScenePage),
                new Dictionary<string, object>
                {
                    { "Campaign", Campaign! },
                    { "Session", session }
                });
        }
    }
}
