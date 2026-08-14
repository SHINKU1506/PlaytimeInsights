# Dashboard 视觉重构路线图可行性审查

**审查日期**：2026-08-14<br>
**审查对象**：`docs/DASHBOARD_VISUAL_REFACTOR_ROADMAP.md` v1.0<br>
**审查范围**：视觉目标、WPF/Playnite 技术可行性、现有架构边界、遗漏依赖、测试与验收可执行性<br>
**审查结论**：有条件通过，需先修订路线图后再进入实施
**v1.1 复审对象**：`docs/DASHBOARD_VISUAL_REFACTOR_ROADMAP.md` v1.1（复审定稿）<br>
**v1.1 复审结论**：有条件达到可实施标准；具体实现必须同时遵循 `docs/superpowers/plans/2026-08-14-dashboard-visual-refactor-implementation.md`

---

## 1. 总体结论

路线图提出的主题修复、信息分级、宽屏空间利用、排行榜整合和下钻闭环方向总体合理，并与当前
Dashboard 的主要视觉问题相符。现有原生 WPF、MVVM 子模块、选择性刷新、`AdaptiveTrendChart`
和 `ResponsiveUniformPanel` 也能继续作为实现基础，不需要引入第三方 UI 框架。

但是，当前 v1.0 不能直接作为工程实施计划，主要原因如下：

1. 将多个涉及数据语义和领域枚举的功能误判为纯 XAML 调整；
2. 未定义 WPF 中实现 1024px 断点和异构双栏布局的具体机制；
3. 未处理现有固定最小宽度、GridView 列宽和九张指标卡完整去向；
4. 部分计划任务实际上已经实现，存在重复建设；
5. “第三方主题无硬编码色块”与大量固定强调色要求互相冲突；
6. 验收条件包含无法直接测量的“60fps”“无明显增加”等表述；
7. 文件影响范围、国际化资源和新增回归测试明显不完整。

在补齐以下修正建议后，整体方案可以实施。原计划约 20 个串行任务日，按当前代码边界重新估算，
建议预留 **26–32 个工程日**，另加一次独立的 Playnite 客户端主题、语言和 DPI 验收。

---

## 2. 审查依据与已验证基线

本次审查对照了以下实现：

- `Views/PlaytimeInsightsDashboardView.xaml`
- `Views/PlaytimeInsightsDashboardView.xaml.cs`
- `Controls/AdaptiveTrendChart.cs`
- `Controls/ResponsiveUniformPanel.cs`
- `ViewModels/DashboardViewModel.cs`
- `ViewModels/Dashboard/DashboardFilterViewModel.cs`
- `ViewModels/Dashboard/DashboardMetricsViewModel.cs`
- `ViewModels/Dashboard/DashboardDistributionViewModel.cs`
- `ViewModels/Dashboard/DashboardDrilldownViewModel.cs`
- `Services/AnalyticsService.cs`
- `Services/AdvancedAnalyticsService.cs`
- `Services/SessionQueryService.cs`
- `Tests/Program.cs`
- Playnite 6.16.0 本地 SDK XML 文档和默认桌面主题资源
- 已安装 Seaside 桌面主题资源

已执行验证：

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore `
  -p:PlayniteInstallDir="D:\software\Playnite"

dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release `
  --no-build -p:PlayniteInstallDir="D:\software\Playnite"
```

结果：

- Release 构建：0 警告、0 错误；
- 现有回归：108/108 通过；
- 10 万会话分析基线：635 ms；
- schema 4 的 10 万会话加载基线：1,174 ms。

上述结果证明当前代码与自动回归基线稳定，但不能证明路线图中的新视觉方案已经满足主题、DPI、
布局或帧率要求。

当前仓库可用的 Dashboard 截图属于 0.9.8 基线，适合确认长滚动、全宽面板和视觉层级问题，
不能代替 1.0.0 当前运行态及重构后的实机截图验收。

---

## 3. 关键审查结论

### P1-1：双栏布局缺少可执行的 WPF 响应式方案

**路线图位置**：

- 第 38 行：宽度大于等于 1024px 时启用双栏；
- 第 47–59 行：62%/38% 主次栏布局；
- 第 129、150 行：三天完成断点与双栏。

**当前实现**：

- 页面根容器是 `ScrollViewer` 内的纵向 `StackPanel`；
- `ResponsiveUniformPanel` 只支持等宽子项，不支持异构模块、跨列和不同布局角色；
- 异常列表项带有 `MinWidth="760"`；
- 趋势图带有 `MinWidth="320"`；
- 热力图和 24 小时分布依赖内部横向滚动。

**风险**：

在约 1024 DIP 的内容区中，38% 右栏只有约 370 DIP，异常列表的 760 DIP 最小宽度会导致严重横向
滚动。若路线图的 1024 指窗口外框物理像素，高 DPI 下实际内容宽度还会进一步缩小。

**修正建议**：

1. 断点统一定义为 **Dashboard 内容区的设备独立像素 DIP**，不使用系统物理像素；
2. 先测量模块最低可用宽度，再确定断点，不预先锁死 1024；
3. 初步建议双栏候选断点为 1200–1280 DIP，最终值由原型截图和长文本测试决定；
4. 在 View 层增加只读布局状态，例如 `IsWideLayout`，由根内容区域的 `SizeChanged` 更新；
5. 使用一个根 `Grid` 承载模块，通过样式触发器切换 `Grid.Row`、`Grid.Column` 和
   `Grid.ColumnSpan`；
6. 保持布局状态属于 View，不把 `ActualWidth` 或断点逻辑放入业务 ViewModel；
7. 宽屏模式下重做异常列表模板，不得把当前 760 DIP 的表格式行直接放入右栏；
8. 为 400–1199 DIP 保留单栏顺序，并确保所有模块仍可访问。

建议新增或修改：

```text
Views/PlaytimeInsightsDashboardView.xaml
Views/PlaytimeInsightsDashboardView.xaml.cs
Tests/Program.cs
```

若不希望在 Code-behind 中维护布局状态，可新增专用宽度转换器，但不得让转换器执行数据刷新或保存
任何业务状态。

---

### P1-2：快捷时间 Chip 不是纯界面改动

**路线图位置**：

- 第 70–72 行：近 7 天、近 30 天、本年、全时段、自定义；
- 第 152–154 行：只修改 XAML 和 `DashboardFilterViewModel`。

**当前实现**：

`DateRangePreset` 只有：

```text
Today
ThisWeek
ThisMonth
ThisYear
Custom
```

日期范围、自动聚合粒度和范围标题由 `AnalyticsService` 计算。`RangeOptions` 只有今天、本周、本月、
本年和自定义。

**风险**：

仅让 Chip 修改 `SelectedRangeOption` 无法表达近 7 天、近 30 天和全时段。将近 7 天或近 30 天
伪装成 `Custom` 会导致 Chip 选中状态、日期选择器显示状态和用户手动自定义范围互相冲突。

“全时段”也缺乏明确语义：

- 全部插件会话；
- 当前元数据筛选结果中的最早会话；
- Playnite 全部累计时长；
- 固定到某个极早日期。

这些口径会产生不同统计结果和趋势周期。

**修正建议**：

1. 新增明确枚举：

```text
Last7Days
Last30Days
ThisYear
AllSessions
Custom
```

2. 明确 `AllSessions` 为“当前数据及元数据筛选范围内，最早有效会话日期至今天”；
3. 不使用 `DateTime.MinValue` 作为起点，避免聚合周期和同比计算生成异常范围；
4. 将最早有效日期作为分析上下文的一部分传给日期范围解析；
5. 为新范围补充自动聚合规则：

```text
Last7Days   -> Day
Last30Days  -> Day
ThisYear    -> Month
AllSessions -> 按实际总天数选择 Day/Week/Month/Year
Custom      -> 保持现有按总天数选择逻辑
```

6. Chip 与下拉框必须使用同一 `RangeOptions` 数据源，避免形成两套状态；
7. Chip 点击只触发一次 `DashboardRefreshReason.Range`；
8. 窄宽度下允许 Chip 横向滚动或收敛为下拉菜单，不强制五个按钮同时显示；
9. 补充简体中文、英文资源与资源键完整性测试。

建议新增或修改：

```text
Services/AnalyticsService.cs
Services/DashboardAnalysisContext.cs
ViewModels/Dashboard/DashboardFilterViewModel.cs
ViewModels/DashboardViewModel.cs
Views/PlaytimeInsightsDashboardView.xaml
Localization/zh_CN.xaml
Localization/en_US.xaml
Tests/Program.cs
```

---

### P1-3：Hero 指标需要新的布局和展示模型

**路线图位置**：

- 第 76 行：两张加宽 Hero 卡；
- 第 84 行：数字和单位分离；
- 第 142–143 行：仅重构样式和 XAML 排版。

**当前实现**：

- 九张指标卡均由 `ResponsiveUniformPanel` 按等宽单元格排列；
- Panel 没有子项列跨度能力；
- 时长值已经在服务层格式化为完整本地化字符串，例如“4 小时 35 分钟”；
- 同比、环比对象只计算游玩秒数，并只显示在区间游玩时长卡；
- 区间会话数没有独立同比或环比模型。

**风险**：

1. 现有 Panel 无法让前两项跨列；
2. XAML 无法可靠地从中文、英文或其他本地化字符串中拆出数字和单位；
3. 会话数 Hero 卡若显示涨跌胶囊，将缺少真实数据；
4. 目标蓝图只展示五项 KPI，但当前有九项，路线图没有说明剩余四项的去向；
5. 直接删除或合并指标会改变信息架构和用户可见能力。

**修正建议**：

1. 第一轮不得静默删除九项指标；
2. 将两张 Hero 卡放入独立的响应式 `Grid`；
3. 保留 `ResponsiveUniformPanel` 承载其余七张 Tier 2 卡，避免扩展 Panel 的跨列算法；
4. Hero 区在宽屏显示两列，在窄屏显示一列；
5. 新增结构化时长展示模型，例如：

```text
DurationDisplayViewModel
  MajorValue
  MajorUnit
  MinorValue
  MinorUnit
  AutomationText
```

6. 无障碍名称继续使用完整本地化文本，不拼接仅供视觉使用的缩写；
7. 第一轮建议仅保留游玩时长的同比和环比；
8. 如果产品明确要求会话数比较，应单独新增：

```text
SessionCountPreviousPeriodComparison
SessionCountYearOverYearComparison
```

并扩展 `AdvancedAnalyticsSnapshot`、快照生成和回归测试，不得复用时长比较对象；
9. 在路线图中列出全部九项最终位置：

```text
Hero:
- 区间游玩时长
- 区间会话数

Tier 2:
- 活跃天数
- 平均会话
- 最长会话
- Playnite 累计总时长
- 最长连续游玩
- 当前连续游玩
- 异常会话
```

建议新增或修改：

```text
ViewModels/Dashboard/DashboardMetricsViewModel.cs
ViewModels/Dashboard/DashboardSnapshot.cs
Services/AnalyticsService.cs
Services/AdvancedAnalyticsService.cs（仅会话比较需要）
Views/PlaytimeInsightsDashboardView.xaml
Localization/zh_CN.xaml
Localization/en_US.xaml
Tests/Program.cs
```

---

### P1-4：跨侧边栏导航不属于 Drilldown ViewModel 的局部改动

**路线图位置**：

- 第 113、158 行：在下钻标题栏加入“在会话管理中查看全部”；
- 第 160–161 行：只为 Drilldown 增加清除命令。

**当前实现**：

- Dashboard 和会话管理是两个独立 `SidebarItem`；
- 它们由插件宿主 `PlaytimeInsights.cs` 分别创建；
- 当前本地 Playnite SDK 的 `IMainViewAPI` 没有公开切换到某个插件 SidebarItem 的方法；
- Dashboard ViewModel 不持有导航服务；
- 现有架构测试要求 ViewModel 不直接依赖具体 Window 或宿主 UI。

**风险**：

在 Drilldown ViewModel 中直接寻找控件、操作 Playnite 主窗口或模拟侧边栏点击，会破坏现有架构边界，
也容易随主题和 Playnite 版本失效。

**修正建议**：

1. “清除选中”可以直接实施：
   - `DashboardDrilldownViewModel.ResetSelection()` 已存在；
   - 在根 `DashboardViewModel` 暴露 `ClearDrilldownSelectionCommand`；
   - 命令执行后保持列表清空、可见性折叠和标题复位；
2. 第一批视觉重构暂缓“在会话管理中查看全部”；
3. 若后续保留该需求，先定义宿主级导航接口，例如：

```text
IDashboardNavigation
  TryOpenSessionManagement(SessionManagementFilter filter)
```

4. 由 `PlaytimeInsights.cs` 注入实现，ViewModel 只调用抽象；
5. 在无法可靠切换 SidebarItem 的 Playnite SDK 版本中，接口返回失败并隐藏按钮；
6. 不进行坐标点击、VisualTree 搜索或反射调用私有 Playnite API。

建议第一轮只修改：

```text
ViewModels/Dashboard/DashboardDrilldownViewModel.cs
ViewModels/DashboardViewModel.cs
Views/PlaytimeInsightsDashboardView.xaml
Localization/zh_CN.xaml
Localization/en_US.xaml
Tests/Program.cs
```

---

### P2-1：排行榜 Tab 化需要明确排名口径

**路线图位置**：

- 第 87–89 行：区间榜与累计榜合并为 Tab；
- 第 151 行：改为一个 `TabControl`。

**当前实现**：

- 区间榜支持游玩时长、会话次数、活跃天数、平均会话、最长会话等排名依据；
- 累计榜固定读取 Playnite `Game.Playtime` 并按累计时长排序；
- 两榜的 `ProgressPercent` 都是各自总时长占比；
- 当前两个榜同时显示，确实占用较多垂直空间。

**风险**：

当全局排名依据选择“会话次数”时，切换到累计榜仍按总时长排序。若 Tab 标题只写“历史霸榜”，
用户会自然认为它继承当前排名依据。

此外，Tab 能减少空间占用，但会隐藏另一榜，不能同时满足“方便对照”这一原始目标。

**修正建议**：

1. 将第二个 Tab 明确命名为“Playnite 累计时长”；
2. 在累计 Tab 内展示固定口径说明，不继承区间榜排名依据；
3. 当累计 Tab 激活时，可将顶部“排名依据”控件标注为“仅影响本期榜”，不应直接禁用整个筛选区；
4. 将设计目标从“便于对照”改为“减少垂直占用并保留快速切换”；
5. Tab 状态属于纯 View 状态，不触发数据库读取或 Dashboard 刷新；
6. 保持两个列表均已存在于当前快照中，切换只改变可见内容；
7. 为 Tab 增加键盘焦点、AutomationProperties 和明确选中状态。

---

### P2-2：排行榜能量条可行，但奖牌样式需语义化

**当前实现**：

- `GameRankingViewModel.ProgressPercent` 已存在；
- 当前模板使用全高、0.12 透明度的背景 ProgressBar；
- 前三名已有金、银、铜画刷和边框样式。

**结论**：

4 DIP 细能量条基本可以只通过 XAML 完成，属于低风险高收益项。无需修改排行榜数据模型。

**修正建议**：

1. 保留 `ProgressBar` 及其 `ProgressPercent` 绑定；
2. 把进度条放在名称与说明下方，高度为 4 DIP；
3. 底轨使用主题边框或文本画刷的低透明度版本；
4. 指示条使用插件语义强调色；
5. 不使用持续发光或大范围阴影，避免在排行榜重复行中增加渲染成本；
6. 金银铜颜色定义为插件命名资源，不散落在模板中；
7. Windows 高对比度模式下回退到主题边框和文字颜色；
8. 保留当前 Tooltip 中的总时长占比说明。

---

### P2-3：星期和 24 小时联动已经存在

**路线图位置**：

- 第 105–106 行：建立星期柱状图与 24 小时柱状图联动。

**当前实现**：

- 星期按钮已绑定 `SelectWeekdayCommand`；
- `DashboardDistributionViewModel` 已维护 `selectedWeekdayIndex`；
- 点击星期会重新生成对应星期的 24 小时分布；
- 再次点击会恢复全部星期；
- 标题和 Automation 文本已经随状态更新；
- 选中按钮已有状态、边框和动画反馈。

**修正建议**：

将此任务改名为：

> 优化既有星期筛选联动的视觉反馈、过渡和当前筛选提示。

不得重复实现数据过滤。新增工作应仅限于：

- 更清晰的选中状态；
- 小于 200 ms 且可禁用的过渡；
- 保留键盘操作和自动化名称；
- 不在动画中触发数据重算；
- 不把 24 小时数据变化误写成新的业务联动。

---

### P2-4：下钻卡片化必须保留虚拟化

**路线图位置**：

- 第 110–111 行：GridView 替换为卡片列表和来源 Tag；
- 第 174 行：保持 Recycling 虚拟化。

**当前实现**：

- `ListView` 使用 `VirtualizingStackPanel`；
- `ScrollViewer.CanContentScroll="True"`；
- `VirtualizingPanel.IsVirtualizing="True"`；
- `VirtualizingPanel.VirtualizationMode="Recycling"`；
- 数据按 100 条分页。

**可行方案**：

保留 `ListView`，移除 `ListView.View/GridView`，改用 `ItemTemplate` 和 `ItemContainerStyle`。每条会话使用
轻量 Border/Grid 卡片，不切换为普通 `ItemsControl` 或外层 StackPanel。

**来源 Tag 的遗漏依赖**：

会话管理页的颜色触发器绑定 `SessionSource` 枚举，而 Dashboard 的 `SessionDetailViewModel` 目前只有
本地化后的 `SourceText`。

**修正建议**：

1. 在 `SessionDetailViewModel` 中增加 `SessionSource Source`；
2. 在 `AnalyticsService.CreateSessionDetails` 投影真实枚举；
3. 将来源 Tag 样式抽到共享 ResourceDictionary，或至少共享颜色资源和触发规则；
4. 不根据 `SourceText` 字符串选择颜色；
5. 卡片容器不得包含重型 Effect；
6. 保留分页、虚拟化和滚轮边界转交；
7. 新增大量会话下的容器复用测试或运行时检查。

建议新增或修改：

```text
ViewModels/Dashboard/SessionDetailViewModel.cs
Services/AnalyticsService.cs
Views/PlaytimeInsightsDashboardView.xaml
Views/SessionManagementView.xaml
App.xaml 或新增共享 ResourceDictionary
Tests/Program.cs
```

---

### P2-5：主题要求与固定色值需要重新定义边界

**路线图冲突**：

- 验收要求第三方主题下“无硬编码色块”；
- 同时指定蓝、紫、橙、红以及金银铜固定颜色；
- 趋势线和现有星期选择也已经使用插件蓝紫视觉语言。

**修正建议**：

将颜色分为两类：

#### 必须使用 Playnite 主题资源

- 页面和面板背景；
- 主文字、次要文字；
- 普通边框和分隔线；
- Popup/Tooltip 背景；
- 禁用和空状态基础颜色。

推荐优先使用：

```text
TextBrush
ControlBackgroundBrush
PanelSeparatorBrush
PopupBackgroundBrush
GlyphBrush
NormalBrush（仅在目标桌面主题验证存在时）
```

#### 允许使用插件语义色

- 趋势增加/减少；
- 图表系列；
- 来源类型；
- 排名金银铜；
- 异常警示。

要求：

1. 所有插件色集中为命名资源；
2. 半透明底座必须叠加在主题背景上；
3. 不把固定强调色用于正文；
4. 高对比度模式提供主题画刷回退；
5. 不以颜色作为唯一状态信息；
6. 手工检查浅色主题下的文字、边框和底座对比度。

热力图空单元格不建议直接给整个单元格设置 `Opacity="0.06"`，因为这会同时降低边框和子元素。
可以使用一个独立的背景 Border：

```xml
<Border Background="{DynamicResource TextBrush}"
        Opacity="0.06"
        IsHitTestVisible="False" />
```

外层 Border 继续负责边框和命中区域，活动热度层单独叠加。

---

### P2-6：趋势图主题化范围应补完整

**当前实现**：

- 网格线已读取 `PanelSeparatorBrush`；
-文字已读取 `TextBrush`；
- Tooltip 背景、Tooltip 边框、十字线、节点边框仍有硬编码色；
- 趋势线蓝紫渐变属于插件图表语义色。

**修正建议**：

1. Tooltip 背景读取 `PopupBackgroundBrush`；
2. Tooltip 普通边框读取 `PanelSeparatorBrush`；
3. 十字线优先使用 `GlyphBrush` 或命名图表强调色；
4. 节点外圈不要固定使用 `Brushes.White`，改用主题背景或分隔线；
5. `ResolveBrush` 保留合理 fallback，避免第三方主题缺少资源时不可见；
6. 日/周/月控件移动到图表标题栏时继续绑定现有
   `SelectedAggregationOption`，不得建立第二套粒度状态；
7. 保留 `Auto` 和 `Year` 能力。若 Segmented Control 只展示日/周/月，应提供 Auto/Year 的溢出菜单；
8. 粒度切换继续只触发 `DashboardRefreshReason.Aggregation`，不得重载数据库。

---

### P2-7：折叠高级筛选需要定义状态与可发现性

**路线图位置**：

- 第 72 行：来源、平台、类型等高级筛选默认折叠。

**当前实现**：

- 元数据维度和值筛选处于同一 WrapPanel；
- 选中筛选会持久保留在缓存 Dashboard ViewModel 中；
- 页面关闭后重新进入会保留筛选和视图状态。

**风险**：

若已有元数据筛选生效但折叠区关闭，用户可能看不到结果为什么减少。默认折叠也可能降低过滤功能的
可发现性。

**修正建议**：

1. 折叠状态属于 View 状态，不触发数据刷新；
2. 有筛选生效时，折叠按钮必须显示摘要或计数；
3. 重新进入缓存 Dashboard 时保留折叠状态；
4. 在 400–640 DIP 下优先折叠，在宽屏下可默认展开；
5. 不把时间范围和粒度放入高级筛选；
6. 为折叠按钮补充 AutomationProperties、键盘操作和明确展开状态。

---

## 4. 逐模块可行性评级

| 模块 | 可行性 | 风险 | 结论 |
|---|---|---|---|
| 热力图空格主题化 | 高 | 低 | 可直接实施 |
| 趋势 Tooltip 主题化 | 高 | 低 | 可直接实施 |
| 排行榜 4 DIP 能量条 | 高 | 低 | 可直接实施 |
| 金银铜徽章优化 | 高 | 中 | 语义色集中后实施 |
| 清除下钻选择 | 高 | 低 | 已有 ResetSelection，只需命令接线 |
| 排行榜 Tab 化 | 高 | 中 | 先澄清排名口径 |
| 快捷时间 Chip | 中 | 高 | 需扩展日期枚举和范围计算 |
| 高级筛选折叠 | 高 | 中 | 需增加生效摘要 |
| Hero KPI 卡 | 中 | 高 | 需独立布局和结构化显示模型 |
| 会话数同比/环比 | 中 | 高 | 当前没有数据，需要新增统计 |
| 下钻卡片化 | 高 | 中 | 保留 ListView Recycling |
| 来源彩色 Tag | 高 | 中 | 需透传 SessionSource 并共享样式 |
| 62%/38% 双栏 | 中 | 高 | 需重做右栏内容与断点机制 |
| 星期与小时联动 | 已完成 | 低 | 只做视觉反馈增强 |
| 跨页打开会话管理 | 低 | 高 | SDK 无直接 Sidebar 导航，建议延期 |

---

## 5. 建议后的实施阶段

### 阶段 0：冻结信息架构和验收口径，2–3 天

1. 明确九张指标卡的最终位置；
2. 明确全时段的数据语义；
3. 明确累计榜固定按 Playnite 总时长；
4. 测量主栏、右栏、异常卡和排行榜的最低宽度；
5. 以内容区 DIP 确定双栏断点；
6. 输出深色、浅色、Seaside 和高对比度的静态视觉目标；
7. 确认是否延期跨页导航和会话数同比。

完成条件：所有功能都有数据口径、布局去向和验收方式。

### 阶段 1：主题与低风险视觉修复，3–4 天

1. 热力图空单元格主题化；
2. Tooltip、边框、十字线和节点外圈主题化；
3. 排行榜 4 DIP 能量条；
4. 金银铜颜色资源集中；
5. 指标图标底座样式；
6. 更新被旧颜色和旧进度条锁定的静态测试。

完成条件：深色、浅色和 Seaside 无明显固定暗色块，现有逻辑回归全部通过。

### 阶段 2：排行榜整合，2–3 天

1. 新增区间榜/Playnite 累计时长 Tab；
2. 累计榜显示固定口径说明；
3. Tab 切换不触发刷新；
4. 保留键盘、焦点和 Automation 语义；
5. 宽窄模式下检查排行榜最小宽度和长游戏名。

完成条件：Tab 口径无歧义，切换无数据重载。

### 阶段 3：时间 Chip 与筛选工具栏，4–5 天

1. 新增日期范围枚举和解析；
2. 实现 AllSessions 起点；
3. 补充自动聚合规则；
4. Chip 和 ComboBox 共用状态；
5. 高级筛选折叠与生效摘要；
6. 补充双语资源和范围边界测试。

完成条件：每种范围只刷新一次，范围标题、聚合和数据结果一致。

### 阶段 4：Hero 指标分级，4–6 天

1. 新增 Hero Grid；
2. Tier 2 继续使用 `ResponsiveUniformPanel`；
3. 增加结构化时长展示；
4. 保留九项指标；
5. 决定是否增加会话数同比；
6. 检查长英文、零值和极大值。

完成条件：Hero 层级明确，所有指标可访问，不通过字符串解析拆单位。

### 阶段 5：主区域双栏重构，4–5 天

1. 增加 View 层布局状态；
2. 重排趋势、分布、热力图、排行榜、异常和下钻；
3. 重做右栏异常模板；
4. 单栏顺序保持完整；
5. 验证内部横向滚动和外层纵向滚动；
6. 连续拖动窗口时不触发数据刷新。

完成条件：断点两侧无重叠、跳动、不可访问模块或异常横向滚动。

### 阶段 6：下钻卡片和闭环，3–4 天

1. 保留虚拟化 ListView；
2. 使用卡片 ItemTemplate；
3. 透传 SessionSource；
4. 复用来源 Tag 资源；
5. 增加清除命令；
6. 跨页导航延期或由单独宿主导航设计实现。

完成条件：大列表仍使用 Recycling，清除操作完整复位状态。

### 阶段 7：自动回归与客户端视觉矩阵，4–5 天

1. 更新所有 XAML 静态护栏；
2. 新增范围、断点状态、Tab、命令和虚拟化测试；
3. 运行完整 Release 构建和回归；
4. 执行语言、主题、DPI、宽度和数据状态矩阵；
5. 保存重构后截图和客户端验收记录。

---

## 6. 修订后的文件影响清单

路线图当前列出的文件不足。建议至少评估：

```text
Views/PlaytimeInsightsDashboardView.xaml
Views/PlaytimeInsightsDashboardView.xaml.cs
Views/SessionManagementView.xaml
Controls/AdaptiveTrendChart.cs
Services/AnalyticsService.cs
Services/AdvancedAnalyticsService.cs
Services/DashboardAnalysisContext.cs
Services/SessionQueryService.cs
ViewModels/DashboardViewModel.cs
ViewModels/Dashboard/DashboardFilterViewModel.cs
ViewModels/Dashboard/DashboardMetricsViewModel.cs
ViewModels/Dashboard/DashboardDistributionViewModel.cs
ViewModels/Dashboard/DashboardDrilldownViewModel.cs
ViewModels/Dashboard/DashboardSnapshot.cs
ViewModels/Dashboard/SessionDetailViewModel.cs
Localization/zh_CN.xaml
Localization/en_US.xaml
App.xaml 或新增共享 ResourceDictionary
Tests/Program.cs
docs/CLIENT_ACCEPTANCE_1.0.0.md
docs/IMPLEMENTATION_STATUS.md
```

仅在保留跨页导航时，再增加宿主导航接口及 `PlaytimeInsights.cs` 改动。

---

## 7. 修订后的验收标准

### 7.1 自动化验收

1. Release 构建 0 警告、0 错误；
2. 全部既有回归和新增回归通过；
3. Last7Days、Last30Days、ThisYear、AllSessions、Custom 日期边界正确；
4. 闰年、空数据、反向自定义日期和单日数据行为明确；
5. Chip 一次点击只产生一次 Range 刷新；
6. Tab、筛选折叠和窗口布局切换不触发 DataReload；
7. 九张指标卡均保留且只出现一次；
8. 下钻仍包含 `VirtualizingStackPanel`、`CanContentScroll=True` 和 Recycling；
9. 来源颜色绑定 `SessionSource`，不绑定本地化字符串；
10. Tooltip 背景和普通边框不再使用固定暗色；
11. XAML 不再锁定旧全高进度条、旧 GridView 或旧热力图空格颜色；
12. 10 万会话分析和 schema 4 加载继续满足现有发布预算。

### 7.2 客户端视觉矩阵

语言：

```text
简体中文
English
```

主题：

```text
Playnite 默认深色
Playnite 默认浅色
Seaside
至少一个其他第三方桌面主题
Windows 高对比度
```

DPI：

```text
100%
125%
150%
175%
200%
```

Dashboard 内容区宽度：

```text
400 DIP
640 DIP
900 DIP
双栏断点前 1 DIP
双栏断点
1600 DIP
2400 DIP
```

数据状态：

```text
无会话
普通数据
长英文游戏名
极大时长和四位以上会话数
同比/环比为新增、上涨、下降、持平
异常列表隐藏
异常列表显示
下钻无结果
下钻超过 100 条
累计榜少于和多于 10 个游戏
```

通过条件：

1. 无关键文字截断、重叠或不可访问；
2. 不因断点切换丢失模块或滚动位置；
3. 横向滚动只出现在热力图等设计允许的局部区域；
4. 当前筛选、Tab 和下钻状态始终可见；
5. 所有交互可通过键盘操作，并有可见焦点；
6. 高对比度模式不依赖固定颜色表达唯一语义；
7. 主题切换后 Tooltip、热力图、图标底座和 Tag 可读；
8. 页面连续缩放和拖动不触发数据库重载；
9. Tab 和清除下钻操作无明显阻塞；
10. 通过前后截图保留验收证据。

### 7.3 性能标准修订

删除无法直接证明的“UI 线程保持 60fps”表述，替换为：

1. 现有 10 万会话自动性能预算不得退化；
2. Tab、折叠和布局模式切换不得触发 DataReload；
3. 窗口连续拖动时不得同步重新读取游戏库或会话仓库；
4. 新增视觉 Effect 不得出现在长列表的每一行；
5. 若要声明帧率，必须使用明确的 WPF Performance Suite、ETW 或等价工具记录，而不是目测。

---

## 8. 建议对原路线图直接修订的条目

| 原条目 | 建议修订 |
|---|---|
| 1024px 双栏断点 | 改为基于内容区 DIP 和模块最小宽度确定，候选 1200–1280 DIP |
| Chips 只改 XAML/FilterViewModel | 增加日期枚举、范围解析、分析上下文、国际化和测试 |
| 两张 Hero 都显示同比/环比 | 第一轮只保留时长比较；会话比较独立评估 |
| 数字和单位直接拆分 | 新增结构化展示模型，不解析格式化字符串 |
| 星期与 24 小时建立联动 | 改为优化已有联动的反馈 |
| Drilldown 增加清除命令 | 保留，属于低风险项 |
| Drilldown 跳转会话管理 | 延期，先设计宿主导航接口 |
| 来源 Tag 只改 XAML | 增加 SessionSource 透传和共享样式 |
| 双榜便于对照 | 改为减少垂直占用并快速切换 |
| 历史霸榜 | 改为 Playnite 累计时长，明确固定口径 |
| 第三方主题无硬编码颜色 | 区分主题基础色与插件语义色 |
| 400px–3840px | 改为内容区 DIP 测试矩阵 |
| 保持 60fps | 改为可测量的数据刷新、布局和长帧标准 |
| 2 天完成完整主题/DPI验证 | 调整为 4–5 天并保存截图证据 |

---

## 9. 最终建议

路线图应先升级到 v1.1，再进入代码实施。v1.1 至少应完成以下四项：

1. 明确九项指标、两个榜单和全时段的业务语义；
2. 写明内容区 DIP 断点和 View 层响应式实现；
3. 补齐完整文件影响、国际化和自动化测试清单；
4. 删除已完成任务，并延期当前 SDK 无法可靠支持的跨侧边栏导航。

完成上述修订后，可以优先启动主题修复、Tooltip、4 DIP 能量条和清除下钻命令。这些工作风险低、
收益明确，也能为后续结构重排建立稳定视觉基础。

---

## 10. v1.1 复审结论与实施条件

### 10.1 复审判定

**判定：有条件达到可实施标准。**

v1.1 已关闭原审查中的路线图级阻塞：九项指标去向完整、日期预设和 `AllSessions` 语义明确、排行榜固定口径明确、下钻虚拟化和跨侧边栏导航边界明确、主题色边界与可测量验收矩阵已补齐。剩余问题属于实现细节，不需要再次回退到产品或架构设计阶段。

### 10.2 已锁定的修正项

1. 主区域使用 `AdaptiveDashboardPanel`，进入/退出双栏阈值固定为 1200/1160 DIP，左右栏独立纵向堆叠；不再使用根 Grid 的 `IsWideLayout` Trigger 重排方案。
2. `AllSessions` 起点在 `AnalyticsService.CreateSnapshotWithContext` 物化 `sessionList` 后、创建 `DashboardAnalysisContext` 前计算；有效会话为 `!IsDeleted && ElapsedSeconds > 0`，日期取 `GetStartedLocalDate()`。
3. `AllSessions` 不创建或扫描 previous/year 比较范围，比较对象为 `null`，比较容器折叠。
4. 共享视觉资源创建在 `Resources/PlaytimeInsightsVisualResources.xaml`，由 Dashboard 和 Session Management 两个 View 显式合并；不依赖或修改 `App.xaml`。
5. 筛选折叠和排行榜 Tab 均为 View 状态。`DashboardFilterViewModel` 只公开激活筛选数量、摘要和可见性。
6. 两张 Hero 与七张 Tier 2 共九项指标完整保留；现有 `ResponsiveUniformPanel` 不修改且只承载七张 Tier 2。
7. 下钻继续使用 `ListView`、`CanContentScroll=True` 和 `VirtualizationMode=Recycling`；根 `DashboardViewModel` 调用现有 `Drilldown.ResetSelection()` 提供清除命令。
8. 第一轮继续排除跨侧边栏导航，不修改 `PlaytimeInsights.cs`，不访问 Playnite 私有 UI。

### 10.3 实施入口

详细文件边界、接口签名、TDD 步骤、提交粒度和 Gate A-D 验收顺序，以 `docs/superpowers/plans/2026-08-14-dashboard-visual-refactor-implementation.md` 为实施补充规范。若路线图与该补充规范在实现机制上出现差异，以补充规范中已冻结的接口和约束为准。
