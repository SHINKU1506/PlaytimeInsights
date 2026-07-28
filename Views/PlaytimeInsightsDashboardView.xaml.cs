using PlaytimeInsights.Controls;
using PlaytimeInsights.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlaytimeInsights.Views
{
    public partial class PlaytimeInsightsDashboardView : UserControl
    {
        public PlaytimeInsightsDashboardView()
        {
            InitializeComponent();
            Loaded += PlaytimeInsightsDashboardView_Loaded;
        }

        private void PlaytimeInsightsDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void AdaptiveTrendChart_PeriodSelected(
            object sender,
            TrendPeriodSelectedEventArgs e)
        {
            (DataContext as DashboardViewModel)?.SelectPeriod(e.Period);
        }

        private void HeatmapCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var cell = element?.Tag as HeatmapCellViewModel;
            (DataContext as DashboardViewModel)?.SelectHeatmapDate(cell);
        }

        private void WeekdayDistribution_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var bar = element?.Tag as DistributionBarViewModel;
            (DataContext as DashboardViewModel)?.SelectWeekdayDistribution(bar);
        }

        private void LoadMoreSessionDetails_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as DashboardViewModel)?.LoadMoreSessionDetails();
        }

        private void Refresh()
        {
            var viewModel = DataContext as DashboardViewModel;
            viewModel?.Refresh();
        }
    }
}
