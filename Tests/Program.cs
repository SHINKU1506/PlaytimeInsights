using Playnite.SDK;
using PlaytimeInsights.Models;
using PlaytimeInsights.Controls;
using PlaytimeInsights.Presentation.Coordinators;
using PlaytimeInsights.Presentation.Interactions;
using PlaytimeInsights.Services;
using PlaytimeInsights.ViewModels;
using PlaytimeInsights.Views;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace PlaytimeInsights.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("Playnite-style minute rounding", TestMinuteRounding);
            Run("Precise short duration display", TestPreciseDuration);
            Run("Cross-midnight allocation", TestCrossMidnightAllocation);
            Run("Allocation preserves total seconds", TestAllocationPreservesTotal);
            Run("Cross-hour allocation preserves total and hour buckets", TestHourlyAllocation);
            Run("Advanced weekday hour distributions and matrix", TestAdvancedDistributions);
            Run("Weekday selection filters the hourly distribution", TestWeekdayHourSelection);
            Run("Advanced streak finds longest consecutive run", TestAdvancedStreak);
            Run("Previous-period and year-over-year comparisons", TestAdvancedComparisons);
            Run("Year-over-year range handles leap day", TestYearOverYearLeapDay);
            Run("Anomaly hints flag suspicious sessions without mutation", TestAnomalyHints);
            Run("Ten-year 100k-session analytics stays within release budget", TestLargeTenYearAnalytics);
            Run("Schema 4 store loads 100k sessions within release budget", TestLargeStoreLoad);
            Run("Interrupted session recovery", TestInterruptedSessionRecovery);
            Run("Completed session deduplication", TestCompletedSessionDeduplication);
            Run("Corrupt primary recovers from backup", TestCorruptPrimaryRecoversFromBackup);
            Run("ISO week range boundary", TestIsoWeekRangeBoundary);
            Run("Custom range normalizes reversed dates", TestCustomRangeNormalizesReversedDates);
            Run("Weekly aggregation and range metrics", TestWeeklyAggregationAndRangeMetrics);
            Run("Range clips cross-midnight duration", TestRangeClipsCrossMidnightDuration);
            Run("Range ranking supports session count", TestRangeRankingBySessionCount);
            Run("Heatmap aligns ISO week and scales intensity", TestHeatmapLayoutAndIntensity);
            Run("Trend points scale to period maximum", TestTrendPointScaling);
            Run("Period drilldown bounds clip to range", TestPeriodBoundsClipToRange);
            Run("Session drilldown clips duration and labels recovery", TestSessionDrilldown);
            Run("Session detail pager loads fixed-size batches", TestSessionDetailPager);
            Run("Automatic aggregation follows range defaults", TestAutomaticAggregationDefaults);
            Run("Manual aggregation overrides automatic rules", TestManualAggregationOverride);
            Run("Session query combines search source and metadata", TestSessionQueryFilters);
            Run("Session query sorts newest first", TestSessionQuerySort);
            Run("Metadata options expose and deduplicate Playnite values", TestMetadataOptions);
            Run("Game metadata filters support developer genre tag and install status", TestGameMetadataFilters);
            Run("Library metadata maps plugins and manual games", TestLibraryMetadata);
            Run("Refresh guard rejects nested refresh", TestRefreshReentrancyGuard);
            Run("Schema 2 sessions normalize to current schema", TestSchemaThreeMigration);
            Run("Legacy schemas 1 through 4 upgrade without session loss", TestAllSchemaUpgrades);
            Run("Diagnostic report excludes session identity and user paths", TestDiagnosticReportPrivacy);
            Run("Soft delete hides and restore returns session", TestSoftDeleteAndRestore);
            Run("Session update preserves identity and records reason", TestSessionUpdate);
            Run("Manual session editor builds precise session", TestManualSessionEditor);
            Run("CSV export escapes punctuation and newlines", TestCsvExportEscaping);
            Run("JSON export includes version and session count", TestJsonExportDocument);
            Run("Playtime Insights CSV round-trips through import preview", TestPlaytimeInsightsCsvImport);
            Run("GameActivity JSON maps exact game id and UTC session", TestGameActivityJsonImport);
            Run("GameActivity localized semicolon CSV converts local time", TestGameActivityLocalizedCsvImport);
            Run("Import preview reports duplicates and invalid rows", TestImportPreviewValidation);
            Run("Import commit creates rollback backup", TestImportCommitRollback);
            Run("Restore replaces sessions but preserves current active checkpoint", TestRestoreBackup);
            Run("Restore rejects filtered export JSON", TestRestoreRejectsExport);
            Run("Reindex repairs ids and removes duplicate fingerprints", TestReindex);
            Run("English and Chinese localization resources stay in parity", TestLocalizationResourceParity);
            Run("Native views keep localization and accessibility markers", TestNativeViewAccessibility);
            Run("Weekday labels follow plugin resources instead of Windows culture", TestLocalizedWeekdayLabels);
            Run("Dialog sizing stays inside high-DPI work areas", TestResponsiveWindowSizing);
            Run("Native views use portable theme brushes and responsive overflow", TestThemeAndResponsiveLayout);
            Run("Responsive metric panel selects expected columns", TestResponsiveMetricPanelColumns);
            Run("Responsive metric panel centers and equalizes rows", TestResponsiveMetricPanelArrangement);
            Run("Responsive metric panel contains invalid inputs", TestResponsiveMetricPanelEdgeCases);
            Run("Dashboard metrics use responsive semantic visual foundation", TestResponsiveMetricVisualFoundation);
            Run("Session management keeps compact hierarchy and table semantics", TestSessionManagementVisualHierarchy);
            Run("Nested dashboard scrollers hand wheel input to page boundaries", TestDashboardMouseWheelRouting);
            Run("Architecture refactor baseline keeps boundaries documented", TestArchitectureRefactorBaseline);
            Run("RelayCommand executes and raises state changes", TestRelayCommand);
            Run("Generic RelayCommand validates parameters", TestGenericRelayCommand);
            Run("Stage B commands keep low-risk bindings", TestStageBCommandBindings);
            Run("Export errors use a non-mnemonic title", TestExportErrorTitle);
            Run("Session coordinator cancels import file selection", TestCoordinatorCancelsImportFileSelection);
            Run("Session coordinator cancels import preview", TestCoordinatorCancelsImportPreview);
            Run("Session coordinator cancels delete confirmation", TestCoordinatorCancelsDeleteConfirmation);
            Run("Session coordinator blocks invalid restore", TestCoordinatorBlocksInvalidRestore);
            Run("Session coordinator cancels restore confirmation", TestCoordinatorCancelsRestoreConfirmation);
            Run("Session coordinator contains export failure", TestCoordinatorContainsExportFailure);
            Run("Session coordinator cancels editor", TestCoordinatorCancelsEditor);
            Run("Session coordinator cancels reindex", TestCoordinatorCancelsReindex);
            Run("Stage C composes WPF session workflows", TestStageCComposition);
            Run("Stage D dashboard keeps one snapshot coordination boundary", TestStageDDashboardComposition);
            Run("Dashboard filters persist across sidebar navigation", TestDashboardNavigationStateLifetime);
            Run("Sidebar navigation uses one automatic refresh", TestSidebarNavigationUsesSingleAutomaticRefresh);
            Run("Session count reuses the refresh snapshot", TestSessionCountUsesRefreshSnapshot);
            Run("Stage E architecture closure keeps event boundaries symmetric", TestStageEArchitectureClosure);
            Run("Trend periods publish one complete replacement", TestTrendPeriodsPublishAtomically);
            Run("Trend chart follows source lifecycle changes", TestTrendChartSourceLifecycle);
            Run("Dashboard filters route selective refresh reasons", TestDashboardFilterRefreshReasons);
            Run("Dashboard refresh plans isolate dependencies", TestDashboardRefreshPlans);
            Run("Dashboard analysis context reprojects trend without rescan", TestDashboardTrendProjectionReuse);
            Run("Dashboard analysis context reprojects ranking without rescan", TestDashboardRankingProjectionReuse);
            Run("Trend projection leaves unrelated dashboard state intact", TestDashboardTrendProjectionApplyBoundary);
            Run("Ranking projection leaves unrelated dashboard state intact", TestDashboardRankingProjectionApplyBoundary);
            Run("Dashboard major lists publish atomically", TestDashboardMajorListsPublishAtomically);
            Run("Dashboard refresh policy keeps local changes off data reload", TestDashboardRefreshRootPolicy);
            Run("Session coordinator completes import workflow", TestCoordinatorCompletesImport);
            Run("Session coordinator completes restore workflow", TestCoordinatorCompletesRestore);
            Run("Session coordinator completes edit and reindex", TestCoordinatorCompletesEditAndReindex);
            Run("Session coordinator completes remaining workflows", TestCoordinatorCompletesRemainingWorkflows);
            Run("Session coordinator contains import failure", TestCoordinatorContainsImportFailure);
            Run("Release metadata and public README stay current", TestReleaseMetadataAndReadme);
            Run("Localization keys and format placeholders stay source-complete", TestLocalizationSourceCoverage);
            Run("Release 0.1 through 0.9 settings keep compatible defaults", TestLegacySettingsMatrix);
            Run("Sidebar entries publish distinct transparent icons", TestSidebarIconPublishing);

            Console.WriteLine(failures == 0
                ? "All Playtime Insights tests passed."
                : string.Format("{0} Playtime Insights test(s) failed.", failures));
            return failures == 0 ? 0 : 1;
        }

        private static void TestMinuteRounding()
        {
            Equal("2 分钟", AnalyticsService.FormatDuration(91));
        }

        private static void TestPreciseDuration()
        {
            Equal("1 分 31 秒", AnalyticsService.FormatDurationPrecise(91));
            Equal("6 分 28 秒", AnalyticsService.FormatDurationPrecise(388));
        }

        private static void TestCrossMidnightAllocation()
        {
            var session = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 60,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };

            var result = new DailyAllocationService().SplitByLocalDay(session);
            Equal(2, result.Count);
            Equal(30UL, result[new DateTime(2026, 7, 27)]);
            Equal(30UL, result[new DateTime(2026, 7, 28)]);
        }

        private static void TestAllocationPreservesTotal()
        {
            var session = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc),
                ElapsedSeconds = 1001,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };

            var result = new DailyAllocationService().SplitByLocalDay(session);
            Equal(1001UL, result.Values.Aggregate<ulong, ulong>(0, (current, value) => current + value));
        }

        private static void TestHourlyAllocation()
        {
            var session = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 61,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };

            var result = new HourlyAllocationService().SplitByLocalHour(session);

            Equal(2, result.Count);
            Equal(new DateTime(2026, 7, 27), result[0].LocalDate);
            Equal(23, result[0].Hour);
            Equal(new DateTime(2026, 7, 28), result[1].LocalDate);
            Equal(0, result[1].Hour);
            Equal(
                61UL,
                result.Aggregate<HourlyAllocation, ulong>(
                    0,
                    (total, item) => total + item.Seconds));
        }

        private static void TestAdvancedDistributions()
        {
            var gameId = Guid.NewGuid();
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(
                        gameId,
                        "Monday Evening",
                        new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                        120),
                    CreateSession(
                        gameId,
                        "Tuesday Evening",
                        new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
                        60)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 8, 2),
                    UseIsoWeekStart = true
                });

            Equal(7, snapshot.Advanced.WeekdayDistribution.Count);
            Equal(24, snapshot.Advanced.HourDistribution.Count);
            Equal(168, snapshot.Advanced.WeekHourCells.Count);
            Equal(120UL, snapshot.Advanced.WeekdayDistribution[0].Seconds);
            Equal(60UL, snapshot.Advanced.WeekdayDistribution[1].Seconds);
            Equal(
                true,
                snapshot.Advanced.WeekdayDistribution[0].TooltipText.StartsWith(
                    snapshot.Advanced.WeekdayDistribution[0].Label + "：",
                    StringComparison.Ordinal));
            Equal(
                false,
                snapshot.Advanced.WeekdayDistribution[0].TooltipText.Contains(
                    "星期 " + snapshot.Advanced.WeekdayDistribution[0].Label));
            Equal(180UL, snapshot.Advanced.HourDistribution[18].Seconds);
            Equal(120UL, snapshot.Advanced.WeekHourCells[18].Seconds);
            Equal(60UL, snapshot.Advanced.WeekHourCells[24 + 18].Seconds);
        }

        private static void TestAdvancedStreak()
        {
            var gameId = Guid.NewGuid();
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(gameId, "Streak", new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Streak", new DateTime(2026, 7, 21, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Streak", new DateTime(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Streak", new DateTime(2026, 7, 24, 10, 0, 0, DateTimeKind.Utc), 60)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 20),
                    CustomEndDate = new DateTime(2026, 7, 24)
                });

            Equal("3 天", snapshot.Advanced.LongestStreakText);
            Equal("1 天", snapshot.Advanced.CurrentStreakText);
            Equal(true, snapshot.Advanced.CurrentStreakDateText.Contains("7/24"));
        }

        private static void TestWeekdayHourSelection()
        {
            var cells = new List<WeekHourCellViewModel>();
            for (var day = 0; day < 7; day++)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    var seconds = hour == 18
                        ? (day == 0 ? 120UL : day == 1 ? 60UL : 0UL)
                        : 0UL;
                    cells.Add(new WeekHourCellViewModel
                    {
                        DayLabel = "Day " + day,
                        HourLabel = hour.ToString("00") + ":00",
                        Seconds = seconds,
                        TooltipText = string.Format(
                            "Day {0} {1:00}:00: {2}",
                            day,
                            hour,
                            seconds)
                    });
                }
            }

            var firstDay =
                AdvancedAnalyticsService.CreateHourDistributionForWeekday(
                    cells,
                    0);
            var secondDay =
                AdvancedAnalyticsService.CreateHourDistributionForWeekday(
                    cells,
                    1);
            var invalid =
                AdvancedAnalyticsService.CreateHourDistributionForWeekday(
                    cells,
                    7);

            Equal(24, firstDay.Count);
            Equal(120UL, firstDay[18].Seconds);
            Equal(100d, firstDay[18].BarHeight);
            Equal("18:00", firstDay[18].Label);
            Equal(60UL, secondDay[18].Seconds);
            Equal(100d, secondDay[18].BarHeight);
            Equal(0, invalid.Count);
        }

        private static void TestAdvancedComparisons()
        {
            var gameId = Guid.NewGuid();
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(gameId, "Current", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 120),
                    CreateSession(gameId, "Previous", new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Last Year", new DateTime(2025, 7, 27, 10, 0, 0, DateTimeKind.Utc), 30)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 7, 27)
                });

            Equal("+100.0%", snapshot.Advanced.PreviousPeriodComparison.DeltaText);
            Equal("+300.0%", snapshot.Advanced.YearOverYearComparison.DeltaText);
            Equal("Increase", snapshot.Advanced.PreviousPeriodComparison.TrendKind);
            Equal("Increase", snapshot.Advanced.YearOverYearComparison.TrendKind);
            Equal(
                true,
                snapshot.Advanced.PreviousPeriodComparison.TagText.Contains("1 分钟"));
            Equal(
                true,
                snapshot.Advanced.PreviousPeriodComparison.TagText.Contains("环比"));
            Equal(
                true,
                snapshot.Advanced.YearOverYearComparison.TooltipText.Contains("+300.0%"));
            Equal(
                true,
                snapshot.Advanced.PreviousPeriodComparison.PreviousText
                    .Contains("2026/7/26"));
            Equal(
                true,
                snapshot.Advanced.YearOverYearComparison.PreviousText
                    .Contains("2025/7/27"));
        }

        private static void TestYearOverYearLeapDay()
        {
            var range = AdvancedAnalyticsService.CreateYearOverYearRange(
                new AnalyticsDateRange
                {
                    StartDate = new DateTime(2024, 2, 29),
                    EndDate = new DateTime(2024, 3, 1)
                });

            Equal(new DateTime(2023, 2, 28), range.StartDate);
            Equal(new DateTime(2023, 3, 1), range.EndDate);
        }

        private static void TestAnomalyHints()
        {
            var gameId = Guid.NewGuid();
            var suspicious = CreateSession(
                gameId,
                "Long Session",
                new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
                18UL * 3600UL);
            var originalSeconds = suspicious.ElapsedSeconds;
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[] { suspicious },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 7, 28)
                });

            Equal(1, snapshot.Advanced.Anomalies.Count);
            Equal(
                true,
                snapshot.Advanced.Anomalies[0].Reason.Contains("18 小时"));
            Equal(originalSeconds, suspicious.ElapsedSeconds);
        }

        private static void TestLargeTenYearAnalytics()
        {
            const int gameCount = 5000;
            const int sessionCount = 100000;
            var games = Enumerable.Range(0, gameCount)
                .Select(index => new Playnite.SDK.Models.Game(
                    "Stress Game " + index)
                {
                    Id = Guid.NewGuid(),
                    Playtime = (ulong)(index + 1) * 60UL
                })
                .ToList();
            var sessions = new List<GameSession>(sessionCount);
            var firstDate = new DateTime(
                2016,
                1,
                1,
                2,
                0,
                0,
                DateTimeKind.Utc);
            for (var index = 0; index < sessionCount; index++)
            {
                sessions.Add(CreateSession(
                    games[index % gameCount].Id,
                    games[index % gameCount].Name,
                    firstDate
                        .AddDays(index % 3653)
                        .AddMinutes((index % 120) * 5),
                    300));
            }

            var stopwatch = Stopwatch.StartNew();
            var snapshot = new AnalyticsService().CreateSnapshot(
                games,
                sessions,
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2016, 1, 1),
                    CustomEndDate = new DateTime(2025, 12, 31),
                    AggregationPeriod = AggregationPeriod.Auto,
                    UseIsoWeekStart = true,
                    TopGames = 20
                });
            stopwatch.Stop();

            Equal(
                sessionCount,
                int.Parse(
                    snapshot.SessionCountText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture));
            Equal(10, snapshot.PeriodActivities.Count);
            Equal(20, snapshot.RangeGameRankings.Count);
            Equal(
                30000000UL,
                snapshot.Advanced.HourDistribution.Aggregate<
                    DistributionBarViewModel,
                    ulong>(
                    0,
                    (total, item) => total + item.Seconds));
            Equal(true, stopwatch.Elapsed < TimeSpan.FromSeconds(30));
            Console.WriteLine(
                string.Format(
                    "       100k sessions / 5k games / 10 years: {0:N0} ms",
                    stopwatch.ElapsedMilliseconds));
        }

        private static void TestLargeStoreLoad()
        {
            WithTempDirectory(tempRoot =>
            {
                const int sessionCount = 100000;
                var gameId = Guid.NewGuid();
                var firstDate = new DateTime(
                    2016,
                    1,
                    1,
                    2,
                    0,
                    0,
                    DateTimeKind.Utc);
                var sessions = new List<GameSession>(sessionCount);
                for (var index = 0; index < sessionCount; index++)
                {
                    sessions.Add(CreateSession(
                        gameId,
                        "Large Store",
                        firstDate
                            .AddDays(index % 3653)
                            .AddMinutes(index % 120),
                        300));
                }

                var serializer = new TestSessionSerializer();
                File.WriteAllText(
                    Path.Combine(tempRoot, "sessions.json"),
                    serializer.Serialize(new SessionStoreDocument
                    {
                        SchemaVersion = GameSession.CurrentSchemaVersion,
                        Sessions = sessions
                    }));

                var stopwatch = Stopwatch.StartNew();
                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                var loaded = repository.GetAll();
                stopwatch.Stop();

                Equal(sessionCount, loaded.Count);
                Equal(
                    GameSession.CurrentSchemaVersion,
                    repository.GetStorageDiagnostics().SchemaVersion);
                Equal(true, stopwatch.Elapsed < TimeSpan.FromSeconds(30));
                Console.WriteLine(
                    string.Format(
                        "       schema 4 JSON load / 100k sessions: {0:N0} ms",
                        stopwatch.ElapsedMilliseconds));
            });
        }

        private static void TestInterruptedSessionRecovery()
        {
            WithRepository(repository =>
            {
                var start = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);
                repository.BeginSession(new ActiveGameSession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Recovery Test",
                    StartedAtUtc = start,
                    LastCheckpointUtc = start,
                    StartUtcOffsetMinutes = 480,
                    TimeZoneId = "China Standard Time"
                });

                repository.CheckpointActiveSessions(start.AddSeconds(90));
                Equal(1, repository.RecoverActiveSessions(
                    start.AddMinutes(5),
                    "AutomatedTest"));
                Equal(0, repository.GetActiveSessions().Count);
                Equal(1, repository.GetAll().Count);
                Equal(90UL, repository.GetAll()[0].ElapsedSeconds);
                Equal(SessionSource.Recovered, repository.GetAll()[0].Source);
            });
        }

        private static void TestCompletedSessionDeduplication()
        {
            WithRepository(repository =>
            {
                var session = new GameSession
                {
                    Id = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    GameName = "Dedup Test",
                    StartedAtUtc = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                    EndedAtUtc = new DateTime(2026, 7, 27, 10, 1, 0, DateTimeKind.Utc),
                    ElapsedSeconds = 60,
                    StartUtcOffsetMinutes = 480,
                    EndUtcOffsetMinutes = 480
                };

                Equal(true, repository.CompleteSession(session));
                Equal(false, repository.CompleteSession(session));
                Equal(1, repository.GetAll().Count);
            });
        }

        private static void TestCorruptPrimaryRecoversFromBackup()
        {
            WithTempDirectory(tempRoot =>
            {
                var serializer = new TestSessionSerializer();
                var firstRepository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                firstRepository.CompleteSession(CreateSession("First", 60, 0));
                firstRepository.CompleteSession(CreateSession("Second", 120, 120));

                File.WriteAllText(Path.Combine(tempRoot, "sessions.json"), "{invalid-json");

                var recoveredRepository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                Equal(1, recoveredRepository.GetAll().Count);
                recoveredRepository.CompleteSession(CreateSession("Third", 180, 240));

                var reloadedRepository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                Equal(2, reloadedRepository.GetAll().Count);
            });
        }

        private static void TestIsoWeekRangeBoundary()
        {
            var range = AnalyticsService.ResolveDateRange(
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.ThisWeek,
                    UseIsoWeekStart = true
                },
                new DateTime(2026, 7, 29));

            Equal(new DateTime(2026, 7, 27), range.StartDate);
            Equal(new DateTime(2026, 8, 2), range.EndDate);
        }

        private static void TestCustomRangeNormalizesReversedDates()
        {
            var range = AnalyticsService.ResolveDateRange(
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 8, 9),
                    CustomEndDate = new DateTime(2026, 7, 27)
                },
                new DateTime(2026, 7, 29));

            Equal(new DateTime(2026, 7, 27), range.StartDate);
            Equal(new DateTime(2026, 8, 9), range.EndDate);
        }

        private static void TestWeeklyAggregationAndRangeMetrics()
        {
            var firstGame = Guid.NewGuid();
            var secondGame = Guid.NewGuid();
            var sessions = new[]
            {
                CreateSession(firstGame, "Week One", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 60),
                CreateSession(secondGame, "Week Two", new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc), 120)
            };
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                sessions,
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 8, 9),
                    AggregationPeriod = AggregationPeriod.Week,
                    UseIsoWeekStart = true
                });

            Equal("3 分钟", snapshot.RangeDurationText);
            Equal("2", snapshot.SessionCountText);
            Equal("2", snapshot.ActiveDaysText);
            Equal(2, snapshot.PeriodActivities.Count);
            Equal(60UL, snapshot.PeriodActivities[0].Seconds);
            Equal(120UL, snapshot.PeriodActivities[1].Seconds);
        }

        private static void TestRangeClipsCrossMidnightDuration()
        {
            var session = new GameSession
            {
                GameId = Guid.NewGuid(),
                GameName = "Midnight Clip",
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 60,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[] { session },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 28),
                    CustomEndDate = new DateTime(2026, 7, 28)
                });

            Equal("0 分 30 秒", snapshot.RangeDurationText);
            Equal("1", snapshot.SessionCountText);
            Equal("0 分 30 秒", snapshot.LongestSessionText);
        }

        private static void TestRangeRankingBySessionCount()
        {
            var frequentGame = Guid.NewGuid();
            var longGame = Guid.NewGuid();
            var sessions = new List<GameSession>
            {
                CreateSession(frequentGame, "Frequent", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 30),
                CreateSession(frequentGame, "Frequent", new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc), 30),
                CreateSession(longGame, "Long", new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc), 600)
            };
            var snapshot = new AnalyticsService().CreateSnapshot(
                new[]
                {
                    new Playnite.SDK.Models.Game
                    {
                        Id = frequentGame,
                        Name = "Frequent",
                        Playtime = 900,
                        CoverImage = "frequent-cover"
                    },
                    new Playnite.SDK.Models.Game
                    {
                        Id = longGame,
                        Name = "Long",
                        Playtime = 100,
                        CoverImage = "long-cover"
                    }
                },
                sessions,
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 7, 28),
                    RankingMetric = RankingMetric.SessionCount
                });

            Equal(2, snapshot.RangeGameRankings.Count);
            Equal("Frequent", snapshot.RangeGameRankings[0].Name);
            Equal("2 次", snapshot.RangeGameRankings[0].PrimaryValueText);
            Equal(frequentGame, snapshot.RangeGameRankings[0].GameId);
            Equal(9.09, Math.Round(
                snapshot.RangeGameRankings[0].ProgressPercent,
                2));
            Equal(90.91, Math.Round(
                snapshot.RangeGameRankings[1].ProgressPercent,
                2));
            Equal(2, snapshot.LifetimeGameRankings.Count);
            Equal(frequentGame, snapshot.LifetimeGameRankings[0].GameId);
            Equal(90.0, snapshot.LifetimeGameRankings[0].ProgressPercent);
            Equal(10.0, snapshot.LifetimeGameRankings[1].ProgressPercent);
        }

        private static void TestDashboardTrendProjectionReuse()
        {
            var gameId = Guid.NewGuid();
            var service = new AnalyticsService();
            var result = service.CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(gameId, "Reusable", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Reusable", new DateTime(2026, 8, 3, 10, 0, 0, DateTimeKind.Utc), 120)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 8, 9),
                    AggregationPeriod = AggregationPeriod.Day,
                    UseIsoWeekStart = true
                });

            var weekly = service.CreateTrendProjection(
                result.Context,
                AggregationPeriod.Week);

            Equal(14, result.Snapshot.PeriodActivities.Count);
            Equal(2, weekly.PeriodActivities.Count);
            Equal(60UL, weekly.PeriodActivities[0].Seconds);
            Equal(120UL, weekly.PeriodActivities[1].Seconds);
            Equal("Reusable", weekly.PeriodActivities[0].GameSummaryText);
            Equal(2, weekly.TrendPoints.Count);
        }

        private static void TestDashboardRankingProjectionReuse()
        {
            var frequentGame = Guid.NewGuid();
            var longGame = Guid.NewGuid();
            var service = new AnalyticsService();
            var result = service.CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(frequentGame, "Frequent", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 30),
                    CreateSession(frequentGame, "Frequent", new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc), 30),
                    CreateSession(longGame, "Long", new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc), 600)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 7, 28),
                    RankingMetric = RankingMetric.Duration
                });

            var bySessionCount = service.CreateRankingProjection(
                result.Context,
                RankingMetric.SessionCount,
                10);

            Equal("Long", result.Snapshot.RangeGameRankings[0].Name);
            Equal("Frequent", bySessionCount.RangeGameRankings[0].Name);
            Equal("2 次", bySessionCount.RangeGameRankings[0].PrimaryValueText);
            Equal(frequentGame, bySessionCount.RangeGameRankings[0].GameId);
        }

        private static void TestHeatmapLayoutAndIntensity()
        {
            var gameId = Guid.NewGuid();
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(gameId, "Heat", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(gameId, "Heat", new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc), 120)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 8, 2),
                    UseIsoWeekStart = true
                });

            Equal(1, snapshot.HeatmapColumnCount);
            Equal(7, snapshot.HeatmapCells.Count);
            Equal(new DateTime(2026, 7, 27), snapshot.HeatmapCells[0].Date);
            Equal(new DateTime(2026, 7, 28), snapshot.HeatmapCells[1].Date);
            Equal(true, snapshot.HeatmapCells[1].HeatOpacity >
                snapshot.HeatmapCells[0].HeatOpacity);
        }

        private static void TestTrendPointScaling()
        {
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(Guid.NewGuid(), "First", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 60),
                    CreateSession(Guid.NewGuid(), "Second", new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc), 120)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 27),
                    CustomEndDate = new DateTime(2026, 7, 28),
                    AggregationPeriod = AggregationPeriod.Day
                });

            Equal(2, snapshot.TrendPoints.Count);
            Equal(2, snapshot.TrendLinePoints.Count);
            Equal(true, snapshot.TrendPoints[1].CanvasTop <
                snapshot.TrendPoints[0].CanvasTop);
            var lineGeometry =
                snapshot.TrendLineGeometry as System.Windows.Media.PathGeometry;
            var areaGeometry =
                snapshot.TrendAreaGeometry as System.Windows.Media.PathGeometry;
            Equal(true, lineGeometry != null);
            Equal(true, areaGeometry != null);
            Equal(
                true,
                lineGeometry.Figures[0].Segments[0] is
                    System.Windows.Media.BezierSegment);
            Equal(true, areaGeometry.Figures[0].IsClosed);
        }

        private static void TestPeriodBoundsClipToRange()
        {
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 29),
                    CustomEndDate = new DateTime(2026, 8, 1),
                    AggregationPeriod = AggregationPeriod.Week,
                    UseIsoWeekStart = true
                });

            Equal(1, snapshot.PeriodActivities.Count);
            Equal(new DateTime(2026, 7, 29), snapshot.PeriodActivities[0].PeriodStart);
            Equal(new DateTime(2026, 8, 1), snapshot.PeriodActivities[0].PeriodEnd);
        }

        private static void TestSessionDrilldown()
        {
            var session = new GameSession
            {
                GameId = Guid.NewGuid(),
                GameName = "Recovered Midnight",
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 60,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time",
                Source = SessionSource.Recovered
            };
            var details = new AnalyticsService().CreateSessionDetails(
                new Playnite.SDK.Models.Game[0],
                new[] { session },
                new DateTime(2026, 7, 28),
                new DateTime(2026, 7, 28));

            Equal(1, details.Count);
            Equal(session.GameId, details[0].GameId);
            Equal("0 分 30 秒", details[0].DurationText);
            Equal("异常恢复", details[0].SourceText);
        }

        private static void TestSessionDetailPager()
        {
            var pager = new SessionDetailPager(100);
            pager.Reset(Enumerable.Range(1, 250).Select(index =>
                new SessionDetailViewModel
                {
                    GameName = "Game " + index
                }));

            Equal(250, pager.TotalCount);
            Equal(100, pager.VisibleCount);
            Equal(true, pager.HasMore);
            Equal(100, pager.AppendNextPage());
            Equal(200, pager.VisibleCount);
            Equal(50, pager.AppendNextPage());
            Equal(250, pager.VisibleCount);
            Equal(false, pager.HasMore);
            Equal(0, pager.AppendNextPage());
        }

        private static void TestAutomaticAggregationDefaults()
        {
            Equal(
                AggregationPeriod.Day,
                ResolveAggregation(DateRangePreset.Today, 1));
            Equal(
                AggregationPeriod.Day,
                ResolveAggregation(DateRangePreset.ThisWeek, 7));
            Equal(
                AggregationPeriod.Day,
                ResolveAggregation(DateRangePreset.ThisMonth, 31));
            Equal(
                AggregationPeriod.Month,
                ResolveAggregation(DateRangePreset.ThisYear, 365));
            Equal(
                AggregationPeriod.Day,
                ResolveAggregation(DateRangePreset.Custom, 62));
            Equal(
                AggregationPeriod.Week,
                ResolveAggregation(DateRangePreset.Custom, 63));
            Equal(
                AggregationPeriod.Week,
                ResolveAggregation(DateRangePreset.Custom, 730));
            Equal(
                AggregationPeriod.Month,
                ResolveAggregation(DateRangePreset.Custom, 731));
            Equal(
                AggregationPeriod.Month,
                ResolveAggregation(DateRangePreset.Custom, 3650));
            Equal(
                AggregationPeriod.Year,
                ResolveAggregation(DateRangePreset.Custom, 3651));
        }

        private static void TestManualAggregationOverride()
        {
            var query = new AnalyticsQuery
            {
                RangePreset = DateRangePreset.ThisYear,
                AggregationPeriod = AggregationPeriod.Day
            };
            var range = new AnalyticsDateRange
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };

            Equal(
                AggregationPeriod.Day,
                AnalyticsService.ResolveAggregationPeriod(query, range));
        }

        private static AggregationPeriod ResolveAggregation(
            DateRangePreset preset,
            int inclusiveDays)
        {
            var start = new DateTime(2020, 1, 1);
            return AnalyticsService.ResolveAggregationPeriod(
                new AnalyticsQuery
                {
                    RangePreset = preset,
                    AggregationPeriod = AggregationPeriod.Auto
                },
                new AnalyticsDateRange
                {
                    StartDate = start,
                    EndDate = start.AddDays(inclusiveDays - 1)
                });
        }

        private static void TestSessionQueryFilters()
        {
            var gameId = Guid.NewGuid();
            var game = new Playnite.SDK.Models.Game("Angel Story")
            {
                Id = gameId
            };
            var metadata = new TestGameMetadataAccessor();
            metadata.Add(gameId, MetadataFilterDimension.Tag, "Visual Novel");
            var sessions = new[]
            {
                CreateSessionWithMetadata("Angel Story", "PC (Windows), Steam Deck", SessionSource.Tracked, 0),
                CreateSessionWithMetadata("Angel Story", "PC (Windows)", SessionSource.Recovered, 120),
                CreateSessionWithMetadata("Other Game", "PC (Windows)", SessionSource.Recovered, 240)
            };
            sessions[0].GameId = gameId;
            sessions[1].GameId = gameId;
            var result = new SessionQueryService(metadata).Filter(
                new[] { game },
                sessions,
                new SessionQuery
                {
                    SearchText = "angel",
                    Source = SessionSource.Recovered,
                    MetadataDimension = MetadataFilterDimension.Tag,
                    MetadataValue = "Visual Novel"
                });

            Equal(1, result.Count);
            Equal(SessionSource.Recovered, result[0].Source);
            Equal("Angel Story", result[0].GameName);
        }

        private static void TestSessionQuerySort()
        {
            var sessions = new[]
            {
                CreateSessionWithMetadata("Old", "PC", SessionSource.Tracked, 0),
                CreateSessionWithMetadata("New", "PC", SessionSource.Tracked, 300)
            };
            var result = new SessionQueryService().Filter(
                new Playnite.SDK.Models.Game[0],
                sessions,
                new SessionQuery());

            Equal("New", result[0].GameName);
            Equal("Old", result[1].GameName);
        }

        private static void TestMetadataOptions()
        {
            var first = new Playnite.SDK.Models.Game("First") { Id = Guid.NewGuid() };
            var second = new Playnite.SDK.Models.Game("Second") { Id = Guid.NewGuid() };
            var metadata = new TestGameMetadataAccessor();
            metadata.Add(
                first.Id,
                MetadataFilterDimension.Tag,
                "Visual Novel",
                "Favorite");
            metadata.Add(
                second.Id,
                MetadataFilterDimension.Tag,
                "visual novel",
                "Short");
            metadata.Add(first.Id, MetadataFilterDimension.Publisher, "Publisher A");
            metadata.Add(first.Id, MetadataFilterDimension.Developer, "Developer A");
            metadata.Add(first.Id, MetadataFilterDimension.Genre, "Adventure");
            metadata.Add(first.Id, MetadataFilterDimension.Category, "Backlog");
            var service = new SessionQueryService(metadata);

            var tags = service.GetMetadataValues(
                new[] { first, second },
                MetadataFilterDimension.Tag);

            Equal(3, tags.Count);
            Equal(1, tags.Count(item =>
                item.Equals("Visual Novel", StringComparison.OrdinalIgnoreCase)));
            Equal("Publisher A", service.GetMetadataValues(
                new[] { first },
                MetadataFilterDimension.Publisher)[0]);
            Equal("Developer A", service.GetMetadataValues(
                new[] { first },
                MetadataFilterDimension.Developer)[0]);
            Equal("Adventure", service.GetMetadataValues(
                new[] { first },
                MetadataFilterDimension.Genre)[0]);
            Equal("Backlog", service.GetMetadataValues(
                new[] { first },
                MetadataFilterDimension.Category)[0]);
        }

        private static void TestGameMetadataFilters()
        {
            var first = new Playnite.SDK.Models.Game("First")
            {
                Id = Guid.NewGuid()
            };
            var second = new Playnite.SDK.Models.Game("Second")
            {
                Id = Guid.NewGuid()
            };
            var metadata = new TestGameMetadataAccessor();
            metadata.Add(
                first.Id,
                MetadataFilterDimension.Genre,
                "Adventure");
            metadata.Add(
                first.Id,
                MetadataFilterDimension.Developer,
                "Developer A");
            metadata.Add(
                first.Id,
                MetadataFilterDimension.Tag,
                "Favorite");
            metadata.Add(
                first.Id,
                MetadataFilterDimension.InstallationStatus,
                "已安装");
            metadata.Add(
                second.Id,
                MetadataFilterDimension.Genre,
                "Strategy");
            metadata.Add(
                second.Id,
                MetadataFilterDimension.Developer,
                "Developer B");
            metadata.Add(
                second.Id,
                MetadataFilterDimension.Tag,
                "Backlog");
            metadata.Add(
                second.Id,
                MetadataFilterDimension.InstallationStatus,
                "未安装");
            var service = new SessionQueryService(metadata);

            Equal(
                first.Id,
                service.FilterGames(
                    new[] { first, second },
                    MetadataFilterDimension.Genre,
                    "adventure")[0].Id);
            Equal(
                first.Id,
                service.FilterGames(
                    new[] { first, second },
                    MetadataFilterDimension.Tag,
                    "FAVORITE")[0].Id);
            Equal(
                first.Id,
                service.FilterGames(
                    new[] { first, second },
                    MetadataFilterDimension.Developer,
                    "developer a")[0].Id);
            Equal(
                second.Id,
                service.FilterGames(
                    new[] { first, second },
                    MetadataFilterDimension.InstallationStatus,
                    "未安装")[0].Id);
        }

        private static void TestLibraryMetadata()
        {
            var pluginId = Guid.NewGuid();
            var libraryGame = new Playnite.SDK.Models.Game("Library")
            {
                PluginId = pluginId
            };
            var manualGame = new Playnite.SDK.Models.Game("Manual")
            {
                PluginId = Guid.Empty
            };
            var names = new Dictionary<Guid, string>
            {
                { pluginId, "Steam" }
            };
            var values = new SessionQueryService().GetMetadataValues(
                new[] { libraryGame, manualGame },
                MetadataFilterDimension.Library,
                names);

            Equal(2, values.Count);
            Equal(true, values.Contains("Steam"));
            Equal(true, values.Contains(SessionQueryService.ManualLibraryName));
            Equal(
                libraryGame.PluginId,
                new SessionQueryService().FilterGames(
                    new[] { libraryGame, manualGame },
                    MetadataFilterDimension.Library,
                    "Steam",
                    names)[0].PluginId);
            Equal(
                Guid.Empty,
                new SessionQueryService().FilterGames(
                    new[] { libraryGame, manualGame },
                    MetadataFilterDimension.Library,
                    SessionQueryService.ManualLibraryName,
                    names)[0].PluginId);
        }

        private static void TestRefreshReentrancyGuard()
        {
            var guard = new RefreshReentrancyGuard();
            Equal(true, guard.TryEnter());
            Equal(false, guard.TryEnter());
            Equal(true, guard.IsActive);
            guard.Exit();
            Equal(false, guard.IsActive);
            Equal(true, guard.TryEnter());
        }

        private static void TestSchemaThreeMigration()
        {
            WithTempDirectory(tempRoot =>
            {
                var serializer = new TestSessionSerializer();
                var document = new SessionStoreDocument
                {
                    SchemaVersion = 2,
                    Sessions = new List<GameSession>
                    {
                        CreateSession("Legacy", 60, 0)
                    }
                };
                document.Sessions[0].SchemaVersion = 2;
                File.WriteAllText(
                    Path.Combine(tempRoot, "sessions.json"),
                    serializer.Serialize(document));

                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                Equal(1, repository.GetAll().Count);
                Equal(GameSession.CurrentSchemaVersion, repository.GetAll()[0].SchemaVersion);
            });
        }

        private static void TestAllSchemaUpgrades()
        {
            WithTempDirectory(tempRoot =>
            {
                for (var schema = 1; schema <= 4; schema++)
                {
                    var schemaRoot = Path.Combine(
                        tempRoot,
                        "schema-" + schema);
                    Directory.CreateDirectory(schemaRoot);
                    var serializer = new TestSessionSerializer();
                    var legacy = CreateSession(
                        "Legacy " + schema,
                        (ulong)(60 + schema),
                        schema * 10);
                    legacy.SchemaVersion = schema;
                    var document = new SessionStoreDocument
                    {
                        SchemaVersion = schema,
                        Sessions = new List<GameSession> { legacy }
                    };
                    File.WriteAllText(
                        Path.Combine(schemaRoot, "sessions.json"),
                        serializer.Serialize(document));

                    var repository = new SessionRepository(
                        schemaRoot,
                        new TestLogger(),
                        serializer);
                    var loaded = repository.GetAll();

                    Equal(1, loaded.Count);
                    Equal(legacy.Id, loaded[0].Id);
                    Equal(legacy.GameId, loaded[0].GameId);
                    Equal(legacy.GameName, loaded[0].GameName);
                    Equal(legacy.ElapsedSeconds, loaded[0].ElapsedSeconds);
                    Equal(
                        GameSession.CurrentSchemaVersion,
                        loaded[0].SchemaVersion);
                    Equal(
                        GameSession.CurrentSchemaVersion,
                        repository.GetStorageDiagnostics().SchemaVersion);
                }
            });
        }

        private static void TestDiagnosticReportPrivacy()
        {
            WithTempDirectory(tempRoot =>
            {
                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    new TestSessionSerializer());
                var tracked = CreateSession(
                    "Private Game Name",
                    60,
                    0);
                repository.CompleteSession(tracked);
                var manual = CreateSession(
                    "Another Private Game",
                    90,
                    120);
                manual.Source = SessionSource.Manual;
                repository.CompleteSession(manual);
                repository.SetSessionDeleted(
                    manual.Id,
                    true,
                    "DiagnosticTest");

                var diagnostics = repository.GetStorageDiagnostics();
                var service = new SessionDiagnosticsService();
                var report = service.CreateReport(
                    diagnostics,
                    new DateTime(
                        2026,
                        7,
                        27,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc));
                var reportPath = Path.Combine(tempRoot, "diagnostics.txt");
                service.SaveReport(
                    reportPath,
                    diagnostics,
                    DateTime.UtcNow);

                Equal(2, diagnostics.SessionCount);
                Equal(1, diagnostics.DeletedSessionCount);
                Equal(1, diagnostics.TrackedSessionCount);
                Equal(1, diagnostics.ManualSessionCount);
                Equal(true, diagnostics.SessionsFileExists);
                Equal(true, diagnostics.SessionsFileBytes > 0);
                Equal(false, report.Contains("Private Game Name"));
                Equal(false, report.Contains(tracked.Id.ToString()));
                Equal(false, report.Contains(tempRoot));
                Equal(true, report.Contains("Privacy:"));
                Equal(true, File.Exists(reportPath));
            });
        }

        private static void TestSoftDeleteAndRestore()
        {
            WithRepository(repository =>
            {
                var session = CreateSession("Delete", 60, 0);
                Equal(true, repository.CompleteSession(session));
                Equal(true, repository.SetSessionDeleted(
                    session.Id,
                    true,
                    "TestDelete"));
                Equal(0, repository.GetAll().Count);
                Equal(1, repository.GetAllIncludingDeleted().Count);
                Equal(true, repository.GetAllIncludingDeleted()[0].IsDeleted);
                Equal(true, repository.SetSessionDeleted(
                    session.Id,
                    false,
                    "TestRestore"));
                Equal(1, repository.GetAll().Count);
                Equal(false, repository.GetAll()[0].IsDeleted);
            });
        }

        private static void TestSessionUpdate()
        {
            WithRepository(repository =>
            {
                var session = CreateSession("Before", 60, 0);
                repository.CompleteSession(session);
                var updated = repository.FindSession(session.Id);
                updated.GameName = "After";
                updated.ElapsedSeconds = 90;
                updated.EndedAtUtc = updated.StartedAtUtc.AddSeconds(90);

                Equal(true, repository.UpdateSession(updated, "AutomatedEdit"));
                var saved = repository.FindSession(session.Id);
                Equal(session.Id, saved.Id);
                Equal("After", saved.GameName);
                Equal(90UL, saved.ElapsedSeconds);
                Equal("AutomatedEdit", saved.LastModifiedReason);
                Equal(true, saved.LastModifiedAtUtc.HasValue);
            });
        }

        private static void TestManualSessionEditor()
        {
            var game = new Playnite.SDK.Models.Game("Manual Test")
            {
                Id = Guid.NewGuid()
            };
            var editor = new SessionEditorViewModel(new[] { game })
            {
                StartDate = new DateTime(2026, 7, 27),
                StartTimeText = "20:15:30",
                ElapsedSecondsText = "90"
            };
            GameSession session;

            Equal(true, editor.TryBuild(out session));
            Equal(game.Id, session.GameId);
            Equal("Manual Test", session.GameName);
            Equal(90UL, session.ElapsedSeconds);
            Equal(SessionSource.Manual, session.Source);
            Equal(GameSession.CurrentSchemaVersion, session.SchemaVersion);
            Equal(DateTimeKind.Utc, session.StartedAtUtc.Kind);
            Equal(session.StartedAtUtc.AddSeconds(90), session.EndedAtUtc);
        }

        private static void TestCsvExportEscaping()
        {
            var session = CreateSessionWithMetadata(
                "A, \"B\"\r\nC",
                "PC",
                SessionSource.Tracked,
                0);
            var csv = new SessionExportService().CreateCsv(new[] { session });

            Equal(true, csv.StartsWith("Id,GameId,GameName,"));
            Equal(true, csv.Contains("\"A, \"\"B\"\"\r\nC\""));
            Equal(true, csv.Contains("\"Tracked\""));
        }

        private static void TestJsonExportDocument()
        {
            var session = CreateSessionWithMetadata(
                "JSON Test",
                "PC",
                SessionSource.Imported,
                0);
            var exportedAt = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            var json = new SessionExportService(new TestExportJsonSerializer())
                .CreateJson(new[] { session }, exportedAt);
            var document = JsonConvert.DeserializeObject<SessionExportDocument>(json);

            Equal(1, document.FormatVersion);
            Equal(1, document.SessionCount);
            Equal(1, document.Sessions.Count);
            Equal("JSON Test", document.Sessions[0].GameName);
            Equal(SessionSource.Imported, document.Sessions[0].Source);
        }

        private static void TestPlaytimeInsightsCsvImport()
        {
            WithTempDirectory(tempRoot =>
            {
                var gameId = Guid.NewGuid();
                var game = new Playnite.SDK.Models.Game("CSV Game")
                {
                    Id = gameId
                };
                var session = CreateSession(
                    gameId,
                    "CSV, \"Game\"\r\nName",
                    new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                    91);
                session.ImportSource = "Original";
                var path = Path.Combine(tempRoot, "sessions.csv");
                File.WriteAllText(
                    path,
                    new SessionExportService().CreateCsv(new[] { session }));

                var preview = new SessionImportService(new TestImportJsonSerializer())
                    .Preview(
                        new[] { path },
                        new[] { game },
                        new GameSession[0]);

                Equal(1, preview.ParsedCount);
                Equal(1, preview.ImportableCount);
                Equal(0, preview.InvalidCount);
                Equal("CSV, \"Game\"\r\nName", preview.Candidates[0].GameName);
                Equal(SessionSource.Imported, preview.Candidates[0].Source);
                Equal("Original", preview.Candidates[0].ImportSource);
                Equal("ExactGameId", preview.Candidates[0].ImportConfidence);
            });
        }

        private static void TestGameActivityJsonImport()
        {
            WithTempDirectory(tempRoot =>
            {
                var gameId = Guid.NewGuid();
                var game = new Playnite.SDK.Models.Game("Activity Game")
                {
                    Id = gameId
                };
                var document = new GameActivityImportDocument
                {
                    Id = gameId,
                    Name = game.Name,
                    Items = new List<GameActivityImportItem>
                    {
                        new GameActivityImportItem
                        {
                            DateSession = new DateTime(
                                2026,
                                7,
                                26,
                                12,
                                30,
                                0,
                                DateTimeKind.Utc),
                            ElapsedSeconds = 120
                        }
                    }
                };
                var path = Path.Combine(tempRoot, "gameactivity.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(document));

                var preview = new SessionImportService(new TestImportJsonSerializer())
                    .Preview(
                        new[] { path },
                        new[] { game },
                        new GameSession[0]);

                Equal(1, preview.ImportableCount);
                Equal(gameId, preview.Candidates[0].GameId);
                Equal(DateTimeKind.Utc, preview.Candidates[0].StartedAtUtc.Kind);
                Equal(120UL, preview.Candidates[0].ElapsedSeconds);
                Equal("GameActivityJson", preview.Candidates[0].ImportSource);
                Equal("ExactGameId", preview.Candidates[0].ImportConfidence);
            });
        }

        private static void TestImportPreviewValidation()
        {
            WithTempDirectory(tempRoot =>
            {
                var existing = CreateSession("Existing", 60, 0);
                var csv = new SessionExportService().CreateCsv(new[]
                {
                    existing,
                    CreateSession("Too Long", 31536001UL, 600)
                });
                var path = Path.Combine(tempRoot, "validation.csv");
                File.WriteAllText(path, csv);

                var preview = new SessionImportService(new TestImportJsonSerializer())
                    .Preview(
                        new[] { path },
                        new Playnite.SDK.Models.Game[0],
                        new[] { existing });

                Equal(2, preview.ParsedCount);
                Equal(0, preview.ImportableCount);
                Equal(1, preview.DuplicateCount);
                Equal(1, preview.InvalidCount);
                Equal(1, preview.Errors.Count);
            });
        }

        private static void TestGameActivityLocalizedCsvImport()
        {
            WithTempDirectory(tempRoot =>
            {
                var game = new Playnite.SDK.Models.Game("本地化游戏")
                {
                    Id = Guid.NewGuid()
                };
                var path = Path.Combine(tempRoot, "gameactivity-zh.csv");
                File.WriteAllText(
                    path,
                    "\uFEFF名称;来源;会话日期;游玩时间;游玩时间\r\n" +
                    "本地化游戏;Steam;2026-07-27 20:00:00;120;00:02:00\r\n");

                var preview = new SessionImportService(new TestImportJsonSerializer())
                    .Preview(
                        new[] { path },
                        new[] { game },
                        new GameSession[0]);
                var expectedLocal = DateTime.SpecifyKind(
                    new DateTime(2026, 7, 27, 20, 0, 0),
                    DateTimeKind.Unspecified);
                var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(
                    expectedLocal,
                    TimeZoneInfo.Local);

                Equal(1, preview.ImportableCount);
                Equal(game.Id, preview.Candidates[0].GameId);
                Equal(expectedUtc, preview.Candidates[0].StartedAtUtc);
                Equal(120UL, preview.Candidates[0].ElapsedSeconds);
                Equal("GameActivityCsv", preview.Candidates[0].ImportSource);
                Equal("UniqueNameMatch", preview.Candidates[0].ImportConfidence);
            });
        }

        private static void TestImportCommitRollback()
        {
            WithRepository(repository =>
            {
                repository.CompleteSession(CreateSession("Before Import", 60, 0));
                var imported = CreateSession("Imported", 90, 600);
                imported.Source = SessionSource.Imported;
                imported.ImportSource = "Test";
                imported.ImportConfidence = "ExactGameId";

                var result = repository.ImportSessions(new[] { imported });

                Equal(1, result.ImportedCount);
                Equal(2, repository.GetAll().Count);
                Equal(true, File.Exists(result.RollbackBackupPath));
                Equal("Test", repository.FindSession(imported.Id).ImportSource);
            });
        }

        private static void TestRestoreBackup()
        {
            WithTempDirectory(tempRoot =>
            {
                var serializer = new TestSessionSerializer();
                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);
                var original = CreateSession("Original", 60, 0);
                repository.CompleteSession(original);
                var backupPath = Path.Combine(tempRoot, "manual-backup.json");
                repository.CreateManualBackup(backupPath);

                repository.CompleteSession(CreateSession("Later", 60, 600));
                var active = new ActiveGameSession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Running",
                    StartedAtUtc = DateTime.UtcNow.AddMinutes(-2),
                    LastCheckpointUtc = DateTime.UtcNow,
                    TimeZoneId = TimeZoneInfo.Local.Id
                };
                repository.BeginSession(active);

                var preview = repository.PreviewRestore(backupPath);
                Equal(true, preview.IsValid);
                Equal(1, preview.SessionCount);
                var result = repository.RestoreBackup(backupPath);

                Equal(1, repository.GetAll().Count);
                Equal("Original", repository.GetAll()[0].GameName);
                Equal(1, repository.GetActiveSessions().Count);
                Equal(active.GameId, repository.GetActiveSessions()[0].GameId);
                Equal(true, File.Exists(result.RollbackBackupPath));
            });
        }

        private static void TestReindex()
        {
            WithTempDirectory(tempRoot =>
            {
                var serializer = new TestSessionSerializer();
                var first = CreateSession("Duplicate", 60, 0);
                first.Id = Guid.Empty;
                var duplicate = CreateSession(
                    first.GameId,
                    first.GameName,
                    first.StartedAtUtc.AddSeconds(1),
                    first.ElapsedSeconds);
                var document = new SessionStoreDocument
                {
                    Sessions = new List<GameSession> { first, duplicate }
                };
                File.WriteAllText(
                    Path.Combine(tempRoot, "sessions.json"),
                    serializer.Serialize(document));
                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    serializer);

                var result = repository.Reindex();

                Equal(1, result.SessionCount);
                Equal(1, result.RemovedDuplicateCount);
                Equal(1, result.RepairedIdCount);
                Equal(false, repository.GetAll()[0].Id == Guid.Empty);
                Equal(true, File.Exists(result.RollbackBackupPath));
            });
        }

        private static void TestRestoreRejectsExport()
        {
            WithTempDirectory(tempRoot =>
            {
                var repository = new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    new TestSessionSerializer());
                var exportPath = Path.Combine(tempRoot, "filtered-export.json");
                File.WriteAllText(
                    exportPath,
                    new SessionExportService(new TestExportJsonSerializer())
                        .CreateJson(
                            new[] { CreateSession("Filtered", 60, 0) },
                            DateTime.UtcNow));

                var preview = repository.PreviewRestore(exportPath);

                Equal(false, preview.IsValid);
                Equal(true, preview.Error.Contains("完整备份"));
            });
        }

        private static GameSession CreateSessionWithMetadata(
            string name,
            string platforms,
            SessionSource source,
            int startOffsetSeconds)
        {
            var session = CreateSession(
                Guid.NewGuid(),
                name,
                new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(startOffsetSeconds),
                60);
            session.PlatformNames = platforms;
            session.Source = source;
            return session;
        }

        private static GameSession CreateSession(string name, ulong seconds, int startOffsetSeconds)
        {
            var startedAtUtc = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc)
                .AddSeconds(startOffsetSeconds);
            return CreateSession(Guid.NewGuid(), name, startedAtUtc, seconds);
        }

        private static GameSession CreateSession(
            Guid gameId,
            string name,
            DateTime startedAtUtc,
            ulong seconds)
        {
            return new GameSession
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                GameName = name,
                StartedAtUtc = startedAtUtc,
                EndedAtUtc = startedAtUtc.AddSeconds(seconds),
                ElapsedSeconds = seconds,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };
        }

        private static void WithRepository(Action<SessionRepository> test)
        {
            WithTempDirectory(tempRoot =>
            {
                test(new SessionRepository(
                    tempRoot,
                    new TestLogger(),
                    new TestSessionSerializer()));
            });
        }

        private static void WithTempDirectory(Action<string> test)
        {
            var tempRoot = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                "PlaytimeInsights.Tests." + Guid.NewGuid().ToString("N")));
            var expectedRoot = Path.GetFullPath(Path.GetTempPath());
            if (!tempRoot.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Test directory escaped the system temp directory.");
            }

            Directory.CreateDirectory(tempRoot);
            try
            {
                test(tempRoot);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("[FAIL] " + name + ": " + ex);
            }
        }

        private static void TestLocalizationResourceParity()
        {
            var sourceRoot = FindSourceRoot();
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");
            var english = XDocument.Load(Path.Combine(
                sourceRoot,
                "Localization",
                "en_US.xaml"));
            var chinese = XDocument.Load(Path.Combine(
                sourceRoot,
                "Localization",
                "zh_CN.xaml"));
            var englishValues = english
                .Descendants()
                .Where(element => element.Attribute(xamlNamespace + "Key") != null)
                .ToDictionary(
                    element => element.Attribute(xamlNamespace + "Key").Value,
                    element => element.Value);
            var chineseValues = chinese
                .Descendants()
                .Where(element => element.Attribute(xamlNamespace + "Key") != null)
                .ToDictionary(
                    element => element.Attribute(xamlNamespace + "Key").Value,
                    element => element.Value);

            Equal(englishValues.Count, chineseValues.Count);
            Equal(
                string.Join("|", englishValues.Keys.OrderBy(value => value)),
                string.Join("|", chineseValues.Keys.OrderBy(value => value)));
            Equal(false, englishValues.Values.Any(string.IsNullOrWhiteSpace));
            Equal(false, chineseValues.Values.Any(string.IsNullOrWhiteSpace));
            Equal(
                false,
                englishValues["LOCPlaytimeInsightsMonthRangeFormat"]
                    .Contains("MMM"));

            foreach (var key in englishValues.Keys)
            {
                var englishArguments = ExtractFormatArguments(englishValues[key]);
                var chineseArguments = ExtractFormatArguments(chineseValues[key]);
                Equal(
                    string.Join("|", englishArguments),
                    string.Join("|", chineseArguments));
            }
        }

        private static void TestSidebarIconPublishing()
        {
            var sourceRoot = FindSourceRoot();
            var pluginSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));
            var projectSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.csproj"));
            var iconNames = new[]
            {
                "icon-dashboard.png",
                "icon-sessions.png"
            };

            foreach (var iconName in iconNames)
            {
                var path = Path.Combine(sourceRoot, iconName);
                Equal(true, File.Exists(path));
                Equal(1, Regex.Matches(
                    pluginSource,
                    Regex.Escape("\"" + iconName + "\""),
                    RegexOptions.CultureInvariant).Count);
                Equal(true, projectSource.Contains(
                    "Update=\"" + iconName +
                    "\" CopyToOutputDirectory=\"PreserveNewest\""));

                var bytes = File.ReadAllBytes(path);
                Equal(true, bytes.Length > 33);
                Equal(0x89, (int)bytes[0]);
                Equal("PNG", System.Text.Encoding.ASCII.GetString(bytes, 1, 3));
                var width =
                    (bytes[16] << 24) |
                    (bytes[17] << 16) |
                    (bytes[18] << 8) |
                    bytes[19];
                var height =
                    (bytes[20] << 24) |
                    (bytes[21] << 16) |
                    (bytes[22] << 8) |
                    bytes[23];
                Equal(64, width);
                Equal(64, height);
                Equal(8, (int)bytes[24]);
                Equal(6, (int)bytes[25]);
            }
        }

        private static void TestLocalizedWeekdayLabels()
        {
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    CultureInfo.GetCultureInfo("zh-CN");
                var english = WeekdayLabelService.CreateLabels(
                    DayOfWeek.Monday,
                    (key, fallback) => key
                        .Replace("LOCPlaytimeInsights", string.Empty)
                        .Replace("Short", string.Empty));
                Equal(
                    "Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday",
                    string.Join("|", english));

                System.Threading.Thread.CurrentThread.CurrentCulture =
                    CultureInfo.GetCultureInfo("en-US");
                var chineseNames = new Dictionary<string, string>
                {
                    { "LOCPlaytimeInsightsSundayShort", "周日" },
                    { "LOCPlaytimeInsightsMondayShort", "周一" },
                    { "LOCPlaytimeInsightsTuesdayShort", "周二" },
                    { "LOCPlaytimeInsightsWednesdayShort", "周三" },
                    { "LOCPlaytimeInsightsThursdayShort", "周四" },
                    { "LOCPlaytimeInsightsFridayShort", "周五" },
                    { "LOCPlaytimeInsightsSaturdayShort", "周六" }
                };
                var chinese = WeekdayLabelService.CreateLabels(
                    DayOfWeek.Sunday,
                    (key, fallback) => chineseNames[key]);
                Equal(
                    "周日|周一|周二|周三|周四|周五|周六",
                    string.Join("|", chinese));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    originalCulture;
            }
        }

        private static void TestResponsiveWindowSizing()
        {
            foreach (var scale in new[] { 1d, 1.25d, 1.5d, 2d })
            {
                var workWidth = 1920d / scale;
                var workHeight = 1080d / scale;
                var size = WindowLayoutService.CalculateConstrainedSize(
                    920,
                    680,
                    workWidth,
                    workHeight);
                Equal(true, size.Width <= workWidth);
                Equal(true, size.Height <= workHeight);
                Equal(true, size.Width >= 320);
                Equal(true, size.Height >= 280);
            }

            var fullHdAt200Percent = WindowLayoutService.CalculateConstrainedSize(
                920,
                680,
                960,
                540);
            Equal(920d, fullHdAt200Percent.Width);
            Equal(508d, fullHdAt200Percent.Height);

            var compactWorkArea = WindowLayoutService.CalculateConstrainedSize(
                920,
                680,
                640,
                420);
            Equal(608d, compactWorkArea.Width);
            Equal(388d, compactWorkArea.Height);
        }

        private static void TestThemeAndResponsiveLayout()
        {
            var sourceRoot = FindSourceRoot();
            var xamlFiles = Directory
                .GetFiles(Path.Combine(sourceRoot, "Views"), "*.xaml")
                .Concat(new[]
                {
                    Path.Combine(sourceRoot, "PlaytimeInsightsSettingsView.xaml")
                })
                .ToList();
            var supportedBrushes = new HashSet<string>(
                new[]
                {
                    "ControlBackgroundBrush",
                    "GlyphBrush",
                    "PanelSeparatorBrush",
                    "PopupBackgroundBrush",
                    "TextBrush"
                },
                StringComparer.Ordinal);
            var brushPattern = new Regex(
                @"DynamicResource\s+([A-Za-z0-9]+Brush)",
                RegexOptions.CultureInvariant);
            foreach (var path in xamlFiles)
            {
                var xaml = File.ReadAllText(path);
                foreach (Match match in brushPattern.Matches(xaml))
                {
                    Equal(true, supportedBrushes.Contains(match.Groups[1].Value));
                }
            }

            var dashboard = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var management = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var editor = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionEditorWindow.xaml"));
            var preview = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionImportPreviewWindow.xaml"));
            var dashboardViewModel = string.Join(
                Environment.NewLine,
                new[]
                {
                    File.ReadAllText(Path.Combine(
                        sourceRoot,
                        "ViewModels",
                        "DashboardViewModel.cs"))
                }.Concat(Directory
                    .GetFiles(
                        Path.Combine(sourceRoot, "ViewModels", "Dashboard"),
                        "*.cs")
                    .Select(File.ReadAllText)));
            var coverConverter = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Converters",
                "CoverImageConverter.cs"));
            var adaptiveTrendChart = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Controls",
                "AdaptiveTrendChart.cs"));
            Equal(true, Regex.IsMatch(
                dashboard,
                @"<ScrollViewer\s+x:Name=""DashboardScrollViewer""\s+" +
                @"VerticalScrollBarVisibility=""Auto""\s+" +
                @"HorizontalScrollBarVisibility=""Disabled""",
                RegexOptions.CultureInvariant));
            Equal(true, Regex.Matches(
                dashboard,
                "HorizontalScrollBarVisibility=\"Auto\"").Count >= 5);
            Equal(true, management.Contains("MinWidth=\"960\""));
            Equal(true, management.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\""));
            Equal(true, editor.Contains("ResizeMode=\"CanResizeWithGrip\""));
            Equal(true, editor.Contains("<ScrollViewer"));
            Equal(true, preview.Contains("MinWidth=\"480\""));
            Equal(false, string.Join(string.Empty, xamlFiles.Select(File.ReadAllText))
                .Contains("WindowBackgroundBrush"));
            Equal(true, dashboard.Contains(
                "DataContext.SelectWeekdayCommand"));
            Equal(true, dashboard.Contains(
                "AutomationProperties.Name=\"{Binding AutomationName}\""));
            Equal(true, dashboard.Contains(
                "Text=\"{Binding HourDistributionTitle}\""));
            Equal(false, dashboard.Contains(
                "FocusVisualStyle\" Value=\"{x:Null}\""));
            Equal(false, Regex.IsMatch(
                dashboard,
                @"MetricCardStyle\}""\s+Margin=""0,0,0,12""",
                RegexOptions.CultureInvariant));
            Equal(true, Regex.IsMatch(
                dashboard,
                @"<Trigger Property=""IsKeyboardFocused"" Value=""True"">" +
                @".*?TextBrush.*?Property=""BorderThickness"".*?Value=""1""",
                RegexOptions.CultureInvariant | RegexOptions.Singleline));
            Equal(true, dashboard.Contains("WeekdaySelectedBackgroundBrush"));
            Equal(true, dashboard.Contains("Color=\"#334A90E2\""));
            Equal(true, dashboard.Contains("Color=\"#1A4A90E2\""));
            Equal(true, dashboard.Contains("WeekdaySelectedBorderBrush"));
            Equal(true, dashboard.Contains("Color=\"#FF8B5CF6\""));
            Equal(true, dashboard.Contains("x:Name=\"SelectionIndicator\""));
            Equal(true, dashboard.Contains("WeekdaySelectedIndicatorBrush"));
            Equal(true, dashboard.Contains("<DropShadowEffect Color=\"#FF4A90E2\""));
            Equal(true, dashboard.Contains("BlurRadius=\"12\""));
            Equal(true, dashboard.Contains("ShadowDepth=\"2\""));
            Equal(true, dashboard.Contains("Opacity=\"0.32\""));
            Equal(true, dashboard.Contains("<TranslateTransform x:Name=\"SelectionTransform\""));
            Equal(true, dashboard.Contains("Storyboard.TargetName=\"SelectionTransform\""));
            Equal(true, dashboard.Contains("To=\"-2\""));
            Equal(true, dashboard.Contains("Duration=\"0:0:0.12\""));
            Equal(true, Regex.IsMatch(
                dashboard,
                @"<DataTrigger Binding=""\{Binding IsSelected\}"" Value=""True"">" +
                @".*?WeekdaySelectedBackgroundBrush.*?WeekdaySelectedBorderBrush" +
                @".*?Property=""BorderThickness"".*?Value=""1""" +
                @".*?Property=""Visibility"".*?Value=""Visible""" +
                @".*?Property=""Foreground"".*?#FFFFFFFF" +
                @".*?Property=""FontWeight"".*?Bold",
                RegexOptions.CultureInvariant | RegexOptions.Singleline));
            Equal(true, dashboard.Contains(
                "Source=\"{Binding CoverImagePath, Converter={StaticResource CoverImageConverter}}\""));
            Equal(true, dashboard.Contains(
                "Value=\"{Binding ProgressPercent}\""));
            Equal(true, dashboard.Contains("RankingGoldBrush"));
            Equal(true, dashboard.Contains("RankingSilverBrush"));
            Equal(true, dashboard.Contains("RankingBronzeBrush"));
            Equal(true, dashboard.Contains("FontSize\" Value=\"26\""));
            Equal(true, dashboard.Contains("FontWeight\" Value=\"Bold\""));
            Equal(true, dashboard.Contains("FontFamily\" Value=\"Segoe MDL2 Assets\""));
            Equal(true, dashboard.Contains("Foreground\" Value=\"{DynamicResource TextBrush}\""));
            Equal(true, dashboard.Contains(
                "DataContext=\"{Binding PreviousPeriodComparison}\""));
            Equal(true, dashboard.Contains(
                "DataContext=\"{Binding YearOverYearComparison}\""));
            Equal(true, dashboard.Contains("Text=\"{Binding TagText}\""));
            Equal(true, dashboard.Contains("TrendIncreaseBrush"));
            Equal(true, dashboard.Contains("TrendDecreaseBrush"));
            Equal(true, dashboard.Contains("ChartBarBrush"));
            Equal(true, dashboard.Contains("CornerRadius=\"7,7,0,0\""));
            Equal(true, dashboard.Contains("HeatmapEmptyBrush"));
            Equal(true, dashboard.Contains("Color=\"#FF2A2A2E\""));
            Equal(false, dashboard.Contains(
                "Data=\"{Binding TrendAreaGeometry}\""));
            Equal(false, dashboard.Contains(
                "Data=\"{Binding TrendLineGeometry}\""));
            Equal(false, dashboard.Contains("<Polyline"));
            Equal(false, dashboard.Contains("DailyAggregationBarBrush"));
            Equal(false, dashboard.Contains("AggregationBarStyle"));
            Equal(true, dashboard.Contains(
                "<controls:AdaptiveTrendChart ItemsSource=\"{Binding PeriodActivities}\""));
            Equal(true, dashboard.Contains(
                "PeriodSelected=\"AdaptiveTrendChart_PeriodSelected\""));
            Equal(false, dashboard.Contains(
                "<ItemsControl ItemsSource=\"{Binding PeriodActivities}\">"));
            Equal(true, adaptiveTrendChart.Contains("DrawHover"));
            Equal(true, adaptiveTrendChart.Contains("DashStyles.Dash"));
            Equal(true, adaptiveTrendChart.Contains("maximumLabels"));
            Equal(true, adaptiveTrendChart.Contains(
                "Color.FromRgb(47, 140, 255)"));
            Equal(true, adaptiveTrendChart.Contains(
                "Color.FromRgb(164, 92, 255)"));
            Equal(true, adaptiveTrendChart.Contains("lastLeft"));
            Equal(true, adaptiveTrendChart.Contains("previousRight + 8"));
            Equal(true, adaptiveTrendChart.Contains("renderedItems.Count >= 180"));
            Equal(true, adaptiveTrendChart.Contains("renderedItems.Count <= 90"));
            Equal(true, adaptiveTrendChart.Contains("GameSummaryText"));
            Equal(true, dashboard.Contains(
                "<ListView ItemsSource=\"{Binding SessionDetails}\""));
            var sessionDetailsStart = dashboard.IndexOf(
                "<ListView ItemsSource=\"{Binding SessionDetails}\"",
                StringComparison.Ordinal);
            var sessionStartTimeColumn = dashboard.IndexOf(
                "GridViewColumn Header=\"{DynamicResource LOCPlaytimeInsightsStartTime}\"",
                sessionDetailsStart,
                StringComparison.Ordinal);
            Equal(true, sessionDetailsStart >= 0);
            Equal(true, sessionStartTimeColumn > sessionDetailsStart);
            Equal(
                true,
                dashboard.Substring(
                    sessionDetailsStart,
                    sessionStartTimeColumn - sessionDetailsStart)
                    .Contains("Image Source=\"{Binding CoverImagePath,"));
            Equal(true, dashboard.Contains(
                "<GridView AllowsColumnReorder=\"False\">"));
            Equal(true, dashboard.Contains(
                "GridViewColumn Header=\"{DynamicResource LOCPlaytimeInsightsGameLabel}\""));
            Equal(true, dashboard.Contains(
                "GridViewColumn Header=\"{DynamicResource LOCPlaytimeInsightsStartTime}\""));
            Equal(true, dashboard.Contains(
                "GridViewColumn Header=\"{DynamicResource LOCPlaytimeInsightsDuration}\""));
            Equal(true, dashboard.Contains(
                "GridViewColumn Header=\"{DynamicResource LOCPlaytimeInsightsSessionSource}\""));
            Equal(true, dashboard.Contains("RankingBackgroundProgressStyle"));
            Equal(true, dashboard.Contains("Opacity\" Value=\"0.12\""));
            Equal(true, dashboard.Contains("x:Name=\"PART_Track\""));
            Equal(true, dashboard.Contains("x:Name=\"PART_Indicator\""));
            Equal(true, dashboard.Contains("Grid.ColumnSpan=\"4\""));
            Equal(true, dashboard.Contains("Margin=\"-8,-5\""));
            Equal(false, Regex.IsMatch(
                dashboard,
                @"<ProgressBar\b[^>]*Height=""5""",
                RegexOptions.CultureInvariant | RegexOptions.Singleline));
            Equal(true, dashboard.Contains("HelpIconButtonStyle"));
            Equal(true, management.Contains("HelpIconButtonStyle"));
            Equal(true, dashboard.Contains(
                "ToolTip=\"{DynamicResource LOCPlaytimeInsightsWeekdayFilterHint}\""));
            Equal(true, dashboard.Contains(
                "ToolTip=\"{DynamicResource LOCPlaytimeInsightsDistributionDescription}\""));
            Equal(true, dashboard.Contains(
                "ToolTip=\"{DynamicResource LOCPlaytimeInsightsPeriodChartHint}\""));
            Equal(true, dashboard.Contains(
                "ToolTip=\"{DynamicResource LOCPlaytimeInsightsDataBasisNote}\""));
            Equal(true, management.Contains(
                "ToolTip=\"{DynamicResource LOCPlaytimeInsightsImportSafetyHint}\""));
            Equal(false, dashboard.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsWeekdayFilterHint}\""));
            Equal(false, dashboard.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsDistributionDescription}\""));
            Equal(false, dashboard.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsPeriodChartHint}\""));
            Equal(false, dashboard.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsDataBasisNote}\""));
            Equal(false, management.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsImportSafetyHint}\""));
            Equal(true, dashboard.Contains(
                "Visibility=\"{Binding SessionDetailVisibility}\""));
            Equal(true, dashboardViewModel.Contains(
                "SessionDetailVisibility = Visibility.Collapsed"));
            Equal(true, dashboardViewModel.Contains(
                "SessionDetailVisibility = Visibility.Visible"));
            Equal(true, dashboard.Contains(
                "Text=\"{Binding CurrentStreakDateText}\""));
            var weekdayChartStart = dashboard.IndexOf(
                "ItemsSource=\"{Binding WeekdayDistribution}\"",
                StringComparison.Ordinal);
            var hourChartStart = dashboard.IndexOf(
                "Text=\"{Binding HourDistributionTitle}\"",
                StringComparison.Ordinal);
            Equal(true, weekdayChartStart >= 0);
            Equal(true, hourChartStart > weekdayChartStart);
            Equal(
                false,
                dashboard.Substring(
                    weekdayChartStart,
                    hourChartStart - weekdayChartStart)
                    .Contains("Text=\"{Binding DurationText}\""));
            Equal(false, dashboard.Contains(
                "Text=\"{DynamicResource LOCPlaytimeInsightsDurationComparison}\""));
            Equal(true, dashboardViewModel.Contains(
                "GetFullFilePath(game.CoverImage)"));
            Equal(true, dashboardViewModel.Contains(
                "ApplyCoverImages(details, activeGames)"));
            Equal(true, coverConverter.Contains(
                "CacheOption = BitmapCacheOption.OnLoad"));
            Equal(true, coverConverter.Contains("DecodePixelWidth = 96"));
        }

        private static void TestStageDDashboardComposition()
        {
            var sourceRoot = FindSourceRoot();
            var rootPath = Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs");
            var dashboardDirectory = Path.Combine(
                sourceRoot,
                "ViewModels",
                "Dashboard");
            var root = File.ReadAllText(rootPath);
            var childFiles = new[]
            {
                "DashboardFilterViewModel.cs",
                "DashboardMetricsViewModel.cs",
                "DashboardDistributionViewModel.cs",
                "DashboardDrilldownViewModel.cs"
            };

            foreach (var childFile in childFiles)
            {
                Equal(true, File.Exists(Path.Combine(dashboardDirectory, childFile)));
            }

            Equal(1, Regex.Matches(
                root,
                @"analyticsService\.CreateSnapshotWithContext\(",
                RegexOptions.CultureInvariant).Count);
            Equal(true, root.Contains("Metrics.Apply(result.Snapshot, gamesById)"));
            Equal(true, root.Contains("Distribution.Apply(result.Snapshot)"));
            Equal(true, root.Contains(
                "Drilldown.ResetContext(filteredGames, filteredSessions)"));
            Equal(true, root.Contains(
                "public DashboardFilterViewModel Filter { get; }"));
            Equal(true, root.Contains(
                "public DashboardMetricsViewModel Metrics { get; }"));
            Equal(true, root.Contains(
                "public DashboardDistributionViewModel Distribution { get; }"));
            Equal(true, root.Contains(
                "public DashboardDrilldownViewModel Drilldown { get; }"));

            var childSource = string.Join(
                Environment.NewLine,
                childFiles.Select(file => File.ReadAllText(
                    Path.Combine(dashboardDirectory, file))));
            Equal(false, childSource.Contains("SessionRepository"));
            Equal(false, childSource.Contains("CreateSnapshot("));
            Equal(false, childSource.Contains("sessionRepository.GetAll"));
        }

        private static void TestDashboardNavigationStateLifetime()
        {
            var sourceRoot = FindSourceRoot();
            var plugin = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));

            Equal(true, plugin.Contains(
                "private DashboardViewModel cachedDashboard;"));
            Equal(true, plugin.Contains(
                "if (cachedDashboard == null)"));
            Equal(true, plugin.Contains(
                "activeDashboard = cachedDashboard;"));
            Equal(true, plugin.Contains(
                "Closed = () => activeDashboard = null"));
            Equal(false, plugin.Contains(
                "Closed = () => cachedDashboard = null"));
            Equal(1, Regex.Matches(
                plugin,
                @"new DashboardViewModel\(",
                RegexOptions.CultureInvariant).Count);
        }

        private static void TestDashboardRefreshRootPolicy()
        {
            var sourceRoot = FindSourceRoot();
            var source = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            var trendBlock = ExtractSourceBlock(
                source,
                "private DashboardRefreshTiming ApplyTrendRefresh()",
                "private DashboardRefreshTiming ApplyRankingRefresh()");
            var rankingBlock = ExtractSourceBlock(
                source,
                "private DashboardRefreshTiming ApplyRankingRefresh()",
                "private DashboardRefreshTiming ApplyFullAnalysis()");

            Equal(true, source.Contains("reason => Refresh(reason)"));
            Equal(true, source.Contains(
                "Refresh(DashboardRefreshReason.DataReload)"));
            Equal(true, source.Contains(
                "DashboardRefreshPlan.Create(reason, cacheReady)"));
            Equal(1, Regex.Matches(
                source,
                @"Filter\.GetLibraryNames\(\)",
                RegexOptions.CultureInvariant).Count);
            Equal(1, Regex.Matches(
                source,
                @"Database\.Games\.ToList\(\)",
                RegexOptions.CultureInvariant).Count);
            Equal(1, Regex.Matches(
                source,
                @"sessionRepository\.GetAll\(\)",
                RegexOptions.CultureInvariant).Count);
            Equal(true, trendBlock.Contains("CreateTrendProjection("));
            Equal(true, trendBlock.Contains("Distribution.ApplyTrend("));
            Equal(true, trendBlock.Contains("Metrics.ApplyPeriodTitle("));
            Equal(false, trendBlock.Contains("GetLibraryNames"));
            Equal(false, trendBlock.Contains("sessionRepository"));
            Equal(false, trendBlock.Contains("CreateSnapshot"));
            Equal(true, rankingBlock.Contains("CreateRankingProjection("));
            Equal(true, rankingBlock.Contains(
                "Metrics.ApplyRangeRanking(projection, gamesById)"));
            Equal(false, rankingBlock.Contains("allGames"));
            Equal(false, rankingBlock.Contains("GetLibraryNames"));
            Equal(false, rankingBlock.Contains("sessionRepository"));
            Equal(false, rankingBlock.Contains("CreateSnapshot"));
            Equal(true, source.Contains(
                "PlaytimeInsights Dashboard refresh reason={0} " +
                "data={1}ms filter={2}ms analytics={3}ms apply={4}ms total={5}ms"));
            Equal(true, source.Contains(
                "gamesById = loadedGames.GroupBy(game => game.Id)"));
        }

        private static void TestSidebarNavigationUsesSingleAutomaticRefresh()
        {
            var sourceRoot = FindSourceRoot();
            var plugin = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));
            var dashboardView = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            var sessionView = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml.cs"));
            var dashboardOpened = ExtractSidebarOpenedBlock(
                plugin,
                "icon-dashboard.png");
            var sessionsOpened = ExtractSidebarOpenedBlock(
                plugin,
                "icon-sessions.png");

            Equal(false, dashboardOpened.Contains("activeDashboard.Refresh()"));
            Equal(false, sessionsOpened.Contains(
                "activeSessionManagement.Refresh()"));
            Equal(true, dashboardView.Contains(
                "Loaded += PlaytimeInsightsDashboardView_Loaded"));
            Equal(true, dashboardView.Contains("command.Execute(null)"));
            Equal(true, sessionView.Contains(
                "Loaded += SessionManagementView_Loaded"));
            Equal(true, sessionView.Contains("ViewModel?.Refresh()"));
            Equal(true, dashboardOpened.Contains(
                "activeDashboard = cachedDashboard"));
            Equal(true, plugin.Contains(
                "Closed = () => activeDashboard = null"));
        }

        private static void TestSessionCountUsesRefreshSnapshot()
        {
            var sourceRoot = FindSourceRoot();
            var source = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "SessionManagementViewModel.cs"));
            var countTextBlock = ExtractSourceBlock(
                source,
                "public string CountText",
                "public Visibility LoadMoreVisibility");
            var refreshBlock = ExtractSourceBlock(
                source,
                "public void Refresh()",
                "public void LoadMore()");

            Equal(false, countTextBlock.Contains("repository.GetAll()"));
            Equal(true, countTextBlock.Contains("activeSessionCount"));
            Equal(true, Regex.IsMatch(
                refreshBlock,
                @"activeSessionCount\s*=\s*allSessions\.Count\s*\(" +
                @"\s*session\s*=>\s*!session\.IsDeleted\s*\)",
                RegexOptions.CultureInvariant));
            Equal(1, Regex.Matches(
                refreshBlock,
                @"repository\.GetAllIncludingDeleted\(\)",
                RegexOptions.CultureInvariant).Count);
        }

        private static void TestStageEArchitectureClosure()
        {
            var sourceRoot = FindSourceRoot();
            var viewNames = new[]
            {
                "PlaytimeInsightsDashboardView",
                "SessionManagementView",
                "SessionEditorWindow",
                "SessionImportPreviewWindow"
            };
            var eventPattern = new Regex(
                "(?:Click|PreviewMouseWheel|PeriodSelected|" +
                "MouseLeftButtonUp)=\"([A-Za-z_][A-Za-z0-9_]*)\"",
                RegexOptions.CultureInvariant);
            var loadedPattern = new Regex(
                "Loaded \\+= ([A-Za-z_][A-Za-z0-9_]*);",
                RegexOptions.CultureInvariant);
            var handlerPattern = new Regex(
                @"private\s+(?:static\s+)?[A-Za-z0-9_<>?]+\s+" +
                @"([A-Za-z_][A-Za-z0-9_]*_(?:Click|PreviewMouseWheel|" +
                @"PeriodSelected|MouseLeftButtonUp|Loaded))\s*\(",
                RegexOptions.CultureInvariant);

            foreach (var viewName in viewNames)
            {
                var xaml = File.ReadAllText(Path.Combine(
                    sourceRoot,
                    "Views",
                    viewName + ".xaml"));
                var code = File.ReadAllText(Path.Combine(
                    sourceRoot,
                    "Views",
                    viewName + ".xaml.cs"));
                var eventSources = new HashSet<string>(
                    eventPattern.Matches(xaml)
                        .Cast<Match>()
                        .Select(match => match.Groups[1].Value));
                foreach (Match match in loadedPattern.Matches(code))
                {
                    eventSources.Add(match.Groups[1].Value);
                }

                var declarations = new HashSet<string>(
                    handlerPattern.Matches(code)
                        .Cast<Match>()
                        .Select(match => match.Groups[1].Value));
                Equal(
                    string.Join("|", eventSources.OrderBy(value => value)),
                    string.Join("|", declarations.OrderBy(value => value)));
            }

            var architecturePath = Path.Combine(
                sourceRoot,
                "docs",
                "ARCHITECTURE.md");
            Equal(true, File.Exists(architecturePath));
            var architecture = File.ReadAllText(architecturePath);
            foreach (var boundary in new[]
            {
                "DashboardFilterViewModel",
                "DashboardMetricsViewModel",
                "DashboardDistributionViewModel",
                "DashboardDrilldownViewModel",
                "SessionManagementCoordinator",
                "ISessionManagementInteraction",
                "WpfSessionManagementInteraction",
                "one DashboardSnapshot"
            })
            {
                Equal(true, architecture.Contains(boundary));
            }
        }

        private static void TestTrendPeriodsPublishAtomically()
        {
            var viewModel = new DashboardDistributionViewModel();
            viewModel.Apply(CreateDistributionSnapshot(
                new PeriodActivityViewModel { Label = "old", Seconds = 10 }));
            var oldPeriods = viewModel.PeriodActivities;
            var notifications = 0;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.PeriodActivities))
                {
                    notifications++;
                }
            };

            viewModel.Apply(CreateDistributionSnapshot(
                new PeriodActivityViewModel { Label = "new-a", Seconds = 20 },
                new PeriodActivityViewModel { Label = "new-b", Seconds = 30 }));

            Equal(false, ReferenceEquals(oldPeriods, viewModel.PeriodActivities));
            Equal(1, notifications);
            Equal(2, viewModel.PeriodActivities.Count);
            Equal("new-a", viewModel.PeriodActivities[0].Label);
            Equal("new-b", viewModel.PeriodActivities[1].Label);
        }

        private static void TestDashboardTrendProjectionApplyBoundary()
        {
            var viewModel = new DashboardDistributionViewModel();
            viewModel.Apply(CreateAtomicDistributionSnapshot("initial"));
            var heatmapCells = viewModel.HeatmapCells;
            var weekdays = viewModel.WeekdayDistribution;
            var notifications = new List<string>();
            viewModel.PropertyChanged += (sender, args) =>
                notifications.Add(args.PropertyName);

            viewModel.ApplyTrend(new DashboardTrendProjection
            {
                PeriodActivities = new List<PeriodActivityViewModel>
                {
                    new PeriodActivityViewModel { Label = "projected", Seconds = 42 }
                },
                TrendChartWidth = 720,
                TrendLinePoints = new System.Windows.Media.PointCollection(),
                TrendLineGeometry = System.Windows.Media.Geometry.Empty,
                TrendAreaGeometry = System.Windows.Media.Geometry.Empty,
                TrendPoints = new List<TrendPointViewModel>()
            });

            Equal("projected", viewModel.PeriodActivities[0].Label);
            Equal(true, ReferenceEquals(heatmapCells, viewModel.HeatmapCells));
            Equal(true, ReferenceEquals(weekdays, viewModel.WeekdayDistribution));
            Equal(false, notifications.Contains(nameof(viewModel.HeatmapCells)));
            Equal(false, notifications.Contains(nameof(viewModel.WeekdayDistribution)));
        }

        private static void TestDashboardRankingProjectionApplyBoundary()
        {
            var metrics = new DashboardMetricsViewModel(null);
            var snapshot = CreateAtomicDistributionSnapshot("metrics");
            snapshot.RangeRankingTitleText = "duration";
            snapshot.StatusText = "unchanged status";
            snapshot.RangeGameRankings = new List<GameRankingViewModel>
            {
                new GameRankingViewModel { Name = "old range" }
            };
            snapshot.LifetimeGameRankings = new List<GameRankingViewModel>
            {
                new GameRankingViewModel { Name = "lifetime" }
            };
            metrics.Apply(snapshot, new Playnite.SDK.Models.Game[0]);
            var lifetimeRankings = metrics.LifetimeGameRankings;
            var notifications = new List<string>();
            metrics.PropertyChanged += (sender, args) =>
                notifications.Add(args.PropertyName);

            metrics.ApplyRangeRanking(
                new DashboardRankingProjection
                {
                    RangeRankingTitleText = "session count",
                    RangeGameRankings = new List<GameRankingViewModel>
                    {
                        new GameRankingViewModel { Name = "new range" }
                    }
                },
                new Playnite.SDK.Models.Game[0]);

            Equal("session count", metrics.RangeRankingTitleText);
            Equal("new range", metrics.RangeGameRankings[0].Name);
            Equal("unchanged status", metrics.StatusText);
            Equal(true, ReferenceEquals(lifetimeRankings, metrics.LifetimeGameRankings));
            Equal(false, notifications.Contains(nameof(metrics.LifetimeGameRankings)));
        }

        private static void TestDashboardMajorListsPublishAtomically()
        {
            var viewModel = new DashboardDistributionViewModel();
            viewModel.Apply(CreateAtomicDistributionSnapshot("old"));
            var oldHeatmap = viewModel.HeatmapCells;
            var oldTrend = viewModel.TrendPoints;
            var oldWeekdays = viewModel.WeekdayDistribution;
            var oldHours = viewModel.HourDistribution;
            var notifications = new Dictionary<string, int>();
            viewModel.PropertyChanged += (sender, args) =>
            {
                int count;
                notifications.TryGetValue(args.PropertyName, out count);
                notifications[args.PropertyName] = count + 1;
            };

            viewModel.Apply(CreateAtomicDistributionSnapshot("new"));

            Equal(false, ReferenceEquals(oldHeatmap, viewModel.HeatmapCells));
            Equal(false, ReferenceEquals(oldTrend, viewModel.TrendPoints));
            Equal(false, ReferenceEquals(oldWeekdays, viewModel.WeekdayDistribution));
            Equal(false, ReferenceEquals(oldHours, viewModel.HourDistribution));
            Equal(1, notifications[nameof(viewModel.HeatmapCells)]);
            Equal(1, notifications[nameof(viewModel.TrendPoints)]);
            Equal(1, notifications[nameof(viewModel.WeekdayDistribution)]);
            Equal(1, notifications[nameof(viewModel.HourDistribution)]);

            var unfilteredHours = viewModel.HourDistribution;
            viewModel.SelectWeekday(viewModel.WeekdayDistribution[0]);
            Equal(true, viewModel.WeekdayDistribution[0].IsSelected);
            Equal(false, ReferenceEquals(unfilteredHours, viewModel.HourDistribution));
            Equal(24, viewModel.HourDistribution.Count);
            var filteredHours = viewModel.HourDistribution;
            viewModel.SelectWeekday(viewModel.WeekdayDistribution[0]);
            Equal(false, viewModel.WeekdayDistribution[0].IsSelected);
            Equal(false, ReferenceEquals(filteredHours, viewModel.HourDistribution));
        }

        private static DashboardSnapshot CreateAtomicDistributionSnapshot(string suffix)
        {
            var weekdays = Enumerable.Range(0, 7)
                .Select(index => new DistributionBarViewModel
                {
                    Label = suffix + "-day-" + index,
                    Seconds = (ulong)(index + 1)
                })
                .ToList();
            var hours = Enumerable.Range(0, 24)
                .Select(index => new DistributionBarViewModel
                {
                    Label = index.ToString("00") + ":00",
                    Seconds = (ulong)(index + 1)
                })
                .ToList();
            var cells = Enumerable.Range(0, 7)
                .SelectMany(day => Enumerable.Range(0, 24).Select(hour =>
                    new WeekHourCellViewModel
                    {
                        DayLabel = suffix + "-day-" + day,
                        HourLabel = hour.ToString("00") + ":00",
                        Seconds = (ulong)(day + hour + 1)
                    }))
                .ToList();
            return new DashboardSnapshot
            {
                PeriodActivities = new List<PeriodActivityViewModel>
                {
                    new PeriodActivityViewModel { Label = suffix, Seconds = 1 }
                },
                HeatmapCells = new List<HeatmapCellViewModel>
                {
                    new HeatmapCellViewModel { TooltipText = suffix }
                },
                HeatmapWeekdayLabels = new List<string> { suffix },
                HeatmapColumnCount = 1,
                TrendLinePoints = new System.Windows.Media.PointCollection(),
                TrendLineGeometry = System.Windows.Media.Geometry.Empty,
                TrendAreaGeometry = System.Windows.Media.Geometry.Empty,
                TrendPoints = new List<TrendPointViewModel>
                {
                    new TrendPointViewModel { TooltipText = suffix }
                },
                RangeGameRankings = new List<GameRankingViewModel>(),
                LifetimeGameRankings = new List<GameRankingViewModel>(),
                Advanced = new AdvancedAnalyticsSnapshot
                {
                    WeekdayDistribution = weekdays,
                    HourDistribution = hours,
                    WeekHourCells = cells,
                    WeekdayLabels = new List<string> { suffix },
                    HourLabels = new List<string> { suffix },
                    AnomalyVisibility = System.Windows.Visibility.Collapsed,
                    Anomalies = new List<AnomalySessionViewModel>
                    {
                        new AnomalySessionViewModel { GameName = suffix }
                    }
                }
            };
        }

        private static DashboardSnapshot CreateDistributionSnapshot(
            params PeriodActivityViewModel[] periods)
        {
            return new DashboardSnapshot
            {
                PeriodActivities = (periods ??
                    new PeriodActivityViewModel[0]).ToList(),
                HeatmapCells = new List<HeatmapCellViewModel>(),
                HeatmapWeekdayLabels = new List<string>(),
                HeatmapColumnCount = 1,
                TrendLinePoints = new System.Windows.Media.PointCollection(),
                TrendLineGeometry = System.Windows.Media.Geometry.Empty,
                TrendAreaGeometry = System.Windows.Media.Geometry.Empty,
                TrendPoints = new List<TrendPointViewModel>(),
                Advanced = new AdvancedAnalyticsSnapshot
                {
                    WeekdayDistribution = new List<DistributionBarViewModel>(),
                    HourDistribution = new List<DistributionBarViewModel>(),
                    WeekHourCells = new List<WeekHourCellViewModel>(),
                    WeekdayLabels = new List<string>(),
                    HourLabels = new List<string>(),
                    AnomalyVisibility = System.Windows.Visibility.Collapsed,
                    Anomalies = new List<AnomalySessionViewModel>()
                }
            };
        }

        private static void TestTrendChartSourceLifecycle()
        {
            RunOnSta(() =>
            {
                var oldSource = new ObservableCollection<PeriodActivityViewModel>
                {
                    new PeriodActivityViewModel
                    {
                        Label = "old",
                        DurationText = "10 秒",
                        Seconds = 10
                    }
                };
                var chart = new AdaptiveTrendChart
                {
                    ItemsSource = oldSource,
                    Width = 640,
                    Height = 230
                };
                RenderTrendChart(chart);
                Equal(1, GetPrivateListCount(chart, "renderedItems"));

                SetPrivateField(chart, "hoverIndex", 0);
                var currentSource =
                    new ObservableCollection<PeriodActivityViewModel>
                    {
                        new PeriodActivityViewModel
                        {
                            Label = "new-a",
                            DurationText = "20 秒",
                            Seconds = 20
                        },
                        new PeriodActivityViewModel
                        {
                            Label = "new-b",
                            DurationText = "30 秒",
                            Seconds = 30
                        }
                    };
                chart.ItemsSource = currentSource;

                Equal(0, GetPrivateListCount(chart, "renderedItems"));
                Equal(0, GetPrivateListCount(chart, "renderedPoints"));
                Equal(-1, GetPrivateField<int>(chart, "hoverIndex"));

                RenderTrendChart(chart);
                Equal(2, GetPrivateListCount(chart, "renderedItems"));
                oldSource.Add(new PeriodActivityViewModel
                {
                    Label = "detached-old",
                    DurationText = "40 秒",
                    Seconds = 40
                });
                Equal(2, GetPrivateListCount(chart, "renderedItems"));

                currentSource.Add(new PeriodActivityViewModel
                {
                    Label = "new-c",
                    DurationText = "50 秒",
                    Seconds = 50
                });
                Equal(0, GetPrivateListCount(chart, "renderedItems"));
                Equal(0, GetPrivateListCount(chart, "renderedPoints"));
                RenderTrendChart(chart);
                Equal(3, GetPrivateListCount(chart, "renderedItems"));
            });
        }

        private static void TestResponsiveMetricPanelColumns()
        {
            RunOnSta(() =>
            {
                foreach (var sample in new[]
                {
                    new { Width = 320d, Columns = 1 },
                    new { Width = 360d, Columns = 1 },
                    new { Width = 640d, Columns = 2 },
                    new { Width = 900d, Columns = 3 },
                    new { Width = 1200d, Columns = 4 }
                })
                {
                    var panel = CreateMetricPanel(9, 154);
                    LayoutMetricPanel(panel, sample.Width);
                    var firstTop = GetLayoutSlot(panel.Children[0]).Top;
                    var columns = panel.Children
                        .Cast<UIElement>()
                        .TakeWhile(child =>
                            Math.Abs(GetLayoutSlot(child).Top - firstTop) < 0.01)
                        .Count();
                    Equal(sample.Columns, columns);
                }
            });
        }

        private static void TestResponsiveMetricPanelArrangement()
        {
            RunOnSta(() =>
            {
                foreach (var sample in new[]
                {
                    new { Count = 9, Width = 640d, Columns = 2 },
                    new { Count = 10, Width = 640d, Columns = 2 },
                    new { Count = 9, Width = 1200d, Columns = 4 },
                    new { Count = 10, Width = 1200d, Columns = 4 }
                })
                {
                    var arranged = CreateMetricPanel(sample.Count, 154);
                    LayoutMetricPanel(arranged, sample.Width);
                    AssertResponsiveMetricPanelSlots(
                        arranged,
                        sample.Width,
                        sample.Columns);
                }

                var panel = CreateMetricPanel(9, 154);
                ((Border)panel.Children[1]).MinHeight = 190;
                LayoutMetricPanel(panel, 1200);

                var first = GetLayoutSlot(panel.Children[0]);
                var second = GetLayoutSlot(panel.Children[1]);
                var fourth = GetLayoutSlot(panel.Children[3]);
                var ninth = GetLayoutSlot(panel.Children[8]);

                Equal(true, Math.Abs(first.Width - second.Width) < 0.01);
                Equal(true, Math.Abs(first.Height - second.Height) < 0.01);
                Equal(true, first.Width >= 204 && first.Width <= 300);
                Equal(true, fourth.Right <= 1200);
                Equal(true, Math.Abs(ninth.Left - ((1200 - ninth.Width) / 2)) < 0.01);
            });
        }

        private static void AssertResponsiveMetricPanelSlots(
            ResponsiveUniformPanel panel,
            double availableWidth,
            int expectedColumns)
        {
            var slots = panel.Children
                .Cast<UIElement>()
                .Select(GetLayoutSlot)
                .ToList();
            var rows = new List<List<Rect>>();

            foreach (var slot in slots)
            {
                var row = rows.Count == 0
                    ? null
                    : rows[rows.Count - 1];
                if (row == null ||
                    Math.Abs(row[0].Top - slot.Top) >= 0.01)
                {
                    row = new List<Rect>();
                    rows.Add(row);
                }

                row.Add(slot);
            }

            Equal(
                (slots.Count + expectedColumns - 1) / expectedColumns,
                rows.Count);

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var expectedItemsInRow = Math.Min(
                    expectedColumns,
                    slots.Count - (rowIndex * expectedColumns));
                Equal(expectedItemsInRow, row.Count);

                var first = row[0];
                for (var itemIndex = 0; itemIndex < row.Count; itemIndex++)
                {
                    var slot = row[itemIndex];
                    Equal(true, Math.Abs(slot.Width - first.Width) < 0.01);
                    Equal(true, Math.Abs(slot.Top - first.Top) < 0.01);
                    Equal(true, Math.Abs(slot.Height - first.Height) < 0.01);
                    Equal(true, slot.Width > 0 && slot.Height > 0);
                    Equal(true, slot.Left >= 0);
                    Equal(true, slot.Right <= availableWidth);
                    Equal(true, slot.Bottom <= panel.DesiredSize.Height);

                    if (itemIndex > 0)
                    {
                        var previous = row[itemIndex - 1];
                        Equal(
                            true,
                            Math.Abs((slot.Left - previous.Right) - 12d) < 0.01);
                    }
                }

                if (rowIndex > 0)
                {
                    var previousRow = rows[rowIndex - 1];
                    foreach (var previous in previousRow)
                    {
                        foreach (var slot in row)
                        {
                            Equal(
                                true,
                                Math.Abs((slot.Top - previous.Bottom) - 12d) < 0.01);
                        }
                    }
                }
            }

            var lastRow = rows[rows.Count - 1];
            if (lastRow.Count < expectedColumns)
            {
                var rowWidth = (lastRow.Count * lastRow[0].Width) +
                    ((lastRow.Count - 1) * 12d);
                Equal(
                    true,
                    Math.Abs(lastRow[0].Left -
                        ((availableWidth - rowWidth) / 2)) < 0.01);
            }
        }

        private static void TestResponsiveMetricPanelEdgeCases()
        {
            RunOnSta(() =>
            {
                foreach (var count in new[] { 0, 1, 9, 10 })
                {
                    var panel = CreateMetricPanel(count, 154);
                    LayoutMetricPanel(panel, 640);
                    Equal(true, IsFiniteNonNegative(panel.DesiredSize.Width));
                    Equal(true, IsFiniteNonNegative(panel.DesiredSize.Height));
                    foreach (UIElement child in panel.Children)
                    {
                        var slot = GetLayoutSlot(child);
                        Equal(true, IsFiniteNonNegative(slot.X));
                        Equal(true, IsFiniteNonNegative(slot.Y));
                        Equal(true, IsFiniteNonNegative(slot.Width));
                        Equal(true, IsFiniteNonNegative(slot.Height));
                    }

                    var slots = panel.Children
                        .Cast<UIElement>()
                        .Select(GetLayoutSlot)
                        .Where(slot => slot.Width > 0 && slot.Height > 0)
                        .ToList();
                    for (var left = 0; left < slots.Count; left++)
                    {
                        for (var right = left + 1; right < slots.Count; right++)
                        {
                            Equal(false, slots[left].IntersectsWith(slots[right]));
                        }
                    }
                }

                var collapsed = CreateMetricPanel(3, 154);
                collapsed.Children[1].Visibility = Visibility.Collapsed;
                LayoutMetricPanel(collapsed, 640);
                Equal(new Rect(0, 0, 0, 0), GetLayoutSlot(collapsed.Children[1]));

                var invalid = CreateMetricPanel(3, 154);
                invalid.MinItemWidth = double.NaN;
                invalid.PreferredItemWidth = double.PositiveInfinity;
                invalid.MaxItemWidth = -1;
                invalid.MinColumns = 0;
                invalid.MaxColumns = -4;
                invalid.HorizontalSpacing = -12;
                invalid.VerticalSpacing = double.NaN;
                LayoutMetricPanel(invalid, 0);
                invalid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Equal(true, IsFiniteNonNegative(invalid.DesiredSize.Width));
                Equal(true, IsFiniteNonNegative(invalid.DesiredSize.Height));
            });
        }

        private static void TestResponsiveMetricVisualFoundation()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1200
                };
                var testTextBrush = new SolidColorBrush(Colors.White);
                view.Resources["TextBrush"] = testTextBrush;
                view.Measure(new Size(1200, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                view.UpdateLayout();
                var panels = FindVisualDescendants<ResponsiveUniformPanel>(view);
                Equal(1, panels.Count);
                var panel = panels[0];
                Equal(9, panel.Children.Count);
                Equal(true, panel.Children.Cast<UIElement>().All(
                    child => child is Border));
                Equal(204d, panel.MinItemWidth);
                Equal(232d, panel.PreferredItemWidth);
                Equal(300d, panel.MaxItemWidth);
                Equal(1, panel.MinColumns);
                Equal(4, panel.MaxColumns);
                Equal(12d, panel.HorizontalSpacing);
                Equal(12d, panel.VerticalSpacing);
                Equal(true, panel.CenterIncompleteRow);

                Equal(1d, (double)view.Resources["TextOpacityPrimary"]);
                Equal(0.72d, (double)view.Resources["TextOpacitySecondary"]);
                Equal(0.58d, (double)view.Resources["TextOpacityTertiary"]);
                Equal(0.45d, (double)view.Resources["TextOpacityDisabled"]);

                var card = new Border
                {
                    Style = (Style)view.Resources["MetricCardStyle"]
                };
                Equal(154d, card.MinHeight);
                Equal(new Thickness(16), card.Padding);
                Equal(true, double.IsNaN(card.Width));
                Equal(true, double.IsNaN(card.Height));
                Equal(new Thickness(0), card.Margin);

                var header = new TextBlock
                {
                    Style = (Style)view.Resources["MetricHeaderStyle"]
                };
                var icon = new TextBlock
                {
                    Style = (Style)view.Resources["MetricIconStyle"]
                };
                var helper = new TextBlock
                {
                    Style = (Style)view.Resources["MetricHelperTextStyle"]
                };
                Equal(0.72d, header.Opacity);
                Equal(0.58d, icon.Opacity);
                Equal(0.58d, helper.Opacity);

                var textBrush = view.FindResource("TextBrush");
                var helperHost = new Border
                {
                    Child = helper
                };
                panel.Children.Add(helperHost);
                helperHost.Measure(new Size(240, double.PositiveInfinity));
                helperHost.Arrange(new Rect(0, 0, 240, helperHost.DesiredSize.Height));
                view.UpdateLayout();
                helper.UpdateLayout();
                Equal(textBrush, helper.Foreground);
                panel.Children.Remove(helperHost);

                LayoutMetricPanel(panel, 1200);
                panel.Arrange(new Rect(0, 0, 1200, panel.DesiredSize.Height));
                var ninth = GetLayoutSlot(panel.Children[8]);
                Equal(true,
                    Math.Abs(ninth.Left - ((1200 - ninth.Width) / 2)) < 0.01);
            });
        }

        private static ResponsiveUniformPanel CreateMetricPanel(int count, double minHeight)
        {
            var panel = new ResponsiveUniformPanel();
            for (var index = 0; index < count; index++)
            {
                panel.Children.Add(new Border
                {
                    MinHeight = minHeight,
                    Child = new TextBlock
                    {
                        Text = index == 1
                            ? "Long localized helper text that wraps onto another line"
                            : "Metric " + index,
                        TextWrapping = TextWrapping.Wrap
                    }
                });
            }

            return panel;
        }

        private static List<T> FindVisualDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            var matches = new List<T>();
            if (root == null)
            {
                return matches;
            }

            CollectVisualDescendants(root, matches);
            return matches;
        }

        private static void CollectVisualDescendants<T>(
            DependencyObject root,
            IList<T> matches)
            where T : DependencyObject
        {
            for (var index = 0;
                index < VisualTreeHelper.GetChildrenCount(root);
                index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                var match = child as T;
                if (match != null)
                {
                    matches.Add(match);
                }

                CollectVisualDescendants(child, matches);
            }
        }

        private static void LayoutMetricPanel(
            ResponsiveUniformPanel panel,
            double width)
        {
            panel.Measure(new Size(width, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
            panel.UpdateLayout();
        }

        private static Rect GetLayoutSlot(UIElement element)
        {
            return LayoutInformation.GetLayoutSlot((FrameworkElement)element);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                value >= 0;
        }

        private static void TestDashboardFilterRefreshReasons()
        {
            var reasons = new List<DashboardRefreshReason>();
            var viewModel = new DashboardFilterViewModel(
                null,
                new SessionQueryService(new TestGameMetadataAccessor()),
                7,
                reasons.Add);

            viewModel.SelectedRangeOption = viewModel.RangeOptions[0];
            viewModel.SelectedAggregationOption = viewModel.AggregationOptions[1];
            viewModel.SelectedRankingMetricOption = viewModel.RankingMetricOptions[1];
            viewModel.SelectedMetadataDimensionOption =
                viewModel.MetadataDimensionOptions[1];
            viewModel.SelectedMetadataValueOption = new SelectionOption<string>
            {
                Value = "Steam",
                Label = "Steam"
            };
            viewModel.SelectedRangeOption = viewModel.RangeOptions[4];
            viewModel.CustomStartDate = viewModel.CustomStartDate.AddDays(-1);
            viewModel.CustomEndDate = viewModel.CustomEndDate.AddDays(-1);

            Equal(
                "Range|Aggregation|Ranking|MetadataDimension|" +
                "MetadataValue|Range|Range|Range",
                string.Join("|", reasons));
        }

        private static void TestDashboardRefreshPlans()
        {
            var uncached = DashboardRefreshPlan.Create(
                DashboardRefreshReason.Aggregation,
                false);
            Equal(DashboardRefreshMode.FullAnalysis, uncached.Mode);
            Equal(true, uncached.ReloadData);
            Equal(true, uncached.RefreshMetadataOptions);
            Equal(true, uncached.RebuildFilter);

            var aggregation = DashboardRefreshPlan.Create(
                DashboardRefreshReason.Aggregation,
                true);
            Equal(DashboardRefreshMode.TrendOnly, aggregation.Mode);
            Equal(false, aggregation.ReloadData);
            Equal(false, aggregation.RefreshMetadataOptions);
            Equal(false, aggregation.RebuildFilter);

            var ranking = DashboardRefreshPlan.Create(
                DashboardRefreshReason.Ranking,
                true);
            Equal(DashboardRefreshMode.RankingOnly, ranking.Mode);
            Equal(false, ranking.ReloadData);

            var range = DashboardRefreshPlan.Create(
                DashboardRefreshReason.Range,
                true);
            Equal(DashboardRefreshMode.FullAnalysis, range.Mode);
            Equal(false, range.ReloadData);
            Equal(false, range.RefreshMetadataOptions);
            Equal(false, range.RebuildFilter);

            var dimension = DashboardRefreshPlan.Create(
                DashboardRefreshReason.MetadataDimension,
                true);
            Equal(true, dimension.RefreshMetadataOptions);
            Equal(true, dimension.RebuildFilter);

            var value = DashboardRefreshPlan.Create(
                DashboardRefreshReason.MetadataValue,
                true);
            Equal(false, value.RefreshMetadataOptions);
            Equal(true, value.RebuildFilter);
        }

        private static void RunOnSta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("STA chart test timed out.");
            }

            if (error != null)
            {
                throw new InvalidOperationException(
                    "STA chart test failed.",
                    error);
            }
        }

        private static void RenderTrendChart(AdaptiveTrendChart chart)
        {
            chart.Measure(new Size(640, 230));
            chart.Arrange(new Rect(0, 0, 640, 230));
            chart.UpdateLayout();
            var bitmap = new RenderTargetBitmap(
                640,
                230,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(chart);
        }

        private static int GetPrivateListCount(
            AdaptiveTrendChart chart,
            string fieldName)
        {
            var value = GetPrivateField<object>(chart, fieldName);
            var count = value.GetType().GetProperty("Count");
            return (int)count.GetValue(value, null);
        }

        private static T GetPrivateField<T>(
            AdaptiveTrendChart chart,
            string fieldName)
        {
            var field = typeof(AdaptiveTrendChart).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException(
                    "Missing chart field: " + fieldName);
            }

            return (T)field.GetValue(chart);
        }

        private static void SetPrivateField(
            AdaptiveTrendChart chart,
            string fieldName,
            object value)
        {
            var field = typeof(AdaptiveTrendChart).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException(
                    "Missing chart field: " + fieldName);
            }

            field.SetValue(chart, value);
        }

        private static void TestSessionManagementVisualHierarchy()
        {
            var sourceRoot = FindSourceRoot();
            var management = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var managementCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml.cs"));
            var managementViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "SessionManagementViewModel.cs"));
            var queryService = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Services",
                "SessionQueryService.cs"));

            var importIndex = management.IndexOf(
                "LOCPlaytimeInsightsImportButton",
                StringComparison.Ordinal);
            var advancedIndex = management.IndexOf(
                "x:Name=\"AdvancedOptionsButton\"",
                StringComparison.Ordinal);
            Equal(true, importIndex >= 0);
            Equal(true, advancedIndex > importIndex);
            Equal(true, management.Contains(
                "Content=\"{DynamicResource LOCPlaytimeInsightsAdvancedOptions}\""));
            Equal(true, management.Contains("<Button.ContextMenu>"));
            Equal(true, management.Contains(
                "Header=\"{DynamicResource LOCPlaytimeInsightsRestoreBackupButton}\""));
            Equal(false, management.Contains(
                "<Button Content=\"{DynamicResource LOCPlaytimeInsightsRestoreBackupButton}\""));
            Equal(true, managementCode.Contains(
                "AdvancedOptionsButton_Click"));

            Equal(true, management.Contains("AlternationCount=\"2\""));
            Equal(true, management.Contains(
                "Property=\"ItemsControl.AlternationIndex\""));
            Equal(true, management.Contains("Value=\"#202A2A2E\""));
            Equal(true, management.Contains("Value=\"#384A90E2\""));
            Equal(true, management.Contains(
                "<Grid Height=\"44\" MinWidth=\"960\""));
            Equal(true, management.Contains(
                "Source=\"{Binding CoverImagePath,"));
            Equal(true, management.Contains("Width=\"24\""));
            Equal(true, management.Contains("Height=\"34\""));
            Equal(true, management.Contains("SourceTagStyle"));
            Equal(true, management.Contains("StateTagStyle"));
            Equal(true, Regex.Matches(
                management,
                "HorizontalAlignment=\"Right\"").Count >= 4);

            Equal(true, managementViewModel.Contains(
                "GetFullFilePath(game.CoverImage)"));
            Equal(true, managementViewModel.Contains(
                "public string CoverImagePath"));
            Equal(true, queryService.Contains("GameId = session.GameId"));
            Equal(true, queryService.Contains("Source = session.Source"));
        }

        private static void TestDashboardMouseWheelRouting()
        {
            var sourceRoot = FindSourceRoot();
            var dashboard = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var dashboardCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            Equal(true, dashboard.Contains(
                "x:Name=\"DashboardScrollViewer\""));
            Equal(
                5,
                Regex.Matches(
                    dashboard,
                    "PreviewMouseWheel=\"NestedScrollViewer_PreviewMouseWheel\"")
                    .Count);
            Equal(true, dashboardCode.Contains(
                "CanContinueVerticalScroll(nestedScrollViewer, e.Delta)"));
            Equal(true, dashboardCode.Contains(
                "scrollViewer.VerticalOffset > 0"));
            Equal(true, dashboardCode.Contains(
                "scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight"));
            Equal(true, dashboardCode.Contains(
                "RoutedEvent = Mouse.MouseWheelEvent"));
            Equal(true, dashboardCode.Contains(
                "DashboardScrollViewer.RaiseEvent(forwardedEvent)"));
            Equal(true, dashboardCode.Contains(
                "FindVisualChild<ScrollViewer>"));
        }

        private static void TestArchitectureRefactorBaseline()
        {
            var sourceRoot = FindSourceRoot();
            var baseline = File.ReadAllText(Path.Combine(
                sourceRoot,
                "docs",
                "ARCHITECTURE_REFACTOR_BASELINE.md"));
            var sessionXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var dashboardXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var editorXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionEditorWindow.xaml"));
            var importXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionImportPreviewWindow.xaml"));
            var sessionCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml.cs"));
            var dashboardCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            var interactionContract = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Presentation",
                "Interactions",
                "ISessionManagementInteraction.cs"));
            var coordinator = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Presentation",
                "Coordinators",
                "SessionManagementCoordinator.cs"));

            var eventPattern = new Regex(
                "(?:Click|PreviewMouseWheel|PeriodSelected|" +
                "MouseLeftButtonUp)=\"([A-Za-z_][A-Za-z0-9_]*)\"");
            foreach (var xaml in new[]
            {
                sessionXaml,
                dashboardXaml,
                editorXaml,
                importXaml
            })
            {
                foreach (Match match in eventPattern.Matches(xaml))
                {
                    Equal(true, baseline.Contains(
                        "`" + match.Groups[1].Value + "`"));
                }
            }

            var loadedPattern = new Regex(
                "Loaded \\+= ([A-Za-z_][A-Za-z0-9_]*);");
            foreach (var code in new[] { sessionCode, dashboardCode })
            {
                foreach (Match match in loadedPattern.Matches(code))
                {
                    Equal(true, baseline.Contains(
                        "`" + match.Groups[1].Value + "`"));
                }
            }

            var forbiddenViewModelTokens = new[]
            {
                "MessageBox",
                "OpenFileDialog",
                "SaveFileDialog",
                "SessionEditorWindow",
                "SessionImportPreviewWindow",
                "Window.GetWindow",
                "System.Windows.Controls"
            };
            foreach (var viewModelFile in new[]
            {
                "SessionManagementViewModel.cs",
                "DashboardViewModel.cs",
                "SessionEditorViewModel.cs"
            })
            {
                var viewModel = File.ReadAllText(Path.Combine(
                    sourceRoot,
                    "ViewModels",
                    viewModelFile));
                foreach (var token in forbiddenViewModelTokens)
                {
                    Equal(false, viewModel.Contains(token));
                }
            }

            foreach (var token in new[]
            {
                "System.Windows",
                "MessageBox",
                "MessageBoxResult",
                "OpenFileDialog",
                "SaveFileDialog",
                "SessionEditorWindow",
                "SessionImportPreviewWindow"
            })
            {
                Equal(false, interactionContract.Contains(token));
                Equal(false, coordinator.Contains(token));
            }
            Equal(true, interactionContract.Contains(
                "IReadOnlyList<string> SelectImportFiles()"));
            Equal(true, interactionContract.Contains(
                "bool ConfirmRestore(SessionRestorePreview preview)"));
            Equal(true, interactionContract.Contains(
                "GameSession EditSession(SessionEditorViewModel editor)"));
            Equal(true, coordinator.Contains(
                "public sealed class SessionManagementCoordinator"));

            var project = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.csproj"));
            foreach (var dependency in new[]
            {
                "CommunityToolkit.Mvvm",
                "Microsoft.Xaml.Behaviors",
                "Prism",
                "ReactiveUI"
            })
            {
                Equal(false, project.Contains(dependency));
            }

            foreach (var binding in new[]
            {
                "IsEnabled=\"{Binding CanEdit}\"",
                "IsEnabled=\"{Binding CanDelete}\"",
                "IsEnabled=\"{Binding HasFilteredSessions}\"",
                "Visibility=\"{Binding LoadMoreVisibility}\""
            })
            {
                Equal(true, sessionXaml.Contains(binding));
            }
            Equal(true, importXaml.Contains(
                "IsEnabled=\"{Binding CanImport}\""));
            Equal(true, sessionXaml.Contains(
                "Command=\"{Binding RestoreSelectedCommand}\""));
            Equal(true, dashboardXaml.Contains(
                "Visibility=\"{Binding LoadMoreVisibility}\""));

            Equal(true, editorXaml.Contains(
                "FocusManager.FocusedElement=\"{Binding ElementName=GameSelector}\""));
            Equal(true, editorXaml.Contains(
                "KeyboardNavigation.TabNavigation=\"Cycle\""));
            Equal(true, editorXaml.Contains("IsCancel=\"True\""));
            Equal(true, editorXaml.Contains("IsDefault=\"True\""));
            Equal(true, importXaml.Contains(
                "KeyboardNavigation.TabNavigation=\"Cycle\""));
            Equal(true, importXaml.Contains("IsCancel=\"True\""));
            Equal(true, importXaml.Contains("IsDefault=\"True\""));

            foreach (var scenario in new[]
            {
                "取消导入文件选择",
                "导入预览后取消",
                "删除确认取消",
                "无效备份恢复",
                "恢复确认取消",
                "导出写入失败",
                "编辑或补录窗口取消",
                "重建索引确认取消"
            })
            {
                Equal(true, baseline.Contains(scenario));
            }
        }

        private static void TestRelayCommand()
        {
            var enabled = false;
            var executeCount = 0;
            var changedCount = 0;
            var command = new global::PlaytimeInsights.ViewModels.RelayCommand(
                () => executeCount++,
                () => enabled);
            command.CanExecuteChanged += (sender, args) => changedCount++;

            Equal(false, command.CanExecute(null));
            enabled = true;
            command.RaiseCanExecuteChanged();
            Equal(1, changedCount);
            Equal(true, command.CanExecute(null));
            command.Execute(null);
            Equal(1, executeCount);
        }

        private static void TestGenericRelayCommand()
        {
            string captured = null;
            var changedCount = 0;
            var command =
                new global::PlaytimeInsights.ViewModels.RelayCommand<string>(
                value => captured = value,
                value => !string.IsNullOrWhiteSpace(value));
            command.CanExecuteChanged += (sender, args) => changedCount++;

            Equal(false, command.CanExecute(null));
            Equal(false, command.CanExecute(42));
            Equal(true, command.CanExecute("weekday"));
            command.Execute("weekday");
            Equal("weekday", captured);
            command.RaiseCanExecuteChanged();
            Equal(1, changedCount);

            var threw = false;
            try
            {
                command.Execute(42);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Equal(true, threw);
            Equal(
                false,
                new global::PlaytimeInsights.ViewModels.RelayCommand<int>(
                    value => { })
                    .CanExecute(null));
        }

        private static void TestStageBCommandBindings()
        {
            var sourceRoot = FindSourceRoot();
            var sessionXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var sessionCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml.cs"));
            var sessionViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "SessionManagementViewModel.cs"));
            var dashboardXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var dashboardCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            var dashboardViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));

            foreach (var command in new[]
            {
                "Command=\"{Binding RefreshCommand}\"",
                "Command=\"{Binding RestoreSelectedCommand}\"",
                "Command=\"{Binding LoadMoreCommand}\""
            })
            {
                Equal(true, sessionXaml.Contains(command));
            }
            Equal(false, sessionXaml.Contains("Click=\"RefreshButton_Click\""));
            Equal(false, sessionXaml.Contains("Click=\"LoadMoreButton_Click\""));
            Equal(false, sessionXaml.Contains("Click=\"RestoreSessionButton_Click\""));
            Equal(false, sessionCode.Contains("RefreshButton_Click"));
            Equal(false, sessionCode.Contains("LoadMoreButton_Click"));
            Equal(false, sessionCode.Contains("RestoreSessionButton_Click"));
            Equal(true, sessionViewModel.Contains(
                "public RelayCommand RestoreSelectedCommand"));
            Equal(true, sessionViewModel.Contains(
                "!refreshGuard.IsActive && CanRestore"));
            Equal(true, sessionViewModel.Contains(
                "!refreshGuard.IsActive && pager.HasMore"));

            Equal(true, dashboardXaml.Contains(
                "Command=\"{Binding RefreshCommand}\""));
            Equal(true, dashboardXaml.Contains(
                "DataContext.SelectWeekdayCommand"));
            Equal(true, dashboardXaml.Contains(
                "CommandParameter=\"{Binding}\""));
            Equal(true, dashboardXaml.Contains(
                "Command=\"{Binding LoadMoreSessionDetailsCommand}\""));
            Equal(false, dashboardXaml.Contains(
                "Click=\"WeekdayDistribution_Click\""));
            Equal(false, dashboardCode.Contains("WeekdayDistribution_Click"));
            Equal(true, dashboardCode.Contains("SelectPeriodCommand"));
            Equal(true, dashboardCode.Contains("SelectHeatmapDateCommand"));
            Equal(true, dashboardViewModel.Contains(
                "private readonly RefreshReentrancyGuard refreshGuard"));
            Equal(true, dashboardViewModel.Contains(
                "public RelayCommand<DistributionBarViewModel> SelectWeekdayCommand"));
            Equal(true, dashboardViewModel.Contains(
                "public RelayCommand<HeatmapCellViewModel> SelectHeatmapDateCommand"));
            Equal(true, dashboardViewModel.Contains(
                "public RelayCommand<PeriodActivityViewModel> SelectPeriodCommand"));
        }

        private static void TestExportErrorTitle()
        {
            var sourceRoot = FindSourceRoot();
            var coordinator = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Presentation",
                "Coordinators",
                "SessionManagementCoordinator.cs"));
            var english = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Localization",
                "en_US.xaml"));
            var chinese = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Localization",
                "zh_CN.xaml"));

            Equal(true, coordinator.Contains(
                "LOCPlaytimeInsightsExportFailedTitle"));
            Equal(false, coordinator.Contains(
                "LOCPlaytimeInsightsExportCsvButton"));
            Equal(false, coordinator.Contains(
                "LOCPlaytimeInsightsExportJsonButton"));
            Equal(true, english.Contains(
                "x:Key=\"LOCPlaytimeInsightsExportFailedTitle\">Export failed<"));
            Equal(true, chinese.Contains(
                "x:Key=\"LOCPlaytimeInsightsExportFailedTitle\">导出失败<"));
        }

        private static void TestCoordinatorCancelsImportFileSelection()
        {
            var operations = new FakeSessionManagementOperations();
            var interaction = new FakeSessionManagementInteraction();
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.ImportSessions());
            Equal(0, operations.PreviewImportCalls);
            Equal(0, operations.CommitImportCalls);
            Equal(0, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestCoordinatorCancelsImportPreview()
        {
            var operations = new FakeSessionManagementOperations();
            var interaction = new FakeSessionManagementInteraction
            {
                ImportFiles = new[] { "sessions.csv" },
                ConfirmImportResult = false
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.ImportSessions());
            Equal(1, operations.PreviewImportCalls);
            Equal(1, interaction.ConfirmImportCalls);
            Equal(0, operations.CommitImportCalls);
            Equal(0, operations.MutationCalls);
        }

        private static void TestCoordinatorCancelsDeleteConfirmation()
        {
            var operations = new FakeSessionManagementOperations
            {
                CanDelete = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                ConfirmDeleteResult = false
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.DeleteSelectedSession());
            Equal(1, interaction.ConfirmDeleteCalls);
            Equal(0, operations.DeleteCalls);
            Equal(0, operations.MutationCalls);
        }

        private static void TestCoordinatorBlocksInvalidRestore()
        {
            var operations = new FakeSessionManagementOperations
            {
                RestorePreview = new SessionRestorePreview
                {
                    IsValid = false,
                    Error = "Invalid backup"
                }
            };
            var interaction = new FakeSessionManagementInteraction
            {
                RestorePath = "invalid.json"
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.RestoreBackup());
            Equal(1, operations.PreviewRestoreCalls);
            Equal(0, interaction.ConfirmRestoreCalls);
            Equal(0, operations.RestoreCalls);
            Equal(0, operations.MutationCalls);
            Equal(1, interaction.ErrorCount);
        }

        private static void TestCoordinatorCancelsRestoreConfirmation()
        {
            var operations = new FakeSessionManagementOperations
            {
                RestorePreview = new SessionRestorePreview
                {
                    IsValid = true,
                    SessionCount = 4,
                    SchemaVersion = GameSession.CurrentSchemaVersion
                }
            };
            var interaction = new FakeSessionManagementInteraction
            {
                RestorePath = "backup.json",
                ConfirmRestoreResult = false
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.RestoreBackup());
            Equal(1, operations.PreviewRestoreCalls);
            Equal(1, interaction.ConfirmRestoreCalls);
            Equal(0, operations.RestoreCalls);
            Equal(0, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestCoordinatorContainsExportFailure()
        {
            var operations = new FakeSessionManagementOperations
            {
                ThrowOnExportCsv = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                ExportPath = "sessions.csv"
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.ExportCsv());
            Equal(1, operations.ExportCsvCalls);
            Equal(0, operations.MutationCalls);
            Equal(1, interaction.ErrorCount);
        }

        private static void TestCoordinatorCancelsEditor()
        {
            var operations = new FakeSessionManagementOperations
            {
                CanEdit = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                EditorResult = null
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.EditSelectedSession());
            Equal(1, operations.CreateEditorCalls);
            Equal(1, interaction.EditSessionCalls);
            Equal(0, operations.UpdateCalls);
            Equal(0, operations.MutationCalls);
        }

        private static void TestCoordinatorCancelsReindex()
        {
            var operations = new FakeSessionManagementOperations();
            var interaction = new FakeSessionManagementInteraction
            {
                ConfirmReindexResult = false
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.Reindex());
            Equal(1, interaction.ConfirmReindexCalls);
            Equal(0, operations.ReindexCalls);
            Equal(0, operations.MutationCalls);
        }

        private static void TestStageCComposition()
        {
            var sourceRoot = FindSourceRoot();
            var gitignore = File.ReadAllText(Path.Combine(
                sourceRoot,
                ".gitignore"));
            var plugin = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));
            var view = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml.cs"));
            var interaction = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Presentation",
                "Interactions",
                "WpfSessionManagementInteraction.cs"));

            Equal(true, gitignore.Contains(".claude/"));
            Equal(true, plugin.Contains(
                "new WpfSessionManagementInteraction("));
            Equal(true, plugin.Contains(
                "new SessionManagementCoordinator("));
            Equal(true, plugin.Contains(
                "new SessionManagementView(coordinator)"));
            Equal(true, view.Contains(
                "private readonly SessionManagementCoordinator coordinator"));
            foreach (var call in new[]
            {
                "coordinator.AddSession()",
                "coordinator.EditSelectedSession()",
                "coordinator.DeleteSelectedSession()",
                "coordinator.ExportCsv()",
                "coordinator.ExportJson()",
                "coordinator.ImportSessions()",
                "coordinator.CreateBackup()",
                "coordinator.RestoreBackup()",
                "coordinator.Reindex()",
                "coordinator.SaveDiagnostics()"
            })
            {
                Equal(true, view.Contains(call));
            }

            foreach (var forbidden in new[]
            {
                "OpenFileDialog",
                "SaveFileDialog",
                "MessageBox.Show",
                "SessionEditorWindow",
                "SessionImportPreviewWindow",
                "ShowDataError",
                "private static void Export"
            })
            {
                Equal(false, view.Contains(forbidden));
            }

            foreach (var required in new[]
            {
                "class WpfSessionManagementInteraction",
                "new OpenFileDialog",
                "new SaveFileDialog",
                "new SessionEditorWindow",
                "new SessionImportPreviewWindow",
                "Owner = ownerProvider()",
                "LOCPlaytimeInsightsDeleteConfirmation",
                "LOCPlaytimeInsightsRestoreConfirmationFormat",
                "LOCPlaytimeInsightsReindexConfirmation",
                "LOCPlaytimeInsightsSessionFileFilter",
                "LOCPlaytimeInsightsBackupFileFilter",
                "LOCPlaytimeInsightsErrorFormat"
            })
            {
                Equal(true, interaction.Contains(required));
            }
        }

        private static void TestCoordinatorCompletesImport()
        {
            var operations = new FakeSessionManagementOperations
            {
                ImportPreview = new SessionImportPreview
                {
                    Candidates = new List<GameSession>
                    {
                        new GameSession { Id = Guid.NewGuid() }
                    }
                }
            };
            var interaction = new FakeSessionManagementInteraction
            {
                ImportFiles = new[] { "sessions.csv" },
                ConfirmImportResult = true
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(true, coordinator.ImportSessions());
            Equal(1, operations.PreviewImportCalls);
            Equal(1, interaction.ConfirmImportCalls);
            Equal(1, operations.CommitImportCalls);
            Equal(1, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestCoordinatorCompletesRestore()
        {
            var operations = new FakeSessionManagementOperations
            {
                RestorePreview = new SessionRestorePreview
                {
                    IsValid = true,
                    SessionCount = 3,
                    SchemaVersion = GameSession.CurrentSchemaVersion
                }
            };
            var interaction = new FakeSessionManagementInteraction
            {
                RestorePath = "backup.json",
                ConfirmRestoreResult = true
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(true, coordinator.RestoreBackup());
            Equal(1, operations.PreviewRestoreCalls);
            Equal(1, interaction.ConfirmRestoreCalls);
            Equal(1, operations.RestoreCalls);
            Equal(1, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestCoordinatorCompletesEditAndReindex()
        {
            var edited = new GameSession
            {
                Id = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                GameName = "Edited"
            };
            var operations = new FakeSessionManagementOperations
            {
                CanEdit = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                EditorResult = edited,
                ConfirmReindexResult = true
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(true, coordinator.EditSelectedSession());
            Equal(true, coordinator.Reindex());
            Equal(1, operations.UpdateCalls);
            Equal(1, operations.ReindexCalls);
            Equal(2, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestCoordinatorContainsImportFailure()
        {
            var operations = new FakeSessionManagementOperations
            {
                ThrowOnPreviewImport = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                ImportFiles = new[] { "sessions.csv" }
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(false, coordinator.ImportSessions());
            Equal(1, operations.PreviewImportCalls);
            Equal(0, operations.CommitImportCalls);
            Equal(0, operations.MutationCalls);
            Equal(1, interaction.ErrorCount);
        }

        private static void TestCoordinatorCompletesRemainingWorkflows()
        {
            var operations = new FakeSessionManagementOperations
            {
                CanDelete = true
            };
            var interaction = new FakeSessionManagementInteraction
            {
                ExportPath = "sessions.csv",
                BackupPath = "backup.json",
                DiagnosticsPath = "diagnostics.txt",
                ConfirmDeleteResult = true,
                EditorResult = new GameSession
                {
                    Id = Guid.NewGuid(),
                    GameId = Guid.NewGuid(),
                    GameName = "Added"
                }
            };
            var coordinator = new SessionManagementCoordinator(
                operations,
                interaction);

            Equal(true, coordinator.ExportCsv());
            interaction.ExportPath = "sessions.json";
            Equal(true, coordinator.ExportJson());
            Equal(true, coordinator.CreateBackup());
            Equal(true, coordinator.AddSession());
            Equal(true, coordinator.DeleteSelectedSession());
            Equal(true, coordinator.SaveDiagnostics());

            Equal(1, operations.ExportCsvCalls);
            Equal(1, operations.ExportJsonCalls);
            Equal(1, operations.CreateBackupCalls);
            Equal(1, operations.AddCalls);
            Equal(1, operations.DeleteCalls);
            Equal(1, operations.SaveDiagnosticsCalls);
            Equal(2, operations.MutationCalls);
            Equal(0, interaction.ErrorCount);
        }

        private static void TestReleaseMetadataAndReadme()
        {
            var sourceRoot = FindSourceRoot();
            var manifest = File.ReadAllText(Path.Combine(
                sourceRoot,
                "extension.yaml"));
            var assemblyInfo = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Properties",
                "AssemblyInfo.cs"));
            var dashboard = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var sessions = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var english = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Localization",
                "en_US.xaml"));
            var chinese = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Localization",
                "zh_CN.xaml"));
            var readme = File.ReadAllText(Path.Combine(
                sourceRoot,
                "README.md"));
            var project = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.csproj"));
            var installerManifest = File.ReadAllText(Path.Combine(
                sourceRoot,
                "manifests",
                "installer.yaml"));
            var addonManifest = File.ReadAllText(Path.Combine(
                sourceRoot,
                "manifests",
                "addon.yaml"));
            var license = File.ReadAllText(Path.Combine(
                sourceRoot,
                "LICENSE"));

            Equal(true, manifest.Contains("Version: 0.9.8"));
            Equal(true, manifest.Contains("Author: SHINKU1506"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights/issues"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights/blob/main/CHANGELOG.md"));
            Equal(true, assemblyInfo.Contains(
                "AssemblyVersion(\"0.9.8.0\")"));
            Equal(true, assemblyInfo.Contains(
                "AssemblyFileVersion(\"0.9.8.0\")"));
            Equal(true, assemblyInfo.Contains(
                "AssemblyCompany(\"SHINKU1506\")"));
            Equal(true, assemblyInfo.Contains(
                "Copyright © SHINKU1506 2026"));
            Equal(true, license.Contains(
                "Copyright (c) 2026 SHINKU1506"));

            Equal(false, dashboard.Contains(
                "LOCPlaytimeInsightsDashboardSubtitle"));
            Equal(false, sessions.Contains(
                "LOCPlaytimeInsightsSessionsSubtitle"));
            Equal(false, english.Contains(
                "LOCPlaytimeInsightsDashboardSubtitle"));
            Equal(false, english.Contains(
                "LOCPlaytimeInsightsSessionsSubtitle"));
            Equal(false, chinese.Contains(
                "LOCPlaytimeInsightsDashboardSubtitle"));
            Equal(false, chinese.Contains(
                "LOCPlaytimeInsightsSessionsSubtitle"));

            Equal(true, readme.Contains("当前版本：`0.9.8`"));
            Equal(true, readme.Contains(
                "作者：[SHINKU1506](https://github.com/SHINKU1506)"));
            Equal(true, readme.Contains("## 界面预览"));
            Equal(true, readme.Contains("## 安装与升级"));
            Equal(true, readme.Contains("## 数据、隐私与诊断"));
            Equal(true, readme.Contains("## 已知限制"));
            Equal(true, readme.Contains("## 从源码构建"));
            Equal(true, readme.Contains("## 问题反馈"));
            Equal(true, readme.Contains("## License"));
            Equal(false, readme.Contains("当前开发版本：`0.9.2`"));
            Equal(false, readme.Contains("原生柱形图与折线趋势"));
            Equal(false, readme.Contains("按日聚合柱形"));

            Equal(true, project.Contains(
                "<DebugType>None</DebugType>"));
            Equal(true, project.Contains(
                "<DebugSymbols>false</DebugSymbols>"));
            Equal(true, project.Contains(
                "<PathMap>$(MSBuildProjectDirectory)=/_/PlaytimeInsights</PathMap>"));
            Equal(true, project.Contains(
                "<None Update=\"LICENSE\" CopyToOutputDirectory=\"PreserveNewest\" />"));
            Equal(true, project.Contains(
                "<Page Remove=\"staging\\**\\*.xaml\" />"));
            Equal(true, project.Contains(
                "<Resource Remove=\"staging\\**\\*\" />"));

            Equal(true, installerManifest.Contains(
                "AddonId: PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd"));
            Equal(true, installerManifest.Contains("Version: 0.9.8"));
            Equal(true, installerManifest.Contains(
                "RequiredApiVersion: 6.16.0"));
            Equal(true, installerManifest.Contains(
                "/releases/download/v0.9.8/PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_8.pext"));
            Equal(true, addonManifest.Contains("Type: Generic"));
            Equal(true, addonManifest.Contains("Author: SHINKU1506"));
            Equal(true, addonManifest.Contains(
                "InstallerManifestUrl: https://raw.githubusercontent.com/SHINKU1506/PlaytimeInsights/main/manifests/installer.yaml"));
            Equal(true, addonManifest.Contains(
                "SourceUrl: https://github.com/SHINKU1506/PlaytimeInsights"));
            Equal(true, addonManifest.Contains("Screenshots:"));

            var screenshotRoot = Path.Combine(
                sourceRoot,
                "docs",
                "screenshots",
                "0.9.8");
            foreach (var screenshot in new[]
            {
                "dashboard-zh.png",
                "dashboard-en.png",
                "settings-zh.png"
            })
            {
                Equal(true, File.Exists(Path.Combine(screenshotRoot, screenshot)));
                Equal(true, addonManifest.Contains(
                    "/docs/screenshots/0.9.8/" + screenshot));
            }
        }

        private static void TestLocalizationSourceCoverage()
        {
            var sourceRoot = FindSourceRoot();
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");
            var english = XDocument.Load(Path.Combine(
                sourceRoot,
                "Localization",
                "en_US.xaml"));
            var resourceKeys = new HashSet<string>(
                english.Descendants()
                    .Select(element => element.Attribute(xamlNamespace + "Key"))
                    .Where(attribute => attribute != null)
                    .Select(attribute => attribute.Value),
                StringComparer.Ordinal);
            var keyPattern = new Regex(
                @"LocalizationService\.(?:Get|Format)\(\s*""([^""]+)""",
                RegexOptions.CultureInvariant);
            var referencedKeys = Directory
                .GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                    path.IndexOf(
                        Path.DirectorySeparatorChar + "Tests" +
                        Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf(
                        Path.DirectorySeparatorChar + "obj" +
                        Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase) < 0)
                .SelectMany(path => keyPattern
                    .Matches(File.ReadAllText(path))
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            Equal(
                string.Empty,
                string.Join(
                    "|",
                    referencedKeys
                        .Where(key => !resourceKeys.Contains(key))
                        .OrderBy(key => key)));
        }

        private static void TestLegacySettingsMatrix()
        {
            var settingsJsonByRelease = new Dictionary<string, string>
            {
                { "0.1", "{}" },
                { "0.2", "{\"EnableSessionTracking\":false}" },
                { "0.3", "{\"RecentDays\":14}" },
                { "0.4", "{\"TopGames\":20}" },
                { "0.5", "{\"UseIsoWeekStart\":false}" },
                { "0.6", "{\"RecoverInterruptedSessions\":false}" },
                { "0.7", "{\"RecentDays\":30,\"TopGames\":15}" },
                { "0.8", "{\"EnableSessionTracking\":true,\"UseIsoWeekStart\":true}" },
                { "0.9", "{\"EnableSessionTracking\":true,\"RecoverInterruptedSessions\":true,\"RecentDays\":7,\"TopGames\":10,\"UseIsoWeekStart\":true}" }
            };

            foreach (var fixture in settingsJsonByRelease)
            {
                var settings = JsonConvert.DeserializeObject<
                    PlaytimeInsightsSettings>(fixture.Value);
                Equal(true, settings != null);
                Equal(true, settings.RecentDays >= 1);
                Equal(true, settings.TopGames >= 1);
            }

            var oldest = JsonConvert.DeserializeObject<
                PlaytimeInsightsSettings>(settingsJsonByRelease["0.1"]);
            Equal(true, oldest.EnableSessionTracking);
            Equal(true, oldest.RecoverInterruptedSessions);
            Equal(7, oldest.RecentDays);
            Equal(10, oldest.TopGames);
            Equal(true, oldest.UseIsoWeekStart);
        }

        private static IList<int> ExtractFormatArguments(string value)
        {
            return Regex.Matches(
                    value ?? string.Empty,
                    @"\{([0-9]+)(?:[^}]*)\}",
                    RegexOptions.CultureInvariant)
                .Cast<Match>()
                .Select(match => int.Parse(
                    match.Groups[1].Value,
                    CultureInfo.InvariantCulture))
                .OrderBy(index => index)
                .ToList();
        }

        private static void TestNativeViewAccessibility()
        {
            var sourceRoot = FindSourceRoot();
            var viewFiles = new[]
            {
                Path.Combine(sourceRoot, "Views", "PlaytimeInsightsDashboardView.xaml"),
                Path.Combine(sourceRoot, "Views", "SessionManagementView.xaml"),
                Path.Combine(sourceRoot, "Views", "SessionEditorWindow.xaml"),
                Path.Combine(sourceRoot, "Views", "SessionImportPreviewWindow.xaml"),
                Path.Combine(sourceRoot, "PlaytimeInsightsSettingsView.xaml")
            };
            var hardcodedChineseAttribute = new Regex(
                "(?:Text|Content|Header|Title|ToolTip|StringFormat)=\"[^\"]*[一-龥]",
                RegexOptions.CultureInvariant);
            foreach (var path in viewFiles)
            {
                var xaml = File.ReadAllText(path);
                Equal(false, hardcodedChineseAttribute.IsMatch(xaml));
                Equal(true, xaml.Contains("DynamicResource LOCPlaytimeInsights"));
                Equal(true, xaml.Contains("AutomationProperties.Name"));
                Equal(true, xaml.Contains("KeyboardNavigation."));
            }
        }

        private static string FindSourceRoot()
        {
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "PlaytimeInsights.csproj")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Localization")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate the PlaytimeInsights source root.");
        }

        private static string ExtractSidebarOpenedBlock(
            string source,
            string iconName)
        {
            var iconIndex = source.IndexOf(iconName, StringComparison.Ordinal);
            if (iconIndex < 0)
            {
                throw new InvalidOperationException(
                    "Could not locate sidebar icon " + iconName + ".");
            }

            var openedIndex = source.IndexOf(
                "Opened = () =>",
                iconIndex,
                StringComparison.Ordinal);
            var closedIndex = source.IndexOf(
                "Closed =",
                openedIndex,
                StringComparison.Ordinal);
            if (openedIndex < 0 || closedIndex < 0)
            {
                throw new InvalidOperationException(
                    "Could not locate sidebar lifecycle block for " +
                    iconName + ".");
            }

            return source.Substring(openedIndex, closedIndex - openedIndex);
        }

        private static string ExtractSourceBlock(
            string source,
            string startMarker,
            string endMarker)
        {
            var startIndex = source.IndexOf(
                startMarker,
                StringComparison.Ordinal);
            var endIndex = source.IndexOf(
                endMarker,
                startIndex,
                StringComparison.Ordinal);
            if (startIndex < 0 || endIndex < 0)
            {
                throw new InvalidOperationException(
                    "Could not extract source block between " +
                    startMarker + " and " + endMarker + ".");
            }

            return source.Substring(startIndex, endIndex - startIndex);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    string.Format("Expected {0}, actual {1}.", expected, actual));
            }
        }

        private sealed class FakeSessionManagementOperations :
            ISessionManagementOperations
        {
            public bool CanEdit { get; set; }

            public bool CanDelete { get; set; }

            public bool ThrowOnExportCsv { get; set; }

            public bool ThrowOnPreviewImport { get; set; }

            public int ExportCsvCalls { get; private set; }

            public int ExportJsonCalls { get; private set; }

            public int SaveDiagnosticsCalls { get; private set; }

            public int CreateBackupCalls { get; private set; }

            public int AddCalls { get; private set; }

            public int PreviewImportCalls { get; private set; }

            public int CommitImportCalls { get; private set; }

            public int PreviewRestoreCalls { get; private set; }

            public int RestoreCalls { get; private set; }

            public int CreateEditorCalls { get; private set; }

            public int UpdateCalls { get; private set; }

            public int DeleteCalls { get; private set; }

            public int ReindexCalls { get; private set; }

            public int MutationCalls { get; private set; }

            public GameSession SelectedSession { get; set; } = new GameSession
            {
                Id = Guid.NewGuid(),
                GameId = Guid.NewGuid(),
                GameName = "Test Game"
            };

            public SessionImportPreview ImportPreview { get; set; } =
                new SessionImportPreview();

            public SessionRestorePreview RestorePreview { get; set; } =
                new SessionRestorePreview { IsValid = true };

            public int ExportCsv(string path)
            {
                ExportCsvCalls++;
                if (ThrowOnExportCsv)
                {
                    throw new IOException("Export failed.");
                }

                return 0;
            }

            public int ExportJson(string path)
            {
                ExportJsonCalls++;
                return 0;
            }

            public void SaveDiagnostics(string path)
            {
                SaveDiagnosticsCalls++;
            }

            public GameSession GetSelectedSession()
            {
                return SelectedSession;
            }

            public SessionEditorViewModel CreateEditor(
                GameSession existing = null)
            {
                CreateEditorCalls++;
                return new SessionEditorViewModel(
                    Enumerable.Empty<Playnite.SDK.Models.Game>(),
                    existing);
            }

            public bool AddSession(GameSession session)
            {
                AddCalls++;
                MutationCalls++;
                return true;
            }

            public bool UpdateSelectedSession(GameSession session)
            {
                UpdateCalls++;
                MutationCalls++;
                return true;
            }

            public bool DeleteSelectedSession()
            {
                DeleteCalls++;
                MutationCalls++;
                return true;
            }

            public SessionImportPreview PreviewImport(
                IEnumerable<string> paths)
            {
                PreviewImportCalls++;
                if (ThrowOnPreviewImport)
                {
                    throw new InvalidOperationException("Import preview failed.");
                }

                return ImportPreview;
            }

            public SessionImportCommitResult CommitImport(
                SessionImportPreview preview)
            {
                CommitImportCalls++;
                MutationCalls++;
                return new SessionImportCommitResult();
            }

            public string CreateBackup(string path)
            {
                CreateBackupCalls++;
                return path;
            }

            public SessionRestorePreview PreviewRestore(string path)
            {
                PreviewRestoreCalls++;
                return RestorePreview;
            }

            public SessionRestoreResult RestoreBackup(string path)
            {
                RestoreCalls++;
                MutationCalls++;
                return new SessionRestoreResult();
            }

            public SessionReindexResult Reindex()
            {
                ReindexCalls++;
                MutationCalls++;
                return new SessionReindexResult();
            }
        }

        private sealed class FakeSessionManagementInteraction :
            ISessionManagementInteraction
        {
            public IReadOnlyList<string> ImportFiles { get; set; } =
                new string[0];

            public string ExportPath { get; set; }

            public string BackupPath { get; set; }

            public string RestorePath { get; set; }

            public string DiagnosticsPath { get; set; }

            public bool ConfirmDeleteResult { get; set; }

            public bool ConfirmRestoreResult { get; set; }

            public bool ConfirmReindexResult { get; set; }

            public bool ConfirmImportResult { get; set; }

            public GameSession EditorResult { get; set; }

            public int ConfirmDeleteCalls { get; private set; }

            public int ConfirmRestoreCalls { get; private set; }

            public int ConfirmReindexCalls { get; private set; }

            public int ConfirmImportCalls { get; private set; }

            public int EditSessionCalls { get; private set; }

            public int ErrorCount { get; private set; }

            public IReadOnlyList<string> SelectImportFiles()
            {
                return ImportFiles;
            }

            public string SelectExportPath(string extension)
            {
                return ExportPath;
            }

            public string SelectBackupPath()
            {
                return BackupPath;
            }

            public string SelectRestorePath()
            {
                return RestorePath;
            }

            public string SelectDiagnosticsPath()
            {
                return DiagnosticsPath;
            }

            public bool ConfirmDelete(string gameName)
            {
                ConfirmDeleteCalls++;
                return ConfirmDeleteResult;
            }

            public bool ConfirmRestore(SessionRestorePreview preview)
            {
                ConfirmRestoreCalls++;
                return ConfirmRestoreResult;
            }

            public bool ConfirmReindex()
            {
                ConfirmReindexCalls++;
                return ConfirmReindexResult;
            }

            public bool ConfirmImport(SessionImportPreview preview)
            {
                ConfirmImportCalls++;
                return ConfirmImportResult;
            }

            public GameSession EditSession(SessionEditorViewModel editor)
            {
                EditSessionCalls++;
                return EditorResult;
            }

            public void ShowError(string title, Exception exception)
            {
                ErrorCount++;
            }
        }

        private sealed class TestLogger : ILogger
        {
            public void Info(string message) { }
            public void Info(Exception exception, string message) { }
            public void Debug(string message) { }
            public void Debug(Exception exception, string message) { }
            public void Warn(string message) { }
            public void Warn(Exception exception, string message) { }
            public void Error(string message) { }
            public void Error(Exception exception, string message) { }
            public void Trace(string message) { }
            public void Trace(Exception exception, string message) { }
        }

        private sealed class TestSessionSerializer : ISessionSerializer
        {
            public string Serialize(SessionStoreDocument document)
            {
                return JsonConvert.SerializeObject(document, Formatting.Indented);
            }

            public bool TryDeserialize(
                string path,
                out SessionStoreDocument document,
                out Exception error)
            {
                try
                {
                    document = JsonConvert.DeserializeObject<SessionStoreDocument>(
                        File.ReadAllText(path));
                    error = null;
                    return document != null;
                }
                catch (Exception ex)
                {
                    document = null;
                    error = ex;
                    return false;
                }
            }
        }

        private sealed class TestExportJsonSerializer : ISessionExportJsonSerializer
        {
            public string Serialize(SessionExportDocument document)
            {
                return JsonConvert.SerializeObject(document, Formatting.Indented);
            }
        }

        private sealed class TestImportJsonSerializer : ISessionImportJsonSerializer
        {
            public bool TryDeserializeExport(
                string json,
                out SessionExportDocument document,
                out Exception error)
            {
                return TryDeserialize(json, out document, out error);
            }

            public bool TryDeserializeStore(
                string json,
                out SessionStoreDocument document,
                out Exception error)
            {
                return TryDeserialize(json, out document, out error);
            }

            public bool TryDeserializeGameActivity(
                string json,
                out GameActivityImportDocument document,
                out Exception error)
            {
                return TryDeserialize(json, out document, out error);
            }

            private static bool TryDeserialize<T>(
                string json,
                out T value,
                out Exception error)
            {
                try
                {
                    value = JsonConvert.DeserializeObject<T>(json);
                    error = null;
                    return value != null;
                }
                catch (Exception ex)
                {
                    value = default(T);
                    error = ex;
                    return false;
                }
            }
        }

        private sealed class TestGameMetadataAccessor : IGameMetadataAccessor
        {
            private readonly Dictionary<
                Guid,
                Dictionary<MetadataFilterDimension, IList<string>>> values =
                new Dictionary<
                    Guid,
                    Dictionary<MetadataFilterDimension, IList<string>>>();

            public void Add(
                Guid gameId,
                MetadataFilterDimension dimension,
                params string[] metadataValues)
            {
                Dictionary<MetadataFilterDimension, IList<string>> byDimension;
                if (!values.TryGetValue(gameId, out byDimension))
                {
                    byDimension =
                        new Dictionary<MetadataFilterDimension, IList<string>>();
                    values[gameId] = byDimension;
                }

                byDimension[dimension] = metadataValues.ToList();
            }

            public IEnumerable<string> GetValues(
                Playnite.SDK.Models.Game game,
                MetadataFilterDimension dimension,
                IReadOnlyDictionary<Guid, string> libraryNames)
            {
                Dictionary<MetadataFilterDimension, IList<string>> byDimension;
                IList<string> result;
                return game != null &&
                    values.TryGetValue(game.Id, out byDimension) &&
                    byDimension.TryGetValue(dimension, out result)
                    ? result
                    : Enumerable.Empty<string>();
            }

            public IEnumerable<string> GetAllSearchableValues(
                Playnite.SDK.Models.Game game,
                IReadOnlyDictionary<Guid, string> libraryNames)
            {
                Dictionary<MetadataFilterDimension, IList<string>> byDimension;
                return game != null && values.TryGetValue(game.Id, out byDimension)
                    ? byDimension.Values.SelectMany(item => item)
                    : Enumerable.Empty<string>();
            }
        }
    }
}
