using Playnite.SDK;
using System;

namespace PlaytimeInsights.Services
{
    public static class LocalizationService
    {
        public static string Get(string key, string fallback)
        {
            try
            {
                var value = ResourceProvider.GetString(key);
                return string.IsNullOrWhiteSpace(value) ||
                    string.Equals(value, key, StringComparison.Ordinal) ||
                    (value.StartsWith("<!", StringComparison.Ordinal) &&
                     value.EndsWith("!>", StringComparison.Ordinal))
                    ? fallback
                    : value;
            }
            catch
            {
                return fallback;
            }
        }

        public static string Format(
            string key,
            string fallback,
            params object[] arguments)
        {
            return string.Format(Get(key, fallback), arguments);
        }
    }
}
