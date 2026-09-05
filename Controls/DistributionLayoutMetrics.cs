using System;

namespace PlaytimeInsights.Controls
{
    // Pure layout math for the distribution charts (V5). Create() sees only the
    // finite viewport width in DIP measured outside the scrollable content - no
    // dispatcher, no theme, no session data. The view applies the result to the
    // charts after layout so a resize never touches analytics.
    public sealed class DistributionLayoutMetrics
    {
        private const double InvalidViewportFallback = 480d;
        private const double WeekLabelWidth = 48d;
        private const double AlignmentEpsilon = 0.5d;

        private DistributionLayoutMetrics()
        {
        }

        public double ViewportWidth { get; private set; }

        // 24-hour bar chart.
        public double HourSlotWidth { get; private set; }

        public double HourContentWidth { get; private set; }

        public double HourBarWidth { get; private set; }

        public int HourLabelStep { get; private set; }

        // Weekday x hour heatmap.
        public double WeekHourSlotWidth { get; private set; }

        public double WeekHourCellSize { get; private set; }

        public double WeekHourContentWidth { get; private set; }

        public int WeekHourLabelStep { get; private set; }

        // Centering is only safe while the content is narrower than the
        // viewport; overflowing centered content would clip its leading edge.
        public bool HourCentersContent =>
            HourContentWidth < ViewportWidth - AlignmentEpsilon;

        public bool WeekHourCentersContent =>
            WeekHourContentWidth < ViewportWidth - AlignmentEpsilon;

        public static DistributionLayoutMetrics Create(double viewportWidth)
        {
            if (double.IsNaN(viewportWidth) ||
                double.IsInfinity(viewportWidth) ||
                viewportWidth <= 0d)
            {
                // Wait for the first valid size; use the smallest width that
                // still shows every hour without scrolling.
                viewportWidth = InvalidViewportFallback;
            }

            var hourSlotWidth = Clamp(viewportWidth / 24d, 20d, 44d);
            var weekHourSlotWidth = Clamp(
                (viewportWidth - WeekLabelWidth) / 24d,
                26d,
                32d);

            return new DistributionLayoutMetrics
            {
                ViewportWidth = viewportWidth,
                HourSlotWidth = hourSlotWidth,
                HourContentWidth = hourSlotWidth * 24d,
                HourBarWidth = Math.Min(24d, hourSlotWidth - 8d),
                HourLabelStep = hourSlotWidth >= 40d ? 1 : 2,
                WeekHourSlotWidth = weekHourSlotWidth,
                WeekHourCellSize = weekHourSlotWidth - 2d,
                WeekHourContentWidth = WeekLabelWidth + 24d * weekHourSlotWidth,
                WeekHourLabelStep = 2
            };
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
