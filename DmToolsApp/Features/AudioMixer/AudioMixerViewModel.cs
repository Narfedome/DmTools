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
        private readonly TutorialService _tutorial;

        private Scene? _activeScene;
        private readonly Dictionary<ChannelStripViewModel, CancellationTokenSource> _pendingSaves = new();

        public AudioMixerViewModel(
            AudioMixerService audioMixerService,
            ILibraryPickerService pickerService,
            ISceneDataService sceneDataService,
            TutorialService tutorial)
        {
            _audioMixerService = audioMixerService;
            _pickerService = pickerService;
            _sceneDataService = sceneDataService;
            _tutorial = tutorial;
        }

        public bool ShowTutorialHint => _tutorial.CurrentStep is
            TutorialService.StepAddChannel or TutorialService.StepPickTrack or
            TutorialService.StepAddSecondChannel or TutorialService.StepPickSecondTrack;

        public string TutorialHintTitle => _tutorial.CurrentStep switch
        {
            TutorialService.StepAddChannel => Loc["TutorialAddChannelTitle"],
            TutorialService.StepPickTrack => Loc["TutorialPickTrackTitle"],
            TutorialService.StepAddSecondChannel => Loc["TutorialAddSecondChannelTitle"],
            TutorialService.StepPickSecondTrack => Loc["TutorialPickSecondTrackTitle"],
            _ => string.Empty
        };

        public string TutorialHintDescription => _tutorial.CurrentStep switch
        {
            TutorialService.StepAddChannel => Loc["TutorialAddChannelDesc"],
            TutorialService.StepPickTrack => Loc["TutorialPickTrackDesc"],
            TutorialService.StepAddSecondChannel => Loc["TutorialAddSecondChannelDesc"],
            TutorialService.StepPickSecondTrack => Loc["TutorialPickSecondTrackDesc"],
            _ => string.Empty
        };

        private void RefreshTutorialHint()
        {
            OnPropertyChanged(nameof(ShowTutorialHint));
            OnPropertyChanged(nameof(TutorialHintTitle));
            OnPropertyChanged(nameof(TutorialHintDescription));
        }

        [RelayCommand]
        public void SkipTutorial()
        {
            _tutorial.Skip();
            RefreshTutorialHint();
        }

        // ── Channels ──────────────────────────────────────────────

        [ObservableProperty]
        private ObservableCollection<ChannelStripViewModel> currentChannels = new();

        [RelayCommand]
        public void AddChannel()
        {
            var channel = new ChannelStripViewModel() { DisplayTrackName = (LocalizationService.Instance["ChannelNew"] + " " + (CurrentChannels.Count + 1)), IsPlaying = false };
            CurrentChannels.Add(channel);

            if (_tutorial.CurrentStep == TutorialService.StepAddChannel)
                _tutorial.Complete(TutorialService.StepAddChannel);
            else if (_tutorial.CurrentStep == TutorialService.StepAddSecondChannel)
                _tutorial.Complete(TutorialService.StepAddSecondChannel);
            RefreshTutorialHint();
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

        // Les boutons livre/gear/X de chaque strip sont liés à UNE seule instance de commande du
        // ViewModel (RelativeSource dans le template) : sans AllowConcurrentExecutions, le toolkit
        // passe CanExecute à false pendant toute l'exécution async, et TOUS les boutons liés
        // passent visuellement en état désactivé tant que le dialog est ouvert. Ce garde reprend
        // le seul rôle utile de ce blocage : empêcher un double-tap d'ouvrir deux dialogs.
        private bool _isStripDialogOpen;

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task RemoveChannel(ChannelStripViewModel channel)
        {
            if (channel == null || _isStripDialogOpen)
                return;
            if (channel.Player == null)
            {
                UnsubscribeChannel(channel);
                if (channel.SceneTrackId > 0)
                    await _sceneDataService.DeleteSceneTrackAsync(new SceneTrack { Id = channel.SceneTrackId });
                CurrentChannels.Remove(channel);
                return;
            }

            _isStripDialogOpen = true;
            try
            {
                // La lecture est suspendue le temps du dialogue, puis reprise UNIQUEMENT si elle
                // était en cours : un TogglePlay inconditionnel lançait la lecture d'un strip à
                // l'arrêt quand l'utilisateur annulait la suppression.
                var wasPlaying = channel.IsPlaying;
                channel.Pause();

                bool confirm = await ConfirmAsync(Loc["DialogDelete"], string.Format(Loc["DialogRemoveChannel"], channel.DisplayTrackName));

                if (!confirm)
                {
                    if (wasPlaying)
                        channel.Play();
                    return;
                }

                channel.Stop();
                UnsubscribeChannel(channel);
                channel.DisposePlayer();
                if (channel.SceneTrackId > 0)
                    await _sceneDataService.DeleteSceneTrackAsync(new SceneTrack { Id = channel.SceneTrackId });
                CurrentChannels.Remove(channel);
            }
            finally
            {
                _isStripDialogOpen = false;
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task PickLibraryItem(ChannelStripViewModel channel)
        {
            if (channel == null || _isStripDialogOpen) return;

            _isStripDialogOpen = true;
            try
            {
                var selectedLibraryItem = await _pickerService.PickTrackAsync();

                if (selectedLibraryItem is not Track selectedTrack || !File.Exists(selectedTrack.FilePath))
                    return;

                channel.Player = await _audioMixerService.CreatePlayerAsync(selectedTrack.FilePath);
                channel.Track = selectedTrack;
                channel.DisplayTrackName = selectedTrack.Title;

                await SaveChannelAsSceneTrack(channel);

                bool tutorialFinished = false;
                if (_tutorial.CurrentStep == TutorialService.StepPickTrack)
                    tutorialFinished = _tutorial.Complete(TutorialService.StepPickTrack);
                else if (_tutorial.CurrentStep == TutorialService.StepPickSecondTrack)
                    tutorialFinished = _tutorial.Complete(TutorialService.StepPickSecondTrack);
                RefreshTutorialHint();
                if (tutorialFinished)
                    await ShowInfoAsync(Loc["TutorialDoneTitle"], Loc["TutorialDoneDesc"]);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
            }
            finally
            {
                _isStripDialogOpen = false;
            }
        }

        /// <summary>
        /// Ouvre les paramètres de la piste assignée au channel strip dans une boîte de dialogue.
        /// Save : applique au strip (le volume agit en direct sur le player) et persiste ; Cancel
        /// (ou tap à côté) : referme sans rien toucher.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task OpenChannelSettings(ChannelStripViewModel channel)
        {
            if (channel == null || _activeScene == null || channel.SceneTrackId == 0 || _isStripDialogOpen)
                return;

            _isStripDialogOpen = true;
            try
            {
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
                    channel.IsFadeOut,
                    persisted.AutoPlay);

                var saved = await ShowDialogAsync(dialog);
                if (!saved)
                    return;

                channel.Volume = dialog.VolumeValue;
                channel.IsLooping = dialog.IsLoopingValue;
                channel.IsFadeIn = dialog.FadeInValue;
                channel.IsFadeOut = dialog.FadeOutValue;

                // Les affectations ci-dessus viennent de déclencher une sauvegarde debouncée,
                // redondante avec la sauvegarde immédiate et complète qui suit : on l'annule.
                if (_pendingSaves.TryGetValue(channel, out var pendingCts))
                {
                    pendingCts.Cancel();
                    _pendingSaves.Remove(channel);
                }

                await _sceneDataService.UpdateSceneTrackAsync(
                    channel.SceneTrackId, dialog.VolumeValue, dialog.IsLoopingValue, dialog.AutoPlayValue, dialog.FadeInValue, dialog.FadeOutValue);
            }
            finally
            {
                _isStripDialogOpen = false;
            }
        }

        /// <summary>
        /// Vide le mixer proprement : désabonnement des sauvegardes auto et libération des players
        /// natifs (FileStream Windows / MediaPlayer Android). Utilisé au changement de scène et
        /// avant la suppression massive de pistes (un player même en pause verrouille son fichier
        /// sur Windows).
        /// </summary>
        public void ClearChannels()
        {
            foreach (var c in CurrentChannels)
            {
                UnsubscribeChannel(c);
                c.DisposePlayer();
            }
            CurrentChannels.Clear();
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
                FadeOut = channel.IsFadeOut,
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
                // Sauvegarde partielle : l'AutoPlay est un réglage explicite de l'utilisateur
                // (dialog de paramètres / page des pistes de scène), il ne doit pas être écrasé
                // par l'état de lecture du moment à chaque changement de volume ou de boucle.
                await _sceneDataService.UpdateSceneTrackSettingsAsync(
                    channel.SceneTrackId, channel.Volume, channel.IsLooping, channel.IsFadeIn, channel.IsFadeOut);
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
            // Cf. DebouncedSaveChannel : on ne persiste que les réglages du strip, jamais
            // l'AutoPlay (réglage explicite, qui serait sinon écrasé par l'état de lecture).
            var tasks = CurrentChannels
                .Where(c => c.SceneTrackId > 0)
                .Select(c => _sceneDataService.UpdateSceneTrackSettingsAsync(
                    c.SceneTrackId, c.Volume, c.IsLooping, c.IsFadeIn, c.IsFadeOut));
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
            RefreshTutorialHint();
        }

        [RelayCommand]
        public async Task SelectSession()
        {
            if (!Sessions.Any()) return;
            // Sélection par index et non par titre : deux chapitres homonymes doivent rester
            // sélectionnables individuellement.
            var index = await ShowActionSheetIndexAsync(Loc["MixerChapter"], Sessions.Select(s => s.Title).ToArray());
            if (index >= 0) SelectedSession = Sessions[index];
        }

        [RelayCommand]
        public async Task SelectScene()
        {
            if (!Scenes.Any()) return;
            var index = await ShowActionSheetIndexAsync(Loc["MixerScene"], Scenes.Select(s => s.Title).ToArray());
            if (index >= 0) SelectedScene = Scenes[index];
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

                    ClearChannels();

                    _activeScene = SelectedScene;

                    var sceneTracks = await _sceneDataService.GetSceneTracksAsync(SelectedScene.Id);

                    // Crée tous les lecteurs en parallèle plutôt que d'attendre chaque piste l'une
                    // après l'autre : le temps de chargement de la scène devient celui de la piste
                    // la plus lente au lieu de la somme de toutes. Une piste illisible (fichier
                    // corrompu, verrouillé...) est simplement ignorée au lieu de faire échouer
                    // toute la scène (et de laisser fuiter les players déjà créés).
                    var playable = sceneTracks.Where(st => File.Exists(st.Track.FilePath)).ToList();
                    var players = await Task.WhenAll(playable.Select(async st =>
                    {
                        try { return await _audioMixerService.CreatePlayerAsync(st.Track.FilePath); }
                        catch { return null; }
                    }));

                    for (int i = 0; i < playable.Count; i++)
                    {
                        if (players[i] == null)
                            continue;

                        var st = playable[i];
                        var channel = new ChannelStripViewModel
                        {
                            SceneTrackId = st.Id,
                            Track = st.Track,
                            DisplayTrackName = st.Track.Title,
                            Volume = st.Volume,
                            IsLooping = st.IsLooping,
                            IsFadeIn = st.FadeIn,
                            IsFadeOut = st.FadeOut,
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
