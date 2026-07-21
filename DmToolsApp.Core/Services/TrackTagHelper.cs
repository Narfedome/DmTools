namespace DmToolsApp.Services
{
    /// <summary>
    /// Logique pure (sans dépendance MAUI) d'extraction de métadonnées audio, extraite de FileService
    /// pour rester testable dans un projet de tests sans hôte MAUI.
    /// </summary>
    public static class TrackTagHelper
    {
        public static string ComputeSha256(string filePath)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(sha.ComputeHash(stream));
        }

        /// <summary>
        /// Vérifie qu'un fichier est un audio réellement décodable (utilisé à l'import d'un .dmpack :
        /// un fichier reçu d'un tiers ne doit jamais être accepté en bibliothèque sur la seule foi de
        /// son extension).
        /// </summary>
        public static bool IsDecodableAudio(string filePath)
        {
            try
            {
                using var tagFile = TagLib.File.Create(filePath);
                return tagFile.Properties != null && tagFile.Properties.Duration > TimeSpan.Zero;
            }
            catch (TagLib.UnsupportedFormatException)
            {
                return false;
            }
            catch (TagLib.CorruptFileException)
            {
                return false;
            }
        }

        /// <summary>
        /// Construit le titre d'une piste à partir de ses tags audio : "Artiste - Titre" si les deux sont
        /// présents, juste le titre si l'artiste manque, ou le nom de fichier si aucun titre n'est taggé.
        /// </summary>
        public static string ExtractTitle(TagLib.Tag tag, string fallbackFileName)
        {
            var title = tag.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                return fallbackFileName;

            var artist = tag.FirstAlbumArtist?.Trim();
            return string.IsNullOrEmpty(artist) ? title : $"{artist} - {title}";
        }
    }
}
