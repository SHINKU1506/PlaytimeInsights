using Playnite.SDK;
using Playnite.SDK.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaytimeInsights.ViewModels
{
    public sealed class DashboardMetricsViewModel : ObservableObject
    {
        private readonly IPlayniteAPI playniteApi;
        private string lifetimeDurationText;
        private string trackedDurationText;
        private string rangeDurationText;
        private string sessionCountText;
        private string activeDaysText;
        private string averageSessionText;
        private string longestSessionText;
        private string rangeText;
        private string periodTitleText;
        private string rangeRankingTitleText;
        private string statusText;
        private ComparisonMetricViewModel previousPeriodComparison;
        private ComparisonMetricViewModel yearOverYearComparison;
        private string longestStreakText;
        private string currentStreakText;
        private string currentStreakDateText;
        private string anomalyCountText;
        private IReadOnlyList<GameRankingViewModel> rangeGameRankings =
            new List<GameRankingViewModel>();
        private IReadOnlyList<GameRankingViewModel> lifetimeGameRankings =
            new List<GameRankingViewModel>();

        public DashboardMetricsViewModel(IPlayniteAPI playniteApi)
        {
            this.playniteApi = playniteApi;
        }

        public string LifetimeDurationText { get => lifetimeDurationText; private set => SetValue(ref lifetimeDurationText, value); }

        public string TrackedDurationText
        {
            get => trackedDurationText;
            private set
            {
                SetValue(ref trackedDurationText, value);
                OnPropertyChanged(nameof(TrackedDurationSummaryText));
            }
        }

        public string TrackedDurationSummaryText => LocalizationService.Format(
            "LOCPlaytimeInsightsTrackedDurationFormat",
            "插件已记录：{0}",
            TrackedDurationText);

        public string RangeDurationText { get => rangeDurationText; private set => SetValue(ref rangeDurationText, value); }

        public string SessionCountText { get => sessionCountText; private set => SetValue(ref sessionCountText, value); }

        public string ActiveDaysText { get => activeDaysText; private set => SetValue(ref activeDaysText, value); }

        public string AverageSessionText { get => averageSessionText; private set => SetValue(ref averageSessionText, value); }

        public string LongestSessionText { get => longestSessionText; private set => SetValue(ref longestSessionText, value); }

        public string RangeText { get => rangeText; private set => SetValue(ref rangeText, value); }

        public string PeriodTitleText { get => periodTitleText; private set => SetValue(ref periodTitleText, value); }

        public string RangeRankingTitleText { get => rangeRankingTitleText; private set => SetValue(ref rangeRankingTitleText, value); }

        public string StatusText { get => statusText; private set => SetValue(ref statusText, value); }

        public ComparisonMetricViewModel PreviousPeriodComparison { get => previousPeriodComparison; private set => SetValue(ref previousPeriodComparison, value); }

        public ComparisonMetricViewModel YearOverYearComparison { get => yearOverYearComparison; private set => SetValue(ref yearOverYearComparison, value); }

        public string LongestStreakText { get => longestStreakText; private set => SetValue(ref longestStreakText, value); }

        public string CurrentStreakText { get => currentStreakText; private set => SetValue(ref currentStreakText, value); }

        public string CurrentStreakDateText { get => currentStreakDateText; private set => SetValue(ref currentStreakDateText, value); }

        public string AnomalyCountText { get => anomalyCountText; private set => SetValue(ref anomalyCountText, value); }

        public IReadOnlyList<GameRankingViewModel> RangeGameRankings
        {
            get => rangeGameRankings;
            private set => SetValue(ref rangeGameRankings, value);
        }

        public IReadOnlyList<GameRankingViewModel> LifetimeGameRankings
        {
            get => lifetimeGameRankings;
            private set => SetValue(ref lifetimeGameRankings, value);
        }

        public void Apply(DashboardSnapshot snapshot, IEnumerable<Game> allGames)
        {
            var gamesById = CreateGameIndex(allGames);
            Apply(snapshot, gamesById);
        }

        public void Apply(
            DashboardSnapshot snapshot,
            IReadOnlyDictionary<Guid, Game> gamesById)
        {
            ApplyRankingCoverImages(snapshot.RangeGameRankings, gamesById);
            ApplyRankingCoverImages(snapshot.LifetimeGameRankings, gamesById);
            LifetimeDurationText = snapshot.LifetimeDurationText;
            TrackedDurationText = snapshot.TrackedDurationText;
            RangeDurationText = snapshot.RangeDurationText;
            SessionCountText = snapshot.SessionCountText;
            ActiveDaysText = snapshot.ActiveDaysText;
            AverageSessionText = snapshot.AverageSessionText;
            LongestSessionText = snapshot.LongestSessionText;
            RangeText = snapshot.RangeText;
            PeriodTitleText = snapshot.PeriodTitleText;
            RangeRankingTitleText = snapshot.RangeRankingTitleText;
            StatusText = snapshot.StatusText;
            PreviousPeriodComparison = snapshot.Advanced.PreviousPeriodComparison;
            YearOverYearComparison = snapshot.Advanced.YearOverYearComparison;
            LongestStreakText = snapshot.Advanced.LongestStreakText;
            CurrentStreakText = snapshot.Advanced.CurrentStreakText;
            CurrentStreakDateText = snapshot.Advanced.CurrentStreakDateText;
            AnomalyCountText = snapshot.Advanced.AnomalyCountText;
            RangeGameRankings = (snapshot.RangeGameRankings ??
                Enumerable.Empty<GameRankingViewModel>()).ToList();
            LifetimeGameRankings = (snapshot.LifetimeGameRankings ??
                Enumerable.Empty<GameRankingViewModel>()).ToList();
        }

        public void ApplyPeriodTitle(DashboardTrendProjection projection)
        {
            PeriodTitleText = projection.PeriodTitleText;
        }

        public void ApplyRangeRanking(
            DashboardRankingProjection projection,
            IEnumerable<Game> allGames)
        {
            ApplyRangeRanking(projection, CreateGameIndex(allGames));
        }

        public void ApplyRangeRanking(
            DashboardRankingProjection projection,
            IReadOnlyDictionary<Guid, Game> gamesById)
        {
            ApplyRankingCoverImages(projection.RangeGameRankings, gamesById);
            RangeRankingTitleText = projection.RangeRankingTitleText;
            RangeGameRankings = (projection.RangeGameRankings ??
                Enumerable.Empty<GameRankingViewModel>()).ToList();
        }

        private void ApplyRankingCoverImages(
            IEnumerable<GameRankingViewModel> rankings,
            IReadOnlyDictionary<Guid, Game> gamesById)
        {
            gamesById = gamesById ?? new Dictionary<Guid, Game>();
            foreach (var ranking in rankings ?? Enumerable.Empty<GameRankingViewModel>())
            {
                Game game;
                if (!gamesById.TryGetValue(ranking.GameId, out game) ||
                    string.IsNullOrWhiteSpace(game.CoverImage))
                {
                    ranking.CoverImagePath = null;
                    continue;
                }

                try
                {
                    ranking.CoverImagePath = playniteApi?.Database
                        ?.GetFullFilePath(game.CoverImage);
                }
                catch
                {
                    ranking.CoverImagePath = null;
                }
            }
        }

        private static IReadOnlyDictionary<Guid, Game> CreateGameIndex(
            IEnumerable<Game> games)
        {
            return (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());
        }

    }
}
