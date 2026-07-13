
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

        public ChannelStripViewModel AddChannel(string? file = null)
        {
            IAudioPlayer? player = null;

            if (!string.IsNullOrWhiteSpace(file))
            {
                using var stream = File.OpenRead(file);
                player = audioManager.CreatePlayer(stream);
            }

            var channel = new ChannelStripViewModel
            {
                Player = player,
                DisplayTrackName = file ?? "New Channel"
            };

            Channels.Add(channel);
            return channel;
        }

        public Task<IAudioPlayer> CreatePlayerAsync(string filePath)
        {
            // Passe le chemin de fichier plutôt qu'un Stream : sur Windows, CreatePlayer(Stream)
            // copie tout le flux en mémoire avant de créer le lecteur, alors que CreatePlayer(string)
            // s'appuie sur un flux natif progressif. Reste sorti du thread UI car la création du
            // lecteur natif peut malgré tout être coûteuse.
            return Task.Run(() => audioManager.CreatePlayer(filePath));
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