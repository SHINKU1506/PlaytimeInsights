using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Services
{
    public sealed class SessionTimeZoneResolver
    {
        private readonly object cacheSync = new object();
        private readonly Dictionary<string, TimeZoneInfo> cache =
            new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);

        public TimeZoneInfo Resolve(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var cacheKey = !string.IsNullOrWhiteSpace(session.TimeZoneId)
                ? "id:" + session.TimeZoneId +
                  "|offset:" + session.StartUtcOffsetMinutes
                : "offset:" + session.StartUtcOffsetMinutes;

            lock (cacheSync)
            {
                TimeZoneInfo cached;
                if (cache.TryGetValue(cacheKey, out cached))
                {
                    return cached;
                }
            }

            TimeZoneInfo resolved = null;
            if (!string.IsNullOrWhiteSpace(session.TimeZoneId))
            {
                try
                {
                    resolved = TimeZoneInfo.FindSystemTimeZoneById(
                        session.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            if (resolved == null)
            {
                var offset = TimeSpan.FromMinutes(session.StartUtcOffsetMinutes);
                var id = string.Format(
                    "PlaytimeInsights.FixedOffset.{0}",
                    session.StartUtcOffsetMinutes);
                resolved = TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
            }

            lock (cacheSync)
            {
                TimeZoneInfo existing;
                if (!cache.TryGetValue(cacheKey, out existing))
                {
                    cache[cacheKey] = resolved;
                    existing = resolved;
                }

                return existing;
            }
        }
    }
}
