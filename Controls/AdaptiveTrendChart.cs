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

        private int hoverIndex = -1;
        private IList<PeriodActivityViewModel> renderedItems =
            new List<PeriodActivityViewModel>();
        private IList<Point> renderedPoints = new List<Point>();

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
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

        private void ResetRenderedState()
        {
            hoverIndex = -1;
            renderedItems = new List<PeriodActivityViewModel>();
            renderedPoints = new List<Point>();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            renderedItems = (ItemsSource ?? Enumerable.Empty<object>())
                .Cast<object>()
                .OfType<PeriodActivityViewModel>()
                .ToList();
            renderedPoints = CreatePoints(renderedItems);
            if (renderedPoints.Count == 0)
            {
                return;
            }

            var separator = ResolveBrush("PanelSeparatorBrush", Color.FromArgb(80, 128, 128, 128));
            var textBrush = ResolveBrush("TextBrush", Colors.White);
            var plot = GetPlotRect();
            var gridPen = new Pen(separator, 1);
            foreach (var ratio in new[] { 0d, 0.5d, 1d })
            {
                var y = plot.Top + plot.Height * ratio;
                drawingContext.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            }

            var area = CreateSmoothGeometry(renderedPoints, plot.Bottom, true);
            var line = CreateSmoothGeometry(renderedPoints, plot.Bottom, false);
            var areaBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            areaBrush.GradientStops.Add(new GradientStop(
                Color.FromArgb(102, 63, 140, 255),
                0));
            areaBrush.GradientStops.Add(new GradientStop(
                Color.FromArgb(31, 122, 101, 255),
                0.65));
            areaBrush.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, 122, 101, 255),
                1));
            drawingContext.DrawGeometry(areaBrush, null, area);
            var thickness = renderedItems.Count >= 180
                ? 1
                : renderedItems.Count >= 90 ? 1.5 : 2.5;
            var lineBrush = new LinearGradientBrush(
                Color.FromRgb(47, 140, 255),
                Color.FromRgb(164, 92, 255),
                new Point(0, 0),
                new Point(1, 0));
            drawingContext.DrawGeometry(
                null,
                new Pen(lineBrush, thickness),
                line);

            if (renderedItems.Count <= 90)
            {
                var nodeBrush = new SolidColorBrush(Color.FromRgb(74, 144, 226));
                foreach (var point in renderedPoints)
                {
                    drawingContext.DrawEllipse(nodeBrush, null, point, 3, 3);
                }
            }

            DrawSparseLabels(drawingContext, plot, textBrush);
            DrawHover(drawingContext, plot, textBrush);
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
            if (hoverIndex >= 0 && hoverIndex < renderedItems.Count)
            {
                PeriodSelected?.Invoke(
                    this,
                    new TrendPeriodSelectedEventArgs(renderedItems[hoverIndex]));
            }
        }

        private Rect GetPlotRect()
        {
            return new Rect(
                12,
                12,
                Math.Max(1, ActualWidth - 24),
                Math.Max(1, ActualHeight - 42));
        }

        private IList<Point> CreatePoints(IList<PeriodActivityViewModel> items)
        {
            var plot = GetPlotRect();
            var maximum = items.Count == 0 ? 0UL : items.Max(item => item.Seconds);
            var points = new List<Point>(items.Count);
            for (var index = 0; index < items.Count; index++)
            {
                var x = items.Count == 1
                    ? plot.Left + plot.Width / 2
                    : plot.Left + plot.Width * index / (items.Count - 1);
                var y = maximum == 0
                    ? plot.Bottom
                    : plot.Bottom - plot.Height * items[index].Seconds / maximum;
                points.Add(new Point(x, y));
            }
            return points;
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
            var previousRight = double.NegativeInfinity;
            foreach (var index in Enumerable.Range(0, renderedItems.Count)
                .Where(index =>
                    index != lastIndex &&
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
                context.DrawText(text, new Point(x, plot.Bottom + 7));
                previousRight = x + text.Width;
            }

            if (lastLeft >= previousRight + 8)
            {
                context.DrawText(
                    lastText,
                    new Point(lastLeft, plot.Bottom + 7));
            }
        }

        private void DrawHover(DrawingContext context, Rect plot, Brush textBrush)
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
            var controlBackground = ResolveBrush("ControlBackgroundBrush",
                Colors.Black);
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
            context.DrawEllipse(
                glyph,
                new Pen(controlBackground, 1),
                point,
                4.5,
                4.5);

            var item = renderedItems[hoverIndex];
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
