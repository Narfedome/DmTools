using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmTools.Components;
using DmTools.Services;
using System.Collections.ObjectModel;

namespace DmTools.Features.AudioMixer
{
    public partial class AudioMixerViewModel : ObservableObject
    {
        private readonly AudioMixerService _audioMixerService;
        public AudioMixerViewModel(AudioMixerService audioMixerService)
        {
            _audioMixerService = audioMixerService;
            Channels = new ObservableCollection<ChannelStripViewModel>();
        }

        [ObservableProperty]
        private ObservableCollection<ChannelStripViewModel> channels = new();

        [RelayCommand]
        public async Task AddChannel()
        {
            var channel = new ChannelStripViewModel() { Name=("Channel " + (channels.Count + 1)), IsPlaying = false };
            Channels.Add(channel);
        }

        [RelayCommand]
        public void StopAll()
        {
            foreach (var c in Channels)
                c.Stop();
        }

        [RelayCommand]
        public async Task RemoveChannel(ChannelStripViewModel channel)
        {
            if (channel == null)
                return;
            if (channel.Player == null)
            {
                Channels.Remove(channel);
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
            Channels.Remove(channel);
        }

        [RelayCommand]
        public async Task PickFile(ChannelStripViewModel channel)
        {
            try
            {

                if (channel != null)
                {

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
                        var localPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);

                        channel.Player = await _audioMixerService.CreatePlayerFromSelectedFile(stream);
                        channel.Source = localPath;
                        channel.Name = result.FileName;
                        channel.TogglePlay();
                    }
                }
            }
            catch (Exception ex)
            {
                // tu peux logguer ou afficher un message
                Console.WriteLine(ex);
            }
        }
    }
}
