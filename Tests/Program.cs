using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using PlaytimeInsights.Converters;
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
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace PlaytimeInsights.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("Drilldown header gap stays compact", TestDrilldownHeaderGapIsCompact);
            Run("Playnite-style minute rounding", TestMinuteRounding);
            Run("Precise short duration display", TestPreciseDuration);
            Run("Duration display separates values units and automation text", TestDurationDisplayProjection);
            Run("Cross-midnight allocation", TestCrossMidnightAllocation);
            Run("Allocation preserves total seconds", TestAllocationPreservesTotal);
            Run("Cross-hour allocation preserves total and hour buckets", TestHourlyAllocation);
            Run("Hourly allocation reuses destination buffers without residue",
                TestHourlyAllocationDestinationReuse);
            Run("Advanced analytics processes sessions in one loop",
                TestAdvancedAnalyticsSingleLoop);
            Run("Daily allocation reuses destination buffers without residue",
                TestDailyAllocationDestinationReuse);
            Run("Session timezone resolver caches valid and fallback zones",
                TestSessionTimeZoneResolverCache);
            Run("Advanced weekday hour distributions and matrix", TestAdvancedDistributions);
            Run("Weekday selection filters the hourly distribution", TestWeekdayHourSelection);
            Run("Advanced streak finds longest consecutive run", TestAdvancedStreak);
            Run("Previous-period and year-over-year comparisons", TestAdvancedComparisons);
            Run("Comparison totals accumulate overlapping ranges once",
                TestComparisonTotalsAccumulateOnce);
            Run("All-sessions snapshot suppresses unstable comparisons", TestAllSessionsComparisonVisibility);
            Run("Finite ranges keep period comparisons visible", TestFiniteRangeComparisonVisibility);
            Run("Year-over-year range handles leap day", TestYearOverYearLeapDay);
            Run("Anomaly hints flag suspicious sessions without mutation", TestAnomalyHints);
            Run("Analytics performance summary reports median maximum and GC deltas",
                TestAnalyticsPerformanceSampleSummary);
            Run("Ten-year 100k-session analytics stays within release budget", TestLargeTenYearAnalytics);
            Run("Schema 4 store loads 100k sessions within release budget", TestLargeStoreLoad);
            Run("Interrupted session recovery", TestInterruptedSessionRecovery);
            Run("Completed session deduplication", TestCompletedSessionDeduplication);
            Run("Corrupt primary recovers from backup", TestCorruptPrimaryRecoversFromBackup);
            Run("ISO week range boundary", TestIsoWeekRangeBoundary);
            Run("Custom range normalizes reversed dates", TestCustomRangeNormalizesReversedDates);
            Run("Relative dashboard ranges use inclusive local dates", TestRelativeDashboardRanges);
            Run("All-sessions range uses a supplied earliest local date", TestAllSessionsDateRange);
            Run("All-sessions automatic aggregation follows actual span", TestAllSessionsAggregation);
            Run("All-sessions snapshot starts at earliest valid filtered local date", TestAllSessionsSnapshotStart);
            Run("Weekly aggregation and range metrics", TestWeeklyAggregationAndRangeMetrics);
            Run("Range clips cross-midnight duration", TestRangeClipsCrossMidnightDuration);
            Run("Range ranking supports session count", TestRangeRankingBySessionCount);
            Run("Range rankings expose share and latest activity", TestRangeRankingAuxiliaryText);
            Run("Ranking details omit the active sort metric", TestRankingDetailDeduplication);
            Run("Ranking secondary text uses readable hierarchy", TestRankingSecondaryTextHierarchy);
            Run("Ranking row tooltip consolidates secondary statistics", TestRankingTooltipComposition);
            Run("Ranking lists mark one and two rows as sparse", TestRankingSparseDensity);
            Run("Ranking share wash keeps the historical full-row contract", TestRankingShareWashContract);
            Run("Lifetime rankings convert Playnite activity to local time", TestLifetimeRankingActivityTimeZone);
            Run("Dashboard snapshot shares one ranking timestamp", TestDashboardSnapshotUsesOneTimestamp);
            Run("Heatmap aligns ISO week and preserves calendar layout", TestHeatmapLayoutAndIntensity);
            Run("Heatmap uses absolute duration levels", TestHeatmapAbsoluteDurationLevels);
            Run("Heatmap month axis follows calendar-week columns", TestHeatmapMonthAxisProjection);
            Run("Heatmap supports six-calendar-week months", TestHeatmapSixWeekMonth);
            Run("Heatmap week columns reuse the row-major cell models",
                TestHeatmapWeekGrouping);
            Run("Heatmap month axis panel measures and arranges spans", TestHeatmapMonthAxisPanel);
            Run("Calendar heatmap keeps aligned visual contracts", TestCalendarHeatmapVisualContract);
            Run("Calendar heatmap maps levels to the real swatches", TestCalendarHeatmapRuntimeMapping);
            Run("Calendar heatmap uses lightweight accessible cell buttons",
                TestHeatmapCellButtonContract);
            Run("Calendar heatmap records one-year and all-session layout cost", TestCalendarHeatmapLayoutCost);
            Run("Heatmap layout summary reports median maximum and realization",
                TestHeatmapLayoutSampleSummary);
            Run("Trend points scale to period maximum", TestTrendPointScaling);
            Run("Period drilldown bounds clip to range", TestPeriodBoundsClipToRange);
            Run("Session drilldown clips duration and labels recovery", TestSessionDrilldown);
            Run("Session detail pager loads fixed-size batches", TestSessionDetailPager);
            Run("Dashboard clear command resets drilldown selection", TestClearDrilldownSelectionCommand);
            Run("Dashboard drilldown cards retain recycling virtualization", TestDrilldownVirtualizationContract);
            Run("Dashboard drilldown source tags use theme text", TestDrilldownSourceTagForeground);
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
            Run("Plugin visual resources load through explicit view merges", TestExplicitVisualResourceMerges);
            Run("Responsive metric panel selects expected columns", TestResponsiveMetricPanelColumns);
            Run("Responsive metric panel centers and equalizes rows", TestResponsiveMetricPanelArrangement);
            Run("Responsive metric panel contains invalid inputs", TestResponsiveMetricPanelEdgeCases);
            Run("Responsive metric panel remeasures for arrange width", TestResponsiveMetricPanelRemeasuresForArrangeWidth);
            Run("Dashboard metrics use responsive semantic visual foundation", TestResponsiveMetricVisualFoundation);
            Run("Duration comparison pills stack without horizontal clipping", TestDurationComparisonPillsStackVertically);
            Run("Dashboard metric additions expose behavior", TestDashboardMetricAdditionsBehavior);
            Run("Advanced filter toggle keeps real interaction contract", TestAdvancedFilterToggleInteraction);
            Run("Dashboard list hover overlays keep microinteraction contracts", TestDashboardListHoverContracts);
            Run("Dashboard entrance plan maps transitions and reduced motion", TestDashboardEntrancePlanBehavior);
            Run("Dashboard refresh publishes one presentation signal", TestDashboardPresentationSignalContract);
            Run("Dashboard entrance hosts and scheduler keep animation contracts", TestDashboardEntranceHostContract);
            Run("Virtualized list items reset hover lift after unload", TestDashboardListHoverReset);
            Run("Entrance animation keeps final base values", TestEntranceAnimationKeepsFinalBaseValues);
            Run("HoverMotion resets recycled list items", TestHoverMotionRecyclesCleanly);
            Run("HoverMotion holds lift while hovered and releases after leave", TestHoverMotionHoldAndRelease);
            Run("Drilldown recycling keeps hover transform at zero", TestDrilldownRecyclingKeepsTransformZero);
            Run("Presentation refresh chain reaches host animations", TestPresentationRefreshChain);
            Run("Ranking tab mouse clicks stay inside the scroll position", TestRankingTabBringIntoViewSuppression);
            Run("Dashboard theme visual contracts stay explicit", TestDashboardThemeVisualContracts);
            Run("Dashboard recomposition keeps 4x2 metric grid and adaptive modules", TestDashboardVisualRefactorStaticContract);
            Run("Dashboard visual refactor keeps final architecture guards", TestDashboardVisualRefactorContract);
            Run("Anomaly module review title stays localized and unique", TestAnomalyModuleReviewTitleLocalization);
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
            Run("Trend chart resolves themed area, line and node resources", TestTrendChartThemeResourceContract);
            Run("Trend axis rounds the peak up to a labelled maximum", TestTrendAxisMaximumRounding);
            Run("Trend chart reserves a measured gutter and labels the gridlines", TestTrendAxisGutterAndLabels);
            Run("Dashboard filters route selective refresh reasons", TestDashboardFilterRefreshReasons);
            Run("Dashboard refresh plans isolate dependencies", TestDashboardRefreshPlans);
            Run("Quick range selection emits at most one range refresh", TestQuickRangeRefreshPurity);
            Run("Quick range command tracks valid options and refresh state", TestSelectRangeCommandBehavior);
            Run("Metadata filter summary counts active constraints", TestActiveMetadataFilterSummary);
            Run("Dashboard ranking tabs are view-only and keep both snapshots", TestRankingTabsStayViewOnly);
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
            Run("Sidebar navigation reuses Dashboard View", TestSidebarNavigationReusesDashboardView);
            Run("Dashboard reentry preserves visual tree", TestDashboardReentryPreservesVisualTree);
            Run("Dashboard cache keeps one Loaded refresh boundary", TestDashboardViewCacheRefreshBoundary);
            Run("Dashboard View reattaches and Loaded fires again", TestDashboardViewLoadedReattaches);
            Run("Cover cache reuses normalized path", TestCoverCacheReusesNormalizedPath);
            Run("Cover cache invalidates changed and missing files", TestCoverCacheInvalidatesFiles);
            Run("Cover cache separates widths and evicts LRU", TestCoverCacheWidthsAndLru);
            Run("Cover decoder returns frozen thumbnail", TestCoverDecoderReturnsFrozenThumbnail);
            Run("Adaptive dashboard panel uses source order in narrow mode", TestAdaptiveDashboardPanelNarrow);
            Run("Adaptive dashboard panel stacks columns independently", TestAdaptiveDashboardPanelWide);
            Run("Adaptive dashboard panel applies 1200 and 1160 DIP hysteresis", TestAdaptiveDashboardPanelHysteresis);
            Run("Visible drilldown module remeasures its session list", TestVisibleDrilldownModuleRemeasures);
            Run("Bound dashboard drilldown expands after selection", TestBoundDashboardDrilldownExpands);
            Run("Dashboard drilldown anchors to its triggering visualization", TestDashboardDrilldownAnchors);
            Run("Dashboard drilldown hosts preserve source adjacency", TestDashboardDrilldownHostLayout);
            Run("Dashboard drilldown reveal uses header viewport bounds", TestDashboardDrilldownViewportBounds);
            Run("Dashboard drilldown reveal scrolls only for a hidden header", TestDashboardDrilldownViewportReveal);
            Run("Dashboard drilldown exposes the active context to automation", TestDashboardDrilldownAutomationName);

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

        private static void TestDurationDisplayProjection()
        {
            var shortValue = AnalyticsService.CreateDurationDisplay(91);
            Equal("1", shortValue.MajorValue);
            Equal("31", shortValue.MinorValue);
            Equal("1 分 31 秒", shortValue.AutomationText);

            var exactHour = AnalyticsService.CreateDurationDisplay(3600);
            Equal("1", exactHour.MajorValue);
            Equal(string.Empty, exactHour.MinorValue);
            Equal("1 小时", exactHour.AutomationText);

            var mixed = AnalyticsService.CreateDurationDisplay(45300);
            Equal("12", mixed.MajorValue);
            Equal("35", mixed.MinorValue);
            Equal("12 小时 35 分", mixed.AutomationText);
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

        private static void TestSessionTimeZoneResolverCache()
        {
            var resolver = new SessionTimeZoneResolver();
            var first = CreateSession("Zone One", 60, 0);
            var second = CreateSession("Zone Two", 120, 60);
            var firstZone = resolver.Resolve(first);
            var secondZone = resolver.Resolve(second);
            Equal(true, ReferenceEquals(firstZone, secondZone));

            var fallbackOne = CreateSession("Fallback One", 60, 0);
            fallbackOne.StartUtcOffsetMinutes = 330;
            fallbackOne.TimeZoneId = "PlaytimeInsights.Missing.Time Zone";
            var fallbackTwo = CreateSession("Fallback Two", 60, 0);
            fallbackTwo.StartUtcOffsetMinutes = 330;
            fallbackTwo.TimeZoneId = "PlaytimeInsights.Missing.Time Zone";
            var fallbackZoneOne = resolver.Resolve(fallbackOne);
            var fallbackZoneTwo = resolver.Resolve(fallbackTwo);
            Equal(true, ReferenceEquals(fallbackZoneOne, fallbackZoneTwo));
            Equal(TimeSpan.FromMinutes(330), fallbackZoneOne.BaseUtcOffset);
        }

        private static void TestDailyAllocationDestinationReuse()
        {
            var service = new DailyAllocationService();
            var normal = CreateSession(
                Guid.NewGuid(),
                "Normal",
                new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                300);
            var crossMidnight = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 60,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };
            var daylight = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                ElapsedSeconds = 7200,
                StartUtcOffsetMinutes = 420,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "Pacific Standard Time"
            };
            var invalid = CreateSession(
                Guid.NewGuid(),
                "Invalid",
                new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                0);

            var destination = new List<DailyAllocation>();
            AssertDestinationMatchesDictionary(service, normal, destination);
            AssertDestinationMatchesDictionary(service, crossMidnight, destination);
            AssertDestinationMatchesDictionary(service, daylight, destination);
            AssertDestinationMatchesDictionary(service, invalid, destination);

            service.SplitByLocalDay(crossMidnight, destination);
            Equal(2, destination.Count);
            service.SplitByLocalDay(normal, destination);
            Equal(1, destination.Count);
            service.SplitByLocalDay(null, destination);
            Equal(0, destination.Count);
            Equal(true, typeof(DailyAllocation).IsValueType);
        }

        private static void AssertDestinationMatchesDictionary(
            DailyAllocationService service,
            GameSession session,
            List<DailyAllocation> destination)
        {
            var expected = service.SplitByLocalDay(session);
            service.SplitByLocalDay(session, destination);
            Equal(expected.Count, destination.Count);
            ulong destinationTotal = 0;
            foreach (var allocation in destination)
            {
                Equal(true, expected.ContainsKey(allocation.LocalDate));
                Equal(expected[allocation.LocalDate], allocation.Seconds);
                destinationTotal += allocation.Seconds;
            }

            Equal(session.ElapsedSeconds, destinationTotal);
        }

        private static void TestHourlyAllocationDestinationReuse()
        {
            var service = new HourlyAllocationService();
            var normal = CreateSession(
                Guid.NewGuid(),
                "Normal",
                new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc),
                300);
            var crossHour = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 61,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 480,
                TimeZoneId = "China Standard Time"
            };
            var crossMidnight = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 7, 27, 15, 59, 30, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 7, 27, 16, 0, 30, DateTimeKind.Utc),
                ElapsedSeconds = 60,
                StartUtcOffsetMinutes = 540,
                EndUtcOffsetMinutes = 540,
                TimeZoneId = "Tokyo Standard Time"
            };
            var daylight = new GameSession
            {
                StartedAtUtc = new DateTime(2026, 3, 8, 9, 30, 0, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2026, 3, 8, 11, 30, 0, DateTimeKind.Utc),
                ElapsedSeconds = 7200,
                StartUtcOffsetMinutes = 480,
                EndUtcOffsetMinutes = 420,
                TimeZoneId = "Pacific Standard Time"
            };

            var destination = new List<HourlyAllocation>();
            AssertHourlyDestinationMatchesList(service, normal, destination);
            AssertHourlyDestinationMatchesList(service, crossHour, destination);
            AssertHourlyDestinationMatchesList(service, crossMidnight, destination);
            AssertHourlyDestinationMatchesList(service, daylight, destination);

            service.SplitByLocalHour(crossHour, destination);
            Equal(2, destination.Count);
            service.SplitByLocalHour(normal, destination);
            Equal(1, destination.Count);
            service.SplitByLocalHour(null, destination);
            Equal(0, destination.Count);
            Equal(true, typeof(HourlyAllocation).IsValueType);
        }

        private static void AssertHourlyDestinationMatchesList(
            HourlyAllocationService service,
            GameSession session,
            List<HourlyAllocation> destination)
        {
            var expected = service.SplitByLocalHour(session);
            service.SplitByLocalHour(session, destination);
            Equal(expected.Count, destination.Count);
            ulong destinationTotal = 0;
            for (var index = 0; index < expected.Count; index++)
            {
                Equal(expected[index].LocalDate, destination[index].LocalDate);
                Equal(expected[index].Hour, destination[index].Hour);
                Equal(expected[index].Seconds, destination[index].Seconds);
                destinationTotal += destination[index].Seconds;
            }

            Equal(session.ElapsedSeconds, destinationTotal);
        }

        private static void TestAdvancedAnalyticsSingleLoop()
        {
            var source = File.ReadAllText(Path.Combine(
                FindSourceRoot(),
                "Services",
                "AdvancedAnalyticsService.cs"));
            Equal(1, CountOccurrences(source, "foreach (var session in"));
            Equal(false, source.Contains("CreateAnomalies(gameList, sessionList, range)"));
            Equal(true, source.Contains("CreateAnomalyCandidate("));

            var gameId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var normal = CreateSession(
                gameId, "Normal", now.AddHours(-2), 600);
            var zero = CreateSession(
                gameId, "Zero", now.AddHours(-1), 0);
            var endBeforeStart = CreateSession(
                gameId, "EndBeforeStart", now.AddMinutes(-30), 60);
            endBeforeStart.EndedAtUtc = endBeforeStart.StartedAtUtc.AddMinutes(-1);
            var wallMismatch = CreateSession(
                gameId, "WallMismatch", now.AddHours(-2), 7200);
            wallMismatch.EndedAtUtc = wallMismatch.StartedAtUtc.AddHours(1);
            var future = CreateSession(
                gameId, "Future", now.AddHours(2), 60);
            var longSession = CreateSession(
                gameId, "Long", now.AddDays(-1), 19UL * 3600UL);
            var sessions = new[]
            {
                normal,
                zero,
                endBeforeStart,
                wallMismatch,
                future,
                longSession
            };
            var snapshots = sessions
                .Select(session => new
                {
                    session.StartedAtUtc,
                    session.EndedAtUtc,
                    session.ElapsedSeconds
                })
                .ToList();

            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                sessions,
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = DateTime.Today.AddDays(-2),
                    CustomEndDate = DateTime.Today.AddDays(3)
                });

            Equal(5, snapshot.Advanced.Anomalies.Count);
            Equal(
                "Future|EndBeforeStart|Zero|WallMismatch|Long",
                string.Join(
                    "|",
                    snapshot.Advanced.Anomalies.Select(item => item.GameName)));
            Equal(
                true,
                snapshot.Advanced.Anomalies[0].Reason.Contains("未来"));
            Equal(
                true,
                snapshot.Advanced.Anomalies[1].Reason.Contains("结束早于开始"));
            Equal(
                true,
                snapshot.Advanced.Anomalies[2].Reason.Contains("零秒会话"));
            Equal(
                true,
                snapshot.Advanced.Anomalies[3].Reason.Contains("墙钟"));
            Equal(
                true,
                snapshot.Advanced.Anomalies[4].Reason.Contains("18 小时"));
            Equal(
                76320UL,
                snapshot.Advanced.HourDistribution.Aggregate<
                    DistributionBarViewModel,
                    ulong>(
                    0,
                    (total, item) => total + item.Seconds));
            foreach (var session in sessions)
            {
                var original = snapshots.First(item =>
                    item.StartedAtUtc == session.StartedAtUtc &&
                    item.ElapsedSeconds == session.ElapsedSeconds);
                Equal(original.EndedAtUtc, session.EndedAtUtc);
                Equal(original.ElapsedSeconds, session.ElapsedSeconds);
            }

            var topFifty = Enumerable.Range(0, 55)
                .Select(index => CreateSession(
                    gameId,
                    "F" + index,
                    now.AddMinutes(10 + index),
                    60))
                .ToList();
            var topSnapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                topFifty,
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = DateTime.Today.AddDays(-2),
                    CustomEndDate = DateTime.Today.AddDays(3)
                });
            Equal(50, topSnapshot.Advanced.Anomalies.Count);
            Equal("F54", topSnapshot.Advanced.Anomalies[0].GameName);
            Equal("F5", topSnapshot.Advanced.Anomalies[49].GameName);
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
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

        private static void TestComparisonTotalsAccumulateOnce()
        {
            var query = new AnalyticsQuery
            {
                RangePreset = DateRangePreset.Custom,
                CustomStartDate = new DateTime(2025, 1, 1),
                CustomEndDate = new DateTime(2026, 6, 1)
            };
            var sessions = new[]
            {
                CreateSession(
                    Guid.NewGuid(),
                    "Overlap CurrentAndYoy",
                    new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                    600),
                CreateSession(
                    Guid.NewGuid(),
                    "CurrentOnly",
                    new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                    300),
                CreateSession(
                    Guid.NewGuid(),
                    "PreviousAndYoyLeapDay",
                    new DateTime(2024, 2, 29, 10, 0, 0, DateTimeKind.Utc),
                    200),
                CreateSession(
                    Guid.NewGuid(),
                    "CrossMidnightBetweenRanges",
                    new DateTime(2024, 12, 31, 15, 59, 30, DateTimeKind.Utc),
                    60)
            };

            var result = new AnalyticsService().CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                sessions,
                query);
            var totals = result.Context.ComparisonTotals;

            Equal(true, totals.Enabled);
            Equal(new DateTime(2023, 8, 3), totals.PreviousRange.StartDate);
            Equal(new DateTime(2024, 12, 31), totals.PreviousRange.EndDate);
            Equal(new DateTime(2024, 1, 1), totals.YearOverYearRange.StartDate);
            Equal(new DateTime(2025, 6, 1), totals.YearOverYearRange.EndDate);
            Equal(230UL, totals.PreviousSeconds);
            Equal(860UL, totals.YearOverYearSeconds);
            Equal(Visibility.Visible, result.Snapshot.Advanced.ComparisonVisibility);

            var allSessionsResult = new AnalyticsService().CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                sessions,
                new AnalyticsQuery { RangePreset = DateRangePreset.AllSessions });
            var allSessionsTotals = allSessionsResult.Context.ComparisonTotals;
            Equal(false, allSessionsTotals.Enabled);
            Equal(null, allSessionsTotals.PreviousRange);
            Equal(null, allSessionsTotals.YearOverYearRange);
            Equal(0UL, allSessionsTotals.PreviousSeconds);
            Equal(0UL, allSessionsTotals.YearOverYearSeconds);
        }

        private static void TestAllSessionsComparisonVisibility()
        {
            var gameId = Guid.NewGuid();
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(
                        gameId,
                        "History",
                        DateTime.UtcNow.AddDays(-3),
                        120)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.AllSessions
                });

            Equal(Visibility.Collapsed, snapshot.Advanced.ComparisonVisibility);
            Equal(null, snapshot.Advanced.PreviousPeriodComparison);
            Equal(null, snapshot.Advanced.YearOverYearComparison);
        }

        private static void TestFiniteRangeComparisonVisibility()
        {
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Last7Days
                });

            Equal(Visibility.Visible, snapshot.Advanced.ComparisonVisibility);
            Equal(true, snapshot.Advanced.PreviousPeriodComparison != null);
            Equal(true, snapshot.Advanced.YearOverYearComparison != null);
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

        private static void TestAnalyticsPerformanceSampleSummary()
        {
            var summary = AnalyticsPerformanceSampleSummary.FromMilliseconds(
                new[] { 640d, 710d, 660d, 700d, 650d },
                3,
                1,
                0);

            Equal(660d, summary.MedianMilliseconds);
            Equal(710d, summary.MaxMilliseconds);
            Equal(3, summary.Gen0Collections);
            Equal(1, summary.Gen1Collections);
            Equal(0, summary.Gen2Collections);
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

            var query = new AnalyticsQuery
            {
                RangePreset = DateRangePreset.Custom,
                CustomStartDate = new DateTime(2016, 1, 1),
                CustomEndDate = new DateTime(2025, 12, 31),
                AggregationPeriod = AggregationPeriod.Auto,
                UseIsoWeekStart = true,
                TopGames = 20
            };

            DashboardSnapshot snapshot = null;
            var summary = MeasureAnalyticsSamples(
                () => snapshot = new AnalyticsService().CreateSnapshot(
                    games,
                    sessions,
                    query),
                1,
                5);

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
            Console.WriteLine(
                string.Format(
                    "       100k analytics samples: {0:N0} / {1:N0} / {2:N0} / {3:N0} / {4:N0} ms; median {5:N0} ms; max {6:N0} ms; GC 0/1/2 = {7}/{8}/{9}",
                    summary.SamplesMilliseconds[0],
                    summary.SamplesMilliseconds[1],
                    summary.SamplesMilliseconds[2],
                    summary.SamplesMilliseconds[3],
                    summary.SamplesMilliseconds[4],
                    summary.MedianMilliseconds,
                    summary.MaxMilliseconds,
                    summary.Gen0Collections,
                    summary.Gen1Collections,
                    summary.Gen2Collections));
            Equal(true, summary.MaxMilliseconds <= 750d);
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
                Console.WriteLine(
                    string.Format(
                        "       schema 4 JSON load / 100k sessions: {0:N0} ms",
                        stopwatch.ElapsedMilliseconds));
                Equal(true, stopwatch.Elapsed <= TimeSpan.FromMilliseconds(1400));
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

        private static void TestRelativeDashboardRanges()
        {
            var today = new DateTime(2026, 8, 14);
            var last7 = AnalyticsService.ResolveDateRange(
                new AnalyticsQuery { RangePreset = DateRangePreset.Last7Days },
                today);
            var last30 = AnalyticsService.ResolveDateRange(
                new AnalyticsQuery { RangePreset = DateRangePreset.Last30Days },
                today);

            Equal(new DateTime(2026, 8, 8), last7.StartDate);
            Equal(today, last7.EndDate);
            Equal(new DateTime(2026, 7, 16), last30.StartDate);
            Equal(today, last30.EndDate);
        }

        private static void TestAllSessionsDateRange()
        {
            var today = new DateTime(2026, 8, 14);
            var query = new AnalyticsQuery
            {
                RangePreset = DateRangePreset.AllSessions
            };

            var supplied = AnalyticsService.ResolveDateRange(
                query,
                today,
                new DateTime(2020, 2, 29));
            var empty = AnalyticsService.ResolveDateRange(query, today, null);
            var future = AnalyticsService.ResolveDateRange(
                query,
                today,
                today.AddDays(3));

            Equal(new DateTime(2020, 2, 29), supplied.StartDate);
            Equal(today, supplied.EndDate);
            Equal(today, empty.StartDate);
            Equal(today, future.StartDate);
        }

        private static void TestAllSessionsAggregation()
        {
            Equal(
                AggregationPeriod.Day,
                ResolveAggregation(DateRangePreset.AllSessions, 62));
            Equal(
                AggregationPeriod.Week,
                ResolveAggregation(DateRangePreset.AllSessions, 63));
            Equal(
                AggregationPeriod.Month,
                ResolveAggregation(DateRangePreset.AllSessions, 731));
            Equal(
                AggregationPeriod.Year,
                ResolveAggregation(DateRangePreset.AllSessions, 3651));
        }

        private static void TestAllSessionsSnapshotStart()
        {
            var gameId = Guid.NewGuid();
            var valid = CreateSession(
                gameId,
                "Valid",
                new DateTime(2020, 1, 1, 18, 30, 0, DateTimeKind.Utc),
                600);
            valid.StartUtcOffsetMinutes = 480;

            var deleted = CreateSession(
                gameId,
                "Deleted",
                new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                600);
            deleted.IsDeleted = true;

            var zero = CreateSession(
                gameId,
                "Zero",
                new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                0);

            var result = new AnalyticsService().CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new[] { deleted, zero, valid },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.AllSessions
                });
            var empty = new AnalyticsService().CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.AllSessions
                });
            var invalidOnly = new AnalyticsService().CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new[] { deleted, zero },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.AllSessions
                });

            Equal(new DateTime(2020, 1, 2), result.Context.Range.StartDate);
            Equal(DateTime.Today, result.Context.Range.EndDate);
            Equal(DateTime.Today, empty.Context.Range.StartDate);
            Equal(DateTime.Today, empty.Context.Range.EndDate);
            Equal(DateTime.Today, invalidOnly.Context.Range.StartDate);
            Equal(DateTime.Today, invalidOnly.Context.Range.EndDate);
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

        private static void TestRangeRankingAuxiliaryText()
        {
            var firstGame = Guid.NewGuid();
            var secondGame = Guid.NewGuid();
            var now = new DateTime(2026, 8, 17, 12, 0, 0);
            var service = new AnalyticsService();
            var laterUtcSession = CreateSession(
                firstGame,
                "First",
                new DateTime(2026, 8, 17, 3, 0, 0, DateTimeKind.Utc),
                10);
            laterUtcSession.StartUtcOffsetMinutes = 0;
            laterUtcSession.EndUtcOffsetMinutes = 0;
            laterUtcSession.TimeZoneId = "UTC";
            var result = service.CreateSnapshotWithContext(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(
                        firstGame,
                        "First",
                        new DateTime(2026, 8, 17, 2, 0, 0, DateTimeKind.Utc),
                        630),
                    CreateSession(
                        secondGame,
                        "Second",
                        new DateTime(2026, 8, 17, 1, 0, 0, DateTimeKind.Utc),
                        370),
                    laterUtcSession
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 8, 17),
                    CustomEndDate = new DateTime(2026, 8, 17),
                    RankingMetric = RankingMetric.Duration
                });

            var firstStats = result.Context.GameStatistics.Single(item =>
                item.GameId == firstGame);
            Equal(
                new DateTime(2026, 8, 17, 3, 0, 0),
                firstStats.LastSessionLocal.Value);

            var projection = InvokeRangeRankingProjection(
                service,
                result.Context,
                RankingMetric.Duration,
                10,
                now);
            var first = projection.RangeGameRankings[0];
            Equal("First", first.Name);
            Equal("占本期总时长 63%", first.ShareText);
            Equal("今天 03:00", first.LastPlayedText);

            Equal(
                "无最近游玩记录",
                RecentActivityFormatter.Format(null, now));
            Equal(
                "今天 09:15",
                RecentActivityFormatter.Format(
                    new DateTime(2026, 8, 17, 9, 15, 0),
                    now));
            Equal(
                "昨天 22:49",
                RecentActivityFormatter.Format(
                    new DateTime(2026, 8, 16, 22, 49, 0),
                    now));
            var older = new DateTime(2026, 8, 10, 8, 30, 0);
            Equal(
                older.ToString("g", CultureInfo.CurrentCulture),
                RecentActivityFormatter.Format(older, now));
        }

        private static void TestRankingDetailDeduplication()
        {
            var service = new AnalyticsService();
            var context = new DashboardAnalysisContext
            {
                GameStatistics = new[]
                {
                    new DashboardGameRangeStatistics
                    {
                        GameId = Guid.NewGuid(),
                        Name = "Detail",
                        Seconds = 600,
                        SessionCount = 3,
                        ActiveDates = new List<DateTime>
                        {
                            new DateTime(2026, 8, 16),
                            new DateTime(2026, 8, 17)
                        },
                        LongestSessionSeconds = 400,
                        LastSessionLocal = new DateTime(
                            2026,
                            8,
                            17,
                            10,
                            0,
                            0)
                    }
                }
            };
            var expected = new Dictionary<RankingMetric, string>
            {
                { RankingMetric.Duration, "3 次 · 2 个活跃日" },
                { RankingMetric.SessionCount, "2 个活跃日" },
                { RankingMetric.ActiveDays, "3 次" },
                { RankingMetric.AverageSession, "3 次 · 2 个活跃日" },
                { RankingMetric.LongestSession, "3 次 · 2 个活跃日" }
            };

            foreach (var pair in expected)
            {
                var projection = InvokeRangeRankingProjection(
                    service,
                    context,
                    pair.Key,
                    10,
                    new DateTime(2026, 8, 17, 12, 0, 0));
                Equal(pair.Value, projection.RangeGameRankings[0].DetailText);
            }
        }

        private static void TestRankingSecondaryTextHierarchy()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                var template = view.TryFindResource(
                    "GameRankingItemTemplate") as DataTemplate;
                Equal(true, template != null);
                var model = new GameRankingViewModel
                {
                    Name = "Readable",
                    DetailText = "3 次 · 2 个活跃日",
                    ShareText = "占本期总时长 63%",
                    LastPlayedText = "今天 03:00",
                    PrimaryValueText = "10 分钟",
                    ProgressPercent = 63
                };
                var presenter = new ContentPresenter
                {
                    Content = model,
                    ContentTemplate = template
                };
                var window = new Window
                {
                    Content = presenter,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 600,
                    Height = 160
                };
                try
                {
                    window.Show();
                    PumpDispatcher();
                    presenter.UpdateLayout();
                    var detail = FindVisualDescendants<TextBlock>(presenter)
                        .Single(text => text.Text == model.DetailText);
                    var lastPlayed = FindVisualDescendants<TextBlock>(presenter)
                        .Single(text => text.Text == model.LastPlayedText);
                    Equal(11d, detail.FontSize);
                    Equal(0.72d, detail.Opacity);
                    Equal(null, lastPlayed.ToolTip);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestRankingTooltipComposition()
        {
            RunOnSta(() =>
            {
                var service = new AnalyticsService();
                var projection = InvokeRangeRankingProjection(
                    service,
                    new DashboardAnalysisContext
                    {
                        GameStatistics = new[]
                        {
                            new DashboardGameRangeStatistics
                            {
                                GameId = Guid.NewGuid(),
                                Name = "Tooltip",
                                Seconds = 600,
                                SessionCount = 3,
                                ActiveDates = new List<DateTime>
                                {
                                    new DateTime(2026, 8, 16),
                                    new DateTime(2026, 8, 17)
                                },
                                LongestSessionSeconds = 400,
                                LastSessionLocal = new DateTime(
                                    2026,
                                    8,
                                    17,
                                    10,
                                    0,
                                    0)
                            }
                        }
                    },
                    RankingMetric.Duration,
                    10,
                    new DateTime(2026, 8, 17, 12, 0, 0));
                var model = projection.RangeGameRankings[0];
                var view = new PlaytimeInsightsDashboardView();
                var template = view.TryFindResource(
                    "GameRankingItemTemplate") as DataTemplate;
                Equal(true, template != null);
                var presenter = new ContentPresenter
                {
                    Content = model,
                    ContentTemplate = template
                };
                var window = new Window
                {
                    Content = presenter,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 600,
                    Height = 160
                };
                ToolTip tooltip = null;
                try
                {
                    window.Show();
                    PumpDispatcher();
                    presenter.UpdateLayout();
                    var row = FindVisualDescendants<Border>(presenter).First();
                    tooltip = row.ToolTip as ToolTip;
                    Equal(true, tooltip != null);
                    tooltip.PlacementTarget = row;
                    tooltip.IsOpen = true;
                    PumpDispatcher();
                    tooltip.UpdateLayout();

                    var texts = FindVisualDescendants<TextBlock>(tooltip)
                        .Select(text => text.Text)
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .ToList();
                    var metricRows = FindVisualDescendants<Grid>(tooltip)
                        .Where(grid =>
                            grid.Visibility == Visibility.Visible &&
                            grid.ColumnDefinitions.Count == 2)
                        .ToList();
                    Equal(2, metricRows.Count);
                    var expectedTexts = new[]
                    {
                        "占本期总时长 100%",
                        "平均会话",
                        "3 分 20 秒",
                        "最长会话",
                        "6 分 40 秒"
                    };
                    var missingTexts = expectedTexts
                        .Where(expected => !texts.Contains(expected))
                        .ToList();
                    if (missingTexts.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "Missing ranking tooltip text: " +
                            string.Join(", ", missingTexts) +
                            ". Actual: " + string.Join(" | ", texts));
                    }
                }
                finally
                {
                    if (tooltip != null)
                    {
                        tooltip.IsOpen = false;
                    }
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestRankingSparseDensity()
        {
            var service = new AnalyticsService();
            Func<int, DashboardRankingProjection> create = count =>
                InvokeRangeRankingProjection(
                    service,
                    new DashboardAnalysisContext
                    {
                        GameStatistics = Enumerable.Range(1, count)
                            .Select(index => new DashboardGameRangeStatistics
                            {
                                GameId = Guid.NewGuid(),
                                Name = "Game " + index,
                                Seconds = (ulong)(1000 - index),
                                SessionCount = 1,
                                ActiveDates = new List<DateTime>
                                {
                                    new DateTime(2026, 8, 17)
                                },
                                LastSessionLocal = new DateTime(
                                    2026,
                                    8,
                                    17,
                                    10,
                                    0,
                                    0)
                            })
                            .ToList()
                    },
                    RankingMetric.Duration,
                    10,
                    new DateTime(2026, 8, 17, 12, 0, 0));

            Equal(0, create(0).RangeGameRankings.Count);
            Equal(true, create(1).RangeGameRankings.All(item =>
                item.IsSparseLayout));
            Equal(true, create(2).RangeGameRankings.All(item =>
                item.IsSparseLayout));
            Equal(true, create(3).RangeGameRankings.All(item =>
                !item.IsSparseLayout));
        }

        private static void TestRankingShareWashContract()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                var style = view.TryFindResource(
                    "RankingEnergyBackgroundBarStyle") as Style;
                Equal(true, style != null);

                var progress = new ProgressBar
                {
                    Style = style,
                    Width = 200,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 63
                };
                progress.Measure(new Size(200, 80));
                progress.Arrange(new Rect(0, 0, 200, 80));
                progress.ApplyTemplate();

                var track = progress.Template.FindName(
                    "PART_Track",
                    progress) as Border;
                var indicator = progress.Template.FindName(
                    "PART_Indicator",
                    progress) as System.Windows.Shapes.Rectangle;
                Equal(true, track != null);
                Equal(true, indicator != null);
                Equal(new CornerRadius(6), track.CornerRadius);
                Equal(
                    Colors.Transparent,
                    ((SolidColorBrush)track.Background).Color);
                var fill = indicator.Fill as SolidColorBrush;
                Equal(true, fill != null);
                Equal(
                    Color.FromRgb(0x4A, 0x90, 0xE2),
                    fill.Color);
                Equal(0.10d, indicator.Opacity);

                var template = view.TryFindResource(
                    "GameRankingItemTemplate") as DataTemplate;
                Equal(true, template != null);
                var normalModel = new GameRankingViewModel
                {
                    Name = "Normal",
                    ShareText = "share",
                    LastPlayedText = "recent",
                    IsSparseLayout = false
                };
                var sparseModel = new GameRankingViewModel
                {
                    Name = "Sparse",
                    ShareText = "share",
                    LastPlayedText = "recent",
                    IsSparseLayout = true,
                    Position = 1
                };
                var normalPresenter = new ContentPresenter
                {
                    Content = normalModel,
                    ContentTemplate = template
                };
                var sparsePresenter = new ContentPresenter
                {
                    Content = sparseModel,
                    ContentTemplate = template
                };
                var host = new StackPanel();
                host.Children.Add(normalPresenter);
                host.Children.Add(sparsePresenter);
                var window = new Window
                {
                    Content = host,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 600,
                    Height = 240
                };
                try
                {
                    window.Show();
                    PumpDispatcher();
                    host.UpdateLayout();
                    var normal = FindVisualDescendants<Border>(
                        normalPresenter).First();
                    var sparse = FindVisualDescendants<Border>(
                        sparsePresenter).First();
                    Equal(64d, normal.Height);
                    Equal(80d, sparse.Height);
                    Equal(new Thickness(12), sparse.Padding);
                    var sparseGlow = sparse.Background as LinearGradientBrush;
                    Equal(true, sparseGlow != null);
                    Equal(
                        Color.FromArgb(0x1A, 0xFF, 0xD7, 0x00),
                        sparseGlow.GradientStops[0].Color);
                    var energyBar = FindVisualDescendants<ProgressBar>(sparse)
                        .Single();
                    Equal(true, double.IsNaN(energyBar.Height));
                    Equal(true, energyBar.ActualHeight > 4d);
                    Equal(
                        VerticalAlignment.Stretch,
                        energyBar.VerticalAlignment);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestLifetimeRankingActivityTimeZone()
        {
            var utcReference = new DateTime(
                2026,
                8,
                17,
                0,
                0,
                0,
                DateTimeKind.Utc);
            var offset = TimeZoneInfo.Local.GetUtcOffset(utcReference);
            var storedUtc = utcReference.Subtract(
                TimeSpan.FromTicks(offset.Ticks / 2));
            var persistedValue = DateTime.SpecifyKind(
                storedUtc,
                DateTimeKind.Unspecified);
            var expectedLocal = DateTime.SpecifyKind(
                persistedValue,
                DateTimeKind.Utc).ToLocalTime();
            var now = expectedLocal.Date.AddHours(12);

            var rankings = InvokeLifetimeRankings(
                new[]
                {
                    new Playnite.SDK.Models.Game
                    {
                        Id = Guid.NewGuid(),
                        Name = "Lifetime",
                        Playtime = 3600,
                        LastActivity = persistedValue
                    }
                },
                10,
                now);

            Equal(1, rankings.Count);
            Equal(
                "今天 " + expectedLocal.ToString("HH:mm"),
                rankings[0].LastPlayedText);
            Equal(true, rankings[0].IsSparseLayout);
        }

        private static void TestDashboardSnapshotUsesOneTimestamp()
        {
            var gameId = Guid.NewGuid();
            var now = new DateTime(2026, 8, 17, 23, 59, 58);
            var result = InvokeSnapshotWithTimestamp(
                new Playnite.SDK.Models.Game[0],
                new[]
                {
                    CreateSession(
                        gameId,
                        "Clock",
                        new DateTime(2026, 8, 17, 2, 0, 0, DateTimeKind.Utc),
                        60)
                },
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 8, 17),
                    CustomEndDate = new DateTime(2026, 8, 17)
                },
                now);

            Equal(true, result.Snapshot.StatusText.Contains("23:59:58"));
            Equal(
                "今天 10:00",
                result.Snapshot.RangeGameRankings[0].LastPlayedText);
        }

        private static DashboardRankingProjection InvokeRangeRankingProjection(
            AnalyticsService service,
            DashboardAnalysisContext context,
            RankingMetric metric,
            int topGames,
            DateTime now)
        {
            var method = typeof(AnalyticsService).GetMethod(
                "CreateRankingProjection",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(DashboardAnalysisContext),
                    typeof(RankingMetric),
                    typeof(int),
                    typeof(DateTime)
                },
                null);
            Equal(true, method != null);
            return (DashboardRankingProjection)method.Invoke(
                service,
                new object[] { context, metric, topGames, now });
        }

        private static IList<GameRankingViewModel> InvokeLifetimeRankings(
            IEnumerable<Playnite.SDK.Models.Game> games,
            int topGames,
            DateTime now)
        {
            var method = typeof(AnalyticsService).GetMethod(
                "CreateLifetimeRankings",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(IEnumerable<Playnite.SDK.Models.Game>),
                    typeof(int),
                    typeof(DateTime)
                },
                null);
            Equal(true, method != null);
            return (IList<GameRankingViewModel>)method.Invoke(
                null,
                new object[] { games, topGames, now });
        }

        private static DashboardSnapshotResult InvokeSnapshotWithTimestamp(
            IEnumerable<Playnite.SDK.Models.Game> games,
            IEnumerable<GameSession> sessions,
            AnalyticsQuery query,
            DateTime now)
        {
            var method = typeof(AnalyticsService).GetMethod(
                "CreateSnapshotWithContext",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(IEnumerable<Playnite.SDK.Models.Game>),
                    typeof(IEnumerable<GameSession>),
                    typeof(AnalyticsQuery),
                    typeof(DateTime)
                },
                null);
            Equal(true, method != null);
            return (DashboardSnapshotResult)method.Invoke(
                new AnalyticsService(),
                new object[] { games, sessions, query, now });
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
                    CreateSession(gameId, "Heat", new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc), 3600)
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
            Equal(HeatmapIntensityLevel.Low, snapshot.HeatmapCells[0].IntensityLevel);
            Equal(HeatmapIntensityLevel.Medium, snapshot.HeatmapCells[1].IntensityLevel);
        }

        private static void TestHeatmapAbsoluteDurationLevels()
        {
            Equal(HeatmapIntensityLevel.None,
                HeatmapIntensityScale.FromSeconds(0));
            Equal(HeatmapIntensityLevel.Low,
                HeatmapIntensityScale.FromSeconds(3599));
            Equal(HeatmapIntensityLevel.Medium,
                HeatmapIntensityScale.FromSeconds(3600));
            Equal(HeatmapIntensityLevel.Medium,
                HeatmapIntensityScale.FromSeconds(10800));
            Equal(HeatmapIntensityLevel.High,
                HeatmapIntensityScale.FromSeconds(10801));
        }

        private static void TestHeatmapMonthAxisProjection()
        {
            var analyticsService = new AnalyticsService();

            // Multi-month range crossing boundary: 2026-07-01 -> 2026-08-31
            var multiMonth = analyticsService.CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 1),
                    CustomEndDate = new DateTime(2026, 8, 31),
                    UseIsoWeekStart = true
                });

            Equal(10, multiMonth.HeatmapColumnCount);
            Equal(2, multiMonth.HeatmapMonthLabels.Count);
            Equal(0, multiMonth.HeatmapMonthLabels[0].ColumnIndex);
            Equal(4, multiMonth.HeatmapMonthLabels[0].ColumnSpan);
            Equal("2026 年 7 月", multiMonth.HeatmapMonthLabels[0].Label);
            Equal(4, multiMonth.HeatmapMonthLabels[1].ColumnIndex);
            Equal(6, multiMonth.HeatmapMonthLabels[1].ColumnSpan);
            Equal("2026 年 8 月", multiMonth.HeatmapMonthLabels[1].Label);
            Equal(10, multiMonth.HeatmapWeekLabels.Count);
            Equal("1", multiMonth.HeatmapWeekLabels[0]);
            Equal("4", multiMonth.HeatmapWeekLabels[3]);
            Equal("1", multiMonth.HeatmapWeekLabels[4]);
            Equal("6", multiMonth.HeatmapWeekLabels[9]);

            // Mid-month crossing boundary: 2026-07-15 -> 2026-08-15
            var midMonth = analyticsService.CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 7, 15),
                    CustomEndDate = new DateTime(2026, 8, 15),
                    UseIsoWeekStart = true
                });

            Equal(5, midMonth.HeatmapColumnCount);
            Equal(35, midMonth.HeatmapCells.Count);
            Equal(2, midMonth.HeatmapMonthLabels.Count);
            Equal(0, midMonth.HeatmapMonthLabels[0].ColumnIndex);
            Equal(2, midMonth.HeatmapMonthLabels[0].ColumnSpan);
            Equal("2026 年 7 月", midMonth.HeatmapMonthLabels[0].Label);
            Equal(2, midMonth.HeatmapMonthLabels[1].ColumnIndex);
            Equal(3, midMonth.HeatmapMonthLabels[1].ColumnSpan);
            Equal("2026 年 8 月", midMonth.HeatmapMonthLabels[1].Label);
            Equal(5, midMonth.HeatmapWeekLabels.Count);
            Equal("1", midMonth.HeatmapWeekLabels[0]);
            Equal("2", midMonth.HeatmapWeekLabels[1]);
            Equal("1", midMonth.HeatmapWeekLabels[2]);
            Equal("3", midMonth.HeatmapWeekLabels[4]);

            // Assert out-of-range hidden dates in row-major layout (row * columnCount + col)
            Equal(Visibility.Hidden, midMonth.HeatmapCells[0 * 5 + 0].CellVisibility); // Mon 2026-07-13
            Equal(Visibility.Hidden, midMonth.HeatmapCells[1 * 5 + 0].CellVisibility); // Tue 2026-07-14
            Equal(Visibility.Visible, midMonth.HeatmapCells[2 * 5 + 0].CellVisibility); // Wed 2026-07-15 (range start)
            Equal(Visibility.Visible, midMonth.HeatmapCells[5 * 5 + 4].CellVisibility); // Sat 2026-08-15 (range end)
            Equal(Visibility.Hidden, midMonth.HeatmapCells[6 * 5 + 4].CellVisibility); // Sun 2026-08-16

            // Localization resource check for MonthRangeFormat in zh_CN and en_US
            var sourceRoot = FindSourceRoot();
            var xamlNamespace = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
            var english = XDocument.Load(Path.Combine(sourceRoot, "Localization", "en_US.xaml"));
            var chinese = XDocument.Load(Path.Combine(sourceRoot, "Localization", "zh_CN.xaml"));

            string GetString(XDocument doc, string key) =>
                doc.Descendants().Where(e => (string)e.Attribute(xamlNamespace + "Key") == key)
                   .Select(e => e.Value).Single();

            var zhFormat = GetString(chinese, "LOCPlaytimeInsightsMonthRangeFormat");
            var enFormat = GetString(english, "LOCPlaytimeInsightsMonthRangeFormat");
            Equal("{0:yyyy 年 M 月}", zhFormat);
            Equal("{0:yyyy/M}", enFormat);

            var julDate = new DateTime(2026, 7, 1);
            var augDate = new DateTime(2026, 8, 1);
            var zhCulture = CultureInfo.GetCultureInfo("zh-CN");
            var enCulture = CultureInfo.GetCultureInfo("en-US");
            Equal("2026 年 7 月", string.Format(zhCulture, zhFormat, julDate));
            Equal("2026 年 8 月", string.Format(zhCulture, zhFormat, augDate));
            Equal("2026/7", string.Format(enCulture, enFormat, julDate));
            Equal("2026/8", string.Format(enCulture, enFormat, augDate));
        }

        private static void TestHeatmapSixWeekMonth()
        {
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 8, 1),
                    CustomEndDate = new DateTime(2026, 8, 31),
                    UseIsoWeekStart = true
                });

            Equal(6, snapshot.HeatmapColumnCount);
            Equal(42, snapshot.HeatmapCells.Count);
            Equal(1, snapshot.HeatmapMonthLabels.Count);
            Equal(0, snapshot.HeatmapMonthLabels[0].ColumnIndex);
            Equal(6, snapshot.HeatmapMonthLabels[0].ColumnSpan);
            Equal(6, snapshot.HeatmapWeekLabels.Count);
            Equal("1", snapshot.HeatmapWeekLabels[0]);
            Equal("6", snapshot.HeatmapWeekLabels[5]);
        }

        private static void TestHeatmapWeekGrouping()
        {
            var snapshot = new AnalyticsService().CreateSnapshot(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0],
                new AnalyticsQuery
                {
                    RangePreset = DateRangePreset.Custom,
                    CustomStartDate = new DateTime(2026, 8, 1),
                    CustomEndDate = new DateTime(2026, 8, 31)
                });

            var columnCount = snapshot.HeatmapColumnCount;
            Equal(6, columnCount);
            Equal(6, snapshot.HeatmapWeeks.Count);
            Equal(42, snapshot.HeatmapCells.Count);
            for (var weekIndex = 0;
                weekIndex < snapshot.HeatmapWeeks.Count;
                weekIndex++)
            {
                var week = snapshot.HeatmapWeeks[weekIndex];
                Equal(weekIndex, week.ColumnIndex);
                Equal(snapshot.HeatmapWeekLabels[weekIndex], week.WeekLabel);
                Equal(7, week.Days.Count);
                for (var row = 0; row < 7; row++)
                {
                    Equal(
                        true,
                        ReferenceEquals(
                            snapshot.HeatmapCells[row * columnCount + weekIndex],
                            week.Days[row]));
                }
            }

            var viewModel = CreateDashboardViewModelForLayout();
            var notifications = 0;
            viewModel.Distribution.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "HeatmapWeeks")
                {
                    notifications++;
                }
            };
            viewModel.Distribution.Apply(snapshot);
            Equal(1, notifications);
            Equal(
                snapshot.HeatmapWeeks.Count,
                viewModel.Distribution.HeatmapWeeks.Count);
            Equal(
                true,
                ReferenceEquals(
                    viewModel.Distribution.HeatmapWeeks[0].Days[0],
                    viewModel.Distribution.HeatmapCells[0]));
        }

        private static void TestHeatmapMonthAxisPanel()
        {
            RunOnSta(() =>
            {
                var panel = new HeatmapMonthAxisPanel
                {
                    ColumnCount = 8
                };
                Equal(26d, panel.ColumnPitch);
                panel.ColumnPitch = 26d;
                panel.Children.Add(CreateMonthAxisChild(0, 2));
                panel.Children.Add(CreateMonthAxisChild(2, 4));
                panel.Children.Add(CreateMonthAxisChild(6, 2));
                panel.Measure(new Size(double.PositiveInfinity, 20));
                panel.Arrange(new Rect(0, 0, 208, 20));

                Equal(208d, panel.DesiredSize.Width);
                Equal(0d, GetLayoutSlot(panel.Children[0]).X);
                Equal(52d, GetLayoutSlot(panel.Children[0]).Width);
                Equal(52d, GetLayoutSlot(panel.Children[1]).X);
                Equal(104d, GetLayoutSlot(panel.Children[1]).Width);
                Equal(156d, GetLayoutSlot(panel.Children[2]).X);
                Equal(52d, GetLayoutSlot(panel.Children[2]).Width);
            });
        }

        private static FrameworkElement CreateMonthAxisChild(int columnIndex, int columnSpan)
        {
            var child = new Border { Height = 16 };
            HeatmapMonthAxisPanel.SetColumnIndex(child, columnIndex);
            HeatmapMonthAxisPanel.SetColumnSpan(child, columnSpan);
            return child;
        }

        private static void TestCalendarHeatmapVisualContract()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboardCodePath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs");
            var resourcesPath = Path.Combine(
                sourceRoot,
                "Resources",
                "PlaytimeInsightsVisualResources.xaml");
            var analyticsPath = Path.Combine(
                sourceRoot,
                "Services",
                "AnalyticsService.cs");
            var heatmapModelPath = Path.Combine(
                sourceRoot,
                "ViewModels",
                "Dashboard",
                "HeatmapCellViewModel.cs");

            var dashboardXaml = File.ReadAllText(dashboardPath);
            var dashboardCode = File.ReadAllText(dashboardCodePath);
            var resourcesXaml = File.ReadAllText(resourcesPath);
            var analyticsSource = File.ReadAllText(analyticsPath);
            var heatmapModelSource = File.ReadAllText(heatmapModelPath);
            var calendarStart = dashboardXaml.IndexOf(
                "ItemsSource=\"{Binding HeatmapMonthLabels}\"",
                StringComparison.Ordinal);
            var calendarEnd = dashboardXaml.IndexOf(
                "ItemsSource=\"{Binding HeatmapCells}\"",
                StringComparison.Ordinal);
            var cellBlockEnd = dashboardXaml.IndexOf(
                "x:Name=\"AnomalyModule\"",
                StringComparison.Ordinal);

            // T1-P2-01: Calendar model & projection must not contain HeatOpacity
            var heatmapProjection = ExtractSourceBlock(
                analyticsSource,
                "private static HeatmapProjection CreateHeatmapProjection(",
                "private static void ApplyPeriodGameSummaries(");
            Equal(false, heatmapProjection.Contains("HeatOpacity"));
            Equal(false, heatmapModelSource.Contains("public double HeatOpacity { get; set; }") &&
                         heatmapModelSource.IndexOf("HeatOpacity", StringComparison.Ordinal) <
                         heatmapModelSource.IndexOf("WeekHourCellViewModel", StringComparison.Ordinal));
            Equal(true, heatmapModelSource.Contains("public double HeatOpacity { get; set; }")); // Preserved in WeekHourCellViewModel

            // T2-P1-01 & T2-P2-01: Visual and accessibility contracts
            Equal(true, dashboardXaml.Contains("controls:HeatmapMonthAxisPanel"));
            Equal(true, dashboardXaml.Contains("AlternationCount=\"7\""));
            Equal(true, dashboardXaml.Contains("(ItemsControl.AlternationIndex)"));
            Equal(false, dashboardXaml.Contains("Property=\"ItemsControl.AlternationIndex\""));
            Equal(true, dashboardXaml.Contains("DataContext.SelectHeatmapDateCommand"));
            Equal(false, dashboardXaml.Contains("HeatmapCell_MouseLeftButtonUp"));
            Equal(false, dashboardCode.Contains("HeatmapCell_MouseLeftButtonUp"));

            Equal(true, calendarStart >= 0);
            Equal(true, calendarEnd > calendarStart);
            Equal(true, cellBlockEnd > calendarEnd);
            var axisGeometry = dashboardXaml.Substring(
                calendarStart,
                calendarEnd - calendarStart);
            var cellGeometry = dashboardXaml.Substring(
                calendarEnd,
                cellBlockEnd - calendarEnd);

            // Axis geometry: 26 DIP pitch and weekday rows.
            Equal(true, axisGeometry.Contains("ColumnPitch=\"26\""));
            Equal(false, axisGeometry.Contains("ColumnPitch=\"24\""));
            Equal(false, axisGeometry.Contains("Width=\"14\""));
            Equal(false, axisGeometry.Contains("Height=\"14\""));

            var weekdayStart = dashboardXaml.IndexOf(
                "ItemsSource=\"{Binding HeatmapWeekdayLabels}\"",
                StringComparison.Ordinal);

            // Week-number labels center horizontally over 26 DIP cells.
            var weekStart = dashboardXaml.IndexOf(
                "ItemsSource=\"{Binding HeatmapWeekLabels}\"",
                StringComparison.Ordinal);
            Equal(true, weekStart > calendarStart);
            Equal(true, weekdayStart > weekStart);
            var weekGeometry = dashboardXaml.Substring(
                weekStart,
                weekdayStart - weekStart);
            Equal(true, weekGeometry.Contains("Width=\"26\""));
            Equal(true, weekGeometry.Contains("TextAlignment=\"Center\""));
            Equal(true, weekGeometry.Contains("HorizontalAlignment=\"Center\""));

            // Weekday glyphs center vertically inside 26 DIP row containers.
            Equal(true, weekdayStart > calendarStart);
            Equal(true, calendarEnd > weekdayStart);
            var weekdayGeometry = dashboardXaml.Substring(
                weekdayStart,
                calendarEnd - weekdayStart);
            Equal(true, weekdayGeometry.Contains("<Grid Height=\"26\">"));
            Equal(true, weekdayGeometry.Contains("VerticalAlignment=\"Center\""));
            Equal(true, weekdayGeometry.Contains("HorizontalAlignment=\"Stretch\""));
            Equal(false, weekdayGeometry.Contains("VerticalAlignment=\"Top\""));

            // Cell geometry: lightweight 26 DIP buttons draw their own 24 DIP swatches.
            Equal(true, cellGeometry.Contains("<controls:HeatmapCellButton"));
            Equal(true, cellGeometry.Contains("Width=\"26\""));
            Equal(true, cellGeometry.Contains("Height=\"26\""));
            Equal(true, cellGeometry.Contains(
                "Style=\"{StaticResource HeatmapCellButtonStyle}\""));
            Equal(false, cellGeometry.Contains("CellButtonRoot"));
            Equal(false, cellGeometry.Contains("CellSwatch"));
            Equal(true, dashboardXaml.Contains("UniformGrid Columns=\"{Binding HeatmapColumnCount}\""));

            // The lightweight button renders the swatch and focus outline itself.
            var cellButtonSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Controls",
                "HeatmapCellButton.cs"));
            Equal(true, cellButtonSource.Contains("new Rect(1d, 1d, 24d, 24d)"));
            Equal(true, cellButtonSource.Contains("3d"));
            Equal(true, cellButtonSource.Contains("IsKeyboardFocused"));
            Equal(false, cellButtonSource.Contains("PanelSeparatorBrush"));

            // Legend brushes
            Equal(true, resourcesXaml.Contains("HeatmapNoneBrush"));
            Equal(true, resourcesXaml.Contains("HeatmapLowBrush"));
            Equal(true, resourcesXaml.Contains("HeatmapMediumBrush"));
            Equal(true, resourcesXaml.Contains("HeatmapHighBrush"));
            var legendStart = dashboardXaml.IndexOf(
                "LOCPlaytimeInsightsCalendarHeatmap}",
                StringComparison.Ordinal);
            Equal(true, legendStart >= 0 && legendStart < calendarStart);
            var legendBlock = dashboardXaml.Substring(
                legendStart,
                calendarStart - legendStart);
            var expectedLegendPairs = new[]
            {
                "HeatmapNoneBrush|LOCPlaytimeInsightsHeatmapZeroHours",
                "HeatmapLowBrush|LOCPlaytimeInsightsHeatmapUnderOneHour",
                "HeatmapMediumBrush|LOCPlaytimeInsightsHeatmapOneToThreeHours",
                "HeatmapHighBrush|LOCPlaytimeInsightsHeatmapOverThreeHours"
            };
            var legendEntries = Regex.Matches(
                legendBlock,
                "<Border\\s+[^>]*Background=\"\\{StaticResource\\s+" +
                "(?<brush>Heatmap(?:None|Low|Medium|High)Brush)\\}\"[^>]*" +
                "ToolTip=\"\\{DynamicResource\\s+(?<tooltip>[^}]+)\\}\"[^>]*/>",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Equal(expectedLegendPairs.Length, legendEntries.Count);
            for (var index = 0; index < expectedLegendPairs.Length; index++)
            {
                Equal(
                    expectedLegendPairs[index],
                    legendEntries[index].Groups["brush"].Value + "|" +
                    legendEntries[index].Groups["tooltip"].Value);
            }
            Equal(true, legendBlock.IndexOf(
                "LOCPlaytimeInsightsHeatmapLess",
                StringComparison.Ordinal) < legendEntries[0].Index);
            Equal(true, legendBlock.IndexOf(
                "LOCPlaytimeInsightsHeatmapMore",
                StringComparison.Ordinal) >
                legendEntries[legendEntries.Count - 1].Index);

            // STA runtime proof of alternating weekday visibility: [Visible, Hidden, Visible, Hidden, Visible, Hidden, Visible]
            RunOnSta(() =>
            {
                var weekdayItems = new[] { "一", "二", "三", "四", "五", "六", "日" };
                var control = new ItemsControl
                {
                    ItemsSource = weekdayItems,
                    AlternationCount = 7
                };
                var style = new Style(typeof(ContentPresenter));
                foreach (var index in new[] { 1, 3, 5 })
                {
                    var trigger = new DataTrigger
                    {
                        Binding = new System.Windows.Data.Binding
                        {
                            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
                            Path = new PropertyPath("(0)", ItemsControl.AlternationIndexProperty)
                        },
                        Value = index
                    };
                    trigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Hidden));
                    style.Triggers.Add(trigger);
                }
                control.ItemContainerStyle = style;

                var window = new Window
                {
                    Content = control,
                    Width = 100,
                    Height = 200
                };
                window.Show();
                control.UpdateLayout();

                var expectedVisibilities = new[]
                {
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible
                };

                for (var i = 0; i < 7; i++)
                {
                    var container = control.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                    Equal(expectedVisibilities[i], container.Visibility);
                }

                window.Close();
            });
        }

        private static void TestCalendarHeatmapRuntimeMapping()
        {
            RunOnSta(() =>
            {
                var viewModel = CreateDashboardViewModelForLayout();
                var snapshot = CreateHeatmapLayoutSnapshot(4);
                snapshot.HeatmapCells[0].IntensityLevel =
                    HeatmapIntensityLevel.None;
                snapshot.HeatmapCells[1].IntensityLevel =
                    HeatmapIntensityLevel.Low;
                snapshot.HeatmapCells[2].IntensityLevel =
                    HeatmapIntensityLevel.Medium;
                snapshot.HeatmapCells[3].IntensityLevel =
                    HeatmapIntensityLevel.High;
                viewModel.Distribution.Apply(snapshot);

                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1248,
                    DataContext = viewModel
                };
                LayoutDashboardViewAt(view, 1248);

                var distribution = (FrameworkElement)view.FindName(
                    "DistributionModule");
                var cellButtons = FindVisualDescendants<HeatmapCellButton>(
                        distribution)
                    .Where(button => button.DataContext is HeatmapCellViewModel)
                    .OrderBy(button =>
                        ((HeatmapCellViewModel)button.DataContext).Date)
                    .ToList();
                Equal(4, cellButtons.Count);
                var expectedKeys = new[]
                {
                    "HeatmapNoneBrush",
                    "HeatmapLowBrush",
                    "HeatmapMediumBrush",
                    "HeatmapHighBrush"
                };
                for (var index = 0; index < cellButtons.Count; index++)
                {
                    Equal(
                        view.TryFindResource(expectedKeys[index]),
                        cellButtons[index].Background);
                }

                var weekdayLabels = snapshot.HeatmapWeekdayLabels;
                var weekdayControl = FindVisualDescendants<ItemsControl>(
                        distribution)
                    .Single(control =>
                        control.Items.Count == 7 &&
                        Equals(control.Items[0], weekdayLabels[0]));
                var expectedVisibility = new[]
                {
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible,
                    Visibility.Hidden,
                    Visibility.Visible
                };
                for (var index = 0; index < expectedVisibility.Length; index++)
                {
                    var container = (UIElement)weekdayControl
                        .ItemContainerGenerator.ContainerFromIndex(index);
                    Equal(expectedVisibility[index], container.Visibility);
                }

                cellButtons[1].Command.Execute(cellButtons[1].CommandParameter);
                PumpDispatcher();
                view.UpdateLayout();
                var host = (ContentControl)view.FindName(
                    "DistributionDrilldownHost");
                Equal(Visibility.Visible, host.Visibility);
                Equal(false, host.HasAnimatedProperties);

                var low = (LinearGradientBrush)view.TryFindResource(
                    "HeatmapLowBrush");
                var medium = (LinearGradientBrush)view.TryFindResource(
                    "HeatmapMediumBrush");
                var high = (LinearGradientBrush)view.TryFindResource(
                    "HeatmapHighBrush");
                var weekHour = (LinearGradientBrush)view.TryFindResource(
                    "HeatmapActiveBrush");
                Equal(true, low != null && medium != null && high != null);
                Equal(true, weekHour != null);
                Equal(
                    "#FF2457D6|#FFA45CFF",
                    string.Join(
                        "|",
                        weekHour.GradientStops.Select(
                            stop => stop.Color.ToString())));
                var calendarBrushes = new[] { low, medium, high };
                var expectedCalendarStops = new[]
                {
                    "#FF0C5C74|#FF20734A",
                    "#FF0692B8|#FF1EA884",
                    "#FF0EBAFF|#FF42EEC0"
                };
                for (var index = 0;
                    index < calendarBrushes.Length;
                    index++)
                {
                    Equal(
                        expectedCalendarStops[index],
                        string.Join(
                            "|",
                            calendarBrushes[index].GradientStops.Select(
                                stop => stop.Color.ToString())));
                }
                var weekHourAnchors = weekHour.GradientStops
                    .Select(stop => stop.Color)
                    .ToList();
                var moduleBackground = Color.FromRgb(0x1B, 0x1C, 0x24);
                var sheenDistances = new List<double>();
                foreach (var brush in calendarBrushes)
                {
                    Equal(2, brush.GradientStops.Count);
                    sheenDistances.Add(Cie76Distance(
                        brush.GradientStops[0].Color,
                        brush.GradientStops[1].Color));
                    foreach (var stop in brush.GradientStops)
                    {
                        Equal(
                            true,
                            ContrastRatio(stop.Color, moduleBackground) >= 2d);
                        foreach (var anchor in weekHourAnchors)
                        {
                            Equal(
                                true,
                                Cie76Distance(stop.Color, anchor) >= 15d);
                            Equal(
                                true,
                                Cie76Distance(
                                    SimulateDeuteranopia(stop.Color),
                                    SimulateDeuteranopia(anchor)) >= 15d);
                        }
                    }
                }
                Equal(true, sheenDistances[1] > sheenDistances[0]);
                Equal(true, sheenDistances[2] > sheenDistances[1]);
                var midpointLightness = new[] { low, medium, high }
                    .Select(brush => CieLabLightness(AverageColor(
                        brush.GradientStops[0].Color,
                        brush.GradientStops[1].Color)))
                    .ToList();
                Equal(true,
                    midpointLightness[1] - midpointLightness[0] >= 6d);
                Equal(true,
                    midpointLightness[2] - midpointLightness[1] >= 6d);
            });
        }

        private static void TestHeatmapCellButtonContract()
        {
            var buttonSource = File.ReadAllText(Path.Combine(
                FindSourceRoot(),
                "Controls",
                "HeatmapCellButton.cs"));
            Equal(true, buttonSource.Contains("IsMouseOver"));
            Equal(true, buttonSource.Contains("IsKeyboardFocused"));
            Equal(true, buttonSource.Contains("BorderBrush"));
            Equal(true, buttonSource.Contains("Foreground"));
            Equal(true, buttonSource.Contains("DrawRoundedRectangle"));
            Equal(4, CountOccurrences(buttonSource, "InvalidateVisual"));
            Equal(false, buttonSource.Contains("GradientStop"));
            Equal(false, buttonSource.Contains("RectangleGeometry"));
            var dashboardXaml = File.ReadAllText(Path.Combine(
                FindSourceRoot(),
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            Equal(true, dashboardXaml.Contains(
                "<Setter Property=\"Foreground\" Value=\"{DynamicResource TextBrush}\" />"));
            Equal(true, dashboardXaml.Contains(
                "<Setter Property=\"BorderBrush\" Value=\"{DynamicResource PanelSeparatorBrush}\" />"));

            RunOnSta(() =>
            {
                var viewModel = CreateDashboardViewModelForLayout();
                var snapshot = CreateHeatmapLayoutSnapshot(4);
                snapshot.HeatmapCells[0].IntensityLevel =
                    HeatmapIntensityLevel.None;
                snapshot.HeatmapCells[1].IntensityLevel =
                    HeatmapIntensityLevel.Low;
                snapshot.HeatmapCells[2].IntensityLevel =
                    HeatmapIntensityLevel.Medium;
                snapshot.HeatmapCells[3].IntensityLevel =
                    HeatmapIntensityLevel.High;
                viewModel.Distribution.Apply(snapshot);

                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1248,
                    DataContext = viewModel
                };
                LayoutDashboardViewAt(view, 1248);
                var distribution = (FrameworkElement)view.FindName(
                    "DistributionModule");
                var cellButtons = FindVisualDescendants<HeatmapCellButton>(
                        distribution)
                    .OrderBy(button =>
                        ((HeatmapCellViewModel)button.DataContext).Date)
                    .ToList();
                Equal(4, cellButtons.Count);

                var expectedKeys = new[]
                {
                    "HeatmapNoneBrush",
                    "HeatmapLowBrush",
                    "HeatmapMediumBrush",
                    "HeatmapHighBrush"
                };
                for (var index = 0; index < cellButtons.Count; index++)
                {
                    var button = cellButtons[index];
                    Equal(26d, button.Width);
                    Equal(26d, button.Height);
                    Equal(true, button.Command != null);
                    Equal(true, button.CommandParameter != null);
                    Equal(
                        true,
                        !string.IsNullOrWhiteSpace(button.ToolTip as string));
                    Equal(
                        true,
                        !string.IsNullOrWhiteSpace(
                            button.GetValue(AutomationProperties.NameProperty)
                            as string));
                    Equal(
                        view.TryFindResource(expectedKeys[index]),
                        button.Background);
                    Equal(
                        true,
                        VisualTreeHelper.GetChildrenCount(button) <= 1);
                }

                var focusTarget = cellButtons[2];
                Equal(true, focusTarget.Focusable);
                // A headless STA harness has no presentation source, so
                // IsVisible stays false and Keyboard focus cannot be granted;
                // visibility and the focus outline contract are asserted
                // structurally, live keyboard entry goes to the manual matrix.
                Equal(Visibility.Visible, focusTarget.Visibility);
            });
        }

        private static void TestHeatmapLayoutSampleSummary()
        {
            var summary = HeatmapLayoutSampleSummary.FromMeasurements(new[]
            {
                new HeatmapLayoutMeasurement
                {
                    ElapsedMilliseconds = 809,
                    RealizedButtons = 1820,
                    RealizedWeekContainers = 260
                },
                new HeatmapLayoutMeasurement
                {
                    ElapsedMilliseconds = 1914,
                    RealizedButtons = 1820,
                    RealizedWeekContainers = 260
                },
                new HeatmapLayoutMeasurement
                {
                    ElapsedMilliseconds = 1200,
                    RealizedButtons = 1820,
                    RealizedWeekContainers = 260
                },
                new HeatmapLayoutMeasurement
                {
                    ElapsedMilliseconds = 839,
                    RealizedButtons = 1820,
                    RealizedWeekContainers = 260
                },
                new HeatmapLayoutMeasurement
                {
                    ElapsedMilliseconds = 1581,
                    RealizedButtons = 1820,
                    RealizedWeekContainers = 260
                }
            });

            Equal(1200d, summary.MedianMilliseconds);
            Equal(1914d, summary.MaxMilliseconds);
            Equal(1820, summary.MaxRealizedButtons);
            Equal(260, summary.MaxRealizedWeekContainers);
        }

        private static void TestCalendarHeatmapLayoutCost()
        {
            RunOnSta(() =>
            {
                var oneYear = CreateHeatmapBenchmarkSnapshot(false);
                var allSessions = CreateHeatmapBenchmarkSnapshot(true);
                Equal(371, oneYear.HeatmapCells.Count);
                Equal(1820, allSessions.HeatmapCells.Count);
                Equal(true, oneYear.HeatmapMonthLabels.Count >= 12);
                Equal(true, allSessions.HeatmapMonthLabels.Count >= 60);
                Equal(168, oneYear.Advanced.WeekHourCells.Count);
                Equal(168, allSessions.Advanced.WeekHourCells.Count);
                MeasureHeatmapLayoutSamples("one year", oneYear, 5);
                MeasureHeatmapLayoutSamples("all sessions", allSessions, 5);
            });
        }

        private static HeatmapLayoutSampleSummary MeasureHeatmapLayoutSamples(
            string label,
            DashboardSnapshot snapshot,
            int measuredCount)
        {
            MeasureHeatmapLayoutSample(snapshot);
            var samples = new List<HeatmapLayoutMeasurement>(measuredCount);
            for (var index = 0; index < measuredCount; index++)
            {
                samples.Add(MeasureHeatmapLayoutSample(snapshot));
            }

            var summary = HeatmapLayoutSampleSummary.FromMeasurements(samples);
            Console.WriteLine(
                "       heatmap UI / {0} / {1:N0} cells: {2:N1} / {3:N1} / {4:N1} / {5:N1} / {6:N1} ms; median {7:N1} ms; max {8:N1} ms; realized buttons {9:N0}",
                label,
                snapshot.HeatmapCells.Count,
                samples[0].ElapsedMilliseconds,
                samples[1].ElapsedMilliseconds,
                samples[2].ElapsedMilliseconds,
                samples[3].ElapsedMilliseconds,
                samples[4].ElapsedMilliseconds,
                summary.MedianMilliseconds,
                summary.MaxMilliseconds,
                summary.MaxRealizedButtons);
            return summary;
        }

        private static HeatmapLayoutMeasurement MeasureHeatmapLayoutSample(
            DashboardSnapshot snapshot)
        {
            var viewModel = CreateDashboardViewModelForLayout();
            viewModel.Distribution.Apply(snapshot);
            var view = new PlaytimeInsightsDashboardView
            {
                Width = 1248,
                DataContext = viewModel
            };
            var distribution = (FrameworkElement)view.FindName(
                "DistributionModule");
            var stopwatch = Stopwatch.StartNew();
            distribution.Measure(new Size(856.84d, double.PositiveInfinity));
            distribution.Arrange(new Rect(
                0d,
                0d,
                856.84d,
                distribution.DesiredSize.Height));
            distribution.UpdateLayout();
            stopwatch.Stop();

            var realizedButtons = FindVisualDescendants<Button>(distribution)
                .Count(button => button.DataContext is HeatmapCellViewModel);
            Equal(snapshot.HeatmapCells.Count, realizedButtons);
            var weekAxis = (FrameworkElement)view.FindName(
                "HeatmapWeekNumberAxis");
            var realizedWeekContainers = weekAxis == null
                ? 0
                : FindVisualDescendants<ContentPresenter>(weekAxis).Count();
            return new HeatmapLayoutMeasurement
            {
                ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                RealizedButtons = realizedButtons,
                RealizedWeekContainers = realizedWeekContainers
            };
        }

        private static DashboardSnapshot CreateHeatmapBenchmarkSnapshot(
            bool allSessions)
        {
            var gameId = Guid.NewGuid();
            var games = new[]
            {
                new Playnite.SDK.Models.Game
                {
                    Id = gameId,
                    Name = allSessions
                        ? "All Sessions heatmap fixture"
                        : "One-year heatmap fixture"
                }
            };
            var sessions = new[]
            {
                CreateSession(
                    gameId,
                    games[0].Name,
                    allSessions
                        ? new DateTime(
                            2021,
                            1,
                            4,
                            12,
                            0,
                            0,
                            DateTimeKind.Utc)
                        : new DateTime(
                            2025,
                            1,
                            1,
                            12,
                            0,
                            0,
                            DateTimeKind.Utc),
                    3600),
                CreateSession(
                    gameId,
                    games[0].Name,
                    new DateTime(
                        2025,
                        12,
                        28,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc),
                    10801)
            };
            var query = new AnalyticsQuery
            {
                RangePreset = allSessions
                    ? DateRangePreset.AllSessions
                    : DateRangePreset.Custom,
                CustomStartDate = new DateTime(2025, 1, 1),
                CustomEndDate = new DateTime(2025, 12, 31),
                AggregationPeriod = AggregationPeriod.Month,
                UseIsoWeekStart = true,
                TopGames = 10
            };
            return InvokeSnapshotWithTimestamp(
                games,
                sessions,
                query,
                new DateTime(
                    2025,
                    12,
                    28,
                    20,
                    0,
                    0,
                    DateTimeKind.Local)).Snapshot;
        }

        private static DashboardViewModel CreateDashboardViewModelForLayout()
        {
            var settings =
                (PlaytimeInsightsSettingsViewModel)
                System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(
                        typeof(PlaytimeInsightsSettingsViewModel));
            settings.Settings = new PlaytimeInsightsSettings();
            return new DashboardViewModel(
                null,
                null,
                new AnalyticsService(),
                new SessionQueryService(new TestGameMetadataAccessor()),
                settings);
        }

        private static DashboardSnapshot CreateHeatmapLayoutSnapshot(
            int cellCount)
        {
            var columnCount = Math.Max(1, (int)Math.Ceiling(cellCount / 7d));
            var cells = Enumerable.Range(0, cellCount)
                .Select(index => new HeatmapCellViewModel
                {
                    Date = new DateTime(2021, 1, 4).AddDays(index),
                    Seconds = (ulong)(index % 4) * 3600UL,
                    IntensityLevel = (HeatmapIntensityLevel)(index % 4),
                    CellVisibility = Visibility.Visible,
                    TooltipText = "Cell " + index
                })
                .ToList();
            var weekLabels = Enumerable.Range(1, columnCount)
                .Select(index => index.ToString(CultureInfo.InvariantCulture))
                .ToList();
            return new DashboardSnapshot
            {
                PeriodActivities = new List<PeriodActivityViewModel>(),
                HeatmapCells = cells,
                HeatmapWeeks = Enumerable.Range(0, columnCount)
                    .Select(column => new HeatmapWeekViewModel
                    {
                        ColumnIndex = column,
                        WeekLabel = weekLabels[column],
                        Days = Enumerable.Range(0, 7)
                            .Where(row => row * columnCount + column < cells.Count)
                            .Select(row => cells[row * columnCount + column])
                            .ToList()
                    })
                    .ToList(),
                HeatmapWeekdayLabels = new List<string>
                {
                    "Mon-fixture",
                    "Tue-fixture",
                    "Wed-fixture",
                    "Thu-fixture",
                    "Fri-fixture",
                    "Sat-fixture",
                    "Sun-fixture"
                },
                HeatmapMonthLabels = new List<HeatmapMonthLabelViewModel>(),
                HeatmapWeekLabels = weekLabels,
                HeatmapColumnCount = columnCount,
                TrendLinePoints = new PointCollection(),
                TrendLineGeometry = Geometry.Empty,
                TrendAreaGeometry = Geometry.Empty,
                TrendPoints = new List<TrendPointViewModel>(),
                RangeGameRankings = new List<GameRankingViewModel>(),
                LifetimeGameRankings = new List<GameRankingViewModel>(),
                Advanced = new AdvancedAnalyticsSnapshot
                {
                    WeekdayDistribution = new List<DistributionBarViewModel>(),
                    HourDistribution = new List<DistributionBarViewModel>(),
                    WeekHourCells = new List<WeekHourCellViewModel>(),
                    WeekdayLabels = new List<string>(),
                    HourLabels = new List<string>(),
                    AnomalyVisibility = Visibility.Collapsed,
                    Anomalies = new List<AnomalySessionViewModel>()
                }
            };
        }

        private static Color AverageColor(Color first, Color second)
        {
            return Color.FromArgb(
                (byte)((first.A + second.A) / 2),
                (byte)((first.R + second.R) / 2),
                (byte)((first.G + second.G) / 2),
                (byte)((first.B + second.B) / 2));
        }

        private static double Cie76Distance(Color first, Color second)
        {
            var firstLab = ToCieLab(first);
            var secondLab = ToCieLab(second);
            return Math.Sqrt(
                Math.Pow(firstLab[0] - secondLab[0], 2d) +
                Math.Pow(firstLab[1] - secondLab[1], 2d) +
                Math.Pow(firstLab[2] - secondLab[2], 2d));
        }

        private static double CieLabLightness(Color color)
        {
            return ToCieLab(color)[0];
        }

        private static double ContrastRatio(Color first, Color second)
        {
            var lighter = Math.Max(
                RelativeLuminance(first),
                RelativeLuminance(second));
            var darker = Math.Min(
                RelativeLuminance(first),
                RelativeLuminance(second));
            return (lighter + 0.05d) / (darker + 0.05d);
        }

        private static double RelativeLuminance(Color color)
        {
            Func<byte, double> linearize = component =>
            {
                var value = component / 255d;
                return value <= 0.04045d
                    ? value / 12.92d
                    : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
            };
            return linearize(color.R) * 0.2126d +
                linearize(color.G) * 0.7152d +
                linearize(color.B) * 0.0722d;
        }

        private static Color SimulateDeuteranopia(Color color)
        {
            Func<byte, double> linearize = component =>
            {
                var value = component / 255d;
                return value <= 0.04045d
                    ? value / 12.92d
                    : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
            };
            Func<double, byte> encode = value =>
            {
                value = Math.Max(0d, Math.Min(1d, value));
                var encoded = value <= 0.0031308d
                    ? value * 12.92d
                    : 1.055d * Math.Pow(value, 1d / 2.4d) - 0.055d;
                return (byte)Math.Round(encoded * 255d);
            };
            var red = linearize(color.R);
            var green = linearize(color.G);
            var blue = linearize(color.B);
            return Color.FromArgb(
                color.A,
                encode(
                    0.367322d * red +
                    0.860646d * green -
                    0.227968d * blue),
                encode(
                    0.280085d * red +
                    0.672501d * green +
                    0.047413d * blue),
                encode(
                    -0.011820d * red +
                    0.042940d * green +
                    0.968881d * blue));
        }

        private static double[] ToCieLab(Color color)
        {
            Func<byte, double> linearize = component =>
            {
                var value = component / 255d;
                return value <= 0.04045d
                    ? value / 12.92d
                    : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
            };
            var red = linearize(color.R);
            var green = linearize(color.G);
            var blue = linearize(color.B);
            var x = (red * 0.4124d + green * 0.3576d + blue * 0.1805d) /
                0.95047d;
            var y = red * 0.2126d + green * 0.7152d + blue * 0.0722d;
            var z = (red * 0.0193d + green * 0.1192d + blue * 0.9505d) /
                1.08883d;
            Func<double, double> pivot = value => value > 0.008856d
                ? Math.Pow(value, 1d / 3d)
                : 7.787d * value + 16d / 116d;
            var fx = pivot(x);
            var fy = pivot(y);
            var fz = pivot(z);
            return new[]
            {
                116d * fy - 16d,
                500d * (fx - fy),
                200d * (fy - fz)
            };
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
            Equal(SessionSource.Recovered, details[0].Source);
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

        private static void TestClearDrilldownSelectionCommand()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            var normalizedSource = Regex.Replace(
                dashboardViewModel,
                @"\s+",
                string.Empty);

            Equal(true, normalizedSource.Contains(
                "ClearDrilldownSelectionCommand=newRelayCommand(" +
                "Drilldown.ResetSelection," +
                "()=>!refreshGuard.IsActive&&" +
                "Drilldown.SessionDetailVisibility==Visibility.Visible);"));
            Equal(true, dashboardViewModel.Contains(
                "public RelayCommand ClearDrilldownSelectionCommand { get; }"));
            Equal(true, Regex.Matches(
                dashboardViewModel,
                Regex.Escape(
                    "ClearDrilldownSelectionCommand?.RaiseCanExecuteChanged();"))
                .Count >= 2);
            Equal(true, normalizedSource.Contains(
                "if(args.PropertyName==nameof(" +
                "DashboardDrilldownViewModel.SessionDetailVisibility))" +
                "{ClearDrilldownSelectionCommand?.RaiseCanExecuteChanged();}"));
        }

        private static void TestDrilldownVirtualizationContract()
        {
            var sourceRoot = FindSourceRoot();
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");
            var document = XDocument.Load(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var visualResources = XDocument.Load(Path.Combine(
                sourceRoot,
                "Resources",
                "PlaytimeInsightsVisualResources.xaml"));
            var drilldownTemplate = document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "DataTemplate" &&
                    (string)element.Attribute(xamlNamespace + "Name") ==
                    null &&
                    (string)element.Attribute(xamlNamespace + "Key") ==
                    "DrilldownCardTemplate");
            var detailList = drilldownTemplate.Descendants()
                .Single(element =>
                    element.Name.LocalName == "ListView" &&
                    (string)element.Attribute("ItemsSource") ==
                    "{Binding SessionDetails}");

            Equal("True", (string)detailList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "VirtualizingPanel.IsVirtualizing"));
            Equal("Recycling", (string)detailList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "VirtualizingPanel.VirtualizationMode"));
            Equal("True", (string)detailList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "ScrollViewer.CanContentScroll"));
            Equal("{StaticResource PlaytimeInsightsDetailItemStyle}",
                (string)detailList.Attribute("ItemContainerStyle"));
            Equal("160", (string)detailList.Attribute("MinHeight"));
            Equal("Top", (string)detailList.Attribute("VerticalContentAlignment"));
            Equal("{StaticResource PlaytimeInsightsDetailListStyle}",
                (string)detailList.Attribute("Style"));
            var detailListStyle = visualResources.Descendants()
                .Single(element =>
                    element.Name.LocalName == "Style" &&
                    (string)element.Attribute(xamlNamespace + "Key") ==
                    "PlaytimeInsightsDetailListStyle");
            Equal("{x:Type ListView}",
                (string)detailListStyle.Attribute("TargetType"));
            Equal(true, detailListStyle.Descendants()
                .Any(element => element.Name.LocalName == "ItemsPresenter"));
            Equal(false, detailListStyle.Descendants()
                .Any(element => element.Name.LocalName == "GridViewHeaderRowPresenter"));
            var detailItemStyle = visualResources.Descendants()
                .Single(element =>
                    element.Name.LocalName == "Style" &&
                    (string)element.Attribute(xamlNamespace + "Key") ==
                    "PlaytimeInsightsDetailItemStyle");
            Equal("{x:Type ListViewItem}",
                (string)detailItemStyle.Attribute("TargetType"));
            Equal(true, detailItemStyle.Descendants()
                .Any(element => element.Name.LocalName == "ContentPresenter"));
            Equal(false, detailItemStyle.Descendants()
                .Any(element => element.Name.LocalName == "GridViewRowPresenter"));
            Equal(false, detailList.Descendants()
                .Any(element => element.Name.LocalName == "GridView"));
        }

        private static void TestDrilldownSourceTagForeground()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                var drilldownCard = (FrameworkElement)
                    ((DataTemplate)view.Resources["DrilldownCardTemplate"])
                        .LoadContent();
                var detailList = FindVisualDescendants<ListView>(
                    drilldownCard).Single();
                var presenter = new ContentPresenter
                {
                    ContentTemplate = detailList.ItemTemplate,
                    Content = new SessionDetailViewModel
                    {
                        Source = SessionSource.Tracked,
                        SourceText = "Automatic recording"
                    }
                };
                var themeTextBrush = new SolidColorBrush(Colors.White);
                var window = new Window
                {
                    Content = presenter,
                    Foreground = new SolidColorBrush(Colors.Black),
                    ShowInTaskbar = false,
                    Width = 320,
                    Height = 120,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000
                };
                window.Resources["TextBrush"] = themeTextBrush;

                try
                {
                    window.Show();
                    PumpDispatcher();

                    var sourceText = FindVisualDescendants<TextBlock>(
                        presenter).Single(text =>
                            text.Text == "Automatic recording");
                    Equal(themeTextBrush, sourceText.Foreground);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
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

        private static string CopyPngTo(
            string tempRoot,
            string targetFileName,
            string assetFileName)
        {
            var source = Path.Combine(
                FindSourceRoot(),
                assetFileName);
            var target = Path.Combine(tempRoot, targetFileName);
            File.Copy(source, target);
            return target;
        }

        private static string CreateGeneratedPng(
            string path,
            int width,
            int height,
            Color color)
        {
            var visual = new DrawingVisual();
            using (var drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawRectangle(
                    new SolidColorBrush(color),
                    null,
                    new Rect(0, 0, width, height));
            }

            var bitmap = new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }

            return path;
        }

        private static CoverImageCacheContract LoadCoverImageCacheContract()
        {
            const string contract =
                "PlaytimeInsights.Services.CoverImageCache with public constructor " +
                "(int capacity) and public BitmapSource GetOrLoad(string path, int decodePixelWidth)";
            var type = typeof(global::PlaytimeInsights.PlaytimeInsights)
                .Assembly
                .GetType("PlaytimeInsights.Services.CoverImageCache");
            if (type == null)
            {
                throw new InvalidOperationException(
                    "Cover cache contract missing: " + contract + ".");
            }

            var constructor = type.GetConstructor(new[] { typeof(int) });
            if (constructor == null || !constructor.IsPublic)
            {
                throw new InvalidOperationException(
                    "Cover cache contract missing: " + contract + ".");
            }

            var method = type.GetMethod(
                "GetOrLoad",
                new[] { typeof(string), typeof(int) });
            if (method == null ||
                !method.IsPublic ||
                method.ReturnType != typeof(BitmapSource))
            {
                throw new InvalidOperationException(
                    "Cover cache contract missing: " + contract + ".");
            }

            return new CoverImageCacheContract(constructor, method);
        }

        private sealed class AnalyticsPerformanceSampleSummary
        {
            public IList<double> SamplesMilliseconds { get; private set; }
            public double MedianMilliseconds { get; private set; }
            public double MaxMilliseconds { get; private set; }
            public int Gen0Collections { get; private set; }
            public int Gen1Collections { get; private set; }
            public int Gen2Collections { get; private set; }

            public static AnalyticsPerformanceSampleSummary FromMilliseconds(
                IEnumerable<double> samples,
                int gen0Collections,
                int gen1Collections,
                int gen2Collections)
            {
                var ordered = samples.OrderBy(value => value).ToList();
                var midpoint = ordered.Count / 2;
                var median = ordered.Count % 2 == 0
                    ? (ordered[midpoint - 1] + ordered[midpoint]) / 2d
                    : ordered[midpoint];
                return new AnalyticsPerformanceSampleSummary
                {
                    SamplesMilliseconds = ordered,
                    MedianMilliseconds = median,
                    MaxMilliseconds = ordered.Max(),
                    Gen0Collections = gen0Collections,
                    Gen1Collections = gen1Collections,
                    Gen2Collections = gen2Collections
                };
            }
        }

        private static AnalyticsPerformanceSampleSummary MeasureAnalyticsSamples(
            Func<DashboardSnapshot> action,
            int warmupCount,
            int measuredCount)
        {
            for (var index = 0; index < warmupCount; index++)
            {
                action();
            }

            var gen0Start = GC.CollectionCount(0);
            var gen1Start = GC.CollectionCount(1);
            var gen2Start = GC.CollectionCount(2);
            var samples = new List<double>(measuredCount);
            for (var index = 0; index < measuredCount; index++)
            {
                var stopwatch = Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            return AnalyticsPerformanceSampleSummary.FromMilliseconds(
                samples,
                GC.CollectionCount(0) - gen0Start,
                GC.CollectionCount(1) - gen1Start,
                GC.CollectionCount(2) - gen2Start);
        }

        private sealed class HeatmapLayoutMeasurement
        {
            public double ElapsedMilliseconds { get; set; }
            public int RealizedButtons { get; set; }
            public int RealizedWeekContainers { get; set; }
        }

        private sealed class HeatmapLayoutSampleSummary
        {
            public IList<double> SamplesMilliseconds { get; private set; }
            public double MedianMilliseconds { get; private set; }
            public double MaxMilliseconds { get; private set; }
            public int MaxRealizedButtons { get; private set; }
            public int MaxRealizedWeekContainers { get; private set; }

            public static HeatmapLayoutSampleSummary FromMeasurements(
                IEnumerable<HeatmapLayoutMeasurement> measurements)
            {
                var ordered = measurements
                    .Select(measurement => measurement.ElapsedMilliseconds)
                    .OrderBy(value => value)
                    .ToList();
                var midpoint = ordered.Count / 2;
                var median = ordered.Count % 2 == 0
                    ? (ordered[midpoint - 1] + ordered[midpoint]) / 2d
                    : ordered[midpoint];
                return new HeatmapLayoutSampleSummary
                {
                    SamplesMilliseconds = ordered,
                    MedianMilliseconds = median,
                    MaxMilliseconds = ordered.Max(),
                    MaxRealizedButtons = measurements.Max(
                        measurement => measurement.RealizedButtons),
                    MaxRealizedWeekContainers = measurements.Max(
                        measurement => measurement.RealizedWeekContainers)
                };
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

        private static void TestSidebarNavigationReusesDashboardView()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var plugin = new global::PlaytimeInsights.PlaytimeInsights(
                        new FakePlayniteApi(tempRoot));
                    var dashboardItem = plugin.GetSidebarItems()
                        .Single(item => string.Equals(
                            Path.GetFileName(Convert.ToString(item.Icon)),
                            "icon-dashboard.png",
                            StringComparison.OrdinalIgnoreCase));

                    var first = dashboardItem.Opened();
                    var second = dashboardItem.Opened();
                    dashboardItem.Closed();
                    var third = dashboardItem.Opened();

                    Equal(true, ReferenceEquals(first, second));
                    Equal(true, ReferenceEquals(first, third));
                    Equal(true, ReferenceEquals(first.DataContext, second.DataContext));
                    Equal(true, ReferenceEquals(first.DataContext, third.DataContext));
                });
            });
        }

        private static void TestDashboardReentryPreservesVisualTree()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var plugin = new global::PlaytimeInsights.PlaytimeInsights(
                        new FakePlayniteApi(tempRoot));
                    var dashboardItem = plugin.GetSidebarItems()
                        .Single(item => string.Equals(
                            Path.GetFileName(Convert.ToString(item.Icon)),
                            "icon-dashboard.png",
                            StringComparison.OrdinalIgnoreCase));

                    var firstView = (PlaytimeInsightsDashboardView)dashboardItem.Opened();
                    LayoutDashboardView(firstView);
                    var firstScroller = FindVisualDescendants<ScrollViewer>(firstView)
                        .Single(scroller => scroller.Name == "DashboardScrollViewer");
                    var firstTreeCount = CountVisualTreeNodes(firstView);
                    Equal(true, firstTreeCount > 0);

                    dashboardItem.Closed();
                    var reopenedView = (PlaytimeInsightsDashboardView)dashboardItem.Opened();
                    LayoutDashboardView(reopenedView);
                    var reopenedScroller = FindVisualDescendants<ScrollViewer>(reopenedView)
                        .Single(scroller => scroller.Name == "DashboardScrollViewer");
                    var reopenedTreeCount = CountVisualTreeNodes(reopenedView);
                    Equal(true, reopenedTreeCount > 0);
                    Equal(firstTreeCount, reopenedTreeCount);

                    Equal(true, ReferenceEquals(firstView, reopenedView));
                    Equal(true, ReferenceEquals(firstScroller, reopenedScroller));
                });
            });
        }

        private static void TestDashboardViewCacheRefreshBoundary()
        {
            var sourceRoot = FindSourceRoot();
            var plugin = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));
            var dashboardView = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            var dashboardViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            var dashboardOpened = ExtractSidebarOpenedBlock(
                plugin,
                "icon-dashboard.png");

            Equal(false, dashboardOpened.Contains("activeDashboard.Refresh()"));
            Equal(false, plugin.Contains(
                "Closed = () => cachedDashboardView = null"));
            Equal(true, plugin.Contains(
                "private PlaytimeInsightsDashboardView cachedDashboardView;"));
            Equal(1, Regex.Matches(
                plugin,
                Regex.Escape("new PlaytimeInsightsDashboardView")).Count);
            Equal(1, Regex.Matches(
                dashboardView,
                Regex.Escape(
                    "Loaded += PlaytimeInsightsDashboardView_Loaded")).Count);
            Equal(true, dashboardViewModel.Contains(
                "Refresh(DashboardRefreshReason.DataReload)"));

            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var instance = new global::PlaytimeInsights.PlaytimeInsights(
                        new FakePlayniteApi(tempRoot));
                    var dashboardItem = instance.GetSidebarItems()
                        .Single(item => string.Equals(
                            Path.GetFileName(Convert.ToString(item.Icon)),
                            "icon-dashboard.png",
                            StringComparison.OrdinalIgnoreCase));
                    var first = dashboardItem.Opened();
                    var second = dashboardItem.Opened();

                    Equal(true, ReferenceEquals(first, second));
                });
            });
        }

        private static void TestDashboardViewLoadedReattaches()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                var loadedCount = 0;
                object lastLoadedSender = null;
                view.Loaded += delegate(object sender, RoutedEventArgs e)
                {
                    loadedCount++;
                    lastLoadedSender = sender;
                };

                Window window = null;
                try
                {
                    window = new Window
                    {
                        Content = view,
                        ShowInTaskbar = false,
                        Width = 640,
                        Height = 480
                    };
                    window.Show();
                    PumpDispatcher();

                    Equal(1, loadedCount);
                    Equal(true, ReferenceEquals(view, lastLoadedSender));

                    window.Content = null;
                    window.Close();
                    PumpDispatcher();

                    window = new Window
                    {
                        Content = view,
                        ShowInTaskbar = false,
                        Width = 640,
                        Height = 480
                    };
                    window.Show();
                    PumpDispatcher();

                    Equal(2, loadedCount);
                    Equal(true, ReferenceEquals(view, lastLoadedSender));
                }
                finally
                {
                    if (window != null)
                    {
                        window.Content = null;
                        window.Close();
                    }
                }
            });
        }

        private static void TestCoverCacheReusesNormalizedPath()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var contract = LoadCoverImageCacheContract();
                    var cache = contract.Create(4);
                    var path = CopyPngTo(tempRoot, "cover.png", "icon-dashboard.png");
                    var aliasDirectory = Path.Combine(tempRoot, "alias");
                    Directory.CreateDirectory(aliasDirectory);
                    var equivalentPath = Path.Combine(
                        aliasDirectory,
                        "..",
                        "cover.png");

                    var samePath = contract.GetOrLoad(cache, path, 96);
                    var normalizedPath = contract.GetOrLoad(
                        cache,
                        equivalentPath,
                        96);
                    Equal(true, ReferenceEquals(samePath, normalizedPath));

                    var converter1 = new CoverImageConverter();
                    var converter2 = new CoverImageConverter();
                    var first = converter1.Convert(
                        path,
                        typeof(BitmapSource),
                        null,
                        CultureInfo.InvariantCulture);
                    var second = converter2.Convert(
                        path,
                        typeof(BitmapSource),
                        null,
                        CultureInfo.InvariantCulture);
                    Equal(true, first != null);
                    Equal(true, ReferenceEquals(first, second));

                    var cacheField = typeof(CoverImageConverter).GetField(
                        "cache",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    Equal(true, cacheField != null);
                    var sharedCache = cacheField.GetValue(null);
                    var capacityField = sharedCache.GetType().GetField(
                        "capacity",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Equal(true, capacityField != null);
                    Equal(512, (int)capacityField.GetValue(sharedCache));
                });
            });
        }

        private static void TestCoverCacheInvalidatesFiles()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var contract = LoadCoverImageCacheContract();
                    var cache = contract.Create(2);
                    var path = CreateGeneratedPng(
                        Path.Combine(tempRoot, "cover.png"),
                        1,
                        1,
                        Color.FromRgb(180, 30, 30));

                    var first = contract.GetOrLoad(cache, path, 96);
                    Equal(true, first != null);

                    var originalLength = new FileInfo(path).Length;
                    CreateGeneratedPng(
                        path,
                        64,
                        64,
                        Color.FromRgb(30, 180, 30));
                    Equal(true, new FileInfo(path).Length != originalLength);
                    var afterLengthChange = contract.GetOrLoad(cache, path, 96);
                    Equal(true, afterLengthChange != null);
                    Equal(true, !ReferenceEquals(first, afterLengthChange));

                    File.SetLastWriteTimeUtc(
                        path,
                        File.GetLastWriteTimeUtc(path).AddSeconds(60));
                    var afterStampChange = contract.GetOrLoad(cache, path, 96);
                    Equal(true, afterStampChange != null);
                    Equal(true, !ReferenceEquals(afterLengthChange, afterStampChange));

                    File.Delete(path);
                    Equal(true, contract.GetOrLoad(cache, path, 96) == null);
                });
            });
        }

        private static void TestCoverCacheWidthsAndLru()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var contract = LoadCoverImageCacheContract();
                    var pathA = CopyPngTo(tempRoot, "a.png", "icon-dashboard.png");
                    var pathB = CopyPngTo(tempRoot, "b.png", "icon-sessions.png");
                    var pathC = CopyPngTo(tempRoot, "c.png", "icon.png");

                    var widths = contract.Create(2);
                    var width96 = contract.GetOrLoad(widths, pathA, 96);
                    var width48 = contract.GetOrLoad(widths, pathA, 48);
                    Equal(true, width96 != null);
                    Equal(true, !ReferenceEquals(width96, width48));

                    var lru = contract.Create(2);
                    var lruA = contract.GetOrLoad(lru, pathA, 96);
                    var lruB = contract.GetOrLoad(lru, pathB, 96);
                    Equal(true, ReferenceEquals(
                        lruA,
                        contract.GetOrLoad(lru, pathA, 96)));
                    var lruC = contract.GetOrLoad(lru, pathC, 96);
                    Equal(true, lruC != null);
                    var lruBReloaded = contract.GetOrLoad(lru, pathB, 96);
                    Equal(true, lruBReloaded != null);
                    Equal(true, !ReferenceEquals(lruB, lruBReloaded));
                });
            });
        }

        private static void TestCoverDecoderReturnsFrozenThumbnail()
        {
            WithTempDirectory(tempRoot =>
            {
                RunOnSta(() =>
                {
                    var contract = LoadCoverImageCacheContract();
                    var cache = contract.Create(2);
                    var path = CopyPngTo(tempRoot, "cover.png", "icon-dashboard.png");

                    var image = contract.GetOrLoad(cache, path, 96);
                    Equal(true, image != null);
                    Equal(true, image.IsFrozen);
                    Equal(true, image.PixelWidth <= 96);

                    File.Delete(path);
                    Equal(true, image.PixelWidth > 0);
                });
            });
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
            var coverCache = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Services",
                "CoverImageCache.cs"));
            var adaptiveTrendChart = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Controls",
                "AdaptiveTrendChart.cs"));
            var visualResources = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Resources",
                "PlaytimeInsightsVisualResources.xaml"));
            Equal(true, Regex.IsMatch(
                dashboard,
                @"<ScrollViewer\s+x:Name=""DashboardScrollViewer""\s+" +
                @"VerticalScrollBarVisibility=""Auto""\s+" +
                @"HorizontalScrollBarVisibility=""Disabled""",
                RegexOptions.CultureInvariant));
            Equal(true, Regex.Matches(
                dashboard,
                "HorizontalScrollBarVisibility=\"Auto\"").Count >= 4);
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
            // The heatmap cell button suppresses the system focus rect exactly once
            // because it draws its own 1 DIP focus outline in OnRender.
            Equal(1, Regex.Matches(
                dashboard,
                Regex.Escape("FocusVisualStyle\" Value=\"{x:Null}\"")).Count);
            Equal(false, Regex.IsMatch(
                dashboard,
                @"MetricCardStyle\}""\s+Margin=""0,0,0,12""",
                RegexOptions.CultureInvariant));
            Equal(true, dashboard.Contains(
                "<Setter Property=\"Foreground\" Value=\"{DynamicResource TextBrush}\" />"));
            Equal(true, dashboard.Contains(
                "<Setter Property=\"BorderBrush\" Value=\"{DynamicResource PanelSeparatorBrush}\" />"));
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
            Equal(true, dashboard.Contains("FontFamily=\"Segoe MDL2 Assets\""));
            Equal(true, dashboard.Contains("Foreground=\"{DynamicResource TextBrush}\""));
            Equal(true, dashboard.Contains(
                "DataContext=\"{Binding PreviousPeriodComparison}\""));
            Equal(true, dashboard.Contains(
                "DataContext=\"{Binding YearOverYearComparison}\""));
            Equal(true, dashboard.Contains("Text=\"{Binding TagText}\""));
            Equal(true, dashboard.Contains("TrendIncreaseBrush"));
            Equal(true, dashboard.Contains("TrendDecreaseBrush"));
            Equal(true, dashboard.Contains("ChartBarBrush"));
            Equal(true, dashboard.Contains("CornerRadius=\"7,7,0,0\""));
            Equal(false, dashboard.Contains("HeatmapEmptyBrush"));
            Equal(false, dashboard.Contains("#FF2A2A2E"));
            Equal(false, visualResources.Contains("<Grid Height=\"4\""));
            Equal(true, visualResources.Contains("RankingEnergyBackgroundBarStyle"));
            Equal(true, dashboard.Contains(
                "Style=\"{StaticResource RankingEnergyBackgroundBarStyle}\""));
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
            Equal(false, adaptiveTrendChart.Contains(
                "Color.FromArgb(220, 35, 37, 44)"));
            Equal(true, adaptiveTrendChart.Contains(
                "ResolveBrush(\"PopupBackgroundBrush\""));
            Equal(true, adaptiveTrendChart.Contains(
                "ResolveBrush(\"PanelSeparatorBrush\""));
            Equal(true, adaptiveTrendChart.Contains(
                "ResolveBrush(\"GlyphBrush\""));
            Equal(true, adaptiveTrendChart.Contains(
                "ResolveBrush(\"ControlBackgroundBrush\""));
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
            var sessionDetailsEnd = dashboard.IndexOf(
                "</ListView>",
                sessionDetailsStart,
                StringComparison.Ordinal);
            Equal(true, sessionDetailsStart >= 0);
            Equal(true, sessionDetailsEnd > sessionDetailsStart);
            var sessionDetailsList = dashboard.Substring(
                sessionDetailsStart,
                sessionDetailsEnd - sessionDetailsStart);
            Equal(
                true,
                sessionDetailsList.Contains(
                    "Image Source=\"{Binding CoverImagePath,"));
            Equal(true, sessionDetailsList.Contains("<ListView.ItemTemplate>"));
            Equal(true, sessionDetailsList.Contains("Width=\"36\""));
            Equal(true, sessionDetailsList.Contains("Height=\"50\""));
            Equal(true, sessionDetailsList.Contains(
                "Style=\"{StaticResource SessionSourceTagStyle}\""));
            Equal(false, sessionDetailsList.Contains("<GridView"));
            Equal(false, dashboard.Contains("RankingBackgroundProgressStyle"));
            Equal(true, dashboard.Contains("Grid.ColumnSpan=\"4\""));
            Equal(false, dashboard.Contains("Margin=\"-8,-5\""));
            Equal(true, visualResources.Contains("Opacity=\"0.10\""));
            Equal(true, visualResources.Contains("x:Name=\"PART_Track\""));
            Equal(true, visualResources.Contains("x:Name=\"PART_Indicator\""));
            Equal(true, dashboard.Contains(
                "Style=\"{StaticResource RankingEnergyBackgroundBarStyle}\""));
            Equal(false, visualResources.Contains("<Grid Height=\"4\""));
            Equal(true, dashboard.Contains(
                "Style=\"{StaticResource AdvancedFilterExpanderStyle}\""));
            Equal(true, dashboard.Contains(
                "Property=\"Foreground\" Value=\"{StaticResource RankingEnergyBrush}\""));
            Equal(true, dashboard.Contains(
                "Property=\"BorderThickness\" Value=\"0,0,0,1\""));
            Equal(true, dashboard.Contains(
                "Margin=\"16,0,4,0\""));
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
                "Visibility=\"{Binding Drilldown.TrendHostVisibility}\""));
            Equal(true, dashboard.Contains(
                "Visibility=\"{Binding Drilldown.DistributionHostVisibility}\""));
            Equal(true, dashboardViewModel.Contains(
                "SessionDetailVisibility = Visibility.Collapsed"));
            Equal(true, dashboardViewModel.Contains(
                "SessionDetailVisibility = Visibility.Visible"));
            Equal(true, dashboard.Contains(
                "Text=\"{Binding CurrentStreakDateText, Mode=OneWay}\""));
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
            Equal(true, coverCache.Contains(
                "CacheOption = BitmapCacheOption.OnLoad"));
            Equal(true, coverCache.Contains(
                "image.DecodePixelWidth = decodePixelWidth"));
            Equal(true, coverCache.Contains(
                "if (!decoded.IsFrozen)"));
            Equal(true, coverCache.Contains(
                "decoded.Freeze();"));
            Equal(true, coverConverter.Contains(
                "private const int DecodePixelWidth = 96"));
            Equal(true, coverConverter.Contains(
                "new CoverImageCache(512)"));
            Equal(false, coverConverter.Contains(
                "private sealed class CoverImageDecoder"));
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

        private static void TestTrendChartThemeResourceContract()
        {
            var sourceRoot = FindSourceRoot();
            var chartSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Controls",
                "AdaptiveTrendChart.cs"));
            var resourcesXaml = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Resources",
                "PlaytimeInsightsVisualResources.xaml"));
            var quote = ((char)34).ToString();

            // The shared dictionary owns the trend colours, and OnRender only
            // resolves them by key.
            foreach (var key in new[]
            {
                "TrendLineBrush",
                "TrendAreaFillBrush",
                "TrendNodeFillBrush"
            })
            {
                Equal(true, resourcesXaml.Contains(
                    "x:Key=" + quote + key + quote));
                Equal(true, chartSource.Contains(
                    "ResolveBrush(" + quote + key + quote));
            }

            // Option A: the node ring is a theme brush, not a named trend
            // resource, so a fixed near-white ring can never be introduced.
            // Checked as a declaration/lookup, not a bare token, so both files
            // may still document why the rejected key does not exist.
            Equal(false, resourcesXaml.Contains(
                "x:Key=" + quote + "TrendNodeRingBrush" + quote));
            Equal(false, chartSource.Contains(
                "ResolveBrush(" + quote + "TrendNodeRingBrush" + quote));
            Equal(false, chartSource.Contains("FallbackTrendNodeRingBrush"));

            // The line and area brushes are gradients, so a Colour-only
            // fallback cannot express them.
            Equal(true, chartSource.Contains(
                "private Brush ResolveBrush(string key, Brush fallback)"));
            Equal(true, chartSource.Contains(
                "private Brush ResolveBrush(string key, Color fallback)"));
            foreach (var fallback in new[]
            {
                "FallbackTrendLineBrush",
                "FallbackTrendAreaBrush",
                "FallbackTrendNodeFillBrush"
            })
            {
                Equal(true, chartSource.Contains(
                    "private static readonly Brush " + fallback));
            }

            // The area fill must stay strong enough to read on the dark module
            // surface. Measured against #1B1C24: the top stop composites to
            // 1.648:1, above the gridline's own 1.525:1. An earlier 0x30 -> 0x18
            // ramp sat at 1.273:1 - fainter than the gridlines painted under it -
            // and had no presence in dark mode.
            Equal(true, resourcesXaml.Contains(
                "Color=" + quote + "#5A3B82F6" + quote));
            Equal(true, resourcesXaml.Contains(
                "Color=" + quote + "#2E5B7CFA" + quote));
            Equal(false, resourcesXaml.Contains("#303B82F6"));
            Equal(false, resourcesXaml.Contains("#185B7CFA"));

            // The frozen fallback must mirror the resource, or the chart changes
            // appearance whenever the dictionary is out of scope.
            Equal(true, chartSource.Contains(
                "Color.FromArgb(90, 59, 130, 246)"));
            Equal(true, chartSource.Contains(
                "Color.FromArgb(46, 91, 124, 250)"));
            Equal(false, chartSource.Contains(
                "Color.FromArgb(48, 59, 130, 246)"));
            Equal(false, chartSource.Contains(
                "Color.FromArgb(24, 91, 124, 250)"));

            var onRender = ExtractSourceBlock(
                chartSource,
                "protected override void OnRender(",
                "protected override void OnMouseMove(");

            // Nothing is rebuilt per frame any more.
            Equal(false, onRender.Contains("new LinearGradientBrush"));
            Equal(false, onRender.Contains("GradientStops.Add"));
            Equal(false, onRender.Contains("Color.FromArgb(102, 63, 140, 255)"));
            Equal(false, onRender.Contains("new SolidColorBrush"));

            // Exactly one closed area geometry, filled exactly once.
            Equal(1, CountSubstring(
                onRender,
                "CreateSmoothGeometry(renderedPoints, plot.Bottom, true)"));
            Equal(1, CountSubstring(
                onRender,
                "DrawGeometry(areaBrush, null, area)"));

            // Normal nodes gain a ring pen but keep the 90-point budget.
            Equal(true, onRender.Contains("renderedItems.Count <= 90"));
            Equal(true, onRender.Contains("renderedItems.Count >= 180"));
            Equal(true, onRender.Contains(
                "DrawEllipse(nodeFillBrush, nodePen, point, 3d, 3d)"));
            Equal(true, onRender.Contains("new Pen(nodeRingBrush, 1.5)"));

            // Normal and hover rings come from one resolved brush.
            Equal(1, CountSubstring(
                onRender,
                "ResolveBrush(" + quote + "ControlBackgroundBrush" + quote));
            Equal(true, onRender.Contains(
                "DrawHover(drawingContext, plot, textBrush, nodeRingBrush)"));

            var drawHover = ExtractSourceBlock(
                chartSource,
                "private void DrawHover(",
                "private static Geometry CreateSmoothGeometry(");
            Equal(true, drawHover.Contains("Brush nodeRingBrush"));
            Equal(true, drawHover.Contains("new Pen(nodeRingBrush, 1)"));
            Equal(false, drawHover.Contains(
                "ResolveBrush(" + quote + "ControlBackgroundBrush" + quote));

            // No fixed light ring constant survives in either render path.
            foreach (var forbidden in new[]
            {
                "Brushes.White",
                "F3F4F6",
                "Color.FromRgb(243, 244, 246)"
            })
            {
                Equal(false, onRender.Contains(forbidden));
                Equal(false, drawHover.Contains(forbidden));
            }
        }

        private static int CountSubstring(string source, string value)
        {
            var count = 0;
            var index = source.IndexOf(value, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = source.IndexOf(
                    value,
                    index + value.Length,
                    StringComparison.Ordinal);
            }

            return count;
        }

        private static void TestTrendAxisMaximumRounding()
        {
            // Ceiling only - no 1/2/5/10 ladder. Sub-hour peaks round up to the
            // next 10 minutes, hour-and-above peaks to the next whole hour, so
            // the midpoint label is always a clean multiple.
            foreach (var sample in new[]
            {
                new { Peak = 0UL, Axis = 0UL },
                new { Peak = 1UL, Axis = 600UL },
                new { Peak = 599UL, Axis = 600UL },
                new { Peak = 600UL, Axis = 600UL },
                new { Peak = 601UL, Axis = 1200UL },
                new { Peak = 3599UL, Axis = 3600UL },
                new { Peak = 3600UL, Axis = 3600UL },
                new { Peak = 3601UL, Axis = 7200UL },
                new { Peak = 13620UL, Axis = 14400UL },
                new { Peak = 18600UL, Axis = 21600UL }
            })
            {
                Equal(
                    sample.Axis,
                    AdaptiveTrendChart.ResolveAxisMaximumSeconds(sample.Peak));
            }
        }

        private static void TestTrendAxisGutterAndLabels()
        {
            RunOnSta(() =>
            {
                // A 3h47m peak rounds up to 4h, so the peak must sit strictly
                // below the top gridline instead of touching it.
                var chart = new AdaptiveTrendChart
                {
                    ItemsSource = new List<PeriodActivityViewModel>
                    {
                        new PeriodActivityViewModel
                        {
                            Label = "a",
                            Seconds = 13620,
                            DurationText = "3 小时 47 分"
                        },
                        new PeriodActivityViewModel
                        {
                            Label = "b",
                            Seconds = 600,
                            DurationText = "10 分钟"
                        }
                    }
                };
                RenderTrendChart(chart);

                var plot = GetPrivatePlotRect(chart);

                // The old geometry started the plot at a fixed 12 DIP with no
                // room for value labels.
                Equal(true, plot.Left > 12d);

                var axisMaximum = GetPrivateAxisMaximum(chart);
                Equal(14400UL, axisMaximum);

                var points = GetPrivatePoints(chart);
                Equal(2, points.Count);

                // Peak normalised against 4h, not against itself.
                var expectedPeakY = plot.Bottom -
                    plot.Height * 13620d / 14400d;
                Equal(true, Math.Abs(points[0].Y - expectedPeakY) < 0.01);
                Equal(true, points[0].Y > plot.Top + 0.5);

                // Wider labels must widen the gutter rather than clip.
                var wide = new AdaptiveTrendChart
                {
                    ItemsSource = new List<PeriodActivityViewModel>
                    {
                        new PeriodActivityViewModel
                        {
                            Label = "a",
                            Seconds = 360000,
                            DurationText = "100 小时"
                        }
                    }
                };
                RenderTrendChart(wide);
                Equal(true, GetPrivatePlotRect(wide).Left >= plot.Left);
            });
        }

        private static Rect GetPrivatePlotRect(AdaptiveTrendChart chart)
        {
            var method = typeof(AdaptiveTrendChart).GetMethod(
                "GetPlotRect",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (Rect)method.Invoke(chart, null);
        }

        private static ulong GetPrivateAxisMaximum(AdaptiveTrendChart chart)
        {
            var field = typeof(AdaptiveTrendChart).GetField(
                "axisMaximumSeconds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (ulong)field.GetValue(chart);
        }

        private static IList<Point> GetPrivatePoints(AdaptiveTrendChart chart)
        {
            var field = typeof(AdaptiveTrendChart).GetField(
                "renderedPoints",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (IList<Point>)field.GetValue(chart);
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

        private static void TestResponsiveMetricPanelRemeasuresForArrangeWidth()
        {
            RunOnSta(() =>
            {
                var panel = new ResponsiveUniformPanel();
                var child = new WidthSensitiveElement(295, 20, 100);
                panel.Children.Add(child);
                panel.Children.Add(new WidthSensitiveElement(295, 20, 100));
                panel.Children.Add(new WidthSensitiveElement(295, 20, 100));

                panel.Measure(new Size(640, double.PositiveInfinity));
                Equal(20d, child.DesiredSize.Height);

                panel.Arrange(new Rect(0, 0, 900, 300));
                panel.UpdateLayout();

                Equal(292d, GetLayoutSlot(child).Width);
                Equal(100d, GetLayoutSlot(child).Height);
            });
        }

        private static void TestAdaptiveDashboardPanelNarrow()
        {
            RunOnSta(() =>
            {
                var panel = new AdaptiveDashboardPanel();
                var collapsed = CreateDashboardPanelChild(
                    300,
                    DashboardLayoutZone.Primary);
                collapsed.Visibility = Visibility.Collapsed;
                panel.Children.Add(collapsed);

                var heights = new[] { 100d, 120d, 260d, 80d };
                foreach (var height in heights)
                {
                    panel.Children.Add(
                        CreateDashboardPanelChild(
                            height,
                            DashboardLayoutZone.Primary));
                }

                LayoutAdaptivePanel(panel, 900);

                Equal(false, panel.IsWideLayout);
                Equal(900d, panel.DesiredSize.Width);
                Equal(614d, panel.DesiredSize.Height);
                Equal(new Rect(0, 0, 0, 0), GetLayoutSlot(collapsed));

                var expectedY = 0d;
                for (var i = 1; i < panel.Children.Count; i++)
                {
                    var slot = GetLayoutSlot(panel.Children[i]);
                    Equal(0d, slot.X);
                    Equal(900d, slot.Width);
                    Equal(expectedY, slot.Y);
                    expectedY += slot.Height + 18d;
                }

                Equal(614d, expectedY - 18d);
            });
        }

        private static void TestAdaptiveDashboardPanelWide()
        {
            RunOnSta(() =>
            {
                var panel = new AdaptiveDashboardPanel();
                var collapsed = CreateDashboardPanelChild(
                    300,
                    DashboardLayoutZone.Primary);
                collapsed.Visibility = Visibility.Collapsed;
                panel.Children.Add(collapsed);
                panel.Children.Add(
                    CreateDashboardPanelChild(100, DashboardLayoutZone.Primary));
                panel.Children.Add(
                    CreateDashboardPanelChild(260, DashboardLayoutZone.Secondary));
                panel.Children.Add(
                    CreateDashboardPanelChild(120, DashboardLayoutZone.Primary));
                panel.Children.Add(
                    CreateDashboardPanelChild(80, DashboardLayoutZone.Secondary));

                LayoutAdaptivePanel(panel, 1400);

                Equal(true, panel.IsWideLayout);
                Equal(358d, panel.DesiredSize.Height);
                Equal(new Rect(0, 0, 0, 0), GetLayoutSlot(collapsed));

                var primary0 = GetLayoutSlot(panel.Children[1]);
                var secondary0 = GetLayoutSlot(panel.Children[2]);
                var primary1 = GetLayoutSlot(panel.Children[3]);
                var secondary1 = GetLayoutSlot(panel.Children[4]);

                Equal(true, Math.Abs(primary0.X - 0d) < 0.01);
                Equal(true, Math.Abs(primary0.Width - 856.84d) < 0.01);
                Equal(true, Math.Abs(primary0.Height - 100d) < 0.01);
                Equal(true, Math.Abs(primary0.Y - 0d) < 0.01);

                Equal(true, Math.Abs(secondary0.X - 874.84d) < 0.01);
                Equal(true, Math.Abs(secondary0.Width - 525.16d) < 0.01);
                Equal(true, Math.Abs(secondary0.Height - 260d) < 0.01);
                Equal(true, Math.Abs(secondary0.Y - 0d) < 0.01);

                Equal(true, Math.Abs(primary1.X - 0d) < 0.01);
                Equal(true, Math.Abs(primary1.Width - 856.84d) < 0.01);
                Equal(true, Math.Abs(primary1.Height - 120d) < 0.01);
                Equal(true, Math.Abs(primary1.Y - 118d) < 0.01);

                Equal(true, Math.Abs(secondary1.X - 874.84d) < 0.01);
                Equal(true, Math.Abs(secondary1.Width - 525.16d) < 0.01);
                Equal(true, Math.Abs(secondary1.Height - 80d) < 0.01);
                Equal(true, Math.Abs(secondary1.Y - 278d) < 0.01);

                Equal(true, Math.Abs(primary0.X - secondary0.X) > 0.01);
            });
        }

        private static void TestAdaptiveDashboardPanelHysteresis()
        {
            RunOnSta(() =>
            {
                var panel = new AdaptiveDashboardPanel();
                panel.Children.Add(
                    CreateDashboardPanelChild(100, DashboardLayoutZone.Primary));

                Equal(1200d, panel.EnterWideWidth);
                Equal(1160d, panel.ExitWideWidth);
                Equal(0.38d, panel.SecondaryColumnRatio);
                Equal(18d, panel.ColumnSpacing);
                Equal(18d, panel.VerticalSpacing);
                Equal(false, panel.IsWideLayout);

                LayoutAdaptivePanel(panel, 1199);
                Equal(false, panel.IsWideLayout);
                LayoutAdaptivePanel(panel, 1200);
                Equal(true, panel.IsWideLayout);
                LayoutAdaptivePanel(panel, 1180);
                Equal(true, panel.IsWideLayout);
                LayoutAdaptivePanel(panel, 1160);
                Equal(true, panel.IsWideLayout);
                LayoutAdaptivePanel(panel, 1159);
                Equal(false, panel.IsWideLayout);
            });
        }

        private static void TestVisibleDrilldownModuleRemeasures()
        {
            RunOnSta(() =>
            {
                var panel = new AdaptiveDashboardPanel
                {
                    EnterWideWidth = 1200,
                    ExitWideWidth = 1160
                };
                var module = new Border
                {
                    Visibility = Visibility.Collapsed,
                    Padding = new Thickness(18)
                };
                AdaptiveDashboardPanel.SetZone(
                    module,
                    DashboardLayoutZone.Primary);
                var content = new StackPanel();
                content.Children.Add(new TextBlock
                {
                    Text = "Drilldown",
                    Height = 32
                });
                var list = new ListView
                {
                    ItemsSource = new ObservableCollection<string>(
                        Enumerable.Range(1, 100)
                            .Select(index => "Session " + index)),
                    MaxHeight = 380,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                ScrollViewer.SetCanContentScroll(list, true);
                VirtualizingPanel.SetIsVirtualizing(list, true);
                VirtualizingPanel.SetVirtualizationMode(
                    list,
                    VirtualizationMode.Recycling);
                content.Children.Add(list);
                module.Child = content;
                panel.Children.Add(module);

                LayoutAdaptivePanel(panel, 1400);
                module.Visibility = Visibility.Visible;
                LayoutAdaptivePanel(panel, 1400);

                Equal(true, module.DesiredSize.Height > 300);
                Equal(true, list.DesiredSize.Height > 0);
            });
        }

        private static void TestDrilldownHeaderGapIsCompact()
        {
            RunOnSta(() =>
            {
                var settings =
                    (PlaytimeInsightsSettingsViewModel)
                    System.Runtime.Serialization.FormatterServices
                        .GetUninitializedObject(
                            typeof(PlaytimeInsightsSettingsViewModel));
                settings.Settings = new PlaytimeInsightsSettings();
                var viewModel = new DashboardViewModel(
                    null,
                    null,
                    new AnalyticsService(),
                    new SessionQueryService(new TestGameMetadataAccessor()),
                    settings);
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1400,
                    DataContext = viewModel
                };
                view.Measure(new Size(1400, 680));
                view.Arrange(new Rect(0, 0, 1400, 680));
                view.UpdateLayout();

                var session = new GameSession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Drilldown Game",
                    StartedAtUtc = new DateTime(
                        2026,
                        8,
                        10,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc),
                    EndedAtUtc = new DateTime(
                        2026,
                        8,
                        10,
                        10,
                        1,
                        0,
                        DateTimeKind.Utc),
                    ElapsedSeconds = 60,
                    StartUtcOffsetMinutes = 0,
                    EndUtcOffsetMinutes = 0,
                    TimeZoneId = "UTC",
                    Source = SessionSource.Manual
                };
                viewModel.Drilldown.ResetContext(
                    new Playnite.SDK.Models.Game[0],
                    new[] { session });
                viewModel.Drilldown.SelectPeriod(
                    new PeriodActivityViewModel
                    {
                        PeriodStart = new DateTime(2026, 8, 10),
                        PeriodEnd = new DateTime(2026, 8, 10),
                        Label = "2026/8/10",
                        DurationText = "1 分钟"
                    });
                view.UpdateLayout();

                var module = (ContentControl)view.FindName("TrendDrilldownHost");
                var list = FindVisualDescendants<ListView>(module).Single();
                var firstItem = (ListViewItem)list.ItemContainerGenerator
                    .ContainerFromIndex(0);
                var count = FindVisualDescendants<TextBlock>(module)
                    .First(text => text.Text == viewModel.SessionDetailCountText);
                var divider = FindVisualDescendants<Border>(module)
                    .Single(border => border.Height == 1);

                Func<FrameworkElement, double> topOf = element =>
                    element.TransformToAncestor(module)
                        .Transform(new Point(0, 0)).Y;
                var countBottom = topOf(count) + count.RenderSize.Height;
                var dividerTop = topOf(divider);
                var dividerBottom = dividerTop + divider.RenderSize.Height;
                var firstItemTop = topOf(firstItem);

                Equal(1d, divider.RenderSize.Height);
                Equal(true, divider.Background is SolidColorBrush);
                Equal(
                    Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF),
                    ((SolidColorBrush)divider.Background).Color);
                Equal(true, dividerTop - countBottom <= 7d);
                Equal(true, firstItemTop - dividerBottom <= 8d);
            });
        }

        private static void TestBoundDashboardDrilldownExpands()
        {
            RunOnSta(() =>
            {
                var settings =
                    (PlaytimeInsightsSettingsViewModel)
                    System.Runtime.Serialization.FormatterServices
                        .GetUninitializedObject(
                            typeof(PlaytimeInsightsSettingsViewModel));
                settings.Settings = new PlaytimeInsightsSettings();
                var viewModel = new DashboardViewModel(
                    null,
                    null,
                    new AnalyticsService(),
                    new SessionQueryService(new TestGameMetadataAccessor()),
                    settings);
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1400,
                    DataContext = viewModel
                };
                view.Measure(new Size(1400, 680));
                view.Arrange(new Rect(0, 0, 1400, 680));
                view.UpdateLayout();

                var session = new GameSession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Drilldown Game",
                    StartedAtUtc = new DateTime(
                        2026,
                        8,
                        10,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc),
                    EndedAtUtc = new DateTime(
                        2026,
                        8,
                        10,
                        10,
                        1,
                        0,
                        DateTimeKind.Utc),
                    ElapsedSeconds = 60,
                    StartUtcOffsetMinutes = 0,
                    EndUtcOffsetMinutes = 0,
                    TimeZoneId = "UTC",
                    Source = SessionSource.Manual
                };
                viewModel.Drilldown.ResetContext(
                    new Playnite.SDK.Models.Game[0],
                    new[] { session });
                viewModel.Drilldown.SelectPeriod(
                    new PeriodActivityViewModel
                    {
                        PeriodStart = new DateTime(2026, 8, 10),
                        PeriodEnd = new DateTime(2026, 8, 10),
                        Label = "2026/8/10",
                        DurationText = "1 分钟"
                    });
                view.UpdateLayout();

                var module = (ContentControl)view.FindName("TrendDrilldownHost");
                var list = FindVisualDescendants<ListView>(module).Single();
                Equal(Visibility.Visible, module.Visibility);
                Equal(true, module.ActualHeight > 100);
                Equal(true, list.ActualHeight > 0);
                Equal(1, list.Items.Count);
            });
        }

        private static void TestDashboardDrilldownAnchors()
        {
            var drilldown = new DashboardDrilldownViewModel(
                null,
                new AnalyticsService());
            var type = drilldown.GetType();
            var anchorProperty = type.GetProperty("SelectedAnchor");
            var trendVisibilityProperty = type.GetProperty(
                "TrendHostVisibility");
            var distributionVisibilityProperty = type.GetProperty(
                "DistributionHostVisibility");

            Equal(true, anchorProperty != null);
            Equal(true, trendVisibilityProperty != null);
            Equal(true, distributionVisibilityProperty != null);
            Equal("None", anchorProperty.GetValue(drilldown).ToString());
            Equal(
                Visibility.Collapsed,
                (Visibility)trendVisibilityProperty.GetValue(drilldown));
            Equal(
                Visibility.Collapsed,
                (Visibility)distributionVisibilityProperty.GetValue(drilldown));

            drilldown.ResetContext(
                new Playnite.SDK.Models.Game[0],
                new GameSession[0]);
            drilldown.SelectPeriod(new PeriodActivityViewModel
            {
                PeriodStart = new DateTime(2026, 8, 10),
                PeriodEnd = new DateTime(2026, 8, 10),
                Label = "2026/8/10",
                DurationText = "0 分钟"
            });

            Equal("Trend", anchorProperty.GetValue(drilldown).ToString());
            Equal(
                Visibility.Visible,
                (Visibility)trendVisibilityProperty.GetValue(drilldown));
            Equal(
                Visibility.Collapsed,
                (Visibility)distributionVisibilityProperty.GetValue(drilldown));
            Equal(Visibility.Visible, drilldown.SessionDetailVisibility);

            drilldown.SelectHeatmapDate(new HeatmapCellViewModel
            {
                Date = new DateTime(2026, 8, 11),
                CellVisibility = Visibility.Visible
            });

            Equal(
                "Distribution",
                anchorProperty.GetValue(drilldown).ToString());
            Equal(
                Visibility.Collapsed,
                (Visibility)trendVisibilityProperty.GetValue(drilldown));
            Equal(
                Visibility.Visible,
                (Visibility)distributionVisibilityProperty.GetValue(drilldown));

            drilldown.ResetSelection();
            Equal("None", anchorProperty.GetValue(drilldown).ToString());
            Equal(Visibility.Collapsed, drilldown.SessionDetailVisibility);
            Equal(
                Visibility.Collapsed,
                (Visibility)trendVisibilityProperty.GetValue(drilldown));
            Equal(
                Visibility.Collapsed,
                (Visibility)distributionVisibilityProperty.GetValue(drilldown));
        }

        private static void TestDashboardDrilldownHostLayout()
        {
            RunOnSta(() =>
            {
                foreach (var viewWidth in new[] { 1248d, 900d })
                {
                var settings =
                    (PlaytimeInsightsSettingsViewModel)
                    System.Runtime.Serialization.FormatterServices
                        .GetUninitializedObject(
                            typeof(PlaytimeInsightsSettingsViewModel));
                settings.Settings = new PlaytimeInsightsSettings();
                var viewModel = new DashboardViewModel(
                    null,
                    null,
                    new AnalyticsService(),
                    new SessionQueryService(new TestGameMetadataAccessor()),
                    settings);
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = viewWidth,
                    DataContext = viewModel
                };
                view.Measure(new Size(viewWidth, 680));
                view.Arrange(new Rect(0, 0, viewWidth, 680));
                view.UpdateLayout();

                var panel = FindVisualDescendants<AdaptiveDashboardPanel>(view)
                    .Single();
                Equal(viewWidth >= 1248d, panel.IsWideLayout);
                var trend = (FrameworkElement)view.FindName("TrendModule");
                var distribution =
                    (FrameworkElement)view.FindName("DistributionModule");
                var trendHost =
                    (ContentControl)view.FindName("TrendDrilldownHost");
                var distributionHost =
                    (ContentControl)view.FindName("DistributionDrilldownHost");

                Equal(true, trendHost != null);
                Equal(true, distributionHost != null);
                Equal(Visibility.Collapsed, trendHost.Visibility);
                Equal(Visibility.Collapsed, distributionHost.Visibility);
                Equal<object>(null, trendHost.Content);
                Equal<object>(null, distributionHost.Content);
                Equal(
                    DashboardLayoutZone.Primary,
                    AdaptiveDashboardPanel.GetZone(trendHost));
                Equal(
                    DashboardLayoutZone.Primary,
                    AdaptiveDashboardPanel.GetZone(distributionHost));

                viewModel.Drilldown.ResetContext(
                    new Playnite.SDK.Models.Game[0],
                    new GameSession[0]);
                viewModel.Drilldown.SelectPeriod(new PeriodActivityViewModel
                {
                    PeriodStart = new DateTime(2026, 8, 10),
                    PeriodEnd = new DateTime(2026, 8, 10),
                    Label = "2026/8/10",
                    DurationText = "0 分钟"
                });
                view.UpdateLayout();

                Equal(Visibility.Visible, trendHost.Visibility);
                Equal(Visibility.Collapsed, distributionHost.Visibility);
                Equal(viewModel, trendHost.Content);
                Equal<object>(null, distributionHost.Content);
                var trendBounds = trend.TransformToAncestor(panel)
                    .TransformBounds(new Rect(trend.RenderSize));
                var trendHostBounds = trendHost.TransformToAncestor(panel)
                    .TransformBounds(new Rect(trendHost.RenderSize));
                Equal(true, Math.Abs(trendBounds.X - trendHostBounds.X) < 0.01);
                Equal(true, trendHostBounds.Top >= trendBounds.Bottom + 17.9d);

                viewModel.Drilldown.SelectHeatmapDate(
                    new HeatmapCellViewModel
                    {
                        Date = new DateTime(2026, 8, 11),
                        CellVisibility = Visibility.Visible
                    });
                view.UpdateLayout();

                Equal(Visibility.Collapsed, trendHost.Visibility);
                Equal(Visibility.Visible, distributionHost.Visibility);
                Equal<object>(null, trendHost.Content);
                Equal(viewModel, distributionHost.Content);
                var distributionBounds = distribution.TransformToAncestor(panel)
                    .TransformBounds(new Rect(distribution.RenderSize));
                var distributionHostBounds = distributionHost
                    .TransformToAncestor(panel)
                    .TransformBounds(new Rect(distributionHost.RenderSize));
                Equal(
                    true,
                    Math.Abs(distributionBounds.X - distributionHostBounds.X) <
                        0.01);
                Equal(
                    true,
                    distributionHostBounds.Top >=
                        distributionBounds.Bottom + 17.9d);
                }
            });
        }

        private static void TestDashboardDrilldownViewportBounds()
        {
            var method = typeof(PlaytimeInsightsDashboardView).GetMethod(
                "IsVerticalBandVisible",
                BindingFlags.NonPublic | BindingFlags.Static);
            Equal(true, method != null);

            Func<double, double, double, bool> invoke =
                (top, height, viewportHeight) => (bool)method.Invoke(
                    null,
                    new object[] { top, height, viewportHeight });
            Equal(true, invoke(0d, 96d, 400d));
            Equal(true, invoke(304d, 96d, 400d));
            Equal(false, invoke(305d, 96d, 400d));
            Equal(false, invoke(-1d, 96d, 400d));
            Equal(false, invoke(0d, 96d, 0d));
        }

        private static void TestDashboardDrilldownViewportReveal()
        {
            RunOnSta(() =>
            {
                foreach (var viewWidth in new[] { 900d, 1248d })
                {
                var viewModel = CreateDashboardViewModelForLayout();
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = viewWidth,
                    Height = 420,
                    DataContext = viewModel
                };
                var scrollViewer = (ScrollViewer)view.FindName(
                    "DashboardScrollViewer");
                view.Measure(new Size(viewWidth, 420));
                view.Arrange(new Rect(0, 0, viewWidth, 420));
                view.UpdateLayout();
                var adaptivePanel =
                    FindVisualDescendants<AdaptiveDashboardPanel>(view)
                        .Single();
                Equal(viewWidth == 1248d, adaptivePanel.IsWideLayout);

                viewModel.Drilldown.ResetContext(
                    new Playnite.SDK.Models.Game[0],
                    new GameSession[0]);
                viewModel.Drilldown.SelectPeriod(new PeriodActivityViewModel
                {
                    PeriodStart = new DateTime(2026, 8, 10),
                    PeriodEnd = new DateTime(2026, 8, 10),
                    Label = "2026/8/10",
                    DurationText = "0 分钟"
                });
                PumpDispatcher();
                view.UpdateLayout();
                var host = (FrameworkElement)view.FindName(
                    "TrendDrilldownHost");
                Equal(Visibility.Visible, host.Visibility);
                Equal(true, host.ActualHeight >= 96d);

                var handler = typeof(PlaytimeInsightsDashboardView)
                    .GetMethod(
                        "DrilldownHost_IsVisibleChanged",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Equal(true, handler != null);
                Equal(true, scrollViewer.ViewportHeight > 96d);
                Equal(true, scrollViewer.ScrollableHeight > 0d);

                Action invokeReveal = () =>
                {
                    handler.Invoke(
                        view,
                        new object[]
                        {
                            host,
                            new DependencyPropertyChangedEventArgs(
                                UIElement.IsVisibleProperty,
                                false,
                                true)
                        });
                    PumpDispatcher();
                    view.UpdateLayout();
                };
                Func<Rect> headerBounds = () => host
                    .TransformToAncestor(scrollViewer)
                    .TransformBounds(new Rect(
                        0d,
                        0d,
                        host.ActualWidth,
                        96d));
                Func<double> expectedOffset = () =>
                {
                    var bounds = headerBounds();
                    var delta = bounds.Top < 0d
                        ? bounds.Top
                        : bounds.Bottom > scrollViewer.ViewportHeight
                            ? bounds.Bottom - scrollViewer.ViewportHeight
                            : 0d;
                    return Math.Max(
                        0d,
                        Math.Min(
                            scrollViewer.ScrollableHeight,
                            scrollViewer.VerticalOffset + delta));
                };

                scrollViewer.ScrollToVerticalOffset(0d);
                view.UpdateLayout();
                var currentBounds = headerBounds();
                scrollViewer.ScrollToVerticalOffset(
                    scrollViewer.VerticalOffset + currentBounds.Top - 24d);
                view.UpdateLayout();
                var visibleOffset = scrollViewer.VerticalOffset;
                var visibleBounds = headerBounds();
                Equal(true, visibleBounds.Top >= 0d);
                Equal(
                    true,
                    visibleBounds.Bottom <= scrollViewer.ViewportHeight);
                invokeReveal();
                Equal(
                    true,
                    Math.Abs(scrollViewer.VerticalOffset - visibleOffset) <
                        0.01d);

                scrollViewer.ScrollToVerticalOffset(0d);
                view.UpdateLayout();
                Equal(
                    true,
                    headerBounds().Bottom > scrollViewer.ViewportHeight);
                var belowExpectedOffset = expectedOffset();
                invokeReveal();
                Equal(
                    true,
                    Math.Abs(
                        scrollViewer.VerticalOffset - belowExpectedOffset) <
                        0.01d);
                var revealedBounds = headerBounds();
                Equal(true, revealedBounds.Top >= 0d);
                Equal(
                    true,
                    revealedBounds.Bottom <= scrollViewer.ViewportHeight);

                scrollViewer.ScrollToVerticalOffset(
                    scrollViewer.VerticalOffset + revealedBounds.Top + 1d);
                view.UpdateLayout();
                var clippedOffset = scrollViewer.VerticalOffset;
                Equal(true, headerBounds().Top < 0d);
                var aboveExpectedOffset = expectedOffset();
                invokeReveal();
                Equal(
                    true,
                    Math.Abs(
                        scrollViewer.VerticalOffset - aboveExpectedOffset) <
                        0.01d);
                Equal(true, Math.Abs(clippedOffset - aboveExpectedOffset) > 0.01d);
                }
            });
        }

        private static void TestDashboardDrilldownAutomationName()
        {
            RunOnSta(() =>
            {
                var settings =
                    (PlaytimeInsightsSettingsViewModel)
                    System.Runtime.Serialization.FormatterServices
                        .GetUninitializedObject(
                            typeof(PlaytimeInsightsSettingsViewModel));
                settings.Settings = new PlaytimeInsightsSettings();
                var viewModel = new DashboardViewModel(
                    null,
                    null,
                    new AnalyticsService(),
                    new SessionQueryService(new TestGameMetadataAccessor()),
                    settings);
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1400,
                    DataContext = viewModel
                };
                view.Measure(new Size(1400, 680));
                view.Arrange(new Rect(0, 0, 1400, 680));
                view.UpdateLayout();

                viewModel.Drilldown.ResetContext(
                    new Playnite.SDK.Models.Game[0],
                    new GameSession[0]);
                viewModel.Drilldown.SelectPeriod(new PeriodActivityViewModel
                {
                    PeriodStart = new DateTime(2026, 8, 10),
                    PeriodEnd = new DateTime(2026, 8, 10),
                    Label = "2026/8/10",
                    DurationText = "0 分钟"
                });
                view.UpdateLayout();
                PumpDispatcher();

                var host = (ContentControl)view.FindName(
                    "TrendDrilldownHost");
                Equal(
                    viewModel.SelectedDetailTitle,
                    System.Windows.Automation.AutomationProperties.GetName(
                        host));
                Equal(
                    true,
                    typeof(PlaytimeInsightsDashboardView).GetMethod(
                        "RaiseDrilldownAutomationNameChanged",
                        BindingFlags.NonPublic | BindingFlags.Instance) != null);
                var lastNameField = typeof(PlaytimeInsightsDashboardView)
                    .GetField(
                        "lastDrilldownAutomationName",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Equal(true, lastNameField != null);
                Equal(
                    viewModel.SelectedDetailTitle,
                    (string)lastNameField.GetValue(view));

                viewModel.Drilldown.SelectHeatmapDate(
                    new HeatmapCellViewModel
                    {
                        Date = new DateTime(2026, 8, 11),
                        CellVisibility = Visibility.Visible
                    });
                view.UpdateLayout();
                PumpDispatcher();
                var distributionHost = (ContentControl)view.FindName(
                    "DistributionDrilldownHost");
                Equal(
                    viewModel.SelectedDetailTitle,
                    System.Windows.Automation.AutomationProperties.GetName(
                        distributionHost));
                Equal(
                    viewModel.SelectedDetailTitle,
                    (string)lastNameField.GetValue(view));

                viewModel.Drilldown.ResetSelection();
                view.UpdateLayout();
                PumpDispatcher();
                Equal(
                    viewModel.SelectedDetailTitle,
                    (string)lastNameField.GetValue(view));
            });
        }

        private static Border CreateDashboardPanelChild(
            double height,
            DashboardLayoutZone zone)
        {
            var child = new Border
            {
                Height = height
            };
            AdaptiveDashboardPanel.SetZone(child, zone);
            return child;
        }

        private static void LayoutAdaptivePanel(
            AdaptiveDashboardPanel panel,
            double width)
        {
            panel.Measure(new Size(width, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
            panel.UpdateLayout();
        }

        private static void TestDashboardEntrancePlanBehavior()
        {
            var full = DashboardEntrancePlan.Create(
                DashboardPresentationTransition.Full,
                true);
            Equal(5, full.Steps.Count);
            Equal("MetricCardsHost", full.Steps[0].HostName);
            Equal(0d, full.Steps[0].DelayMilliseconds);
            Equal(160d, full.Steps[0].DurationMilliseconds);
            Equal(6d, full.Steps[0].OffsetY);
            Equal("TrendModule", full.Steps[1].HostName);
            Equal(24d, full.Steps[1].DelayMilliseconds);
            Equal("RankingModule", full.Steps[2].HostName);
            Equal(24d, full.Steps[2].DelayMilliseconds);
            Equal("DistributionModule", full.Steps[3].HostName);
            Equal(48d, full.Steps[3].DelayMilliseconds);
            Equal("AnomalyModule", full.Steps[4].HostName);
            Equal(48d, full.Steps[4].DelayMilliseconds);
            Equal(5d, full.Steps[1].OffsetY);
            Equal(5d, full.Steps[2].OffsetY);
            Equal(5d, full.Steps[3].OffsetY);
            Equal(5d, full.Steps[4].OffsetY);

            var trend = DashboardEntrancePlan.Create(
                DashboardPresentationTransition.Trend,
                true);
            Equal(1, trend.Steps.Count);
            Equal("TrendModule", trend.Steps[0].HostName);
            Equal(0d, trend.Steps[0].DelayMilliseconds);
            Equal(140d, trend.Steps[0].DurationMilliseconds);
            Equal(4d, trend.Steps[0].OffsetY);

            var ranking = DashboardEntrancePlan.Create(
                DashboardPresentationTransition.Ranking,
                true);
            Equal(1, ranking.Steps.Count);
            Equal("RankingModule", ranking.Steps[0].HostName);
            Equal(0d, ranking.Steps[0].DelayMilliseconds);
            Equal(140d, ranking.Steps[0].DurationMilliseconds);
            Equal(4d, ranking.Steps[0].OffsetY);

            var none = DashboardEntrancePlan.Create(
                DashboardPresentationTransition.None,
                true);
            Equal(0, none.Steps.Count);

            var reduced = DashboardEntrancePlan.Create(
                DashboardPresentationTransition.Full,
                false);
            Equal(5, reduced.Steps.Count);
            Equal(true, reduced.Steps.All(step =>
                step.DelayMilliseconds == 0 &&
                step.DurationMilliseconds == 0 &&
                step.OffsetY == 0));
        }

        private static void TestDashboardPresentationSignalContract()
        {
            var sourceRoot = FindSourceRoot();
            var source = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            Equal(true, source.Contains(
                "enum DashboardPresentationTransition"));
            Equal(true, source.Contains(
                "DashboardPresentationTransition PresentationTransition"));
            Equal(true, source.Contains(
                "int PresentationRevision"));
            Equal(true, source.Contains(
                "PublishPresentationUpdate"));
            Equal(true, source.Contains(
                "PresentationRevision"));
            Equal(true, source.Contains(
                "plan.Mode"));

            var settings =
                (PlaytimeInsightsSettingsViewModel)
                System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(
                        typeof(PlaytimeInsightsSettingsViewModel));
            settings.Settings = new PlaytimeInsightsSettings();
            var viewModel = new DashboardViewModel(
                null,
                null,
                new AnalyticsService(),
                new SessionQueryService(new TestGameMetadataAccessor()),
                settings);
            Equal(0, viewModel.PresentationRevision);

            var refreshMethod = typeof(DashboardViewModel).GetMethod(
                "Refresh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(DashboardRefreshReason) },
                null);
            Equal(true, refreshMethod != null);
            viewModel.SelectedMetadataDimensionOption = null;
            refreshMethod.Invoke(
                viewModel,
                new object[] { DashboardRefreshReason.DataReload });
            Equal(0, viewModel.PresentationRevision);

            var publishMethod = typeof(DashboardViewModel).GetMethod(
                "PublishPresentationUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Equal(true, publishMethod != null);
            var revision = viewModel.PresentationRevision;
            publishMethod.Invoke(
                viewModel,
                new object[] { DashboardPresentationTransition.Trend });
            Equal(DashboardPresentationTransition.Trend,
                viewModel.PresentationTransition);
            Equal(revision + 1, viewModel.PresentationRevision);
            publishMethod.Invoke(
                viewModel,
                new object[] { DashboardPresentationTransition.Ranking });
            Equal(DashboardPresentationTransition.Ranking,
                viewModel.PresentationTransition);
            Equal(revision + 2, viewModel.PresentationRevision);
        }

        private static void TestDashboardEntranceHostContract()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboard = File.ReadAllText(dashboardPath);
            var codeBehind = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));

            foreach (var hostName in new[]
            {
                "MetricCardsHost",
                "TrendModule",
                "RankingModule",
                "DistributionModule",
                "AnomalyModule"
            })
            {
                Equal(true, dashboard.Contains(
                    "x:Name=\"" + hostName + "\""));
            }

            Equal(true, codeBehind.Contains("DataContextChanged"));
            Equal(true, codeBehind.Contains("PresentationRevision"));
            Equal(true, codeBehind.Contains("DispatcherPriority.Loaded"));
            Equal(true, codeBehind.Contains(
                "SystemParameters.ClientAreaAnimation"));
            Equal(true, codeBehind.Contains(
                "DashboardEntrancePlan.Create"));
            Equal(true, codeBehind.Contains("BeginAnimation"));
            Equal(true, codeBehind.Contains("FillBehavior.Stop"));
            Equal(true, codeBehind.Contains(
                "HandoffBehavior.SnapshotAndReplace"));

            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1200
                };
                view.Measure(new Size(1200, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                view.UpdateLayout();

                var metricHost = (FrameworkElement)view.FindName(
                    "MetricCardsHost");
                Equal(true, metricHost != null);
                foreach (var hostName in new[]
                {
                    "TrendModule",
                    "RankingModule",
                    "DistributionModule",
                    "AnomalyModule"
                })
                {
                    Equal(true, view.FindName(hostName) is FrameworkElement);
                }

                var hosts = new[]
                {
                    metricHost,
                    (FrameworkElement)view.FindName("TrendModule"),
                    (FrameworkElement)view.FindName("RankingModule"),
                    (FrameworkElement)view.FindName("DistributionModule"),
                    (FrameworkElement)view.FindName("AnomalyModule")
                };
                foreach (var host in hosts)
                {
                    var transform = host.RenderTransform as TranslateTransform;
                    Equal(true, transform != null);
                    Equal(0d, transform.Y);
                    Equal(1d, host.Opacity);
                }
            });
        }

        private static void TestEntranceAnimationKeepsFinalBaseValues()
        {
            RunOnSta(() =>
            {
                var playStep = typeof(PlaytimeInsightsDashboardView).GetMethod(
                    "PlayEntranceStep",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Equal(true, playStep != null);
                var host = new Border
                {
                    Width = 120,
                    Height = 60,
                    RenderTransform = new TranslateTransform(0, 0)
                };
                playStep.Invoke(
                    null,
                    new object[]
                    {
                        host,
                        new DashboardEntranceStep("TestHost", 0d, 160d, 6d),
                        true
                    });
                Equal(1d, host.Opacity);
                Equal(0d, ((TranslateTransform)host.RenderTransform).Y);
                Equal(true, host.HasAnimatedProperties);
            });
        }

        private static void TestDrilldownRecyclingKeepsTransformZero()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                view.Measure(new Size(1200, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                view.UpdateLayout();
                var drilldownModule = (FrameworkElement)
                    ((DataTemplate)view.Resources["DrilldownCardTemplate"])
                        .LoadContent();
                drilldownModule.Measure(new Size(520, 500));
                drilldownModule.Arrange(new Rect(
                    0,
                    0,
                    520,
                    drilldownModule.DesiredSize.Height));
                var template = FindVisualDescendants<ListView>(
                    drilldownModule).Single().ItemTemplate;

                var items = Enumerable.Range(0, 200)
                    .Select(index => new SessionDetailViewModel
                    {
                        GameName = "Game " + index
                    })
                    .ToList();
                var list = new ListView
                {
                    Width = 360,
                    Height = 300,
                    ItemsSource = items,
                    ItemTemplate = template
                };
                VirtualizingPanel.SetIsVirtualizing(list, true);
                VirtualizingPanel.SetVirtualizationMode(
                    list,
                    VirtualizationMode.Recycling);
                ScrollViewer.SetCanContentScroll(list, true);
                list.ItemsPanel = new ItemsPanelTemplate(
                    new FrameworkElementFactory(
                        typeof(VirtualizingStackPanel)));

                var window = new Window
                {
                    Content = list,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 400,
                    Height = 340
                };
                try
                {
                    window.Show();
                    PumpDispatcher();
                    list.UpdateLayout();
                    var firstContainer = (ListViewItem)list
                        .ItemContainerGenerator.ContainerFromIndex(0);
                    Equal(true, firstContainer != null);
                    var root = FindVisualDescendants<Border>(
                        firstContainer).First(border =>
                            HoverMotion.GetEnabled(border));
                    var transform = (TranslateTransform)root.RenderTransform;
                    transform.Y = -1;

                    list.ScrollIntoView(items[items.Count - 1]);
                    list.UpdateLayout();
                    PumpDispatcher();
                    Equal(0d, transform.Y);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestHoverMotionHoldAndRelease()
        {
            RunOnSta(() =>
            {
                var root = new Border
                {
                    Width = 100,
                    Height = 40,
                    RenderTransform = new TranslateTransform(0, 0)
                };
                HoverMotion.SetEnabled(root, true);
                HoverMotion.SetLiftY(root, 1d);
                HoverMotion.SetDuration(root, 60d);
                var transform = (TranslateTransform)root.RenderTransform;
                var window = new Window
                {
                    Content = root,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 200,
                    Height = 120
                };
                try
                {
                    window.Show();
                    PumpDispatcher();

                    double RenderedY()
                    {
                        return root.TransformToAncestor(window)
                            .Transform(new Point(0, 0)).Y;
                    }

                    var initialY = RenderedY();
                    root.RaiseEvent(new MouseEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount)
                    {
                        RoutedEvent = Mouse.MouseEnterEvent
                    });
                    PumpDispatcherFor(TimeSpan.FromMilliseconds(140));
                    Equal(true, Math.Abs(RenderedY() - (initialY - 1d)) < 0.01);

                    root.RaiseEvent(new MouseEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount)
                    {
                        RoutedEvent = Mouse.MouseLeaveEvent
                    });
                    PumpDispatcherFor(TimeSpan.FromMilliseconds(140));
                    Equal(true, Math.Abs(RenderedY() - initialY) < 0.01);
                    Equal(0d, transform.Y);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestHoverMotionRecyclesCleanly()
        {
            RunOnSta(() =>
            {
                var root = new Border
                {
                    RenderTransform = new TranslateTransform(0, 0)
                };
                HoverMotion.SetEnabled(root, true);
                HoverMotion.SetLiftY(root, 1d);
                HoverMotion.SetDuration(root, 100d);
                var transform = (TranslateTransform)root.RenderTransform;

                transform.Y = -1;
                root.DataContext = new object();
                Equal(0d, transform.Y);

                transform.Y = -1;
                root.RaiseEvent(new RoutedEventArgs(
                    FrameworkElement.UnloadedEvent));
                PumpDispatcher();
                Equal(0d, transform.Y);

                transform.Y = -1;
                root.IsEnabled = false;
                Equal(0d, transform.Y);
            });
        }

        private static RequestBringIntoViewEventArgs CreateBringIntoViewRequest(
            DependencyObject target,
            Rect targetRect)
        {
            var constructor = typeof(RequestBringIntoViewEventArgs)
                .GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[]
                    {
                        typeof(DependencyObject),
                        typeof(Rect)
                    },
                    null);
            Equal(true, constructor != null);
            return (RequestBringIntoViewEventArgs)constructor.Invoke(
                new object[] { target, targetRect });
        }

        private static void TestRankingTabBringIntoViewSuppression()
        {
            var sourceRoot = FindSourceRoot();
            var dashboard = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            Equal(true, dashboard.Contains(
                "RequestBringIntoView=\"RankingModule_RequestBringIntoView\""));
            Equal(true, dashboard.Contains(
                "PreviewMouseDown=\"RankingModule_PreviewMouseDown\""));
            Equal(true, dashboard.Contains(
                "PreviewKeyDown=\"RankingModule_PreviewKeyDown\""));
            var codeBehind = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            Equal(true, codeBehind.Contains(
                "rankingTabMouseInteraction"));
            Equal(true, codeBehind.Contains(
                "rankingTabMouseInteractionTimer"));
            Equal(true, codeBehind.Contains("DispatcherTimer"));

            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 900,
                    Height = 800
                };
                var window = new Window
                {
                    Content = view,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 900,
                    Height = 800
                };
                try
                {
                    window.Show();
                    PumpDispatcher();

                    var scroller = (ScrollViewer)view.FindName(
                        "DashboardScrollViewer");
                    Equal(true, scroller.ScrollableHeight > 0);
                    scroller.ScrollToVerticalOffset(120);
                    view.UpdateLayout();
                    var initialOffset = scroller.VerticalOffset;
                    Equal(true, initialOffset > 0);

                    var module = (Border)view.FindName("RankingModule");
                    var tabItems = FindVisualDescendants<TabItem>(module)
                        .ToList();
                    Equal(2, tabItems.Count);
                    var lifetimeTab = tabItems[1];

                    lifetimeTab.RaiseEvent(new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    });
                    lifetimeTab.Focus();
                    lifetimeTab.RaiseEvent(new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseDownEvent
                    });
                    lifetimeTab.RaiseEvent(new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseUpEvent
                    });
                    PumpDispatcherFor(TimeSpan.FromMilliseconds(200));

                    Equal(true, lifetimeTab.IsSelected);
                    Equal(true, Math.Abs(
                        scroller.VerticalOffset - initialOffset) < 0.01);

                    PumpDispatcherFor(TimeSpan.FromMilliseconds(250));
                    Equal(true, Math.Abs(
                        scroller.VerticalOffset - initialOffset) < 0.01);

                    var request = CreateBringIntoViewRequest(
                        lifetimeTab,
                        new Rect(0, 0, 200, 40));
                    request.RoutedEvent =
                        FrameworkElement.RequestBringIntoViewEvent;
                    var handler = typeof(PlaytimeInsightsDashboardView).GetMethod(
                        "RankingModule_RequestBringIntoView",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Equal(true, handler != null);
                    handler.Invoke(
                        view,
                        new object[] { module, request });
                    Equal(false, request.Handled);
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestPresentationRefreshChain()
        {
            RunOnSta(() =>
            {
                var settings =
                    (PlaytimeInsightsSettingsViewModel)
                    System.Runtime.Serialization.FormatterServices
                        .GetUninitializedObject(
                            typeof(PlaytimeInsightsSettingsViewModel));
                settings.Settings = new PlaytimeInsightsSettings();
                var viewModel = new DashboardViewModel(
                    null,
                    null,
                    new AnalyticsService(),
                    new SessionQueryService(new TestGameMetadataAccessor()),
                    settings);
                viewModel.SelectedMetadataDimensionOption = null;
                var view = new PlaytimeInsightsDashboardView
                {
                    DataContext = viewModel,
                    Width = 1200,
                    Height = 800
                };
                var window = new Window
                {
                    Content = view,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000,
                    Width = 1200,
                    Height = 800
                };
                try
                {
                    window.Show();
                    PumpDispatcher();

                    var publish = typeof(DashboardViewModel).GetMethod(
                        "PublishPresentationUpdate",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Equal(true, publish != null);
                    publish.Invoke(
                        viewModel,
                        new object[] { DashboardPresentationTransition.Full });
                    publish.Invoke(
                        viewModel,
                        new object[] { DashboardPresentationTransition.Trend });
                    PumpDispatcher();

                    var metricHost = (FrameworkElement)view.FindName(
                        "MetricCardsHost");
                    var trendHost = (FrameworkElement)view.FindName(
                        "TrendModule");
                    Equal(1d, metricHost.Opacity);
                    Equal(0d,
                        ((TranslateTransform)metricHost.RenderTransform).Y);
                    if (SystemParameters.ClientAreaAnimation)
                    {
                        Equal(false, metricHost.HasAnimatedProperties);
                        Equal(true, trendHost.HasAnimatedProperties);
                    }
                    else
                    {
                        Equal(1d, trendHost.Opacity);
                        Equal(0d,
                            ((TranslateTransform)trendHost.RenderTransform).Y);
                    }
                }
                finally
                {
                    window.Content = null;
                    window.Close();
                }
            });
        }

        private static void TestDashboardListHoverReset()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView();
                view.Measure(new Size(1200, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                view.UpdateLayout();

                var drilldownModule = (FrameworkElement)
                    ((DataTemplate)view.Resources["DrilldownCardTemplate"])
                        .LoadContent();
                drilldownModule.Measure(new Size(520, 500));
                drilldownModule.Arrange(new Rect(
                    0,
                    0,
                    520,
                    drilldownModule.DesiredSize.Height));
                var drilldownList = FindVisualDescendants<ListView>(
                    drilldownModule).Single();
                var drilldownTemplate = drilldownList.ItemTemplate;
                Equal(true, drilldownTemplate != null);
                var drilldownPresenter = new ContentPresenter
                {
                    ContentTemplate = drilldownTemplate,
                    Content = new SessionDetailViewModel()
                };
                var drilldownWindow = new Window
                {
                    Content = drilldownPresenter,
                    ShowInTaskbar = false,
                    Width = 320,
                    Height = 120,
                    WindowStyle = WindowStyle.None,
                    Left = -10000,
                    Top = -10000
                };
                try
                {
                    drilldownWindow.Show();
                    PumpDispatcher();
                    var drilldownRoot = FindVisualDescendants<Border>(
                        drilldownPresenter).First(border =>
                            border.RenderTransform is TranslateTransform);
                    Equal(true, HoverMotion.GetEnabled(drilldownRoot));
                    var drilldownTransform =
                        (TranslateTransform)drilldownRoot.RenderTransform;
                    drilldownTransform.Y = -1;
                    drilldownRoot.DataContext = new object();
                    Equal(0d, drilldownTransform.Y);

                    drilldownTransform.Y = -1;
                    drilldownRoot.RaiseEvent(new RoutedEventArgs(
                        FrameworkElement.UnloadedEvent));
                    PumpDispatcher();
                    Equal(0d, drilldownTransform.Y);
                }
                finally
                {
                    drilldownWindow.Content = null;
                    drilldownWindow.Close();
                }
            });
        }

        private static void TestDashboardListHoverContracts()
        {
            var sourceRoot = FindSourceRoot();
            var visualResources = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Resources",
                "PlaytimeInsightsVisualResources.xaml"));
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboard = File.ReadAllText(dashboardPath);
            var document = XDocument.Load(dashboardPath);
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");

            foreach (var brush in new[]
            {
                "DrilldownSessionHoverOverlayBrush",
                "DrilldownSessionHoverBorderBrush"
            })
            {
                Equal(true, visualResources.Contains(brush));
            }

            var rankingTemplate = document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "DataTemplate" &&
                    (string)element.Attribute(xamlNamespace + "Key") ==
                        "GameRankingItemTemplate");
            var rankingSource = rankingTemplate.ToString();
            Equal(false, rankingSource.Contains(
                "RankingItemRoot"));
            Equal(false, rankingSource.Contains(
                "RankingItemTransform"));
            Equal(false, rankingSource.Contains(
                "RankingItemHoverOverlay"));
            Equal(false, rankingSource.Contains(
                "RoutedEvent=\"MouseEnter\""));
            Equal(false, rankingSource.Contains("DropShadowEffect"));

            var drilldownList = document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "ListView" &&
                    (string)element.Attribute("ItemsSource") ==
                    "{Binding SessionDetails}");
            var drilldownSource = drilldownList.ToString();
            Equal(true, drilldownSource.Contains(
                "x:Name=\"DrilldownSessionRoot\""));
            Equal(true, drilldownSource.Contains(
                "x:Name=\"DrilldownSessionTransform\""));
            Equal(true, drilldownSource.Contains(
                "x:Name=\"DrilldownSessionHoverOverlay\""));
            Equal(true, drilldownSource.Contains(
                "{StaticResource DrilldownSessionHoverOverlayBrush}"));
            Equal(true, drilldownSource.Contains(
                "{StaticResource DrilldownSessionHoverBorderBrush}"));
            Equal(true, drilldownSource.Contains(
                "IsHitTestVisible=\"False\""));
            Equal(true, drilldownSource.Contains(
                "controls:HoverMotion.Enabled=\"True\""));
            Equal(true, drilldownSource.Contains(
                "controls:HoverMotion.LiftY=\"1\""));
            Equal(true, drilldownSource.Contains(
                "controls:HoverMotion.Duration=\"100\""));
            Equal(false, drilldownSource.Contains(
                "RoutedEvent=\"MouseEnter\""));
            Equal(false, drilldownSource.Contains("DropShadowEffect"));
        }

        private static void TestDashboardMetricAdditionsBehavior()
        {
            var compact = new DurationDisplayViewModel(
                "5",
                "小时",
                "8",
                "分",
                "5 小时 8 分");
            Equal("5 小时 8 分", compact.CompactText);

            var metrics = new DashboardMetricsViewModel(null);
            metrics.Apply(
                new DashboardSnapshot
                {
                    AverageSessionDisplay = new DurationDisplayViewModel(
                        "43",
                        "分",
                        "56",
                        "秒",
                        "43 分 56 秒"),
                    AverageSessionText = "43 分 56 秒",
                    Advanced = new AdvancedAnalyticsSnapshot
                    {
                        AnomalyCount = 3,
                        AnomalyCountText = "3 条"
                    }
                },
                new Dictionary<Guid, Playnite.SDK.Models.Game>());
            Equal("均 43 分 56 秒 / 次", metrics.AverageSessionSummaryText);
            Equal(3, metrics.AnomalyCount);

            var distribution = new DashboardDistributionViewModel();
            distribution.Apply(new DashboardSnapshot
            {
                Advanced = new AdvancedAnalyticsSnapshot
                {
                    WeekdayDistribution = new List<DistributionBarViewModel>(),
                    HourDistribution = new List<DistributionBarViewModel>(),
                    WeekHourCells = new List<WeekHourCellViewModel>
                    {
                        new WeekHourCellViewModel
                        {
                            DayLabel = "周五",
                            HourLabel = "20:00",
                            Seconds = 90
                        },
                        new WeekHourCellViewModel
                        {
                            DayLabel = "周一",
                            HourLabel = "08:00",
                            Seconds = 10
                        }
                    },
                    WeekdayLabels = new List<string>(),
                    HourLabels = new List<string>(),
                    Anomalies = new List<AnomalySessionViewModel>(),
                    AnomalyVisibility = Visibility.Collapsed
                }
            });
            Equal("周五 20:00", distribution.PeakPeriodText);
            Equal(true, Regex.IsMatch(
                distribution.PeakPeriodShareText,
                @"^占区间 90\s?%$",
                RegexOptions.CultureInvariant));

            var settings =
                (PlaytimeInsightsSettingsViewModel)
                System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(
                        typeof(PlaytimeInsightsSettingsViewModel));
            settings.Settings = new PlaytimeInsightsSettings();
            var dashboard = new DashboardViewModel(
                null,
                null,
                new AnalyticsService(),
                new SessionQueryService(new TestGameMetadataAccessor()),
                settings);
            Equal(false, string.IsNullOrWhiteSpace(
                dashboard.StreakCardTitle));
            Equal(false, string.IsNullOrWhiteSpace(
                dashboard.PeakPeriodCardTitle));
        }

        private static void TestAdvancedFilterToggleInteraction()
        {
            RunOnSta(() =>
            {
                var view = new PlaytimeInsightsDashboardView
                {
                    Width = 1200
                };
                view.Measure(new Size(1200, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                view.UpdateLayout();

                var expander = (Expander)view.FindName(
                    "AdvancedFilterExpander");
                Equal(true, expander.IsExpanded);
                var toggle = FindVisualDescendants<ToggleButton>(expander)
                    .Single(button =>
                        button.TemplatedParent == expander);
                Equal(true, toggle.Focusable);
                Equal(true, toggle.IsChecked);
                var contentHost = FindVisualDescendants<ContentPresenter>(
                    expander).Single(presenter =>
                        presenter.ContentSource == "Content" &&
                        presenter.TemplatedParent == expander);
                Equal(Visibility.Visible, contentHost.Visibility);

                toggle.IsChecked = false;
                view.UpdateLayout();
                Equal(false, expander.IsExpanded);
                Equal(false, toggle.IsChecked);
                Equal(Visibility.Collapsed, contentHost.Visibility);

                toggle.IsChecked = true;
                view.UpdateLayout();
                Equal(true, expander.IsExpanded);
                Equal(Visibility.Visible, contentHost.Visibility);

                view.Width = 608;
                view.Measure(new Size(608, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 608, view.DesiredSize.Height));
                view.UpdateLayout();
                var toggleLeft = toggle.TransformToAncestor(view)
                    .Transform(new Point(0, 0)).X;
                Equal(true, toggleLeft >= 0);
                Equal(true, toggleLeft + toggle.ActualWidth <= 608.5);
            });
        }

        private static void TestDashboardThemeVisualContracts()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var document = XDocument.Load(dashboardPath);
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");

            var rangeText = document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "TextBlock" &&
                    (string)element.Attribute("Text") ==
                        "{Binding RangeText}");
            Equal(
                "{DynamicResource TextBrush}",
                (string)rangeText.Attribute("Foreground"));

            foreach (var styleName in new[]
            {
                "GhostIconButtonStyle",
                "HelpIconButtonStyle",
                "WeekdayButtonStyle"
            })
            {
                var style = document.Descendants()
                    .Single(element =>
                        element.Name.LocalName == "Style" &&
                        (string)element.Attribute(xamlNamespace + "Key") ==
                            styleName);
                Equal(null, (string)style.Attribute("BasedOn"));

                var styleSource = style.ToString();
                Equal(false, styleSource.Contains("DynamicResource TextBrush"));
                Equal(false, styleSource.Contains("DynamicResource ControlBackgroundBrush"));
                Equal(false, styleSource.Contains("DynamicResource PanelSeparatorBrush"));
                Equal(true, styleSource.Contains(
                    "StaticResource MetricCardTextBrush"));
            }

            var ghostStyle = FindStyle(document, xamlNamespace,
                "GhostIconButtonStyle");
            AssertStyleSetter(ghostStyle, "Background", "#14FFFFFF");
            AssertStyleSetter(ghostStyle, "BorderBrush", "#1AFFFFFF");
            AssertStyleSetter(ghostStyle, "Foreground",
                "{StaticResource MetricCardTextBrush}");
            AssertStyleTrigger(ghostStyle, "IsMouseOver", "Background",
                "#264A90E2");
            AssertStyleTrigger(ghostStyle, "IsPressed", "Background",
                "#334A90E2");
            AssertStyleTrigger(ghostStyle, "IsKeyboardFocused",
                "BorderBrush", "{StaticResource MetricCardTextBrush}");

            var helpStyle = FindStyle(document, xamlNamespace,
                "HelpIconButtonStyle");
            AssertStyleSetter(helpStyle, "Background", "#14FFFFFF");
            AssertStyleSetter(helpStyle, "BorderBrush", "#1AFFFFFF");
            AssertStyleSetter(helpStyle, "Foreground",
                "{StaticResource MetricCardTextBrush}");
            AssertStyleTrigger(helpStyle, "IsMouseOver", "Background",
                "#264A90E2");
            AssertStyleTrigger(helpStyle, "IsPressed", "Background",
                "#334A90E2");
            AssertStyleTrigger(helpStyle, "IsKeyboardFocused",
                "BorderBrush", "{StaticResource MetricCardTextBrush}");

            var weekdayStyle = FindStyle(document, xamlNamespace,
                "WeekdayButtonStyle");
            AssertStyleSetter(weekdayStyle, "Foreground",
                "{StaticResource MetricCardTextBrush}");
            AssertStyleTrigger(weekdayStyle, "IsMouseOver", "Background",
                "#14FFFFFF");
            AssertStyleTrigger(weekdayStyle, "IsMouseOver", "BorderBrush",
                "#1AFFFFFF");
            AssertStyleTrigger(weekdayStyle, "IsKeyboardFocused",
                "BorderBrush", "{StaticResource MetricCardTextBrush}");
        }

        private static XElement FindStyle(
            XDocument document,
            XNamespace xamlNamespace,
            string key)
        {
            return document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "Style" &&
                    (string)element.Attribute(xamlNamespace + "Key") == key);
        }

        private static void AssertStyleSetter(
            XElement style,
            string property,
            string value)
        {
            Equal(1, style.Elements()
                .Count(element =>
                    element.Name.LocalName == "Setter" &&
                    (string)element.Attribute("Property") == property &&
                    (string)element.Attribute("Value") == value));
        }

        private static void AssertStyleTrigger(
            XElement style,
            string triggerProperty,
            string setterProperty,
            string value)
        {
            Equal(1, style.Descendants()
                .Count(element =>
                    element.Name.LocalName == "Trigger" &&
                    (string)element.Attribute("Property") ==
                        triggerProperty &&
                    element.Elements()
                        .Any(setter =>
                            setter.Name.LocalName == "Setter" &&
                            (string)setter.Attribute("Property") ==
                                setterProperty &&
                            (string)setter.Attribute("Value") == value)));
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

                var metricGrids = FindVisualDescendants<ResponsiveUniformPanel>(view);
                Equal(1, metricGrids.Count);
                var metricGrid = metricGrids[0];
                Equal(200d, metricGrid.MinItemWidth);
                Equal(232d, metricGrid.PreferredItemWidth);
                Equal(280d, metricGrid.MaxItemWidth);
                Equal(1, metricGrid.MinColumns);
                Equal(4, metricGrid.MaxColumns);
                Equal(12d, metricGrid.HorizontalSpacing);
                Equal(12d, metricGrid.VerticalSpacing);
                Equal(true, metricGrid.CenterIncompleteRow);
                Equal(8, metricGrid.Children.Count);
                Equal(true, metricGrid.Children.Cast<UIElement>().All(
                    child => child is Border));
                Equal(true, metricGrid.Children.Cast<Border>().All(metricCard =>
                    metricCard.Style == view.Resources["MetricCardStyle"]));

                var firstWideTop = GetLayoutSlot(metricGrid.Children[0]).Top;
                Equal(true, metricGrid.Children.Cast<UIElement>()
                    .Take(4)
                    .All(child =>
                        Math.Abs(GetLayoutSlot(child).Top - firstWideTop) < 0.01));
                Equal(true, GetLayoutSlot(metricGrid.Children[4]).Top >
                    firstWideTop + 1);

                view.Width = 608;
                view.Measure(new Size(608, double.PositiveInfinity));
                view.Arrange(new Rect(0, 0, 608, view.DesiredSize.Height));
                view.UpdateLayout();
                var firstNarrowTop = GetLayoutSlot(metricGrid.Children[0]).Top;
                Equal(true, GetLayoutSlot(metricGrid.Children[1]).Top ==
                    firstNarrowTop);
                Equal(true, GetLayoutSlot(metricGrid.Children[2]).Top >
                    firstNarrowTop + 1);

                Equal(1d, (double)view.Resources["TextOpacityPrimary"]);
                Equal(0.72d, (double)view.Resources["TextOpacitySecondary"]);
                Equal(0.58d, (double)view.Resources["TextOpacityTertiary"]);
                Equal(0.45d, (double)view.Resources["TextOpacityDisabled"]);

                var sampleCard = new Border
                {
                    Style = (Style)view.Resources["MetricCardStyle"]
                };
                Equal(154d, sampleCard.MinHeight);
                Equal(new Thickness(16), sampleCard.Padding);
                Equal(true, double.IsNaN(sampleCard.Width));
                Equal(true, double.IsNaN(sampleCard.Height));
                Equal(new Thickness(0), sampleCard.Margin);

                var header = new TextBlock
                {
                    Style = (Style)view.Resources["MetricHeaderStyle"]
                };
                var helper = new TextBlock
                {
                    Style = (Style)view.Resources["MetricHelperTextStyle"]
                };
                Equal(1d, header.Opacity);
                Equal(1d, helper.Opacity);
                Equal(
                    view.TryFindResource("MetricCardMutedTextBrush"),
                    header.Foreground);
                Equal(
                    view.TryFindResource("MetricCardMutedTextBrush"),
                    helper.Foreground);

                var iconBases = FindVisualDescendants<Border>(metricGrid)
                    .Where(border =>
                        border.Width == 32 &&
                        border.Height == 32 &&
                        Math.Abs(border.CornerRadius.TopLeft - 8) < 0.01)
                    .ToList();
                Equal(8, iconBases.Count);
            });
        }

        private static void TestDurationComparisonPillsStackVertically()
        {
            RunOnSta(() =>
            {
                var previous = new ComparisonMetricViewModel
                {
                    TagText = "↑ 123 小时 45 分（环比）",
                    TrendKind = "Increase",
                    TooltipText = "Previous period comparison"
                };
                var yearOverYear = new ComparisonMetricViewModel
                {
                    TagText = "↓ 98 小时 30 分（同比）",
                    TrendKind = "Decrease",
                    TooltipText = "Year-over-year comparison"
                };
                var snapshot = new DashboardSnapshot
                {
                    RangeDurationDisplay = new DurationDisplayViewModel(
                        "245",
                        "小时",
                        "15",
                        "分",
                        "245 小时 15 分"),
                    Advanced = new AdvancedAnalyticsSnapshot
                    {
                        ComparisonVisibility = Visibility.Visible,
                        PreviousPeriodComparison = previous,
                        YearOverYearComparison = yearOverYear
                    }
                };
                var viewModel = CreateDashboardViewModelForLayout();
                viewModel.Metrics.Apply(
                    snapshot,
                    Enumerable.Empty<Playnite.SDK.Models.Game>());
                var view = new PlaytimeInsightsDashboardView
                {
                    DataContext = viewModel
                };

                LayoutDashboardViewAt(view, 560);

                var previousPill = FindVisualDescendants<Border>(view)
                    .Single(border => ReferenceEquals(
                        border.DataContext,
                        previous));
                var yearOverYearPill = FindVisualDescendants<Border>(view)
                    .Single(border => ReferenceEquals(
                        border.DataContext,
                        yearOverYear));
                var host = VisualTreeHelper.GetParent(previousPill)
                    as FrameworkElement;
                Equal(true, host != null);
                Equal(host, VisualTreeHelper.GetParent(yearOverYearPill));

                var previousSlot = GetLayoutSlot(previousPill);
                var yearOverYearSlot = GetLayoutSlot(yearOverYearPill);
                Equal(true, yearOverYearSlot.Top >= previousSlot.Bottom - 0.01);
                Equal(true, previousSlot.Right <= host.ActualWidth + 0.01);
                Equal(true, yearOverYearSlot.Right <= host.ActualWidth + 0.01);
                foreach (var pill in new[]
                {
                    previousPill,
                    yearOverYearPill
                })
                {
                    Equal(HorizontalAlignment.Left, pill.HorizontalAlignment);
                    Equal(true, pill.ActualWidth < host.ActualWidth - 0.01);
                    var text = FindVisualDescendants<TextBlock>(pill).Single();
                    Equal(TextWrapping.NoWrap, text.TextWrapping);
                    Equal(TextTrimming.None, text.TextTrimming);
                    var textOrigin = text.TransformToAncestor(pill)
                        .Transform(new Point(0, 0));
                    Equal(true, textOrigin.X >= pill.BorderThickness.Left);
                    var expectedPillWidth = text.ActualWidth +
                        pill.Padding.Left +
                        pill.Padding.Right +
                        pill.BorderThickness.Left +
                        pill.BorderThickness.Right;
                    Equal(
                        true,
                        Math.Abs(pill.ActualWidth - expectedPillWidth) < 0.01);
                    Equal(
                        true,
                        textOrigin.X + text.ActualWidth <=
                            pill.ActualWidth - pill.BorderThickness.Right + 0.01);
                }
            });
        }

        private static void TestDashboardVisualRefactorStaticContract()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboard = File.ReadAllText(dashboardPath);
            var document = XDocument.Load(dashboardPath);
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");

            var metricTitleKeys = new[]
            {
                "LOCPlaytimeInsightsRangeDuration",
                "LOCPlaytimeInsightsSessionCount",
                "LOCPlaytimeInsightsActiveDays",
                "LOCPlaytimeInsightsLongestSession",
                "LOCPlaytimeInsightsLifetimeDuration",
                "LOCPlaytimeInsightsAnomalyHints"
            };
            foreach (var key in metricTitleKeys)
            {
                Equal(1, Regex.Matches(dashboard, key + "}").Count);
            }

            var dashboardViewModelSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            Equal(true, dashboardViewModelSource.Contains(
                "LOCPlaytimeInsightsStreakCombined"));
            Equal(true, dashboardViewModelSource.Contains(
                "LOCPlaytimeInsightsPeakPeriodTitle"));

            var metricGrids = document.Descendants()
                .Where(element =>
                    element.Name.LocalName == "ResponsiveUniformPanel")
                .ToList();
            Equal(1, metricGrids.Count);
            var metricGrid = metricGrids[0];
            Equal("200", (string)metricGrid.Attribute("MinItemWidth"));
            Equal("232", (string)metricGrid.Attribute("PreferredItemWidth"));
            Equal("280", (string)metricGrid.Attribute("MaxItemWidth"));
            Equal("1", (string)metricGrid.Attribute("MinColumns"));
            Equal("4", (string)metricGrid.Attribute("MaxColumns"));
            var metricCards = metricGrid.Elements().ToList();
            Equal(8, metricCards.Count);
            Equal(true, metricCards.All(element =>
                element.Name.LocalName == "Border"));
            Equal(false, dashboard.Contains(
                "<UniformGrid Columns=\"4\""));
            Equal(0, Regex.Matches(dashboard, "IsCompactHeroLayout").Count);

            var metricKeyOrder = metricTitleKeys;
            var lastMetricKeyIndex = -1;
            foreach (var key in metricKeyOrder)
            {
                var keyIndex = dashboard.IndexOf(
                    "{DynamicResource " + key + "}",
                    StringComparison.Ordinal);
                Equal(true, keyIndex > lastMetricKeyIndex);
                lastMetricKeyIndex = keyIndex;
            }

            foreach (var binding in new[]
            {
                "{Binding RangeDurationDisplay.MajorValue, Mode=OneWay}",
                "{Binding RangeDurationDisplay.MajorUnit, Mode=OneWay}",
                "{Binding RangeDurationDisplay.MinorValue, Mode=OneWay}",
                "{Binding RangeDurationDisplay.MinorUnit, Mode=OneWay}",
                "{Binding LongestSessionDisplay.MajorValue, Mode=OneWay}",
                "{Binding LongestSessionDisplay.MajorUnit, Mode=OneWay}",
                "{Binding LongestSessionDisplay.MinorValue, Mode=OneWay}",
                "{Binding LongestSessionDisplay.MinorUnit, Mode=OneWay}",
                "{Binding LifetimeDurationDisplay.MajorValue, Mode=OneWay}",
                "{Binding LifetimeDurationDisplay.MajorUnit, Mode=OneWay}",
                "{Binding LifetimeDurationDisplay.MinorValue, Mode=OneWay}",
                "{Binding LifetimeDurationDisplay.MinorUnit, Mode=OneWay}",
                "{Binding SessionCountText}",
                "{Binding AverageSessionSummaryText}",
                "{Binding ActiveDaysText}",
                "{Binding StreakCardTitle}",
                "{Binding LongestStreakText, Mode=OneWay}",
                "{Binding CurrentStreakText}",
                "{Binding CurrentStreakDateText, Mode=OneWay}",
                "{Binding AnomalyCountText}",
                "{Binding AnomalyCount}",
                "{Binding PeakPeriodCardTitle}",
                "{Binding PeakPeriodText}",
                "{Binding PeakPeriodShareText}"
            })
            {
                Equal(true, dashboard.Contains(binding));
            }

            var metricIconBases = metricGrids[0].Descendants()
                .Where(element =>
                    element.Name.LocalName == "Border" &&
                    (string)element.Attribute("Width") == "32" &&
                    (string)element.Attribute("Height") == "32" &&
                    (string)element.Attribute("CornerRadius") == "8")
                .ToList();
            Equal(8, metricIconBases.Count);
            foreach (var brush in new[]
            {
                "MetricDurationForegroundBrush",
                "MetricDurationBackgroundBrush",
                "MetricSessionForegroundBrush",
                "MetricSessionBackgroundBrush",
                "MetricActivityForegroundBrush",
                "MetricActivityBackgroundBrush",
                "MetricAnomalyForegroundBrush",
                "MetricAnomalyBackgroundBrush"
            })
            {
                Equal(true, dashboard.Contains(brush));
            }

            Equal(true, dashboard.Contains("MetricComparisonTagStyle"));
            Equal(true, dashboard.Contains("#FF4ADE80"));
            Equal(true, dashboard.Contains("#1A4ADE80"));
            Equal(true, dashboard.Contains("#FF60A5FA"));
            Equal(true, dashboard.Contains("#1A60A5FA"));
            Equal(true, dashboard.Contains("RefreshGhostButtonStyle"));
            Equal(true, dashboard.Contains("&#xE72C;"));
            Equal(true, dashboard.Contains("AnomalyMetricIconStyle"));
            Equal(true, dashboard.Contains("AnomalyMetricIconTextStyle"));

            var adaptivePanels = document.Descendants()
                .Where(element =>
                    element.Name.LocalName == "AdaptiveDashboardPanel")
                .ToList();
            Equal(1, adaptivePanels.Count);
            var adaptivePanel = adaptivePanels[0];
            Equal("1200", (string)adaptivePanel.Attribute("EnterWideWidth"));
            Equal("1160", (string)adaptivePanel.Attribute("ExitWideWidth"));
            Equal("0.38",
                (string)adaptivePanel.Attribute("SecondaryColumnRatio"));
            Equal("18", (string)adaptivePanel.Attribute("ColumnSpacing"));
            Equal("18", (string)adaptivePanel.Attribute("VerticalSpacing"));

            var moduleElements = adaptivePanel.Elements().ToList();
            Equal(6, moduleElements.Count);
            var expectedZones = new Dictionary<string, string>
            {
                { "TrendModule", "Primary" },
                { "TrendDrilldownHost", "Primary" },
                { "RankingModule", "Secondary" },
                { "DistributionModule", "Primary" },
                { "DistributionDrilldownHost", "Primary" },
                { "AnomalyModule", "Secondary" }
            };
            var modulesByName = new Dictionary<string, XElement>();
            foreach (var moduleElement in moduleElements)
            {
                var moduleName = (string)moduleElement.Attribute(
                    xamlNamespace + "Name");
                Equal(true, expectedZones.ContainsKey(moduleName));
                modulesByName.Add(moduleName, moduleElement);
                Equal(expectedZones[moduleName], moduleElement.Attributes()
                    .Single(attribute =>
                        attribute.Name.LocalName ==
                        "AdaptiveDashboardPanel.Zone")
                    .Value);
            }

            var moduleOrder = new[]
            {
                "TrendModule",
                "TrendDrilldownHost",
                "RankingModule",
                "DistributionModule",
                "DistributionDrilldownHost",
                "AnomalyModule"
            };
            var lastModuleIndex = -1;
            foreach (var moduleName in moduleOrder)
            {
                var moduleIndex = dashboard.IndexOf(
                    "x:Name=\"" + moduleName + "\"",
                    StringComparison.Ordinal);
                Equal(true, moduleIndex > lastModuleIndex);
                lastModuleIndex = moduleIndex;
            }

            var trendModule = modulesByName["TrendModule"];
            foreach (var binding in new[]
            {
                "{Binding PeriodTitleText}",
                "{Binding AggregationOptions}",
                "{Binding SelectedAggregationOption, Mode=TwoWay}",
                "{Binding PeriodActivities}"
            })
            {
                Equal(true, trendModule.DescendantsAndSelf()
                    .Attributes()
                    .Any(attribute => attribute.Value == binding));
            }

            var rankingModule = modulesByName["RankingModule"];
            foreach (var binding in new[]
            {
                "{Binding RangeGameRankings}",
                "{Binding LifetimeGameRankings}"
            })
            {
                Equal(true, rankingModule.DescendantsAndSelf()
                    .Attributes()
                    .Any(attribute => attribute.Value == binding));
            }

            var distributionModule = modulesByName["DistributionModule"];
            foreach (var binding in new[]
            {
                "{Binding WeekdayDistribution}",
                "{Binding HourDistribution}",
                "{Binding HeatmapCells}",
                "{Binding WeekHourCells}"
            })
            {
                Equal(true, distributionModule.DescendantsAndSelf()
                    .Attributes()
                    .Any(attribute => attribute.Value == binding));
            }

            var anomalyModule = modulesByName["AnomalyModule"];
            foreach (var binding in new[]
            {
                "{Binding AnomalyVisibility}",
                "{Binding Anomalies}"
            })
            {
                Equal(true, anomalyModule.DescendantsAndSelf()
                    .Attributes()
                    .Any(attribute => attribute.Value == binding));
            }

            var drilldownTemplate = document.Descendants()
                .Single(element =>
                    element.Name.LocalName == "DataTemplate" &&
                    (string)element.Attribute(xamlNamespace + "Key") ==
                    "DrilldownCardTemplate");
            foreach (var binding in new[]
            {
                "{Binding SessionDetails}",
                "{Binding LoadMoreSessionDetailsCommand}"
            })
            {
                Equal(true, drilldownTemplate.DescendantsAndSelf()
                    .Attributes()
                    .Any(attribute => attribute.Value == binding));
            }
            Equal(
                "{Binding Drilldown.TrendHostVisibility}",
                (string)modulesByName["TrendDrilldownHost"].Attribute(
                    "Visibility"));
            Equal(
                "{Binding Drilldown.DistributionHostVisibility}",
                (string)modulesByName["DistributionDrilldownHost"].Attribute(
                    "Visibility"));
            foreach (var hostName in new[]
            {
                "TrendDrilldownHost",
                "DistributionDrilldownHost"
            })
            {
                Equal(
                    "{StaticResource DrilldownCardTemplate}",
                    (string)modulesByName[hostName].Attribute(
                        "ContentTemplate"));
            }

            Equal(1, Regex.Matches(
                dashboard,
                Regex.Escape("ItemsSource=\"{Binding AggregationOptions}\""))
                .Count);
            Equal(1, Regex.Matches(
                dashboard,
                Regex.Escape(
                    "SelectedItem=\"{Binding SelectedAggregationOption, Mode=TwoWay}\""))
                .Count);
            Equal(true, Regex.IsMatch(
                dashboard,
                @"<ScrollViewer\s+x:Name=""DashboardScrollViewer""\s+" +
                @"VerticalScrollBarVisibility=""Auto""\s+" +
                @"HorizontalScrollBarVisibility=""Disabled""",
                RegexOptions.CultureInvariant));
            Equal(5, Regex.Matches(
                dashboard,
                "PreviewMouseWheel=\"NestedScrollViewer_PreviewMouseWheel\"")
                .Count);
            Equal(2, Regex.Matches(
                dashboard,
                "IsVisibleChanged=\"DrilldownHost_IsVisibleChanged\"")
                .Count);
            Equal(true, dashboard.Contains(
                "ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\""));

            var dashboardCode = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml.cs"));
            Equal(true, dashboardCode.Contains(
                "DrilldownHost_IsVisibleChanged"));
            Equal(true, dashboardCode.Contains("ScrollToVerticalOffset"));
            Equal(true, dashboardCode.Contains("IsVerticalBandVisible"));
            Equal(true, dashboardCode.Contains(
                "Loaded += PlaytimeInsightsDashboardView_Loaded;"));
            Equal(false, dashboardCode.Contains("IsCompactHeroLayout"));
            Equal(false, dashboardCode.Contains("SizeChanged"));
            var drilldownReveal = ExtractSourceBlock(
                dashboardCode,
                "private void DrilldownHost_IsVisibleChanged(",
                "private static bool IsHeaderBandVisible(");
            Equal(false, drilldownReveal.Contains("BeginAnimation"));
            Equal(false, drilldownReveal.Contains(".Focus("));
            Equal(false, drilldownReveal.Contains("Keyboard.Focus"));
            foreach (var hostName in new[]
            {
                "TrendDrilldownHost",
                "DistributionDrilldownHost"
            })
            {
                var host = modulesByName[hostName];
                Equal(false, host.DescendantsAndSelf().Attributes().Any(
                    attribute =>
                        attribute.Name.LocalName == "Opacity" ||
                        attribute.Name.LocalName == "RenderTransform"));
            }
        }

        private static void TestDashboardVisualRefactorContract()
        {
            var sourceRoot = FindSourceRoot();
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboardSource = File.ReadAllText(dashboardPath);
            var dashboard = XDocument.Load(dashboardPath);
            var sessionManagement = XDocument.Load(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));
            var app = XDocument.Load(Path.Combine(
                sourceRoot,
                "App.xaml"));
            var pluginSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "PlaytimeInsights.cs"));
            var dashboardViewModelSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            var adaptiveTrendChartSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Controls",
                "AdaptiveTrendChart.cs"));

            Equal(
                true,
                Enum.GetNames(typeof(DashboardLayoutZone)).SequenceEqual(
                    new[] { "Primary", "Secondary" }));

            var adaptivePanels = dashboard.Descendants()
                .Where(element =>
                    element.Name.LocalName == "AdaptiveDashboardPanel")
                .ToList();
            var responsivePanels = dashboard.Descendants()
                .Where(element =>
                    element.Name.LocalName == "ResponsiveUniformPanel")
                .ToList();
            var metricGrids = dashboard.Descendants()
                .Where(element =>
                    element.Name.LocalName == "UniformGrid" &&
                    (string)element.Attribute("Columns") == "4")
                .ToList();
            Equal(1, adaptivePanels.Count);
            Equal(1, responsivePanels.Count);
            Equal(0, metricGrids.Count);

            var metricCards = responsivePanels[0].Elements().ToList();
            Equal(8, metricCards.Count);
            Equal(true, metricCards.All(element =>
                element.Name.LocalName == "Border"));

            foreach (var rankingBinding in new[]
            {
                "{Binding RangeGameRankings}",
                "{Binding LifetimeGameRankings}"
            })
            {
                Equal(1, dashboard.Root.DescendantsAndSelf()
                    .Attributes()
                    .Count(attribute =>
                        attribute.Name.LocalName == "ItemsSource" &&
                        attribute.Value == rankingBinding));
            }

            const string sharedDictionarySource =
                "../Resources/PlaytimeInsightsVisualResources.xaml";
            foreach (var viewDocument in new[]
            {
                dashboard,
                sessionManagement
            })
            {
                Equal(1, viewDocument.Descendants()
                    .Count(element =>
                        element.Name.LocalName == "ResourceDictionary" &&
                        (string)element.Attribute("Source") ==
                        sharedDictionarySource));
            }
            Equal(0, app.Descendants()
                .Count(element =>
                    element.Name.LocalName == "ResourceDictionary" &&
                    (string)element.Attribute("Source") ==
                    sharedDictionarySource));

            foreach (var deferredNavigationToken in new[]
            {
                "IDashboardNavigation",
                "NavigateToDashboard",
                "VisualTreeHelper",
                "MouseButtonEventArgs"
            })
            {
                Equal(false, pluginSource.Contains(
                    deferredNavigationToken));
            }

            foreach (var forbiddenViewModelState in new[]
            {
                "IsWideLayout",
                "IsCompactHeroLayout",
                "LayoutWidth",
                "DashboardWidth",
                "ColumnWidth",
                "IsFilterExpanded",
                "FilterExpansion",
                "ExpandedWidth"
            })
            {
                Equal(false, dashboardViewModelSource.Contains(
                    forbiddenViewModelState));
            }

            RunOnSta(() =>
            {
                var adaptivePanel = new AdaptiveDashboardPanel();
                Equal(1200d, adaptivePanel.EnterWideWidth);
                Equal(1160d, adaptivePanel.ExitWideWidth);
                Equal(18d, adaptivePanel.ColumnSpacing);
                Equal(18d, adaptivePanel.VerticalSpacing);
                Equal(0.38d, adaptivePanel.SecondaryColumnRatio);
            });

            var drilldownList = dashboard.Descendants()
                .Single(element =>
                    element.Name.LocalName == "ListView" &&
                    (string)element.Attribute("ItemsSource") ==
                    "{Binding SessionDetails}");
            Equal("True", (string)drilldownList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "VirtualizingPanel.IsVirtualizing"));
            Equal("Recycling", (string)drilldownList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "VirtualizingPanel.VirtualizationMode"));
            Equal("True", (string)drilldownList.Attributes()
                .Single(attribute =>
                    attribute.Name.LocalName ==
                    "ScrollViewer.CanContentScroll"));
            Equal(1, drilldownList.Descendants()
                .Count(element =>
                    element.Name.LocalName ==
                    "VirtualizingStackPanel"));

            var rootScrollViewer = dashboard.Descendants()
                .Single(element =>
                    element.Name.LocalName == "ScrollViewer" &&
                    (string)element.Attribute(xamlNamespace + "Name") ==
                    "DashboardScrollViewer");
            Equal("Disabled", (string)rootScrollViewer.Attribute(
                "HorizontalScrollBarVisibility"));

            Equal(0, dashboard.Root.DescendantsAndSelf()
                .Attributes()
                .Count(attribute =>
                    attribute.Value == "#FF2A2A2E" ||
                    attribute.Value.Contains("HeatmapEmptyBrush")));
            Equal(1, dashboard.Descendants()
                .Count(element =>
                    element.Name.LocalName == "Border" &&
                    (string)element.Attribute("Background") ==
                    "{DynamicResource TextBrush}" &&
                    (string)element.Attribute("Opacity") == "0.06"));
            Equal(false, adaptiveTrendChartSource.Contains(
                "Color.FromArgb(220, 35, 37, 44)"));
            var quote = ((char)34).ToString();
            foreach (var themeBrush in new[]
            {
                "ResolveBrush(" + quote + "PopupBackgroundBrush" + quote,
                "ResolveBrush(" + quote + "PanelSeparatorBrush" + quote,
                "ResolveBrush(" + quote + "GlyphBrush" + quote,
                "ResolveBrush(" + quote + "ControlBackgroundBrush" + quote
            })
            {
                Equal(true, adaptiveTrendChartSource.Contains(themeBrush));
            }
        }

        private static void TestAnomalyModuleReviewTitleLocalization()
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
            string GetValue(XDocument resourceDocument, string key)
            {
                return resourceDocument.Descendants()
                    .Where(element =>
                        (string)element.Attribute(xamlNamespace + "Key") == key)
                    .Select(element => element.Value)
                    .SingleOrDefault();
            }

            Equal("Suspicious sessions",
                GetValue(english, "LOCPlaytimeInsightsAnomalyReviewTitle"));
            Equal("异常会话",
                GetValue(chinese, "LOCPlaytimeInsightsAnomalyReviewTitle"));

            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboard = File.ReadAllText(dashboardPath);
            var document = XDocument.Load(dashboardPath);
            Equal(1, Regex.Matches(
                dashboard,
                "LOCPlaytimeInsightsAnomalyReviewTitle").Count);
            var anomalyModule = document.Descendants()
                .Where(element =>
                    element.Name.LocalName == "Border" &&
                    (string)element.Attribute(xamlNamespace + "Name") ==
                    "AnomalyModule")
                .Single();
            Equal(true, anomalyModule.DescendantsAndSelf()
                .Attributes()
                .Any(attribute =>
                    attribute.Value ==
                    "{DynamicResource LOCPlaytimeInsightsAnomalyReviewTitle}"));
            Equal(false, anomalyModule.DescendantsAndSelf()
                .Attributes()
                .Any(attribute =>
                    attribute.Value ==
                    "{DynamicResource LOCPlaytimeInsightsAnomalyReadOnly}"));
        }
        private static string GetStyleSetterValue(
            XElement style,
            string propertyName)
        {
            var setter = style.Descendants()
                .Single(element =>
                    element.Name.LocalName == "Setter" &&
                    (string)element.Attribute("Property") == propertyName);
            return (string)setter.Attribute("Value");
        }

        private static void TestExplicitVisualResourceMerges()
        {
            var sourceRoot = FindSourceRoot();
            const string merge =
                @"<ResourceDictionary Source=""../Resources/PlaytimeInsightsVisualResources.xaml"" />";
            var dashboardSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml"));
            var managementSource = File.ReadAllText(Path.Combine(
                sourceRoot,
                "Views",
                "SessionManagementView.xaml"));

            Equal(true, dashboardSource.Contains(merge));
            Equal(true, managementSource.Contains(merge));

            RunOnSta(() =>
            {
                var dashboard = new PlaytimeInsightsDashboardView();
                var coordinator = new SessionManagementCoordinator(
                    new FakeSessionManagementOperations(),
                    new FakeSessionManagementInteraction());
                var management = new SessionManagementView(coordinator);
                foreach (var view in new FrameworkElement[]
                {
                    dashboard,
                    management
                })
                {
                    view.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
                    view.Resources["ControlBackgroundBrush"] =
                        new SolidColorBrush(Colors.Black);
                    view.Resources["PanelSeparatorBrush"] =
                        new SolidColorBrush(Colors.Gray);
                    view.Resources["PopupBackgroundBrush"] =
                        new SolidColorBrush(Colors.DarkGray);
                    view.Resources["GlyphBrush"] =
                        new SolidColorBrush(Colors.LightGray);
                    view.Measure(new Size(1200, double.PositiveInfinity));
                    view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
                    view.UpdateLayout();
                }

                Equal(true,
                    dashboard.TryFindResource("SessionSourceTagStyle") is Style);
                Equal(true,
                    management.TryFindResource("SessionSourceTagStyle") is Style);
                Equal(true,
                    dashboard.TryFindResource("RankingGoldBrush") is Brush);
                var sharedStyle = dashboard.TryFindResource("SessionSourceTagStyle") as Style;
                Equal(true, sharedStyle != null);
                Equal(4, sharedStyle.Triggers.Count);
                var dashboardLocalKeys = dashboard.Resources.Keys.Cast<object>().ToList();
                var managementLocalKeys = management.Resources.Keys.Cast<object>().ToList();
                Equal(false, dashboardLocalKeys.Contains("RankingGoldBrush"));
                Equal(false, dashboardLocalKeys.Contains("RankingSilverBrush"));
                Equal(false, dashboardLocalKeys.Contains("RankingBronzeBrush"));
                Equal(false, managementLocalKeys.Contains("SourceTagStyle"));
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

        private static int CountVisualTreeNodes(DependencyObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var count = 1;
            for (var index = 0;
                index < VisualTreeHelper.GetChildrenCount(root);
                index++)
            {
                count += CountVisualTreeNodes(
                    VisualTreeHelper.GetChild(root, index));
            }

            return count;
        }

        private static void LayoutDashboardView(
            PlaytimeInsightsDashboardView view)
        {
            view.Measure(new Size(1200, double.PositiveInfinity));
            view.Arrange(new Rect(0, 0, 1200, view.DesiredSize.Height));
            view.UpdateLayout();
        }

        private static void LayoutDashboardViewAt(
            PlaytimeInsightsDashboardView view,
            double width)
        {
            view.Width = width;
            view.Measure(new Size(width, double.PositiveInfinity));
            view.Arrange(new Rect(0, 0, width, view.DesiredSize.Height));
            view.UpdateLayout();
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

        private sealed class WidthSensitiveElement : FrameworkElement
        {
            private readonly double widthThreshold;
            private readonly double wideHeight;
            private readonly double narrowHeight;

            public WidthSensitiveElement(
                double widthThreshold,
                double wideHeight,
                double narrowHeight)
            {
                this.widthThreshold = widthThreshold;
                this.wideHeight = wideHeight;
                this.narrowHeight = narrowHeight;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                var height = availableSize.Width < widthThreshold
                    ? narrowHeight
                    : wideHeight;
                return new Size(0, height);
            }
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
            viewModel.SelectedRangeOption = viewModel.RangeOptions.First(
                option => option.Value == DateRangePreset.Custom);
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

        private static void TestQuickRangeRefreshPurity()
        {
            var reasons = new List<DashboardRefreshReason>();
            var viewModel = new DashboardFilterViewModel(
                null,
                new SessionQueryService(new TestGameMetadataAccessor()),
                7,
                reasons.Add);

            viewModel.SelectRange(DateRangePreset.Last7Days);
            viewModel.SelectRange(DateRangePreset.Last7Days);

            Equal("Range", string.Join("|", reasons));
            Equal(
                DateRangePreset.Last7Days,
                viewModel.SelectedRangeOption.Value);
        }

        private static void TestActiveMetadataFilterSummary()
        {
            var viewModel = new DashboardFilterViewModel(
                null,
                new SessionQueryService(new TestGameMetadataAccessor()),
                7,
                null);
            var activePropertyNames = new HashSet<string>
            {
                nameof(DashboardFilterViewModel.ActiveMetadataFilterCount),
                nameof(DashboardFilterViewModel.ActiveMetadataFilterSummary),
                nameof(DashboardFilterViewModel.ActiveMetadataFilterVisibility)
            };
            var activeNotifications = new List<string>();
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (activePropertyNames.Contains(args.PropertyName))
                {
                    activeNotifications.Add(args.PropertyName);
                }
            };

            Equal(0, viewModel.ActiveMetadataFilterCount);
            Equal(string.Empty, viewModel.ActiveMetadataFilterSummary);
            Equal(Visibility.Collapsed,
                viewModel.ActiveMetadataFilterVisibility);

            activeNotifications.Clear();
            viewModel.SelectedMetadataDimensionOption =
                viewModel.MetadataDimensionOptions.First(
                    option => option.Value.HasValue);
            AssertActiveMetadataNotifications(
                activePropertyNames,
                activeNotifications);
            Equal(0, viewModel.ActiveMetadataFilterCount);
            Equal(string.Empty, viewModel.ActiveMetadataFilterSummary);
            Equal(Visibility.Collapsed,
                viewModel.ActiveMetadataFilterVisibility);

            activeNotifications.Clear();
            viewModel.SelectedMetadataValueOption =
                new SelectionOption<string>
                {
                    Value = "Steam",
                    Label = "Steam"
                };
            AssertActiveMetadataNotifications(
                activePropertyNames,
                activeNotifications);

            Equal(1, viewModel.ActiveMetadataFilterCount);
            Equal(true, viewModel.ActiveMetadataFilterSummary.Contains("1"));
            Equal(Visibility.Visible,
                viewModel.ActiveMetadataFilterVisibility);

            activeNotifications.Clear();
            viewModel.SelectedMetadataValueOption =
                new SelectionOption<string>
                {
                    Value = string.Empty,
                    Label = string.Empty
                };
            AssertActiveMetadataNotifications(
                activePropertyNames,
                activeNotifications);

            Equal(0, viewModel.ActiveMetadataFilterCount);
            Equal(string.Empty, viewModel.ActiveMetadataFilterSummary);
            Equal(Visibility.Collapsed,
                viewModel.ActiveMetadataFilterVisibility);
        }

        private static void TestSelectRangeCommandBehavior()
        {
            var settings =
                (PlaytimeInsightsSettingsViewModel)
                System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(
                        typeof(PlaytimeInsightsSettingsViewModel));
            settings.Settings = new PlaytimeInsightsSettings();
            var viewModel = new DashboardViewModel(
                null,
                null,
                new AnalyticsService(),
                new SessionQueryService(new TestGameMetadataAccessor()),
                settings);
            var command = viewModel.SelectRangeCommand;
            var invalidPreset = (DateRangePreset)int.MaxValue;

            Equal(true, command.CanExecute(DateRangePreset.Last7Days));
            Equal(false, command.CanExecute(invalidPreset));

            var guardField = typeof(DashboardViewModel).GetField(
                "refreshGuard",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var raiseCommandStates = typeof(DashboardViewModel).GetMethod(
                "RaiseCommandStates",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Equal(false, guardField == null);
            Equal(false, raiseCommandStates == null);
            var guard = (RefreshReentrancyGuard)guardField.GetValue(viewModel);
            var canExecuteChangedCount = 0;
            command.CanExecuteChanged +=
                (sender, args) => canExecuteChangedCount++;

            Equal(true, guard.TryEnter());
            raiseCommandStates.Invoke(viewModel, null);
            Equal(1, canExecuteChangedCount);
            Equal(false, command.CanExecute(DateRangePreset.Last7Days));

            guard.Exit();
            raiseCommandStates.Invoke(viewModel, null);
            Equal(2, canExecuteChangedCount);
            Equal(true, command.CanExecute(DateRangePreset.Last7Days));
        }

        private static void AssertActiveMetadataNotifications(
            ISet<string> expected,
            IList<string> actual)
        {
            Equal(3, actual.Count);
            Equal(true, expected.SetEquals(actual));
        }

        private static void TestRankingTabsStayViewOnly()
        {
            var sourceRoot = FindSourceRoot();
            var dashboardPath = Path.Combine(
                sourceRoot,
                "Views",
                "PlaytimeInsightsDashboardView.xaml");
            var dashboard = File.ReadAllText(dashboardPath);
            var document = XDocument.Load(dashboardPath);
            var xamlNamespace = XNamespace.Get(
                "http://schemas.microsoft.com/winfx/2006/xaml");
            var dashboardViewModel = File.ReadAllText(Path.Combine(
                sourceRoot,
                "ViewModels",
                "DashboardViewModel.cs"));
            var buttons = document.Descendants()
                .Where(element => element.Name.LocalName == "Button")
                .ToList();
            var quickRangeButtons = buttons
                .Where(button =>
                    (string)button.Attribute("Command") ==
                    "{Binding SelectRangeCommand}")
                .ToList();
            var expectedParameters = new[]
            {
                "{x:Static services:DateRangePreset.Last7Days}",
                "{x:Static services:DateRangePreset.Last30Days}",
                "{x:Static services:DateRangePreset.ThisYear}",
                "{x:Static services:DateRangePreset.AllSessions}"
            };

            Equal(4, quickRangeButtons.Count);
            foreach (var expectedParameter in expectedParameters)
            {
                Equal(1, quickRangeButtons.Count(button =>
                    (string)button.Attribute("CommandParameter") ==
                    expectedParameter));
            }

            var expanders = document.Descendants()
                .Where(element =>
                    element.Name.LocalName == "Expander" &&
                    (string)element.Attribute(xamlNamespace + "Name") ==
                    "AdvancedFilterExpander")
                .ToList();
            Equal(1, expanders.Count);
            var advancedFilterExpander = expanders[0];
            Equal("True",
                (string)advancedFilterExpander.Attribute("IsExpanded"));
            Equal(false,
                ((string)advancedFilterExpander.Attribute("IsExpanded"))
                    .Contains("Binding"));
            Equal(1, advancedFilterExpander.DescendantsAndSelf()
                .Attributes()
                .Count(attribute => attribute.Value ==
                    "{Binding Filter.ActiveMetadataFilterVisibility}"));
            Equal(1, advancedFilterExpander.DescendantsAndSelf()
                .Attributes()
                .Count(attribute => attribute.Value ==
                    "{Binding Filter.ActiveMetadataFilterSummary}"));

            var tabControls = document.Descendants()
                .Where(element => element.Name.LocalName == "TabControl")
                .ToList();
            Equal(1, tabControls.Count);
            var tabControl = tabControls[0];
            var tabItems = tabControl.Descendants()
                .Where(element => element.Name.LocalName == "TabItem")
                .ToList();

            Equal(2, tabItems.Count);
            Equal(true, dashboard.Contains("RangeGameRankings"));
            Equal(true, dashboard.Contains("LifetimeGameRankings"));
            Equal(false, tabControl.DescendantsAndSelf()
                .Attributes()
                .Any(attribute =>
                    attribute.Name.LocalName == "SelectedIndex" ||
                    attribute.Name.LocalName == "SelectionChanged" ||
                    attribute.Name.LocalName == "Command"));
            Equal(false, dashboardViewModel.Contains("SelectedRankingTab"));
            Equal(false, dashboardViewModel.Contains("RankingTab"));
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

        private static void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private static void PumpDispatcherFor(TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
            {
                Thread.Sleep(10);
                PumpDispatcher();
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
            Equal(true, dashboardXaml.Contains("DataContext.SelectHeatmapDateCommand"));
            Equal(false, dashboardCode.Contains("SelectHeatmapDateCommand"));
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
            var preReleaseWorkflow = File.ReadAllText(Path.Combine(
                sourceRoot,
                "docs",
                "PRE_RELEASE_WORKFLOW.md"));

            Equal(true, manifest.Contains("Version: 1.0.0"));
            Equal(true, manifest.Contains("Author: SHINKU1506"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights/issues"));
            Equal(true, manifest.Contains(
                "https://github.com/SHINKU1506/PlaytimeInsights/blob/main/CHANGELOG.md"));
            Equal(true, assemblyInfo.Contains(
                "AssemblyVersion(\"1.0.0.0\")"));
            Equal(true, assemblyInfo.Contains(
                "AssemblyFileVersion(\"1.0.0.0\")"));
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

            Equal(true, readme.Contains("当前版本：`1.0.0`"));
            Equal(true, readme.Contains(
                "作者：[SHINKU1506](https://github.com/SHINKU1506)"));
            Equal(true, readme.Contains("## 界面预览"));
            Equal(true, readme.Contains("## 安装与升级"));
            Equal(true, readme.Contains("## 数据、隐私与诊断"));
            Equal(true, readme.Contains("## 已知限制"));
            Equal(true, readme.Contains("## 从源码构建"));
            Equal(true, readme.Contains("## 问题反馈"));
            Equal(true, readme.Contains("## License"));
            Equal(true, readme.Contains("docs/PRE_RELEASE_WORKFLOW.md"));
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
            Equal(true, installerManifest.Contains("Version: 1.0.0"));
            Equal(true, installerManifest.Contains("Version: 0.9.8"));
            Equal(true, installerManifest.Contains(
                "RequiredApiVersion: 6.16.0"));
            Equal(true, installerManifest.Contains(
                "/releases/download/v1.0.0/PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_1_0_0.pext"));
            Equal(true, preReleaseWorkflow.Contains(
                "PEXT URL returns HTTP 200"));
            Equal(true, preReleaseWorkflow.Contains(
                "Package-only release"));
            Equal(true, preReleaseWorkflow.Contains(
                "git push origin v1.0.0"));
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
                    using (var stream = File.OpenRead(path))
                    using (var streamReader = new StreamReader(stream, System.Text.Encoding.UTF8))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var serializer = JsonSerializer.CreateDefault();
                        document = serializer.Deserialize<SessionStoreDocument>(jsonReader);
                    }
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

        private sealed class CoverImageCacheContract
        {
            private readonly ConstructorInfo constructor;
            private readonly MethodInfo getOrLoad;

            public CoverImageCacheContract(
                ConstructorInfo constructor,
                MethodInfo getOrLoad)
            {
                this.constructor = constructor;
                this.getOrLoad = getOrLoad;
            }

            public object Create(int capacity)
            {
                return constructor.Invoke(new object[] { capacity });
            }

            public BitmapSource GetOrLoad(
                object cache,
                string path,
                int decodePixelWidth)
            {
                return (BitmapSource)getOrLoad.Invoke(
                    cache,
                    new object[] { path, decodePixelWidth });
            }
        }

        private sealed class FakePlayniteApi : IPlayniteAPI
        {
            public FakePlayniteApi(string pathsRoot)
            {
                Paths = new FakePlaynitePathsApi(pathsRoot);
            }

            public IMainViewAPI MainView => null;

            public IGameDatabaseAPI Database => null;

            public IDialogsFactory Dialogs => null;

            public IPlaynitePathsAPI Paths { get; }

            public INotificationsAPI Notifications => null;

            public IPlayniteInfoAPI ApplicationInfo => null;

            public IWebViewFactory WebViews => null;

            public IResourceProvider Resources => null;

            public IUriHandlerAPI UriHandler => null;

            public IPlayniteSettingsAPI ApplicationSettings => null;

            public IAddons Addons => null;

            public IEmulationAPI Emulation => null;

            public string ExpandGameVariables(
                Playnite.SDK.Models.Game game,
                string inputString)
            {
                return null;
            }

            public string ExpandGameVariables(
                Playnite.SDK.Models.Game game,
                string inputString,
                string emulatorDir)
            {
                return null;
            }

            public Playnite.SDK.Models.GameAction ExpandGameVariables(
                Playnite.SDK.Models.Game game,
                Playnite.SDK.Models.GameAction action)
            {
                return null;
            }

            public void StartGame(Guid gameId)
            {
            }

            public void InstallGame(Guid gameId)
            {
            }

            public void UninstallGame(Guid gameId)
            {
            }

            public void AddCustomElementSupport(
                Plugin source,
                AddCustomElementSupportArgs args)
            {
            }

            public void AddSettingsSupport(
                Plugin source,
                AddSettingsSupportArgs args)
            {
            }

            public void AddConvertersSupport(
                Plugin source,
                AddConvertersSupportArgs args)
            {
            }

            public List<GamepadController> GetConnectedControllers()
            {
                return new List<GamepadController>();
            }
        }

        private sealed class FakePlaynitePathsApi : IPlaynitePathsAPI
        {
            private readonly string root;

            public FakePlaynitePathsApi(string root)
            {
                this.root = root;
            }

            public bool IsPortable => false;

            public string ApplicationPath => root;

            public string ConfigurationPath => root;

            public string ExtensionsDataPath => root;
        }
    }
}
