using CommunityToolkit.Mvvm.ComponentModel;

namespace DmToolsApp.Models.Library
{
    public partial class Spell : LibraryItem
    {
        [ObservableProperty]
        private string description = "";
    }
}
