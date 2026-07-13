using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SceneTracksViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly ILibraryPickerService _pickerService;

        public SceneTracksViewModel(ISceneDataService sceneDataService, ILibraryPickerService pickerService)
        {
            _sceneDataService = sceneDataService;
            _pickerService = pickerService;
        }

        [ObservableProperty] private Scene? scene;
        [ObservableProperty] private ObservableCollection<SceneTrack> sceneTracks = new();
        [ObservableProperty] private SceneTrack? selectedTrack;

        // Piste à présélectionner au prochain chargement (ouverture depuis le bouton gear d'un
        // channel strip du mixer), consommée une seule fois.
        private int _focusSceneTrackId;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Ne recharge pas ici : OnNavigatedTo (SceneTracksPage) s'en charge à chaque navigation
            // réelle (cf. SessionListViewModel/SceneListViewModel pour le détail du problème que pose
            // un rechargement déclenché par les query attributes).
            if (query.TryGetValue("Scene", out var value) && value is Scene scene)
                Scene = scene;
            if (query.TryGetValue("FocusSceneTrackId", out var focus) && focus is int focusId)
                _focusSceneTrackId = focusId;
        }

        public async Task ReloadAsync()
        {
            if (Scene == null) return;
            await Loading.RunAsync(async () =>
            {
                var list = await _sceneDataService.GetSceneTracksAsync(Scene.Id);
                SceneTracks = new ObservableCollection<SceneTrack>(list);

                if (_focusSceneTrackId > 0)
                {
                    SelectedTrack = SceneTracks.FirstOrDefault(t => t.Id == _focusSceneTrackId);
                    _focusSceneTrackId = 0;
                }
            });
        }

        [RelayCommand]
        public async Task AddTrack()
        {
            if (Scene == null) return;

            var picked = await _pickerService.PickTrackAsync();
            if (picked is not Models.Library.Track track) return;

            var sceneTrack = new SceneTrack
            {
                SceneId = Scene.Id,
                Track = track,
                Volume = 1.0,
                Position = SceneTracks.Count,
                AutoPlay = false
            };

            await _sceneDataService.SaveSceneTrackAsync(sceneTrack);

            // Recharge plutôt que d'ajouter à la main : le retour du picker déclenche déjà un
            // OnNavigatedTo → ReloadAsync qui peut s'exécuter avant ou après la sauvegarde ; un Add
            // manuel risquerait alors un doublon. Deux rechargements concurrents, eux, convergent.
            await ReloadAsync();
        }

        [RelayCommand]
        public async Task RemoveTrack(SceneTrack sceneTrack)
        {
            if (sceneTrack == null) return;
            await _sceneDataService.DeleteSceneTrackAsync(sceneTrack);
            SceneTracks.Remove(sceneTrack);
            if (SelectedTrack == sceneTrack) SelectedTrack = null;
        }

        [RelayCommand]
        public async Task SaveTrack(SceneTrack sceneTrack)
        {
            if (sceneTrack == null) return;
            await _sceneDataService.SaveSceneTrackAsync(sceneTrack);
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
