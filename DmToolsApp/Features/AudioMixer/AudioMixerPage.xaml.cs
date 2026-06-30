namespace DmToolsApp.Features.AudioMixer;

public partial class AudioMixerPage : ContentPage
{
    private readonly AudioMixerViewModel _vm;

    public AudioMixerPage(AudioMixerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private async void OnSessionClicked(object sender, EventArgs e)
    {
        if (!_vm.Sessions.Any()) return;
        var titles = _vm.Sessions.Select(s => s.Title).ToArray();
        var result = await DisplayActionSheet(_vm.Loc["MixerChapter"], _vm.Loc["BtnCancel"], null, titles);
        if (result is null || result == _vm.Loc["BtnCancel"]) return;
        _vm.SelectedSession = _vm.Sessions.First(s => s.Title == result);
    }

    private async void OnSceneClicked(object sender, EventArgs e)
    {
        if (!_vm.Scenes.Any()) return;
        var titles = _vm.Scenes.Select(s => s.Title).ToArray();
        var result = await DisplayActionSheet(_vm.Loc["MixerScene"], _vm.Loc["BtnCancel"], null, titles);
        if (result is null || result == _vm.Loc["BtnCancel"]) return;
        _vm.SelectedScene = _vm.Scenes.First(s => s.Title == result);
    }
}
