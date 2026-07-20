using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DmToolsApp.Components.Dialogs;
using DmToolsApp.Extensions;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Models;
using DmToolsApp.Services;
using System.Collections.ObjectModel;

namespace DmToolsApp.Features.Campaigns
{
    /// <summary>
    /// Accordéon Campagne → Chapitre → Scène sur une seule page : Rows est un arbre "à plat", les
    /// lignes filles ne sont insérées qu'à l'expansion de leur parent, et une seule branche par niveau
    /// reste dépliée à la fois (déplier une campagne/un chapitre replie son précédent sibling déplié).
    /// Un unique "+" (barre du bas) ouvre un formulaire générique (CreateItemDialog) où le type
    /// Campagne/Chapitre/Scène et son parent sont pré-remplis sur SelectedRow mais restent modifiables.
    /// </summary>
    public partial class CampaignViewModel : BaseViewModel
    {
        private readonly ISceneDataService _sceneDataService;
        private readonly AudioMixerViewModel _audioMixerViewModel;
        private readonly SessionStateService _sessionStateService;

        public CampaignViewModel(
            ISceneDataService sceneDataService,
            AudioMixerViewModel audioMixerViewModel,
            SessionStateService sessionStateService)
        {
            _sceneDataService = sceneDataService;
            _audioMixerViewModel = audioMixerViewModel;
            _sessionStateService = sessionStateService;
        }

        [ObservableProperty] private ObservableCollection<ExplorerRow> rows = new();
        [ObservableProperty] private ExplorerRow? selectedRow;
        [ObservableProperty] private bool hasCampaigns;

        // Reste false pendant le court instant entre l'affichage de la page et le vrai début du
        // chargement (Loading.IsLoading ne passe à true qu'à l'intérieur de InitializeAsync) : sans ce
        // garde-fou, HasCampaigns valait encore false par défaut durant cette fenêtre et l'état vide
        // (bouton "Créer") s'affichait brièvement avant le spinner.
        [ObservableProperty] private bool isInitialized;

        // IsSelected vit sur la ligne elle-même : le style de sélection (cf. CampaignPage.xaml) se lie
        // dessus plutôt que sur la sélection native du CollectionView (SelectionMode="None"), dont le
        // rendu par défaut diverge trop entre Windows et Android.
        partial void OnSelectedRowChanged(ExplorerRow? oldValue, ExplorerRow? newValue)
        {
            if (oldValue != null) oldValue.IsSelected = false;
            if (newValue != null) newValue.IsSelected = true;
        }

        /// <summary>Sélection d'une ligne sans action associée (Scène : Campagne/Chapitre sélectionnent déjà via ToggleCampaign/ToggleSession).</summary>
        [RelayCommand]
        public void Select(ExplorerRow row) => SelectedRow = row;

        public async Task InitializeAsync()
        {
            await Loading.RunAsync(async () =>
            {
                var campaigns = await _sceneDataService.GetCampaignsAsync();
                Rows = new ObservableCollection<ExplorerRow>(campaigns.Select(c => new CampaignRow(c)));
                HasCampaigns = campaigns.Count > 0;
                SelectedRow = null;
                IsInitialized = true;
            });
        }

        // ── Expand / collapse ─────────────────────────────────────

        // Ouvrir une ligne et la sélectionner (pour Renommer/Supprimer) sont fondus en un seul geste.
        // Deux commandes distinctes selon la zone tapée (cf. CampaignPage.xaml) :
        // - Open* (corps de la ligne) n'ouvre jamais que — ne referme jamais un volet déjà ouvert,
        //   pour qu'on puisse retaper une campagne/un chapitre pour le sélectionner (Renommer/
        //   Supprimer) sans le refermer sous ses pieds pendant qu'on navigue dedans.
        // - Toggle* (le chevron uniquement) fait un vrai bascule, y compris la fermeture : son
        //   affordance visuelle appelle explicitement ce geste.
        // Dans les deux cas, un volet s'auto-referme dès qu'un AUTRE élément du même niveau s'ouvre
        // (cf. ExpandCampaignAsync/ExpandSessionAsync).
        [RelayCommand]
        public async Task OpenCampaign(CampaignRow row)
        {
            SelectedRow = row;
            if (!row.IsExpanded) await ExpandCampaignAsync(row);
        }

        [RelayCommand]
        public async Task OpenSession(SessionRow row)
        {
            SelectedRow = row;
            if (!row.IsExpanded) await ExpandSessionAsync(row);
        }

        [RelayCommand]
        public async Task ToggleCampaign(CampaignRow row)
        {
            SelectedRow = row;
            if (row.IsExpanded) CollapseCampaign(row);
            else await ExpandCampaignAsync(row);
        }

        [RelayCommand]
        public async Task ToggleSession(SessionRow row)
        {
            SelectedRow = row;
            if (row.IsExpanded) CollapseSession(row);
            else await ExpandSessionAsync(row);
        }

        private async Task ExpandCampaignAsync(CampaignRow row)
        {
            if (row.IsExpanded) return;

            var otherExpanded = Rows.OfType<CampaignRow>().FirstOrDefault(r => r.IsExpanded);
            if (otherExpanded != null) CollapseCampaign(otherExpanded);

            row.IsExpanded = true;
            var sessions = await _sceneDataService.GetSessionsAsync(row.Campaign.Id);
            int insertAt = Rows.IndexOf(row) + 1;
            foreach (var session in sessions)
                Rows.Insert(insertAt++, new SessionRow(session, row.Campaign));
        }

        private void CollapseCampaign(CampaignRow row)
        {
            if (!row.IsExpanded) return;

            row.IsExpanded = false;
            int idx = Rows.IndexOf(row) + 1;
            while (idx < Rows.Count && Rows[idx].Depth > 0)
                Rows.RemoveAt(idx);
        }

        private async Task ExpandSessionAsync(SessionRow row)
        {
            if (row.IsExpanded) return;

            var otherExpanded = Rows.OfType<SessionRow>().FirstOrDefault(r => r.IsExpanded);
            if (otherExpanded != null) CollapseSession(otherExpanded);

            row.IsExpanded = true;
            var scenes = await _sceneDataService.GetScenesAsync(row.Session.Id);
            int insertAt = Rows.IndexOf(row) + 1;
            foreach (var scene in scenes)
                Rows.Insert(insertAt++, new SceneRow(scene, row.Session, row.ParentCampaign));
        }

        private void CollapseSession(SessionRow row)
        {
            if (!row.IsExpanded) return;

            row.IsExpanded = false;
            int idx = Rows.IndexOf(row) + 1;
            while (idx < Rows.Count && Rows[idx].Depth > 1)
                Rows.RemoveAt(idx);
        }

        /// <summary>Index juste après le dernier descendant direct de <paramref name="row"/> (avant le prochain sibling ou la fin).</summary>
        private int EndOfSubtree(ExplorerRow row)
        {
            int idx = Rows.IndexOf(row) + 1;
            while (idx < Rows.Count && Rows[idx].Depth > row.Depth) idx++;
            return idx;
        }

        // ── Création (formulaire générique, type + parent pré-remplis sur SelectedRow) ─

        [RelayCommand]
        public async Task Create()
        {
            var campaigns = Rows.OfType<CampaignRow>().Select(r => r.Campaign).ToList();
            var (initialKind, initialCampaign, initialSession) = SelectedRow switch
            {
                CampaignRow c => (CreateItemKind.Session, c.Campaign, (Session?)null),
                SessionRow s => (CreateItemKind.Scene, s.ParentCampaign, (Session?)s.Session),
                SceneRow sc => (CreateItemKind.Scene, sc.ParentCampaign, (Session?)sc.ParentSession),
                _ => (CreateItemKind.Campaign, (Campaign?)null, (Session?)null),
            };

            var dialog = new CreateItemDialog(campaigns, _sceneDataService.GetSessionsAsync, initialKind, initialCampaign, initialSession);
            if (await ShowDialogAsync(dialog) != true) return;

            string name = dialog.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.CapitalizeFirst();

            switch (dialog.SelectedKind)
            {
                case CreateItemKind.Campaign:
                {
                    var campaign = new Campaign { Title = name };
                    await _sceneDataService.SaveCampaignAsync(campaign);
                    Rows.Add(new CampaignRow(campaign));
                    HasCampaigns = true;
                    break;
                }
                case CreateItemKind.Session:
                {
                    if (dialog.SelectedCampaign == null) return;
                    var session = new Session { CampaignId = dialog.SelectedCampaign.Id, Title = name };
                    await _sceneDataService.SaveSessionAsync(session);

                    var campaignRow = Rows.OfType<CampaignRow>().FirstOrDefault(r => r.Campaign.Id == dialog.SelectedCampaign.Id);
                    if (campaignRow != null && campaignRow.IsExpanded)
                        Rows.Insert(EndOfSubtree(campaignRow), new SessionRow(session, campaignRow.Campaign));
                    break;
                }
                case CreateItemKind.Scene:
                {
                    if (dialog.SelectedCampaign == null || dialog.SelectedSession == null) return;
                    var scene = new Scene { SessionId = dialog.SelectedSession.Id, Title = name };
                    await _sceneDataService.SaveSceneAsync(scene);

                    var sessionRow = Rows.OfType<SessionRow>().FirstOrDefault(r => r.Session.Id == dialog.SelectedSession.Id);
                    if (sessionRow != null && sessionRow.IsExpanded)
                        Rows.Insert(EndOfSubtree(sessionRow), new SceneRow(scene, sessionRow.Session, sessionRow.ParentCampaign));
                    break;
                }
            }
        }

        // ── Rename / Delete (ciblent SelectedRow, quel que soit son niveau) ─

        [RelayCommand]
        public async Task Rename()
        {
            switch (SelectedRow)
            {
                case CampaignRow campaignRow:
                {
                    string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: campaignRow.Campaign.Title);
                    if (string.IsNullOrWhiteSpace(name)) return;
                    campaignRow.Campaign.Title = name.CapitalizeFirst();
                    await _sceneDataService.SaveCampaignAsync(campaignRow.Campaign);
                    break;
                }
                case SessionRow sessionRow:
                {
                    string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: sessionRow.Session.Title);
                    if (string.IsNullOrWhiteSpace(name)) return;
                    sessionRow.Session.Title = name.CapitalizeFirst();
                    await _sceneDataService.SaveSessionAsync(sessionRow.Session);
                    break;
                }
                case SceneRow sceneRow:
                {
                    string? name = await ShowPromptAsync(Loc["DialogRename"], Loc["PromptName"], initialValue: sceneRow.Scene.Title);
                    if (string.IsNullOrWhiteSpace(name)) return;
                    sceneRow.Scene.Title = name.CapitalizeFirst();
                    await _sceneDataService.SaveSceneAsync(sceneRow.Scene);
                    break;
                }
            }
        }

        [RelayCommand]
        public async Task Delete()
        {
            switch (SelectedRow)
            {
                case CampaignRow campaignRow:
                    if (!await ConfirmDeleteAsync(campaignRow.Campaign.Title)) return;
                    // DeleteCampaignAsync cascade déjà les Session/Scene/SceneTrack en base ; ici il
                    // faut aussi retirer ses éventuelles lignes Chapitre/Scène dépliées de Rows, sinon
                    // elles restent affichées, orphelines, sans plus aucune campagne au-dessus d'elles.
                    await _sceneDataService.DeleteCampaignAsync(campaignRow.Campaign);
                    RemoveSubtree(campaignRow);
                    HasCampaigns = Rows.OfType<CampaignRow>().Any();
                    break;
                case SessionRow sessionRow:
                    if (!await ConfirmDeleteAsync(sessionRow.Session.Title)) return;
                    await _sceneDataService.DeleteSessionAsync(sessionRow.Session);
                    RemoveSubtree(sessionRow);
                    break;
                case SceneRow sceneRow:
                    if (!await ConfirmDeleteAsync(sceneRow.Scene.Title)) return;
                    await _sceneDataService.DeleteSceneAsync(sceneRow.Scene);
                    Rows.Remove(sceneRow);
                    break;
                default:
                    return;
            }

            SelectedRow = null;
        }

        private void RemoveSubtree(ExplorerRow row)
        {
            int idx = Rows.IndexOf(row);
            int end = EndOfSubtree(row);
            for (int i = end - 1; i >= idx; i--)
                Rows.RemoveAt(i);
        }

        [RelayCommand]
        public async Task Launch(SceneRow sceneRow)
        {
            await _audioMixerViewModel.LoadFromPlayAsync(sceneRow.ParentCampaign, sceneRow.ParentSession, sceneRow.Scene);
            _sessionStateService.SetActive(true);
            await Shell.Current.GoToAsync("//AudioMixerPage");
        }
    }
}
