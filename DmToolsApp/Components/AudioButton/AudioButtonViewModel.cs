using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace DmToolsApp.Components.AudioButton
{
    public partial class AudioButtonViewModel : ObservableObject
    {
        public AudioButtonViewModel()
        {
            IsPlaying = false;
        }

        [ObservableProperty]
        public string filePath = "";

        [ObservableProperty]
        private bool isPlaying;
        public IAudioPlayer? Player { get; set; }

        private void LoadAudio()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            try
            {
                Player = AudioManager.Current.CreatePlayer(FilePath);
                Player.Volume = 1;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, unsupported format)
                Console.WriteLine($"Error loading audio: {ex.Message}");
            }
        }


        [RelayCommand]
        public void TogglePlay()
        {
            LoadAudio();
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
    }
}
