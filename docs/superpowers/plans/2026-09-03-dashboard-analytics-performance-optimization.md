# Dashboard Analytics Performance Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Status (2026-09-03):** Task 1–6 已全部实现、逐项提交并通过自动化门禁；性能构建已部署到本机 Playnite。当前性能分支尚未推送或合并；交付状态以 `docs/IMPLEMENTATION_STATUS.md` 为准。

**Goal:** 将 10 年 / 5,000 游戏 / 100,000 会话 Dashboard 分析从临界的 695–759 ms 收敛到五轮中位数不高于 650 ms、最大值不高于 700 ms，同时保持既有 750 ms 发布硬门禁、统计语义和选择性刷新边界不变。

**Architecture:** 保留 `AnalyticsService` 作为完整快照协调边界，但让一次按日拆分同时服务当前范围、上一等长区间和去年同期，避免 Advanced 阶段重复扫描并重复拆分全部会话。抽取一个由按日/按小时拆分器共享的时区解析缓存；为按日和按小时拆分增加可复用目标缓冲区，消除每会话 Dictionary、LINQ 迭代器、List 和小时分片对象分配。Advanced 阶段只做一次会话扫描，并在同一循环内完成小时累计和异常候选生成。

**Tech Stack:** C# 7.3、.NET Framework 4.6.2、WPF、Playnite SDK、现有控制台回归套件 `Tests/Program.cs`、`Stopwatch`、`GC.CollectionCount`。

**Spec:** `README.md` 的 100k 已知性能项、`docs/CLIENT_ACCEPTANCE_1.1.0.md` 的 Release / Performance Gate、`docs/superpowers/plans/2026-08-17-dashboard-visual-elevation-implementation.md` 的 Gate A 与 Gate E。

## Global Constraints

- 不提高或删除 100k 分析 `750 ms` 硬门禁；优化完成目标为五轮中位数 `<= 650 ms`、五轮最大值 `<= 700 ms`，为环境抖动保留至少 50 ms 余量。
- schema 4 / 100k 会话加载继续 `<= 1400 ms`；本计划不修改存储 schema、序列化格式或迁移逻辑。
- 基准数据生成、5,000 个 Game 与 100,000 个 GameSession 的构造必须位于计时器外；计时器只覆盖 `AnalyticsService.CreateSnapshot`。
- 基准流程固定为一次不计时预热后连续五次正式测量；打印五个样本、中位数、最大值和 Gen 0/1/2 GC 增量。不得只取最小值，不得失败后自动重试。
- 保持跨零点、DST、保存时 UTC offset、Custom / All Sessions、上一等长区间、去年同期、异常检测和五种排行指标的现有口径。
- `DashboardAnalysisContext` 继续是趋势与排行重新投影的唯一共享上下文；筛选、趋势粒度或排行指标切换不得重新扫描会话。
- 不引入 BenchmarkDotNet、第三方集合或新的运行时依赖；Release 构建保持 0 warning / 0 error。
- 每个优化提交只改变一个成本来源；每步都先取得可复现 RED 或可量化基线，再实施最小改动。

## File Map

| File | Action | Responsibility |
| --- | --- | --- |
| `Tests/Program.cs` | Modify | 五轮统计、GC 证据、语义回归和最终门禁 |
| `Services/SessionTimeZoneResolver.cs` | Create | 按 TimeZoneId + offset 缓存并解析会话时区 |
| `Services/DailyAllocationService.cs` | Modify | 使用共享时区解析器；发布可复用 `DailyAllocation` 缓冲区重载 |
| `Services/HourlyAllocationService.cs` | Modify | 复用同一个 `SessionTimeZoneResolver`；发布值类型小时分片与可复用缓冲区重载 |
| `Services/DashboardAnalysisContext.cs` | Modify | 携带上一周期/去年同期范围与总秒数 |
| `Services/AnalyticsService.cs` | Modify | 单次按日拆分累计三个范围，并向 Advanced 传递比较结果 |
| `Services/AdvancedAnalyticsService.cs` | Modify | 删除两次比较区间全量扫描；小时与异常处理合并为一次会话循环 |
| `README.md` | Modify | 优化完成后用新五轮证据替换 715/759 ms 已知项 |
| `docs/IMPLEMENTATION_STATUS.md` | Modify | 记录最终分析性能证据和剩余 UI 性能项 |

---

### Task 1: Establish a Deterministic Five-Sample Analytics Gate

**Files:**
- Modify: `Tests/Program.cs:539-606`

**Interfaces:**
- Produces: nested test helper `AnalyticsPerformanceSampleSummary`
- Produces: `MeasureAnalyticsSamples(Func<DashboardSnapshot> action, int warmupCount, int measuredCount)`
- Preserves: existing fixture size, query, semantic assertions and 750 ms hard limit

- [x] **Step 1: Register a deterministic summary test**

在 `Main()` 的 100k 性能测试之前注册：

```csharp
Run("Analytics performance summary reports median maximum and GC deltas",
    TestAnalyticsPerformanceSampleSummary);
```

新增固定样本测试，不依赖墙钟：

```csharp
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
```

- [x] **Step 2: Run the focused test to verify RED**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: 编译失败，指出 `AnalyticsPerformanceSampleSummary` 尚不存在。

- [x] **Step 3: Add the sample summary and measurement helper**

在 `Tests/Program.cs` 的测试辅助类型区域增加：

```csharp
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
```

`MeasureAnalyticsSamples` 必须先执行一次不计时 `action()`，再记录 GC 计数、连续执行五次 `action()`，最后计算差值。不得在五个正式样本之间调用 `GC.Collect()`。

- [x] **Step 4: Convert `TestLargeTenYearAnalytics` to the five-sample contract**

保留 fixture 和现有快照语义断言；用同一个 `Func<DashboardSnapshot>` 进行一次预热与五次正式测量。打印格式固定为：

```text
100k analytics samples: 640 / 650 / 660 / 700 / 710 ms; median 660 ms; max 710 ms; GC 0/1/2 = 3/1/0
```

此任务只建立证据，不立即要求 650/700 优化目标。继续保留现有 `MaxMilliseconds <= 750` 发布门禁；把当前五轮结果写入测试输出，作为后续任务 RED 基线。

- [x] **Step 5: Run the complete regression and commit the harness**

Run:

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: 两个构建 0 warning / 0 error；除现有 750 ms 性能抖动可能触发的门禁外无功能失败；输出完整五轮样本和 GC 增量。

```powershell
git add Tests\Program.cs
git commit -m "test: stabilize dashboard analytics performance evidence"
```

---

### Task 2: Share and Cache Session Time-Zone Resolution

**Files:**
- Create: `Services/SessionTimeZoneResolver.cs`
- Modify: `Services/DailyAllocationService.cs:9-105`
- Modify: `Services/HourlyAllocationService.cs:18-158`
- Modify: `Services/AnalyticsService.cs:69-74`
- Modify: `Services/AdvancedAnalyticsService.cs:12-17`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `SessionTimeZoneResolver.Resolve(GameSession session) : TimeZoneInfo`
- Produces: `DailyAllocationService(SessionTimeZoneResolver resolver)`
- Produces: `HourlyAllocationService(SessionTimeZoneResolver resolver)`
- Produces: `AdvancedAnalyticsService(SessionTimeZoneResolver resolver)`
- Preserves: parameterless constructors for existing callers and tests

- [x] **Step 1: Add cache identity and fallback tests**

注册并实现：

```csharp
Run("Session timezone resolver caches valid and fallback zones",
    TestSessionTimeZoneResolverCache);
```

测试使用两个 `TimeZoneId="China Standard Time"`、offset 480 的不同会话，断言两次 `Resolve` 返回同一引用；再使用无效 TimeZoneId + offset 330 的两个会话，断言返回同一固定偏移引用且 `BaseUtcOffset == TimeSpan.FromMinutes(330)`。

- [x] **Step 2: Run the focused test to verify RED**

Run test build. Expected: `SessionTimeZoneResolver` 类型不存在。

- [x] **Step 3: Implement one thread-safe resolver**

`SessionTimeZoneResolver` 使用 `Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase)` 和现有 lock 模式。cache key 固定为：

```csharp
var cacheKey = !string.IsNullOrWhiteSpace(session.TimeZoneId)
    ? "id:" + session.TimeZoneId + "|offset:" + session.StartUtcOffsetMinutes
    : "offset:" + session.StartUtcOffsetMinutes;
```

先尝试 `FindSystemTimeZoneById`；失败时创建固定偏移时区。解析完成后在锁内二次检查并发布，保证并发调用只保留一个缓存实例。

- [x] **Step 4: Inject the resolver into both allocation services**

两个服务都保留参数less constructor，并委托到带 resolver 的 constructor：

```csharp
public DailyAllocationService()
    : this(new SessionTimeZoneResolver())
{
}

public DailyAllocationService(SessionTimeZoneResolver resolver)
{
    timeZoneResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
}
```

`AnalyticsService` 的构造函数创建一个 resolver，并传给 `DailyAllocationService` 与 `AdvancedAnalyticsService`；Advanced 再把同一实例传给 `HourlyAllocationService`。删除两个拆分器中的重复 ResolveTimeZone 实现和 Hourly 私有缓存。

- [x] **Step 5: Run allocation, DST, cross-midnight and full tests**

Expected: 所有时间分配测试通过；五轮 100k 样本相较 Task 1 不得退化；Release 0 warning / 0 error。

```powershell
git add Services\SessionTimeZoneResolver.cs Services\DailyAllocationService.cs Services\HourlyAllocationService.cs Services\AnalyticsService.cs Services\AdvancedAnalyticsService.cs Tests\Program.cs
git commit -m "perf: share cached session timezone resolution"
```

---

### Task 3: Reuse Daily-Allocation Buffers and Remove Per-Session LINQ

**Files:**
- Modify: `Services/DailyAllocationService.cs`
- Modify: `Services/AnalyticsService.cs:140-208`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: value type `DailyAllocation { DateTime LocalDate; ulong Seconds; }`
- Produces: `DailyAllocationService.SplitByLocalDay(GameSession session, IList<DailyAllocation> destination) : void`
- Preserves: existing `SplitByLocalDay(GameSession) : IDictionary<DateTime, ulong>` API

- [x] **Step 1: Add destination-reuse equivalence tests**

测试创建普通、跨午夜、DST 和 `EndedAtUtc <= StartedAtUtc` 四类会话。对每类会话分别调用旧 Dictionary API 与新 destination API，断言日期和秒数完全一致；在同一个 `List<DailyAllocation>` 上连续调用两次，断言第二次结果不包含第一次残留。

- [x] **Step 2: Run the test build to verify RED**

Expected: `DailyAllocation` 和 destination overload 不存在。

- [x] **Step 3: Implement the value buffer overload**

新增：

```csharp
public struct DailyAllocation
{
    public DateTime LocalDate { get; set; }
    public ulong Seconds { get; set; }
}

public void SplitByLocalDay(
    GameSession session,
    IList<DailyAllocation> destination)
```

方法入口调用 `destination.Clear()`，现有循环直接向 destination 合并同一天条目。旧 Dictionary API 创建一个小 List、调用 overload，再投影为 Dictionary，保证兼容调用方语义不变。

- [x] **Step 4: Replace the main-loop allocation pipeline**

在 `CreateSnapshotWithContext` 外层创建一次：

```csharp
var dailyAllocationBuffer = new List<DailyAllocation>(2);
```

每个 session 调用 destination overload，并用一个 foreach 同时完成范围判断、`includedSeconds`、`dailySeconds`、`ActiveDates` 和 `dailyGameNames`。删除：

```csharp
allocations.Where(...).ToList();
includedAllocations.Aggregate(...);
```

- [x] **Step 5: Verify semantics, five-sample evidence and commit**

Expected: 所有分配/范围/排行测试通过；Task 1 五轮 GC Gen 0 增量和最大时间不高于前一提交。

```powershell
git add Services\DailyAllocationService.cs Services\AnalyticsService.cs Tests\Program.cs
git commit -m "perf: reuse daily allocation buffers"
```

---

### Task 4: Accumulate Current, Previous, and Year Ranges in One Pass

**Files:**
- Modify: `Services/DashboardAnalysisContext.cs`
- Modify: `Services/AnalyticsService.cs:118-263`
- Modify: `Services/AdvancedAnalyticsService.cs:81-145,199-215`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `DashboardComparisonTotals`
- Produces: `DashboardAnalysisContext.ComparisonTotals`
- Consumes: each `DailyAllocation` exactly once per session
- Removes: `AdvancedAnalyticsService.CalculateRangeSeconds`

- [x] **Step 1: Add overlapping-range accumulator tests**

定义测试数据覆盖：当前范围与去年同期重叠、上一周期不重叠、闰日和跨午夜。断言同一日可以同时计入 Current 与 YearOverYear，但只按各自范围判断一次。

`DashboardComparisonTotals` 最终接口固定为：

```csharp
public sealed class DashboardComparisonTotals
{
    public bool Enabled { get; set; }
    public AnalyticsDateRange PreviousRange { get; set; }
    public AnalyticsDateRange YearOverYearRange { get; set; }
    public ulong PreviousSeconds { get; set; }
    public ulong YearOverYearSeconds { get; set; }
}
```

- [x] **Step 2: Run tests to verify RED**

Expected: `ComparisonTotals` 属性不存在。

- [x] **Step 3: Resolve comparison ranges before the session loop**

当 `RangePreset != AllSessions` 时，使用现有 `CreatePreviousPeriodRange` 和 `CreateYearOverYearRange` 生成两个范围；All Sessions 发布 `Enabled=false` 且两个 range 为 null。

- [x] **Step 4: Add comparison totals to the existing allocation foreach**

在 Task 3 的 `foreach (var allocation in dailyAllocationBuffer)` 中，先独立判断 PreviousRange 和 YearOverYearRange 并累计到两个 ulong，再判断是否属于 Current。比较累计必须发生在 `includedSeconds == 0` 的 current-range `continue` 之前，否则完全位于上一周期或去年同期的会话会被漏掉。当前范围逻辑保持原样；范围重叠时允许同一 allocation 同时进入 Current 和 YearOverYear。

- [x] **Step 5: Consume totals in Advanced and delete two rescans**

`AdvancedAnalyticsService.CreateSnapshot` 接收 `DashboardComparisonTotals comparisonTotals`。直接用其中的 range/seconds 创建两个 ComparisonMetricViewModel，删除 `CalculateRangeSeconds` 和两次 100k session 扫描。

- [x] **Step 6: Run comparison semantics and performance gate**

Expected: previous/year/leap-day/All Sessions 比较测试不变；五轮 100k 中位数 `<= 650 ms`、最大值 `<= 700 ms`。若最大值仍超过 700 ms，继续 Task 5；不得放宽目标。

```powershell
git add Services\DashboardAnalysisContext.cs Services\AnalyticsService.cs Services\AdvancedAnalyticsService.cs Tests\Program.cs
git commit -m "perf: aggregate comparison ranges in one pass"
```

---

### Task 5: Reuse Hourly Buffers and Merge Advanced Scans

**Files:**
- Modify: `Services/HourlyAllocationService.cs`
- Modify: `Services/AdvancedAnalyticsService.cs:19-167,419-508`
- Test: `Tests/Program.cs`

**Interfaces:**
- Changes: `HourlyAllocation` from reference type to value type
- Produces: `HourlyAllocationService.SplitByLocalHour(GameSession session, IList<HourlyAllocation> destination) : void`
- Preserves: existing `SplitByLocalHour(GameSession) : IList<HourlyAllocation>` API
- Produces: `CreateAnomalyCandidate(GameSession session, IDictionary<Guid, string> names) : Tuple<DateTime, AnomalySessionViewModel>`
- Preserves: anomaly sort, top-50 limit, text and no-mutation behavior
- Preserves: exactly one hourly split per session

- [x] **Step 1: Add hourly destination-reuse tests**

为普通、跨小时、跨午夜和 DST 会话分别调用旧返回值 API 与新的 destination API，断言日期、小时、秒数和总秒数完全一致。在同一个 `List<HourlyAllocation>` 上连续调用两次，断言第二次结果不含第一次残留；断言 `typeof(HourlyAllocation).IsValueType` 为 true。

- [x] **Step 2: Run the hourly tests to verify RED**

Expected: destination overload 不存在，`HourlyAllocation` 仍是 reference type。

- [x] **Step 3: Implement the value buffer overload**

把 `HourlyAllocation` 改为 `struct`。新增 destination overload，入口先 `destination.Clear()`，并把当前所有 `result.Add(new HourlyAllocation { ... })` 写入 destination。旧 API 只创建 List、调用 overload 并返回，保持所有现有调用方兼容。

- [x] **Step 4: Add a source-loop contract and anomaly equivalence tests**

注册 `Advanced analytics processes sessions in one loop`。测试读取完整 `AdvancedAnalyticsService.cs`，最终契约直接断言 `CountOccurrences(source, "foreach (var session in") == 1`，并断言不存在 `CreateAnomalies(gameList, sessionList, range)`。源码计数使用测试文件内新增的 `CountOccurrences(string source, string value)`，不得用脆弱的行号。

同时保留运行时等价测试：构造覆盖零秒、结束早于开始、未来开始、18 小时以上、墙钟不一致和正常会话的固定集合，断言异常原因、降序顺序、top-50、小时分布与输入对象不变。

- [x] **Step 5: Run the Advanced test to verify RED**

Expected: 源码契约失败，因为当前 `CreateSnapshot` 的小时循环之外仍调用接收完整 `sessionList` 的 `CreateAnomalies`；运行时等价测试继续通过。

- [x] **Step 6: Fold allocation and anomaly creation into one loop**

在进入 session loop 前创建一个 `List<HourlyAllocation>(4)`、names 字典和 anomaly tuple list。每个 session 调用新的 destination overload，遍历复用缓冲区完成现有小时累计，再调用 `CreateAnomalyCandidate`；非异常返回 null，异常加入列表。循环后保持现有 `OrderByDescending().Take(50)` 投影。

- [x] **Step 7: Remove redundant materialization**

将 Advanced 参数改为 `IList<Game>` 与 `IList<GameSession>`；删除入口的 `.ToList()`。`AnalyticsService` 已传入 list，因此不改变调用语义。

- [x] **Step 8: Run anomaly/hourly/full regression and commit**

Expected: 完整回归通过；五轮 100k 中位数 `<= 650 ms`、最大值 `<= 700 ms`；schema 4 `<= 1400 ms`。

```powershell
git add Services\HourlyAllocationService.cs Services\AdvancedAnalyticsService.cs Tests\Program.cs
git commit -m "perf: combine advanced session scans"
```

---

### Task 6: Freeze the Analytics Performance Evidence

**Files:**
- Modify: `README.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`
- Modify: `docs/CLIENT_ACCEPTANCE_1.1.0.md`
- Verify: all production and test files from Tasks 1–5

**Interfaces:**
- Consumes: Task 1 five-sample summary
- Produces: auditable final median/max/GC evidence

- [x] **Step 1: Run five independent complete Release gates**

每轮执行两个 Release build 和完整回归；记录每轮内部五样本的 median/max。所有轮次必须满足：plugin/test 0 warning / 0 error、100k 五样本 max `<= 700 ms`、median `<= 650 ms`、schema 4 `<= 1400 ms`。

- [x] **Step 2: Compare semantics and scope**

Run:

```powershell
git diff main -- Services Tests README.md docs
git diff main -- Views Controls Localization Resources
```

Expected: 第二条命令无差异；本计划不得改变 XAML、视觉资源、本地化文本或控件布局。

- [x] **Step 3: Update evidence without hiding the historical failure**

README 保留历史 715/759 ms 说明，并追加新五轮范围和“预算未放宽”；`IMPLEMENTATION_STATUS` 与 `CLIENT_ACCEPTANCE_1.1.0` 写入最终 max/median/GC。不得删除 759 ms 失败样本。

- [x] **Step 4: Commit final analytics evidence**

```powershell
git add README.md docs\IMPLEMENTATION_STATUS.md docs\CLIENT_ACCEPTANCE_1.1.0.md Tests\Program.cs
git commit -m "docs: record stabilized dashboard analytics performance"
```

## Delivery Gates

- 两个 Release build：0 warning、0 error。
- 完整回归：`All Playtime Insights tests passed.`。
- 100k / 10 年 / 5,000 游戏：每个门禁内五样本 median `<= 650 ms`，max `<= 700 ms`；硬上限仍为 750 ms。
- schema 4 / 100k：`<= 1400 ms`。
- 与 `main` 相比，`Views/`、`Controls/`、`Localization/`、`Resources/` 无差异。
- 跨零点、DST、比较区间、异常、排行和选择性刷新语义全部保持。

## Execution Handoff

建议先独立执行本计划并完成审核，再开始 Calendar 热力图 UI 性能计划。两个计划不共享生产文件；唯一共享热点是 `Tests/Program.cs`，若并行执行必须由主执行者顺序整合该文件，禁止两个实现者同时提交对同一区域的修改。
