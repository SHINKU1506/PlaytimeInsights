using System;
using System.Collections.Generic;
using System.Linq;
using PlaytimeInsights.Services;

namespace PlaytimeInsights.Models
{
    public sealed class SessionImportPreview
    {
        public List<GameSession> Candidates { get; set; } = new List<GameSession>();

        public List<string> Errors { get; set; } = new List<string>();

        public int ParsedCount { get; set; }

        public int DuplicateCount { get; set; }

        public int InvalidCount { get; set; }

        public int FileCount { get; set; }

        public string FormatSummary { get; set; } = string.Empty;

        public int ImportableCount => Candidates.Count;

        public bool CanImport => Candidates.Count > 0;

        public string Summary => LocalizationService.Format(
            "LOCPlaytimeInsightsImportSummaryFormat",
            "文件 {0:N0} 个；解析 {1:N0} 条；可导入 {2:N0} 条；重复 {3:N0} 条；无效 {4:N0} 条",
            FileCount,
            ParsedCount,
            ImportableCount,
            DuplicateCount,
            InvalidCount);

        public string FormatSummaryText => LocalizationService.Format(
            "LOCPlaytimeInsightsDetectedFormatFormat",
            "识别格式：{0}",
            FormatSummary);

        public string ErrorCountText => LocalizationService.Format(
            "LOCPlaytimeInsightsErrorsCountFormat",
            "错误报告（{0:N0}）",
            Errors.Count);

        public IList<SessionImportCandidateViewModel> CandidateItems =>
            Candidates.Select(session =>
                new SessionImportCandidateViewModel
                {
                    GameName = session.GameName,
                    StartedAtText = session.StartedAtUtc.ToString(
                        "yyyy-MM-dd HH:mm:ss 'UTC'"),
                    ElapsedText = LocalizationService.Format(
                        "LOCPlaytimeInsightsSecondsFormat",
                        "{0:N0} 秒",
                        session.ElapsedSeconds),
                    ImportConfidence = session.ImportConfidence
                }).ToList();
    }

    public sealed class SessionImportCandidateViewModel
    {
        public string GameName { get; set; }

        public string StartedAtText { get; set; }

        public string ElapsedText { get; set; }

        public string ImportConfidence { get; set; }
    }

    public sealed class SessionImportCommitResult
    {
        public int ImportedCount { get; set; }

        public int DuplicateCount { get; set; }

        public string RollbackBackupPath { get; set; } = string.Empty;
    }

    public sealed class SessionRestorePreview
    {
        public bool IsValid { get; set; }

        public int SessionCount { get; set; }

        public int ActiveSessionCount { get; set; }

        public int SchemaVersion { get; set; }

        public string Error { get; set; } = string.Empty;
    }

    public sealed class SessionRestoreResult
    {
        public int SessionCount { get; set; }

        public string RollbackBackupPath { get; set; } = string.Empty;
    }

    public sealed class SessionReindexResult
    {
        public int SessionCount { get; set; }

        public int RemovedDuplicateCount { get; set; }

        public int RepairedIdCount { get; set; }

        public string RollbackBackupPath { get; set; } = string.Empty;
    }
}
