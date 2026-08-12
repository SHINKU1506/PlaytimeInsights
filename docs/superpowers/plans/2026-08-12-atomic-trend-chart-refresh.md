# Atomic Trend Chart Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make aggregation changes publish one complete period sequence and cause `AdaptiveTrendChart` to redraw from the new data without waiting for scrolling, layout, viewport or mouse events.

**Architecture:** `DashboardDistributionViewModel` will publish `PeriodActivities` as a newly allocated `IReadOnlyList<PeriodActivityViewModel>` once per snapshot instead of mutating one observable collection. `AdaptiveTrendChart` will still support any `IEnumerable`, using a dependency-property callback and WPF's weak `CollectionChangedEventManager` to reset interaction/render caches and invalidate itself when the source reference or current source contents change.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF dependency properties and rendering, `INotifyCollectionChanged`, Playnite SDK, existing console regression harness.

## Global Constraints

- Work only on `refactor/architecture-preparation`; do not merge to `main` during this implementation.
- Preserve one synchronous `DashboardSnapshot` per refresh and do not introduce `Task.Run`, Dispatcher forcing, cancellation or loading UI in this change.
- Preserve aggregation math, automatic-granularity rules, chart gradients/smoothing/labels/Crosshair, period selection and drilldown bounds.
- Do not call `UpdateLayout()` or synchronous `Dispatcher.Invoke`; one completed data publication schedules one normal WPF render pass.
- Do not change XAML layout, localization, plugin ID, version, session schema or user data.
- Do not modify, stage or commit the user-owned untracked `perf_test.ps1`.
- Use real WPF objects in the chart regression and run them on a dedicated STA thread.

---

### Task 1: Publish period activities atomically

**Files:**
- Modify: `Tests/Program.cs`
- Modify: `ViewModels/Dashboard/DashboardDistributionViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`

**Interfaces:**
- Consumes: `DashboardDistributionViewModel.Apply(DashboardSnapshot snapshot)` and `DashboardSnapshot.PeriodActivities`.
- Produces: `IReadOnlyList<PeriodActivityViewModel> DashboardDistributionViewModel.PeriodActivities` and the matching read-only forwarding property on `DashboardViewModel`.

- [ ] **Step 1: Register a behavior regression**

Add this test registration immediately after the Stage E architecture closure test:

```csharp
Run("Trend periods publish one complete replacement", TestTrendPeriodsPublishAtomically);
```

- [ ] **Step 2: Add the failing real-ViewModel test**

Create two complete minimal `DashboardSnapshot` values through a test helper. Apply the first snapshot, subscribe to `PropertyChanged`, apply the second, then assert the collection reference changed, `PeriodActivities` raised exactly once and only the two second-snapshot labels are visible:

```csharp
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
```

Add this literal helper; it initializes every member read by
`DashboardDistributionViewModel.Apply` without calling the production analytics builder:

```csharp
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
        TrendLinePoints = new PointCollection(),
        TrendLineGeometry = Geometry.Empty,
        TrendAreaGeometry = Geometry.Empty,
        TrendPoints = new List<TrendPointViewModel>(),
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
```

- [ ] **Step 3: Run the regression suite and verify RED**

Run:

```powershell
dotnet build PlaytimeInsights.sln -c Release -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build
```

Expected: exactly the new `Trend periods publish one complete replacement` test fails because `ReferenceEquals(oldPeriods, viewModel.PeriodActivities)` is `true`; the existing 85 tests remain green.

- [ ] **Step 4: Implement the minimal atomic publication**

In `DashboardDistributionViewModel`, replace the constructor-owned mutable period collection with a field and property:

```csharp
private IReadOnlyList<PeriodActivityViewModel> periodActivities =
    new List<PeriodActivityViewModel>();

public IReadOnlyList<PeriodActivityViewModel> PeriodActivities
{
    get => periodActivities;
    private set => SetValue(ref periodActivities, value);
}
```

Remove `PeriodActivities = new ObservableCollection<PeriodActivityViewModel>();` from the constructor. In `Apply`, replace `Replace(PeriodActivities, snapshot.PeriodActivities)` with one full-list publication:

```csharp
PeriodActivities = (snapshot.PeriodActivities ??
    Enumerable.Empty<PeriodActivityViewModel>()).ToList();
```

Change only the root forwarding signature:

```csharp
public IReadOnlyList<PeriodActivityViewModel> PeriodActivities =>
    Distribution.PeriodActivities;
```

- [ ] **Step 5: Run tests and verify GREEN**

Run the same Release build and full regression command. Expected: 86/86 tests pass, 0 warnings and 0 errors.

- [ ] **Step 6: Review and commit Task 1**

Run `git diff --check`, confirm aggregation service/XAML are unchanged and `perf_test.ps1` remains untracked, then commit only Task 1 files:

```powershell
git add -- Tests/Program.cs ViewModels/Dashboard/DashboardDistributionViewModel.cs ViewModels/DashboardViewModel.cs
git commit -m "fix(dashboard): publish trend periods atomically"
```

---

### Task 2: Invalidate the self-drawn chart for source lifecycle changes

**Files:**
- Modify: `Tests/PlaytimeInsights.Tests.csproj`
- Modify: `Tests/Program.cs`
- Modify: `Controls/AdaptiveTrendChart.cs`

**Interfaces:**
- Consumes: `AdaptiveTrendChart.ItemsSource` as any `IEnumerable`; optionally consumes `INotifyCollectionChanged` from that source.
- Produces: an `ItemsSourceProperty` callback, weak old/new collection subscription and one private `ResetRenderedState()` cache/invalidity boundary.

- [ ] **Step 1: Enable and register the WPF behavior regression**

Add an explicit `<Reference Include="PresentationFramework" />` to the test project. Add these exact imports to `Tests/Program.cs`:

```csharp
using PlaytimeInsights.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
```

Register:

```csharp
Run("Trend chart follows source lifecycle changes", TestTrendChartSourceLifecycle);
```

- [ ] **Step 2: Add the failing STA integration test**

Run the test body through a `RunOnSta(Action action)` helper that starts a real thread, sets `ApartmentState.STA`, captures/rethrows any exception and always joins. Inside it:

1. Bind an `ObservableCollection<PeriodActivityViewModel>` containing `old`.
2. Measure, arrange and render the real chart to a `RenderTargetBitmap`; reflect the existing private `renderedItems`, `renderedPoints` and `hoverIndex` fields.
3. Set `hoverIndex` to `0`, replace `ItemsSource` with a collection containing `new-a` and `new-b`, and immediately require empty render caches and `hoverIndex == -1`.
4. Render again and require two current cached items.
5. Mutate the detached old collection and require the two-item current cache remains.
6. Mutate the current collection and immediately require the cache is cleared; render and require three current items.

The test catches these production mutations: removing the dependency-property callback, failing to detach the old collection, failing to subscribe to the current collection, or preserving a stale hover/index cache.

- [ ] **Step 3: Run the regression suite and verify RED**

Run the Release build and full regression suite. Expected: the new lifecycle test fails immediately after `ItemsSource` replacement because current code retains the old rendered cache and hover index; the preceding 86 tests remain green.

- [ ] **Step 4: Implement source lifecycle handling**

Add `using System.Collections.Specialized;`. Extend the dependency-property metadata with `OnItemsSourceChanged` while retaining `AffectsRender`:

```csharp
new FrameworkPropertyMetadata(
    null,
    FrameworkPropertyMetadataOptions.AffectsRender,
    OnItemsSourceChanged)
```

Implement the callback with WPF's weak collection event manager:

```csharp
private static void OnItemsSourceChanged(
    DependencyObject dependencyObject,
    DependencyPropertyChangedEventArgs args)
{
    var chart = (AdaptiveTrendChart)dependencyObject;
    var oldCollection = args.OldValue as INotifyCollectionChanged;
    if (oldCollection != null)
    {
        CollectionChangedEventManager.RemoveHandler(
            oldCollection,
            chart.ItemsSource_CollectionChanged);
    }

    var newCollection = args.NewValue as INotifyCollectionChanged;
    if (newCollection != null)
    {
        CollectionChangedEventManager.AddHandler(
            newCollection,
            chart.ItemsSource_CollectionChanged);
    }

    chart.ResetRenderedState();
}
```

The collection handler calls `ResetRenderedState()`. That method resets `hoverIndex`, replaces both cached lists with empty lists and calls `InvalidateVisual()`. Do not call `UpdateLayout`, Dispatcher methods or render directly.

- [ ] **Step 5: Run tests and verify GREEN**

Run the Release build and full suite. Expected: 87/87 tests pass, 0 warnings and 0 errors.

- [ ] **Step 6: Review and commit Task 2**

Run `git diff --check`, inspect `git diff`, confirm source replacement, old-source detachment and current-source mutation are all exercised, then commit:

```powershell
git add -- Tests/PlaytimeInsights.Tests.csproj Tests/Program.cs Controls/AdaptiveTrendChart.cs
git commit -m "fix(chart): redraw when trend data changes"
```

---

### Task 3: Verify, document, deploy and hand off

**Files:**
- Modify: `docs/superpowers/specs/2026-08-12-atomic-trend-chart-refresh-design.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`
- Modify: `docs/DEVELOPMENT.md`
- Modify: `docs/superpowers/plans/2026-08-12-atomic-trend-chart-refresh.md`

**Interfaces:**
- Consumes: green Task 1/2 tree, existing deterministic pack script and installed plugin path.
- Produces: fresh Release/PEXT evidence, deployed nine-file plugin and client acceptance checklist.

- [ ] **Step 1: Record pre-deployment safety state**

Confirm no `Playnite*` process is running. Record all seven files under
`ExtensionsData\7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd` as relative path, length, UTC timestamp and SHA-256, then hash that normalized manifest. If Playnite is open, stop before deployment and request the user to exit it.

- [ ] **Step 2: Run a clean final Release verification**

Run:

```powershell
dotnet clean PlaytimeInsights.sln -c Release -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build PlaytimeInsights.sln -c Release -p:PlayniteInstallDir="D:\software\Playnite" --no-restore
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build
```

Require 0 warnings, 0 errors and 87/87 passing tests. Record the 100k analytics and schema-load timings and require both existing release budgets.

- [ ] **Step 3: Build and inspect one deterministic PEXT**

Run `scripts/Pack-Deterministic.ps1` against `bin/Release/net462` into
`staging/atomic-trend-refresh/dist`. Require the same exact nine entry names as the Stage E release, no PDB/rooted/parent path and no username/development/PDB strings in the DLL. Record DLL and PEXT size/SHA-256.

- [ ] **Step 4: Deploy only the nine Release files**

Copy the verified nine files to `staging/atomic-trend-refresh/deployed` and
`Extensions/PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd`. Compare relative names and every SHA-256 across Release, staging and installed directories. Recompute the user-data manifest fingerprint and require an exact pre/post match.

- [ ] **Step 5: Update status and design evidence**

Mark the design implemented and record root cause, atomic property publication, weak collection subscription, test count, timings, hashes, nine-file deployment match, unchanged user-data fingerprint and remaining client checks. Keep the later asynchronous generation/cancellation route explicitly pending.

- [ ] **Step 6: Run completion verification and commit**

Run the full Release test command fresh after documentation changes, `git diff --check`, inspect the complete diff from `91862b7`, and confirm `perf_test.ps1` hash remains
`2CE6EA067F5321869C2BD8E2E49EE5A6D0E520F8B2F26E7F2822B2AB3F42B12E`.

Commit only status/design/plan files:

```powershell
git add -- docs/superpowers/specs/2026-08-12-atomic-trend-chart-refresh-design.md docs/IMPLEMENTATION_STATUS.md docs/DEVELOPMENT.md docs/superpowers/plans/2026-08-12-atomic-trend-chart-refresh.md
git commit -m "docs(performance): record trend refresh fix"
```

- [ ] **Step 7: Push and hand off client checks**

Push `refactor/architecture-preparation`, verify local and remote HEAD match, keep the branch intact and ask the user to exercise the six client checks in the approved design before any merge decision.
