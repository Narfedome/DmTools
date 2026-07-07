using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

public partial class ActionSheetDialog : Popup<string?>
{
    public ActionSheetDialog(string title, IEnumerable<string> options, string cancelLabel)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        CancelButton.Text = cancelLabel;

        var optionStyle = Application.Current?.Resources.TryGetValue("DialogOptionButton", out var style) == true
            ? (Style)style
            : null;

        foreach (var option in options)
        {
            var button = new Button { Text = option, Style = optionStyle };
            button.Clicked += async (_, _) => await CloseAsync(option);
            OptionsStack.Add(button);
        }
    }

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(null);
}
