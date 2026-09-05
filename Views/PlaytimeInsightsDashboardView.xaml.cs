using PlaytimeInsights.Controls;
using PlaytimeInsights.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PlaytimeInsights.Views
{
    public partial class PlaytimeInsightsDashboardView : UserControl
    {
        private const double DrilldownHeaderBandHeight = 96d;
        private DashboardViewModel presentationViewModel;
        private string lastDrilldownAutomationName;
        private bool rankingTabMouseInteraction;
        private readonly DispatcherTimer rankingTabMouseInteractionTimer;

        // V5 distribution layout: measured viewport width -> local size values.
        // The DataTemplates bind these; nothing here touches analytics state.
        public static readonly DependencyProperty HourContentWidthProperty =
            DependencyProperty.Register(
                nameof(HourContentWidth),
                typeof(double),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(480d));

        public static readonly DependencyProperty HourBarWidthProperty =
            DependencyProperty.Register(
                nameof(HourBarWidth),
                typeof(double),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(16d));

        public static readonly DependencyProperty HourLabelStepProperty =
            DependencyProperty.Register(
                nameof(HourLabelStep),
                typeof(int),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(2));

        public static readonly DependencyProperty WeekHourContentWidthProperty =
            DependencyProperty.Register(
                nameof(WeekHourContentWidth),
                typeof(double),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(672d));

        public static readonly DependencyProperty WeekHourSlotWidthProperty =
            DependencyProperty.Register(
                nameof(WeekHourSlotWidth),
                typeof(double),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(26d));

        public static readonly DependencyProperty WeekHourCellSizeProperty =
            DependencyProperty.Register(
                nameof(WeekHourCellSize),
                typeof(double),
                typeof(PlaytimeInsightsDashboardView),
                new PropertyMetadata(24d));

        private double lastHourViewportWidth = -1d;
        private double lastWeekHourViewportWidth = -1d;

        public double HourContentWidth
        {
            get => (double)GetValue(HourContentWidthProperty);
            set => SetValue(HourContentWidthProperty, value);
        }

        public double HourBarWidth
        {
            get => (double)GetValue(HourBarWidthProperty);
            set => SetValue(HourBarWidthProperty, value);
        }

        public int HourLabelStep
        {
            get => (int)GetValue(HourLabelStepProperty);
            set => SetValue(HourLabelStepProperty, value);
        }

        public double WeekHourContentWidth
        {
            get => (double)GetValue(WeekHourContentWidthProperty);
            set => SetValue(WeekHourContentWidthProperty, value);
        }

        public double WeekHourSlotWidth
        {
            get => (double)GetValue(WeekHourSlotWidthProperty);
            set => SetValue(WeekHourSlotWidthProperty, value);
        }

        public double WeekHourCellSize
        {
            get => (double)GetValue(WeekHourCellSizeProperty);
            set => SetValue(WeekHourCellSizeProperty, value);
        }

        public PlaytimeInsightsDashboardView()
        {
            InitializeComponent();
            rankingTabMouseInteractionTimer = new DispatcherTimer(
                DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            rankingTabMouseInteractionTimer.Tick +=
                RankingTabMouseInteractionTimer_Tick;
            DataContextChanged += PlaytimeInsightsDashboardView_DataContextChanged;
            // Loaded is the sole automatic refresh boundary for sidebar activation.
            Loaded += PlaytimeInsightsDashboardView_Loaded;
        }

        private void PlaytimeInsightsDashboardView_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (presentationViewModel != null)
            {
                presentationViewModel.PropertyChanged -=
                    PresentationViewModel_PropertyChanged;
            }

            presentationViewModel = DataContext as DashboardViewModel;
            lastDrilldownAutomationName =
                presentationViewModel?.SelectedDetailTitle;
            if (presentationViewModel != null)
            {
                presentationViewModel.PropertyChanged +=
                    PresentationViewModel_PropertyChanged;
            }
        }

        private void PresentationViewModel_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(
                DashboardViewModel.SelectedDetailTitle))
            {
                Dispatcher.BeginInvoke(
                    new Action(RaiseDrilldownAutomationNameChanged),
                    DispatcherPriority.Loaded);
            }

            if (e.PropertyName != nameof(DashboardViewModel.PresentationRevision))
            {
                return;
            }

            var viewModel = presentationViewModel;
            if (viewModel == null)
            {
                return;
            }

            var revision = viewModel.PresentationRevision;
            var transition = viewModel.PresentationTransition;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (!ReferenceEquals(DataContext, viewModel) ||
                        viewModel.PresentationRevision != revision)
                    {
                        return;
                    }

                    PlayEntrance(transition);
                }));
        }

        private void PlayEntrance(DashboardPresentationTransition transition)
        {
            var animationsEnabled = SystemParameters.ClientAreaAnimation;
            var plan = DashboardEntrancePlan.Create(
                transition,
                animationsEnabled);
            foreach (var step in plan.Steps)
            {
                var host = FindName(step.HostName) as FrameworkElement;
                if (host == null)
                {
                    continue;
                }

                PlayEntranceStep(host, step, animationsEnabled);
            }
        }

        private static void PlayEntranceStep(
            FrameworkElement host,
            DashboardEntranceStep step,
            bool animationsEnabled)
        {
            var translate = host.RenderTransform as TranslateTransform;
            host.BeginAnimation(OpacityProperty, null);
            if (translate != null)
            {
                translate.BeginAnimation(TranslateTransform.YProperty, null);
            }

            host.Opacity = 1;
            if (translate != null)
            {
                translate.Y = 0;
            }

            if (!animationsEnabled || step.DurationMilliseconds <= 0)
            {
                return;
            }

            var duration = TimeSpan.FromMilliseconds(
                step.DurationMilliseconds);
            var beginTime = TimeSpan.FromMilliseconds(
                step.DelayMilliseconds);
            var easing = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };
            host.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0d, 1d, duration)
                {
                    BeginTime = beginTime,
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.Stop
                },
                HandoffBehavior.SnapshotAndReplace);
            if (translate != null)
            {
                translate.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(step.OffsetY, 0d, duration)
                    {
                        BeginTime = beginTime,
                        EasingFunction = easing,
                        FillBehavior = FillBehavior.Stop
                    },
                    HandoffBehavior.SnapshotAndReplace);
            }
        }

        private void PlaytimeInsightsDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void RankingModule_PreviewMouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            rankingTabMouseInteraction = true;
            rankingTabMouseInteractionTimer.Stop();
            rankingTabMouseInteractionTimer.Start();
        }

        private void RankingModule_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            rankingTabMouseInteraction = false;
            rankingTabMouseInteractionTimer.Stop();
        }

        private void RankingTabMouseInteractionTimer_Tick(
            object sender,
            EventArgs e)
        {
            rankingTabMouseInteractionTimer.Stop();
            rankingTabMouseInteraction = false;
        }

        private void RankingModule_RequestBringIntoView(
            object sender,
            RequestBringIntoViewEventArgs e)
        {
            if (!rankingTabMouseInteraction)
            {
                return;
            }

            // A mouse-triggered ranking tab switch owns a short lifecycle.
            // Suppress every bring-into-view request bubbled by that TabControl,
            // including delayed requests from the content host after layout.
            e.Handled = true;
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

        private void HeatmapWeekList_ScrollChanged(
            object sender,
            ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange == 0)
            {
                return;
            }

            HeatmapMonthScrollViewer?.ScrollToHorizontalOffset(
                e.HorizontalOffset);
        }

        private void DistributionViewport_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            var viewport = sender as ScrollViewer;
            if (viewport == null)
            {
                return;
            }

            // The ScrollViewer is the finite viewport (vertical scrollbar is
            // disabled); never measure the scrollable content itself.
            var width = viewport.ViewportWidth;
            if (width <= 0d || double.IsNaN(width) || double.IsInfinity(width))
            {
                width = viewport.ActualWidth;
            }

            if (width <= 0d || double.IsNaN(width) || double.IsInfinity(width))
            {
                return;
            }

            if (ReferenceEquals(sender, WeekHourHeatmapScrollViewer))
            {
                if (Math.Abs(width - lastWeekHourViewportWidth) < 0.5d)
                {
                    return;
                }

                lastWeekHourViewportWidth = width;
            }
            else
            {
                if (Math.Abs(width - lastHourViewportWidth) < 0.5d)
                {
                    return;
                }

                lastHourViewportWidth = width;
            }

            ApplyDistributionLayout(viewport, width);
        }

        private void ApplyDistributionLayout(
            ScrollViewer viewport,
            double viewportWidth)
        {
            var metrics = DistributionLayoutMetrics.Create(viewportWidth);
            HourContentWidth = metrics.HourContentWidth;
            HourBarWidth = metrics.HourBarWidth;
            HourLabelStep = metrics.HourLabelStep;
            WeekHourContentWidth = metrics.WeekHourContentWidth;
            WeekHourSlotWidth = metrics.WeekHourSlotWidth;
            WeekHourCellSize = metrics.WeekHourCellSize;

            // Centering only while the content is narrower than the viewport;
            // an overflowing centered child would clip its leading edge.
            if (HourDistributionChart != null)
            {
                HourDistributionChart.HorizontalAlignment =
                    metrics.HourCentersContent
                        ? HorizontalAlignment.Center
                        : HorizontalAlignment.Left;
            }

            if (WeekHourHeatmapGrid != null)
            {
                WeekHourHeatmapGrid.HorizontalAlignment =
                    metrics.WeekHourCentersContent
                        ? HorizontalAlignment.Center
                        : HorizontalAlignment.Left;
            }

            // Content widths land after this layout pass; normalize the saved
            // offsets then. Selections, queries and drilldown state stay put.
            viewport.Dispatcher.BeginInvoke(
                new Action(() => ClampDistributionOffset(viewport)),
                DispatcherPriority.Loaded);
        }

        private static void ClampDistributionOffset(ScrollViewer viewport)
        {
            if (viewport.ScrollableWidth <= 0d)
            {
                if (viewport.HorizontalOffset != 0d)
                {
                    viewport.ScrollToHorizontalOffset(0d);
                }

                return;
            }

            var clamped = Math.Min(
                viewport.HorizontalOffset,
                viewport.ScrollableWidth);
            if (Math.Abs(clamped - viewport.HorizontalOffset) > 0.01d)
            {
                viewport.ScrollToHorizontalOffset(clamped);
            }
        }

        private void DrilldownHost_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is bool isVisible) ||
                !isVisible ||
                !(sender is FrameworkElement host))
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (host.Visibility != Visibility.Visible)
                {
                    return;
                }

                ScrollHeaderBandIntoView(
                    host,
                    DashboardScrollViewer,
                    DrilldownHeaderBandHeight);
            }), DispatcherPriority.Loaded);
        }

        private static void ScrollHeaderBandIntoView(
            FrameworkElement host,
            ScrollViewer scrollViewer,
            double bandHeight)
        {
            if (IsHeaderBandVisible(host, scrollViewer, bandHeight))
            {
                return;
            }

            Rect bounds;
            if (!TryGetHeaderBandBounds(
                host,
                scrollViewer,
                bandHeight,
                out bounds))
            {
                return;
            }

            var offsetDelta = bounds.Top < 0d
                ? bounds.Top
                : bounds.Bottom > scrollViewer.ViewportHeight
                    ? bounds.Bottom - scrollViewer.ViewportHeight
                    : 0d;
            if (Math.Abs(offsetDelta) < 0.01d)
            {
                return;
            }

            var targetOffset = Math.Max(
                0d,
                Math.Min(
                    scrollViewer.ScrollableHeight,
                    scrollViewer.VerticalOffset + offsetDelta));
            scrollViewer.ScrollToVerticalOffset(targetOffset);
        }

        private static bool IsHeaderBandVisible(
            FrameworkElement host,
            ScrollViewer scrollViewer,
            double bandHeight)
        {
            Rect bounds;
            if (!TryGetHeaderBandBounds(
                host,
                scrollViewer,
                bandHeight,
                out bounds))
            {
                return false;
            }

            return IsVerticalBandVisible(
                bounds.Top,
                bounds.Height,
                scrollViewer.ViewportHeight);
        }

        private static bool TryGetHeaderBandBounds(
            FrameworkElement host,
            ScrollViewer scrollViewer,
            double bandHeight,
            out Rect bounds)
        {
            bounds = Rect.Empty;
            if (host == null ||
                scrollViewer == null ||
                bandHeight <= 0d ||
                host.ActualHeight <= 0d)
            {
                return false;
            }

            try
            {
                var visibleBandHeight = Math.Min(
                    bandHeight,
                    host.ActualHeight);
                bounds = host.TransformToAncestor(scrollViewer)
                    .TransformBounds(new Rect(
                        0d,
                        0d,
                        Math.Max(0d, host.ActualWidth),
                        visibleBandHeight));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool IsVerticalBandVisible(
            double top,
            double height,
            double viewportHeight)
        {
            return top >= 0d &&
                height > 0d &&
                viewportHeight > 0d &&
                top + height <= viewportHeight;
        }

        private void RaiseDrilldownAutomationNameChanged()
        {
            var viewModel = presentationViewModel;
            if (viewModel == null)
            {
                return;
            }

            var currentName = viewModel.SelectedDetailTitle ?? string.Empty;
            var previousName = lastDrilldownAutomationName ?? string.Empty;
            lastDrilldownAutomationName = currentName;
            if (string.Equals(
                previousName,
                currentName,
                StringComparison.Ordinal))
            {
                return;
            }

            var host = TrendDrilldownHost.IsVisible
                ? TrendDrilldownHost
                : DistributionDrilldownHost.IsVisible
                    ? DistributionDrilldownHost
                    : null;
            if (host == null)
            {
                return;
            }

            var peer = FrameworkElementAutomationPeer.FromElement(host) ??
                new FrameworkElementAutomationPeer(host);
            peer.RaisePropertyChangedEvent(
                AutomationElementIdentifiers.NameProperty,
                previousName,
                currentName);
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
