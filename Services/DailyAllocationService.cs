using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Services
{
    public struct DailyAllocation
    {
        public DateTime LocalDate { get; set; }

        public ulong Seconds { get; set; }
    }

    public sealed class DailyAllocationService
    {
        private readonly SessionTimeZoneResolver timeZoneResolver;

        public DailyAllocationService()
            : this(new SessionTimeZoneResolver())
        {
        }

        public DailyAllocationService(SessionTimeZoneResolver resolver)
        {
            timeZoneResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

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
            var destination = new List<DailyAllocation>();
            SplitByLocalDay(session, destination);
            var result = new Dictionary<DateTime, ulong>();
            foreach (var allocation in destination)
            {
                result[allocation.LocalDate] = allocation.Seconds;
            }

            return result;
        }

        public void SplitByLocalDay(GameSession session, IList<DailyAllocation> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            if (session == null)
            {
                return;
            }

            var startedAtUtc = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc);
            var endedAtUtc = DateTime.SpecifyKind(session.EndedAtUtc, DateTimeKind.Utc);
            if (endedAtUtc <= startedAtUtc)
            {
                Add(destination, session.GetStartedLocalDate(), session.ElapsedSeconds);
                return;
            }

            var timeZone = timeZoneResolver.Resolve(session);
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

                Add(destination, localDate, allocated);
                remainingElapsed -= allocated;
                cursorUtc = segmentEndUtc;
            }

            if (remainingElapsed > 0)
            {
                var finalLocalDate = TimeZoneInfo.ConvertTimeFromUtc(endedAtUtc.AddTicks(-1), timeZone).Date;
                Add(destination, finalLocalDate, remainingElapsed);
            }
        }

        private static void Add(IList<DailyAllocation> destination, DateTime date, ulong seconds)
        {
            var target = date.Date;
            for (var index = destination.Count - 1; index >= 0; index--)
            {
                if (destination[index].LocalDate == target)
                {
                    var existing = destination[index];
                    existing.Seconds += seconds;
                    destination[index] = existing;
                    return;
                }
            }

            destination.Add(new DailyAllocation { LocalDate = target, Seconds = seconds });
        }
    }
}

