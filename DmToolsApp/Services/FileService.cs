namespace DmToolsApp.Services
{
    public class FileService
    {
        private readonly string _tracksDirectory;
        private readonly string _assetsDirectory;

        public string TracksDirectory => _tracksDirectory;
        public string AssetsDirectory => _assetsDirectory;

        public FileService()
        {
            _assetsDirectory = Path.Combine(FileSystem.AppDataDirectory, "Assets");
            _tracksDirectory = Path.Combine(FileSystem.AppDataDirectory, "Tracks");
            Directory.CreateDirectory(_assetsDirectory);
            Directory.CreateDirectory(_tracksDirectory);
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
        public void DeleteTrackFromLocal(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Le fichier source est introuvable", filePath);

            File.Delete(filePath);
        }

        public static string ComputeSha256(string filePath)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(sha.ComputeHash(stream));
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
