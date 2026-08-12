# 开发与构建

更新日期：2026-08-12

## 架构重构准备

架构重构在 `refactor/architecture-preparation` 分支独立进行，不与视觉计划混合。执行依据：

- `docs\ARCHITECTURE_OPTIMIZATION_PLAN.md`：阶段、边界和完成定义；
- `docs\ARCHITECTURE_REFACTOR_BASELINE.md`：当前事件、启用条件、键盘焦点和副作用基线。

准备分支新增第 62 项回归 `Architecture refactor baseline keeps boundaries documented`。该测试会
动态扫描 XAML 事件并核对职责矩阵，同时阻止 ViewModel 引入具体对话框/Window 类型或外部 MVVM
框架。阶段 A 新增 8 项 Coordinator 假交互测试，阶段 B 新增 4 项命令回归，阶段 C 新增 6 项
WPF 接线及成功/失败路径回归；阶段 D/E 又覆盖 Dashboard 组合、导航生命周期、刷新快照与事件
对称性，当前共 85 项。应运行：

```powershell
dotnet build PlaytimeInsights.sln -c Release -p:PlayniteInstallDir="D:\software\Playnite"
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release `
  -p:PlayniteInstallDir="D:\software\Playnite"
```

阶段 B 验证产物部署到 `staging\architecture-stage-b` 和本机插件目录；两处 9 个发布文件均与
Release 输出一致。该开发 DLL SHA-256 为
`654CEDAE3753E205507828C0A5634D4D5234CAD26CD7F60C93392408EC77B5B0`，部署未改变用户数据指纹。

阶段 C 中 `PlaytimeInsights.cs` 负责组装 `SessionManagementViewModel`、
`WpfSessionManagementInteraction`、`SessionManagementCoordinator` 和 View。会话页 Code-behind
只保留生命周期、ContextMenu 与 Coordinator 转发；所有文件对话框、Owner、确认、编辑/预览窗口
和错误弹窗集中在 Presentation/Interactions。

阶段 C 验证产物已部署到 `staging\architecture-stage-c` 和本机插件目录；9/9 发布文件一致。
开发 DLL SHA-256 为 `2CA2F983EC130F965A86B77D63A2CD352617182DE8E8382586E65608A2A607BE`，
部署前后用户数据指纹不变。

阶段 D 将 Dashboard 条目类型迁入 `ViewModels\Dashboard`，并以 Filter、Metrics、Distribution、
Drilldown 四个子 ViewModel 组合根对象。根 `DashboardViewModel` 是唯一的全量游戏/会话读取者，
只创建一次 `DashboardSnapshot` 后分发；子对象不得持有 `SessionRepository` 或自行调用
`AnalyticsService.CreateSnapshot`。对应静态护栏会在回归中持续检查该边界。

阶段 D 验证产物已部署到 `staging\architecture-stage-d` 和本机插件目录；Release、staging、
安装目录三处 9/9 发布文件一致。开发 DLL 为 294,400 字节，SHA-256 为
`0156F2C0F11D5310BF4B79B26958BDAD010F4433A40C46A704DFA3AA2713764D`；部署前后用户数据均为
7 个文件，联合指纹保持
`C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`。

侧边栏性能修复规定 View `Loaded` 是页面进入时唯一的自动刷新入口，`SidebarItem.Opened` 只负责
组装或复用 ViewModel 和 View。不要同时在两处调用 `Refresh()`，否则会在 UI 线程重复枚举游戏库、
克隆会话、重建筛选与可观察集合。`SessionManagementViewModel.CountText` 必须只投影最近一次刷新
快照中的 `activeSessionCount`，不得在属性 getter 中调用 Repository。

本次性能修复验证产物已部署到 `staging\architecture-stage-d` 和本机插件目录；三处 9/9 文件
逐项一致。DLL 为 294,400 字节，SHA-256 为
`7A763012974D25685A512CDB4A10A7ACE32FF31C08B09DF2A583F20DFA807ADE`；部署前后 7 个用户数据
文件联合指纹保持 `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`。

阶段 E 最终架构见 `docs\ARCHITECTURE.md`。事件双向审计未发现孤立处理器，现有 View 代码均有
真实事件源和明确职责。两轮独立干净构建 85/85 项回归均通过；DLL SHA-256 均为
`7A763012974D25685A512CDB4A10A7ACE32FF31C08B09DF2A583F20DFA807ADE`。确定性 PEXT 两轮均为
139,177 字节，SHA-256 均为
`3DDF721B41078D694984D044C71797A38A801098D1359B6F824EDED1926F9126`，仅含 9 个预期条目。
第二轮 Release 已部署至 `staging\architecture-stage-e\deployed` 和本机插件目录；部署前后用户
数据联合指纹保持 `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`。

## 环境

- Playnite 安装目录：`D:\software\Playnite`
- PlayniteSDK：本机安装目录中的 6.16.0
- 目标框架：`net462`
- 构建工具：.NET SDK 7.0.306
- WPF 工程格式：SDK-style `Microsoft.NET.Sdk.WindowsDesktop`

本机已安装 .NET Framework 4.6.2 Developer Pack。项目仍保留
`Microsoft.NETFramework.ReferenceAssemblies.net462` 作为仅编译依赖，确保构建环境可复现；该包设置为
`PrivateAssets=All`，不会进入插件目录。

运行时只引用 Playnite 自带的 `Playnite.SDK.dll`，并设置 `Private=false`，不会复制或分发 SDK DLL。

正式 Release 配置设置 `DebugType=None`、`DebugSymbols=false`，不生成或分发 PDB；`PathMap` 将
项目目录映射到 `/_/PlaytimeInsights`。构建后还会对 DLL 扫描用户名、开发目录和 PDB 路径，
避免正式二进制泄露本机绝对路径。

`staging\**` 显式从 `Compile`、`Page`、`Resource`、`EmbeddedResource` 和 `None` 默认项中排除。
这项设置不可删除：SDK-style WPF 否则会把被 Git 忽略的历史暂存 `Localization\*.xaml` 编译成
BAML 并嵌入 DLL，导致同一源码在不同开发机或暂存状态下产生不同程序集。

## 恢复与编译

```powershell
dotnet restore .\PlaytimeInsights.csproj
dotnet build .\PlaytimeInsights.csproj -c Release --no-restore
```

产物：

```text
bin\Release\net462\
  PlaytimeInsights.dll
  extension.yaml
  icon.png
  icon-dashboard.png
  icon-sessions.png
  LICENSE
  PRIVACY.md
  Localization\
    en_US.xaml
    zh_CN.xaml
```

可以通过 MSBuild 属性覆盖 Playnite 安装路径：

```powershell
dotnet build .\PlaytimeInsights.csproj -c Release -p:PlayniteInstallDir="X:\Playnite"
```

## 开发部署

当前 Demo 部署目录：

```text
%AppData%\Playnite\Extensions\
  PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd\
```

部署发布文件：

- `PlaytimeInsights.dll`
- `extension.yaml`
- `icon.png`
- `icon-dashboard.png`
- `icon-sessions.png`
- `LICENSE`
- `PRIVACY.md`
- `Localization\en_US.xaml`
- `Localization\zh_CN.xaml`

Playnite 插件 DLL 不能热重载。每次更新 DLL 后必须完全退出并重新启动 Playnite。

## 打包

```powershell
.\scripts\Pack-Deterministic.ps1 `
  -SourceDirectory .\bin\Release\net462 `
  -OutputDirectory .\dist `
  -ToolboxPath D:\software\Playnite\Toolbox.exe
```

该脚本先验证 Release 目录严格包含 9 个预期文件，再在一次性临时副本中统一文件时间戳并调用
Toolbox。Toolbox 直接打包会把 DLL 的构建时间写入 ZIP 元数据：即使两轮 DLL 和包内所有文件内容
完全一致，PEXT 外层 SHA-256 仍会变化。确定性入口不修改原 Release 文件，只消除这一元数据差异。

当前 0.9.8 包名：

```text
PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_8.pext
```

当前 0.9.8 发布校验：

- 程序集：`PlaytimeInsights, Version=0.9.8.0`；
- 清单：`Version: 0.9.8`；
- DLL SHA-256：`9BEFE2370DA5BA3E21F5E5E55862B59497EC6DA8CE6840BD268942F900DB5AB4`；
- DLL 大小：282,112 字节；
- PEXT SHA-256：`09ACBD2CE1B62346AC658C4FE3C2539FA456394C7CC6773EAE46BBAA3BAB4B82`；
- PEXT 大小：134,031 字节；
- 暂存目录与安装目录的 9 个发布文件逐项 SHA-256 一致；
- PEXT 内容包含 MIT `LICENSE` 与两个本地化 XAML，不包含 PDB；
- DLL 敏感路径扫描未发现用户名、开发目录或 PDB 路径；
- 发布过程不写入 `ExtensionsData`；本轮部署前后全部 7 个数据/备份文件的内容、长度与时间戳
  联合指纹均为
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`。

Playnite Add-on Database 清单位于 `manifests\installer.yaml` 和 `manifests\addon.yaml`。2026-08-11
`v0.9.8` Release 和 PEXT 已发布，仓库、图标、三张截图、Installer manifest、Add-on manifest
及 PackageUrl 均可匿名访问。正式远程校验命令为：

```powershell
D:\software\Playnite\Toolbox.exe verify installer .\manifests\installer.yaml
D:\software\Playnite\Toolbox.exe verify addon .\manifests\addon.yaml
```

2026-08-10 使用一次性本机 HTTP 镜像完成预校验；2026-08-11 在正式公网 URL 上再次运行，
installer 与 addon（含远程 installer 联动）均完整通过。Add-on Database 清单已通过
[PR #626](https://github.com/JosefNemec/PlayniteAddonDatabase/pull/626) 提交。

0.9.8 正式截图位于 `docs\screenshots\0.9.8`：中文/英文分析页和中文插件设置页。会话管理页
截图按发布决定不附加；README 与 Add-on manifest 均只引用三张公开截图；公开 Author 为
`SHINKU1506`。

## 0.2 数据流

```text
OnGameStarted
  -> ActiveGameSession 写入 sessions.json
  -> 每分钟更新 LastCheckpointUtc

OnGameStopped
  -> 读取持久化 ActiveGameSession
  -> 如果缺少开始事件，用 EndedAtUtc - ElapsedSeconds 推算
  -> 创建 GameSession
  -> 原子移除 active + 写入 completed session
  -> 指纹去重
  -> 临时文件 + File.Replace 保存
  -> 如果 Dashboard 正在打开，通过 UIDispatcher 刷新

OnApplicationStarted
  -> 检查残留 active session
  -> 自动恢复到 LastCheckpointUtc，或按设置丢弃

OnApplicationStopped
  -> 写入最终检查点
  -> 兜底完成仍处于 active 状态的会话
```

## 0.3 查询与聚合流

```text
Dashboard selector
  -> range: Today / ThisWeek / ThisMonth / ThisYear / Custom
  -> granularity: Day / Week / Month / Year
  -> ranking metric: Duration / Sessions / ActiveDays / Average / Longest
  -> AnalyticsQuery
  -> ResolveDateRange（本地自然日，包含首尾）
  -> DailyAllocationService.SplitByLocalDay
  -> 只保留落入范围的日期分片
  -> 聚合为周期柱图、区间指标、区间游戏排名
  -> 同时独立读取 Game.Playtime 生成累计总时长与累计排名
```

区间会话口径：

- 时长：会话落入日期范围的秒数之和；
- 会话数：在范围内至少有一秒分片的会话数量；
- 活跃天：范围内时长大于零的本地自然日数量；
- 平均会话：区间时长除以区间会话数；
- 最长会话：单个会话落入范围的最大秒数；
- 游戏改名时优先显示 Playnite 当前名称，游戏已删除时使用会话快照名称。

## 0.5 原生可视化与钻取

```text
Daily allocation
  +-> HeatmapCells（7 行 × N 周，强度 0.08–1.0）
  +-> PeriodActivities（日/周/月/年柱形）
        +-> TrendLinePoints（同源折线坐标）

Heatmap cell click
  -> selected local date
  -> CreateSessionDetails(date, date)

Bar / trend point click
  -> clipped PeriodStart / PeriodEnd
  -> CreateSessionDetails(period start, period end)

CreateSessionDetails
  -> 逐会话 SplitByLocalDay
  -> 只累加钻取范围内的秒数
  -> 当前游戏名称或历史快照名称
  -> 本地开始时间、精确时长、Tracked/Recovered/Imported/Manual 来源
```

图表实现：

- 热力图：WPF `ItemsControl + UniformGrid`；
- 柱形图：WPF `ItemsControl + Border`；
- 折线图：WPF `Canvas + Polyline + clickable point buttons`；
- tooltip 与钻取：WPF 原生事件和绑定；
- 不使用第三方图表库、WebView、远程脚本或网络资源。

### 会话列表容量策略

```text
CreateSessionDetails
  -> SessionDetailPager.Reset(all matched sessions)
  -> VisibleItems = first 100
  -> ListBox MaxHeight = 380
  -> VirtualizingStackPanel + Recycling
  -> Load more
  -> append next 100 until VisibleCount == TotalCount
```

- 查询结果仍保留完整会话集合，以便显示准确总数；
- WPF 首屏仅绑定 100 条，避免一次向集合发布全部条目；
- 虚拟化列表只为可见行创建容器，并回收离屏行；
- 列表独立滚动，不再随会话数量无限拉长整个仪表盘；
- “加载更多”按钮仅在 `VisibleCount < TotalCount` 时显示。

### 自动聚合粒度

`AnalyticsQuery.AggregationPeriod` 默认值为 `Auto`。解析规则：

| 时间范围 | 自动采用粒度 |
|---|---|
| 今天 | 日 |
| 本周 | 日 |
| 本月 | 日 |
| 本年 | 月 |
| 自定义 1–62 天 | 日 |
| 自定义 63–730 天 | 周 |
| 自定义 731–3650 天 | 月 |
| 自定义超过 3650 天 | 年 |

`ResolveAggregationPeriod` 只在查询值为 `Auto` 时应用规则。显式选择 `Day`、`Week`、`Month`
或 `Year` 时直接返回用户选择，因此自动默认不会限制细粒度分析。

## 0.8 数据管理分层

0.8 不直接在分析仪表盘中堆叠所有管理控件，而是注册第二个原生侧边栏视图：

```text
Playtime Insights
  -> 分析仪表盘（只读统计与钻取）

Playtime Insights · 会话
  -> SessionManagementViewModel
  -> SessionQueryService
  -> SessionExportService
  -> 原生筛选、虚拟化列表和导出
```

分阶段边界：

- 0.8.0 只读浏览和导出，不改变 schema 2 数据；
- 0.8.1 修正元数据筛选语义和选项刷新，不改变 schema；
- 0.8.2 才引入 schema 3、补录、编辑和软删除；
- 0.8.3 才允许导入或恢复，并要求预览、去重和可回滚备份；
- 0.8.4 加入高级分布图和对比。

0.8.0 导出原则：

- 导出只读取 `SessionRepository.GetAll()` 返回的快照；
- CSV 使用 UTF-8 BOM，确保中文游戏名可由 Excel 直接识别；
- 所有字段执行 RFC 4180 风格的双引号转义；
- JSON 输出带格式版本、导出时间和筛选后的会话副本；
- 用户取消文件选择时不创建文件；
- 导出失败只显示错误，不修改 `sessions.json` 或备份。

### 0.8.0 查询与导出实现

```text
SessionRepository.GetAll()
  -> SessionQueryService.Filter
       +-> keyword
       +-> SessionSource?
       +-> exact split platform
  -> newest-first GameSession snapshot
  -> SessionQueryService.CreateItems
  -> PagedCollection<SessionManagementItemViewModel>(200)
  -> virtualized ListBox

filtered GameSession snapshot
  +-> SessionExportService.CreateCsv
  |    -> UTF-8 BOM file
  |    -> quoted RFC 4180-style fields
  +-> SessionExportService.CreateJson
       -> FormatVersion 1 envelope
       -> Playnite runtime serializer
```

`ISessionExportJsonSerializer` 隔离 Playnite 静态序列化器。正式运行使用
`PlayniteExportJsonSerializer`，独立测试使用 Newtonsoft 测试实现；插件产物不复制 Newtonsoft DLL。

### 0.8.1 当前 Playnite 元数据筛选

平台快照不代表会话运行环境，因此从默认筛选中移除。新的结构：

```text
Metadata dimension
  -> Library: Game.PluginId -> loaded LibraryPlugin.Name
  -> Source: Game.Source.Name
  -> Publisher: Game.Publishers
  -> Developer: Game.Developers
  -> Tag: Game.Tags
  -> Genre: Game.Genres
  -> Category: Game.Categories
  -> Installation: Game.IsInstalled
  -> distinct/sorted metadata values
  -> filter sessions by current Game.Id join
```

这些维度读取当前 Playnite 游戏元数据。游戏被删除后会话仍能显示，但无法再匹配当前元数据筛选。

筛选集合更新策略：

```text
Refresh
  -> RefreshReentrancyGuard.TryEnter
  -> suppressFilterRefresh = true
  -> clear and repopulate MetadataValueOptions
  -> restore selected value or select "全部..."
  -> suppressFilterRefresh = false
  -> execute query once
  -> guard.Exit
```

这同时阻止 `ComboBox.SelectedItem` 在集合清空时回写 `null` 所造成的嵌套刷新和重复追加。

## 0.8.2 schema 3 与会话变更

schema 3 新字段：

| 字段 | 说明 |
|---|---|
| `IsDeleted` | 软删除标志 |
| `DeletedAtUtc` | 删除发生的 UTC 时间，恢复后清空 |
| `LastModifiedAtUtc` | 最近编辑、删除或恢复时间 |
| `LastModifiedReason` | `ManualEntry`、`UserEdit`、`UserSoftDelete`、`UserRestore` 等 |

Repository 读写边界：

```text
GetAll()
  -> clones of non-deleted sessions

GetAllIncludingDeleted()
  -> clones of all sessions

CompleteSession
  -> clone + schema 3 + dedup + atomic save

UpdateSession
  -> find by stable Id
  -> preserve deletion state
  -> validate no duplicate fingerprint excluding self
  -> modified timestamp/reason
  -> atomic save

SetSessionDeleted
  -> toggle IsDeleted / DeletedAtUtc
  -> modified timestamp/reason
  -> atomic save
```

补录/编辑窗口：

- 选择 Playnite 游戏；
- 输入本地开始日期和 `HH:mm[:ss]`；
- 输入 0–31536000 的持续秒数；
- 用 Windows 当前时区转换开始/结束 UTC；
- 补录来源为 `Manual`，编辑保留原来源；
- 无效本地时间、非法秒数和重复会话拒绝保存。

## 0.8.3 导入、备份与恢复

导入分成严格的预览和提交两阶段：

```text
OpenFileDialog (multi-select JSON/CSV)
  -> SessionImportService.Preview
       -> detect Playtime Insights / GameActivity
       -> RFC 4180-style CSV parse or typed Playnite JSON deserialize
       -> normalize UTC and current schema
       -> resolve game by exact ID / unique name / stable external ID
       -> validate
       -> deduplicate against store and current batch
       -> SessionImportPreview (no writes)
  -> native SessionImportPreviewWindow
       -> candidate list + counters + errors
       -> optional UTF-8 error report
  -> confirm
  -> SessionRepository.ImportSessions
       -> timestamped pre-import rollback backup
       -> one atomic save
```

支持格式：

| 格式 | 识别字段 | 时间口径 |
|---|---|---|
| Playtime Insights JSON | `Sessions` | `StartedAtUtc` / `EndedAtUtc` |
| Playtime Insights 完整备份 | `Sessions` + `ActiveSessions` | UTC |
| Playtime Insights CSV | `GameId` + `StartedAtUtc` + `ElapsedSeconds` | UTC |
| GameActivity JSON | `Items[].DateSession` + `ElapsedSeconds` | `DateSession` 视为 UTC |
| GameActivity CSV | 技术表头或中英文本地化表头 | 导出值为本地时间，按 Windows 当前时区回转 UTC |

导入安全规则：

- 单条持续时长不得超过 31,536,000 秒；
- 结束时间与持续秒数差异超过 2 秒时，以开始时间 + 秒数规范化；
- ID 相同，或游戏 ID、秒数相同且开始时间相差不超过 2 秒，均视为重复；
- CSV 每行独立报告错误，单行失败不会阻断同文件其他合法行；
- CSV 自动识别逗号、GameActivity 默认分号或制表符；支持 `Name`/`名称`、
  `Date session`/`会话日期`、`Time Played`/`游玩时间`；
- `SessionSource` 统一写为 `Imported`；
- schema 4 的 `ImportSource` 保存格式来源；
- `ImportConfidence` 保存 `ExactGameId`、`UniqueNameMatch`、
  `ExternalGameId` 或 `UnmatchedNameSnapshot`；
- 多个 Playnite 游戏同名时拒绝猜测关联。

完整备份和恢复：

```text
CreateManualBackup
  -> clone current SessionStoreDocument
  -> temporary file
  -> atomic replace/move to user-selected JSON

RestoreBackup
  -> require Sessions + ActiveSessions full-backup marker
  -> deserialize + validate
  -> Backups/sessions.<UTC>.pre-restore.json
  -> replace completed-session collection
  -> preserve current in-memory ActiveSessions
  -> ignore stale ActiveSessions from backup
  -> atomic save
```

`Reindex` 先创建 `pre-reindex` 回滚备份，再按开始时间规范化排序，修复空或冲突 ID，并按
现有两秒容差指纹移除重复。它不会改变合法会话的秒数、游戏归属或软删除状态。

筛选导出 JSON 不允许进入完整恢复流程；它只能通过导入预览合并，避免局部筛选结果意外覆盖全库。

## 0.8.4 高级分布与对比

分析页筛选先从当前 Playnite 游戏库得到匹配的 `GameId` 集合，再以同一集合限制会话：

```text
Game database
  -> SessionQueryService.GetMetadataValues
  -> library / developer / type / tag / installation selection
  -> SessionQueryService.FilterGames
  -> matching GameId set
       +-> filtered Game collection
       +-> filtered completed-session collection
  -> AnalyticsService.CreateSnapshot
  -> CreateSessionDetails uses the same filtered collections
```

因此筛选不会只影响某一个图表：区间指标、趋势、两种热力图、分布、会话钻取、区间排名和
Playnite 累计排名均保持同一游戏集合口径。筛选读取当前元数据；游戏被删除后，其历史会话不会命中
库来源、开发者、类型、标签或安装状态筛选。库来源复用会话管理页的已加载
`LibraryPlugin.Id → Name` 映射，并区分手动添加游戏与未知/未加载库。

小时分配流程：

```text
GameSession UTC interval
  -> resolve saved TimeZoneId
     -> fallback fixed StartUtcOffsetMinutes
  -> scan UTC timeline for each next local whole-hour boundary
  -> split into local date + local hour wall-clock segments
  -> allocate ElapsedSeconds proportionally
  -> final segment absorbs integer remainder
  -> sum(hour segments) == session.ElapsedSeconds
```

在 UTC 时间线上寻找下一个本地整点，可以覆盖夏令时跳过小时和重复小时。星期分布直接复用每日秒数，
星期 × 小时矩阵按当前周起点输出 7 × 24 个单元格。

星期到小时联动：

```text
点击 WeekdayDistribution 柱条
  -> DashboardViewModel 保存星期索引并设置 IsSelected
  -> 从 WeekHourCells 对应的连续 24 个单元格重建 HourDistribution
  -> 在所选星期内部重新归一化柱高
  -> 标题显示“24 小时分布 · 星期”
再次点击同一星期
  -> 恢复完整范围的 HourDistribution
```

该选择是分布面板内的临时探索状态，不改变区间指标、排名、趋势、热力图或会话数据。切换日期范围、
元数据筛选或点击刷新会回到“全部星期”。星期项使用原生 `Button`，支持鼠标、Tab、Enter 和 Space；
选中态具有主题画刷边框/背景，并为屏幕阅读器提供带操作含义的名称。

对比和连续天数：

- 环比范围为当前范围之前的上一等长闭区间；
- 同比范围为当前起止日期分别减一年，闰日按 `DateTime.AddYears` 的安全日历规则回退；
- 上期为零且本期非零显示“新增”，两期均为零显示“持平”，其余显示一位小数百分比；
- 最长连续天数基于范围内所有非零自然日；
- 当前连续天数从 `min(范围结束日, 今天)` 向前逐日计算。

异常提示是纯分析结果：

- 零秒会话；
- 结束早于开始；
- 开始时间超过当前 UTC 五分钟；
- 单次记录至少 18 小时；
- `ElapsedSeconds` 比开始/结束墙钟差多出超过五分钟。

提示按开始时间倒序，最多发布 50 条到虚拟化列表，不对 Repository 发起任何写操作。

## 0.9.0 性能、升级、隐私与诊断

只读诊断流：

```text
SessionRepository.GetStorageDiagnostics
  -> lock current document
  -> schema / counts / source distribution
  -> main + backup existence and byte size
  -> rollback backup count
  -> writable / loaded-from-backup state
  -> SessionStorageDiagnostics
  -> SessionDiagnosticsService.CreateReport
       -> omit DataDirectory
       -> no names / timestamps / ids
  -> user-selected local UTF-8 text file
```

诊断报告只有用户点击“保存诊断报告”并确认文件路径后才写入。它不读取会话内容之外的系统信息，
不包含用户路径，也不执行网络操作。正式隐私边界见根目录 `PRIVACY.md`。

小时分配性能改进：

- `HourlyAllocationService` 按“时区 ID + 保存偏移”缓存 `TimeZoneInfo`；
- 无夏令时时区直接把下一个本地整点转换为 UTC；
- 支持夏令时的时区仍在 UTC 时间线上寻找本地整点，保持跳时/重复小时正确；
- 缓存仅保存系统时区规则，不缓存或暴露会话。

发布压力场景：

| 场景 | 本机结果 | 发布预算 |
|---|---:|---:|
| 5,000 游戏、100,000 会话、2016–2025 完整分析 | 约 300 ms | 30 s |
| schema 4 JSON、100,000 会话加载并克隆查询 | 约 1,136 ms | 30 s |

测试使用合成游戏和会话，不读取用户真实数据。压力结果表明当前目标规模下无需立即引入数据库或
年度分片，但写入、导入预览和更高数量级仍应在 0.9.2 前继续观察。

升级矩阵直接构造 schema 1、2、3、4 文档并经当前 `SessionRepository` 加载，确认：

- 会话数量不变；
- 会话 ID 和游戏 ID 不变；
- 游戏名称快照和持续秒数不变；
- 文档与会话在内存中规范化到当前 schema 4；
- 加载本身不覆盖原始用户文件。

## 0.9.1 本地化与可访问性

本地化加载链路：

```text
App.xaml
  -> 合并 Localization/en_US.xaml 作为内置回退
  -> Playnite 按当前地区加载同名 LOC 键覆盖
  -> 静态 XAML 使用 {DynamicResource LOC...}
  -> 运行时文本使用 LocalizationService.Get / Format
  -> ResourceProvider 未初始化或返回 <!LOC...!> 时使用代码中的中文兼容回退
```

资源约束：

- `Localization/en_US.xaml` 与 `Localization/zh_CN.xaml` 当前各 214 个键；
- 键集合及非空值由 `TestLocalizationResourceParity` 自动验证；
- 新增用户可见文本必须同时进入两份资源；
- 文件筛选器、确认框、状态文本、图表 tooltip 和格式化时长同样属于本地化范围；
- XAML 中不允许重新加入硬编码中文的 `Text`、`Content`、`Header`、`Title`、`ToolTip` 或
  `StringFormat` 属性。

无障碍约束：

- 仪表盘、会话管理、编辑、导入预览和设置视图使用显式 Tab 顺序与循环键盘导航；
- 窗口给主要输入设置初始焦点；
- 主要按钮、输入、筛选、列表和状态文本提供 `AutomationProperties.Name`；
- 按钮通过 WPF `AccessText` 下划线提供访问键，同一视图不复用同一访问键；
- 热力图/图表强度始终伴随文字时长、tooltip、标题或可钻取明细；
- `TestNativeViewAccessibility` 检查本地化资源引用、自动化名称、键盘导航标记及硬编码中文属性。

独立测试进程不会初始化完整 Playnite 资源系统。`LocalizationService` 必须识别
`ResourceProvider.GetString` 的 `<!key!>` 缺失标记并回退，否则统计服务测试会把缺失标记误当成
有效格式字符串。

## 0.9.2 显示兼容性与最终升级矩阵

星期标签链路：

```text
Localization/en_US.xaml 或 zh_CN.xaml
  -> WeekdayLabelService
  -> AnalyticsService 星期分布和日历热力图
  -> AdvancedAnalyticsService 星期 × 小时热力图
```

`WeekdayLabelService` 接收周起点，但星期名称只从插件 LOC 资源解析。Windows 地区仍决定关闭 ISO
周一选项后的星期顺序，不再决定界面语言。交叉回归分别模拟“中文 Windows 地区 + 英文 Playnite
资源”和“英文 Windows 地区 + 简体中文 Playnite 资源”。英文月份标题使用资源内数字格式
`{0:yyyy/M}`，避免 `MMMM` 再次调用 Windows `CurrentCulture`。

窗口与主题约束：

- `WindowLayoutService` 根据 `SystemParameters.WorkArea` 和 DPI 比例计算安全尺寸；
- 编辑窗口最低 360 × 320，导入预览最低 480 × 420；内容不足时由窗口级垂直滚动承接；
- 仪表盘外层禁用水平滚动，保证指标卡片按真实可用宽度换行；
- 仅会话长行、异常行、导入候选和逐日图表保留局部水平滚动；
- 弹窗背景统一使用 `PopupBackgroundBrush`，其余发布画刷限于默认主题和 Seaside 共同提供的
  `ControlBackgroundBrush`、`GlyphBrush`、`PanelSeparatorBrush`、`PopupBackgroundBrush` 和
  `TextBrush`。

最终矩阵：

- 中英资源各 272 个键，键集合、非空值和格式化占位符完全一致；
- 源码内 `LocalizationService` 键引用必须在两份资源中存在；
- 0.1–0.9 历史设置 JSON 加载后补齐当前默认值；
- schema 1–4 会话数据无损升级到 schema 4；
- 编辑、导入、CSV/JSON 解析、备份和恢复的主要错误路径均有中英资源；
- Release 主项目和测试项目均为 0 警告、0 错误，58/58 回归通过；
- Seaside 真实客户端中英文检查确认两张热力图、星期分布、数字月份标题及窄窗口布局正常。

原生界面整理：

- 两个主页面继续使用 WPF `UserControl`，没有引入 WebView、外部网页、第三方图表库或远程资源；
- 页面背景、普通描边、焦点和选择反馈只使用已列入主题白名单的 Playnite 动态画刷；唯一固定语义色
  是前三名勋章的金、银、铜描边与低透明度填充；
- 仪表盘标题与会话页标题增加主题色导航标识，面板圆角、内边距和字段标签层级统一；
- 分析范围摘要移到面板右上角的主题浮层中，减少与帮助文字争夺纵向空间；
- 星期按钮使用自定义原生 `ControlTemplate`，选中、悬停和键盘焦点由主题描边表达，不再出现
  覆盖整块图表的高亮背景；
- 所有指标卡统一继承 `MetricCardStyle` 的右侧与底部 12 像素外边距；移除“Playnite 累计总时长”
  卡片原有的局部 `Margin="0,0,0,12"` 覆盖，窗口换行时卡片之间不会黏连；
- 星期按钮把“选中”与“键盘焦点”拆成两套视觉状态：选中使用底部 2 像素 `GlyphBrush`，
  焦点使用四周 1 像素 `TextBrush`；取消选中后即使按钮仍持有焦点，也不会残留蓝色选中条；
- 会话页常用操作放在页头右侧，数据工具成为独立的次级按钮组，筛选区域保持原有绑定和键盘顺序；
- 会话列表增加固定列头、行分隔线、悬停和左侧主题色选中标识；`ListBoxItem.Foreground`
  显式绑定 `TextBrush`，避免自定义容器模板回落到系统深色前景；
- 列表继续使用 Recycling 虚拟化，`MinWidth="860"` 和局部横向滚动策略不变；
- 审查前截图位于 `docs\audit\0.9.2-ui-polish-before`，最终截图及同尺寸前后对照位于
  `docs\audit\0.9.2-ui-polish-after`。

排名视觉链路：

```text
Playnite Game.CoverImage
  -> IGameDatabaseAPI.GetFullFilePath
  -> GameRankingViewModel.CoverImagePath
  -> CoverImageConverter
  -> 36 × 50 WPF Image
```

- 区间排名与累计排名共用 `GameRankingItemTemplate`，保证封面、勋章、文字和值列严格对齐；
- 封面只读取 Playnite 数据库的本地媒体文件，不下载、不联网；缺失或损坏时保留固定缩略图槽位；
- `CoverImageConverter` 使用 `BitmapCacheOption.OnLoad`，以 `DecodePixelWidth=96` 解码并 `Freeze`，
  避免排名页长期锁定库媒体文件，同时限制内存和缩放开销；
- 缩略图视觉尺寸固定为 36 × 50，使用 `UniformToFill` 和高质量缩放；
- 第一、二、三名分别使用金 `#D6B34B`、银 `#BFC7D5`、铜 `#C9824A` 的圆形数字勋章；
- 每个条目名称区底部使用原生 `ProgressBar`；`ProgressPercent` 表示该游戏时长占当前统计口径
  全部游戏总时长的百分比，而不是相对第一名；
- 区间排名的分母包含所选范围与筛选结果内的全部精确会话时长，即使当前只显示 Top N；
  累计排名的分母包含当前筛选后全部 `Playtime > 0` 游戏；
- 排名依据切换为会话数、活跃天数、平均或最长会话时，排序仍按所选指标，进度条仍固定表达时长占比；
- `LOCPlaytimeInsightsShareOfTotalFormat` 为进度 tooltip 提供中英文百分比说明。

指标卡信息层级：

- 顶部 9 张卡片继续由原生 WPF `WrapPanel` 承接响应式换行，统一为 218 × 154，并保留右侧、
  底部各 12 像素间距；
- `MetricValueStyle` 将核心数字统一为 26pt、Bold；`MetricHelperTextStyle` 将辅助说明统一为
  11pt 和 `#8A8A8A`，标题与单色图标保持次级层级；
- 图标使用 Windows 内置 `Segoe MDL2 Assets` 字体，不新增图片、字体包、WebView 或网络资源；
- `ComparisonMetricViewModel` 同时保留原有 `DeltaText` 百分比，并新增 `TagText`、
  `TrendKind` 和 `TooltipText`；
- `CreateComparison` 计算有符号方向和无符号绝对时长差；增长 Tag 使用绿色，下降使用蓝色，
  持平使用主题中性色；
- 只有“区间游玩时长”存在真实的上一等长区间与去年同期基线，因此两项 Tag 只合并到该卡；
  会话数、活跃天数等指标不伪造趋势；
- tooltip 继续给出完整比较标题、基准范围时长和百分比，卡面只保留方向、绝对时长差和短标签。

图表视觉与几何：

- `CurrentStreakText` 只承载 `{0:N0} 天/days`，新增 `CurrentStreakDateText` 承载本地化截止日期；
  卡片辅助文字的 tooltip 继续说明“以区间结束日或今天为准”；
- 星期柱图不再绑定 `DurationText` 常驻标签，星期/小时柱都只显示 X 轴刻度；
  `TooltipText` 仍包含精确时长；
- `ChartBarBrush` 是深蓝、亮蓝到紫色的垂直渐变；星期与小时柱体分别使用 7/6 像素顶部圆角；
- 星期 × 小时单元格为 30 × 30，日历单元格为 24 × 24；ScrollViewer 使用居中内容对齐，
  当网格宽于面板时仍由原生水平滚动承接；
- 热力格底层固定为 `HeatmapEmptyBrush #2A2A2E` 并带 1 像素主题分隔线，数据层使用
  `HeatmapActiveBrush` 与原 `HeatOpacity` 叠加；零值因此仍可见而不是空洞；
- `CreateSmoothTrendGeometries` 从现有趋势坐标生成两份冻结 `PathGeometry`：一份开放曲线，
  一份闭合到 150 像素基线的面积；
- 曲线使用 Fritsch–Carlson/Hyman 限制的单调三次 Hermite 切线，再转换为 WPF
  `BezierSegment`，相邻点之间平滑且不会在局部极值处产生明显过冲；
- 面积填充使用自上而下约 40% 到 0% 的渐变；原 `TrendPointViewModel` 圆点仍覆盖在 Path 上方，
  因而 tooltip、点击和会话钻取不变；
- `TrendLinePoints` 暂时保留用于坐标回归和兼容，界面已不再渲染 `Polyline`。

聚合柱、钻取列与排名背景进度：

- `PeriodActivityViewModel.IsDailyAggregation` 由 `CreatePeriodActivities` 根据实际生效粒度设置；
- `AggregationBarStyle` 默认继续使用 Playnite `GlyphBrush`，仅在日粒度触发
  `DailyAggregationBarBrush`；
- 日粒度柱体使用 WPF `Rectangle`，`RadiusX=2`、`RadiusY=2`，垂直填充从不透明
  `#4A90E2` 到同色全透明；因此可以与上方蓝紫渐变柱直接进行客户端视觉对比；
- 会话钻取改为原生 `ListView + GridView`，四列固定宽度依次为 260/170/120/110；
  列头与内容共用 GridView 布局，水平滚动时同步移动，解决原 ListBox 无列头且列边界不明确的问题；
- 游玩时长列右对齐，游戏名和来源保留省略号；最大高度 380、Recycling 虚拟化、100 条分页不变；
- `RankingBackgroundProgressStyle` 是自定义原生 `ProgressBar` 模板，完整提供
  `PART_Track` 和 `PART_Indicator`；
- 背景进度置于排名 Grid 的第一绘制层，跨越徽章、封面、名称和值四列，并以负边距延伸到整行；
  `#4A90E2` 的 0.12 透明度只作用于进度层，所有前景文字与图片保持原对比度；
- 列表项自身承载原占比 tooltip；进度语义和分母算法没有变化。

自适应趋势颜色与标签防碰撞：

- 线条使用水平方向 `#2F8CFF → #A45CFF`，面积使用蓝紫约 40% 透明度到 0% 的垂直渐隐；
- `DrawSparseLabels` 的初筛仍按可用宽度估算最大标签数，但最终是否绘制由实际
  `FormattedText.Width` 决定；
- 最后一个周期先测量并保留 `lastLeft` 区域；中间候选不得侵入末标签左侧 8 像素安全区；
- 已绘制标签的右边界保存在 `previousRight`，下一候选左边界不足 8 像素时直接跳过；
- 极窄场景优先保留最后周期，避免为同时显示首尾而产生重叠。

主界面帮助折叠：

- `HelpIconButtonStyle` 在分析页和会话页保持相同的 19 × 19、透明背景、主题描边与 Help 光标；
- Tooltip 显示时长设为 60 秒，按钮使用 `LOCPlaytimeInsightsHelp` 提供中英文自动化名称；
- 分析范围、异常检测、跨小时/跨日分配、星期筛选、日历、趋势交互和底部数据口径不再占用正文行；
- 会话页导入安全说明移到加载按钮旁的帮助图标；
- 状态、错误、范围摘要、实际筛选数量和已有会话分页数量不属于帮助文本，继续常驻；
- `SessionDetailVisibility` 初始及刷新后为 Collapsed，点击趋势或热力格执行
  `LoadSessionDetails` 后切为 Visible；因此空白状态不再显示“未显示会话”面板。

会话管理操作层级与紧凑表格：

- 数据工具面板使用两列 Grid：左侧 WrapPanel 固定放置导入、CSV 导出和 JSON 导出，右侧按钮
  通过原生 `ContextMenu` 打开“高级选项”；
- 高级菜单包含所选会话的软删除/恢复，以及完整备份、备份恢复、重建索引和诊断报告；
  `ContextMenu.DataContext` 显式绑定回 `PlacementTarget.DataContext`，删除与恢复菜单项继续响应
  `CanDelete` / `CanRestore`；
- 原顶部区保留补录、编辑和刷新，避免高危操作与高频编辑并列；
- `ListBox.AlternationCount=2` 配合容器 `AlternationIndex` 生成斑马纹；偶数视觉行使用
  `#202A2A2E` 半透明中性叠色，Hover 使用 `#384A90E2`，选中使用更强的 `#484A90E2`
  并保留左侧主题色边线；
- 行高由 48 降至 44；游戏列使用 24 × 34 封面和固定占位，转换器继续以 OnLoad 方式解码，
  不锁定 Playnite 媒体文件；
- `SessionManagementViewModel.ApplyCoverImages` 根据 `GameId` 调用
  `playniteApi.Database.GetFullFilePath`，缺失游戏、缺失封面和路径异常均回退为空；
- 开始时间与时长的列头/数据均右对齐；来源与状态居中显示为圆角 Tag；
- 来源默认追踪为蓝色、异常恢复为橙色、导入为紫色、手动为中性色；有效状态为绿色，
  已删除状态为红色；
- 200 条分页、Recycling 虚拟化、横向溢出和键盘选择行为保持不变。

主看板嵌套滚轮接力：

- 外层纵向滚动器命名为 `DashboardScrollViewer`；
- 24 小时分布、星期 × 小时热力图、日历热力图的横向 `ScrollViewer`，以及异常 `ListBox`、
  会话钻取 `ListView`，统一绑定 `NestedScrollViewer_PreviewMouseWheel`；
- 横向图表没有可用纵向范围，因此滚轮事件直接重新路由到外层 `Mouse.MouseWheelEvent`；
- 对内部列表通过其模板中的 `ScrollViewer.VerticalOffset` 与 `ScrollableHeight` 判断当前方向：
  向上且偏移大于零、或向下且未到底时保留列表滚动，否则交给整页；
- 内层 `ScrollViewer` 通过视觉树按需查找，不依赖 Playnite 主题的具体列表模板层级；
- 转发只发生在子控件无法继续滚动时，横向滚动条、Recycling 虚拟化和列表内部滚动均保持不变。

0.9.4 公开界面与元数据清理：

- 分析页和会话页删除阶段性副标题 TextBlock，只保留稳定页面标题；
- 中英资源同步移除 `LOCPlaytimeInsightsDashboardSubtitle` 与
  `LOCPlaytimeInsightsSessionsSubtitle`，没有保留未使用版本字符串；
- `extension.yaml` 和程序集统一为 0.9.4 / 0.9.4.0；
- 清单新增 Source code、Issue tracker 和 Changelog 三个 HTTPS 链接，Toolbox 打包后内容保持；
- README 按用户文档重写，覆盖真实功能、数据口径、兼容性、安装升级、使用、隐私、限制、
  构建测试、反馈和 MIT License，不再描述已删除的聚合柱形图；
- 第 61 项回归锁定版本、链接、副标题清理和 README 必备章节。

## Git 与发布源码边界

- 项目根目录 `.gitignore` 是发布仓库的唯一忽略规则入口；
- `bin`、`obj`、`TestResults`、覆盖率结果、`dist`、`staging`、`.pext`、IDE 用户设置、
  日志、转储和临时文件均为本机生成物，不进入源码历史；
- 如果 `ExtensionsData`、`sessions.json`、导出或备份被误复制到项目根目录，也会被忽略，
  防止用户会话数据进入提交；
- C#、XAML、项目文件、清单、本地化、正式图标、`Assets` 设计源和 `docs\audit` 审查证据
  应继续纳入版本控制；
- 当前目录尚未初始化为 Git 仓库，因此本轮没有创建提交、分支或标签；
- 项目尚无 `LICENSE`。公开发布前必须由维护者明确选择许可证，不能根据现有源码自动代选；
- 完整发布顺序和核对项见 `docs\RELEASE_CHECKLIST.md`。

## 已知技术债

- 恢复精度受一分钟检查点间隔限制；
- JSON 文件会随会话数量增长；10 万会话读取已通过预算，写入和更高数量级仍需观察；
- 目前是轻量独立测试执行器，尚未引入完整测试框架或 Playnite API mock；
- Fullscreen 模式尚未支持。

## 侧边栏图标

2026-07-28 已制作并接入两枚独立图标：

- `Assets\SidebarIcons\icon-dashboard.png`：64 × 64 RGBA，“时钟 + 递增柱形图”；
- `Assets\SidebarIcons\icon-sessions.png`：64 × 64 RGBA，“三行会话列表 + 时钟徽标”；
- `Assets\SidebarIcons\sidebar-icons-preview.png`：深色、浅色背景以及 64/32/20 像素组合预览；
- `*-master.png` 为去背后的高分辨率母版，`*-chroma-source.png` 为内置图像生成工具输出的键色源；
- `preview-*-32.png` 与 `preview-*-20.png` 用于缩小辨识度审查，不作为发布入口文件。

生成与处理约束：

- 使用内置图像生成工具分别生成两项独立资产；
- 采用纯 `#ff00ff` 键色背景，再通过技能内置 `remove_chroma_key.py` 生成 alpha；
- 按非透明边界裁剪，等比缩放到 54 × 54 内并居中到 64 × 64，四周保留约 5 像素安全区；
- 两枚正式图标均为 RGBA，四角 alpha 为 0；
- 深色和浅色背景以及 20 像素预览均已人工检查，主要语义仍可区分。

资源校验：

- `icon-dashboard.png` SHA-256：
  `5AA4534F878D51C461A7BEDBABD4F78BCB793BAFCAB825651C730E37EAACD47A`；
- `icon-sessions.png` SHA-256：
  `191BF654C79BACD85230DC7667A4916EE5F991085A63C15BDF6EDAF5E6BED40A`；
- `sidebar-icons-preview.png` SHA-256：
  `7629D0A0C91CACE4E4E0D8F372DA6C1066C1145AA46DE0626B32CD83D314080A`。

接入和验证结果：

- 两枚正式 PNG 已复制到项目根目录并由 `PlaytimeInsights.csproj` 发布；
- 分析 `SidebarItem.Icon` 指向 `icon-dashboard.png`，会话管理指向 `icon-sessions.png`；
- 新增 PNG 文件存在性、64 × 64 尺寸、8 位 RGBA 色型、独立绑定和构建复制规则回归；
- 0.9.2 PEXT 现在包含 9 个文件，新增两枚正式侧边栏 PNG；
- Seaside 深色主题真实客户端实测中，蓝色分析图标和紫色会话图标在约 32 像素侧栏与选中光晕下
  均清晰可辨，点击后分别打开正确页面；
- 浅色背景和 20/32 像素视觉检查基于组合预览完成，默认浅色 Playnite 主题仍可由用户追加确认。
