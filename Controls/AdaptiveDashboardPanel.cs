using System;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Controls
{
    public enum DashboardLayoutZone
    {
        Primary,
        Secondary
    }

    public sealed class AdaptiveDashboardPanel : Panel
    {
        public static readonly DependencyProperty ZoneProperty =
            DependencyProperty.RegisterAttached(
                "Zone",
                typeof(DashboardLayoutZone),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    DashboardLayoutZone.Primary,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static readonly DependencyProperty EnterWideWidthProperty =
            DependencyProperty.Register(
                nameof(EnterWideWidth),
                typeof(double),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    1200d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ExitWideWidthProperty =
            DependencyProperty.Register(
                nameof(ExitWideWidth),
                typeof(double),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    1160d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty SecondaryColumnRatioProperty =
            DependencyProperty.Register(
                nameof(SecondaryColumnRatio),
                typeof(double),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    0.38d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ColumnSpacingProperty =
            DependencyProperty.Register(
                nameof(ColumnSpacing),
                typeof(double),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    18d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty VerticalSpacingProperty =
            DependencyProperty.Register(
                nameof(VerticalSpacing),
                typeof(double),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    18d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static void SetZone(
            DependencyObject element,
            DashboardLayoutZone value)
        {
            element.SetValue(ZoneProperty, value);
        }

        public static DashboardLayoutZone GetZone(
            DependencyObject element)
        {
            return (DashboardLayoutZone)element.GetValue(ZoneProperty);
        }

        public double EnterWideWidth
        {
            get => (double)GetValue(EnterWideWidthProperty);
            set => SetValue(EnterWideWidthProperty, value);
        }

        public double ExitWideWidth
        {
            get => (double)GetValue(ExitWideWidthProperty);
            set => SetValue(ExitWideWidthProperty, value);
        }

        public double SecondaryColumnRatio
        {
            get => (double)GetValue(SecondaryColumnRatioProperty);
            set => SetValue(SecondaryColumnRatioProperty, value);
        }

        public double ColumnSpacing
        {
            get => (double)GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        public double VerticalSpacing
        {
            get => (double)GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }

        public bool IsWideLayout { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateLayoutMode(availableSize.Width);
            var childWidth = IsFinite(availableSize.Width)
                ? Math.Max(0d, availableSize.Width)
                : 0d;
            var verticalSpacing = NormalizeNonNegative(VerticalSpacing);

            if (!IsWideLayout)
            {
                var totalHeight = 0d;
                var visibleCount = 0;
                foreach (UIElement child in InternalChildren)
                {
                    if (child.Visibility == Visibility.Collapsed)
                    {
                        continue;
                    }

                    child.Measure(new Size(
                        childWidth,
                        double.PositiveInfinity));
                    if (visibleCount > 0)
                    {
                        totalHeight += verticalSpacing;
                    }

                    totalHeight += child.DesiredSize.Height;
                    visibleCount++;
                }

                return new Size(childWidth, totalHeight);
            }

            var columnSpacing = NormalizeNonNegative(ColumnSpacing);
            var usableWidth = Math.Max(0d, childWidth - columnSpacing);
            var secondaryWidth = usableWidth * ResolveSecondaryRatio();
            var primaryWidth = usableWidth - secondaryWidth;
            var primaryHeight = 0d;
            var secondaryHeight = 0d;
            var primaryCount = 0;
            var secondaryCount = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (GetZone(child) == DashboardLayoutZone.Primary)
                {
                    child.Measure(new Size(
                        primaryWidth,
                        double.PositiveInfinity));
                    if (primaryCount > 0)
                    {
                        primaryHeight += verticalSpacing;
                    }

                    primaryHeight += child.DesiredSize.Height;
                    primaryCount++;
                }
                else
                {
                    child.Measure(new Size(
                        secondaryWidth,
                        double.PositiveInfinity));
                    if (secondaryCount > 0)
                    {
                        secondaryHeight += verticalSpacing;
                    }

                    secondaryHeight += child.DesiredSize.Height;
                    secondaryCount++;
                }
            }

            return new Size(
                childWidth,
                Math.Max(primaryHeight, secondaryHeight));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var childWidth = IsFinite(finalSize.Width)
                ? Math.Max(0d, finalSize.Width)
                : 0d;
            var verticalSpacing = NormalizeNonNegative(VerticalSpacing);

            if (!IsWideLayout)
            {
                var y = 0d;
                foreach (UIElement child in InternalChildren)
                {
                    if (child.Visibility == Visibility.Collapsed)
                    {
                        child.Arrange(new Rect(0, 0, 0, 0));
                        continue;
                    }

                    var height = child.DesiredSize.Height;
                    child.Arrange(new Rect(0, y, childWidth, height));
                    y += height + verticalSpacing;
                }

                return finalSize;
            }

            var columnSpacing = NormalizeNonNegative(ColumnSpacing);
            var usableWidth = Math.Max(0d, childWidth - columnSpacing);
            var secondaryWidth = usableWidth * ResolveSecondaryRatio();
            var primaryWidth = usableWidth - secondaryWidth;
            var secondaryX = primaryWidth + columnSpacing;
            var primaryY = 0d;
            var secondaryY = 0d;

            foreach (UIElement child in InternalChildren)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                var height = child.DesiredSize.Height;
                if (GetZone(child) == DashboardLayoutZone.Primary)
                {
                    child.Arrange(new Rect(
                        0,
                        primaryY,
                        primaryWidth,
                        height));
                    primaryY += height + verticalSpacing;
                }
                else
                {
                    child.Arrange(new Rect(
                        secondaryX,
                        secondaryY,
                        secondaryWidth,
                        height));
                    secondaryY += height + verticalSpacing;
                }
            }

            return finalSize;
        }

        private void UpdateLayoutMode(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width))
            {
                IsWideLayout = false;
                return;
            }

            var enterWidth = Math.Max(0d, EnterWideWidth);
            var exitWidth = Math.Max(0d, ExitWideWidth);
            if (exitWidth > enterWidth)
            {
                exitWidth = enterWidth;
            }

            IsWideLayout = IsWideLayout
                ? width >= exitWidth
                : width >= enterWidth;
        }

        private double ResolveSecondaryRatio()
        {
            var ratio = SecondaryColumnRatio;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                return 0.38d;
            }

            return Math.Min(0.50d, Math.Max(0.20d, ratio));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double NormalizeNonNegative(double value)
        {
            return IsFinite(value) ? Math.Max(0d, value) : 0d;
        }
    }
}