using Playnite.SDK;
using Playnite.SDK.Models;
using PlaytimeInsights.Models;
using PlaytimeInsights.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace PlaytimeInsights.ViewModels
{
    public sealed class GameSelectionOption
    {
        public Guid GameId { get; set; }

        public string Name { get; set; }

        public Game Game { get; set; }
    }

    public sealed class SessionEditorViewModel : ObservableObject
    {
        private readonly GameSession existing;
        private GameSelectionOption selectedGame;
        private DateTime startDate;
        private string startTimeText;
        private string elapsedSecondsText;
        private string validationMessage;

        public SessionEditorViewModel(IEnumerable<Game> games, GameSession existing = null)
        {
            this.existing = existing;
            var options = (games ?? Enumerable.Empty<Game>())
                .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(game => new GameSelectionOption
                {
                    GameId = game.Id,
                    Name = game.Name,
                    Game = game
                })
                .ToList();

            if (existing != null && options.All(option => option.GameId != existing.GameId))
            {
                options.Insert(0, new GameSelectionOption
                {
                    GameId = existing.GameId,
                    Name = LocalizationService.Format(
                        "LOCPlaytimeInsightsRemovedGameFormat",
                        "{0}（已从库中删除）",
                        existing.GameName)
                });
            }

            GameOptions = new ObservableCollection<GameSelectionOption>(options);
            SelectedGame = existing == null
                ? GameOptions.FirstOrDefault()
                : GameOptions.FirstOrDefault(option => option.GameId == existing.GameId);

            if (existing == null)
            {
                var now = DateTime.Now;
                StartDate = now.Date;
                StartTimeText = now.ToString("HH:mm:ss");
                ElapsedSecondsText = "60";
                TitleText = LocalizationService.Get(
                    "LOCPlaytimeInsightsAddSessionTitle",
                    "补录会话");
            }
            else
            {
                var startedUtc = DateTime.SpecifyKind(existing.StartedAtUtc, DateTimeKind.Utc);
                var localStarted = new DateTimeOffset(startedUtc)
                    .ToOffset(TimeSpan.FromMinutes(existing.StartUtcOffsetMinutes))
                    .DateTime;
                StartDate = localStarted.Date;
                StartTimeText = localStarted.ToString("HH:mm:ss");
                ElapsedSecondsText = existing.ElapsedSeconds.ToString(
                    CultureInfo.InvariantCulture);
                TitleText = LocalizationService.Get(
                    "LOCPlaytimeInsightsEditSessionTitle",
                    "编辑会话");
            }
        }

        public ObservableCollection<GameSelectionOption> GameOptions { get; }

        public string TitleText { get; }

        public GameSelectionOption SelectedGame
        {
            get => selectedGame;
            set => SetValue(ref selectedGame, value);
        }

        public DateTime StartDate
        {
            get => startDate;
            set => SetValue(ref startDate, value);
        }

        public string StartTimeText
        {
            get => startTimeText;
            set => SetValue(ref startTimeText, value);
        }

        public string ElapsedSecondsText
        {
            get => elapsedSecondsText;
            set => SetValue(ref elapsedSecondsText, value);
        }

        public string ValidationMessage
        {
            get => validationMessage;
            private set => SetValue(ref validationMessage, value);
        }

        public bool TryBuild(out GameSession session)
        {
            session = null;
            ValidationMessage = string.Empty;
            if (SelectedGame == null)
            {
                ValidationMessage = LocalizationService.Get(
                    "LOCPlaytimeInsightsSelectGameValidation",
                    "请选择游戏。");
                return false;
            }

            TimeSpan time;
            if (!TimeSpan.TryParse(StartTimeText, CultureInfo.CurrentCulture, out time) ||
                time < TimeSpan.Zero ||
                time >= TimeSpan.FromDays(1))
            {
                ValidationMessage = LocalizationService.Get(
                    "LOCPlaytimeInsightsStartTimeValidation",
                    "开始时间必须是 HH:mm 或 HH:mm:ss。");
                return false;
            }

            ulong elapsedSeconds;
            if (!ulong.TryParse(
                    ElapsedSecondsText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out elapsedSeconds) ||
                elapsedSeconds > 31536000UL)
            {
                ValidationMessage = LocalizationService.Get(
                    "LOCPlaytimeInsightsElapsedValidation",
                    "持续秒数必须是 0 到 31536000 的整数。");
                return false;
            }

            var localStarted = DateTime.SpecifyKind(
                StartDate.Date.Add(time),
                DateTimeKind.Unspecified);
            DateTime startedUtc;
            try
            {
                startedUtc = TimeZoneInfo.ConvertTimeToUtc(localStarted, TimeZoneInfo.Local);
            }
            catch (ArgumentException)
            {
                ValidationMessage = LocalizationService.Get(
                    "LOCPlaytimeInsightsLocalTimeValidation",
                    "该本地时间在当前时区无效，请检查夏令时切换。");
                return false;
            }

            var endedUtc = startedUtc.AddSeconds(elapsedSeconds);
            var game = SelectedGame.Game;
            session = new GameSession
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                GameId = SelectedGame.GameId,
                GameName = game?.Name ?? existing?.GameName ?? SelectedGame.Name,
                GameSourceName = game?.Source?.Name ?? existing?.GameSourceName ?? string.Empty,
                PlatformNames = game?.Platforms == null
                    ? existing?.PlatformNames ?? string.Empty
                    : string.Join(", ", game.Platforms.Select(platform => platform.Name)),
                StartedAtUtc = DateTime.SpecifyKind(startedUtc, DateTimeKind.Utc),
                EndedAtUtc = DateTime.SpecifyKind(endedUtc, DateTimeKind.Utc),
                ElapsedSeconds = elapsedSeconds,
                StartUtcOffsetMinutes = (int)TimeZoneInfo.Local
                    .GetUtcOffset(startedUtc).TotalMinutes,
                EndUtcOffsetMinutes = (int)TimeZoneInfo.Local
                    .GetUtcOffset(endedUtc).TotalMinutes,
                TimeZoneId = TimeZoneInfo.Local.Id,
                ManuallyStopped = existing?.ManuallyStopped ?? false,
                Source = existing?.Source ?? SessionSource.Manual,
                RecoveryReason = existing?.RecoveryReason ?? string.Empty,
                IsDeleted = existing?.IsDeleted ?? false,
                DeletedAtUtc = existing?.DeletedAtUtc,
                SchemaVersion = GameSession.CurrentSchemaVersion
            };
            return true;
        }
    }
}
