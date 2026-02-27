using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components;
using DmToolsApp.Features.Library;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.AudioMixer
{
    public partial class AudioMixerViewModel : ObservableObject
    {
        private readonly AudioMixerService _audioMixerService;
        private readonly ILibraryPickerService _pickerService;

        public AudioMixerViewModel(
            AudioMixerService audioMixerService,
            ILibraryPickerService pickerService)
        {
            _audioMixerService = audioMixerService;
            _pickerService = pickerService;
        }

        [ObservableProperty]
        private ObservableCollection<ChannelStripViewModel> currentChannels = new();

        [RelayCommand]
        public async Task AddChannel()
        {
            var channel = new ChannelStripViewModel() { Name = ("Channel " + (CurrentChannels.Count + 1)), IsPlaying = false };
            CurrentChannels.Add(channel);
        }

        [RelayCommand]
        public void PlayAll()
        {
            foreach (var c in CurrentChannels)
                c.TogglePlay();
        }
        [RelayCommand]
        public void StopAll()
        {
            foreach (var c in CurrentChannels)
                c.Stop();
        }

        [RelayCommand]
        public async Task RemoveChannel(ChannelStripViewModel channel)
        {
            if (channel == null)
                return;
            if (channel.Player == null)
            {
                CurrentChannels.Remove(channel);
                return;
            }
            channel.Pause();

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Delete",
                $"Remove {channel.Name} ?",
                "Yes",
                "No");

            if (!confirm)
            {
                channel.TogglePlay();
                return;
            }

            channel.Stop();
            CurrentChannels.Remove(channel);
        }


        [RelayCommand]
        public async Task PickFile(ChannelStripViewModel channel)
        {
            try
            {
                if (channel == null) return;
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select audio file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.iOS, new[] { "public.audio" } },
                            { DevicePlatform.Android, new[] { "audio/*" } },
                            { DevicePlatform.WinUI, new[] { ".mp3", ".wav", ".m4a" } },
                            { DevicePlatform.MacCatalyst, new[] { "public.audio" } }
                        })
                });

                if (result != null)
                {
                    // Ouvrir le fichier

                    var stream = await result.OpenReadAsync();

                    channel.Player = await _audioMixerService.CreatePlayerFromSelectedFile(stream);
                    channel.DisplayTrackName = result.FileName;
                    channel.TogglePlay();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        [RelayCommand]
        public async Task PickLibraryItem(ChannelStripViewModel channel)
        {
            try
            {
                if (channel == null) return;
                var selectedLibraryItem = await _pickerService.PickTrackAsync();

                if (selectedLibraryItem is null)
                    return;
                else
                {
                    Track selectedTrack = (Track)selectedLibraryItem;
                    if (File.Exists(selectedTrack.FilePath))
                    {
                        var stream = File.OpenRead(selectedTrack.FilePath);

                        channel.Player = await _audioMixerService.CreatePlayerFromSelectedFile(stream);
                        channel.DisplayTrackName = selectedTrack.Title;
                        channel.TogglePlay();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        //public async Task LoadChannels()
        //{
        //    foreach (var channel in CurrentChannels)
        //    {
        //        if (channel.Player == null)
        //        {
        //            var stream = File.OpenRead(channel.Track.FilePath);
        //            //channel.Player = await _audioMixerService.CreatePlayerFromSelectedFile(stream);
        //        }
        //    }
        //}
    }
}

