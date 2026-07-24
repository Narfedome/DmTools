using DmToolsApp.Services;

namespace DmToolsApp.Tests;

public class LibraryStorageScannerTests
{
    [Fact]
    public void FindOrphans_ExcludesReferencedFiles()
    {
        var candidates = new[] { "/tracks/a.mp3", "/tracks/b.mp3" };
        var referenced = new HashSet<string> { "/tracks/a.mp3" };

        var (orphans, totalBytes) = LibraryStorageScanner.FindOrphans(candidates, referenced, _ => 100);

        Assert.Equal(new[] { "/tracks/b.mp3" }, orphans);
        Assert.Equal(100, totalBytes);
    }

    [Fact]
    public void FindOrphans_SumsSizeOfEachOrphanOnly()
    {
        var candidates = new[] { "/tracks/a.mp3", "/tracks/b.mp3", "/covers/c.jpg" };
        var referenced = new HashSet<string>();

        var (orphans, totalBytes) = LibraryStorageScanner.FindOrphans(
            candidates, referenced, f => f.EndsWith(".mp3") ? 1000 : 50);

        Assert.Equal(3, orphans.Count);
        Assert.Equal(2050, totalBytes);
    }

    [Fact]
    public void FindOrphans_WithNoCandidates_ReturnsEmpty()
    {
        var (orphans, totalBytes) = LibraryStorageScanner.FindOrphans(
            Enumerable.Empty<string>(), new HashSet<string>(), _ => 1);

        Assert.Empty(orphans);
        Assert.Equal(0, totalBytes);
    }

    [Fact]
    public void FindOrphans_WhenEverythingReferenced_ReturnsEmpty()
    {
        var candidates = new[] { "/tracks/a.mp3" };
        var referenced = new HashSet<string> { "/tracks/a.mp3" };

        var (orphans, totalBytes) = LibraryStorageScanner.FindOrphans(candidates, referenced, _ => 100);

        Assert.Empty(orphans);
        Assert.Equal(0, totalBytes);
    }
}
