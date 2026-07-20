using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

public partial class PromptDialog : Popup<string?>
{
    public PromptDialog(string title, string message, string placeholder, string initialValue, string confirmLabel, string cancelLabel)
    {
        InitializeComponent();
        ContentScroll.MaximumHeightRequest = DialogSizing.MaxContentHeight();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrEmpty(message);
        InputEntry.Placeholder = placeholder;
        InputEntry.Text = initialValue;
        OkButton.Text = confirmLabel;
        CancelButton.Text = cancelLabel;

        // Focus direct sur le champ de saisie à l'ouverture, pour pouvoir taper sans clic préalable.
        // Sans CursorPosition, le curseur reste par défaut au début du texte pré-rempli (rename) —
        // gênant pour ajouter la fin d'un nom existant.
        Opened += (_, _) =>
        {
            InputEntry.Focus();
            InputEntry.CursorPosition = InputEntry.Text?.Length ?? 0;
        };
    }

    async void OnOkClicked(object? sender, EventArgs e) => await CloseAsync(InputEntry.Text);

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(null);
}
