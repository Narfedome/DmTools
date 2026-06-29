using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    public partial class SceneTracksViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly ILibraryPickerService _pickerService;
        public Services.LocalizationService Loc => Services.LocalizationService.Instance;

        public SceneTracksViewModel(ISceneDataService sceneDataService, ILibraryPickerService pickerService)
        {
            _sceneDataService = sceneDataService;
            _pickerService = pickerService;
        }

        [ObservableProperty] private Scene? scene;
        [ObservableProperty] private ObservableCollection<SceneTrack> sceneTracks = new();
        [ObservableProperty] private SceneTrack? selectedTrack;
        [ObservableProperty] private bool isBusy;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Scene", out var value) && value is Scene scene)
            {
                Scene = scene;
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            if (Scene == null) return;
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetSceneTracksAsync(Scene.Id);
                SceneTracks = new ObservableCollection<SceneTrack>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task AddTrack()
        {
            if (Scene == null) return;

            var picked = await _pickerService.PickTrackAsync();
            if (picked == null) return;

            var sceneTrack = new SceneTrack
            {
                SceneId = Scene.Id,
                Track = (Models.Library.Track)picked,
                Volume = 1.0,
                Position = SceneTracks.Count,
                AutoPlay = false
            };

            await _sceneDataService.SaveSceneTrackAsync(sceneTrack);
            SceneTracks.Add(sceneTrack);
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
