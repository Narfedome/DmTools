using DmToolsApp.Models.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class TrackFileService
    {
        private readonly string _tracksDirectory;

        public TrackFileService()
        {
            _tracksDirectory = Path.Combine(FileSystem.AppDataDirectory, "Tracks");
            Directory.CreateDirectory(_tracksDirectory);
        }

        /// <summary>
        /// Copie un fichier mp3 dans le dossier privé de l'application et retourne le nouveau path
        /// </summary>
        public string CopyToLocal(string originalFilePath)
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
                throw new FileNotFoundException("Le fichier source est introuvable", originalFilePath);

            // Génère un nom unique pour éviter les collisions
            var destFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFilePath);
            var destPath = Path.Combine(_tracksDirectory, destFileName);

            File.Copy(originalFilePath, destPath, overwrite: true);
            return destPath;
        }
    }
}
