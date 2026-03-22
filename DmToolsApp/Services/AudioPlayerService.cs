using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Services
{
    public class AudioPlayerService
    {
        private IAudioPlayer? _player;

        public string? CurrentFile { get; private set; }

        public event Action<string?>? OnStateChanged;

        public void Toggle(string filePath)
        {
            if (_player != null && CurrentFile == filePath && _player.IsPlaying)
            {
                _player.Stop();
                OnStateChanged?.Invoke(null);
                CurrentFile = null;
                return;
            }

            _player?.Stop();

            _player = AudioManager.Current.CreatePlayer(filePath);
            _player.Play();

            CurrentFile = filePath;
            OnStateChanged?.Invoke(filePath);
        }
    }
}
