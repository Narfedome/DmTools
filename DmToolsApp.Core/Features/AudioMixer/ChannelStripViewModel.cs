using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models.Library;
using Plugin.Maui.Audio;

namespace DmToolsApp.Components
{
    public partial class ChannelStripViewModel : ObservableObject
    {
        // Durée/pas des fades, surchargeables par les tests (InternalsVisibleTo) pour ne pas
        // attendre 1,5 s réelle par fade.
        internal static TimeSpan FadeDuration = TimeSpan.FromMilliseconds(1500);
        internal static int FadeSteps = 30;

        /// <summary>
        /// Renvoi vers le thread UI pour les callbacks du player (fin de lecture), branché sur
        /// MainThread par l'appli au démarrage. Par défaut : exécution synchrone (tests, ou tout
        /// hôte sans dispatcher).
        /// </summary>
        public static Action<Action> UiDispatcher { get; set; } = action => action();

        private CancellationTokenSource? _fadeCts;

        private IAudioPlayer? _player;
        public IAudioPlayer? Player
        {
            get => _player;
            set
            {
                if (ReferenceEquals(_player, value))
                    return;

                CancelFade();
                if (_player != null)
                {
                    _player.PlaybackEnded -= OnPlaybackEnded;
                    // Le strip est propriétaire de son player : un player remplacé ne sera plus
                    // jamais utilisé. Sans Dispose, le FileStream sous-jacent reste ouvert sur
                    // Windows (fichier verrouillé) et le MediaPlayer natif fuit sur Android.
                    _player.Dispose();
                }

                _player = value;
                if (_player != null)
                {
                    _player.Volume = Volume;
                    _player.Loop = IsLooping;
                    _player.PlaybackEnded += OnPlaybackEnded;
                }
            }
        }

        /// <summary>
        /// Libère le player natif du strip (fin de vie : retrait du channel, changement de scène,
        /// purge de la bibliothèque). Le setter de Player se charge du Dispose effectif.
        /// </summary>
        public void DisposePlayer()
        {
            Player = null;
            IsPlaying = false;
        }

        /// <summary>
        /// Fin de lecture naturelle (piste non bouclée) : sans ça le strip restait visuellement en
        /// lecture (bordure active, icône pause). On rembobine aussi pour qu'un nouveau play
        /// reparte du début.
        /// </summary>
        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            UiDispatcher(() =>
            {
                if (!ReferenceEquals(sender, _player) || _player == null)
                    return;

                // Certaines plateformes notifient la fin de chaque itération même en boucle : dans
                // ce cas la lecture continue, on ne touche à rien.
                if (_player.IsPlaying)
                    return;

                _player.Seek(0);
                IsPlaying = false;
            });
        }

        [ObservableProperty]
        private Track? track = new Track();

        [ObservableProperty]
        private string? displayTrackName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayVolume))]
        private double volume = 1;

        public int DisplayVolume => (int)(Volume * 100);

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLooping;

        // Le strip existe (et apparaît, dans l'ordre) avant que son lecteur natif soit prêt :
        // AudioMixerViewModel.LoadScene affiche tous les strips d'un coup puis crée les lecteurs
        // en tâche de fond, au lieu de bloquer l'affichage de la scène entière derrière un unique
        // spinner tant que la piste la plus lente à charger n'est pas prête.
        [ObservableProperty]
        private bool isLoading;

        // Réglage persisté sur la piste de scène (édité via SceneTracksPage) : au lancement de la
        // lecture, le volume monte progressivement de 0 jusqu'au volume du strip.
        [ObservableProperty]
        private bool isFadeIn;

        // Réglage persisté sur la piste de scène : le bouton stop du strip fait un fade out
        // plutôt qu'un arrêt sec.
        [ObservableProperty]
        private bool isFadeOut;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSceneTrack))]
        private int sceneTrackId;

        // Le bouton de paramètres (gear) n'a de sens que si le strip est persisté comme piste de
        // scène : c'est là que vivent les réglages édités par SceneTracksPage.
        public bool HasSceneTrack => SceneTrackId > 0;

        partial void OnVolumeChanged(double oldValue, double newValue)
        {
            if (Player != null)
                Player.Volume = newValue;

            if (Track != null)
                Track.Volume = newValue;
        }

        partial void OnIsLoopingChanged(bool value)
        {
            if (Player != null)
                Player.Loop = value;
        }

        [RelayCommand]
        public void TogglePlay()
        {
            if (Player == null)
                return;

            if (Player.IsPlaying)
            {
                Player.Pause();
                IsPlaying = false;
            }
            else
            {
                Play();
            }
        }

        public void Play()
        {
            if (Player == null || Player.IsPlaying)
                return;

            CancelFade();

            if (IsFadeIn)
            {
                _ = FadeInAsync();
                return;
            }

            Player.Volume = Volume;
            Player.Play();
            IsPlaying = true;
        }

        /// <summary>
        /// Démarre la lecture avec une montée progressive du volume (miroir de FadeOut). Interrompu
        /// (stop, fade out, replay...), le volume est ramené directement à sa valeur cible : c'est
        /// l'action suivante qui décide de la suite.
        /// </summary>
        private async Task FadeInAsync()
        {
            if (Player == null)
                return;

            var cts = new CancellationTokenSource();
            _fadeCts = cts;
            var token = cts.Token;

            var targetVolume = Volume;
            var stepDelay = (int)(FadeDuration.TotalMilliseconds / FadeSteps);
            var volumeStep = targetVolume / FadeSteps;

            Player.Volume = 0;
            Player.Play();
            IsPlaying = true;

            try
            {
                for (int i = 0; i < FadeSteps; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(stepDelay, token);
                    if (Player != null)
                        Player.Volume = Math.Min(targetVolume, volumeStep * (i + 1));
                }

                if (Player != null)
                    Player.Volume = targetVolume;
            }
            catch (OperationCanceledException)
            {
                if (Player != null)
                    Player.Volume = targetVolume;
            }
            finally
            {
                // Ne remet à zéro que si un autre fade n'a pas déjà pris la main (sinon on
                // disposerait SON CancellationTokenSource et son Task.Delay planterait).
                if (_fadeCts == cts)
                    _fadeCts = null;
                cts.Dispose();
            }
        }

        public void Pause()
        {
            if (Player == null)
                return;

            Player.Pause();
            IsPlaying = false;
        }

        [RelayCommand]
        public void Stop()
        {
            if (Player == null) return;

            CancelFade();
            Player.Stop();
            IsPlaying = false;
        }

        /// <summary>
        /// Bouton stop du strip : fade out ou arrêt sec selon le réglage de la piste. Les actions
        /// globales du mixer (Fade Out All, Stop All) restent explicites et n'en tiennent pas compte.
        /// </summary>
        [RelayCommand]
        public async Task StopPlayback()
        {
            if (IsFadeOut)
                await FadeOut();
            else
                Stop();
        }

        [RelayCommand]
        public async Task FadeOut()
        {
            if (Player == null || !Player.IsPlaying)
                return;

            CancelFade();
            var cts = new CancellationTokenSource();
            _fadeCts = cts;
            var token = cts.Token;

            var startVolume = Volume;
            var stepDelay = (int)(FadeDuration.TotalMilliseconds / FadeSteps);
            var volumeStep = startVolume / FadeSteps;

            try
            {
                for (int i = 0; i < FadeSteps; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(stepDelay, token);
                    if (Player != null)
                        Player.Volume = Math.Max(0, startVolume - volumeStep * (i + 1));
                }

                if (Player != null)
                {
                    Player.Stop();
                    Player.Volume = startVolume;
                    IsPlaying = false;
                }
            }
            catch (OperationCanceledException)
            {
                if (Player != null)
                    Player.Volume = startVolume;
            }
            finally
            {
                // Cf. FadeInAsync : ne touche pas à _fadeCts si un autre fade l'a déjà remplacé.
                if (_fadeCts == cts)
                    _fadeCts = null;
                cts.Dispose();
            }
        }

        private void CancelFade()
        {
            _fadeCts?.Cancel();
        }
    }
}
