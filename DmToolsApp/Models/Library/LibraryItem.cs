using CommunityToolkit.Mvvm.ComponentModel;

namespace DmToolsApp.Models.Library
{
    public partial class LibraryItem : ObservableObject
    {

        [ObservableProperty]
        private int id; 

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string imagePath = "";

        [ObservableProperty]
        private string filePath  ="";
        public LibraryItem Clone()
        {
            return (LibraryItem)this.MemberwiseClone();
        }
    }
}
