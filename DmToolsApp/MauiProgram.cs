using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using DmToolsApp.Data;
using DmToolsApp.Features.AudioMixer;
using DmToolsApp.Features.Library;
using DmToolsApp.Models.Library;
using DmToolsApp.Services;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace DmToolsApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            string tracksDir = Path.Combine(FileSystem.AppDataDirectory, "Tracks");
            Directory.CreateDirectory(tracksDir);
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "FontRegular");
                    fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FontBrands");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontSolid");
                });
            builder.AddAudio();
            builder.Services.AddSingleton(
                new AppDatabase(dbPath));
            builder.Services.AddSingleton<AudioMixerService>();
            builder.Services.AddSingleton<TrackFileService>();
            builder.Services.AddSingleton<ILibraryPickerService, LibraryPickerService>();
            builder.Services.AddSingleton<ILibraryPickerNavigationService, LibraryPickerNavigationService>();
            builder.Services.AddSingleton<ILibraryDataService, LibraryDataService>();
            builder.Services.AddTransient<LibraryViewModel>();
            builder.Services.AddTransient<LibraryItemEditViewModel>();
            builder.Services.AddTransient<LibrarySelectorPage>();
            builder.Services.AddSingleton<AudioMixerViewModel>();
            builder.Services.AddTransient<AudioMixerPage>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        static string dbPath = Path.Combine(
    FileSystem.AppDataDirectory,
    "dmtools.db3");
    }

}
