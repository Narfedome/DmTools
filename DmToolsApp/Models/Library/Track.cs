namespace DmToolsApp.Models.Library
{
    public class Track
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string? CoverUrl { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
