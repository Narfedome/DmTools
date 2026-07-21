using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DmToolsApp.Components;
using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Models;
using DmToolsApp.Models.ImportExport;
using DmToolsApp.Services;

namespace DmToolsApp.Features.ImportExport
{
    public partial class ImportExportViewModel : BaseViewModel
    {
        private readonly IImportExportService _importExportService;
        private readonly ISceneDataService _sceneDataService;
        private readonly FileService _fileService;

        private List<Campaign> _campaigns = new();
        private ExportLevel _selectedLevel = ExportLevel.StructureOnly;

        [ObservableProperty]
        private string selectedLevelLabel;

        [ObservableProperty]
        private Campaign? selectedCampaign;

        public bool RequiresCampaignSelection => _selectedLevel is ExportLevel.StructureOnly or ExportLevel.StructureWithChannels;

        public ImportExportViewModel(IImportExportService importExportService, ISceneDataService sceneDataService, FileService fileService)
        {
            _importExportService = importExportService;
            _sceneDataService = sceneDataService;
            _fileService = fileService;
            selectedLevelLabel = LevelLabel(_selectedLevel);
        }

        public async Task InitializeAsync()
        {
            _campaigns = await _sceneDataService.GetCampaignsAsync();
            if (SelectedCampaign == null || _campaigns.All(c => c.Id != SelectedCampaign.Id))
                SelectedCampaign = _campaigns.FirstOrDefault();
        }

        [RelayCommand]
        private async Task SelectLevel()
        {
            var levels = new[] { ExportLevel.StructureOnly, ExportLevel.StructureWithChannels, ExportLevel.AudioLibraryOnly, ExportLevel.FullBackup };
            var labels = levels.Select(LevelLabel).ToArray();

            var index = await ShowActionSheetIndexAsync(Loc["ImportExportLevelTitle"], labels);
            if (index < 0) return;

            _selectedLevel = levels[index];
            SelectedLevelLabel = labels[index];
            OnPropertyChanged(nameof(RequiresCampaignSelection));
        }

        [RelayCommand]
        private async Task SelectCampaign()
        {
            if (_campaigns.Count == 0)
            {
                await ShowInfoAsync(Loc["ImportExportTitle"], Loc["ImportExportNoCampaigns"]);
                return;
            }

            var labels = _campaigns.Select(c => c.Title).ToArray();
            var index = await ShowActionSheetIndexAsync(Loc["ImportExportCampaignTitle"], labels);
            if (index < 0) return;

            SelectedCampaign = _campaigns[index];
        }

        [RelayCommand]
        private async Task Export()
        {
            if (RequiresCampaignSelection && SelectedCampaign == null)
            {
                await ShowInfoAsync(Loc["ImportExportTitle"], Loc["ImportExportSelectCampaignFirst"]);
                return;
            }

            var request = new ExportRequest { Level = _selectedLevel, CampaignId = SelectedCampaign?.Id ?? 0 };

            var popupView = new ImportProgressPopupView();
            popupView.ViewModel.Title = Loc["ImportExportExportInProgress"];
            var page = Shell.Current.CurrentPage;
            page.ShowPopup(popupView, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = false });

            try
            {
                using var stream = new MemoryStream();
                var progress = new Progress<ExportProgress>(p =>
                {
                    popupView.ViewModel.CurrentFileName = p.CurrentItem;
                    popupView.ViewModel.TotalCount = p.Total;
                    popupView.ViewModel.ProcessedCount = p.Processed;
                });

                await _importExportService.ExportAsync(request, stream, progress);
                stream.Position = 0;

                var fileName = SanitizeFileName($"{SelectedCampaign?.Title ?? "bibliotheque"}-{DateTime.Now:yyyyMMdd-HHmm}.dmpack");
                var savedPath = await _fileService.SaveExportPackageAsync(fileName, stream, CancellationToken.None);

                await page.ClosePopupAsync();

                if (savedPath != null)
                    await ShowInfoAsync(Loc["ImportExportTitle"], string.Format(Loc["ImportExportExportSuccess"], savedPath));
            }
            catch (Exception ex)
            {
                await page.ClosePopupAsync();
                await ShowErrorAsync(ex);
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            var file = await _fileService.PickImportPackageAsync(Loc["ImportExportPickFile"]);
            if (file == null) return;

            var popupView = new ImportProgressPopupView();
            popupView.ViewModel.Title = Loc["ImportExportImportInProgress"];
            var page = Shell.Current.CurrentPage;
            page.ShowPopup(popupView, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = false });

            try
            {
                using var stream = await file.OpenReadAsync();
                var progress = new Progress<ImportProgress>(p =>
                {
                    popupView.ViewModel.CurrentFileName = p.CurrentItem;
                    popupView.ViewModel.TotalCount = p.Total;
                    popupView.ViewModel.ProcessedCount = p.Processed;
                });

                var result = await _importExportService.ImportAsync(stream, progress);

                // Les campagnes/pistes/sorts importés sont insérés directement en base, sans passer
                // par les commandes normales (Create/Edit) qui patchent déjà les listes affichées :
                // sans ces messages, la page Campagnes et la Bibliothèque restent figées jusqu'au
                // redémarrage de l'appli.
                WeakReferenceMessenger.Default.Send(new CampaignsUpdatedMessage());
                WeakReferenceMessenger.Default.Send(new LibraryUpdatedMessage());

                await page.ClosePopupAsync();
                await InitializeAsync();

                await ShowInfoAsync(Loc["ImportExportTitle"], string.Format(
                    Loc["ImportExportImportSummary"],
                    result.CampaignsImported, result.TracksCopied, result.TracksReused, result.TracksRejected, result.SpellsImported));
            }
            catch (Exception ex)
            {
                await page.ClosePopupAsync();
                await ShowErrorAsync(ex);
            }
        }

        private static string LevelLabel(ExportLevel level) => level switch
        {
            ExportLevel.StructureOnly => LocalizationService.Instance["ImportExportLevel1"],
            ExportLevel.StructureWithChannels => LocalizationService.Instance["ImportExportLevel2"],
            ExportLevel.AudioLibraryOnly => LocalizationService.Instance["ImportExportLevel3"],
            ExportLevel.FullBackup => LocalizationService.Instance["ImportExportLevel4"],
            _ => level.ToString()
        };

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
