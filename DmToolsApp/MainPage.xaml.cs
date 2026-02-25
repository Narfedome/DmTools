using DmTools.Features.AudioMixer;

namespace DmToolsApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            var mixerView = serviceProvider.GetRequiredService<AudioMixerView>();
            Content = mixerView;
        }
    }
}
