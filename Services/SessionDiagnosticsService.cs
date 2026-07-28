using PlaytimeInsights.Models;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace PlaytimeInsights.Services
{
    public sealed class SessionDiagnosticsService
    {
        public string CreateReport(
            SessionStorageDiagnostics diagnostics,
            DateTime generatedAtUtc)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var version = typeof(SessionDiagnosticsService)
                .Assembly
                .GetName()
                .Version;
            var builder = new StringBuilder();
            builder.AppendLine("Playtime Insights diagnostic report");
            builder.AppendLine("==================================");
            builder.AppendLine("GeneratedAtUtc: " +
                generatedAtUtc.ToUniversalTime().ToString(
                    "O",
                    CultureInfo.InvariantCulture));
            builder.AppendLine("PluginVersion: " + version);
            builder.AppendLine("SessionSchemaVersion: " +
                diagnostics.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("SessionCount: " +
                diagnostics.SessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ActiveSessionCount: " +
                diagnostics.ActiveSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("DeletedSessionCount: " +
                diagnostics.DeletedSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("TrackedSessionCount: " +
                diagnostics.TrackedSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("RecoveredSessionCount: " +
                diagnostics.RecoveredSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ImportedSessionCount: " +
                diagnostics.ImportedSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ManualSessionCount: " +
                diagnostics.ManualSessionCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("StoreUpdatedAtUtc: " +
                diagnostics.UpdatedAtUtc.ToUniversalTime().ToString(
                    "O",
                    CultureInfo.InvariantCulture));
            builder.AppendLine("StorageWritable: " + diagnostics.StorageWritable);
            builder.AppendLine("LoadedFromBackup: " + diagnostics.LoadedFromBackup);
            builder.AppendLine("SessionsFileExists: " +
                diagnostics.SessionsFileExists);
            builder.AppendLine("SessionsFileBytes: " +
                diagnostics.SessionsFileBytes.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BackupFileExists: " +
                diagnostics.BackupFileExists);
            builder.AppendLine("BackupFileBytes: " +
                diagnostics.BackupFileBytes.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("RollbackBackupCount: " +
                diagnostics.RollbackBackupCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
            builder.AppendLine(
                "Privacy: this report contains no game names, session timestamps, " +
                "user paths, or session identifiers.");
            builder.AppendLine(
                "Playtime Insights processes session data locally and does not " +
                "upload this report.");
            return builder.ToString();
        }

        public void SaveReport(
            string destinationPath,
            SessionStorageDiagnostics diagnostics,
            DateTime generatedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException(
                    "Diagnostic report destination is required.",
                    nameof(destinationPath));
            }

            File.WriteAllText(
                destinationPath,
                CreateReport(diagnostics, generatedAtUtc),
                new UTF8Encoding(true));
        }
    }
}
