using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using DmToolsApp.Models;
using DmToolsApp.Resources.Icons;
using DmToolsApp.Services;

namespace DmToolsApp.Components.Dialogs;

public enum CreateItemKind { Campaign, Session, Scene }

/// <summary>
/// Formulaire générique de création/édition (Campagne/Chapitre/Scène) : le type et le parent sont
/// pré-remplis sur la sélection courante de l'accordéon (cf. CampaignViewModel.Create/Edit) mais
/// restent modifiables — éditer un Chapitre/une Scène permet donc aussi de le/la déplacer vers une
/// autre Campagne/un autre Chapitre. Les champs de sélection reprennent le pattern "bouton +
/// ActionSheetDialog" déjà utilisé par l'AudioMixer pour choisir chapitre/scène (cf.
/// AudioMixerViewModel.SelectSession/SelectScene), plutôt que le Picker natif qui détonne avec le
/// thème de l'appli. Retourne true si l'utilisateur a validé ; l'appelant lit alors
/// SelectedKind/Name/SelectedCampaign/SelectedSession.
/// </summary>
public partial class CreateItemDialog : Popup<bool>
{
    private static LocalizationService Loc => LocalizationService.Instance;
    private static Page CurrentPage => Application.Current!.Windows[0].Page!;

    private readonly List<Campaign> _campaigns;
    private readonly Func<int, Task<List<Session>>> _loadSessions;
    private List<Session> _sessions = new();
    private CreateItemKind _kind;
    private Campaign? _selectedCampaign;
    private Session? _selectedSession;

    /// <param name="lockType">Édition d'un élément existant : changer sa nature (Campagne/Chapitre/
    /// Scène) n'a pas de sens, seul le nom et le parent (déplacement) restent modifiables.</param>
    public CreateItemDialog(
        List<Campaign> campaigns,
        Func<int, Task<List<Session>>> loadSessions,
        CreateItemKind initialKind,
        Campaign? initialCampaign,
        Session? initialSession,
        string? initialName = null,
        bool lockType = false)
    {
        InitializeComponent();
        ContentScroll.MaximumHeightRequest = DialogSizing.MaxContentHeight();
        _campaigns = campaigns;
        _loadSessions = loadSessions;
        // Pas de campagne existante : seul "Campagne" a un sens, les autres n'auraient aucun parent.
        _kind = campaigns.Count == 0 ? CreateItemKind.Campaign : initialKind;

        TitleLabel.Text = Loc[lockType ? "DialogEditItem" : "DialogCreateItem"];
        NameEntry.Text = initialName;
        TypeButton.IsEnabled = !lockType;
        UpdateTypeButtonText();
        UpdateFieldVisibility();

        Opened += async (_, _) =>
        {
            var campaign = initialCampaign ?? _campaigns.FirstOrDefault();
            if (campaign != null)
                await SelectCampaign(campaign, initialSession);

            UpdateTypeButtonText();
            UpdateFieldVisibility();
            NameEntry.Focus();
            NameEntry.CursorPosition = NameEntry.Text?.Length ?? 0;
        };
    }

    public CreateItemKind SelectedKind => _kind;
    public string Name => NameEntry.Text ?? string.Empty;
    public Campaign? SelectedCampaign => _selectedCampaign;
    public Session? SelectedSession => _selectedSession;

    private async void OnTypeButtonClicked(object? sender, EventArgs e)
    {
        var kinds = AvailableKinds();
        int index = await ShowActionSheetIndexAsync(Loc["FieldType"], kinds.Select(NounFor).ToList());
        if (index < 0) return;

        _kind = kinds[index];
        UpdateTypeButtonText();
        UpdateFieldVisibility();
    }

    private async void OnCampaignButtonClicked(object? sender, EventArgs e)
    {
        if (_campaigns.Count == 0) return;
        int index = await ShowActionSheetIndexAsync(Loc["NounCampaign"], _campaigns.Select(c => c.Title).ToList());
        if (index < 0) return;

        await SelectCampaign(_campaigns[index], null);
        UpdateTypeButtonText();
        UpdateFieldVisibility();
    }

    private async void OnSessionButtonClicked(object? sender, EventArgs e)
    {
        if (_sessions.Count == 0) return;
        int index = await ShowActionSheetIndexAsync(Loc["NounChapter"], _sessions.Select(s => s.Title).ToList());
        if (index < 0) return;

        _selectedSession = _sessions[index];
        SessionButton.Text = _selectedSession.Title;
    }

    private async Task SelectCampaign(Campaign campaign, Session? preselect)
    {
        _selectedCampaign = campaign;
        CampaignButton.Text = campaign.Title;

        _sessions = await _loadSessions(campaign.Id);
        _selectedSession = preselect != null
            ? _sessions.FirstOrDefault(s => s.Id == preselect.Id) ?? _sessions.FirstOrDefault()
            : _sessions.FirstOrDefault();
        SessionButton.Text = _selectedSession?.Title ?? string.Empty;

        // Cette campagne n'a aucun chapitre : "Scène" n'a plus de parent possible.
        if (_sessions.Count == 0 && _kind == CreateItemKind.Scene)
            _kind = CreateItemKind.Session;
    }

    /// <summary>Types proposables dans l'état courant : Chapitre exige au moins une campagne, Scène exige que la campagne sélectionnée ait au moins un chapitre.</summary>
    private List<CreateItemKind> AvailableKinds()
    {
        var kinds = new List<CreateItemKind> { CreateItemKind.Campaign };
        if (_campaigns.Count > 0) kinds.Add(CreateItemKind.Session);
        if (_campaigns.Count > 0 && _sessions.Count > 0) kinds.Add(CreateItemKind.Scene);
        return kinds;
    }

    private static string NounFor(CreateItemKind kind) => kind switch
    {
        CreateItemKind.Campaign => Loc["NounCampaign"],
        CreateItemKind.Session => Loc["NounChapter"],
        CreateItemKind.Scene => Loc["NounScene"],
        _ => string.Empty,
    };

    private static string IconFor(CreateItemKind kind) => kind switch
    {
        CreateItemKind.Campaign => SolidFont.Map,
        CreateItemKind.Session => SolidFont.Bookmark,
        CreateItemKind.Scene => SolidFont.MasksTheater,
        _ => string.Empty,
    };

    private void UpdateTypeButtonText()
    {
        TypeButton.Text = NounFor(_kind);
        TypeButton.Icon = IconFor(_kind);
    }

    private void UpdateFieldVisibility()
    {
        CampaignField.IsVisible = _kind != CreateItemKind.Campaign;
        SessionField.IsVisible = _kind == CreateItemKind.Scene;
    }

    /// <summary>Cf. BaseViewModel.ShowActionSheetIndexAsync — ce Popup n'est pas un ViewModel et ne peut pas en hériter.</summary>
    private static async Task<int> ShowActionSheetIndexAsync(string title, IEnumerable<string> options)
    {
        var popup = new ActionSheetDialog(title, options, Loc["BtnCancel"]);
        var result = await CurrentPage.ShowPopupAsync<int>(popup, new PopupOptions { CanBeDismissedByTappingOutsideOfPopup = true }, CancellationToken.None);
        return result.WasDismissedByTappingOutsideOfPopup ? -1 : result.Result is int index ? index : -1;
    }

    async void OnSaveClicked(object? sender, EventArgs e) => await CloseAsync(true);

    async void OnCancelClicked(object? sender, EventArgs e) => await CloseAsync(false);
}
