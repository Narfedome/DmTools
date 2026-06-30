using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.ComponentModel;
using DmToolsApp;

namespace DmToolsApp.Components.TrackButton
{
    public partial class TrackButtonViewModel : BaseViewModel
    {


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

            CoverImage = GetCoverImage(value.FilePath);
            IsPlaying = _audioService.CurrentFile == value.FilePath;
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
