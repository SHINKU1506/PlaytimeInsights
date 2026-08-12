# Playtime Insights Architecture

状态：阶段 E 架构基线

更新日期：2026-08-13

## Runtime composition

`PlaytimeInsights` is the composition root. Its constructor creates the shared settings, session repository, analytics,
query, import/export and diagnostics services. Sidebar activation then constructs presentation objects without moving
window or control types into ViewModels.

### Dashboard

The plugin keeps one Dashboard ViewModel for the lifetime of the Playnite process so date range, aggregation, ranking,
metadata filters and custom dates survive sidebar navigation. `activeDashboard` only identifies whether the page is
currently open. A View `Loaded` event is the sole automatic refresh entry; `SidebarItem.Opened` does not run a second
refresh.

`DashboardViewModel` is the coordinator and public compatibility surface for existing XAML bindings. A typed refresh
plan chooses the smallest valid path:

1. `DataReload` reads library names, the Playnite game list and session repository once, builds the cover index and
   refreshes metadata options;
2. range changes reuse the current filtered game/session input; metadata changes only refresh options or rebuild the
   filter as required;
3. full analysis creates one DashboardSnapshot plus one reusable `DashboardAnalysisContext`;
4. aggregation creates only a trend projection; ranking creates only a range-ranking projection;
5. complete major lists are published as one new `IReadOnlyList` reference, while session-detail paging remains
   incremental.

The root therefore creates one DashboardSnapshot for every required full analysis, and the child ViewModels never
rescan the repository:

- `DashboardFilterViewModel`: date range, aggregation, ranking and metadata filter state;
- `DashboardMetricsViewModel`: metric cards, comparisons and game rankings;
- `DashboardDistributionViewModel`: trend, weekday/hour distributions, heatmaps and anomaly presentation;
- `DashboardDrilldownViewModel`: selected period/date session details, cover paths and pagination.

### Session management

Each session-page activation creates a fresh `SessionManagementViewModel`, `WpfSessionManagementInteraction`,
`SessionManagementCoordinator` and View. The ViewModel implements `ISessionManagementOperations`; the Coordinator
orchestrates import/export, backup/restore, manual editing, soft deletion, reindex and diagnostics; the
`ISessionManagementInteraction` contract describes user intent without exposing WPF types. The concrete
`WpfSessionManagementInteraction` owns file dialogs, Window Owner lookup, confirmations, editor/import windows and
localized error presentation.

`SessionManagementView` retains only page lifecycle, Advanced Options ContextMenu placement and single-line
Coordinator forwarding. A View `Loaded` event is its sole automatic refresh entry. `CountText` projects the activity
count calculated from the current `GetAllIncludingDeleted()` refresh input and does not read the repository from a WPF
property getter.

## View boundary

Code-behind is not required to be empty. The following behavior remains in View code because it depends on WPF control
or window lifecycle:

- View `Loaded` refresh and work-area sizing;
- `AdaptiveTrendChart.PeriodSelected` and heatmap mouse-event parameter adaptation;
- nested `ScrollViewer` boundary detection, VisualTree lookup and mouse-wheel event forwarding;
- ContextMenu `PlacementTarget` and opening;
- thin Coordinator forwarding from session-management buttons;
- editor/import window validation, `DialogResult`, local save dialog and error-report presentation.

The Stage E audit found no orphan handlers: every named XAML event and named `Loaded +=` subscription has exactly one
matching private handler, and every handler in those categories has a source. The dynamic Stage E guard checks this
symmetry; it deliberately does not assert a Code-behind line count.

## Refresh and data flow

```text
Playnite game start/stop
  -> SessionRepository active checkpoint / completed GameSession
  -> refresh currently open analytics pages

Dashboard Loaded or explicit refresh
  -> DashboardViewModel
  -> DataReload: one game/session input snapshot + cover index
  -> one AnalyticsService DashboardSnapshot + DashboardAnalysisContext
  -> Filter / Metrics / Distribution / Drilldown state

Dashboard aggregation/ranking filter
  -> reuse DashboardAnalysisContext
  -> trend projection or range-ranking projection only

Dashboard range/metadata filter
  -> reuse cached game/session input
  -> full DashboardSnapshot + replacement DashboardAnalysisContext

Session page Loaded, explicit/filter refresh or CRUD
  -> SessionManagementViewModel
  -> one GetAllIncludingDeleted input
  -> filter + item/cover projection + pager + active count

Session workflow button
  -> SessionManagementView
  -> SessionManagementCoordinator
  -> ISessionManagementInteraction + ISessionManagementOperations
  -> local confirmation/file/window interaction and repository operation
```

All session data stays local. The storage schema and plugin ID are outside the presentation refactor boundary.

## Commands and interaction semantics

Standard refresh, pagination, restore, weekday selection and Dashboard drilldown actions use the plugin's
`RelayCommand` / `RelayCommand<T>`. Commands explicitly raise `CanExecuteChanged` when selection, paging or refresh
state changes. Multi-step session workflows remain Coordinator calls because they cross file dialogs, confirmation and
business operations. Custom control events remain thin View adapters instead of adding a Behavior dependency.

Localized content and automation names remain in XAML. `SessionEditorWindow` and `SessionImportPreviewWindow` retain
cyclic Tab navigation, default/cancel buttons and their current focus behavior. ContextMenu commands continue to resolve
the ViewModel from `PlacementTarget.DataContext`.

## Test evidence

| Boundary | Regression evidence |
|---|---|
| Statistics, range clipping and aggregation | analytics allocation, aggregation, ranking, heatmap, trend and drilldown tests |
| 100k-session budget and schema load | analytics and schema-load performance tests |
| Repository safety and schema 1-4 | recovery, deduplication, corruption fallback and schema migration tests |
| Import/export/backup/restore | round-trip, validation, rollback, restore and reindex tests |
| Localization and accessibility | resource parity/source coverage, native-view accessibility and responsive layout tests |
| Command state | RelayCommand, Stage B bindings, pagination and refresh-reentrancy tests |
| Session workflow boundary | Coordinator cancellation, success and exception-path tests |
| Dashboard composition | Stage D one-snapshot and runtime filter-lifetime tests |
| Navigation performance | single automatic refresh and refresh-snapshot count tests |
| View event ownership | Stage E dynamic handler/source symmetry test |

Release completion additionally requires two clean deterministic builds, two Toolbox PEXT packages with identical
hashes and nine expected entries, no PDB or sensitive local paths, matching deployed-file hashes and an unchanged user
data fingerprint.
