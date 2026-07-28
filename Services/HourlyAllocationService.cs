using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Services
{
    public sealed class HourlyAllocation
    {
        public DateTime LocalDate { get; set; }

        public int Hour { get; set; }

        public ulong Seconds { get; set; }
    }

    public sealed class HourlyAllocationService
    {
        private readonly object timeZoneCacheSync = new object();
        private readonly Dictionary<string, TimeZoneInfo> timeZoneCache =
            new Dictionary<string, TimeZoneInfo>(
                StringComparer.OrdinalIgnoreCase);

        public IList<HourlyAllocation> SplitByLocalHour(GameSession session)
        {
            var result = new List<HourlyAllocation>();
            if (session == null)
            {
                return result;
            }

            var startedAtUtc = DateTime.SpecifyKind(
                session.StartedAtUtc,
                DateTimeKind.Utc);
            var endedAtUtc = DateTime.SpecifyKind(
                session.EndedAtUtc,
                DateTimeKind.Utc);
            var timeZone = ResolveTimeZone(session);
            if (endedAtUtc <= startedAtUtc)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(startedAtUtc, timeZone);
                result.Add(new HourlyAllocation
                {
                    LocalDate = local.Date,
                    Hour = local.Hour,
                    Seconds = session.ElapsedSeconds
                });
                return result;
            }

            var wallSeconds = (endedAtUtc - startedAtUtc).TotalSeconds;
            var remainingElapsed = session.ElapsedSeconds;
            var cursorUtc = startedAtUtc;
            var guard = 0;

            while (cursorUtc < endedAtUtc && guard++ < 200000)
            {
                var localCursor = TimeZoneInfo.ConvertTimeFromUtc(cursorUtc, timeZone);
                var segmentEndUtc = FindNextLocalHourBoundary(
                    cursorUtc,
                    localCursor,
                    timeZone);
                if (segmentEndUtc <= cursorUtc)
                {
                    segmentEndUtc = cursorUtc.AddHours(1);
                }
                if (segmentEndUtc > endedAtUtc)
                {
                    segmentEndUtc = endedAtUtc;
                }

                ulong allocated;
                if (segmentEndUtc >= endedAtUtc)
                {
                    allocated = remainingElapsed;
                }
                else
                {
                    var segmentWallSeconds =
                        (segmentEndUtc - cursorUtc).TotalSeconds;
                    allocated = (ulong)Math.Floor(
                        session.ElapsedSeconds *
                        segmentWallSeconds /
                        wallSeconds);
                    if (allocated > remainingElapsed)
                    {
                        allocated = remainingElapsed;
                    }
                }

                result.Add(new HourlyAllocation
                {
                    LocalDate = localCursor.Date,
                    Hour = localCursor.Hour,
                    Seconds = allocated
                });
                remainingElapsed -= allocated;
                cursorUtc = segmentEndUtc;
            }

            if (remainingElapsed > 0)
            {
                var finalLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    endedAtUtc.AddTicks(-1),
                    timeZone);
                result.Add(new HourlyAllocation
                {
                    LocalDate = finalLocal.Date,
                    Hour = finalLocal.Hour,
                    Seconds = remainingElapsed
                });
            }

            return result;
        }

        private static DateTime FindNextLocalHourBoundary(
            DateTime cursorUtc,
            DateTime localCursor,
            TimeZoneInfo timeZone)
        {
            if (!timeZone.SupportsDaylightSavingTime)
            {
                var nextLocal = DateTime.SpecifyKind(
                    new DateTime(
                        localCursor.Year,
                        localCursor.Month,
                        localCursor.Day,
                        localCursor.Hour,
                        0,
                        0).AddHours(1),
                    DateTimeKind.Unspecified);
                var directCandidate = TimeZoneInfo.ConvertTimeToUtc(
                    nextLocal,
                    timeZone);
                if (directCandidate > cursorUtc)
                {
                    return directCandidate;
                }
            }

            var candidate = new DateTime(
                cursorUtc.Year,
                cursorUtc.Month,
                cursorUtc.Day,
                cursorUtc.Hour,
                cursorUtc.Minute,
                0,
                DateTimeKind.Utc).AddMinutes(1);
            for (var minute = 0; minute < 240; minute++)
            {
                var localCandidate = TimeZoneInfo.ConvertTimeFromUtc(
                    candidate,
                    timeZone);
                if (localCandidate.Minute == 0 &&
                    localCandidate.Second == 0 &&
                    localCandidate.Millisecond == 0)
                {
                    return candidate;
                }
                candidate = candidate.AddMinutes(1);
            }

            return cursorUtc.AddHours(1);
        }

        private TimeZoneInfo ResolveTimeZone(GameSession session)
        {
            var cacheKey = !string.IsNullOrWhiteSpace(session.TimeZoneId)
                ? "id:" + session.TimeZoneId +
                  "|offset:" + session.StartUtcOffsetMinutes
                : "offset:" + session.StartUtcOffsetMinutes;
            lock (timeZoneCacheSync)
            {
                TimeZoneInfo cached;
                if (timeZoneCache.TryGetValue(cacheKey, out cached))
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
                var offset = TimeSpan.FromMinutes(
                    session.StartUtcOffsetMinutes);
                var id = string.Format(
                    "PlaytimeInsights.HourlyOffset.{0}",
                    session.StartUtcOffsetMinutes);
                resolved = TimeZoneInfo.CreateCustomTimeZone(
                    id,
                    offset,
                    id,
                    id);
            }

            lock (timeZoneCacheSync)
            {
                timeZoneCache[cacheKey] = resolved;
            }
            return resolved;
        }
    }
}
