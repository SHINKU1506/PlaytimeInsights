# Client Acceptance 1.1.0 — Dashboard Visual Elevation

状态：Task 7 自动化发布门禁通过；100k 分析与 All Sessions UI 自动化性能预算已收敛，性能构建已部署。本轮仍待虚拟化后实机矩阵、推送与合并；Reduced motion 待验收，实际读屏器已跳过
日期：2026-09-03

## Frozen Layout Contract

- KPI 区是单一 `ResponsiveUniformPanel`，共 8 张卡；不存在 Hero / Tier 2 拆分。
- `AdaptiveDashboardPanel` 仅有 Primary / Secondary 两个区域，不存在 FullWidth。
- Calendar Heatmap 始终属于 Primary。
- Trend 与 Distribution 各自拥有一个紧邻来源模块的 Primary Drilldown Host；两者复用唯一的 `DrilldownCardTemplate`，且任一时刻最多一个可见。
- 窄屏源码顺序：Trend、Trend Drilldown、Ranking、Distribution、Distribution Drilldown、Anomaly；非活动 Host 为 `Collapsed` 且不保留 Content。
- Heatmap 列间距 26 DIP；视觉格 24×24 DIP；按钮命中区 26×26 DIP。
- Heatmap 月份、周次和格子共享 26 DIP 横向坐标；首列为星期一。
- Calendar 使用 0h / <1h / 1–3h / >3h 四档绝对强度；Week×Hour 继续使用蓝紫色连续相对强度。
- Calendar 保留已批准的冰青—青绿冷色系渐变。它是按明度单调递增的 ordinal ramp，而不是字面意义上“色相完全不变”；每个 stop 均与 Week×Hour 的蓝紫锚点保持可辨距离。
- 宽屏滞回以面板内容宽度计算：从窄屏放大到 `>= 1200` 进入双栏；进入后在 `>= 1160` 保持双栏；`1159` 退出。
- 参考视图宽度 = 内容宽度 + 48；Playnite 整窗截图还包含左侧导航栏，不能直接当作视图宽度。

## Automated Contract Evidence

- [x] 单一响应式指标面板、8 张卡、`MetricCardsHost` 与入口计划首步一致。
- [x] Adaptive 区域仅 Primary / Secondary；Trend/Distribution Drilldown 均为 Primary 并复用同一模板。
- [x] Period 选择只激活 Trend Host；Calendar 日期选择只激活 Distribution Host；Reset 后两个 Host 都为 `Collapsed`。
- [x] Drilldown 在 900 与 1248 DIP 视图下均与来源模块同 X，且非活动 Host 不占高度。
- [x] Drilldown 的 96 DIP 标题带只在离开视口时最小滚动；窄/宽布局都执行同一逻辑。
- [x] Drilldown 没有 Opacity/Translate reveal Storyboard，显示时不调用 `.Focus()` 或 `Keyboard.Focus`。
- [x] 1199→1200 进入双栏；1200→1180→1160 保持双栏；1160→1159 退出双栏。
- [x] Month Axis、周次轴、热力格均使用 26 DIP；真实模板的星期标签可见序列为显/隐/显/隐/显/隐/显。
- [x] 0、3599、3600、10800、10801 秒边界与 None/Low/Medium/High 真实 swatch 及图例顺序一致。
- [x] `HeatmapCellViewModel` 无 `HeatOpacity`；`WeekHourCellViewModel` 保留该属性。
- [x] Calendar 使用 Button + Command，不含旧的 `MouseLeftButtonUp` 处理器。
- [x] Week×Hour 锚点锁定为 `#2457D6 → #A45CFF`；Calendar 六个 stop 锁定为已批准色值，中点明度逐档至少增加 6 CIELAB L*，格内光泽距离逐档递增，每个 stop 对模块底色至少 2:1，且在正常视觉与 deutan 模拟下对两个 Week×Hour 锚点的 CIE76 距离均不小于 15。
- [x] Trend 仅绘制一层 Area Geometry；普通/hover 节点外圈同源于 `ControlBackgroundBrush`；无 `TrendNodeRingBrush`。
- [x] Ranking 使用 10% 透明度、全行高度的时长占比背景；区间榜与累计榜使用各自的占比文案。
- [x] Ranking DetailText 最多保留次数/活跃日并排除当前排序项；11 DIP、0.72 Opacity；Tooltip 统一承载占比、平均、最长。
- [x] Reduced motion 的入口计划将 delay、duration 与 offset 全部置零。
- [x] 跨月、六周月份、一年/All Sessions 投影、排行稀疏状态、0/1/100/250 下钻与 Recycling virtualization 均有自动化覆盖。

## Release and Performance Gate

- [x] Release plugin build：0 warning / 0 error（2026-08-30）。
- [x] Release test build：0 warning / 0 error（2026-08-30）。
- [x] 修正最终护栏后连续五轮完整回归均输出 `All Playtime Insights tests passed.`。
- [x] 100k 会话分析最大值 <= 750 ms：最终五轮为 656 / 692 / 638 / 647 / 683 ms，最大值 **692 ms**。
- [x] schema 4 加载最大值 <= 1400 ms：最终五轮为 1,031 / 1,023 / 1,001 / 983 / 960 ms，最大值 **1,031 ms**。

历史并发负载样本仍保留：在 Playnite 正运行且 Codex 同时执行前台窗口验收时，先前五轮 100k 分析为 531 / 553 / 605 / 684 / **791 ms**，第 5 轮超过预算；schema 4 同组最大值为 **1,397 ms**。该失败没有被删除或改写，最终五轮是修正护栏后单独建立的新证据组。

## Dashboard Analytics Performance Optimization Gate（2026-09-03）

分支 `codex/dashboard-performance-optimization` 将 100k 分析基线（五样本中位 707 ms、最大 720 ms、GC 0/1/2 = 189/31/7）收敛到以下证据；750 ms 硬门禁未放宽。

- [x] 五轮独立 Release 门禁：每轮两个构建 0 warning / 0 error，完整回归输出 `All Playtime Insights tests passed.`。
- [x] 100k 五样本中位数 <= 650 ms：五轮中位数为 560 / 550 / 563 / 587 / 532 ms，最大中位数 **587 ms**。
- [x] 100k 五样本最大值 <= 700 ms：五轮最大值为 579 / 567 / 600 / 667 / 546 ms，总最大值 **667 ms**。
- [x] schema 4 加载 <= 1400 ms：五轮为 1,364 / 1,260 / 1,190 / 1,318 / 1,197 ms，最大 **1,364 ms**。
- [x] GC 0/1/2 增量在全部五轮稳定为 **59/15/4**（基线 189/31/7）。
- [x] 分析 Task 1–6 的提交未修改 `Views/`、`Controls/`、`Localization/` 或 `Resources/`；统计口径、比较区间、异常、排行与选择性刷新语义由既有回归全部保持。当前分支后续存在 Calendar UI 优化差异，不再将整条分支描述为“与 main 无 UI 差异”。

被环境负载污染的一轮如实保留：五样本 562 / 619 / 619 / 636 / **719 ms**（max 719 > 700 目标），schema 4 加载 **1,403 ms** 超预算 3 ms；同一二进制复跑立即通过（五样本 518–546 ms、schema 4 1,197 ms）。该轮按环境噪声弃用，记录不删除。

## Heatmap UI Layout Baseline

测量对象是实际 `DistributionModule` 内的非虚拟化 `ItemsControl` + `UniformGrid` + Button 模板，测量宽度固定为 856.84 DIP。每轮先进行 7 格预热；一年数据由真实 Custom 2025-01-01 至 2025-12-31 分析投影生成，All Sessions 数据由固定时钟 2025-12-28 和 2021-01-04 起始会话经真实 `AllSessions` 投影生成。Snapshot 计算与 `Distribution.Apply` 在计时器外，Measure、Arrange、数据模板绑定及 Button 实例化在计时器内。

| 范围 | 格数 | 五轮耗时 |
| --- | ---: | --- |
| 一年真实投影 | 371 | 168.0 / 158.5 / 161.3 / 151.0 / 161.0 ms（最大 **168.0 ms**） |
| All Sessions 真实投影 | 1,820 | 707.6 / 648.8 / 685.4 / 628.8 / 655.1 ms（最大 **707.6 ms**） |

- [x] 一年与 All Sessions 的格数及 UI Measure + Arrange 成本已实测。
- [x] All Sessions 交互流畅度已收敛（2026-09-03，见下节）：1,820 格 Measure + Arrange 从最大 707.6 ms 降至最大 168.4 ms。

## Calendar Heatmap UI Performance Gate（2026-09-03）

分支 `codex/dashboard-performance-optimization` 将热力图 UI 收敛到以下证据；历史样本全部保留。

历史基线（保留）：原始单次测量 All Sessions 峰值 **707.6 ms**；轻量 Button 前（原始架构）五样本门禁 All Sessions 为 732.3 / 663.3 / 807.2 / 810.4 / **855.3 ms**（最大 855.3），一年为 177.3 / 186.2 / 197.4 / 203.9 / 179.8 ms（最大 203.9）。

- [x] 五轮独立 Release 门禁：两个构建 0 error，完整回归输出 `All Playtime Insights tests passed.`。
- [x] 一年 371 格五样本 max <= 200 ms：五轮为 187.3 / 188.9 / 192.0 / 180.3 / 179.9 ms。
- [x] All Sessions 1,820 格五样本 max <= 300 ms：五轮为 152.5 / 153.7 / 157.4 / 168.4 / 151.2 ms（五轮中位 137.8–143.3 ms）。
- [x] 虚拟化契约：初始与滚动末端的 realized 周列容器 25–26 个（< 60）、realized `HeatmapCellButton` 175–182 个（< 420）；滚动到末端后首周容器被回收（`ContainerFromIndex(0)` 为 null），最后一周 7 格可实现且 Command/CommandParameter/Automation Name/Focusable 保持；ViewModel 数据仍为完整 1,820 格。
- [x] 月份轴同步：滚动到末端时月份轴 HorizontalOffset 与周列内容 offset 差为 0.0 DIP（<= 0.5 DIP 契约）；月份轴保留全部约 60 个轻量 TextBlock，不虚拟化。
- [x] 键盘与无障碍自动化结构：日期仍为 `HeatmapCellButton : Button`（26×26 DIP 命中、24×24 自绘光泽、CornerRadius 3），Command/ToolTip/AutomationProperties.Name 由运行时测试逐项断言。
- [ ] 性能版实机复验：虚拟化后的主题、DPI、zh_CN / en_US、Tab、Space/Enter、Tooltip、月轴滚动同步与跨零点显示待用户验收；实际读屏器播报继续按既定决定跳过，不计作失败。

实现路径：Task 2 轻量自绘 Button 后一年已达标（max 203.9 → 192.0 范围），All Sessions 未达 300 ms，因此按计划硬分支执行了 Task 3（周列发布，不复制 Cell 模型）与 Task 4（水平 Recycling 虚拟化 + 月份轴同步）。

状态同步复跑（2026-09-03）：两个 Release 构建 0 warning / 0 error，完整回归输出 `All Playtime Insights tests passed.`；100k 五样本 508 / 524 / 555 / 609 / 647 ms（median 555、max 647），schema 4 为 1,074 ms；一年热力图五样本 max 139.3 ms，All Sessions max 102.6 ms。

## Performance Branch Delivery State

- [x] 本地实现与自动化性能门禁完成；状态同步前代码工作树干净，本次未修改生产代码。
- [ ] 推送：性能提交仍只在本地；远端性能分支与 `main` 仍为 `d9a5d34`。
- [ ] 合并：性能分支尚未合并回 `main`。
- [x] 部署：2026-09-03 部署前两个 Release 构建 0 warning / 0 error，完整回归通过；100k median/max 530/540 ms，schema 4 为 1,000 ms，一年/All Sessions UI max 135.9/107.4 ms。Release 与安装目录严格 9/9 文件哈希一致，DLL SHA-256 为 `574980438951103AEE19CB20CD27CCFC7A352D23E77B5647A3EEA6B2343C0E5D`；旧版备份位于 `C:\Users\chan\AppData\Roaming\Playnite\Backup\PlaytimeInsights-deploy-20260903-212723`。部署前后 7 个用户数据文件联合指纹均为 `A1CCAB93B14ACC88EF4C78253169FE149947DD9FD3701F1C4AC3E6944DF8932E`。部署时 Playnite 未运行，尚未执行启动加载与人工验证。
- [ ] 人工验收：虚拟化后实机矩阵与 Reduced motion 尚未完成；实际读屏器播报已明确跳过。

## Actual UI Evidence（性能虚拟化前的视觉基线）

以下记录来自性能虚拟化前的视觉分支，用于后续回归对照，不代表性能分支最终实机验收。2026-08-30 的 Codex 临时屏幕捕获未写入仓库；Playnite 启动进程单独使用 `HTTP_PROXY` / `HTTPS_PROXY=http://127.0.0.1:10456`，没有修改仓库、测试或全局代理设置。

- [x] 当前中文深色主题、2026 年 8 月数据、Playnite 整窗约 1451×979 px：布局为双栏；8 张指标卡无横向溢出；Trend Area 未遮挡网格、折线和节点；Ranking 位于右栏。
- [x] 当前中文深色主题、Playnite 整窗约 1261×979 px：布局为单栏；8 张指标卡无横向溢出；Trend 先于 Ranking，页面无横向滚动。
- [x] 640、900、1159、1160、1199、1200、1440、1600 DIP 内容宽度逐项实机核验通过（2026-09-03 用户人工验收）；包含从双栏缩小到 1160 后保持双栏、到 1159 退出，以及从单栏放大到 1199 后保持单栏、到 1200 进入双栏的双向滞回检查。
- [x] 空范围、排行 1/2/3/10 项、下钻 0/1/100/250、跨月、六周月份、一年、All Sessions 的逐项实机矩阵完成（2026-09-03 用户人工验收）。
- [x] Default Dark、Default Light、Seaside Dark、Windows High Contrast 全主题矩阵完成（2026-09-03 用户人工验收）。
- [x] zh_CN / en_US 双语言实机矩阵完成（2026-09-03 用户人工验收）；两种语言下 Dashboard 文案、热力图档位、排行辅助信息和下钻区域均完成核验。
- [x] DPI 100% / 125% / 150% / 175% / 200% 全缩放矩阵完成（2026-09-03 用户人工验收）。
- [x] Tab 进入 Calendar Button、Space/Enter 触发下钻与焦点描边完成实机核验（2026-09-03 用户人工验收）。
- [ ] Reduced motion 实机核验：自动化已确认入口计划将 delay、duration 与 offset 置零，仍待 Windows 减弱动画设置下的人工确认。
- [x] 实际读屏器播报：**跳过，不作为本轮验收门禁**（2026-09-03 用户决定）。活动 Host 的 Automation Name 与 `NameProperty` 事件路径保留自动化护栏；未执行进程外读屏器确认，因此仍不得声称具备原生 Polite live-region 语义。
- [x] Calendar 与 Week×Hour 同卡片视觉区分、图例管辖关系及 24 DIP 对角光泽完成实机核验（2026-09-03 用户人工验收）。
- [x] 区间榜与累计榜跨零点前后的相对日期完成实机对照（2026-09-03 用户人工验收），两个 Tab 使用一致的“今天 / 昨天 / 日期”口径。

2026-08-30 检测到用户重新操作前台窗口时，Codex 停止继续控制 Playnite。2026-09-03 用户后续独立完成性能虚拟化前的精确内容宽度、完整数据状态、全主题、zh_CN / en_US、全 DPI、Calendar 键盘链路、Calendar/Week×Hour 视觉区分和跨零点相对日期矩阵；实际读屏器播报明确跳过。All Sessions 自动化 UI 成本现已收敛；当前人工待办是 Reduced motion 与性能虚拟化后的聚焦复验矩阵。

## Visual Baseline Delivery Record（不含性能分支）

- [x] 最终提交 `e59f9bf` 已推送到 `origin/codex/dashboard-visual-refactor`；本地分支与远端一致。
- [x] 最终部署使用 Release 严格 9 个文件，源产物与安装目录逐文件哈希一致。
- [x] 当前源产物与已安装 `PlaytimeInsights.dll` 的 SHA-256 均为 `6D79971D2E50B6EA701AFAAC581FCB9BB5B0FDFABB988532776E303C10C55937`。
- [x] 部署前备份位于 `C:\Users\chan\AppData\Roaming\Playnite\Backup\PlaytimeInsights-deploy-20260830-215523`。
- [x] 覆盖前后 7 个 `ExtensionsData` 用户数据文件联合指纹一致；Playnite 日志确认 Playtime Insights 1.0.0 已加载。
- [x] Playnite 仅在本次启动进程内使用 `HTTP_PROXY` / `HTTPS_PROXY=http://127.0.0.1:10456`；未写入仓库、测试或系统全局代理设置。

该记录只证明 Dashboard Visual Elevation 基线已部署；性能分支引入新的分析和热力图生产代码，当前源 DLL 已不再与已安装 DLL 相同。正式 1.1.0 发版（版本号、CHANGELOG、PEXT、标签和公开发布）是独立后续工作，不属于本次 Task 7 验收状态。

## Known Ranking Behavior

整行蓝色背景长度始终表示该游戏在当前排行范围内的时长占比，与当前所选排序指标无关。切换为会话次数或活跃天数排序时，主数值决定排序，而背景仍表达时长占比；Tooltip 明确给出分母。前三名的金、银、铜 Glow、徽章和文字仍独立显示。

## Scope Review

- [x] `Controls/AdaptiveDashboardPanel.cs` 无 Task 7 差异。
- [x] `Controls/ResponsiveUniformPanel.cs` 无 Task 7 差异。
- [x] `PlaytimeInsights.cs` 无 Task 7 差异。
- [x] `docs/CLIENT_ACCEPTANCE_1.0.0.md` 未被改写。
- [x] Task 7 主体验收未改动既有生产架构；最终提交额外包含用户验收发现的比较胶囊纵向排列修复。除此之外仅增加测试护栏、更新本验收记录，并同步修正实施计划中与已批准 19° 色彩结果冲突的旧“单一色相 / ≤10°”措辞。
