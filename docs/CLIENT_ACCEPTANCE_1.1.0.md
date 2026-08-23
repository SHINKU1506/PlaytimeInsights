# Client Acceptance 1.1.0 — Dashboard Visual Elevation

状态：增量验收中
日期：2026-08-23

## Frozen Layout Contract

- KPI inventory: 2 Hero + 6 Tier 2 = 8
- Calendar heatmap: Primary column
- Drilldown: Secondary column after Ranking
- Heatmap column pitch: 26 DIP
- Heatmap visual cell: 24x24 DIP
- Heatmap hit target: 26x26 DIP
- Heatmap axes: week numbers horizontally centered; weekday labels vertically centered in 26 DIP rows
- Heatmap levels: 0h / <1h / 1–3h / >3h
- Heatmap first column: Monday
- Wide layout thresholds are panel content widths: enter at >= 1200, stay wide at >= 1160, exit below 1160
- Content width = view width - 48 (root StackPanel margin), minus the vertical scrollbar when visible

## Frozen Composition

- [ ] Wide: Trend/Distribution in Primary
- [ ] Wide: Ranking/Drilldown/Anomaly in Secondary
- [ ] Narrow source order: Trend, Ranking, Distribution, Drilldown, Anomaly
- [ ] Calendar heatmap stays in Primary

## Interaction States

- [ ] No drilldown selection
- [ ] Drilldown with 0 exact sessions
- [ ] Drilldown with 1 session
- [ ] Drilldown with 100 visible sessions and more available
- [ ] Reduced motion enabled

## Visual Matrix

- [ ] Content widths: 640, 900, 1159, 1160, 1199, 1200, 1440, 1600 DIP (record the view width used for each)
- [ ] Themes: Default Dark, Default Light, Seaside Dark, Windows High Contrast
- [ ] DPI: 100%, 125%, 150%, 175%, 200%
- [ ] Languages: zh_CN, en_US
- [ ] Ranges: one month, range crossing a month boundary, six-calendar-week month, one year, all sessions
- [ ] Ranking counts: 0, 1, 2, 3, 10
- [ ] Drilldown rows: 0, 1, 100, 250
- [ ] Keyboard: Tab, Space/Enter, focus outline, polite state announcement
- [ ] Heatmap layout timing measured for one year and all sessions

完整矩阵尚未执行，以上项目保持未勾选。用户于 2026-08-23 确认当前部署版本的视觉效果可以，作为本轮手动抽查记录，不替代完整主题、DPI、语言和宽度矩阵。

## Automated Gate

- [x] Release plugin build: 0 warning / 0 error (2026-08-23)
- [x] Release test build: 0 warning / 0 error (2026-08-23)
- [x] Full regression suite passes in five consecutive runs (2026-08-23)
- [x] 100k-session analytics <= 750 ms: min 615 ms / avg 662 ms / max 747 ms
- [x] schema 4 load <= 1400 ms: min 962 ms / avg 1,014 ms / max 1,115 ms

The five-run evidence was collected without Playnite or a concurrent review agent consuming CPU and disk. An earlier contended run measured 1,023 ms / 2,045 ms and exposed that the regression assertions still used a 30-second diagnostic ceiling; the assertions are now aligned with the frozen 750/1,400 ms release budgets.

## Known Ranking Behavior

- The translucent full-row ranking wash always represents duration share. The first three rows retain their independent gold, silver, and bronze card glows beneath that shared blue wash. When the selected ranking metric is session count, active days, average session, or longest session, the primary value controls sorting while the wash remains the share of playtime shown in its tooltip.
