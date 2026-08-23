using System;

namespace PlaytimeInsights.Services
{
    public static class RecentActivityFormatter
    {
        public static string Format(DateTime? activity, DateTime now)
        {
            if (!activity.HasValue)
            {
                return LocalizationService.Get(
                    "LOCPlaytimeInsightsNoRecentActivity",
                    "无最近游玩记录");
            }

            var value = activity.Value;
            if (value.Date == now.Date)
            {
                return LocalizationService.Format(
                    "LOCPlaytimeInsightsTodayAtFormat",
                    "今天 {0:HH:mm}",
                    value);
            }

            if (value.Date == now.Date.AddDays(-1))
            {
                return LocalizationService.Format(
                    "LOCPlaytimeInsightsYesterdayAtFormat",
                    "昨天 {0:HH:mm}",
                    value);
            }

            return LocalizationService.Format(
                "LOCPlaytimeInsightsRecentActivityDateFormat",
                "{0:g}",
                value);
        }
    }
}
