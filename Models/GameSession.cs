using System;

namespace PlaytimeInsights.Models
{
    public enum SessionSource
    {
        Tracked,
        Imported,
        Manual,
        Recovered
    }

    public class GameSession
    {
        public const int CurrentSchemaVersion = 4;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid GameId { get; set; }

        public string GameName { get; set; } = string.Empty;

        public string GameSourceName { get; set; } = string.Empty;

        public string PlatformNames { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public DateTime EndedAtUtc { get; set; }

        public ulong ElapsedSeconds { get; set; }

        public int StartUtcOffsetMinutes { get; set; }

        public int EndUtcOffsetMinutes { get; set; }

        public string TimeZoneId { get; set; } = string.Empty;

        public bool ManuallyStopped { get; set; }

        public SessionSource Source { get; set; } = SessionSource.Tracked;

        public string RecoveryReason { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAtUtc { get; set; }

        public DateTime? LastModifiedAtUtc { get; set; }

        public string LastModifiedReason { get; set; } = string.Empty;

        public string ImportSource { get; set; } = string.Empty;

        public string ImportConfidence { get; set; } = string.Empty;

        public DateTime GetStartedLocalDate()
        {
            var utc = DateTime.SpecifyKind(StartedAtUtc, DateTimeKind.Utc);
            return new DateTimeOffset(utc).ToOffset(TimeSpan.FromMinutes(StartUtcOffsetMinutes)).Date;
        }
    }

    public class ActiveGameSession
    {
        public Guid GameId { get; set; }

        public string GameName { get; set; } = string.Empty;

        public string GameSourceName { get; set; } = string.Empty;

        public string PlatformNames { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public DateTime LastCheckpointUtc { get; set; }

        public int StartUtcOffsetMinutes { get; set; }

        public string TimeZoneId { get; set; } = string.Empty;
    }

    public class SessionStoreDocument
    {
        public int SchemaVersion { get; set; } = GameSession.CurrentSchemaVersion;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public System.Collections.Generic.List<GameSession> Sessions { get; set; } =
            new System.Collections.Generic.List<GameSession>();

        public System.Collections.Generic.List<ActiveGameSession> ActiveSessions { get; set; } =
            new System.Collections.Generic.List<ActiveGameSession>();
    }
}
