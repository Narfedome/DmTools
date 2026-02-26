
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;
using DmToolsApp.Components;

namespace DmToolsApp.Services
{
    public class AudioMixerService
    {
        IAudioManager audioManager;
        public List<ChannelStripViewModel> Channels { get; } = new();
        private readonly List<IAudioPlayer> activePlayers = new();

        public AudioMixerService(IAudioManager audioManager)
        {
            this.audioManager = audioManager;
        }

        public async Task<ChannelStripViewModel> AddChannel(string? file = null)
        {
            IAudioPlayer? player = null;

            if (!string.IsNullOrWhiteSpace(file))
            {
                var stream = await FileSystem.OpenAppPackageFileAsync(file);
                player = audioManager.CreatePlayer(stream);
            }

            var channel = new ChannelStripViewModel
            {
                Player = player,
                Name = file ?? "New Channel"
            };

            Channels.Add(channel);
            return channel;
        }
        public IAudioPlayer CreatePlayerFromSelectedFile(Stream stream)
        {
            return audioManager.CreatePlayer(stream);
        }

        public async Task<IAudioPlayer> PlayLoop(string file, double volume = 1)
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(file);
            var player = audioManager.CreatePlayer(stream);

            player.Volume = volume;
            player.Loop = true;
            player.Play();

            activePlayers.Add(player);
            return player;
        }

        public async Task PlayOneShot(string file, double volume = 1)
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(file);
            var player = audioManager.CreatePlayer(stream);

            player.Volume = volume;
            player.Play();
        }

        public void StopAll()
        {
            foreach (var p in activePlayers)
                p.Stop();

            activePlayers.Clear();
        }
    }



}