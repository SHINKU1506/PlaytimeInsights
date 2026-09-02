using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Services
{
    public struct HourlyAllocation
    {
        public DateTime LocalDate { get; set; }

        public int Hour { get; set; }

        public ulong Seconds { get; set; }
    }

    public sealed class HourlyAllocationService
    {
        private readonly SessionTimeZoneResolver timeZoneResolver;

        public HourlyAllocationService()
            : this(new SessionTimeZoneResolver())
        {
        }

        public HourlyAllocationService(SessionTimeZoneResolver resolver)
        {
            timeZoneResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public IList<HourlyAllocation> SplitByLocalHour(GameSession session)
        {
            var destination = new List<HourlyAllocation>();
            SplitByLocalHour(session, destination);
            return destination;
        }

        public void SplitByLocalHour(GameSession session, IList<HourlyAllocation> destination)
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

            var startedAtUtc = DateTime.SpecifyKind(
                session.StartedAtUtc,
                DateTimeKind.Utc);
            var endedAtUtc = DateTime.SpecifyKind(
                session.EndedAtUtc,
                DateTimeKind.Utc);
            var timeZone = timeZoneResolver.Resolve(session);
            if (endedAtUtc <= startedAtUtc)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(startedAtUtc, timeZone);
                destination.Add(new HourlyAllocation
                {
                    LocalDate = local.Date,
                    Hour = local.Hour,
                    Seconds = session.ElapsedSeconds
                });
                return;
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

                destination.Add(new HourlyAllocation
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
                destination.Add(new HourlyAllocation
                {
                    LocalDate = finalLocal.Date,
                    Hour = finalLocal.Hour,
                    Seconds = remainingElapsed
                });
            }
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
    }
}
