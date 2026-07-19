using DmToolsApp.Models.Library;

namespace DmToolsApp.Services
{
    /// <summary>
    /// Pilote le tutoriel guidé de première utilisation : Campagne -> Chapitre -> Scène -> Mixer
    /// (2 channels en parallèle pour montrer la superposition musique/ambiance). Les pages
    /// concernées lisent CurrentStep pour savoir quelle bulle d'aide afficher et appellent
    /// Complete(stepId) une fois l'action réelle effectuée par l'utilisateur.
    /// </summary>
    public class TutorialService
    {
        public const string StepCreateCampaign = "create-campaign";
        public const string StepOpenCampaign = "open-campaign";
        public const string StepCreateChapter = "create-chapter";
        public const string StepOpenChapter = "open-chapter";
        public const string StepCreateScene = "create-scene";
        public const string StepLaunchScene = "launch-scene";
        public const string StepAddChannel = "add-channel";
        public const string StepPickTrack = "pick-track";
        public const string StepAddSecondChannel = "add-second-channel";
        public const string StepPickSecondTrack = "pick-second-track";

        private static readonly string[] Order =
        {
            StepCreateCampaign, StepOpenCampaign, StepCreateChapter, StepOpenChapter,
            StepCreateScene, StepLaunchScene, StepAddChannel, StepPickTrack,
            StepAddSecondChannel, StepPickSecondTrack
        };

        // Copiées depuis Resources/Raw (MauiAsset) vers la bibliothèque au premier lancement du
        // tutoriel : une piste de musique et une piste d'ambiance, pour que les étapes "choisir
        // une piste" du mixer aient de quoi illustrer la superposition de deux channels.
        private const string MusicExampleAssetName = "tavern_ambience.opus";
        private const string AmbienceExampleAssetName = "ambience_example.opus";

        private readonly ILibraryDataService _libraryDataService;
        private readonly FileService _fileService;

        public TutorialService(ILibraryDataService libraryDataService, FileService fileService)
        {
            _libraryDataService = libraryDataService;
            _fileService = fileService;
        }

        public bool IsActive { get; private set; }
        public string? CurrentStep { get; private set; }

        public async Task StartAsync()
        {
            // Le rejeu du tutoriel ("Revoir le tutoriel" dans les Réglages) n'a pas besoin de
            // ré-extraire/hasher les pistes d'exemple à chaque fois : un flag suffit, on évite
            // ainsi un aller-retour disque + hash perceptible à chaque redémarrage du tutoriel.
            if (!Preferences.Default.Get("tutorial_tracks_seeded", false))
            {
                await SeedExampleTrackAsync(MusicExampleAssetName,
                    LocalizationService.Instance["TutorialMusicTrackTitle"],
                    LocalizationService.Instance["LibCategoryMusic"]);
                await SeedExampleTrackAsync(AmbienceExampleAssetName,
                    LocalizationService.Instance["TutorialAmbienceTrackTitle"],
                    LocalizationService.Instance["LibCategoryAmbience"]);
                Preferences.Default.Set("tutorial_tracks_seeded", true);
            }

            IsActive = true;
            CurrentStep = Order[0];
        }

        /// <summary>
        /// Fait avancer le tutoriel si l'étape complétée est bien l'étape courante (sans effet
        /// sinon : une action réalisée hors séquence, ou le tutoriel déjà terminé/désactivé,
        /// ne doit rien changer). Retourne true si cette completion vient de terminer le
        /// tutoriel (dernière étape de la séquence).
        /// </summary>
        public bool Complete(string step)
        {
            if (!IsActive || CurrentStep != step) return false;

            var idx = Array.IndexOf(Order, step);
            if (idx == Order.Length - 1)
            {
                Finish();
                return true;
            }

            CurrentStep = Order[idx + 1];
            return false;
        }

        public void Skip() => Finish();

        private void Finish()
        {
            IsActive = false;
            CurrentStep = null;
            Preferences.Default.Set("tutorial_done", true);
        }

        private async Task SeedExampleTrackAsync(string assetName, string title, string category)
        {
            using var assetStream = await FileSystem.OpenAppPackageFileAsync(assetName);
            var tempPath = Path.Combine(FileSystem.CacheDirectory, assetName);
            using (var fileStream = File.Create(tempPath))
                await assetStream.CopyToAsync(fileStream);

            // Hash + lecture des tags hors du thread UI (comme l'import manuel, cf.
            // LibraryTrackEditViewModel.PickFile) : en synchrone, ça gèle l'interface le temps de
            // l'opération.
            var (hash, duration) = await Task.Run(() =>
            {
                var h = TrackTagHelper.ComputeSha256(tempPath);
                var d = TagLib.File.Create(tempPath).Properties.Duration;
                return (h, d);
            });

            // Déduplication par hash (même logique que l'import manuel) : évite de recréer une
            // seconde entrée si jamais cette méthode est appelée alors qu'une piste identique existe déjà.
            var existing = await _libraryDataService.FindTrackByHashAsync(hash, excludeId: 0);
            if (existing == null)
            {
                var track = new Track
                {
                    Title = title,
                    FilePath = await Task.Run(() => _fileService.CopyTrackToLocal(tempPath)),
                    Duration = duration,
                    Hash = hash,
                    Category = category
                };
                await _libraryDataService.SaveLibraryItemAsync(track);
            }

            _fileService.DeleteIfCached(tempPath);
        }
    }
}
