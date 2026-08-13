# Sidebar View and Cover Cache Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reuse the Dashboard WPF visual tree across sidebar activations and prevent repeated synchronous decoding of identical cover thumbnails.

**Architecture:** `PlaytimeInsights` owns one process-lifetime Dashboard View and ViewModel while `Loaded` remains the only automatic refresh event entry. A shared `CoverImageCache` normalizes path/width keys, validates file stamps, returns frozen thumbnails, and bounds retained entries with a 512-entry LRU.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF, Playnite SDK, existing console regression harness.

## Global Constraints

- Do not reuse `SessionManagementView`, `SessionManagementViewModel`, or its Coordinator.
- Do not add background decoding or asynchronous Dashboard analysis.
- Do not add a runtime dependency or change semantic version `0.9.8`.
- `Opened` and `Closed` must not call `Refresh`; `Loaded` remains the only automatic refresh event entry and each Loaded event invokes at most one DataReload.
- WPF may raise Loaded for reasons other than sidebar navigation; tests and comments must not equate the two.
- Dashboard scroll offset, focus, and other pure View state may remain with the cached View and must not be reset on close.
- Cover cache key is normalized full path plus decode width, with `OrdinalIgnoreCase` path semantics.
- Cover cache capacity is exactly 512 entries in production; this is an entry bound, not a strict memory bound.
- File length plus `LastWriteTimeUtc` define cache validity. Missing, invalid, unreadable, or undecodable files return null and remove stale entries.
- Decoded `BitmapSource` values use `OnLoad`, `IgnoreImageCache`, requested `DecodePixelWidth`, and `Freeze()`.
- The current lock only guarantees dictionary/LRU structural safety. No claim of newest-wins concurrent decoding is allowed.
- Preserve the existing untracked root-cause report and `perf_test.ps1`; never add them to an implementation commit unless Task 4 explicitly updates the report.

---

### Task 1: Establish red regression and structural performance tests

**Files:**
- Modify: `Tests/Program.cs`

**Interfaces:**
- Consumes: existing `PlaytimeInsights.GetSidebarItems()`, `CoverImageConverter.Convert`, `RunOnSta`, `WithTempDirectory`, `FindVisualDescendants`, and `ExtractSidebarOpenedBlock`.
- Produces: red tests that can discover the not-yet-existing `PlaytimeInsights.Services.CoverImageCache` through reflection, so the test project continues to compile before production code exists.

- [ ] **Step 1: Add Dashboard runtime and source guards**

Register these tests next to the current sidebar tests:

```csharp
Run("Sidebar navigation reuses Dashboard View", TestSidebarNavigationReusesDashboardView);
Run("Dashboard reentry preserves visual tree", TestDashboardReentryPreservesVisualTree);
Run("Dashboard cache keeps one Loaded refresh boundary", TestDashboardViewCacheRefreshBoundary);
```

Use a minimal fake `IPlayniteAPI` and `IPlaynitePathsAPI` whose paths point at `WithTempDirectory`; all unrelated SDK properties return null. In STA, call the Dashboard item's `Opened` twice and once after `Closed`; assert all returned controls and DataContexts are reference-equal. Layout the first View, retain its `DashboardScrollViewer`, close/reopen, layout again, and assert the ScrollViewer is the same object and image-node count does not grow.

The source guard must assert:

```csharp
Equal(true, plugin.Contains("private PlaytimeInsightsDashboardView cachedDashboardView;"));
Equal(false, dashboardOpened.Contains("activeDashboard.Refresh()"));
Equal(false, plugin.Contains("Closed = () => cachedDashboardView = null"));
Equal(1, Regex.Matches(plugin, @"new PlaytimeInsightsDashboardView").Count);
Equal(1, Regex.Matches(dashboardView,
    "Loaded += PlaytimeInsightsDashboardView_Loaded").Count);
Equal(true, dashboardViewModel.Contains(
    "Refresh(DashboardRefreshReason.DataReload)"));
```

- [ ] **Step 2: Add cover-cache behavior tests without compile-time cache dependency**

Register:

```csharp
Run("Cover cache reuses normalized path", TestCoverCacheReusesNormalizedPath);
Run("Cover cache invalidates changed and missing files", TestCoverCacheInvalidatesFiles);
Run("Cover cache separates widths and evicts LRU", TestCoverCacheWidthsAndLru);
Run("Cover decoder returns frozen thumbnail", TestCoverDecoderReturnsFrozenThumbnail);
```

Use reflection to load type `PlaytimeInsights.Services.CoverImageCache`, construct it with a test capacity (`2` for LRU tests), and invoke `GetOrLoad(string, int)`. If the type or API is missing, fail with a message naming the missing contract instead of throwing an unclassified reflection exception.

Use copied repository PNG assets in a temp directory. Assert:

- same path and `path\..\directory\file` return the same object;
- width 96 and 48 return distinct objects;
- changing length or `LastWriteTimeUtc` returns a different object;
- deleting a previously loaded file returns null;
- capacity 2 follows access-order LRU (`A`, `B`, touch `A`, add `C`, then `B` decodes to a new object);
- returned images are frozen and have pixel width no greater than the requested width;
- after `OnLoad`, the source file can be replaced/deleted.

Also call two independent `CoverImageConverter` instances with the same path and assert they return the same object, proving the XAML-local converters share the process cache.

- [ ] **Step 3: Build and verify the red state**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
& '.\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe'
```

Expected: build succeeds; the new Dashboard identity/source tests and cover cache tests fail for the intended missing behaviors, while pre-existing tests remain passing. Record the exact failing test names and messages in the task report.

- [ ] **Step 4: Commit only the red tests**

```powershell
git add -- Tests/Program.cs
git commit -m "test: cover sidebar view and thumbnail reuse"
```

### Task 2: Reuse the Dashboard View safely

**Files:**
- Modify: `PlaytimeInsights.cs`

**Interfaces:**
- Consumes: Task 1 Dashboard identity, visual-tree, and refresh-boundary tests.
- Produces: plugin-instance field `private PlaytimeInsightsDashboardView cachedDashboardView;` and Dashboard `Opened` returning that single View.

- [ ] **Step 1: Confirm the Dashboard tests are red for the expected reason**

Run the regression executable and record the three Dashboard failures. Do not modify tests.

- [ ] **Step 2: Implement the minimal cached View lifecycle**

Add the View field beside `cachedDashboard`. In Dashboard `Opened`, retain the existing one-time ViewModel creation, then create the View only when `cachedDashboardView == null`:

```csharp
if (cachedDashboardView == null)
{
    cachedDashboardView = new PlaytimeInsightsDashboardView
    {
        DataContext = cachedDashboard
    };
}

activeDashboard = cachedDashboard;
return cachedDashboardView;
```

Keep `Closed = () => activeDashboard = null`. Do not reset DataContext, scroll offset, focus, filters, or drilldown state. Do not add `Unloaded`, theme, or explicit refresh handlers.

- [ ] **Step 3: Verify this slice**

Run the full regression executable. Expected: all Dashboard tests pass; cover-cache tests remain red until Task 3; no pre-existing test regresses.

- [ ] **Step 4: Commit the lifecycle change**

```powershell
git add -- PlaytimeInsights.cs
git commit -m "perf: reuse dashboard sidebar view"
```

### Task 3: Add the bounded cover thumbnail cache

**Files:**
- Create: `Services/CoverImageCache.cs`
- Modify: `Converters/CoverImageConverter.cs`

**Interfaces:**
- Consumes: Task 1 reflection contract `CoverImageCache(int capacity)` and `BitmapSource GetOrLoad(string path, int decodePixelWidth)`.
- Produces: default process cache with capacity 512, file-stamp invalidation, access-order LRU, and shared converter usage.

- [ ] **Step 1: Confirm cover tests are red for the expected reason**

Run the regression executable and record the cover failures. Do not modify tests.

- [ ] **Step 2: Implement focused cache types**

`CoverImageCache.cs` contains:

```csharp
internal struct CoverFileStamp
{
    public long Length { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
}

internal interface ICoverFileStampProvider
{
    bool TryGetStamp(string path, out CoverFileStamp stamp);
}

internal interface ICoverImageDecoder
{
    BitmapSource Decode(string path, int decodePixelWidth);
}

public sealed class CoverImageCache
{
    public CoverImageCache(int capacity);
    internal CoverImageCache(
        int capacity,
        ICoverFileStampProvider stampProvider,
        ICoverImageDecoder decoder);
    public BitmapSource GetOrLoad(string path, int decodePixelWidth);
}
```

Reject non-positive capacity. Normalize with `Path.GetFullPath`; reject blank paths and non-positive widths. Use a key struct or string that includes normalized path and width, comparing path with `OrdinalIgnoreCase`. Store stamp, frozen image, and `LinkedListNode<CacheKey>`; move hits to the LRU front and evict from the back until `Count <= capacity`.

Read stamps and decode outside the private cache lock. On commit, if an entry with the same stamp already exists, return it and discard the duplicate. Otherwise replace/insert the current result. This guarantees container integrity only; do not claim newest-wins for future concurrent decoding.

Default decoder must use:

```csharp
image.CacheOption = BitmapCacheOption.OnLoad;
image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
image.DecodePixelWidth = decodePixelWidth;
image.UriSource = new Uri(path, UriKind.Absolute);
image.EndInit();
image.Freeze();
```

All path, stamp, and decode exceptions return null and remove a stale entry for that key.

- [ ] **Step 3: Route converters through one process cache**

Replace per-call decode logic with:

```csharp
private const int DecodePixelWidth = 96;
private static readonly CoverImageCache cache = new CoverImageCache(512);

return cache.GetOrLoad(path, DecodePixelWidth);
```

Keep `ConvertBack` unchanged. Separate converter instances from both XAML views must hit this same static cache.

- [ ] **Step 4: Verify this slice**

Run the full regression executable. Expected: all cover tests pass; no pre-existing cover/XAML/static test regresses.

- [ ] **Step 5: Commit the cache change**

```powershell
git add -- Services/CoverImageCache.cs Converters/CoverImageConverter.cs
git commit -m "perf: cache decoded cover thumbnails"
```

### Task 4: Integrate, document, measure, and deploy

**Files:**
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/DEVELOPMENT.md`
- Modify: `docs/superpowers/specs/2026-08-13-sidebar-lag-root-cause-analysis.md`
- Modify: `docs/superpowers/specs/2026-08-14-sidebar-view-and-cover-cache-design.md`

**Interfaces:**
- Consumes: Tasks 1–3 implementation and test evidence.
- Produces: final verification record and local deployed Release.

- [ ] **Step 1: Run fresh integrated verification**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore
& '.\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe'
git diff --check
```

Expected: 0 warnings, 0 errors, every regression passes including the 100,000-session budgets, and no whitespace errors.

- [ ] **Step 2: Repeat structural performance evidence**

Run the Dashboard independent-process layout benchmark and cover converter benchmark used by the root-cause analysis. Record first layout versus reused layout and first decode versus cache-hit timing as diagnostics only; pass/fail remains based on object identity, visual-tree identity, decode identity, and 512-entry capacity.

- [ ] **Step 3: Update documentation**

Document the process-lifetime Dashboard View, Loaded event semantics, preserved pure View state, 512-entry cache, file-stamp invalidation, typical—not maximum—memory estimate, and the current concurrency limitation. Mark the root cause P0/P1 items implemented and include fresh verification data.

- [ ] **Step 4: Independent whole-change review**

Give an independent reviewer the approved spec, this plan, commit range from `ae13fd9` to current HEAD, and full diff. Fix every Critical/Important finding and re-run covering tests before proceeding.

- [ ] **Step 5: Deploy with rollback**

Gracefully close Playnite; abort instead of force-killing if it does not exit within 30 seconds. Back up the installed extension under `C:\Users\chan\AppData\Roaming\Playnite\Backup\PlaytimeInsights-deploy-<timestamp>`, copy the exact nine Release artifacts, verify source/deployed SHA-256 equality, restart Playnite, and confirm the fresh log loads Playtime Insights `0.9.8` with a responding process.

- [ ] **Step 6: Client acceptance**

Verify both sidebar entries, repeated Dashboard entry, retained Dashboard scroll position, explicit refresh, session-page covers, a theme switch, DPI/window resizing, and game-stop refresh behavior. Record any host-only limitation without claiming it passed.

- [ ] **Step 7: Commit documentation only**

```powershell
git add -- docs/ARCHITECTURE.md docs/DEVELOPMENT.md docs/superpowers/specs/2026-08-13-sidebar-lag-root-cause-analysis.md docs/superpowers/specs/2026-08-14-sidebar-view-and-cover-cache-design.md
git commit -m "docs: record sidebar performance implementation"
```
