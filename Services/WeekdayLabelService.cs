using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaytimeInsights.Services
{
    public static class WeekdayLabelService
    {
        private static readonly string[] ResourceKeys =
        {
            "LOCPlaytimeInsightsSundayShort",
            "LOCPlaytimeInsightsMondayShort",
            "LOCPlaytimeInsightsTuesdayShort",
            "LOCPlaytimeInsightsWednesdayShort",
            "LOCPlaytimeInsightsThursdayShort",
            "LOCPlaytimeInsightsFridayShort",
            "LOCPlaytimeInsightsSaturdayShort"
        };

        private static readonly string[] ChineseFallbacks =
        {
            "周日",
            "周一",
            "周二",
            "周三",
            "周四",
            "周五",
            "周六"
        };

        public static IList<string> CreateLabels(
            DayOfWeek firstDayOfWeek,
            Func<string, string, string> resourceResolver = null)
        {
            resourceResolver = resourceResolver ?? LocalizationService.Get;
            var names = ResourceKeys
                .Select((key, index) =>
                    resourceResolver(key, ChineseFallbacks[index]))
                .ToArray();

            return Enumerable.Range(0, 7)
                .Select(offset => names[
                    ((int)firstDayOfWeek + offset) % 7])
                .ToList();
        }
    }
}
