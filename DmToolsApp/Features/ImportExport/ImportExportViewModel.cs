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
using DmToolsApp.Models.Library;
using DmToolsApp.Services;

namespace DmToolsApp.Features.ImportExport
{
    public partial class ImportExportViewModel : BaseViewModel
    {
        private readonly IImportExportService _importExportService;
        private readonly ISceneDataService _sceneDataService;
        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;

        private List<Campaign> _campaigns = new();
        private ExportLevel _selectedLevel = ExportLevel.StructureOnly;

        // Clés des catégories par défaut (cf. MauiProgram) : ce sont des libellés localisés, pas des
        // clés stables, donc un import venant d'une install dans une autre langue (ex. Android EN vs
        // Windows FR) crée sinon des doublons ("Music" + "Musique") au lieu de fusionner sur la même
        // catégorie logique. Cf. ReconcileDefaultCategoryTranslationsAsync.
        private static readonly string[] DefaultCategoryKeys =
            { "LibCategoryMusic", "LibCategoryAmbience", "LibCategorySoundEffect" };

        [ObservableProperty]
        private string selectedLevelLabel;

        [ObservableProperty]
        private Campaign? selectedCampaign;

        public bool RequiresCampaignSelection => _selectedLevel is ExportLevel.StructureOnly or ExportLevel.StructureWithChannels;

        public ImportExportViewModel(
            IImportExportService importExportService,
            ISceneDataService sceneDataService,
            ILibraryDataService libraryDataService,
            FileService fileService)
        {
            _importExportService = importExportService;
            _sceneDataService = sceneDataService;
            _libraryDataService = libraryDataService;
            _fileService = fileService;
            selectedLevelLabel = LevelLabel(_selectedLevel);
        }

        public async Task InitializeAsync()
        {
            _campaigns = await _sceneDataService.GetCampaignsAsync();
            if (SelectedCampaign == null || _campaigns.All(c => c.Id != SelectedCampaign.Id))
                SelectedCampaign = _campaigns.FirstOrDefault();
        }

        // Remplace le bouton retour natif du Shell (cf. ImportExportPage.xaml, Shell.NavBarIsVisible="False").
        [RelayCommand]
        public async Task Back() => await Shell.Current.GoToAsync("..");

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

            // Écrit d'abord sur disque (fichier temporaire) plutôt que dans un MemoryStream : une
            // bibliothèque audio complète dépasse vite les 2 Go, et MemoryStream est limité à un
            // buffer int (~2 Go) - au-delà, l'écriture lève IOException "Stream was too long."
            var tempPath = Path.Combine(Path.GetTempPath(), $"dmtools-export-{Guid.NewGuid():N}.dmpack");
            try
            {
                var progress = new Progress<ExportProgress>(p =>
                {
                    popupView.ViewModel.CurrentFileName = p.CurrentItem;
                    popupView.ViewModel.TotalCount = p.Total;
                    popupView.ViewModel.ProcessedCount = p.Processed;
                });

                using (var fileStream = File.Create(tempPath))
                    await _importExportService.ExportAsync(request, fileStream, progress);

                // Basé sur le niveau choisi, pas juste sur SelectedCampaign : cette propriété peut
                // rester une valeur résiduelle d'une sélection précédente (Structure) alors que le
                // niveau actuel (Bibliothèque seule, Backup complet) n'a plus de campagne unique -
                // sans ce switch, le fichier récupérait un nom de campagne sans rapport avec son contenu.
                var baseName = _selectedLevel switch
                {
                    ExportLevel.StructureOnly => $"{SelectedCampaign?.Title ?? "Campagne"}-Structure",
                    ExportLevel.StructureWithChannels => $"{SelectedCampaign?.Title ?? "Campagne"}Structure-WithAudio",
                    ExportLevel.AudioLibraryOnly => "Library",
                    ExportLevel.FullBackup => "All",
                    _ => "Export"
                };
                var fileName = SanitizeFileName($"{baseName}-{DateTime.Now:yyyyMMdd-HHmm}.dmpack");

                // FileSaver.SaveAsync ne remonte aucune progression une fois l'emplacement choisi (API
                // du plugin) : on estime un temps restant nous-mêmes en observant, via ProgressReportingStream,
                // combien d'octets du flux source il a effectivement consommés au fil de la copie.
                popupView.ViewModel.CurrentFileName = Loc["ImportExportSavingFile"];
                var totalBytes = new FileInfo(tempPath).Length;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var lastUiUpdate = TimeSpan.Zero;
                var saveProgress = new Progress<long>(bytesRead =>
                {
                    var elapsed = stopwatch.Elapsed;
                    if (elapsed - lastUiUpdate < TimeSpan.FromMilliseconds(200) && bytesRead < totalBytes)
                        return;
                    lastUiUpdate = elapsed;

                    if (elapsed.TotalSeconds < 0.5 || bytesRead <= 0 || totalBytes <= 0)
                        return;

                    var bytesPerSecond = bytesRead / elapsed.TotalSeconds;
                    var remainingBytes = Math.Max(0, totalBytes - bytesRead);
                    var etaSeconds = bytesPerSecond > 0 ? remainingBytes / bytesPerSecond : 0;

                    popupView.ViewModel.CurrentFileName = string.Format(Loc["ImportExportSavingFileEta"], FormatEta(etaSeconds));
                });

                string? savedPath;
                using (var readStream = File.OpenRead(tempPath))
                using (var progressStream = new ProgressReportingStream(readStream, bytesRead => ((IProgress<long>)saveProgress).Report(bytesRead)))
                    savedPath = await _fileService.SaveExportPackageAsync(fileName, progressStream, CancellationToken.None);

                await page.ClosePopupAsync();

                if (savedPath != null)
                    await ShowInfoAsync(Loc["ImportExportTitle"], string.Format(Loc["ImportExportExportSuccess"], savedPath));
            }
            catch (Exception ex)
            {
                await page.ClosePopupAsync();
                await ShowErrorAsync(ex);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { /* meilleur effort, le fichier temporaire est jetable */ }
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
                await ReconcileDefaultCategoryTranslationsAsync();

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

        /// <summary>
        /// Ramène, pour les catégories par défaut uniquement, toute variante connue dans une autre
        /// langue vers son libellé dans la langue actuelle de l'appli - les catégories créées par
        /// l'utilisateur ne sont jamais concernées, elles sont comparées telles quelles. Que la
        /// catégorie canonique existe déjà ici ou non (l'utilisateur a pu la renommer ou la
        /// supprimer), une catégorie par défaut réimportée doit malgré tout s'aligner sur la langue
        /// courante plutôt que de rester dans la langue de l'export : RenameCategoryAsync fusionne
        /// si la cible existe déjà, ou renomme simplement sinon.
        /// </summary>
        private async Task ReconcileDefaultCategoryTranslationsAsync()
        {
            foreach (var key in DefaultCategoryKeys)
            {
                var canonical = Loc[key];

                foreach (var languageCode in LocalizationService.SupportedLanguages.Keys)
                {
                    var variant = LocalizationService.GetString(key, languageCode);
                    if (string.Equals(variant, canonical, StringComparison.Ordinal))
                        continue;

                    await _libraryDataService.RenameCategoryAsync(typeof(Track), variant, canonical);
                    await _libraryDataService.RenameCategoryAsync(typeof(Spell), variant, canonical);
                }
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

        private static string FormatEta(double seconds)
        {
            if (seconds < 1) return "< 1 s";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes} min {ts.Seconds:D2} s" : $"{ts.Seconds} s";
        }
    }
}
