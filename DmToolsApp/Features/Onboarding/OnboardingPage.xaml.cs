namespace DmToolsApp.Features.Onboarding
{
    public partial class OnboardingPage : ContentPage
    {
        public OnboardingPage(OnboardingViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
