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
        public void AddChannel()
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

                var dialogViewModel = new ChannelSettingsDialogViewModel(
                    channel.DisplayTrackName ?? persisted.Track.Title,
                    channel.Volume,
                    channel.IsLooping,
                    channel.IsFadeIn,
                    channel.IsFadeOut,
                    persisted.AutoPlay);

                var saved = await ShowDialogAsync(new ChannelSettingsDialog(dialogViewModel));
                if (!saved)
                    return;

                channel.Volume = dialogViewModel.Volume;
                channel.IsLooping = dialogViewModel.IsLooping;
                channel.IsFadeIn = dialogViewModel.FadeIn;
                channel.IsFadeOut = dialogViewModel.FadeOut;

                // Les affectations ci-dessus viennent de déclencher une sauvegarde debouncée,
                // redondante avec la sauvegarde immédiate et complète qui suit : on l'annule.
                if (_pendingSaves.TryGetValue(channel, out var pendingCts))
                {
                    pendingCts.Cancel();
                    _pendingSaves.Remove(channel);
                }

                await _sceneDataService.UpdateSceneTrackAsync(
                    channel.SceneTrackId, dialogViewModel.Volume, dialogViewModel.IsLooping, dialogViewModel.AutoPlay, dialogViewModel.FadeIn, dialogViewModel.FadeOut);
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

        // Appelée depuis le code-behind d'AudioMixerPage (gestionnaire Drop) pour réordonner les
        // channel strips par glisser-déposer. Seuls les strips déjà persistés (SceneTrackId > 0)
        // sont inclus dans la sauvegarde : un strip tout juste ajouté (aucune piste assignée) n'a
        // pas encore de SceneTrack, sa position sera fixée normalement lors de son premier
        // SaveChannelAsSceneTrack (qui lit déjà l'index courant dans CurrentChannels).
        public async Task ReorderChannelsAsync(ChannelStripViewModel dragged, ChannelStripViewModel target)
        {
            if (dragged == null || target == null || ReferenceEquals(dragged, target)) return;

            var oldIndex = CurrentChannels.IndexOf(dragged);
            var newIndex = CurrentChannels.IndexOf(target);
            if (oldIndex < 0 || newIndex < 0) return;

            CurrentChannels.Move(oldIndex, newIndex);

            var orderedIds = CurrentChannels.Where(c => c.SceneTrackId > 0).Select(c => c.SceneTrackId).ToList();
            if (orderedIds.Count > 0)
                await _sceneDataService.ReorderSceneTracksAsync(orderedIds);
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
                _ = LoadScenesAfterSessionChangeAsync(value.Id);
        }

        partial void OnSelectedSceneChanged(Scene? value)
        {
            OnPropertyChanged(nameof(SelectedSceneLabel));
            if (_suppressHandlers) return;
            SceneIndex = value != null ? Scenes.IndexOf(value) + 1 : 0;
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));
            if (value != null)
                _ = LoadSceneAfterSelectionChangeAsync();
        }

        // Wrappers pour les deux fire-and-forget ci-dessus : les handlers de changement de propriete
        // sont void (impose par le generateur de source), pas moyen d'y faire un vrai await. Sans ce
        // wrapper, une erreur ici (DB, creation de lecteur...) disparaissait silencieusement au lieu
        // de remonter a l'utilisateur.
        private async Task LoadScenesAfterSessionChangeAsync(int sessionId)
        {
            try
            {
                await LoadScenesAsync(sessionId);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
            }
        }

        private async Task LoadSceneAfterSelectionChangeAsync()
        {
            try
            {
                await LoadScene();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(ex);
            }
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

        /// <summary>
        /// Appelé juste après avoir navigué vers AudioMixerPage (cf. CampaignViewModel.Launch).
        /// Sa propre préparation (sauvegarde, chargement chapitre/scène) ET celle de LoadScene
        /// tournent sous le MÊME Loading.RunAsync continu (au lieu d'appeler LoadScene() qui ouvre
        /// son propre empan Show/Hide séparé) : sans ça, IsLoading retombe à false entre les deux
        /// puis repasse à true, ce qui fait clignoter l'overlay - visible uniquement depuis ce
        /// chemin (l'accordéon), jamais lors d'un changement de scène classique dans le mixer.
        /// </summary>
        public async Task LoadFromPlayAsync(Campaign campaign, Session session, Scene scene)
        {
            if (_isLoadingScene) return;
            _isLoadingScene = true;

            var shell = Shell.Current;
            if (shell != null) shell.IsEnabled = false;

            try
            {
                List<SceneTrack> playable = new();

                await Loading.RunAsync(async () =>
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

                    playable = await PrepareSceneTracksAsync();
                });

                await PopulateChannelsAsync(playable);
            }
            finally
            {
                if (shell != null) shell.IsEnabled = true;
                _isLoadingScene = false;
            }
        }

        public bool IsActiveScene(int sceneId) => _activeScene?.Id == sceneId;
        public bool IsActiveSession(int sessionId) => _activeScene?.SessionId == sessionId;
        public bool IsActiveCampaign(int campaignId) => SelectedSession?.CampaignId == campaignId;

        /// <summary>
        /// Réinitialise le mixer quand la campagne/chapitre/scène affichée vient d'être supprimée
        /// ailleurs (page Campagnes) : libère les players et vide le sélecteur au lieu de laisser le
        /// mixer pointer sur des données qui n'existent plus en base.
        /// </summary>
        public void ResetActiveScene()
        {
            ClearChannels();
            _activeScene = null;

            _suppressHandlers = true;
            SelectedScene = null;
            SelectedSession = null;
            _suppressHandlers = false;

            Scenes.Clear();
            Sessions.Clear();
            SceneCount = 0;
            SceneIndex = 1;
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));
        }

        [RelayCommand]
        public async Task SelectSession()
        {
            if (!Sessions.Any()) return;
            // Sélection par index et non par titre : deux chapitres homonymes doivent rester
            // sélectionnables individuellement.
            var index = await ShowActionSheetIndexAsync(Loc["MixerChapter"], Sessions.Select(s => s.Title).ToArray());
            if (index < 0) return;

            var candidate = Sessions[index];

            // Un chapitre sans scène laisserait le mixer dans un état incohérent (chapitre
            // sélectionné mais aucune scène/piste à jouer) : on vérifie avant d'affecter
            // SelectedSession plutôt qu'après, pour que le chapitre précédent reste sélectionné
            // sans avoir à revenir dessus explicitement en cas de rejet.
            var scenes = await _sceneDataService.GetScenesAsync(candidate.Id);
            if (scenes.Count == 0)
            {
                await ShowInfoAsync(Loc["ErrorTitle"], Loc["ErrorChapterHasNoScenes"]);
                return;
            }

            SelectedSession = candidate;
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
                List<SceneTrack> playable = new();

                // L'overlay plein écran (Loading.IsLoading) ne couvre que la préparation - pas la
                // création des lecteurs, potentiellement coûteuse par piste sur Android (cf.
                // AudioMixerService.CreatePlayerAsync) - sinon les strips resteraient invisibles
                // derrière le spinner jusqu'à ce que TOUS les lecteurs soient prêts.
                await Loading.RunAsync(async () => { playable = await PrepareSceneTracksAsync(); });

                await PopulateChannelsAsync(playable);
            }
            finally
            {
                if (shell != null) shell.IsEnabled = true;
                _isLoadingScene = false;
            }
        }

        /// <summary>
        /// Sauvegarde/fade out/vide les channels actuels et renvoie les pistes jouables de
        /// SelectedScene. Appelé sous Loading.RunAsync par LoadScene ET LoadFromPlayAsync : dans
        /// les deux cas ce doit être le MÊME empan Show/Hide que la préparation qui précède (sans
        /// quoi IsLoading retombe à false puis repasse à true entre les deux, et l'overlay
        /// clignote au lieu de rester affiché en continu).
        /// </summary>
        private async Task<List<SceneTrack>> PrepareSceneTracksAsync()
        {
            await SaveCurrentSceneAsync();

            var fadeTasks = CurrentChannels.Where(c => c.IsPlaying).Select(c => c.FadeOut()).ToArray();
            await Task.WhenAll(fadeTasks);

            ClearChannels();

            _activeScene = SelectedScene;

            var sceneTracks = await _sceneDataService.GetSceneTracksAsync(SelectedScene!.Id);
            // Une piste illisible (fichier corrompu, verrouillé...) est simplement ignorée au lieu
            // de faire échouer toute la scène.
            return sceneTracks.Where(st => File.Exists(st.Track.FilePath)).ToList();
        }

        /// <summary>
        /// Ajoute tout de suite un strip par piste jouable, dans l'ordre de la scène, avec
        /// IsLoading à true (cf. binding dans AudioMixerPage.xaml qui désactive le strip et
        /// affiche un spinner local) puis crée les lecteurs en parallèle - chaque strip bascule à
        /// IsLoading=false dès que SON lecteur est prêt, indépendamment des autres. Volontairement
        /// hors de Loading.RunAsync : sinon l'overlay plein écran resterait affiché jusqu'à ce que
        /// TOUS les lecteurs soient créés, masquant cet affichage progressif.
        /// </summary>
        private async Task PopulateChannelsAsync(List<SceneTrack> playable)
        {
            var channels = playable.Select(st => new ChannelStripViewModel
            {
                SceneTrackId = st.Id,
                Track = st.Track,
                DisplayTrackName = st.Track.Title,
                Volume = st.Volume,
                IsLooping = st.IsLooping,
                IsFadeIn = st.FadeIn,
                IsFadeOut = st.FadeOut,
                IsLoading = true
            }).ToList();

            foreach (var channel in channels)
                CurrentChannels.Add(channel);

            // Echecs collectes plutot que remontes un par un : plusieurs strips se chargent en
            // parallele (Task.WhenAll), un ShowErrorAsync par echec empilerait plusieurs popups
            // modales d'un coup - une seule notification groupee a la fin est plus lisible.
            var failedTracks = new List<string>();
            var creationTasks = playable.Zip(channels, async (st, channel) =>
            {
                try
                {
                    channel.Player = await _audioMixerService.CreatePlayerAsync(st.Track.FilePath);
                    SubscribeChannel(channel);

                    if (st.AutoPlay)
                        channel.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AudioMixerViewModel] Échec de création du lecteur pour '{st.Track.Title}' : {ex}");
                    failedTracks.Add(st.Track.Title);
                    CurrentChannels.Remove(channel);
                }
                finally
                {
                    channel.IsLoading = false;
                }
            });
            await Task.WhenAll(creationTasks);

            if (failedTracks.Count > 0)
                await ShowInfoAsync(Loc["ErrorTitle"], string.Format(Loc["ErrorTracksFailedToLoad"], string.Join(", ", failedTracks)));
        }
    }
}
