# Playtime Insights 响应式指标卡与视觉基础设计

状态：待实施  
设计日期：2026-08-13  
依据：`docs/VISUAL_UX_OPTIMIZATION_PLAN.md` 与当前 `main` 实现

## 目标

本批次解决主看板九张指标卡在窄窗口、高 DPI 和长本地化文本下的固定尺寸问题，并建立可复用的
语义文本层级。改动保持原生 WPF、Playnite 动态主题画刷和现有数据绑定，不改变统计、筛选、
刷新、存储、导入导出或用户数据。

本批次完成后，320 设备独立像素以上的内容区域应稳定显示 1–4 列等宽指标卡；同一行等高，
不完整末行居中，关键文本继续跟随主题且具有一致层级。

## 范围

### 包含

- 新增原生 `ResponsiveUniformPanel`，负责指标卡测量与排列；
- 将九张指标卡从 `WrapPanel` 迁移到该 Panel；
- 移除卡片固定宽高和逐卡间距，改为 Panel 统一控制；
- 将固定高度改为最小高度，允许本地化文本撑高；
- 在 Dashboard 页面资源中定义 Primary、Secondary、Tertiary、Disabled 四级语义透明度；
- 优先替换指标卡及主看板关键辅助文字中的固定灰色和散落透明度；
- 增加 Panel 布局、XAML 结构和主题资源静态回归；
- 更新实施状态与客户端验收步骤。

### 不包含

- 空状态枚举、导入历史会话 CTA、清除筛选 CTA；
- Dashboard 或 Session ViewModel 行为变更；
- Skeleton、异步刷新、全页面淡入淡出；
- 指标卡 Hover、缩放、上浮、阴影增强、点击或手型光标；
- 会话列表列宽治理、DataGrid 迁移或虚拟化改动；
- Playnite 安装目录、已安装插件、Seaside 主题或用户配置修改；
- 版本号、插件 ID、统计口径、存储 schema 和发布包结构变更。

## 响应式布局设计

### 组件边界

新增 `Controls/ResponsiveUniformPanel.cs`。该类只负责子元素布局，不读取 ViewModel、主题、
本地化或业务状态。Dashboard XAML 是其唯一首批消费者。

Panel 暴露以下依赖属性：

| 属性 | 默认值 | 约束 |
|---|---:|---|
| `MinItemWidth` | 204 | 有限且大于 0 |
| `PreferredItemWidth` | 232 | 有限且大于 0 |
| `MaxItemWidth` | 300 | 不小于有效最小宽度 |
| `MinColumns` | 1 | 至少为 1 |
| `MaxColumns` | 4 | 不小于有效最小列数 |
| `HorizontalSpacing` | 12 | 有限且不小于 0 |
| `VerticalSpacing` | 12 | 有限且不小于 0 |
| `CenterIncompleteRow` | `true` | 控制末行水平居中 |

无效属性值不应造成异常、负尺寸或重叠。布局计算时统一规范化为安全值：非有限或非正宽度回退
到默认值；负间距按 0；列数至少为 1；最大值不得小于最小值。

### 列数选择

Panel 根据有限可用宽度、子元素数量、最小/推荐/最大宽度和间距选择列数：

1. 候选列数限制在有效的 `MinColumns..MaxColumns`，并且不超过可见子元素数量；
2. 选择能让原始单元格宽度不低于 `PreferredItemWidth` 的最大候选列数；等价计算为
   `floor((availableWidth + spacing) / (PreferredItemWidth + spacing))` 后限制到候选范围；
3. 如果显式 `MinColumns` 或极窄宽度令单元格低于推荐宽度，则逐列退化到不低于
   `MinItemWidth` 的最大可行列数，最低保留一列；
4. 若可用宽度小于一张最小卡片，保留一列并使用可用宽度，不生成负尺寸或横向重叠；
5. 单元格最终宽度不超过 `MaxItemWidth`；受最大宽度限制而未占满可用区域时，整个网格水平居中；
6. 若 Measure 宽度为正无穷，使用有效推荐宽度和最大列数计算期望尺寸，避免依赖无限宽度排列。

验收样例以内容区宽度而非窗口外框为准：

| 内容宽度 | 预期列数 |
|---:|---:|
| 360 | 1 |
| 640 | 2 |
| 900 | 3 |
| 1200 | 4 |

### 测量与排列

- 每个子元素以计算后的统一单元格宽度、无限高度进行测量；
- 每一行高度取该行所有可见子元素的最大 `DesiredSize.Height`；
- 行内所有子元素使用相同行高排列，避免英文长文本只让单张卡片变高；
- 完整行在网格占满可用宽度时从左侧开始；网格受最大宽度限制时整体居中；不完整末行在
  `CenterIncompleteRow=true` 时相对 Panel 可用宽度整体居中；
- Panel 自身期望宽度为实际占用宽度，期望高度为各行高度与垂直间距之和；
- `Collapsed` 子元素由 WPF 保持零期望尺寸，但仍不应造成索引错误或负排列区域；
- 0 个子元素返回空尺寸，1 个子元素遵循相同宽度限制与居中规则。

### XAML 迁移

`MetricCardStyle` 调整为：

- 删除 `Width=218`；
- 删除 `Height=154`；
- 设置 `MinHeight=154`；
- 删除 `Margin=0,0,12,12`；
- 保留现有 Padding、背景、边框、圆角及内容结构；
- 保持指标卡为普通 `Border`，不增加交互状态。

九张指标卡的顺序、绑定、图标、趋势 Tag 和文案全部保持不变。Panel 与上下内容之间继续使用页面级
Margin，卡片之间仅使用 Panel 的 12 像素横纵间距。

## 语义文本层级

Dashboard 页面资源定义四个 `x:Double`：

| 资源 | 值 | 使用范围 |
|---|---:|---|
| `TextOpacityPrimary` | 1.00 | 标题、关键数字、错误、当前状态 |
| `TextOpacitySecondary` | 0.72 | 时间、时长、筛选摘要、重要辅助信息 |
| `TextOpacityTertiary` | 0.58 | 非关键解释、来源补充、卡片说明 |
| `TextOpacityDisabled` | 0.45 | 禁用控件和非交互占位 |

首批迁移遵循以下规则：

- `MetricHeaderStyle` 使用 Secondary；
- `MetricIconStyle` 和 `MetricHelperTextStyle` 使用 Tertiary；
- `MetricHelperTextStyle` 删除固定 `#FF8A8A8A`，改用 `TextBrush` 与透明度；
- 指标数值保持 Primary；
- 当前筛选、时间、时长、状态与错误不得降为 Disabled；
- 固定金、银、铜徽章和蓝紫图表渐变属于已有语义色，不在本批次替换；
- 不宣称透明度本身满足 WCAG，对比度由默认深色、Seaside、浅色和高对比度客户端检查确认。

本批次只迁移 Dashboard 中与指标卡和关键辅助信息直接相关的散落值。Sessions 和设置页的共享资源
抽取留给后续独立批次，避免跨页面扩大回归面。

## 数据与交互

数据流保持不变：

```text
DashboardViewModel 属性
        |
        v
现有九张指标卡绑定
        |
        v
ResponsiveUniformPanel 只读取子元素 DesiredSize
        |
        v
统一测量和排列
```

Panel 不订阅 ViewModel 事件，不触发刷新，不改变集合，不缓存业务数据。尺寸、子元素或依赖属性变化
通过 WPF 的 `AffectsMeasure` 和 `AffectsArrange` 元数据触发布局更新。

键盘焦点、AutomationProperties、滚轮接力、趋势图 Hover/下钻和星期选择动画均不改变。指标卡仍然
不可聚焦、不可点击，动画不会承载布局或状态信息。

## 异常与退化策略

- 极窄宽度下使用一列并压缩到非负可用宽度，外层页面决定是否继续缩小窗口；
- 非有限 Measure 宽度使用推荐宽度计算，不传播 `NaN` 或无穷值到 `Rect`；
- 无效依赖属性值在布局计算时规范化，不因 XAML 或运行时设置导致崩溃；
- 子元素期望高度不同则以行最大高度排列，不裁剪较高元素；
- Panel 发生问题时可通过单个提交回退到原 `WrapPanel`，不需要迁移数据或恢复配置。

## 测试设计

### 自动化

在现有 `Tests/Program.cs` 自定义测试入口中增加 STA/WPF 测试：

1. 360、640、900、1200 宽度分别产生 1、2、3、4 列；
2. 同一行元素的排列宽度相同；
3. 正常宽度下元素不低于最小宽度且不高于最大宽度；
4. 9 个元素在四列布局时，末行单卡相对完整行居中；
5. 0、1、9、10 个子元素均得到有限、非负、不重叠的布局；
6. 长文本元素让所在行统一增高；
7. 无限 Measure 宽度、零宽度和无效属性值不产生异常或非有限尺寸；
8. Dashboard XAML 不再包含指标卡固定宽高和逐卡 Margin，并使用新 Panel；
9. Dashboard 语义文本资源存在，指标辅助文字不再使用固定灰色；
10. 既有主题、无障碍、滚轮、架构和刷新回归继续通过。

测试应验证可观察布局结果，不锁死内部辅助方法实现。

### 构建与回归

- `dotnet build Tests/PlaytimeInsights.Tests.csproj -c Release --no-restore`；
- 运行测试可执行文件并要求全部既有及新增回归通过；
- `dotnet build PlaytimeInsights.csproj -c Release --no-restore`；
- Release 构建保持 0 警告、0 错误；
- 不部署到已安装插件目录，不修改用户数据；部署和打包留待客户端验收阶段单独授权。

### 客户端验收

客户端手工检查矩阵：

- 语言：简体中文、English；
- DPI：100%、125%、150%、200%；
- 主题：默认深色、Seaside、至少一个浅色主题、Windows 高对比度；
- 宽度：最小、中等、最大及连续拖动；
- 内容：普通数据、长英文说明、趋势 Tag 可见。

通过标准：卡片不黏连、不重叠、不截断关键值；同排等宽等高；末行平衡；主题切换后辅助文字可读；
指标卡没有错误的 Hover 或点击暗示；滚轮、图表、筛选和下钻行为与基线一致。

## 文件影响

计划新增或修改：

```text
Controls/ResponsiveUniformPanel.cs
Views/PlaytimeInsightsDashboardView.xaml
Tests/Program.cs
docs/IMPLEMENTATION_STATUS.md
docs/superpowers/plans/2026-08-13-responsive-metrics-visual-foundation.md
```

设计文档自身位于：

```text
docs/superpowers/specs/2026-08-13-responsive-metrics-visual-foundation-design.md
```

## 完成定义

- 指标卡布局满足 1–4 列、204–300 宽度、12 间距和末行居中设计；
- 九张卡片顺序、内容、绑定和非交互语义保持不变；
- 关键文本层级使用主题画刷和命名语义资源；
- 新增布局与静态回归通过，全部既有测试继续通过；
- Release 构建 0 警告、0 错误；
- 未修改 Playnite 安装目录、已安装插件、主题、用户配置或用户数据；
- 客户端矩阵通过后再更新发布与部署记录。
