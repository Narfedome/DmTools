using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Plugin.Maui.Audio;

namespace DmToolsApp.Components
{
    public partial class ChannelStripViewModel : ObservableObject
    {        // Player audio réel (pas observable)
        public IAudioPlayer? Player { get; set; }

        // Nom de la tranche
        [ObservableProperty]
        private string? name;


        [ObservableProperty]
        private Track? track = new Track();

        [ObservableProperty]
        private string? displayTrackName;

        // Volume (TwoWay binding)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayVolume))]
        private double volume = 1;

        public int DisplayVolume => (int)(Volume * 100);

        // Lecture en cours
        [ObservableProperty]
        private bool isPlaying;

        // Méthode pour appliquer le volume sur le player
        partial void OnVolumeChanged(double oldValue, double newValue)
        {
            if (Player != null)
                Player.Volume = newValue;

            if (Track != null)
                Track.Volume = newValue;
        }

        // Commande toggle play/pause pour MVVM
        [RelayCommand]
        public void TogglePlay()
        {
            if (Player == null)
                return;

            if (Player.IsPlaying)
            {
                Player.Pause();
                IsPlaying = false;
            }
            else
            {
                Player.Play();
                IsPlaying = true;
            }
        }
        public void Pause()
        {
            if (Player == null)
                return;

            Player.Pause();
            IsPlaying = false;
        }




        [RelayCommand]
        public void Stop()
        {
            if (Player == null) return;

            Player.Stop();
            IsPlaying = false;
        }
    }
}
