using Playnite.SDK;
using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PlaytimeInsights.ViewModels
{
    public enum DashboardDrilldownAnchor
    {
        None,
        Trend,
        Distribution
    }

    public sealed class DashboardDrilldownViewModel : ObservableObject
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly AnalyticsService analyticsService;
        private readonly SessionDetailPager pager = new SessionDetailPager(100);
        private IList<Game> activeGames = new List<Game>();
        private IList<GameSession> activeSessions = new List<GameSession>();
        private string selectedDetailTitle = LocalizationService.Get(
            "LOCPlaytimeInsightsDetailsPrompt",
            "点击柱形、折线点或热力格查看会话");
        private Visibility sessionDetailVisibility = Visibility.Collapsed;
        private DashboardDrilldownAnchor selectedAnchor =
            DashboardDrilldownAnchor.None;

        public DashboardDrilldownViewModel(
            IPlayniteAPI playniteApi,
            AnalyticsService analyticsService)
        {
            this.playniteApi = playniteApi;
            this.analyticsService = analyticsService;
            SessionDetails = pager.VisibleItems;
        }

        public string SelectedDetailTitle { get => selectedDetailTitle; private set => SetValue(ref selectedDetailTitle, value); }

        public Visibility SessionDetailVisibility
        {
            get => sessionDetailVisibility;
            private set
            {
                if (sessionDetailVisibility == value)
                {
                    return;
                }

                SetValue(ref sessionDetailVisibility, value);
                NotifyHostVisibilityChanged();
            }
        }

        public DashboardDrilldownAnchor SelectedAnchor
        {
            get => selectedAnchor;
            private set
            {
                if (selectedAnchor == value)
                {
                    return;
                }

                SetValue(ref selectedAnchor, value);
                NotifyHostVisibilityChanged();
            }
        }

        public Visibility TrendHostVisibility =>
            SessionDetailVisibility == Visibility.Visible &&
            SelectedAnchor == DashboardDrilldownAnchor.Trend
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility DistributionHostVisibility =>
            SessionDetailVisibility == Visibility.Visible &&
            SelectedAnchor == DashboardDrilldownAnchor.Distribution
                ? Visibility.Visible
                : Visibility.Collapsed;

        public ObservableCollection<SessionDetailViewModel> SessionDetails { get; }

        public string SessionDetailCountText =>
            pager.TotalCount == 0
                ? LocalizationService.Get(
                    "LOCPlaytimeInsightsNoSessionsDisplayed",
                    "未显示会话")
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsDisplayedSessionsFormat",
                    "已显示 {0:N0} / {1:N0}",
                    pager.VisibleCount,
                    pager.TotalCount);

        public Visibility LoadMoreVisibility => pager.HasMore ? Visibility.Visible : Visibility.Collapsed;

        public bool HasMore => pager.HasMore;

        public void ResetContext(IList<Game> games, IList<GameSession> sessions)
        {
            activeGames = games ?? new List<Game>();
            activeSessions = sessions ?? new List<GameSession>();
            ResetSelection();
        }

        public void ResetSelection()
        {
            pager.Reset(null);
            NotifyPagingChanged();
            SelectedAnchor = DashboardDrilldownAnchor.None;
            SessionDetailVisibility = Visibility.Collapsed;
            SelectedDetailTitle = LocalizationService.Get(
                "LOCPlaytimeInsightsDetailsPrompt",
                "点击柱形、折线点或热力格查看会话");
        }

        public void SelectPeriod(PeriodActivityViewModel period)
        {
            if (period == null)
            {
                return;
            }

            Load(
                period.PeriodStart,
                period.PeriodEnd,
                period.Label + " · " + period.DurationText,
                DashboardDrilldownAnchor.Trend);
        }

        public void SelectHeatmapDate(HeatmapCellViewModel cell)
        {
            if (cell == null || cell.CellVisibility != Visibility.Visible)
            {
                return;
            }

            Load(
                cell.Date,
                cell.Date,
                cell.Date.ToString("yyyy/M/d") + " · " +
                    AnalyticsService.FormatDurationPrecise(cell.Seconds),
                DashboardDrilldownAnchor.Distribution);
        }

        public bool LoadMore()
        {
            if (pager.AppendNextPage() <= 0)
            {
                return false;
            }

            NotifyPagingChanged();
            return true;
        }

        private void Load(
            DateTime startDate,
            DateTime endDate,
            string title,
            DashboardDrilldownAnchor anchor)
        {
            var details = analyticsService.CreateSessionDetails(
                activeGames,
                activeSessions,
                startDate,
                endDate);
            ApplyCoverImages(details, activeGames);
            pager.Reset(details);
            NotifyPagingChanged();
            SelectedAnchor = anchor;
            SessionDetailVisibility = Visibility.Visible;
            SelectedDetailTitle = details.Count == 0
                ? LocalizationService.Format(
                    "LOCPlaytimeInsightsNoPreciseDetailFormat",
                    "{0} · 没有精确会话",
                    title)
                : LocalizationService.Format(
                    "LOCPlaytimeInsightsDetailSessionCountFormat",
                    "{0} · {1:N0} 条会话",
                    title,
                    details.Count);
        }

        private void ApplyCoverImages(
            IEnumerable<SessionDetailViewModel> details,
            IEnumerable<Game> games)
        {
            var gamesById = (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var detail in details ?? Enumerable.Empty<SessionDetailViewModel>())
            {
                Game game;
                if (!gamesById.TryGetValue(detail.GameId, out game) ||
                    string.IsNullOrWhiteSpace(game.CoverImage))
                {
                    detail.CoverImagePath = null;
                    continue;
                }

                try
                {
                    detail.CoverImagePath = playniteApi.Database.GetFullFilePath(game.CoverImage);
                }
                catch
                {
                    detail.CoverImagePath = null;
                }
            }
        }

        private void NotifyPagingChanged()
        {
            OnPropertyChanged(nameof(SessionDetailCountText));
            OnPropertyChanged(nameof(LoadMoreVisibility));
            OnPropertyChanged(nameof(HasMore));
        }

        private void NotifyHostVisibilityChanged()
        {
            OnPropertyChanged(nameof(TrendHostVisibility));
            OnPropertyChanged(nameof(DistributionHostVisibility));
        }
    }
}
