# Playtime Insights 架构重构行为基线

状态：重构准备基线

记录日期：2026-08-11

适用分支：`refactor/architecture-preparation`

关联计划：`docs\ARCHITECTURE_OPTIMIZATION_PLAN.md`

## 目的

本文记录命令与协调器迁移前的可见行为、事件职责、启用条件和数据副作用。后续重构允许删除
已迁移的事件处理器，但不得在没有更新本基线和自动化护栏的情况下改变这些行为。

阶段 0 没有修改生产代码。阶段 A 已新增尚未接线的交互接口、操作接口和 Coordinator，并让
`SessionManagementViewModel` 实现操作接口；现有 View/XAML 仍走 0.9.8 路径，因此不改变存储
schema、插件 ID、统计口径、对话框和客户端行为。

## 当前组装边界

- `PlaytimeInsights.cs` 创建 `DashboardViewModel` 和 `SessionManagementViewModel`，并向其传入
  Playnite API、仓库、查询、分析、导入导出和诊断服务；
- View 通过 `DataContext` 获取 ViewModel；ViewModel 不创建具体窗口；
- `SessionManagementView.xaml.cs` 当前负责文件选择、确认、窗口 Owner 和多步骤流程；
- `PlaytimeInsightsDashboardView.xaml.cs` 当前负责自定义事件适配和嵌套滚轮接力；
- `SessionEditorWindow` 与 `SessionImportPreviewWindow` 负责自身对话框生命周期；
- `SessionManagementCoordinator` 当前只由自动化测试实例化，尚未接入真实 View；
- ViewModel 可以使用 `Visibility`、`Geometry`、`PointCollection` 等 WPF 表示层值，但禁止依赖
  `Window`、文件对话框、`MessageBox` 或具体窗口类型。

## 主页面事件职责矩阵

### SessionManagementView

| 处理器 | 当前分类 | 当前行为 | 计划迁移 |
|---|---|---|---|
| `SessionManagementView_Loaded` | UI 适配 | 首次加载时刷新 ViewModel | 保留 View 生命周期适配 |
| `RefreshButton_Click` | 简单命令 | 调用 `Refresh()` | 阶段 B：`RefreshCommand` |
| `AdvancedOptionsButton_Click` | UI 适配 | 设置 PlacementTarget 并打开 ContextMenu | 保留在 View |
| `LoadMoreButton_Click` | 简单命令 | 调用 `LoadMore()` | 阶段 B：`LoadMoreCommand` |
| `AddSessionButton_Click` | 多步骤工作流 | 打开空白编辑器并在确认后补录 | 阶段 C：Coordinator |
| `EditSessionButton_Click` | 多步骤工作流 | 读取选中会话并在确认后更新 | 阶段 C：Coordinator |
| `DeleteSessionButton_Click` | 多步骤工作流 | 危险确认后软删除 | 阶段 C：Coordinator |
| `RestoreSessionButton_Click` | 简单命令 | 恢复选中的已删除会话 | 阶段 B：`RestoreSelectedCommand` |
| `ExportCsvButton_Click` | 多步骤工作流 | 选择保存路径后导出 CSV | 阶段 C：Coordinator |
| `ExportJsonButton_Click` | 多步骤工作流 | 选择保存路径后导出 JSON | 阶段 C：Coordinator |
| `ImportButton_Click` | 多步骤工作流 | 多文件选择、预览、确认、提交和错误呈现 | 阶段 C：Coordinator |
| `BackupButton_Click` | 多步骤工作流 | 选择路径并创建完整备份 | 阶段 C：Coordinator |
| `RestoreBackupButton_Click` | 多步骤工作流 | 选择、预览、验证、确认并恢复备份 | 阶段 C：Coordinator |
| `ReindexButton_Click` | 多步骤工作流 | 危险确认后重建索引 | 阶段 C：Coordinator |
| `DiagnosticsButton_Click` | 多步骤工作流 | 选择路径并保存隐私诊断 | 阶段 C：Coordinator |

辅助流程 `OpenEditor`、`Export` 和 `ShowDataError` 随阶段 C 一并收敛到强类型交互实现与
Coordinator；窗口 Owner、默认文件名、文件过滤器和本地化错误文案仍由 WPF 交互实现负责。

### PlaytimeInsightsDashboardView

| 处理器 | 当前分类 | 当前行为 | 计划迁移 |
|---|---|---|---|
| `PlaytimeInsightsDashboardView_Loaded` | UI 适配 | 首次加载时刷新 | 保留生命周期适配 |
| `RefreshButton_Click` | 简单命令 | 刷新完整分析快照 | 阶段 B：`RefreshCommand` |
| `AdaptiveTrendChart_PeriodSelected` | UI 适配 | 将自定义事件参数传给 `SelectPeriod` | 保留一行适配或转发命令 |
| `HeatmapCell_MouseLeftButtonUp` | UI 适配 | 从 Tag 取热力格并下钻 | 保留适配或迁移为参数命令 |
| `WeekdayDistribution_Click` | UI 适配 | 从 Tag 取星期柱并切换筛选 | 阶段 B：参数命令 |
| `LoadMoreSessionDetails_Click` | 简单命令 | 加载下一批下钻会话 | 阶段 B：命令 |
| `NestedScrollViewer_PreviewMouseWheel` | 纯视觉行为 | 内层到边界后把滚轮转发给外层 | 保留在 View |

`CanContinueVerticalScroll`、`FindVisualChild<T>` 和外层滚轮事件重建属于纯 View 逻辑，不迁移
到 ViewModel，也不引入 Behavior 依赖。

## 对话框事件职责矩阵

| 处理器 | 当前分类 | 当前行为 | 迁移约束 |
|---|---|---|---|
| `SaveButton_Click` | UI 适配 | 调用编辑器 `TryBuild`，成功后设置 Result 与 DialogResult | 可以保留 |
| `ImportButton_Click` | UI 适配 | 确认预览并设置 DialogResult | 可以保留 |
| `SaveErrorsButton_Click` | 多步骤工作流 | 选择路径并保存导入错误报告 | 由专用交互方法承接或保留在窗口 |

两个窗口的 `Loaded` 匿名处理器只调用 `WindowLayoutService.ConstrainToWorkArea`，属于窗口生命
周期与高 DPI 布局职责，必须留在 View。

## 按钮启用与分页基线

| 操作 | 当前条件 |
|---|---|
| 编辑 | `CanEdit`：存在选中会话且未删除 |
| 软删除 | `CanDelete`：存在选中会话且未删除 |
| 恢复会话 | `CanRestore`：存在选中会话且已删除 |
| CSV/JSON 导出 | `HasFilteredSessions`：筛选结果非空 |
| 确认导入 | `CanImport`：预览包含可导入候选 |
| 会话页加载更多 | `LoadMoreVisibility`：200 条分页器仍有下一页 |
| 主看板下钻加载更多 | `LoadMoreVisibility`：100 条分页器仍有下一页 |
| 刷新 | 当前按钮始终可用，内部 `RefreshReentrancyGuard` 拒绝嵌套刷新 |
| 补录、导入、备份、恢复备份、重建、诊断 | 当前入口始终可用，处理器先检查 ViewModel/选择结果 |

阶段 B 引入命令后，条件来源仍由 ViewModel 状态驱动；`SelectedSession`、筛选结果、分页器和
刷新状态变化必须显式触发 `CanExecuteChanged`。

## 键盘、焦点与无障碍基线

- 所有主要按钮继续使用本地化 `AutomationProperties.Name`；
- 主页面保持自然 Tab 顺序，不在命令迁移时重排视觉树；
- `SessionEditorWindow` 使用 `KeyboardNavigation.TabNavigation="Cycle"`，初始焦点为
  `GameSelector`，取消按钮为 `IsCancel="True"`，保存按钮为 `IsDefault="True"`；
- 编辑窗口 TabIndex 顺序为游戏 0、日期 1、时间 2、时长 3、取消 4、保存 5；
- `SessionImportPreviewWindow` 使用循环 Tab；取消按钮为 `IsCancel="True"`，确认导入为
  `IsDefault="True"`，并由 `CanImport` 控制；
- ContextMenu 继续从 `PlacementTarget.DataContext` 取得会话页 ViewModel；
- Access key、Automation Name、默认/取消按钮语义不得因 Click → Command 迁移而消失。

## 取消、失败与数据副作用基线

| 场景 | 必须保持的结果 |
|---|---|
| 取消导入文件选择 | 不预览、不提交、不写会话 |
| 导入预览后取消 | 可以生成只读预览和状态文本，不提交候选、不创建回滚备份 |
| 删除确认取消 | 不调用软删除，不触发 `dataChanged` |
| 无效备份恢复 | 只显示错误，不替换会话、不创建恢复回滚备份 |
| 恢复确认取消 | 不恢复、不创建恢复回滚备份 |
| 导出路径选择取消 | 不创建文件，不修改会话 |
| 导出写入失败 | 显示错误，不修改会话集合 |
| 编辑或补录窗口取消 | 不调用 Add/Update，不修改会话 |
| 重建索引确认取消 | 不调用 Reindex，不创建回滚备份 |
| 备份路径选择取消 | 不创建备份 |
| 诊断路径选择取消 | 不创建报告 |
| 任一工作流抛出异常 | 进入本地化错误呈现路径，不静默吞掉 |

阶段 A 的 8 项假交互测试已覆盖表中要求的取消、拒绝、无效恢复和导出失败路径。危险操作的
自动回滚备份只能由现有 Repository 操作在真正提交时创建，不能在预览或取消路径提前创建。

## 自动化架构护栏

测试 `Architecture refactor baseline keeps boundaries documented` 执行以下检查：

1. 扫描四个 XAML 文件中的 Click、自定义事件、鼠标和滚轮处理器，要求每个处理器都出现在
   本文职责矩阵；
2. 禁止三个 ViewModel 源文件引用 MessageBox、文件对话框、具体窗口或 `Window.GetWindow`；
3. 禁止项目引入 CommunityToolkit.Mvvm、Prism、ReactiveUI 或 Microsoft.Xaml.Behaviors；
4. 锁定当前编辑/删除/恢复/导出/导入和分页条件；
5. 锁定编辑与导入窗口的循环 Tab、默认按钮、取消按钮和初始焦点语义。
6. 锁定强类型交互接口和 Coordinator 不引用 WPF、MessageBox、文件对话框或具体窗口。

护栏不要求 Code-behind 行数为零，也不强制保留已经迁移的处理器。事件从 XAML 删除后可以从
职责矩阵的“当前处理器”部分移入迁移记录；新增事件必须先分类。

## 阶段 A 结果与进入阶段 B 的条件

- 当前 61 项发布回归、1 项架构护栏和 8 项工作流回归共 70 项均通过；
- Release 构建保持 0 警告、0 错误；
- 生产代码与 0.9.8 行为一致；
- 强类型交互边界和取消、拒绝、无效输入、文件失败测试已建立；
- 阶段 B 只迁移低风险命令，不同时改变视觉布局、存储或异步模型；
- Coordinator 在阶段 C 接线前不得绕过现有 WPF 文案、Owner、默认文件名和过滤器语义。
