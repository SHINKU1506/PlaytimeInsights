using Playnite.SDK.Data;
using PlaytimeInsights.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PlaytimeInsights.Services
{
    public interface ISessionExportJsonSerializer
    {
        string Serialize(SessionExportDocument document);
    }

    public sealed class PlayniteExportJsonSerializer : ISessionExportJsonSerializer
    {
        public string Serialize(SessionExportDocument document)
        {
            return Serialization.ToJson(document, true);
        }
    }

    public sealed class SessionExportDocument
    {
        public int FormatVersion { get; set; } = 1;

        public DateTime ExportedAtUtc { get; set; }

        public int SessionCount { get; set; }

        public List<GameSession> Sessions { get; set; } = new List<GameSession>();
    }

    public sealed class SessionExportService
    {
        private readonly ISessionExportJsonSerializer jsonSerializer;

        private static readonly string[] CsvHeaders =
        {
            "Id",
            "GameId",
            "GameName",
            "GameSourceName",
            "PlatformNames",
            "StartedAtUtc",
            "EndedAtUtc",
            "ElapsedSeconds",
            "StartUtcOffsetMinutes",
            "EndUtcOffsetMinutes",
            "TimeZoneId",
            "ManuallyStopped",
            "Source",
            "RecoveryReason",
            "SchemaVersion",
            "IsDeleted",
            "DeletedAtUtc",
            "LastModifiedAtUtc",
            "LastModifiedReason",
            "ImportSource",
            "ImportConfidence"
        };

        public SessionExportService(ISessionExportJsonSerializer jsonSerializer = null)
        {
            this.jsonSerializer = jsonSerializer ?? new PlayniteExportJsonSerializer();
        }

        public string CreateCsv(IEnumerable<GameSession> sessions)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", CsvHeaders));
            foreach (var session in sessions ?? Enumerable.Empty<GameSession>())
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape(session.Id.ToString("D")),
                    Escape(session.GameId.ToString("D")),
                    Escape(session.GameName),
                    Escape(session.GameSourceName),
                    Escape(session.PlatformNames),
                    Escape(ToUtcText(session.StartedAtUtc)),
                    Escape(ToUtcText(session.EndedAtUtc)),
                    Escape(session.ElapsedSeconds.ToString(CultureInfo.InvariantCulture)),
                    Escape(session.StartUtcOffsetMinutes.ToString(CultureInfo.InvariantCulture)),
                    Escape(session.EndUtcOffsetMinutes.ToString(CultureInfo.InvariantCulture)),
                    Escape(session.TimeZoneId),
                    Escape(session.ManuallyStopped ? "true" : "false"),
                    Escape(session.Source.ToString()),
                    Escape(session.RecoveryReason),
                    Escape(session.SchemaVersion.ToString(CultureInfo.InvariantCulture)),
                    Escape(session.IsDeleted ? "true" : "false"),
                    Escape(ToNullableUtcText(session.DeletedAtUtc)),
                    Escape(ToNullableUtcText(session.LastModifiedAtUtc)),
                    Escape(session.LastModifiedReason),
                    Escape(session.ImportSource),
                    Escape(session.ImportConfidence)
                }));
            }

            return builder.ToString();
        }

        public string CreateJson(IEnumerable<GameSession> sessions, DateTime exportedAtUtc)
        {
            var values = (sessions ?? Enumerable.Empty<GameSession>()).ToList();
            return jsonSerializer.Serialize(new SessionExportDocument
            {
                ExportedAtUtc = DateTime.SpecifyKind(exportedAtUtc, DateTimeKind.Utc),
                SessionCount = values.Count,
                Sessions = values
            });
        }

        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string ToUtcText(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc)
                .ToString("O", CultureInfo.InvariantCulture);
        }

        private static string ToNullableUtcText(DateTime? value)
        {
            return value.HasValue ? ToUtcText(value.Value) : string.Empty;
        }
    }
}
