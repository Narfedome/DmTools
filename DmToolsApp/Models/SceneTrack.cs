using CommunityToolkit.Mvvm.ComponentModel;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Models
{
    public partial class SceneTrack : ObservableObject
    {
        public int Id { get; set; }

        public int SceneId { get; set; }

        public int Position { get; set; }

        [ObservableProperty]
        private Track track = new();

        [ObservableProperty]
        private double volume = 1.0;

        [ObservableProperty]
        private bool autoPlay = false;
    }
}
