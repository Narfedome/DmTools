namespace DmToolsApp.Services
{
    /// <summary>
    /// Abstraction du stockage physique des fichiers audio locaux (implémentée côté MAUI par
    /// FileService), pour que les services Core restent testables sans dépendance MAUI.
    /// </summary>
    public interface ITrackFileStore
    {
        string CopyTrackToLocal(string originalFilePath);

        /// <summary>Réserve un chemin unique dans le stockage local pour y écrire directement une
        /// nouvelle piste (extension incluse, avec le point), sans copier de fichier source existant.</summary>
        string ReserveTrackPath(string extension);
    }
}
