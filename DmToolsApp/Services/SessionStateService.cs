namespace DmToolsApp.Services
{
    public class SessionStateService
    {
        public bool IsSessionActive { get; private set; }

        public event Action? StateChanged;

        public void SetActive(bool active)
        {
            if (IsSessionActive == active) return;
            IsSessionActive = active;
            StateChanged?.Invoke();
        }
    }
}
