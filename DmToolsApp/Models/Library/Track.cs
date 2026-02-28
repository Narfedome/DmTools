namespace DmToolsApp.Models.Library
{
    public class Track : LibraryItem
    {
        public string FilePath { get; set; } = "";
        public TimeSpan Duration { get; set; }        
        public double Volume { get; set; } = 1.0;
    }
}
