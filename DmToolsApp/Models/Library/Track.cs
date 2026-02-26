namespace DmToolsApp.Models.Library
{
    public class Track : LibraryItem
    {
        public string FilePath { get; set; } = "";
        public string? CoverUrl { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
