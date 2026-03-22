using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Resources.Icons;
using DmToolsApp.Services;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Components.AudioButton
{
    public partial class AudioButtonViewModel : ObservableObject
    {


        private readonly AudioPlayerService _audioService;

        public AudioButtonViewModel(AudioPlayerService audioService)
        {
            _audioService = audioService;

            _audioService.OnStateChanged += OnAudioChanged;
        }

        private void OnAudioChanged(string? currentFile)
        {
            IsPlaying = currentFile == FilePath;
        }

        private string filePath = "";
        public string FilePath
        {
            get => filePath;
            set
            {
                SetProperty(ref filePath, value);
                CoverImage = GetCoverImage(filePath);
            }
        }


        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private ImageSource? coverImage;


        public byte[]? GetCover(string filePath)
        {
            var file = TagLib.File.Create(filePath);

            if (file.Tag.Pictures.Length > 0)
            {
                var pic = file.Tag.Pictures[0];
                return pic.Data.Data;
            }

            return null;
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
