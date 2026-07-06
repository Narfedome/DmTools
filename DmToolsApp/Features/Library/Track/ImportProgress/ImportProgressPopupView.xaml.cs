namespace DmToolsApp.Features.Library
{
    public partial class ImportProgressPopupView : ContentView
    {
        public ImportProgressViewModel ViewModel { get; } = new();

        public ImportProgressPopupView()
        {
            InitializeComponent();
            BindingContext = ViewModel;
        }
    }
}
