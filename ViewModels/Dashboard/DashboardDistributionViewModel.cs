using PlaytimeInsights.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private int? selectedWeekdayIndex;
        private IList<DistributionBarViewModel> allHourDistribution =
            new List<DistributionBarViewModel>();
        private string hourDistributionTitle = LocalizationService.Get(
            "LOCPlaytimeInsightsHourDistributionAll",
            "24 小时分布 · 全部星期");

        public DashboardDistributionViewModel()
        {
            PeriodActivities = new ObservableCollection<PeriodActivityViewModel>();
            HeatmapCells = new ObservableCollection<HeatmapCellViewModel>();
            HeatmapWeekdayLabels = new ObservableCollection<string>();
            TrendPoints = new ObservableCollection<TrendPointViewModel>();
            WeekdayDistribution = new ObservableCollection<DistributionBarViewModel>();
            HourDistribution = new ObservableCollection<DistributionBarViewModel>();
            WeekHourCells = new ObservableCollection<WeekHourCellViewModel>();
            AdvancedWeekdayLabels = new ObservableCollection<string>();
            AdvancedHourLabels = new ObservableCollection<string>();
            Anomalies = new ObservableCollection<AnomalySessionViewModel>();
        }

        public int HeatmapColumnCount { get => heatmapColumnCount; private set => SetValue(ref heatmapColumnCount, value); }

        public double TrendChartWidth { get => trendChartWidth; private set => SetValue(ref trendChartWidth, value); }

        public PointCollection TrendLinePoints { get => trendLinePoints; private set => SetValue(ref trendLinePoints, value); }

        public Geometry TrendLineGeometry { get => trendLineGeometry; private set => SetValue(ref trendLineGeometry, value); }

        public Geometry TrendAreaGeometry { get => trendAreaGeometry; private set => SetValue(ref trendAreaGeometry, value); }

        public Visibility AnomalyVisibility { get => anomalyVisibility; private set => SetValue(ref anomalyVisibility, value); }

        public string HourDistributionTitle { get => hourDistributionTitle; private set => SetValue(ref hourDistributionTitle, value); }

        public ObservableCollection<PeriodActivityViewModel> PeriodActivities { get; }

        public ObservableCollection<HeatmapCellViewModel> HeatmapCells { get; }

        public ObservableCollection<string> HeatmapWeekdayLabels { get; }

        public ObservableCollection<TrendPointViewModel> TrendPoints { get; }

        public ObservableCollection<DistributionBarViewModel> WeekdayDistribution { get; }

        public ObservableCollection<DistributionBarViewModel> HourDistribution { get; }

        public ObservableCollection<WeekHourCellViewModel> WeekHourCells { get; }

        public ObservableCollection<string> AdvancedWeekdayLabels { get; }

        public ObservableCollection<string> AdvancedHourLabels { get; }

        public ObservableCollection<AnomalySessionViewModel> Anomalies { get; }

        public void Apply(DashboardSnapshot snapshot)
        {
            HeatmapColumnCount = snapshot.HeatmapColumnCount;
            TrendChartWidth = snapshot.TrendChartWidth;
            TrendLinePoints = snapshot.TrendLinePoints;
            TrendLineGeometry = snapshot.TrendLineGeometry;
            TrendAreaGeometry = snapshot.TrendAreaGeometry;
            AnomalyVisibility = snapshot.Advanced.AnomalyVisibility;
            Replace(PeriodActivities, snapshot.PeriodActivities);
            Replace(HeatmapCells, snapshot.HeatmapCells);
            Replace(HeatmapWeekdayLabels, snapshot.HeatmapWeekdayLabels);
            Replace(TrendPoints, snapshot.TrendPoints);

            selectedWeekdayIndex = null;
            foreach (var bar in snapshot.Advanced.WeekdayDistribution)
            {
                bar.IsSelected = false;
                bar.AutomationName = LocalizationService.Format(
                    "LOCPlaytimeInsightsWeekdayFilterAutomationFormat",
                    "按 {0} 筛选 24 小时分布；再次选择可恢复全部星期",
                    bar.Label);
            }

            allHourDistribution = snapshot.Advanced.HourDistribution.ToList();
            Replace(WeekdayDistribution, snapshot.Advanced.WeekdayDistribution);
            Replace(HourDistribution, allHourDistribution);
            Replace(WeekHourCells, snapshot.Advanced.WeekHourCells);
            Replace(AdvancedWeekdayLabels, snapshot.Advanced.WeekdayLabels);
            Replace(AdvancedHourLabels, snapshot.Advanced.HourLabels);
            Replace(Anomalies, snapshot.Advanced.Anomalies);
            HourDistributionTitle = LocalizationService.Get(
                "LOCPlaytimeInsightsHourDistributionAll",
                "24 小时分布 · 全部星期");
        }

        public bool ContainsWeekday(DistributionBarViewModel bar)
        {
            return bar != null && WeekdayDistribution.Contains(bar);
        }

        public void SelectWeekday(DistributionBarViewModel bar)
        {
            var index = WeekdayDistribution.IndexOf(bar);
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
                Replace(HourDistribution, allHourDistribution);
                HourDistributionTitle = LocalizationService.Get(
                    "LOCPlaytimeInsightsHourDistributionAll",
                    "24 小时分布 · 全部星期");
                return;
            }

            var selectedBar = WeekdayDistribution[selectedWeekdayIndex.Value];
            Replace(
                HourDistribution,
                AdvancedAnalyticsService.CreateHourDistributionForWeekday(
                    WeekHourCells,
                    selectedWeekdayIndex.Value));
            HourDistributionTitle = LocalizationService.Format(
                "LOCPlaytimeInsightsHourDistributionSelectedFormat",
                "24 小时分布 · {0}",
                selectedBar.Label);
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
        {
            target.Clear();
            foreach (var value in values)
            {
                target.Add(value);
            }
        }
    }
}
