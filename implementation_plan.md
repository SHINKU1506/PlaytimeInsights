# WPF 前端逻辑与体验改进方案 (PlaytimeInsights)

## 背景

通过对插件 `Views` 和 `ViewModels` 目录下的代码进行分析，发现当前 WPF 前端不仅在架构层面（未严格遵守 MVVM）存在改进空间，在前端视觉设计与用户体验 (UI/UX) 方面也有进一步优化的潜力。

本方案分为两部分：**架构重构** 与 **UI/UX 优化**。

---

## 第一部分：架构与逻辑重构 (MVVM)

### 存在的问题
1. **视图层代码过载**：视图的 `.xaml.cs` 文件充当了控制器，包含大量直接调用 ViewModel 方法的事件处理代码。
2. **缺乏 ICommand 支持**：UI 控件未利用 WPF 的数据绑定和命令机制，导致强耦合。
3. **UI 逻辑泄露**：ViewModel 的业务流程被切割在 UI 层（如直接调用 `MessageBox`, `OpenFileDialog`），无法进行独立单元测试。

### 改进建议

#### 1. 引入基础类型
- **`ViewModels/RelayCommand.cs`**：引入标准的 `RelayCommand` 实现，以便 ViewModel 能暴露交互命令。
- **`Services/IDialogService.cs`** 与 **`WpfDialogService.cs`**：将 `MessageBox` 和文件选择等 UI 弹窗逻辑封装至服务中。

#### 2. 重构 ViewModels
- 在 `SessionManagementViewModel` 和 `DashboardViewModel` 中注入 `IDialogService`。
- 将原本视图调用的方法（如 `Refresh`, `ExportCsv`）重构为 `ICommand` 属性。
- ViewModel 内部通过 `IDialogService` 处理完整的交互流程（如导入前的文件选择与弹窗确认）。

#### 3. 重构 Views (清理 Code-Behind)
- 修改 `SessionManagementView.xaml` 和 `PlaytimeInsightsDashboardView.xaml`，将交互事件改为 `Command="{Binding XxxCommand}"`。
- 彻底清空 `.xaml.cs` 中与业务逻辑相关的事件处理器。

---

## 第二部分：前端视觉与用户体验优化 (UI/UX)

基于现有的 XAML 样式（如渐变、圆角、阴影），我们可以进一步提升其现代感和流畅度。

### 存在的问题与优化建议

#### 1. 响应式布局优化 (Responsive & Fluid Layout)
- **问题**：在 `PlaytimeInsightsDashboardView.xaml` 中，仪表盘指标卡片 (`MetricCardStyle`) 使用了固定的宽高 (`Width="218" Height="154"`) 并放置于 `WrapPanel` 中。在不同分辨率和 DPI 缩放比例下，可能会导致卡片换行不规则、留白过多或布局生硬。
- **优化**：将固定宽度的卡片布局重构为基于 `UniformGrid` 或按比例划分的 `Grid` (如 `ColumnDefinition Width="*"`) 的流式布局。让卡片宽度能够根据窗口尺寸自适应拉伸或缩小，保证视觉平衡。

#### 2. 微交互与动画增强 (Micro-interactions)
- **问题**：目前仅有少部分按钮（如星期选择按钮）实现了 `Storyboard` 动画，列表项和统计卡片的悬浮状态相对生硬。
- **优化**：
  - 为 `SessionListItemStyle` 和 `MetricCardStyle` 增加更细腻的过渡动画（Transitions）。例如：鼠标悬停时添加平滑的微缩放效果（`ScaleTransform`）或阴影加深效果，增强控件的“可点击”暗示。
  - 在数据加载和切换筛选条件时，引入淡入淡出 (Fade In/Out) 的渐变动画，避免界面内容的生硬闪烁。

#### 3. 空状态与加载状态引导 (Empty States & Loading Skeletons)
- **问题**：当没有游戏会话数据，或者筛选条件未匹配到任何结果时，界面目前只显示一段简单的文本（`StatusText`）和空白的列表。
- **优化**：
  - **加载状态 (Loading)**：在数据初始化或大量数据重新计算时，展示骨架屏 (Skeleton Screen) 动画，替代简单的进度条或直接卡顿。
  - **空状态 (Empty State)**：设计并引入精美的空状态插图（SVG 路径或图标组合）。如果是因筛选导致的空数据，提供一个显眼的“清除所有筛选条件”的按钮 (Call-to-Action)；如果是全新安装，则引导用户“导入会话”。

#### 4. 视觉层级与对比度打磨 (Visual Hierarchy & Typography)
- **问题**：部分辅助文本（如指标说明、副标题）的对比度在深色主题下可能偏低；列表表头使用了多个硬编码的宽度比例。
- **优化**：
  - 标准化透明度阶梯：主标题 100%、次文本 70%、辅助提示 45%，并确保符合 WCAG 对比度标准。
  - 使用更现代的图标字体或 SVG Path，让图标在不同尺寸下保持锐利。
  - 会话管理列表可以考虑使用 `DataGrid` 替代 `ListBox` 加上硬编码的 `Grid`，或者使用 `SharedSizeGroup` 确保表头和内容列永远精准对齐。

## User Review Required

> [!IMPORTANT]
> 包含 UI/UX 优化在内，这是一个比较全面的前端重构计划。
> 1. MVVM 架构重构可能会影响到当前的事件绑定流。
> 2. 响应式布局和微交互的加入可能会需要修改部分现有样式的逻辑（如移除部分定宽设置）。
> 
> 请确认是否同意以上两部分优化方案，以便我们可以开始落实具体的代码修改？
