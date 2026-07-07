using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using DmToolsApp.Data;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Features.Campaigns;
using DmToolsApp.Features.Library;
using DmToolsApp.Models.Library;
using DmToolsApp.Models;
using DmToolsApp.Features.Onboarding;
using DmToolsApp.Features.Settings;
using DmToolsApp.Services;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

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
                });
            builder.AddAudio();
            builder.Services.AddSingleton(
                new AppDatabase(dbPath)); 
            builder.Services.AddSingleton<LoadingService>();
            builder.Services.AddSingleton<AudioPlayerService>();
            builder.Services.AddSingleton<AudioMixerService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<ILibraryPickerService, LibraryPickerService>();
            builder.Services.AddSingleton<ILibraryPickerNavigationService, LibraryPickerNavigationService>();
            builder.Services.AddSingleton<ILibraryDataService, LibraryDataService>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<ISceneDataService, SceneDataService>();
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
            builder.Services.AddTransient<LibraryTrackEditViewModel>();
            builder.Services.AddTransient<LibrarySpellSelectorPage>();
            builder.Services.AddTransient<LibraryTrackSelectorPage>();
            builder.Services.AddSingleton<AudioMixerViewModel>();
            builder.Services.AddTransient<AudioMixerPage>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<CampaignViewModel>();
            builder.Services.AddTransient<CampaignPage>();
            builder.Services.AddTransient<SessionListViewModel>();
            builder.Services.AddTransient<SessionListPage>();
            builder.Services.AddTransient<SceneListViewModel>();
            builder.Services.AddTransient<SceneListPage>();
            builder.Services.AddTransient<SceneTracksViewModel>();
            builder.Services.AddTransient<SceneTracksPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

    }

}
