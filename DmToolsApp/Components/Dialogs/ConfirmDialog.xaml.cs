using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

public partial class ConfirmDialog : Popup<bool>
{
    public ConfirmDialog(string title, string message, string confirmLabel, string? cancelLabel)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        PrimaryButton.Text = confirmLabel;

        if (cancelLabel == null)
        {
            SecondaryButton.IsVisible = false;
            Grid.SetColumnSpan(PrimaryButton, 2);
        }
        else
        {
            SecondaryButton.Text = cancelLabel;
        }
    }

    async void OnPrimaryClicked(object? sender, EventArgs e) => await CloseAsync(true);

    async void OnSecondaryClicked(object? sender, EventArgs e) => await CloseAsync(false);
}
