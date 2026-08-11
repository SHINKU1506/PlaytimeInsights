using PlaytimeInsights.Models;
using PlaytimeInsights.ViewModels;
using System.Collections.Generic;

namespace PlaytimeInsights.Presentation.Coordinators
{
    public interface ISessionManagementOperations
    {
        bool CanEdit { get; }

        bool CanDelete { get; }

        int ExportCsv(string path);

        int ExportJson(string path);

        void SaveDiagnostics(string path);

        GameSession GetSelectedSession();

        SessionEditorViewModel CreateEditor(GameSession existing = null);

        bool AddSession(GameSession session);

        bool UpdateSelectedSession(GameSession session);

        bool DeleteSelectedSession();

        SessionImportPreview PreviewImport(IEnumerable<string> paths);

        SessionImportCommitResult CommitImport(SessionImportPreview preview);

        string CreateBackup(string path);

        SessionRestorePreview PreviewRestore(string path);

        SessionRestoreResult RestoreBackup(string path);

        SessionReindexResult Reindex();
    }
}
