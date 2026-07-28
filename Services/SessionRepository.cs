using Playnite.SDK;
using Playnite.SDK.Data;
using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PlaytimeInsights.Services
{
    public interface ISessionSerializer
    {
        string Serialize(SessionStoreDocument document);

        bool TryDeserialize(
            string path,
            out SessionStoreDocument document,
            out Exception error);
    }

    public sealed class PlayniteSessionSerializer : ISessionSerializer
    {
        public string Serialize(SessionStoreDocument document)
        {
            return Serialization.ToJson(document, true);
        }

        public bool TryDeserialize(
            string path,
            out SessionStoreDocument document,
            out Exception error)
        {
            return Serialization.TryFromJsonFile(path, out document, out error);
        }
    }

    public sealed class SessionRepository
    {
        private readonly object syncRoot = new object();
        private readonly ILogger logger;
        private readonly string dataDirectory;
        private readonly string sessionsPath;
        private readonly string backupPath;
        private readonly ISessionSerializer serializer;
        private bool storageWritable = true;
        private bool loadedFromBackup;
        private SessionStoreDocument document;

        public SessionRepository(
            string dataDirectory,
            ILogger logger,
            ISessionSerializer serializer = null)
        {
            this.dataDirectory = dataDirectory;
            this.logger = logger;
            this.serializer = serializer ?? new PlayniteSessionSerializer();
            sessionsPath = Path.Combine(dataDirectory, "sessions.json");
            backupPath = Path.Combine(dataDirectory, "sessions.backup.json");
            document = Load();
        }

        public IReadOnlyList<GameSession> GetAll()
        {
            lock (syncRoot)
            {
                return document.Sessions
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.StartedAtUtc)
                    .Select(CloneSession)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public IReadOnlyList<GameSession> GetAllIncludingDeleted()
        {
            lock (syncRoot)
            {
                return document.Sessions
                    .OrderBy(a => a.StartedAtUtc)
                    .Select(CloneSession)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public SessionStorageDiagnostics GetStorageDiagnostics()
        {
            lock (syncRoot)
            {
                var sessions = document.Sessions ?? new List<GameSession>();
                var rollbackDirectory = Path.Combine(dataDirectory, "Backups");
                return new SessionStorageDiagnostics
                {
                    SchemaVersion = document.SchemaVersion,
                    SessionCount = sessions.Count,
                    ActiveSessionCount = document.ActiveSessions?.Count ?? 0,
                    DeletedSessionCount = sessions.Count(session =>
                        session.IsDeleted),
                    TrackedSessionCount = sessions.Count(session =>
                        session.Source == SessionSource.Tracked),
                    RecoveredSessionCount = sessions.Count(session =>
                        session.Source == SessionSource.Recovered),
                    ImportedSessionCount = sessions.Count(session =>
                        session.Source == SessionSource.Imported),
                    ManualSessionCount = sessions.Count(session =>
                        session.Source == SessionSource.Manual),
                    UpdatedAtUtc = document.UpdatedAtUtc,
                    StorageWritable = storageWritable,
                    LoadedFromBackup = loadedFromBackup,
                    SessionsFileExists = File.Exists(sessionsPath),
                    SessionsFileBytes = GetFileLength(sessionsPath),
                    BackupFileExists = File.Exists(backupPath),
                    BackupFileBytes = GetFileLength(backupPath),
                    RollbackBackupCount = GetJsonFileCount(
                        rollbackDirectory),
                    DataDirectory = dataDirectory
                };
            }
        }

        public GameSession FindSession(Guid sessionId)
        {
            lock (syncRoot)
            {
                var session = document.Sessions.FirstOrDefault(a => a.Id == sessionId);
                return session == null ? null : CloneSession(session);
            }
        }

        public IReadOnlyList<ActiveGameSession> GetActiveSessions()
        {
            lock (syncRoot)
            {
                return document.ActiveSessions
                    .Select(CloneActive)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public void BeginSession(ActiveGameSession activeSession)
        {
            if (activeSession == null)
            {
                throw new ArgumentNullException(nameof(activeSession));
            }

            lock (syncRoot)
            {
                var existing = document.ActiveSessions.FirstOrDefault(a =>
                    a.GameId == activeSession.GameId);
                if (existing == null)
                {
                    document.ActiveSessions.Add(CloneActive(activeSession));
                }
                else
                {
                    // A duplicate start event must not move the session start forward.
                    existing.GameName = activeSession.GameName;
                    existing.GameSourceName = activeSession.GameSourceName;
                    existing.PlatformNames = activeSession.PlatformNames;
                    existing.LastCheckpointUtc = activeSession.LastCheckpointUtc;
                    existing.StartUtcOffsetMinutes = activeSession.StartUtcOffsetMinutes;
                    existing.TimeZoneId = activeSession.TimeZoneId;
                }

                SaveUnsafe();
            }
        }

        public ActiveGameSession FindActiveSession(Guid gameId)
        {
            lock (syncRoot)
            {
                var active = document.ActiveSessions.FirstOrDefault(a => a.GameId == gameId);
                return active == null ? null : CloneActive(active);
            }
        }

        public bool CompleteSession(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (syncRoot)
            {
                document.ActiveSessions.RemoveAll(a => a.GameId == session.GameId);

                if (IsDuplicateUnsafe(session))
                {
                    SaveUnsafe();
                    return false;
                }

                session.SchemaVersion = GameSession.CurrentSchemaVersion;
                document.Sessions.Add(CloneSession(session));
                SaveUnsafe();
                return true;
            }
        }

        public bool UpdateSession(GameSession updatedSession, string reason)
        {
            if (updatedSession == null)
            {
                throw new ArgumentNullException(nameof(updatedSession));
            }

            lock (syncRoot)
            {
                var index = document.Sessions.FindIndex(a => a.Id == updatedSession.Id);
                if (index < 0)
                {
                    return false;
                }

                var existing = document.Sessions[index];
                var replacement = CloneSession(updatedSession);
                replacement.Id = existing.Id;
                replacement.SchemaVersion = GameSession.CurrentSchemaVersion;
                replacement.IsDeleted = existing.IsDeleted;
                replacement.DeletedAtUtc = existing.DeletedAtUtc;
                replacement.LastModifiedAtUtc = DateTime.UtcNow;
                replacement.LastModifiedReason = reason ?? "Edited";
                if (IsDuplicateUnsafe(replacement, existing.Id))
                {
                    return false;
                }

                document.Sessions[index] = replacement;
                SaveUnsafe();
                return true;
            }
        }

        public bool SetSessionDeleted(Guid sessionId, bool isDeleted, string reason)
        {
            lock (syncRoot)
            {
                var session = document.Sessions.FirstOrDefault(a => a.Id == sessionId);
                if (session == null || session.IsDeleted == isDeleted)
                {
                    return false;
                }

                session.SchemaVersion = GameSession.CurrentSchemaVersion;
                session.IsDeleted = isDeleted;
                session.DeletedAtUtc = isDeleted ? (DateTime?)DateTime.UtcNow : null;
                session.LastModifiedAtUtc = DateTime.UtcNow;
                session.LastModifiedReason = reason ??
                    (isDeleted ? "SoftDeleted" : "Restored");
                SaveUnsafe();
                return true;
            }
        }

        public SessionImportCommitResult ImportSessions(
            IEnumerable<GameSession> sessions)
        {
            if (sessions == null)
            {
                throw new ArgumentNullException(nameof(sessions));
            }

            lock (syncRoot)
            {
                EnsureStorageWritable();
                var pending = new List<GameSession>();
                var duplicates = 0;
                foreach (var source in sessions)
                {
                    if (source == null)
                    {
                        continue;
                    }

                    var candidate = CloneSession(source);
                    candidate.SchemaVersion = GameSession.CurrentSchemaVersion;
                    candidate.Source = SessionSource.Imported;
                    candidate.LastModifiedAtUtc = DateTime.UtcNow;
                    candidate.LastModifiedReason = "Imported";
                    if (IsDuplicateUnsafe(candidate) ||
                        pending.Any(existing =>
                            SessionImportService.IsDuplicate(existing, candidate)))
                    {
                        duplicates++;
                        continue;
                    }
                    pending.Add(candidate);
                }

                if (pending.Count == 0)
                {
                    return new SessionImportCommitResult
                    {
                        DuplicateCount = duplicates
                    };
                }

                var rollbackPath = CreateRollbackBackupUnsafe("pre-import");
                document.Sessions.AddRange(pending);
                SaveUnsafe();
                return new SessionImportCommitResult
                {
                    ImportedCount = pending.Count,
                    DuplicateCount = duplicates,
                    RollbackBackupPath = rollbackPath
                };
            }
        }

        public string CreateManualBackup(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException(
                    "Backup destination is required.",
                    nameof(destinationPath));
            }

            lock (syncRoot)
            {
                EnsureStorageWritable();
                WriteDocumentSnapshotUnsafe(destinationPath, CloneDocument(document));
                return destinationPath;
            }
        }

        public SessionRestorePreview PreviewRestore(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return new SessionRestorePreview
                {
                    Error = LocalizationService.Get(
                        "LOCPlaytimeInsightsEmptyRestorePath",
                        "恢复文件路径为空。")
                };
            }

            lock (syncRoot)
            {
                if (!LooksLikeFullBackupFile(sourcePath))
                {
                    return new SessionRestorePreview
                    {
                        Error = LocalizationService.Get(
                            "LOCPlaytimeInsightsNotFullBackup",
                            "该文件不是完整备份（缺少 ActiveSessions）；筛选导出文件请使用“导入”。")
                    };
                }

                SessionStoreDocument loaded;
                Exception error;
                if (!serializer.TryDeserialize(sourcePath, out loaded, out error) ||
                    loaded == null)
                {
                    return new SessionRestorePreview
                    {
                        Error = error?.Message ?? LocalizationService.Get(
                            "LOCPlaytimeInsightsReadRestoreFailed",
                            "无法读取恢复文件。")
                    };
                }

                string validationError;
                if (!TryValidateRestoreDocument(loaded, out validationError))
                {
                    return new SessionRestorePreview
                    {
                        Error = validationError
                    };
                }

                return new SessionRestorePreview
                {
                    IsValid = true,
                    SchemaVersion = loaded.SchemaVersion,
                    SessionCount = loaded.Sessions.Count,
                    ActiveSessionCount = loaded.ActiveSessions?.Count ?? 0
                };
            }
        }

        public SessionRestoreResult RestoreBackup(string sourcePath)
        {
            lock (syncRoot)
            {
                EnsureStorageWritable();
                if (!LooksLikeFullBackupFile(sourcePath))
                {
                    throw new InvalidDataException(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsNotFullBackup",
                            "该文件不是完整备份（缺少 ActiveSessions）；筛选导出文件请使用“导入”。"));
                }

                SessionStoreDocument loaded;
                Exception error;
                if (!serializer.TryDeserialize(sourcePath, out loaded, out error) ||
                    loaded == null)
                {
                    throw new InvalidDataException(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsReadRestoreFailed",
                            "无法读取恢复文件。"),
                        error);
                }

                string validationError;
                if (!TryValidateRestoreDocument(loaded, out validationError))
                {
                    throw new InvalidDataException(validationError);
                }

                var rollbackPath = CreateRollbackBackupUnsafe("pre-restore");
                var original = CloneDocument(document);
                try
                {
                    var restored = NormalizeDocument(CloneDocument(loaded));
                    // Never resurrect stale running-game checkpoints from a backup.
                    restored.ActiveSessions = document.ActiveSessions
                        .Select(CloneActive)
                        .ToList();
                    document = restored;
                    SaveUnsafe();
                }
                catch
                {
                    document = original;
                    throw;
                }

                return new SessionRestoreResult
                {
                    SessionCount = document.Sessions.Count,
                    RollbackBackupPath = rollbackPath
                };
            }
        }

        public SessionReindexResult Reindex()
        {
            lock (syncRoot)
            {
                EnsureStorageWritable();
                var rollbackPath = CreateRollbackBackupUnsafe("pre-reindex");
                var accepted = new List<GameSession>();
                var usedIds = new HashSet<Guid>();
                var removedDuplicates = 0;
                var repairedIds = 0;

                foreach (var source in document.Sessions
                    .OrderBy(session => session.StartedAtUtc)
                    .ThenBy(session => session.Id))
                {
                    var candidate = CloneSession(source);
                    candidate.SchemaVersion = GameSession.CurrentSchemaVersion;
                    if (candidate.Id == Guid.Empty || usedIds.Contains(candidate.Id))
                    {
                        candidate.Id = Guid.NewGuid();
                        repairedIds++;
                    }

                    if (accepted.Any(existing => SameFingerprint(existing, candidate)))
                    {
                        removedDuplicates++;
                        continue;
                    }

                    usedIds.Add(candidate.Id);
                    accepted.Add(candidate);
                }

                document.Sessions = accepted;
                SaveUnsafe();
                return new SessionReindexResult
                {
                    SessionCount = accepted.Count,
                    RemovedDuplicateCount = removedDuplicates,
                    RepairedIdCount = repairedIds,
                    RollbackBackupPath = rollbackPath
                };
            }
        }

        public void CheckpointActiveSessions(DateTime checkpointUtc)
        {
            lock (syncRoot)
            {
                if (document.ActiveSessions.Count == 0)
                {
                    return;
                }

                foreach (var active in document.ActiveSessions)
                {
                    if (checkpointUtc > active.LastCheckpointUtc)
                    {
                        active.LastCheckpointUtc = checkpointUtc;
                    }
                }

                SaveUnsafe();
            }
        }

        public int RecoverActiveSessions(DateTime maximumEndUtc, string reason)
        {
            lock (syncRoot)
            {
                if (document.ActiveSessions.Count == 0)
                {
                    return 0;
                }

                var recoveredCount = 0;
                foreach (var active in document.ActiveSessions.ToList())
                {
                    var endedAtUtc = active.LastCheckpointUtc;
                    if (endedAtUtc < active.StartedAtUtc)
                    {
                        endedAtUtc = active.StartedAtUtc;
                    }

                    if (endedAtUtc > maximumEndUtc)
                    {
                        endedAtUtc = maximumEndUtc;
                    }

                    var elapsed = endedAtUtc - active.StartedAtUtc;
                    var elapsedSeconds = elapsed.TotalSeconds <= 0
                        ? 0UL
                        : (ulong)Math.Floor(elapsed.TotalSeconds);
                    var session = new GameSession
                    {
                        GameId = active.GameId,
                        GameName = active.GameName,
                        GameSourceName = active.GameSourceName,
                        PlatformNames = active.PlatformNames,
                        StartedAtUtc = DateTime.SpecifyKind(active.StartedAtUtc, DateTimeKind.Utc),
                        EndedAtUtc = DateTime.SpecifyKind(endedAtUtc, DateTimeKind.Utc),
                        ElapsedSeconds = elapsedSeconds,
                        StartUtcOffsetMinutes = active.StartUtcOffsetMinutes,
                        EndUtcOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(endedAtUtc).TotalMinutes,
                        TimeZoneId = active.TimeZoneId,
                        Source = SessionSource.Recovered,
                        RecoveryReason = reason
                    };

                    if (!IsDuplicateUnsafe(session))
                    {
                        document.Sessions.Add(session);
                        recoveredCount++;
                    }
                }

                document.ActiveSessions.Clear();
                SaveUnsafe();
                return recoveredCount;
            }
        }

        public int DiscardActiveSessions()
        {
            lock (syncRoot)
            {
                var count = document.ActiveSessions.Count;
                if (count == 0)
                {
                    return 0;
                }

                document.ActiveSessions.Clear();
                SaveUnsafe();
                return count;
            }
        }

        private SessionStoreDocument Load()
        {
            Directory.CreateDirectory(dataDirectory);

            if (!File.Exists(sessionsPath))
            {
                return new SessionStoreDocument();
            }

            try
            {
                SessionStoreDocument loaded;
                Exception error;
                if (serializer.TryDeserialize(sessionsPath, out loaded, out error) && loaded != null)
                {
                    return NormalizeDocument(loaded);
                }

                logger.Error(error, "Could not deserialize Playtime Insights session store.");

                if (File.Exists(backupPath) &&
                    serializer.TryDeserialize(backupPath, out loaded, out error) &&
                    loaded != null)
                {
                    loadedFromBackup = true;
                    logger.Warn("Loaded Playtime Insights sessions from the backup file.");
                    return NormalizeDocument(loaded);
                }

                if (error != null)
                {
                    logger.Error(error, "Could not deserialize Playtime Insights session backup.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not load Playtime Insights session store.");
            }

            storageWritable = false;
            return new SessionStoreDocument();
        }

        private static SessionStoreDocument NormalizeDocument(SessionStoreDocument loaded)
        {
            if (loaded.Sessions == null)
            {
                loaded.Sessions = new List<GameSession>();
            }

            if (loaded.ActiveSessions == null)
            {
                loaded.ActiveSessions = new List<ActiveGameSession>();
            }

            loaded.SchemaVersion = GameSession.CurrentSchemaVersion;
            foreach (var session in loaded.Sessions)
            {
                session.SchemaVersion = GameSession.CurrentSchemaVersion;
            }
            return loaded;
        }

        private bool IsDuplicateUnsafe(GameSession candidate, Guid? ignoredSessionId = null)
        {
            return document.Sessions.Any(existing =>
                (!ignoredSessionId.HasValue || existing.Id != ignoredSessionId.Value) &&
                (
                existing.Id == candidate.Id ||
                (existing.GameId == candidate.GameId &&
                 existing.ElapsedSeconds == candidate.ElapsedSeconds &&
                 Math.Abs((existing.StartedAtUtc - candidate.StartedAtUtc).TotalSeconds) <= 2)
                ));
        }

        private static bool SameFingerprint(GameSession left, GameSession right)
        {
            return left.GameId == right.GameId &&
                   left.ElapsedSeconds == right.ElapsedSeconds &&
                   Math.Abs((left.StartedAtUtc - right.StartedAtUtc).TotalSeconds) <= 2;
        }

        private static ActiveGameSession CloneActive(ActiveGameSession source)
        {
            return new ActiveGameSession
            {
                GameId = source.GameId,
                GameName = source.GameName,
                GameSourceName = source.GameSourceName,
                PlatformNames = source.PlatformNames,
                StartedAtUtc = source.StartedAtUtc,
                LastCheckpointUtc = source.LastCheckpointUtc,
                StartUtcOffsetMinutes = source.StartUtcOffsetMinutes,
                TimeZoneId = source.TimeZoneId
            };
        }

        private static GameSession CloneSession(GameSession source)
        {
            return new GameSession
            {
                SchemaVersion = source.SchemaVersion,
                Id = source.Id,
                GameId = source.GameId,
                GameName = source.GameName,
                GameSourceName = source.GameSourceName,
                PlatformNames = source.PlatformNames,
                StartedAtUtc = source.StartedAtUtc,
                EndedAtUtc = source.EndedAtUtc,
                ElapsedSeconds = source.ElapsedSeconds,
                StartUtcOffsetMinutes = source.StartUtcOffsetMinutes,
                EndUtcOffsetMinutes = source.EndUtcOffsetMinutes,
                TimeZoneId = source.TimeZoneId,
                ManuallyStopped = source.ManuallyStopped,
                Source = source.Source,
                RecoveryReason = source.RecoveryReason,
                IsDeleted = source.IsDeleted,
                DeletedAtUtc = source.DeletedAtUtc,
                LastModifiedAtUtc = source.LastModifiedAtUtc,
                LastModifiedReason = source.LastModifiedReason,
                ImportSource = source.ImportSource,
                ImportConfidence = source.ImportConfidence
            };
        }

        private static SessionStoreDocument CloneDocument(SessionStoreDocument source)
        {
            return new SessionStoreDocument
            {
                SchemaVersion = source.SchemaVersion,
                UpdatedAtUtc = source.UpdatedAtUtc,
                Sessions = (source.Sessions ?? new List<GameSession>())
                    .Select(CloneSession)
                    .ToList(),
                ActiveSessions = (source.ActiveSessions ??
                    new List<ActiveGameSession>())
                    .Select(CloneActive)
                    .ToList()
            };
        }

        private static bool TryValidateRestoreDocument(
            SessionStoreDocument candidate,
            out string error)
        {
            if (candidate.Sessions == null)
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsMissingSessionsCollection",
                    "恢复文件缺少 Sessions 集合。");
                return false;
            }

            for (var index = 0; index < candidate.Sessions.Count; index++)
            {
                var session = candidate.Sessions[index];
                if (session == null)
                {
                    error = LocalizationService.Format(
                        "LOCPlaytimeInsightsEmptySessionAtIndexFormat",
                        "第 {0} 条会话为空。",
                        index + 1);
                    return false;
                }
                if (session.GameId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(session.GameName))
                {
                    error = LocalizationService.Format(
                        "LOCPlaytimeInsightsMissingGameAtIndexFormat",
                        "第 {0} 条会话缺少游戏标识或名称。",
                        index + 1);
                    return false;
                }
                if (session.StartedAtUtc == DateTime.MinValue ||
                    session.EndedAtUtc < session.StartedAtUtc)
                {
                    error = LocalizationService.Format(
                        "LOCPlaytimeInsightsInvalidTimeAtIndexFormat",
                        "第 {0} 条会话时间无效。",
                        index + 1);
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool LooksLikeFullBackupFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            return json.IndexOf(
                "\"ActiveSessions\"",
                StringComparison.OrdinalIgnoreCase) >= 0 &&
                json.IndexOf(
                    "\"Sessions\"",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string CreateRollbackBackupUnsafe(string reason)
        {
            var directory = Path.Combine(dataDirectory, "Backups");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(
                directory,
                string.Format(
                    "sessions.{0}.{1}.json",
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),
                    reason));
            WriteDocumentSnapshotUnsafe(path, CloneDocument(document));
            return path;
        }

        private void WriteDocumentSnapshotUnsafe(
            string destinationPath,
            SessionStoreDocument snapshot)
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidDataException(LocalizationService.Get(
                    "LOCPlaytimeInsightsInvalidBackupDirectory",
                    "备份目标目录无效。"));
            }

            Directory.CreateDirectory(directory);
            snapshot.SchemaVersion = GameSession.CurrentSchemaVersion;
            snapshot.UpdatedAtUtc = DateTime.UtcNow;
            var temporaryPath = fullPath + ".tmp." + Guid.NewGuid().ToString("N");
            File.WriteAllText(
                temporaryPath,
                serializer.Serialize(snapshot),
                new UTF8Encoding(false));
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null, true);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            catch
            {
                File.Copy(temporaryPath, fullPath, true);
                File.Delete(temporaryPath);
            }
        }

        private void EnsureStorageWritable()
        {
            if (!storageWritable)
            {
                throw new InvalidDataException(
                    "The session store and its backup could not be read. Existing files were not overwritten.");
            }
        }

        private void SaveUnsafe()
        {
            EnsureStorageWritable();

            document.SchemaVersion = GameSession.CurrentSchemaVersion;
            document.UpdatedAtUtc = DateTime.UtcNow;
            Directory.CreateDirectory(dataDirectory);
            var temporaryPath = sessionsPath + ".tmp";
            var json = serializer.Serialize(document);

            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            try
            {
                if (loadedFromBackup)
                {
                    File.Copy(temporaryPath, sessionsPath, true);
                    File.Delete(temporaryPath);
                    loadedFromBackup = false;
                }
                else if (File.Exists(sessionsPath))
                {
                    File.Replace(temporaryPath, sessionsPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, sessionsPath);
                }
            }
            catch
            {
                if (File.Exists(sessionsPath))
                {
                    File.Copy(sessionsPath, backupPath, true);
                }

                File.Copy(temporaryPath, sessionsPath, true);
                File.Delete(temporaryPath);
            }
        }

        private static long GetFileLength(string path)
        {
            try
            {
                return File.Exists(path) ? new FileInfo(path).Length : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private static int GetJsonFileCount(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.GetFiles(
                        directory,
                        "*.json",
                        SearchOption.TopDirectoryOnly).Length
                    : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
