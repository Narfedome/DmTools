using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DmToolsApp.Models
{
    public partial class Campaign : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Session> sessions = new();
    }
}
