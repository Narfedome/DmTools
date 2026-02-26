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
            var channel = new ChannelStripViewModel() { Name = ("Channel " + (currentChannels.Count + 1)), IsPlaying = false };
            CurrentChannels.Add(channel);
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
            channel.TogglePlay();

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

                Track selectedTrack = (Track)await _pickerService.PickTrackAsync();

                if (selectedTrack == null)
                    return;

                if (!string.IsNullOrEmpty(selectedTrack.FilePath))
                {
                    await using var stream = File.OpenRead(selectedTrack.FilePath);

                    channel.Player = _audioMixerService.CreatePlayerFromSelectedFile(stream);
                    channel.Track = selectedTrack;
                    channel.Name = selectedTrack.Title;

                    channel.TogglePlay();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public async Task LoadChannels()
        {
            foreach (var channel in CurrentChannels)
            {
                if (channel.Player == null)
                {
                    var stream = File.OpenRead(channel.Track.FilePath);
                    //channel.Player = await _audioMixerService.CreatePlayerFromSelectedFile(stream);
                }
            }
        }
    }
}

