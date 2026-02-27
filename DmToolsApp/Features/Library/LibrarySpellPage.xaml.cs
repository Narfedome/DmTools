using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibrarySpellPage : ContentPage
{
	public LibrarySpellPage(LibraryViewModel vm)
    {
        InitializeComponent();
        vm.CurrentLibraryType = typeof(Spell);
        BindingContext = vm;
    }
}