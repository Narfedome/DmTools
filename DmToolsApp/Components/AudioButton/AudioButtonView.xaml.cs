using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components.AudioButton;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using System.Windows.Input;

namespace DmToolsApp.Components;

public partial class AudioButtonView : ContentView
{
    private readonly AudioButtonViewModel vm;

    public AudioButtonView()
    {
        InitializeComponent();
        vm = new AudioButtonViewModel();
        BindingContext = vm;
    }

    // BindableProperty du ContentView
    public static readonly BindableProperty FilePathProperty =
        BindableProperty.Create(
            nameof(FilePath),
            typeof(string),
            typeof(AudioButtonView),
            default(string),
            propertyChanged: OnFilePathChanged);

    public string FilePath
    {
        get => (string)GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    private static void OnFilePathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (AudioButtonView)bindable;
        if (view.vm != null)
        {
            view.vm.FilePath = newValue?.ToString() ?? string.Empty;
        }
    }
}