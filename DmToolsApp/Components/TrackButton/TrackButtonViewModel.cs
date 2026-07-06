using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.Concurrent;
using System.ComponentModel;
using DmToolsApp;

namespace DmToolsApp.Components.TrackButton
{
    public partial class TrackButtonViewModel : BaseViewModel
    {
        // Evite de re-parser le fichier (TagLib) à chaque affichage/scroll de la même track
        private static readonly ConcurrentDictionary<string, ImageSource?> CoverImageCache = new();

        private readonly AudioPlayerService _audioService;

        public TrackButtonViewModel(AudioPlayerService audioService)
        {
            _audioService = audioService;

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
                return;
            }

            IsPlaying = _audioService.CurrentFile == value.FilePath;

            if (CoverImageCache.TryGetValue(value.FilePath, out var cached))
            {
                CoverImage = cached;
                return;
            }

            CoverImage = null;
            _ = LoadCoverImageAsync(value);
        }

        private async Task LoadCoverImageAsync(Track track)
        {
            var image = await Task.Run(() => GetCoverImage(track.FilePath));
            CoverImageCache[track.FilePath] = image;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (CurrentTrack == track)
                    CoverImage = image;
            });
        }

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private ImageSource? coverImage;

        [RelayCommand]
        private void TogglePlay()
        {
            if (CurrentTrack == null)
                return;

            _audioService.Toggle(CurrentTrack.FilePath);
        }

        public ImageSource? GetCoverImage(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {

                var file = TagLib.File.Create(filePath);

                if (file.Tag.Pictures.Length > 0)
                {
                    var pic = file.Tag.Pictures[0];
                    return ImageSource.FromStream(() => new MemoryStream(pic.Data.Data));
                }
            }
            return null;
        }

    }
}
