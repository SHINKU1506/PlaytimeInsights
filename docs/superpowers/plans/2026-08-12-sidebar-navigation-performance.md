# Sidebar Navigation Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure each Playtime Insights sidebar page performs one automatic refresh per navigation and make the session count use the already-loaded refresh snapshot.

**Architecture:** Keep `Loaded` as the sole automatic refresh boundary for both WPF views. Preserve Dashboard's plugin-lifetime ViewModel cache, explicit refresh commands, filter-triggered refreshes, CRUD refreshes and game-stop refreshes. Store the active session count calculated from the single `GetAllIncludingDeleted()` result in `SessionManagementViewModel` so `CountText` remains a pure state projection.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF, Playnite SDK 6.16.0, existing console regression harness.

## Global Constraints

- Do not cache `SessionManagementViewModel` or the Playnite game list.
- Do not introduce database event subscriptions, asynchronous refresh, new runtime dependencies or schema changes.
- Do not change Dashboard's plugin-runtime filter persistence or restart default of “This month”.
- Do not change statistics, plugin ID, XAML layout or localization text.
- Do not modify or commit the user-owned untracked `perf_test.ps1`.
- Every production change must be preceded by a regression that fails for the expected current behavior.

---

### Task 1: Remove duplicate automatic navigation refreshes

**Files:**
- Modify: `Tests/Program.cs`
- Modify: `PlaytimeInsights.cs:187-239`

**Interfaces:**
- Consumes: existing `SidebarItem.Opened`, `PlaytimeInsightsDashboardView.Loaded` and `SessionManagementView.Loaded` lifecycle.
- Produces: one automatic refresh path per navigation, with `Loaded` as the owner.

- [ ] **Step 1: Add the failing navigation lifecycle regression**

Register `TestSidebarNavigationUsesSingleAutomaticRefresh` and implement it to read the plugin and both View code-behind files. The test must independently identify each `Opened` lambda by its sidebar icon and assert:

```csharp
Equal(false, dashboardOpened.Contains("activeDashboard.Refresh()"));
Equal(false, sessionsOpened.Contains("activeSessionManagement.Refresh()"));
Equal(true, dashboardView.Contains("Loaded += PlaytimeInsightsDashboardView_Loaded"));
Equal(true, dashboardView.Contains("command.Execute(null)"));
Equal(true, sessionView.Contains("Loaded += SessionManagementView_Loaded"));
Equal(true, sessionView.Contains("ViewModel?.Refresh()"));
Equal(true, dashboardOpened.Contains("activeDashboard = cachedDashboard"));
Equal(true, plugin.Contains("Closed = () => activeDashboard = null"));
```

Use a small test-only `ExtractSidebarOpenedBlock(string source, string iconName)` helper that finds the sidebar item containing the literal icon filename, then returns text from `Opened = () =>` through its following `Closed =`. This ensures the assertion targets the correct navigation entry rather than matching unrelated explicit refresh paths.

- [ ] **Step 2: Run the regression and verify RED**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build
```

Expected: exactly the new navigation regression fails because both extracted `Opened` blocks currently contain their active ViewModel `Refresh()` call.

- [ ] **Step 3: Implement the minimal navigation fix**

Delete only these two statements from `PlaytimeInsights.cs`:

```csharp
activeDashboard.Refresh();
activeSessionManagement.Refresh();
```

Do not change constructors, `Loaded` handlers, `Closed` handlers, explicit commands, cache ownership or `RefreshOpenDashboard` / `RefreshOpenAnalytics`.

- [ ] **Step 4: Verify GREEN**

Rebuild and run the regression harness. Expected: the new navigation test and all existing tests pass, with zero build warnings and errors.

- [ ] **Step 5: Commit Task 1**

```powershell
git add PlaytimeInsights.cs Tests\Program.cs
git commit -m "perf(navigation): remove duplicate sidebar refreshes"
```

---

### Task 2: Make session count a refresh-snapshot projection

**Files:**
- Modify: `Tests/Program.cs`
- Modify: `ViewModels/SessionManagementViewModel.cs:48-290`

**Interfaces:**
- Consumes: `IReadOnlyList<GameSession> allSessions` returned once by `SessionRepository.GetAllIncludingDeleted()`.
- Produces: private `int activeSessionCount` and repository-free `CountText` projection.

- [ ] **Step 1: Add the failing session count boundary regression**

Register `TestSessionCountUsesRefreshSnapshot`. Read `SessionManagementViewModel.cs`, extract the `CountText` property through `LoadMoreVisibility`, and extract `Refresh()` through `LoadMore()`. Assert:

```csharp
Equal(false, countTextBlock.Contains("repository.GetAll()"));
Equal(true, countTextBlock.Contains("activeSessionCount"));
Equal(true, refreshBlock.Contains(
    "activeSessionCount = allSessions.Count(session => !session.IsDeleted)"));
Equal(1, Regex.Matches(
    refreshBlock,
    @"repository\.GetAllIncludingDeleted\(\)",
    RegexOptions.CultureInvariant).Count);
```

The production mutation this catches is reintroducing a repository read in a frequently evaluated binding property or failing to update the count from the current refresh input.

- [ ] **Step 2: Run the regression and verify RED**

Run the rebuilt regression harness. Expected: exactly the new session count test fails because `CountText` currently calls `repository.GetAll()` and no `activeSessionCount` exists.

- [ ] **Step 3: Implement the minimal count fix**

Add:

```csharp
private int activeSessionCount;
```

Change the third `CountText` argument to `activeSessionCount`. Immediately after obtaining `allSessions` in `Refresh()`, assign:

```csharp
activeSessionCount = allSessions.Count(session => !session.IsDeleted);
```

Keep the existing `NotifyPagingChanged()` call, which already raises `OnPropertyChanged(nameof(CountText))` after every refresh and page append.

- [ ] **Step 4: Verify GREEN**

Rebuild and run all tests. Expected: both new tests and the full suite pass with zero warnings and errors.

- [ ] **Step 5: Commit Task 2**

```powershell
git add ViewModels\SessionManagementViewModel.cs Tests\Program.cs
git commit -m "perf(sessions): reuse refresh snapshot for counts"
```

---

### Task 3: Final verification, documentation and deployment

**Files:**
- Modify: `docs/superpowers/specs/2026-08-12-sidebar-navigation-performance-design.md`
- Modify: `docs/ARCHITECTURE_OPTIMIZATION_PLAN.md`
- Modify: `docs/DEVELOPMENT.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`

**Interfaces:**
- Consumes: completed Tasks 1 and 2.
- Produces: verified Release artifacts in `staging/architecture-stage-d` and the installed plugin directory, plus a reproducible handoff record.

- [ ] **Step 1: Run a clean Release build and complete regression suite**

Run:

```powershell
dotnet clean PlaytimeInsights.csproj -c Release
dotnet build PlaytimeInsights.csproj -c Release --no-restore
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build
```

Expected: zero warnings, zero errors, every existing and new regression passes, and the 100k-session analytics timing remains inside its existing budget.

- [ ] **Step 2: Review scope and production diff**

Run `git diff --check`, inspect `git diff`, verify `perf_test.ps1` remains untracked and unchanged, and confirm there are no changes to XAML, localization, schema, extension metadata or dependencies.

- [ ] **Step 3: Update implementation records**

Mark the design status implemented and document:

- duplicate `Opened` refreshes removed;
- `Loaded` remains the sole automatic entry;
- Dashboard filter cache behavior retained;
- session `CountText` uses the refresh snapshot;
- final test count, build result and 100k timing;
- client verification steps and remaining scope exclusions.

- [ ] **Step 4: Deploy with data protection checks**

Confirm no Playnite process is running. Record the count and combined SHA-256 fingerprint of
`ExtensionsData\7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd`, copy the nine Release files to
`staging\architecture-stage-d` and the installed plugin directory, compare each file's SHA-256 across all three locations, and verify the user-data count and fingerprint are unchanged.

- [ ] **Step 5: Commit documentation and push**

```powershell
git add docs\superpowers\specs\2026-08-12-sidebar-navigation-performance-design.md docs\ARCHITECTURE_OPTIMIZATION_PLAN.md docs\DEVELOPMENT.md docs\IMPLEMENTATION_STATUS.md
git commit -m "docs(performance): record sidebar refresh fix"
git push origin refactor/architecture-preparation
```

- [ ] **Step 6: Hand off client verification**

Ask the user to test Dashboard entry, session page entry, explicit refresh buttons, Dashboard filter persistence across navigation, session counts/filter/pagination, and the restart default.
