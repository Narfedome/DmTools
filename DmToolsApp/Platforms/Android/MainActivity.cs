using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace DmToolsApp
{
    // WindowSoftInputMode=AdjustResize : sans ça, le clavier recouvre les champs de saisie des popups
    // (Renommer, création de campagne/chapitre/scène...) au lieu de réduire la fenêtre disponible pour
    // les laisser visibles.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, WindowSoftInputMode = SoftInput.AdjustResize)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
