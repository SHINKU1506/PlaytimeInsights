# Playtime Insights 主体架构优化计划

状态：重构准备、阶段 A 与阶段 B 已完成；阶段 C 待实施

规划日期：2026-08-11

来源：`implementation_plan.md` 架构部分及现有代码复审

## 重构准备状态

2026-08-11 已在 `refactor/architecture-preparation` 分支完成阶段 0：

- 新增 `docs\ARCHITECTURE_REFACTOR_BASELINE.md`，逐项记录主页面与对话框事件分类；
- 锁定按钮启用条件、分页、刷新重入、键盘、焦点、默认/取消按钮和无障碍语义；
- 明确取消、拒绝、无效输入和文件失败路径允许的副作用；
- 新增第 62 项自动化护栏，动态检查 XAML 事件均已分类，并禁止 ViewModel 引入具体 WPF
  对话框/窗口类型或外部 MVVM/Behavior 框架；
- 未修改生产代码、存储 schema、插件 ID、统计口径或客户端可见行为。

阶段 0 完成时尚不代表阶段 A 完成；因此后续先建立可替换的强类型交互边界和假交互测试，再
进入低风险命令迁移，没有跳过取消/失败路径直接改写 XAML Command。

### 阶段 A 完成记录

同日已继续完成阶段 A：

- 新增 `Presentation\Interactions\ISessionManagementInteraction.cs`，接口只表达路径选择、确认、
  编辑结果和错误呈现，不暴露任何 WPF 窗口/对话框类型；
- 新增 `Presentation\Coordinators\ISessionManagementOperations.cs`，将协调器需要的业务操作从具体
  ViewModel 中抽象出来，`SessionManagementViewModel` 仅增加接口声明，执行逻辑不变；
- 新增 `SessionManagementCoordinator`，覆盖导入导出、备份恢复、补录编辑、软删除、重建和诊断
  的取消、确认、异常捕获与调用顺序；
- 新增 8 项假交互工作流回归，覆盖阶段 A 要求的全部取消/无效/失败场景；当前总计 70 项测试；
- 协调器尚未接入 `SessionManagementView`，没有修改真实对话框、XAML 事件或客户端执行路径。

阶段 B 只引入 RelayCommand 并迁移低风险命令；WPF 交互实现及 Coordinator 正式接线仍留在阶段 C。

### 阶段 B 完成记录

2026-08-12 已完成阶段 B：

- 新增插件自有 `RelayCommand` 与 `RelayCommand<T>`，支持 CanExecute、显式
  `RaiseCanExecuteChanged()`、null/错误参数处理，未引入第三方 MVVM 依赖；
- 会话页迁移 `RefreshCommand`、`LoadMoreCommand` 和 `RestoreSelectedCommand`，删除对应三个
  Click 处理器；选中会话、分页和刷新状态变化会主动更新命令状态；
- 主看板迁移刷新、下钻分页和星期筛选按钮；趋势与热力图仍保留薄 Code-behind，但只负责把
  自定义事件参数转交 `SelectPeriodCommand` / `SelectHeatmapDateCommand`；
- 主看板加入刷新重入保护，刷新期间所有相关命令不可执行，结束后统一恢复状态；
- 修复 Coordinator 将带访问键的导出按钮标签用作错误标题的问题，新增不含助记符的
  `LOCPlaytimeInsightsExportFailedTitle`；中英资源现各 272 个键；
- 新增 4 项命令/绑定/错误标题回归，当前总计 74 项测试；Release 构建 0 警告、0 错误。

阶段 B 没有迁移文件选择、确认、编辑窗口、ContextMenu 打开、Loaded、滚轮或动画逻辑。

## 结论

项目值得引入命令、提高 UI 工作流的可测试性，并逐步拆分大型 ViewModel；但优化目标不是
“消灭 Code-behind”或追求形式上的纯 MVVM。文件选择、Window Owner、焦点、滚轮、动画和
自定义控件事件适配仍属于合理的 View 层职责。

本计划不阻塞 0.9.8 发布。所有阶段应在独立版本中小步实施，不与大规模视觉重构合并。

## 当前基线

- `SessionManagementView.xaml.cs`：约 375 行，主要承担文件选择、确认/错误弹窗、窗口创建和
  会话管理流程编排；
- `PlaytimeInsightsDashboardView.xaml.cs`：约 135 行，其中大部分为滚轮边界接力、视觉树查找
  和自定义图表事件适配；
- `SessionManagementViewModel.cs`：约 649 行，核心导入、导出、备份、恢复、重建索引和会话
  CRUD 已独立于窗口实现；
- `DashboardViewModel.cs`：约 1094 行，同时包含筛选、指标、分布、排名、下钻、分页和多个条目
  模型，是比主看板 Code-behind 更明显的长期维护点；
- 当前没有统一 `ICommand` 实现；
- 0.9.8 发布基线为 61 项自动化回归；阶段 0 新增 1 项架构护栏，阶段 A 新增 8 项工作流回归，
  阶段 B 新增 4 项命令回归，当前共 74 项。

## 优化目标

1. 标准按钮通过命令绑定统一执行与 `CanExecute`；
2. 会话管理的跨步骤 UI 流程可以使用假交互对象进行自动测试；
3. ViewModel 不直接引用 `Window`、文件对话框、`MessageBoxResult` 或具体窗口类型；
4. 保留合理的 View 层代码，不为追求纯 MVVM 引入 Behavior 依赖；
5. 拆分大型 ViewModel，降低单文件职责和刷新路径复杂度；
6. 每一阶段保持存储格式、插件 ID、会话口径和用户可见行为兼容。

## 明确不做

- 不在 0.9.8 发布冻结期实施；
- 不要求 `.xaml.cs` 只剩 `InitializeComponent()`；
- 不把滚轮、VisualTree、Window Owner、焦点或动画逻辑移入 ViewModel；
- 不创建包含所有弹窗类型的万能 `IDialogService`；
- 不在同一提交中同时重构命令、异步刷新、存储和视觉布局；
- 不引入 `Microsoft.Xaml.Behaviors` 等新运行时依赖，仅为替换一个 `Loaded` 事件。

## 目标职责边界

### View 保留

- `Loaded` 与控件生命周期；
- `PreviewMouseWheel` 和滚动边界转发；
- 视觉树查询、焦点和 Window Owner；
- 文件选择器及具体窗口的显示实现；
- `AdaptiveTrendChart.PeriodSelected` 等自定义控件事件到命令/方法的轻量适配；
- 纯视觉 Storyboard 和动画。

### ViewModel 保留

- 可观察状态、筛选状态、分页状态和 `CanExecute` 条件；
- 调用查询、分析和会话服务；
- 导入预览、提交、备份恢复、重建索引等业务操作；
- 状态文本和用户可见结果；
- 不依赖具体 Window 或文件对话框类型。

### 交互/协调层新增

- 跨越多个对话框和业务步骤的会话管理工作流；
- 将 UI 选择结果转换为 ViewModel/Service 参数；
- 捕获异常并调用强类型交互接口展示结果；
- 可以用假交互对象覆盖取消、确认、失败和成功路径。

## 阶段 A：建立重构安全网（已完成）

### 工作内容

1. 为现有 View 事件建立职责清单，分类为：
   - 简单命令；
   - UI 适配；
   - 多步骤工作流；
   - 纯视觉行为。
2. 补充当前尚未独立覆盖的工作流测试：
   - 用户取消导入文件选择；
   - 导入预览后取消；
   - 删除确认取消；
   - 无效备份恢复被阻止；
   - 恢复确认取消；
   - 文件保存失败后不修改会话；
   - 编辑窗口取消后不提交结果；
   - 重建索引取消后不创建备份。
3. 记录现有按钮启用条件和焦点/键盘行为，作为迁移基线。

### 验收

- 不改变生产代码行为；
- 原 61 项发布回归、新增架构护栏和 8 项工作流回归（共 70 项）保持通过；
- 新增工作流测试可以在不打开真实对话框的情况下执行。

## 阶段 B：引入低风险命令基础设施（已完成）

### 新文件

```text
ViewModels/
  RelayCommand.cs
  RelayCommandOfT.cs
```

也可以在同一文件中实现泛型和非泛型版本，但不应引入外部 MVVM 框架。

### RelayCommand 要求

- 实现 `ICommand`；
- 支持 `Action` / `Action<T>`；
- 支持 `Func<bool>` / `Predicate<T>`；
- 显式提供 `RaiseCanExecuteChanged()`；
- 不把 View 或控件保存在闭包中；
- 参数类型不匹配时安全返回或抛出开发期可定位异常；
- UI 命令异常必须进入现有错误呈现路径，不能静默吞掉。

### 第一批迁移命令

#### SessionManagementViewModel

- `RefreshCommand`；
- `LoadMoreCommand`；
- `RestoreSelectedCommand`（仅业务执行，不含确认弹窗）；
- 后续协调层完成后再迁移导入、导出、备份和重建索引。

#### DashboardViewModel

- `RefreshCommand`；
- `LoadMoreSessionDetailsCommand`；
- `SelectWeekdayCommand`；
- `SelectHeatmapDateCommand` 和 `SelectPeriodCommand` 可以先由 Code-behind 适配自定义事件参数。

### CanExecute 规则

- 编辑、删除：存在选中会话且未删除；
- 恢复：存在选中会话且已删除；
- 加载更多：分页器仍有下一页；
- 导出：筛选结果非空；
- 刷新期间：禁止嵌套刷新；
- `SelectedSession`、分页和刷新状态变化时必须主动刷新命令状态。

### 保留的 Code-behind

- `Loaded`；
- `AdaptiveTrendChart_PeriodSelected` 适配；
- `NestedScrollViewer_PreviewMouseWheel`；
- 视觉树查找；
- 文件和窗口交互。

### 验收

- 简单 Button 不再需要对应 Click 处理器；
- 键盘、访问键和屏幕阅读器名称保持；
- `CanExecute` 自动化覆盖全部状态组合；
- 不引入新程序集依赖。

## 阶段 C：提取会话管理交互与协调器

### 建议目录

```text
Presentation/
  Interactions/
    ISessionManagementInteraction.cs
    WpfSessionManagementInteraction.cs
  Coordinators/
    SessionManagementCoordinator.cs
```

不要把 WPF 实现放入当前承载查询、存储和分析逻辑的 `Services` 目录，以免混淆应用服务与
Presentation 服务。

### 强类型接口原则

接口应表达用户意图和领域结果，而不是暴露 WPF 类型。示意：

```csharp
public interface ISessionManagementInteraction
{
    IReadOnlyList<string> SelectImportFiles();
    string SelectExportPath(string extension);
    string SelectBackupPath();
    string SelectRestorePath();
    bool ConfirmDelete(string gameName);
    bool ConfirmRestore(SessionRestorePreview preview);
    bool ConfirmReindex();
    bool ConfirmImport(SessionImportPreview preview);
    GameSession EditSession(SessionEditorViewModel editor);
    void ShowError(string title, Exception exception);
}
```

允许返回 `null` 或空集合表示用户取消。接口中禁止出现：

- `Window`；
- `MessageBoxResult`；
- `OpenFileDialog` / `SaveFileDialog`；
- `SessionEditorWindow` / `SessionImportPreviewWindow`。

### Coordinator 职责

以导入为例：

```text
选择文件
  → 用户取消则结束
  → ViewModel.PreviewImport
  → Interaction.ConfirmImport
  → 用户确认后 ViewModel.CommitImport
  → 异常统一交给 Interaction.ShowError
```

ViewModel 继续只接收路径、预览和领域对象，不主动“打开窗口”。Coordinator 可以由命令调用，
也可以暂时由薄 Code-behind 调用，分阶段迁移。

### 必须覆盖的工作流

- CSV/JSON 导出；
- 多文件导入、预览、取消和提交；
- 完整备份创建；
- 备份预览、无效备份、确认和恢复；
- 会话补录与编辑；
- 软删除确认；
- 重建索引确认；
- 诊断报告保存；
- 所有异常路径。

### 验收

- 工作流测试不创建真实窗口；
- 用户取消任何步骤均无数据副作用；
- Window Owner、默认文件名和本地化文案在 WPF 实现中保持；
- 危险操作的确认和自动回滚备份语义不变。

## 阶段 D：拆分大型 ViewModel

### 先移动无状态条目类型

从 `DashboardViewModel.cs` 移出：

```text
ViewModels/Dashboard/
  PeriodActivityViewModel.cs
  HeatmapCellViewModel.cs
  DistributionBarViewModel.cs
  GameRankingViewModel.cs
  SessionDetailViewModel.cs
  DashboardSnapshot.cs
```

第一步只移动文件，不改变 API 和命名空间，形成可审查的机械提交。

### 再拆分职责

建议组合而非继承：

```text
DashboardViewModel
  ├─ DashboardFilterViewModel
  ├─ DashboardMetricsViewModel
  ├─ DashboardDistributionViewModel
  └─ DashboardDrilldownViewModel
```

- Filter：范围、粒度、排名和元数据筛选；
- Metrics：指标卡和比较结果；
- Distribution：趋势、星期、小时和热力图；
- Drilldown：选中周期、会话分页和封面路径；
- 根 ViewModel 负责一次刷新快照和子状态协调。

会话页可后续拆出：

- `SessionFilterViewModel`；
- `SessionPagerViewModel`；
- `SessionSelectionViewModel`。

### 验收

- 单个 ViewModel 文件不再同时定义大量无关条目类型；
- 刷新仍只生成一次一致快照，不能让子 ViewModel 各自重复扫描 10 万会话；
- 筛选变更、下钻和封面解析结果与重构前一致；
- 压力预算不得退化超过 10%。

## 阶段 E：清理与文档化

1. 删除已无引用的事件处理器；
2. 保留并注释合理的 View 层适配代码；
3. 更新架构图、构造依赖和测试说明；
4. 静态测试不应机械要求 Code-behind 行数为 0；
5. 检查所有命令的本地化、自动化名称和键盘行为；
6. 完成两轮干净 Release 构建和完整 PEXT 验证。

## 测试矩阵

### 命令

- Execute 正常路径；
- CanExecute 全状态组合；
- `RaiseCanExecuteChanged`；
- null/错误参数；
- 快速重复执行和刷新重入。

### 交互

- 文件选择取消；
- 窗口取消；
- 确认拒绝；
- 文件不存在或不可写；
- 预览失败；
- 提交失败与自动回滚；
- 本地化消息参数正确。

### 回归

- 现有 61 项全部通过；
- schema 1–4 无损；
- 10 万会话性能预算；
- 数据指纹、导入导出和备份恢复；
- 中英资源键与主题资源；
- 鼠标、键盘、屏幕阅读器和高 DPI。

## 风险与缓解

| 风险 | 缓解 |
|---|---|
| 命令状态不及时刷新 | 对每个状态源显式调用 `RaiseCanExecuteChanged` 并测试 |
| Coordinator 变成新的 God Object | 按导入/备份/编辑流程拆私有方法，限制其不持有业务状态 |
| ViewModel 反而依赖更多 UI 抽象 | 强类型接口不暴露 WPF 类型，优先让 Coordinator 持有交互接口 |
| 自定义事件命令化增加复杂度 | 允许保留一行 Code-behind 适配，不强制 Behavior |
| 大拆分导致刷新重复计算 | 根 ViewModel 只创建一次 Snapshot，再分发到子状态 |
| 架构与视觉改动难以定位回归 | 两类改动分版本、分提交、分客户端验收 |

## 推荐版本顺序

1. 发布并冻结 0.9.8；
2. 0.10：优先完成视觉计划中的响应式和空状态；
3. 1.1-A：阶段 A、B，建立测试并迁移低风险命令；
4. 1.1-B：阶段 C，会话管理 Coordinator；
5. 1.2：阶段 D、E，拆分大型 ViewModel 和清理。

## 完成定义

- 不以 Code-behind 清零为完成标准；
- 所有标准按钮命令有正确 CanExecute；
- 会话管理关键工作流可完全使用假交互测试；
- ViewModel 和交互接口不引用具体 WPF 窗口类型；
- 滚轮、焦点、Window Owner 和自定义控件事件行为不退化；
- 全部自动化、性能、数据保护和客户端验收通过；
- 没有新增非必要运行时依赖。
