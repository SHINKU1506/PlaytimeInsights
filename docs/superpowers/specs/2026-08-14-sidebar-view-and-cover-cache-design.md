# 侧边栏 View 复用与封面缓存设计

日期：2026-08-14
状态：已实施并部署；自动化、热点复验与宿主加载通过，交互体感待人工确认
适用分支：`main`

关联文档：

- `docs/superpowers/specs/2026-08-13-sidebar-lag-root-cause-analysis.md`；
- `docs/superpowers/specs/2026-08-12-sidebar-navigation-performance-design.md`；
- `docs/ARCHITECTURE.md`。

## 目标

本批次修复两个已经通过热点测试确认的问题：

1. Dashboard 每次从侧边栏打开都会创建新的完整 WPF View，重复支付约 157–160 ms 的首次布局成本；
2. 相同封面路径会被 `CoverImageConverter` 重复同步解码，当前真实数据测试的一轮转换约
   175–263 ms。

实现必须保持现有筛选状态、刷新语义、会话管理生命周期和 XAML 绑定兼容。

## 范围

本批次包含：

- 缓存并复用单个 `PlaytimeInsightsDashboardView`；
- 保留已有的单个 `DashboardViewModel` 缓存；
- 新增进程级、有界、文件戳失效的封面缩略图缓存；
- 让两个页面现有的 `CoverImageConverter` 资源共享同一缓存；
- 增加运行时回归测试、结构化性能护栏和缓存行为测试；
- 更新架构、开发和根因分析文档。

本批次不包含：

- 复用 `SessionManagementView`、`SessionManagementViewModel` 或 Coordinator；
- 后台线程或异步图片解码；
- Dashboard 数据读取/分析异步化；
- 改变 Dashboard 筛选或下钻状态的现有持久周期；
- 主动重置 Dashboard 的滚动位置、焦点等纯 View 状态；这些状态允许随缓存 View 的进程期生命
  周期保留；
- 引入新的运行时依赖；
- 修改插件语义版本号。

## 方案选择

采用“Dashboard View 进程期复用 + 应用层有界 LRU 封面缓存”。

没有选择仅移除 `BitmapCreateOptions.IgnoreImageCache`，因为 WPF URI 缓存没有明确容量、失效和测试
边界。没有选择异步解码，因为它需要专用 STA、冻结对象回传、占位状态和过期结果治理，会扩大本次
修复面。没有复用会话页 View，因为它的 View 构造时绑定 Coordinator；安全复用需要重构 Coordinator
替换和旧对象可回收语义，应单独设计。

## Dashboard View 生命周期

`PlaytimeInsights` 新增一个插件实例字段：

```csharp
private PlaytimeInsightsDashboardView cachedDashboardView;
```

Dashboard `SidebarItem.Opened` 的顺序固定为：

1. 若 `cachedDashboard` 为空，创建一次 `DashboardViewModel`；
2. 若 `cachedDashboardView` 为空，创建一次 View，并把 `DataContext` 设置为 `cachedDashboard`；
3. 将 `activeDashboard` 指向 `cachedDashboard`；
4. 返回 `cachedDashboardView`。

`Closed` 继续只把 `activeDashboard` 置空，不清除 View 或 ViewModel 缓存，不清空 DataContext。
插件/Playnite 退出后，插件实例及缓存自然释放。

View 的 `Loaded` 是唯一自动刷新**事件入口**；每次 `Loaded` 事件最多触发一次
`Refresh(DataReload)`。`Opened` 和 `Closed` 不读取数据、不调用 `Refresh()`。因此关闭期间的数据变化
仍会在下次 Loaded 时进入页面，且不会恢复阶段 1 已消除的双重刷新。

`Loaded` 不与一次 Sidebar navigation 建立严格的一一对应关系。WPF 可能在主题或模板导致视觉树失效
时触发额外的 `Unloaded → Loaded`；这种额外 Loaded 仍按现有策略执行一次 DataReload。设计约束是
“每次 Loaded 最多刷新一次”，而不是“每次导航恰好只有一个 Loaded”。

本设计依赖 Playnite 在页面切换时把 Control 从视觉树移除，并在再次显示时重新触发 `Loaded`。除源码
结构测试外，必须在 STA WPF 测试中验证同一 Control 的卸载/重新挂载会再次触发 Loaded，并在真实
Playnite 宿主中完成客户端复验。测试不得假设 Loaded 只能由侧边栏切换触发。

复用完整视觉树意味着 DashboardScrollViewer 的滚动偏移、控件焦点等纯 View 状态可以随缓存 View
保留。本批次不增加“关闭时重置”代码去模拟旧 View 重建的副作用；筛选和下钻等 ViewModel 状态继续
沿用既有缓存语义。

### 生命周期与失效

- 游戏、会话和设置变化：不重建 View，由下次 DataReload 刷新数据；
- 窗口大小与 DPI 变化：依赖 WPF 正常重测重排；
- 语言变化：DynamicResource 和下一次刷新负责更新；
- 主题变化：缓存 View 会延长 StaticResource 的生命周期。这是既有 XAML 的潜在限制；本批次通过
  客户端主题切换测试验证。若发现实际陈旧资源，后续把对应资源改为 DynamicResource 或增加明确的
  View 失效入口，不在 `Opened` 中无条件重建。

## 封面缓存

新增 `Services/CoverImageCache.cs`，职责仅限封面文件戳、缩略图解码、缓存命中与淘汰。XAML 和
ViewModel 继续传递完整路径字符串，不改变绑定模型。

### API 与共享方式

缓存公开一个同步入口：

```csharp
BitmapSource GetOrLoad(string path, int decodePixelWidth)
```

`CoverImageConverter` 使用进程级共享缓存实例，默认宽度保持 `96`。Dashboard 与会话页各自在 XAML
中创建 Converter，但它们命中同一个缓存，不因 View 或 Converter 实例不同而重复解码。

### 缓存键

键由以下两部分组成：

- `Path.GetFullPath(path)` 得到的规范化绝对路径；
- `decodePixelWidth`。

路径比较使用 `StringComparer.OrdinalIgnoreCase`，符合当前仅支持 Windows 的运行环境。同一路径的 `.`
变体和大小写变体命中同一宽度条目；不同解码宽度互不复用。

无效、空白或无法规范化的路径返回 `null`，不得抛出到 WPF 绑定管线。

### 文件失效

每个条目保存：

- 文件长度；
- `LastWriteTimeUtc`；
- 冻结的 `BitmapSource`；
- LRU 节点。

每次访问读取当前文件戳：

- 文件不存在：移除已有条目并返回 `null`；
- 长度和修改时间一致：更新 LRU 顺序并返回同一 BitmapSource；
- 任一字段变化：重新解码并原子替换条目；
- 文件戳读取或解码失败：移除陈旧条目并返回 `null`。

文件戳不能发现长度和修改时间都未变化的原地内容覆盖；这在 Playnite 封面管理中概率低，接受该边界，
避免每次命中计算内容哈希。

### 解码与线程语义

首次未命中仍在调用线程同步解码：

- `BitmapCacheOption.OnLoad`；
- `BitmapCreateOptions.IgnoreImageCache`；
- `DecodePixelWidth = decodePixelWidth`；
- `EndInit()` 后 `Freeze()`。

保留 `IgnoreImageCache`，因为一致性和失效由本缓存负责。`OnLoad` 保证文件句柄在初始化后释放；冻结后
的对象可以被多个 Image 复用，也为未来迁移到后台 STA 解码保留线程安全前提。

本批次不在线程池解码 WPF 对象。

### 容量与并发

缓存上限固定为 512 个“路径 + 宽度”条目。插入第 513 项时淘汰最久未使用项。当前典型 96 px 封面
按约 96×96、32 位像素估算约 36 KiB/项，512 项约 18 MiB 像素数据。实际占用随封面宽高比和 WPF
对象开销变化，因此 512 是条目数量上限，而非严格内存上限。

缓存内部用一个私有锁保护字典和 LRU 链。文件戳读取和解码不在锁内执行；提交结果时再次检查已有
条目：若相同键已存在文件戳相同的结果，则丢弃重复解码结果并返回现有对象；否则提交本次结果。
当前锁策略只承诺缓存容器结构线程安全，并为未来后台解码预留边界，不承诺并发解码时结果一定最新。
若后续引入并发解码，还必须在解码完成后重新读取文件戳，并通过代际或等价机制抑制过期结果，防止
较早启动、较晚完成的解码覆盖新文件结果。

为保证实现可测，文件戳读取和图片解码通过内部接口注入。默认实现使用 `FileInfo` 与 `BitmapImage`；
测试使用计数型假实现验证命中、失效和淘汰，无需依赖真实解码时间。

## 错误处理

- View 缓存创建失败沿用现有侧边栏异常行为，不吞掉 XAML 初始化错误；
- 封面路径、文件戳和解码错误均在缓存边界转换为 `null`，保持当前“无封面则不显示图片”的行为；
- 缓存不记录用户路径到普通日志；
- 缓存失败不影响会话或分析数据刷新。

## 测试设计

### TDD 红灯

生产改动前新增并运行以下测试，确认它们因当前行为失败：

1. `Sidebar navigation reuses Dashboard View`：两次 Opened 及 Closed 后重新 Opened 返回相同 View 和
   DataContext 实例；
2. `Dashboard reentry preserves visual tree`：首次布局后重新进入仍持有相同 DashboardScrollViewer，
   且视觉节点数量不增长；
3. `Dashboard cache keeps one Loaded refresh boundary`：源码护栏确保只创建一次 View、Closed 不清缓存、
   Opened 不刷新、每次 Loaded 最多执行一次 DataReload，且不假设 Loaded 仅由导航触发；
4. `Cover cache reuses normalized path`：同一路径及等价路径只调用一次解码器并返回同一对象；
5. `Cover cache invalidates changed and missing files`：长度或修改时间变化会重解码，文件删除返回 null；
6. `Cover cache separates widths and evicts LRU`：不同宽度独立，超容量淘汰最久未使用条目；
7. `Cover decoder returns frozen thumbnail`：真实临时 PNG 的返回值已冻结，宽度不超过 96，文件可在
   解码后替换或删除。

测试应先观察红灯，再实施生产代码，再观察绿灯。对于回归测试，若不能通过恢复旧实现证明红灯，则
不将其声称为有效回归测试。

### 性能护栏

自动测试不使用绝对毫秒阈值，避免受机器、JIT 和 I/O 缓存影响。通过以下结构事实证明热点被移除：

- 第二次导航返回同一 View；
- 第二次布局使用同一视觉树节点；
- 同一路径同宽度只调用一次解码器；
- 512 项容量不增长。

诊断基准可以输出首次/复用布局和首次/命中解码时间，但不作为 CI 成败条件。

### 完整验证

- Release 构建 0 警告、0 错误；
- 全部回归测试通过，包括 100,000 会话性能预算；
- 根因热点基准重跑，确认复用路径不重建 View、封面命中不重新解码；
- Playnite 本地部署后验证两个侧边栏入口、重复切入、显式刷新、游戏停止刷新、主题/DPI/语言行为；
- 源 Release 与部署目录文件 SHA-256 一致，启动日志确认插件加载成功。

## 文档同步

实施完成后同步：

- 根因分析文档的状态、实施结果和验证数据；
- `docs/ARCHITECTURE.md` 中 Dashboard View 生命周期；
- `docs/DEVELOPMENT.md` 中导航和封面缓存约束；
- 必要时更新客户端验收文档。

## 验收标准

- Dashboard 在同一 Playnite 进程中只构造一个 View；
- 每次 Loaded 最多触发一次自动 DataReload，不出现同一事件内的双刷新；
- Dashboard 的滚动位置、焦点等纯 View 状态允许随缓存 View 保留，不主动重置；
- 会话页维持每次创建新 ViewModel/Coordinator/View 的既有生命周期；
- 相同规范化路径和宽度返回相同冻结缩略图，文件变化后返回新对象；
- 封面缓存条目不超过 512；
- 无新增运行时依赖，无后台 WPF 线程访问，无用户路径日志；
- 全部自动化和客户端验收通过。
