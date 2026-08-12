# Dashboard Selective Refresh Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop unrelated Dashboard sections from synchronously reloading and republishing whenever one filter changes, while preserving complete refresh correctness.

**Architecture:** A typed `DashboardRefreshReason` and pure `DashboardRefreshPlan` classify every filter change. Full/range/metadata analysis produces a reusable `DashboardAnalysisContext`; aggregation and ranking create small projections from that context, while the Dashboard caches the last database input and publishes major UI lists atomically.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF/MVVM, Playnite SDK, `Stopwatch`/`Trace`, existing console regression harness.

## Global Constraints

- Work only on `refactor/architecture-preparation`; do not merge during this implementation.
- Preserve the aggregation math, ranking math, automatic granularity, localization, chart visuals, drilldown bounds and session schema.
- `DataReload` is the only reason allowed to read Playnite games, repository sessions and library plugins.
- Never move Playnite `Game`, resource access, live database enumeration or WPF objects into `Task.Run` in this phase.
- Aggregation and ranking must use the last `DashboardAnalysisContext`; if no context exists, fall back to `DataReload`.
- Major complete-snapshot lists publish one new `IReadOnlyList<T>` reference rather than `Clear + Add`.
- Keep incremental session-detail paging as `ObservableCollection`; it has true append semantics.
- Diagnostics must contain only reason and elapsed milliseconds, never game/session/filter/path data.
- Do not modify, stage or commit the user-owned `perf_test.ps1`.

---

### Task 1: Route filter changes through typed refresh plans

**Files:**
- Create: `ViewModels/Dashboard/DashboardRefreshPlan.cs`
- Modify: `ViewModels/Dashboard/DashboardFilterViewModel.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Produces: `DashboardRefreshReason`, `DashboardRefreshMode`, `DashboardRefreshPlan.Create(DashboardRefreshReason reason, bool cacheReady)` and `Action<DashboardRefreshReason>` filter callback.
- Consumes: no Playnite API state in the plan; the root ViewModel will consume the plan in Task 4.

- [x] Add registrations `Dashboard filters route selective refresh reasons` and `Dashboard refresh plans isolate dependencies`.
- [x] Write a failing real `DashboardFilterViewModel` test using `null` Playnite API and a captured `List<DashboardRefreshReason>`. Assert Range, Aggregation, Ranking, MetadataDimension, MetadataValue and Custom-date routing with literal expected sequences.
- [x] Write a failing table test for `DashboardRefreshPlan.Create`: uncached local reasons fall back to `DataReload`; cached Aggregation uses `TrendOnly`; cached Ranking uses `RankingOnly`; Range uses `FullAnalysis` without reload/filter; MetadataDimension refreshes options and filter; MetadataValue rebuilds only the filter.
- [x] Build `Tests/PlaytimeInsights.Tests.csproj` and run the suite. Expected RED: new types/callback do not exist.
- [x] Implement the enum/plan and change `DashboardFilterViewModel` callback to `Action<DashboardRefreshReason>`. Remove the dimension setter's direct live `RefreshMetadataValueOptions()` call. Extend that method to accept `IEnumerable<Game> games` plus library names, falling back to live games only for constructor compatibility.
- [x] Rebuild and run. Expected: 89/89 pass, 0 warnings/errors.
- [x] Commit `feat(dashboard): route selective refresh reasons`.

---

### Task 2: Produce reusable analysis context and local projections

**Files:**
- Create: `Services/DashboardAnalysisContext.cs`
- Modify: `Services/AnalyticsService.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Produces:
  - `DashboardSnapshotResult { DashboardSnapshot Snapshot; DashboardAnalysisContext Context; }`
  - `DashboardTrendProjection AnalyticsService.CreateTrendProjection(DashboardAnalysisContext, AggregationPeriod)`
  - `DashboardRankingProjection AnalyticsService.CreateRankingProjection(DashboardAnalysisContext, RankingMetric, int)`
  - `DashboardSnapshotResult AnalyticsService.CreateSnapshotWithContext(IEnumerable<Game>, IEnumerable<GameSession>, AnalyticsQuery)`
- `CreateSnapshot(...)` remains source-compatible and returns `CreateSnapshotWithContext(...).Snapshot`.

- [x] Register `Aggregation projection reuses analysis context` and `Ranking projection reuses analysis context`.
- [x] Write failing behavior tests: build one context from fixed games/sessions, create day/month trend projections and duration/session-count ranking projections, and assert literal labels/counts/order/seconds. Mutate neither input after context creation.
- [x] Run suite. Expected RED: context/projection API missing.
- [x] Promote the existing private range-stat record into `DashboardGameRangeStatistics` in the new context file. Refactor the existing snapshot loop once so it returns both snapshot and context; create full snapshot trend/ranking by calling the new projection methods to prevent duplicate formulas.
- [x] Run suite. Expected: 91/91 pass and existing analytics/performance tests remain green.
- [x] Commit `refactor(analytics): expose reusable dashboard projections`.

---

### Task 3: Apply local projections and publish Dashboard lists atomically

**Files:**
- Modify: `ViewModels/Dashboard/DashboardMetricsViewModel.cs`
- Modify: `ViewModels/Dashboard/DashboardDistributionViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs` (forwarding property types only in this task)
- Modify: `Tests/Program.cs`

**Interfaces:**
- Produces:
  - `DashboardMetricsViewModel.ApplyPeriodTitle(DashboardTrendProjection)`
  - `DashboardMetricsViewModel.ApplyRangeRanking(DashboardRankingProjection, IEnumerable<Game>)`
  - `DashboardDistributionViewModel.ApplyTrend(DashboardTrendProjection)`
  - major list properties as `IReadOnlyList<T>`.

- [x] Register `Trend projection leaves unrelated dashboard state intact`, `Ranking projection leaves unrelated dashboard state intact`, and `Dashboard major lists publish atomically`.
- [x] Write failing tests against real Metrics/Distribution objects. Capture property notifications and references before local apply; assert trend changes only trend/title, ranking changes only range ranking/title, and two full applies replace each major list once with complete content. Verify `SelectWeekday` still toggles selection and replaces the 24-hour list.
- [x] Run suite. Expected RED: partial apply methods and atomic list properties are missing.
- [x] Replace major `ObservableCollection` properties with field-backed `IReadOnlyList` properties and one `SetValue` per full apply. Keep weekday item mutation; implement hour selection by assigning a new complete list. Implement the two local apply boundaries and root forwarding signatures.
- [x] Run suite. Expected: 94/94 pass, XAML ItemsSource bindings unchanged.
- [x] Commit `perf(dashboard): publish only affected dashboard state`.

---

### Task 4: Cache loaded data and execute refresh plans

**Files:**
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Consumes `DashboardRefreshPlan`, cached games/sessions/library names/current filtered lists and `DashboardAnalysisContext`.
- Public `Refresh()` remains `DataReload`; filter callback calls private/internal `Refresh(DashboardRefreshReason)`.

- [ ] Register `Dashboard refresh policy keeps local changes off data reload` as a plan-driven behavior test. Use a small `DashboardRefreshExecutor`-independent policy test if constructing Playnite API is impractical; assert the root consumes every plan flag and no local branch calls `GetLibraryNames`, `Database.Games` or `sessionRepository.GetAll` by reviewing the bounded switch in the same test only as an architecture boundary supplement.
- [ ] Run suite and verify RED against the current unconditional `RefreshCore`.
- [ ] Add cached fields and implement plan execution:
  - `DataReload`: load library names/games/sessions, refresh metadata values, rebuild filter, full snapshot/context/apply;
  - `Range`: reuse filtered lists, full snapshot/context/apply;
  - `MetadataDimension`: refresh options from cached games, rebuild filter, full snapshot/context/apply;
  - `MetadataValue`: rebuild filter, full snapshot/context/apply;
  - `Aggregation`: `CreateTrendProjection`, apply trend/title, reset drilldown selection only;
  - `Ranking`: `CreateRankingProjection`, apply range ranking/title only.
- [ ] Add `Stopwatch` phase markers and one `Trace.WriteLine` record: `PlaytimeInsights Dashboard refresh reason={0} data={1}ms filter={2}ms analytics={3}ms apply={4}ms total={5}ms`.
- [ ] Run suite. Expected: 95/95 pass and all command/reentrancy tests green.
- [ ] Commit `perf(dashboard): reuse data across filter changes`.

---

### Task 5: Final verification, deployment and documentation

**Files:**
- Modify: `docs/superpowers/specs/2026-08-13-dashboard-filter-refresh-performance-design.md`
- Modify: `docs/ARCHITECTURE_OPTIMIZATION_PLAN.md`
- Modify: `docs/DEVELOPMENT.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`
- Modify: `docs/superpowers/plans/2026-08-13-dashboard-filter-refresh-performance.md`

- [ ] Confirm Playnite closed and record the seven-file normalized user-data fingerprint.
- [ ] Clean/build plugin and test projects explicitly; run 95/95 tests and record 100k/schema timings.
- [ ] Build deterministic PEXT into `staging/dashboard-selective-refresh/dist`; require exact nine entries, safe paths and no sensitive DLL strings.
- [ ] Deploy the nine files to `staging/dashboard-selective-refresh/deployed` and installed plugin directory; require 9/9 hashes and unchanged user-data fingerprint.
- [ ] Mark design implemented, record red/green evidence, refresh dependency matrix, test count, timings, hashes and client checks. Keep pure-DTO asynchronous generation/cancellation as the next phase only if range/metadata changes still produce perceptible stalls.
- [ ] Run the full suite fresh, `git diff --check`, inspect complete diff, confirm `perf_test.ps1` hash remains `2CE6EA067F5321869C2BD8E2E49EE5A6D0E520F8B2F26E7F2822B2AB3F42B12E`, commit `docs(performance): record selective refresh optimization`, push the branch and keep it intact for client acceptance.
