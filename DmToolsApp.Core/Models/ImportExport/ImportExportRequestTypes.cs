namespace DmToolsApp.Models.ImportExport
{
    public enum ExportLevel
    {
        /// <summary>Campagne choisie : Campagne/Chapitre/Scène, sans canaux ni audio.</summary>
        StructureOnly = 1,

        /// <summary>Campagne choisie, avec les canaux (SceneTrack) et les fichiers audio référencés.</summary>
        StructureWithChannels = 2,

        /// <summary>Toute la bibliothèque audio, sans structure de campagne.</summary>
        AudioLibraryOnly = 3,

        /// <summary>Backup complet : toutes les campagnes (avec canaux) + toute la bibliothèque.</summary>
        FullBackup = 4
    }

    /// <summary>
    /// <see cref="CampaignId"/> est requis pour <see cref="ExportLevel.StructureOnly"/> et
    /// <see cref="ExportLevel.StructureWithChannels"/> ; ignoré pour les deux autres niveaux, qui ne
    /// portent pas sur une seule campagne.
    /// </summary>
    public class ExportRequest
    {
        public ExportLevel Level { get; set; }
        public int CampaignId { get; set; }
    }

    public class ExportProgress
    {
        public string CurrentItem { get; set; } = string.Empty;
        public int Processed { get; set; }
        public int Total { get; set; }
    }

    public class ImportProgress
    {
        public string CurrentItem { get; set; } = string.Empty;
        public int Processed { get; set; }
        public int Total { get; set; }
    }

    /// <summary>Résumé d'un import, affiché à l'utilisateur en fin d'opération.</summary>
    public class ImportResult
    {
        public int CampaignsImported { get; set; }
        public int TracksReused { get; set; }
        public int TracksCopied { get; set; }
        public int TracksRejected { get; set; }

        // Détail des causes de rejet (leur somme égale TracksRejected) : affiché à l'utilisateur
        // uniquement s'il y a effectivement des rejets, pour comprendre pourquoi sans avoir à
        // consulter des logs.
        public int TracksRejectedHashMismatch { get; set; }
        public int TracksRejectedNotDecodable { get; set; }
        public int TracksRejectedMissingEntry { get; set; }
        public int TracksRejectedOther { get; set; }

        public int SpellsImported { get; set; }

        // TEMPORAIRE (diagnostic perf) : temps cumulé par phase sur toutes les pistes, pour savoir où
        // part réellement le temps avant d'optimiser à l'aveugle. À retirer une fois la mesure faite.
        public TimeSpan DiagExtractHashWrite { get; set; }
        public TimeSpan DiagDecodabilityCheck { get; set; }
        public TimeSpan DiagDatabaseSave { get; set; }
    }
}
