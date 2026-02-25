namespace DmToolsApp.Features.AudioMixer;

public partial class AudioMixerView : ContentPage
{
    public AudioMixerView(AudioMixerViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}