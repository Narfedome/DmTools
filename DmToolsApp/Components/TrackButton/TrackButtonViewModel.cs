using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.ComponentModel;

namespace DmToolsApp.Components.TrackButton
{
    public partial class TrackButtonViewModel : BaseViewModel
    {
        private readonly AudioPlayerService _audioService;
        private readonly CoverArtService _coverArtService;

        public TrackButtonViewModel(AudioPlayerService audioService, CoverArtService coverArtService)
        {
            _audioService = audioService;
            _coverArtService = coverArtService;

            _audioService.OnStateChanged += OnAudioChanged;
        }

        private void OnAudioChanged(string? currentFile)
        {
            IsPlaying = CurrentTrack != null && currentFile == CurrentTrack.FilePath;
        }

        [ObservableProperty]
        private Track? currentTrack;
        partial void OnCurrentTrackChanged(Track? oldValue, Track? newValue)
        {
            if (oldValue != null)
                oldValue.PropertyChanged -= OnTrackChanged;

            if (newValue != null)
                newValue.PropertyChanged += OnTrackChanged;

            UpdateFromTrack(newValue);
        }
        private void OnTrackChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateFromTrack(CurrentTrack);
        }

        private void UpdateFromTrack(Track? value)
        {
            if (value == null)
            {
                CoverImage = null;
                IsPlaying = false;
                IsLoadingCover = false;
                return;
            }

            IsPlaying = _audioService.CurrentFile == value.FilePath;

            if (_coverArtService.TryGetCached(value, out var cached))
            {
                CoverImage = cached;
                IsLoadingCover = false;
                return;
            }

            CoverImage = null;
            IsLoadingCover = true;
            _ = LoadCoverImageAsync(value);
        }

        private async Task LoadCoverImageAsync(Track track)
        {
            var image = await _coverArtService.GetCoverAsync(track);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (CurrentTrack == track)
                {
                    CoverImage = image;
                    IsLoadingCover = false;
                }
            });
        }

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLoadingCover;

        [ObservableProperty]
        private ImageSource? coverImage;

        [RelayCommand]
        private void TogglePlay()
        {
            if (CurrentTrack == null)
                return;

            _audioService.Toggle(CurrentTrack.FilePath);
        }
    }
}
