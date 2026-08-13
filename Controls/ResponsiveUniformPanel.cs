using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Controls
{
    public sealed class ResponsiveUniformPanel : Panel
    {
        public static readonly DependencyProperty MinItemWidthProperty =
            DependencyProperty.Register(
                nameof(MinItemWidth),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    204d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double MinItemWidth
        {
            get => (double)GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        public static readonly DependencyProperty PreferredItemWidthProperty =
            DependencyProperty.Register(
                nameof(PreferredItemWidth),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    232d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double PreferredItemWidth
        {
            get => (double)GetValue(PreferredItemWidthProperty);
            set => SetValue(PreferredItemWidthProperty, value);
        }

        public static readonly DependencyProperty MaxItemWidthProperty =
            DependencyProperty.Register(
                nameof(MaxItemWidth),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    300d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double MaxItemWidth
        {
            get => (double)GetValue(MaxItemWidthProperty);
            set => SetValue(MaxItemWidthProperty, value);
        }

        public static readonly DependencyProperty MinColumnsProperty =
            DependencyProperty.Register(
                nameof(MinColumns),
                typeof(int),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public int MinColumns
        {
            get => (int)GetValue(MinColumnsProperty);
            set => SetValue(MinColumnsProperty, value);
        }

        public static readonly DependencyProperty MaxColumnsProperty =
            DependencyProperty.Register(
                nameof(MaxColumns),
                typeof(int),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    4,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public int MaxColumns
        {
            get => (int)GetValue(MaxColumnsProperty);
            set => SetValue(MaxColumnsProperty, value);
        }

        public static readonly DependencyProperty HorizontalSpacingProperty =
            DependencyProperty.Register(
                nameof(HorizontalSpacing),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    12d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double HorizontalSpacing
        {
            get => (double)GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        public static readonly DependencyProperty VerticalSpacingProperty =
            DependencyProperty.Register(
                nameof(VerticalSpacing),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    12d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double VerticalSpacing
        {
            get => (double)GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }

        public static readonly DependencyProperty CenterIncompleteRowProperty =
            DependencyProperty.Register(
                nameof(CenterIncompleteRow),
                typeof(bool),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public bool CenterIncompleteRow
        {
            get => (bool)GetValue(CenterIncompleteRowProperty);
            set => SetValue(CenterIncompleteRowProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var visibleChildren = GetVisibleChildren(InternalChildren);
            if (visibleChildren.Count == 0)
            {
                return new Size(0, 0);
            }

            var settings = GetSettings();
            var layoutWidth = ResolveLayoutWidth(
                availableSize.Width,
                visibleChildren.Count,
                settings);
            var columns = SelectColumnCount(
                layoutWidth,
                visibleChildren.Count,
                settings);
            var itemWidth = CalculateItemWidth(layoutWidth, columns, settings);

            foreach (var child in visibleChildren)
            {
                child.Measure(new Size(itemWidth, double.PositiveInfinity));
            }

            var rowHeights = GetRowHeights(visibleChildren, columns);
            return new Size(
                CalculateGridWidth(itemWidth, columns, settings),
                CalculateGridHeight(rowHeights, settings.VerticalSpacing));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                }
            }

            var visibleChildren = GetVisibleChildren(InternalChildren);
            if (visibleChildren.Count == 0)
            {
                return finalSize;
            }

            var settings = GetSettings();
            var layoutWidth = IsFinite(finalSize.Width)
                ? Math.Max(0, finalSize.Width)
                : ResolveLayoutWidth(
                    double.PositiveInfinity,
                    visibleChildren.Count,
                    settings);
            var columns = SelectColumnCount(
                layoutWidth,
                visibleChildren.Count,
                settings);
            var itemWidth = CalculateItemWidth(layoutWidth, columns, settings);
            var rowHeights = GetRowHeights(visibleChildren, columns);
            var gridWidth = CalculateGridWidth(itemWidth, columns, settings);
            var gridStart = Math.Max(0, (layoutWidth - gridWidth) / 2);
            var childIndex = 0;
            var y = 0d;

            for (var rowIndex = 0;
                rowIndex < rowHeights.Count;
                rowIndex++)
            {
                var itemsInRow = Math.Min(
                    columns,
                    visibleChildren.Count - childIndex);
                var rowWidth = CalculateGridWidth(
                    itemWidth,
                    itemsInRow,
                    settings);
                var rowStart = settings.CenterIncompleteRow && itemsInRow < columns
                    ? Math.Max(0, (layoutWidth - rowWidth) / 2)
                    : gridStart;

                for (var column = 0; column < itemsInRow; column++)
                {
                    visibleChildren[childIndex].Arrange(new Rect(
                        rowStart + (column * (itemWidth + settings.HorizontalSpacing)),
                        y,
                        itemWidth,
                        rowHeights[rowIndex]));
                    childIndex++;
                }

                y += rowHeights[rowIndex];
                if (rowIndex < rowHeights.Count - 1)
                {
                    y += settings.VerticalSpacing;
                }
            }

            return finalSize;
        }

        private int SelectColumnCount(
            double availableWidth,
            int visibleChildCount,
            LayoutSettings settings)
        {
            var upperBound = Math.Min(settings.MaxColumns, visibleChildCount);
            var lowerBound = Math.Min(settings.MinColumns, upperBound);
            var preferredColumns = (int)Math.Floor(
                (availableWidth + settings.HorizontalSpacing) /
                (settings.PreferredItemWidth + settings.HorizontalSpacing));
            var columns = Math.Max(
                lowerBound,
                Math.Min(upperBound, preferredColumns));

            while (columns > 1 &&
                CalculateItemWidth(availableWidth, columns, settings) <
                    settings.MinItemWidth)
            {
                columns--;
            }

            return Math.Max(1, columns);
        }

        private static double CalculateItemWidth(
            double availableWidth,
            int columns,
            LayoutSettings settings)
        {
            var spacingWidth = (columns - 1) * settings.HorizontalSpacing;
            var width = Math.Max(0, (availableWidth - spacingWidth) / columns);
            return Math.Min(settings.MaxItemWidth, width);
        }

        private static List<UIElement> GetVisibleChildren(
            UIElementCollection children)
        {
            var visible = new List<UIElement>();
            foreach (UIElement child in children)
            {
                if (child.Visibility != Visibility.Collapsed)
                {
                    visible.Add(child);
                }
            }

            return visible;
        }

        private static double ResolveLayoutWidth(
            double availableWidth,
            int visibleChildCount,
            LayoutSettings settings)
        {
            if (IsFinite(availableWidth))
            {
                return Math.Max(0, availableWidth);
            }

            var columns = Math.Min(
                settings.MaxColumns,
                Math.Max(1, visibleChildCount));
            return (columns * settings.PreferredItemWidth) +
                ((columns - 1) * settings.HorizontalSpacing);
        }

        private static double CalculateGridWidth(
            double itemWidth,
            int columns,
            LayoutSettings settings)
        {
            if (columns <= 0)
            {
                return 0;
            }

            return (columns * itemWidth) +
                ((columns - 1) * settings.HorizontalSpacing);
        }

        private static List<double> GetRowHeights(
            IList<UIElement> children,
            int columns)
        {
            var heights = new List<double>();
            for (var start = 0; start < children.Count; start += columns)
            {
                var height = 0d;
                var end = Math.Min(start + columns, children.Count);
                for (var index = start; index < end; index++)
                {
                    var desiredHeight = children[index].DesiredSize.Height;
                    if (IsFinite(desiredHeight))
                    {
                        height = Math.Max(height, Math.Max(0, desiredHeight));
                    }
                }

                heights.Add(height);
            }

            return heights;
        }

        private static double CalculateGridHeight(
            IList<double> rowHeights,
            double verticalSpacing)
        {
            var height = 0d;
            for (var index = 0; index < rowHeights.Count; index++)
            {
                height += rowHeights[index];
                if (index < rowHeights.Count - 1)
                {
                    height += verticalSpacing;
                }
            }

            return height;
        }

        private LayoutSettings GetSettings()
        {
            var minWidth = NormalizePositive(MinItemWidth, 204d);
            var preferredWidth = Math.Max(
                minWidth,
                NormalizePositive(PreferredItemWidth, 232d));
            var maxWidth = Math.Max(
                preferredWidth,
                NormalizePositive(MaxItemWidth, 300d));
            var minColumns = Math.Max(1, MinColumns);
            var maxColumns = Math.Max(minColumns, MaxColumns);

            return new LayoutSettings
            {
                MinItemWidth = minWidth,
                PreferredItemWidth = preferredWidth,
                MaxItemWidth = maxWidth,
                MinColumns = minColumns,
                MaxColumns = maxColumns,
                HorizontalSpacing = NormalizeSpacing(HorizontalSpacing, 12d),
                VerticalSpacing = NormalizeSpacing(VerticalSpacing, 12d),
                CenterIncompleteRow = CenterIncompleteRow
            };
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return IsFinite(value) && value > 0 ? value : fallback;
        }

        private static double NormalizeSpacing(double value, double fallback)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Math.Max(0, value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class LayoutSettings
        {
            public double MinItemWidth { get; set; }
            public double PreferredItemWidth { get; set; }
            public double MaxItemWidth { get; set; }
            public int MinColumns { get; set; }
            public int MaxColumns { get; set; }
            public double HorizontalSpacing { get; set; }
            public double VerticalSpacing { get; set; }
            public bool CenterIncompleteRow { get; set; }
        }
    }
}
