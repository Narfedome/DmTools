using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Plugin.Maui.Audio;

namespace DmToolsApp.Components
{
    public partial class ChannelStripViewModel : ObservableObject
    {
        private IAudioPlayer? _player;
        public IAudioPlayer? Player
        {
            get => _player;
            set
            {
                _player = value;
                if (_player != null)
                {
                    _player.Volume = Volume;
                    _player.Loop = IsLooping;
                }
            }
        }

        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private Track? track = new Track();

        [ObservableProperty]
        private string? displayTrackName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayVolume))]
        private double volume = 1;

        public int DisplayVolume => (int)(Volume * 100);

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLooping;

        partial void OnVolumeChanged(double oldValue, double newValue)
        {
            if (Player != null)
                Player.Volume = newValue;

            if (Track != null)
                Track.Volume = newValue;
        }

        partial void OnIsLoopingChanged(bool value)
        {
            if (Player != null)
                Player.Loop = value;
        }

        [RelayCommand]
        public void ToggleLoop()
        {
            IsLooping = !IsLooping;
        }

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
        public void Play()
        {
            if (Player == null || Player.IsPlaying)
                return;

            Player.Play();
            IsPlaying = true;
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
