using Playnite.SDK;
using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                reason => Refresh());
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

        public string TrackedDurationText => Metrics.TrackedDurationText;

        public string TrackedDurationSummaryText => Metrics.TrackedDurationSummaryText;

        public string RangeDurationText => Metrics.RangeDurationText;

        public string SessionCountText => Metrics.SessionCountText;

        public string ActiveDaysText => Metrics.ActiveDaysText;

        public string AverageSessionText => Metrics.AverageSessionText;

        public string LongestSessionText => Metrics.LongestSessionText;

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

        private void RefreshCore()
        {
            if (!Filter.IsComplete)
            {
                return;
            }

            var libraryNames = Filter.GetLibraryNames();
            Filter.RefreshMetadataValueOptions(libraryNames);
            var allGames = playniteApi.Database.Games.ToList();
            var allSessions = sessionRepository.GetAll();
            IList<Game> filteredGames;
            IList<GameSession> filteredSessions;
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

            // The root owns the only repository scan and creates one coherent snapshot.
            // Child view models only project that snapshot into observable UI state.
            var snapshot = analyticsService.CreateSnapshot(
                filteredGames,
                filteredSessions,
                new AnalyticsQuery
                {
                    RangePreset = Filter.SelectedRangeOption.Value,
                    AggregationPeriod = Filter.SelectedAggregationOption.Value,
                    RankingMetric = Filter.SelectedRankingMetricOption.Value,
                    CustomStartDate = Filter.CustomStartDate,
                    CustomEndDate = Filter.CustomEndDate,
                    UseIsoWeekStart = settings.Settings.UseIsoWeekStart,
                    TopGames = settings.Settings.TopGames
                });
            Metrics.Apply(snapshot, allGames);
            Distribution.Apply(snapshot);
            Drilldown.ResetContext(filteredGames, filteredSessions);
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
    }
}
