namespace DmToolsApp.Features.Play;

public partial class PlaySessionPage : ContentPage
{
    public PlaySessionPage(PlaySessionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
