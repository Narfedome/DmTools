using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components;
using DmToolsApp.Features.Library;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using System.Collections.ObjectModel;


namespace DmToolsApp.Features.AudioMixer
{
    public partial class AudioMixerViewModel : ObservableObject
    {
        public Services.LocalizationService Loc => Services.LocalizationService.Instance;
        private readonly AudioMixerService _audioMixerService;
        private readonly ILibraryPickerService _pickerService;
        private readonly ISceneDataService _sceneDataService;

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
            var channel = new ChannelStripViewModel() { DisplayTrackName = (Services.LocalizationService.Instance.ChannelNew + " " + (CurrentChannels.Count + 1)), IsPlaying = false };
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
                CurrentChannels.Remove(channel);
                return;
            }
            channel.Pause();

            var loc = Services.LocalizationService.Instance;
            bool confirm = await Shell.Current.DisplayAlertAsync(
                loc.DialogDelete,
                string.Format(loc.DialogRemoveChannel, channel.DisplayTrackName),
                loc.DialogYes,
                loc.DialogNo);

            if (!confirm)
            {
                channel.TogglePlay();
                return;
            }

            channel.Stop();
            CurrentChannels.Remove(channel);
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task PickFile(ChannelStripViewModel channel)
        {
            try
            {
                if (channel == null) return;
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = Services.LocalizationService.Instance.TrackSelectFile,
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
                    var stream = await result.OpenReadAsync();
                    channel.Player = _audioMixerService.CreatePlayerFromSelectedFile(stream);
                    channel.DisplayTrackName = result.FileName;
                    channel.TogglePlay();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task PickLibraryItem(ChannelStripViewModel channel)
        {
            try
            {
                if (channel == null) return;
                var selectedLibraryItem = await _pickerService.PickTrackAsync();

                if (selectedLibraryItem is null)
                    return;

                Track selectedTrack = (Track)selectedLibraryItem;
                if (File.Exists(selectedTrack.FilePath))
                {
                    var stream = File.OpenRead(selectedTrack.FilePath);
                    channel.Player = _audioMixerService.CreatePlayerFromSelectedFile(stream);
                    channel.DisplayTrackName = selectedTrack.Title;
                    channel.TogglePlay();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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

        private bool _suppressHandlers;

        partial void OnSelectedSessionChanged(Session? value)
        {
            if (_suppressHandlers) return;
            SelectedScene = null;
            Scenes.Clear();
            if (value != null)
                _ = LoadScenesAsync(value.Id);
        }

        partial void OnSelectedSceneChanged(Scene? value)
        {
            if (_suppressHandlers) return;
            SceneIndex = value != null ? Scenes.IndexOf(value) + 1 : 0;
            OnPropertyChanged(nameof(CanGoPrevScene));
            OnPropertyChanged(nameof(CanGoNextScene));
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

        /// <summary>
        /// Appelé depuis le flow de jeu (Accueil → Chapitre → Scène).
        /// Charge le contexte de la campagne active et lance la scène.
        /// </summary>
        private async Task SaveCurrentSceneAsync()
        {
            var tasks = CurrentChannels
                .Where(c => c.SceneTrackId > 0)
                .Select(c => _sceneDataService.UpdateSceneTrackVolumeAsync(c.SceneTrackId, (float)c.Volume));
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

            await LoadScene();
        }

        [RelayCommand]
        public void PrevScene()
        {
            if (!CanGoPrevScene) return;
            SelectedScene = Scenes[SceneIndex - 2]; // SceneIndex est 1-based
        }

        [RelayCommand]
        public void NextScene()
        {
            if (!CanGoNextScene) return;
            SelectedScene = Scenes[SceneIndex]; // SceneIndex est 1-based, donc SceneIndex = prochain index 0-based
        }

        [RelayCommand]
        public async Task LoadScene()
        {
            if (SelectedScene == null) return;

            // Fade out tous les channels actifs
            var fadeTasks = CurrentChannels.Where(c => c.IsPlaying).Select(c => c.FadeOut()).ToArray();
            await Task.WhenAll(fadeTasks);

            CurrentChannels.Clear();

            var sceneTracks = await _sceneDataService.GetSceneTracksAsync(SelectedScene.Id);

            foreach (var st in sceneTracks)
            {
                if (!File.Exists(st.Track.FilePath)) continue;

                var stream = File.OpenRead(st.Track.FilePath);
                var channel = new ChannelStripViewModel
                {
                    SceneTrackId = st.Id,
                    DisplayTrackName = st.Track.Title,
                    Volume = st.Volume,
                    IsLooping = true,
                    Player = _audioMixerService.CreatePlayerFromSelectedFile(stream)
                };

                if (st.AutoPlay)
                    channel.Play();

                CurrentChannels.Add(channel);
            }
        }
    }
}
