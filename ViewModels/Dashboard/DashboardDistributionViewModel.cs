using PlaytimeInsights.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlaytimeInsights.ViewModels
{
    public sealed class DashboardDistributionViewModel : ObservableObject
    {
        private int heatmapColumnCount = 1;
        private double trendChartWidth = 640;
        private PointCollection trendLinePoints = new PointCollection();
        private Geometry trendLineGeometry = Geometry.Empty;
        private Geometry trendAreaGeometry = Geometry.Empty;
        private Visibility anomalyVisibility = Visibility.Collapsed;
        private IReadOnlyList<PeriodActivityViewModel> periodActivities =
            new List<PeriodActivityViewModel>();
        private IReadOnlyList<HeatmapCellViewModel> heatmapCells =
            new List<HeatmapCellViewModel>();
        private IReadOnlyList<string> heatmapWeekdayLabels = new List<string>();
        private IReadOnlyList<TrendPointViewModel> trendPoints =
            new List<TrendPointViewModel>();
        private IReadOnlyList<DistributionBarViewModel> weekdayDistribution =
            new List<DistributionBarViewModel>();
        private IReadOnlyList<DistributionBarViewModel> hourDistribution =
            new List<DistributionBarViewModel>();
        private IReadOnlyList<WeekHourCellViewModel> weekHourCells =
            new List<WeekHourCellViewModel>();
        private IReadOnlyList<string> advancedWeekdayLabels = new List<string>();
        private IReadOnlyList<string> advancedHourLabels = new List<string>();
        private IReadOnlyList<AnomalySessionViewModel> anomalies =
            new List<AnomalySessionViewModel>();
        private int? selectedWeekdayIndex;
        private IReadOnlyList<DistributionBarViewModel> allHourDistribution =
            new List<DistributionBarViewModel>();
        private string hourDistributionTitle = LocalizationService.Get(
            "LOCPlaytimeInsightsHourDistributionAll",
            "24 小时分布 · 全部星期");
        private string peakPeriodText = LocalizationService.Get(
            "LOCPlaytimeInsightsNoPeakPeriod",
            "暂无数据");
        private string peakPeriodShareText;

        public int HeatmapColumnCount { get => heatmapColumnCount; private set => SetValue(ref heatmapColumnCount, value); }

        public double TrendChartWidth { get => trendChartWidth; private set => SetValue(ref trendChartWidth, value); }

        public PointCollection TrendLinePoints { get => trendLinePoints; private set => SetValue(ref trendLinePoints, value); }

        public Geometry TrendLineGeometry { get => trendLineGeometry; private set => SetValue(ref trendLineGeometry, value); }

        public Geometry TrendAreaGeometry { get => trendAreaGeometry; private set => SetValue(ref trendAreaGeometry, value); }

        public Visibility AnomalyVisibility { get => anomalyVisibility; private set => SetValue(ref anomalyVisibility, value); }

        public string HourDistributionTitle { get => hourDistributionTitle; private set => SetValue(ref hourDistributionTitle, value); }

        public string PeakPeriodText { get => peakPeriodText; private set => SetValue(ref peakPeriodText, value); }

        public string PeakPeriodShareText { get => peakPeriodShareText; private set => SetValue(ref peakPeriodShareText, value); }

        public IReadOnlyList<PeriodActivityViewModel> PeriodActivities
        {
            get => periodActivities;
            private set => SetValue(ref periodActivities, value);
        }

        public IReadOnlyList<HeatmapCellViewModel> HeatmapCells
        {
            get => heatmapCells;
            private set => SetValue(ref heatmapCells, value);
        }

        public IReadOnlyList<string> HeatmapWeekdayLabels
        {
            get => heatmapWeekdayLabels;
            private set => SetValue(ref heatmapWeekdayLabels, value);
        }

        public IReadOnlyList<TrendPointViewModel> TrendPoints
        {
            get => trendPoints;
            private set => SetValue(ref trendPoints, value);
        }

        public IReadOnlyList<DistributionBarViewModel> WeekdayDistribution
        {
            get => weekdayDistribution;
            private set => SetValue(ref weekdayDistribution, value);
        }

        public IReadOnlyList<DistributionBarViewModel> HourDistribution
        {
            get => hourDistribution;
            private set => SetValue(ref hourDistribution, value);
        }

        public IReadOnlyList<WeekHourCellViewModel> WeekHourCells
        {
            get => weekHourCells;
            private set => SetValue(ref weekHourCells, value);
        }

        public IReadOnlyList<string> AdvancedWeekdayLabels
        {
            get => advancedWeekdayLabels;
            private set => SetValue(ref advancedWeekdayLabels, value);
        }

        public IReadOnlyList<string> AdvancedHourLabels
        {
            get => advancedHourLabels;
            private set => SetValue(ref advancedHourLabels, value);
        }

        public IReadOnlyList<AnomalySessionViewModel> Anomalies
        {
            get => anomalies;
            private set => SetValue(ref anomalies, value);
        }

        public void Apply(DashboardSnapshot snapshot)
        {
            HeatmapColumnCount = snapshot.HeatmapColumnCount;
            AnomalyVisibility = snapshot.Advanced.AnomalyVisibility;
            ApplyTrend(new DashboardTrendProjection
            {
                PeriodActivities = snapshot.PeriodActivities,
                TrendChartWidth = snapshot.TrendChartWidth,
                TrendLinePoints = snapshot.TrendLinePoints,
                TrendLineGeometry = snapshot.TrendLineGeometry,
                TrendAreaGeometry = snapshot.TrendAreaGeometry,
                TrendPoints = snapshot.TrendPoints
            });
            HeatmapCells = Copy(snapshot.HeatmapCells);
            HeatmapWeekdayLabels = Copy(snapshot.HeatmapWeekdayLabels);

            selectedWeekdayIndex = null;
            foreach (var bar in snapshot.Advanced.WeekdayDistribution)
            {
                bar.IsSelected = false;
                bar.AutomationName = LocalizationService.Format(
                    "LOCPlaytimeInsightsWeekdayFilterAutomationFormat",
                    "按 {0} 筛选 24 小时分布；再次选择可恢复全部星期",
                    bar.Label);
            }

            allHourDistribution = Copy(snapshot.Advanced.HourDistribution);
            WeekdayDistribution = Copy(snapshot.Advanced.WeekdayDistribution);
            HourDistribution = allHourDistribution.ToList();
            WeekHourCells = Copy(snapshot.Advanced.WeekHourCells);
            UpdatePeakPeriod();
            AdvancedWeekdayLabels = Copy(snapshot.Advanced.WeekdayLabels);
            AdvancedHourLabels = Copy(snapshot.Advanced.HourLabels);
            Anomalies = Copy(snapshot.Advanced.Anomalies);
            HourDistributionTitle = LocalizationService.Get(
                "LOCPlaytimeInsightsHourDistributionAll",
                "24 小时分布 · 全部星期");
        }

        public void ApplyTrend(DashboardTrendProjection projection)
        {
            TrendChartWidth = projection.TrendChartWidth;
            TrendLinePoints = projection.TrendLinePoints;
            TrendLineGeometry = projection.TrendLineGeometry;
            TrendAreaGeometry = projection.TrendAreaGeometry;
            PeriodActivities = Copy(projection.PeriodActivities);
            TrendPoints = Copy(projection.TrendPoints);
        }

        public bool ContainsWeekday(DistributionBarViewModel bar)
        {
            return bar != null && WeekdayDistribution.Contains(bar);
        }

        public void SelectWeekday(DistributionBarViewModel bar)
        {
            var index = FindIndex(WeekdayDistribution, bar);
            if (index < 0)
            {
                return;
            }

            selectedWeekdayIndex = selectedWeekdayIndex == index ? (int?)null : index;
            for (var day = 0; day < WeekdayDistribution.Count; day++)
            {
                WeekdayDistribution[day].IsSelected = selectedWeekdayIndex == day;
            }

            if (!selectedWeekdayIndex.HasValue)
            {
                HourDistribution = allHourDistribution.ToList();
                HourDistributionTitle = LocalizationService.Get(
                    "LOCPlaytimeInsightsHourDistributionAll",
                    "24 小时分布 · 全部星期");
                return;
            }

            var selectedBar = WeekdayDistribution[selectedWeekdayIndex.Value];
            HourDistribution = AdvancedAnalyticsService
                .CreateHourDistributionForWeekday(
                    WeekHourCells.ToList(),
                    selectedWeekdayIndex.Value)
                .ToList();
            HourDistributionTitle = LocalizationService.Format(
                "LOCPlaytimeInsightsHourDistributionSelectedFormat",
                "24 小时分布 · {0}",
                selectedBar.Label);
        }

        private void UpdatePeakPeriod()
        {
            var cells = WeekHourCells ?? new List<WeekHourCellViewModel>();
            var peak = cells.OrderByDescending(cell => cell.Seconds)
                .FirstOrDefault();
            if (peak == null || peak.Seconds == 0)
            {
                PeakPeriodText = LocalizationService.Get(
                    "LOCPlaytimeInsightsNoPeakPeriod",
                    "暂无数据");
                PeakPeriodShareText = string.Empty;
                return;
            }

            var totalSeconds = cells.Aggregate(
                0UL,
                (total, cell) => total + cell.Seconds);
            PeakPeriodText = string.Join(
                " ",
                new[]
                {
                    peak.DayLabel,
                    peak.HourLabel
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
            PeakPeriodShareText = LocalizationService.Format(
                "LOCPlaytimeInsightsPeakPeriodShareFormat",
                "占区间 {0:P0}",
                totalSeconds == 0
                    ? 0d
                    : (double)peak.Seconds / totalSeconds);
        }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
        {
            return (values ?? Enumerable.Empty<T>()).ToList();
        }

        private static int FindIndex<T>(IReadOnlyList<T> values, T value)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (EqualityComparer<T>.Default.Equals(values[index], value))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
