using Playnite.SDK;
using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly RefreshReentrancyGuard refreshGuard =
            new RefreshReentrancyGuard();
        private IReadOnlyDictionary<Guid, string> libraryNames =
            new Dictionary<Guid, string>();
        private IList<Game> allGames = new List<Game>();
        private IReadOnlyDictionary<Guid, Game> gamesById =
            new Dictionary<Guid, Game>();
        private IList<GameSession> allSessions = new List<GameSession>();
        private IList<Game> filteredGames = new List<Game>();
        private IList<GameSession> filteredSessions = new List<GameSession>();
        private DashboardAnalysisContext analysisContext;
        private bool dataCacheReady;

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

            Filter = new DashboardFilterViewModel(
                playniteApi,
                queryService,
                settings.Settings.RecentDays,
                reason => Refresh(reason));
            Metrics = new DashboardMetricsViewModel(playniteApi);
            Distribution = new DashboardDistributionViewModel();
            Drilldown = new DashboardDrilldownViewModel(
                playniteApi,
                analyticsService);
            Filter.PropertyChanged += ForwardPropertyChanged;
            Metrics.PropertyChanged += ForwardPropertyChanged;
            Distribution.PropertyChanged += ForwardPropertyChanged;
            Drilldown.PropertyChanged += DrilldownPropertyChanged;

            RefreshCommand = new RelayCommand(Refresh, CanRefresh);
            LoadMoreSessionDetailsCommand = new RelayCommand(
                LoadMoreSessionDetails,
                () => !refreshGuard.IsActive && Drilldown.HasMore);
            SelectWeekdayCommand = new RelayCommand<DistributionBarViewModel>(
                SelectWeekdayDistribution,
                CanSelectWeekday);
            SelectHeatmapDateCommand = new RelayCommand<HeatmapCellViewModel>(
                SelectHeatmapDate,
                cell => !refreshGuard.IsActive &&
                    cell != null &&
                    cell.CellVisibility == Visibility.Visible);
            SelectPeriodCommand = new RelayCommand<PeriodActivityViewModel>(
                SelectPeriod,
                period => !refreshGuard.IsActive && period != null);
        }

        public DashboardFilterViewModel Filter { get; }

        public DashboardMetricsViewModel Metrics { get; }

        public DashboardDistributionViewModel Distribution { get; }

        public DashboardDrilldownViewModel Drilldown { get; }

        public ObservableCollection<SelectionOption<DateRangePreset>> RangeOptions => Filter.RangeOptions;

        public ObservableCollection<SelectionOption<AggregationPeriod>> AggregationOptions => Filter.AggregationOptions;

        public ObservableCollection<SelectionOption<RankingMetric>> RankingMetricOptions => Filter.RankingMetricOptions;

        public ObservableCollection<SelectionOption<MetadataFilterDimension?>> MetadataDimensionOptions => Filter.MetadataDimensionOptions;

        public ObservableCollection<SelectionOption<string>> MetadataValueOptions => Filter.MetadataValueOptions;

        public SelectionOption<DateRangePreset> SelectedRangeOption
        {
            get => Filter.SelectedRangeOption;
            set => Filter.SelectedRangeOption = value;
        }

        public Visibility CustomDateVisibility => Filter.CustomDateVisibility;

        public SelectionOption<AggregationPeriod> SelectedAggregationOption
        {
            get => Filter.SelectedAggregationOption;
            set => Filter.SelectedAggregationOption = value;
        }

        public SelectionOption<RankingMetric> SelectedRankingMetricOption
        {
            get => Filter.SelectedRankingMetricOption;
            set => Filter.SelectedRankingMetricOption = value;
        }

        public SelectionOption<MetadataFilterDimension?> SelectedMetadataDimensionOption
        {
            get => Filter.SelectedMetadataDimensionOption;
            set => Filter.SelectedMetadataDimensionOption = value;
        }

        public SelectionOption<string> SelectedMetadataValueOption
        {
            get => Filter.SelectedMetadataValueOption;
            set => Filter.SelectedMetadataValueOption = value;
        }

        public Visibility MetadataValueVisibility => Filter.MetadataValueVisibility;

        public DateTime CustomStartDate
        {
            get => Filter.CustomStartDate;
            set => Filter.CustomStartDate = value;
        }

        public DateTime CustomEndDate
        {
            get => Filter.CustomEndDate;
            set => Filter.CustomEndDate = value;
        }

        public string LifetimeDurationText => Metrics.LifetimeDurationText;

        public DurationDisplayViewModel LifetimeDurationDisplay => Metrics.LifetimeDurationDisplay;

        public string TrackedDurationText => Metrics.TrackedDurationText;

        public string TrackedDurationSummaryText => Metrics.TrackedDurationSummaryText;

        public string RangeDurationText => Metrics.RangeDurationText;

        public DurationDisplayViewModel RangeDurationDisplay => Metrics.RangeDurationDisplay;

        public string SessionCountText => Metrics.SessionCountText;

        public string ActiveDaysText => Metrics.ActiveDaysText;

        public string AverageSessionText => Metrics.AverageSessionText;

        public DurationDisplayViewModel AverageSessionDisplay => Metrics.AverageSessionDisplay;

        public string LongestSessionText => Metrics.LongestSessionText;

        public DurationDisplayViewModel LongestSessionDisplay => Metrics.LongestSessionDisplay;

        public string RangeText => Metrics.RangeText;

        public string PeriodTitleText => Metrics.PeriodTitleText;

        public string RangeRankingTitleText => Metrics.RangeRankingTitleText;

        public string StatusText => Metrics.StatusText;

        public string SelectedDetailTitle => Drilldown.SelectedDetailTitle;

        public int HeatmapColumnCount => Distribution.HeatmapColumnCount;

        public double TrendChartWidth => Distribution.TrendChartWidth;

        public PointCollection TrendLinePoints => Distribution.TrendLinePoints;

        public Geometry TrendLineGeometry => Distribution.TrendLineGeometry;

        public Geometry TrendAreaGeometry => Distribution.TrendAreaGeometry;

        public Visibility ComparisonVisibility => Metrics.ComparisonVisibility;

        public ComparisonMetricViewModel PreviousPeriodComparison => Metrics.PreviousPeriodComparison;

        public ComparisonMetricViewModel YearOverYearComparison => Metrics.YearOverYearComparison;

        public string LongestStreakText => Metrics.LongestStreakText;

        public string CurrentStreakText => Metrics.CurrentStreakText;

        public string CurrentStreakDateText => Metrics.CurrentStreakDateText;

        public string AnomalyCountText => Metrics.AnomalyCountText;

        public Visibility AnomalyVisibility => Distribution.AnomalyVisibility;

        public Visibility SessionDetailVisibility => Drilldown.SessionDetailVisibility;

        public string HourDistributionTitle => Distribution.HourDistributionTitle;

        public IReadOnlyList<PeriodActivityViewModel> PeriodActivities =>
            Distribution.PeriodActivities;

        public IReadOnlyList<HeatmapCellViewModel> HeatmapCells => Distribution.HeatmapCells;

        public IReadOnlyList<string> HeatmapWeekdayLabels => Distribution.HeatmapWeekdayLabels;

        public IReadOnlyList<TrendPointViewModel> TrendPoints => Distribution.TrendPoints;

        public IReadOnlyList<GameRankingViewModel> RangeGameRankings => Metrics.RangeGameRankings;

        public IReadOnlyList<GameRankingViewModel> LifetimeGameRankings => Metrics.LifetimeGameRankings;

        public IReadOnlyList<DistributionBarViewModel> WeekdayDistribution => Distribution.WeekdayDistribution;

        public IReadOnlyList<DistributionBarViewModel> HourDistribution => Distribution.HourDistribution;

        public IReadOnlyList<WeekHourCellViewModel> WeekHourCells => Distribution.WeekHourCells;

        public IReadOnlyList<string> AdvancedWeekdayLabels => Distribution.AdvancedWeekdayLabels;

        public IReadOnlyList<string> AdvancedHourLabels => Distribution.AdvancedHourLabels;

        public IReadOnlyList<AnomalySessionViewModel> Anomalies => Distribution.Anomalies;

        public ObservableCollection<SessionDetailViewModel> SessionDetails => Drilldown.SessionDetails;

        public RelayCommand RefreshCommand { get; }

        public RelayCommand LoadMoreSessionDetailsCommand { get; }

        public RelayCommand<DistributionBarViewModel> SelectWeekdayCommand { get; }

        public RelayCommand<HeatmapCellViewModel> SelectHeatmapDateCommand { get; }

        public RelayCommand<PeriodActivityViewModel> SelectPeriodCommand { get; }

        public string SessionDetailCountText => Drilldown.SessionDetailCountText;

        public Visibility LoadMoreVisibility => Drilldown.LoadMoreVisibility;

        public void Refresh()
        {
            Refresh(DashboardRefreshReason.DataReload);
        }

        private void Refresh(DashboardRefreshReason reason)
        {
            if (!refreshGuard.TryEnter())
            {
                return;
            }

            RaiseCommandStates();
            try
            {
                RefreshCore(reason);
            }
            finally
            {
                refreshGuard.Exit();
                RaiseCommandStates();
            }
        }

        public void SelectWeekdayDistribution(DistributionBarViewModel bar)
        {
            Distribution.SelectWeekday(bar);
        }

        public void SelectPeriod(PeriodActivityViewModel period)
        {
            Drilldown.SelectPeriod(period);
        }

        public void SelectHeatmapDate(HeatmapCellViewModel cell)
        {
            Drilldown.SelectHeatmapDate(cell);
        }

        public void LoadMoreSessionDetails()
        {
            Drilldown.LoadMore();
        }

        private void RefreshCore(DashboardRefreshReason reason)
        {
            if (!Filter.IsComplete)
            {
                return;
            }

            var total = Stopwatch.StartNew();
            long dataMilliseconds = 0;
            long filterMilliseconds = 0;
            var cacheReady = dataCacheReady && analysisContext != null;
            var plan = DashboardRefreshPlan.Create(reason, cacheReady);

            var phase = Stopwatch.StartNew();
            if (plan.ReloadData)
            {
                LoadData();
            }
            if (plan.RefreshMetadataOptions)
            {
                Filter.RefreshMetadataValueOptions(allGames, libraryNames);
            }
            dataMilliseconds = phase.ElapsedMilliseconds;

            phase.Restart();
            if (plan.RebuildFilter)
            {
                RebuildFilteredData();
            }
            filterMilliseconds = phase.ElapsedMilliseconds;

            DashboardRefreshTiming timing;
            switch (plan.Mode)
            {
                case DashboardRefreshMode.TrendOnly:
                    timing = ApplyTrendRefresh();
                    break;
                case DashboardRefreshMode.RankingOnly:
                    timing = ApplyRankingRefresh();
                    break;
                case DashboardRefreshMode.FullAnalysis:
                default:
                    timing = ApplyFullAnalysis();
                    break;
            }

            total.Stop();
            Trace.WriteLine(string.Format(
                "PlaytimeInsights Dashboard refresh reason={0} data={1}ms filter={2}ms analytics={3}ms apply={4}ms total={5}ms",
                plan.Reason,
                dataMilliseconds,
                filterMilliseconds,
                timing.AnalyticsMilliseconds,
                timing.ApplyMilliseconds,
                total.ElapsedMilliseconds));
        }

        private void LoadData()
        {
            dataCacheReady = false;
            analysisContext = null;
            var loadedLibraryNames = Filter.GetLibraryNames();
            var loadedGames = playniteApi.Database.Games.ToList();
            var loadedSessions = sessionRepository.GetAll().ToList();
            libraryNames = loadedLibraryNames;
            allGames = loadedGames;
            gamesById = loadedGames.GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());
            allSessions = loadedSessions;
            dataCacheReady = true;
        }

        private void RebuildFilteredData()
        {
            var selectedDimension = Filter.SelectedMetadataDimensionOption.Value;
            var selectedValue = Filter.SelectedMetadataValueOption?.Value;
            if (selectedDimension.HasValue &&
                !string.IsNullOrWhiteSpace(selectedValue))
            {
                filteredGames = queryService.FilterGames(
                    allGames,
                    selectedDimension.Value,
                    selectedValue,
                    libraryNames);
                var gameIds = new HashSet<Guid>(filteredGames.Select(game => game.Id));
                filteredSessions = allSessions
                    .Where(session => gameIds.Contains(session.GameId))
                    .ToList();
            }
            else
            {
                filteredGames = allGames;
                filteredSessions = allSessions.ToList();
            }
        }

        private DashboardRefreshTiming ApplyTrendRefresh()
        {
            var phase = Stopwatch.StartNew();
            var projection = analyticsService.CreateTrendProjection(
                analysisContext,
                Filter.SelectedAggregationOption.Value);
            var analyticsMilliseconds = phase.ElapsedMilliseconds;
            phase.Restart();
            Metrics.ApplyPeriodTitle(projection);
            Distribution.ApplyTrend(projection);
            Drilldown.ResetSelection();
            return new DashboardRefreshTiming(
                analyticsMilliseconds,
                phase.ElapsedMilliseconds);
        }

        private DashboardRefreshTiming ApplyRankingRefresh()
        {
            var phase = Stopwatch.StartNew();
            var projection = analyticsService.CreateRankingProjection(
                analysisContext,
                Filter.SelectedRankingMetricOption.Value,
                settings.Settings.TopGames);
            var analyticsMilliseconds = phase.ElapsedMilliseconds;
            phase.Restart();
            Metrics.ApplyRangeRanking(projection, gamesById);
            return new DashboardRefreshTiming(
                analyticsMilliseconds,
                phase.ElapsedMilliseconds);
        }

        private DashboardRefreshTiming ApplyFullAnalysis()
        {
            var phase = Stopwatch.StartNew();
            var result = analyticsService.CreateSnapshotWithContext(
                filteredGames,
                filteredSessions,
                CreateAnalyticsQuery());
            analysisContext = result.Context;
            var analyticsMilliseconds = phase.ElapsedMilliseconds;
            phase.Restart();
            Metrics.Apply(result.Snapshot, gamesById);
            Distribution.Apply(result.Snapshot);
            Drilldown.ResetContext(filteredGames, filteredSessions);
            return new DashboardRefreshTiming(
                analyticsMilliseconds,
                phase.ElapsedMilliseconds);
        }

        private AnalyticsQuery CreateAnalyticsQuery()
        {
            return new AnalyticsQuery
            {
                RangePreset = Filter.SelectedRangeOption.Value,
                AggregationPeriod = Filter.SelectedAggregationOption.Value,
                RankingMetric = Filter.SelectedRankingMetricOption.Value,
                CustomStartDate = Filter.CustomStartDate,
                CustomEndDate = Filter.CustomEndDate,
                UseIsoWeekStart = settings.Settings.UseIsoWeekStart,
                TopGames = settings.Settings.TopGames
            };
        }

        private bool CanRefresh()
        {
            return !refreshGuard.IsActive && Filter.IsComplete;
        }

        private bool CanSelectWeekday(DistributionBarViewModel bar)
        {
            return !refreshGuard.IsActive && Distribution.ContainsWeekday(bar);
        }

        private void RaiseCommandStates()
        {
            RefreshCommand?.RaiseCanExecuteChanged();
            LoadMoreSessionDetailsCommand?.RaiseCanExecuteChanged();
            SelectWeekdayCommand?.RaiseCanExecuteChanged();
            SelectHeatmapDateCommand?.RaiseCanExecuteChanged();
            SelectPeriodCommand?.RaiseCanExecuteChanged();
        }

        private void ForwardPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            OnPropertyChanged(args.PropertyName);
        }

        private void DrilldownPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            OnPropertyChanged(args.PropertyName);
            if (args.PropertyName == nameof(DashboardDrilldownViewModel.HasMore))
            {
                LoadMoreSessionDetailsCommand?.RaiseCanExecuteChanged();
            }
        }

        private sealed class DashboardRefreshTiming
        {
            public DashboardRefreshTiming(
                long analyticsMilliseconds,
                long applyMilliseconds)
            {
                AnalyticsMilliseconds = analyticsMilliseconds;
                ApplyMilliseconds = applyMilliseconds;
            }

            public long AnalyticsMilliseconds { get; }

            public long ApplyMilliseconds { get; }
        }
    }
}
