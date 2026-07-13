using CommunityToolkit.Maui.Views;

namespace DmToolsApp.Components.Dialogs;

/// <summary>
/// Ferme avec l'index de l'option choisie (-1 : annulation). Retourner l'index plutôt que le
/// libellé permet de distinguer deux options homonymes (scènes/chapitres du même nom).
/// </summary>
public partial class ActionSheetDialog : Popup<int>
{
    public ActionSheetDialog(string title, IEnumerable<string> options, string cancelLabel)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        CancelButton.Text = cancelLabel;

        var optionStyle = Application.Current?.Resources.TryGetValue("DialogOptionButton", out var style) == true
            ? (Style)style
            : null;

        int index = 0;
        foreach (var option in options)
        {
            var optionIndex = index++;
            var button = new Button { Text = option, Style = optionStyle };
            button.Clicked += async (_, _) => await CloseAsync(optionIndex);
            OptionsStack.Add(button);
        }
    }

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(-1);
}
