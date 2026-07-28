using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlaytimeInsights.Services
{
    public interface ISessionImportJsonSerializer
    {
        bool TryDeserializeExport(
            string json,
            out SessionExportDocument document,
            out Exception error);

        bool TryDeserializeStore(
            string json,
            out SessionStoreDocument document,
            out Exception error);

        bool TryDeserializeGameActivity(
            string json,
            out GameActivityImportDocument document,
            out Exception error);
    }

    public sealed class PlayniteImportJsonSerializer : ISessionImportJsonSerializer
    {
        public bool TryDeserializeExport(
            string json,
            out SessionExportDocument document,
            out Exception error)
        {
            return Serialization.TryFromJson(json, out document, out error);
        }

        public bool TryDeserializeStore(
            string json,
            out SessionStoreDocument document,
            out Exception error)
        {
            return Serialization.TryFromJson(json, out document, out error);
        }

        public bool TryDeserializeGameActivity(
            string json,
            out GameActivityImportDocument document,
            out Exception error)
        {
            return Serialization.TryFromJson(json, out document, out error);
        }
    }

    public sealed class GameActivityImportDocument
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool GameExist { get; set; } = true;

        public List<GameActivityImportItem> Items { get; set; } =
            new List<GameActivityImportItem>();
    }

    public sealed class GameActivityImportItem
    {
        public DateTime DateSession { get; set; }

        public ulong ElapsedSeconds { get; set; }
    }

    internal sealed class ImportedSessionRow
    {
        public GameSession Session { get; set; }

        public string Location { get; set; }

        public string ImportSource { get; set; }

        public bool NeedsNameMatch { get; set; }

        public string ParseError { get; set; }
    }

    internal sealed class CsvRecord
    {
        public int RowNumber { get; set; }

        public List<string> Values { get; set; }
    }

    public sealed class SessionImportService
    {
        private const ulong MaximumElapsedSeconds = 31536000UL;
        private readonly ISessionImportJsonSerializer jsonSerializer;

        public SessionImportService(ISessionImportJsonSerializer jsonSerializer = null)
        {
            this.jsonSerializer = jsonSerializer ?? new PlayniteImportJsonSerializer();
        }

        public SessionImportPreview Preview(
            IEnumerable<string> paths,
            IEnumerable<Game> games,
            IEnumerable<GameSession> existingSessions)
        {
            var preview = new SessionImportPreview();
            var gameList = (games ?? Enumerable.Empty<Game>()).ToList();
            var existing = (existingSessions ?? Enumerable.Empty<GameSession>()).ToList();
            var accepted = new List<GameSession>();
            var formats = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var inputPaths = (paths ?? Enumerable.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
            preview.FileCount = inputPaths.Count;

            foreach (var path in inputPaths)
            {
                try
                {
                    var rows = ParseFile(path, formats);
                    foreach (var row in rows)
                    {
                        preview.ParsedCount++;
                        GameSession normalized;
                        string error;
                        if (!TryNormalize(row, gameList, out normalized, out error))
                        {
                            preview.InvalidCount++;
                            preview.Errors.Add(LocalizationService.Format(
                                "LOCPlaytimeInsightsLocationErrorFormat",
                                "{0}：{1}",
                                row.Location,
                                error));
                            continue;
                        }

                        if (existing.Any(item => IsDuplicate(item, normalized)) ||
                            accepted.Any(item => IsDuplicate(item, normalized)))
                        {
                            preview.DuplicateCount++;
                            continue;
                        }

                        accepted.Add(normalized);
                    }
                }
                catch (Exception ex)
                {
                    preview.InvalidCount++;
                    preview.Errors.Add(LocalizationService.Format(
                        "LOCPlaytimeInsightsLocationErrorFormat",
                        "{0}：{1}",
                        Path.GetFileName(path),
                        ex.Message));
                }
            }

            preview.Candidates = accepted
                .OrderBy(session => session.StartedAtUtc)
                .ThenBy(session => session.GameName)
                .ToList();
            preview.FormatSummary = formats.Count == 0
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsUnrecognized",
                    "未识别")
                : string.Join(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsFormatSeparator",
                        "、"),
                    formats.OrderBy(value => value));
            return preview;
        }

        private IList<ImportedSessionRow> ParseFile(
            string path,
            ISet<string> formats)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsImportFileNotFound",
                        "找不到导入文件。"),
                    path);
            }

            var extension = Path.GetExtension(path);
            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ParseCsvFile(path, formats);
            }

            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return ParseJsonFile(path, formats);
            }

            throw new InvalidDataException(LocalizationService.Get(
                "LOCPlaytimeInsightsUnsupportedImportFile",
                "仅支持 .json 和 .csv 文件。"));
        }

        private IList<ImportedSessionRow> ParseJsonFile(
            string path,
            ISet<string> formats)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var fileName = Path.GetFileName(path);
            Exception error;

            if (ContainsJsonProperty(json, "ActiveSessions"))
            {
                SessionStoreDocument store;
                if (!jsonSerializer.TryDeserializeStore(json, out store, out error) ||
                    store?.Sessions == null)
                {
                    throw new InvalidDataException(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsReadBackupJsonFailed",
                            "无法读取 Playtime Insights 备份 JSON。"),
                        error);
                }

                formats.Add(LocalizationService.Get(
                    "LOCPlaytimeInsightsBackupJsonFormat",
                    "Playtime Insights 备份 JSON"));
                return store.Sessions.Select((session, index) => new ImportedSessionRow
                {
                    Session = session,
                    Location = LocalizationService.Format(
                        "LOCPlaytimeInsightsSessionLocationFormat",
                        "{0} / 会话 {1}",
                        fileName,
                        index + 1),
                    ImportSource = "PlaytimeInsightsBackupJson"
                }).ToList();
            }

            if (ContainsJsonProperty(json, "Sessions"))
            {
                SessionExportDocument export;
                if (!jsonSerializer.TryDeserializeExport(json, out export, out error) ||
                    export?.Sessions == null)
                {
                    throw new InvalidDataException(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsReadExportJsonFailed",
                            "无法读取 Playtime Insights 导出 JSON。"),
                        error);
                }

                formats.Add("Playtime Insights JSON");
                return export.Sessions.Select((session, index) => new ImportedSessionRow
                {
                    Session = session,
                    Location = LocalizationService.Format(
                        "LOCPlaytimeInsightsSessionLocationFormat",
                        "{0} / 会话 {1}",
                        fileName,
                        index + 1),
                    ImportSource = "PlaytimeInsightsJson"
                }).ToList();
            }

            if (ContainsJsonProperty(json, "Items") &&
                ContainsJsonProperty(json, "DateSession"))
            {
                GameActivityImportDocument gameActivity;
                if (!jsonSerializer.TryDeserializeGameActivity(
                        json,
                        out gameActivity,
                        out error) ||
                    gameActivity?.Items == null)
                {
                    throw new InvalidDataException(
                        LocalizationService.Get(
                            "LOCPlaytimeInsightsReadGameActivityJsonFailed",
                            "无法读取 GameActivity JSON。"),
                        error);
                }

                formats.Add("GameActivity JSON");
                return gameActivity.Items.Select((item, index) =>
                {
                    var started = EnsureUtc(item.DateSession);
                    return new ImportedSessionRow
                    {
                        Session = new GameSession
                        {
                            GameId = gameActivity.Id,
                            GameName = gameActivity.Name ?? string.Empty,
                            StartedAtUtc = started,
                            EndedAtUtc = SafeAddSeconds(started, item.ElapsedSeconds),
                            ElapsedSeconds = item.ElapsedSeconds
                        },
                        Location = string.Format("{0} / Items[{1}]", fileName, index),
                        ImportSource = "GameActivityJson",
                        NeedsNameMatch = gameActivity.Id == Guid.Empty
                    };
                }).ToList();
            }

            throw new InvalidDataException(
                LocalizationService.Get(
                    "LOCPlaytimeInsightsUnrecognizedJson",
                    "无法识别 JSON：需要 Playtime Insights 导出/备份或 GameActivity 单游戏数据。"));
        }

        private IList<ImportedSessionRow> ParseCsvFile(
            string path,
            ISet<string> formats)
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            var records = ParseCsv(text);
            if (records.Count < 1)
            {
                throw new InvalidDataException(LocalizationService.Get(
                    "LOCPlaytimeInsightsEmptyCsv",
                    "CSV 文件为空。"));
            }

            var headers = records[0].Values
                .Select((value, index) => new { value, index })
                .GroupBy(item => (item.value ?? string.Empty).Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().index,
                    StringComparer.OrdinalIgnoreCase);
            var fileName = Path.GetFileName(path);

            if (headers.ContainsKey("StartedAtUtc") &&
                headers.ContainsKey("GameId") &&
                headers.ContainsKey("ElapsedSeconds"))
            {
                formats.Add("Playtime Insights CSV");
                return records.Skip(1)
                    .Where(record => record.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
                    .Select(record => SafeParseCsvRow(
                        record,
                        fileName,
                        () => ParsePlaytimeInsightsCsv(
                            record,
                            headers,
                            fileName)))
                    .ToList();
            }

            if (HasHeader(
                    headers,
                    "DateSession",
                    "Session",
                    "Date session",
                    "会话日期") &&
                HasHeader(
                    headers,
                    "PlaytimeSeconds",
                    "ElapsedSeconds",
                    "Playtime",
                    "Time Played",
                    "游玩时间") &&
                HasHeader(
                    headers,
                    "GameName",
                    "Name",
                    "Game",
                    "名称"))
            {
                formats.Add("GameActivity CSV");
                return records.Skip(1)
                    .Where(record => record.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
                    .Select(record => SafeParseCsvRow(
                        record,
                        fileName,
                        () => ParseGameActivityCsv(
                            record,
                            headers,
                            fileName)))
                    .ToList();
            }

            throw new InvalidDataException(
                LocalizationService.Get(
                    "LOCPlaytimeInsightsUnrecognizedCsvHeaders",
                    "无法识别 CSV 表头；请选择 Playtime Insights 或 GameActivity 的会话明细导出。"));
        }

        private static ImportedSessionRow ParsePlaytimeInsightsCsv(
            CsvRecord record,
            IDictionary<string, int> headers,
            string fileName)
        {
            var session = new GameSession
            {
                Id = ParseGuid(Get(record, headers, "Id")),
                GameId = ParseGuid(Get(record, headers, "GameId")),
                GameName = Get(record, headers, "GameName"),
                GameSourceName = Get(record, headers, "GameSourceName"),
                PlatformNames = Get(record, headers, "PlatformNames"),
                StartedAtUtc = ParseUtc(Get(record, headers, "StartedAtUtc")),
                EndedAtUtc = ParseUtc(Get(record, headers, "EndedAtUtc")),
                ElapsedSeconds = ParseUlong(Get(record, headers, "ElapsedSeconds")),
                StartUtcOffsetMinutes = ParseInt(
                    Get(record, headers, "StartUtcOffsetMinutes")),
                EndUtcOffsetMinutes = ParseInt(
                    Get(record, headers, "EndUtcOffsetMinutes")),
                TimeZoneId = Get(record, headers, "TimeZoneId"),
                ManuallyStopped = ParseBool(Get(record, headers, "ManuallyStopped")),
                RecoveryReason = Get(record, headers, "RecoveryReason"),
                IsDeleted = ParseBool(Get(record, headers, "IsDeleted")),
                DeletedAtUtc = ParseNullableUtc(Get(record, headers, "DeletedAtUtc")),
                LastModifiedAtUtc = ParseNullableUtc(
                    Get(record, headers, "LastModifiedAtUtc")),
                LastModifiedReason = Get(record, headers, "LastModifiedReason"),
                ImportSource = Get(record, headers, "ImportSource"),
                ImportConfidence = Get(record, headers, "ImportConfidence")
            };

            return new ImportedSessionRow
            {
                Session = session,
                Location = LocalizationService.Format(
                    "LOCPlaytimeInsightsRowLocationFormat",
                    "{0} / 第 {1} 行",
                    fileName,
                    record.RowNumber),
                ImportSource = "PlaytimeInsightsCsv"
            };
        }

        private static ImportedSessionRow ParseGameActivityCsv(
            CsvRecord record,
            IDictionary<string, int> headers,
            string fileName)
        {
            var name = FirstValue(
                record,
                headers,
                "GameName",
                "Name",
                "Game",
                "名称");
            var dateText = FirstValue(
                record,
                headers,
                "DateSession",
                "Session",
                "Date session",
                "会话日期");
            var secondsText = FirstValue(
                record,
                headers,
                "PlaytimeSeconds",
                "ElapsedSeconds",
                "Playtime",
                "Time Played",
                "游玩时间");
            var started = ParseGameActivityCsvDate(dateText);
            var seconds = ParseUlong(secondsText);
            var gameId = ParseGuid(FirstValue(record, headers, "GameId", "Id"));

            return new ImportedSessionRow
            {
                Session = new GameSession
                {
                    GameId = gameId,
                    GameName = name,
                    GameSourceName = FirstValue(
                        record,
                        headers,
                        "Source",
                        "SourceName",
                        "来源"),
                    StartedAtUtc = started,
                    EndedAtUtc = SafeAddSeconds(started, seconds),
                    ElapsedSeconds = seconds
                },
                Location = LocalizationService.Format(
                    "LOCPlaytimeInsightsRowLocationFormat",
                    "{0} / 第 {1} 行",
                    fileName,
                    record.RowNumber),
                ImportSource = "GameActivityCsv",
                NeedsNameMatch = gameId == Guid.Empty
            };
        }

        private static bool TryNormalize(
            ImportedSessionRow row,
            IList<Game> games,
            out GameSession normalized,
            out string error)
        {
            normalized = null;
            error = string.Empty;
            if (!string.IsNullOrWhiteSpace(row?.ParseError))
            {
                error = row.ParseError;
                return false;
            }
            if (row?.Session == null)
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsMissingSessionObject",
                    "缺少会话对象。");
                return false;
            }

            var source = row.Session;
            var game = source.GameId == Guid.Empty
                ? null
                : games.FirstOrDefault(item => item.Id == source.GameId);
            var confidence = game == null ? "ExternalGameId" : "ExactGameId";

            if (source.GameId == Guid.Empty || row.NeedsNameMatch)
            {
                var matches = games.Where(item =>
                    string.Equals(
                        item.Name?.Trim(),
                        source.GameName?.Trim(),
                        StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                if (matches.Count > 1)
                {
                    error = LocalizationService.Get(
                        "LOCPlaytimeInsightsAmbiguousGameName",
                        "存在多个同名 Playnite 游戏，无法安全关联。");
                    return false;
                }

                if (matches.Count == 1)
                {
                    game = matches[0];
                    source.GameId = game.Id;
                    confidence = "UniqueNameMatch";
                }
                else
                {
                    source.GameId = CreateStableExternalGameId(source.GameName);
                    confidence = "UnmatchedNameSnapshot";
                }
            }

            if (string.IsNullOrWhiteSpace(source.GameName))
            {
                source.GameName = game?.Name ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(source.GameName))
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsEmptyGameName",
                    "游戏名称为空。");
                return false;
            }

            if (source.ElapsedSeconds > MaximumElapsedSeconds)
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsDurationSafetyLimit",
                    "持续时长超过 365 天安全上限。");
                return false;
            }

            var started = EnsureUtc(source.StartedAtUtc);
            if (started == DateTime.MinValue || started.Year < 1970)
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsInvalidStartTime",
                    "开始时间无效或早于 1970 年。");
                return false;
            }

            var ended = source.EndedAtUtc == DateTime.MinValue
                ? SafeAddSeconds(started, source.ElapsedSeconds)
                : EnsureUtc(source.EndedAtUtc);
            if (ended < started)
            {
                error = LocalizationService.Get(
                    "LOCPlaytimeInsightsEndBeforeStart",
                    "结束时间早于开始时间。");
                return false;
            }

            if (Math.Abs((ended - started).TotalSeconds -
                         source.ElapsedSeconds) > 2)
            {
                ended = SafeAddSeconds(started, source.ElapsedSeconds);
            }

            var hasTimeZoneSnapshot = !string.IsNullOrWhiteSpace(source.TimeZoneId);
            var startOffset = hasTimeZoneSnapshot &&
                              IsValidOffset(source.StartUtcOffsetMinutes)
                ? source.StartUtcOffsetMinutes
                : (int)TimeZoneInfo.Local.GetUtcOffset(started).TotalMinutes;
            var endOffset = hasTimeZoneSnapshot &&
                            IsValidOffset(source.EndUtcOffsetMinutes)
                ? source.EndUtcOffsetMinutes
                : (int)TimeZoneInfo.Local.GetUtcOffset(ended).TotalMinutes;

            normalized = new GameSession
            {
                SchemaVersion = GameSession.CurrentSchemaVersion,
                Id = source.Id == Guid.Empty ? Guid.NewGuid() : source.Id,
                GameId = source.GameId,
                GameName = source.GameName.Trim(),
                GameSourceName = string.IsNullOrWhiteSpace(source.GameSourceName)
                    ? game?.Source?.Name ?? string.Empty
                    : source.GameSourceName,
                PlatformNames = string.IsNullOrWhiteSpace(source.PlatformNames)
                    ? GetPlatformNames(game)
                    : source.PlatformNames,
                StartedAtUtc = started,
                EndedAtUtc = ended,
                ElapsedSeconds = source.ElapsedSeconds,
                StartUtcOffsetMinutes = startOffset,
                EndUtcOffsetMinutes = endOffset,
                TimeZoneId = string.IsNullOrWhiteSpace(source.TimeZoneId)
                    ? TimeZoneInfo.Local.Id
                    : source.TimeZoneId,
                ManuallyStopped = source.ManuallyStopped,
                Source = SessionSource.Imported,
                RecoveryReason = source.RecoveryReason ?? string.Empty,
                IsDeleted = source.IsDeleted,
                DeletedAtUtc = source.DeletedAtUtc,
                LastModifiedAtUtc = DateTime.UtcNow,
                LastModifiedReason = "Imported",
                ImportSource = string.IsNullOrWhiteSpace(source.ImportSource)
                    ? row.ImportSource
                    : source.ImportSource,
                ImportConfidence = string.IsNullOrWhiteSpace(source.ImportConfidence)
                    ? confidence
                    : source.ImportConfidence
            };
            return true;
        }

        internal static bool IsDuplicate(GameSession left, GameSession right)
        {
            return left != null && right != null &&
                (left.Id == right.Id ||
                 (left.GameId == right.GameId &&
                  left.ElapsedSeconds == right.ElapsedSeconds &&
                  Math.Abs((EnsureUtc(left.StartedAtUtc) -
                            EnsureUtc(right.StartedAtUtc)).TotalSeconds) <= 2));
        }

        internal static IList<CsvRecord> ParseCsv(string text)
        {
            var records = new List<CsvRecord>();
            var values = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var rowNumber = 1;
            var recordStart = 1;
            var delimiter = DetectCsvDelimiter(text);

            for (var index = 0; index < (text ?? string.Empty).Length; index++)
            {
                var current = text[index];
                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(current);
                        if (current == '\n')
                        {
                            rowNumber++;
                        }
                    }
                    continue;
                }

                if (current == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (current == delimiter)
                {
                    values.Add(field.ToString());
                    field.Clear();
                }
                else if (current == '\r' || current == '\n')
                {
                    values.Add(field.ToString());
                    field.Clear();
                    records.Add(new CsvRecord
                    {
                        RowNumber = recordStart,
                        Values = values
                    });
                    values = new List<string>();
                    if (current == '\r' &&
                        index + 1 < text.Length &&
                        text[index + 1] == '\n')
                    {
                        index++;
                    }
                    rowNumber++;
                    recordStart = rowNumber;
                }
                else
                {
                    field.Append(current);
                }
            }

            if (inQuotes)
            {
                throw new InvalidDataException(LocalizationService.Get(
                    "LOCPlaytimeInsightsUnclosedCsvQuote",
                    "CSV 包含未闭合的双引号。"));
            }

            if (field.Length > 0 || values.Count > 0)
            {
                values.Add(field.ToString());
                records.Add(new CsvRecord
                {
                    RowNumber = recordStart,
                    Values = values
                });
            }

            if (records.Count > 0 && records[0].Values.Count > 0)
            {
                records[0].Values[0] = records[0].Values[0].TrimStart('\uFEFF');
            }
            return records;
        }

        private static string Get(
            CsvRecord record,
            IDictionary<string, int> headers,
            string header)
        {
            int index;
            return headers.TryGetValue(header, out index) &&
                   index >= 0 &&
                   index < record.Values.Count
                ? record.Values[index]
                : string.Empty;
        }

        private static string FirstValue(
            CsvRecord record,
            IDictionary<string, int> headers,
            params string[] names)
        {
            foreach (var name in names)
            {
                var value = Get(record, headers, name);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            return string.Empty;
        }

        private static bool HasHeader(
            IDictionary<string, int> headers,
            params string[] names)
        {
            return names.Any(headers.ContainsKey);
        }

        private static Guid ParseGuid(string value)
        {
            Guid result;
            return Guid.TryParse(value, out result) ? result : Guid.Empty;
        }

        private static ulong ParseUlong(string value)
        {
            ulong result;
            if (!ulong.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                throw new FormatException(LocalizationService.Get(
                    "LOCPlaytimeInsightsInvalidElapsedSeconds",
                    "持续秒数不是有效的非负整数。"));
            }
            return result;
        }

        private static int ParseInt(string value)
        {
            int result;
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result)
                ? result
                : 0;
        }

        private static bool ParseBool(string value)
        {
            bool result;
            return bool.TryParse(value, out result) && result;
        }

        private static DateTime ParseUtc(string value)
        {
            DateTime result;
            var styles = DateTimeStyles.AllowWhiteSpaces |
                         DateTimeStyles.AssumeUniversal |
                         DateTimeStyles.AdjustToUniversal;
            var normalizedText = NormalizeLegacyGameActivityDate(value);
            if (!DateTime.TryParse(
                    normalizedText,
                    CultureInfo.CurrentCulture,
                    styles,
                    out result) &&
                !DateTime.TryParse(
                    normalizedText,
                    CultureInfo.InvariantCulture,
                    styles,
                    out result))
            {
                throw new FormatException(LocalizationService.Get(
                    "LOCPlaytimeInsightsInvalidUtcTime",
                    "时间字段不是有效的 UTC/ISO 8601 时间。"));
            }
            return EnsureUtc(result);
        }

        private static ImportedSessionRow SafeParseCsvRow(
            CsvRecord record,
            string fileName,
            Func<ImportedSessionRow> parser)
        {
            try
            {
                return parser();
            }
            catch (Exception ex)
            {
                return new ImportedSessionRow
                {
                    Location = LocalizationService.Format(
                        "LOCPlaytimeInsightsRowLocationFormat",
                        "{0} / 第 {1} 行",
                        fileName,
                        record.RowNumber),
                    ParseError = ex.Message
                };
            }
        }

        private static string NormalizeLegacyGameActivityDate(string value)
        {
            var text = (value ?? string.Empty).Trim();
            var space = text.IndexOf(' ');
            if (space >= 0 && space + 1 < text.Length)
            {
                var prefix = text.Substring(0, space + 1);
                var time = text.Substring(space + 1).Replace('.', ':');
                return prefix + time;
            }
            return text;
        }

        private static DateTime? ParseNullableUtc(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? (DateTime?)null
                : ParseUtc(value);
        }

        private static DateTime ParseGameActivityCsvDate(string value)
        {
            var normalizedText = NormalizeLegacyGameActivityDate(value);
            DateTime parsed;
            var styles = DateTimeStyles.AllowWhiteSpaces;
            if (!DateTime.TryParse(
                    normalizedText,
                    CultureInfo.CurrentCulture,
                    styles,
                    out parsed) &&
                !DateTime.TryParse(
                    normalizedText,
                    CultureInfo.InvariantCulture,
                    styles,
                    out parsed))
            {
                throw new FormatException(LocalizationService.Get(
                    "LOCPlaytimeInsightsInvalidGameActivityDate",
                    "GameActivity 会话日期不是有效时间。"));
            }

            if (parsed.Kind == DateTimeKind.Utc)
            {
                return parsed;
            }
            if (parsed.Kind == DateTimeKind.Local)
            {
                return parsed.ToUniversalTime();
            }

            parsed = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
            if (TimeZoneInfo.Local.IsInvalidTime(parsed))
            {
                throw new FormatException(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsInvalidDstDate",
                        "GameActivity 会话日期落在本地夏令时无效时间。"));
            }
            return TimeZoneInfo.ConvertTimeToUtc(parsed, TimeZoneInfo.Local);
        }

        private static char DetectCsvDelimiter(string text)
        {
            var commaCount = 0;
            var semicolonCount = 0;
            var tabCount = 0;
            var inQuotes = false;
            for (var index = 0; index < (text ?? string.Empty).Length; index++)
            {
                var current = text[index];
                if (current == '"')
                {
                    if (inQuotes &&
                        index + 1 < text.Length &&
                        text[index + 1] == '"')
                    {
                        index++;
                        continue;
                    }
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && (current == '\r' || current == '\n'))
                {
                    break;
                }
                else if (!inQuotes && current == ',')
                {
                    commaCount++;
                }
                else if (!inQuotes && current == ';')
                {
                    semicolonCount++;
                }
                else if (!inQuotes && current == '\t')
                {
                    tabCount++;
                }
            }
            if (tabCount > commaCount && tabCount > semicolonCount)
            {
                return '\t';
            }
            return semicolonCount > commaCount ? ';' : ',';
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }
            if (value.Kind == DateTimeKind.Local)
            {
                return value.ToUniversalTime();
            }
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static DateTime SafeAddSeconds(DateTime startedAtUtc, ulong seconds)
        {
            if (seconds > MaximumElapsedSeconds)
            {
                return startedAtUtc;
            }
            return startedAtUtc.AddSeconds((double)seconds);
        }

        private static bool IsValidOffset(int offset)
        {
            return offset >= -840 && offset <= 840;
        }

        private static string GetPlatformNames(Game game)
        {
            return game?.Platforms == null
                ? string.Empty
                : string.Join(", ", game.Platforms
                    .Where(platform => platform != null)
                    .Select(platform => platform.Name));
        }

        private static Guid CreateStableExternalGameId(string name)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(
                    "PlaytimeInsights.Import:" +
                    (name ?? string.Empty).Trim().ToUpperInvariant()));
                var guidBytes = new byte[16];
                Array.Copy(bytes, guidBytes, guidBytes.Length);
                return new Guid(guidBytes);
            }
        }

        private static bool ContainsJsonProperty(string json, string name)
        {
            return (json ?? string.Empty).IndexOf(
                "\"" + name + "\"",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
