using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

/// <summary>
/// Paramètres de la piste assignée à un channel strip (volume, boucle, fade in, autoplay).
/// Retourne true si l'utilisateur a enregistré, false s'il a annulé (ou fermé la popup) ;
/// l'appelant lit alors les valeurs éditées via les propriétés *Value.
/// </summary>
public partial class ChannelSettingsDialog : Popup<bool>
{
    public ChannelSettingsDialog(string trackName, double volume, bool isLooping, bool fadeIn, bool fadeOut, bool autoPlay)
    {
        InitializeComponent();

        TrackLabel.Text = trackName;
        VolumeSlider.Value = volume;
        VolumeValueLabel.Text = ((int)(volume * 100)).ToString();
        LoopCheck.IsChecked = isLooping;
        FadeInCheck.IsChecked = fadeIn;
        FadeOutCheck.IsChecked = fadeOut;
        AutoPlayCheck.IsChecked = autoPlay;
    }

    public double VolumeValue => VolumeSlider.Value;
    public bool IsLoopingValue => LoopCheck.IsChecked;
    public bool FadeInValue => FadeInCheck.IsChecked;
    public bool FadeOutValue => FadeOutCheck.IsChecked;
    public bool AutoPlayValue => AutoPlayCheck.IsChecked;

    void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
        => VolumeValueLabel.Text = ((int)(e.NewValue * 100)).ToString();

    async void OnSaveClicked(object? sender, EventArgs e) => await CloseAsync(true);

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(false);
}
