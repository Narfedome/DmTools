namespace DmToolsApp.Services
{
    /// <summary>
    /// Abstraction du stockage physique des fichiers audio locaux (implémentée côté MAUI par
    /// FileService), pour que les services Core restent testables sans dépendance MAUI.
    /// </summary>
    public interface ITrackFileStore
    {
        string CopyTrackToLocal(string originalFilePath);
    }
}
