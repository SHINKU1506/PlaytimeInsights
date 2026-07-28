using Microsoft.Win32;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Views
{
    public partial class SessionManagementView : UserControl
    {
        public SessionManagementView()
        {
            InitializeComponent();
            Loaded += SessionManagementView_Loaded;
        }

        private void SessionManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.Refresh();
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.LoadMore();
        }

        private void AddSessionButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditor(null);
        }

        private void EditSessionButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEditor(ViewModel?.GetSelectedSession());
        }

        private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null ||
                MessageBox.Show(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsDeleteConfirmation",
                        "软删除后统计将忽略此会话，可勾选“包含已删除”后恢复。是否继续？"),
                    "Playtime Insights",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            ViewModel.DeleteSelectedSession();
        }

        private void RestoreSessionButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.RestoreSelectedSession();
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            Export(
                LocalizationService.Get(
                    "LOCPlaytimeInsightsCsvFileFilter",
                    "CSV 文件 (*.csv)|*.csv"),
                ".csv",
                path => ViewModel.ExportCsv(path));
        }

        private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
        {
            Export(
                LocalizationService.Get(
                    "LOCPlaytimeInsightsJsonFileFilter",
                    "JSON 文件 (*.json)|*.json"),
                ".json",
                path => ViewModel.ExportJson(path));
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = true,
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionFileFilter",
                    "支持的会话文件 (*.json;*.csv)|*.json;*.csv|" +
                    "JSON 文件 (*.json)|*.json|CSV 文件 (*.csv)|*.csv")
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var preview = ViewModel.PreviewImport(dialog.FileNames);
                var window = new SessionImportPreviewWindow
                {
                    Owner = Window.GetWindow(this),
                    DataContext = preview
                };
                if (window.ShowDialog() == true)
                {
                    ViewModel.CommitImport(preview);
                }
            }
            catch (Exception ex)
            {
                ShowDataError(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsImportFailed",
                        "导入失败"),
                    ex);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".json",
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsBackupFileFilter",
                    "Playtime Insights 完整备份 (*.json)|*.json"),
                FileName = "PlaytimeInsights-full-backup-" +
                    DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ViewModel.CreateBackup(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowDataError(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsBackupFailed",
                        "备份失败"),
                    ex);
            }
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsBackupFileFilter",
                    "Playtime Insights 完整备份 (*.json)|*.json")
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var preview = ViewModel.PreviewRestore(dialog.FileName);
                if (!preview.IsValid)
                {
                    throw new InvalidOperationException(preview.Error);
                }

                var message = LocalizationService.Format(
                    "LOCPlaytimeInsightsRestoreConfirmationFormat",
                    "备份 schema {0}，包含 {1:N0} 条会话。\n\n" +
                    "恢复将替换当前会话集合；当前正在运行的游戏检查点会保留，" +
                    "备份中的旧检查点不会恢复。操作前会强制创建可回滚备份。是否继续？",
                    preview.SchemaVersion,
                    preview.SessionCount);
                if (MessageBox.Show(
                        message,
                        "Playtime Insights",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                ViewModel.RestoreBackup(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowDataError(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsRestoreFailed",
                        "恢复失败"),
                    ex);
            }
        }

        private void ReindexButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null ||
                MessageBox.Show(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsReindexConfirmation",
                        "重建会规范化 schema、按开始时间排序、修复空/冲突 ID，" +
                        "并移除相同游戏、开始时间和时长的重复会话。" +
                        "\n\n操作前会强制创建可回滚备份。是否继续？"),
                    "Playtime Insights",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                ViewModel.Reindex();
            }
            catch (Exception ex)
            {
                ShowDataError(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsReindexFailed",
                        "重建失败"),
                    ex);
            }
        }

        private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".txt",
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsTextFileFilter",
                    "文本文件 (*.txt)|*.txt"),
                FileName = "PlaytimeInsights-diagnostics-" +
                    DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ViewModel.SaveDiagnostics(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowDataError(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsDiagnosticsSaveFailed",
                        "诊断报告保存失败"),
                    ex);
            }
        }

        private SessionManagementViewModel ViewModel =>
            DataContext as SessionManagementViewModel;

        private void OpenEditor(GameSession existing)
        {
            if (ViewModel == null)
            {
                return;
            }

            var editor = new SessionEditorWindow
            {
                Owner = Window.GetWindow(this),
                DataContext = ViewModel.CreateEditor(existing)
            };
            if (editor.ShowDialog() != true || editor.Result == null)
            {
                return;
            }

            if (existing == null)
            {
                ViewModel.AddSession(editor.Result);
            }
            else
            {
                ViewModel.UpdateSelectedSession(editor.Result);
            }
        }

        private static void Export(string filter, string extension, Func<string, int> export)
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = extension,
                Filter = filter,
                FileName = "PlaytimeInsights-sessions-" +
                    DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                export(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Format(
                        "LOCPlaytimeInsightsExportFailedFormat",
                        "导出失败：{0}",
                        ex.Message),
                    "Playtime Insights",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void ShowDataError(string title, Exception exception)
        {
            MessageBox.Show(
                LocalizationService.Format(
                    "LOCPlaytimeInsightsErrorFormat",
                    "{0}：{1}",
                    title,
                    exception.Message),
                "Playtime Insights",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
