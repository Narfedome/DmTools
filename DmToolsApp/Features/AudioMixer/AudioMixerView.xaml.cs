namespace DmTools.Features.AudioMixer;

public partial class AudioMixerView : ContentView
{
    public AudioMixerView(AudioMixerViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}