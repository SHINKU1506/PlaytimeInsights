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

namespace PlaytimeInsights.ViewModels
{
    public sealed class DashboardFilterViewModel : ObservableObject
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly SessionQueryService queryService;
        private readonly Action<DashboardRefreshReason> refreshRequested;
        private SelectionOption<DateRangePreset> selectedRangeOption;
        private SelectionOption<AggregationPeriod> selectedAggregationOption;
        private SelectionOption<RankingMetric> selectedRankingMetricOption;
        private SelectionOption<MetadataFilterDimension?> selectedMetadataDimensionOption;
        private SelectionOption<string> selectedMetadataValueOption;
        private bool suppressRefresh;
        private DateTime customStartDate;
        private DateTime customEndDate = DateTime.Today;

        public DashboardFilterViewModel(
            IPlayniteAPI playniteApi,
            SessionQueryService queryService,
            int recentDays,
            Action<DashboardRefreshReason> refreshRequested)
        {
            this.playniteApi = playniteApi;
            this.queryService = queryService;
            this.refreshRequested = refreshRequested;
            customStartDate = DateTime.Today.AddDays(
                -(Math.Max(1, Math.Min(366, recentDays)) - 1));

            RangeOptions = new ObservableCollection<SelectionOption<DateRangePreset>>
            {
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.Today, Label = LocalizationService.Get("LOCPlaytimeInsightsToday", "今天") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.Last7Days, Label = LocalizationService.Get("LOCPlaytimeInsightsLast7Days", "近 7 天") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.Last30Days, Label = LocalizationService.Get("LOCPlaytimeInsightsLast30Days", "近 30 天") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisWeek, Label = LocalizationService.Get("LOCPlaytimeInsightsThisWeek", "本周") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisMonth, Label = LocalizationService.Get("LOCPlaytimeInsightsThisMonth", "本月") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.ThisYear, Label = LocalizationService.Get("LOCPlaytimeInsightsThisYear", "本年") },
                new SelectionOption<DateRangePreset> { Value = DateRangePreset.AllSessions, Label = LocalizationService.Get("LOCPlaytimeInsightsAllSessions", "全部记录") },
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
                    CreateDimensionOption(null, "LOCPlaytimeInsightsNoFilter", "不筛选"),
                    CreateDimensionOption(MetadataFilterDimension.Library, "LOCPlaytimeInsightsLibrary", "库来源"),
                    CreateDimensionOption(MetadataFilterDimension.Developer, "LOCPlaytimeInsightsDeveloper", "开发者"),
                    CreateDimensionOption(MetadataFilterDimension.Genre, "LOCPlaytimeInsightsGenre", "类型"),
                    CreateDimensionOption(MetadataFilterDimension.Tag, "LOCPlaytimeInsightsTag", "标签"),
                    CreateDimensionOption(MetadataFilterDimension.InstallationStatus, "LOCPlaytimeInsightsInstallationStatus", "安装状态")
                };
            MetadataValueOptions = new ObservableCollection<SelectionOption<string>>();
            selectedRangeOption = RangeOptions.First(
                option => option.Value == DateRangePreset.ThisMonth);
            selectedAggregationOption = AggregationOptions[0];
            selectedRankingMetricOption = RankingMetricOptions[0];
            selectedMetadataDimensionOption = MetadataDimensionOptions[0];
            RefreshMetadataValueOptions();
        }

        public ObservableCollection<SelectionOption<DateRangePreset>> RangeOptions { get; }

        public ObservableCollection<SelectionOption<AggregationPeriod>> AggregationOptions { get; }

        public ObservableCollection<SelectionOption<RankingMetric>> RankingMetricOptions { get; }

        public ObservableCollection<SelectionOption<MetadataFilterDimension?>> MetadataDimensionOptions { get; }

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
                    RequestRefresh(DashboardRefreshReason.Range);
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
                    RequestRefresh(DashboardRefreshReason.Aggregation);
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
                    RequestRefresh(DashboardRefreshReason.Ranking);
                }
            }
        }

        public SelectionOption<MetadataFilterDimension?> SelectedMetadataDimensionOption
        {
            get => selectedMetadataDimensionOption;
            set
            {
                if (!ReferenceEquals(selectedMetadataDimensionOption, value))
                {
                    SetValue(ref selectedMetadataDimensionOption, value);
                    OnPropertyChanged(nameof(MetadataValueVisibility));
                    OnPropertyChanged(nameof(ActiveMetadataFilterCount));
                    OnPropertyChanged(nameof(ActiveMetadataFilterSummary));
                    OnPropertyChanged(nameof(ActiveMetadataFilterVisibility));
                    RequestRefresh(DashboardRefreshReason.MetadataDimension);
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
                    OnPropertyChanged(nameof(ActiveMetadataFilterCount));
                    OnPropertyChanged(nameof(ActiveMetadataFilterSummary));
                    OnPropertyChanged(nameof(ActiveMetadataFilterVisibility));
                    RequestRefresh(DashboardRefreshReason.MetadataValue);
                }
            }
        }

        public Visibility MetadataValueVisibility =>
            SelectedMetadataDimensionOption?.Value.HasValue == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        public int ActiveMetadataFilterCount =>
            SelectedMetadataDimensionOption?.Value.HasValue == true &&
            !string.IsNullOrWhiteSpace(SelectedMetadataValueOption?.Value)
                ? 1
                : 0;

        public string ActiveMetadataFilterSummary =>
            ActiveMetadataFilterCount == 0
                ? string.Empty
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsActiveFilterCountFormat",
                    "Active ({0})",
                    ActiveMetadataFilterCount);

        public Visibility ActiveMetadataFilterVisibility =>
            ActiveMetadataFilterCount == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

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
                        RequestRefresh(DashboardRefreshReason.Range);
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
                        RequestRefresh(DashboardRefreshReason.Range);
                    }
                }
            }
        }

        public bool IsComplete =>
            SelectedRangeOption != null &&
            SelectedAggregationOption != null &&
            SelectedRankingMetricOption != null &&
            SelectedMetadataDimensionOption != null;

        public void SelectRange(DateRangePreset preset)
        {
            var option = RangeOptions.FirstOrDefault(
                value => value.Value == preset);
            if (option != null &&
                !ReferenceEquals(option, SelectedRangeOption))
            {
                SelectedRangeOption = option;
            }
        }

        public void RefreshMetadataValueOptions(
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            RefreshMetadataValueOptions(null, libraryNames);
        }

        public void RefreshMetadataValueOptions(
            IEnumerable<Game> games,
            IReadOnlyDictionary<Guid, string> libraryNames)
        {
            if (SelectedMetadataDimensionOption == null)
            {
                return;
            }

            var previousValue = SelectedMetadataValueOption?.Value ?? string.Empty;
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
                var availableGames = games ??
                    playniteApi?.Database?.Games ??
                    Enumerable.Empty<Game>();
                values.AddRange(queryService.GetMetadataValues(
                        availableGames,
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
            suppressRefresh = true;
            try
            {
                Replace(MetadataValueOptions, values);
                SelectedMetadataValueOption = selected;
            }
            finally
            {
                suppressRefresh = false;
            }
        }

        public IReadOnlyDictionary<Guid, string> GetLibraryNames()
        {
            return playniteApi.Addons.Plugins
                .OfType<LibraryPlugin>()
                .GroupBy(plugin => plugin.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name ?? string.Empty);
        }

        private void RequestRefresh(DashboardRefreshReason reason)
        {
            if (!suppressRefresh)
            {
                refreshRequested?.Invoke(reason);
            }
        }

        private static SelectionOption<MetadataFilterDimension?> CreateDimensionOption(
            MetadataFilterDimension? value,
            string resourceKey,
            string fallback)
        {
            return new SelectionOption<MetadataFilterDimension?>
            {
                Value = value,
                Label = LocalizationService.Get(resourceKey, fallback)
            };
        }

        private static void Replace<T>(
            ObservableCollection<T> target,
            IEnumerable<T> values)
        {
            target.Clear();
            foreach (var value in values)
            {
                target.Add(value);
            }
        }
    }
}
