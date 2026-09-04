# Calendar Heatmap UI Performance Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Status (2026-09-04):** Task 1–4 及 Task 5 的自动化、范围审查、证据记录与提交步骤已完成；性能分支已推送并 fast-forward 合并回 `main`。Task 5 的虚拟化后实机矩阵仍待用户验收。

**Goal:** 将一年 371 格 Calendar 热力图的五轮最大 Measure + Arrange 成本降到 `<= 200 ms`，将 All Sessions 1,820 格成本从约 809–1,914 ms 降到 `<= 300 ms`，同时保留 26 DIP 列节拍、24×24 DIP 光泽色块、Button 键盘语义、Tooltip、Automation Name 和月份/周次/格子对齐。

**Architecture:** 第一阶段把每格从多层 Border ControlTemplate 改为仍继承 Button 的轻量自绘控件，消除每格内部视觉树和模板 Trigger 成本。完成五轮测量后执行硬分支：若一年与 All Sessions 已同时达到 200/300 ms，则记录周列虚拟化为跳过；否则把 7 个日期组成一个固定 26 DIP 宽的周列，使用水平 Recycling `VirtualizingStackPanel` 只实现视口附近周列，并用同步 ScrollViewer 让非虚拟化月份轴跟随同一 HorizontalOffset。

**Tech Stack:** C# 7.3、.NET Framework 4.6.2、WPF、Playnite SDK、`DrawingContext`、`Button`、`VirtualizingStackPanel`、现有控制台回归套件 `Tests/Program.cs`。

**Spec:** `README.md` 的 All Sessions 已知限制、`docs/CLIENT_ACCEPTANCE_1.1.0.md` 的 Heatmap UI Layout Baseline、`docs/superpowers/plans/2026-08-17-dashboard-visual-elevation-implementation.md` 的 Global Constraints 与 Gate D。

## Global Constraints

- Calendar 列节拍固定 26 DIP；可见色块固定 24×24 DIP；CornerRadius 固定 3；不得缩回 14×14 DIP。
- 首列保持星期一；星期文字行高 26 DIP；周次文字在 26 DIP 列内水平居中。
- 绝对颜色档位保持 0、`<1h`、`1–3h`、`>3h`，继续使用现有冰青—青绿四档资源；Week×Hour 蓝紫相对刻度不在本计划修改范围。
- 每个日期继续是 `Button`，必须保留 Command、CommandParameter、ToolTip、AutomationProperties.Name、Tab、Space/Enter、焦点描边和 26×26 DIP 命中区。
- 月份轴、周次和日期格始终共享 26 DIP 坐标；虚拟化不得造成滚动漂移、月标签消失或周次错列。
- All Sessions 数据仍包含完整日期范围，不通过截断年份、分页、降低日期精度或隐藏无数据周减少工作量。
- 不使用纯 Canvas 替代可交互元素，不引入 WebView 或第三方 UI 库。
- 测量固定在 STA；snapshot 创建与 `Distribution.Apply` 位于计时器外；计时器只覆盖模板绑定、容器生成、Measure、Arrange 和 UpdateLayout。
- 基准固定一次不计时预热 + 五次正式测量；不得只报告最小值，不得失败后重试。最终门禁为一年 max `<= 200 ms`、All Sessions max `<= 300 ms`。
- 本计划不得修改会话 schema、Analytics 统计口径、热力图绝对阈值或 Dashboard 双栏断点。

## File Map

| File | Action | Responsibility |
| --- | --- | --- |
| `Tests/Program.cs` | Modify | 五轮 UI 样本、视觉树规模、虚拟滚动、键盘和轴同步回归 |
| `Controls/HeatmapCellButton.cs` | Create | 无内部色块视觉树的 26×26 DIP 自绘 Button |
| `ViewModels/Dashboard/HeatmapWeekViewModel.cs` | Conditional Create | 周次、列索引和 7 个现有日期 Cell 的轻量分组 |
| `ViewModels/Dashboard/DashboardSnapshot.cs` | Conditional Modify | 快照携带 `HeatmapWeeks` |
| `ViewModels/Dashboard/DashboardDistributionViewModel.cs` | Conditional Modify | 原子发布周列集合 |
| `ViewModels/DashboardViewModel.cs` | Conditional Modify | 代理 `HeatmapWeeks` |
| `Services/AnalyticsService.cs` | Conditional Modify | 从现有 Cells 引用构建周列，不重复创建 Cell ViewModel |
| `Views/PlaytimeInsightsDashboardView.xaml` | Modify | 使用轻量 Button；条件阶段切换到水平虚拟周列 |
| `Views/PlaytimeInsightsDashboardView.xaml.cs` | Conditional Modify | 同步月份轴与周列 HorizontalOffset |
| `README.md` | Modify | 用最终 UI 预算证据替换 All Sessions 刷新停顿说明 |
| `docs/CLIENT_ACCEPTANCE_1.1.0.md` | Modify | 收口 Heatmap UI Layout Baseline |
| `docs/IMPLEMENTATION_STATUS.md` | Modify | 记录实现策略、最终五轮证据和是否启用虚拟化 |

---

### Task 1: Replace One-Shot Layout Evidence with a Five-Sample Gate

**Files:**
- Modify: `Tests/Program.cs:2197-2253`

**Interfaces:**
- Produces: nested helper `HeatmapLayoutSampleSummary`
- Produces: `MeasureHeatmapLayoutSample(DashboardSnapshot snapshot) : HeatmapLayoutMeasurement`
- Produces: `MeasureHeatmapLayoutSamples(string label, DashboardSnapshot snapshot, int measuredCount) : HeatmapLayoutSampleSummary`
- Preserves: real Distribution template and exact 371 / 1,820 fixture counts

- [x] **Step 1: Add deterministic summary and visual-count tests**

注册：

```csharp
Run("Heatmap layout summary reports median maximum and realization",
    TestHeatmapLayoutSampleSummary);
```

使用固定 `HeatmapLayoutMeasurement` 样本断言 median、max、MaxRealizedButtons。类型接口固定为：

```csharp
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
}
```

- [x] **Step 2: Run test build to verify RED**

Expected: 两个 helper type 尚不存在。

- [x] **Step 3: Implement repeated measurement without changing the template**

每个 snapshot 先构造并布局一次 view 作为预热；然后连续五次重新创建 ViewModel/View 并计时。每次 measurement 记录日期 Button 数与周列 ContentPresenter 数。Task 1 继续断言未虚拟化实现的 `RealizedButtons == HeatmapCells.Count`，建立旧架构基线。

- [x] **Step 4: Print and preserve the baseline**

输出固定格式：

```text
heatmap UI / all sessions / 1,820 cells: 809 / 839 / 1200 / 1581 / 1914 ms; median 1200 ms; max 1914 ms; realized buttons 1820
```

本任务不添加 200/300 ms 硬断言；记录当前 RED 基线并确认没有功能失败。

- [x] **Step 5: Run full regression and commit the harness**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
git add Tests\Program.cs
git commit -m "test: stabilize calendar heatmap layout evidence"
```

---

### Task 2: Replace the Nested Cell Template with a Lightweight Button

**Files:**
- Create: `Controls/HeatmapCellButton.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:1947-2012`
- Modify: `Tests/Program.cs:1823-2195`

**Interfaces:**
- Produces: `HeatmapCellButton : Button`
- Consumes: existing `Background`, `BorderBrush`, `Foreground`, `IsMouseOver`, `IsKeyboardFocused`
- Preserves: `IntensityLevel` to Background mapping in XAML Style triggers

- [x] **Step 1: Add rendering and accessibility contract tests**

注册：

```csharp
Run("Calendar heatmap uses lightweight accessible cell buttons",
    TestHeatmapCellButtonContract);
```

运行时测试加载真实 Distribution 模板并断言：每个日期元素类型为 `HeatmapCellButton`；Width/Height 为 26；Command、CommandParameter、ToolTip 与 Automation Name 非空；None/Low/Medium/High 的 `Background` 分别是四个现有资源；`Foreground` 来自 TextBrush；获得键盘焦点后控件仍可见且触发重新绘制；单格内部 visual descendant 数不超过 1。源码契约另断言 `OnRender` 同时读取 `IsMouseOver`、`IsKeyboardFocused`、`BorderBrush` 和 `Foreground`。

- [x] **Step 2: Run focused tests to verify RED**

Expected: `HeatmapCellButton` 不存在，现有模板仍有 `CellButtonRoot` 与 `CellSwatch` 两层 Border。

- [x] **Step 3: Implement the self-rendering Button**

控件固定接口：

```csharp
public sealed class HeatmapCellButton : Button
{
    protected override void OnRender(DrawingContext drawingContext);
    protected override void OnMouseEnter(MouseEventArgs e);
    protected override void OnMouseLeave(MouseEventArgs e);
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e);
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e);
}
```

`OnRender` 在 `(1,1,24,24)` 绘制 `Background` 圆角矩形；普通边框使用 `BorderBrush` 1 DIP，hover/focus 使用 `Foreground` 1 DIP，focus 再绘制内缩 2 DIP 的虚线或 1 DIP 实线轮廓。四个事件只调用 base 与 `InvalidateVisual()`。每次绘制只按需构造边框与焦点 `Pen`；不得构造子视觉元素、`Geometry` 或 `GradientStop`。

- [x] **Step 4: Replace the inline ControlTemplate**

XAML 使用 `controls:HeatmapCellButton`，保留现有 Command/ToolTip/Automation bindings。Style 默认 Background=HeatmapNoneBrush，并用四个 DataTrigger 设置 Background；BorderBrush 使用 PanelSeparatorBrush，Foreground 使用 TextBrush。Style 内提供一个只含透明 `AdornerDecorator` 的共享 `ControlTemplate`，让 `OnRender` 不被模板覆盖且每格最多一个内部视觉；删除 `CellButtonRoot`、`CellSwatch` 和旧 ControlTemplate triggers。

- [x] **Step 5: Run five-sample gate and take the branch decision**

运行 Task 1 五轮测量：

- 若一年 max `<= 200 ms` 且 All Sessions max `<= 300 ms`：在验收文档记录“轻量 Button 已达预算”，将 Tasks 3–4 标记为跳过并直接执行 Task 5。
- 若任一范围超预算：记录五个样本，不改变阈值，继续 Tasks 3–4。

- [x] **Step 6: Run full regression and commit**

```powershell
git add Controls\HeatmapCellButton.cs Views\PlaytimeInsightsDashboardView.xaml Tests\Program.cs
git commit -m "perf: lighten calendar heatmap cell rendering"
```

---

### Task 3: Publish Week Columns Without Duplicating Cell Models

> **Conditional gate:** 仅当 Task 2 任一五轮最大值超过 200/300 ms 时执行；否则整项标记为“跳过：轻量 Button 已达预算”。

**Files:**
- Create: `ViewModels/Dashboard/HeatmapWeekViewModel.cs`
- Modify: `ViewModels/Dashboard/DashboardSnapshot.cs`
- Modify: `ViewModels/Dashboard/DashboardDistributionViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Services/AnalyticsService.cs:789-917`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `HeatmapWeekViewModel`
- Produces: `DashboardSnapshot.HeatmapWeeks`
- Produces: `DashboardDistributionViewModel.HeatmapWeeks`
- Preserves: existing `HeatmapCells` collection and every Cell object identity

- [x] **Step 1: Add week-grouping and atomic-publication tests**

最终类型：

```csharp
public sealed class HeatmapWeekViewModel
{
    public int ColumnIndex { get; set; }
    public string WeekLabel { get; set; }
    public IReadOnlyList<HeatmapCellViewModel> Days { get; set; }
}
```

测试 2026 年 8 月六周范围：6 个 week；每周 7 个 Day；`Days[row]` 与现有 `HeatmapCells[row * columnCount + column]` 为同一引用；Distribution.Apply 只通知 `HeatmapWeeks` 一次。

- [x] **Step 2: Run tests to verify RED**

Expected: 类型和属性不存在。

- [x] **Step 3: Build week groups from existing cells**

`CreateHeatmapProjection` 完成 Cells 和 WeekLabels 后，按 column 构造 week：

```csharp
var days = new List<HeatmapCellViewModel>(7);
for (var row = 0; row < 7; row++)
{
    days.Add(values[row * columnCount + column]);
}
```

不得 new 第二份 HeatmapCellViewModel。`HeatmapProjection`、Snapshot、Distribution 和根 ViewModel 依次携带只读 week 集合。

- [x] **Step 4: Run projection, atomic-publication and full tests**

Expected: 现有 row-major `HeatmapCells` 契约继续通过；新增 week 引用/通知测试通过。

```powershell
git add ViewModels\Dashboard\HeatmapWeekViewModel.cs ViewModels\Dashboard\DashboardSnapshot.cs ViewModels\Dashboard\DashboardDistributionViewModel.cs ViewModels\DashboardViewModel.cs Services\AnalyticsService.cs Tests\Program.cs
git commit -m "perf: publish calendar heatmap week columns"
```

---

### Task 4: Virtualize Horizontal Week Columns and Synchronize the Month Axis

> **Conditional gate:** 只在 Task 3 已执行时执行。

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:1845-2012`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Consumes: `HeatmapWeeks`
- Produces named elements: `HeatmapMonthScrollViewer`, `HeatmapWeekList`
- Produces handler: `HeatmapWeekList_ScrollChanged(object sender, ScrollChangedEventArgs e)`
- Preserves: one shared 26 DIP horizontal coordinate

- [x] **Step 1: Add virtualization, endpoint and scroll-sync tests**

在 1,820 格 snapshot 下布局真实 Distribution，断言：

- `HeatmapWeekList.Items.Count == 260`；
- 初始 realized week containers `< 60`，realized HeatmapCellButton `< 420`；
- 滚动到末端后最后一周 7 格可实现，首周与末周容器不会同时驻留，realized 数量仍低于阈值；
- 月份轴 HorizontalOffset 与 week list 的内容 offset 差 `<= 0.5 DIP`；
- 首周/末周日期、Command、Automation Name 和 Tab/Space/Enter 不变。

- [x] **Step 2: Run tests to verify RED**

Expected: 当前 XAML 仍绑定 `HeatmapCells` 并实现全部 1,820 个 Button，没有 `HeatmapWeekList` 或同步 offset。

- [x] **Step 3: Recompose the calendar body into synchronized regions**

移除当前包住整张 Calendar 的外层 `ScrollViewer`，改成有限宽度的两列 Grid（左列 46 DIP、右列 `*`），否则内部 `VirtualizingStackPanel` 会收到无限水平 Measure 而完整实现全部周列。月份轴放进右列的 `ScrollViewer x:Name="HeatmapMonthScrollViewer"`，隐藏自身 scrollbar 和命中；周次与 7 格放入右列 `ListBox x:Name="HeatmapWeekList"` 的每个 week item。ListBox 设置：

```xml
ScrollViewer.HorizontalScrollBarVisibility="Auto"
ScrollViewer.VerticalScrollBarVisibility="Disabled"
ScrollViewer.CanContentScroll="True"
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.VirtualizationMode="Recycling"
VirtualizingPanel.ScrollUnit="Pixel"
ScrollViewer.ScrollChanged="HeatmapWeekList_ScrollChanged"
```

ItemsPanel 使用 `VirtualizingStackPanel Orientation="Horizontal"`。每个 item Width=26，上方周次 TextBlock 水平居中，下方 StackPanel 绑定 `Days` 并生成 7 个 `HeatmapCellButton`。星期标签仍在左侧固定 46 DIP 列。

`HeatmapWeekList` 必须 `HorizontalAlignment="Stretch"`，由右侧 `*` 列提供有限 viewport。ItemContainerStyle 设置 `Focusable="False"`、`KeyboardNavigation.IsTabStop="False"`、Padding/Margin/BorderThickness 为 0，并使用只含 ContentPresenter 的模板，避免 ListBoxItem 抢占日期 Button 的 Tab 顺序或重新引入容器视觉层。

- [x] **Step 4: Synchronize the month axis**

`ScrollViewer.ScrollChanged` 是从 ListBox 内部 ScrollViewer 冒泡到 `HeatmapWeekList` 的附加事件。`HeatmapWeekList_ScrollChanged` 仅在 `e.HorizontalChange != 0` 时调用：

```csharp
HeatmapMonthScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
```

不得在 handler 中触发 Dashboard 刷新、修改 ViewModel 或调用 BringIntoView。月份轴保留全部约 60 个轻量 TextBlock，不对其虚拟化。

- [x] **Step 5: Replace the old full-realization assertion**

删除 `realizedCount == HeatmapCells.Count`。新契约同时断言完整数据仍在 `HeatmapCells`/`HeatmapWeeks`、初始视觉只实现视口附近 week、滚动到两端数据与坐标正确。不得把“少实现元素”误写成“少生成日期数据”。

- [x] **Step 6: Run five-sample gate, interaction tests and commit**

Expected: 一年 max `<= 200 ms`；All Sessions max `<= 300 ms`；All Sessions 最大 realized Button `< 420`；键盘、Automation、月份轴和双语言测试通过。

```powershell
git add Views\PlaytimeInsightsDashboardView.xaml Views\PlaytimeInsightsDashboardView.xaml.cs Tests\Program.cs
git commit -m "perf: virtualize calendar heatmap week columns"
```

---

### Task 5: Validate Themes, DPI, Accessibility, and Final UI Budgets

> **Current status:** 自动化与文档步骤已完成；Step 2 的性能版实机矩阵仍待执行，因此本 Task 尚未完全关闭。

**Files:**
- Modify: `README.md`
- Modify: `docs/CLIENT_ACCEPTANCE_1.1.0.md`
- Modify: `docs/IMPLEMENTATION_STATUS.md`
- Verify: all files touched by Tasks 1–4

**Interfaces:**
- Consumes: final five-sample summaries and realized-container counts
- Produces: final user-visible performance status

- [x] **Step 1: Run complete automated gates five times**

每轮执行两个 Release build 和完整回归。每轮都必须满足：0 warning / 0 error、所有测试通过、一年 heatmap max `<= 200 ms`、All Sessions max `<= 300 ms`、100k analytics `<= 750 ms`、schema 4 `<= 1400 ms`。

- [ ] **Step 2: Execute the focused manual matrix**

在 Default Dark、Default Light、Seaside Dark、Windows High Contrast 与 100/125/150/175/200% DPI 下检查：24×24 光泽、26 DIP 对齐、横向滚动、月轴同步、焦点描边、Tab、Space/Enter、Tooltip、读屏器名称。实际读屏器播报继续按已批准决定跳过，不改写为通过。

- [x] **Step 3: Review scope and conditional path**

Run:

```powershell
git diff main -- Services Models Localization
git diff main -- Controls Views ViewModels Tests README.md docs
```

Expected: 第一条命令仅在执行 Task 3 时允许 `Services/AnalyticsService.cs` 的 week grouping 差异；不得出现统计、schema 或本地化差异。文档必须明确记录 Tasks 3–4 是执行还是因 Task 2 达标而跳过。

- [x] **Step 4: Update evidence without deleting historical values**

`CLIENT_ACCEPTANCE_1.1.0` 保留历史 707.6、808.7、1,581.1、1,914.0 ms 样本并新增最终五轮；README 将“可能出现刷新停顿”改为最终实现与预算状态；`IMPLEMENTATION_STATUS` 记录轻量 Button 是否单独达标以及是否启用周列虚拟化。

- [x] **Step 5: Commit final UI performance evidence**

```powershell
git add README.md docs\CLIENT_ACCEPTANCE_1.1.0.md docs\IMPLEMENTATION_STATUS.md Tests\Program.cs
git commit -m "docs: record calendar heatmap performance closure"
```

## Delivery Gates

- 两个 Release build 0 warning / 0 error；完整回归输出 `All Playtime Insights tests passed.`。
- 一年 371 格：五轮 max `<= 200 ms`。
- All Sessions 1,820 格：五轮 max `<= 300 ms`。
- 若启用虚拟化：初始/滚动样本中最大 realized Button `< 420`，但 ViewModel 日期总数仍为 1,820。
- 100k analytics `<= 750 ms`；schema 4 `<= 1400 ms`。
- 四主题、五 DPI、zh_CN/en_US、Tab、Space/Enter、焦点、Tooltip、Automation Name 和轴同步通过。
- 不改变 Calendar 阈值、Week×Hour 色相/相对刻度、Dashboard 双栏架构或会话统计语义。

## Rejected Alternatives

- **提高或删除 UI 预算：** 只会掩盖 1,820 个完整 Button 的线性成本。
- **纯 Canvas 单元素绘制：** 会失去现有 Button、命令、焦点和自动化元素语义，恢复无障碍的成本高于收益。
- **按年截断或分页 All Sessions：** 改变用户看到的时间范围和滚动语义，不属于纯性能优化。
- **缩小到 14×14 DIP：** 已被视觉验收明确否决，并会重新造成坐标轴错位。

## Execution Handoff

先执行 Task 1–2 并在 Task 2 Step 5 做硬分支判断。只有实际五轮结果超过 200/300 ms 才执行 Tasks 3–4；执行者不得凭主观判断预先跳过测量，也不得为避免虚拟化而放宽目标。完成本计划前，先确保 Dashboard Analytics Performance Optimization 计划已经合并或在不同工作树中独立执行，避免两个实现者同时修改 `Tests/Program.cs`。
