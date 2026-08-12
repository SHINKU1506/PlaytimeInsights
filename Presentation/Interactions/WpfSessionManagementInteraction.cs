using Microsoft.Win32;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using PlaytimeInsights.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PlaytimeInsights.Presentation.Interactions
{
    public sealed class WpfSessionManagementInteraction :
        ISessionManagementInteraction
    {
        private readonly Func<Window> ownerProvider;

        public WpfSessionManagementInteraction(Func<Window> ownerProvider)
        {
            this.ownerProvider = ownerProvider ??
                throw new ArgumentNullException(nameof(ownerProvider));
        }

        public IReadOnlyList<string> SelectImportFiles()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = true,
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionFileFilter",
                    "支持的会话文件 (*.json;*.csv)|*.json;*.csv|" +
                    "JSON 文件 (*.json)|*.json|CSV 文件 (*.csv)|*.csv")
            };
            if (!ShowDialog(dialog))
            {
                return new string[0];
            }

            return dialog.FileNames.ToList();
        }

        public string SelectExportPath(string extension)
        {
            var normalizedExtension = string.Equals(
                extension,
                ".json",
                StringComparison.OrdinalIgnoreCase)
                ? ".json"
                : ".csv";
            var filter = normalizedExtension == ".json"
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsJsonFileFilter",
                    "JSON 文件 (*.json)|*.json")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsCsvFileFilter",
                    "CSV 文件 (*.csv)|*.csv");
            return SelectSavePath(
                normalizedExtension,
                filter,
                "PlaytimeInsights-sessions-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public string SelectBackupPath()
        {
            return SelectSavePath(
                ".json",
                GetBackupFilter(),
                "PlaytimeInsights-full-backup-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public string SelectRestorePath()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = GetBackupFilter()
            };
            return ShowDialog(dialog) ? dialog.FileName : null;
        }

        public string SelectDiagnosticsPath()
        {
            return SelectSavePath(
                ".txt",
                LocalizationService.Get(
                    "LOCPlaytimeInsightsTextFileFilter",
                    "文本文件 (*.txt)|*.txt"),
                "PlaytimeInsights-diagnostics-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public bool ConfirmDelete(string gameName)
        {
            return ShowConfirmation(LocalizationService.Get(
                "LOCPlaytimeInsightsDeleteConfirmation",
                "软删除后统计将忽略此会话，可勾选“包含已删除”后恢复。是否继续？"));
        }

        public bool ConfirmRestore(SessionRestorePreview preview)
        {
            if (preview == null)
            {
                return false;
            }

            return ShowConfirmation(LocalizationService.Format(
                "LOCPlaytimeInsightsRestoreConfirmationFormat",
                "备份 schema {0}，包含 {1:N0} 条会话。\n\n" +
                "恢复将替换当前会话集合；当前正在运行的游戏检查点会保留，" +
                "备份中的旧检查点不会恢复。操作前会强制创建可回滚备份。是否继续？",
                preview.SchemaVersion,
                preview.SessionCount));
        }

        public bool ConfirmReindex()
        {
            return ShowConfirmation(LocalizationService.Get(
                "LOCPlaytimeInsightsReindexConfirmation",
                "重建会规范化 schema、按开始时间排序、修复空/冲突 ID，" +
                "并移除相同游戏、开始时间和时长的重复会话。" +
                "\n\n操作前会强制创建可回滚备份。是否继续？"));
        }

        public bool ConfirmImport(SessionImportPreview preview)
        {
            var window = new SessionImportPreviewWindow
            {
                Owner = ownerProvider(),
                DataContext = preview
            };
            return window.ShowDialog() == true;
        }

        public GameSession EditSession(SessionEditorViewModel editor)
        {
            if (editor == null)
            {
                return null;
            }

            var window = new SessionEditorWindow
            {
                Owner = ownerProvider(),
                DataContext = editor
            };
            return window.ShowDialog() == true ? window.Result : null;
        }

        public void ShowError(string title, Exception exception)
        {
            var message = LocalizationService.Format(
                "LOCPlaytimeInsightsErrorFormat",
                "{0}：{1}",
                title,
                exception?.Message ?? string.Empty);
            var owner = ownerProvider();
            if (owner == null)
            {
                MessageBox.Show(
                    message,
                    "Playtime Insights",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    owner,
                    message,
                    "Playtime Insights",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string SelectSavePath(
            string extension,
            string filter,
            string fileName)
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = extension,
                Filter = filter,
                FileName = fileName
            };
            return ShowDialog(dialog) ? dialog.FileName : null;
        }

        private bool ShowConfirmation(string message)
        {
            var owner = ownerProvider();
            return owner == null
                ? MessageBox.Show(
                    message,
                    "Playtime Insights",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes
                : MessageBox.Show(
                    owner,
                    message,
                    "Playtime Insights",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private bool ShowDialog(FileDialog dialog)
        {
            var owner = ownerProvider();
            return owner == null
                ? dialog.ShowDialog() == true
                : dialog.ShowDialog(owner) == true;
        }

        private static string GetBackupFilter()
        {
            return LocalizationService.Get(
                "LOCPlaytimeInsightsBackupFileFilter",
                "Playtime Insights 完整备份 (*.json)|*.json");
        }
    }
}
