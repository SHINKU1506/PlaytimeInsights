# Playtime Insights 1.0.0 客户端验收

状态：发布前检查中
日期：2026-08-14

## 已确认

- [x] 用户确认侧边栏 View 复用与共享封面缓存性能优化验收通过；
- [x] 已验收性能实现保持 Dashboard 重挂载、Loaded 刷新和封面失效语义；
- [x] 架构重构 A–E 与 Dashboard 选择性刷新已有客户端验收记录。

## 发布候选必须复验

- [ ] 1.0.0 PEXT 从 0.9.8 原位升级；
- [ ] 简体中文与 English；
- [ ] 默认深色、Seaside、浅色和 Windows 高对比度；
- [ ] 100%、125%、150%、200% DPI；
- [ ] 320/360/640/900/1200 内容宽度与连续缩放，指标卡不重叠、同排等高、末行居中；
- [ ] 辅助文字在目标主题中可读，指标卡无错误 Hover/点击暗示；
- [ ] 趋势、热力图、排名、星期联动、筛选、下钻与滚轮接力不变；
- [ ] 侧边栏连续切换至少 20 次，Dashboard 视觉树不增长，滚动与焦点状态符合预期；
- [ ] 封面文件修改/删除后缓存正确失效，无媒体文件锁定；
- [ ] 会话导入预览、CSV/JSON 导出、备份恢复、软删除/恢复和诊断报告；
- [ ] Playnite 重启后插件版本为 1.0.0，设置与 schema 1–4 会话数据正常；
- [ ] 无用户操作期间 `ExtensionsData` 文件数、长度、时间戳和 SHA-256 联合指纹不变。

## 发布外部门禁

- [x] 两轮确定性 PEXT 哈希一致；
- [x] GitHub Release 附件可匿名下载，HTTP 200、大小和 SHA-256 正确；
- [x] 正式公网 Toolbox installer/addon 校验通过；
- [x] 远端 `main` raw installer manifest 已以 1.0.0 为首包；
- [ ] Playnite Add-on Browser 实际检测并完成 0.9.8 → 1.0.0 更新；
- [ ] 若采用新版目录截图，AddonDatabase 元数据 PR 已合并；否则明确保留 0.9.8 截图。

## Dashboard Visual Refactor

### Frozen Layout Contract

- Enter wide layout: 1200 DIP
- Exit wide layout: 1160 DIP
- Column spacing: 18 DIP
- Secondary column ratio: 0.38
- KPI inventory: 2 Hero + 7 Tier 2 = 9
- AllSessions comparison: hidden

### Automated Gate

- [x] Release plugin build: 0 warning / 0 error (2026-08-14: 0 warning, 0 error)
- [x] Release test build: 0 warning / 0 error (2026-08-14: 0 warning, 0 error)
- [x] Full regression suite passes (2026-08-14: 128/128)
- [x] 100k-session analytics <= 750 ms (2026-08-14: 628 ms)
- [x] schema 4 load <= 1400 ms (2026-08-14: 1,073 ms)

### Visual Evidence Matrix

- [ ] Languages: zh_CN, en_US
- [ ] Themes: Default Dark, Default Light, Seaside Dark, third-party high contrast, Windows High Contrast
- [ ] DPI: 100%, 125%, 150%, 175%, 200%
- [ ] Widths: 400, 640, 900, 1159, 1160, 1199, 1200, 1600, 2400 DIP
- [ ] Data: empty, normal, long English names, large duration, comparison states, anomaly states, 100+ drilldown rows, ranking counts below 3 and above 10

截图目录和实际主题名将在完整矩阵执行后记录；当前没有满足上述整行覆盖要求的实机证据。

### Running Client Interaction Purity

- [ ] Quick range click: exactly one reason=Range
- [ ] Aggregation change: reason=Aggregation and no data reload
- [ ] Ranking metric change: reason=Ranking and no data reload
- [ ] Ranking Tab switch: no refresh trace
- [ ] Filter Expander toggle: no refresh trace
- [ ] 1199/1200 and 1160/1159 layout transition: no refresh trace
- [ ] Clear drilldown: no analysis refresh

以上项目仍需在运行中的 Playnite 客户端使用 refresh Trace 留存证据。
