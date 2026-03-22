using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components.AudioButton;
using DmToolsApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using System.Windows.Input;

namespace DmToolsApp.Components;

public partial class AudioButtonView : ContentView
{
    private AudioButtonViewModel vm;
    public AudioButtonViewModel ViewModel => vm;
    public AudioButtonView()
    {
        InitializeComponent();

        var audioService = Application.Current
            .Handler
            .MauiContext
            .Services
            .GetService<AudioPlayerService>();

        vm = new AudioButtonViewModel(audioService!);

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

    // ICON (glyph)
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(FaIconButtonView), default(string));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }



    // COMMAND PARAM
    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(FaIconButtonView));

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

}