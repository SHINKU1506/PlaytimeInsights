using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace PlaytimeInsights.Services
{
    public sealed class AdvancedAnalyticsService
    {
        private readonly DailyAllocationService dailyAllocationService =
            new DailyAllocationService();
        private readonly HourlyAllocationService hourlyAllocationService =
            new HourlyAllocationService();

        public AdvancedAnalyticsSnapshot CreateSnapshot(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            AnalyticsDateRange range,
            DayOfWeek firstDayOfWeek,
            IDictionary<DateTime, ulong> rangeDailySeconds)
        {
            var gameList = (games ?? Enumerable.Empty<Game>()).ToList();
            var sessionList = (sessions ?? Enumerable.Empty<GameSession>()).ToList();
            var daily = rangeDailySeconds ??
                new Dictionary<DateTime, ulong>();
            var weekdayLabels = WeekdayLabelService.CreateLabels(
                firstDayOfWeek);
            var hourLabels = Enumerable.Range(0, 24)
                .Select(hour => hour.ToString("00"))
                .ToList();
            var weekdaySeconds = new ulong[7];
            foreach (var value in daily)
            {
                var index = GetWeekdayIndex(value.Key.DayOfWeek, firstDayOfWeek);
                weekdaySeconds[index] += value.Value;
            }

            var hourSeconds = new ulong[24];
            var weekHourSeconds = new ulong[7, 24];
            foreach (var session in sessionList)
            {
                foreach (var allocation in hourlyAllocationService
                    .SplitByLocalHour(session))
                {
                    if (allocation.LocalDate.Date < range.StartDate ||
                        allocation.LocalDate.Date > range.EndDate ||
                        allocation.Seconds == 0)
                    {
                        continue;
                    }

                    var weekdayIndex = GetWeekdayIndex(
                        allocation.LocalDate.DayOfWeek,
                        firstDayOfWeek);
                    hourSeconds[allocation.Hour] += allocation.Seconds;
                    weekHourSeconds[weekdayIndex, allocation.Hour] +=
                        allocation.Seconds;
                }
            }

            var activeDates = daily
                .Where(value => value.Value > 0)
                .Select(value => value.Key.Date)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            int longestStreak;
            int currentStreak;
            CalculateStreaks(
                activeDates,
                range,
                DateTime.Today,
                out longestStreak,
                out currentStreak);

            var previousRange = CreatePreviousPeriodRange(range);
            var previousSeconds = CalculateRangeSeconds(
                sessionList,
                previousRange);
            var yearRange = CreateYearOverYearRange(range);
            var yearSeconds = CalculateRangeSeconds(sessionList, yearRange);
            var currentSeconds = daily.Aggregate<KeyValuePair<DateTime, ulong>, ulong>(
                0,
                (current, value) => current + value.Value);
            var anomalies = CreateAnomalies(gameList, sessionList, range);

            return new AdvancedAnalyticsSnapshot
            {
                WeekdayDistribution = CreateDistribution(
                    weekdaySeconds,
                    weekdayLabels,
                    string.Empty),
                HourDistribution = CreateDistribution(
                    hourSeconds,
                    hourLabels.Select(label => label + ":00").ToList(),
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsHourPrefix",
                        "时段")),
                WeekHourCells = CreateWeekHourCells(
                    weekHourSeconds,
                    weekdayLabels,
                    hourLabels),
                WeekdayLabels = weekdayLabels,
                HourLabels = hourLabels,
                PreviousPeriodComparison = CreateComparison(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsPreviousPeriodComparison",
                        "环比 · 上一等长区间"),
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsPreviousPeriodShort",
                        "环比"),
                    currentSeconds,
                    previousSeconds,
                    previousRange),
                YearOverYearComparison = CreateComparison(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsYearOverYearComparison",
                        "同比 · 去年同期"),
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsYearOverYearShort",
                        "同比"),
                    currentSeconds,
                    yearSeconds,
                    yearRange),
                LongestStreakText = LocalizationService.Format(
                    "LOCPlaytimeInsightsCountDaysFormat",
                    "{0:N0} 天",
                    longestStreak),
                CurrentStreakText = LocalizationService.Format(
                    "LOCPlaytimeInsightsCountDaysFormat",
                    "{0:N0} 天",
                    currentStreak),
                CurrentStreakDateText = LocalizationService.Format(
                    "LOCPlaytimeInsightsCurrentStreakDateFormat",
                    "截至 {0:M/d}",
                    MinDate(range.EndDate, DateTime.Today)),
                AnomalyCountText = LocalizationService.Format(
                    "LOCPlaytimeInsightsCountItemsFormat",
                    "{0:N0} 条",
                    anomalies.Count),
                AnomalyVisibility = anomalies.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed,
                Anomalies = anomalies
            };
        }

        public static AnalyticsDateRange CreatePreviousPeriodRange(
            AnalyticsDateRange range)
        {
            var days = Math.Max(
                1,
                (int)(range.EndDate.Date - range.StartDate.Date).TotalDays + 1);
            var end = range.StartDate.Date.AddDays(-1);
            var start = end.AddDays(-(days - 1));
            return new AnalyticsDateRange
            {
                StartDate = start,
                EndDate = end,
                Label = string.Format("{0:yyyy/M/d}–{1:yyyy/M/d}", start, end)
            };
        }

        public static AnalyticsDateRange CreateYearOverYearRange(
            AnalyticsDateRange range)
        {
            var start = SafeAddYears(range.StartDate.Date, -1);
            var end = SafeAddYears(range.EndDate.Date, -1);
            return new AnalyticsDateRange
            {
                StartDate = start,
                EndDate = end,
                Label = string.Format("{0:yyyy/M/d}–{1:yyyy/M/d}", start, end)
            };
        }

        private ulong CalculateRangeSeconds(
            IEnumerable<GameSession> sessions,
            AnalyticsDateRange range)
        {
            ulong total = 0;
            foreach (var session in sessions)
            {
                total += dailyAllocationService.SplitByLocalDay(session)
                    .Where(value =>
                        value.Key.Date >= range.StartDate &&
                        value.Key.Date <= range.EndDate)
                    .Aggregate<KeyValuePair<DateTime, ulong>, ulong>(
                        0,
                        (current, value) => current + value.Value);
            }
            return total;
        }

        private static IList<DistributionBarViewModel> CreateDistribution(
            IList<ulong> seconds,
            IList<string> labels,
            string prefix)
        {
            var maximum = seconds.Count == 0 ? 0UL : seconds.Max();
            return seconds.Select((value, index) => new DistributionBarViewModel
            {
                Label = labels[index],
                Seconds = value,
                DurationText = AnalyticsService.FormatDurationPrecise(value),
                TooltipText = string.Format(
                    "{0}：{1}",
                    string.IsNullOrWhiteSpace(prefix)
                        ? labels[index]
                        : prefix + " " + labels[index],
                    AnalyticsService.FormatDurationPrecise(value)),
                BarHeight = maximum == 0
                    ? 4
                    : 6 + (double)value / maximum * 94
            }).ToList();
        }

        public static IList<DistributionBarViewModel>
            CreateHourDistributionForWeekday(
                IList<WeekHourCellViewModel> cells,
                int weekdayIndex)
        {
            if (cells == null ||
                weekdayIndex < 0 ||
                weekdayIndex >= 7 ||
                cells.Count < (weekdayIndex + 1) * 24)
            {
                return new List<DistributionBarViewModel>();
            }

            var selectedCells = cells
                .Skip(weekdayIndex * 24)
                .Take(24)
                .ToList();
            var maximum = selectedCells.Count == 0
                ? 0UL
                : selectedCells.Max(cell => cell.Seconds);
            return selectedCells.Select(cell => new DistributionBarViewModel
            {
                Label = cell.HourLabel,
                Seconds = cell.Seconds,
                DurationText = AnalyticsService.FormatDurationPrecise(
                    cell.Seconds),
                TooltipText = cell.TooltipText,
                BarHeight = maximum == 0
                    ? 4
                    : 6 + (double)cell.Seconds / maximum * 94
            }).ToList();
        }

        private static IList<WeekHourCellViewModel> CreateWeekHourCells(
            ulong[,] seconds,
            IList<string> weekdayLabels,
            IList<string> hourLabels)
        {
            ulong maximum = 0;
            for (var day = 0; day < 7; day++)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    maximum = Math.Max(maximum, seconds[day, hour]);
                }
            }

            var result = new List<WeekHourCellViewModel>(7 * 24);
            for (var day = 0; day < 7; day++)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    var value = seconds[day, hour];
                    result.Add(new WeekHourCellViewModel
                    {
                        DayLabel = weekdayLabels[day],
                        HourLabel = hourLabels[hour] + ":00",
                        Seconds = value,
                        HeatOpacity = value == 0 || maximum == 0
                            ? 0.06
                            : 0.16 + (double)value / maximum * 0.84,
                        TooltipText = string.Format(
                            "{0} {1}：{2}",
                            weekdayLabels[day],
                            hourLabels[hour] + ":00",
                            AnalyticsService.FormatDurationPrecise(value))
                    });
                }
            }
            return result;
        }

        private static ComparisonMetricViewModel CreateComparison(
            string title,
            string shortTitle,
            ulong current,
            ulong previous,
            AnalyticsDateRange comparisonRange)
        {
            string delta;
            string trendKind;
            string arrow;
            if (previous == 0)
            {
                delta = current == 0
                    ? LocalizationService.Get(
                        "LOCPlaytimeInsightsUnchanged",
                        "持平")
                    : LocalizationService.Get(
                        "LOCPlaytimeInsightsNew",
                        "新增");
            }
            else
            {
                var percentage = ((double)current - previous) / previous * 100;
                delta = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}{1:0.0}%",
                    percentage > 0 ? "+" : string.Empty,
                    percentage);
            }

            if (current > previous)
            {
                trendKind = "Increase";
                arrow = "↑";
            }
            else if (current < previous)
            {
                trendKind = "Decrease";
                arrow = "↓";
            }
            else
            {
                trendKind = "Neutral";
                arrow = "—";
            }

            var absoluteDelta = current > previous
                ? current - previous
                : previous - current;
            var previousText = string.Format(
                "{0}：{1}",
                comparisonRange.Label,
                AnalyticsService.FormatDurationPrecise(previous));
            var tagText = LocalizationService.Format(
                "LOCPlaytimeInsightsTrendTagFormat",
                "{0} {1}（{2}）",
                arrow,
                AnalyticsService.FormatDurationPrecise(absoluteDelta),
                shortTitle);

            return new ComparisonMetricViewModel
            {
                Title = title,
                CurrentText = AnalyticsService.FormatDurationPrecise(current),
                PreviousText = previousText,
                DeltaText = delta,
                TagText = tagText,
                TrendKind = trendKind,
                TooltipText = LocalizationService.Format(
                    "LOCPlaytimeInsightsTrendTooltipFormat",
                    "{0}；{1}；变化 {2}",
                    title,
                    previousText,
                    delta)
            };
        }

        private static void CalculateStreaks(
            IList<DateTime> activeDates,
            AnalyticsDateRange range,
            DateTime today,
            out int longest,
            out int current)
        {
            longest = 0;
            var running = 0;
            DateTime? previous = null;
            foreach (var date in activeDates)
            {
                running = previous.HasValue &&
                          date == previous.Value.AddDays(1)
                    ? running + 1
                    : 1;
                longest = Math.Max(longest, running);
                previous = date;
            }

            current = 0;
            var cursor = MinDate(range.EndDate, today.Date);
            var set = new HashSet<DateTime>(activeDates);
            while (cursor >= range.StartDate && set.Contains(cursor))
            {
                current++;
                cursor = cursor.AddDays(-1);
            }
        }

        private static IList<AnomalySessionViewModel> CreateAnomalies(
            IList<Game> games,
            IList<GameSession> sessions,
            AnalyticsDateRange range)
        {
            var names = games
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First().Name);
            var result = new List<Tuple<DateTime, AnomalySessionViewModel>>();
            foreach (var session in sessions)
            {
                var localStart = new DateTimeOffset(
                    DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc))
                    .ToOffset(TimeSpan.FromMinutes(session.StartUtcOffsetMinutes))
                    .DateTime;
                var localEnd = new DateTimeOffset(
                    DateTime.SpecifyKind(session.EndedAtUtc, DateTimeKind.Utc))
                    .ToOffset(TimeSpan.FromMinutes(session.EndUtcOffsetMinutes))
                    .DateTime;
                if (localEnd.Date < range.StartDate ||
                    localStart.Date > range.EndDate)
                {
                    continue;
                }

                var reasons = new List<string>();
                if (session.ElapsedSeconds == 0)
                {
                    reasons.Add(LocalizationService.Get(
                        "LOCPlaytimeInsightsAnomalyZeroSeconds",
                        "零秒会话"));
                }
                if (session.EndedAtUtc < session.StartedAtUtc)
                {
                    reasons.Add(LocalizationService.Get(
                        "LOCPlaytimeInsightsAnomalyEndBeforeStart",
                        "结束早于开始"));
                }
                if (session.StartedAtUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    reasons.Add(LocalizationService.Get(
                        "LOCPlaytimeInsightsAnomalyFutureStart",
                        "开始时间位于未来"));
                }
                if (session.ElapsedSeconds >= 18UL * 3600UL)
                {
                    reasons.Add(LocalizationService.Get(
                        "LOCPlaytimeInsightsAnomalyLongDuration",
                        "持续至少 18 小时"));
                }

                var wallSeconds =
                    (session.EndedAtUtc - session.StartedAtUtc).TotalSeconds;
                if (wallSeconds >= 0 &&
                    session.ElapsedSeconds > wallSeconds + 300)
                {
                    reasons.Add(LocalizationService.Get(
                        "LOCPlaytimeInsightsAnomalyWallClockMismatch",
                        "记录秒数明显大于墙钟时长"));
                }
                if (reasons.Count == 0)
                {
                    continue;
                }

                string name;
                names.TryGetValue(session.GameId, out name);
                result.Add(Tuple.Create(
                    session.StartedAtUtc,
                    new AnomalySessionViewModel
                    {
                        GameName = string.IsNullOrWhiteSpace(name)
                            ? session.GameName
                            : name,
                        StartedText = localStart.ToString("yyyy/M/d HH:mm"),
                        DurationText = AnalyticsService.FormatDurationPrecise(
                            session.ElapsedSeconds),
                        Reason = string.Join(
                            LocalizationService.Get(
                                "LOCPlaytimeInsightsListSeparator",
                                "；"),
                            reasons)
                    }));
            }

            return result
                .OrderByDescending(item => item.Item1)
                .Take(50)
                .Select(item => item.Item2)
                .ToList();
        }

        private static int GetWeekdayIndex(
            DayOfWeek day,
            DayOfWeek firstDayOfWeek)
        {
            return (7 + (day - firstDayOfWeek)) % 7;
        }

        private static DateTime SafeAddYears(DateTime value, int years)
        {
            try
            {
                return value.AddYears(years);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new DateTime(
                    value.Year + years,
                    value.Month,
                    DateTime.DaysInMonth(value.Year + years, value.Month));
            }
        }

        private static DateTime MinDate(DateTime first, DateTime second)
        {
            return first <= second ? first : second;
        }
    }
}
