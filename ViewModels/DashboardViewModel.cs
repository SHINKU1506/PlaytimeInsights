using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlaytimeInsights.ViewModels
{
    public sealed class DashboardViewModel : ObservableObject
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly SessionRepository sessionRepository;
        private readonly AnalyticsService analyticsService;
        private readonly SessionQueryService queryService;
        private readonly PlaytimeInsightsSettingsViewModel settings;
        private readonly SessionDetailPager sessionDetailPager = new SessionDetailPager(100);
        private readonly RefreshReentrancyGuard refreshGuard =
            new RefreshReentrancyGuard();
        private SelectionOption<DateRangePreset> selectedRangeOption;
        private SelectionOption<AggregationPeriod> selectedAggregationOption;
        private SelectionOption<RankingMetric> selectedRankingMetricOption;
        private SelectionOption<MetadataFilterDimension?> selectedMetadataDimensionOption;
        private SelectionOption<string> selectedMetadataValueOption;
        private bool suppressFilterRefresh;
        private IList<Game> activeFilteredGames = new List<Game>();
        private IList<GameSession> activeFilteredSessions = new List<GameSession>();
        private DateTime customStartDate = DateTime.Today.AddDays(-29);
        private DateTime customEndDate = DateTime.Today;
        private string lifetimeDurationText;
        private string trackedDurationText;
        private string rangeDurationText;
        private string sessionCountText;
        private string activeDaysText;
        private string averageSessionText;
        private string longestSessionText;
        private string rangeText;
        private string periodTitleText;
        private string rangeRankingTitleText;
        private string statusText;
        private string selectedDetailTitle = LocalizationService.Get(
            "LOCPlaytimeInsightsDetailsPrompt",
            "点击柱形、折线点或热力格查看会话");
        private int heatmapColumnCount = 1;
        private double trendChartWidth = 640;
        private PointCollection trendLinePoints = new PointCollection();
        private Geometry trendLineGeometry = Geometry.Empty;
        private Geometry trendAreaGeometry = Geometry.Empty;
        private ComparisonMetricViewModel previousPeriodComparison;
        private ComparisonMetricViewModel yearOverYearComparison;
        private string longestStreakText;
        private string currentStreakText;
        private string currentStreakDateText;
        private string anomalyCountText;
        private Visibility anomalyVisibility = Visibility.Collapsed;
        private Visibility sessionDetailVisibility = Visibility.Collapsed;
        private int? selectedWeekdayIndex;
        private IList<DistributionBarViewModel> allHourDistribution =
            new List<DistributionBarViewModel>();
        private string hourDistributionTitle = LocalizationService.Get(
            "LOCPlaytimeInsightsHourDistributionAll",
            "24 小时分布 · 全部星期");

        public DashboardViewModel(
            IPlayniteAPI playniteApi,
            SessionRepository sessionRepository,
            AnalyticsService analyticsService,
            SessionQueryService queryService,
            PlaytimeInsightsSettingsViewModel settings)
        {
            this.playniteApi = playniteApi;
            this.sessionRepository = sessionRepository;
            this.analyticsService = analyticsService;
            this.queryService = queryService;
            this.settings = settings;
            customStartDate = DateTime.Today.AddDays(
                -(Math.Max(1, Math.Min(366, settings.Settings.RecentDays)) - 1));

            RangeOptions = new ObservableCollection<SelectionOption<DateRangePreset>>
            {
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.Today, Label = LocalizationService.Get("LOCPlaytimeInsightsToday", "今天") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisWeek, Label = LocalizationService.Get("LOCPlaytimeInsightsThisWeek", "本周") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisMonth, Label = LocalizationService.Get("LOCPlaytimeInsightsThisMonth", "本月") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisYear, Label = LocalizationService.Get("LOCPlaytimeInsightsThisYear", "本年") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.Custom, Label = LocalizationService.Get("LOCPlaytimeInsightsCustom", "自定义") }
            };
            AggregationOptions = new ObservableCollection<SelectionOption<AggregationPeriod>>
            {
                new SelectionOption<AggregationPeriod> { Value = AggregationPeriod.Auto, Label = LocalizationService.Get("LOCPlaytimeInsightsAutoRecommended", "自动（推荐）") },
                new SelectionOption<AggregationPeriod> { Value = AggregationPeriod.Day, Label = LocalizationService.Get("LOCPlaytimeInsightsDay", "日") },
                new SelectionOption<AggregationPeriod> { Value = AggregationPeriod.Week, Label = LocalizationService.Get("LOCPlaytimeInsightsWeek", "周") },
                new SelectionOption<AggregationPeriod> { Value = AggregationPeriod.Month, Label = LocalizationService.Get("LOCPlaytimeInsightsMonth", "月") },
                new SelectionOption<AggregationPeriod> { Value = AggregationPeriod.Year, Label = LocalizationService.Get("LOCPlaytimeInsightsYear", "年") }
            };
            RankingMetricOptions = new ObservableCollection<SelectionOption<RankingMetric>>
            {
                new SelectionOption<RankingMetric> { Value = RankingMetric.Duration, Label = LocalizationService.Get("LOCPlaytimeInsightsDuration", "游玩时长") },
                new SelectionOption<RankingMetric> { Value = RankingMetric.SessionCount, Label = LocalizationService.Get("LOCPlaytimeInsightsSessionCount", "会话次数") },
                new SelectionOption<RankingMetric> { Value = RankingMetric.ActiveDays, Label = LocalizationService.Get("LOCPlaytimeInsightsActiveDays", "活跃天数") },
                new SelectionOption<RankingMetric> { Value = RankingMetric.AverageSession, Label = LocalizationService.Get("LOCPlaytimeInsightsAverageSessionOption", "平均会话") },
                new SelectionOption<RankingMetric> { Value = RankingMetric.LongestSession, Label = LocalizationService.Get("LOCPlaytimeInsightsLongestSessionOption", "最长会话") }
            };
            MetadataDimensionOptions =
                new ObservableCollection<SelectionOption<MetadataFilterDimension?>>
                {
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = null,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsNoFilter",
                            "不筛选")
                    },
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = MetadataFilterDimension.Library,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsLibrary",
                            "库来源")
                    },
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = MetadataFilterDimension.Developer,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsDeveloper",
                            "开发者")
                    },
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = MetadataFilterDimension.Genre,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsGenre",
                            "类型")
                    },
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = MetadataFilterDimension.Tag,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsTag",
                            "标签")
                    },
                    new SelectionOption<MetadataFilterDimension?>
                    {
                        Value = MetadataFilterDimension.InstallationStatus,
                        Label = LocalizationService.Get(
                            "LOCPlaytimeInsightsInstallationStatus",
                            "安装状态")
                    }
                };
            MetadataValueOptions =
                new ObservableCollection<SelectionOption<string>>();

            selectedRangeOption = RangeOptions[2];
            selectedAggregationOption = AggregationOptions[0];
            selectedRankingMetricOption = RankingMetricOptions[0];
            selectedMetadataDimensionOption = MetadataDimensionOptions[0];
            PeriodActivities = new ObservableCollection<PeriodActivityViewModel>();
            HeatmapCells = new ObservableCollection<HeatmapCellViewModel>();
            HeatmapWeekdayLabels = new ObservableCollection<string>();
            TrendPoints = new ObservableCollection<TrendPointViewModel>();
            RangeGameRankings = new ObservableCollection<GameRankingViewModel>();
            LifetimeGameRankings = new ObservableCollection<GameRankingViewModel>();
            WeekdayDistribution =
                new ObservableCollection<DistributionBarViewModel>();
            HourDistribution =
                new ObservableCollection<DistributionBarViewModel>();
            WeekHourCells =
                new ObservableCollection<WeekHourCellViewModel>();
            AdvancedWeekdayLabels = new ObservableCollection<string>();
            AdvancedHourLabels = new ObservableCollection<string>();
            Anomalies = new ObservableCollection<AnomalySessionViewModel>();
            SessionDetails = sessionDetailPager.VisibleItems;
            RefreshCommand = new RelayCommand(
                Refresh,
                CanRefresh);
            LoadMoreSessionDetailsCommand = new RelayCommand(
                LoadMoreSessionDetails,
                () => !refreshGuard.IsActive && sessionDetailPager.HasMore);
            SelectWeekdayCommand =
                new RelayCommand<DistributionBarViewModel>(
                    SelectWeekdayDistribution,
                    CanSelectWeekday);
            SelectHeatmapDateCommand =
                new RelayCommand<HeatmapCellViewModel>(
                    SelectHeatmapDate,
                    cell => !refreshGuard.IsActive &&
                        cell != null &&
                        cell.CellVisibility == Visibility.Visible);
            SelectPeriodCommand =
                new RelayCommand<PeriodActivityViewModel>(
                    SelectPeriod,
                    period => !refreshGuard.IsActive && period != null);
            RefreshMetadataValueOptions();
        }

        public ObservableCollection<SelectionOption<DateRangePreset>> RangeOptions { get; }

        public ObservableCollection<SelectionOption<AggregationPeriod>> AggregationOptions { get; }

        public ObservableCollection<SelectionOption<RankingMetric>> RankingMetricOptions { get; }

        public ObservableCollection<SelectionOption<MetadataFilterDimension?>>
            MetadataDimensionOptions { get; }

        public ObservableCollection<SelectionOption<string>> MetadataValueOptions { get; }

        public SelectionOption<DateRangePreset> SelectedRangeOption
        {
            get => selectedRangeOption;
            set
            {
                if (!ReferenceEquals(selectedRangeOption, value))
                {
                    SetValue(ref selectedRangeOption, value);
                    OnPropertyChanged(nameof(CustomDateVisibility));
                    Refresh();
                }
            }
        }

        public Visibility CustomDateVisibility =>
            SelectedRangeOption?.Value == DateRangePreset.Custom
                ? Visibility.Visible
                : Visibility.Collapsed;

        public SelectionOption<AggregationPeriod> SelectedAggregationOption
        {
            get => selectedAggregationOption;
            set
            {
                if (!ReferenceEquals(selectedAggregationOption, value))
                {
                    SetValue(ref selectedAggregationOption, value);
                    Refresh();
                }
            }
        }

        public SelectionOption<RankingMetric> SelectedRankingMetricOption
        {
            get => selectedRankingMetricOption;
            set
            {
                if (!ReferenceEquals(selectedRankingMetricOption, value))
                {
                    SetValue(ref selectedRankingMetricOption, value);
                    Refresh();
                }
            }
        }

        public SelectionOption<MetadataFilterDimension?>
            SelectedMetadataDimensionOption
        {
            get => selectedMetadataDimensionOption;
            set
            {
                if (!ReferenceEquals(selectedMetadataDimensionOption, value))
                {
                    SetValue(ref selectedMetadataDimensionOption, value);
                    OnPropertyChanged(nameof(MetadataValueVisibility));
                    RefreshMetadataValueOptions();
                    if (!suppressFilterRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        public SelectionOption<string> SelectedMetadataValueOption
        {
            get => selectedMetadataValueOption;
            set
            {
                if (!ReferenceEquals(selectedMetadataValueOption, value))
                {
                    SetValue(ref selectedMetadataValueOption, value);
                    if (!suppressFilterRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        public Visibility MetadataValueVisibility =>
            SelectedMetadataDimensionOption?.Value.HasValue == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        public DateTime CustomStartDate
        {
            get => customStartDate;
            set
            {
                if (customStartDate != value)
                {
                    SetValue(ref customStartDate, value);
                    if (SelectedRangeOption?.Value == DateRangePreset.Custom)
                    {
                        Refresh();
                    }
                }
            }
        }

        public DateTime CustomEndDate
        {
            get => customEndDate;
            set
            {
                if (customEndDate != value)
                {
                    SetValue(ref customEndDate, value);
                    if (SelectedRangeOption?.Value == DateRangePreset.Custom)
                    {
                        Refresh();
                    }
                }
            }
        }

        public string LifetimeDurationText
        {
            get => lifetimeDurationText;
            private set => SetValue(ref lifetimeDurationText, value);
        }

        public string TrackedDurationText
        {
            get => trackedDurationText;
            private set
            {
                SetValue(ref trackedDurationText, value);
                OnPropertyChanged(nameof(TrackedDurationSummaryText));
            }
        }

        public string TrackedDurationSummaryText =>
            LocalizationService.Format(
                "LOCPlaytimeInsightsTrackedDurationFormat",
                "插件已记录：{0}",
                TrackedDurationText);

        public string RangeDurationText
        {
            get => rangeDurationText;
            private set => SetValue(ref rangeDurationText, value);
        }

        public string SessionCountText
        {
            get => sessionCountText;
            private set => SetValue(ref sessionCountText, value);
        }

        public string ActiveDaysText
        {
            get => activeDaysText;
            private set => SetValue(ref activeDaysText, value);
        }

        public string AverageSessionText
        {
            get => averageSessionText;
            private set => SetValue(ref averageSessionText, value);
        }

        public string LongestSessionText
        {
            get => longestSessionText;
            private set => SetValue(ref longestSessionText, value);
        }

        public string RangeText
        {
            get => rangeText;
            private set => SetValue(ref rangeText, value);
        }

        public string PeriodTitleText
        {
            get => periodTitleText;
            private set => SetValue(ref periodTitleText, value);
        }

        public string RangeRankingTitleText
        {
            get => rangeRankingTitleText;
            private set => SetValue(ref rangeRankingTitleText, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => SetValue(ref statusText, value);
        }

        public string SelectedDetailTitle
        {
            get => selectedDetailTitle;
            private set => SetValue(ref selectedDetailTitle, value);
        }

        public int HeatmapColumnCount
        {
            get => heatmapColumnCount;
            private set => SetValue(ref heatmapColumnCount, value);
        }

        public double TrendChartWidth
        {
            get => trendChartWidth;
            private set => SetValue(ref trendChartWidth, value);
        }

        public PointCollection TrendLinePoints
        {
            get => trendLinePoints;
            private set => SetValue(ref trendLinePoints, value);
        }

        public Geometry TrendLineGeometry
        {
            get => trendLineGeometry;
            private set => SetValue(ref trendLineGeometry, value);
        }

        public Geometry TrendAreaGeometry
        {
            get => trendAreaGeometry;
            private set => SetValue(ref trendAreaGeometry, value);
        }

        public ComparisonMetricViewModel PreviousPeriodComparison
        {
            get => previousPeriodComparison;
            private set => SetValue(ref previousPeriodComparison, value);
        }

        public ComparisonMetricViewModel YearOverYearComparison
        {
            get => yearOverYearComparison;
            private set => SetValue(ref yearOverYearComparison, value);
        }

        public string LongestStreakText
        {
            get => longestStreakText;
            private set => SetValue(ref longestStreakText, value);
        }

        public string CurrentStreakText
        {
            get => currentStreakText;
            private set => SetValue(ref currentStreakText, value);
        }

        public string CurrentStreakDateText
        {
            get => currentStreakDateText;
            private set => SetValue(ref currentStreakDateText, value);
        }

        public string AnomalyCountText
        {
            get => anomalyCountText;
            private set => SetValue(ref anomalyCountText, value);
        }

        public Visibility AnomalyVisibility
        {
            get => anomalyVisibility;
            private set => SetValue(ref anomalyVisibility, value);
        }

        public Visibility SessionDetailVisibility
        {
            get => sessionDetailVisibility;
            private set => SetValue(ref sessionDetailVisibility, value);
        }

        public string HourDistributionTitle
        {
            get => hourDistributionTitle;
            private set => SetValue(ref hourDistributionTitle, value);
        }

        public ObservableCollection<PeriodActivityViewModel> PeriodActivities { get; }

        public ObservableCollection<HeatmapCellViewModel> HeatmapCells { get; }

        public ObservableCollection<string> HeatmapWeekdayLabels { get; }

        public ObservableCollection<TrendPointViewModel> TrendPoints { get; }

        public ObservableCollection<GameRankingViewModel> RangeGameRankings { get; }

        public ObservableCollection<GameRankingViewModel> LifetimeGameRankings { get; }

        public ObservableCollection<DistributionBarViewModel>
            WeekdayDistribution { get; }

        public ObservableCollection<DistributionBarViewModel>
            HourDistribution { get; }

        public ObservableCollection<WeekHourCellViewModel> WeekHourCells { get; }

        public ObservableCollection<string> AdvancedWeekdayLabels { get; }

        public ObservableCollection<string> AdvancedHourLabels { get; }

        public ObservableCollection<AnomalySessionViewModel> Anomalies { get; }

        public ObservableCollection<SessionDetailViewModel> SessionDetails { get; }

        public RelayCommand RefreshCommand { get; }

        public RelayCommand LoadMoreSessionDetailsCommand { get; }

        public RelayCommand<DistributionBarViewModel> SelectWeekdayCommand { get; }

        public RelayCommand<HeatmapCellViewModel> SelectHeatmapDateCommand { get; }

        public RelayCommand<PeriodActivityViewModel> SelectPeriodCommand { get; }

        public string SessionDetailCountText =>
            sessionDetailPager.TotalCount == 0
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsNoSessionsDisplayed",
                    "未显示会话")
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsDisplayedSessionsFormat",
                    "已显示 {0:N0} / {1:N0}",
                    sessionDetailPager.VisibleCount,
                    sessionDetailPager.TotalCount);

        public Visibility LoadMoreVisibility =>
            sessionDetailPager.HasMore ? Visibility.Visible : Visibility.Collapsed;

        public void Refresh()
        {
            if (!refreshGuard.TryEnter())
            {
                return;
            }

            RaiseCommandStates();
            try
            {
                RefreshCore();
            }
            finally
            {
                refreshGuard.Exit();
                RaiseCommandStates();
            }
        }

        private void RefreshCore()
        {
            if (SelectedRangeOption == null ||
                SelectedAggregationOption == null ||
                SelectedRankingMetricOption == null ||
                SelectedMetadataDimensionOption == null)
            {
                return;
            }

            var libraryNames = GetLibraryNames();
            RefreshMetadataValueOptions(libraryNames);
            var allGames = playniteApi.Database.Games.ToList();
            var allSessions = sessionRepository.GetAll();
            var selectedDimension = SelectedMetadataDimensionOption.Value;
            var selectedValue = SelectedMetadataValueOption?.Value;
            if (selectedDimension.HasValue &&
                !string.IsNullOrWhiteSpace(selectedValue))
            {
                activeFilteredGames = queryService.FilterGames(
                    allGames,
                    selectedDimension.Value,
                    selectedValue,
                    libraryNames);
                var gameIds = new HashSet<Guid>(
                    activeFilteredGames.Select(game => game.Id));
                activeFilteredSessions = allSessions
                    .Where(session => gameIds.Contains(session.GameId))
                    .ToList();
            }
            else
            {
                activeFilteredGames = allGames;
                activeFilteredSessions = allSessions.ToList();
            }

            var snapshot = analyticsService.CreateSnapshot(
                activeFilteredGames,
                activeFilteredSessions,
                new AnalyticsQuery
                {
                    RangePreset = SelectedRangeOption.Value,
                    AggregationPeriod = SelectedAggregationOption.Value,
                    RankingMetric = SelectedRankingMetricOption.Value,
                    CustomStartDate = CustomStartDate,
                    CustomEndDate = CustomEndDate,
                    UseIsoWeekStart = settings.Settings.UseIsoWeekStart,
                    TopGames = settings.Settings.TopGames
                });
            ApplyRankingCoverImages(snapshot.RangeGameRankings, allGames);
            ApplyRankingCoverImages(snapshot.LifetimeGameRankings, allGames);

            LifetimeDurationText = snapshot.LifetimeDurationText;
            TrackedDurationText = snapshot.TrackedDurationText;
            RangeDurationText = snapshot.RangeDurationText;
            SessionCountText = snapshot.SessionCountText;
            ActiveDaysText = snapshot.ActiveDaysText;
            AverageSessionText = snapshot.AverageSessionText;
            LongestSessionText = snapshot.LongestSessionText;
            RangeText = snapshot.RangeText;
            PeriodTitleText = snapshot.PeriodTitleText;
            RangeRankingTitleText = snapshot.RangeRankingTitleText;
            StatusText = snapshot.StatusText;
            HeatmapColumnCount = snapshot.HeatmapColumnCount;
            TrendChartWidth = snapshot.TrendChartWidth;
            TrendLinePoints = snapshot.TrendLinePoints;
            TrendLineGeometry = snapshot.TrendLineGeometry;
            TrendAreaGeometry = snapshot.TrendAreaGeometry;
            PreviousPeriodComparison = snapshot.Advanced.PreviousPeriodComparison;
            YearOverYearComparison = snapshot.Advanced.YearOverYearComparison;
            LongestStreakText = snapshot.Advanced.LongestStreakText;
            CurrentStreakText = snapshot.Advanced.CurrentStreakText;
            CurrentStreakDateText = snapshot.Advanced.CurrentStreakDateText;
            AnomalyCountText = snapshot.Advanced.AnomalyCountText;
            AnomalyVisibility = snapshot.Advanced.AnomalyVisibility;

            Replace(PeriodActivities, snapshot.PeriodActivities);
            Replace(HeatmapCells, snapshot.HeatmapCells);
            Replace(HeatmapWeekdayLabels, snapshot.HeatmapWeekdayLabels);
            Replace(TrendPoints, snapshot.TrendPoints);
            Replace(RangeGameRankings, snapshot.RangeGameRankings);
            Replace(LifetimeGameRankings, snapshot.LifetimeGameRankings);
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
            Replace(
                WeekdayDistribution,
                snapshot.Advanced.WeekdayDistribution);
            Replace(HourDistribution, allHourDistribution);
            Replace(WeekHourCells, snapshot.Advanced.WeekHourCells);
            Replace(
                AdvancedWeekdayLabels,
                snapshot.Advanced.WeekdayLabels);
            Replace(AdvancedHourLabels, snapshot.Advanced.HourLabels);
            Replace(Anomalies, snapshot.Advanced.Anomalies);
            sessionDetailPager.Reset(null);
            NotifySessionDetailPagingChanged();
            SessionDetailVisibility = Visibility.Collapsed;
            SelectedDetailTitle = LocalizationService.Get(
                "LOCPlaytimeInsightsDetailsPrompt",
                "点击柱形、折线点或热力格查看会话");
            HourDistributionTitle = LocalizationService.Get(
                "LOCPlaytimeInsightsHourDistributionAll",
                "24 小时分布 · 全部星期");
        }

        public void SelectWeekdayDistribution(DistributionBarViewModel bar)
        {
            var index = WeekdayDistribution.IndexOf(bar);
            if (index < 0)
            {
                return;
            }

            selectedWeekdayIndex =
                selectedWeekdayIndex == index ? (int?)null : index;
            for (var day = 0; day < WeekdayDistribution.Count; day++)
            {
                WeekdayDistribution[day].IsSelected =
                    selectedWeekdayIndex == day;
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

        public void SelectPeriod(PeriodActivityViewModel period)
        {
            if (period == null)
            {
                return;
            }

            LoadSessionDetails(
                period.PeriodStart,
                period.PeriodEnd,
                period.Label + " · " + period.DurationText);
        }

        public void SelectHeatmapDate(HeatmapCellViewModel cell)
        {
            if (cell == null || cell.CellVisibility != Visibility.Visible)
            {
                return;
            }

            LoadSessionDetails(
                cell.Date,
                cell.Date,
                cell.Date.ToString("yyyy/M/d") + " · " +
                    AnalyticsService.FormatDurationPrecise(cell.Seconds));
        }

        private void LoadSessionDetails(DateTime startDate, DateTime endDate, string title)
        {
            var details = analyticsService.CreateSessionDetails(
                activeFilteredGames,
                activeFilteredSessions,
                startDate,
                endDate);
            ApplySessionDetailCoverImages(details, activeFilteredGames);
            sessionDetailPager.Reset(details);
            NotifySessionDetailPagingChanged();
            SessionDetailVisibility = Visibility.Visible;
            SelectedDetailTitle = details.Count == 0
                ? LocalizationService.Format(
                    "LOCPlaytimeInsightsNoPreciseDetailFormat",
                    "{0} · 没有精确会话",
                    title)
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsDetailSessionCountFormat",
                    "{0} · {1:N0} 条会话",
                    title,
                    details.Count);
        }

        private void ApplyRankingCoverImages(
            IEnumerable<GameRankingViewModel> rankings,
            IEnumerable<Game> games)
        {
            var gamesById = (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var ranking in rankings ?? Enumerable.Empty<GameRankingViewModel>())
            {
                Game game;
                if (!gamesById.TryGetValue(ranking.GameId, out game) ||
                    string.IsNullOrWhiteSpace(game.CoverImage))
                {
                    ranking.CoverImagePath = null;
                    continue;
                }

                try
                {
                    ranking.CoverImagePath =
                        playniteApi.Database.GetFullFilePath(game.CoverImage);
                }
                catch
                {
                    ranking.CoverImagePath = null;
                }
            }
        }

        private void ApplySessionDetailCoverImages(
            IEnumerable<SessionDetailViewModel> details,
            IEnumerable<Game> games)
        {
            var gamesById = (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var detail in details ?? Enumerable.Empty<SessionDetailViewModel>())
            {
                Game game;
                if (!gamesById.TryGetValue(detail.GameId, out game) ||
                    string.IsNullOrWhiteSpace(game.CoverImage))
                {
                    detail.CoverImagePath = null;
                    continue;
                }

                try
                {
                    detail.CoverImagePath =
                        playniteApi.Database.GetFullFilePath(game.CoverImage);
                }
                catch
                {
                    detail.CoverImagePath = null;
                }
            }
        }

        public void LoadMoreSessionDetails()
        {
            if (sessionDetailPager.AppendNextPage() > 0)
            {
                NotifySessionDetailPagingChanged();
            }
        }

        private void NotifySessionDetailPagingChanged()
        {
            OnPropertyChanged(nameof(SessionDetailCountText));
            OnPropertyChanged(nameof(LoadMoreVisibility));
            LoadMoreSessionDetailsCommand?.RaiseCanExecuteChanged();
        }

        private bool CanRefresh()
        {
            return !refreshGuard.IsActive &&
                SelectedRangeOption != null &&
                SelectedAggregationOption != null &&
                SelectedRankingMetricOption != null &&
                SelectedMetadataDimensionOption != null;
        }

        private bool CanSelectWeekday(DistributionBarViewModel bar)
        {
            return !refreshGuard.IsActive &&
                bar != null &&
                WeekdayDistribution.Contains(bar);
        }

        private void RaiseCommandStates()
        {
            RefreshCommand?.RaiseCanExecuteChanged();
            LoadMoreSessionDetailsCommand?.RaiseCanExecuteChanged();
            SelectWeekdayCommand?.RaiseCanExecuteChanged();
            SelectHeatmapDateCommand?.RaiseCanExecuteChanged();
            SelectPeriodCommand?.RaiseCanExecuteChanged();
        }

        private void RefreshMetadataValueOptions(
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            if (SelectedMetadataDimensionOption == null)
            {
                return;
            }

            var previousValue = SelectedMetadataValueOption?.Value ??
                string.Empty;
            var values = new List<SelectionOption<string>>
            {
                new SelectionOption<string>
                {
                    Value = string.Empty,
                    Label = SelectedMetadataDimensionOption.Value.HasValue
                        ? LocalizationService.Format(
                            "LOCPlaytimeInsightsAllFormat",
                            "全部{0}",
                            SessionQueryService.GetDimensionLabel(
                                SelectedMetadataDimensionOption.Value.Value))
                        : LocalizationService.Get(
                            "LOCPlaytimeInsightsAllGames",
                            "全部游戏")
                }
            };
            if (SelectedMetadataDimensionOption.Value.HasValue)
            {
                libraryNames = libraryNames ?? GetLibraryNames();
                values.AddRange(queryService.GetMetadataValues(
                        playniteApi.Database.Games,
                        SelectedMetadataDimensionOption.Value.Value,
                        libraryNames)
                    .Select(value => new SelectionOption<string>
                    {
                        Value = value,
                        Label = value
                    }));
            }

            var selected = values.FirstOrDefault(option =>
                    string.Equals(
                        option.Value,
                        previousValue,
                        StringComparison.CurrentCultureIgnoreCase)) ??
                values[0];
            suppressFilterRefresh = true;
            try
            {
                Replace(MetadataValueOptions, values);
                SelectedMetadataValueOption = selected;
            }
            finally
            {
                suppressFilterRefresh = false;
            }
        }

        private IReadOnlyDictionary<Guid, string> GetLibraryNames()
        {
            return playniteApi.Addons.Plugins
                .OfType<LibraryPlugin>()
                .GroupBy(plugin => plugin.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name ?? string.Empty);
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
