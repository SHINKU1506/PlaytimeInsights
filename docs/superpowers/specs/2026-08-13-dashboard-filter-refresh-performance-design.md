# Dashboard 筛选刷新性能优化设计

日期：2026-08-13

状态：已确认，待实施

适用分支：`refactor/architecture-preparation`

## 问题与证据

聚合趋势图旧画面失效已经修复，但任意筛选变化仍会让整个 Playnite 窗口轻微停顿。原因是所有
筛选 setter 都同步调用同一个无参数 `DashboardViewModel.Refresh()`，而该入口始终执行完整流程：

```text
读取库插件名称
  -> 重建元数据选项
  -> 枚举全部 Playnite 游戏
  -> 克隆、排序全部有效会话
  -> 元数据过滤
  -> 生成完整 DashboardSnapshot
       指标 + 环比/同比 + 趋势 + 日历热力图
       星期/小时分布 + 异常 + 区间排名 + 累计排名
  -> 两次重建游戏字典并解析排名封面
  -> Clear + Add 重填多个 ObservableCollection
  -> 重置星期筛选、下钻与分页
```

这条链不区分筛选依赖。实际依赖是：

| 变化 | 必须更新的结果 |
|---|---|
| 聚合粒度 | 趋势周期、趋势标题、趋势命中/下钻状态 |
| 排名依据 | 区间排名、排名标题、排名封面 |
| 时间范围/自定义日期 | 当前区间的全部分析；无需重新读取数据库或重建元数据选项 |
| 元数据维度/值 | 从缓存游戏与会话重新过滤，再生成全部区间分析 |
| 显式刷新、页面进入、游戏停止 | 重新读取库/会话和元数据，再生成全部分析 |

当前用户数据只有 18 条会话（16 条有效，约 14 KB），因此本机轻微停顿不能归因于大会话扫描。
更直接的成本是无差别数据库/元数据读取、完整快照投影、封面解析，以及多个集合逐项通知导致的
WPF 绑定和布局风暴。

## 本轮方案

采用“强类型刷新原因 + 输入缓存 + 局部投影 + 集合原子发布”。本轮不直接把现有完整
`CreateSnapshot()` 放入 `Task.Run`。

### 1. 强类型刷新原因

新增 `DashboardRefreshReason`：

```csharp
public enum DashboardRefreshReason
{
    DataReload,
    Range,
    MetadataDimension,
    MetadataValue,
    Aggregation,
    Ranking
}
```

`DashboardFilterViewModel` 的回调从 `Action` 改为 `Action<DashboardRefreshReason>`：

- 范围、自定义起止日期 -> `Range`；
- 聚合粒度 -> `Aggregation`；
- 排名依据 -> `Ranking`；
- 元数据维度 -> `MetadataDimension`，用于从缓存游戏重建可选值并重新过滤；
- 元数据值 -> `MetadataValue`，只重新过滤，不重复重建可选值。

显式刷新命令、页面 Loaded、游戏停止和会话变化继续调用公开 `Refresh()`，其语义固定为
`DataReload`。

### 2. Dashboard 输入缓存

根 ViewModel 保存最后一次数据加载得到的：

- `allGames`；
- `allSessions`；
- `libraryNames`；
- 当前 `filteredGames` 和 `filteredSessions`。

只有 `DataReload` 允许访问 Playnite 数据库、Repository 和库插件列表，并重建元数据值选项。
`Range` 与 `Aggregation`/`Ranking` 复用当前过滤结果；`MetadataDimension` 从缓存游戏重建元数据
可选值后重新过滤；`MetadataValue` 只从 `allGames`/`allSessions` 重新过滤。

若局部原因在首次完整加载前到达，安全降级为 `DataReload`，不得使用空缓存生成误导结果。

### 3. 可复用分析上下文与局部投影

完整分析除 `DashboardSnapshot` 外，同时返回 `DashboardAnalysisContext`。上下文只保存当前范围的
日分配、每日游戏名称摘要、区间游戏统计、日期范围与周起始规则，不持有 Repository、数据库
枚举器或 ViewModel 集合。

分析服务增加两个局部入口：

- `CreateTrendProjection(context, aggregation)`：只从日分配与每日游戏摘要创建周期、标题和趋势
  几何；
- `CreateRankingProjection(context, metric, topGames)`：只从区间游戏统计重新排序和格式化排名。

应用范围按原因收窄：

- `Aggregation`：只调用 `DashboardDistributionViewModel.ApplyTrend(snapshot)` 和
  `DashboardMetricsViewModel.ApplyPeriodTitle(snapshot)`；不替换热力图、星期/小时分布、排名、
  累计指标或筛选上下文；清除趋势下钻，避免旧周期索引残留；
- `Ranking`：只调用 `DashboardMetricsViewModel.ApplyRangeRanking(snapshot, allGames)`；不替换趋势、
  热力图、分布、累计排名或区间指标；
- `Range`/`MetadataDimension`/`MetadataValue`/`DataReload`：应用完整快照并重置完整下钻上下文。

聚合和排名不会再次扫描会话，也不会重建 Advanced 分布、热力图和无关指标。时间范围与元数据
变化会用缓存游戏/会话创建新的完整快照和上下文。

### 4. 主要集合原子发布

下列完整快照列表从永久 `ObservableCollection` 改为新 `IReadOnlyList` 一次发布：

- 区间与累计排名；
- 日历热力图及其星期标签；
- 趋势点；
- 星期分布、小时分布、星期×小时热力格和轴标签；
- 异常提示列表。

星期卡片选择仍直接修改当前条目的 `IsSelected`；小时筛选通过替换完整 `HourDistribution` 列表
更新。会话下钻分页继续使用现有 ObservableCollection，因为“加载更多”是增量语义。

### 5. 分段计时

根刷新使用 `Stopwatch` 记录以下阶段，并通过 `System.Diagnostics.Trace.WriteLine` 输出一行本地诊断：

- `data`：数据库、Repository、库名与元数据选项；
- `filter`：元数据过滤；
- `analytics`：快照计算；
- `apply`：ViewModel/集合发布；
- `total`：总时长与刷新原因。

诊断不包含游戏名、会话时间、用户路径或筛选值，不写入插件数据文件。

## 为什么本轮不直接后台化完整快照

当前分析服务会读取 Playnite 本地化资源，并创建 `PointCollection`、`Geometry` 和多个 WPF
ViewModel。虽然部分 Geometry 已 Freeze，但 `Game`/元数据集合、资源访问和其他 WPF 对象没有形成
明确的纯数据边界。直接 `Task.Run(() => CreateSnapshot(...))` 会产生跨线程访问和竞态风险。

正确的后续异步阶段需要：

1. UI 线程捕获不可变 `DashboardAnalysisInput` DTO，不把 Playnite `Game`、数据库枚举器、资源字典
   或 WPF 对象交给后台；
2. 后台仅生成纯 CLR `DashboardAnalysisResult`；
3. generation + `CancellationTokenSource` 丢弃过期结果；
4. UI 线程完成本地化、封面路径、WPF 几何和一次性状态发布；
5. 用本轮分段计时判断是否值得实施以及应优化哪一阶段。

该后续阶段继续保留在 `docs\ARCHITECTURE_OPTIMIZATION_PLAN.md`，不与本轮低风险性能修复混合。

## 测试设计

### 刷新原因路由

直接实例化真实 `DashboardFilterViewModel`，捕获回调原因，依次修改范围、聚合、排名、元数据维度/
值和自定义日期，验证每个 setter 只产生正确原因。该回归在旧 `Action` API 上先编译失败。

### 局部应用隔离

对真实 Metrics/Distribution 子 ViewModel 先应用完整快照，再应用仅趋势或仅排名快照，验证：

- 聚合只更换周期标题/趋势数据；热力图、星期分布和排名引用保持；
- 排名只更换区间排名/标题；趋势、累计排名与热力图引用保持；
- 属性通知不包含未受影响的属性。

### 原子集合发布

连续应用两个完整快照，验证所有主要列表引用只各变化一次、内容完整，并验证星期选择仍可切换
`IsSelected` 和小时列表。

### 完整回归和部署

- Release 构建 0 警告、0 错误；
- 现有 87 项和新增回归全部通过；
- 10 万会话和 schema 4 预算不退化；
- `git diff --check` 通过；
- 确定性 PEXT 仍严格包含 9 个预期文件；
- Release、暂存和安装目录 9/9 哈希一致；
- 部署前后用户数据数量与规范化联合指纹不变；
- `perf_test.ps1` 不修改、不提交。

## 客户端验收

1. 连续切换聚合粒度，确认指标、热力图、星期分布和排名不闪烁或重置；
2. 连续切换排名依据，确认只有区间排名更新，趋势与分布不闪烁；
3. 切换时间范围与自定义日期，确认整页结果一致且比当前更顺滑；
4. 切换元数据维度和值，确认选项与统计联动正确；
5. 选中星期后切换聚合或排名，星期筛选状态不应被无关原因清除；
6. 点击显式刷新、切出再返回、完成一次游戏停止，确认仍重新读取最新数据；
7. 检查趋势/热力图/排名下钻和会话封面没有退化。

## 完成定义

- 聚合与排名变化不访问数据库、Repository 或元数据枚举；
- 聚合与排名变化不发布无关 Dashboard 集合/指标；
- 范围与元数据筛选复用最后一次数据加载缓存；
- 主要完整快照列表一次性原子发布；
- 完整刷新仍能读取最新游戏、会话与元数据；
- 分段计时不包含敏感数据；
- 自动化、构建、性能、制品、部署和客户端验收完成。
