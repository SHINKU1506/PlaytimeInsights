using PlaytimeInsights.Models;
using PlaytimeInsights.ViewModels;
using System;
using System.Collections.Generic;

namespace PlaytimeInsights.Presentation.Interactions
{
    public interface ISessionManagementInteraction
    {
        IReadOnlyList<string> SelectImportFiles();

        string SelectExportPath(string extension);

        string SelectBackupPath();

        string SelectRestorePath();

        string SelectDiagnosticsPath();

        bool ConfirmDelete(string gameName);

        bool ConfirmRestore(SessionRestorePreview preview);

        bool ConfirmReindex();

        bool ConfirmImport(SessionImportPreview preview);

        GameSession EditSession(SessionEditorViewModel editor);

        void ShowError(string title, Exception exception);
    }
}
