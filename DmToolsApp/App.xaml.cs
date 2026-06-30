using DmToolsApp.Features.Onboarding;
using DmToolsApp.Services;

namespace DmToolsApp
{
    public partial class App : Application
    {
        private readonly AppShell _shell;
        private readonly IServiceProvider _services;

        public App(AppShell shell, IServiceProvider services)
        {
            InitializeComponent();
            _shell    = shell;
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            ThemeService.Instance.Initialize();
            FontService.Instance.Initialize();

            bool hasLaunched = Preferences.Default.Get("has_launched", false);
            if (!hasLaunched)
            {
                var onboarding = _services.GetRequiredService<OnboardingPage>();
                return new Window(onboarding);
            }

            return new Window(_shell);
        }
    }
}
