# Dashboard Visual Elevation and Context-Anchored Drilldown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有 Dashboard 视觉重构实现上，完成按触发来源就近展开的下钻、趋势图微质感、带绝对时长语义的日历热力图，以及排行榜短列表与时长占比背景精修，同时保持既有 8 卡指标布局、统计口径、刷新边界、虚拟化和性能预算。

**Architecture:** 本计划是 `codex/dashboard-visual-refactor` 分支的增量计划，不重写现有 Dashboard 架构。布局继续由 `AdaptiveDashboardPanel` 负责宽窄切换和双栏独立累加；下钻卡片抽为一个共享模板，由趋势模块后的 `TrendDrilldownHost` 与分布模块后的 `DistributionDrilldownHost` 按触发来源二选一承载，不再固定到 Secondary；热力图语义在 `AnalyticsService` 生成并经 `DashboardSnapshot` 投影到 `DashboardDistributionViewModel`；视觉资源集中在 `PlaytimeInsightsVisualResources.xaml`，`AdaptiveTrendChart` 只解析命名资源并执行绘制。

**Tech Stack:** C# 7.3、.NET Framework 4.6.2、WPF、Playnite SDK、MVVM、`DrawingContext`、自定义 `Panel`、现有控制台回归套件 `Tests/Program.cs`。

## Global Constraints

- 实施起点是 `codex/dashboard-visual-refactor` 分支提交 `b9e06a2` 或其后继提交。截至 2026-08-22，`git rev-list --left-right --count main...codex/dashboard-visual-refactor` 为 `0 24`，merge-base 就是 `main` 的 HEAD `4b4be9a`，即该分支是 `main` 的严格超集，回合 `main` 是快进合并，不存在冲突风险。
- 不要在 `main` 工作目录直接套用本计划。`main` 上尚不存在 `Controls/AdaptiveDashboardPanel.cs`、`Controls/HoverMotion.cs`、`Controls/HeatmapMonthAxisPanel.cs` 和整个 `Resources/` 目录；这些文件缺失是分支差异，不是实现缺陷。实施工作树为 `.worktrees/dashboard-visual-refactor`。
- 本计划只覆盖 Dashboard 视觉增强，不改变会话存储 schema、导入导出格式、日期范围口径、排行榜排序口径或选择性刷新策略。
- Dashboard 宽屏进入阈值保持 1200 DIP，退出阈值保持 1160 DIP，栏间距保持 18 DIP，Secondary 比例保持 0.38。
- 上述阈值是 `AdaptiveDashboardPanel` 自身的可用宽度（下称“内容宽度”），不是窗口或 UserControl 宽度。Dashboard 根容器是 `StackPanel Margin="24,22,24,24"`，因此内容宽度 = 视图宽度 − 48（垂直滚动条可见时还要再减其宽度）。所有宽度断言、验收矩阵和截图记录必须显式区分这两者。
- 滞回判定语义固定为 `Controls/AdaptiveDashboardPanel.cs` 中的 `IsWideLayout ? width >= ExitWideWidth : width >= EnterWideWidth`。对应结论：内容宽度 1199 为单栏，1200 进入双栏，1160 和 1180 保持双栏，1159 退出双栏。任何文档、矩阵或测试都不得写成“1160 退出双栏”。
- 日历热力图保留在 Primary 栏；不得移动到 Secondary。
- 下钻不固定进入 Secondary：趋势点触发时显示紧随 `TrendModule` 的 `TrendDrilldownHost`，日历热力格触发时显示紧随 `DistributionModule` 的 `DistributionDrilldownHost`；两个宿主都属于 Primary，任一时刻最多一个可见。
- “双栏高度完美对称”不作为实现或测试目标；不得通过拉伸卡片、伪造最小高度或插入空白占位强制对齐底边。
- 下钻列表继续使用 `ListView`、`VirtualizingStackPanel`、`CanContentScroll="True"` 和 `VirtualizationMode="Recycling"`，单页继续加载 100 条。
- KPI 数量固定为 8，并继续共用当前单一 `ResponsiveUniformPanel`；宽屏保持紧凑的 4×2，1000×900 窗口下允许稳定重排为 3–3–2。Task 5 已于 2026-08-28 跳过，不得重新引入 2 Hero + 6 Tier 2 拆分。
- 指标卡继续使用现有主数值层级。不得新增会话次数环比/同比统计，现有比较 Pills 仍只表示已有时长比较数据。
- 热力图颜色改为绝对时长分档：0、低于 1 小时、1–3 小时、超过 3 小时；同一天的颜色不得因查询范围最大值变化而改变。
- 热力图列节拍固定为 26 DIP，交互容器固定为 26×26 DIP，可见色块固定为 24×24 DIP；不得恢复为 14×14 DIP 小色块。周次文字在列内水平居中，星期文字在 26 DIP 行容器中垂直居中，确保坐标轴文字与格子对齐。
- 热力图月份和周次必须与热力格共享横向滚动坐标；不得在 ScrollViewer 外使用独立宽度估算。
- 日历热力图在本轮冻结为“周一为周首列”。星期坐标轴的 `AlternationIndex` 隐藏规则（隐藏 1、3、5 对应周二、周四、周六）依赖这一前提；若未来需要支持 `UseIsoWeekStart=false`，隐藏索引必须由 `firstDayOfWeek` 推导，不得继续硬编码。
- 热力格宿主是非虚拟化的 `ItemsControl` + `UniformGrid`，每格是完整 `Button`（含 ControlTemplate、四档 DataTrigger、Hover Trigger、ToolTip 和 AutomationProperties）。一年范围约 371 格，All Sessions 跨多年可达 1800 格以上，且每次刷新全量实例化。因此热力图必须有 UI 侧布局预算，见 Gate D；数据侧 750 ms / 1400 ms 预算不能代替它。
- 趋势面积填充复用一层现有 Area Geometry，不得叠加第二层面积 Geometry。
- 所有新增颜色通过命名资源提供；除资源字典中的语义色外，不在 XAML 模板和 `AdaptiveTrendChart.OnRender` 中散落硬编码色值。
- 新增 Style 必须与其 `BasedOn` 基样式处于同一资源查找域。`MetricCardStyle`（`Views/PlaytimeInsightsDashboardView.xaml:46`）、`MetricValueStyle`（`:272`）和 `MetricHelperTextStyle`（`:279`）目前定义在 View 的 `UserControl.Resources` 中，被合并的共享字典无法通过 `StaticResource` 反向引用它们；在共享字典里写 `BasedOn="{StaticResource MetricValueStyle}"` 会在解析期失败。
- 语义画刷必须能在 Playnite Light 主题下保持可分辨。凡是当前实现已经使用主题动态资源的地方（例如 Trend Hover 节点外圈用的 `ControlBackgroundBrush`），不得为了“资源统一”换成固定浅色常量——那与固定 `Brushes.White` 是同一个缺陷，只是换了载体。
- 所有出现和悬停动效遵守 `SystemParameters.ClientAreaAnimation`；减弱动效时立即进入最终状态。
- 所有新增可见文本同时加入 `Localization/en_US.xaml` 和 `Localization/zh_CN.xaml`，键集合与格式参数保持一致。
- 不引入第三方 UI 或图表库，不修改 `PlaytimeInsights.cs`，不访问 Playnite 私有视觉树 API。
- Release 构建保持 0 warning、0 error；完整回归保持通过；10 万会话分析不高于 750 ms，schema 4 加载不高于 1400 ms。
- 本轮验收证据写入新建的 `docs/CLIENT_ACCEPTANCE_1.1.0.md`，不得追加到 `docs/CLIENT_ACCEPTANCE_1.0.0.md`。`extension.yaml` 的 `Version` 已是 1.0.0，且 `main` 的 `4b4be9a` 已把该文件作为 1.0.0 的发布记录；向其追加未发布的视觉验收等于追溯改写发布记录。

---

## Frozen Product Decisions

1. **Drilldown 按触发来源就近展开，不进入固定 Secondary 位置。** 趋势点和日历热力格都能触发会话明细；固定放到累计时长排行榜下方既会产生错误的语义归属，也不能保证结果进入视口。趋势下钻紧随 Trend，日期下钻紧随 Distribution。
2. **采用确定性 Zone，不采用自动 Masonry。** 模块不会因为内容高度变化在左右栏之间跳动，键盘阅读顺序和窄屏源码顺序保持稳定。
3. **不实现强制等高。** 页面高度继续取两栏最大值；下钻使用有限高度和内部滚动吸收动态内容，不通过伸展 Ranking、Trend 或 Distribution 制造对称。
4. **热力图 Legend 使用固定时长语义。** 既然 Legend 显示 `0h / <1h / 1–3h / >3h`，数据层必须输出离散等级，不能继续使用“除以当前最大值”的相对强度。
5. **24 DIP 是视觉色块，26 DIP 是列节拍与点击目标。** 外层使用 26×26 DIP 可聚焦 Button，内部居中放置 24×24 DIP 色块，并保留 Tooltip 和自动化名称；不得使用 14×14 DIP 小色块。
6. **KPI 保留单一响应式 Panel。** Task 5 的 Hero/Tier 2 拆分已跳过；不新增 ColumnSpan、复杂断点 DSL 或第二个指标面板。
7. **排行榜保留历史整行时长占比背景。** 进度层统一使用 `RankingEnergyBrush` 蓝色和 `Opacity="0.10"`，在排行项内容下方按 `ProgressPercent` 填充整行高度；Track 透明且不可见，Indicator 不使用渐变或固定高度。第一至第三名的金、银、铜 Glow、徽章和文字仍是独立层，不给进度层增加 `Position` Trigger，也不把进度色本身改成金、银、铜。底部 4 DIP 能量条是已撤销的中间方案，不得恢复。
8. **区间榜与累计榜的“最近游玩”统一为本地时间。** 区间榜使用会话自带的 `StartUtcOffsetMinutes`，累计榜的 Playnite `LastActivity` 先由 UTC 转本地，再交给同一个 formatter。两个 Tab 的“今天 / 昨天”必须是同一口径。
9. **ShareText 使用区间专用文案，不复用累计文案。** 区间榜占比的分母是本期总时长，累计榜占比的分母是 Playnite 累计总时长，二者不能共用同一个本地化 key。
10. **整行背景填充长度恒为时长占比，属于已知且刻意保留的不一致。** `ProgressPercent` 与当前选中的排行指标无关；切到“会话次数”或“活跃天数”排序时，背景长度与主数值不对应。低透明满铺背景让这种差异较克制，但 Tooltip 仍必须讲清分母，并在计划中登记为已知行为，而不是当成缺陷临时改口径。
11. **排行项的常显信息只保留不重复的高频事实。** `PrimaryValueText` 始终显示当前排序指标；`DetailText` 最多显示会话次数和活跃日，并排除当前排序指标。时长由主数值和整行背景表达，平均/最长移入统一行级 Tooltip；最近游玩继续常显，但不再给自身套一份相同 Tooltip。

## Current Implementation State (2026-08-30 Final Reconciliation)

本节是当前事实入口，优先于下方各 Task 保留的历史 RED 假设和提交命令。

- Task 0–7 的已实施范围均已提交并推送到 `origin/codex/dashboard-visual-refactor`；生产实现的最终提交为 `e59f9bf`。本次状态更新前，本地分支与远端一致且工作树干净；本次仅文档状态更新尚未提交。
- 最终提交映射：Task 0 为 `71a599d`；Task 1 为 `cda8081`；Task 2 与 Task 2.5 为 `0180a81`；Task 3 与 Task 3.5 为 `108a4c4`；Task 4 与 Task 4.5 为 `301708c`；Task 6 为 `a0983ee`；Task 7 及最终比较胶囊修复为 `e59f9bf`。
- Task 5 已于 2026-08-28 明确跳过，未实施 Hero/Tier 2 拆分；其未勾选 Steps 是已否决方案的历史记录，不是当前待办。
- Task 1 Step 2 和 Task 2 Step 2 的 RED 期望在 2026-08-22 对账时已不可复现，因此按“确认实现存在且回归为 GREEN”完成；没有为制造 RED 回退已有实现。
- Task 7 的精确内容宽度、完整数据状态、全主题、zh_CN / en_US、全 DPI、Calendar 键盘链路、Calendar/Week×Hour 视觉区分和跨零点相对日期已于 2026-09-03 完成人工核验；实际读屏器播报由用户决定跳过，且不得据此声称具备原生 Polite live-region 语义。视觉基线人工矩阵仅剩减弱动效。All Sessions 1,820 格 UI Measure + Arrange 的 707.6 ms 后续项已由 `codex/dashboard-performance-optimization` 收敛并于 2026-09-04 合并回 `main`；性能版仍待虚拟化后实机复验。
- 2026-08-30 最终部署的严格 9 个文件与当前 Release 源产物一致；DLL SHA-256 为 `6D79971D2E50B6EA701AFAAC581FCB9BB5B0FDFABB988532776E303C10C55937`。详细证据见 `docs/CLIENT_ACCEPTANCE_1.1.0.md`。

## Relationship to the 2026-08-14 Plan

- `2026-08-14-dashboard-visual-refactor-implementation.md` 仍是基础重构计划，本文件不重复日期预设、筛选器、共享资源字典、封面缓存或 Dashboard 子 ViewModel 拆分任务。
- 本文件以基础计划已经在 `codex/dashboard-visual-refactor` 落地为前提。
- 本文件覆盖并取代基础计划中与以下内容有关的最终呈现约束：Primary 内单一固定位置的 Drilldown、Heatmap 连续相对透明度、Ranking 全高 Energy 背景，以及 Trend 节点仅有单层实心圆。单个 8 卡 `ResponsiveUniformPanel` 继续保留。
- 若两个计划发生冲突，以本文件的 `Global Constraints` 和 `Frozen Product Decisions` 为准；未冲突部分继续遵循 2026-08-14 计划。

## File Map

`Action` 列已按 2026-08-30 最终状态更新；这些文件均已提交，表格表示本计划实际交付范围，不再表示待执行动作。

| File | Action | Responsibility |
| --- | --- | --- |
| `ViewModels/Dashboard/HeatmapCellViewModel.cs` | Modified (committed) | 绝对强度枚举和单元格等级；Task 2.5 删除残留 `HeatOpacity` |
| `ViewModels/Dashboard/HeatmapMonthLabelViewModel.cs` | Created (committed) | 月份标签的列起点、跨度和文本 |
| `Services/HeatmapIntensityScale.cs` | Created (committed) | 将秒数稳定映射到四档 Heatmap 强度 |
| `ViewModels/Dashboard/DashboardSnapshot.cs` | Modified (committed) | 快照携带月份标签和周次标签 |
| `ViewModels/Dashboard/DashboardDistributionViewModel.cs` | Modified (committed) | 原子发布热力图轴数据 |
| `ViewModels/DashboardViewModel.cs` | Modified (committed) | 代理热力图轴集合 |
| `Services/AnalyticsService.cs` | Modified (committed) | 绝对强度、月份跨度、周次、最近游玩投影和区间占比文案 |
| `Services/RecentActivityFormatter.cs` | Created (committed) | 可测试地格式化今天、昨天、较早日期和未知活动 |
| `Services/DashboardAnalysisContext.cs` | Modified (committed) | 区间排行榜统计携带最近会话本地时间 |
| `ViewModels/Dashboard/GameRankingViewModel.cs` | Modified (committed) | 显式占比、最近游玩、短列表密度 |
| `Controls/HeatmapMonthAxisPanel.cs` | Created (committed) | 按周列起点与跨度排列月份标签 |
| `Controls/AdaptiveTrendChart.cs` | Modified (committed) | 主题化 Area、Line、Node 和 Hover 绘制资源；新增 `ResolveBrush(string, Brush)` 重载 |
| `Resources/PlaytimeInsightsVisualResources.xaml` | Modified (committed) | 趋势、热力图与 Ranking Energy 语义资源 |
| `ViewModels/Dashboard/DashboardDrilldownViewModel.cs` | Modified (committed) | 记录 Trend/Distribution 下钻锚点并驱动两个就近宿主二选一显示 |
| `Views/PlaytimeInsightsDashboardView.xaml` | Modified (committed) | 热力图轴/Legend、共享下钻模板、两个上下文宿主和排行榜模板 |
| `Views/PlaytimeInsightsDashboardView.xaml.cs` | Modified (committed) | 仅在活动下钻标题区不在视口时执行最小滚动，不新增下钻出现动画 |
| `Localization/en_US.xaml` | Modified (committed) | 热力图、最近游玩和 Legend 英文文本 |
| `Localization/zh_CN.xaml` | Modified (committed) | 热力图、最近游玩和 Legend 中文文本 |
| `Tests/Program.cs` | Modified (committed) | 数据语义、Panel、XAML 契约、动效与性能回归 |
| `docs/CLIENT_ACCEPTANCE_1.1.0.md` | Created (committed) | 本轮视觉增强的截图矩阵和验收记录 |
| `docs/CLIENT_ACCEPTANCE_1.0.0.md` | Restored (committed) | 还原为 `main` 上的 1.0.0 发布记录，移出本轮追加内容 |

`Controls/AdaptiveDashboardPanel.cs` 和 `Controls/ResponsiveUniformPanel.cs` 不在预期修改范围内。Task 6 只新增两个普通 Primary 子项并通过 Collapsed/Visible 切换活动宿主；若实现发现必须修改 Panel，应暂停并先证明现有源顺序和 Zone 无法满足本计划。

---

### Task 0: Reconcile the Partially Implemented Baseline

> **状态（2026-08-30）：已完成并提交（`71a599d`）。** Task 0–2 对账、1.1.0 验收契约拆分和提交编排均已完成；下方 RED/命令保留为历史执行记录。

**Files:**
- Create: `docs/CLIENT_ACCEPTANCE_1.1.0.md`
- Restore: `docs/CLIENT_ACCEPTANCE_1.0.0.md`
- Verify: `docs/superpowers/reviews/2026-08-18-dashboard-task-0-2-acceptance-review.md`
- Verify: `docs/superpowers/plans/2026-08-14-dashboard-visual-refactor-implementation.md`
- Verify: `Views/PlaytimeInsightsDashboardView.xaml`
- Verify: `Controls/AdaptiveTrendChart.cs`

**Interfaces:**
- Consumes: 已实现的 `AdaptiveDashboardPanel`、`HeatmapMonthAxisPanel`、`HeatmapIntensityScale`、四档绝对强度 Heatmap、8 张等尺寸 KPI、Primary 栏卡片式 Drilldown
- Produces: 与真实工作树一致的基线记录，以及独立于 1.0.0 发布记录的 1.1.0 增量验收章节

- [x] **Step 1: Inventory the uncommitted Task 0–2 work**

Run:

```powershell
git -C .worktrees\dashboard-visual-refactor status --short --branch
git -C .worktrees\dashboard-visual-refactor log -1 --oneline --decorate
git -C .worktrees\dashboard-visual-refactor diff --stat
```

Expected: 分支为 `codex/dashboard-visual-refactor`，HEAD 为 `b9e06a2` 或后继提交；工作树存在未提交的 Task 1 / Task 2 产物，至少包含 `Controls/HeatmapMonthAxisPanel.cs`、`Services/HeatmapIntensityScale.cs` 和 `ViewModels/Dashboard/HeatmapMonthLabelViewModel.cs` 三个新文件。

把每个改动文件归类为“Task 1 产物”、“Task 2 产物”或“计划外改动”，并把归类结果记录下来。它们是本计划自身的交付物，不适用“只记录并避开用户改动”；同时禁止 `git reset`、`git checkout --` 和 `git clean`。

- [x] **Step 2: Create the 1.1.0 acceptance record and restore the 1.0.0 record**

先还原发布记录：若工作树已向 `docs/CLIENT_ACCEPTANCE_1.0.0.md` 追加过本轮内容（对账时约 41 行），把这部分内容迁出，使该文件回到 `main` 上 `4b4be9a` 的状态。

Run:

```powershell
git -C .worktrees\dashboard-visual-refactor diff main -- docs/CLIENT_ACCEPTANCE_1.0.0.md
```

新建 `docs/CLIENT_ACCEPTANCE_1.1.0.md`：

```markdown
# Client Acceptance 1.1.0 — Dashboard Visual Elevation

## Frozen Layout Contract

- KPI inventory: 8 cards in one responsive panel
- Calendar heatmap: Primary column
- Drilldown: context-anchored in Primary; Trend selection after Trend, Calendar selection after Distribution
- Heatmap column pitch: 26 DIP
- Heatmap visual cell: 24x24 DIP
- Heatmap hit target: 26x26 DIP
- Heatmap axes: week numbers horizontally centered; weekday labels vertically centered in 26 DIP rows
- Heatmap levels: 0h / <1h / 1-3h / >3h
- Heatmap first column: Monday
- Wide layout thresholds are panel content widths: enter at >= 1200, stay wide at >= 1160, exit below 1160
- Content width = view width - 48 (root StackPanel margin), minus the vertical scrollbar when visible

### Frozen Composition
- [ ] Wide: Trend/Distribution in Primary
- [ ] Wide: Ranking/Anomaly in Secondary; active Drilldown host remains adjacent to its Primary trigger module
- [ ] Narrow source order: Trend, TrendDrilldownHost, Ranking, Distribution, DistributionDrilldownHost, Anomaly（inactive host is Collapsed）
- [ ] Calendar heatmap stays in Primary

### Interaction States
- [ ] No drilldown selection
- [ ] Trend-anchored drilldown
- [ ] Distribution-anchored drilldown
- [ ] Drilldown with 0 exact sessions
- [ ] Drilldown with 1 session
- [ ] Drilldown with 100 visible sessions and more available
- [ ] Reduced motion enabled

### Visual Matrix
- [ ] Content widths: 640, 900, 1159, 1160, 1199, 1200, 1440, 1600 DIP (record the view width used for each)
- [ ] Themes: Default Dark, Default Light, Seaside Dark, Windows High Contrast
- [ ] DPI: 100%, 125%, 150%, 175%, 200%
- [ ] Languages: zh_CN, en_US
- [ ] Ranges: one month, range crossing a month boundary, six-calendar-week month, one year, all sessions
- [ ] Ranking counts: 0, 1, 2, 3, 10
- [ ] Drilldown rows: 0, 1, 100, 250
- [ ] Keyboard: Tab, Space/Enter, focus outline, net462-compatible UI Automation name-change notification
- [ ] Heatmap layout timing measured for one year and all sessions
```

以上代码块是 2026-08-22 创建验收文档时使用的历史模板，不代表当前执行状态；实际勾选和未完成项只以 `docs/CLIENT_ACCEPTANCE_1.1.0.md` 为准。KPI 必须记录为单一响应式面板中的 8 张卡；Task 5 已跳过，不再记录 Hero/Tier 2 验收项。

- [x] **Step 3: Run the current worktree state as the working baseline**

Run:

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: 两次构建均为 0 warning、0 error；测试输出 `All Playtime Insights tests passed.`。

记录时必须写明这是“含未提交 Task 1/Task 2 实现的当前基线”，而不是“未改动的分支基线”。真正的 `b9e06a2` 基线已无法在不丢弃实现的前提下复现，不要为了取得它而回退工作树。

- [x] **Step 4: Plan the commits, do not commit unilaterally**

Task 0–2 的实现应拆成三个提交，而不是一个：

```text
docs: freeze dashboard visual elevation contract
  docs/CLIENT_ACCEPTANCE_1.1.0.md, docs/CLIENT_ACCEPTANCE_1.0.0.md

feat: add absolute calendar heatmap semantics
  ViewModels/Dashboard/HeatmapCellViewModel.cs
  ViewModels/Dashboard/HeatmapMonthLabelViewModel.cs
  ViewModels/Dashboard/DashboardSnapshot.cs
  Services/HeatmapIntensityScale.cs
  Services/AnalyticsService.cs
  Tests/Program.cs

feat: refine calendar heatmap navigation and legend
  Controls/HeatmapMonthAxisPanel.cs
  ViewModels/Dashboard/DashboardDistributionViewModel.cs
  ViewModels/DashboardViewModel.cs
  Resources/PlaytimeInsightsVisualResources.xaml
  Views/PlaytimeInsightsDashboardView.xaml
  Views/PlaytimeInsightsDashboardView.xaml.cs
  Localization/en_US.xaml
  Localization/zh_CN.xaml
  Tests/Program.cs
```

原计划这一步只 `git add docs/CLIENT_ACCEPTANCE_1.0.0.md`，会把 Task 1/Task 2 的代码继续留在未提交状态——这正是当前状态的成因，不要重复。

提交已按用户指示完成：`71a599d`、`cda8081`、`0180a81` 分别对应文档契约、绝对日历语义、日历导航与 Task 2.5 收口。

---

### Task 1: Replace Relative Heat Intensity with Absolute Duration Levels

> **状态（2026-08-30）：已实现并提交（`cda8081`）。** Step 2 的 RED 期望在对账时已不可复现，按验证完成；剩余缺口已由 Task 2.5 收口。

**Files:**
- Modify: `ViewModels/Dashboard/HeatmapCellViewModel.cs`
- Create: `ViewModels/Dashboard/HeatmapMonthLabelViewModel.cs`
- Create: `Services/HeatmapIntensityScale.cs`
- Modify: `ViewModels/Dashboard/DashboardSnapshot.cs`
- Modify: `Services/AnalyticsService.cs`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `HeatmapIntensityLevel { None, Low, Medium, High }`
- Produces: `HeatmapIntensityScale.FromSeconds(ulong seconds)`
- Produces: `HeatmapCellViewModel.IntensityLevel`
- Produces: `HeatmapMonthLabelViewModel(string label, int columnIndex, int columnSpan)`
- Produces: `DashboardSnapshot.HeatmapMonthLabels` and `DashboardSnapshot.HeatmapWeekLabels`
- Preserves: `HeatmapCellViewModel.Date`、`Seconds`、`CellVisibility`、`TooltipText`

- [x] **Step 1: Register failing heatmap semantic tests**

在 `Main()` 注册：

```csharp
Run("Heatmap uses absolute duration levels", TestHeatmapAbsoluteDurationLevels);
Run("Heatmap month axis follows calendar-week columns", TestHeatmapMonthAxisProjection);
Run("Heatmap supports six-calendar-week months", TestHeatmapSixWeekMonth);
```

新增等级断言，使用包含 0 秒、3599 秒、3600 秒、10800 秒和 10801 秒的固定日期数据：

```csharp
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
```

月份轴测试至少覆盖 2026 年 8 月。该月从周一制日历的 2026-07-27 周开始，到 2026-08-31 周结束，必须允许 6 个周列，不能把文案需求中的“第1周～第5周”误写成固定五列。

- [x] **Step 2: Verify the tests exist and are GREEN**

Run:

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: `HeatmapIntensityLevel`、`HeatmapIntensityScale` 和轴投影属性均已存在，上述三项测试通过。

不要为了制造 RED 而删除或回退已有实现。如果某一项测试其实不存在，只补该项，并按原始 RED-GREEN 顺序处理它。

- [x] **Step 3: Add the heatmap contracts**

在 `HeatmapCellViewModel.cs` 增加：

```csharp
public enum HeatmapIntensityLevel
{
    None,
    Low,
    Medium,
    High
}

public HeatmapIntensityLevel IntensityLevel { get; set; }
```

`HeatOpacity` 的处置分两步，不要求在 Task 1 内做到单一来源——Task 1 的提交范围不含 `Views/`，所以“迁移完成后再删”和“同一提交不得双源”在 Task 1 内无法同时满足，原文这两句是自相矛盾的：

- Task 1：保留 `HeatmapCellViewModel.HeatOpacity`，允许其与 `IntensityLevel` 短暂共存；此时 XAML 尚未迁移，删除会破坏渲染。
- Task 2.5：`Views/` 已只消费 `IntensityLevel` 后，删除 `HeatmapCellViewModel.HeatOpacity` 及 `CreateHeatmapProjection` 中的赋值，并加源码块护栏。
- 两步都不得删除 `AdvancedAnalyticsService` 中 `WeekHourCellViewModel.HeatOpacity` 的连续强度计算——那是周×小时矩阵的正当用法。

创建 `HeatmapMonthLabelViewModel.cs`：

```csharp
namespace PlaytimeInsights.ViewModels
{
    public sealed class HeatmapMonthLabelViewModel
    {
        public string Label { get; set; }
        public int ColumnIndex { get; set; }
        public int ColumnSpan { get; set; }
    }
}
```

在 `DashboardSnapshot` 增加：

```csharp
public IList<HeatmapMonthLabelViewModel> HeatmapMonthLabels { get; set; }
public IList<string> HeatmapWeekLabels { get; set; }
```

- [x] **Step 4: Implement deterministic intensity resolution**

创建 `Services/HeatmapIntensityScale.cs`：

```csharp
public static class HeatmapIntensityScale
{
    public static HeatmapIntensityLevel FromSeconds(ulong seconds)
    {
        if (seconds == 0)
        {
            return HeatmapIntensityLevel.None;
        }

        if (seconds < 3600)
        {
            return HeatmapIntensityLevel.Low;
        }

        if (seconds <= 10800)
        {
            return HeatmapIntensityLevel.Medium;
        }

        return HeatmapIntensityLevel.High;
    }
}
```

在 `CreateHeatmapCells` 中通过 `HeatmapIntensityScale.FromSeconds(seconds)` 设置 `IntensityLevel`，并移除对 `maximumSeconds` 的颜色计算依赖。

- [x] **Step 5: Project month spans and week labels**

增加一个私有结果类型或两个 `out` 参数，使热力图创建阶段同时返回：

- 每个月份标签的起始周列；
- 到下一个月份标签前的跨度；
- 每个周列在所属月份中的短周次 `1`、`2`……；
- 周列包含下个月 1 日时，该列归属新月份；
- 范围前后的隐藏日期不产生额外可见月份标签。

推荐接口：

```csharp
private static HeatmapProjection CreateHeatmapProjection(
    IDictionary<DateTime, ulong> dailySeconds,
    AnalyticsDateRange range,
    DayOfWeek firstDayOfWeek)
```

`HeatmapProjection` 包含 `Cells`、`ColumnCount`、`MonthLabels`、`WeekLabels`，避免继续增加 `out` 参数。

- [x] **Step 6: Run the heatmap data tests**

Run:

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected: 新增三项 Heatmap 测试通过；既有 `Heatmap aligns ISO week and scales intensity` 测试应改名并更新为绝对等级断言，不得继续检查相对最大值缩放。

- [x] **Step 7: Commit the heatmap semantic model**

提交范围（时机由用户决定，见 Task 0 Step 4）：

```powershell
git add ViewModels/Dashboard/HeatmapCellViewModel.cs ViewModels/Dashboard/HeatmapMonthLabelViewModel.cs ViewModels/Dashboard/DashboardSnapshot.cs Services/HeatmapIntensityScale.cs Services/AnalyticsService.cs Tests/Program.cs
git commit -m "feat: add absolute calendar heatmap semantics"
```

---

### Task 2: Build the Aligned Calendar Axis, Legend, and Keyboard Cells

> **状态（2026-08-30）：已实现并提交（`0180a81`）。** `HeatmapMonthAxisPanel`、26/24 DIP Button 结构、`SelectHeatmapDateCommand`、四档 Legend 和横向共享滚动均已落地，`HeatmapCell_MouseLeftButtonUp` 已删除；Task 2.5 的结构护栏也已随本提交收口。

**Files:**
- Create: `Controls/HeatmapMonthAxisPanel.cs`
- Modify: `ViewModels/Dashboard/DashboardDistributionViewModel.cs`
- Modify: `ViewModels/DashboardViewModel.cs`
- Modify: `Resources/PlaytimeInsightsVisualResources.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml.cs`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Consumes: `HeatmapMonthLabels`、`HeatmapWeekLabels`、`HeatmapColumnCount`、`IntensityLevel`
- Produces: `HeatmapMonthAxisPanel.ColumnCount` and `ColumnPitch`
- Produces attached properties: `HeatmapMonthAxisPanel.ColumnIndex` and `ColumnSpan`
- Replaces: mouse-only `HeatmapCell_MouseLeftButtonUp` with `SelectHeatmapDateCommand` binding

- [x] **Step 1: Register failing Panel and XAML contract tests**

注册：

```csharp
Run("Heatmap month axis aligns weekly columns", TestHeatmapMonthAxisPanel);
Run("Calendar heatmap keeps keyboard command and legend contract", TestCalendarHeatmapVisualContract);
```

Panel 测试创建三个标签：

```csharp
var panel = new HeatmapMonthAxisPanel
{
    ColumnCount = 8,
    ColumnPitch = 26
};
panel.Children.Add(CreateMonthAxisChild(0, 2));
panel.Children.Add(CreateMonthAxisChild(2, 4));
panel.Children.Add(CreateMonthAxisChild(6, 2));
panel.Measure(new Size(double.PositiveInfinity, 20));
panel.Arrange(new Rect(0, 0, 208, 20));

Equal(0d, GetLayoutSlot(panel.Children[0]).X);
Equal(52d, GetLayoutSlot(panel.Children[0]).Width);
Equal(52d, GetLayoutSlot(panel.Children[1]).X);
Equal(104d, GetLayoutSlot(panel.Children[1]).Width);
```

静态 XAML 测试必须确认：存在月份轴、周次轴、四档 Legend、26 DIP Button、24 DIP 内部色块、Command/CommandParameter；周次文字水平居中，星期文字在 26 DIP 行容器中垂直居中；并且不再存在 `MouseLeftButtonUp="HeatmapCell_MouseLeftButtonUp"`。

- [x] **Step 2: Verify the Panel and XAML contracts are GREEN**

Run 完整测试。Expected: `HeatmapMonthAxisPanel` 已存在，XAML 契约测试通过。不要为了制造 RED 而回退控件或 XAML。

- [x] **Step 3: Implement HeatmapMonthAxisPanel**

新控件职责仅限月份标签布局：

```csharp
public sealed class HeatmapMonthAxisPanel : Panel
{
    public static readonly DependencyProperty ColumnCountProperty;
    public static readonly DependencyProperty ColumnPitchProperty;

    public int ColumnCount
    {
        get => (int)GetValue(ColumnCountProperty);
        set => SetValue(ColumnCountProperty, value);
    }

    public double ColumnPitch
    {
        get => (double)GetValue(ColumnPitchProperty);
        set => SetValue(ColumnPitchProperty, value);
    }

    public static int GetColumnIndex(DependencyObject element);
    public static void SetColumnIndex(DependencyObject element, int value);
    public static int GetColumnSpan(DependencyObject element);
    public static void SetColumnSpan(DependencyObject element, int value);
}
```

`ColumnCountProperty` 默认值为 1，`ColumnPitchProperty` 默认值为 26d，两者 metadata 均包含 `AffectsMeasure | AffectsArrange`。`MeasureOverride` 使用 `ColumnCount * ColumnPitch` 作为宽度，每个可见子元素测量宽度为 `max(1, ColumnSpan) * ColumnPitch`；`ArrangeOverride` 使用 `ColumnIndex * ColumnPitch` 定位。非法列索引钳制到 0，跨度钳制到剩余列数；Collapsed 子元素排列到空矩形。

- [x] **Step 4: Publish axis collections atomically**

在 `DashboardDistributionViewModel` 增加只读集合属性，并在 `Apply(DashboardSnapshot)` 中与 `HeatmapCells` 同一批次替换：

```csharp
public IReadOnlyList<HeatmapMonthLabelViewModel> HeatmapMonthLabels { get; private set; }
public IReadOnlyList<string> HeatmapWeekLabels { get; private set; }
```

在根 `DashboardViewModel` 增加代理属性。更新原子发布测试，确认旧集合引用被一次替换且每个属性只通知一次。

- [x] **Step 5: Add heatmap semantic brushes**

热力图四档是**单一冷色家族内的 ordinal ramp**，不是四个互不相关的颜色。原实现（`b9e06a2` 的 `HeatmapActiveBrush`）是一条 `#FF2457D6 → #FFA45CFF` 的对角蓝紫渐变，所有格子共用同一渐变、只用 `HeatOpacity` 改变强度。离散四档改为已批准的冰青—青绿光泽后，等级仍主要由单调明度表达；允许受控的色相变化来形成格内对角光泽，但不得按档位跳到无关色相。验收口径以本节后文已实测通过的 19° 中点跨度为准。

**色相归属（已定）：Calendar 用冰青，Week×Hour 保留蓝紫。** `DistributionModule`（Border 起于 `Views/PlaytimeInsightsDashboardView.xaml:1298`）里同时装了两个热力图：Week×Hour 在 1399 行，Calendar 在 1486 行，中间只隔一个小标题。两者刻度**不兼容**：

| | Week×Hour | Calendar |
| --- | --- | --- |
| 刻度 | 相对：`0.16 + value/maximum * 0.84` | 绝对：固定 0 / <1h / 1–3h / >3h |
| 同色含义 | 占本期最忙那格的比例，换区间即变 | 固定时长，跨查询稳定 |
| 图例 | 无 | `少 [0h][<1h][1–3h][>3h] 多`（1499–1536 行） |

刻度无法统一——Week×Hour 每格是「某星期几的某小时」在整个区间上的累加，绝对量随区间周数线性增长，一年区间下会整片饱和，所以相对刻度对它是正确选择。既然刻度不能统一，颜色就必须区分：否则卡片里唯一那条图例（绝对档位）看起来像管着整张卡，读者会拿 `1–3h` 去读一个相对刻度的格子。

色相选择经过两轮：琥珀（`#7A5622 / #A97430 / #DE9C40`）分离度最大但暖冷对撞过硬，同一张卡里看着刺眼，已否决。**冰青是最终选择**：留在冷色家族让卡片仍读作一个整体，同时与蓝紫拉开足够距离——本 ramp 每个 stop 对 Week×Hour 的两个 stop（`#2457D6`、`#A45CFF`）都满足 ΔE ≥ 15。

**不要用海军蓝做锚点。** `#1D4ED8` 已评估并否决：它与 `#2457D6` 只有 **ΔE 2.2**（正常视觉）、1.4（色盲模拟），即与上方网格视觉同色；而且海军蓝→青的 ramp 跨 **53°** 色相，单色相校验 FAIL——比被替换的旧配色 36° 漂移更差。

Week×Hour 的 `HeatmapActiveBrush` 本轮**不动**，保持 `b9e06a2` 原样（已核对与 `b9e06a2` 逐行相同）。

在共享资源字典中：

```xml
<SolidColorBrush x:Key="HeatmapNoneBrush" Color="#14FFFFFF" />
<LinearGradientBrush x:Key="HeatmapLowBrush" StartPoint="0,1" EndPoint="1,0">
    <GradientStop Color="#FF0C5C74" Offset="0" />
    <GradientStop Color="#FF20734A" Offset="1" />
</LinearGradientBrush>
<LinearGradientBrush x:Key="HeatmapMediumBrush" StartPoint="0,1" EndPoint="1,0">
    <GradientStop Color="#FF0692B8" Offset="0" />
    <GradientStop Color="#FF1EA884" Offset="1" />
</LinearGradientBrush>
<LinearGradientBrush x:Key="HeatmapHighBrush" StartPoint="0,1" EndPoint="1,0">
    <GradientStop Color="#FF0EBAFF" Offset="0" />
    <GradientStop Color="#FF42EEC0" Offset="1" />
</LinearGradientBrush>
```

**每格光泽强度以星期热力图为标尺。** 参考值：`HeatmapActiveBrush` 两个 stop 相距 ΔE 19.1。本 ramp 每档 stop 间距为 **11.6 / 11.8 / 19.6**（Low / Medium / High），刻意随档位递增，与原实现「光泽随 `HeatOpacity` 增强」一致。

Low 和 Medium 刻意不追到 19，原因是结构性的：星期热力图是**连续 opacity 刻度、没有离散档位**，明度可以随意用于光泽；而日历的四个档位**正是靠明度编码的**，格子内部的明度跨度会直接吃掉档间可辨识度。所以 Low/Medium 的光泽主要来自色相旋转（不消耗档位信号），只加少量明度倾斜；High 有充足余量，可以做到与参考值持平。

**光泽只能往青绿方向扩，不能往蓝方向扩。** 本 ramp 中允许的最蓝 stop 约为 `#0C5F7A`（对 `#2457D6` ΔE 15.4），`#0B5F80` 已经跌到 14.4。实测记录：把低档 stop 往蓝推到 `#0A5F84` / `#075F8E` / `#045F98`，对 `#2457D6` 的 ΔE 依次降为 13.7 / 12.1 / 10.4，全部失守。因此**所有 stop 的蓝侧都必须留在这条界内**，加宽跨度一律往绿侧走。

校验依据（surface 取 `ModuleBackgroundBrush` 的等效深色 `#1B1C24`，Dashboard 模块底色是硬编码深色，不随 Playnite 主题变化）：

| 档位 | 两个 stop | 渐变中点 | stop 对比度 | 光泽 ΔE |
| --- | --- | --- | ---: | ---: |
| Low | `#0C5C74` → `#20734A` | `#16685F` | 2.26 / 2.92 | 11.6 |
| Medium | `#0692B8` → `#1EA884` | `#129D9E` | 4.69 / 5.64 | 11.8 |
| High | `#0EBAFF` → `#42EEC0` | `#28D4E0` | 7.66 / 11.49 | 19.6 |

中点序列 ordinal 校验：明度单调、相邻 ΔL 均 ≥ 0.06、亮端 2.57:1、色相跨度 19°，全部通过。天蓝侧与青绿侧两条锚点子 ramp 单独校验也通过（亮端 2.26:1 / 2.92:1），即**任一格子的任一角都不会掉出对比度底线**。

每个 stop 对 Week×Hour 两个 stop（`#2457D6`、`#A45CFF`）均满足 ΔE ≥ 15。

High 维持此前已认可的平均亮度：两个 stop 为 7.66:1 与 11.49:1，均值约 9.6:1，对比它替换掉的平色 `#22D3EE`（9.39:1）基本不变——格子开始有光泽，但整片网格没有变吵。All Sessions 下可达 1800 格以上，因此不得再提亮中档或高档。

被替换的旧配色（`#594A90E2` / `#995B7CFA` / `#D98B5CF6`）有两个实测问题，不要回退：

- Low 档中点合成后对比度仅 **1.71:1**，低于 2:1 底线，`<1h` 的格子糊进卡片底色；
- 三档跨 **36°** 色相漂移（蓝 → 靛 → 紫），四档读起来像四个类别而不是一条强度刻度。

`HeatmapNoneBrush` 刻意保持低对比（约 1.20:1）：0 档表示“没有游玩”，属于缺席而非 ramp 的一级，让它退到底色是正确的；空格子靠 1 DIP 的 `PanelSeparatorBrush` 描边读出网格位置。

已知遗留两项，本轮不改：

- Week×Hour 的低强度格子同样低于 2:1（原实现按 alpha 合成后本就如此）；若以后要修，连同它的相对刻度图例一起处理，不要顺手改成与 Calendar 同色。
- 三色视觉缺失（tritanopia）下，Calendar 低档与 Week×Hour 蓝端的距离会收窄到警戒带（前一版平色方案实测 7.8）。缓解来自非颜色通道且已经存在：两个网格的格子尺寸不同（30 vs 24 DIP）、网格形状不同（固定 7×24 vs 可横向滚动的 7×N）、只有 Calendar 有图例和键盘焦点。tritanopia 发生率约 0.01%，本轮接受该警戒带，但不得再削弱上述任何一个区分通道。

实机高对比度验证不通过时调整资源，不在 DataTemplate 内直接改色；任何调整后必须重跑 ordinal 校验，并保持以下五条不变：

1. 中点序列保持冰青—青绿冷色系、色相跨度 ≤ 20°、明度单调、相邻 ΔL ≥ 0.06；
2. 每个 stop 相对模块底色 ≥ 2:1（两条锚点子 ramp 都要单独校验，不能只看中点）；
3. 每个 stop 对 `#2457D6` 和 `#A45CFF` 的 ΔE ≥ 15，蓝侧不得超过 `#0C5F7A`；
4. 每档 stop 间距（光泽强度）随档位递增，High 不低于 ΔE 18；
5. High 两个 stop 的平均对比度不高于约 9.6:1。

焦点描边直接使用 `{DynamicResource TextBrush}`。注意一个已知风险：Dashboard 模块底色是硬编码深色，而 `TextBrush` 随 Playnite 主题变化，因此 Light 主题下焦点描边会变成深色压在深色卡片上。这属于 Task 7 Step 4 的实机核对项，若确认不可见，改动焦点描边资源而不是改动这四个档位画刷。

- [x] **Step 6: Recompose the calendar heatmap header and body**

卡片标题使用 Grid：左侧标题和帮助，右侧 Legend；当可用宽度不足时使用 WrapPanel 或第二行容器让 Legend 下移。

Legend 固定四档：

```text
少  [0h] [<1h] [1–3h] [>3h]  多
```

在同一个横向 `ScrollViewer` 内按顺序放置：

1. 月份轴，左侧预留与星期标签相同的 46 DIP；
2. 周次轴，每周一格，短文本为 1、2、3……；
3. 星期标签和 7×N 热力格。

月份标签 ItemsControl 使用 `HeatmapMonthAxisPanel`，并在 ItemContainerStyle 中绑定 attached properties。

- [x] **Step 7: Replace each mouse-only cell with a keyboard Button**

Button 外框固定 26×26 DIP，模板内部居中放置 24×24 DIP Border。周次标签在 26 DIP 列内水平居中；星期标签使用 26 DIP 高的行容器，并让文本自然尺寸垂直居中。Button 使用：

```xml
Command="{Binding DataContext.SelectHeatmapDateCommand,
    RelativeSource={RelativeSource AncestorType={x:Type UserControl}}}"
CommandParameter="{Binding}"
AutomationProperties.Name="{Binding TooltipText}"
ToolTip="{Binding TooltipText}"
```

通过 DataTrigger 按 `IntensityLevel` 选择四个画刷。焦点状态必须有 1 DIP 可见描边；Hover 可以提高边框透明度，但不得改变强度等级。

星期标签 ItemsControl 设置 `AlternationCount="7"`，隐藏周二、周四、周六的可见文本（用 `Hidden` 而非 `Collapsed`，保留行高与热力格对齐），保留周一、周三、周五、周日。

`AlternationIndex` 由 ItemContainer 承载，因此 Trigger 必须写在 `ItemContainerStyle`（`TargetType="{x:Type ContentPresenter}"`）里，不能写在 `DataTemplate.Triggers` 中——写在 DataTemplate 内不会生效，而静态字符串测试也证明不了它真的隐藏了。具体写法与测试补充见 Task 2.5 的 T2-P1-01。

隐藏索引 1/3/5 依赖“周一为周首列”的冻结前提（见 Global Constraints）。

- [x] **Step 8: Remove the obsolete mouse handler**

删除 `HeatmapCell_MouseLeftButtonUp` 以及只为该事件存在的代码。不得影响趋势图的 `PeriodSelected` 路径。

- [x] **Step 9: Add localization keys**

至少新增并保持中英文参数一致：

```text
LOCPlaytimeInsightsHeatmapLess
LOCPlaytimeInsightsHeatmapMore
LOCPlaytimeInsightsHeatmapZeroHours
LOCPlaytimeInsightsHeatmapUnderOneHour
LOCPlaytimeInsightsHeatmapOneToThreeHours
LOCPlaytimeInsightsHeatmapOverThreeHours
LOCPlaytimeInsightsHeatmapWeekNumberFormat
```

- [x] **Step 10: Run Panel, localization, XAML, and full regression tests**

Run完整测试。Expected: Panel 和 Calendar Heatmap 新测试通过；本地化 parity、源键完整性和 Dashboard 可访问性测试继续通过。

- [x] **Step 11: Commit the calendar heatmap UI**

提交范围（时机由用户决定，见 Task 0 Step 4）：

```powershell
git add Controls/HeatmapMonthAxisPanel.cs ViewModels/Dashboard/DashboardDistributionViewModel.cs ViewModels/DashboardViewModel.cs Resources/PlaytimeInsightsVisualResources.xaml Views/PlaytimeInsightsDashboardView.xaml Views/PlaytimeInsightsDashboardView.xaml.cs Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: refine calendar heatmap navigation and legend"
```

---

### Task 2.5: Close the Task 0–2 Review Findings

> **状态（2026-08-30）：已完成并随 `0180a81` 提交，后续最终护栏纳入 `e59f9bf`。** 来源为 `docs/superpowers/reviews/2026-08-18-dashboard-task-0-2-acceptance-review.md` 的批次 A/B/C；该必经关口已在进入 Task 3 前完成。

**Files:**
- Modify: `ViewModels/Dashboard/HeatmapCellViewModel.cs`
- Modify: `Services/AnalyticsService.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Tests/Program.cs`
- Modify: `docs/CLIENT_ACCEPTANCE_1.1.0.md`

**Interfaces:**
- Produces: Calendar 热力图的单一颜色来源（只有 `IntensityLevel`）
- Produces: 生效且可验证的星期标签隐藏规则
- Preserves: `WeekHourCellViewModel.HeatOpacity` 的连续强度计算
- Preserves: 所有已通过的 Task 1/Task 2 测试

- [x] **Step 1: Remove the residual Calendar HeatOpacity**

`Views/` 已只消费 `IntensityLevel`，因此删除 Calendar 侧的第二个颜色来源：

```diff
 public HeatmapIntensityLevel IntensityLevel { get; set; }
-public double HeatOpacity { get; set; }
```

同时删除 `CreateHeatmapProjection` 中 `HeatmapCellViewModel` 初始化器里的 `HeatOpacity` 赋值（形如 `seconds == 0 ? 0.08 : 0.18 + ...`）。

不要删除 `AdvancedAnalyticsService` 中 `WeekHourCellViewModel.HeatOpacity` 的连续强度计算。

加源码块护栏：

```csharp
var heatmapProjection = ExtractSourceBlock(
    analyticsSource,
    "private static HeatmapProjection CreateHeatmapProjection(",
    "private static void ApplyPeriodGameSummaries(");
Equal(false, heatmapProjection.Contains("HeatOpacity"));
```

另加一条断言确认 `WeekHourCellViewModel` 仍包含 `HeatOpacity`，避免误删周×小时矩阵功能。

- [x] **Step 2: Move the weekday AlternationIndex trigger into ItemContainerStyle**

把当前位于 `DataTemplate.Triggers` 的隐藏规则改到 `ItemContainerStyle`，并删除 DataTemplate 内的旧 Trigger，避免两个来源竞争：

```xml
<ItemsControl.ItemContainerStyle>
    <Style TargetType="{x:Type ContentPresenter}">
        <Style.Triggers>
            <DataTrigger
                Binding="{Binding RelativeSource={RelativeSource Self},
                                  Path=(ItemsControl.AlternationIndex)}"
                Value="1">
                <Setter Property="Visibility" Value="Hidden" />
            </DataTrigger>
            <DataTrigger
                Binding="{Binding RelativeSource={RelativeSource Self},
                                  Path=(ItemsControl.AlternationIndex)}"
                Value="3">
                <Setter Property="Visibility" Value="Hidden" />
            </DataTrigger>
            <DataTrigger
                Binding="{Binding RelativeSource={RelativeSource Self},
                                  Path=(ItemsControl.AlternationIndex)}"
                Value="5">
                <Setter Property="Visibility" Value="Hidden" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</ItemsControl.ItemContainerStyle>
```

静态测试增加：星期 ItemsControl 存在 `ItemContainerStyle`；Style 绑定 `(ItemsControl.AlternationIndex)`；DataTemplate 不再包含 `Property="ItemsControl.AlternationIndex"` 的 Trigger。

STA 测试能构造完整 View 时，断言 7 个 `ContentPresenter` 的可见性序列为 `Visible, Hidden, Visible, Hidden, Visible, Hidden, Visible`（周一至周日）。

- [x] **Step 3: Strengthen the structural contract test**

`TestCalendarHeatmapVisualContract` 目前主要检查控件名、命令和四个 Brush key。补齐尺寸与资源约束：

```text
Button Width=26 Height=26
CellSwatch Width=24 Height=24
CellSwatch CornerRadius=3
HeatmapMonthAxisPanel ColumnPitch=26
周次轴使用 HeatmapColumnCount
周次文字水平居中，星期文字在 26 DIP 行容器中垂直居中
Legend 包含四档资源，且阈值文案与 HeatmapIntensityScale.FromSeconds 完全一致
焦点状态使用 DynamicResource TextBrush
```

- [x] **Step 4: Add month-projection Culture and clipping tests**

新增范围测试 `2026-07-15 → 2026-08-15` 和 `2026-08-01 → 2026-08-31`，断言：标签数量与文本；起始列与跨度；月份切换后周次重置；范围外隐藏日期不产生额外标签；zh-CN 和 en-US 下月份文本正确。

测试必须显式设置 Culture，不得依赖执行机默认文化。

- [x] **Step 5: Align and stabilize the schema 4 load budget**

先核对 `Tests/Program.cs` 中 schema 4 加载的实际 PASS 阈值是否为 1400 ms；与文档不一致时以冻结的 1400 ms 为准统一。

连续采样 5 次并记录平均值、最大值、最小值：

```powershell
for ($i = 1; $i -le 5; $i++) {
    Write-Host "=== RUN $i ==="
    $output = & dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite" 2>&1
    $output | Select-String "100k sessions|schema 4 JSON load|All Playtime Insights tests passed|FAIL"
}
```

已知参考值：审查中出现过 1465 ms 与 1343 ms 两次结果，说明证据不稳定。若 5 次最大值仍超过 1400 ms，进入存储加载路径 profiling，不得通过放宽文档阈值掩盖超预算。

- [x] **Step 6: Run the full regression**

Expected: 两次 Release 构建 0 warning、0 error；输出 `All Playtime Insights tests passed.`；新增护栏与 Culture 测试通过；5 次 schema 4 加载最大值 ≤ 1400 ms。

---

### Task 3: Theme and Cache the Trend Area, Line, and Nodes

**Files:**
- Modify: `Controls/AdaptiveTrendChart.cs`
- Modify: `Resources/PlaytimeInsightsVisualResources.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Consumes resource keys: `TrendLineBrush`、`TrendAreaFillBrush`、`TrendNodeFillBrush`
- Produces: `ResolveBrush(string key, Brush fallback)` 重载（现有签名只有 `ResolveBrush(string key, Color fallback)`，无法给渐变 Brush 兜底）
- Preserves: one closed Area Geometry, one line Geometry, existing hover and sparse-label behavior
- Preserves: 节点外圈继续解析 `ControlBackgroundBrush`（方案 A），不新增外圈命名资源，也不改为固定色值
- Preserves: 线宽分档 `Count >= 180 ? 1 : Count >= 90 ? 1.5 : 2.5`
- Produces: normal node ring for data sets with at most 90 rendered points

- [x] **Step 1: Add failing trend visual-contract tests**

扩展现有 Trend Chart 测试，确认：

- 共享资源字典包含三个新 Brush key（`TrendLineBrush`、`TrendAreaFillBrush`、`TrendNodeFillBrush`）；
- `AdaptiveTrendChart` 存在 `ResolveBrush(string, Brush)` 重载，且通过 `ResolveBrush` 获取这三个 key；
- 节点外圈画刷来自 `ResolveBrush("ControlBackgroundBrush", …)`，普通节点与 hover 节点同源；
- Area 只调用一次 `DrawGeometry(areaBrush, null, area)`；
- 普通节点同时具有填充和 Pen；
- `OnRender` 不再构造当前硬编码的三段蓝紫 Area Brush（`Color.FromArgb(102, 63, 140, 255)` 等）；
- `OnRender` 不再逐帧 `new LinearGradientBrush`；
- 点数超过 90 时仍不绘制普通节点（该阈值已存在，属于保持项）；
- `OnRender` 与 hover 绘制路径都不含固定近白色常量，也不存在 `TrendNodeRingBrush` 这个 key。

- [x] **Step 2: Run tests to verify RED**

Expected: 缺少三个资源 key、缺少 `ResolveBrush(string, Brush)` 重载、缺少普通节点外圈契约。

实际 RED 结果：`[FAIL] Trend chart resolves themed area, line and node resources`，其余测试全部通过，确认新契约是唯一失败项。

- [x] **Step 3: Add semantic trend resources**

```xml
<LinearGradientBrush x:Key="TrendLineBrush" StartPoint="0,0" EndPoint="1,0">
    <GradientStop Color="#FF2F8CFF" Offset="0" />
    <GradientStop Color="#FFA45CFF" Offset="1" />
</LinearGradientBrush>
<LinearGradientBrush x:Key="TrendAreaFillBrush" StartPoint="0.5,0" EndPoint="0.5,1">
    <GradientStop Color="#303B82F6" Offset="0" />
    <GradientStop Color="#185B7CFA" Offset="0.55" />
    <GradientStop Color="#005B7CFA" Offset="1" />
</LinearGradientBrush>
<SolidColorBrush x:Key="TrendNodeFillBrush" Color="#FF4A90E2" />
```

`TrendNodeRingBrush` 不使用固定色值。当前 hover 节点外圈用的是 `ControlBackgroundBrush`（见 `Controls/AdaptiveTrendChart.cs` 的 hover 绘制路径，`DrawEllipse(glyph, new Pen(controlBackground, 1), point, 4.5, 4.5)`），它随主题变化；改成固定近白色 `#CCF3F4F6` 会在 Playnite Light 主题下失去边界，与本计划自己写的“不允许回退到 `Brushes.White`”是同一个缺陷，只是换了载体。

**已定：采用方案 A。** 不新增 `TrendNodeRingBrush` 资源；普通节点与 hover 节点的外圈都调用 `ResolveBrush("ControlBackgroundBrush", ...)`，即把现有 hover 行为推广到普通节点。因此：

- `Interfaces` 中的四个 key 收敛为三个：`TrendLineBrush`、`TrendAreaFillBrush`、`TrendNodeFillBrush`；
- 外圈不占用命名资源槽位，测试断言的是“节点 Pen 的画刷来自 `ResolveBrush("ControlBackgroundBrush", …)`”，而不是某个新 key 是否存在；
- Task 7 的护栏相应写成“hover 与普通节点外圈同源且解析主题资源”，不要断言 `TrendNodeRingBrush` 存在。

方案 B（新增 `TrendNodeRingBrush`，值为 `{DynamicResource ControlBackgroundBrush}` 之类的主题派生引用）已被否决，仅作为记录保留。无论如何都不得让 hover 节点从主题资源退化为固定常量。

- [x] **Step 4: Add a Brush-fallback overload and reuse frozen fallbacks**

现有签名是：

```csharp
private Brush ResolveBrush(string key, Color fallback)
```

它只能构造 `SolidColorBrush`，无法给 `TrendLineBrush` 和 `TrendAreaFillBrush` 这两个渐变 Brush 兜底。新增重载：

```csharp
private Brush ResolveBrush(string key, Brush fallback)
{
    return TryFindResource(key) as Brush ?? fallback;
}
```

保留原 `Color` 重载供 `PanelSeparatorBrush`、`TextBrush` 等既有调用点使用。

将 fallback Brush 和 Pen 创建为静态、可冻结对象。每次 `OnRender` 只解析资源引用，不再逐帧创建多段 GradientStop 集合。

推荐成员：

```csharp
private static readonly Brush FallbackTrendLineBrush = CreateFallbackTrendLineBrush();
private static readonly Brush FallbackTrendAreaBrush = CreateFallbackTrendAreaBrush();
private static readonly Brush FallbackTrendNodeFillBrush = CreateFrozenBrush(...);
```

每个静态 fallback 在创建后立即 `Freeze()`；未冻结的 Brush 被多次 render 复用会带来跨线程和失效风险。

- [x] **Step 5: Draw the subtle area and two-layer normal nodes**

保留现有面积 Geometry；将填充改为 `TrendAreaFillBrush`。普通节点仅在 `renderedItems.Count <= 90` 时绘制（该阈值已存在于现有实现，不要改动）：

```csharp
var nodePen = new Pen(nodeRingBrush, 1.5);
if (nodePen.CanFreeze)
{
    nodePen.Freeze();
}

foreach (var point in renderedPoints)
{
    drawingContext.DrawEllipse(nodeFillBrush, nodePen, point, 3d, 3d);
}
```

Hover 节点继续使用更大半径（4.5），外圈按 Step 3 所选方案与普通节点保持同源，但该同源必须是主题资源方向的统一，不是把 hover 拉到固定色值。

- [x] **Step 6: Run focused render tests and full regression**

Run完整测试。Expected: Trend 生命周期、绘制、主题契约和性能测试全部通过。

实际结果：两个 Release 构建 0 warning、0 error；`All Playtime Insights tests passed.`；6 项 Trend 相关测试全部 PASS；100k 分析 650 ms（预算 750），schema 4 加载 1,058 ms（预算 1,400）。

- [x] **Step 7: Commit the chart elevation**

提交范围（时机由用户决定）：

```powershell
git add Controls/AdaptiveTrendChart.cs Resources/PlaytimeInsightsVisualResources.xaml Tests/Program.cs
git commit -m "feat: refine trend chart visual elevation"
```

---

### Task 3.5: Give the Trend Gridlines a Duration Scale

> **Why this exists.** Task 3 lowered then re-raised the area alpha to keep the gridlines readable through the fill. That trade is only worth making if the gridlines carry information — and today they carry none. `OnRender` draws three horizontal lines at 0 / 0.5 / 1 of the plot height with **no labels**, `GetPlotRect` reserves **no left gutter**, and `CreatePoints` normalises by `items.Max(Seconds)` — a maximum that appears nowhere on screen except the hover tooltip. So the chart currently has no readable magnitude at all without hovering. Labelling the lines converts decoration into a Y scale and retro-justifies the alpha.

**Files:**
- Modify: `Controls/AdaptiveTrendChart.cs`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `AdaptiveTrendChart.ResolveAxisMaximumSeconds(ulong peakSeconds)` — deterministic, testable ceiling
- Produces: a left gutter in `GetPlotRect()` sized to the measured label widths
- Consumes: `AnalyticsService.FormatDuration(ulong)` (already `public static`; no new localization keys)
- Changes: `CreatePoints` normalises to the axis maximum, not the raw peak
- Preserves: X-axis sparse date labels, hover hit-testing, the single area geometry, line thickness tiers, the 90-point node budget

**Axis rule (deliberately simple — no 1/2/5/10 ladder).** Round the peak **up** only:

- peak `0` → axis maximum `0`; draw the gridlines but no value labels (there is nothing to scale)
- peak `< 3600s` → ceil to the next `600s` (10 minutes)
- peak `>= 3600s` → ceil to the next `3600s` (1 hour)

The midpoint label is `axisMaximum / 2`. Because the maximum is always a multiple of 600 or 3600, the midpoint is a multiple of 300 or 1800, so `FormatDuration` renders it cleanly (`4 小时` → `2 小时`; `5 小时` → `2 小时 30 分`; `30 分钟` → `15 分钟`).

Two consequences to accept explicitly:

1. **Every point's Y coordinate shifts.** The peak no longer touches the top gridline, because it is normalised against the rounded maximum rather than itself. That is the point — it turns the top line into a real reference instead of a restatement of the peak.
2. The baseline label is the culture-formatted numeral `0` (`0.ToString(CultureInfo.CurrentCulture)`), not `FormatDuration(0)`. A bare numeral needs no localization key and keeps the gutter narrow; `0 分钟` would be both verbose and wider.

**Gutter must be measured, not guessed.** `FormatDuration` yields variable-width strings (`30 分钟`, `4 小时`, `2 小时 30 分`). Build the label `FormattedText` objects, take the widest, and set the gutter to that plus padding, clamped so it never eats the plot. Never clip an axis label.

`GetPlotRect()` is called from three places — `OnRender`, `OnMouseMove` and `CreatePoints` — and all three must agree, or hover hit-testing drifts from the drawn geometry. The axis maximum depends only on `renderedItems` (not on the plot rect), so cache the maximum and the gutter in fields during `OnRender` and have `GetPlotRect()` read the cached gutter. Do **not** measure text on every mouse move. Reset both fields in `ResetRenderedState`.

- [x] **Step 1: Register failing axis tests**

```csharp
Run("Trend axis rounds the peak up to a labelled maximum", TestTrendAxisMaximumRounding);
Run("Trend chart reserves a measured gutter and labels the gridlines", TestTrendAxisGutterAndLabels);
```

`TestTrendAxisMaximumRounding` asserts the boundaries directly against `ResolveAxisMaximumSeconds`: `0 → 0`, `1 → 600`, `600 → 600`, `601 → 1200`, `3599 → 3600`, `3600 → 3600`, `3601 → 7200`, `13620 (3h47m) → 14400 (4h)`.

`TestTrendAxisGutterAndLabels` renders a chart on STA and asserts the plot rect's left edge is greater than the old fixed `12`, that the gutter grows for a wider label, and that points normalise against the rounded maximum (the peak sits strictly below the top gridline whenever the peak is not already a clean multiple).

- [x] **Step 2: Run tests to verify RED**

Expected: `ResolveAxisMaximumSeconds` missing; plot rect still starts at 12.

实际 RED 结果：`error CS0117: AdaptiveTrendChart 未包含 ResolveAxisMaximumSeconds 的定义`，编译期失败。

- [x] **Step 3: Add the axis maximum and cached gutter**

Add the rounding helper, the `axisMaximumSeconds` and `axisGutter` fields, the measure-then-cache step in `OnRender`, and the gutter-aware `GetPlotRect()`. Point `CreatePoints` at the cached maximum.

实现记录：`ResolveAxisMaximumSeconds` 定为 `public static`（本项目没有 `InternalsVisibleTo`，而它是纯函数、控件本身已是 public）。`OnRender` 的顺序改为「取 items → 解析 separator/textBrush → 算 axisMaximumSeconds → 测量标签得 axisGutter → CreatePoints → 绘制」，因为刻度最大值只依赖 items、留白只依赖标签宽度，二者都先于几何。`ResetRenderedState` 同时复位两个缓存字段。

- [x] **Step 4: Draw the three value labels**

Right-align each label in the gutter against its gridline, using the same `textBrush` the X-axis labels already use, at the same 10–11 DIP size. Per the design rules, axis text wears text ink and never the series colour.

- [x] **Step 5: Run the full regression**

Expected: two Release builds 0 warning / 0 error; `All Playtime Insights tests passed.`; the Task 3 trend contract test still passes unchanged; performance budgets unaffected (this is render-time only).

实际结果：两个 Release 构建 0 warning、0 error；`All Playtime Insights tests passed.`；8 项 Trend 相关测试全部 PASS（含 Task 3 的契约测试，未改动）；100k 分析 638 ms，schema 4 加载 1,068 ms。

同时核对了一处坐标一致性风险：`DashboardTrendProjection` 仍在按**原始峰值**产出 `TrendPoints` / `TrendLineGeometry` / `TrendAreaGeometry` / `TrendLinePoints` / `TrendChartWidth`，与控件新的取整归一化不一致。经查这些属性在 `Views/PlaytimeInsightsDashboardView.xaml` 中**完全没有绑定**——趋势图只绑 `ItemsSource="{Binding PeriodActivities}"` 并自行计算几何，所以它们是遗留投影输出，不会造成错位。`BarHeight` 的两处绑定属于星期/小时分布条，与趋势图无关。若将来要复用这些投影属性渲染，必须先让它们改用 `ResolveAxisMaximumSeconds`。

- [x] **Step 6: Commit the trend axis**

```powershell
git add Controls/AdaptiveTrendChart.cs Tests/Program.cs
git commit -m "feat: add trend chart duration axis"
```

#### Known issue deferred out of this task: the chart's ink does not match its card

`ModulePanelStyle` sets `Background="{StaticResource ModuleBackgroundBrush}"` — a **hardcoded dark gradient** (`#FF1E2028 → #FF181920`) that does not follow the Playnite theme — and sets `TextElement.Foreground="{StaticResource MetricCardTextBrush}"` (fixed light `#FFF0F0F5`). Every `TextBlock` inside the module therefore gets fixed light ink, correct for a permanently dark card.

`AdaptiveTrendChart` is a `FrameworkElement` that paints text with `DrawText`, so `TextElement.Foreground` does not reach it. It resolves the **theme's** `TextBrush` instead. Consequences under Playnite Light: the chart's axis labels turn dark on a card that stayed dark, while every sibling `TextBlock` in the same card stays light. The node ring has the mirrored problem — it resolves the theme's `ControlBackgroundBrush`, which goes light under Playnite Light and paints a bright halo on a dark card, when a surface ring is supposed to match the surface.

Task 3.5 keeps using `textBrush` so the new Y labels stay consistent with the existing X labels rather than splitting one chart across two ink sources. The wholesale fix — repointing the chart's ink and ring at the card's own family (`MetricCardTextBrush`, `MetricCardMutedTextBrush`, and a surface-matching ring) — is a separate change and should be its own task. Note that no test currently asserts `ResolveBrush("TextBrush"`, so the repoint is not blocked by existing guards.

---

### Task 4: Enrich Sparse Rankings and Restore the Historical Share Wash

> **最终实现基线（2026-08-23）：** 已放弃曾用于验收的底部 4 DIP 渐变能量条，恢复历史整行时长占比背景并完成本地部署。后续实现、审查和验收均以本节的整行背景契约为准；不得把底部细条或前三名专属进度色当作目标效果。

**Files:**
- Modify: `Services/DashboardAnalysisContext.cs`
- Modify: `Services/AnalyticsService.cs`
- Create: `Services/RecentActivityFormatter.cs`
- Modify: `ViewModels/Dashboard/GameRankingViewModel.cs`
- Modify: `Resources/PlaytimeInsightsVisualResources.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces: `DashboardGameRangeStatistics.LastSessionLocal`
- Produces: `RecentActivityFormatter.Format(DateTime? activity, DateTime now)`
- Produces: `GameRankingViewModel.ShareText`、`LastPlayedText`、`IsSparseLayout`
- Produces: `LOCPlaytimeInsightsShareOfRangeFormat`（区间榜专用占比文案）
- Preserves: `ProgressPercent` remains the share of total duration, independent of selected ranking metric
- Preserves: `LOCPlaytimeInsightsShareOfTotalFormat` 仅由累计榜使用
- Restores: the historical full-item translucent blue duration-share wash while preserving independent gold, silver, and bronze card glows
- Rejects: a fixed-height bottom bar, gradient indicator, visible track, or rank-specific `Position` trigger on the progress layer

- [x] **Step 1: Register failing ranking projection tests**

注册：

```csharp
Run("Range rankings expose share and latest activity", TestRangeRankingAuxiliaryText);
Run("Ranking lists mark one and two rows as sparse", TestRankingSparseDensity);
Run("Ranking share wash keeps the historical full-row contract", TestRankingShareWashContract);
Run("Lifetime rankings convert Playnite activity to local time", TestLifetimeRankingActivityTimeZone);
Run("Dashboard snapshot shares one ranking timestamp", TestDashboardSnapshotUsesOneTimestamp);
```

投影测试使用固定 `now = new DateTime(2026, 8, 17, 12, 0, 0)`，避免相对时间断言依赖系统时间。至少断言：当天、昨天、较早日期和未知日期。

时区测试必须构造一个「UTC 时间落在前一天、本地时间落在当天」的 `LastActivity`（或反向），断言累计榜显示的相对日期与区间榜同口径。

- [x] **Step 2: Run tests to verify RED**

Expected: 缺少最近活动、ShareText 或 Sparse 属性。

实际 RED：初始实现因缺少 `LastSessionLocal`、`ShareText`、`LastPlayedText`、`IsSparseLayout` 和 `RecentActivityFormatter` 编译失败；历史整行背景契约随后针对当前 4 DIP 渐变实现产生两项预期失败（运行时模板契约和旧结构守卫）。

- [x] **Step 3: Extend the ranking contracts**

在 `DashboardGameRangeStatistics` 增加：

```csharp
public DateTime? LastSessionLocal { get; set; }
```

在 `GameRankingViewModel` 增加：

```csharp
public string ShareText { get; set; }
public string LastPlayedText { get; set; }
public bool IsSparseLayout { get; set; }
```

- [x] **Step 4: Capture latest local session while aggregating**

在处理每个有效会话时，根据 `StartedAtUtc` 和 `StartUtcOffsetMinutes` 计算本地开始时间；仅当新值更晚时更新对应游戏的 `LastSessionLocal`。不得使用当前机器时区覆盖会话自身保存的偏移量。

- [x] **Step 5: Add deterministic relative-activity formatting**

新增可测试方法：

```csharp
public static class RecentActivityFormatter
{
    public static string Format(DateTime? activity, DateTime now)
    {
        if (!activity.HasValue)
        {
            return LocalizationService.Get(
                "LOCPlaytimeInsightsNoRecentActivity",
                "无最近游玩记录");
        }

        var value = activity.Value;
        if (value.Date == now.Date)
        {
            return LocalizationService.Format(
                "LOCPlaytimeInsightsTodayAtFormat",
                "今天 {0:HH:mm}",
                value);
        }

        if (value.Date == now.Date.AddDays(-1))
        {
            return LocalizationService.Format(
                "LOCPlaytimeInsightsYesterdayAtFormat",
                "昨天 {0:HH:mm}",
                value);
        }

        return LocalizationService.Format(
            "LOCPlaytimeInsightsRecentActivityDateFormat",
            "{0:g}",
            value);
    }
}
```

规则：

- `null` → 本地化“无最近游玩记录”；
- 同一天 → “今天 HH:mm”；
- 前一天 → “昨天 HH:mm”；
- 其他日期 → 当前文化的短日期和时间；
- 不在该方法内部读取 `DateTime.Now`；
- 传入值必须已经是**本地时间**。该方法不做任何时区换算，所有换算责任在调用方。

实现位于独立 `Services/RecentActivityFormatter.cs`，通过 `LocalizationService` 获取文案。调用投影入口时只读取一次 `DateTime.Now`，并把同一个 `now` 传给所有排行项。

- [x] **Step 6: Publish auxiliary ranking text and sparse density**

区间榜使用 `LastSessionLocal`（已由 Step 4 用会话自带 offset 转为本地）。

累计榜使用 Playnite `Game.LastActivity`，但**必须先转本地再交给 formatter**：

```csharp
var lastActivityLocal = game.LastActivity.HasValue
    ? DateTime.SpecifyKind(game.LastActivity.Value, DateTimeKind.Utc).ToLocalTime()
    : (DateTime?)null;
```

`LastActivity` 在本仓库此前零引用，这是首次接入。Playnite 以 UTC 持久化该字段，而 `RecentActivityFormatter` 拿的是本地 `now`；直接把 UTC 值传进去会在跨零点和非零时区显示错误的“今天/昨天”，并让两个排行 Tab 口径不一致。实现时先用一条断言或一次实机核对确认所用 SDK 版本的 `Kind`，再固定到测试。

占比文案不复用累计文案。区间榜 `totalDuration` 的分母是**本期**总时长（`CreateRangeRankings` 只累加当期 `allStats`），而现有 `LOCPlaytimeInsightsShareOfTotalFormat` 的文案是“占总游玩时长 {0:P1}”。藏在 Tooltip 里勉强可忍，提为常显行文就是错的。因此：

- 新增 `LOCPlaytimeInsightsShareOfRangeFormat`，中文形如“占本期总时长 {0:P0}”，英文对应；
- 区间榜的 `ShareText` 使用新 key；Task 4.5 删除 `ProgressTooltipText`，行级结构化 Tooltip 直接复用 `ShareText`；
- 累计榜继续使用 `LOCPlaytimeInsightsShareOfTotalFormat`；
- 注意格式说明符：`{0:P1}` 渲染为 `63.0%`，若要 Step 8 示意中的 `63%` 需用 `{0:P0}`。二者选一并固定到测试，不要让文档与实现各说一套。

列表 materialize 后统一设置：

```csharp
var isSparse = results.Count > 0 && results.Count <= 2;
foreach (var result in results)
{
    result.IsSparseLayout = isSparse;
}
```

0 项使用现有空状态，不视为 Sparse。

- [x] **Step 7: Restore the historical full-row progress resources**

保留共享的 `RankingEnergyBrush`，最终资源契约与已部署实现一致：

```xml
<SolidColorBrush x:Key="RankingEnergyBrush" Color="#FF4A90E2" />

<Style x:Key="RankingEnergyBackgroundBarStyle"
       TargetType="{x:Type ProgressBar}">
    <Setter Property="Foreground" Value="{StaticResource RankingEnergyBrush}" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="IsHitTestVisible" Value="False" />
    <Setter Property="Focusable" Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type ProgressBar}">
                <Grid ClipToBounds="True">
                    <Border x:Name="PART_Track"
                            Background="{TemplateBinding Background}"
                            CornerRadius="6"
                            ClipToBounds="True">
                        <Rectangle x:Name="PART_Indicator"
                                   HorizontalAlignment="Left"
                                   Fill="{TemplateBinding Foreground}"
                                   Opacity="0.10" />
                    </Border>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

`PART_Track` 只提供透明、圆角 6 的裁剪边界，不绘制可见轨道；`PART_Indicator` 是左对齐、没有固定高度的 `Rectangle`，宽度继续由 WPF `ProgressBar` 根据 `Value / Maximum` 计算。`Opacity="0.10"` 只作用于指示层，不降低排行项前景内容或前三名 Glow 的透明度。

不要重新加入 `RankingEnergyTrackBrush`、`RankingEnergyIndicatorBrush`、渐变 Indicator 或固定 4 DIP 高度。进度层始终使用统一的 `#FF4A90E2` 蓝色，不添加按 `Position` 切换金、银、铜的 Trigger。前三名差异只由外层 `RankingItemCardStyle` 的 Gold/Silver/Bronze Glow、徽章、边框和文字提供；首名原有的金色顶部高光同样保留在卡片层，不属于进度层。

- [x] **Step 8: Recompose each ranking item**

默认项保持约 64 DIP；`IsSparseLayout=True` 时通过 DataTrigger 将高度提升到 80 DIP、Padding 提升到 12 DIP。

辅助区域显示两项：

```text
占本期总时长 63%  ·  最近游玩：昨天 22:49
```

文本空间不足时优先保留 ShareText，LastPlayedText 允许 CharacterEllipsis；Task 4.5 删除了 LastPlayedText 自身那份重复 Tooltip。时长占比 ProgressBar 在内容下层纵向 Stretch，以 10% 透明度填充排行项可用高度；封面、名称和数值保持在前景层。

ProgressBar 必须是排行内容内层 Grid 的**第一个子元素**，跨越全部四列并保持无固定高度：

```xml
<ProgressBar Grid.ColumnSpan="4"
             Minimum="0"
             Maximum="100"
             Value="{Binding ProgressPercent}"
             VerticalAlignment="Stretch"
             HorizontalAlignment="Stretch"
             Style="{StaticResource RankingEnergyBackgroundBarStyle}" />
```

这里的“整行”指填满排行项 Border 内层 Grid 的可用高度：普通项约 64 DIP，Sparse 项约 80 DIP。它不是覆盖全卡片的不透明色块，而是位于封面、名称、辅助信息和数值后方的低透明度背景层；Grid 后声明的前景元素自然绘制在其上方。

整行背景填充长度恒为时长占比，与当前选中的排行指标无关（见 Frozen Product Decision 10）。切到“会话次数”或“活跃天数”排序时，背景长度与主数值不对应。因此 Tooltip 必须讲清分母，并在 `docs/CLIENT_ACCEPTANCE_1.1.0.md` 中登记为已知行为——不要在本轮临时改动 `ProgressPercent` 的口径。

- [x] **Step 9: Add localization keys**

```text
LOCPlaytimeInsightsRecentPlayedPrefix
LOCPlaytimeInsightsTodayAtFormat
LOCPlaytimeInsightsYesterdayAtFormat
LOCPlaytimeInsightsNoRecentActivity
LOCPlaytimeInsightsRecentActivityDateFormat
LOCPlaytimeInsightsShareOfRangeFormat
```

`LOCPlaytimeInsightsShareOfTotalFormat` 保持现有文案不动，仅供累计榜使用。所有新键必须同时进入 `en_US.xaml` 和 `zh_CN.xaml`，格式参数个数与顺序一致，并纳入既有的本地化 parity 与源键完整性测试。

- [x] **Step 10: Run ranking projection, localization, XAML, and performance tests**

Expected: 新排名测试和完整回归通过；10 万会话分析仍不高于 750 ms。最近活动聚合必须保持 O(session count)，不得对每个排行项重新扫描全部会话。

实际 GREEN：158 项完整回归通过；两个 Release 构建 0 warning、0 error；历史整行背景运行时契约通过；100k 分析 625 ms，schema 4 加载 1,061 ms。运行时测试同时断言：Track 透明、圆角为 6，Indicator 为统一蓝色且透明度为 0.10，ProgressBar 高度未固定并实际超过 4 DIP、纵向 Stretch，以及第一名外层金色 Glow 仍然存在。

部署复验（2026-08-23）：158/158 回归通过，Release 构建 0 warning、0 error；已安装目录保持严格 9 文件，`PlaytimeInsights.dll` SHA-256 为 `23988409C564B5DD63C7631A18213C68860B4F8BD05F116EE8F1051DB398A5F6`，部署期间用户数据指纹未变化，Playnite 日志确认插件 1.0.0 已加载。该记录只证明部署基线，不代替 Task 7 的人工视觉验收。

- [x] **Step 11: Commit the ranking refinement**

```powershell
git add Services/DashboardAnalysisContext.cs Services/AnalyticsService.cs Services/RecentActivityFormatter.cs ViewModels/Dashboard/GameRankingViewModel.cs Resources/PlaytimeInsightsVisualResources.xaml Views/PlaytimeInsightsDashboardView.xaml Localization/en_US.xaml Localization/zh_CN.xaml Tests/Program.cs
git commit -m "feat: enrich sparse dashboard rankings"
```

---

### Task 4.5: De-duplicate Ranking Details and Consolidate the Row Tooltip

> **状态（2026-08-30）：已实现、部署、完成用户复核并随 `301708c` 提交。** Task 4 的整行蓝色时长占比背景保持不变。

**Files:**
- Modify: `Services/AnalyticsService.cs`
- Modify: `ViewModels/Dashboard/GameRankingViewModel.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Localization/en_US.xaml`
- Modify: `Localization/zh_CN.xaml`
- Test: `Tests/Program.cs`

**Interfaces:**
- Preserves: `PrimaryValueText` always represents the active `RankingMetric`
- Changes: `DetailText` contains at most session count and active days, excluding the active metric
- Produces: `AverageSessionText`、`AverageSessionLabelText`、`LongestSessionText`、`LongestSessionLabelText`
- Removes: `ProgressTooltipText`
- Preserves: `ShareText` as the localized share sentence and `LastPlayedText` as visible row text
- Preserves: Task 4's `ProgressPercent` duration-share semantics and full-row blue wash

- [x] **Step 1: Register failing de-duplication and Tooltip tests**

注册：

```csharp
Run("Ranking details omit the active sort metric", TestRankingDetailDeduplication);
Run("Ranking secondary text uses readable hierarchy", TestRankingSecondaryTextHierarchy);
Run("Ranking row tooltip consolidates secondary statistics", TestRankingTooltipComposition);
```

第一个测试以固定统计值覆盖五种 `RankingMetric`，使用手工推导的最终字符串，防止当前排序指标重新混入 `DetailText`。后两个测试加载真实 `GameRankingItemTemplate`，断言实际字体、透明度、Tooltip 视觉内容和 LastPlayedText 的 Tooltip 状态，不用静态字符串扫描代替运行时行为。

- [x] **Step 2: Run tests to verify RED**

实际 RED：

- DetailText 仍为“时长 · 次数 · 活跃日 · 平均 · 最长”，期望“次数 · 活跃日”；
- DetailText 仍为 10 DIP，期望 11 DIP；
- 行级 Tooltip 仍是单一 `ProgressTooltipText` 字符串，无法提供结构化平均/最长信息。

- [x] **Step 3: Reduce DetailText and remove the active metric duplicate**

`PrimaryValueText` 对五种排序指标始终保留。区间榜 DetailText 的最终矩阵：

| 当前排序指标 | DetailText |
|---|---|
| 游玩时长 | 次数 · 活跃日 |
| 会话次数 | 活跃日 |
| 活跃天数 | 次数 |
| 平均会话 | 次数 · 活跃日 |
| 最长会话 | 次数 · 活跃日 |

实现使用 `FormatRankingDetail(stats, metric)`，最多创建两个片段；不得把时长、平均或最长重新塞回常显行。累计榜的 `Playnite 当前累计口径` 不属于这组五值 DetailText，保持现状。

旧的五参数 `LOCPlaytimeInsightsRankingDetailFormat` 删除，新增：

```text
LOCPlaytimeInsightsRankingActiveDaysDetailFormat
```

会话次数继续复用 `LOCPlaytimeInsightsCountTimesFormat`；新键负责在 DetailText 中明确写出“活跃日 / active days”，避免英文只显示含义模糊的“days”。

- [x] **Step 4: Move average and longest into one structured row Tooltip**

区间排行投影发布平均/最长的本地化标签与格式化值。排行项根 Border 使用一个 ToolTip：第一行是 `ShareText`，第二、三行分别用两列 Grid 显示平均会话和最长会话。标签随 ViewModel 投影，而不是在 Popup 内依赖 View 级 `DynamicResource`，确保 ToolTip 脱离主视觉树后仍有完整文案。

累计榜没有这组精确会话统计，`AverageSessionText` 和 `LongestSessionText` 保持 `null`；Tooltip 的两个统计行通过 `RankingTooltipMetricRowStyle` 折叠，只显示占比，不伪造平均/最长值。

`LastPlayedText` 继续显示在辅助行，但移除 `ToolTip="{Binding LastPlayedText}"`，避免悬停后只重复同一段文字。

- [x] **Step 5: Raise the DetailText visual hierarchy**

DetailText 从 `FontSize="10"` 调整到 `FontSize="11"`，Opacity 从 `TextOpacityTertiary`（0.58）调整到 `TextOpacitySecondary`（0.72）。ShareText 与最近游玩行的既有层级不在本任务扩大调整。

- [x] **Step 6: Verify localization and existing ranking contracts**

英文和中文新增键保持参数一致；删除旧五值格式键。Task 4 的整行背景、Sparse 64/80 DIP、高亮 Glow、区间/累计时区口径和 `ProgressPercent` 分母均不改变。

- [x] **Step 7: Run the focused and full regression suite**

三项新增测试已转绿，既有 Dashboard 静态契约改为使用完整 DynamicResource 标记判断 KPI 顺序，避免 `LOCPlaytimeInsightsLongestSessionOption` 对 `LOCPlaytimeInsightsLongestSession` 造成子串误命中。

一次完整回归的 100k 分析样本测得 768 ms，单次超过 750 ms 预算；同一构建立即连续复测三次为 650 / 694 / 653 ms，三次均通过，确认是计时抖动，未放宽预算。最终干净 Release 验证为：两个构建 0 warning、0 error，161/161 回归通过，100k 分析 596 ms，schema 4 加载 976 ms，`git diff --check` 通过。

- [x] **Step 8: Commit after user review**

用户已明确触发提交，本任务随 `301708c` 落盘。

- [x] **Step 9: Deploy for manual acceptance**

2026-08-24 使用干净 Release 产物完成本地部署：两个构建 0 warning、0 error，161/161 回归通过，100k 分析 744 ms，schema 4 加载 1,167 ms；安装前已备份原插件，Release 与安装目录严格保持 9/9 文件哈希一致，`PlaytimeInsights.dll` SHA-256 为 `59C911BD54D91960AD51090285C7CFABEC668B0B804254EE6C63202726FDBDDE`，覆盖期间 7 个用户数据文件的规范化指纹未变化。Playnite 启动后正常响应，日志确认 Playtime Insights 1.0.0 已加载。人工视觉验收仍待用户完成。

---

### Task 5: Split KPI Metrics into Hero and Tier 2 Panels

> **状态（2026-08-28）：跳过，不实施。** 当前单一响应式指标面板在宽屏下保持紧凑的 4×2 排列，在 1000×900 窗口下可稳定重排为 3–3–2，未发现截断、重叠或可读性缺陷。拆分为 2 张 Hero 与 6 张 Tier 2 只会提供偏好型层级增强，并会在中等宽度下显著增加纵向占用、把趋势图和排行榜推至更下方。以下 Steps 仅作为已否决方案的历史记录，不再进入实施、提交或验收范围；除非用户明确重新开启本任务，否则保留现有单面板 `MetricCardsHost`。

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Resources/PlaytimeInsightsVisualResources.xaml`（仅 `HeroMetricCardTintBrush`，Style 不进共享字典）
- Test: `Tests/Program.cs`

**Interfaces:**
- Produces named hosts: `HeroMetricCardsHost` and `Tier2MetricCardsHost`
- Uses two existing `ResponsiveUniformPanel` instances
- Preserves all 8 metric bindings and existing comparison data
- Preserves the outer `Grid x:Name="MetricCardsHost"` as the entrance-animation host（该 Grid 已存在于 `Views/PlaytimeInsightsDashboardView.xaml:891`，内含 `MetricCardsTransform`）
- Removes the single-responsive-panel assumption for `MetricCardsHost`

- [ ] **Step 1: Replace the old 4×2 static contract with failing hierarchy tests**

将测试名称改为：

```csharp
Run("Dashboard metrics use two hero and six tier-two cards", TestDashboardMetricHierarchy);
```

断言：

- 存在两个 `ResponsiveUniformPanel`；
- Hero Panel 有 2 个 Border，`MaxColumns=2`；
- Tier 2 Panel 有 6 个 Border，`MaxColumns=3`；
- 第一张 Hero 绑定 `RangeDurationDisplay`；
- 第二张 Hero 绑定 `SessionCountText`；
- 其余六张卡按现有数据顺序全部保留；
- 不出现第九张卡或重复的平均会话卡。

当前 8 张卡的主数值绑定已核对如下，重新分栏时逐一比对，不得增删：

| 序号 | 主数值绑定 | 归属 |
| --- | --- | --- |
| 1 | `RangeDurationDisplay.*`（Run 拼装，附比较 Pills） | Hero |
| 2 | `SessionCountText`（辅助文本 `AverageSessionSummaryText`） | Hero |
| 3 | `ActiveDaysText` | Tier 2 |
| 4 | `LongestSessionDisplay.*` | Tier 2 |
| 5 | `LifetimeDurationDisplay.*` | Tier 2 |
| 6 | `CurrentStreakText` | Tier 2 |
| 7 | `AnomalyCountText` | Tier 2 |
| 8 | `PeakPeriodText`（辅助文本 `PeakPeriodShareText`） | Tier 2 |

“平均会话”当前已经是第 2 张卡的辅助文本，不是独立卡片；Hero 2 沿用这一结构即可，不要新建平均会话卡。

Hero 1 的主数值是多个 `Run` 拼装的 TextBlock，`Run` 自身未设 `FontSize`，因此 `HeroMetricValueStyle` 的 `FontSize="30"` 能正常继承生效。

- [ ] **Step 2: Run tests to verify RED**

Expected: 当前只有一个 8 子项 Panel，测试失败。

- [ ] **Step 3: Add Hero semantic resources in the correct scope**

资源归属先定下来，否则会在解析期直接失败：

- `MetricCardStyle` 定义在 `Views/PlaytimeInsightsDashboardView.xaml:46`，`MetricValueStyle` 在 `:272`，`MetricHelperTextStyle` 在 `:279`，三者都在 View 的 `UserControl.Resources` 里；
- 共享字典 `Resources/PlaytimeInsightsVisualResources.xaml` 是被 View 合并进来的，**看不到宿主 UserControl 的本地资源**；
- 因此在共享字典里写 `BasedOn="{StaticResource MetricCardStyle}"` 或 `BasedOn="{StaticResource MetricValueStyle}"` 会解析失败。

本轮采用就近定义：三个新 Style 定义在 `Views/PlaytimeInsightsDashboardView.xaml` 的 `UserControl.Resources`，与基样式同域：

```xml
<Style x:Key="HeroMetricCardStyle"
       TargetType="Border"
       BasedOn="{StaticResource MetricCardStyle}">
    <Setter Property="MinHeight" Value="166" />
</Style>

<Style x:Key="HeroMetricValueStyle"
       TargetType="TextBlock"
       BasedOn="{StaticResource MetricValueStyle}">
    <Setter Property="FontSize" Value="30" />
    <Setter Property="FontWeight" Value="Bold" />
</Style>

<Style x:Key="Tier2MetricValueStyle"
       TargetType="TextBlock"
       BasedOn="{StaticResource MetricValueStyle}">
    <Setter Property="FontSize" Value="24" />
</Style>
```

只有不依赖本地基样式的纯资源进共享字典：

```xml
<LinearGradientBrush x:Key="HeroMetricCardTintBrush" StartPoint="0,0" EndPoint="1,1">
    <GradientStop Color="#142F8CFF" Offset="0" />
    <GradientStop Color="#0FA45CFF" Offset="1" />
</LinearGradientBrush>
```

备选方案是先把 `MetricCardStyle`、`MetricValueStyle`、`MetricHelperTextStyle` 及其依赖的 `MetricCardTextBrush`、`MetricCardMutedTextBrush` 整体下移到共享字典，再在共享字典里派生。该方案改动面明显更大且会牵动既有 XAML 契约测试，本轮不采用；若执行时确实需要它，先暂停并说明理由。

注意 `MetricValueStyle` 当前是 `FontSize="26"`，所以 Hero 是 26 → 30 的提升，Tier 2 是 26 → 24 的收敛，两者都不是“保持现状”。

Hero 根 Border 继续使用 `MetricCardBackgroundBrush`。Hero 卡当前内容是 `Border > StackPanel`，没有可叠层的 Grid；要加 tint 必须先把内容容器改成 `Grid`，第 0 层放 `Background="{StaticResource HeroMetricCardTintBrush}"`、`IsHitTestVisible="False"` 的 Border，原 StackPanel 作为其上一层，使 3%–5% 蓝紫 tint 叠加在现有卡片底色之上，而不是替换主题背景。

- [ ] **Step 4: Build the Hero panel**

使用：

```xml
<controls:ResponsiveUniformPanel
    x:Name="HeroMetricCardsHost"
    MinItemWidth="320"
    PreferredItemWidth="480"
    MaxItemWidth="560"
    MinColumns="1"
    MaxColumns="2"
    HorizontalSpacing="12"
    VerticalSpacing="12"
    CenterIncompleteRow="True" />
```

第一张 Hero 的值与比较 Pills 使用 Grid：左列主值，右列 WrapPanel。宽度不足导致 Pills 换行时，Pills 进入值下方，不能挤压时长文本到不可读。

第二张 Hero 显示会话次数和平均会话辅助文本；不生成不存在的会话次数同比/环比。

- [ ] **Step 5: Build the Tier 2 panel**

使用：

```xml
<controls:ResponsiveUniformPanel
    x:Name="Tier2MetricCardsHost"
    MinItemWidth="220"
    PreferredItemWidth="320"
    MaxItemWidth="420"
    MinColumns="1"
    MaxColumns="3"
    HorizontalSpacing="12"
    VerticalSpacing="12"
    CenterIncompleteRow="True" />
```

按以下顺序移动六张现有卡片，不改绑定：活跃天数、最长会话、累计总时长、连续天数、异常提示、峰值时段。

- [ ] **Step 6: Update presentation animation hosts**

现有入口动画只认识 `MetricCardsHost`（`ViewModels/Dashboard/DashboardEntrancePlan.cs` 的首个 step，`Tests/Program.cs` 也断言 `full.Steps[0].HostName == "MetricCardsHost"`）。

好消息是 `MetricCardsHost` 已经是一个 `Grid`（`Views/PlaytimeInsightsDashboardView.xaml:891`，内含 `MetricCardsTransform`），当前只是里面装了一个 8 卡 `ResponsiveUniformPanel`。因此本步骤只需把这一个 Panel 换成两个 Panel，**不要改动 `DashboardEntrancePlan` 和它的既有测试**。

保留 `Grid x:Name="MetricCardsHost"` 作为动画宿主，内部包含 `HeroMetricCardsHost` 和 `Tier2MetricCardsHost` 两个响应式 Panel。测试断言内部 Panel 数量和各自子项数量，不要删除宿主名。

- [ ] **Step 7: Run responsive metric tests**

期望列数必须先算出来写死，不能写成“以 Panel 计算为准”——那等于让实现反过来定义测试，RED-GREEN 就失效了。

`ResponsiveUniformPanel.SelectColumnCount` 的实际算法是：

```text
preferredColumns = floor((W + HorizontalSpacing) / (PreferredItemWidth + HorizontalSpacing))
columns         = clamp(preferredColumns, MinColumns, min(MaxColumns, childCount))
while columns > 1 and itemWidth(W, columns) < MinItemWidth: columns--
itemWidth(W, c) = min(MaxItemWidth, (W - (c-1) * HorizontalSpacing) / c)
```

**选列由 `PreferredItemWidth` 决定，`MinItemWidth` 只用于事后降列。** 这一点决定了下表，写测试前不要凭 `MinItemWidth` 估算。

按 Hero（`Min=320`、`Preferred=480`、`Max=560`、`MaxColumns=2`、`HS=12`、6 张→2 张子项）和 Tier 2（`Min=220`、`Preferred=320`、`Max=420`、`MaxColumns=3`、`HS=12`、6 张子项）代入，内容宽度 = 视图宽度 − 48：

| 视图宽度 | 内容宽度 | Hero 列数 | Hero item 宽 | Tier 2 列数 | Tier 2 item 宽 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 640 | 592 | 1 | 560（被 Max 钳制） | 1 | 420（被 Max 钳制） |
| 900 | 852 | 1 | 560（被 Max 钳制） | 2 | 420（被 Max 钳制） |
| 1200 | 1152 | 2 | 560（被 Max 钳制） | 3 | 376 |
| 1600 | 1552 | 2 | 560（被 Max 钳制） | 3 | 420（被 Max 钳制） |

推导出的两个断点，也一并固定到测试：

- Hero 进入 2 列需要内容宽度 ≥ 972（`2 × 480 + 12`），即视图宽度 ≥ 1020；
- Tier 2 进入 3 列需要内容宽度 ≥ 984（`3 × 320 + 24`），即视图宽度 ≥ 1032。

注意 900 DIP 视图下 **Hero 只有 1 列**：内容宽 852 时 `floor((852+12)/492) = 1`。不要因为 `2 × 320 + 12 = 652 ≤ 852` 就以为是 2 列——那是 `MinItemWidth` 的算式，不是选列算式。

断言：

- 四个宽度下 Hero 与 Tier 2 的列数和 item 宽等于上表固定值；
- Tier 2 不超过 3 列，Hero 不超过 2 列；
- 内容宽 1152 时 Hero 网格宽为 `2 × 560 + 12 = 1132`，小于 1152 并被居中（`gridStart = 10`），不溢出内容宽度；
- 每行等高，不重叠；
- 8 张指标卡均存在且各出现一次；
- 测试中显式记录用的是视图宽度还是内容宽度，避免与 `AdaptiveDashboardPanel` 阈值混用。

- [ ] **Step 8: Run full regression and commit**

```powershell
git add Resources/PlaytimeInsightsVisualResources.xaml Views/PlaytimeInsightsDashboardView.xaml Tests/Program.cs
git commit -m "feat: establish dashboard metric hierarchy"
```

---

### Task 6: Anchor Drilldown to the Triggering Visualization

> **状态（2026-09-03）：已实现、部署、完成视觉人工验收并提交；实际读屏器验收已由用户决定跳过。** 已完成 Anchor 状态、两个上下文宿主、共享模板、视口感知最小滚动、无动画与虚拟化回归；视觉验收中发现的深色主题来源标签黑字问题也已修复并补充真实模板回归。目标框架 net462 不提供 `AutomationProperties.LiveSetting` 或 `LiveRegionChanged`，因此无障碍通知改用活动宿主的 `AutomationProperties.Name` 与 `AutomationElementIdentifiers.NameProperty` 变更事件；原计划由 Task 7 使用进程外实际读屏器验证该兼容路径，但该项现已跳过，仍不得声称具备原生 Polite live-region 语义。本测试环境中，同一 STA 进程使用 UIA 客户端监听自身 WPF 树实测会阻塞并触发测试超时；自动化回归只验证 Name 绑定、通知方法存在及 Trend→Distribution→Reset 生命周期，不把自监听结果伪装成读屏器验收。

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml.cs`
- Modify: `ViewModels/Dashboard/DashboardDrilldownViewModel.cs`
- Modify: `Tests/Program.cs`

**Interfaces:**
- Consumes: existing `SelectPeriodCommand`, `SelectHeatmapDateCommand`, `SessionDetailVisibility` and `DashboardScrollViewer`
- Produces: `DashboardDrilldownAnchor.None/Trend/Distribution`, `TrendDrilldownHost`, `DistributionDrilldownHost` and one shared `DrilldownCardTemplate`
- Selects exactly one host from the trigger source: Trend point → Trend host; Calendar heatmap cell → Distribution host
- Preserves: existing Drilldown ListView, paging, clear command, cover cache, Recycling virtualization

- [x] **Step 1: Add failing anchor-state and layout tests**

新增：

```csharp
Run("Dashboard drilldown anchors to its triggering visualization", TestDashboardDrilldownAnchors);
Run("Dashboard drilldown hosts preserve source adjacency", TestDashboardDrilldownHostLayout);
Run("Dashboard drilldown reveal scrolls only when its header is outside the viewport", TestDashboardDrilldownViewportReveal);
```

状态测试必须证明：

- 初始与 `ResetSelection()` 后 Anchor 为 `None`，两个宿主都 Collapsed；
- `SelectPeriod(...)` 后 Anchor 为 `Trend`，只显示 `TrendDrilldownHost`；
- `SelectHeatmapDate(...)` 后 Anchor 为 `Distribution`，只显示 `DistributionDrilldownHost`；
- 从 Trend 连续切换到 Distribution 时，旧宿主先退出布局，任一时刻不出现两张明细卡；
- 既有 `SessionDetailVisibility` 继续表示“存在活动下钻”，供清除命令和旧绑定兼容使用。

XAML 源码顺序固定为：

```csharp
var moduleOrder = new[]
{
    "TrendModule",
    "TrendDrilldownHost",
    "RankingModule",
    "DistributionModule",
    "DistributionDrilldownHost",
    "AnomalyModule"
};
```

两个 Host 都固定为 `AdaptiveDashboardPanel.Zone="Primary"`。在内容宽度 1200 的双栏与内容宽度 900 的单栏分别断言：Trend Host 紧随 Trend，Distribution Host 紧随 Distribution；未激活宿主为 Collapsed，不占布局高度。宽度测试继续显式区分内容宽度与视图宽度。

模板测试断言 `DrilldownCardTemplate` 只定义一次，两个 Host 都引用它；不得复制两份 ListView 标记。活动宿主内仍只有一个可见 ListView，非活动宿主不得生成已实现的列表项容器。

- [x] **Step 2: Run tests to verify RED**

Expected: 当前只有一个位于 Distribution 之后的 `DrilldownModule`，没有 Anchor 状态、两个上下文宿主或视口判断。

- [x] **Step 3: Add explicit drilldown anchor state**

在 `DashboardDrilldownViewModel.cs` 增加：

```csharp
public enum DashboardDrilldownAnchor
{
    None,
    Trend,
    Distribution
}
```

`SelectPeriod(...)` 请求 `Trend`，`SelectHeatmapDate(...)` 请求 `Distribution`；会话查询、封面投影和分页准备成功后再原子切换 `SelectedAnchor` 与可见性，加载失败时保留上一份已发布状态。`ResetSelection()` 回到 `None`。提供只读 `TrendHostVisibility` 与 `DistributionHostVisibility`，并在 Anchor 变化时通知二者。`SessionDetailVisibility` 继续与 `Anchor != None` 同步，避免破坏清除命令及既有绑定。

`SelectedDetailTitle` 已包含周期/日期、时长和会话数量，继续作为语义标题，不新增“属于排行榜”的文案，也不需要新增本地化 key。

- [x] **Step 4: Extract one shared card template and add two adjacent hosts**

把现有 `DrilldownModule` 内部卡片提取为 `UserControl.Resources` 中的：

```xml
<DataTemplate x:Key="DrilldownCardTemplate">
    <!-- existing Drilldown card, ListView, paging and clear command -->
</DataTemplate>
```

在 `TrendModule` 后插入 `TrendDrilldownHost`，在 `DistributionModule` 后插入 `DistributionDrilldownHost`。两者都属于 Primary、都以当前 `DashboardViewModel` 为 Content、都引用同一个模板，并分别绑定 `Drilldown.TrendHostVisibility` 与 `Drilldown.DistributionHostVisibility`。

模板中的 `SourceText` 必须显式使用 `{DynamicResource TextBrush}`，不能跨 `ContentControl`、`ListView` 和 `ListViewItem` 边界依赖默认前景色继承；否则深色主题会退回黑色。`TestDrilldownSourceTagForeground` 使用真实 `DataTemplate` 在黑色默认前景、白色主题 `TextBrush` 下验证最终文字画刷。

不要保留旧 `DrilldownModule`，不要把 Host 放到 `RankingModule` 下方，也不要复制卡片模板。Anomaly 继续属于 Secondary。

- [x] **Step 5: Replace unconditional scrolling with viewport-aware minimal reveal**

将事件统一为 `DrilldownHost_IsVisibleChanged`。Host 变为 Visible 且布局完成后，只检查其顶部标题带（建议 96 DIP）相对 `DashboardScrollViewer` 视口的位置：

```csharp
if (visibility != Visibility.Visible || !(sender is FrameworkElement host))
{
    return;
}

Dispatcher.BeginInvoke(new Action(() =>
{
    ScrollHeaderBandIntoView(host, DashboardScrollViewer, 96d);
}), DispatcherPriority.Loaded);
```

不得再用 `AdaptiveDashboardPanel.IsWideLayout` 决定是否滚动。宽屏和窄屏遵循同一规则：标题带已经在视口内就不滚动；下方越界时只增加 `VerticalOffset` 到标题底部进入视口，上方裁切时只减少越界量。直接使用 `ScrollToVerticalOffset`，避免 `BringIntoView` 依赖 PresentationSource 后产生不可预测跳转；滚动不转移键盘焦点。

- [x] **Step 6: Do not add a drilldown reveal animation**

本次问题是位置和语义，不需要 160ms Opacity/Translate 动画。Host 出现后立即使用最终 Opacity 与 Transform；Reduced motion 不需要 Task 6 专用分支。保留全局动效护栏，但不得为下钻新增 Storyboard。

- [x] **Step 7: Add the net462-compatible automation notification**

net462 的 WPF 参考程序集不包含 `AutomationProperties.LiveSetting` 和 `AutomationEvents.LiveRegionChanged`，不得写入无法编译的 Polite live-region XAML。两个活动宿主改为绑定完整上下文标题：

```xml
AutomationProperties.Name="{Binding SelectedDetailTitle}"
```

`SelectedDetailTitle` 变化后，View 为当前可见宿主取得或创建 `FrameworkElementAutomationPeer`，并调用 `RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, newName)`。这是 net462 可用的兼容通知，不等价于现代 WPF 原生 Polite live region；最终读屏效果留给 Task 7 实机确认。

不强制把键盘焦点从趋势图或热力格移到列表；用户可按正常 Tab 顺序进入紧随触发模块的内容。文档和自动化名称不得再称其为“右栏内容”。

- [x] **Step 8: Verify no virtualization regression**

既有测试继续断言：

- ListView；
- `VirtualizingStackPanel`；
- `CanContentScroll=True`；
- `IsVirtualizing=True`；
- `VirtualizationMode=Recycling`；
- 水平滚动关闭；
- 100 条分页按钮仍可用。
- 非活动宿主为 Collapsed，且不生成已实现的 `ListViewItem` 容器。

- [x] **Step 9: Run layout, interaction, and full regression tests**

Expected: 两种 Anchor 状态、宽窄布局下的源邻接、视口内不滚动、视口外最小滚动、可见性重新测量、绑定下钻展开、UI Automation NameProperty 变更通知、虚拟化及完整回归全部通过。

实际结果（2026-08-30）：Anchor 主流程先确认 7 项预期 RED，再完成 GREEN；深色主题来源标签缺陷也经过独立 RED（期望 `#FFFFFFFF`、实际 `#FF000000`）与 GREEN。最终两次 Release 构建均为 0 warning、0 error，167/167 回归通过，100k 会话分析 545 ms，schema 4 加载 901 ms，`git diff --check` 通过。非活动宿主会把 Content 置空，避免隐藏列表保留已实现容器。原生 Polite live region 因 net462 API 缺失未实现，改用上一步记录的 NameProperty 兼容通知，读屏器效果仍待 Task 7 人工验收。

- [x] **Step 10: Commit the context-anchored drilldown workflow**

```powershell
git add ViewModels/Dashboard/DashboardDrilldownViewModel.cs Views/PlaytimeInsightsDashboardView.xaml Views/PlaytimeInsightsDashboardView.xaml.cs Tests/Program.cs docs/superpowers/plans/2026-08-17-dashboard-visual-elevation-implementation.md
git commit -m "feat: anchor dashboard drilldown to selection source"
```

---

### Task 7: Integrate Contracts and Execute the Acceptance Matrix

> **状态（2026-09-03）：自动护栏、Release 门禁、真实热力图布局成本、证据记录、范围审查与最终提交已完成；视觉基线人工矩阵仅剩减弱动效。** 最终连续五轮回归通过；100k / schema 4 最大值分别为 692 / 1,031 ms。640–1600 DIP 精确内容宽度、完整数据状态、全主题、zh_CN / en_US、全 DPI、Calendar 键盘链路、Calendar/Week×Hour 视觉区分和跨零点相对日期已完成人工核验；实际读屏器由用户决定跳过，不计为通过。All Sessions 707.6 ms 后续项已由本地性能分支自动化收敛；性能版虚拟化后实机矩阵仍待执行。最终提交同时包含用户验收发现的比较胶囊纵向排列修复及其真实 WPF 布局回归，不改变 8 卡响应式架构。

**Files:**
- Modify: `Tests/Program.cs`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml`
- Modify: `docs/CLIENT_ACCEPTANCE_1.1.0.md`
- Verify: every file in this plan's File Map

**Interfaces:**
- Consumes: all Tasks 1–6 outputs（含 Task 2.5）
- Produces: final automated gate and explicit manual visual evidence record

- [x] **Step 1: Replace obsolete static guards**

删除或更新以下旧假设：

- 单个 `DrilldownModule` 固定在 Distribution 之后；
- 按宽屏/窄屏决定是否调用 `BringIntoView`；
- Drilldown 固定进入 Secondary 或累计时长排行榜下方；
- Heatmap 使用 `HeatOpacity` 连续缩放；
- Calendar cell 使用 MouseLeftButtonUp；
- Ranking Energy 使用全高 0.10 透明 Rectangle；
- 区间榜占比复用 `LOCPlaytimeInsightsShareOfTotalFormat`。

新增护栏：

- `AdaptiveDashboardPanel` 仍只有 Primary/Secondary，不新增 FullWidth；
- `ResponsiveUniformPanel.cs` 未被修改为跨列布局；
- `Grid x:Name="MetricCardsHost"` 仍存在，且 `DashboardEntrancePlan.Steps[0].HostName` 仍为 `MetricCardsHost`；
- 指标区仍为单一 `ResponsiveUniformPanel` 和 8 张卡，不存在 Hero/Tier 2 拆分；
- `DrilldownCardTemplate` 只定义一次，`TrendDrilldownHost` 与 `DistributionDrilldownHost` 都属于 Primary 且引用同一模板；
- Period 选择只激活 Trend Host，Calendar 日期选择只激活 Distribution Host，Reset 后两个 Host 都 Collapsed；
- Heatmap Month Axis 和热力格 ColumnPitch 均为 26；
- Legend 阈值与 `HeatmapIntensityScale.FromSeconds` 完全一致；
- `HeatmapCellViewModel` 不再包含 `HeatOpacity`，`WeekHourCellViewModel` 仍包含；
- 星期标签隐藏 Trigger 位于 `ItemContainerStyle`，不在 `DataTemplate` 内；
- 趋势图只有一个 Area Geometry 绘制；
- `AdaptiveTrendChart` 存在 `ResolveBrush(string, Brush)` 重载；节点外圈解析 `ControlBackgroundBrush`，且源码中不存在 `TrendNodeRingBrush`；
- 热力图四档画刷为冰青—青绿冷色系 ordinal ramp，等级主要由明度表达，中点色相跨度 ≤ 20°；
- Calendar 四档画刷属冰青色系，`HeatmapActiveBrush`（Week×Hour）仍为蓝紫且未被修改；Calendar 任一档不得出现与 `#2457D6` 的 ΔE < 15 的色值（海军蓝锚点已因此否决）；
- Drilldown 是否最小滚动只由其 96 DIP 标题带是否位于 `DashboardScrollViewer` 视口决定，与宽屏/窄屏无关；
- Task 6 不新增 Opacity/Translate reveal Storyboard，也不转移键盘焦点；
- 滞回文档与测试一致：内容宽度 1160 与 1180 为双栏，1159 为单栏。

- [x] **Step 2: Run the complete Release gate**

```powershell
dotnet build PlaytimeInsights.sln -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

Expected:

- plugin build: 0 warning、0 error；
- test build: 0 warning、0 error；
- test runner: `All Playtime Insights tests passed.`；
- 100k analytics ≤ 750 ms；
- schema 4 load ≤ 1400 ms。

- [x] **Step 3: Execute the width and data matrix**

矩阵按**内容宽度**给出，实机时把窗口调到「内容宽度 + 48」（垂直滚动条可见时再加其宽度），并在记录里同时写下两个数字：

| 内容宽度 | 参考视图宽度 | Required result |
| ---: | ---: | --- |
| 640 | 688 | 单栏；8 张指标卡不溢出；活动 Drilldown 紧随触发模块，标题带在视口外时最小滚动 |
| 900 | 948 | 单栏；8 张指标卡不溢出；两个 Drilldown Host 均不占用非活动高度；月份/周次与热力格对齐 |
| 1159 | 1207 | 从宽屏缩小时退出双栏 |
| 1160 | 1208 | **保持双栏**（滞回下限为 `width >= 1160`） |
| 1199 | 1247 | 从窄屏放大时仍保持单栏 |
| 1200 | 1248 | 进入双栏；活动 Drilldown 与其 Trend/Distribution 触发模块同 X，Ranking/Anomaly 留在 Secondary |
| 1440 | 1488 | 单一指标面板稳定重排；右栏无横向滚动 |
| 1600 | 1648 | 两栏不被强制拉伸等高；Calendar 横向坐标稳定 |

滞回是有方向的，1159、1160 和 1199 三行必须按方向分别走一遍：从双栏缩小到 1160（应保持双栏）再到 1159（应退出），以及从单栏放大到 1199（应保持单栏）再到 1200（应进入）。只在单一方向上取样无法验证滞回。

数据状态至少覆盖：空范围、1/2/3/10 个排行项、0/1/100/250 条下钻、跨月、六周月份、一年、All Sessions。

2026-09-03 状态：上述 8 个精确内容宽度、双向滞回和完整数据状态矩阵均由用户逐项实机核验通过，Step 3 完成。

- [x] **Step 3b: Measure the heatmap layout cost**

在 All Sessions 和一年范围下各计一次 Distribution 模块的 Measure + Arrange 耗时，并记录热力格总数。热力格是非虚拟化 `ItemsControl` + `UniformGrid`，每格是完整 `Button`；一年约 371 格，All Sessions 跨多年可达 1800 格以上，且每次刷新全量实例化。

记录实测值并与 Gate D 的预算比对。若超出预算，可选的收敛方向（本轮不预先实现，先记录）：为热力格宿主启用虚拟化、把每格从 `Button` 降级为轻量可聚焦元素、或对超长范围按月分段。不得只因为数据侧 750 ms / 1400 ms 达标就跳过本步骤。

- [ ] **Step 4: Execute theme, language, keyboard, and motion checks**

逐项记录：

- Default Dark、Default Light、Seaside Dark、Windows High Contrast；
- zh_CN、en_US；
- Tab 进入 Heatmap Button，Space/Enter 可触发下钻；
- 焦点描边可见；
- Drilldown 状态变化会更新活动宿主的 Automation Name，并发送 NameProperty 变更事件；使用实际读屏器记录是否播报，未播报则明确记录 net462 限制；
- Reduced motion 下无淡入位移；
- Trend Area 不遮挡网格、折线或 Hover Tooltip；
- 普通节点与 Hover 节点的外圈在浅色和深色主题均可分辨（这是 Task 3 Step 3 所选外圈方案的实机验证点）；
- Legend 文案与实际颜色档位一致；
- 同一张 `DistributionModule` 卡片内，Calendar（冰青，绝对档位）与 Week×Hour（蓝紫，相对刻度）一眼可分且不显刺眼；Calendar 的绝对档位图例不会被误读为管辖上方的相对刻度网格；
- All Sessions 下整片中档不过吵，高档作为稀有强调仍能跳出；每格的对角光泽在 24 DIP 上确实可见（这是把每档 stop 间距从 ΔE 4 左右提到 11.6 / 11.8 / 19.6 的目的，需实机确认判断成立）；
- 区间榜与累计榜的“最近游玩”在同一时刻显示同一相对日期口径（跨零点前后各查一次）。

2026-09-03 状态：全主题、zh_CN / en_US、全 DPI、Calendar Button 的 Tab/Space/Enter/焦点描边、Calendar/Week×Hour 视觉区分和跨零点相对日期已由用户实机核验通过；实际读屏器由用户决定跳过。减弱动效仍待人工确认，因此 Step 4 保持未勾选。

- [x] **Step 5: Record actual evidence, not expected evidence**

在 `docs/CLIENT_ACCEPTANCE_1.1.0.md` 中只勾选实际完成的矩阵项。截图应记录内容宽度、视图宽度、主题、语言、数据状态和日期。无法验证的高对比度、读屏或焦点项目保持未勾选并注明原因。

不要向 `docs/CLIENT_ACCEPTANCE_1.0.0.md` 写入本轮内容。

- [x] **Step 6: Review the final diff for scope creep**

Run:

```powershell
git status --short
git diff --stat
git diff -- Controls/AdaptiveDashboardPanel.cs Controls/ResponsiveUniformPanel.cs PlaytimeInsights.cs
```

Expected: 最后一条命令无差异。若有差异，必须在提交前解释并重新审查本计划的架构边界。

- [x] **Step 7: Commit final guards and acceptance evidence**

提交范围（时机由用户决定，见 Task 0 Step 4）：

```powershell
git add Tests/Program.cs Views/PlaytimeInsightsDashboardView.xaml docs/CLIENT_ACCEPTANCE_1.1.0.md docs/superpowers/plans/2026-08-17-dashboard-visual-elevation-implementation.md
git commit -m "fix: finalize dashboard visual acceptance"
```

---

## Delivery Gates

### Gate A: Data Semantics

- Heatmap 四档阈值边界 0、3599、3600、10800、10801 秒有自动化测试。
- Month Axis 支持跨月和六个日历周，不假设每月固定五周。
- Month Axis 文本在 zh-CN 和 en-US 下均有显式 Culture 测试，不依赖执行机默认文化。
- `HeatmapCellViewModel` 不再包含 `HeatOpacity`；`WeekHourCellViewModel` 仍保留连续强度。
- Ranking 最近活动使用会话保存的 UTC offset；累计排行使用 Playnite `LastActivity` 并先由 UTC 转本地。
- 区间榜与累计榜的相对日期口径一致，有跨零点用例覆盖。
- ShareText 明确表示所属范围的时长占比：区间榜用 `LOCPlaytimeInsightsShareOfRangeFormat`，累计榜用 `LOCPlaytimeInsightsShareOfTotalFormat`，两者不共用。
- 新聚合保持 O(session count)，无排行项级全量会话重扫。

### Gate B: Presentation Contracts

- Task 5 已跳过；8 张指标卡继续共用现有单一响应式面板，宽屏 4×2 与 1000×900 下 3–3–2 均不得出现截断、重叠或绑定丢失。
- `Grid x:Name="MetricCardsHost"` 及当前单面板结构保留为入口动画宿主，`DashboardEntrancePlan` 未被改动。
- Calendar Heatmap 留在 Primary；Trend/Distribution 下钻分别紧随对应触发模块且仍位于 Primary，累计时长排行榜下方不承接 Drilldown。
- 24 DIP 色块拥有 26 DIP Button 和键盘命令，周次与星期坐标轴文字分别水平、垂直居中。
- 星期标签隐藏 Trigger 位于 `ItemContainerStyle` 并有可见性序列证明。
- Legend、月份轴、周次轴与热力格共享滚动坐标。
- 热力图四档是冰青—青绿冷色系 ordinal ramp：渐变中点明度单调、相邻 ΔL ≥ 0.06、亮端相对模块底色 ≥ 2:1、三档中点色相跨度 ≤ 20°。0 档允许退到底色，靠描边读出网格。
- Calendar（冰青）与 Week×Hour（蓝紫）同属冷色但拉开距离：Calendar 低档对 Week×Hour 蓝端 ΔE ≥ 15（正常视觉与 deutan 均满足），高档对紫端更远；图例色块与 Calendar 网格同色，不会被误读为管辖 Week×Hour。
- Trend 只有一层 Area Fill；普通节点与 Hover 节点外圈同源，且同源指向 `ControlBackgroundBrush` 而非固定色值；不存在 `TrendNodeRingBrush` 这个 key。
- `AdaptiveTrendChart` 提供 `ResolveBrush(string, Brush)` 重载，`OnRender` 不再逐帧构造渐变 Brush。
- Ranking 使用统一蓝色、10% 透明度的整行时长占比背景；前三名金、银、铜 Glow、徽章和文字仍独立可辨。
- Ranking 的 DetailText 最多为“次数 · 活跃日”，并排除当前排序指标；字体为 11 DIP、0.72 Opacity。行级 Tooltip 统一承载占比、平均和最长，LastPlayedText 不再拥有自重复 Tooltip。

### Gate C: Responsive Interaction

- 1200/1160 DIP 滞回保持不变，且文档、矩阵与测试统一为「1160 保持双栏、1159 退出双栏」。
- 所有宽度断言显式区分内容宽度与视图宽度（差值 48）。
- 宽屏和窄屏都只在活动 Drilldown 的 96 DIP 标题带不在视口时执行最小滚动；标题已经可见时滚动偏移保持不变。
- Trend 与 Distribution 两个宿主任一时刻最多一个 Visible，显示位置与触发来源一致。
- Drilldown 100 条列表仍使用 Recycling virtualization。
- Task 6 不新增 Drilldown 出现过渡；其他新增动效在 Reduced motion 下禁用。

### Gate D: Release, Performance and Evidence

- 两个 Release build 为 0 warning、0 error。
- 完整测试输出 `All Playtime Insights tests passed.`。
- 100k 会话分析 ≤ 750 ms。
- schema 4 加载连续 5 次采样，**最大值** ≤ 1400 ms；测试内阈值与文档阈值一致。
- 热力图 UI 侧预算：记录一年与 All Sessions 两种范围下 Distribution 模块的 Measure + Arrange 实测耗时与热力格总数。本轮先建立基线读数并写入验收文档；若 All Sessions 明显影响交互流畅度，在验收文档中记为待收敛项，不得以“数据侧预算达标”为由跳过测量。
- 视觉矩阵只记录实际检查结果，未验证项不得标记通过。
- 本轮证据写入 `docs/CLIENT_ACCEPTANCE_1.1.0.md`；`docs/CLIENT_ACCEPTANCE_1.0.0.md` 与 `main` 保持一致。

## Spec Coverage Self-Review

| Requirement | Covered by |
| --- | --- |
| Trend/Calendar Drilldown 按触发来源就近展开 | Task 6 |
| Drilldown 标题带按视口状态执行最小滚动 | Task 6 Step 5, Gate C |
| 不移动 Calendar Heatmap 到窄右栏 | Global Constraints, Frozen Decision 1 |
| 不追求强制等高 | Global Constraints, Frozen Decision 3, Gate C |
| Area Fill Gradient | Task 3 |
| 节点高光外圈 | Task 3 |
| 月份和周次轴 | Tasks 1–2 |
| 固定时长 Legend | Tasks 1–2 |
| 24 DIP 色块、26 DIP 命中区与坐标轴对齐 | Task 2 |
| 2 Hero + 6 Tier 2 | Task 5（已跳过，不验收） |
| Hero 30 DIP、Tier 2 24 DIP | Task 5（已跳过，不验收） |
| Pills 与 Hero 值对齐 | Task 5（已跳过，不验收） |
| 短列表呼吸感和辅助信息 | Task 4 |
| 历史整行时长占比背景与前三名独立 Glow | Task 4 |
| 排行详情去重、次级文字层级和统一行级 Tooltip | Task 4.5 |
| 主题、语言、键盘、动效、性能验收 | Task 7 |
| Task 0–2 review 遗留项收口 | Task 2.5 |
| 单一颜色来源（删除 Calendar HeatOpacity） | Task 2.5 Step 1 |
| 星期标签隐藏规则真正生效 | Task 2.5 Step 2 |
| schema 4 加载证据稳定化 | Task 2.5 Step 5, Gate D |
| 累计榜最近活动时区口径 | Task 4 Step 6, Gate A |
| 区间占比专用文案 | Task 4 Steps 6/9, Gate A |
| 资源查找域与 BasedOn 归属 | 不适用（Task 5 已跳过） |
| 内容宽度与视图宽度区分 | Global Constraints, Task 6 Step 1, Task 7 Step 3 |
| 滞回边界 1160/1159 表述统一 | Global Constraints, Task 7 Steps 1/3, Gate C |
| 热力图 UI 侧预算 | Global Constraints, Task 7 Step 3b, Gate D |
| 验收记录与 1.0.0 发布记录分离 | Task 0 Step 2, Task 7 Step 5, Gate D |

## Explicit Non-Goals

- 不新增 FullWidth Zone。
- 不实现自动把模块移动到较短栏的 Masonry 算法。
- 不强制左右栏底边对齐。
- 不把 Calendar Heatmap 移到 Secondary。
- 不新增会话次数的同比或环比分析。
- 不实施 Task 5 的 Hero/Tier 2 拆分或 Tier 2 六列断点。
- 不改变排行榜当前可选排序指标。
- 不改变 `ProgressPercent` 的口径（它恒为时长占比，与选中指标无关；见 Frozen Decision 10）。
- 不改变 Heatmap 点击后的会话筛选口径。
- 不在本轮为热力格实现虚拟化或分段加载（只测量并记录，见 Task 7 Step 3b）。
- 不在本轮把 `MetricCardStyle` 系列样式下移到共享字典。
- 不新增跨侧边栏导航或详情页。

## Execution Handoff

2026-08-30 最终执行记录：

```text
Task 0（对账 + 1.1.0 验收文档）
  → Task 2.5（收口 Task 0–2 review 遗留项）★ 必经关口
  → Task 3 和 Task 4（可并行）
  → Task 3.5（趋势时长刻度，依赖 Task 3）和 Task 4.5（排行信息去重，依赖 Task 4）
  → Task 5（跳过）
  → Task 6
  → Task 7
```

Task 1 和 Task 2 的章节保留为规格记录，其中 Step 2 的 RED 期望已改为验证；实际提交分别为 `cda8081` 与 `0180a81`。

Task 3 / 3.5 已由 `108a4c4` 提交，Task 4 / 4.5 已由 `301708c` 提交，Task 5 已于 2026-08-28 跳过，Task 6 已由 `a0983ee` 提交，Task 7 与最终比较胶囊修复已由 `e59f9bf` 提交。

视觉分支所有已实施任务均已按用户指示提交并推送；各 Task 内的 `git add` / `git commit` 命令只保留为历史执行记录。性能分支已于 2026-09-04 推送并 fast-forward 合并回 `main`。当前尚未完成的是 Reduced motion 与性能版虚拟化后实机复验；All Sessions 自动化 UI 性能收敛已经完成，不再列为待办。
