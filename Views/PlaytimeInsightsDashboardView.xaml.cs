using PlaytimeInsights.Controls;
using PlaytimeInsights.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private void NestedScrollViewer_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            if (e.Handled || DashboardScrollViewer == null)
            {
                return;
            }

            var nestedScrollViewer = sender as ScrollViewer ??
                FindVisualChild<ScrollViewer>(sender as DependencyObject);
            if (CanContinueVerticalScroll(nestedScrollViewer, e.Delta))
            {
                return;
            }

            e.Handled = true;
            var forwardedEvent = new MouseWheelEventArgs(
                e.MouseDevice,
                e.Timestamp,
                e.Delta)
            {
                RoutedEvent = Mouse.MouseWheelEvent,
                Source = DashboardScrollViewer
            };
            DashboardScrollViewer.RaiseEvent(forwardedEvent);
        }

        private static bool CanContinueVerticalScroll(
            ScrollViewer scrollViewer,
            int wheelDelta)
        {
            if (scrollViewer == null || scrollViewer.ScrollableHeight <= 0)
            {
                return false;
            }

            if (wheelDelta > 0)
            {
                return scrollViewer.VerticalOffset > 0;
            }

            return wheelDelta < 0 &&
                scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
        }

        private static T FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0;
                index < VisualTreeHelper.GetChildrenCount(parent);
                index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                var match = child as T;
                if (match != null)
                {
                    return match;
                }

                match = FindVisualChild<T>(child);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void Refresh()
        {
            var viewModel = DataContext as DashboardViewModel;
            viewModel?.Refresh();
        }
    }
}
