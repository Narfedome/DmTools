namespace DmToolsApp.Features.Library;

public partial class LibraryTrackEditPage : ContentPage
{
	public LibraryTrackEditPage(LibraryTrackEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}