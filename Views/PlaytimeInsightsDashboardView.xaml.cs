using PlaytimeInsights.Controls;
using PlaytimeInsights.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlaytimeInsights.Views
{
    public partial class PlaytimeInsightsDashboardView : UserControl
    {
        public PlaytimeInsightsDashboardView()
        {
            InitializeComponent();
            // Loaded is the sole automatic refresh boundary for sidebar activation.
            Loaded += PlaytimeInsightsDashboardView_Loaded;
        }

        private void PlaytimeInsightsDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void AdaptiveTrendChart_PeriodSelected(
            object sender,
            TrendPeriodSelectedEventArgs e)
        {
            // Custom control events stay in the View as typed command adapters.
            var command = (DataContext as DashboardViewModel)?.SelectPeriodCommand;
            if (command?.CanExecute(e.Period) == true)
            {
                command.Execute(e.Period);
            }
        }

        private void HeatmapCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var cell = element?.Tag as HeatmapCellViewModel;
            var command = (DataContext as DashboardViewModel)?
                .SelectHeatmapDateCommand;
            if (command?.CanExecute(cell) == true)
            {
                command.Execute(cell);
            }
        }

        private void NestedScrollViewer_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            // VisualTree inspection and routed-input handoff are WPF View concerns.
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

        private void DrilldownModule_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Visibility visibility &&
                visibility == Visibility.Visible &&
                sender is FrameworkElement element)
            {
                Dispatcher.BeginInvoke(
                    new Action(() => element.BringIntoView()),
                    DispatcherPriority.Loaded);
            }
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
            var command = (DataContext as DashboardViewModel)?.RefreshCommand;
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
            }
        }
    }
}
