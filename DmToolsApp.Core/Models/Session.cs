using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DmToolsApp.Models
{
    public partial class Session : ObservableObject
    {
        public int Id { get; set; }

        public int CampaignId { get; set; }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Scene> scenes = new();
    }
}
