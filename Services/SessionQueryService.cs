using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaytimeInsights.Services
{
    public enum MetadataFilterDimension
    {
        Library,
        Source,
        Developer,
        Publisher,
        Tag,
        Genre,
        Category,
        InstallationStatus
    }

    public sealed class SessionQuery
    {
        public string SearchText { get; set; } = string.Empty;

        public SessionSource? Source { get; set; }

        public MetadataFilterDimension MetadataDimension { get; set; } =
            MetadataFilterDimension.Library;

        public string MetadataValue { get; set; } = string.Empty;

        public bool IncludeDeleted { get; set; }
    }

    public interface IGameMetadataAccessor
    {
        IEnumerable<string> GetValues(
            Game game,
            MetadataFilterDimension dimension,
            IReadOnlyDictionary<Guid, string> libraryNames);

        IEnumerable<string> GetAllSearchableValues(
            Game game,
            IReadOnlyDictionary<Guid, string> libraryNames);
    }

    public sealed class PlayniteGameMetadataAccessor : IGameMetadataAccessor
    {
        public IEnumerable<string> GetValues(
            Game game,
            MetadataFilterDimension dimension,
            IReadOnlyDictionary<Guid, string> libraryNames)
        {
            if (game == null)
            {
                return Enumerable.Empty<string>();
            }

            switch (dimension)
            {
                case MetadataFilterDimension.Source:
                    return Single(game.Source?.Name);
                case MetadataFilterDimension.Publisher:
                    return Names(game.Publishers);
                case MetadataFilterDimension.Developer:
                    return Names(game.Developers);
                case MetadataFilterDimension.Tag:
                    return Names(game.Tags);
                case MetadataFilterDimension.Genre:
                    return Names(game.Genres);
                case MetadataFilterDimension.Category:
                    return Names(game.Categories);
                case MetadataFilterDimension.InstallationStatus:
                    return Single(game.IsInstalled
                        ? LocalizationService.Get(
                            "LOCPlaytimeInsightsInstalled",
                            "已安装")
                        : LocalizationService.Get(
                            "LOCPlaytimeInsightsNotInstalled",
                            "未安装"));
                case MetadataFilterDimension.Library:
                default:
                    return Single(GetLibraryName(game, libraryNames));
            }
        }

        public IEnumerable<string> GetAllSearchableValues(
            Game game,
            IReadOnlyDictionary<Guid, string> libraryNames)
        {
            if (game == null)
            {
                yield break;
            }

            foreach (var dimension in new[]
            {
                MetadataFilterDimension.Library,
                MetadataFilterDimension.Source,
                MetadataFilterDimension.Developer,
                MetadataFilterDimension.Publisher,
                MetadataFilterDimension.Tag,
                MetadataFilterDimension.Genre,
                MetadataFilterDimension.Category,
                MetadataFilterDimension.InstallationStatus
            })
            {
                foreach (var value in GetValues(game, dimension, libraryNames))
                {
                    yield return value;
                }
            }
        }

        private static string GetLibraryName(
            Game game,
            IReadOnlyDictionary<Guid, string> libraryNames)
        {
            if (game.PluginId == Guid.Empty)
            {
                return LocalizationService.Get(
                    "LOCPlaytimeInsightsManualLibrary",
                    SessionQueryService.ManualLibraryName);
            }

            string name;
            return libraryNames != null &&
                libraryNames.TryGetValue(game.PluginId, out name) &&
                !string.IsNullOrWhiteSpace(name)
                ? name
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsUnknownLibrary",
                    SessionQueryService.UnknownLibraryName);
        }

        private static IEnumerable<string> Names<T>(IEnumerable<T> values)
            where T : DatabaseObject
        {
            return (values ?? Enumerable.Empty<T>())
                .Select(value => value?.Name)
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static IEnumerable<string> Single(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Enumerable.Empty<string>()
                : new[] { value };
        }
    }

    public sealed class SessionQueryService
    {
        public const string ManualLibraryName = "手动添加";
        public const string UnknownLibraryName = "未知或未加载的库";
        private readonly IGameMetadataAccessor metadataAccessor;

        public SessionQueryService(IGameMetadataAccessor metadataAccessor = null)
        {
            this.metadataAccessor = metadataAccessor ?? new PlayniteGameMetadataAccessor();
        }

        public IList<GameSession> Filter(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            SessionQuery query,
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            query = query ?? new SessionQuery();
            var gameList = (games ?? Enumerable.Empty<Game>()).ToList();
            var gamesById = gameList
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var search = (query.SearchText ?? string.Empty).Trim();
            var metadataValue = (query.MetadataValue ?? string.Empty).Trim();

            return (sessions ?? Enumerable.Empty<GameSession>())
                .Where(session => query.IncludeDeleted || !session.IsDeleted)
                .Where(session => !query.Source.HasValue || session.Source == query.Source.Value)
                .Where(session =>
                {
                    if (string.IsNullOrEmpty(metadataValue))
                    {
                        return true;
                    }

                    Game game;
                    return gamesById.TryGetValue(session.GameId, out game) &&
                        metadataAccessor.GetValues(
                            game,
                            query.MetadataDimension,
                            libraryNames).Any(value =>
                            string.Equals(
                                value,
                                metadataValue,
                                StringComparison.CurrentCultureIgnoreCase));
                })
                .Where(session =>
                {
                    if (string.IsNullOrEmpty(search))
                    {
                        return true;
                    }

                    Game game;
                    gamesById.TryGetValue(session.GameId, out game);
                    return Contains(game?.Name, search) ||
                        Contains(session.GameName, search) ||
                        Contains(session.GameSourceName, search) ||
                        Contains(GetSourceLabel(session.Source), search) ||
                        (game != null && metadataAccessor
                            .GetAllSearchableValues(game, libraryNames)
                            .Any(value => Contains(value, search)));
                })
                .OrderByDescending(session => session.StartedAtUtc)
                .ToList();
        }

        public IList<SessionManagementItemViewModel> CreateItems(
            IEnumerable<Game> games,
            IEnumerable<GameSession> sessions,
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            var gamesById = (games ?? Enumerable.Empty<Game>())
                .GroupBy(game => game.Id)
                .ToDictionary(group => group.Key, group => group.First());

            return (sessions ?? Enumerable.Empty<GameSession>())
                .Select(session =>
                {
                    Game game;
                    gamesById.TryGetValue(session.GameId, out game);
                    var startedUtc = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc);
                    var localStarted = new DateTimeOffset(startedUtc)
                        .ToOffset(TimeSpan.FromMinutes(session.StartUtcOffsetMinutes))
                        .DateTime;
                    return new SessionManagementItemViewModel
                    {
                        SessionId = session.Id,
                        GameName = string.IsNullOrWhiteSpace(game?.Name)
                            ? session.GameName
                            : game.Name,
                        StartedText = localStarted.ToString("yyyy/M/d HH:mm"),
                        DurationText = AnalyticsService.FormatDurationPrecise(session.ElapsedSeconds),
                        SourceText = GetSourceLabel(session.Source),
                        StateText = session.IsDeleted
                            ? LocalizationService.Get(
                                "LOCPlaytimeInsightsDeleted",
                                "已删除")
                            : LocalizationService.Get(
                                "LOCPlaytimeInsightsActive",
                                "有效"),
                        IsDeleted = session.IsDeleted,
                        LibraryName = game == null
                            ? LocalizationService.Get(
                                "LOCPlaytimeInsightsGameRemoved",
                                "游戏已从库中删除")
                            : metadataAccessor.GetValues(
                                game,
                                MetadataFilterDimension.Library,
                                libraryNames).FirstOrDefault() ??
                                UnknownLibraryName
                    };
                })
                .ToList();
        }

        public IList<string> GetMetadataValues(
            IEnumerable<Game> games,
            MetadataFilterDimension dimension,
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            return (games ?? Enumerable.Empty<Game>())
                .SelectMany(game => metadataAccessor.GetValues(game, dimension, libraryNames))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public IList<Game> FilterGames(
            IEnumerable<Game> games,
            MetadataFilterDimension dimension,
            string metadataValue,
            IReadOnlyDictionary<Guid, string> libraryNames = null)
        {
            var value = (metadataValue ?? string.Empty).Trim();
            return (games ?? Enumerable.Empty<Game>())
                .Where(game =>
                    string.IsNullOrEmpty(value) ||
                    metadataAccessor.GetValues(
                        game,
                        dimension,
                        libraryNames).Any(item =>
                        string.Equals(
                            item,
                            value,
                            StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
        }

        public static string GetSourceLabel(SessionSource source)
        {
            switch (source)
            {
                case SessionSource.Recovered:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsRecoveredSource",
                        "异常恢复");
                case SessionSource.Imported:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsImportedSource",
                        "导入");
                case SessionSource.Manual:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsManualSource",
                        "手动");
                case SessionSource.Tracked:
                default:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsTrackedSource",
                        "自动记录");
            }
        }

        public static string GetDimensionLabel(MetadataFilterDimension dimension)
        {
            switch (dimension)
            {
                case MetadataFilterDimension.Source:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsSource",
                        "Playnite 来源");
                case MetadataFilterDimension.Publisher:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsPublisher",
                        "发行商");
                case MetadataFilterDimension.Developer:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsDeveloper",
                        "开发者");
                case MetadataFilterDimension.Tag:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsTag",
                        "标签");
                case MetadataFilterDimension.Genre:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsGenre",
                        "类型");
                case MetadataFilterDimension.Category:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsCategory",
                        "分类");
                case MetadataFilterDimension.InstallationStatus:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsInstallationStatus",
                        "安装状态");
                case MetadataFilterDimension.Library:
                default:
                    return LocalizationService.Get(
                        "LOCPlaytimeInsightsLibrary",
                        "库来源");
            }
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
