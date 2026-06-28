using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DmToolsApp.Models
{
    public partial class Scene : ObservableObject
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SceneTrack> tracks = new();
    }
}
