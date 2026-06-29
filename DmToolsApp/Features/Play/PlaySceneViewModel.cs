using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Play
{
    public partial class PlaySceneViewModel : ObservableObject, IQueryAttributable
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly AudioMixerViewModel _audioMixerViewModel;
        private Campaign? _campaign;
        public LocalizationService Loc => LocalizationService.Instance;

        public PlaySceneViewModel(ISceneDataService sceneDataService, AudioMixerViewModel audioMixerViewModel)
        {
            _sceneDataService = sceneDataService;
            _audioMixerViewModel = audioMixerViewModel;
        }

        [ObservableProperty] private Session? session;
        [ObservableProperty] private ObservableCollection<Scene> scenes = new();
        [ObservableProperty] private bool isBusy;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Campaign", out var c) && c is Campaign campaign)
                _campaign = campaign;
            if (query.TryGetValue("Session", out var s) && s is Session session)
            {
                Session = session;
                _ = LoadAsync(session.Id);
            }
        }

        private async Task LoadAsync(int sessionId)
        {
            IsBusy = true;
            try
            {
                var list = await _sceneDataService.GetScenesAsync(sessionId);
                Scenes = new ObservableCollection<Scene>(list);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task Launch(Scene scene)
        {
            if (_campaign == null || Session == null) return;
            await _audioMixerViewModel.LoadFromPlayAsync(_campaign, Session, scene);
            await Shell.Current.GoToAsync("//AudioMixerPage");
        }
    }
}
