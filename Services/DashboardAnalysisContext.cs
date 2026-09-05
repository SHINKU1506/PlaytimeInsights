using PlaytimeInsights.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace PlaytimeInsights.Services
{
    public sealed class DashboardSnapshotResult
    {
        public DashboardSnapshot Snapshot { get; set; }

        public DashboardAnalysisContext Context { get; set; }
    }

    public sealed class DashboardAnalysisContext
    {
        public DateRangePreset RangePreset { get; set; }

        public AnalyticsDateRange Range { get; set; }

        // Local snapshot date from the same clock that resolved the range.
        public DateTime SnapshotDate { get; set; }

        public DayOfWeek FirstDayOfWeek { get; set; }

        public IDictionary<DateTime, ulong> DailySeconds { get; set; }

        public IDictionary<DateTime, IList<string>> DailyGameNames { get; set; }

        public IList<DashboardGameRangeStatistics> GameStatistics { get; set; }

        public DashboardComparisonTotals ComparisonTotals { get; set; }
    }

    public sealed class DashboardComparisonTotals
    {
        public bool Enabled { get; set; }

        public AnalyticsDateRange PreviousRange { get; set; }

        public AnalyticsDateRange YearOverYearRange { get; set; }

        public ulong PreviousSeconds { get; set; }

        public ulong YearOverYearSeconds { get; set; }
    }

    public sealed class DashboardGameRangeStatistics
    {
        public Guid GameId { get; set; }

        public string Name { get; set; }

        public ulong Seconds { get; set; }

        public int SessionCount { get; set; }

        public IList<DateTime> ActiveDates { get; set; }

        public ulong LongestSessionSeconds { get; set; }

        public DateTime? LastSessionLocal { get; set; }

        public ulong AverageSessionSeconds =>
            SessionCount == 0 ? 0UL : Seconds / (ulong)SessionCount;
    }

    public sealed class DashboardTrendProjection
    {
        public string PeriodTitleText { get; set; }

        public DateTime SnapshotDate { get; set; }

        public IList<PeriodActivityViewModel> PeriodActivities { get; set; }

        public double TrendChartWidth { get; set; }

        public PointCollection TrendLinePoints { get; set; }

        public Geometry TrendLineGeometry { get; set; }

        public Geometry TrendAreaGeometry { get; set; }

        public IList<TrendPointViewModel> TrendPoints { get; set; }
    }

    public sealed class DashboardRankingProjection
    {
        public string RangeRankingTitleText { get; set; }

        public IList<GameRankingViewModel> RangeGameRankings { get; set; }
    }
}
