using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Extensions;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class CampaignViewModel : BaseViewModel
    {
        private readonly ISceneDataService _sceneDataService;

        public CampaignViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private ObservableCollection<Campaign> campaigns = new();
        [ObservableProperty] private Campaign? selectedCampaign;

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetCampaignsAsync();
                Campaigns = new ObservableCollection<Campaign>(list);
            });
        }

        [RelayCommand]
        public async Task Create()
        {
            string? name = await ShowPromptAsync(Loc["DialogNewCampaign"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var campaign = new Campaign { Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveCampaignAsync(campaign);
            Campaigns.Add(campaign);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedCampaign == null) return;
            string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: SelectedCampaign.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedCampaign.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveCampaignAsync(SelectedCampaign);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedCampaign == null) return;
            if (!await ConfirmDeleteAsync(SelectedCampaign.Title)) return;

            await _sceneDataService.DeleteCampaignAsync(SelectedCampaign);
            Campaigns.Remove(SelectedCampaign);
            SelectedCampaign = null;
        }

        [RelayCommand]
        public async Task Navigate(Campaign campaign)
        {
            await Shell.Current.GoToAsync(nameof(SessionListPage),
                new Dictionary<string, object> { { "Campaign", campaign } });
        }
    }
}
