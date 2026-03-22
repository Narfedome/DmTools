using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using DmToolsApp.Resources.Icons;
using DmToolsApp.Services;
using System.IO;
using System.Threading.Tasks;

namespace DmToolsApp.Components.TrackButton
{
    public partial class TrackButtonViewModel : ObservableObject
    {


        private readonly AudioPlayerService _audioService;

        public TrackButtonViewModel(AudioPlayerService audioService)
        {
            _audioService = audioService;

            _audioService.OnStateChanged += OnAudioChanged;
        }

        private void OnAudioChanged(string? currentFile)
        {
            IsPlaying = currentTrack != null && currentFile == currentTrack.FilePath;
        }

        [ObservableProperty]
        private Track? currentTrack;
        partial void OnCurrentTrackChanged(Track? value)
        {
            if (value == null)
            {
                CoverImage = null;
                IsPlaying = false;
                return;
            }

            CoverImage = GetCoverImage(value.FilePath);

            // Sync avec état actuel du player
            IsPlaying = _audioService.CurrentFile == value.FilePath;
        }

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private ImageSource? coverImage;

        [RelayCommand]
        private async Task TogglePlay()
        {
            if (CurrentTrack == null)
                return;

            await _audioService.Toggle(CurrentTrack.FilePath);
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
            return new FontImageSource
            {
                Glyph = SolidFont.Music,
                FontFamily = "FontSolid"
            };
        }

    }
}
