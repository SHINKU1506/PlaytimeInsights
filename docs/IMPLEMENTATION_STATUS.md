# Playtime Insights 实现状态

最后更新：2026-08-13

当前阶段：0.9.8 已正式发布；架构重构 A–E 与 Dashboard 选择性刷新客户端验收通过

## 2026-08-13 响应式指标卡与 Dashboard 语义文本基础

- 本批次实现文件：新增 `Controls\ResponsiveUniformPanel.cs`；修改 `Views\PlaytimeInsightsDashboardView.xaml` 和 `Tests\Program.cs`；本 Task 3 仅更新本文件与 `docs\VISUAL_UX_OPTIMIZATION_PLAN.md`；
- `ResponsiveUniformPanel` 使用 204/232/300 像素宽度约束、1–4 列、12 像素横纵间距、同排等宽等高和不完整末行居中；指标卡移除固定 218 × 154，保留 `MinHeight=154`；
- 自动化覆盖 320/360/640/900/1200 内容宽度、0/1/9/10 子元素、长本地化文本、无限约束和无效属性值；真实 Dashboard 运行时测试通过视觉树类型定位 `ResponsiveUniformPanel`；
- Panel 无 `x:Name`。WPF `CS0426` 根因是无命名根元素，按批准修正改为真实视觉树类型定位，避免使用会触发该错误的命名路径；
- 最终回归：`Tests\bin\Release\net462\PlaytimeInsightsRegression.exe` 输出 99/99，所有项目均为 `[PASS]`，并以 `All Playtime Insights tests passed.` 结束；
- 测试项目 Release：`dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore`，0 警告、0 错误；生产项目 Release：`dotnet build PlaytimeInsights.csproj -c Release --no-restore`，0 警告、0 错误；
- Dashboard 已加入四个命名语义透明度资源：`TextOpacityPrimary`、`TextOpacitySecondary`、`TextOpacityTertiary`、`TextOpacityDisabled`；Sessions/settings 跨页面抽取仍待后续；
- 本轮未部署、未打包、未启动 Playnite，未修改主题、配置或用户数据；未触碰任何安装目录或用户数据文件；
- `perf_test.ps1` 是主工作区用户保留的未跟踪文件；隔离工作树中不出现属于正常状态，本任务未触碰它，也不会将其加入提交；
- 本批次完成范围仅为响应式指标卡和 Dashboard 语义文本基础；空状态、跨页面 token 共享、微交互和会话列宽治理仍是后续工作。

### 客户端视觉验收（待用户在 Playnite 中执行）

以下步骤分别用中文和英文执行；客户端验收尚未通过，仍是唯一外部未完成项：

#### 中文

1. 在 100%、125%、150%、200% DPI 下，分别使用默认深色、Seaside、浅色和 Windows 高对比度主题；
2. 在 Dashboard 内容宽度 320、360、640、900、1200 以及连续拖动调整窗口时，确认指标卡按 1/2/3/4 列稳定换行；
3. 确认同排卡片等宽、等高，最后一张不完整末行居中，长标题和辅助文本可读且不截断、不重叠；
4. 确认纯展示指标卡没有 Hover 或 Click affordance；
5. 确认现有图表、筛选、下钻和滚轮行为未改变。

#### English

1. At 100%, 125%, 150%, and 200% DPI, check the default dark, Seaside, light, and Windows high-contrast themes;
2. At Dashboard content widths 320, 360, 640, 900, and 1200, plus continuous window resizing, confirm stable 1/2/3/4-column metric-card layout;
3. Confirm equal widths and heights within each row, a centered final card in an incomplete row, and readable long titles/helper text without clipping or overlap;
4. Confirm that the display-only metric cards expose no hover or click affordance;
5. Confirm that existing charts, filters, drilldown, and wheel behavior are unchanged.

## 架构重构准备分支

- 2026-08-13 用户完成客户端验收：连续切换聚合、排名、时间范围和元数据筛选未发现本轮选择性
  刷新回归，趋势/热力图/排名下钻、星期联动、封面和显式刷新行为通过；
- 当前架构 PR 的范围至此冻结，只接受审查所需修正和验收记录，不再混入新的性能实现；
- 后续仍有两个可感知性能方向：首次切入插件主页面时的完整 `DataReload` 卡顿，以及切换时间范围
  时的完整分析卡顿。两者共享“完整刷新”路径，应在当前 PR 合并后从最新 `main` 新建
  `perf/dashboard-full-refresh-pipeline` 分支统一设计和测量，并分别保留独立验收场景；
- 后续性能分支优先使用现有 data/filter/analytics/apply/total Trace 确认瓶颈，再决定纯 DTO 后台分析、
  generation、取消过期请求和轻量加载状态的实施范围，不预先把所有阶段一律异步化。

- 任意筛选变化的轻微卡顿已定位为同一 UI 线程入口无条件执行数据读取、完整统计、封面解析及
  多个列表逐项发布；当前 18 条会话不是主要耗时证据；
- 新增强类型刷新原因和刷新计划：聚合/排名分别只重算趋势/排名投影，范围与元数据变化复用缓存，
  只有 `DataReload` 读取 Playnite 数据库、LibraryPlugin 和 SessionRepository；
- `AnalyticsService` 现在一次生成完整快照和 `DashboardAnalysisContext`，局部投影不重新拆分会话、
  生成热力图或高级统计；游戏封面索引也只在数据加载时建立一次；
- 主要 Dashboard 列表已改为完整 `IReadOnlyList` 一次发布，避免 `Clear + Add` 引发 WPF 重复布局；
  会话下钻的真实增量分页不变；
- 新增 8 项性能回归，当前共 95/95 项；最终干净 Release 0 警告/0 错误，10 万会话 618 ms，
  schema 4 载入 1,165 ms；
- DLL 为 305,664 字节，SHA-256：
  `10D9153B303979C2E897FBA71E30E3064C8D2085E0842B4E38B904808DB97AEC`；确定性 PEXT 为
  143,596 字节，SHA-256：
  `51755E6EC35275D2D3CBE065C64398BF4EB761591B59C518BD48D727070C6178`；
- Release、`staging\dashboard-selective-refresh\deployed` 与安装目录 9/9 哈希一致，包内仅有 9 个
  安全条目且 DLL 无敏感路径；部署前后 7 个用户数据文件指纹保持
  `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`；
- 本轮客户端验收已完成；后续客户端性能基线改为插件主页面首次进入与时间范围切换两条完整刷新
  路径，不再重复打开已经通过的聚合/排名局部刷新范围。

- 分支：`refactor/architecture-preparation`；
- 完整事件、按钮、键盘、焦点与副作用基线已落盘到
  `docs\ARCHITECTURE_REFACTOR_BASELINE.md`；
- `docs\ARCHITECTURE_OPTIMIZATION_PLAN.md` 已增加阶段 0 完成状态和下一步准入条件；
- 新增第 62 项静态架构护栏：动态扫描四个 XAML 的事件处理器，要求全部进入职责矩阵；
- 护栏禁止 ViewModel 引入 MessageBox、文件对话框、具体 Window 类型和外部 MVVM/Behavior
  框架，并锁定现有 CanExecute 来源、分页可见性和两个对话框的键盘语义；
- 阶段 0 只建立文档与测试护栏；阶段 A 仅新增接口、未接线 Coordinator 和 ViewModel 接口声明，
  不修改业务执行逻辑、会话数据或客户端行为；
- 已新增强类型 `ISessionManagementInteraction`、`ISessionManagementOperations` 和尚未接线的
  `SessionManagementCoordinator`；
- 8 项假交互测试覆盖文件选择取消、预览取消、删除拒绝、无效恢复、恢复拒绝、导出失败、编辑
  取消和重建拒绝；
- 阶段 B 新增非泛型/泛型 RelayCommand，无第三方依赖；会话页刷新/分页/恢复和主看板刷新/分页/
  星期筛选已改为命令，趋势与热力图 Code-behind 只保留参数适配；
- 会话页和主看板刷新期间均禁用相关命令，并在选中、分页和刷新状态变化时主动刷新 CanExecute；
- 导出异常统一使用无访问键的 `LOCPlaytimeInsightsExportFailedTitle`，不会再显示“导出 CSV(_C)”；
- 中英资源各 272 个键；Release 构建 0 警告/0 错误，当前 74/74 回归通过；
- 阶段 B Release 已在 Playnite 关闭时部署到 `staging\architecture-stage-b` 和插件安装目录，
  两处与 Release 输出 9/9 文件一致；DLL 大小 290,816 字节，SHA-256 为
  `654CEDAE3753E205507828C0A5634D4D5234CAD26CD7F60C93392408EC77B5B0`；
- 部署前后用户数据均为 7 个文件，联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`；
- 阶段 C 新增 `WpfSessionManagementInteraction` 并由插件根组装；会话页 10 个多步骤处理器现只做
  Coordinator 转发，文件选择、Owner、确认、窗口和错误呈现已移出 View；
- `.claude/` 已加入根 `.gitignore`；
- 新增 6 项阶段 C 接线、完整成功路径和异常路径回归；Release 构建 0 警告/0 错误，当前
  80/80 回归通过；
- 阶段 C Release 已在 Playnite 关闭时部署到 `staging\architecture-stage-c` 和插件安装目录，
  两处与 Release 输出 9/9 文件一致；DLL 大小 290,816 字节，SHA-256 为
  `2CA2F983EC130F965A86B77D63A2CD352617182DE8E8382586E65608A2A607BE`；
- 部署前后 7 个用户数据文件联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`；
- 阶段 D 首个机械提交已将 Dashboard 条目、选择项和快照类型移入 `ViewModels\Dashboard`，类名、
  命名空间、XAML 绑定和客户端行为保持不变；
- 根 `DashboardViewModel` 已组合 Filter、Metrics、Distribution、Drilldown 四个子 ViewModel，
  文件由约 1094 行缩减到 354 行，既有根级属性作为兼容转发层保留；
- 根对象仍是唯一全量读取会话并创建 `DashboardSnapshot` 的位置，子状态不持有 Repository、
  不重复调用 `CreateSnapshot`；
- 新增 1 项阶段 D 组合边界护栏，当前 81/81 回归通过；Release 构建 0 警告/0 错误；
- 干净构建的 10 万会话分析为 510 ms，相对机械搬迁验证的 521 ms 未退化，符合 10% 预算；
- 阶段 D Release 已部署到 `staging\architecture-stage-d` 和本机插件目录；Release、staging、
  安装目录均为 9 个文件且逐项 SHA-256 一致；
- 阶段 D DLL 为 294,400 字节，SHA-256：
  `0156F2C0F11D5310BF4B79B26958BDAD010F4433A40C46A704DFA3AA2713764D`；
- 部署前后用户数据均为 7 个文件，联合指纹保持
  `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`；
- Playnite 保持关闭；客户端验收应重点检查筛选联动、指标/排名、星期切换、趋势/热力图下钻、
  封面缩略图和“加载更多”分页；
- 阶段 D 客户端复审发现主页面每次侧边栏重入都会重建 ViewModel，导致“本年”等筛选恢复为
  默认“本月”；现改为在插件实例生命周期内缓存 Dashboard ViewModel，页面重入时复用筛选状态
  并刷新数据；Playnite 退出后缓存自然释放，下次启动仍使用默认值；
- 页面关闭仍清除 `activeDashboard`，因此隐藏期间不会刷新已关闭的 UI；星期选择、下钻和分页会在
  重入刷新时按原逻辑重置。新增运行期导航状态回归，当前共 82/82 项通过；Release 构建
  0 警告/0 错误；
- 修复版已部署到 `staging\architecture-stage-d` 和本机插件目录，三处 9/9 文件一致；DLL 为
  295,424 字节，SHA-256：
  `A91685ADF8AF60C35E9D81053F5CE34C2EA3FA19F45809F50984FE037556CCA7`；部署前后 7 个用户数据
  文件联合指纹保持 `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`；
- 侧边栏卡顿复审确认两个页面在 `Opened` 与 `Loaded` 中各刷新一次，每次进入实际执行两轮同步
  全量扫描；“ViewModel 文件行数”和当前 18 条/约 14 KB 会话数据不是本机主要瓶颈证据；
- 已移除两个 `Opened` 刷新，保留 `Loaded` 为唯一自动入口；显式刷新、筛选变更、会话 CRUD、
  游戏停止后的刷新与 Dashboard 运行期筛选缓存均保持；
- 会话 `CountText` 已改用本轮 `GetAllIncludingDeleted()` 结果计算的活动会话总数，WPF 读取绑定
  属性时不再触发额外的仓库克隆和排序；
- 两项性能回归均按 TDD 先失败再通过；干净 Release 构建 0 警告/0 错误，当前 84/84 项通过，
  10 万会话分析为 483 ms；未缓存会话 ViewModel 或游戏列表，未引入异步和数据库事件订阅；
- 性能修复 Release 已部署到 `staging\architecture-stage-d` 和本机插件目录，Release、staging、
  安装目录均为 9 个文件且逐项 SHA-256 一致；DLL 为 294,400 字节，SHA-256：
  `7A763012974D25685A512CDB4A10A7ACE32FF31C08B09DF2A583F20DFA807ADE`；
- 部署前后用户数据均为 7 个文件，联合指纹保持
  `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`；Playnite 保持关闭；
- 客户端应验收两个页面首次进入和来回切换、显式刷新、Dashboard 筛选保留、会话计数/筛选/分页
  以及完全重启后的默认“本月”；
- 阶段 E 对四个 View 完成事件源/处理器双向审计，未发现孤立处理器，生产删除数为 0；保留的
  Loaded、自定义事件、滚轮、ContextMenu、Coordinator 转发和窗口内部事件均补充职责注释；
- 新增 `docs\ARCHITECTURE.md` 和第 85 项阶段 E 护栏；护栏动态拒绝孤立事件/方法，不以
  Code-behind 行数为完成标准；命令本地化、AutomationProperties、CanExecute、默认/取消按钮、
  循环 Tab 和初始焦点继续由既有自动化覆盖；
- 两轮独立干净 Release 构建均为 0 警告/0 错误、85/85 回归通过；10 万会话分别为 567 ms 和
  561 ms，均在发布预算内；
- 原始 Toolbox PEXT 的条目内容完全一致但外层哈希受 DLL ZIP 时间戳影响；新增
  `scripts\Pack-Deterministic.ps1`，严格验证 9 文件并在临时副本中统一时间戳，不修改 Release；
- 两轮确定性 PEXT 均为 139,177 字节，SHA-256：
  `3DDF721B41078D694984D044C71797A38A801098D1359B6F824EDED1926F9126`；DLL 均为 294,400 字节，
  SHA-256：`7A763012974D25685A512CDB4A10A7ACE32FF31C08B09DF2A583F20DFA807ADE`；
- 两个包均仅含 9 个预期条目，逐项内容与 Release 相同，不含 PDB、绝对路径或额外文件；
- 第二轮 Release 已部署到 `staging\architecture-stage-e\deployed` 和插件安装目录，三处 9/9
  文件一致；部署前后 7 个用户数据文件联合指纹保持
  `C318F566DFB2032202836D457D1CC0E5C77CDDED09921136A7273007B594225A`；
- Playnite 保持关闭；下一步为客户端复验 Dashboard/会话导航、显式刷新、筛选保留、图表下钻、
  滚轮、Advanced Options、编辑/导入窗口键盘行为及会话 CRUD/导入导出。
- 聚合粒度切换后的旧图残留已定位为自绘 `AdaptiveTrendChart` 没有监听同一
  `ObservableCollection` 的内容变化：依赖属性引用不变时 `AffectsRender` 不会触发，旧的
  `renderedItems`、`renderedPoints` 和 Hover 索引会一直保留到后续鼠标、布局或视口事件；
- 修复按 TDD 分两步完成：第一项回归先因两次 `Apply` 返回同一集合引用失败，随后
  `DashboardDistributionViewModel` 改为一次发布完整的新
  `IReadOnlyList<PeriodActivityViewModel>`；第二项真实 STA/WPF 回归先因换源后缓存仍为 1 失败，
  随后控件通过 `CollectionChangedEventManager` 弱订阅新源、退订旧源，并统一重置 Hover 与
  渲染缓存后调用 `InvalidateVisual()`；
- 最终双 clean Release 构建 0 警告/0 错误，87/87 回归通过；10 万会话分析为 557 ms，schema 4
  JSON 载入为 1,066 ms，均在既有发布预算内；
- 最终 DLL 为 294,912 字节，SHA-256：
  `B142B20DAF2EA1F6B968A3F96557CA4CD7B393A8DA1EF161E424B4468816F18C`；确定性 PEXT 为
  139,530 字节，SHA-256：
  `E74F9B5774DBFF44BE168CB136D72650EF77549BEBCBCCFFAAFB22799DF6CA05`；
- Release 与 PEXT 均严格包含 9 个预期文件，PEXT 不含 PDB、绝对/父级路径，DLL 未扫描到用户
  名、开发目录或 PDB 痕迹；Release、`staging\atomic-trend-refresh\deployed` 和插件安装目录
  9/9 文件哈希一致；
- 部署前后用户数据均为 7 个文件，以相对路径、长度、UTC 时间戳和文件 SHA-256 生成的规范化
  联合指纹保持 `8739B76AD190E16BC9BCD752D268B6FE52C4C59D5B169D9779836ACEFE3C18EF`；
  Playnite 保持关闭；
- 后续异步可取消刷新仍仅作为独立性能路线保留：先捕获不可变 DTO，再引入后台纯分析、generation、
  取消过期请求与轻量加载状态；本轮未改变统计口径、图表视觉、下钻、XAML、版本或用户数据。

## 0.9.8 正式发布候选

- `extension.yaml` 与程序集统一升级为 0.9.8 / 0.9.8.0；
- 星期分布 Tooltip 直接使用完整本地化星期标签，不再显示“星期 周一”之类的重复表达；
- 趋势周期和日历热力图下钻会话现保留 GameId、解析 Playnite 本地封面路径，并在游戏列显示
  24 × 34 缩略图；缺少封面或游戏已从库删除时保持稳定占位；
- Release 设置 `DebugType=None`、`DebugSymbols=false` 和稳定 `PathMap`，不再生成或分发 PDB；
- MIT `LICENSE` 已加入 Release 输出和 PEXT，包仍为 9 个预期文件；
- 新增 `manifests\installer.yaml` 与 `manifests\addon.yaml`，版本、API 6.16.0、Release URL、
  源码、图标、隐私与反馈链接均已落盘；
- 发布元数据自动回归扩展到版本、README、Release 调试策略、LICENSE 和两层 manifest；
- 干净 Release 构建 0 警告、0 错误，61/61 自动化回归通过；
- DLL 未扫描到用户名、开发目录或 PDB 路径；
- PEXT 只包含 9 个预期条目：DLL、清单、三个图标、LICENSE、PRIVACY 和两个本地化文件；
- Release、`staging\0.9.8` 与安装目录 9 个文件逐项 SHA-256 一致；
- 部署前后用户数据均为 7 个文件，内容、长度与时间戳联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`；
- DLL SHA-256：
  `9BEFE2370DA5BA3E21F5E5E55862B59497EC6DA8CE6840BD268942F900DB5AB4`；
- DLL 大小 282,112 字节；
- PEXT SHA-256：
  `09ACBD2CE1B62346AC658C4FE3C2539FA456394C7CC6773EAE46BBAA3BAB4B82`；
- PEXT 大小 134,031 字节；
- 2026-07-30 用户确认当前客户端检查未发现问题；现有配置下的原位升级、主要分析页、
  会话管理页和星期 Tooltip 修复通过；
- 2026-08-09 下钻封面修复 Release 构建 0 警告/0 错误，61/61 回归通过；Release、staging、
  安装目录 9 文件一致，部署前后用户数据指纹不变，等待客户端复验；
- 2026-08-10 正式发布复审发现 SDK-style WPF 会误编译被 Git 忽略的 `staging` 本地化 XAML；
  项目已显式排除所有 staging 默认编译项并加入回归；连续两轮干净构建 DLL 哈希相同，obj 中
  staging BAML 为 0，61/61 回归通过；
- 依赖枚举仅包含 Playnite.SDK 6.16.0.0、WPF 和 .NET Framework 系统程序集；中英资源各
  271 键，DLL 不含 PDB、本机用户名、源码目录或 Playnite 安装路径；
- 最终 PEXT 含 9 个预期条目、LICENSE 和 0.9.8 清单，不含 PDB；Release、staging、安装目录
  逐文件一致，部署前后 7 个用户数据文件联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`；
- 两层 manifest 通过一次性本机 HTTP 和正式公网 URL 完整联动校验；2026-08-11 匿名访问正式
  仓库、图标、三张截图、两层 manifest 和 GitHub Release 附件均为 HTTP 200；
- 星期分布选中卡片新增 `#334A90E2 → #1A4A90E2` 低透明渐变、蓝紫 1px 全围框、轻微蓝色
  Glow、右上角状态点、白色粗体标题及 120ms 上移/回落动画；筛选逻辑保持不变；
- 2026-08-11 用户确认当前客户端验收完成；正式截图已归档中文/英文分析页和中文设置页，并接入
  README/Add-on manifest；会话管理页截图不纳入公开发布材料；
- extension、程序集、LICENSE、README 和 Add-on manifest 的公开 Author 统一为
  `SHINKU1506`；独立 Portable 安装和卸载验收已通过；
- 最终发布提交 `8e19718` 已推送，`v0.9.8` 标签和 GitHub Release 已创建，134,031 字节 PEXT
  已上传；Toolbox Installer/Add-on 正式远程校验均通过；
- Add-on Database 清单已提交为 [PR #626](https://github.com/JosefNemec/PlayniteAddonDatabase/pull/626)，
  当前等待上游审核合并。

0.9.8 发布链路已完成。后续只需跟踪 Add-on Database PR 的上游审查结果；不再有本地发布阻塞项。

## 0.9.4 公开界面、README 与清单链接

- `extension.yaml` 和程序集统一升级为 0.9.4 / 0.9.4.0；
- 分析页和会话页页头删除版本号、发布候选状态与阶段功能说明，只保留稳定页面标题；
- 中英资源各移除两个副标题键，现各为 271 个键；
- README 已完全按当前实现重写，新增功能、数据口径、兼容性、安装升级、基本使用、数据隐私、
  已知限制、源码构建、文档、问题反馈和许可证章节；
- README 已删除 0.9.2 错误版本及已删除的聚合柱形图/按日柱形方案描述；
- `extension.yaml` 新增源码、GitHub Issues 和 CHANGELOG 三个公开链接；
- 新增第 61 项回归，静态校验版本、程序集、三个 URL、无副标题资源及 README 必备章节；
- Release 构建 0 警告、0 错误，61/61 回归通过；
- Release、`staging\0.9.4` 和安装目录 9 个文件逐项 SHA-256 一致；
- 0.9.4 PEXT 内部只含 9 个预期文件，打包清单中的版本和三个链接正确；
- DLL SHA-256：`4BF5CE09C4A89B904FE67E0FF41F9A5822899F4D7863DCE68BC3D0355FC0E904`；
  PEXT SHA-256：`15BAFE3225882D430129722EC2B9B54210F317591D0A9AFB9CFF1F99247C384D`；
  PEXT 大小：191,488 字节；
- Playnite 部署时保持关闭，`ExtensionsData` 部署前后内容指纹一致。

## 2026-07-29 正式发布就绪审查

- 核心候选质量通过：Release 0 警告/0 错误、61/61 回归、schema 1–4 升级、压力预算、
  九文件一致性和用户数据保护均已有证据；
- 程序集引用仅包含 Playnite SDK 6.16.0、WPF 和 .NET Framework 系统程序集；
- 当前 15 个源文件/文档改动尚未提交，远程 `main` 仍为 `c6469bd`；
- 当前没有 Git 标签、GitHub Release、Installer manifest 或 Add-on manifest；
- 清单/程序集现为 0.9.4，界面候选字样与旧功能说明已删除，README 已按现状重写；
- 当前 DLL 嵌入本机绝对 PDB 路径，PEXT 也没有包含 MIT LICENSE；
- 现有六张审查图均为 0.9.2，尚缺最新主看板、会话页和英文正式截图；
- 最新会话页与滚轮接力仍需用户客户端验收，并在 1.0.0 版本冻结后完成干净安装及
  0.9.4 原位升级测试；
- 结论为“尚不可直接正式发布”；完整 P0/P1 清单见 `docs\RELEASE_READINESS_1.0.md`。

## 2026-07-29 主看板滚轮一致性修复

- 主看板最外层是负责整页滚动的纵向 `ScrollViewer`；
- 24 小时分布、星期 × 小时热力图和日历热力图内部另有只开启横向滚动的 `ScrollViewer`；
  WPF 鼠标滚轮事件先到命中的子滚动器，即使其纵向滚动已禁用，也可能不再冒泡给外层；
- 异常会话 `ListBox` 和会话钻取 `ListView` 自带纵向滚动，鼠标位于其中时优先滚动内部列表；
  当内部列表无需滚动或已经到达顶部/底部时，事件仍不会自动转交外层；
- 星期分布、趋势图、排名等没有独立滚动器的区域，滚轮会直接到达外层，因此表现正常；
- 结论：差异来自嵌套滚动容器的事件所有权，并非统计数据、黑色背景或鼠标设备问题；
- 外层滚动器现命名为 `DashboardScrollViewer`，三个横向图表区和两个内部列表区统一绑定
  `NestedScrollViewer_PreviewMouseWheel`；
- 横向图表区没有纵向滚动范围，直接将纵向滚轮重新路由到外层页面；
- 异常列表和会话钻取列表通过 `VerticalOffset` / `ScrollableHeight` 判断当前方向：
  自身可滚时保持内部滚动，到顶或到底后自动接力整页；
- 新增第 60 项静态回归，锁定外层命名、五处绑定、双向边界判断和事件重路由；
- Release 构建 0 警告、0 错误，60/60 回归通过；
- Release、staging 和安装目录 9 个文件哈希一致，部署前后用户数据内容指纹一致；
- DLL SHA-256：`8BCBEA1AAC4567EDB8F1882160FB1186258A53B0F1F105C368FC9888CEE8F2F8`；
  PEXT SHA-256：`E78CBB43FB26E29D07B408794BBE66453AEBEC4003FB64AD87ED27CA498222F7`；
  PEXT 大小：191,682 字节。

0.9.1 客户端发现的星期标签问题已在 0.9.2 修复。星期分布、日历热力图和“星期 × 小时”
热力图现在统一读取 Playnite 当前语言资源；中文 Windows 地区 + Playnite English 的真实客户端
检查已确认三处均显示 `Mon`–`Sun`。同一检查还发现英文月份标题曾因 `{0:MMMM yyyy}` 重新读取
Windows 地区而显示中文月份，0.9.2 最终候选已改为插件控制的 `yyyy/M` 数字格式。

## 客户端基线

用户已完成 0.5.x 检查并要求进入下一阶段。当前稳定基线包括：

- 今天、本周、本月、本年和自定义范围；
- 自定义起止日期只在自定义范围下显示；
- 日、周、月、年聚合；
- 五种区间排名；
- 正常停止和异常退出恢复。
- 原生热力图、柱形、折线和会话钻取；
- 会话列表限高滚动、Recycling 虚拟化和 100 条分批加载；
- 默认自动粒度及手动覆盖。

0.5 继续直接读取现有 schema 1/2 会话，没有修改数据结构或用户会话文件。

## 0.5 已完成

- 原生 WPF 日历热力图；
- 热力图星期顺序跟随 ISO 周一或系统地区周起点设置；
- 热力强度按当前范围最大日时长线性缩放；
- 与柱形图共享同一聚合数据的原生折线趋势；
- 柱形、折线点和热力格精确 tooltip；
- 三类图形均支持点击；
- 热力格点击钻取单日会话；
- 柱形或折线点点击钻取对应日/周/月/年周期；
- 周期首尾自动裁剪到当前查询范围；
- 会话明细显示游戏名称、本地开始时间、区间内时长和会话来源；
- 跨午夜会话钻取只累计落入所选日期的秒数；
- 当前游戏名称优先，游戏删除后使用会话名称快照；
- 整套实现不使用 WebView、第三方图表库或远程资源。

## 0.5.1 已完成

- 会话明细改为原生 `ListBox`；
- 最大高度固定为 380，超过后在列表内部垂直滚动；
- 启用 `VirtualizingStackPanel`；
- 启用 `VirtualizationMode=Recycling`；
- 启用基于条目的逻辑滚动；
- 首批只向界面集合发布 100 条；
- 每次点击“加载更多”追加最多 100 条；
- 显示“已显示 X / 总数 Y”；
- 全部显示后自动隐藏加载按钮；
- 新增独立 `SessionDetailPager`，可脱离 Playnite API 测试分页边界。

## 0.5.2 已完成

- 聚合下拉框新增“自动（推荐）”并设为默认；
- 今天、本周、本月自动使用日粒度；
- 本年自动使用月粒度；
- 自定义 1–62 天使用日粒度；
- 自定义 63–730 天使用周粒度；
- 自定义 731–3650 天使用月粒度；
- 自定义超过 3650 天使用年粒度；
- 图表标题同时显示实际粒度和“自动”标记；
- 手动选择日、周、月或年时完全绕过自动规则；
- 热力图继续保留逐日细节，不受趋势聚合粒度影响。

## 0.8.0 已完成

- 第二个原生侧边栏“Playtime Insights · 会话”；
- 独立 `SessionManagementViewModel` 和 `SessionManagementView`；
- 搜索当前游戏名、快照游戏名、游戏来源、平台和会话来源标签；
- 自动记录、异常恢复、导入和手动来源筛选；
- 平台快照拆分、去重、排序和精确筛选；
- 最新会话优先；
- 首批 200 条，每次加载更多 200 条；
- `VirtualizingStackPanel` 与 Recycling 容器复用；
- 当前筛选结果 CSV 导出；
- CSV UTF-8 BOM、全字段双引号和内部引号转义；
- 当前筛选结果版本化 JSON 导出；
- 导出成功后显示条目数和文件名；
- 取消文件对话框时不创建文件；
- 导出失败只显示错误；
- schema 2 保持不变，用户会话存储只读。

## 0.8.1 已完成

- 移除会误导为会话运行环境的“平台”筛选；
- 默认元数据维度改为库来源；
- `Game.PluginId` 映射加载中的 `LibraryPlugin.Name`；
- 手动添加和未知/未加载库分别标记；
- 可切换 Playnite 来源、发行商、标签、类型、分类和安装状态；
- 元数据值不区分大小写去重并按当前地区排序；
- 关键字搜索覆盖全部当前元数据；
- 会话列表最后一列显示库来源；
- 游戏已删除时显示历史会话但不匹配当前元数据筛选；
- `suppressFilterRefresh` 阻止更新选项期间 setter 发起查询；
- `RefreshReentrancyGuard` 拒绝所有嵌套刷新；
- 修复“全部平台”和 `PC (Windows)` 重复出现；
- `IGameMetadataAccessor` 使所有维度可独立测试；
- schema 2 和用户会话文件保持不变。

## 0.8.2 已完成

- 元数据筛选新增开发者；
- 会话文档与新会话默认 schema 3；
- schema 1/2 会话加载后规范化为 schema 3；
- `GetAll()` 默认排除已删除会话；
- `GetAllIncludingDeleted()` 供管理页使用；
- Repository 对外返回深拷贝会话；
- 原生补录窗口；
- 原生编辑窗口；
- 可修改游戏、开始日期、开始时间和持续秒数；
- 本地时间按当前 Windows 时区转换为 UTC；
- 补录来源标为 `Manual`；
- 编辑保留原始会话来源与恢复原因；
- 软删除与恢复；
- “包含已删除”筛选和状态列；
- 删除、恢复与编辑记录修改时间和原因；
- 编辑时阻止与其他会话重复；
- 分析、热力图和排名自动忽略软删除会话；
- schema 3 写操作继续使用原子替换和备份。

## 0.8.3 已完成

- 会话存储升级到 schema 4；
- 新增 `ImportSource` 和 `ImportConfidence` 导入审计字段；
- 原生导入预览窗口，确认前不写入任何数据；
- 支持多选 Playtime Insights JSON、完整备份 JSON 和 CSV；
- 支持 GameActivity 单游戏 JSON 和会话明细 CSV；
- GameActivity JSON `DateSession` 按 UTC 解释；
- GameActivity CSV 本地时间按当前 Windows 时区回转 UTC；
- GameActivity CSV 自动识别逗号/分号/制表符及中英文表头；
- 兼容 ISO 8601 和旧式 `HH.mm.ss` 时间；
- RFC 4180 风格 CSV 引号、转义引号和跨行字段解析；
- 游戏 ID 精确关联、唯一同名关联和稳定外部 ID；
- 多个同名 Playnite 游戏时拒绝猜测并写入错误报告；
- 游戏名、时间、时长安全上限逐行校验；
- 与现有数据及同一导入批次双重去重；
- 错误列表可在预览窗口保存为 UTF-8 文本；
- 确认导入后统一标记来源为 `Imported`；
- 导入提交前自动创建 `pre-import` 时间戳回滚备份；
- 手动完整备份到用户选择的 JSON；
- 恢复文件预验证与原生确认；
- 恢复入口拒绝把筛选导出 JSON 当作完整备份；
- 恢复前自动创建 `pre-restore` 回滚备份；
- 恢复完成会话时保留当前运行检查点，不复活备份中的旧检查点；
- 重建存储索引前自动创建 `pre-reindex` 回滚备份；
- 重建会规范化 schema、排序、修复空/冲突 ID 并移除重复指纹；
- CSV 导出补齐软删除、修改审计和导入审计字段；
- 数据变更后同步刷新已经打开的分析页和管理页；
- 全部功能继续使用原生 WPF、Playnite 自带序列化器和 BCL，不使用 WebView 或新增运行时 DLL。

用户已完成 0.8.3 导出、导入、备份、恢复与重建检查，未发现问题。

## 0.8.4 已完成

- 分析页新增“不筛选 / 库来源 / 开发者 / 类型 / 标签 / 安装状态”维度；
- 库来源复用会话管理页的 `LibraryPlugin.Id → Name` 映射，并保留手动添加和未知库标签；
- 筛选值从当前 Playnite 游戏元数据动态去重并排序；
- 元数据筛选统一限制区间指标、周期图、日历热力图、高级图表、会话钻取、区间排名、
  Playnite 累计总时长和累计排名；
- `HourlyAllocationService` 按会话保存的时区拆分本地小时；
- 时区 ID 不可用时回退到开始 UTC 偏移；
- 小时边界在 UTC 时间线上查找本地整点，兼容夏令时跳时和重复小时；
- 小时分片按墙钟区段比例分配，最后分片吸收整数余数，总秒数严格守恒；
- 新增原生星期分布柱形图；
- 新增原生 24 小时时段分布柱形图；
- 新增原生 7 × 24 星期 × 小时二维热力图；
- 星期顺序继续跟随 ISO 周一设置或系统地区周起点；
- 新增范围内最长连续游玩天数；
- 新增截至范围结束日或今天的当前连续游玩天数；
- 新增上一等长区间环比；
- 新增去年同期同比与闰日安全处理；
- 零基数对比显示“新增”或“持平”，普通对比显示一位小数百分比；
- 新增零秒、结束早于开始、未来开始、至少 18 小时以及秒数显著大于墙钟时长的异常提示；
- 异常提示只读、按时间倒序、最多 50 条，不修改或删除会话；
- 新图表和筛选全部使用原生 WPF，没有引入 WebView、第三方图表库或远程资源。

用户已完成 0.8.4 五类元数据筛选、高级分布、连续天数、同比/环比和异常提示检查，未发现问题。

## 0.9.0 已完成

- 会话管理页新增原生“保存诊断报告”按钮；
- 诊断文件由用户主动选择路径，使用 UTF-8 文本；
- `SessionRepository.GetStorageDiagnostics()` 在线程锁内生成只读存储摘要；
- 摘要包含 schema、完成/运行中/删除/来源会话数量、文件大小、备份数量和存储状态；
- 报告不写入游戏名称、会话时间、用户目录、游戏 ID 或会话 ID；
- 报告明确声明插件不自动上传诊断内容；
- 设置页新增“隐私与诊断”说明并支持内容超高后的原生滚动；
- 新增随插件安装的 `PRIVACY.md`；
- `HourlyAllocationService` 缓存“时区 ID + 保存偏移”的解析结果；
- 无夏令时时区使用下一个本地整点直接换算，夏令时时区继续使用 UTC 时间线安全边界；
- 新增 5,000 游戏、100,000 会话、十年范围完整分析压力测试；
- 新增 schema 4 JSON 的 100,000 会话加载压力测试；
- 两项压力测试均设 30 秒发布预算；
- 新增 schema 1–4 到当前 schema 4 的身份、归属、名称和秒数无损升级回归；
- 测试执行器保持独立程序集名称，压力数据全部为临时合成数据，不读取用户真实会话。

## 图表口径

- 热力图：一个格子代表一个本地自然日；
- 强度：`0.18 + 当日秒数 / 范围最大日秒数 × 0.82`，零时长为 0.08；
- 柱形与折线：使用完全相同的 `PeriodActivities`；
- 趋势坐标：范围最大周期位于图表顶部基准，其余按秒数线性缩放；
- 钻取：逐会话按本地自然日拆分，只累计点击范围内的分片；
- 所有计算保留原始秒数，格式化仅发生在显示层。

## 已执行验证

| 验证 | 结果 |
|---|---|
| net462 Release 编译 | 成功，0 warning / 0 error |
| WPF 热力图 XAML 编译 | 成功 |
| WPF Canvas / Polyline 编译 | 成功 |
| 0.2/0.3 原有 12 项回归 | 全部通过 |
| ISO 周热力图布局 | 通过 |
| 热力强度相对大小 | 通过 |
| 折线点最大值缩放 | 通过 |
| 周期钻取边界裁剪 | 通过 |
| 跨午夜钻取秒数 | 通过 |
| 异常恢复来源标签 | 通过 |
| 关键词 + 来源 + 平台组合筛选 | 通过 |
| 会话最新优先排序 | 通过 |
| 平台拆分、去重与排序 | 通过 |
| CSV 逗号、引号和换行转义 | 通过 |
| JSON 版本、数量与会话内容 | 通过 |
| Playtime Insights CSV 导出后导入往返 | 通过 |
| GameActivity JSON 的 UTC 和游戏 ID 映射 | 通过 |
| GameActivity 中文分号 CSV 与本地时间回转 | 通过 |
| 导入无效行与重复报告 | 通过 |
| 导入提交前回滚备份 | 通过 |
| 恢复替换会话且保留当前运行检查点 | 通过 |
| 恢复拒绝筛选导出 JSON | 通过 |
| 重建索引修复 ID 并移除重复指纹 | 通过 |
| 跨小时分片、日期/小时桶与秒数守恒 | 通过 |
| 星期、24 小时与 7 × 24 分布矩阵 | 通过 |
| 最长连续游玩天数 | 通过 |
| 上一等长区间环比与去年同期同比 | 通过 |
| 同比闰日日期回退 | 通过 |
| 异常会话只读提示 | 通过 |
| 库来源、开发者、类型、标签和安装状态游戏筛选 | 通过 |
| 5,000 游戏 / 100,000 会话 / 十年完整分析 | 约 300 ms，通过 30 s 预算 |
| schema 4 JSON / 100,000 会话加载 | 约 1,136 ms，通过 30 s 预算 |
| schema 1、2、3、4 无损升级到当前 schema | 通过 |
| 诊断报告排除名称、时间、路径与 ID | 通过 |
| 设置页隐私说明与诊断报告 XAML | 编译通过 |

当前全部 49 个自动化测试通过。

最终 Release 构建结果：0 个警告、0 个错误。

0.9.0 安装包：

`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_0.pext`

安装包 SHA-256：

`A05DF1B36CC6316C1A87EEFD1B31191AABD7552B301097A2DBB53782C486B148`

0.9.0 暂存目录：

`staging\0.9.0`

## 部署核对

- 部署前已确认 `Playnite.DesktopApp` 进程不存在；
- 已确认 `Playnite.DesktopApp` 和 `Playnite.FullscreenApp` 均未运行；
- 已从 `staging\0.9.0` 更新 DLL、PDB、清单、图标和 `PRIVACY.md`；
- 已安装程序集：`PlaytimeInsights, Version=0.9.0.0`；
- 已安装清单版本：`0.9.0`；
- 已安装 DLL SHA-256：
  `06F46351FB061E336DA5FBBCC26E5B2B748F897CF6BE93E026A15175AEC2C092`；
- 已确认安装目录包含 `PRIVACY.md`；
- 已安装 DLL 与暂存 DLL 哈希完全一致；
- 部署前后 `ExtensionsData` 全部 7 个文件的长度、时间戳和 SHA-256 完全一致。

## 当前文件变化

- `Services/AnalyticsService.cs`：热力格、趋势坐标、周期边界和会话明细查询；
- `ViewModels/DashboardViewModel.cs`：热力、趋势、明细集合和图形选择状态；
- `Views/PlaytimeInsightsDashboardView.xaml`：原生热力图、折线图、增强柱形和明细区；
- `Views/PlaytimeInsightsDashboardView.xaml.cs`：三类图形点击事件；
- `Tests/Program.cs`：新增 4 项 0.5 分析测试；
- `Tests/PlaytimeInsights.Tests.csproj`：加入测试所需 WPF 图形程序集引用；
- `ViewModels/SessionDetailPager.cs`：100 条一批的可测试会话分页器；
- `Views/PlaytimeInsightsDashboardView.xaml`：限高、独立滚动和 Recycling 虚拟化列表；
- `Tests/Program.cs`：新增 250 条数据的 100/100/50 分批边界测试；
- `Services/AnalyticsService.cs`：新增 `Auto` 粒度与跨度解析；
- `ViewModels/DashboardViewModel.cs`：自动粒度成为默认选项；
- `Tests/Program.cs`：新增全部自动跨度边界和手动覆盖测试；
- `Services/SessionQueryService.cs`：会话组合筛选、平台选项和管理行映射；
- `Services/SessionExportService.cs`：CSV/JSON 导出和可注入序列化边界；
- `ViewModels/SessionManagementViewModel.cs`：筛选、200 条分页和导出状态；
- `Views/SessionManagementView.xaml(.cs)`：第二原生侧边栏和保存文件对话框；
- `PlaytimeInsights.cs`：注册会话侧边栏并在新会话后刷新；
- `Tests/Program.cs`：新增筛选、排序、平台、CSV 和 JSON 测试；
- `Services/SessionQueryService.cs`：可切换元数据维度和可注入访问器；
- `ViewModels/SessionManagementViewModel.cs`：元数据选项、刷新抑制和库插件映射；
- `ViewModels/RefreshReentrancyGuard.cs`：拒绝嵌套刷新；
- `Views/SessionManagementView.xaml`：维度/值双下拉和库来源列；
- `Tests/Program.cs`：元数据去重、库映射和重入保护测试；
- `Models/GameSession.cs`：schema 3 删除和修改审计字段；
- `Services/SessionRepository.cs`：克隆查询、更新、软删除和恢复；
- `ViewModels/SessionEditorViewModel.cs`：本地时间与精确秒数校验/构建；
- `Views/SessionEditorWindow.xaml(.cs)`：原生补录与编辑窗口；
- `ViewModels/SessionManagementViewModel.cs`：包含已删除、选择和变更命令；
- `Views/SessionManagementView.xaml(.cs)`：补录、编辑、软删除和恢复控件；
- `Tests/Program.cs`：开发者、迁移、编辑、删除恢复和补录测试；
- `Models/SessionDataManagement.cs`：导入预览、提交、恢复和重建结果；
- `Models/GameSession.cs`：schema 4 导入来源和可信度；
- `Services/SessionImportService.cs`：格式识别、CSV/JSON 解析、映射、验证和去重；
- `Services/SessionRepository.cs`：批量导入、时间戳回滚、完整恢复和重建索引；
- `Services/SessionExportService.cs`：schema 4 CSV 字段完整往返；
- `Views/SessionImportPreviewWindow.xaml(.cs)`：原生候选/错误预览与错误报告；
- `Views/SessionManagementView.xaml(.cs)`：导入、完整备份、恢复和重建入口；
- `ViewModels/SessionManagementViewModel.cs`：数据工具编排和跨视图刷新；
- `Tests/Program.cs`：新增 8 项导入、GameActivity CSV、回滚、恢复与重建测试；
- `Services/HourlyAllocationService.cs`：本地小时分片、夏令时边界与秒数守恒；
- `Services/AdvancedAnalyticsService.cs`：星期/小时矩阵、连续天数、对比和异常提示；
- `Services/AnalyticsService.cs`：将高级统计快照接入现有统一范围口径；
- `Services/SessionQueryService.cs`：增加可复用的当前游戏集合筛选；
- `ViewModels/DashboardViewModel.cs`：元数据筛选选项、统一过滤集合和高级统计绑定；
- `Views/PlaytimeInsightsDashboardView.xaml`：原生高级图表、对比卡片、连续天数和异常列表；
- `PlaytimeInsights.cs`：向分析页注入共用的 `SessionQueryService`；
- `Tests/Program.cs`：新增 7 项高级分析与游戏筛选测试；
- `Tests/PlaytimeInsights.Tests.csproj`：测试执行器使用独立名称，避免与插件程序集身份混淆；
- `Models/SessionDiagnostics.cs`：不含会话身份内容的存储诊断 DTO；
- `Services/SessionDiagnosticsService.cs`：隐私安全的诊断文本生成和用户选定路径保存；
- `Services/SessionRepository.cs`：线程安全的只读存储摘要；
- `Services/HourlyAllocationService.cs`：时区缓存和无夏令时快速整点路径；
- `ViewModels/SessionManagementViewModel.cs`：诊断报告编排与状态提示；
- `Views/SessionManagementView.xaml(.cs)`：原生诊断保存入口；
- `PlaytimeInsightsSettingsView.xaml`：隐私与诊断说明；
- `PRIVACY.md`：随包发布的完整隐私边界；
- `Tests/Program.cs`：新增十年分析、10 万 JSON 加载、全 schema 升级和诊断隐私测试；
- `PlaytimeInsights.csproj`：将隐私文档复制到发布产物；
- 程序集、清单、README、路线、开发文档和变更日志更新到 0.9.0。

## 0.9.3 会话管理操作层级与表格视觉

- 数据工具区改为左右两组：左侧为导入 JSON/CSV、导出 CSV、导出 JSON；右侧为
  “⚙ 高级选项”；
- 高级菜单收纳软删除、恢复、完整备份、从备份恢复、重建存储索引和保存诊断报告；
  软删除与恢复继续根据当前行状态自动启用或禁用；
- 顶部常用操作只保留补录、编辑和刷新；
- 会话列表行高从 48 降为 44，增加 `AlternationCount=2` 斑马纹、蓝色整行 Hover、
  选中背景和左侧主题色边线；
- 游戏名前增加 24 × 34 封面缩略图；路径来自 Playnite 本地数据库，
  OnLoad 解码后立即释放句柄，缺失封面保持固定占位；
- 开始时间和游玩时长的列头与内容均右对齐；来源与状态居中显示为圆角 Tag；
- 异常恢复来源为橙色，有效状态为绿色；追踪、导入、手动和已删除也有独立可辨色调；
- 200 条分页、Recycling 虚拟化、筛选、横向滚动、导入导出、备份恢复和软删除数据逻辑均未改变；
- 中英资源各 273 个键；新增会话管理视觉层级静态回归；
- Release 构建 0 警告、0 错误，60/60 自动化回归通过；
- Release、`staging\0.9.3` 与安装目录均为 9 个文件，逐文件 SHA-256 一致；
- DLL SHA-256：`8BCBEA1AAC4567EDB8F1882160FB1186258A53B0F1F105C368FC9888CEE8F2F8`；
  PEXT SHA-256：`E78CBB43FB26E29D07B408794BBE66453AEBEC4003FB64AD87ED27CA498222F7`；
  PEXT 大小：191,682 字节，内部仍为 9 个预期文件；
- Playnite 部署前保持关闭；`ExtensionsData` 部署前后内容清单指纹一致，没有写入用户数据。

## 0.9.3 发布准备

- 在项目根目录新增 `.gitignore`，排除 .NET/MSBuild 输出、测试与覆盖率结果、`dist`、
  `staging`、`.pext`、NuGet 包、IDE 用户设置、日志、转储和临时文件；
- 增加防泄漏规则：若 `ExtensionsData`、`sessions.json`、导出或备份被误复制到源码树，
  Git 将忽略这些用户运行数据；
- 正式源码、清单、本地化、图标、`Assets` 设计源与 `docs\audit` 审查证据不在忽略范围；
- 新增 `docs\RELEASE_CHECKLIST.md`，记录版本、构建、测试、九文件打包、哈希和用户数据保护检查；
- README 的本机源码目录已由具体用户路径改为 `%AppData%\Playnite\Development\PlaytimeInsights`；
- 源码关键字扫描未发现 API key、token、password 或 secret；存储路径引用均为插件预期行为；
- 当前源码目录不是 Git 仓库，本轮未执行 `git init`、提交或标签操作；
- 尚无 `LICENSE`，远程仓库、发布页与标签策略也未配置，以上均为公开发布前待维护者决定事项；
- 已用一次性临时 Git 仓库验证规则：构建、暂存、PEXT 和用户数据样例全部忽略，README、XAML、
  正式图标及 `docs\audit` 样例保持可跟踪；验证目录随后已删除；
- 当前 DLL 和 PEXT 哈希已随滚轮修正更新为 `8BCBEA1A...F2F8` 与
  `E78CBB43...22F7`，PEXT 仍只含 9 个预期文件；
- 本轮未修改 C#、XAML、资源或发布文件，因此无需重新构建、部署或打包；
- Playnite 保持关闭；`ExtensionsData` 仍为 7 个文件，最新修改时间保持
  `2026-07-27T09:29:17.7770314Z`，现有 0.9.3 PEXT 与已部署二进制保持不变。

## 已知限制

- 热力图尚未绘制月份文字轴，但日期和时长可由 tooltip 精确确认；
- 折线图当前显示基础水平参考线，尚无移动平均；
- 完整命中结果当前仍一次保存在内存中；0.5.1 优化的是 UI 集合发布量和 WPF 可视容器数量，数据库
  达到数十万会话时仍需在存储查询层引入真正的分页；
- 编辑窗口当前以持续秒数作为精确输入，尚未提供“小时/分钟/秒”拆分输入；
- 会话查询、预览和去重仍在内存中扫描单个 schema 4 JSON；真正的存储层分页留待大型数据性能测试；
- 元数据筛选反映当前 Playnite 游戏记录；标签、发行商或库归属后续变化不会作为历史快照保存；
- 热力图在自定义超长日期范围下仍会产生水平滚动，这是为了保留逐日细节；柱形和折线已自动降粒度；
- 精确日期统计只覆盖插件开始记录后的会话；
- 异常恢复仍最多损失约一个检查点间隔；
- 10 万会话读取和分析已通过预算；写入、导入预览及更高数量级仍保留观察；
- GameActivity 当前支持单游戏 JSON 与会话明细 CSV，不直接读取其 LiteDB 或硬件采样明细；
- 旧式 GameActivity 本地化日期存在日/月歧义时按当前 Windows 地区解析，推荐优先使用原始 JSON 或当前版 CSV；
- 独立测试进程无法初始化 Playnite 静态序列化服务；真实 JSON 反序列化需在客户端 Demo 中确认。

## 0.9.1 已完成

- 新增 `Localization/en_US.xaml` 与 `Localization/zh_CN.xaml`，当前各 214 个非空资源键；
- `App.xaml` 合并英文安全回退；静态界面使用 `DynamicResource`；
- 新增 `Services/LocalizationService.cs`，运行时文本通过 Playnite `ResourceProvider` 解析，并正确识别
  独立测试环境的 `<!LOC...!>` 缺失标记；
- 仪表盘、会话管理、编辑、导入预览、设置、侧边栏、文件筛选器和主要确认/错误对话框完成本地化；
- 日期范围、聚合、时长、排名、对比、连续天数、异常原因、会话来源、元数据维度及操作状态完成动态本地化；
- 五个原生视图增加显式 Tab 顺序、初始焦点或循环键盘导航；
- 主要交互控件和状态区域增加 `AutomationProperties.Name`；
- 按钮使用访问键，并修复会话管理页“恢复/刷新”和“删除/包含已删除”的冲突；
- 图表、热力图和状态均保留文字或 tooltip 冗余，不单独依赖颜色；
- 新增资源键对称/非空测试和原生视图本地化/无障碍静态测试；
- Release 主项目和测试项目构建均为 0 警告、0 错误；
- 51 项回归全部通过；本轮压力基线为十年 10 万会话分析约 258 ms、schema 4 读取约 1,141 ms。

0.9.1 最终发布产物：

- 安装包：`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_1.pext`；
- 暂存目录：`staging\0.9.1`；
- 程序集：`PlaytimeInsights, Version=0.9.1.0`；
- 清单版本：`0.9.1`；
- DLL SHA-256：`F503F29BDD589914109979E4E6CBBE2BC504F594E0AA3EDBDE204E2FF9BC94C6`；
- PEXT SHA-256：`1972052B02D6E2302E1EED8E6CBFAC5400FFD8C3BFACA6F64363DA663B764A25`；
- PEXT 已核验包含 7 个文件：DLL、PDB、清单、图标、`PRIVACY.md`、`en_US.xaml`、`zh_CN.xaml`；
- 暂存与安装目录的 7 个文件逐项 SHA-256 一致；
- 最终 Release 构建 0 警告、0 错误，51/51 回归通过；
- 最终压力采样为十年 10 万会话分析约 275 ms、schema 4 读取约 1,425 ms；
- 部署前再次确认 Playnite 进程未运行；部署没有触碰 `ExtensionsData`，现有数据与备份时间戳均未变化。

## 0.9.2 已完成

- 新增 `Services/WeekdayLabelService.cs`，按周起点返回 7 个 LOC 星期标签；
- `AnalyticsService` 和 `AdvancedAnalyticsService` 移除各自的 `CurrentCulture` 星期实现，星期分布与
  两张热力图共用同一来源；
- 中英资源新增完整星期短名称；Windows 地区与模拟 Playnite 资源语言交叉回归确认语言不串线；
- 英文月份标题使用 `{0:yyyy/M}`，并增加禁止 `MMM`/`MMMM` 回归，阻止 Windows 地区月份名称渗透；
- 新增 `Services/WindowLayoutService.cs`，编辑和导入预览窗口按工作区约束尺寸；
- 编辑窗口支持缩放和垂直滚动，导入预览候选支持局部水平滚动；
- 仪表盘和会话页长标题、帮助、状态与摘要文本换行，会话、异常及候选长行使用局部滚动；
- 仪表盘外层明确禁用水平滚动，保持指标卡片按真实视口宽度自动换行；
- 弹窗使用默认主题和 Seaside 均支持的 `PopupBackgroundBrush`，发布 XAML 的主题画刷已纳入白名单回归；
- 设置校验、导入解析、备份和恢复主要错误路径完成中英本地化；
- 中英资源各 272 个非空键，键集合、格式化占位符和源码 LOC 键引用均由自动化检查；
- 新增 100%、125%、150%、200% DPI 工作区尺寸用例和小窗口/长文本静态布局审查；
- 0.1–0.9 历史设置 JSON 默认值矩阵通过；schema 1–4 数据无损升级矩阵继续通过；
- 星期分布改为可选择按钮，联动下方 24 小时分布；再次选择同一天恢复全部星期；
- 选择状态具有动态标题、主题高亮、中英提示和自动化名称，范围/筛选/刷新变化时安全重置；
- Release 主项目与测试项目均为 0 警告、0 错误，58/58 回归通过。

0.9.2 真实客户端检查：

- 主题：Seaside；
- 简体中文启动、页面显示和语言恢复正常；
- Playnite English + 中文 Windows 地区下，星期分布和两张热力图均显示 `Mon`–`Sun`；
- 英文月份标题显示 `2026/7 · precise sessions`，不再显示中文月份；
- 指标卡片在当前窗口内按 5 + 4 换行，页面底部没有错误的全局水平滚动条；
- 日聚合等需要保留精度的区域只在自身范围内显示水平滚动；
- 分析入口显示蓝色“时钟 + 柱形图”，点击后正确打开分析页；
- 会话入口显示紫色“列表 + 时钟”，点击后正确打开会话管理页；
- 两枚图标在 Seaside 深色侧栏约 32 像素缩放和选中光晕下仍可清楚区分；
- 初始小时标题显示“24 小时分布 · 全部星期”；
- 点击周一后标题切换为“· 周一”并显示周一的 24 个小时数据；
- 切换到无记录的周二后小时柱全部归零，再次点击周二恢复全部星期；
- 取消选择后选中高亮正常消失；该轮联动检查结束时停在“周一已选”状态；
- 启动时偶发的 SaveManager 同步弹窗属于外部插件，不是 Playtime Insights 加载失败。

0.9.2 最终发布产物：

- 安装包：`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_2.pext`；
- 暂存目录：`staging\0.9.2`；
- 程序集：`PlaytimeInsights, Version=0.9.3.0`；
- 清单版本：`0.9.2`；
- DLL SHA-256：`FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`；
- PEXT SHA-256：`5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`；
- PEXT 大小：189,043 字节；
- PEXT 只包含 DLL、PDB、清单、主图标、两枚侧边栏图标、`PRIVACY.md`、`en_US.xaml` 和
  `zh_CN.xaml`；
- 暂存目录与安装目录 9 个文件逐项 SHA-256 一致；
- 部署前已确认 Playnite 完全退出；部署后重新启动并保持在“首条会话已选”的会话页供用户检查；
- 发布过程没有写入 `ExtensionsData`；全部 7 个数据/备份文件时间戳仍为 2026-07-27，
  内容清单指纹为 `A89418D0B55A3421456702508E0A05F047CC77F9CE719EF2FD194DCA000C1AC7`。

## 0.9.2 原生前端视觉整理

实现文件：

- `Views\PlaytimeInsightsDashboardView.xaml`；
- `Views\SessionManagementView.xaml`；
- `Localization\en_US.xaml`；
- `Localization\zh_CN.xaml`；
- `docs\audit\0.9.2-ui-polish-before\*`；
- `docs\audit\0.9.2-ui-polish-after\*`。

实现结果：

- 保持 Playnite Desktop 原生 WPF，未使用 WebView、外部网页、第三方 UI/图表库或远程资源；
- 两页标题新增基于 `GlyphBrush` 的原生导航标识，面板圆角统一为 8，内边距、字段标签字号和弱化层级统一；
- 仪表盘分析范围摘要移至面板右上角主题信息块，指标区与下方对比、分布面板保持原有绑定和布局能力；
- 星期按钮改用自定义 WPF `ControlTemplate`：悬停使用 `ControlBackgroundBrush`，选中使用
  `GlyphBrush` 底部描边，键盘焦点使用 `TextBrush` 四周细描边；真实客户端确认不会再出现浅色整块覆盖图表；
- 会话页把补录与编辑操作放到页头右侧；数据工具独立成次级按钮组，筛选器保留原绑定、访问键和焦点顺序；
- 会话列表新增游戏、开始时间、游玩时长、会话来源、状态和库来源列头，行高从 44 调整为 48；
- `SessionListItemStyle` 提供行分隔、悬停和左侧主题色选中反馈，并显式使用 `TextBrush`；
- 列表继续使用 `VirtualizingStackPanel`、Recycling 虚拟化、`MinWidth="860"` 和局部横向滚动，
  条目过多时仍由列表自身滚动，不增加页面高度；
- 中英资源新增 `LOCPlaytimeInsightsSessionState`，后续排名占比提示加入
  `LOCPlaytimeInsightsShareOfTotalFormat`，本轮趋势短标签与格式键加入后资源总数更新为每种语言
  272 个键。

验证结果：

- Product Design 审查前先在当前 Seaside 客户端保存两个页面截图，再按相同 1373 × 1019 窗口状态
  保存最终截图和左右前后对照；
- 两轮最终 Release 构建均为 0 警告、0 错误；
- 两轮最终自动化均为 58/58 通过，主题画刷白名单、原生视图本地化、无障碍标记、滚动与星期交互回归均通过；
- 真实客户端检查统计页标题、范围摘要、指标卡片、星期选择/取消、动态小时标题与滚动正常；
- 真实客户端检查会话页按钮主次、数据工具换行、筛选、列头、5 条真实会话、行悬停和选中态正常；
- 首轮客户端检查发现自定义 `ListBoxItem` 回落到系统深色前景，已通过显式 `TextBrush` 修正并重新完成
  构建、测试、部署和截图；
- 最终安装目录 DLL 与 Release DLL SHA-256 一致，PEXT 只包含 9 个预期文件；
- 全部 7 个用户数据/备份文件的长度、时间戳和 SHA-256 未变化。

2026-07-28 用户验收后修正：

- 原因一：九张指标卡由 `MetricCardStyle` 统一提供 `Margin="0,0,12,12"`，但“Playnite 累计总时长”
  卡片额外覆盖为 `Margin="0,0,0,12"`，在 WrapPanel 换行布局变化时会失去右侧 12 像素间距；
- 修正一：删除该局部覆盖，所有指标卡统一继承样式间距；新增静态回归，禁止再次出现
  `MetricCardStyle` 配合该无右边距覆盖；
- 原因二：再次点击星期后 `IsSelected` 已正确变为 `false`，小时数据也恢复总体，但按钮仍保有
  `IsKeyboardFocused=true`；旧焦点触发器同样使用 `GlyphBrush`，因此底部蓝条继续显示；
- 修正二：焦点状态改为四周 1 像素 `TextBrush`，选中状态显式设置底部 2 像素 `GlyphBrush`；
  新增静态回归，锁定两种状态的画刷和边框厚度；
- 修正版 Release 构建 0 警告、0 错误，58/58 自动测试通过；
- 部署时 Playnite 进程未运行；Release、`staging\0.9.2` 和安装目录 9 个文件逐项 SHA-256 一致；
- 修正版 PEXT 只包含 9 个预期文件；按用户要求部署后没有重新启动 Playnite。

2026-07-28 排名视觉增强：

- `GameRankingViewModel` 新增 `GameId`、`CoverImagePath`、`ProgressPercent` 和
  `ProgressTooltipText`；
- `DashboardViewModel` 通过 `IPlayniteAPI.Database.GetFullFilePath(game.CoverImage)` 把排名游戏
  映射到 Playnite 本地库封面；已从库删除、无封面或路径解析失败时返回空缩略图槽位；
- 新增 `Converters\CoverImageConverter.cs`，封面以 `BitmapCacheOption.OnLoad`、96 像素解码、
  `Freeze` 方式加载，避免锁定库媒体文件；
- 区间排名与累计排名改为共享 `GameRankingItemTemplate`，条目高度 64，封面显示区 36 × 50；
- 前三名分别使用金 `#D6B34B`、银 `#BFC7D5`、铜 `#C9824A` 的 30 × 30 圆形数字勋章；
- 名称和详情下方使用 5 像素高原生 `ProgressBar`，主题 `GlyphBrush` 以 0.5 透明度填充；
- 区间进度分母为当前范围和元数据筛选内全部精确会话秒数；累计进度分母为当前筛选后全部
  `Game.Playtime > 0` 的秒数；两者都不是“相对第一名”；
- 即使区间按会话数等非时长指标排序，进度条仍表达时长占比；自动化用 60 秒/600 秒样本确认
  第一名按会话数排序时进度为 9.09%，第二名为 90.91%；
- 静态回归确认 XAML 使用封面转换器、`ProgressPercent`、金银铜资源，并确认封面路径来自
  `GetFullFilePath`、转换器使用 OnLoad 与 96 像素解码；
- Release 构建 0 警告、0 错误，58/58 自动测试通过；
- Playnite 部署前未运行；Release、`staging\0.9.2` 与安装目录 9 个文件逐项 SHA-256 一致；
- PEXT 只含 9 个预期文件；部署后保持 Playnite 关闭，等待用户手动验收。

2026-07-28 顶部指标卡重构：

- 顶部仍保留 9 张核心指标卡，全部统一为 218 × 154、26pt Bold 主数字和 11pt
  `#8A8A8A` 辅助说明；
- 每张卡片右上角使用 Windows 内置 `Segoe MDL2 Assets` 单色字形，分别表达时长、会话、
  活跃日期、平均、最长、累计、连续游玩与异常；
- 删除占用整行的独立“时长对比”面板，把上一等长区间环比与去年同期同比合并到
  “区间游玩时长”卡片底部；
- Tag 卡面显示 `↑/↓/— + 绝对时长差 + 环比/同比`；增长使用绿色、下降使用蓝色，
  持平回落到 Playnite 主题中性色；
- 原 `DeltaText` 百分比没有移除，连同比较范围与基准时长一起放入 tooltip，保证信息没有丢失；
- `ComparisonMetricViewModel` 新增 `TagText`、`TrendKind`、`TooltipText`；
  `AdvancedAnalyticsService.CreateComparison` 负责方向、绝对差和本地化展示文本；
- 中英文各新增 4 个资源键，总数为 266；静态回归锁定 26pt、Bold、灰色辅助字、内置单色图标、
  两项卡内绑定、趋势语义色以及旧独立面板的移除；
- Release 构建 0 警告、0 错误，58/58 自动测试通过；未读写任何会话数据。
- 部署前后 Playnite 进程数均为 0；Release、`staging\0.9.2` 与安装目录 9 个文件逐项
  SHA-256 一致，重新打包的 PEXT 也只包含这 9 个文件；
- 本轮 DLL SHA-256 为 `FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`，
  PEXT SHA-256 为 `5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`，
  大小为 189,043 字节；
- `ExtensionsData` 仍为 7 个文件，最新时间戳保持
  `2026-07-27T09:29:17.7770314Z`；部署后保持 Playnite 关闭，等待用户手动验收。

2026-07-28 图表密度与视觉层级重构：

- “当前连续游玩”的 26pt 主值改为只显示天数，截止日期移动到 11pt 小字栏，并保留口径 tooltip；
- 星期分布删除柱下 `DurationText`，星期和 24 小时分布平时只显示横轴标签；精确时长仍通过
  `TooltipText` 在悬浮时显示；
- 两组柱体改为蓝→亮蓝→紫的原生 `LinearGradientBrush`，顶部圆角分别为 7 和 6；
- 星期 × 小时热力格由 24 × 24 增大为 30 × 30，日历热力格由 18 × 18 增大为 24 × 24；
  短范围居中，长范围保持局部水平滚动；
- 两张热力图增加 `#2A2A2E` 零值底色、1 像素主题网格线和蓝紫活跃叠加层；
- 趋势图移除直线 `Polyline`，新增冻结 `TrendLineGeometry` 与 `TrendAreaGeometry`；
  单调切线经 Hyman 限制后转换为三次 `BezierSegment`，避免平滑曲线在局部极值明显过冲；
- 面积层从约 40% 蓝紫透明度渐变到基线 0%；原圆点按钮位于曲线之上，tooltip、点击和周期钻取
  均保持原实现；
- 新增连续天数/截止日期拆分、贝塞尔段、闭合面积、渐变资源、热力网格、旧 Polyline 移除和
  星期常驻时长移除静态回归；
- 中英资源各 272 个键；Release 构建 0 警告、0 错误，58/58 自动测试通过；未读写会话数据。
- 部署前后 Playnite 进程数为 0；Release、`staging\0.9.2` 与安装目录 9 文件逐项哈希一致，
  PEXT 只包含这 9 个文件；
- 本轮 DLL SHA-256 为 `FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`，
  PEXT SHA-256 为 `5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`，
  大小为 189,043 字节；
- `ExtensionsData` 仍为 7 个文件，最新时间戳保持
  `2026-07-27T09:29:17.7770314Z`；部署后未启动 Playnite，等待用户手动验收。

2026-07-28 日聚合对比、钻取列与整行排名进度：

- `PeriodActivityViewModel` 新增 `IsDailyAggregation`；仅实际日粒度的柱体触发新方案；
- 聚合柱由 Border 改为 `Rectangle`，日粒度使用 `RadiusX=2`、`RadiusY=2` 和
  `#4A90E2 → #004A90E2` 垂直透明渐变；其他粒度继续使用主题 `GlyphBrush`；
- 会话下钻列表由无列头 ListBox 改为带 GridView 的 ListView，游戏、开始时间、游玩时长和
  会话来源列宽固定为 260/170/120/110，时长右对齐；
- ListView 继续保持最大高度 380、水平/垂直滚动、CanContentScroll、Recycling 虚拟化和
  既有 100 条分页；
- 排名条目移除名称下方 5 像素进度线，新增跨四列、延伸至整行边缘的底层 ProgressBar；
- 自定义进度模板完整声明 `PART_Track/PART_Indicator`，以 `#4A90E2`、Opacity 0.12 从左向右
  填充；徽章、封面、名称、详情和值按后续绘制层悬浮在色块之上；
- ProgressPercent 的范围、分母和 tooltip 未改动；只修改表达方式；
- 静态回归新增日粒度标记、精确渐变端点、Rectangle 圆角、GridView 四列、整行跨列进度、
  0.12 透明度和 WPF 模板部件约束。
- Release 构建 0 警告、0 错误，58/58 自动测试通过；
- 部署前后 Playnite 进程数为 0；Release、`staging\0.9.2` 与安装目录 9 个文件逐项
  SHA-256 一致，PEXT 只包含这 9 个文件；
- DLL SHA-256：`FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`；
  PEXT SHA-256：`5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`；
  PEXT 大小：189,043 字节；
- `ExtensionsData` 仍为 7 个文件，最新时间戳保持
  `2026-07-27T09:29:17.7770314Z`；部署后保持 Playnite 关闭。

2026-07-28 版本 0.9.3 自适应趋势交互：

- 清单和程序集升级为 0.9.3 / 0.9.3.0，两个页面副标题同步为 0.9.3；
- 删除聚合柱形 ItemsControl、日期/时长常驻标签以及趋势区横向 ScrollViewer；
- 新增纯 WPF `Controls\AdaptiveTrendChart.cs`，根据 `ActualWidth` 实时生成单调贝塞尔线与闭合渐变面积；
- X 轴按约 88 像素可用宽度自动稀疏标签，并强制保留最后一个周期；
- MouseMove 将横坐标映射到最近周期，绘制贯穿上下的虚线 Crosshair、蓝色锚点和半透明卡片；
- 卡片分三行显示周期标签、当前库/会话快照游戏摘要及“共/Total + 时长”；最多显示三个游戏后加“等/etc.”；
- 点击图表使用 `PeriodSelected` 事件复用 `DashboardViewModel.SelectPeriod`，会话下钻口径不变；
- 周期数超过 90 时隐藏常驻数据节点；90–179 点线宽 1.5，180 点及以上线宽 1；
- 曲线始终填满可用宽度，包含 365 个日点时也不创建横向滚动；
- 中英资源各 272 个键；Release 构建 0 警告、0 错误，58/58 测试通过。
- Release、`staging\0.9.3` 与安装目录 9 个文件逐项 SHA-256 一致；
- DLL SHA-256：`FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`；
  PEXT SHA-256：`5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`；
  PEXT 大小：189,043 字节；
- PEXT 只包含 9 个预期文件；Playnite 保持关闭，`ExtensionsData` 仍为 7 个文件且最新时间戳
  保持 `2026-07-27T09:29:17.7770314Z`。
- 用户复查确认 0.9.3 自绘控件颜色与此前不同；原因是控件初版使用了单色 `#4A90E2`；
- 已恢复 `#2F8CFF → #A45CFF` 蓝紫渐变线及蓝紫半透明渐隐面积；
- 日期标签由固定 88 像素估算升级为实际文字宽度碰撞检测，末标签预留区域与相邻 8 像素安全距
  共同阻止日期聚合下的偶发重叠；
- 修正版 Release 构建 0 警告、0 错误，58/58 测试通过。
- 主界面新增统一原生 `HelpIconButtonStyle`，分析范围、异常说明、时间分配口径、星期点击说明、
  日历说明、趋势交互、数据口径和导入安全提示均改为 `?` Tooltip；
- 问号按钮为 19 × 19、主题描边、Help 光标、60 秒 Tooltip，并提供中英文屏幕阅读器名称；
- 新增 `SessionDetailVisibility`：刷新时折叠空会话下钻面板，选择趋势/热力格后显示真实列表，
  “未显示会话”不再作为常驻占位文字；
- 关键状态、错误、当前范围、筛选数量和真实分页计数没有折叠；
- 中英资源各 272 个键；静态回归锁定 Tooltip 资源、移除常驻 Text 绑定和下钻显隐切换。

已知限制：

- 页头右侧操作组和会话列头以 Desktop 常用宽度为主；窄窗口继续依赖既有换行与局部横向滚动，
  本轮没有把页面重做为响应式 Web 布局；
- Seaside 启动时出现的 SaveManager 同步窗口来自外部插件，与 Playtime Insights 界面加载无关。

## 0.9.3 客户端 Demo 验收步骤

1. 启动 Playnite，确认侧边栏副标题显示 0.9.3 发布候选；
2. 在简体中文下查看星期分布、星期 × 小时热力图和日历热力图，确认星期均为中文；
3. 点击有记录和无记录的星期，确认 24 小时标题、柱形数据和选中态联动；再次点击同一天恢复全部，
   且星期下方蓝色选中条消失；
4. 切换到 English 并重启，确认上述三处显示 `Mon`–`Sun`，月份标题使用 `2026/7` 一类数字格式；
5. 反复缩放 Playnite 主窗口，确认指标卡片换行时横向和纵向始终保留间距，标题和帮助文本换行，
   页面没有全局水平滚动；
6. 打开会话页、编辑窗口和导入预览，检查长游戏名、长错误文本、垂直滚动和局部水平滚动；
7. 如 Windows 缩放不是 100%，重点确认编辑和导入预览窗口没有超出有效工作区；
8. 在当前主题下检查弹窗背景、正文和按钮可读性；如方便，可再用默认主题复查一次；
9. 切回常用语言并重启，确认会话数量、累计时长和现有筛选设置不变；
10. 检查分析页的主题色标题标识、右上角范围摘要、统一面板圆角及星期选中描边；
11. 检查会话页的页头操作主次、次级数据工具、固定列头、行分隔、悬停和左侧选中标识；
12. 滚动到区间与累计排名，确认有封面的游戏显示 36 × 50 缩略图，无封面游戏不会挤压文字；
13. 确认前三名数字勋章依次为金、银、铜，第四名以后恢复中性主题描边；
14. 将聚合粒度切换为“日”，确认聚合柱使用 `#4A90E2` 到透明的 2 像素圆角渐变；再切换周/月，
    确认恢复主题色，便于比较两种方案；
15. 点击聚合柱或趋势点，确认下钻列表出现游戏、开始时间、游玩时长、会话来源四个固定列头，
    各行内容形成整齐垂直列，水平滚动时列头同步；
16. 将排名依据切换为会话数等指标，确认排序改变但整行浅蓝背景长度仍表示时长占比，文字、封面
    和勋章位于背景之上且保持清晰，悬停可看到百分比；
17. 若启动时出现 SaveManager 同步弹窗，可先关闭或等待；该弹窗来自外部插件；
18. 如出现插件加载或布局异常，保留窗口缩放比例、Playnite 语言、主题名称和截图。

## 侧边栏图标已接入并完成客户端实测

- 分析入口：`icon-dashboard.png`，64 × 64 RGBA，语义为时钟与递增柱形图；
- 会话入口：`icon-sessions.png`，64 × 64 RGBA，语义为三行记录与时钟徽标；
- 组合预览：`Assets\SidebarIcons\sidebar-icons-preview.png`；
- 已检查深色、浅色背景及 64/32/20 像素显示，透明四角和缩小辨识度正常；
- 两个 `SidebarItem.Icon` 已分别指向对应文件，`PlaytimeInsights.csproj` 将两枚图标复制到发布输出；
- 第 57 项回归校验图标尺寸、RGBA PNG 色型、独立绑定和发布规则；
- 0.9.2 已重新构建、部署和打包，暂存与安装目录 9 个文件哈希一致；
- Seaside 客户端点击实测确认两个入口与页面对应正确；
- Playnite 当前保持关闭，等待用户手动启动验收修正版。

## 0.9.1 客户端 Demo 检查步骤

部署后：

1. 保持 Playnite 为简体中文，打开两个侧边栏页面，确认副标题为“0.9.1 发布候选 · 本地化与可访问性”；
2. 依次查看日期范围、聚合、排名、筛选、图表 tooltip、钻取明细、会话来源和数据工具状态，确认中文正常；
3. 打开补录/编辑、导入预览、删除确认、恢复确认及文件选择器，确认主要原生对话框没有混入英文；
4. 用 Tab 和 Shift+Tab 走查仪表盘、会话页、编辑、导入预览和设置，确认焦点可见、顺序合理且不会陷入控件；
5. 按 Alt 后测试按钮访问键，重点确认“刷新/恢复”和“软删除/包含已删除”不会互相冲突；
6. 如使用 Windows 讲述人，确认主要输入、筛选、按钮、会话列表和状态提示具有可理解名称；
7. 将 Playnite 界面语言切换为 English 并重启，重复打开两个页面，确认静态界面、筛选项、时长、范围、
   排名、对比、异常提示、状态消息和原生对话框显示英文；
8. 检查热力图和柱形图：即使不区分颜色，也能通过文字、tooltip 和钻取明细理解日期与时长；
9. 切回常用语言并重启，确认会话数量与既有统计不变；
10. 如插件未加载或资源显示为 `<!LOC...!>`，检查 `%AppData%\Playnite\extensions.log` 并告知具体键名。

## 下一动作

等待用户启动 Playnite 验收主看板滚轮：在三个横向图表区确认整页可滚，在异常列表和会话
钻取列表确认内部可滚且到达上下边界后自动接力整页；同时可复查会话页操作层级与紧凑表格。
确认后再进入 1.0 发布冻结并整理公开发布截图与 Add-on Database 元数据。
