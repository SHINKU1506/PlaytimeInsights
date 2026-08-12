using System;

namespace PlaytimeInsights.ViewModels
{
    public sealed class PeriodActivityViewModel
    {
        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public string Label { get; set; }

        public string DurationText { get; set; }

        public string HoverDurationText { get; set; }

        public string TooltipText { get; set; }

        public string GameSummaryText { get; set; }

        public ulong Seconds { get; set; }

        public double BarHeight { get; set; }
    }

    public sealed class TrendPointViewModel
    {
        public PeriodActivityViewModel Period { get; set; }

        public double CanvasLeft { get; set; }

        public double CanvasTop { get; set; }

        public string TooltipText { get; set; }
    }
}
