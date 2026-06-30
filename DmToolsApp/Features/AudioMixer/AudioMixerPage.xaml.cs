namespace DmToolsApp.Features.AudioMixer;

public partial class AudioMixerPage : ContentPage
{
    public AudioMixerPage(AudioMixerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
