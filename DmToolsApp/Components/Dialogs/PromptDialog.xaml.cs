using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

public partial class PromptDialog : Popup<string?>
{
    public PromptDialog(string title, string message, string placeholder, string initialValue, string confirmLabel, string cancelLabel)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrEmpty(message);
        InputEntry.Placeholder = placeholder;
        InputEntry.Text = initialValue;
        OkButton.Text = confirmLabel;
        CancelButton.Text = cancelLabel;
    }

    async void OnOkClicked(object? sender, EventArgs e) => await CloseAsync(InputEntry.Text);

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(null);
}
