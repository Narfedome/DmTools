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
        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(1500);
        private static readonly int FadeSteps = 30;

        private CancellationTokenSource? _fadeCts;

        private IAudioPlayer? _player;
        public IAudioPlayer? Player
        {
            get => _player;
            set
            {
                CancelFade();
                _player = value;
                if (_player != null)
                {
                    _player.Volume = Volume;
                    _player.Loop = IsLooping;
                }
            }
        }

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

        public int SceneTrackId { get; set; }

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
                CancelFade();
                Player.Volume = Volume;
                Player.Play();
                IsPlaying = true;
            }
        }

        public void Play()
        {
            if (Player == null || Player.IsPlaying)
                return;

            CancelFade();
            Player.Volume = Volume;
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

            CancelFade();
            Player.Stop();
            IsPlaying = false;
        }

        [RelayCommand]
        public async Task FadeOut()
        {
            if (Player == null || !Player.IsPlaying)
                return;

            CancelFade();
            _fadeCts = new CancellationTokenSource();
            var token = _fadeCts.Token;

            var startVolume = Volume;
            var stepDelay = (int)(FadeDuration.TotalMilliseconds / FadeSteps);
            var volumeStep = startVolume / FadeSteps;

            try
            {
                for (int i = 0; i < FadeSteps; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(stepDelay, token);
                    if (Player != null)
                        Player.Volume = Math.Max(0, startVolume - volumeStep * (i + 1));
                }

                if (Player != null)
                {
                    Player.Stop();
                    Player.Volume = startVolume;
                    IsPlaying = false;
                }
            }
            catch (OperationCanceledException)
            {
                if (Player != null)
                    Player.Volume = startVolume;
            }
            finally
            {
                _fadeCts?.Dispose();
                _fadeCts = null;
            }
        }

        private void CancelFade()
        {
            _fadeCts?.Cancel();
        }
    }
}
