using Plugin.Maui.Audio;

namespace DmToolsApp.Services;

public class AudioPlayerService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;

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

        // Passe le chemin de fichier plutôt qu'un Stream : sur Windows, CreatePlayer(Stream) copie
        // tout le flux en mémoire avant de créer le lecteur, alors que CreatePlayer(string) s'appuie
        // sur un flux natif progressif (pas de contrainte de seek ici, juste une prévisualisation).
        _player = _audioManager.CreatePlayer(filePath);

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
        CurrentFile = null;

        OnStateChanged?.Invoke(null);
    }
}