using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Extensions;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class CampaignViewModel : ObservableObject
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly LocalizationService _loc = LocalizationService.Instance;
        public LocalizationService Loc => _loc;

        public CampaignViewModel(ISceneDataService sceneDataService)
        {
            _sceneDataService = sceneDataService;
        }

        [ObservableProperty] private ObservableCollection<Campaign> campaigns = new();
        [ObservableProperty] private Campaign? selectedCampaign;
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
        public async Task Create()
        {
            string? name = await Shell.Current.DisplayPromptAsync(_loc["DialogNewCampaign"], _loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var campaign = new Campaign { Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveCampaignAsync(campaign);
            Campaigns.Add(campaign);
        }

        [RelayCommand]
        public async Task Rename()
        {
            if (SelectedCampaign == null) return;
            string? name = await Shell.Current.DisplayPromptAsync(_loc["DialogRename"], _loc["PromptName"], initialValue: SelectedCampaign.Title);
            if (string.IsNullOrWhiteSpace(name)) return;

            SelectedCampaign.Title = name.CapitalizeFirst();
            await _sceneDataService.SaveCampaignAsync(SelectedCampaign);
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (SelectedCampaign == null) return;
            bool ok = await Shell.Current.DisplayAlertAsync(
                _loc["DialogDelete"],
                string.Format(_loc["DialogDeleteConfirm"], SelectedCampaign.Title),
                _loc["DialogYes"],
                _loc["DialogNo"]);
            if (!ok) return;

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
