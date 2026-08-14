using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PlaytimeInsights.ViewModels
{
    public sealed class ComparisonMetricViewModel
    {
        public string Title { get; set; }

        public string CurrentText { get; set; }

        public string PreviousText { get; set; }

        public string DeltaText { get; set; }

        public string TagText { get; set; }

        public string TrendKind { get; set; }

        public string TooltipText { get; set; }
    }

    public sealed class AnomalySessionViewModel
    {
        public string GameName { get; set; }

        public string StartedText { get; set; }

        public string DurationText { get; set; }

        public string Reason { get; set; }
    }

    public sealed class AdvancedAnalyticsSnapshot
    {
        public IList<DistributionBarViewModel> WeekdayDistribution { get; set; }

        public IList<DistributionBarViewModel> HourDistribution { get; set; }

        public IList<WeekHourCellViewModel> WeekHourCells { get; set; }

        public IList<string> WeekdayLabels { get; set; }

        public IList<string> HourLabels { get; set; }

        public Visibility ComparisonVisibility { get; set; }

        public ComparisonMetricViewModel PreviousPeriodComparison { get; set; }

        public ComparisonMetricViewModel YearOverYearComparison { get; set; }

        public string LongestStreakText { get; set; }

        public string CurrentStreakText { get; set; }

        public string CurrentStreakDateText { get; set; }

        public string AnomalyCountText { get; set; }

        public Visibility AnomalyVisibility { get; set; }

        public IList<AnomalySessionViewModel> Anomalies { get; set; }
    }

    public sealed class DashboardSnapshot
    {
        public string LifetimeDurationText { get; set; }

        public DurationDisplayViewModel LifetimeDurationDisplay { get; set; }

        public string TrackedDurationText { get; set; }

        public string RangeDurationText { get; set; }

        public DurationDisplayViewModel RangeDurationDisplay { get; set; }

        public string SessionCountText { get; set; }

        public string ActiveDaysText { get; set; }

        public string AverageSessionText { get; set; }

        public DurationDisplayViewModel AverageSessionDisplay { get; set; }

        public string LongestSessionText { get; set; }

        public DurationDisplayViewModel LongestSessionDisplay { get; set; }

        public string RangeText { get; set; }

        public string PeriodTitleText { get; set; }

        public string RangeRankingTitleText { get; set; }

        public string StatusText { get; set; }

        public IList<PeriodActivityViewModel> PeriodActivities { get; set; }

        public IList<HeatmapCellViewModel> HeatmapCells { get; set; }

        public IList<string> HeatmapWeekdayLabels { get; set; }

        public int HeatmapColumnCount { get; set; }

        public PointCollection TrendLinePoints { get; set; }

        public Geometry TrendLineGeometry { get; set; }

        public Geometry TrendAreaGeometry { get; set; }

        public IList<TrendPointViewModel> TrendPoints { get; set; }

        public double TrendChartWidth { get; set; }

        public IList<GameRankingViewModel> RangeGameRankings { get; set; }

        public IList<GameRankingViewModel> LifetimeGameRankings { get; set; }

        public AdvancedAnalyticsSnapshot Advanced { get; set; }
    }
}
