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
        private readonly TutorialService _tutorial;

        public CampaignViewModel(ISceneDataService sceneDataService, TutorialService tutorial)
        {
            _sceneDataService = sceneDataService;
            _tutorial = tutorial;
        }

        [ObservableProperty] private ObservableCollection<Campaign> campaigns = new();
        [ObservableProperty] private Campaign? selectedCampaign;

        public bool ShowTutorialHint => _tutorial.CurrentStep is TutorialService.StepCreateCampaign or TutorialService.StepOpenCampaign;

        public string TutorialHintTitle => _tutorial.CurrentStep switch
        {
            TutorialService.StepCreateCampaign => Loc["TutorialCreateCampaignTitle"],
            TutorialService.StepOpenCampaign => Loc["TutorialOpenCampaignTitle"],
            _ => string.Empty
        };

        public string TutorialHintDescription => _tutorial.CurrentStep switch
        {
            TutorialService.StepCreateCampaign => Loc["TutorialCreateCampaignDesc"],
            TutorialService.StepOpenCampaign => Loc["TutorialOpenCampaignDesc"],
            _ => string.Empty
        };

        // Fait ressortir la flèche > de chaque campagne pendant l'étape "ouvrez votre campagne" :
        // le texte de la bulle seule ne dit pas QUEL élément taper dans une liste à plusieurs lignes.
        public bool ShowOpenHint => _tutorial.CurrentStep == TutorialService.StepOpenCampaign;

        private void RefreshTutorialHint()
        {
            OnPropertyChanged(nameof(ShowTutorialHint));
            OnPropertyChanged(nameof(TutorialHintTitle));
            OnPropertyChanged(nameof(TutorialHintDescription));
            OnPropertyChanged(nameof(ShowOpenHint));
        }

        [RelayCommand]
        public void SkipTutorial()
        {
            _tutorial.Skip();
            RefreshTutorialHint();
        }

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetCampaignsAsync();
                Campaigns = new ObservableCollection<Campaign>(list);
            });
            RefreshTutorialHint();
        }

        [RelayCommand]
        public async Task Create()
        {
            string? name = await ShowPromptAsync(Loc["DialogNewCampaign"], Loc["PromptName"]);
            if (string.IsNullOrWhiteSpace(name)) return;

            var campaign = new Campaign { Title = name.CapitalizeFirst() };
            await _sceneDataService.SaveCampaignAsync(campaign);
            Campaigns.Add(campaign);

            _tutorial.Complete(TutorialService.StepCreateCampaign);
            RefreshTutorialHint();
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
            _tutorial.Complete(TutorialService.StepOpenCampaign);

            await Shell.Current.GoToAsync(nameof(SessionListPage),
                new Dictionary<string, object> { { "Campaign", campaign } });
        }
    }
}
