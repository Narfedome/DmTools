using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components;
using DmToolsApp.Components.Dialogs;
using DmToolsApp.Features.Library;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace DmToolsApp.Features.AudioMixer
{
    public partial class AudioMixerViewModel : BaseViewModel
    {
        private readonly AudioMixerService _audioMixerService;
        private readonly ILibraryPickerService _pickerService;
        private readonly ISceneDataService _sceneDataService;

        private Scene? _activeScene;
        private readonly Dictionary<ChannelStripViewModel, CancellationTokenSource> _pendingSaves = new();

        public AudioMixerViewModel(
            AudioMixerService audioMixerService,
            ILibraryPickerService pickerService,
            ISceneDataService sceneDataService)
        {
            _audioMixerService = audioMixerService;
            _pickerService = pickerService;
            _sceneDataService = sceneDataService;
        }

        // ── Channels ──────────────────────────────────────────────

        [ObservableProperty]
        private ObservableCollection<ChannelStripViewModel> currentChannels = new();

        [RelayCommand]
        public async Task AddChannel()
        {
            var channel = new ChannelStripViewModel() { DisplayTrackName = (LocalizationService.Instance["ChannelNew"] + " " + (CurrentChannels.Count + 1)), IsPlaying = false };
            CurrentChannels.Add(channel);
        }

        [RelayCommand]
        public void PlayAll()
        {
            foreach (var c in CurrentChannels)
                c.Play();
        }

        [RelayCommand]
        public void StopAll()
        {
            foreach (var c in CurrentChannels)
                c.Stop();
        }

        [RelayCommand]
        public async Task FadeOutAll()
        {
            var tasks = CurrentChannels.Select(c => c.FadeOut()).ToArray();
            await Task.WhenAll(tasks);
        }

        [RelayCommand]
        public async Task RemoveChannel(ChannelStripViewModel channel)
        {
            if (channel == null)
                return;
            if (channel.Player == null)
            {
                UnsubscribeChannel(channel);
                if (channel.SceneTrackId > 0)
                    await _sceneDataService.DeleteSceneTrackAsync(new SceneTrack { Id = channel.SceneTrackId });
                CurrentChannels.Remove(channel);
                return;
            }
            channel.Pause();

            bool confirm = await ConfirmAsync(Loc["DialogDelete"], string.Format(Loc["DialogRemoveChannel"], channel.DisplayTrackName));

            if (!confirm)
            {
                channel.TogglePlay();
                return;
            }

            channel.Stop();
            UnsubscribeChannel(channel);
            if (channel.SceneTrackId > 0)
                await _sceneDataService.DeleteSceneTrackAsync(new SceneTrack { Id = channel.SceneTrackId });
            CurrentChannels.Remove(channel);
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task PickLibraryItem(ChannelStripViewModel channel)
        {
            try
            {
                if (channel == null) return;
                var selectedLibraryItem = await _pickerService.PickTrackAsync();

                if (selectedLibraryItem is not Track selectedTrack || !File.Exists(selectedTrack.FilePath))
                    return;

                channel.Player = await _audioMixerService.CreatePlayerAsync(selectedTrack.FilePath);
                channel.Track = selectedTrack;
                channel.DisplayTrackName = selectedTrack.Title;

                await SaveChannelAsSceneTrack(channel);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
            }
        }

        /// <summary>
        /// Ouvre les paramètres de la piste assignée au channel strip dans une boîte de dialogue.
        /// Save : applique au strip (le volume agit en direct sur le player) et persiste ; Cancel
        /// (ou tap à côté) : referme sans rien toucher.
        /// </summary>
        [RelayCommand]
        public async Task OpenChannelSettings(ChannelStripViewModel channel)
        {
            if (channel == null || _activeScene == null || channel.SceneTrackId == 0)
                return;

            // L'AutoPlay n'est pas porté par le strip : on relit l'état persisté de la piste.
            var sceneTracks = await _sceneDataService.GetSceneTracksAsync(_activeScene.Id);
            var persisted = sceneTracks.FirstOrDefault(st => st.Id == channel.SceneTrackId);
            if (persisted == null)
                return;

            var dialog = new ChannelSettingsDialog(
                channel.DisplayTrackName ?? persisted.Track.Title,
                channel.Volume,
                channel.IsLooping,
                channel.IsFadeIn,
                persisted.AutoPlay);

            var saved = await ShowDialogAsync(dialog);
            if (!saved)
                return;

            channel.Volume = dialog.VolumeValue;
            channel.IsLooping = dialog.IsLoopingValue;
            channel.IsFadeIn = dialog.FadeInValue;

            // Les affectations ci-dessus viennent de déclencher une sauvegarde debouncée (qui
            // écraserait l'AutoPlay avec l'état de lecture) : on l'annule au profit d'une
            // sauvegarde immédiate et complète.
            if (_pendingSaves.TryGetValue(channel, out var pendingCts))
            {
                pendingCts.Cancel();
                _pendingSaves.Remove(channel);
            }

            await _sceneDataService.UpdateSceneTrackAsync(
                channel.SceneTrackId, dialog.VolumeValue, dialog.IsLoopingValue, dialog.AutoPlayValue, dialog.FadeInValue);
        }

        private async Task SaveChannelAsSceneTrack(ChannelStripViewModel channel)
        {
            if (_activeScene == null || channel.Track == null || channel.Track.Id == 0) return;

            var sceneTrack = new SceneTrack
            {
                Id = channel.SceneTrackId,
                SceneId = _activeScene.Id,
                Track = channel.Track,
                Volume = channel.Volume,
                IsLooping = channel.IsLooping,
                FadeIn = channel.IsFadeIn,
                AutoPlay = channel.IsPlaying,
                Position = CurrentChannels.IndexOf(channel)
            };

            await _sceneDataService.SaveSceneTrackAsync(sceneTrack);
            channel.SceneTrackId = sceneTrack.Id;

            SubscribeChannel(channel);
        }

        // ── Subscriptions pour sauvegarde automatique ─────────────

        private void SubscribeChannel(ChannelStripViewModel channel)
        {
            channel.PropertyChanged -= OnChannelPropertyChanged;
            channel.PropertyChanged += OnChannelPropertyChanged;
        }

        private void UnsubscribeChannel(ChannelStripViewModel channel)
        {
            channel.PropertyChanged -= OnChannelPropertyChanged;
            if (_pendingSaves.TryGetValue(channel, out var cts))
            {
                cts.Cancel();
                _pendingSaves.Remove(channel);
            }
        }

        private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ChannelStripViewModel channel) return;
            if (e.PropertyName is nameof(ChannelStripViewModel.Volume) or nameof(ChannelStripViewModel.IsLooping))
                _ = DebouncedSaveChannel(channel);
        }

        private async Task DebouncedSaveChannel(ChannelStripViewModel channel)
        {
            if (channel.SceneTrackId == 0) return;

            if (_pendingSaves.TryGetValue(channel, out var existingCts))
                existingCts.Cancel();

            var cts = new CancellationTokenSource();
            _pendingSaves[channel] = cts;

            try
            {
                await Task.Delay(500, cts.Token);
                await _sceneDataService.UpdateSceneTrackAsync(
                    channel.SceneTrackId, channel.Volume, channel.IsLooping, channel.IsPlaying, channel.IsFadeIn);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_pendingSaves.TryGetValue(channel, out var current) && current == cts)
                    _pendingSaves.Remove(channel);
                cts.Dispose();
            }
        }

        // ── Sélecteur de scène ────────────────────────────────────

        [ObservableProperty]
        private ObservableCollection<Session> sessions = new();

        [ObservableProperty]
        private ObservableCollection<Scene> scenes = new();

        [ObservableProperty]
        private Session? selectedSession;

        [ObservableProperty]
        private Scene? selectedScene;

        [ObservableProperty]
        private int sceneIndex = 1;

        [ObservableProperty]
        private int sceneCount = 0;

        public bool CanGoPrevScene => SceneIndex > 1;
        public bool CanGoNextScene => SceneIndex < SceneCount;

        public string SelectedSessionLabel => SelectedSession?.Title ?? Loc["MixerChapter"];
        public string SelectedSceneLabel   => SelectedScene?.Title   ?? Loc["MixerScene"];

        private bool _suppressHandlers;

        partial void OnSelectedSessionChanged(Session? value)
        {
            OnPropertyChanged(nameof(SelectedSessionLabel));
            if (_suppressHandlers) return;
            SelectedScene = null;
            Scenes.Clear();
            if (value != null)
                _ = LoadScenesAsync(value.Id);
        }

        partial void OnSelectedSceneChanged(Scene? value)
        {
            OnPropertyChanged(nameof(SelectedSceneLabel));
            if (_suppressHandlers) return;
            SceneIndex = value != null ? Scenes.IndexOf(value) + 1 : 0;
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));
            if (value != null)
                _ = LoadScene();
        }

        private async Task LoadScenesAsync(int sessionId)
        {
            var list = await _sceneDataService.GetScenesAsync(sessionId);
            Scenes = new ObservableCollection<Scene>(list);
            SceneCount = list.Count;
            SelectedScene = Scenes.FirstOrDefault();
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));
        }

        private async Task SaveCurrentSceneAsync()
        {
            var tasks = CurrentChannels
                .Where(c => c.SceneTrackId > 0)
                .Select(c => _sceneDataService.UpdateSceneTrackAsync(
                    c.SceneTrackId, c.Volume, c.IsLooping, c.IsPlaying, c.IsFadeIn));
            await Task.WhenAll(tasks);
        }

        public async Task LoadFromPlayAsync(Campaign campaign, Session session, Scene scene)
        {
            await SaveCurrentSceneAsync();

            // Charger les chapitres de la campagne
            var sessionList = await _sceneDataService.GetSessionsAsync(campaign.Id);
            Sessions = new ObservableCollection<Session>(sessionList);

            // Charger les scènes du chapitre sans déclencher les handlers de cascade
            var sceneList = await _sceneDataService.GetScenesAsync(session.Id);
            Scenes = new ObservableCollection<Scene>(sceneList);
            SceneCount = sceneList.Count;

            // Matcher par Id pour que le Picker trouve l'instance dans la collection
            var matchedSession = Sessions.FirstOrDefault(s => s.Id == session.Id) ?? session;
            var matchedScene = Scenes.FirstOrDefault(s => s.Id == scene.Id) ?? scene;

            _suppressHandlers = true;
            SelectedSession = matchedSession;
            SelectedScene = matchedScene;
            _suppressHandlers = false;

            SceneIndex = Scenes.IndexOf(matchedScene) + 1;
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));

            _activeScene = matchedScene;
            await LoadScene();
        }

        [RelayCommand]
        public async Task SelectSession()
        {
            if (!Sessions.Any()) return;
            var result = await ShowActionSheetAsync(Loc["MixerChapter"], Sessions.Select(s => s.Title).ToArray());
            if (result != null) SelectedSession = Sessions.First(s => s.Title == result);
        }

        [RelayCommand]
        public async Task SelectScene()
        {
            if (!Scenes.Any()) return;
            var result = await ShowActionSheetAsync(Loc["MixerScene"], Scenes.Select(s => s.Title).ToArray());
            if (result != null) SelectedScene = Scenes.First(s => s.Title == result);
        }

        [RelayCommand]
        public void PrevScene()
        {
            if (!CanGoPrevScene) return;
            SelectedScene = Scenes[SceneIndex - 2];
        }

        [RelayCommand]
        public void NextScene()
        {
            if (!CanGoNextScene) return;
            SelectedScene = Scenes[SceneIndex];
        }

        // Empêche deux chargements de scène concurrents (l'UI est bloquée pendant le chargement,
        // mais LoadScene peut aussi être déclenché programmatiquement via LoadFromPlayAsync).
        private bool _isLoadingScene;

        [RelayCommand]
        public async Task LoadScene()
        {
            if (SelectedScene == null || _isLoadingScene) return;
            _isLoadingScene = true;

            // Bloque toute la navigation (onglets, changement de scène...) pendant la (re)création
            // des channel strips : changer de scène ou d'onglet en plein chargement laisserait des
            // players orphelins ou un état de mixer incohérent.
            var shell = Shell.Current;
            if (shell != null) shell.IsEnabled = false;

            try
            {
                await Loading.RunAsync(async () =>
                {
                    await SaveCurrentSceneAsync();

                    // Fade out tous les channels actifs
                    var fadeTasks = CurrentChannels.Where(c => c.IsPlaying).Select(c => c.FadeOut()).ToArray();
                    await Task.WhenAll(fadeTasks);

                    foreach (var c in CurrentChannels)
                        UnsubscribeChannel(c);
                    CurrentChannels.Clear();

                    _activeScene = SelectedScene;

                    var sceneTracks = await _sceneDataService.GetSceneTracksAsync(SelectedScene.Id);

                    // Crée tous les lecteurs en parallèle plutôt que d'attendre chaque piste l'une
                    // après l'autre : le temps de chargement de la scène devient celui de la piste
                    // la plus lente au lieu de la somme de toutes.
                    var playable = sceneTracks.Where(st => File.Exists(st.Track.FilePath)).ToList();
                    var players = await Task.WhenAll(playable.Select(st => _audioMixerService.CreatePlayerAsync(st.Track.FilePath)));

                    for (int i = 0; i < playable.Count; i++)
                    {
                        var st = playable[i];
                        var channel = new ChannelStripViewModel
                        {
                            SceneTrackId = st.Id,
                            Track = st.Track,
                            DisplayTrackName = st.Track.Title,
                            Volume = st.Volume,
                            IsLooping = st.IsLooping,
                            IsFadeIn = st.FadeIn,
                            Player = players[i]
                        };

                        SubscribeChannel(channel);
                        CurrentChannels.Add(channel);
                    }
                });
            }
            finally
            {
                if (shell != null) shell.IsEnabled = true;
                _isLoadingScene = false;
            }
        }
    }
}
