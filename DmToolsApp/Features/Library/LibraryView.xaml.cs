using DmToolsApp.Components;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Features.Library;

public partial class LibraryView : ContentView
{
	public LibraryView()
	{
		InitializeComponent();
    }
        
    public static readonly BindableProperty IsCrudProperty =
    BindableProperty.Create(nameof(IsCrud), typeof(bool), typeof(LibraryView), default(bool));

    public bool IsCrud
    {
        get => (bool)GetValue(IsCrudProperty);
        set => SetValue(IsCrudProperty, value);
    }
}