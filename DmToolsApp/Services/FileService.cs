namespace DmToolsApp.Services
{
    public class FileService
    {
        private readonly string _tracksDirectory;
        private readonly string _assetsDirectory;
        private readonly string _coversDirectory;

        public string TracksDirectory => _tracksDirectory;
        public string AssetsDirectory => _assetsDirectory;
        public string CoversDirectory => _coversDirectory;

        public FileService()
        {
            _assetsDirectory = Path.Combine(FileSystem.AppDataDirectory, "Assets");
            _tracksDirectory = Path.Combine(FileSystem.AppDataDirectory, "Tracks");
            _coversDirectory = Path.Combine(FileSystem.AppDataDirectory, "Covers");
            Directory.CreateDirectory(_assetsDirectory);
            Directory.CreateDirectory(_tracksDirectory);
            Directory.CreateDirectory(_coversDirectory);

            ClearPickerCache();
        }

        // Sur Android, FilePicker copie systématiquement le fichier choisi dans le cache privé de
        // l'appli (getCacheDir()) et ne le supprime jamais lui-même - comportement documenté et non
        // géré par MAUI. Sans ce coup de balai au démarrage, les copies laissées par les imports
        // précédant ce correctif restent coincées indéfiniment : invisibles pour le calcul de
        // stockage de l'appli (qui ne regarde que Tracks/Assets) mais comptées par Android dans la
        // taille totale de l'appli.
        private static void ClearPickerCache()
        {
            var dir = FileSystem.CacheDirectory;
            if (!Directory.Exists(dir))
                return;

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                try { File.Delete(file); } catch { /* fichier verrouillé, on continue */ }
            }
        }

        /// <summary>
        /// Supprime le fichier s'il s'agit d'une copie laissée par FilePicker dans le cache de l'appli
        /// (cf. ClearPickerCache) - ne touche jamais un fichier hors de ce dossier, pour ne pas risquer
        /// de supprimer le fichier original de l'utilisateur sur les plateformes où FilePicker renvoie
        /// directement son chemin réel (Windows, MacCatalyst) plutôt qu'une copie.
        /// </summary>
        public void DeleteIfCached(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !filePath.StartsWith(FileSystem.CacheDirectory, StringComparison.Ordinal))
                return;

            try { File.Delete(filePath); } catch { /* déjà supprimé ou verrouillé, sans conséquence */ }
        }

        public string CopyAssetToLocal(string originalFilePath)
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
                throw new FileNotFoundException("Le fichier source est introuvable", originalFilePath);

            // Génère un nom unique pour éviter les collisions
            var destFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFilePath);
            var destPath = Path.Combine(_assetsDirectory, destFileName);

            File.Copy(originalFilePath, destPath, overwrite: true);
            return destPath;
        }

        /// <summary>
        /// Copie un fichier mp3 dans le dossier privé de l'application et retourne le nouveau path
        /// </summary>
        public string CopyTrackToLocal(string originalFilePath)
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
                throw new FileNotFoundException("Le fichier source est introuvable", originalFilePath);

            // Génère un nom unique pour éviter les collisions
            var destFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFilePath);
            var destPath = Path.Combine(_tracksDirectory, destFileName);

            File.Copy(originalFilePath, destPath, overwrite: true);
            return destPath;
        }
        /// <summary>
        /// Sauvegarde une vignette de pochette (déjà extraite/redimensionnée) dans le dossier privé
        /// de l'application et retourne son chemin.
        /// </summary>
        public string SaveCoverThumbnail(byte[] jpegBytes)
        {
            var destPath = Path.Combine(_coversDirectory, Guid.NewGuid().ToString() + ".jpg");
            File.WriteAllBytes(destPath, jpegBytes);
            return destPath;
        }

        public void DeleteTrackFromLocal(string filePath)
        {
            // Suppression "best effort" : une track peut ne pas avoir de fichier associé (FilePath vide)
            // ou pointer vers un fichier déjà supprimé — dans ce cas il n'y a simplement rien à faire.
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            File.Delete(filePath);
        }

        private static readonly FilePickerFileType AudioFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.audio" } },
            { DevicePlatform.Android, new[] { "audio/*" } },
            { DevicePlatform.WinUI, new[] { ".mp3", ".wav", ".m4a", ".opus" } },
            { DevicePlatform.MacCatalyst, new[] { "public.audio" } }
        });

        public async Task<FileResult?> PickAudioFileAsync(string pickerTitle)
        {
            return await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = pickerTitle,
                FileTypes = AudioFileTypes
            });
        }

        public async Task<IEnumerable<FileResult>?> PickAudioFilesAsync(string pickerTitle)
        {
            return await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = pickerTitle,
                FileTypes = AudioFileTypes
            });
        }
    }
}
