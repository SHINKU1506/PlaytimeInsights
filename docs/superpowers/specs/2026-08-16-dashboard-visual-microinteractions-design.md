# Dashboard 视觉微交互与刷新过渡技术路线

日期：2026-08-16
状态：设计完成，待用户确认后拆分实施计划
适用分支：codex/dashboard-visual-refactor

关联文档：

- docs/superpowers/plans/2026-08-14-dashboard-visual-refactor-implementation.md；
- docs/superpowers/specs/2026-08-13-responsive-metrics-visual-foundation-design.md；
- docs/CLIENT_ACCEPTANCE_1.0.0.md。

## 1. 目标与结论

本路线在不改变 Dashboard 数据口径、选择性刷新边界、响应式布局、虚拟化和主题对比度契约的前提下，补足两类当前可感知但尚未统一的反馈：

1. 排行榜和下钻会话等可点击列表项，在 Hover 时提供克制的可操作确认；
2. 首次加载及数据刷新完成后，只有内容确实被替换的模块出现短促、可跳过的淡入回位。

不实施重做图标光晕、指标数字滚动和自绘趋势图路径生长。当前八张指标卡已经有 32×32 圆角图标底座，并通过 MetricDuration、MetricSession、MetricActivity、MetricAnomaly 四组前景/背景画刷提供蓝、紫、琥珀和红粉语义；重复调整的视觉增量很小。指标文本包含时长、计数、百分比、空状态与本地化内容，数字滚动会增加读取延迟和屏幕阅读器重复播报风险。AdaptiveTrendChart 为 DrawingContext 自绘控件，曲线生长需要逐帧重绘、取消和连续刷新治理，收益不足以覆盖风险。

预期结果是提高可点击条目的辨识度和刷新后的内容更新感，而不是将 Dashboard 改成高频动效界面。

## 2. 当前基线与边界

### 2.1 已有基础

现有资源和 XAML 已具备：

| 范围 | 当前实现 |
| --- | --- |
| 指标图标 | 8 张指标卡均使用 32×32 圆角彩色底座；时长蓝、会话紫、活跃/连续琥珀、异常红粉 |
| 排行榜 | Hover 背景、前三名金银铜渐变光带、底层比例能量条 |
| 下钻会话 | Hover 背景、虚拟化 ListView、来源标签 |
| 星期筛选 | 选中时 120ms 上移与阴影 |
| 工具按钮 | 刷新、帮助、关闭均有 Hover、Pressed、Focus 状态 |
| 布局 | ResponsiveUniformPanel 指标网格、AdaptiveDashboardPanel 宽窄布局滞回 |

新工作必须增量扩展，不能替换已验证的图标光晕、排名渐变、下钻高度修复或星期按钮动效。

### 2.2 不变量

- 不引入第三方 UI、动画或图表库；保持 C# 7.3、.NET Framework 4.6.2、WPF；
- 不改变 DashboardRefreshPlan 的数据读取、筛选、分析和投影逻辑；
- 动画不得改变 DesiredSize、ActualWidth、ActualHeight、Margin、Padding、Height、Width 或面板测量结果；
- 不在虚拟化列表项中增加循环动画、计时器或新的动态 DropShadowEffect；
- 纯展示指标卡不增加 Hover 上浮，避免暗示它们可点击；
- 不叠加星期按钮现有选中动效；
- 下钻模块显示仍由既有 BringIntoView 处理，不加入入口动画；
- 系统关闭客户端动画时，所有新视觉效果直接落到最终状态，键盘焦点保持清晰。

## 3. 方案选择

采用“列表项 Hover 表层 + 单一刷新完成信号 + 模块级渲染属性动画”。

| 候选方案 | 效果 | 风险/成本 | 决策 |
| --- | --- | --- | --- |
| 调整图标光晕 | 很低，现有底座已覆盖 | 可能破坏异常语义 | 不实施 |
| 全部指标卡 Hover 上浮 | 视觉强 | 错误传达可点击性，窄屏密度过高 | 不实施 |
| 可点击列表项上浮和描边 | 高，符合用户意图 | 需验证虚拟化容器复位 | 实施 |
| 动态 DropShadowEffect | 中等 | 模糊效果在滚动和高 DPI 下昂贵 | 不实施 |
| 每个属性变更播放动画 | 明显但杂乱 | 全量 Apply 会造成重复闪烁 | 不实施 |
| 刷新完成后模块级淡入/回位 | 中等，信息稳定后才反馈 | 需要唯一完成信号 | 实施 |
| 数字滚动或趋势路径生长 | 中等 | 本地化、重绘、取消与可达性复杂 | 延后 |

## 4. 视觉规范

### 4.1 可点击列表项 Hover

仅适用于现有可选择或可下钻的排行榜条目、下钻会话条目。Hover 采用表层覆盖，不重置条目原有背景，确保前三名渐变、排行榜比例条和下钻来源语义保留。

| 属性 | 排行榜 | 下钻会话 | 说明 |
| --- | --- | --- | --- |
| 位移 | Y: 0 到 -1 DIP，120ms | Y: 0 到 -1 DIP，100ms | 仅改变 RenderTransform |
| 覆盖层 | #0D4A90E2 | #0F60A5FA | 覆盖于原始背景上方 |
| 描边 | #664A90E2，1 DIP | #4D60A5FA，1 DIP | 透明边框淡入，不改变布局 |
| 离开 | 100 到 120ms 回位 | 100ms 回位 | 无弹簧、无过冲 |
| 焦点 | 保留现有键盘焦点边框 | 保留现有 Item/ListView 焦点 | 动效不能替代焦点反馈 |

新增画刷统一放入 Resources/PlaytimeInsightsVisualResources.xaml：

    RankingItemHoverOverlayBrush
    RankingItemHoverBorderBrush
    DrilldownSessionHoverOverlayBrush
    DrilldownSessionHoverBorderBrush

不得在 DataTemplate 中复制硬编码色值。固定色仍仅服务于固定深色表面的语义反馈，不依赖浅色主题中的 TextBrush。

### 4.2 刷新后的模块级入场

动画以模块为单位，不以每个文本、排名行或图表节点为单位。

| 刷新模式 | 目标 | 过渡 | 延迟 |
| --- | --- | --- | --- |
| FullAnalysis | 指标区、趋势、排行、分布、异常 | Opacity 0 到 1；Y 5/6 到 0；160ms | 0ms、24ms、48ms 三档 |
| TrendOnly | 趋势模块 | Opacity 0 到 1；Y 4 到 0；140ms | 0ms |
| RankingOnly | 排行模块 | Opacity 0 到 1；Y 4 到 0；140ms | 0ms |
| 首次 Loaded | 同 FullAnalysis | 同 FullAnalysis | 同 FullAnalysis |
| 下钻选择/清除 | 无 | 保持 BringIntoView | 无 |

使用简单 CubicEase 或 DecelerationRatio，不使用弹跳、缩放、旋转或无限循环。每次启动均使用 SnapshotAndReplace，避免快速刷新堆积动画。

## 5. 技术架构

### 5.1 单一刷新完成信号

DashboardViewModel 当前会通过 ApplyFullAnalysis、ApplyTrendRefresh、ApplyRankingRefresh 更新不同子 ViewModel。View 不能监听 Metrics 或 Distribution 的每个 PropertyChanged 启动动效，因为一次完整 Apply 会连续发布很多属性，造成重复闪烁。

在 DashboardViewModel 新增只读展示协调状态：

    DashboardPresentationTransition PresentationTransition
    int PresentationRevision

枚举只包含 None、Full、Trend、Ranking。每个 Apply 路径成功完成后，RefreshCore 调用私有 PublishPresentationUpdate：

1. 将实际成功应用的刷新模式映射为过渡类型；
2. 先通知 PresentationTransition；
3. PresentationRevision 加一；
4. 最后通知 PresentationRevision。

筛选不完整、刷新守卫拒绝、分析失败或未真正应用投影的路径不得递增版本。版本号是“全部可见数据已替换”的唯一边界，不是“刷新已开始”的信号。

### 5.2 View 订阅与过期抑制

PlaytimeInsightsDashboardView 在 DataContextChanged 时解除旧 DashboardViewModel.PropertyChanged，再订阅新实例；只监听 PresentationRevision。收到事件后：

1. 捕获 ViewModel 引用、版本号和过渡类型；
2. 以 DispatcherPriority.Loaded 延迟到当前绑定、测量和排列完成；
3. 回调执行前复核 DataContext 未变且版本号仍一致；
4. 不一致时丢弃，保证快速连续切换仅表现最后一次有效结果。

code-behind 只调度 Visual Tree 动画，不读取会话、不调用命令、不修改业务数据，职责与现有鼠标滚轮转交和下钻 BringIntoView 适配一致。

### 5.3 动画宿主

XAML 为以下元素设置 RenderTransformOrigin 和 TranslateTransform Y=0：

| 宿主名 | 当前元素 | 用途 |
| --- | --- | --- |
| MetricCardsHost | ResponsiveUniformPanel | FullAnalysis 指标整体淡入 |
| TrendModule | 趋势 Border | FullAnalysis/TrendOnly |
| RankingModule | 排行 Border | FullAnalysis/RankingOnly |
| DistributionModule | 分布 Border | FullAnalysis |
| AnomalyModule | 异常 Border | FullAnalysis |

动画工具只修改 Opacity 与 TranslateTransform.Y；不得动画布局属性。基值保持 Opacity=1、Y=0，动画使用 FillBehavior.Stop，以免结束后冻结局部值或阻断后续样式更新。

### 5.4 Hover 表层

GameRankingItemTemplate 与下钻 ListView.ItemTemplate 各增加一个最后绘制、IsHitTestVisible=False 的覆盖 Border。覆盖层根据命名模板根元素的 IsMouseOver 显示半透明背景与描边。根元素继续保留：

- RankingGold/Silver/Bronze 渐变；
- 比例进度背景；
- DrilldownSessionItem 原背景和来源标签。

根元素的 1 DIP 位移优先用样式 EventTrigger/Storyboard 实现。阶段 0 必须确认容器回收、卸载和快速移入移出后会可靠回到 Y=0。仅当样式级 Storyboard 在 Playnite 宿主中无法复位时，才新增最小附加行为 HoverMotion。该回退行为只允许 Enabled、LiftY、Duration 三个属性，并在 MouseLeave、Unloaded、禁用时立即复位；不创建定时器，不处理业务命令。

### 5.5 减弱动效与无障碍

播放前读取 SystemParameters.ClientAreaAnimation：

- true：执行对应短动画；
- false：停止现有时钟并直接落到 Opacity=1、Y=0；
- 两种状态下，AutomationProperties、Tooltip、命令可用性和键盘焦点均保持不变。

首期不增加插件设置。若实机验收显示用户需要独立开关，再单独设计设置项与迁移。

## 6. 文件范围

| 文件 | 动作 | 责任 |
| --- | --- | --- |
| Resources/PlaytimeInsightsVisualResources.xaml | 修改 | Hover 覆盖层与描边语义画刷 |
| ViewModels/DashboardViewModel.cs | 修改 | 过渡枚举、版本号与成功应用后的唯一发布点 |
| ViewModels/Dashboard/DashboardRefreshPlan.cs | 仅验证 | 保持刷新模式映射，不扩大刷新范围 |
| Views/PlaytimeInsightsDashboardView.xaml | 修改 | 入口宿主变换、排行/下钻覆盖层、Hover 样式 |
| Views/PlaytimeInsightsDashboardView.xaml.cs | 修改 | DataContext 订阅、版本过滤、动画调度 |
| Tests/Program.cs | 修改 | 刷新发布、计划映射、XAML、回收复位、减弱动效护栏 |
| Controls/HoverMotion.cs | 条件新增 | 仅在样式 Storyboard 无法稳定复位时使用 |
| Controls/AdaptiveTrendChart.cs | 不改 | 本路线不做图表路径生长 |
| Controls/ResponsiveUniformPanel.cs | 不改 | 动效不得影响测量/排列 |
| Controls/AdaptiveDashboardPanel.cs | 不改 | 动效不得影响宽窄布局 |

本路线不增加可见文案，因此不修改本地化资源。

## 7. 分阶段实施路线

### 阶段 0：基线与 Hover 方式验证

1. 运行 Release 插件构建、测试构建、完整回归、git diff --check；
2. STA WPF 测试验证 Border 样式动画只改变 TranslateTransform.Y，离开/卸载后回到 0；
3. 使用虚拟化 ListView 滚动回收容器，确认无残留 Y=-1；
4. 失败才启用 HoverMotion 回退。

退出条件：确定一种可在虚拟化容器中可靠复位、且无动态阴影的实现。

### 阶段 1：资源与列表 Hover

1. 添加四个 Hover 画刷；
2. 为排行榜和下钻模板加入命名根元素与透明覆盖层；
3. 添加 100 到 120ms 的 1 DIP 上移/回位和描边/覆盖反馈；
4. 验证前三名、比例条、来源标签和 Tooltip 未受覆盖层影响；
5. 验证覆盖层不拦截鼠标与键盘交互。

### 阶段 2：刷新完成信号与纯计划

1. 新增 DashboardPresentationTransition、PresentationTransition、PresentationRevision；
2. 仅在一个 Apply 路径成功完成后发布一次版本；
3. 抽取纯 DashboardEntrancePlan：输入过渡类型和减弱动效状态，输出目标、延迟、时长、位移；
4. 为 Full、Trend、Ranking、None、关闭动画、连续版本和失败路径建立回归；
5. 确认既有 TrendOnly/RankingOnly 测试继续证明它们不回退为全量分析。

### 阶段 3：View 调度与模块入场

1. 增加五个入口宿主的 RenderTransform；
2. 安全订阅和解除订阅 DataContext；
3. 通过 DispatcherPriority.Loaded 调度，并按版本丢弃过期回调；
4. 实现 ClientAreaAnimation=false 的直接最终状态；
5. 下钻模块不加入入场计划，保留现有 BringIntoView。

### 阶段 4：回归、性能与实机矩阵

1. Release 两个构建均为 0 warning、0 error；
2. 完整回归通过；
3. 10 万会话分析和 schema 4 加载保持既有预算；
4. 在 zh_CN/en_US、深浅/高对比主题、100 到 200% DPI、400/640/900/1160/1200/1600 DIP 下复验；
5. 使用空数据、普通数据、前三名、长英文名、100+ 下钻、连续范围切换和关闭系统动画验收；
6. 通过后才部署本地供人工确认。

## 8. 自动化与人工验收

### 自动化契约

- Full 刷新发布一次 Full 版本；TrendOnly 只发布 Trend；RankingOnly 只发布 Ranking；
- 刷新守卫拒绝、筛选不完整和失败路径版本不变；
- Full 入口计划包含五个目标，Trend/Ranking 各只有一个；关闭动画时计划直接输出最终状态；
- 五个入口宿主各恰好一份，且只包含 Opacity/TranslateTransform 入口属性；
- 两个列表模板各恰好一个 IsHitTestVisible=False 覆盖层，且引用共享资源；
- 排名前三名渐变、虚拟化 Recycling、AdaptiveTrendChart 无动画进度属性、星期按钮既有动效均保持；
- 回收/卸载后的列表项 TranslateTransform.Y 等于 0。

### 人工问题清单

1. Hover 前三名排行时，金银铜光带是否仍清楚？
2. 快速横扫或滚动长列表后，是否有条目停留在上移位置？
3. 7 天、30 天、全部连续切换时，是否只对最终内容播放一次淡入？
4. 聚合或排行切换是否仅反馈对应模块，而非整页闪动？
5. 下钻是否仍立即滚到会话卡，且列表高度正常展开？
6. 系统关闭动画时，结果是否即时可读、焦点是否清晰？
7. 125%、150%、200% DPI 下的 1 DIP 描边是否模糊、闪烁或改变行高？

## 9. 风险与缓解

| 风险 | 缓解 |
| --- | --- |
| 虚拟化容器携带旧位移 | MouseLeave/Unloaded/禁用复位；STA 回收测试；必要时使用 HoverMotion |
| 快速刷新回放旧动画 | Dispatcher 回调复核 PresentationRevision，不一致即丢弃 |
| Hover 覆盖前三名光带 | 使用最后绘制的透明覆盖层，不改根背景 |
| 性能下降 | 禁止新的动态阴影；最多五个模块和可见列表项参与短动画 |
| 动效不适或影响可达性 | 遵循 ClientAreaAnimation；焦点与无障碍属性不依赖动画 |
| 下钻定位回归 | 下钻不纳入刷新入口，保留 BringIntoView |

## 10. 明确延后项与完成定义

延后项：趋势路径生长、数值滚动、用户独立动效开关、主题级背景渐变重构，以及将指标卡升级为可点击入口。

完成条件：所有自动化契约、Release 构建、完整回归、性能预算和虚拟化回收验证通过；视觉矩阵不存在布局、排名光带、下钻高度、主题对比度或键盘焦点回归；本地部署后人工确认动效足够克制且没有延迟感。
