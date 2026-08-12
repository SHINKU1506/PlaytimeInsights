using Microsoft.Win32;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace PlaytimeInsights.Views
{
    public partial class SessionImportPreviewWindow : Window
    {
        public SessionImportPreviewWindow()
        {
            InitializeComponent();
            // Work-area sizing belongs to the concrete WPF Window lifecycle.
            Loaded += (sender, args) =>
                WindowLayoutService.ConstrainToWorkArea(this);
        }

        private SessionImportPreview Preview => DataContext as SessionImportPreview;

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void SaveErrorsButton_Click(object sender, RoutedEventArgs e)
        {
            // This dialog-local export needs its Window owner and file picker.
            if (Preview == null || Preview.Errors.Count == 0)
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "LOCPlaytimeInsightsNoErrorsToSave",
                        "当前没有需要保存的错误。"),
                    "Playtime Insights",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".txt",
                Filter = LocalizationService.Get(
                    "LOCPlaytimeInsightsTextFileFilter",
                    "文本文件 (*.txt)|*.txt"),
                FileName = "PlaytimeInsights-import-errors-" +
                    DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var content = Preview.Summary + Environment.NewLine +
                    LocalizationService.Format(
                        "LOCPlaytimeInsightsDetectedFormatFormat",
                        "识别格式：{0}",
                        Preview.FormatSummary) + Environment.NewLine +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, Preview.Errors);
                File.WriteAllText(dialog.FileName, content, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Format(
                        "LOCPlaytimeInsightsSaveErrorsFailedFormat",
                        "保存错误报告失败：{0}",
                        ex.Message),
                    "Playtime Insights",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
