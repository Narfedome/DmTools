namespace DmToolsApp.Services
{
    // Vide le contenu d'un dossier (fichiers et sous-dossiers, récursivement) sans supprimer le
    // dossier lui-même. Un fichier ou dossier verrouillé est ignoré plutôt que de faire échouer tout
    // le nettoyage. Chaque sous-dossier est supprimé récursivement en un seul appel : nécessaire car
    // certains dossiers de cache (ex. cache FilePicker sur Android) imbriquent le contenu réel
    // plusieurs niveaux plus bas plutôt qu'à la racine.
    public static class DirectoryCleaner
    {
        public static void ClearContents(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            foreach (var entry in Directory.EnumerateFileSystemEntries(directoryPath))
            {
                try
                {
                    if (Directory.Exists(entry))
                        Directory.Delete(entry, recursive: true);
                    else
                        File.Delete(entry);
                }
                catch { /* fichier ou dossier verrouillé, on continue */ }
            }
        }
    }
}
