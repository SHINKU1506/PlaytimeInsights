using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlaytimeInsights.Services
{
    public enum DateRangePreset
    {
        Today,
        Last7Days,
        Last30Days,
        ThisWeek,
        ThisMonth,
        ThisYear,
        AllSessions,
        Custom
    }

    public enum AggregationPeriod
    {
        Auto,
        Day,
        Week,
        Month,
        Year
    }

    public enum RankingMetric
    {
        Duration,
        SessionCount,
        ActiveDays,
        AverageSession,
        LongestSession
    }

    public sealed class AnalyticsQuery
    {
        public DateRangePreset RangePreset { get; set; } = DateRangePreset.ThisMonth;

        public AggregationPeriod AggregationPeriod { get; set; } = AggregationPeriod.Auto;

        public RankingMetric RankingMetric { get; set; } = RankingMetric.Duration;

        public DateTime CustomStartDate { get; set; } = DateTime.Today.AddDays(-29);

        public DateTime CustomEndDate { get; set; } = DateTime.Today;

        public bool UseIsoWeekStart { get; set; } = true;

        public int TopGames { get; set; } = 10;
    }

    public sealed class AnalyticsDateRange
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Label { get; set; }
    }

    public sealed class AnalyticsService
    {
        private readonly DailyAllocationService dailyAllocationService = new DailyAllocationService();
        private readonly AdvancedAnalyticsService advancedAnalyticsService =
            new AdvancedAnalyticsService();

        public DashboardSnapshot CreateSnapshot(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            AnalyticsQuery query)
        {
            return CreateSnapshotWithContext(games, sessions, query).Snapshot;
        }

        public DashboardSnapshotResult CreateSnapshotWithContext(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            AnalyticsQuery query)
        {
            var gameList = games == null ? new List<Game>() : games.ToList();
            var sessionList = sessions == null ? new List<GameSession>() : sessions.ToList();
            query = query ?? new AnalyticsQuery();
            var topGames = Math.Max(1, Math.Min(50, query.TopGames));
            DateTime? allSessionsStartDate = null;
            if (query.RangePreset == DateRangePreset.AllSessions)
            {
                allSessionsStartDate = sessionList
                    .Where(session =>
                        session != null &&
                        !session.IsDeleted &&
                        session.ElapsedSeconds > 0)
                    .Select(session => (DateTime?)session.GetStartedLocalDate())
                    .OrderBy(date => date)
                    .FirstOrDefault();
            }

            var range = ResolveDateRange(
                query,
                DateTime.Today,
                allSessionsStartDate);
            var firstDayOfWeek = GetFirstDayOfWeek(query.UseIsoWeekStart);

            var lifetimeSeconds = gameList.Aggregate<Game, ulong>(0, (current, game) =>
                current + game.Playtime);
            var trackedSeconds = sessionList.Aggregate<GameSession, ulong>(0, (current, session) =>
                current + session.ElapsedSeconds);

            var dailySeconds = new Dictionary<DateTime, ulong>();
            var dailyGameNames = new Dictionary<DateTime, HashSet<string>>();
            var gameStats = new Dictionary<Guid, MutableGameRangeStats>();
            var rangeSessionCount = 0;
            ulong rangeSeconds = 0;
            ulong longestSessionSeconds = 0;

            var currentGameNames = gameList
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First().Name ?? string.Empty);

            foreach (var session in sessionList)
            {
                var allocations = dailyAllocationService.SplitByLocalDay(session);
                var includedAllocations = allocations
                    .Where(allocation =>
                        allocation.Key.Date >= range.StartDate &&
                        allocation.Key.Date <= range.EndDate &&
                        allocation.Value > 0)
                    .ToList();
                var includedSeconds = includedAllocations.Aggregate<KeyValuePair<DateTime, ulong>, ulong>(
                    0,
                    (current, allocation) => current + allocation.Value);
                if (includedSeconds == 0)
                {
                    continue;
                }

                rangeSeconds += includedSeconds;
                rangeSessionCount++;
                longestSessionSeconds = Math.Max(longestSessionSeconds, includedSeconds);

                MutableGameRangeStats stats;
                if (!gameStats.TryGetValue(session.GameId, out stats))
                {
                    string currentName;
                    currentGameNames.TryGetValue(session.GameId, out currentName);
                    stats = new MutableGameRangeStats
                    {
                        GameId = session.GameId,
                        Name = string.IsNullOrWhiteSpace(currentName)
                            ? session.GameName
                            : currentName
                    };
                    gameStats[session.GameId] = stats;
                }

                stats.Seconds += includedSeconds;
                stats.SessionCount++;
                stats.LongestSessionSeconds = Math.Max(stats.LongestSessionSeconds, includedSeconds);

                foreach (var allocation in includedAllocations)
                {
                    Add(dailySeconds, allocation.Key.Date, allocation.Value);
                    stats.ActiveDates.Add(allocation.Key.Date);
                    HashSet<string> names;
                    if (!dailyGameNames.TryGetValue(allocation.Key.Date, out names))
                    {
                        names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                        dailyGameNames[allocation.Key.Date] = names;
                    }
                    if (!string.IsNullOrWhiteSpace(stats.Name))
                    {
                        names.Add(stats.Name);
                    }
                }
            }

            var context = new DashboardAnalysisContext
            {
                RangePreset = query.RangePreset,
                Range = new AnalyticsDateRange
                {
                    StartDate = range.StartDate,
                    EndDate = range.EndDate,
                    Label = range.Label
                },
                FirstDayOfWeek = firstDayOfWeek,
                DailySeconds = new Dictionary<DateTime, ulong>(dailySeconds),
                DailyGameNames = dailyGameNames.ToDictionary(
                    item => item.Key,
                    item => (IList<string>)item.Value
                        .OrderBy(name => name, StringComparer.CurrentCulture)
                        .ToList()),
                GameStatistics = gameStats.Values.Select(stats =>
                    new DashboardGameRangeStatistics
                    {
                        GameId = stats.GameId,
                        Name = stats.Name,
                        Seconds = stats.Seconds,
                        SessionCount = stats.SessionCount,
                        ActiveDates = stats.ActiveDates.OrderBy(date => date).ToList(),
                        LongestSessionSeconds = stats.LongestSessionSeconds
                    }).ToList()
            };
            var trend = CreateTrendProjection(context, query.AggregationPeriod);
            int heatmapColumnCount;
            var heatmapCells = CreateHeatmapCells(
                dailySeconds,
                range,
                firstDayOfWeek,
                out heatmapColumnCount);
            var ranking = CreateRankingProjection(context, query.RankingMetric, topGames);
            var lifetimeRankings = CreateLifetimeRankings(gameList, topGames);
            var activeDays = dailySeconds.Count(item => item.Value > 0);
            var averageSessionSeconds = rangeSessionCount == 0
                ? 0UL
                : rangeSeconds / (ulong)rangeSessionCount;
            var advanced = advancedAnalyticsService.CreateSnapshot(
                gameList,
                sessionList,
                range,
                firstDayOfWeek,
                dailySeconds,
                query.RangePreset);

            var snapshot = new DashboardSnapshot
            {
                LifetimeDurationText = FormatDuration(lifetimeSeconds),
                LifetimeDurationDisplay = CreateDurationDisplay(lifetimeSeconds),
                TrackedDurationText = FormatDurationPrecise(trackedSeconds),
                RangeDurationText = FormatDurationPrecise(rangeSeconds),
                RangeDurationDisplay = CreateDurationDisplay(rangeSeconds),
                SessionCountText = rangeSessionCount.ToString("N0"),
                ActiveDaysText = activeDays.ToString("N0"),
                AverageSessionText = FormatDurationPrecise(averageSessionSeconds),
                AverageSessionDisplay = CreateDurationDisplay(averageSessionSeconds),
                LongestSessionText = FormatDurationPrecise(longestSessionSeconds),
                LongestSessionDisplay = CreateDurationDisplay(longestSessionSeconds),
                RangeText = LocalizationService.Format(
                    "LOCPlaytimeInsightsPreciseRangeFormat",
                    "{0} · 精确会话",
                    range.Label),
                PeriodTitleText = trend.PeriodTitleText,
                RangeRankingTitleText = ranking.RangeRankingTitleText,
                StatusText = rangeSessionCount == 0
                    ? LocalizationService.Get(
                        "LOCPlaytimeInsightsNoRangeSessions",
                        "所选范围内没有精确会话；Playnite 累计数据仍保留在下方累计排名中。")
                    : LocalizationService.Format(
                        "LOCPlaytimeInsightsRangeStatusFormat",
                        "所选范围包含 {0:N0} 个游戏、{1:N0} 次会话 · 最近刷新 {2:HH:mm:ss}",
                        gameStats.Count,
                        rangeSessionCount,
                        DateTime.Now),
                PeriodActivities = trend.PeriodActivities,
                HeatmapCells = heatmapCells,
                HeatmapWeekdayLabels = WeekdayLabelService.CreateLabels(
                    firstDayOfWeek),
                HeatmapColumnCount = heatmapColumnCount,
                TrendChartWidth = trend.TrendChartWidth,
                TrendLinePoints = trend.TrendLinePoints,
                TrendLineGeometry = trend.TrendLineGeometry,
                TrendAreaGeometry = trend.TrendAreaGeometry,
                TrendPoints = trend.TrendPoints,
                RangeGameRankings = ranking.RangeGameRankings,
                LifetimeGameRankings = lifetimeRankings,
                Advanced = advanced
            };

            return new DashboardSnapshotResult
            {
                Snapshot = snapshot,
                Context = context
            };
        }

        public DashboardTrendProjection CreateTrendProjection(
            DashboardAnalysisContext context,
            AggregationPeriod aggregationPeriod)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var query = new AnalyticsQuery
            {
                RangePreset = context.RangePreset,
                AggregationPeriod = aggregationPeriod
            };
            var effectiveAggregationPeriod = ResolveAggregationPeriod(
                query,
                context.Range);
            var periodActivities = CreatePeriodActivities(
                context.DailySeconds,
                context.Range,
                effectiveAggregationPeriod,
                context.FirstDayOfWeek);
            ApplyPeriodGameSummaries(periodActivities, context.DailyGameNames);
            double trendChartWidth;
            PointCollection trendLinePoints;
            Geometry trendLineGeometry;
            Geometry trendAreaGeometry;
            var trendPoints = CreateTrendPoints(
                periodActivities,
                out trendChartWidth,
                out trendLinePoints,
                out trendLineGeometry,
                out trendAreaGeometry);

            return new DashboardTrendProjection
            {
                PeriodTitleText = LocalizationService.Format(
                    "LOCPlaytimeInsightsAggregationTitleFormat",
                    "{0}聚合{1}",
                    GetAggregationLabel(effectiveAggregationPeriod),
                    aggregationPeriod == AggregationPeriod.Auto
                        ? LocalizationService.Get(
                            "LOCPlaytimeInsightsAutomaticSuffix",
                            " · 自动")
                        : string.Empty),
                PeriodActivities = periodActivities,
                TrendChartWidth = trendChartWidth,
                TrendLinePoints = trendLinePoints,
                TrendLineGeometry = trendLineGeometry,
                TrendAreaGeometry = trendAreaGeometry,
                TrendPoints = trendPoints
            };
        }

        public DashboardRankingProjection CreateRankingProjection(
            DashboardAnalysisContext context,
            RankingMetric rankingMetric,
            int topGames)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            topGames = Math.Max(1, Math.Min(50, topGames));
            return new DashboardRankingProjection
            {
                RangeRankingTitleText = LocalizationService.Format(
                    "LOCPlaytimeInsightsRangeRankingTitleFormat",
                    "区间游戏排名 · {0}",
                    GetRankingMetricLabel(rankingMetric)),
                RangeGameRankings = CreateRangeRankings(
                    context.GameStatistics,
                    rankingMetric,
                    topGames)
            };
        }

        public IList<SessionDetailViewModel> CreateSessionDetails(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            DateTime startDate,
            DateTime endDate)
        {
            startDate = startDate.Date;
            endDate = endDate.Date;
            if (endDate < startDate)
            {
                var swap = startDate;
                startDate = endDate;
                endDate = swap;
            }

            var gameNames = (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First().Name ?? string.Empty);
            var values = new List<Tuple<DateTime, SessionDetailViewModel>>();
            foreach (var session in sessions ?? Enumerable.Empty<GameSession>())
            {
                var includedSeconds = dailyAllocationService.SplitByLocalDay(session)
                    .Where(allocation =>
                        allocation.Key.Date >= startDate &&
                        allocation.Key.Date <= endDate)
                    .Aggregate<KeyValuePair<DateTime, ulong>, ulong>(
                        0,
                        (current, allocation) => current + allocation.Value);
                if (includedSeconds == 0)
                {
                    continue;
                }

                string currentName;
                gameNames.TryGetValue(session.GameId, out currentName);
                var startedUtc = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc);
                var localStarted = new DateTimeOffset(startedUtc)
                    .ToOffset(TimeSpan.FromMinutes(session.StartUtcOffsetMinutes))
                    .DateTime;
                values.Add(Tuple.Create(
                    session.StartedAtUtc,
                    new SessionDetailViewModel
                    {
                        GameId = session.GameId,
                        GameName = string.IsNullOrWhiteSpace(currentName)
                            ? session.GameName
                            : currentName,
                        StartedText = localStarted.ToString("yyyy/M/d HH:mm"),
                        DurationText = FormatDurationPrecise(includedSeconds),
                        Source = session.Source,
                        SourceText = GetSessionSourceLabel(session.Source)
                    }));
            }

            return values
                .OrderByDescending(value => value.Item1)
                .Select(value => value.Item2)
                .ToList();
        }

        public static AnalyticsDateRange ResolveDateRange(
            AnalyticsQuery query,
            DateTime today,
            DateTime? allSessionsStartDate = null)
        {
            query = query ?? new AnalyticsQuery();
            today = today.Date;
            DateTime start;
            DateTime end;
            string label;

            switch (query.RangePreset)
            {
                case DateRangePreset.Today:
                    start = today;
                    end = today;
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsTodayRangeFormat",
                        "今天 · {0:yyyy/M/d}",
                        today);
                    break;
                case DateRangePreset.Last7Days:
                    start = today.AddDays(-6);
                    end = today;
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsLast7DaysRangeFormat",
                        "近 7 天 · {0:M/d}–{1:M/d}",
                        start,
                        end);
                    break;
                case DateRangePreset.Last30Days:
                    start = today.AddDays(-29);
                    end = today;
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsLast30DaysRangeFormat",
                        "近 30 天 · {0:M/d}–{1:M/d}",
                        start,
                        end);
                    break;
                case DateRangePreset.ThisWeek:
                    var firstDayOfWeek = GetFirstDayOfWeek(query.UseIsoWeekStart);
                    start = StartOfWeek(today, firstDayOfWeek);
                    end = start.AddDays(6);
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsWeekRangeFormat",
                        "本周 · {0:M/d}–{1:M/d}",
                        start,
                        end);
                    break;
                case DateRangePreset.ThisYear:
                    start = new DateTime(today.Year, 1, 1);
                    end = new DateTime(today.Year, 12, 31);
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsYearRangeFormat",
                        "{0} 年",
                        today.Year);
                    break;
                case DateRangePreset.AllSessions:
                    start = allSessionsStartDate.HasValue
                        ? allSessionsStartDate.Value.Date
                        : today;
                    if (start > today)
                    {
                        start = today;
                    }

                    end = today;
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsAllSessionsRangeFormat",
                        "全部记录 · {0:yyyy/M/d}–{1:yyyy/M/d}",
                        start,
                        end);
                    break;
                case DateRangePreset.Custom:
                    start = query.CustomStartDate.Date;
                    end = query.CustomEndDate.Date;
                    if (end < start)
                    {
                        var swap = start;
                        start = end;
                        end = swap;
                    }

                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsCustomRangeFormat",
                        "自定义 · {0:yyyy/M/d}–{1:yyyy/M/d}",
                        start,
                        end);
                    break;
                case DateRangePreset.ThisMonth:
                default:
                    start = new DateTime(today.Year, today.Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    label = LocalizationService.Format(
                        "LOCPlaytimeInsightsMonthRangeFormat",
                        "{0:yyyy 年 M 月}",
                        today);
                    break;
            }

            return new AnalyticsDateRange
            {
                StartDate = start,
                EndDate = end,
                Label = label
            };
        }

        public static AggregationPeriod ResolveAggregationPeriod(
            AnalyticsQuery query,
            AnalyticsDateRange range)
        {
            query = query ?? new AnalyticsQuery();
            if (query.AggregationPeriod != AggregationPeriod.Auto)
            {
                return query.AggregationPeriod;
            }

            switch (query.RangePreset)
            {
                case DateRangePreset.Last7Days:
                case DateRangePreset.Last30Days:
                    return AggregationPeriod.Day;
                case DateRangePreset.ThisYear:
                    return AggregationPeriod.Month;
                case DateRangePreset.AllSessions:
                case DateRangePreset.Custom:
                    var totalDays = Math.Max(
                        1,
                        (int)(range.EndDate.Date - range.StartDate.Date).TotalDays + 1);
                    if (totalDays <= 62)
                    {
                        return AggregationPeriod.Day;
                    }

                    if (totalDays <= 730)
                    {
                        return AggregationPeriod.Week;
                    }

                    return totalDays <= 3650
                        ? AggregationPeriod.Month
                        : AggregationPeriod.Year;
                case DateRangePreset.Today:
                case DateRangePreset.ThisWeek:
                case DateRangePreset.ThisMonth:
                default:
                    return AggregationPeriod.Day;
            }
        }

        public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
        {
            var difference = (7 + (date.DayOfWeek - firstDayOfWeek)) % 7;
            return date.Date.AddDays(-difference);
        }

        public static string FormatDuration(ulong seconds)
        {
            var totalMinutes = seconds == 0
                ? 0UL
                : Math.Max(1UL, (ulong)Math.Round(
                    seconds / 60d,
                    MidpointRounding.AwayFromZero));
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;

            if (hours == 0)
            {
                return LocalizationService.Format(
                    "LOCPlaytimeInsightsMinutesFormat",
                    "{0} 分钟",
                    minutes);
            }

            return minutes == 0
                ? LocalizationService.Format(
                    "LOCPlaytimeInsightsHoursFormat",
                    "{0:N0} 小时",
                    hours)
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsHoursMinutesFormat",
                    "{0:N0} 小时 {1} 分",
                    hours,
                    minutes);
        }

        public static string FormatDurationPrecise(ulong seconds)
        {
            if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return remainingSeconds == 0
                    ? LocalizationService.Format(
                        "LOCPlaytimeInsightsMinutesFormat",
                        "{0} 分钟",
                        minutes)
                    : LocalizationService.Format(
                        "LOCPlaytimeInsightsMinutesSecondsFormat",
                        "{0} 分 {1} 秒",
                        minutes,
                        remainingSeconds);
            }

            return FormatDuration(seconds);
        }

        public static DurationDisplayViewModel CreateDurationDisplay(ulong seconds)
        {
            if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return new DurationDisplayViewModel(
                    minutes.ToString("N0"),
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsMinuteUnitShort",
                        "分"),
                    remainingSeconds == 0
                        ? string.Empty
                        : remainingSeconds.ToString("N0"),
                    remainingSeconds == 0
                        ? string.Empty
                        : LocalizationService.Get(
                            "LOCPlaytimeInsightsSecondUnitShort",
                            "秒"),
                    FormatDurationPrecise(seconds));
            }

            var totalMinutes = Math.Max(
                1UL,
                (ulong)Math.Round(
                    seconds / 60d,
                    MidpointRounding.AwayFromZero));
            var hours = totalMinutes / 60;
            var minutesPart = totalMinutes % 60;
            return new DurationDisplayViewModel(
                hours.ToString("N0"),
                LocalizationService.Get(
                    "LOCPlaytimeInsightsHourUnitShort",
                    "小时"),
                minutesPart == 0 ? string.Empty : minutesPart.ToString("N0"),
                minutesPart == 0
                    ? string.Empty
                    : LocalizationService.Get(
                        "LOCPlaytimeInsightsMinuteUnitShort",
                        "分"),
                FormatDurationPrecise(seconds));
        }

        private static DayOfWeek GetFirstDayOfWeek(bool useIsoWeekStart)
        {
            return useIsoWeekStart
                ? DayOfWeek.Monday
                : CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        }

        private static IList<PeriodActivityViewModel> CreatePeriodActivities(
            IDictionary<DateTime, ulong> dailySeconds,
            AnalyticsDateRange range,
            AggregationPeriod period,
            DayOfWeek firstDayOfWeek)
        {
            var values = new List<PeriodActivityViewModel>();
            var periodSeconds = new Dictionary<DateTime, ulong>();
            foreach (var daily in dailySeconds)
            {
                Add(periodSeconds, GetPeriodStart(daily.Key, period, firstDayOfWeek), daily.Value);
            }

            var cursor = GetPeriodStart(range.StartDate, period, firstDayOfWeek);
            var finalPeriod = GetPeriodStart(range.EndDate, period, firstDayOfWeek);
            ulong maximumSeconds = 0;
            while (cursor <= finalPeriod)
            {
                ulong seconds;
                periodSeconds.TryGetValue(cursor, out seconds);
                maximumSeconds = Math.Max(maximumSeconds, seconds);
                values.Add(new PeriodActivityViewModel
                {
                    PeriodStart = cursor < range.StartDate ? range.StartDate : cursor,
                    PeriodEnd = MinDate(AddPeriod(cursor, period).AddDays(-1), range.EndDate),
                    Label = FormatPeriodLabel(cursor, period),
                    DurationText = FormatDurationPrecise(seconds),
                    HoverDurationText = LocalizationService.Format(
                        "LOCPlaytimeInsightsTrendTotalDurationFormat",
                        "共 {0}",
                        FormatDurationPrecise(seconds)),
                    TooltipText = LocalizationService.Format(
                        "LOCPlaytimeInsightsChartTooltipFormat",
                        "{0}：{1}（点击查看会话）",
                        FormatPeriodLabel(cursor, period),
                        FormatDurationPrecise(seconds)),
                    Seconds = seconds
                });
                cursor = AddPeriod(cursor, period);
            }

            foreach (var value in values)
            {
                value.BarHeight = maximumSeconds == 0
                    ? 4
                    : 8 + (double)value.Seconds / maximumSeconds * 132;
            }

            return values;
        }

        private static IList<HeatmapCellViewModel> CreateHeatmapCells(
            IDictionary<DateTime, ulong> dailySeconds,
            AnalyticsDateRange range,
            DayOfWeek firstDayOfWeek,
            out int columnCount)
        {
            var firstWeek = StartOfWeek(range.StartDate, firstDayOfWeek);
            var lastWeek = StartOfWeek(range.EndDate, firstDayOfWeek);
            columnCount = Math.Max(1, (int)((lastWeek - firstWeek).TotalDays / 7) + 1);
            var maximumSeconds = dailySeconds.Count == 0
                ? 0UL
                : dailySeconds.Max(item => item.Value);
            var values = new List<HeatmapCellViewModel>(columnCount * 7);

            for (var row = 0; row < 7; row++)
            {
                for (var column = 0; column < columnCount; column++)
                {
                    var date = firstWeek.AddDays(column * 7 + row);
                    var inRange = date >= range.StartDate && date <= range.EndDate;
                    ulong seconds = 0;
                    if (inRange)
                    {
                        dailySeconds.TryGetValue(date, out seconds);
                    }

                    values.Add(new HeatmapCellViewModel
                    {
                        Date = date,
                        Seconds = seconds,
                        CellVisibility = inRange ? Visibility.Visible : Visibility.Hidden,
                        HeatOpacity = seconds == 0 || maximumSeconds == 0
                            ? 0.08
                            : 0.18 + (double)seconds / maximumSeconds * 0.82,
                        TooltipText = inRange
                            ? LocalizationService.Format(
                                "LOCPlaytimeInsightsChartTooltipFormat",
                                "{0:yyyy/M/d}：{1}（点击查看会话）",
                                date,
                                FormatDurationPrecise(seconds))
                            : string.Empty
                    });
                }
            }

            return values;
        }

        private static void ApplyPeriodGameSummaries(
            IList<PeriodActivityViewModel> periods,
            IDictionary<DateTime, IList<string>> dailyGameNames)
        {
            foreach (var period in periods)
            {
                var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                for (var date = period.PeriodStart.Date;
                    date <= period.PeriodEnd.Date;
                    date = date.AddDays(1))
                {
                    IList<string> dailyNames;
                    if (dailyGameNames.TryGetValue(date, out dailyNames))
                    {
                        names.UnionWith(dailyNames);
                    }
                }

                var ordered = names.OrderBy(name => name, StringComparer.CurrentCulture)
                    .ToList();
                period.GameSummaryText = ordered.Count == 0
                    ? LocalizationService.Get(
                        "LOCPlaytimeInsightsNoGamesInPeriod",
                        "无游戏")
                    : string.Join(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsGameListSeparator",
                            "、"),
                        ordered.Take(3)) +
                        (ordered.Count > 3
                            ? LocalizationService.Get(
                                "LOCPlaytimeInsightsGameListMoreSuffix",
                                " 等")
                            : string.Empty);
            }
        }

        private static IList<TrendPointViewModel> CreateTrendPoints(
            IList<PeriodActivityViewModel> periods,
            out double chartWidth,
            out PointCollection linePoints,
            out Geometry lineGeometry,
            out Geometry areaGeometry)
        {
            const double horizontalStep = 74;
            const double chartBottom = 150;
            const double chartRange = 120;
            chartWidth = Math.Max(640, periods.Count * horizontalStep);
            linePoints = new PointCollection();
            var values = new List<TrendPointViewModel>();
            var maximumSeconds = periods.Count == 0
                ? 0UL
                : periods.Max(period => period.Seconds);

            for (var index = 0; index < periods.Count; index++)
            {
                var period = periods[index];
                var x = horizontalStep / 2 + index * horizontalStep;
                var y = maximumSeconds == 0
                    ? chartBottom
                    : chartBottom - (double)period.Seconds / maximumSeconds * chartRange;
                linePoints.Add(new Point(x, y));
                values.Add(new TrendPointViewModel
                {
                    Period = period,
                    CanvasLeft = x - 5,
                    CanvasTop = y - 5,
                    TooltipText = period.TooltipText
                });
            }

            CreateSmoothTrendGeometries(
                linePoints.Cast<Point>().ToList(),
                chartBottom,
                out lineGeometry,
                out areaGeometry);
            return values;
        }

        private static void CreateSmoothTrendGeometries(
            IList<Point> points,
            double baseline,
            out Geometry lineGeometry,
            out Geometry areaGeometry)
        {
            if (points.Count == 0)
            {
                lineGeometry = Geometry.Empty;
                areaGeometry = Geometry.Empty;
                return;
            }

            if (points.Count == 1)
            {
                var point = points[0];
                var lineFigure = new PathFigure
                {
                    StartPoint = new Point(point.X - 1, point.Y),
                    IsClosed = false,
                    IsFilled = false
                };
                lineFigure.Segments.Add(new LineSegment(
                    new Point(point.X + 1, point.Y),
                    true));
                var singleLine = new PathGeometry(new[] { lineFigure });

                var areaFigure = new PathFigure
                {
                    StartPoint = new Point(point.X - 1, baseline),
                    IsClosed = true,
                    IsFilled = true
                };
                areaFigure.Segments.Add(new LineSegment(
                    new Point(point.X - 1, point.Y),
                    true));
                areaFigure.Segments.Add(new LineSegment(
                    new Point(point.X + 1, point.Y),
                    true));
                areaFigure.Segments.Add(new LineSegment(
                    new Point(point.X + 1, baseline),
                    true));
                var singleArea = new PathGeometry(new[] { areaFigure });
                FreezeGeometry(singleLine);
                FreezeGeometry(singleArea);
                lineGeometry = singleLine;
                areaGeometry = singleArea;
                return;
            }

            var tangents = CreateMonotoneTangents(points);
            var lineFigureSmooth = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = false,
                IsFilled = false
            };
            var areaFigureSmooth = new PathFigure
            {
                StartPoint = new Point(points[0].X, baseline),
                IsClosed = true,
                IsFilled = true
            };
            areaFigureSmooth.Segments.Add(new LineSegment(points[0], true));

            for (var index = 0; index < points.Count - 1; index++)
            {
                var current = points[index];
                var next = points[index + 1];
                var width = next.X - current.X;
                var control1 = new Point(
                    current.X + width / 3,
                    current.Y + tangents[index] * width / 3);
                var control2 = new Point(
                    next.X - width / 3,
                    next.Y - tangents[index + 1] * width / 3);
                lineFigureSmooth.Segments.Add(new BezierSegment(
                    control1,
                    control2,
                    next,
                    true));
                areaFigureSmooth.Segments.Add(new BezierSegment(
                    control1,
                    control2,
                    next,
                    true));
            }

            areaFigureSmooth.Segments.Add(new LineSegment(
                new Point(points[points.Count - 1].X, baseline),
                true));
            var smoothLine = new PathGeometry(new[] { lineFigureSmooth });
            var smoothArea = new PathGeometry(new[] { areaFigureSmooth });
            FreezeGeometry(smoothLine);
            FreezeGeometry(smoothArea);
            lineGeometry = smoothLine;
            areaGeometry = smoothArea;
        }

        private static double[] CreateMonotoneTangents(IList<Point> points)
        {
            var segmentCount = points.Count - 1;
            var slopes = new double[segmentCount];
            var tangents = new double[points.Count];
            for (var index = 0; index < segmentCount; index++)
            {
                slopes[index] =
                    (points[index + 1].Y - points[index].Y) /
                    (points[index + 1].X - points[index].X);
            }

            tangents[0] = slopes[0];
            tangents[tangents.Length - 1] = slopes[slopes.Length - 1];
            for (var index = 1; index < tangents.Length - 1; index++)
            {
                var previous = slopes[index - 1];
                var next = slopes[index];
                tangents[index] =
                    previous == 0 ||
                    next == 0 ||
                    Math.Sign(previous) != Math.Sign(next)
                        ? 0
                        : 2 * previous * next / (previous + next);
            }

            for (var index = 0; index < segmentCount; index++)
            {
                if (slopes[index] == 0)
                {
                    tangents[index] = 0;
                    tangents[index + 1] = 0;
                    continue;
                }

                var alpha = tangents[index] / slopes[index];
                var beta = tangents[index + 1] / slopes[index];
                var magnitude = alpha * alpha + beta * beta;
                if (magnitude <= 9)
                {
                    continue;
                }

                var scale = 3 / Math.Sqrt(magnitude);
                tangents[index] = scale * alpha * slopes[index];
                tangents[index + 1] = scale * beta * slopes[index];
            }

            return tangents;
        }

        private static void FreezeGeometry(Geometry geometry)
        {
            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }
        }

        private static IList<GameRankingViewModel> CreateRangeRankings(
            IEnumerable<DashboardGameRangeStatistics> stats,
            RankingMetric metric,
            int topGames)
        {
            var allStats = stats.ToList();
            var totalDuration = allStats.Aggregate<DashboardGameRangeStatistics, decimal>(
                0,
                (current, item) => current + item.Seconds);
            var ranked = allStats
                .Select(item => new
                {
                    Stats = item,
                    Score = GetRankingScore(item, metric)
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Stats.Name)
                .Take(topGames)
                .ToList();

            return ranked.Select((item, index) => new GameRankingViewModel
            {
                GameId = item.Stats.GameId,
                Position = index + 1,
                Name = item.Stats.Name,
                PrimaryValueText = FormatRankingValue(item.Stats, metric),
                DetailText = LocalizationService.Format(
                    "LOCPlaytimeInsightsRankingDetailFormat",
                    "{0} · {1:N0} 次 · {2:N0} 个活跃日 · 平均 {3} · 最长 {4}",
                    FormatDurationPrecise(item.Stats.Seconds),
                    item.Stats.SessionCount,
                    item.Stats.ActiveDates.Count,
                    FormatDurationPrecise(item.Stats.AverageSessionSeconds),
                    FormatDurationPrecise(item.Stats.LongestSessionSeconds)),
                ProgressPercent = totalDuration == 0
                    ? 0
                    : (double)((decimal)item.Stats.Seconds / totalDuration * 100),
                ProgressTooltipText = LocalizationService.Format(
                    "LOCPlaytimeInsightsShareOfTotalFormat",
                    "占总游玩时长 {0:P1}",
                    totalDuration == 0
                        ? 0
                        : (double)((decimal)item.Stats.Seconds / totalDuration))
            }).ToList();
        }

        private static IList<GameRankingViewModel> CreateLifetimeRankings(
            IEnumerable<Game> games,
            int topGames)
        {
            var allPlayedGames = games
                .Where(game => game.Playtime > 0)
                .OrderByDescending(game => game.Playtime)
                .ThenBy(game => game.Name)
                .ToList();
            var totalDuration = allPlayedGames.Aggregate<Game, decimal>(
                0,
                (current, game) => current + game.Playtime);
            var ranked = allPlayedGames
                .Take(topGames)
                .ToList();

            return ranked.Select((game, index) => new GameRankingViewModel
            {
                GameId = game.Id,
                Position = index + 1,
                Name = game.Name,
                PrimaryValueText = FormatDuration(game.Playtime),
                DetailText = LocalizationService.Get(
                    "LOCPlaytimeInsightsPlayniteLifetimeBasis",
                    "Playnite 当前累计口径"),
                ProgressPercent = totalDuration == 0
                    ? 0
                    : (double)((decimal)game.Playtime / totalDuration * 100),
                ProgressTooltipText = LocalizationService.Format(
                    "LOCPlaytimeInsightsShareOfTotalFormat",
                    "占总游玩时长 {0:P1}",
                    totalDuration == 0
                        ? 0
                        : (double)((decimal)game.Playtime / totalDuration))
            }).ToList();
        }

        private static ulong GetRankingScore(
            DashboardGameRangeStatistics stats,
            RankingMetric metric)
        {
            switch (metric)
            {
                case RankingMetric.SessionCount:
                    return (ulong)stats.SessionCount;
                case RankingMetric.ActiveDays:
                    return (ulong)stats.ActiveDates.Count;
                case RankingMetric.AverageSession:
                    return stats.AverageSessionSeconds;
                case RankingMetric.LongestSession:
                    return stats.LongestSessionSeconds;
                case RankingMetric.Duration:
                default:
                    return stats.Seconds;
            }
        }

        private static string FormatRankingValue(
            DashboardGameRangeStatistics stats,
            RankingMetric metric)
        {
            switch (metric)
            {
                case RankingMetric.SessionCount:
                    return LocalizationService.Format(
                        "LOCPlaytimeInsightsCountTimesFormat",
                        "{0:N0} 次",
                        stats.SessionCount);
                case RankingMetric.ActiveDays:
                    return LocalizationService.Format(
                        "LOCPlaytimeInsightsCountDaysFormat",
                        "{0:N0} 天",
                        stats.ActiveDates.Count);
                case RankingMetric.AverageSession:
                    return FormatDurationPrecise(stats.AverageSessionSeconds);
                case RankingMetric.LongestSession:
                    return FormatDurationPrecise(stats.LongestSessionSeconds);
                case RankingMetric.Duration:
                default:
                    return FormatDurationPrecise(stats.Seconds);
            }
        }

        private static DateTime GetPeriodStart(
            DateTime date,
            AggregationPeriod period,
            DayOfWeek firstDayOfWeek)
        {
            switch (period)
            {
                case AggregationPeriod.Auto:
                    return date.Date;
                case AggregationPeriod.Week:
                    return StartOfWeek(date, firstDayOfWeek);
                case AggregationPeriod.Month:
                    return new DateTime(date.Year, date.Month, 1);
                case AggregationPeriod.Year:
                    return new DateTime(date.Year, 1, 1);
                case AggregationPeriod.Day:
                default:
                    return date.Date;
            }
        }

        private static DateTime AddPeriod(DateTime date, AggregationPeriod period)
        {
            switch (period)
            {
                case AggregationPeriod.Auto:
                    return date.AddDays(1);
                case AggregationPeriod.Week:
                    return date.AddDays(7);
                case AggregationPeriod.Month:
                    return date.AddMonths(1);
                case AggregationPeriod.Year:
                    return date.AddYears(1);
                case AggregationPeriod.Day:
                default:
                    return date.AddDays(1);
            }
        }

        private static string FormatPeriodLabel(DateTime date, AggregationPeriod period)
        {
            switch (period)
            {
                case AggregationPeriod.Auto:
                    return date.ToString("M/d");
                case AggregationPeriod.Week:
                    return string.Format("{0:M/d}–{1:M/d}", date, date.AddDays(6));
                case AggregationPeriod.Month:
                    return date.ToString("yyyy/M");
                case AggregationPeriod.Year:
                    return date.ToString("yyyy");
                case AggregationPeriod.Day:
                default:
                    return date.ToString("M/d");
            }
        }

        private static string GetAggregationLabel(AggregationPeriod period)
        {
            switch (period)
            {
                case AggregationPeriod.Auto:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsAuto",
                        "自动");
                case AggregationPeriod.Week:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsByWeek",
                        "按周");
                case AggregationPeriod.Month:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsByMonth",
                        "按月");
                case AggregationPeriod.Year:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsByYear",
                        "按年");
                case AggregationPeriod.Day:
                default:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsByDay",
                        "按日");
            }
        }

        private static string GetRankingMetricLabel(RankingMetric metric)
        {
            switch (metric)
            {
                case RankingMetric.SessionCount:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsSessionCount",
                        "会话次数");
                case RankingMetric.ActiveDays:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsActiveDays",
                        "活跃天数");
                case RankingMetric.AverageSession:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsAverageSessionOption",
                        "平均会话");
                case RankingMetric.LongestSession:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsLongestSessionOption",
                        "最长会话");
                case RankingMetric.Duration:
                default:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsDuration",
                        "游玩时长");
            }
        }

        private static string GetSessionSourceLabel(SessionSource source)
        {
            return SessionQueryService.GetSourceLabel(source);
        }

        private static DateTime MinDate(DateTime first, DateTime second)
        {
            return first <= second ? first : second;
        }

        private static void Add(IDictionary<DateTime, ulong> values, DateTime key, ulong seconds)
        {
            ulong existing;
            values.TryGetValue(key, out existing);
            values[key] = existing + seconds;
        }

        private sealed class MutableGameRangeStats
        {
            public Guid GameId { get; set; }

            public string Name { get; set; }

            public ulong Seconds { get; set; }

            public int SessionCount { get; set; }

            public HashSet<DateTime> ActiveDates { get; } = new HashSet<DateTime>();

            public ulong LongestSessionSeconds { get; set; }

            public ulong AverageSessionSeconds =>
                SessionCount == 0 ? 0UL : Seconds / (ulong)SessionCount;
        }
    }
}
