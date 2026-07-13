
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
            // Création sortie du thread UI car le lecteur natif peut être coûteux à construire.
#if WINDOWS
            // Sur Windows, CreatePlayer(string) du plugin préfixe le chemin par "ms-appx:///Assets/"
            // (réservé aux assets packagés de l'appli) : un chemin absolu devient une source
            // invalide → MediaFailed, IsPlaying toujours false, aucun son. On passe donc par un
            // FileStream, que le plugin enveloppe via AsRandomAccessStream : lecture progressive,
            // sans copie mémoire (la copie intégrale ne concerne que les MemoryStream). Le stream
            // doit rester ouvert pendant toute la vie du player.
            return Task.Run(() => audioManager.CreatePlayer(File.OpenRead(filePath)));
#else
            // Android/iOS résolvent correctement un chemin de fichier absolu (flux natif progressif).
            return Task.Run(() => audioManager.CreatePlayer(filePath));
#endif
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