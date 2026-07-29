using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace PlaytimeInsights.ViewModels
{
    public sealed class SessionManagementItemViewModel
    {
        public Guid SessionId { get; set; }

        public Guid GameId { get; set; }

        public string GameName { get; set; }

        public string CoverImagePath { get; set; }

        public string StartedText { get; set; }

        public string DurationText { get; set; }

        public string SourceText { get; set; }

        public SessionSource Source { get; set; }

        public string StateText { get; set; }

        public bool IsDeleted { get; set; }

        public string LibraryName { get; set; }
    }

    public sealed class SessionManagementViewModel : ObservableObject
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly SessionRepository repository;
        private readonly SessionQueryService queryService;
        private readonly SessionExportService exportService;
        private readonly SessionImportService importService;
        private readonly SessionDiagnosticsService diagnosticsService;
        private readonly Action dataChanged;
        private readonly PagedCollection<SessionManagementItemViewModel> pager =
            new PagedCollection<SessionManagementItemViewModel>(200);
        private readonly RefreshReentrancyGuard refreshGuard =
            new RefreshReentrancyGuard();
        private IList<GameSession> filteredSessions = new List<GameSession>();
        private string searchText = string.Empty;
        private SelectionOption<SessionSource?> selectedSource;
        private SelectionOption<MetadataFilterDimension> selectedMetadataDimension;
        private SelectionOption<string> selectedMetadataValue;
        private string statusText;
        private bool suppressFilterRefresh;
        private bool includeDeleted;
        private SessionManagementItemViewModel selectedSession;

        public SessionManagementViewModel(
            IPlayniteAPI playniteApi,
            SessionRepository repository,
            SessionQueryService queryService,
            SessionExportService exportService,
            SessionImportService importService,
            SessionDiagnosticsService diagnosticsService,
            Action dataChanged = null)
        {
            this.playniteApi = playniteApi;
            this.repository = repository;
            this.queryService = queryService;
            this.exportService = exportService;
            this.importService = importService;
            this.diagnosticsService = diagnosticsService;
            this.dataChanged = dataChanged;

            SourceOptions = new ObservableCollection<SelectionOption<SessionSource?>>
            {
                new SelectionOption<SessionSource?>
                {
                    Value = null,
                    Label = LocalizationService.Get(
                        "LOCPlaytimeInsightsAllSources",
                        "全部来源")
                },
                new SelectionOption<SessionSource?>
                {
                    Value = SessionSource.Tracked,
                    Label = SessionQueryService.GetSourceLabel(SessionSource.Tracked)
                },
                new SelectionOption<SessionSource?>
                {
                    Value = SessionSource.Recovered,
                    Label = SessionQueryService.GetSourceLabel(SessionSource.Recovered)
                },
                new SelectionOption<SessionSource?>
                {
                    Value = SessionSource.Imported,
                    Label = SessionQueryService.GetSourceLabel(SessionSource.Imported)
                },
                new SelectionOption<SessionSource?>
                {
                    Value = SessionSource.Manual,
                    Label = SessionQueryService.GetSourceLabel(SessionSource.Manual)
                }
            };
            MetadataDimensionOptions =
                new ObservableCollection<SelectionOption<MetadataFilterDimension>>
                {
                    CreateDimensionOption(MetadataFilterDimension.Library),
                    CreateDimensionOption(MetadataFilterDimension.Source),
                    CreateDimensionOption(MetadataFilterDimension.Developer),
                    CreateDimensionOption(MetadataFilterDimension.Publisher),
                    CreateDimensionOption(MetadataFilterDimension.Tag),
                    CreateDimensionOption(MetadataFilterDimension.Genre),
                    CreateDimensionOption(MetadataFilterDimension.Category),
                    CreateDimensionOption(MetadataFilterDimension.InstallationStatus)
                };
            MetadataValueOptions = new ObservableCollection<SelectionOption<string>>();
            Sessions = pager.VisibleItems;
            selectedSource = SourceOptions[0];
            selectedMetadataDimension = MetadataDimensionOptions[0];
        }

        public ObservableCollection<SelectionOption<SessionSource?>> SourceOptions { get; }

        public ObservableCollection<SelectionOption<MetadataFilterDimension>>
            MetadataDimensionOptions { get; }

        public ObservableCollection<SelectionOption<string>> MetadataValueOptions { get; }

        public ObservableCollection<SessionManagementItemViewModel> Sessions { get; }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (!string.Equals(searchText, value, StringComparison.Ordinal))
                {
                    SetValue(ref searchText, value);
                    Refresh();
                }
            }
        }

        public SelectionOption<SessionSource?> SelectedSource
        {
            get => selectedSource;
            set
            {
                if (!ReferenceEquals(selectedSource, value))
                {
                    SetValue(ref selectedSource, value);
                    Refresh();
                }
            }
        }

        public SelectionOption<MetadataFilterDimension> SelectedMetadataDimension
        {
            get => selectedMetadataDimension;
            set
            {
                if (!ReferenceEquals(selectedMetadataDimension, value))
                {
                    SetValue(ref selectedMetadataDimension, value);
                    if (!suppressFilterRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        public SelectionOption<string> SelectedMetadataValue
        {
            get => selectedMetadataValue;
            set
            {
                if (!ReferenceEquals(selectedMetadataValue, value))
                {
                    SetValue(ref selectedMetadataValue, value);
                    if (!suppressFilterRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        public string StatusText
        {
            get => statusText;
            private set => SetValue(ref statusText, value);
        }

        public bool IncludeDeleted
        {
            get => includeDeleted;
            set
            {
                if (includeDeleted != value)
                {
                    SetValue(ref includeDeleted, value);
                    Refresh();
                }
            }
        }

        public SessionManagementItemViewModel SelectedSession
        {
            get => selectedSession;
            set
            {
                if (!ReferenceEquals(selectedSession, value))
                {
                    SetValue(ref selectedSession, value);
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanDelete));
                    OnPropertyChanged(nameof(CanRestore));
                }
            }
        }

        public bool CanEdit => SelectedSession != null && !SelectedSession.IsDeleted;

        public bool CanDelete => SelectedSession != null && !SelectedSession.IsDeleted;

        public bool CanRestore => SelectedSession != null && SelectedSession.IsDeleted;

        public string CountText => LocalizationService.Format(
            "LOCPlaytimeInsightsSessionCountFormat",
            "已显示 {0:N0} / 筛选结果 {1:N0} / 全部 {2:N0}",
            pager.VisibleCount,
            pager.TotalCount,
            repository.GetAll().Count);

        public Visibility LoadMoreVisibility =>
            pager.HasMore ? Visibility.Visible : Visibility.Collapsed;

        public bool HasFilteredSessions => filteredSessions.Count > 0;

        public void Refresh()
        {
            if (!refreshGuard.TryEnter())
            {
                return;
            }

            try
            {
                var allSessions = repository.GetAllIncludingDeleted();
                var games = playniteApi.Database.Games.ToList();
                var libraryNames = GetLibraryNames();
                RefreshMetadataValueOptions(games, libraryNames);
                filteredSessions = queryService.Filter(
                    games,
                    allSessions,
                    new SessionQuery
                    {
                        SearchText = SearchText,
                        Source = SelectedSource?.Value,
                        MetadataDimension = SelectedMetadataDimension?.Value ??
                            MetadataFilterDimension.Library,
                        MetadataValue = SelectedMetadataValue?.Value,
                        IncludeDeleted = IncludeDeleted
                    },
                    libraryNames);
                var items = queryService.CreateItems(
                    games,
                    filteredSessions,
                    libraryNames);
                ApplyCoverImages(items, games);
                pager.Reset(items);
                StatusText = filteredSessions.Count == 0
                    ? LocalizationService.Get(
                        "LOCPlaytimeInsightsNoFilteredSessions",
                        "当前筛选没有会话。")
                    : LocalizationService.Format(
                        "LOCPlaytimeInsightsLastRefreshedFormat",
                        "最近刷新：{0:HH:mm:ss}",
                        DateTime.Now);
                NotifyPagingChanged();
                OnPropertyChanged(nameof(HasFilteredSessions));
                SelectedSession = null;
            }
            finally
            {
                refreshGuard.Exit();
            }
        }

        public void LoadMore()
        {
            if (pager.AppendNextPage() > 0)
            {
                NotifyPagingChanged();
            }
        }

        private void ApplyCoverImages(
            IEnumerable<SessionManagementItemViewModel> items,
            IEnumerable<Playnite.SDK.Models.Game> games)
        {
            var gamesById = (games ?? Enumerable.Empty<Playnite.SDK.Models.Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var item in items ??
                Enumerable.Empty<SessionManagementItemViewModel>())
            {
                Playnite.SDK.Models.Game game;
                if (!gamesById.TryGetValue(item.GameId, out game) ||
                    string.IsNullOrWhiteSpace(game.CoverImage))
                {
                    item.CoverImagePath = null;
                    continue;
                }

                try
                {
                    item.CoverImagePath =
                        playniteApi.Database.GetFullFilePath(game.CoverImage);
                }
                catch
                {
                    item.CoverImagePath = null;
                }
            }
        }

        public int ExportCsv(string path)
        {
            EnsureExportPath(path);
            File.WriteAllText(
                path,
                exportService.CreateCsv(filteredSessions),
                new UTF8Encoding(true));
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsExportedSessionsFormat",
                "已导出 {0:N0} 条会话到 {1}",
                filteredSessions.Count,
                Path.GetFileName(path));
            return filteredSessions.Count;
        }

        public int ExportJson(string path)
        {
            EnsureExportPath(path);
            File.WriteAllText(
                path,
                exportService.CreateJson(filteredSessions, DateTime.UtcNow),
                new UTF8Encoding(false));
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsExportedSessionsFormat",
                "已导出 {0:N0} 条会话到 {1}",
                filteredSessions.Count,
                Path.GetFileName(path));
            return filteredSessions.Count;
        }

        public void SaveDiagnostics(string path)
        {
            diagnosticsService.SaveReport(
                path,
                repository.GetStorageDiagnostics(),
                DateTime.UtcNow);
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsDiagnosticsSavedFormat",
                "已保存不含游戏名称和会话明细的诊断报告：{0}",
                Path.GetFileName(path));
        }

        public GameSession GetSelectedSession()
        {
            return SelectedSession == null
                ? null
                : repository.FindSession(SelectedSession.SessionId);
        }

        public SessionEditorViewModel CreateEditor(GameSession existing = null)
        {
            return new SessionEditorViewModel(playniteApi.Database.Games, existing);
        }

        public bool AddSession(GameSession session)
        {
            if (session == null)
            {
                return false;
            }

            session.Source = SessionSource.Manual;
            session.SchemaVersion = GameSession.CurrentSchemaVersion;
            session.LastModifiedAtUtc = DateTime.UtcNow;
            session.LastModifiedReason = "ManualEntry";
            var added = repository.CompleteSession(session);
            Refresh();
            StatusText = added
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionAdded",
                    "已补录会话。")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsDuplicateSessionSkipped",
                    "未补录：检测到重复会话。");
            if (added)
            {
                dataChanged?.Invoke();
            }
            return added;
        }

        public bool UpdateSelectedSession(GameSession session)
        {
            if (SelectedSession == null || session == null)
            {
                return false;
            }

            session.Id = SelectedSession.SessionId;
            var updated = repository.UpdateSession(session, "UserEdit");
            Refresh();
            StatusText = updated
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionUpdated",
                    "已更新会话。")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionNotFound",
                    "未找到要更新的会话。");
            if (updated)
            {
                dataChanged?.Invoke();
            }
            return updated;
        }

        public bool DeleteSelectedSession()
        {
            if (!CanDelete)
            {
                return false;
            }

            var deleted = repository.SetSessionDeleted(
                SelectedSession.SessionId,
                true,
                "UserSoftDelete");
            Refresh();
            StatusText = deleted
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionDeleted",
                    "会话已软删除，可勾选“包含已删除”后恢复。")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsDeleteFailed",
                    "删除失败。");
            if (deleted)
            {
                dataChanged?.Invoke();
            }
            return deleted;
        }

        public bool RestoreSelectedSession()
        {
            if (!CanRestore)
            {
                return false;
            }

            var restored = repository.SetSessionDeleted(
                SelectedSession.SessionId,
                false,
                "UserRestore");
            Refresh();
            StatusText = restored
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionRestored",
                    "会话已恢复。")
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsRestoreFailed",
                    "恢复失败。");
            if (restored)
            {
                dataChanged?.Invoke();
            }
            return restored;
        }

        public SessionImportPreview PreviewImport(IEnumerable<string> paths)
        {
            var preview = importService.Preview(
                paths,
                playniteApi.Database.Games,
                repository.GetAllIncludingDeleted());
            StatusText = preview.Summary;
            return preview;
        }

        public SessionImportCommitResult CommitImport(SessionImportPreview preview)
        {
            if (preview == null || !preview.CanImport)
            {
                return new SessionImportCommitResult();
            }

            var result = repository.ImportSessions(preview.Candidates);
            Refresh();
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsImportCommittedFormat",
                "已导入 {0:N0} 条；提交时跳过重复 {1:N0} 条。回滚备份：{2}",
                result.ImportedCount,
                result.DuplicateCount,
                string.IsNullOrWhiteSpace(result.RollbackBackupPath)
                    ? LocalizationService.Get(
                        "LOCPlaytimeInsightsNotCreated",
                        "未创建")
                    : Path.GetFileName(result.RollbackBackupPath));
            if (result.ImportedCount > 0)
            {
                dataChanged?.Invoke();
            }
            return result;
        }

        public string CreateBackup(string path)
        {
            var result = repository.CreateManualBackup(path);
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsBackupCreatedFormat",
                "已创建完整备份：{0}",
                Path.GetFileName(result));
            return result;
        }

        public SessionRestorePreview PreviewRestore(string path)
        {
            return repository.PreviewRestore(path);
        }

        public SessionRestoreResult RestoreBackup(string path)
        {
            var result = repository.RestoreBackup(path);
            Refresh();
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsBackupRestoredFormat",
                "已恢复 {0:N0} 条会话。恢复前回滚备份：{1}",
                result.SessionCount,
                Path.GetFileName(result.RollbackBackupPath));
            dataChanged?.Invoke();
            return result;
        }

        public SessionReindexResult Reindex()
        {
            var result = repository.Reindex();
            Refresh();
            StatusText = LocalizationService.Format(
                "LOCPlaytimeInsightsReindexCompletedFormat",
                "重建完成：{0:N0} 条，移除重复 {1:N0} 条，修复 ID {2:N0} 条。回滚备份：{3}",
                result.SessionCount,
                result.RemovedDuplicateCount,
                result.RepairedIdCount,
                Path.GetFileName(result.RollbackBackupPath));
            dataChanged?.Invoke();
            return result;
        }

        private void RefreshMetadataValueOptions(
            IList<Playnite.SDK.Models.Game> games,
            IReadOnlyDictionary<Guid, string> libraryNames)
        {
            var selectedValue = SelectedMetadataValue?.Value ?? string.Empty;
            var dimension = SelectedMetadataDimension?.Value ??
                MetadataFilterDimension.Library;
            var values = new List<SelectionOption<string>>
            {
                new SelectionOption<string>
                {
                    Value = string.Empty,
                    Label = LocalizationService.Format(
                        "LOCPlaytimeInsightsAllFormat",
                        "全部{0}",
                        SessionQueryService.GetDimensionLabel(dimension))
                }
            };
            values.AddRange(queryService.GetMetadataValues(games, dimension, libraryNames)
                .Select(name => new SelectionOption<string> { Value = name, Label = name }));

            suppressFilterRefresh = true;
            try
            {
                MetadataValueOptions.Clear();
                foreach (var value in values)
                {
                    MetadataValueOptions.Add(value);
                }

                SelectedMetadataValue = MetadataValueOptions.FirstOrDefault(option =>
                    string.Equals(
                        option.Value,
                        selectedValue,
                        StringComparison.CurrentCultureIgnoreCase)) ??
                    MetadataValueOptions[0];
            }
            finally
            {
                suppressFilterRefresh = false;
            }
        }

        private IReadOnlyDictionary<Guid, string> GetLibraryNames()
        {
            return playniteApi.Addons.Plugins
                .OfType<LibraryPlugin>()
                .GroupBy(plugin => plugin.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Name ?? string.Empty);
        }

        private static SelectionOption<MetadataFilterDimension> CreateDimensionOption(
            MetadataFilterDimension dimension)
        {
            return new SelectionOption<MetadataFilterDimension>
            {
                Value = dimension,
                Label = SessionQueryService.GetDimensionLabel(dimension)
            };
        }

        private void NotifyPagingChanged()
        {
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(LoadMoreVisibility));
        }

        private static void EnsureExportPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Export path is required.", nameof(path));
            }
        }
    }
}
