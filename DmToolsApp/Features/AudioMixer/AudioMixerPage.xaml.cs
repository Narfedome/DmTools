namespace DmToolsApp.Features.AudioMixer;

public partial class AudioMixerPage : ContentPage
{
    private readonly AudioMixerViewModel _vm;

    public AudioMixerPage(AudioMixerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}
