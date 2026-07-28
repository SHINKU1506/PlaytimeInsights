using System;

namespace PlaytimeInsights.Models
{
    public sealed class SessionStorageDiagnostics
    {
        public int SchemaVersion { get; set; }

        public int SessionCount { get; set; }

        public int ActiveSessionCount { get; set; }

        public int DeletedSessionCount { get; set; }

        public int TrackedSessionCount { get; set; }

        public int RecoveredSessionCount { get; set; }

        public int ImportedSessionCount { get; set; }

        public int ManualSessionCount { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public bool StorageWritable { get; set; }

        public bool LoadedFromBackup { get; set; }

        public bool SessionsFileExists { get; set; }

        public long SessionsFileBytes { get; set; }

        public bool BackupFileExists { get; set; }

        public long BackupFileBytes { get; set; }

        public int RollbackBackupCount { get; set; }

        public string DataDirectory { get; set; }
    }
}
