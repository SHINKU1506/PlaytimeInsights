using System;
using System.Windows;

namespace PlaytimeInsights.ViewModels
{
    public enum HeatmapIntensityLevel
    {
        None,
        Low,
        Medium,
        High
    }

    public sealed class HeatmapCellViewModel
    {
        public DateTime Date { get; set; }

        public ulong Seconds { get; set; }

        public HeatmapIntensityLevel IntensityLevel { get; set; }

        public Visibility CellVisibility { get; set; }

        public string TooltipText { get; set; }
    }

    public sealed class WeekHourCellViewModel
    {
        public string DayLabel { get; set; }

        public string HourLabel { get; set; }

        public ulong Seconds { get; set; }

        public double HeatOpacity { get; set; }

        public string TooltipText { get; set; }
    }
}
