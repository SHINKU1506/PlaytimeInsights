# Playtime Insights 视觉与体验优化计划

状态：待 0.9.8 正式发布后实施  
规划日期：2026-08-11  
来源：`implementation_plan.md` UI/UX 部分及现有界面复审

## 结论

下一阶段视觉优化应优先解决真实可用性问题：指标卡响应式布局、空状态引导和文本层级一致性。
微交互只用于可点击元素；当前不同步的刷新架构下不引入骨架屏；会话列表继续使用现有虚拟化
ListView，不迁移 DataGrid。

本计划独立于主体架构重构，不阻塞 0.9.8 发布，也不与命令/对话框重构放在同一版本中。

## 当前基线

- 主看板 9 张指标卡固定为 218 × 154，并放在 WrapPanel 中；
- 主看板已使用原生主题 Brush、圆角、趋势面积渐变、热力图、Crosshair 和 Tooltip；
- 星期卡片已有低透明渐变、蓝紫围框、Glow、状态点和 120ms 选中动画；
- 排名列表已有封面、前三名视觉、占比背景和 Hover；
- 会话列表已有严格 Grid、横向滚动、斑马纹、Hover、封面、状态 Tag 和 Recycling 虚拟化；
- 数据刷新目前同步执行，统计期间 UI 线程不能保证持续播放动画；
- 现有中英文资源各 271 键，并已验证默认主题与 Seaside 深色主题。

## 优化目标

1. 指标卡在常见窗口宽度和 DPI 下均匀、可读且不过度留白；
2. 全新安装、无精确会话和筛选无结果提供不同的下一步引导；
3. 主、次、辅助文本使用一致的语义层级，不损害关键数据可读性；
4. 微交互强化真实可操作元素，不让纯展示卡片产生错误点击暗示；
5. 保持 Playnite 原生主题、高 DPI、键盘、屏幕阅读器和虚拟化性能；
6. 所有改动可单独回退，不改变统计口径、存储或导入导出。

## 明确不做

- 不为纯展示指标卡增加 Hover 缩放或可点击暗示；
- 不缩放虚拟化会话列表行，不为每一行添加 DropShadow；
- 不在每次筛选变化时执行全屏 Fade Out/Fade In；
- 不在同步刷新架构下添加动画骨架屏；
- 不为“现代感”整体替换 Segoe MDL2 图标；
- 不把当前 ListView 迁移为 DataGrid；
- 不用固定 45% 透明度声称自动满足 WCAG；
- 不在 0.9.8 发布候选继续扩大视觉范围。

## 阶段 1：响应式指标卡布局

### 问题

固定 218px 宽度只能按固定尺寸换行，可能出现行尾大面积留白、孤立卡片和高 DPI 下拥挤。
简单替换为固定列数 UniformGrid 仍会在窄窗口压缩过度或在宽窗口产生过宽卡片。

### 推荐实现

新增一个轻量原生 Panel：

```text
Controls/
  ResponsiveUniformPanel.cs
```

建议依赖属性：

- `MinItemWidth`：204；
- `PreferredItemWidth`：232；
- `MaxItemWidth`：300；
- `MinColumns`：1；
- `MaxColumns`：4；
- `HorizontalSpacing`：12；
- `VerticalSpacing`：12；
- `CenterIncompleteRow`：true。

Panel 根据可用宽度选择 1–4 列，同一行等宽，不让卡片低于最小宽度；最后一行不满时居中，
避免 4+4+1 的单卡贴左。建议宽度行为：

| 内容区宽度 | 推荐列数 |
|---|---:|
| 320–519 | 1 |
| 520–759 | 2 |
| 760–1059 | 3 |
| 1060 以上 | 4 |

实际列数应由 `MinItemWidth` 和可用宽度计算，表中数字作为验收样例而非散落在 XAML 的魔法值。

### XAML 调整

- `MetricCardStyle` 删除固定 `Width=218`；
- 固定 `Height=154` 改为 `MinHeight=154`，允许长本地化文字撑高；
- 卡片横向 Stretch；
- 间距由 Panel 统一管理，不再依赖每卡右侧 Margin；
- 9 张卡片内容和顺序保持不变；
- 卡片仍为纯展示 Border，不增加 Cursor、Hover 或 Click。

### Panel 自动测试

- 输入宽度 360、640、900、1200 时列数符合预期；
- 所有同排元素宽度一致；
- 元素宽度不小于 MinItemWidth；
- 不完整末行正确居中；
- 0、1、9、10 个子元素均能布局；
- DPI 只改变 WPF 设备独立单位换算，不产生负宽度或重叠。

### 客户端验收

- 100%、125%、150%、200% DPI；
- 窗口从最小宽度连续拖到最大；
- 中文和英文；
- 默认、Seaside 和至少一个浅色主题；
- 卡片不黏连、不截断、不重叠，最后一行视觉平衡。

## 阶段 2：空状态与恢复操作

### 状态分类

不要用同一个 `StatusText` 覆盖所有无数据情况。至少区分：

#### A. 全新安装，完全没有精确会话

- 标题：尚无精确游玩会话；
- 说明：启动一次游戏后会自动记录；
- 主 CTA：导入历史会话；
- 次说明：Playnite 已有累计时长仍会出现在累计统计中。

#### B. 有会话，但当前筛选无结果

- 标题：当前筛选没有匹配会话；
- 展示当前筛选摘要；
- 主 CTA：清除筛选；
- 不显示“导入会话”，避免暗示数据丢失。

#### C. 有 Playnite 累计时长，但没有插件精确历史

- 累计排名与总时长正常展示；
- 趋势/热力图区域说明需要精确会话；
- 明确累计分钟无法反推出安装前逐次会话。

#### D. 下钻条件无精确会话

- 在下钻面板内部显示紧凑空状态；
- 保留选中的日期/周期标题；
- 提示跨日裁剪和过滤条件，而不是隐藏整个上下文。

### 视觉实现

- 使用 Segoe MDL2 Assets 和简单几何组合，不引入大型插图；
- 图标使用主题 TextBrush/PanelSeparatorBrush 和语义透明度；
- 主 CTA 使用原生 Button 样式；
- 空状态面板有最大宽度并居中，不占满整个页面；
- 自动化名称说明当前状态和按钮作用；
- 所有文案进入中英本地化资源。

### 状态模型

建议引入明确枚举，而不是多个相互冲突的 bool：

```csharp
public enum SessionEmptyState
{
    None,
    NoSessions,
    NoFilterMatches
}

public enum DashboardEmptyState
{
    None,
    NoPreciseSessions,
    NoRangeMatches,
    NoDrilldownMatches
}
```

ViewModel 暴露状态、标题、说明和 CTA Visibility；清除筛选方法必须恢复来源、元数据值、搜索、
已删除开关和范围内相关条件，并只触发一次刷新。

### 自动测试

- 空仓库映射到 NoSessions；
- 有数据但筛选为空映射到 NoFilterMatches；
- 清除筛选恢复默认值且只刷新一次；
- 累计时长存在但精确会话为空时口径说明正确；
- 中英文资源键和占位符一致；
- CTA 在不适用状态下不可见。

## 阶段 3：语义化文本层级

### 资源阶梯

在页面资源中定义语义透明度，避免散落 `0.55`、`0.58`、`0.7`、`0.86`：

| 层级 | 建议透明度 | 使用范围 |
|---|---:|---|
| Primary | 1.00 | 标题、关键数字、错误、当前状态 |
| Secondary | 0.72 | 时间、时长、筛选摘要、重要辅助信息 |
| Tertiary | 0.58 | 非关键说明、来源补充、卡片解释 |
| Disabled | 0.45 | 禁用控件及非交互占位 |

### 规则

- 时间、时长、状态、错误和当前筛选不得使用 Disabled 层级；
- Tooltip 内容不依赖低透明度表达层级；
- 使用 Playnite 主题 Brush，不把正文统一改为固定白色；
- 选中状态允许使用高亮前景色，但普通状态跟随 TextBrush；
- 实际对比度必须在主题背景上测量，不能仅以透明度推断 WCAG。

### 图标

- 保留 Segoe MDL2 Assets；
- 仅在现有图标语义不清、字形缺失或不同 Windows 版本显示不一致时换成 Path；
- 新 Path 必须使用主题 Brush、统一 ViewBox 和自动化名称；
- 不为整体风格变化批量替换图标。

### 验收

- 默认深色、Seaside、浅色和 Windows 高对比度模式；
- 关键数字和错误信息始终清晰；
- 禁用状态与普通状态可区分；
- 200% DPI 下图标和文本无模糊、裁剪。

## 阶段 4：有针对性的微交互

### 保留并统一

- 星期选择卡：现有 120ms 选中/取消动画；
- 普通 Button：依赖 Playnite 主题的 Hover/Pressed；
- 下钻面板首次从 Collapsed 变为 Visible 时可做一次 120–160ms Opacity 淡入；
- 空状态切换可做一次 140ms 淡入；
- 高风险高级操作不添加娱乐性动画，只强化确认和颜色语义。

### 会话列表

- 保留当前斑马纹、Hover 和选中背景；
- 可选：对左侧 3px 高亮条或背景色做 100ms 颜色过渡；
- 禁止整行 ScaleTransform；
- 禁止每行 DropShadow；
- Recycling 容器重用前后不能保留旧动画状态。

### 指标卡

- 当前不可点击，不设置 Hand Cursor；
- 不添加 Hover 缩放、上浮或阴影加深；
- 如果未来指标卡支持点击下钻，再按 Button/ToggleButton 重新设计完整状态。

### 筛选刷新

- 不做全页面 Fade Out；
- 不让旧数据淡出期间写入新数据；
- 快速连续筛选不能排队执行 Storyboard；
- 若刷新仍同步，只允许刷新完成后的轻量局部淡入。

### 动画可访问性

- 动画不承载唯一状态信息；
- 颜色、边框、状态点和文字必须在静止状态下表达完整含义；
- 可参考 `SystemParameters.ClientAreaAnimation` 决定是否禁用非必要动画；
- 动画时长控制在 100–180ms，不使用循环动画。

## 阶段 5：会话列表列宽治理（仅在实际错位时实施）

### 保留现有 ListView

当前列表已经具备：

- 表头和行严格 Grid；
- 横向滚动；
- Recycling 虚拟化；
- Zebra、Hover、Selected；
- 封面、时间右对齐和 Tag。

因此不迁移 DataGrid，也不默认启用 `SharedSizeGroup`。

### 可选改进

1. 把表头和行的列宽集中为同一组资源或常量；
2. 游戏名称列保留 `*`；
3. 时间、时长、来源、状态和操作列保持稳定宽度；
4. 小窗口继续使用横向滚动，不强行压缩关键时间列；
5. 若使用 SharedSizeGroup，必须重新测量 10 万会话分页和滚动性能。

### 触发条件

只有客户端复现以下问题才实施：

- 表头与内容列实际错位；
- 本地化后来源或状态长期截断；
- 125%–200% DPI 下固定宽度明显不合理。

## 加载状态策略

### 当前版本

不实现 Skeleton。现有同步刷新期间 UI 线程可能无法持续播放 Storyboard，骨架屏只会增加复杂度。

### 未来触发条件

满足以下任一条件再设计异步加载：

- 真实用户常规刷新 P95 超过 500ms；
- UI 无响应可稳定复现；
- 会话规模接近压力数据时筛选频繁卡顿。

### 异步化前置设计

1. UI 线程获取 Playnite 游戏和设置快照；
2. 后台线程只执行纯数据统计；
3. 每次刷新带版本号或 CancellationToken；
4. 新筛选取消旧计算，旧结果不得覆盖新状态；
5. 回到 UI 线程一次性应用 Snapshot；
6. `IsBusy` 禁止嵌套刷新并提供静态加载提示；
7. 完成后再评估骨架屏，而不是反向由骨架屏推动架构变化。

## 文件影响预估

### 阶段 1

```text
Controls/ResponsiveUniformPanel.cs
Views/PlaytimeInsightsDashboardView.xaml
Tests/Program.cs
```

### 阶段 2

```text
ViewModels/DashboardViewModel.cs
ViewModels/SessionManagementViewModel.cs
Views/PlaytimeInsightsDashboardView.xaml
Views/SessionManagementView.xaml
Views/SessionManagementView.xaml.cs
Localization/en_US.xaml
Localization/zh_CN.xaml
Tests/Program.cs
```

### 阶段 3–4

```text
Views/PlaytimeInsightsDashboardView.xaml
Views/SessionManagementView.xaml
PlaytimeInsightsSettingsView.xaml
Tests/Program.cs
```

## 自动化与性能门槛

- 现有 61 项回归全部通过；
- Responsive Panel 增加独立测量/排列测试；
- 空状态和清除筛选增加状态机测试；
- 中英资源键和格式占位符一致；
- 10 万会话分析与载入预算不退化超过 10%；
- 会话列表 Recycling 保持启用；
- DLL 不重新嵌入 staging BAML；
- 连续两轮干净构建 DLL 哈希一致；
- PEXT 内容、LICENSE、PDB 和敏感路径检查保持通过。

## 客户端验收矩阵

| 维度 | 组合 |
|---|---|
| 语言 | 中文、英文 |
| DPI | 100%、125%、150%、200% |
| 窗口 | 最小、中等、最大、连续拖动 |
| 主题 | 默认深色、Seaside、浅色、高对比度 |
| 数据 | 空数据、少量、筛选无结果、大量会话 |
| 输入 | 鼠标、滚轮、键盘、访问键 |
| 状态 | 普通、Hover、Focused、Selected、Disabled、Busy |

重点检查：

- 指标卡不黏连、不截断、不产生大面积不平衡留白；
- 空状态 CTA 与真实数据状态匹配；
- 关键文字对比度足够；
- 动画不会造成布局跳动或列表复用残影；
- 滚轮边界接力、Crosshair、下钻和封面保持正常。

## 风险与缓解

| 风险 | 缓解 |
|---|---|
| 响应式 Panel 在极窄宽度测量错误 | MinItemWidth、0/1 子元素和无限 Measure 约束测试 |
| 英文卡片变高导致同排不齐 | Panel 以该行最大 DesiredHeight 统一行高 |
| 空状态错误判断为无数据 | 分离总会话数、筛选结果数和范围结果数 |
| 清除筛选触发多次刷新 | 设置 suppress 标志，全部恢复后只刷新一次 |
| 动画与虚拟化冲突 | 不缩放行、不循环动画，测试容器复用 |
| 固定透明度在主题中不可读 | 语义层级加多主题人工对比度验收 |
| 视觉改动扩大正式版风险 | 独立 0.10 版本，不回灌到 0.9.8 |

## 推荐实施顺序

1. 发布并冻结 0.9.8；
2. 0.10-A：ResponsiveUniformPanel 与指标卡；
3. 0.10-B：空状态与清除筛选 CTA；
4. 0.10-C：文本层级资源和多主题审查；
5. 0.10-D：局部微交互；
6. 仅在真实问题出现时执行列宽治理；
7. 仅在性能指标触发时启动异步刷新/加载状态设计。

## 完成定义

- 320px 以上内容宽度均有稳定卡片布局；
- 空数据与筛选无结果不会只留下空白列表；
- 所有 CTA 行为、安全边界和本地化正确；
- 关键文本在目标主题和 DPI 下可读；
- 仅可操作元素拥有交互动画；
- 不引入 DataGrid、骨架屏或不必要图标替换；
- 自动化、性能、可复现构建、数据保护和客户端验收全部通过。
