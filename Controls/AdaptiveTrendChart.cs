using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PlaytimeInsights.Controls
{
    public sealed class TrendPeriodSelectedEventArgs : EventArgs
    {
        public TrendPeriodSelectedEventArgs(PeriodActivityViewModel period)
        {
            Period = period;
        }

        public PeriodActivityViewModel Period { get; }
    }

    public sealed class AdaptiveTrendChart : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(AdaptiveTrendChart),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public static readonly DependencyProperty SnapshotDateProperty =
            DependencyProperty.Register(
                nameof(SnapshotDate),
                typeof(DateTime?),
                typeof(AdaptiveTrendChart),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSnapshotDateChanged));

        private int hoverIndex = -1;
        private IList<PeriodActivityViewModel> renderedItems =
            new List<PeriodActivityViewModel>();
        private IList<Point> renderedPoints = new List<Point>();
        private int observableCount;

        // Y scale. The gridlines are only worth protecting from the area fill if
        // they carry values, so the plot reserves a measured left gutter and the
        // points normalise against a rounded-up maximum rather than the raw peak.
        // Both are cached here because GetPlotRect is also called from OnMouseMove
        // for hit-testing and must never disagree with what was drawn.
        private const double AxisLabelFontSize = 10d;
        private const double AxisGutterPadding = 8d;
        private ulong axisMaximumSeconds;
        private double axisGutter = 12d;

        // Frozen once per process. OnRender resolves the shared dictionary by key and
        // only reaches for these when the dictionary is not in scope, so no frame
        // allocates a gradient or a brush.
        private static readonly Brush FallbackTrendLineBrush =
            CreateFallbackTrendLineBrush();
        private static readonly Brush FallbackTrendAreaBrush =
            CreateFallbackTrendAreaBrush();
        private static readonly Brush FallbackTrendNodeFillBrush =
            CreateFrozenBrush(Color.FromRgb(74, 144, 226));
        private static readonly Brush FallbackTrendFutureFillBrush =
            CreateFrozenBrush(Color.FromArgb(13, 255, 255, 255));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // Local snapshot date of the current projection. Null keeps legacy
        // callers rendering without a today marker; the production dashboard
        // binds Distribution.SnapshotDate so the chart re-renders after both
        // the data and the date metadata have been applied.
        public DateTime? SnapshotDate
        {
            get => (DateTime?)GetValue(SnapshotDateProperty);
            set => SetValue(SnapshotDateProperty, value);
        }

        public event EventHandler<TrendPeriodSelectedEventArgs> PeriodSelected;

        private static void OnItemsSourceChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var chart = (AdaptiveTrendChart)dependencyObject;
            var oldCollection = args.OldValue as INotifyCollectionChanged;
            if (oldCollection != null)
            {
                CollectionChangedEventManager.RemoveHandler(
                    oldCollection,
                    chart.ItemsSource_CollectionChanged);
            }

            var newCollection = args.NewValue as INotifyCollectionChanged;
            if (newCollection != null)
            {
                CollectionChangedEventManager.AddHandler(
                    newCollection,
                    chart.ItemsSource_CollectionChanged);
            }

            chart.ResetRenderedState();
        }

        private void ItemsSource_CollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs args)
        {
            ResetRenderedState();
        }

        private static void OnSnapshotDateChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            // The date application completes the presentation update; drop any
            // stale hover state so it cannot outlive the old projection.
            ((AdaptiveTrendChart)dependencyObject).ResetRenderedState();
        }

        private void ResetRenderedState()
        {
            hoverIndex = -1;
            renderedItems = new List<PeriodActivityViewModel>();
            renderedPoints = new List<Point>();
            axisMaximumSeconds = 0;
            axisGutter = 12d;
            InvalidateVisual();
        }

        // Ceiling only, no 1/2/5/10 ladder: sub-hour peaks round up to the next
        // 10 minutes, anything from an hour up to the next whole hour. Both steps
        // keep the midpoint label (maximum / 2) a clean multiple. Public because it
        // is a pure function and the regression suite asserts its boundaries; there
        // is no InternalsVisibleTo in this project.
        public static ulong ResolveAxisMaximumSeconds(ulong peakSeconds)
        {
            if (peakSeconds == 0)
            {
                return 0;
            }

            var step = peakSeconds < 3600UL ? 600UL : 3600UL;
            var steps = peakSeconds / step;
            if (peakSeconds % step != 0)
            {
                steps++;
            }

            return steps * step;
        }

        private IList<FormattedText> CreateAxisLabels(Brush textBrush)
        {
            var labels = new List<FormattedText>();
            if (axisMaximumSeconds == 0)
            {
                return labels;
            }

            // Top, midpoint, baseline. FormatDuration is already localized, so no
            // new resource keys; the baseline is a culture-formatted numeral.
            labels.Add(CreateText(
                AnalyticsService.FormatDuration(axisMaximumSeconds),
                AxisLabelFontSize,
                textBrush));
            labels.Add(CreateText(
                AnalyticsService.FormatDuration(axisMaximumSeconds / 2UL),
                AxisLabelFontSize,
                textBrush));
            labels.Add(CreateText(
                0.ToString(CultureInfo.CurrentCulture),
                AxisLabelFontSize,
                textBrush));
            return labels;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            renderedItems = (ItemsSource ?? Enumerable.Empty<object>())
                .Cast<object>()
                .OfType<PeriodActivityViewModel>()
                .ToList();

            var separator = ResolveBrush("PanelSeparatorBrush", Color.FromArgb(80, 128, 128, 128));
            var textBrush = ResolveBrush("TextBrush", Colors.White);

            // Observable prefix: future periods form the tail of the collection
            // and are never drawn as data. The full collection keeps its slot
            // indices so hit-testing stays aligned with the drawn geometry.
            observableCount = 0;
            while (observableCount < renderedItems.Count &&
                !renderedItems[observableCount].IsFuture)
            {
                observableCount++;
            }

            // Axis first: the maximum depends only on observable items, and the
            // gutter depends only on the label widths, so both are known before
            // any geometry. Measure the labels rather than guessing a width, so
            // an axis label is never clipped.
            axisMaximumSeconds = observableCount == 0
                ? 0UL
                : ResolveAxisMaximumSeconds(
                    renderedItems.Take(observableCount).Max(item => item.Seconds));
            var axisLabels = CreateAxisLabels(textBrush);
            axisGutter = 12d;
            if (axisLabels.Count > 0)
            {
                var widest = axisLabels.Max(label => label.Width);
                axisGutter = Math.Min(
                    Math.Max(12d, widest + AxisGutterPadding + 4d),
                    Math.Max(12d, ActualWidth * 0.3));
            }

            renderedPoints = CreatePoints(renderedItems);
            if (renderedPoints.Count == 0)
            {
                return;
            }

            var areaBrush = ResolveBrush("TrendAreaFillBrush", FallbackTrendAreaBrush);
            var lineBrush = ResolveBrush("TrendLineBrush", FallbackTrendLineBrush);
            var nodeFillBrush = ResolveBrush("TrendNodeFillBrush", FallbackTrendNodeFillBrush);

            // Option A: one theme-tracking ring shared by the normal and hover nodes.
            var nodeRingBrush = ResolveBrush("ControlBackgroundBrush", Colors.Black);
            var plot = GetPlotRect();
            var gridPen = new Pen(separator, 1);
            var ratios = new[] { 0d, 0.5d, 1d };
            for (var index = 0; index < ratios.Length; index++)
            {
                var y = plot.Top + plot.Height * ratios[index];
                drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));

                // Right-align each value against its own gridline so the scale
                // reads without hovering. Axis text wears text ink, never the
                // series colour.
                if (index < axisLabels.Count)
                {
                    var label = axisLabels[index];
                    drawingContext.DrawText(
                        label,
                        new Point(
                            Math.Max(0d, plot.Left - AxisGutterPadding - label.Width),
                            y - label.Height / 2));
                }
            }

            // Low-emphasis future region: starts midway between the last
            // observable slot and the first future slot so the curve never
            // appears to reach into it.
            if (observableCount < renderedItems.Count)
            {
                var futureFill = ResolveBrush(
                    "TrendFutureFillBrush",
                    FallbackTrendFutureFillBrush);
                var boundaryX = observableCount == 0
                    ? plot.Left
                    : (renderedPoints[observableCount - 1].X +
                        renderedPoints[observableCount].X) / 2;
                drawingContext.DrawRectangle(
                    futureFill,
                    null,
                    new Rect(
                        boundaryX,
                        plot.Top,
                        Math.Max(0d, plot.Right - boundaryX),
                        plot.Height));
            }

            if (observableCount == 0)
            {
                // No observable period at all: the axis stays, the curve does
                // not, and the state text explains the empty plot.
                var futureOnlyText = CreateText(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsTrendFutureOnly",
                        "所选范围尚未开始"),
                    12,
                    textBrush,
                    FontWeights.SemiBold);
                drawingContext.DrawText(
                    futureOnlyText,
                    new Point(
                        plot.Left + Math.Max(0d, (plot.Width - futureOnlyText.Width) / 2),
                        plot.Top + Math.Max(0d, (plot.Height - futureOnlyText.Height) / 2)));
                return;
            }

            var observablePoints = renderedPoints.Take(observableCount).ToList();
            if (observableCount > 1)
            {
                var area = CreateSmoothGeometry(observablePoints, plot.Bottom, true);
                drawingContext.DrawGeometry(areaBrush, null, area);
            }

            var line = CreateSmoothGeometry(observablePoints, plot.Bottom, false);
            var thickness = renderedItems.Count >= 180
                ? 1
                : renderedItems.Count >= 90 ? 1.5 : 2.5;
            var linePen = new Pen(lineBrush, thickness);
            if (linePen.CanFreeze)
            {
                linePen.Freeze();
            }

            drawingContext.DrawGeometry(null, linePen, line);

            if (renderedItems.Count <= 90)
            {
                var nodePen = new Pen(nodeRingBrush, 1.5);
                if (nodePen.CanFreeze)
                {
                    nodePen.Freeze();
                }

                foreach (var point in observablePoints)
                {
                    drawingContext.DrawEllipse(nodeFillBrush, nodePen, point, 3d, 3d);
                }
            }

            DrawTodayMarker(drawingContext, plot);
            DrawSparseLabels(drawingContext, plot, textBrush);
            DrawHover(drawingContext, plot, textBrush, nodeRingBrush);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (renderedItems.Count == 0)
            {
                return;
            }

            var plot = GetPlotRect();
            var x = Math.Max(plot.Left, Math.Min(plot.Right, e.GetPosition(this).X));
            var next = renderedItems.Count == 1
                ? 0
                : (int)Math.Round((x - plot.Left) / plot.Width * (renderedItems.Count - 1));
            if (next != hoverIndex)
            {
                hoverIndex = next;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            // Future slots show a state tooltip only; they must not open a
            // drilldown for a period that has not started.
            if (hoverIndex >= 0 && hoverIndex < renderedItems.Count &&
                !renderedItems[hoverIndex].IsFuture)
            {
                PeriodSelected?.Invoke(
                    this,
                    new TrendPeriodSelectedEventArgs(renderedItems[hoverIndex]));
            }
        }

        private Rect GetPlotRect()
        {
            // axisGutter is cached by OnRender so this stays cheap for the
            // per-mouse-move hit-test path and can never disagree with the
            // geometry that was actually drawn.
            var left = Math.Max(12d, axisGutter);
            return new Rect(
                left,
                12,
                Math.Max(1, ActualWidth - left - 12),
                Math.Max(1, ActualHeight - 42));
        }

        private IList<Point> CreatePoints(IList<PeriodActivityViewModel> items)
        {
            var plot = GetPlotRect();

            // Normalise against the rounded axis maximum, not the raw peak, so the
            // top gridline is a real reference instead of a restatement of the peak.
            // Future slots anchor to the baseline: they carry no drawn value, so
            // hovering them must not float a crosshair above the plot.
            var maximum = axisMaximumSeconds;
            var points = new List<Point>(items.Count);
            for (var index = 0; index < items.Count; index++)
            {
                var x = items.Count == 1
                    ? plot.Left + plot.Width / 2
                    : plot.Left + plot.Width * index / (items.Count - 1);
                var y = items[index].IsFuture || maximum == 0
                    ? plot.Bottom
                    : plot.Bottom - plot.Height * items[index].Seconds / maximum;
                points.Add(new Point(x, y));
            }
            return points;
        }

        private int FindTodayIndex()
        {
            for (var index = 0; index < renderedItems.Count; index++)
            {
                if (renderedItems[index].ContainsToday)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string ResolveTodayLabel(PeriodActivityViewModel period)
        {
            // A day period labels itself "today"; a week/month/year period keeps
            // its whole-slot meaning instead of claiming an exact moment.
            return period.PeriodStart.Date == period.PeriodEnd.Date
                ? LocalizationService.Get("LOCPlaytimeInsightsTrendToday", "今天")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsTrendCurrentPeriod",
                    "本周期·截至今天");
        }

        private void DrawTodayMarker(DrawingContext context, Rect plot)
        {
            var todayIndex = FindTodayIndex();
            if (todayIndex < 0)
            {
                return;
            }

            var accent = ResolveBrush("GlyphBrush", Color.FromRgb(120, 177, 235));
            var tickPen = new Pen(accent, 1.5);
            if (tickPen.CanFreeze)
            {
                tickPen.Freeze();
            }

            var x = renderedPoints[todayIndex].X;
            context.DrawLine(tickPen, new Point(x, plot.Bottom - 10), new Point(x, plot.Bottom));
        }

        private void DrawSparseLabels(
            DrawingContext context,
            Rect plot,
            Brush textBrush)
        {
            var maximumLabels = Math.Max(2, (int)(plot.Width / 88));
            var step = Math.Max(1, (int)Math.Ceiling(
                (double)renderedItems.Count / maximumLabels));
            var lastIndex = renderedItems.Count - 1;
            var lastText = CreateText(
                renderedItems[lastIndex].Label,
                10,
                textBrush);
            var lastLeft = Math.Max(
                plot.Left,
                Math.Min(
                    plot.Right - lastText.Width,
                    renderedPoints[lastIndex].X - lastText.Width / 2));

            // The today label anchors to its own slot and wins collisions: when
            // it would overlap a normal tick, the normal tick is dropped.
            var todayIndex = FindTodayIndex();
            FormattedText todayText = null;
            double todayLeft = 0d;
            if (todayIndex >= 0)
            {
                var accent = ResolveBrush("GlyphBrush", Color.FromRgb(120, 177, 235));
                todayText = CreateText(
                    ResolveTodayLabel(renderedItems[todayIndex]),
                    10,
                    accent,
                    FontWeights.SemiBold);
                todayLeft = Math.Max(
                    plot.Left,
                    Math.Min(
                        plot.Right - todayText.Width,
                        renderedPoints[todayIndex].X - todayText.Width / 2));
            }

            var lastIsToday = todayIndex == lastIndex;
            var previousRight = double.NegativeInfinity;
            foreach (var index in Enumerable.Range(0, renderedItems.Count)
                .Where(index =>
                    index != lastIndex &&
                    index != todayIndex &&
                    (index == 0 || index % step == 0)))
            {
                var text = CreateText(renderedItems[index].Label, 10, textBrush);
                var x = Math.Max(
                    plot.Left,
                    Math.Min(plot.Right - text.Width, renderedPoints[index].X - text.Width / 2));
                if (x < previousRight + 8 || x + text.Width > lastLeft - 8)
                {
                    continue;
                }

                if (todayText != null && !lastIsToday &&
                    x < todayLeft + todayText.Width + 8 &&
                    todayLeft < x + text.Width + 8)
                {
                    continue;
                }

                context.DrawText(text, new Point(x, plot.Bottom + 7));
                previousRight = x + text.Width;
            }

            if (todayText != null)
            {
                context.DrawText(todayText, new Point(todayLeft, plot.Bottom + 7));
                previousRight = Math.Max(previousRight, todayLeft + todayText.Width);
            }

            if (!lastIsToday && lastLeft >= previousRight + 8)
            {
                context.DrawText(
                    lastText,
                    new Point(lastLeft, plot.Bottom + 7));
            }
        }

        private void DrawHover(
            DrawingContext context,
            Rect plot,
            Brush textBrush,
            Brush nodeRingBrush)
        {
            if (hoverIndex < 0 || hoverIndex >= renderedPoints.Count)
            {
                return;
            }

            var point = renderedPoints[hoverIndex];
            var popupBackground = ResolveBrush("PopupBackgroundBrush",
                Color.FromRgb(35, 37, 44));
            var separator = ResolveBrush("PanelSeparatorBrush",
                Color.FromArgb(150, 74, 144, 226));
            var glyph = ResolveBrush("GlyphBrush",
                Color.FromRgb(120, 177, 235));
            var crosshairPen = new Pen(
                glyph,
                1)
            {
                DashStyle = DashStyles.Dash
            };
            context.DrawLine(
                crosshairPen,
                new Point(point.X, plot.Top),
                new Point(point.X, plot.Bottom));

            var item = renderedItems[hoverIndex];
            if (item.IsFuture)
            {
                // Future slot: state card only. No node, no duration - showing
                // a zero would misread as "played nothing that day".
                var futureState = CreateText(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsTrendFuture",
                        "未来日期"),
                    11,
                    glyph);
                var futureDate = CreateText(
                    item.Label,
                    11,
                    textBrush,
                    FontWeights.SemiBold);
                var futureWidth = Math.Min(
                    Math.Max(140, Math.Max(futureDate.Width, futureState.Width) + 24),
                    Math.Max(140, ActualWidth - 24));
                var futureLeft = point.X + 14;
                if (futureLeft + futureWidth > ActualWidth - 8)
                {
                    futureLeft = point.X - futureWidth - 14;
                }

                futureLeft = Math.Max(8, futureLeft);
                var futureTop = Math.Max(8, Math.Min(point.Y - 50, plot.Bottom - 56));
                var futureCard = new Rect(futureLeft, futureTop, futureWidth, 46);
                context.DrawRoundedRectangle(
                    popupBackground,
                    new Pen(separator, 1),
                    futureCard,
                    7,
                    7);
                context.PushClip(new RectangleGeometry(new Rect(
                    futureCard.Left + 10,
                    futureCard.Top + 5,
                    futureCard.Width - 20,
                    futureCard.Height - 10)));
                context.DrawText(futureDate, new Point(futureCard.Left + 11, futureCard.Top + 6));
                context.DrawText(futureState, new Point(futureCard.Left + 11, futureCard.Top + 24));
                context.Pop();
                return;
            }

            // Same ring brush as the normal nodes, only a larger radius.
            context.DrawEllipse(
                glyph,
                new Pen(nodeRingBrush, 1),
                point,
                4.5,
                4.5);

            var date = CreateText(item.Label, 11, textBrush, FontWeights.SemiBold);
            var games = CreateText(item.GameSummaryText ?? string.Empty, 11, textBrush);
            var duration = CreateText(
                item.HoverDurationText ?? item.DurationText,
                11,
                textBrush,
                FontWeights.SemiBold);
            var width = Math.Min(
                Math.Max(220, Math.Max(date.Width, Math.Max(games.Width, duration.Width)) + 24),
                Math.Max(220, ActualWidth - 24));
            var left = point.X + 14;
            if (left + width > ActualWidth - 8)
            {
                left = point.X - width - 14;
            }
            left = Math.Max(8, left);
            var top = Math.Max(8, Math.Min(point.Y - 70, plot.Bottom - 76));
            var card = new Rect(left, top, width, 66);
            context.DrawRoundedRectangle(
                popupBackground,
                new Pen(separator, 1),
                card,
                7,
                7);
            context.PushClip(new RectangleGeometry(new Rect(
                card.Left + 10,
                card.Top + 6,
                card.Width - 20,
                card.Height - 12)));
            context.DrawText(date, new Point(card.Left + 11, card.Top + 7));
            context.DrawText(games, new Point(card.Left + 11, card.Top + 25));
            context.DrawText(duration, new Point(card.Left + 11, card.Top + 43));
            context.Pop();
        }

        private static Geometry CreateSmoothGeometry(
            IList<Point> points,
            double baseline,
            bool closeArea)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                if (closeArea)
                {
                    context.BeginFigure(
                        new Point(points[0].X, baseline),
                        true,
                        true);
                    context.LineTo(points[0], true, false);
                }
                else
                {
                    context.BeginFigure(points[0], false, false);
                }

                if (points.Count == 1)
                {
                    context.LineTo(new Point(points[0].X + 0.1, points[0].Y), true, false);
                }
                else
                {
                    var tangents = CreateTangents(points);
                    for (var index = 0; index < points.Count - 1; index++)
                    {
                        var width = points[index + 1].X - points[index].X;
                        context.BezierTo(
                            new Point(
                                points[index].X + width / 3,
                                points[index].Y + tangents[index] * width / 3),
                            new Point(
                                points[index + 1].X - width / 3,
                                points[index + 1].Y - tangents[index + 1] * width / 3),
                            points[index + 1],
                            true,
                            false);
                    }
                }

                if (closeArea)
                {
                    context.LineTo(
                        new Point(points[points.Count - 1].X, baseline),
                        true,
                        false);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private static double[] CreateTangents(IList<Point> points)
        {
            var slopes = new double[points.Count - 1];
            var tangents = new double[points.Count];
            for (var index = 0; index < slopes.Length; index++)
            {
                slopes[index] = (points[index + 1].Y - points[index].Y) /
                    (points[index + 1].X - points[index].X);
            }
            tangents[0] = slopes[0];
            tangents[tangents.Length - 1] = slopes[slopes.Length - 1];
            for (var index = 1; index < tangents.Length - 1; index++)
            {
                var left = slopes[index - 1];
                var right = slopes[index];
                tangents[index] = left == 0 || right == 0 ||
                    Math.Sign(left) != Math.Sign(right)
                    ? 0
                    : 2 * left * right / (left + right);
            }
            return tangents;
        }

        private Brush ResolveBrush(string key, Color fallback)
        {
            return TryFindResource(key) as Brush ??
                new SolidColorBrush(fallback);
        }

        // TrendLineBrush and TrendAreaFillBrush are gradients, which the Color
        // overload cannot express; both overloads short-circuit, so a present
        // resource costs no allocation.
        private Brush ResolveBrush(string key, Brush fallback)
        {
            return TryFindResource(key) as Brush ?? fallback;
        }

        private static Brush CreateFallbackTrendLineBrush()
        {
            var brush = new LinearGradientBrush(
                Color.FromRgb(47, 140, 255),
                Color.FromRgb(164, 92, 255),
                new Point(0, 0),
                new Point(1, 0));
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }

        private static Brush CreateFallbackTrendAreaBrush()
        {
            // Must mirror TrendAreaFillBrush in the shared dictionary; a contract
            // test asserts the two stay in sync.
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(90, 59, 130, 246),
                0));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(46, 91, 124, 250),
                0.62));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, 91, 124, 250),
                1));
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }

        private static FormattedText CreateText(
            string value,
            double size,
            Brush brush,
            FontWeight? weight = null)
        {
            return new FormattedText(
                value ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    weight ?? FontWeights.Normal,
                    FontStretches.Normal),
                size,
                brush,
                1.0);
        }
    }
}
