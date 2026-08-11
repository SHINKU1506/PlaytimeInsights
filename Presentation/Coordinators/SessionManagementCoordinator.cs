using PlaytimeInsights.Models;
using PlaytimeInsights.Presentation.Interactions;
using PlaytimeInsights.Services;
using System;

namespace PlaytimeInsights.Presentation.Coordinators
{
    public sealed class SessionManagementCoordinator
    {
        private readonly ISessionManagementOperations operations;
        private readonly ISessionManagementInteraction interaction;

        public SessionManagementCoordinator(
            ISessionManagementOperations operations,
            ISessionManagementInteraction interaction)
        {
            this.operations = operations ??
                throw new ArgumentNullException(nameof(operations));
            this.interaction = interaction ??
                throw new ArgumentNullException(nameof(interaction));
        }

        public bool ImportSessions()
        {
            return Run(
                "LOCPlaytimeInsightsImportFailed",
                "导入失败",
                () =>
                {
                    var paths = interaction.SelectImportFiles();
                    if (paths == null || paths.Count == 0)
                    {
                        return false;
                    }

                    var preview = operations.PreviewImport(paths);
                    if (preview == null)
                    {
                        throw new InvalidOperationException(
                            "Import preview was not created.");
                    }

                    if (!interaction.ConfirmImport(preview))
                    {
                        return false;
                    }

                    operations.CommitImport(preview);
                    return true;
                });
        }

        public bool ExportCsv()
        {
            return Export(
                ".csv",
                path => operations.ExportCsv(path));
        }

        public bool ExportJson()
        {
            return Export(
                ".json",
                path => operations.ExportJson(path));
        }

        public bool CreateBackup()
        {
            return Run(
                "LOCPlaytimeInsightsBackupFailed",
                "备份失败",
                () =>
                {
                    var path = interaction.SelectBackupPath();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false;
                    }

                    operations.CreateBackup(path);
                    return true;
                });
        }

        public bool RestoreBackup()
        {
            return Run(
                "LOCPlaytimeInsightsRestoreFailed",
                "恢复失败",
                () =>
                {
                    var path = interaction.SelectRestorePath();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false;
                    }

                    var preview = operations.PreviewRestore(path);
                    if (preview == null || !preview.IsValid)
                    {
                        interaction.ShowError(
                            LocalizationService.Get(
                                "LOCPlaytimeInsightsRestoreFailed",
                                "恢复失败"),
                            new InvalidOperationException(
                                preview?.Error ?? "Restore preview was not created."));
                        return false;
                    }

                    if (!interaction.ConfirmRestore(preview))
                    {
                        return false;
                    }

                    operations.RestoreBackup(path);
                    return true;
                });
        }

        public bool AddSession()
        {
            return Run(
                "LOCPlaytimeInsightsAddSessionTitle",
                "补录会话",
                () =>
                {
                    var result = interaction.EditSession(
                        operations.CreateEditor());
                    return result != null && operations.AddSession(result);
                });
        }

        public bool EditSelectedSession()
        {
            return Run(
                "LOCPlaytimeInsightsEditSessionTitle",
                "编辑会话",
                () =>
                {
                    if (!operations.CanEdit)
                    {
                        return false;
                    }

                    var existing = operations.GetSelectedSession();
                    if (existing == null)
                    {
                        return false;
                    }

                    var result = interaction.EditSession(
                        operations.CreateEditor(existing));
                    return result != null &&
                        operations.UpdateSelectedSession(result);
                });
        }

        public bool DeleteSelectedSession()
        {
            return Run(
                "LOCPlaytimeInsightsDeleteFailed",
                "删除失败。",
                () =>
                {
                    if (!operations.CanDelete)
                    {
                        return false;
                    }

                    var selected = operations.GetSelectedSession();
                    if (selected == null ||
                        !interaction.ConfirmDelete(selected.GameName))
                    {
                        return false;
                    }

                    return operations.DeleteSelectedSession();
                });
        }

        public bool Reindex()
        {
            return Run(
                "LOCPlaytimeInsightsReindexFailed",
                "重建失败",
                () =>
                {
                    if (!interaction.ConfirmReindex())
                    {
                        return false;
                    }

                    operations.Reindex();
                    return true;
                });
        }

        public bool SaveDiagnostics()
        {
            return Run(
                "LOCPlaytimeInsightsDiagnosticsSaveFailed",
                "诊断报告保存失败",
                () =>
                {
                    var path = interaction.SelectDiagnosticsPath();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false;
                    }

                    operations.SaveDiagnostics(path);
                    return true;
                });
        }

        private bool Export(
            string extension,
            Func<string, int> export)
        {
            return Run(
                "LOCPlaytimeInsightsExportFailedTitle",
                "导出失败",
                () =>
                {
                    var path = interaction.SelectExportPath(extension);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false;
                    }

                    export(path);
                    return true;
                });
        }

        private bool Run(
            string errorTitleKey,
            string errorTitleFallback,
            Func<bool> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                interaction.ShowError(
                    LocalizationService.Get(errorTitleKey, errorTitleFallback),
                    ex);
                return false;
            }
        }
    }
}
