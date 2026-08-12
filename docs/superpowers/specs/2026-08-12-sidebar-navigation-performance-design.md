# 侧边栏切换卡顿修复设计

日期：2026-08-12

状态：已确认并实施

适用分支：`refactor/architecture-preparation`

## 问题与证据

客户端在侧边栏切换到 Playtime Insights 主看板或会话管理页时出现明显卡顿。复核
`project_stage_d_review.md`、导航入口、View 生命周期和实际用户数据后，原评审文档中的部分推断
成立，但主要原因需要修正。

### 已确认的根因

两个侧边栏页面都存在两个自动刷新入口：

1. `PlaytimeInsights.GetSidebarItems()` 的 `SidebarItem.Opened` 创建 ViewModel 后主动调用
   `Refresh()`；
2. View 创建并进入视觉树后，`Loaded` 事件再次调用同一个 `Refresh()`。

因此每次进入任一页面都会连续执行两轮同步全量刷新。每轮刷新均发生在 UI 线程，并包含以下
操作：

- 枚举 Playnite 游戏库并创建列表；
- 从会话仓库取得克隆后的会话集合；
- 读取库插件名称和重建元数据筛选项；
- 过滤会话、构建指标或会话表格条目；
- 解析封面路径；
- 清空并重新填充多个可观察集合。

重复执行整条链路会直接延长页面出现前的 UI 阻塞时间。这一调用关系可由源码稳定复现，不依赖
主观体感。

### 次要的可扩展性问题

会话管理页的 `Refresh()` 已调用 `GetAllIncludingDeleted()`，但绑定属性 `CountText` 又调用
`repository.GetAll()` 计算全部有效会话数量。属性通知和 WPF 绑定取值时会再次克隆、排序会话，
在大会话库中构成额外的 O(n log n) 工作。

当前本机数据只有 18 条会话，其中 2 条已删除；`sessions.json` 约 14 KB。因此该问题不是当前
卡顿的主要来源，但可以在同一刷新快照中零风险消除。

## 对原评审文档的修正

### 有依据的部分

- 两个页面切入时都会执行同步全量刷新；
- 每轮刷新都会枚举 Playnite 游戏库和会话集合；
- Dashboard 已缓存而会话页未缓存，生命周期设计确实不对称。

### 证据不足或表述不准确的部分

- `SessionManagementViewModel` 有约 678 行并不能证明构造昂贵。构造函数只初始化服务引用、选项
  集合、分页器和命令，源文件行数不是性能指标；
- 当前会话库很小，无法把仓库读取认定为本机主要瓶颈；
- `Games.ToList()` 可能有成本，但尚无分段计时证明它单独占主导。已确认的问题是包含该操作的
  整条刷新链被执行了两次；
- 直接缓存 `SessionManagementViewModel` 会延长选择状态、Coordinator 和 Window Owner 相关对象
  的生命周期，不应在没有性能证据时作为首选修复；
- 插件级缓存游戏列表还需要监听游戏新增、删除和元数据更新并正确失效，复杂度和回归风险超出
  当前问题所需范围。

## 修复设计

### 1. 每次导航只保留一个自动刷新入口

删除两个 `SidebarItem.Opened` 中的主动 `Refresh()`：

- Dashboard 仍复用插件运行期缓存的 ViewModel，以保留范围、粒度、排名、元数据筛选和自定义
  日期；
- SessionManagement 仍按当前行为创建新的 ViewModel、Interaction、Coordinator 和 View；
- 两个 View 的 `Loaded` 事件负责页面进入后的唯一一次自动刷新；
- 用户点击刷新按钮、修改筛选、会话 CRUD 和游戏停止事件等显式刷新路径保持不变。

将刷新留在 `Loaded` 的理由是，此时 `DataContext` 已设置且 View 已进入正常生命周期。这样不会
依赖 `Opened` 返回 View 之前的手工初始化顺序，也不会改变现有 XAML 或命令绑定。

### 2. 会话总数来自当前刷新快照

在 `SessionManagementViewModel` 中保存本轮 `GetAllIncludingDeleted()` 结果计算出的有效会话总数。
`CountText` 读取该内存字段，不再访问 Repository。

刷新流程为：

```text
GetAllIncludingDeleted 一次
  → 计算未删除会话总数
  → 筛选并创建 UI 条目
  → 更新分页器
  → 通知 CountText、分页和命令状态
```

所有新增、编辑、软删除、恢复、导入和备份恢复路径本来都会调用 `Refresh()`，因此总数会随数据
变更同步更新。

## 明确不做

- 不缓存 SessionManagement ViewModel；
- 不缓存 Playnite 游戏列表；
- 不引入数据库事件订阅或版本号失效机制；
- 不将同步刷新改为异步；
- 不改变 Dashboard 的运行期筛选保留语义；
- 不改变统计口径、会话 schema、插件 ID、视觉布局或本地化文案；
- 不修改用户未纳入版本控制的 `perf_test.ps1`。

如果去除双刷新后客户端仍有明显卡顿，再单独增加分段计时，分别测量游戏枚举、会话克隆、元数据
选项、分析服务、封面解析和可观察集合投影，依据数据决定第二阶段优化。

## 测试设计

实施按测试驱动顺序进行。

### 导航刷新入口回归

新增静态架构测试，要求：

- Dashboard `Opened` 不调用 `activeDashboard.Refresh()`；
- SessionManagement `Opened` 不调用 `activeSessionManagement.Refresh()`；
- Dashboard View 保留 `Loaded` 到 `RefreshCommand` 的唯一自动入口；
- SessionManagement View 保留 `Loaded` 到 ViewModel `Refresh()` 的唯一自动入口；
- Dashboard 缓存和 `Closed = () => activeDashboard = null` 语义保持。

测试必须先在现状代码上以“双刷新仍存在”为预期原因失败，再修改生产代码。

### 会话计数回归

新增静态边界测试，要求 `CountText` 不包含 `repository.GetAll()`，并验证刷新使用
`GetAllIncludingDeleted()` 的同一结果计算有效总数。该测试同样先失败，再实施最小修改。

### 完整回归和性能

- Release 构建 0 警告、0 错误；
- 现有 82 项回归与新增测试全部通过；
- 10 万会话分析预算不得退化超过 10%；
- Release、staging 和插件安装目录的 9 个文件逐项哈希一致；
- 部署前后用户数据文件数量与联合指纹不变。

## 客户端验收

1. 打开 Dashboard，确认只出现一次刷新结果且页面切入更顺畅；
2. 选择“本年”等非默认范围，切换页面再返回，筛选仍保留；
3. 返回 Dashboard 后数据已刷新，但星期选中、下钻和分页按既有行为重置；
4. 打开会话管理页，确认计数、筛选、封面和分页正常；
5. 点击两个页面的刷新按钮，确认仍能执行显式刷新；
6. 完全重启 Playnite，Dashboard 恢复默认“本月”。

## 完成定义

- 每个侧边栏页面每次进入只有一次自动刷新；
- 会话页 `CountText` 不再读取 Repository；
- 显式刷新和数据变更后的刷新均保持；
- Dashboard 运行期筛选缓存不退化；
- 自动化、构建、性能、部署和数据保护验收全部通过；
- 修正后的根因、实施结果和客户端检查步骤同步到项目状态文档。

## 实施结果

2026-08-12 已按本设计完成修复：

- 删除 Dashboard 和 SessionManagement 两个 `SidebarItem.Opened` 中的主动刷新；
- 两个 View 的 `Loaded` 成为页面进入时唯一自动刷新入口；
- Dashboard 的运行期 ViewModel 缓存、筛选保留、显式刷新和数据变更刷新保持不变；
- 会话页 `CountText` 改为读取本轮 `GetAllIncludingDeleted()` 快照计算的活动会话总数，不再在
  WPF 读取绑定属性时访问 Repository；
- 两项新回归均先在旧代码上按预期失败，再由最小生产改动转为通过；
- 干净 Release 构建 0 警告、0 错误，84/84 项回归通过，10 万会话分析为 483 ms；
- 未缓存会话 ViewModel 或游戏列表，未引入异步、数据库事件订阅或新依赖。
