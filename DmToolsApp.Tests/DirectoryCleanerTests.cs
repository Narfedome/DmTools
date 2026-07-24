using DmToolsApp.Services;

namespace DmToolsApp.Tests;

public class DirectoryCleanerTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "DmToolsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void ClearContents_DeletesTopLevelFiles()
    {
        var dir = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "b");

            DirectoryCleaner.ClearContents(dir);

            Assert.Empty(Directory.EnumerateFileSystemEntries(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ClearContents_DeletesNestedSubdirectories()
    {
        // Reproduit le cas Android : le fichier réel n'est pas à la racine du cache mais
        // plusieurs niveaux plus bas (cache/<hash>/<hash>/fichier.mp3).
        var dir = CreateTempDirectory();
        try
        {
            var nested = Path.Combine(dir, "level1", "level2");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(nested, "file.mp3"), "data");

            DirectoryCleaner.ClearContents(dir);

            Assert.Empty(Directory.EnumerateFileSystemEntries(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ClearContents_DoesNotDeleteTheDirectoryItself()
    {
        var dir = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");

            DirectoryCleaner.ClearContents(dir);

            Assert.True(Directory.Exists(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ClearContents_OnMissingDirectory_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DmToolsTests_" + Guid.NewGuid());

        var exception = Record.Exception(() => DirectoryCleaner.ClearContents(dir));

        Assert.Null(exception);
    }

    [Fact]
    public void ClearContents_IgnoresLockedFile_AndStillClearsTheRest()
    {
        var dir = CreateTempDirectory();
        try
        {
            var lockedPath = Path.Combine(dir, "locked.bin");
            File.WriteAllText(Path.Combine(dir, "unlocked.txt"), "a");
            File.WriteAllText(lockedPath, "locked");

            using (new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                DirectoryCleaner.ClearContents(dir);
            }

            Assert.False(File.Exists(Path.Combine(dir, "unlocked.txt")));
            Assert.True(File.Exists(lockedPath));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
