using PlaytimeInsights.Presentation.Coordinators;
using PlaytimeInsights.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Views
{
    public partial class SessionManagementView : UserControl
    {
        private readonly SessionManagementCoordinator coordinator;

        public SessionManagementView(
            SessionManagementCoordinator coordinator)
        {
            this.coordinator = coordinator ??
                throw new System.ArgumentNullException(nameof(coordinator));
            InitializeComponent();
            // Loaded is the sole automatic refresh boundary for sidebar activation.
            Loaded += SessionManagementView_Loaded;
        }

        private void SessionManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Refresh();
        }

        private void AdvancedOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            // PlacementTarget and ContextMenu opening depend on the concrete Button.
            var button = sender as Button;
            if (button?.ContextMenu == null)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }

        private void AddSessionButton_Click(object sender, RoutedEventArgs e)
        {
            // Multi-step UI workflows are coordinated outside the ViewModel.
            coordinator.AddSession();
        }

        private void EditSessionButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.EditSelectedSession();
        }

        private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.DeleteSelectedSession();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.ExportCsv();
        }

        private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.ExportJson();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.ImportSessions();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.CreateBackup();
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.RestoreBackup();
        }

        private void ReindexButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.Reindex();
        }

        private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            coordinator.SaveDiagnostics();
        }

        private SessionManagementViewModel ViewModel =>
            DataContext as SessionManagementViewModel;

    }
}
