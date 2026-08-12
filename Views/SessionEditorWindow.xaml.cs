using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using System.Windows;

namespace PlaytimeInsights.Views
{
    public partial class SessionEditorWindow : Window
    {
        public SessionEditorWindow()
        {
            InitializeComponent();
            // Work-area sizing belongs to the concrete WPF Window lifecycle.
            Loaded += (sender, args) =>
                WindowLayoutService.ConstrainToWorkArea(this);
        }

        public GameSession Result { get; private set; }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // The dialog owns validation-to-DialogResult translation.
            var viewModel = DataContext as SessionEditorViewModel;
            GameSession session;
            if (viewModel != null && viewModel.TryBuild(out session))
            {
                Result = session;
                DialogResult = true;
            }
        }
    }
}
