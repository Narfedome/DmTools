using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using DmToolsApp.Components;
using DmToolsApp.Data;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.ImportExport;
using DmToolsApp.Features.Library;
using DmToolsApp.Models.Library;
using DmToolsApp.Models;
using DmToolsApp.Features.Onboarding;
using DmToolsApp.Features.Settings;
using DmToolsApp.Services;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
#if WINDOWS
using Microsoft.Maui.Platform;
#endif

namespace DmToolsApp
{
    public static class MauiProgram
    {
        static readonly string dbPath = Path.Combine(FileSystem.AppDataDirectory, "dmtools.db3");

        public static MauiApp CreateMauiApp()
        {
            string tracksDir = Path.Combine(FileSystem.AppDataDirectory, "Tracks");
            Directory.CreateDirectory(tracksDir);
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    // Toutes les popups de l'app (dialogues thémés, import) dessinent leur propre
                    // carte (Border) : on désactive le cadre/l'ombre par défaut du toolkit pour éviter
                    // un double contour (bug connu de bordure blanche sur Windows).
                    options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                    {
                        Shape = null,
                        Shadow = null
                    });
                })
                .UseMauiCommunityToolkitCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("PirataOne-Regular.ttf", "PirataOne");
                    fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FontRegular");
                    fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "FontBrands");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontSolid");
                    fonts.AddFont("rpgawesome-webfont.ttf", "RpgAwesome");
                })
#if WINDOWS
                .ConfigureMauiHandlers(handlers =>
                {
                    // Sur Windows, ListViewBase.SingleSelectionFollowsFocus vaut true par défaut : le
                    // premier élément réalisé d'un CollectionView prend le focus clavier au chargement
                    // et s'affiche "sélectionné" (bordure accent) sans qu'aucun tap n'ait eu lieu, alors
                    // que SelectedItem reste bien null côté ViewModel — trompeur pour l'utilisateur.
                    Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("NoFocusFollowsSelection", (handler, view) =>
                    {
                        if (handler.PlatformView is Microsoft.UI.Xaml.Controls.ListViewBase listViewBase)
                            listViewBase.SingleSelectionFollowsFocus = false;
                    });

                    // ChannelVolumeSliderView (StyleId="ChannelVolumeSliderTrack") tournait un Slider
                    // horizontal de -90° pour le rendre vertical : sur WinUI, le RenderTransform de
                    // rotation ne se redessine pas correctement quand la taille change dynamiquement
                    // dans l'item template d'un CollectionView (le layout MAUI est correct, mais le
                    // rendu natif reste calé sur une passe antérieure). Plutôt que rustiner ce rendu,
                    // on bascule ce Slider précis sur l'orientation verticale native de WinUI, qui n'a
                    // pas ce problème. Scopé via StyleId pour ne pas affecter les autres Slider de
                    // l'app (ChannelSettingsDialog, SceneTracksPage), qui restent horizontaux.
                    // Testé sans cette bascule (retour au rendu MAUI tourné, comme sur les autres
                    // plateformes) : le bug de redessin est bien revenu, ce n'était pas lié au thumb
                    // bleu (cf. mapping "AccentThumbBrush" ci-dessous, qui est un souci séparé et
                    // s'applique de toute façon à tous les Slider, natif ou non).
                    Microsoft.Maui.Handlers.SliderHandler.Mapper.AppendToMapping("VerticalOrientation", (handler, view) =>
                    {
                        if (view is VisualElement { StyleId: "ChannelVolumeSliderTrack" } &&
                            handler.PlatformView is Microsoft.UI.Xaml.Controls.Slider nativeSlider)
                        {
                            nativeSlider.Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical;
                        }
                    });

                    // Sur Windows, TOUT Slider MAUI est en réalité rendu par le vrai contrôle natif
                    // WinUI (Microsoft.UI.Xaml.Controls.Slider) - ce n'est pas spécifique au slider
                    // vertical de l'AudioMixer ni à une bascule d'orientation. Le thumb natif Fluent
                    // Design a un disque intérieur dont la couleur au survol/appui vient des
                    // ressources thème SliderThumbBackgroundPointerOver/Pressed - pas de la propriété
                    // MAUI ThumbColor, donc il reste au bleu d'accent système Windows par défaut, quel
                    // que soit le Slider. On écrase ces ressources avec l'accent de la palette
                    // courante pour tous les Slider de l'appli, et on les réapplique à chaque
                    // changement de thème (ce ne sont pas des {DynamicResource} MAUI).
                    Microsoft.Maui.Handlers.SliderHandler.Mapper.AppendToMapping("AccentThumbBrush", (handler, view) =>
                    {
                        if (handler.PlatformView is Microsoft.UI.Xaml.Controls.Slider nativeSlider)
                        {
                            ApplyAccentThumbBrush(nativeSlider);
                            ThemeService.Instance.ThemeChanged += () => ApplyAccentThumbBrush(nativeSlider);
                        }
                    });
                })
#endif
                ;
            builder.AddAudio();

            // Le dispatcher UI du ChannelStripViewModel (projet Core, sans dépendance MAUI) est
            // branché ici sur le vrai thread UI.
            ChannelStripViewModel.UiDispatcher = action => MainThread.BeginInvokeOnMainThread(action);

            // Les catégories par défaut sont localisées ici : la couche données (Core) ne dépend
            // pas de la localisation.
            builder.Services.AddSingleton(
                new AppDatabase(dbPath, new[]
                {
                    LocalizationService.Instance["LibCategoryMusic"],
                    LocalizationService.Instance["LibCategoryAmbience"],
                    LocalizationService.Instance["LibCategorySoundEffect"]
                }));
            builder.Services.AddTransient<LoadingService>();
            builder.Services.AddSingleton<AudioPlayerService>();
            builder.Services.AddSingleton<AudioMixerService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<ITrackFileStore>(sp => sp.GetRequiredService<FileService>());
            builder.Services.AddSingleton<ILibraryPickerService, LibraryPickerService>();
            builder.Services.AddSingleton<ILibraryPickerNavigationService, LibraryPickerNavigationService>();
            builder.Services.AddSingleton<ILibraryDataService, LibraryDataService>();
            builder.Services.AddSingleton<CoverArtService>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<ISceneDataService, SceneDataService>();
            builder.Services.AddSingleton<IImportExportService, ImportExportService>();
            builder.Services.AddSingleton<SessionStateService>();
            builder.Services.AddTransient<OnboardingViewModel>();
            builder.Services.AddTransient<OnboardingPage>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<LibraryTrackViewModel>();
            builder.Services.AddTransient<CategoryListViewModel>();
            builder.Services.AddTransient<CategoryListPage>();
            builder.Services.AddTransient<LibrarySpellViewModel>();
            builder.Services.AddTransient<LibrarySpellEditViewModel>();
            builder.Services.AddTransient<LibrarySpellSelectorPage>();
            builder.Services.AddTransient<LibraryTrackSelectorPage>();
            builder.Services.AddSingleton<AudioMixerViewModel>();
            builder.Services.AddTransient<AudioMixerPage>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<CampaignViewModel>();
            builder.Services.AddTransient<CampaignPage>();
            builder.Services.AddTransient<SceneTracksViewModel>();
            builder.Services.AddTransient<SceneTracksPage>();
            builder.Services.AddTransient<ImportExportViewModel>();
            builder.Services.AddTransient<ImportExportPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

#if WINDOWS
        // Cf. commentaire sur "AccentThumbBrush" ci-dessus (ConfigureMauiHandlers) : ressources
        // theme Fluent natives du Slider, pas des DynamicResource MAUI - a reappliquer nous-memes.
        static void ApplyAccentThumbBrush(Microsoft.UI.Xaml.Controls.Slider nativeSlider)
        {
            var accent = (Microsoft.Maui.Graphics.Color)Microsoft.Maui.Controls.Application.Current!.Resources["AppAccent"];
            var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(accent.ToWindowsColor());
            nativeSlider.Resources["SliderThumbBackground"] = brush;
            nativeSlider.Resources["SliderThumbBackgroundPointerOver"] = brush;
            nativeSlider.Resources["SliderThumbBackgroundPressed"] = brush;

            // Changer une entree de ressource locale ne repeint pas a chaud un Slider deja affiche :
            // constate en testant un changement de theme app en cours d'execution - seul le slider
            // qu'on venait de toucher (donc qui avait transite par un vrai etat visuel Pointer/
            // Pressed) affichait la bonne couleur, les autres restaient bleus malgre la ressource
            // mise a jour. Un aller-retour IsEnabled force WinUI a re-parcourir le VisualStateManager
            // et donc a relire ces ressources, sans flash visible (aucun rendu n'a lieu entre les
            // deux lignes, toutes deux synchrones sur le thread UI).
            nativeSlider.IsEnabled = false;
            nativeSlider.IsEnabled = true;
        }
#endif
    }

}
