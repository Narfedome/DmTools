using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Play
{
    public partial class PlayCampaignViewModel : ObservableObject
    {
        private readonly ISceneDataService _sceneDataService;

        public PlayCampaignViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private ObservableCollection<Campaign> campaigns = new();
        [ObservableProperty] private bool isBusy;

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetCampaignsAsync();
                Campaigns = new ObservableCollection<Campaign>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task Play(Campaign campaign)
        {
            await Shell.Current.GoToAsync(nameof(PlaySessionPage),
                new Dictionary<string, object> { { "Campaign", campaign } });
        }
    }
}
