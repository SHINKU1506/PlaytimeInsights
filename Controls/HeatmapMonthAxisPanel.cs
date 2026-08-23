using System;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Controls
{
    public sealed class HeatmapMonthAxisPanel : Panel
    {
        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.Register(
                nameof(ColumnCount),
                typeof(int),
                typeof(HeatmapMonthAxisPanel),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ColumnPitchProperty =
            DependencyProperty.Register(
                nameof(ColumnPitch),
                typeof(double),
                typeof(HeatmapMonthAxisPanel),
                new FrameworkPropertyMetadata(
                    26d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ColumnIndexProperty =
            DependencyProperty.RegisterAttached(
                "ColumnIndex",
                typeof(int),
                typeof(HeatmapMonthAxisPanel),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty ColumnSpanProperty =
            DependencyProperty.RegisterAttached(
                "ColumnSpan",
                typeof(int),
                typeof(HeatmapMonthAxisPanel),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public int ColumnCount
        {
            get => (int)GetValue(ColumnCountProperty);
            set => SetValue(ColumnCountProperty, value);
        }

        public double ColumnPitch
        {
            get => (double)GetValue(ColumnPitchProperty);
            set => SetValue(ColumnPitchProperty, value);
        }

        public static int GetColumnIndex(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (int)element.GetValue(ColumnIndexProperty);
        }

        public static void SetColumnIndex(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(ColumnIndexProperty, value);
        }

        public static int GetColumnSpan(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (int)element.GetValue(ColumnSpanProperty);
        }

        public static void SetColumnSpan(DependencyObject element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(ColumnSpanProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var pitch = Math.Max(0d, ColumnPitch);
            var columnCount = Math.Max(1, ColumnCount);
            var totalWidth = columnCount * pitch;
            var maxChildHeight = 0d;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                var colIndex = Math.Max(0, Math.Min(GetColumnIndex(child), columnCount - 1));
                var colSpan = Math.Max(1, Math.Min(GetColumnSpan(child), columnCount - colIndex));
                var childWidth = colSpan * pitch;
                child.Measure(new Size(childWidth, availableSize.Height));
                maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
            }

            var desiredHeight = double.IsPositiveInfinity(availableSize.Height)
                ? maxChildHeight
                : Math.Min(availableSize.Height, maxChildHeight);

            return new Size(totalWidth, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var pitch = Math.Max(0d, ColumnPitch);
            var columnCount = Math.Max(1, ColumnCount);

            foreach (UIElement child in InternalChildren)
            {
                if (child == null)
                {
                    continue;
                }

                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                var colIndex = Math.Max(0, Math.Min(GetColumnIndex(child), columnCount - 1));
                var colSpan = Math.Max(1, Math.Min(GetColumnSpan(child), columnCount - colIndex));
                var x = colIndex * pitch;
                var width = colSpan * pitch;

                child.Arrange(new Rect(x, 0, width, finalSize.Height));
            }

            return finalSize;
        }
    }
}
