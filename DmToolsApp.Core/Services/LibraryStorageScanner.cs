namespace DmToolsApp.Services
{
    // Isole le calcul "quels fichiers candidats ne sont référencés par aucun item de la
    // bibliothèque" de l'accès disque réel (StorageService), pour le rendre testable sans dépendre
    // du système de fichiers ni de MAUI.
    public static class LibraryStorageScanner
    {
        public static (List<string> Orphans, long TotalBytes) FindOrphans(
            IEnumerable<string> candidateFiles,
            ISet<string> referencedFilePaths,
            Func<string, long> getFileSize)
        {
            var orphans = new List<string>();
            long totalBytes = 0;

            foreach (var file in candidateFiles)
            {
                if (referencedFilePaths.Contains(file))
                    continue;

                orphans.Add(file);
                totalBytes += getFileSize(file);
            }

            return (orphans, totalBytes);
        }
    }
}
