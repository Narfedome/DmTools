using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DmToolsApp.Models;
using DmToolsApp.Models.ImportExport;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Services
{
    /// <summary>
    /// Export/import de campagnes et de la bibliothèque au format .dmpack (zip renommé, cf.
    /// documentation du format). Ne dépend que des services de données existants (ISceneDataService,
    /// ILibraryDataService) — aucun accès SQL direct : l'import n'écrit jamais par-dessus une ligne
    /// existante, il insère toujours de nouvelles lignes via les mêmes Save*Async que le reste de
    /// l'appli utilise, avec dédup des tracks par hash (FindTrackByHashAsync).
    ///
    /// Le zip lu à l'import est traité comme non fiable (peut venir d'un tiers) : toute entrée dont le
    /// nom ne correspond pas exactement au format attendu ("manifest.json", "manifest.sig" ou
    /// "tracks/&lt;hash SHA256&gt;.&lt;ext&gt;") fait rejeter l'archive entière avant la moindre
    /// extraction, ce qui élimine tout risque de zip-slip. La taille déclarée de chaque entrée et le
    /// total sont plafonnés pour se prémunir d'un zip-bomb, et le hash de chaque fichier extrait est
    /// revérifié avant d'être accepté en bibliothèque.
    ///
    /// Le manifeste est signé (HMAC-SHA256, clé fixe embarquée dans l'appli) : toute édition du
    /// manifest.json après export (titres, volumes, structure de campagne...), par exemple via un
    /// logiciel de zip, invalide la signature et fait rejeter l'archive à l'import. Ça détecte aussi
    /// une corruption accidentelle du fichier. Ce n'est en revanche pas une protection contre un
    /// attaquant qui décompile l'appli pour en extraire la clé et forger une nouvelle signature — ce
    /// n'est pas l'objectif ici, seulement d'empêcher une modification "à la main" silencieuse.
    /// </summary>
    public class ImportExportService : IImportExportService
    {
        private const int FormatVersion = 1;
        private const long MaxTrackEntryBytes = 500L * 1024 * 1024;
        private const long MaxTotalUncompressedBytes = 2L * 1024 * 1024 * 1024;
        private const string ManifestEntryName = "manifest.json";
        private const string SignatureEntryName = "manifest.sig";

        private static readonly byte[] ManifestSignatureKey =
            Convert.FromHexString("61d46ee3f647bca5824870dc1330e22e9f5201b2b395c17cba19f27e790ef21e");

        private static readonly Regex TrackEntryPattern = new(@"^tracks/[a-fA-F0-9]{64}\.[a-z0-9]{1,5}$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly ISceneDataService _sceneDataService;
        private readonly ILibraryDataService _libraryDataService;
        private readonly ITrackFileStore _trackFileStore;

        public ImportExportService(ISceneDataService sceneDataService, ILibraryDataService libraryDataService, ITrackFileStore trackFileStore)
        {
            _sceneDataService = sceneDataService;
            _libraryDataService = libraryDataService;
            _trackFileStore = trackFileStore;
        }

        // ── Export ────────────────────────────────────────────────

        public async Task ExportAsync(ExportRequest request, Stream destination, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var manifest = new ExportManifest
            {
                FormatVersion = FormatVersion,
                ExportLevel = (int)request.Level,
                ExportedAt = DateTime.UtcNow
            };

            var trackIdsToInclude = new HashSet<int>();

            switch (request.Level)
            {
                case ExportLevel.StructureOnly:
                case ExportLevel.StructureWithChannels:
                    var campaign = (await _sceneDataService.GetCampaignsAsync())
                        .FirstOrDefault(c => c.Id == request.CampaignId)
                        ?? throw new InvalidOperationException($"Campagne {request.CampaignId} introuvable.");
                    manifest.Campaigns.Add(await BuildCampaignExportAsync(
                        campaign, includeChannels: request.Level == ExportLevel.StructureWithChannels, trackIdsToInclude, cancellationToken));
                    break;

                case ExportLevel.AudioLibraryOnly:
                    foreach (var t in (await _libraryDataService.GetAllItemsTypeAsync(typeof(Track))).OfType<Track>())
                        trackIdsToInclude.Add(t.Id);
                    break;

                case ExportLevel.FullBackup:
                    foreach (var c in await _sceneDataService.GetCampaignsAsync())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        manifest.Campaigns.Add(await BuildCampaignExportAsync(c, includeChannels: true, trackIdsToInclude, cancellationToken));
                    }
                    foreach (var t in (await _libraryDataService.GetAllItemsTypeAsync(typeof(Track))).OfType<Track>())
                        trackIdsToInclude.Add(t.Id);

                    manifest.Library = await BuildLibraryExportAsync();
                    break;
            }

            using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

            var allTracksById = (await _libraryDataService.GetAllItemsTypeAsync(typeof(Track)))
                .OfType<Track>()
                .Where(t => trackIdsToInclude.Contains(t.Id))
                .ToDictionary(t => t.Id);

            int processed = 0;
            foreach (var trackId in trackIdsToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!allTracksById.TryGetValue(trackId, out var track) || string.IsNullOrEmpty(track.FilePath) || !File.Exists(track.FilePath))
                    continue;

                var hash = string.IsNullOrEmpty(track.Hash) ? TrackTagHelper.ComputeSha256(track.FilePath) : track.Hash;
                var ext = Path.GetExtension(track.FilePath).ToLowerInvariant();
                var entryName = $"tracks/{hash}{ext}";

                manifest.Tracks.Add(new TrackExport
                {
                    Id = track.Id,
                    Title = track.Title,
                    Category = track.Category,
                    DurationSeconds = track.Duration.TotalSeconds,
                    DefaultVolume = track.Volume,
                    Hash = hash,
                    FileEntry = entryName
                });

                var entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenRead(track.FilePath))
                    await fileStream.CopyToAsync(entryStream, cancellationToken);

                progress?.Report(new ExportProgress { CurrentItem = track.Title, Processed = ++processed, Total = allTracksById.Count });
            }

            var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);

            var manifestEntry = zip.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
            using (var manifestStream = manifestEntry.Open())
                await manifestStream.WriteAsync(manifestBytes, cancellationToken);

            var signatureEntry = zip.CreateEntry(SignatureEntryName, CompressionLevel.Optimal);
            using (var signatureStream = signatureEntry.Open())
            {
                var signatureHex = Encoding.ASCII.GetBytes(Convert.ToHexString(ComputeManifestSignature(manifestBytes)));
                await signatureStream.WriteAsync(signatureHex, cancellationToken);
            }
        }

        /// <summary>Internal (pas private) uniquement pour permettre aux tests de signer un manifeste
        /// forgé à la main lorsqu'ils veulent isoler une autre cause de rejet.</summary>
        internal static byte[] ComputeManifestSignature(byte[] manifestBytes)
        {
            using var hmac = new HMACSHA256(ManifestSignatureKey);
            return hmac.ComputeHash(manifestBytes);
        }

        private async Task<CampaignExport> BuildCampaignExportAsync(Campaign campaign, bool includeChannels, HashSet<int> trackIdsToInclude, CancellationToken cancellationToken)
        {
            var campaignExport = new CampaignExport { Id = campaign.Id, Title = campaign.Title };

            foreach (var session in await _sceneDataService.GetSessionsAsync(campaign.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sessionExport = new SessionExport { Id = session.Id, Title = session.Title };

                foreach (var scene in await _sceneDataService.GetScenesAsync(session.Id))
                {
                    var sceneExport = new SceneExport { Id = scene.Id, Title = scene.Title };

                    if (includeChannels)
                    {
                        foreach (var sceneTrack in await _sceneDataService.GetSceneTracksAsync(scene.Id))
                        {
                            trackIdsToInclude.Add(sceneTrack.Track.Id);
                            sceneExport.SceneTracks.Add(new SceneTrackExport
                            {
                                TrackId = sceneTrack.Track.Id,
                                Volume = sceneTrack.Volume,
                                Position = sceneTrack.Position,
                                AutoPlay = sceneTrack.AutoPlay,
                                IsLooping = sceneTrack.IsLooping,
                                FadeIn = sceneTrack.FadeIn,
                                FadeOut = sceneTrack.FadeOut
                            });
                        }
                    }

                    sessionExport.Scenes.Add(sceneExport);
                }

                campaignExport.Sessions.Add(sessionExport);
            }

            return campaignExport;
        }

        private async Task<LibraryExport> BuildLibraryExportAsync()
        {
            var library = new LibraryExport();

            foreach (var s in (await _libraryDataService.GetAllItemsTypeAsync(typeof(Spell))).OfType<Spell>())
                library.Spells.Add(new SpellExport { Id = s.Id, Title = s.Title, Description = s.Description });

            foreach (var name in await _libraryDataService.GetCategoryNamesAsync(typeof(Track)))
                library.Categories.Add(new CategoryExport { Name = name, LibraryType = nameof(Track) });
            foreach (var name in await _libraryDataService.GetCategoryNamesAsync(typeof(Spell)))
                library.Categories.Add(new CategoryExport { Name = name, LibraryType = nameof(Spell) });

            return library;
        }

        // ── Import ────────────────────────────────────────────────

        public async Task<ImportResult> ImportAsync(Stream source, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            using var zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);

            ValidateArchiveEntries(zip);

            var manifestEntry = zip.GetEntry(ManifestEntryName)
                ?? throw new InvalidDataException("Fichier .dmpack invalide : manifeste manquant.");
            var signatureEntry = zip.GetEntry(SignatureEntryName)
                ?? throw new InvalidDataException("Fichier .dmpack invalide : signature manquante.");

            byte[] manifestBytes;
            using (var manifestStream = manifestEntry.Open())
            using (var buffer = new MemoryStream())
            {
                await manifestStream.CopyToAsync(buffer, cancellationToken);
                manifestBytes = buffer.ToArray();
            }

            string declaredSignatureHex;
            using (var signatureStream = signatureEntry.Open())
            using (var reader = new StreamReader(signatureStream, Encoding.ASCII))
                declaredSignatureHex = (await reader.ReadToEndAsync(cancellationToken)).Trim();

            byte[] declaredSignature;
            try
            {
                declaredSignature = Convert.FromHexString(declaredSignatureHex);
            }
            catch (FormatException)
            {
                throw new InvalidDataException("Fichier .dmpack invalide : signature illisible.");
            }

            if (!CryptographicOperations.FixedTimeEquals(ComputeManifestSignature(manifestBytes), declaredSignature))
                throw new InvalidDataException("Archive .dmpack corrompue ou modifiée : signature invalide.");

            var manifest = JsonSerializer.Deserialize<ExportManifest>(manifestBytes, JsonOptions)
                ?? throw new InvalidDataException("Fichier .dmpack invalide : manifeste illisible.");
            ValidateManifest(manifest);

            var result = new ImportResult();
            var trackIdMap = new Dictionary<int, int>();

            int processed = 0;
            foreach (var trackExport in manifest.Tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var newTrackId = await ImportTrackAsync(zip, trackExport, result);
                if (newTrackId is int id)
                    trackIdMap[trackExport.Id] = id;
                else
                    result.TracksRejected++;

                progress?.Report(new ImportProgress { CurrentItem = trackExport.Title, Processed = ++processed, Total = manifest.Tracks.Count });
            }

            foreach (var campaignExport in manifest.Campaigns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ImportCampaignAsync(campaignExport, trackIdMap, result);
            }

            if (manifest.Library != null)
            {
                foreach (var spell in manifest.Library.Spells)
                {
                    await _libraryDataService.SaveLibraryItemAsync(new Spell { Title = spell.Title, Description = spell.Description });
                    result.SpellsImported++;
                }

                foreach (var category in manifest.Library.Categories)
                {
                    var type = category.LibraryType == nameof(Spell) ? typeof(Spell) : typeof(Track);
                    await _libraryDataService.EnsureCategoryAsync(type, category.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// Rejette l'archive entière si une seule entrée ne correspond pas au format attendu (jamais
        /// de chemin de traversée possible : le nom validé sert ensuite tel quel à l'extraction) ou si
        /// la taille déclarée totale dépasse le plafond anti zip-bomb.
        /// </summary>
        private static void ValidateArchiveEntries(ZipArchive zip)
        {
            long totalDeclared = 0;
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName != ManifestEntryName && entry.FullName != SignatureEntryName && !TrackEntryPattern.IsMatch(entry.FullName))
                    throw new InvalidDataException($"Entrée d'archive non reconnue : {entry.FullName}");

                if (entry.Length > MaxTrackEntryBytes)
                    throw new InvalidDataException($"Entrée d'archive trop volumineuse : {entry.FullName}");

                totalDeclared += entry.Length;
            }

            if (totalDeclared > MaxTotalUncompressedBytes)
                throw new InvalidDataException("Archive .dmpack trop volumineuse.");
        }

        private static void ValidateManifest(ExportManifest manifest)
        {
            if (manifest.FormatVersion != FormatVersion)
                throw new InvalidDataException($"Version de format .dmpack non supportée : {manifest.FormatVersion}.");

            if (manifest.ExportLevel is < 1 or > 4)
                throw new InvalidDataException("Niveau d'export invalide dans le manifeste.");

            foreach (var track in manifest.Tracks)
            {
                if (string.IsNullOrWhiteSpace(track.Hash) || track.Hash.Length != 64)
                    throw new InvalidDataException("Hash de track invalide dans le manifeste.");

                if (!TrackEntryPattern.IsMatch(track.FileEntry) ||
                    !track.FileEntry.StartsWith($"tracks/{track.Hash}.", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Référence de fichier incohérente avec le hash déclaré.");
            }
        }

        /// <returns>Le nouvel Id local de la track (réutilisée ou copiée), ou null si l'entrée a été rejetée.</returns>
        private async Task<int?> ImportTrackAsync(ZipArchive zip, TrackExport trackExport, ImportResult result)
        {
            var existing = await _libraryDataService.FindTrackByHashAsync(trackExport.Hash, excludeId: 0);
            if (existing != null)
            {
                result.TracksReused++;
                return existing.Id;
            }

            var entry = zip.GetEntry(trackExport.FileEntry);
            if (entry == null)
                return null;

            var tempPath = Path.Combine(Path.GetTempPath(), $"dmpack-import-{Guid.NewGuid():N}{Path.GetExtension(trackExport.FileEntry)}");
            string? localPath = null;
            try
            {
                using (var entryStream = entry.Open())
                using (var fileStream = File.Create(tempPath))
                    await entryStream.CopyToAsync(fileStream);

                var actualHash = TrackTagHelper.ComputeSha256(tempPath);
                if (!string.Equals(actualHash, trackExport.Hash, StringComparison.OrdinalIgnoreCase))
                    return null;

                if (!TrackTagHelper.IsDecodableAudio(tempPath))
                    return null;

                localPath = _trackFileStore.CopyTrackToLocal(tempPath);
                var track = new Track
                {
                    Title = trackExport.Title,
                    Category = trackExport.Category,
                    Duration = TimeSpan.FromSeconds(trackExport.DurationSeconds),
                    Volume = trackExport.DefaultVolume,
                    Hash = trackExport.Hash,
                    FilePath = localPath
                };
                await _libraryDataService.SaveLibraryItemAsync(track);

                // La catégorie du manifeste n'est autrement enregistrée que pour un backup complet
                // (via manifest.Library.Categories) : pour les autres niveaux d'export, une catégorie
                // "custom" (ni Musique/Ambiance/SFX) resterait un simple texte sur la track, invisible
                // dans le sélecteur/la gestion des catégories tant qu'elle n'existe pas ici aussi.
                if (!string.IsNullOrEmpty(track.Category))
                    await _libraryDataService.EnsureCategoryAsync(typeof(Track), track.Category);

                result.TracksCopied++;
                return track.Id;
            }
            catch
            {
                // La piste a pu être copiée dans le stockage local avant l'échec (base de données,
                // disque plein...) : sans ce nettoyage, ce fichier resterait orphelin, sans plus
                // aucune ligne Track pour le référencer. On rejette simplement cette piste plutôt
                // que de faire échouer tout l'import à cause d'elle.
                if (localPath != null)
                {
                    try { File.Delete(localPath); } catch { /* meilleur effort */ }
                }
                return null;
            }
            finally
            {
                try { File.Delete(tempPath); } catch { /* meilleur effort, le fichier temporaire est jetable */ }
            }
        }

        private async Task ImportCampaignAsync(CampaignExport campaignExport, Dictionary<int, int> trackIdMap, ImportResult result)
        {
            var campaign = new Campaign { Title = campaignExport.Title };
            await _sceneDataService.SaveCampaignAsync(campaign);
            result.CampaignsImported++;

            foreach (var sessionExport in campaignExport.Sessions)
            {
                var session = new Session { CampaignId = campaign.Id, Title = sessionExport.Title };
                await _sceneDataService.SaveSessionAsync(session);

                foreach (var sceneExport in sessionExport.Scenes)
                {
                    var scene = new Scene { SessionId = session.Id, Title = sceneExport.Title };
                    await _sceneDataService.SaveSceneAsync(scene);

                    foreach (var sceneTrackExport in sceneExport.SceneTracks)
                    {
                        // Track absente du manifeste ou rejetée à l'import (sécurité) : le canal est
                        // simplement omis plutôt que de faire échouer tout l'import de la campagne.
                        if (!trackIdMap.TryGetValue(sceneTrackExport.TrackId, out var newTrackId))
                            continue;

                        await _sceneDataService.SaveSceneTrackAsync(new SceneTrack
                        {
                            SceneId = scene.Id,
                            Track = new Track { Id = newTrackId },
                            Position = sceneTrackExport.Position,
                            Volume = sceneTrackExport.Volume,
                            AutoPlay = sceneTrackExport.AutoPlay,
                            IsLooping = sceneTrackExport.IsLooping,
                            FadeIn = sceneTrackExport.FadeIn,
                            FadeOut = sceneTrackExport.FadeOut
                        });
                    }
                }
            }
        }
    }

    public interface IImportExportService
    {
        Task ExportAsync(ExportRequest request, Stream destination, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<ImportResult> ImportAsync(Stream source, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
