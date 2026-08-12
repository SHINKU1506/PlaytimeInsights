using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlaytimeInsights.Models;
using PlaytimeInsights.Presentation.Coordinators;
using PlaytimeInsights.Presentation.Interactions;
using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using PlaytimeInsights.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PlaytimeInsights
{
    public class PlaytimeInsights : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly TimeSpan CheckpointInterval = TimeSpan.FromMinutes(1);
        private readonly PlaytimeInsightsSettingsViewModel settings;
        private readonly SessionRepository sessionRepository;
        private readonly AnalyticsService analyticsService;
        private readonly SessionQueryService sessionQueryService;
        private readonly SessionExportService sessionExportService;
        private readonly SessionImportService sessionImportService;
        private readonly SessionDiagnosticsService sessionDiagnosticsService;
        private DashboardViewModel cachedDashboard;
        private DashboardViewModel activeDashboard;
        private SessionManagementViewModel activeSessionManagement;
        private Timer checkpointTimer;

        public override Guid Id { get; } = Guid.Parse("7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd");

        public PlaytimeInsights(IPlayniteAPI api) : base(api)
        {
            settings = new PlaytimeInsightsSettingsViewModel(this);
            sessionRepository = new SessionRepository(GetPluginUserDataPath(), logger);
            analyticsService = new AnalyticsService();
            sessionQueryService = new SessionQueryService();
            sessionExportService = new SessionExportService();
            sessionImportService = new SessionImportService();
            sessionDiagnosticsService = new SessionDiagnosticsService();

            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            try
            {
                var count = settings.Settings.RecoverInterruptedSessions
                    ? sessionRepository.RecoverActiveSessions(
                        DateTime.UtcNow,
                        "UnexpectedApplicationExit")
                    : sessionRepository.DiscardActiveSessions();

                if (count > 0)
                {
                    logger.Info(settings.Settings.RecoverInterruptedSessions
                        ? $"Recovered {count} interrupted play session(s)."
                        : $"Discarded {count} interrupted play session(s).");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not recover interrupted Playtime Insights sessions.");
            }

            checkpointTimer = new Timer(
                CheckpointActiveSessions,
                null,
                CheckpointInterval,
                CheckpointInterval);
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            checkpointTimer?.Dispose();
            checkpointTimer = null;

            try
            {
                var now = DateTime.UtcNow;
                sessionRepository.CheckpointActiveSessions(now);
                var count = sessionRepository.RecoverActiveSessions(
                    now,
                    "ApplicationStoppedBeforeGameStopped");
                if (count > 0)
                {
                    logger.Info($"Finalized {count} active session(s) during application shutdown.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not finalize active Playtime Insights sessions.");
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (!settings.Settings.EnableSessionTracking || args?.Game == null)
            {
                return;
            }

            try
            {
                var startedAtUtc = DateTime.UtcNow;
                sessionRepository.BeginSession(CreateActiveSession(args.Game, startedAtUtc));
                logger.Debug($"Started tracking play session for {args.Game.Name}.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not persist active Playtime Insights session.");
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (!settings.Settings.EnableSessionTracking || args?.Game == null)
            {
                return;
            }

            try
            {
                var endedAtUtc = DateTime.UtcNow;
                var active = sessionRepository.FindActiveSession(args.Game.Id);
                var startedAtUtc = active?.StartedAtUtc ??
                    endedAtUtc.AddSeconds(-(double)args.ElapsedSeconds);

                if (startedAtUtc > endedAtUtc)
                {
                    startedAtUtc = endedAtUtc.AddSeconds(-(double)args.ElapsedSeconds);
                }

                var session = new GameSession
                {
                    GameId = args.Game.Id,
                    GameName = active?.GameName ?? args.Game.Name ?? string.Empty,
                    GameSourceName = active?.GameSourceName ?? args.Game.Source?.Name ?? string.Empty,
                    PlatformNames = active?.PlatformNames ?? GetPlatformNames(args.Game),
                    StartedAtUtc = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc),
                    EndedAtUtc = DateTime.SpecifyKind(endedAtUtc, DateTimeKind.Utc),
                    ElapsedSeconds = args.ElapsedSeconds,
                    StartUtcOffsetMinutes = active?.StartUtcOffsetMinutes ??
                        (int)TimeZoneInfo.Local.GetUtcOffset(startedAtUtc).TotalMinutes,
                    EndUtcOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(endedAtUtc).TotalMinutes,
                    TimeZoneId = active?.TimeZoneId ?? TimeZoneInfo.Local.Id,
                    ManuallyStopped = args.ManuallyStopped,
                    Source = SessionSource.Tracked
                };

                if (sessionRepository.CompleteSession(session))
                {
                    logger.Info(
                        $"Recorded play session for {args.Game.Name}: {args.ElapsedSeconds} seconds.");
                }
                else
                {
                    logger.Warn($"Ignored duplicate play session for {args.Game.Name}.");
                }

                RefreshOpenDashboard();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not record Playtime Insights session.");
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var installationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "Playtime Insights",
                Icon = Path.Combine(installationDirectory, "icon-dashboard.png"),
                Opened = () =>
                {
                    if (cachedDashboard == null)
                    {
                        cachedDashboard = new DashboardViewModel(
                            PlayniteApi,
                            sessionRepository,
                            analyticsService,
                            sessionQueryService,
                            settings);
                    }

                    activeDashboard = cachedDashboard;

                    return new PlaytimeInsightsDashboardView
                    {
                        DataContext = activeDashboard
                    };
                },
                Closed = () => activeDashboard = null
            };

            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = LocalizationService.Get(
                    "LOCPlaytimeInsightsSessionsSidebar",
                    "Playtime Insights · 会话"),
                Icon = Path.Combine(installationDirectory, "icon-sessions.png"),
                Opened = () =>
                {
                    activeSessionManagement = new SessionManagementViewModel(
                        PlayniteApi,
                        sessionRepository,
                        sessionQueryService,
                        sessionExportService,
                        sessionImportService,
                        sessionDiagnosticsService,
                        RefreshOpenAnalytics);

                    SessionManagementView view = null;
                    var interaction = new WpfSessionManagementInteraction(
                        () => System.Windows.Window.GetWindow(view));
                    var coordinator = new SessionManagementCoordinator(
                        activeSessionManagement,
                        interaction);
                    view = new SessionManagementView(coordinator)
                    {
                        DataContext = activeSessionManagement
                    };
                    return view;
                },
                Closed = () => activeSessionManagement = null
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override System.Windows.Controls.UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlaytimeInsightsSettingsView();
        }

        private static ActiveGameSession CreateActiveSession(Game game, DateTime startedAtUtc)
        {
            return new ActiveGameSession
            {
                GameId = game.Id,
                GameName = game.Name ?? string.Empty,
                GameSourceName = game.Source?.Name ?? string.Empty,
                PlatformNames = GetPlatformNames(game),
                StartedAtUtc = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc),
                LastCheckpointUtc = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc),
                StartUtcOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(startedAtUtc).TotalMinutes,
                TimeZoneId = TimeZoneInfo.Local.Id
            };
        }

        private static string GetPlatformNames(Game game)
        {
            return game?.Platforms == null
                ? string.Empty
                : string.Join(", ", game.Platforms.Select(a => a.Name));
        }

        private void CheckpointActiveSessions(object state)
        {
            try
            {
                sessionRepository.CheckpointActiveSessions(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not checkpoint active Playtime Insights sessions.");
            }
        }

        private void RefreshOpenDashboard()
        {
            if (activeDashboard == null && activeSessionManagement == null)
            {
                return;
            }

            PlayniteApi.MainView.UIDispatcher.BeginInvoke(new Action(() =>
            {
                if (activeDashboard != null)
                {
                    activeDashboard.Refresh();
                }

                if (activeSessionManagement != null)
                {
                    activeSessionManagement.Refresh();
                }
            }));
        }

        private void RefreshOpenAnalytics()
        {
            if (activeDashboard == null)
            {
                return;
            }

            PlayniteApi.MainView.UIDispatcher.BeginInvoke(new Action(() =>
            {
                activeDashboard?.Refresh();
            }));
        }
    }
}
