using CommunityToolkit.Mvvm.ComponentModel;

namespace DmToolsApp.Components
{
    public partial class ImportProgressViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string currentFileName = string.Empty;

        [ObservableProperty]
        private int processedCount;

        [ObservableProperty]
        private int totalCount;

        public string ProgressText => $"{ProcessedCount} / {TotalCount}";

        partial void OnProcessedCountChanged(int value) => OnPropertyChanged(nameof(ProgressText));
        partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(ProgressText));
    }
}
