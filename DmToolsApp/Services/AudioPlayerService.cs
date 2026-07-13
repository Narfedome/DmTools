using Plugin.Maui.Audio;

namespace DmToolsApp.Services;

public class AudioPlayerService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    private Stream? _playerStream;

    public string? CurrentFile { get; private set; }

    public event Action<string?>? OnStateChanged;

    public AudioPlayerService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public void Toggle(string filePath)
    {
        if(string.IsNullOrEmpty(filePath))
        { 
            return;
        }

        if (_player != null && CurrentFile == filePath && _player.IsPlaying)
        {
            _player.Stop();
            Cleanup();
            return;
        }

        Stop();

#if WINDOWS
        // Sur Windows, CreatePlayer(string) du plugin préfixe le chemin par "ms-appx:///Assets/"
        // (réservé aux assets packagés) : un chemin absolu devient une source invalide et muette.
        // On passe par un FileStream (enveloppé progressivement, pas de copie mémoire), gardé
        // ouvert le temps de la lecture et refermé au Cleanup.
        _playerStream = File.OpenRead(filePath);
        _player = _audioManager.CreatePlayer(_playerStream);
#else
        // Android/iOS résolvent correctement un chemin de fichier absolu (flux natif progressif).
        _player = _audioManager.CreatePlayer(filePath);
#endif

        // Fin de lecture naturelle : sans ça, le bouton play de la tuile restait visuellement en
        // lecture une fois le fichier terminé. On vérifie que le player notifiant est toujours le
        // player courant (l'utilisateur a pu lancer une autre piste entre-temps).
        var player = _player;
        _player.PlaybackEnded += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ReferenceEquals(_player, player))
                Cleanup();
        });

        _player.Play();

        CurrentFile = filePath;
        OnStateChanged?.Invoke(CurrentFile);
    }

    public void Stop()
    {
        if (_player != null)
        {
            _player.Stop();
            Cleanup();
        }
    }

    private void Cleanup()
    {
        _player?.Dispose();
        _player = null;
        _playerStream?.Dispose();
        _playerStream = null;
        CurrentFile = null;

        OnStateChanged?.Invoke(null);
    }
}