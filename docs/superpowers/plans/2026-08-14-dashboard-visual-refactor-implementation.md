# Dashboard Visual Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改变既有统计口径、选择性刷新边界和性能预算的前提下，将 Playtime Insights 主仪表盘实现为主题兼容、宽屏双栏、九项指标完整、可键盘操作且可实机验收的 WPF 数据界面。

**Architecture:** 保持现有 `DashboardViewModel` 及 `Filter`、`Metrics`、`Distribution`、`Drilldown` 子模块边界，新增的日期语义在分析服务层完成，展示投影在 ViewModel 层完成，布局状态留在 View/Panel 层。主区域使用专用 `AdaptiveDashboardPanel` 将 Primary 和 Secondary 区域分别纵向堆叠，避免普通共享 Grid 行高造成的空白；视觉资源通过两个 View 显式合并的资源字典加载，不依赖插件 `App.xaml` 的宿主级传播。

**Tech Stack:** C# 7.3、.NET Framework 4.6.2、WPF、Playnite SDK、现有自定义回归测试程序 `Tests/Program.cs`、MVVM、`DrawingContext` 自绘图表。

## Global Constraints

- 实施依据为 `docs/DASHBOARD_VISUAL_REFACTOR_ROADMAP.md` v1.1；本文对实现顺序、接口和文件边界的细化具有补充约束力。
- Release 构建必须保持 0 warning、0 error。
- 所有既有 108 项回归以及本计划新增回归必须通过。
- 10 万会话分析耗时必须不高于 750 ms；schema 4 加载耗时必须不高于 1400 ms。
- Dashboard 内容区进入双栏的阈值固定为 1200 DIP，退出双栏的阈值固定为 1160 DIP。
- 双栏间距固定为 18 DIP，Secondary 区域宽度比例固定为 0.38。
- 九项指标全部保留：两张 Hero 卡加七张 Tier 2 卡，不得删除、合并数据口径或改用 Tooltip 隐藏。
- `ResponsiveUniformPanel` 保持现有实现，仅承载七张 Tier 2 卡；不得把两张 Hero 卡加入该 Panel。
- `AllSessions` 的起点是当前元数据筛选后的有效会话中最早的本地日期；有效会话定义为 `!IsDeleted && ElapsedSeconds > 0`。
- `AllSessions` 不展示上一等长区间和去年同期比较，比较对象为 `null`，比较区域 `Visibility.Collapsed`。
- 排行榜 Tab、筛选折叠和宽窄布局切换均为纯 View 状态，不触发数据读取或 Dashboard 分析刷新。
- 下钻列表继续使用 `ListView`、`VirtualizingStackPanel`、`CanContentScroll="True"` 和 `VirtualizationMode="Recycling"`。
- 第一轮不实现跨侧边栏导航，不修改 `PlaytimeInsights.cs`，不访问 Playnite 私有 UI 或使用 VisualTree 反射模拟导航。
- Playnite 基础界面色必须来自 `ControlBackgroundBrush`、`TextBrush`、`PanelSeparatorBrush`、`PopupBackgroundBrush` 和 `GlyphBrush`；插件固定色仅用于图表系列、趋势、来源、奖牌和异常等语义。
- 所有新增界面文本同时加入 `Localization/en_US.xaml` 与 `Localization/zh_CN.xaml`，键集合和格式参数必须一致。
- 不引入第三方 UI 库，不更换图表实现，不改变会话存储 schema。

---

## Feasibility Decision

**结论：有条件达到可实施标准。**

路线图 v1.1 已补齐九项指标去向、日期预设、排行榜口径、虚拟化要求、主题边界、导航延期、工期和视觉矩阵，已不存在路线图级阻塞。以下实现决策必须按本文执行，否则会重新引入已识别风险：

1. `AllSessions` 起点不能从 `DashboardAnalysisContext` 读取。Context 在范围解析和完整分析之后才创建，必须在 `AnalyticsService.CreateSnapshotWithContext` 将输入 materialize 为 `sessionList` 后先计算最早有效本地日期，再调用新的三参数 `ResolveDateRange`。
2. `AllSessions` 的全历史区间没有稳定的上一等长区间或去年同期语义。该预设必须隐藏比较，不得生成跨度可能溢出的历史范围。
3. 共享样式不得只放入插件 `App.xaml`。Playnite 托管插件 View 时没有证据保证该 Application 资源是全局入口，必须由 Dashboard 和 Session Management 两个 View 显式合并。
4. 筛选折叠状态是 View 状态。现有 Dashboard View 被缓存，`Expander.IsExpanded` 会随视觉树自然保留；ViewModel 只提供激活筛选数量和摘要文本。
5. 普通双栏 Grid 会以共享行高对齐左右模块，高排行榜旁可能出现大块空白。主内容必须由 `AdaptiveDashboardPanel` 分栏后独立累加各栏高度。
6. 路线图中的 `IsWideLayout + Grid Trigger` 方案由 `AdaptiveDashboardPanel.IsWideLayout` 取代；布局状态仍属于 View 层，且增加 1200/1160 DIP 滞回，避免临界宽度反复抖动。

## File Map

| File | Action | Responsibility |
| --- | --- | --- |
| `docs/CLIENT_ACCEPTANCE_1.0.0.md` | Modify | 固化视觉验收矩阵、阈值、截图证据和通过记录 |
| `Services/AnalyticsService.cs` | Modify | 日期预设、最早有效会话日期、范围解析、结构化时长、来源投影 |
| `Services/AdvancedAnalyticsService.cs` | Modify | `AllSessions` 比较禁用和比较可见性 |
| `ViewModels/Dashboard/DashboardSnapshot.cs` | Modify | 结构化时长和 `ComparisonVisibility` 快照契约 |
| `ViewModels/Dashboard/DurationDisplayViewModel.cs` | Create | 数值、单位和无障碍完整文本的不可变展示模型 |
| `ViewModels/Dashboard/DashboardMetricsViewModel.cs` | Modify | 接收并公开结构化指标和比较可见性 |
| `ViewModels/Dashboard/DashboardFilterViewModel.cs` | Modify | 新预设选项、单次选择 API、激活筛选摘要 |
| `ViewModels/Dashboard/SessionDetailViewModel.cs` | Modify | 透传真实 `SessionSource` |
| `ViewModels/DashboardViewModel.cs` | Modify | 快捷范围命令、清除下钻命令及属性代理 |
| `Controls/AdaptiveDashboardPanel.cs` | Create | 双栏独立纵向堆叠、单栏源顺序排列和断点滞回 |
| `Controls/AdaptiveTrendChart.cs` | Modify | Tooltip、十字线和节点外圈主题化 |
| `Resources/PlaytimeInsightsVisualResources.xaml` | Create | Dashboard 与 Session Management 共享的语义画刷和样式 |
| `Views/PlaytimeInsightsDashboardView.xaml` | Modify | 工具栏、Hero/Tier 2、Tab 排行榜、自适应主区域、下钻卡片 |
| `Views/PlaytimeInsightsDashboardView.xaml.cs` | Modify | 维护 640 DIP Hero 单/双行 View 状态，不参与业务刷新 |
| `Views/SessionManagementView.xaml` | Modify | 显式合并共享资源并复用来源 Tag 样式 |
| `Localization/en_US.xaml` | Modify | 新增英文日期、Tab、筛选和操作文本 |
| `Localization/zh_CN.xaml` | Modify | 新增中文日期、Tab、筛选和操作文本 |
| `Tests/Program.cs` | Modify | 日期、比较、展示模型、布局、XAML 护栏和刷新纯度回归 |

`App.xaml`、`PlaytimeInsights.cs`、`Controls/ResponsiveUniformPanel.cs`、`Services/DashboardAnalysisContext.cs` 和 `Services/SessionQueryService.cs` 不在预期修改范围内。若实现中必须改动其中任一文件，应先暂停对应任务并证明现有接口无法满足本文契约。

---

### Task 0: Freeze the Implementation Contract and Baseline

**Files:**
- Modify: `docs/CLIENT_ACCEPTANCE_1.0.0.md`
- Verify: `docs/DASHBOARD_VISUAL_REFACTOR_ROADMAP.md`
- Verify: `docs/DASHBOARD_VISUAL_REFACTOR_ROADMAP_REVIEW.md`

**Interfaces:**
- Consumes: 本文 `Global Constraints` 和 `Feasibility Decision`
- Produces: 后续任务共同使用的验收记录章节 `Dashboard Visual Refactor`

- [ ] **Step 1: Add the frozen acceptance section**

在 `docs/CLIENT_ACCEPTANCE_1.0.0.md` 增加以下结构，并保留未执行状态：

```markdown
## Dashboard Visual Refactor

### Frozen Layout Contract
- Enter wide layout: 1200 DIP
- Exit wide layout: 1160 DIP
- Column spacing: 18 DIP
- Secondary column ratio: 0.38
- KPI inventory: 2 Hero + 7 Tier 2 = 9
- AllSessions comparison: hidden

### Automated Gate
- [ ] Release plugin build: 0 warning / 0 error
- [ ] Release test build: 0 warning / 0 error
- [ ] Full regression suite passes
- [ ] 100k-session analytics <= 750 ms
- [ ] schema 4 load <= 1400 ms

### Visual Evidence Matrix
- [ ] Languages: zh_CN, en_US
- [ ] Themes: Default Dark, Default Light, Seaside Dark, third-party high contrast, Windows High Contrast
- [ ] DPI: 100%, 125%, 150%, 175%, 200%
- [ ] Widths: 400, 640, 900, 1159, 1160, 1199, 1200, 1600, 2400 DIP
- [ ] Data: empty, normal, long English names, large duration, comparison states, anomaly states, 100+ drilldown rows, ranking counts below 3 and above 10
```

- [ ] **Step 2: Run the unchanged Release baseline**

Run:

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: 两个构建均为 0 warning、0 error；测试程序输出 `All Playtime Insights tests passed.`。若基线失败，在进入 Task 1 前记录失败测试和工作区已有改动。

- [ ] **Step 3: Commit the frozen contract**

```powershell
git add docs/CLIENT_ACCEPTANCE_1.0.0.md
git commit -m "docs: freeze dashboard visual refactor contract"
```

---

### Task 1: Add Relative and All-Sessions Date Presets

**Files:**
- Modify: `Services/AnalyticsService.cs` at `DateRangePreset`, `ResolveDateRange`, and `ResolveAggregationPeriod`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Consumes: existing `AnalyticsQuery`, `AnalyticsDateRange`
- Produces: `DateRangePreset.Last7Days`, `Last30Days`, `AllSessions`
- Produces: `AnalyticsService.ResolveDateRange(AnalyticsQuery query, DateTime today, DateTime? allSessionsStartDate = null)`

- [ ] **Step 1: Register failing range tests**

在 `Main()` 中注册：

```csharp
Run("Relative dashboard ranges use inclusive local dates", TestRelativeDashboardRanges);
Run("All-sessions range uses a supplied earliest local date", TestAllSessionsDateRange);
Run("All-sessions automatic aggregation follows actual span", TestAllSessionsAggregation);
```

新增测试：

```csharp
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
    Equal(AggregationPeriod.Day,
        ResolveAggregation(DateRangePreset.AllSessions, 62));
    Equal(AggregationPeriod.Week,
        ResolveAggregation(DateRangePreset.AllSessions, 63));
    Equal(AggregationPeriod.Month,
        ResolveAggregation(DateRangePreset.AllSessions, 731));
    Equal(AggregationPeriod.Year,
        ResolveAggregation(DateRangePreset.AllSessions, 3651));
}
```

- [ ] **Step 2: Run tests and confirm the contract fails**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: compile failure because the three enum members and three-argument range behavior do not exist.

- [ ] **Step 3: Implement enum and range resolution**

将枚举顺序固定为：

```csharp
public enum DateRangePreset
{
    Today,
    Last7Days,
    Last30Days,
    ThisWeek,
    ThisMonth,
    ThisYear,
    AllSessions,
    Custom
}
```

将范围方法签名改为：

```csharp
public static AnalyticsDateRange ResolveDateRange(
    AnalyticsQuery query,
    DateTime today,
    DateTime? allSessionsStartDate = null)
```

新增分支的精确日期规则：

```csharp
case DateRangePreset.Last7Days:
    start = today.AddDays(-6);
    end = today;
    label = LocalizationService.Format(
        "LOCPlaytimeInsightsLast7DaysRangeFormat",
        "近 7 天 · {0:M/d}–{1:M/d}",
        start,
        end);
    break;
case DateRangePreset.Last30Days:
    start = today.AddDays(-29);
    end = today;
    label = LocalizationService.Format(
        "LOCPlaytimeInsightsLast30DaysRangeFormat",
        "近 30 天 · {0:M/d}–{1:M/d}",
        start,
        end);
    break;
case DateRangePreset.AllSessions:
    start = allSessionsStartDate.HasValue
        ? allSessionsStartDate.Value.Date
        : today;
    if (start > today)
    {
        start = today;
    }
    end = today;
    label = LocalizationService.Format(
        "LOCPlaytimeInsightsAllSessionsRangeFormat",
        "全部记录 · {0:yyyy/M/d}–{1:yyyy/M/d}",
        start,
        end);
    break;
```

自动聚合规则中 `Last7Days`、`Last30Days` 返回 `Day`；`AllSessions` 与 `Custom` 共用现有 62、730、3650 天阈值。

- [ ] **Step 4: Add localization keys**

两个语言资源必须同时新增：

```xml
<sys:String x:Key="LOCPlaytimeInsightsLast7Days">Last 7 days</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast30Days">Last 30 days</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAllSessions">All sessions</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast7DaysRangeFormat">Last 7 days · {0:M/d}–{1:M/d}</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast30DaysRangeFormat">Last 30 days · {0:M/d}–{1:M/d}</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAllSessionsRangeFormat">All sessions · {0:yyyy/M/d}–{1:yyyy/M/d}</sys:String>
```

```xml
<sys:String x:Key="LOCPlaytimeInsightsLast7Days">近 7 天</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast30Days">近 30 天</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAllSessions">全部记录</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast7DaysRangeFormat">近 7 天 · {0:M/d}–{1:M/d}</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLast30DaysRangeFormat">近 30 天 · {0:M/d}–{1:M/d}</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAllSessionsRangeFormat">全部记录 · {0:yyyy/M/d}–{1:yyyy/M/d}</sys:String>
```

- [ ] **Step 5: Run the full regression program**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass, including localization parity.

- [ ] **Step 6: Commit**

```powershell
git add Services/AnalyticsService.cs Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: add dashboard relative date presets"
```

---

### Task 2: Resolve All-Sessions from Filtered Valid Sessions

**Files:**
- Modify: `Services/AnalyticsService.cs` at `CreateSnapshotWithContext`
- Test: `Tests/Program.cs`

**Interfaces:**
- Consumes: `GameSession.GetStartedLocalDate()`
- Consumes: Task 1 `AnalyticsService.ResolveDateRange(AnalyticsQuery query, DateTime today, DateTime? allSessionsStartDate = null)`
- Produces: range calculated before `DashboardAnalysisContext` construction

- [ ] **Step 1: Register the failing snapshot test**

```csharp
Run("All-sessions snapshot starts at earliest valid filtered local date", TestAllSessionsSnapshotStart);
```

测试必须覆盖删除会话、零时长会话、时区偏移和未来无效起点：

```csharp
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

    Equal(new DateTime(2020, 1, 2), result.Context.Range.StartDate);
    Equal(DateTime.Today, result.Context.Range.EndDate);
}
```

- [ ] **Step 2: Run and verify the test fails**

Run:

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: the new assertion fails because `CreateSnapshotWithContext` does not supply an earliest date.

- [ ] **Step 3: Compute the earliest valid local date before range resolution**

在 `sessionList` 创建后、`ResolveDateRange` 调用前加入：

```csharp
DateTime? allSessionsStartDate = null;
if (query.RangePreset == DateRangePreset.AllSessions)
{
    allSessionsStartDate = sessionList
        .Where(session =>
            session != null &&
            !session.IsDeleted &&
            session.ElapsedSeconds > 0)
        .Select(session => (DateTime?)session.GetStartedLocalDate())
        .OrderBy(date => date)
        .FirstOrDefault();
}

var range = ResolveDateRange(
    query,
    DateTime.Today,
    allSessionsStartDate);
```

不要给 `DashboardAnalysisContext` 新增最早日期字段。解析后的 `Range` 已经是 Context 和后续投影所需的唯一事实源。

- [ ] **Step 4: Run all tests**

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add Services/AnalyticsService.cs Tests/Program.cs
git commit -m "fix: derive all-sessions range from valid sessions"
```

---

### Task 3: Hide Comparisons for All-Sessions

**Files:**
- Modify: `Services/AdvancedAnalyticsService.cs`
- Modify: `Services/AnalyticsService.cs`
- Modify: `ViewModels/Dashboard/DashboardSnapshot.cs`
- Modify: `ViewModels/Dashboard/DashboardMetricsViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `AdvancedAnalyticsSnapshot.ComparisonVisibility : Visibility`
- Produces: `DashboardMetricsViewModel.ComparisonVisibility : Visibility`
- Produces: `DashboardViewModel.ComparisonVisibility : Visibility`
- Changes: `AdvancedAnalyticsService.CreateSnapshot(IEnumerable<Game> games, IEnumerable<GameSession> sessions, AnalyticsDateRange range, DayOfWeek firstDayOfWeek, IDictionary<DateTime, ulong> rangeDailySeconds, DateRangePreset rangePreset)`

- [ ] **Step 1: Register comparison visibility tests**

```csharp
Run("All-sessions snapshot suppresses unstable comparisons", TestAllSessionsComparisonVisibility);
Run("Finite ranges keep period comparisons visible", TestFiniteRangeComparisonVisibility);
```

```csharp
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
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: compile failure because `ComparisonVisibility` is absent.

- [ ] **Step 3: Extend the snapshot and service contract**

在 `AdvancedAnalyticsSnapshot` 增加：

```csharp
public Visibility ComparisonVisibility { get; set; }
```

在 `AdvancedAnalyticsService.CreateSnapshot` 最后增加参数：

```csharp
DateRangePreset rangePreset
```

在计算 previous/year ranges 之前确定：

```csharp
var comparisonsEnabled = rangePreset != DateRangePreset.AllSessions;
AnalyticsDateRange previousRange = null;
AnalyticsDateRange yearRange = null;
ulong previousSeconds = 0;
ulong yearSeconds = 0;

if (comparisonsEnabled)
{
    previousRange = CreatePreviousPeriodRange(range);
    previousSeconds = CalculateRangeSeconds(
        sessionList,
        previousRange);
    yearRange = CreateYearOverYearRange(range);
    yearSeconds = CalculateRangeSeconds(
        sessionList,
        yearRange);
}
```

对象初始化规则固定为：

```csharp
ComparisonVisibility = comparisonsEnabled
    ? Visibility.Visible
    : Visibility.Collapsed,
PreviousPeriodComparison = comparisonsEnabled
    ? CreateComparison(
        LocalizationService.Get(
            "LOCPlaytimeInsightsPreviousPeriodComparison",
            "环比 · 上一等长区间"),
        LocalizationService.Get(
            "LOCPlaytimeInsightsPreviousPeriodShort",
            "环比"),
        currentSeconds,
        previousSeconds,
        previousRange)
    : null,
YearOverYearComparison = comparisonsEnabled
    ? CreateComparison(
        LocalizationService.Get(
            "LOCPlaytimeInsightsYearOverYearComparison",
            "同比 · 去年同期"),
        LocalizationService.Get(
            "LOCPlaytimeInsightsYearOverYearShort",
            "同比"),
        currentSeconds,
        yearSeconds,
        yearRange)
    : null,
```

删除原有无条件创建 `previousRange`/`yearRange` 并扫描会话的四行代码，替换为上面的条件初始化。仅在 `comparisonsEnabled` 为 true 时创建和扫描 previous/year ranges；不能先扫描再丢弃结果。`AnalyticsDateRange` 是引用类型，因此禁用比较时用 `null` 初始化可直接编译。

- [ ] **Step 4: Forward visibility to the View**

`DashboardMetricsViewModel.Apply` 保存 `snapshot.Advanced.ComparisonVisibility`，根 ViewModel 公开同名只读代理。Dashboard XAML 中包住两张比较卡的父容器绑定：

```xml
Visibility="{Binding ComparisonVisibility}"
```

子卡仍绑定原比较对象，不增加空对象占位。

- [ ] **Step 5: Run the suite**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass，且原 `TestAdvancedComparisons` 保持通过。

- [ ] **Step 6: Commit**

```powershell
git add Services/AdvancedAnalyticsService.cs Services/AnalyticsService.cs ViewModels/Dashboard/DashboardSnapshot.cs ViewModels/Dashboard/DashboardMetricsViewModel.cs ViewModels/DashboardViewModel.cs Views/PlaytimeInsightsDashboardView.xaml Tests/Program.cs
git commit -m "fix: suppress comparisons for all-session ranges"
```

---

### Task 4: Add Structured Duration Display Models

**Files:**
- Create: `ViewModels/Dashboard/DurationDisplayViewModel.cs`
- Modify: `Services/AnalyticsService.cs`
- Modify: `ViewModels/Dashboard/DashboardSnapshot.cs`
- Modify: `ViewModels/Dashboard/DashboardMetricsViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: immutable `DurationDisplayViewModel`
- Produces: `AnalyticsService.CreateDurationDisplay(ulong seconds)`
- Produces snapshot properties: `RangeDurationDisplay`, `LifetimeDurationDisplay`, `AverageSessionDisplay`, `LongestSessionDisplay`

- [ ] **Step 1: Register model projection tests**

```csharp
Run("Duration display separates values units and automation text", TestDurationDisplayProjection);
```

```csharp
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
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: compile failure because the model and factory do not exist.

- [ ] **Step 3: Create the immutable model**

```csharp
namespace PlaytimeInsights.ViewModels
{
    public sealed class DurationDisplayViewModel
    {
        public DurationDisplayViewModel(
            string majorValue,
            string majorUnit,
            string minorValue,
            string minorUnit,
            string automationText)
        {
            MajorValue = majorValue ?? string.Empty;
            MajorUnit = majorUnit ?? string.Empty;
            MinorValue = minorValue ?? string.Empty;
            MinorUnit = minorUnit ?? string.Empty;
            AutomationText = automationText ?? string.Empty;
        }

        public string MajorValue { get; }
        public string MajorUnit { get; }
        public string MinorValue { get; }
        public string MinorUnit { get; }
        public string AutomationText { get; }
    }
}
```

- [ ] **Step 4: Implement the projection factory**

`CreateDurationDisplay` 使用与 `FormatDurationPrecise` 一致的秒数语义。小于一小时显示分钟和可选秒；一小时及以上先按现有 `FormatDuration` 的规则四舍五入到分钟，再拆成小时和可选分钟。

新增单位键：

```xml
<sys:String x:Key="LOCPlaytimeInsightsHourUnitShort">h</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsMinuteUnitShort">m</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsSecondUnitShort">s</sys:String>
```

```xml
<sys:String x:Key="LOCPlaytimeInsightsHourUnitShort">小时</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsMinuteUnitShort">分</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsSecondUnitShort">秒</sys:String>
```

工厂返回值必须满足：

```csharp
public static DurationDisplayViewModel CreateDurationDisplay(ulong seconds)
{
    if (seconds < 3600)
    {
        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;
        return new DurationDisplayViewModel(
            minutes.ToString("N0"),
            LocalizationService.Get(
                "LOCPlaytimeInsightsMinuteUnitShort",
                "分"),
            remainingSeconds == 0
                ? string.Empty
                : remainingSeconds.ToString("N0"),
            remainingSeconds == 0
                ? string.Empty
                : LocalizationService.Get(
                    "LOCPlaytimeInsightsSecondUnitShort",
                    "秒"),
            FormatDurationPrecise(seconds));
    }

    var totalMinutes = Math.Max(
        1UL,
        (ulong)Math.Round(
            seconds / 60d,
            MidpointRounding.AwayFromZero));
    var hours = totalMinutes / 60;
    var minutesPart = totalMinutes % 60;
    return new DurationDisplayViewModel(
        hours.ToString("N0"),
        LocalizationService.Get(
            "LOCPlaytimeInsightsHourUnitShort",
            "小时"),
        minutesPart == 0 ? string.Empty : minutesPart.ToString("N0"),
        minutesPart == 0
            ? string.Empty
            : LocalizationService.Get(
                "LOCPlaytimeInsightsMinuteUnitShort",
                "分"),
        FormatDurationPrecise(seconds));
}
```

- [ ] **Step 5: Populate and forward the four duration projections**

`DashboardSnapshot` 增加四个属性，`CreateSnapshotWithContext` 与现有文本属性同时赋值。`DashboardMetricsViewModel.Apply` 保存它们，根 ViewModel 提供只读代理。现有 `*Text` 属性在本轮保留，供状态文本、Tooltip 和兼容测试继续使用。

- [ ] **Step 6: Run all tests**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add ViewModels/Dashboard/DurationDisplayViewModel.cs Services/AnalyticsService.cs ViewModels/Dashboard/DashboardSnapshot.cs ViewModels/Dashboard/DashboardMetricsViewModel.cs ViewModels/DashboardViewModel.cs Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: add structured dashboard duration displays"
```

---

### Task 5: Introduce an Explicit Shared Visual Resource Dictionary

**Files:**
- Create: `Resources/PlaytimeInsightsVisualResources.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Views/SessionManagementView.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces shared keys: ranking brushes, metric icon brushes, source brushes, `SessionSourceTagStyle`
- Consumes Playnite theme resources only through `DynamicResource`

- [ ] **Step 1: Register resource-loading and merge guards**

```csharp
Run("Plugin visual resources load through explicit view merges", TestExplicitVisualResourceMerges);
```

测试读取两个 View 并断言都包含：

```csharp
@"<ResourceDictionary Source=""../Resources/PlaytimeInsightsVisualResources.xaml"" />"
```

同时在 STA 线程中创建两个 View，给根资源注入五个 Playnite 主题画刷，执行 `Measure` 和 `Arrange`，并断言：

```csharp
Equal(true, dashboard.TryFindResource("SessionSourceTagStyle") is Style);
Equal(true, management.TryFindResource("SessionSourceTagStyle") is Style);
Equal(true, dashboard.TryFindResource("RankingGoldBrush") is Brush);
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: the new merge assertions fail because the dictionary is absent.

- [ ] **Step 3: Create the resource dictionary**

根声明：

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="clr-namespace:PlaytimeInsights.Models">
```

至少集中以下命名资源：

```xml
<SolidColorBrush x:Key="RankingGoldBrush" Color="#FFD6B34B" />
<SolidColorBrush x:Key="RankingGoldFillBrush" Color="#33D6B34B" />
<SolidColorBrush x:Key="RankingSilverBrush" Color="#FFBFC7D5" />
<SolidColorBrush x:Key="RankingSilverFillBrush" Color="#33BFC7D5" />
<SolidColorBrush x:Key="RankingBronzeBrush" Color="#FFC9824A" />
<SolidColorBrush x:Key="RankingBronzeFillBrush" Color="#33C9824A" />
<SolidColorBrush x:Key="RankingEnergyBrush" Color="#FF4A90E2" />
<SolidColorBrush x:Key="MetricDurationForegroundBrush" Color="#FF60A5FA" />
<SolidColorBrush x:Key="MetricDurationBackgroundBrush" Color="#203B82F6" />
<SolidColorBrush x:Key="MetricSessionForegroundBrush" Color="#FFA78BFA" />
<SolidColorBrush x:Key="MetricSessionBackgroundBrush" Color="#208B5CF6" />
<SolidColorBrush x:Key="MetricActivityForegroundBrush" Color="#FFFBBF24" />
<SolidColorBrush x:Key="MetricActivityBackgroundBrush" Color="#20F59E0B" />
<SolidColorBrush x:Key="MetricAnomalyForegroundBrush" Color="#FFFB7185" />
<SolidColorBrush x:Key="MetricAnomalyBackgroundBrush" Color="#20F43F5E" />
<SolidColorBrush x:Key="SessionTrackedBrush" Color="#FF60A5FA" />
<SolidColorBrush x:Key="SessionTrackedFillBrush" Color="#2060A5FA" />
<SolidColorBrush x:Key="SessionImportedBrush" Color="#FFA78BFA" />
<SolidColorBrush x:Key="SessionImportedFillBrush" Color="#20A78BFA" />
<SolidColorBrush x:Key="SessionManualBrush" Color="#FFFBBF24" />
<SolidColorBrush x:Key="SessionManualFillBrush" Color="#20FBBF24" />
<SolidColorBrush x:Key="SessionRecoveredBrush" Color="#FFFB7185" />
<SolidColorBrush x:Key="SessionRecoveredFillBrush" Color="#20FB7185" />
```

`SessionSourceTagStyle` 的触发器绑定 `Source` 枚举，不绑定 `SourceText`。默认边框和背景使用 Playnite 主题资源；四个枚举值覆盖为上述语义色。

```xml
<Style x:Key="SessionSourceTagStyle" TargetType="{x:Type Border}">
    <Setter Property="Padding" Value="7,2" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="BorderBrush" Value="{DynamicResource PanelSeparatorBrush}" />
    <Setter Property="Background" Value="{DynamicResource PopupBackgroundBrush}" />
    <Style.Triggers>
        <DataTrigger Binding="{Binding Source}"
                     Value="{x:Static models:SessionSource.Tracked}">
            <Setter Property="BorderBrush" Value="{StaticResource SessionTrackedBrush}" />
            <Setter Property="Background" Value="{StaticResource SessionTrackedFillBrush}" />
        </DataTrigger>
        <DataTrigger Binding="{Binding Source}"
                     Value="{x:Static models:SessionSource.Imported}">
            <Setter Property="BorderBrush" Value="{StaticResource SessionImportedBrush}" />
            <Setter Property="Background" Value="{StaticResource SessionImportedFillBrush}" />
        </DataTrigger>
        <DataTrigger Binding="{Binding Source}"
                     Value="{x:Static models:SessionSource.Manual}">
            <Setter Property="BorderBrush" Value="{StaticResource SessionManualBrush}" />
            <Setter Property="Background" Value="{StaticResource SessionManualFillBrush}" />
        </DataTrigger>
        <DataTrigger Binding="{Binding Source}"
                     Value="{x:Static models:SessionSource.Recovered}">
            <Setter Property="BorderBrush" Value="{StaticResource SessionRecoveredBrush}" />
            <Setter Property="Background" Value="{StaticResource SessionRecoveredFillBrush}" />
        </DataTrigger>
    </Style.Triggers>
</Style>
```

- [ ] **Step 4: Merge explicitly in both views**

两个 View 的 `UserControl.Resources` 都使用：

```xml
<ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="../Resources/PlaytimeInsightsVisualResources.xaml" />
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

实际编辑时把每个 View 当前已有的 converter、template 和 layout style 放在 `MergedDictionaries` 后、`ResourceDictionary` 结束标签前。不要修改 `App.xaml`。Dashboard 中已有同名奖牌资源迁移到共享字典，避免重复键。

- [ ] **Step 5: Run tests and build**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: XAML 编译成功，所有测试通过。

- [ ] **Step 6: Commit**

```powershell
git add Resources/PlaytimeInsightsVisualResources.xaml Views/PlaytimeInsightsDashboardView.xaml Views/SessionManagementView.xaml Tests/Program.cs
git commit -m "refactor: share dashboard semantic visual resources"
```

---

### Task 6: Theme the Chart and Heatmap and Replace Ranking Energy Bars

**Files:**
- Modify: `Controls/AdaptiveTrendChart.cs` at `OnRender`, `DrawHover`, and `ResolveBrush`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Resources/PlaytimeInsightsVisualResources.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Consumes: Playnite `PopupBackgroundBrush`, `PanelSeparatorBrush`, `GlyphBrush`, `ControlBackgroundBrush`
- Produces: heatmap empty-cell layered theme treatment
- Produces: `RankingEnergyBrush` and `RankingEnergyBarStyle` with 4 DIP track

- [ ] **Step 1: Add source guards**

扩展 `TestThemeAndResponsiveLayout`，断言：

```csharp
Equal(false, dashboard.Contains("#FF2A2A2E"));
Equal(false, adaptiveTrendChart.Contains("Color.FromArgb(220, 35, 37, 44)"));
Equal(true, adaptiveTrendChart.Contains(
    "ResolveBrush(\"PopupBackgroundBrush\""));
Equal(true, adaptiveTrendChart.Contains(
    "ResolveBrush(\"PanelSeparatorBrush\""));
Equal(true, adaptiveTrendChart.Contains(
    "ResolveBrush(\"GlyphBrush\""));
Equal(true, adaptiveTrendChart.Contains(
    "ResolveBrush(\"ControlBackgroundBrush\""));
Equal(true, dashboard.Contains("Height=\"4\""));
Equal(true, dashboard.Contains(
    "Style=\"{StaticResource RankingEnergyBarStyle}\""));
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: source guard assertions fail.

- [ ] **Step 3: Theme `AdaptiveTrendChart`**

`DrawHover` 每次绘制时解析：

```csharp
var popupBackground = ResolveBrush(
    "PopupBackgroundBrush",
    Color.FromRgb(35, 37, 44));
var separator = ResolveBrush(
    "PanelSeparatorBrush",
    Color.FromArgb(150, 74, 144, 226));
var glyph = ResolveBrush(
    "GlyphBrush",
    Color.FromRgb(120, 177, 235));
var controlBackground = ResolveBrush(
    "ControlBackgroundBrush",
    Colors.Black);
```

使用 `glyph` 绘制十字线和节点填充，使用 `controlBackground` 绘制节点外圈，使用 `popupBackground` 和 `separator` 绘制 Tooltip。`ResolveBrush` 保留当前安全 fallback：

```csharp
private Brush ResolveBrush(string key, Color fallback)
{
    return TryFindResource(key) as Brush ??
        new SolidColorBrush(fallback);
}
```

- [ ] **Step 4: Replace heatmap empty-cell color**

删除 `HeatmapEmptyBrush`。两个热力格模板都改为双层 Border：

```xml
<Border BorderBrush="{DynamicResource PanelSeparatorBrush}"
        BorderThickness="1"
        Background="Transparent">
    <Grid>
        <Border Background="{DynamicResource TextBrush}"
                Opacity="0.06"
                IsHitTestVisible="False" />
        <Border Background="{StaticResource HeatmapActiveBrush}"
                Opacity="{Binding Intensity}"
                IsHitTestVisible="False" />
        <ContentPresenter />
    </Grid>
</Border>
```

实际模板保留原尺寸、命中事件、日期 Tag、Tooltip 和可见性绑定，只替换背景层。

- [ ] **Step 5: Implement the 4 DIP ranking energy bar**

Task 5 创建共享字典时已经固定新增：

```xml
<SolidColorBrush x:Key="RankingEnergyBrush" Color="#FF4A90E2" />
```

底轨使用 `TextBrush` 的低透明度层，进度前景使用该插件图表强调色。模板必须包含固定高度：

```xml
<Style x:Key="RankingEnergyBarStyle"
       TargetType="{x:Type ProgressBar}">
    <Setter Property="Height" Value="4" />
    <Setter Property="Foreground" Value="{StaticResource RankingEnergyBrush}" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="IsHitTestVisible" Value="False" />
    <Setter Property="Focusable" Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type ProgressBar}">
                <Grid Height="4" ClipToBounds="True">
                    <Border Background="{DynamicResource TextBrush}"
                            Opacity="0.10"
                            CornerRadius="2" />
                    <Border x:Name="PART_Track"
                            Background="Transparent"
                            CornerRadius="2"
                            ClipToBounds="True">
                        <Rectangle x:Name="PART_Indicator"
                                   HorizontalAlignment="Left"
                                   Fill="{TemplateBinding Foreground}" />
                    </Border>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

排行榜 ItemTemplate 删除全卡背景进度条，在名称/详情下方放置绑定 `ProgressPercent` 的 4 DIP ProgressBar。

- [ ] **Step 6: Run tests**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: build and all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add Controls/AdaptiveTrendChart.cs Views/PlaytimeInsightsDashboardView.xaml Resources/PlaytimeInsightsVisualResources.xaml Tests/Program.cs
git commit -m "style: theme dashboard charts and ranking bars"
```

---

### Task 7: Build Quick Ranges, Filter Summary, and Ranking Tabs

**Files:**
- Modify: `ViewModels/Dashboard/DashboardFilterViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `DashboardFilterViewModel.SelectRange(DateRangePreset preset)`
- Produces: `DashboardFilterViewModel.ActiveMetadataFilterCount : int`
- Produces: `DashboardFilterViewModel.ActiveMetadataFilterSummary : string`
- Produces: `DashboardFilterViewModel.ActiveMetadataFilterVisibility : Visibility`
- Produces: `DashboardViewModel.SelectRangeCommand : RelayCommand<DateRangePreset>`
- Keeps: ranking Tab and filter `Expander.IsExpanded` as View-only state

- [ ] **Step 1: Register refresh and filter summary tests**

```csharp
Run("Quick range selection emits at most one range refresh", TestQuickRangeRefreshPurity);
Run("Metadata filter summary counts active constraints", TestActiveMetadataFilterSummary);
Run("Dashboard ranking tabs are view-only and keep both snapshots", TestRankingTabsStayViewOnly);
```

核心断言：

```csharp
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
    Equal(DateRangePreset.Last7Days, viewModel.SelectedRangeOption.Value);
}
```

`TestActiveMetadataFilterSummary` 先断言 0 和空摘要，再设置一个有效维度及非空值，断言 count 为 1 且摘要包含 `1`，清空值后恢复 0。

`TestRankingTabsStayViewOnly` 读取 Dashboard XAML，断言存在一个 `TabControl`、两个 `TabItem`、`RangeGameRankings` 和 `LifetimeGameRankings`；读取 `DashboardViewModel.cs`，断言没有排名 Tab 选择属性和 Tab 切换刷新原因。

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: missing methods/properties and XAML structure assertions fail.

- [ ] **Step 3: Implement a single-source range selection API**

`RangeOptions` 顺序固定为 Today、Last7Days、Last30Days、ThisWeek、ThisMonth、ThisYear、AllSessions、Custom。

新增：

```csharp
public void SelectRange(DateRangePreset preset)
{
    var option = RangeOptions.FirstOrDefault(
        value => value.Value == preset);
    if (option != null &&
        !ReferenceEquals(option, SelectedRangeOption))
    {
        SelectedRangeOption = option;
    }
}
```

根 ViewModel 构造命令：

```csharp
SelectRangeCommand = new RelayCommand<DateRangePreset>(
    Filter.SelectRange,
    preset => !refreshGuard.IsActive &&
        Filter.RangeOptions.Any(option => option.Value == preset));
```

并在 `RaiseCommandStates` 调用 `SelectRangeCommand.RaiseCanExecuteChanged()`。

- [ ] **Step 4: Add active filter summary without storing expansion state**

```csharp
public int ActiveMetadataFilterCount =>
    SelectedMetadataDimensionOption?.Value.HasValue == true &&
    !string.IsNullOrWhiteSpace(SelectedMetadataValueOption?.Value)
        ? 1
        : 0;

public string ActiveMetadataFilterSummary =>
    ActiveMetadataFilterCount == 0
        ? string.Empty
        : LocalizationService.Format(
            "LOCPlaytimeInsightsActiveFilterCountFormat",
            "Active ({0})",
            ActiveMetadataFilterCount);

public Visibility ActiveMetadataFilterVisibility =>
    ActiveMetadataFilterCount == 0
        ? Visibility.Collapsed
        : Visibility.Visible;
```

在元数据维度和值变化时同时通知这三个属性。不得新增 `IsFilterExpanded` 或等价 ViewModel 属性。

- [ ] **Step 5: Build the toolbar and advanced filter Expander**

快捷按钮只包含 7D、30D、1Y、ALL，均绑定 `SelectRangeCommand` 和枚举 `CommandParameter`。其中 `1Y` 明确映射已有 `DateRangePreset.ThisYear`，表示本年，不新增 `LastYear` 或 `Last365Days`。完整 `ComboBox` 继续绑定同一 `RangeOptions`/`SelectedRangeOption`。

在根 `UserControl` 增加：

```xml
xmlns:services="clr-namespace:PlaytimeInsights.Services"
```

按钮组使用同一命令源，具体写法固定为：

```xml
<StackPanel Orientation="Horizontal">
    <Button Content="{DynamicResource LOCPlaytimeInsightsQuick7Days}"
            Margin="0,0,6,0"
            Command="{Binding SelectRangeCommand}"
            CommandParameter="{x:Static services:DateRangePreset.Last7Days}" />
    <Button Content="{DynamicResource LOCPlaytimeInsightsQuick30Days}"
            Margin="0,0,6,0"
            Command="{Binding SelectRangeCommand}"
            CommandParameter="{x:Static services:DateRangePreset.Last30Days}" />
    <Button Content="{DynamicResource LOCPlaytimeInsightsQuick1Year}"
            Margin="0,0,6,0"
            Command="{Binding SelectRangeCommand}"
            CommandParameter="{x:Static services:DateRangePreset.ThisYear}" />
    <Button Content="{DynamicResource LOCPlaytimeInsightsQuickAll}"
            Command="{Binding SelectRangeCommand}"
            CommandParameter="{x:Static services:DateRangePreset.AllSessions}" />
</StackPanel>
```

四个按钮可统一应用项目现有 Button 基样式或在本地增加视觉 Style，但不得改变上述命令和枚举参数。

高级筛选使用有名称的 `Expander`：

```xml
<Expander x:Name="AdvancedFilterExpander"
          IsExpanded="True">
    <Expander.Header>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{DynamicResource LOCPlaytimeInsightsAdvancedFilters}" />
            <Border Margin="8,0,0,0"
                    Visibility="{Binding Filter.ActiveMetadataFilterVisibility}">
                <TextBlock Text="{Binding Filter.ActiveMetadataFilterSummary}" />
            </Border>
        </StackPanel>
    </Expander.Header>
    <Grid Margin="0,10,0,0">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="12" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <StackPanel Grid.Column="0">
            <TextBlock Text="{DynamicResource LOCPlaytimeInsightsFilterDimension}"
                       Style="{StaticResource FieldLabelStyle}" />
            <ComboBox ItemsSource="{Binding MetadataDimensionOptions}"
                      SelectedItem="{Binding SelectedMetadataDimensionOption, Mode=TwoWay}"
                      DisplayMemberPath="Label" />
        </StackPanel>
        <StackPanel Grid.Column="2"
                    Visibility="{Binding MetadataValueVisibility}">
            <TextBlock Text="{DynamicResource LOCPlaytimeInsightsFilterValue}"
                       Style="{StaticResource FieldLabelStyle}" />
            <ComboBox ItemsSource="{Binding MetadataValueOptions}"
                      SelectedItem="{Binding SelectedMetadataValueOption, Mode=TwoWay}"
                      DisplayMemberPath="Label" />
        </StackPanel>
    </Grid>
</Expander>
```

Dashboard 根 `DataContext` 保持 `DashboardViewModel`，摘要绑定必须固定为 `Filter.ActiveMetadataFilterVisibility` 和 `Filter.ActiveMetadataFilterSummary`。不要在根 ViewModel 新增同名代理。其余元数据 ComboBox 继续使用根 ViewModel 已有代理属性。折叠状态只由控件保存。

- [ ] **Step 6: Merge the two rankings into one TabControl**

Tab 1 绑定 `RangeGameRankings`，Tab 2 绑定 `LifetimeGameRankings`。Tab 2 内固定显示本地化说明：

```text
Playnite library playtime; unaffected by the selected date range or ranking metric.
Playnite 游戏库累计时长；不受当前日期范围和排名依据影响。
```

不得为 Tab 增加 ViewModel selected-index 属性、命令或刷新原因。

- [ ] **Step 7: Add localization**

新增并保持中英文键对齐。快捷按钮文案在两种语言中都保持路线图规定的 7D/30D/1Y/ALL：

```xml
<sys:String x:Key="LOCPlaytimeInsightsQuick7Days">7D</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuick30Days">30D</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuick1Year">1Y</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuickAll">ALL</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAdvancedFilters">Advanced filters</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsActiveFilterCountFormat">Active ({0})</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsRangeRankingTab">Selected range</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLifetimeRankingTab">Playnite lifetime</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLifetimeRankingScope">Playnite library playtime; unaffected by the selected date range or ranking metric.</sys:String>
```

```xml
<sys:String x:Key="LOCPlaytimeInsightsQuick7Days">7D</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuick30Days">30D</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuick1Year">1Y</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsQuickAll">ALL</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsAdvancedFilters">高级筛选</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsActiveFilterCountFormat">已生效（{0}）</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsRangeRankingTab">本期排行</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLifetimeRankingTab">Playnite 累计时长</sys:String>
<sys:String x:Key="LOCPlaytimeInsightsLifetimeRankingScope">Playnite 游戏库累计时长；不受当前日期范围和排名依据影响。</sys:String>
```

- [ ] **Step 8: Run all tests**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass; one quick-range state change produces one `Range` reason.

- [ ] **Step 9: Commit**

```powershell
git add ViewModels/Dashboard/DashboardFilterViewModel.cs ViewModels/DashboardViewModel.cs Views/PlaytimeInsightsDashboardView.xaml Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: add dashboard quick filters and ranking tabs"
```

---

### Task 8: Implement `AdaptiveDashboardPanel`

**Files:**
- Create: `Controls/AdaptiveDashboardPanel.cs`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `DashboardLayoutZone { Primary, Secondary }`
- Produces attached property: `AdaptiveDashboardPanel.Zone`
- Produces DPs: `EnterWideWidth`, `ExitWideWidth`, `SecondaryColumnRatio`, `ColumnSpacing`, `VerticalSpacing`
- Produces read-only state: `IsWideLayout`

- [ ] **Step 1: Register panel behavior tests**

```csharp
Run("Adaptive dashboard panel uses source order in narrow mode", TestAdaptiveDashboardPanelNarrow);
Run("Adaptive dashboard panel stacks columns independently", TestAdaptiveDashboardPanelWide);
Run("Adaptive dashboard panel applies 1200 and 1160 DIP hysteresis", TestAdaptiveDashboardPanelHysteresis);
```

测试用固定期望尺寸的 Border 子项：

```csharp
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
```

窄屏以 900 DIP 排列四项，断言每项宽度为 900，Y 坐标按源顺序增加。宽屏以 1400 DIP 排列 Primary 100/120 和 Secondary 260/80，断言左右栏 X 不同、同栏 Y 独立累加、Panel 高度取两栏较大值而不是逐行配对总和。

滞回断言顺序固定：

```csharp
LayoutAdaptivePanel(panel, 1199);
Equal(false, panel.IsWideLayout);
LayoutAdaptivePanel(panel, 1200);
Equal(true, panel.IsWideLayout);
LayoutAdaptivePanel(panel, 1180);
Equal(true, panel.IsWideLayout);
LayoutAdaptivePanel(panel, 1159);
Equal(false, panel.IsWideLayout);
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: compile failure because the panel and enum do not exist.

- [ ] **Step 3: Define the public API**

```csharp
namespace PlaytimeInsights.Controls
{
    public enum DashboardLayoutZone
    {
        Primary,
        Secondary
    }

    public sealed class AdaptiveDashboardPanel : Panel
    {
        public static readonly DependencyProperty ZoneProperty =
            DependencyProperty.RegisterAttached(
                "Zone",
                typeof(DashboardLayoutZone),
                typeof(AdaptiveDashboardPanel),
                new FrameworkPropertyMetadata(
                    DashboardLayoutZone.Primary,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure |
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static void SetZone(
            DependencyObject element,
            DashboardLayoutZone value)
        {
            element.SetValue(ZoneProperty, value);
        }

        public static DashboardLayoutZone GetZone(
            DependencyObject element)
        {
            return (DashboardLayoutZone)element.GetValue(ZoneProperty);
        }

        public bool IsWideLayout { get; private set; }
    }
}
```

五个 DependencyProperty 默认值固定为 1200d、1160d、0.38d、18d、18d，全部带 `AffectsMeasure | AffectsArrange`。

- [ ] **Step 4: Implement measurement rules**

每次 `MeasureOverride` 先更新滞回状态：

```csharp
private void UpdateLayoutMode(double width)
{
    if (double.IsNaN(width) || double.IsInfinity(width))
    {
        IsWideLayout = false;
        return;
    }

    if (IsWideLayout)
    {
        IsWideLayout = width >= ExitWideWidth;
    }
    else
    {
        IsWideLayout = width >= EnterWideWidth;
    }
}
```

窄屏：

```text
childWidth = max(0, availableWidth)
measure visible children in source order with (childWidth, infinity)
desiredHeight = sum(child.DesiredSize.Height) + VerticalSpacing * (visibleCount - 1)
```

宽屏：

```text
usableWidth = max(0, availableWidth - ColumnSpacing)
secondaryWidth = usableWidth * SecondaryColumnRatio
primaryWidth = usableWidth - secondaryWidth
measure each child with its zone width and infinite height
primaryHeight = sum primary heights + spacing between primary children
secondaryHeight = sum secondary heights + spacing between secondary children
desiredHeight = max(primaryHeight, secondaryHeight)
```

将比例限定到 0.20–0.50，间距和阈值限定为非负值，`ExitWideWidth` 大于 `EnterWideWidth` 时按 `EnterWideWidth` 处理，保证状态可退出。

- [ ] **Step 5: Implement arrangement rules**

窄屏使用一个 `y` 游标按源顺序排列。宽屏使用 `primaryY` 和 `secondaryY` 两个独立游标；Primary X 为 0，Secondary X 为 `primaryWidth + ColumnSpacing`。Collapsed 子项安排到 `Rect(0,0,0,0)`。

Panel 返回传入的 `finalSize`，不得根据 hover、文本或子项状态改变列宽。

- [ ] **Step 6: Run panel and full tests**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: new panel tests and all existing responsive panel tests pass.

- [ ] **Step 7: Commit**

```powershell
git add Controls/AdaptiveDashboardPanel.cs Tests/Program.cs
git commit -m "feat: add adaptive dashboard column panel"
```

---

### Task 9: Recompose Hero, Tier 2, and Main Dashboard Regions

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Consumes: Task 4 structured duration projections
- Consumes: Task 8 `AdaptiveDashboardPanel` and `DashboardLayoutZone`
- Keeps: exactly one `ResponsiveUniformPanel` with exactly seven children

- [ ] **Step 1: Replace the old nine-card foundation test**

更新 `TestResponsiveMetricVisualFoundation`：

```csharp
var responsivePanels = FindVisualDescendants<ResponsiveUniformPanel>(view);
Equal(1, responsivePanels.Count);
Equal(7, responsivePanels[0].Children.Count);

var adaptivePanels = FindVisualDescendants<AdaptiveDashboardPanel>(view);
Equal(1, adaptivePanels.Count);
Equal(1200d, adaptivePanels[0].EnterWideWidth);
Equal(1160d, adaptivePanels[0].ExitWideWidth);
Equal(18d, adaptivePanels[0].ColumnSpacing);
Equal(0.38d, adaptivePanels[0].SecondaryColumnRatio);
```

新增 XAML 静态断言，九个指标标题资源键每个恰好出现一次，两张 Hero 卡包含 `AutomationProperties.Name` 或 `AutomationProperties.HelpText` 绑定到完整展示文本。

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: old card count and new adaptive panel assertions fail.

- [ ] **Step 3: Build the two-card Hero Grid**

先在 Dashboard View 本地资源中创建固定样式：

```xml
<Style x:Key="HeroMetricCardStyle"
       TargetType="Border"
       BasedOn="{StaticResource PanelStyle}">
    <Setter Property="MinHeight" Value="176" />
    <Setter Property="Padding" Value="20" />
</Style>
<Style x:Key="HeroMetricValueStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="34" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="VerticalAlignment" Value="Bottom" />
    <Setter Property="TextTrimming" Value="CharacterEllipsis" />
</Style>
<Style x:Key="HeroMetricMinorValueStyle"
       TargetType="TextBlock"
       BasedOn="{StaticResource HeroMetricValueStyle}">
    <Setter Property="FontSize" Value="20" />
    <Setter Property="Margin" Value="10,0,0,2" />
</Style>
<Style x:Key="HeroMetricUnitStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="13" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Margin" Value="4,0,0,5" />
    <Setter Property="VerticalAlignment" Value="Bottom" />
    <Setter Property="Opacity" Value="{StaticResource TextOpacitySecondary}" />
</Style>
```

Hero 使用独立 Grid，不嵌入卡片集合：

```xml
<Grid Margin="0,0,0,12">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="12" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="12" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <Border x:Name="RangeDurationHeroCard">
        <Border.Style>
            <Style TargetType="Border"
                   BasedOn="{StaticResource HeroMetricCardStyle}">
                <Setter Property="Grid.Row" Value="0" />
                <Setter Property="Grid.Column" Value="0" />
                <Setter Property="Grid.ColumnSpan" Value="1" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsCompactHeroLayout, RelativeSource={RelativeSource AncestorType={x:Type UserControl}}}"
                                 Value="True">
                        <Setter Property="Grid.Row" Value="0" />
                        <Setter Property="Grid.Column" Value="0" />
                        <Setter Property="Grid.ColumnSpan" Value="3" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>
        <StackPanel>
            <TextBlock Text="{DynamicResource LOCPlaytimeInsightsRangeDuration}"
                       Style="{StaticResource MetricHeaderStyle}" />
            <Grid Margin="0,12,0,0"
                  ToolTip="{Binding RangeDurationDisplay.AutomationText}"
                  AutomationProperties.Name="{Binding RangeDurationDisplay.AutomationText}">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0"
                           MinWidth="0"
                           Text="{Binding RangeDurationDisplay.MajorValue}"
                           Style="{StaticResource HeroMetricValueStyle}" />
                <TextBlock Grid.Column="1"
                           Text="{Binding RangeDurationDisplay.MajorUnit}"
                           Style="{StaticResource HeroMetricUnitStyle}" />
                <TextBlock Grid.Column="2"
                           Text="{Binding RangeDurationDisplay.MinorValue}"
                           Style="{StaticResource HeroMetricMinorValueStyle}" />
                <TextBlock Grid.Column="3"
                           Text="{Binding RangeDurationDisplay.MinorUnit}"
                           Style="{StaticResource HeroMetricUnitStyle}" />
            </Grid>
        </StackPanel>
    </Border>
    <Border x:Name="SessionCountHeroCard">
        <Border.Style>
            <Style TargetType="Border"
                   BasedOn="{StaticResource HeroMetricCardStyle}">
                <Setter Property="Grid.Row" Value="0" />
                <Setter Property="Grid.Column" Value="2" />
                <Setter Property="Grid.ColumnSpan" Value="1" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsCompactHeroLayout, RelativeSource={RelativeSource AncestorType={x:Type UserControl}}}"
                                 Value="True">
                        <Setter Property="Grid.Row" Value="2" />
                        <Setter Property="Grid.Column" Value="0" />
                        <Setter Property="Grid.ColumnSpan" Value="3" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>
        <StackPanel>
            <TextBlock Text="{DynamicResource LOCPlaytimeInsightsSessionCount}"
                       Style="{StaticResource MetricHeaderStyle}" />
            <TextBlock Text="{Binding SessionCountText}"
                       Margin="0,12,0,0"
                       Style="{StaticResource HeroMetricValueStyle}"
                       AutomationProperties.Name="{Binding SessionCountText}" />
        </StackPanel>
    </Border>
</Grid>
```

上述两个本地 `Border.Style` 是唯一布局切换入口：默认两张卡都在 Row 0，分别位于 Column 0 和 Column 2；`IsCompactHeroLayout=True` 时，第一张卡跨三列留在 Row 0，第二张卡跨三列移至 Row 2。不要同时在 Border 属性和 Style Setter 中重复设置 `Grid.Row`、`Grid.Column` 或 `Grid.ColumnSpan`，否则会因本地值优先级导致触发器失效。

代码后置新增 View 状态：

```csharp
public static readonly DependencyProperty IsCompactHeroLayoutProperty =
    DependencyProperty.Register(
        nameof(IsCompactHeroLayout),
        typeof(bool),
        typeof(PlaytimeInsightsDashboardView),
        new PropertyMetadata(false));

public bool IsCompactHeroLayout
{
    get => (bool)GetValue(IsCompactHeroLayoutProperty);
    private set => SetValue(
        IsCompactHeroLayoutProperty,
        value);
}
```

构造函数订阅 `SizeChanged`，处理器只执行：

```csharp
private void PlaytimeInsightsDashboardView_SizeChanged(
    object sender,
    SizeChangedEventArgs e)
{
    IsCompactHeroLayout = e.NewSize.Width < 640d;
}
```

该状态不调用任何命令。不得让文本缩放依赖 viewport 字体计算。时长 Hero 必须使用上面的四列 Grid：主数值放在 `Width="*"` 列并设置 `MinWidth="0"` 与 `TextTrimming`，其余单位和值使用 Auto 列；不要改回横向 `StackPanel`，否则主数值不会获得可用于省略的有限宽度。会话数 TextBlock 继续使用 `TextTrimming="CharacterEllipsis"`。

时长 Hero 的完整本地化文本绑定到：

```xml
AutomationProperties.Name="{Binding RangeDurationDisplay.AutomationText}"
ToolTip="{Binding RangeDurationDisplay.AutomationText}"
```

- [ ] **Step 4: Keep seven Tier 2 cards in `ResponsiveUniformPanel`**

顺序固定为：

```text
Active Days
Average Session
Longest Session
Playnite Lifetime Duration
Longest Streak
Current Streak
Anomaly Hints
```

平均、最长和累计时长卡使用结构化展示模型；活跃天数、连续天数和异常数继续使用现有本地化文本属性。图标放入 32x32、`CornerRadius=8` 的固定底座，分别引用 Task 5 的 duration/session/activity/anomaly 资源。

- [ ] **Step 5: Replace the main vertical sequence with `AdaptiveDashboardPanel`**

根元素属性固定为：

```xml
<controls:AdaptiveDashboardPanel
    EnterWideWidth="1200"
    ExitWideWidth="1160"
    SecondaryColumnRatio="0.38"
    ColumnSpacing="18"
    VerticalSpacing="18" />
```

将该自闭合元素展开，并按下表把当前模块的完整现有内容移动为五个直接子项。每个子项增加指定 `x:Name` 和 Zone，不复制绑定或创建第二份数据控件。

| Source order | Child name | Zone | Required contained bindings |
| --- | --- | --- | --- |
| 1 | `TrendModule` | `Primary` | `PeriodTitleText`, `AggregationOptions`, `SelectedAggregationOption`, `PeriodActivities` |
| 2 | `RankingModule` | `Secondary` | Task 7 的两个 Tab，`RangeGameRankings`, `LifetimeGameRankings` |
| 3 | `DistributionModule` | `Primary` | `WeekdayDistribution`, `HourDistribution`, `HeatmapCells`, `WeekHourCells` |
| 4 | `AnomalyModule` | `Secondary` | `AnomalyVisibility`, `Anomalies` |
| 5 | `DrilldownModule` | `Primary` | `SessionDetailVisibility`, `SessionDetails`, `LoadMoreSessionDetailsCommand` |

每个直接子项使用 `controls:AdaptiveDashboardPanel.Zone="Primary"` 或 `controls:AdaptiveDashboardPanel.Zone="Secondary"`。`AnomalyModule` 和 `DrilldownModule` 保留各自现有 Visibility 绑定。

保留根 `DashboardScrollViewer` 的 `HorizontalScrollBarVisibility="Disabled"`。热力图可保留自身局部横向滚动，其他模块不得设置全局 `MinWidth`。

- [ ] **Step 6: Move aggregation controls into the trend header**

粒度控件继续绑定 `AggregationOptions` 和 `SelectedAggregationOption`，包含 Auto、Day、Week、Month、Year 全部选项。移动控件不得改 ViewModel 接口，仍只触发 `DashboardRefreshReason.Aggregation`。

- [ ] **Step 7: Run build and visual-tree tests**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass; visual tree contains two Hero cards, seven Tier 2 cards, one adaptive main panel.

- [ ] **Step 8: Commit**

```powershell
git add Views/PlaytimeInsightsDashboardView.xaml Views/PlaytimeInsightsDashboardView.xaml.cs Tests/Program.cs
git commit -m "feat: recompose dashboard visual hierarchy"
```

---

### Task 10: Complete the Drilldown Card Workflow

**Files:**
- Modify: `ViewModels/Dashboard/SessionDetailViewModel.cs`
- Modify: `Services/AnalyticsService.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `SessionDetailViewModel.Source : SessionSource`
- Produces: `DashboardViewModel.ClearDrilldownSelectionCommand : RelayCommand`
- Consumes: existing `DashboardDrilldownViewModel.ResetSelection()`

- [ ] **Step 1: Extend drilldown tests**

在 `TestSessionDrilldown` 增加：

```csharp
Equal(SessionSource.Recovered, details[0].Source);
```

注册：

```csharp
Run("Dashboard clear command resets drilldown selection", TestClearDrilldownSelectionCommand);
Run("Dashboard drilldown cards retain recycling virtualization", TestDrilldownVirtualizationContract);
```

命令测试可直接验证根源码包含：

```csharp
ClearDrilldownSelectionCommand = new RelayCommand(
    Drilldown.ResetSelection,
    () => !refreshGuard.IsActive &&
        Drilldown.SessionDetailVisibility == Visibility.Visible);
```

虚拟化测试解析 Dashboard XAML，断言同一 `ListView` 包含：

```xml
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.VirtualizationMode="Recycling"
ScrollViewer.CanContentScroll="True"
```

并断言不再包含该列表的 `GridView`。

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: compile failure for missing `Source` and command.

- [ ] **Step 3: Project the source enum**

```csharp
public SessionSource Source { get; set; }
```

`AnalyticsService.CreateSessionDetails` 对象初始化同时设置：

```csharp
Source = session.Source,
SourceText = GetSessionSourceLabel(session.Source)
```

颜色触发继续绑定 `Source`；`SourceText` 只负责可见文本和无障碍名称。

- [ ] **Step 4: Add the root clear command**

在构造函数创建：

```csharp
ClearDrilldownSelectionCommand = new RelayCommand(
    Drilldown.ResetSelection,
    () => !refreshGuard.IsActive &&
        Drilldown.SessionDetailVisibility == Visibility.Visible);
```

声明公共属性并在 `RaiseCommandStates`、`DrilldownPropertyChanged` 的 `SessionDetailVisibility` 分支更新 CanExecute。

- [ ] **Step 5: Replace the GridView with a virtualized card template**

标题栏右侧放置使用 MDL2 `Clear`/`Cancel` 字形的图标按钮，绑定清除命令，Tooltip 和 Automation Name 使用 `LOCPlaytimeInsightsClearSelection`。

列表保留 `ListView`，ItemTemplate 每项包含：

```text
36x50 cover
game name with CharacterEllipsis
local start text
duration text
source tag using SessionSourceTagStyle
```

卡片不得设置超过右栏宽度的 `MinWidth`。`LoadMoreSessionDetailsCommand` 和 100 项分页保持不变。

- [ ] **Step 6: Add localization**

新增：

```xml
<sys:String x:Key="LOCPlaytimeInsightsClearSelection">Clear selection</sys:String>
```

```xml
<sys:String x:Key="LOCPlaytimeInsightsClearSelection">清除选中</sys:String>
```

- [ ] **Step 7: Run tests**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: all tests pass; recovered source enum is preserved; virtualization guards pass.

- [ ] **Step 8: Commit**

```powershell
git add ViewModels/Dashboard/SessionDetailViewModel.cs Services/AnalyticsService.cs ViewModels/DashboardViewModel.cs Views/PlaytimeInsightsDashboardView.xaml Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: complete dashboard drilldown cards"
```

---

### Task 11: Add Final Guards and Execute the Acceptance Gates

**Files:**
- Modify: `Tests/Program.cs`
- Modify: `docs/CLIENT_ACCEPTANCE_1.0.0.md`
- Verify: all files listed in `File Map`

**Interfaces:**
- Consumes: Tasks 1-10
- Produces: automated gate evidence and manual visual acceptance record

- [ ] **Step 1: Add final static architecture guards**

新增一个 `TestDashboardVisualRefactorContract`，至少断言：

```text
Dashboard XAML contains exactly one AdaptiveDashboardPanel.
Dashboard XAML contains exactly one ResponsiveUniformPanel.
The Tier 2 panel contains seven metric card children.
RangeGameRankings and LifetimeGameRankings both remain bound.
The shared resource dictionary is explicitly merged in both views.
App.xaml is not used as the only visual resource entry.
PlaytimeInsights.cs contains no new dashboard navigation API.
DashboardViewModel contains no layout-width or filter-expansion state.
AdaptiveDashboardPanel constants are 1200, 1160, 18, and 0.38.
The drilldown ListView keeps Recycling virtualization.
The Dashboard root ScrollViewer disables global horizontal scrolling.
No fixed dark heatmap or tooltip color remains.
```

使用 `XDocument` 检查元素数量和属性；仅对 C# 方法调用或资源字符串使用精确 source guard，避免用宽泛正则代替可解析结构。

- [ ] **Step 2: Run localization and source coverage**

Run:

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: `TestLocalizationResourceParity`、`TestLocalizationSourceCoverage` 和新的视觉契约测试全部通过。

- [ ] **Step 3: Run the final Release gate**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected:

```text
Plugin build: 0 warning, 0 error
Test build: 0 warning, 0 error
Test process: All Playtime Insights tests passed.
100k-session analytics: <= 750 ms
schema 4 load: <= 1400 ms
```

- [ ] **Step 4: Execute the Playnite visual matrix**

对以下组合留存截图并把文件名写入 `docs/CLIENT_ACCEPTANCE_1.0.0.md`：

```text
Languages: zh_CN, en_US
Themes: Default Dark, Default Light, Seaside Dark, one high-contrast third-party theme, Windows High Contrast
DPI: 100%, 125%, 150%, 175%, 200%
Widths: 400, 640, 900, 1159, 1160, 1199, 1200, 1600, 2400 DIP
Data: empty, 10-500 sessions, long English game names, very large duration,
      comparison increase/decrease/equal/new, anomaly hidden/visible,
      drilldown empty/100+, ranking below 3/above 10
```

每个失败项记录主题、DPI、内容宽度、数据状态、截图和复现步骤。修复后重新执行受影响组合以及相邻断点 1159/1160/1199/1200。

- [ ] **Step 5: Verify interaction purity in the running client**

使用现有 refresh Trace 输出验证：

```text
Quick range click: exactly one reason=Range
Aggregation change: reason=Aggregation and no data reload
Ranking metric change: reason=Ranking and no data reload
Ranking Tab switch: no refresh trace
Filter Expander toggle: no refresh trace
1199/1200 and 1160/1159 layout transition: no refresh trace
Clear drilldown: no analysis refresh
```

- [ ] **Step 6: Mark acceptance evidence complete**

仅在对应证据已存在时勾选 `docs/CLIENT_ACCEPTANCE_1.0.0.md`。记录实际测试总数、耗时、主题名和截图目录，不填写推测值。

- [ ] **Step 7: Commit final guards and evidence**

```powershell
git add Tests/Program.cs docs/CLIENT_ACCEPTANCE_1.0.0.md
git commit -m "test: close dashboard visual refactor acceptance"
```

---

## Delivery Gates

### Gate A: Data Semantics

在 Task 3 后检查：

- `Last7Days` 和 `Last30Days` 为包含今天的 7/30 个本地自然日。
- `AllSessions` 从筛选后的最早有效会话本地日期开始。
- 空数据和仅含删除/零时长数据时，`AllSessions` 范围为今天。
- `AllSessions` 不创建 previous/year 范围，也不扫描比较数据。
- 手动聚合继续覆盖自动聚合。

### Gate B: Presentation Contracts

在 Task 7 后检查：

- 时长的数值和单位来自结构化模型，不在 XAML 拆字符串。
- 共享资源由两个 View 显式合并。
- 快捷时间与 ComboBox 使用同一 `SelectedRangeOption`。
- 排行榜 Tab 切换和筛选折叠没有 ViewModel 选择状态。
- 主题基础色没有被插件固定色替代。

### Gate C: Responsive Composition

在 Task 9 后检查：

- 1200 DIP 进入双栏，1160 DIP 以下退出双栏。
- 双栏左右区域独立累加高度。
- 窄屏恢复 XAML 源顺序。
- 两张 Hero 和七张 Tier 2 全部存在。
- 根页面无横向滚动；局部热力图仍可访问。

### Gate D: Workflow and Release

在 Task 11 后检查：

- 下钻来源绑定枚举，清除命令可用。
- Recycling 虚拟化和 100 项分页未退化。
- 跨侧边栏导航仍未进入第一轮范围。
- 自动化、性能和实机视觉矩阵均有证据。

## Spec Coverage Self-Review

| Roadmap requirement | Implementation task |
| --- | --- |
| 新日期预设与自动聚合 | Tasks 1-2 |
| `AllSessions` 最早有效本地日期 | Task 2 |
| `AllSessions` 比较语义 | Task 3 |
| 数字/单位结构化 | Task 4 |
| 主题资源和语义色边界 | Tasks 5-6 |
| Tooltip、热力图、节点和十字线主题化 | Task 6 |
| 4 DIP 排行榜能量条 | Task 6 |
| 快捷 Chips 与单次刷新 | Task 7 |
| 高级筛选折叠和激活摘要 | Task 7 |
| 本期/累计排行榜 Tab 和固定口径 | Task 7 |
| 1200/1160 DIP 自适应布局 | Tasks 8-9 |
| 两张 Hero 和七张 Tier 2 | Task 9 |
| 粒度控件移入趋势标题 | Task 9 |
| 异常微卡片 | Task 9 |
| 下钻来源、清除和卡片化 | Task 10 |
| 虚拟化和分页保全 | Task 10 |
| 中英文、主题、DPI、宽度和数据矩阵 | Task 11 |
| 性能预算 | Tasks 0 and 11 |
| 跨侧边栏导航延期 | Global Constraints and Gate D |

自审结果：路线图 v1.1 的可实施范围均有对应任务；接口名称在各任务间保持一致；没有要求修改存储 schema、私有 Playnite UI 或现有 `ResponsiveUniformPanel`。

## Execution Handoff

计划实施时优先使用 `superpowers:subagent-driven-development`，每个 Task 使用独立实现者并在提交前做规格和代码质量两阶段审查。若在单一会话内执行，使用 `superpowers:executing-plans`，按 Gate A-D 分批推进并在每个 Gate 后停下复核证据。
