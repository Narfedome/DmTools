using DmToolsApp.Services;

namespace DmToolsApp.Tests;

public class TrackTagHelperTests
{
    [Fact]
    public void ExtractTitle_UsesArtistAndTitle_WhenBothPresent()
    {
        var tag = new TagLib.Id3v2.Tag { Title = "Song", AlbumArtists = new[] { "Artist" } };

        var result = TrackTagHelper.ExtractTitle(tag, "fallback.mp3");

        Assert.Equal("Artist - Song", result);
    }

    [Fact]
    public void ExtractTitle_UsesTitleOnly_WhenArtistMissing()
    {
        var tag = new TagLib.Id3v2.Tag { Title = "Song" };

        var result = TrackTagHelper.ExtractTitle(tag, "fallback.mp3");

        Assert.Equal("Song", result);
    }

    [Fact]
    public void ExtractTitle_FallsBackToFileName_WhenNoTitleTag()
    {
        var tag = new TagLib.Id3v2.Tag { AlbumArtists = new[] { "Artist" } };

        var result = TrackTagHelper.ExtractTitle(tag, "fallback.mp3");

        Assert.Equal("fallback.mp3", result);
    }

    [Fact]
    public void ExtractTitle_TrimsWhitespace()
    {
        var tag = new TagLib.Id3v2.Tag { Title = "  Song  ", AlbumArtists = new[] { "  Artist  " } };

        var result = TrackTagHelper.ExtractTitle(tag, "fallback.mp3");

        Assert.Equal("Artist - Song", result);
    }

    [Fact]
    public void ComputeSha256_MatchesKnownVector()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "hello");

            var hash = TrackTagHelper.ComputeSha256(path);

            Assert.Equal("2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824", hash);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ComputeSha256_IsStableForIdenticalContent_AndDiffersOtherwise()
    {
        var pathA = Path.GetTempFileName();
        var pathB = Path.GetTempFileName();
        try
        {
            File.WriteAllText(pathA, "same content");
            File.WriteAllText(pathB, "same content");
            var hashA = TrackTagHelper.ComputeSha256(pathA);
            var hashB = TrackTagHelper.ComputeSha256(pathB);
            Assert.Equal(hashA, hashB);

            File.WriteAllText(pathB, "different content");
            var hashC = TrackTagHelper.ComputeSha256(pathB);
            Assert.NotEqual(hashA, hashC);
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }
}
