# 聚合趋势图原子刷新修复设计

日期：2026-08-12

状态：已实施，待客户端复验

适用分支：`refactor/architecture-preparation`

## 问题与根因

用户修改“图表粒度”后，新的统计已经生成，但聚合趋势图会短暂保留旧图形；图表重新进入视口、
发生尺寸变化或鼠标经过后才稳定显示新图。该现象由数据通知与自绘失效机制不匹配造成，而不是
按日、周、月或年聚合的统计口径错误。

当前链路为：

```text
SelectedAggregationOption 变化
  -> DashboardViewModel.Refresh() 在 UI 线程同步生成 DashboardSnapshot
  -> DashboardDistributionViewModel.Apply(snapshot)
  -> 对同一个 PeriodActivities ObservableCollection 执行 Clear + N 次 Add
  -> AdaptiveTrendChart.ItemsSource 引用不变
  -> ItemsSourceProperty 的 AffectsRender 不触发
  -> 自绘控件继续显示上一次 OnRender 生成的视觉内容
  -> 后续鼠标、布局或视口事件触发 InvalidateVisual 后才读取新集合
```

`AdaptiveTrendChart` 只在依赖属性引用变化时依赖 `AffectsRender`，没有订阅
`INotifyCollectionChanged.CollectionChanged`。普通 `ItemsControl` 会自行处理集合通知，因此同一页面
的星期和小时柱形图没有相同的失效缺口。

同步聚合占用 UI 线程会延迟帧提交，因而放大旧图停留的体感；但“必须等额外视觉事件才更新”的
直接原因仍是自绘控件没有完整处理数据源生命周期。

## 已选方案

采用“计算完成后原子替换聚合序列，并主动安排一次重绘”。本轮不异步化统计链路。

### 1. ViewModel 原子发布聚合序列

`DashboardDistributionViewModel.PeriodActivities` 不再暴露一个永久复用的可变
`ObservableCollection<PeriodActivityViewModel>`，改为由私有字段支持的
`IReadOnlyList<PeriodActivityViewModel>` 属性。

`Apply(DashboardSnapshot)` 先从快照创建完整的新列表，再通过一次 `SetValue` 发布。该属性只产生
一次 `PropertyChanged`，WPF 绑定看到新的引用后一次性切换数据源；不会经历空集合和逐条增长的
中间状态，也不会为 N 个聚合点请求 N 次重绘。

根 `DashboardViewModel.PeriodActivities` 的转发类型同步改为
`IReadOnlyList<PeriodActivityViewModel>`。统计服务、快照类型、下钻事件参数和图表外观均保持不变。

### 2. 自绘控件补齐数据源生命周期

`AdaptiveTrendChart.ItemsSourceProperty` 增加属性变化回调：

1. 从旧数据源解除 `INotifyCollectionChanged.CollectionChanged`；
2. 订阅新数据源的 `CollectionChanged`；
3. 将 `hoverIndex` 重置为 `-1`；
4. 清空 `renderedItems` 和 `renderedPoints`，避免旧索引或旧命中区继续参与交互；
5. 调用 `InvalidateVisual()`，让 WPF 在当前 UI 操作返回后的渲染阶段读取新数据。

集合变化回调执行同一套“重置缓存并失效”逻辑。这是防御性兼容：本轮 ViewModel 会替换整个只读
列表，但控件的公共 `IEnumerable ItemsSource` 仍允许其他调用者绑定可变集合。旧集合解除订阅后，
继续修改旧集合不得影响图表；新集合原位变化必须使缓存失效。

控件不调用同步 `UpdateLayout()`，不使用 `Dispatcher.Invoke` 强制抢占绘制，也不在 `OnRender`
内部修改绑定状态。WPF 保持正常的布局/渲染合并机制，一次数据发布对应一次排队重绘。

## 数据流与时序

```text
UI 线程同步计算完整 DashboardSnapshot
  -> Metrics.Apply(snapshot)
  -> Distribution.Apply(snapshot)
       -> 构造完整 IReadOnlyList
       -> PeriodActivities 引用一次变更
  -> WPF Binding 更新 AdaptiveTrendChart.ItemsSource
       -> 解除旧数据源事件
       -> 订阅新数据源事件
       -> 清除 hover/命中缓存
       -> InvalidateVisual
  -> 当前输入事件结束
  -> WPF Render pass 调用 OnRender
  -> 仅使用新列表生成曲线、面积、标签和命中点
```

计算期间允许旧图继续存在；新快照就绪并发布后，不再依赖滚动、重新进入视口、尺寸变化或鼠标移动
才能刷新。

## 明确不做

- 不把 Playnite API、游戏库对象或 WPF 对象移到后台线程；
- 不引入 `Task.Run`、取消令牌、请求防抖或加载遮罩；
- 不修改按日、周、月、年的统计算法与自动粒度规则；
- 不改变趋势面积、渐变、平滑曲线、标签稀疏化、Crosshair 和下钻语义；
- 不调用 `UpdateLayout()` 或同步 Dispatcher 强制立即绘制；
- 不修改 XAML 布局、本地化文案、插件版本、会话 schema 或用户数据；
- 不修改或提交用户未跟踪的 `perf_test.ps1`。

## 测试设计

实施遵循红—绿测试顺序。

### 原子发布回归

构造两个最小 `DashboardSnapshot` 并依次调用 `DashboardDistributionViewModel.Apply`，验证：

- 两次应用后的 `PeriodActivities` 引用不同；
- 第二次属性通知中 `PeriodActivities` 只出现一次；
- 读到的序列只包含第二份快照的完整内容，不暴露 Clear/Add 中间状态。

该测试在现状上应因集合引用相同而失败。

### 自绘数据源生命周期回归

在 STA 线程创建 `AdaptiveTrendChart`，使用真实 `ObservableCollection` 和实际 WPF 依赖属性，验证：

- 替换 `ItemsSource` 会清除旧的渲染/悬停缓存；
- 替换后修改旧集合不会再清除当前缓存，证明已解除订阅；
- 修改当前集合会清除缓存，证明已订阅并响应集合通知；
- 重新渲染后缓存只包含新数据源项目。

必要时使用反射读取控件现有私有渲染缓存；测试不新增面向生产的诊断 API。

### 完整回归

- Release 构建 0 警告、0 错误；
- 现有 85 项和新增回归全部通过；
- 10 万会话与 schema 4 性能仍处于既有发布预算；
- `git diff --check` 通过；
- Release、阶段暂存和安装目录 9 个文件逐项哈希一致；
- 部署前后用户数据文件数量与联合指纹不变。

## 客户端验收

1. 在聚合趋势图可见时依次切换日、周、月、年和自动；
2. 确认新图在聚合完成后的下一次渲染中直接替换旧图，不需要滚动或移出再移回视口；
3. 在图表不完全可见时修改粒度，再滚动到图表，确认首次出现即为新图；
4. 在新图上移动鼠标，确认 Crosshair、数据卡片和日期索引对应当前粒度；
5. 点击锚点，确认下钻范围和会话条目对应当前新图；
6. 快速连续切换多个粒度，确认没有旧 Hover、旧下钻索引或异常弹窗。

## 后续增强：异步可取消刷新

更完整的体验优化留到独立性能阶段，不与本次失效修复混合：

1. 在 UI 线程一次性捕获纯数据 DTO，包括游戏元数据、会话副本、筛选值与查询参数；
2. 后台线程只运行不依赖 Playnite API/WPF 的纯分析服务；
3. 每次刷新递增 generation，并使用 `CancellationTokenSource` 取消尚未开始或可取消的旧计算；
4. 回到 UI 线程前比较 generation，只允许最后一次请求原子应用快照；
5. 增加 `IsRefreshing`、轻量加载状态和命令状态，不立即清空可用旧数据；
6. 分别记录捕获、分析、投影和首帧提交耗时，以数据决定是否增加防抖；
7. 增加快速切换、取消、异常恢复、页面关闭和游戏库变化期间的竞态测试。

异步阶段的前置条件是先把分析输入变成不可变纯数据边界，并确认所有后台代码不访问
`IPlayniteAPI`、WPF `DispatcherObject`、可观察集合或游戏数据库实时枚举器。

## 完成定义

- 新快照发布后，趋势图无需额外视口、布局或鼠标事件即可排队重绘；
- 一次聚合刷新只发布一次 `PeriodActivities` 属性变化；
- 旧数据源解除集合通知，新数据源可正确触发失效；
- Hover、Crosshair 和点击命中缓存不会跨粒度复用；
- 不改变统计结果、图表视觉、下钻边界和 Dashboard 单快照架构；
- 自动化、构建、性能、部署、数据保护和客户端验收完成。

## 实施结果

2026-08-12 已按本设计完成工程实施：

- `DashboardDistributionViewModel.PeriodActivities` 改为一次发布完整的新只读列表；一次快照只产生
  一次属性通知，不再暴露 Clear/Add 中间状态；
- `AdaptiveTrendChart` 在数据源引用变化时通过 `CollectionChangedEventManager` 弱退订/订阅，
  并在换源或当前源内容变化时重置 Hover、渲染项目和点缓存，调用正常的 `InvalidateVisual()`；
- 原子发布回归先因旧集合引用仍相同而失败，修复后通过；真实 STA/WPF 回归先因换源后旧缓存仍为
  1 而失败，修复后覆盖换源清理、旧源退订、当前源失效和新源重新渲染；
- 双 clean Release 构建 0 警告/0 错误，87/87 回归通过；10 万会话 557 ms，schema 4 载入
  1,066 ms；
- DLL 为 294,912 字节，SHA-256
  `B142B20DAF2EA1F6B968A3F96557CA4CD7B393A8DA1EF161E424B4468816F18C`；确定性 PEXT 为
  139,530 字节，SHA-256
  `E74F9B5774DBFF44BE168CB136D72650EF77549BEBCBCCFFAAFB22799DF6CA05`；
- Release、PEXT、阶段暂存和安装目录均为精确 9 文件，部署前后 7 个用户数据文件规范化联合指纹
  保持 `8739B76AD190E16BC9BCD752D268B6FE52C4C59D5B169D9779836ACEFE3C18EF`；
- 异步可取消刷新路线继续保留为后续独立性能阶段，当前只待本文“客户端验收”六项检查。
