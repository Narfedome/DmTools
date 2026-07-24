using DmToolsApp.Models.Library;

namespace DmToolsApp.Services
{
    public record OrphanScanResult(List<string> OrphanedFiles, long TotalOrphanBytes);

    public class StorageService : IStorageService
    {
        private readonly FileService _fileService;
        private readonly ILibraryDataService _libraryDataService;

        public StorageService(FileService fileService, ILibraryDataService libraryDataService)
        {
            _fileService = fileService;
            _libraryDataService = libraryDataService;
        }

        public Task<long> GetTotalLibrarySizeAsync()
        {
            long total = 0;

            foreach (var directory in new[] { _fileService.TracksDirectory, _fileService.AssetsDirectory, _fileService.CoversDirectory })
            {
                if (!Directory.Exists(directory))
                    continue;

                foreach (var file in Directory.EnumerateFiles(directory))
                    total += new FileInfo(file).Length;
            }

            return Task.FromResult(total);
        }

        public async Task<List<(string Title, long SizeBytes)>> GetLargestTracksAsync(int count)
        {
            var items = await _libraryDataService.GetAllItemsTypeAsync(typeof(Track));

            return items
                .OfType<Track>()
                .Where(t => File.Exists(t.FilePath))
                .Select(t => (t.Title, SizeBytes: new FileInfo(t.FilePath).Length))
                .OrderByDescending(t => t.SizeBytes)
                .Take(count)
                .ToList();
        }

        public async Task<OrphanScanResult> ScanOrphansAsync()
        {
            var referenced = await _libraryDataService.GetAllReferencedFilePathsAsync();

            var candidates = new[] { _fileService.TracksDirectory, _fileService.AssetsDirectory, _fileService.CoversDirectory }
                .Where(Directory.Exists)
                .SelectMany(Directory.EnumerateFiles);

            var (orphans, totalBytes) = LibraryStorageScanner.FindOrphans(
                candidates, referenced, file => new FileInfo(file).Length);

            return new OrphanScanResult(orphans, totalBytes);
        }

        public Task<long> DeleteOrphansAsync(OrphanScanResult scan)
        {
            long freed = 0;

            foreach (var file in scan.OrphanedFiles)
            {
                try
                {
                    var size = new FileInfo(file).Length;
                    File.Delete(file);
                    freed += size;
                }
                catch { /* fichier déjà supprimé ou verrouillé, on continue */ }
            }

            return Task.FromResult(freed);
        }

        public async Task<(int Count, long TotalBytes)> GetTracksSummaryAsync()
        {
            var items = await _libraryDataService.GetAllItemsTypeAsync(typeof(Track));
            var tracks = items.OfType<Track>().ToList();

            long total = tracks
                .Where(t => File.Exists(t.FilePath))
                .Sum(t => new FileInfo(t.FilePath).Length);

            return (tracks.Count, total);
        }

        public async Task<int> DeleteAllTracksAsync()
        {
            var deletedTracks = await _libraryDataService.DeleteAllTracksAsync();

            foreach (var track in deletedTracks)
            {
                _fileService.DeleteTrackFromLocal(track.FilePath);
                // Les vignettes de pochette accompagnent leurs tracks : toutes supprimées, plus
                // aucune pochette n'est référencée (les sorts ont leurs propres images).
                _fileService.DeleteTrackFromLocal(track.ImagePath);
            }

            return deletedTracks.Count;
        }
    }

    public interface IStorageService
    {
        Task<long> GetTotalLibrarySizeAsync();
        Task<List<(string Title, long SizeBytes)>> GetLargestTracksAsync(int count);
        Task<OrphanScanResult> ScanOrphansAsync();
        Task<long> DeleteOrphansAsync(OrphanScanResult scan);
        Task<(int Count, long TotalBytes)> GetTracksSummaryAsync();
        Task<int> DeleteAllTracksAsync();
    }
}
