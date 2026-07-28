using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Services
{
    public sealed class DailyAllocationService
    {
        public IDictionary<DateTime, ulong> AggregateByLocalDay(IEnumerable<GameSession> sessions)
        {
            var result = new Dictionary<DateTime, ulong>();
            if (sessions == null)
            {
                return result;
            }

            foreach (var session in sessions)
            {
                foreach (var allocation in SplitByLocalDay(session))
                {
                    ulong existing;
                    result.TryGetValue(allocation.Key, out existing);
                    result[allocation.Key] = existing + allocation.Value;
                }
            }

            return result;
        }

        public IDictionary<DateTime, ulong> SplitByLocalDay(GameSession session)
        {
            var result = new Dictionary<DateTime, ulong>();
            if (session == null)
            {
                return result;
            }

            var startedAtUtc = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc);
            var endedAtUtc = DateTime.SpecifyKind(session.EndedAtUtc, DateTimeKind.Utc);
            if (endedAtUtc <= startedAtUtc)
            {
                Add(result, session.GetStartedLocalDate(), session.ElapsedSeconds);
                return result;
            }

            var timeZone = ResolveTimeZone(session);
            var wallSeconds = (endedAtUtc - startedAtUtc).TotalSeconds;
            var remainingElapsed = session.ElapsedSeconds;
            var cursorUtc = startedAtUtc;
            var guard = 0;

            while (cursorUtc < endedAtUtc && guard++ < 4096)
            {
                var localCursor = TimeZoneInfo.ConvertTimeFromUtc(cursorUtc, timeZone);
                var localDate = localCursor.Date;
                var nextLocalMidnight = DateTime.SpecifyKind(localDate.AddDays(1), DateTimeKind.Unspecified);
                DateTime nextMidnightUtc;

                try
                {
                    nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocalMidnight, timeZone);
                }
                catch (ArgumentException)
                {
                    // Midnight can theoretically be invalid in a timezone transition.
                    nextMidnightUtc = cursorUtc.AddDays(1);
                }

                if (nextMidnightUtc <= cursorUtc)
                {
                    nextMidnightUtc = cursorUtc.AddDays(1);
                }

                var segmentEndUtc = nextMidnightUtc < endedAtUtc
                    ? nextMidnightUtc
                    : endedAtUtc;
                ulong allocated;
                if (segmentEndUtc >= endedAtUtc)
                {
                    allocated = remainingElapsed;
                }
                else
                {
                    var segmentWallSeconds = (segmentEndUtc - cursorUtc).TotalSeconds;
                    allocated = (ulong)Math.Floor(
                        session.ElapsedSeconds * segmentWallSeconds / wallSeconds);
                    if (allocated > remainingElapsed)
                    {
                        allocated = remainingElapsed;
                    }
                }

                Add(result, localDate, allocated);
                remainingElapsed -= allocated;
                cursorUtc = segmentEndUtc;
            }

            if (remainingElapsed > 0)
            {
                var finalLocalDate = TimeZoneInfo.ConvertTimeFromUtc(endedAtUtc.AddTicks(-1), timeZone).Date;
                Add(result, finalLocalDate, remainingElapsed);
            }

            return result;
        }

        private static TimeZoneInfo ResolveTimeZone(GameSession session)
        {
            if (!string.IsNullOrWhiteSpace(session.TimeZoneId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(session.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            var offset = TimeSpan.FromMinutes(session.StartUtcOffsetMinutes);
            var id = string.Format("PlaytimeInsights.FixedOffset.{0}", session.StartUtcOffsetMinutes);
            return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
        }

        private static void Add(IDictionary<DateTime, ulong> result, DateTime date, ulong seconds)
        {
            ulong existing;
            result.TryGetValue(date.Date, out existing);
            result[date.Date] = existing + seconds;
        }
    }
}

