# Architecture Stage E Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the architecture refactor with verified event cleanup, explicit View-layer boundaries, current dependency documentation and reproducible two-pass Release/PEXT evidence.

**Architecture:** Preserve every currently wired View event because the audit found no orphan handlers. Add a dynamic handler-symmetry guard so future XAML and programmatic lifecycle events cannot drift from Code-behind. Document the plugin root composition, Dashboard snapshot fan-out and Session coordinator/interaction boundaries in one final architecture reference.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF, Playnite SDK 6.16.0, Playnite Toolbox, existing console regression harness.

## Global Constraints

- Do not require Code-behind line count to be zero.
- Keep `Loaded`, custom control event adaptation, VisualTree/mouse-wheel routing, ContextMenu placement, Window Owner, focus and dialog lifecycle in View code.
- Do not change XAML layout, statistics, session schema, plugin ID, localization copy, version, runtime dependencies or asynchronous behavior.
- Do not delete a handler unless both its event source and behavior are demonstrably unused.
- Do not modify or commit the user-owned untracked `perf_test.ps1`.
- Release output and each PEXT must contain exactly the nine expected files and no PDB.

---

### Task 1: Establish the Stage E closure guard

**Files:**
- Modify: `Tests/Program.cs`
- Create: `docs/ARCHITECTURE.md`

**Interfaces:**
- Consumes: four View XAML files, four View Code-behind files and constructor-level `Loaded +=` subscriptions.
- Produces: `TestStageEArchitectureClosure`, which dynamically rejects orphan event handlers and requires the final architecture reference.

- [ ] Add a regression that collects handler names from XAML attributes `Click`, `PreviewMouseWheel`, `PeriodSelected` and `MouseLeftButtonUp`, plus named `Loaded +=` subscriptions in Code-behind.
- [ ] Collect private methods ending in `_Click`, `_PreviewMouseWheel`, `_PeriodSelected`, `_MouseLeftButtonUp` or `_Loaded`; assert the source and declaration sets are equal.
- [ ] Assert `docs/ARCHITECTURE.md` exists and names `DashboardFilterViewModel`, `DashboardMetricsViewModel`, `DashboardDistributionViewModel`, `DashboardDrilldownViewModel`, `SessionManagementCoordinator`, `ISessionManagementInteraction`, `WpfSessionManagementInteraction`, and the one-snapshot rule.
- [ ] Build and run the suite. Expected RED: architecture reference is absent; handler symmetry already passes and proves there is no production handler to delete.
- [ ] Create `docs/ARCHITECTURE.md` with responsibilities, construction dependencies, runtime refresh paths, retained View adapters and test boundaries.
- [ ] Rebuild and run the full suite. Expected GREEN.
- [ ] Commit with `test(architecture): establish stage E closure guard`.

### Task 2: Annotate retained View boundaries and synchronize baseline

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml.cs`
- Modify: `Views/SessionManagementView.xaml.cs`
- Modify: `Views/SessionEditorWindow.xaml.cs`
- Modify: `Views/SessionImportPreviewWindow.xaml.cs`
- Modify: `docs/ARCHITECTURE_REFACTOR_BASELINE.md`

**Interfaces:**
- Consumes: the audited handler set from Task 1.
- Produces: concise comments explaining why each category remains in View code, and a current post-Stage-E responsibility matrix.

- [ ] Add one concise comment per retained category: lifecycle refresh, custom chart/heatmap adapter, nested scroll routing, ContextMenu placement, coordinator forwarding, work-area sizing and dialog-local validation/file interaction.
- [ ] Do not add comments that merely restate method names or implementation steps.
- [ ] Update the baseline status and composition section: WPF interaction owns file/window work for session management; `SessionManagementView` owns lifecycle, ContextMenu placement and coordinator forwarding; Dashboard root fans one snapshot to four child states.
- [ ] Record the zero-deletion result and dynamic handler-symmetry rule.
- [ ] Run the full regression suite and `git diff --check`.
- [ ] Commit with `docs(architecture): finalize view boundaries`.

### Task 3: Verify command, localization, keyboard and accessibility semantics

**Files:**
- Modify: `Tests/Program.cs` only if a genuine coverage gap is found.
- Modify: `docs/ARCHITECTURE.md`

**Interfaces:**
- Consumes: existing `TestNativeViewAccessibility`, `TestArchitectureRefactorBaseline`, `TestStageBCommandBindings`, localization parity and source coverage tests.
- Produces: an explicit evidence matrix showing every Stage E requirement is covered without fixed Code-behind-size assertions.

- [ ] Map standard commands to bindings and CanExecute sources; verify localized content/automation names and editor/import default/cancel/focus semantics.
- [ ] Confirm existing dynamic tests cover each requirement. Add only a missing behavior/boundary assertion; do not duplicate existing source checks.
- [ ] Document the evidence matrix in `docs/ARCHITECTURE.md`.
- [ ] Run all tests and commit only if code/test changes are required; otherwise include the documentation in Task 2 or Task 4.

### Task 4: Complete two-pass Release and PEXT verification

**Files:**
- Modify: `docs/ARCHITECTURE_OPTIMIZATION_PLAN.md`
- Modify: `docs/DEVELOPMENT.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`
- Modify: `docs/ROADMAP.md`

**Interfaces:**
- Consumes: green Stage E source tree and `D:\software\Playnite\Toolbox.exe pack`.
- Produces: two clean Release builds and two PEXT packages with identical DLL and package hashes, verified contents, deployment hashes and unchanged user-data fingerprint.

- [ ] Confirm Playnite is closed and record the seven-file user-data fingerprint.
- [ ] Clean, build plugin and tests, run the full suite, pack to a fresh `staging/architecture-stage-e/pass-1/dist` directory and capture DLL/PEXT hashes.
- [ ] Remove only verified generated `bin/Release`, `obj/Release`, test Release and Stage-E pass-2 directories through normal `dotnet clean`/fresh output; do not delete source or user data.
- [ ] Repeat clean build, tests and pack into `staging/architecture-stage-e/pass-2/dist`; require pass-1/pass-2 DLL and PEXT SHA-256 equality.
- [ ] Open each PEXT as ZIP and require exactly: DLL, extension.yaml, three icons, LICENSE, PRIVACY.md and two Localization XAML files; reject PDB, absolute paths and unexpected entries.
- [ ] Deploy the second-pass nine files to `staging/architecture-stage-e/deployed` and the installed plugin directory; require all hashes equal.
- [ ] Recompute user-data count/fingerprint and require no change.
- [ ] Mark Stage E complete in all status documents, recording test count, timing, hashes, package contents, zero-deletion audit and remaining client verification.
- [ ] Commit with `docs(architecture): complete stage E verification` and push `refactor/architecture-preparation`.

### Task 5: Client handoff

**Files:** None.

- [ ] Ask the user to check Dashboard/session navigation, explicit refresh, filter persistence, chart and heatmap drilldown, nested scrolling, Advanced Options, editor/import dialog keyboard behavior and session CRUD/import/export.
- [ ] Keep the branch intact until the user chooses merge or PR integration.
