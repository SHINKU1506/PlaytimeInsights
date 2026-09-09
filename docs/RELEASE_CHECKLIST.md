# Playtime Insights 1.1.0 发布检查清单

更新日期：2026-09-09

通用顺序、安全激活和 Add-on Database 判定见 `PRE_RELEASE_WORKFLOW.md`。本文件记录 1.1.0
候选的实际证据；1.0.0 历史证据保留在 Git 历史与对应 Release 中。

## 候选身份

- 插件版本：1.1.0；
- 程序集版本：1.1.0.0；
- AddonId：`PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd`；
- 目标框架：.NET Framework 4.6.2；
- Playnite SDK / Required API：6.16.0；
- 候选源码提交：待创建最终发布提交；当前功能 HEAD 为 `4da4ab6`；
- PEXT 文件名：
  `PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_1_1_0.pext`。

## 已通过的自动化和本地部署证据

- [x] `extension.yaml`、程序集、README 与 CHANGELOG 使用 1.1.0 / 1.1.0.0；
- [x] installer manifest 已将 1.1.0 放在首位，并保留 1.0.0 与 0.9.8；
- [x] Playnite SDK 与 RequiredApiVersion 均为 6.16.0；
- [x] 2026-09-09 主项目和测试项目 Release 构建均为 0 warning / 0 error；
- [x] 2026-09-09 当前 190 项回归完成；首轮 100k 第五样本受整机并发负载影响升至 784 ms，
  同一二进制等待 15 秒复跑全部通过，100k 五样本 median/max 497/521 ms、schema 4 1,033 ms，
  一年/All Sessions 热力图 UI max 135.7/123.8 ms；失败样本保留，未放宽预算；
- [x] 最终 clean Release 输出严格为 9 个预期文件；DLL 为 401,408 字节、程序集 1.1.0.0，
  SHA-256 `92A59F52A1AE7CE5DD356A7DE2262A6F33421BF9DCBB8CE83D3E53F4923F3AC5`；
- [x] 本机曾部署相同功能 HEAD 的增量构建 DLL `E797AE072927001FC9ED7D264FBEA40A2E869556269A9457E78A4CDB59E96016`；
  部署前后 7 个用户数据文件联合指纹均为
  `D12D5D8C4D9CC1A77542B129BCD343DF3A206C3B70DB51CB005D085A08FC4D95`；回退备份位于
  `C:\Users\chan\AppData\Roaming\Playnite\Backup\PlaytimeInsights-deploy-20260905-222356`。

## 制品门禁

- [x] 两轮独立 clean Release 的 DLL SHA-256 均为
  `92A59F52A1AE7CE5DD356A7DE2262A6F33421BF9DCBB8CE83D3E53F4923F3AC5`；
- [x] 两轮确定性 PEXT 均为 176,733 字节，SHA-256 均为
  `0F76E01E58DD8BF6DB6FDA863A8F946AB01C1B034FD4E36B59BDF669C3F23B41`；
- [x] PEXT 严格包含 9 个与 Release 逐项哈希一致的安全条目，含 LICENSE、PRIVACY 和两个本地化 XAML；
- [x] PEXT 不含 PDB、Playnite SDK DLL、绝对路径、父级路径或用户数据；DLL 敏感/调试路径扫描 0 命中；
- [x] 包内 `extension.yaml` 为 1.1.0 且 AddonId 正确。

## 客户端门禁

- [x] 用户已完成当前源码的常规功能与视觉验收，并授权部署最新构建；
- [ ] 使用正式候选 PEXT 从公开 1.0.0 原位升级，确认设置、会话和备份保持；
- [ ] 重启 Playnite 后确认 1.1.0 加载，并复验快捷范围、指标卡、未来趋势、分布图宽度和
  Week×Hour 完整格填充；
- [ ] Reduced Motion 实机核验；实际读屏器播报按既定决定跳过，不作为发布失败项。

## 发布动作门禁

- [ ] 提交最终发布候选，并确认三个用户保留的未跟踪项未进入提交；
- [ ] 创建并推送注释标签 `v1.1.0`，暂不推送新版 `main`；
- [ ] 创建公开、非草稿、非预发布 GitHub Release，上传精确名称的 PEXT；
- [ ] 匿名 PackageUrl 返回 HTTP 200，大小、SHA-256 与本文件一致；
- [ ] Toolbox 对本地新版 installer manifest 校验通过；
- [ ] 附件门禁通过后推送 `main`，再对公开 installer/addon manifest 做联动校验；
- [ ] Playnite Add-on Browser 展示 1.1.0，并能从 1.0.0 更新。

## Git 边界

以下三个用户保留的未跟踪项不纳入本轮提交：`docs/superpowers/reviews/`、
`docs/superpowers/specs/2026-09-05-dashboard-visual-hierarchy-and-distribution-layout-design.md`、
`perf_test.ps1`。发布提交还不得包含 `bin`、`obj`、`dist`、`staging`、PEXT、测试结果、IDE
设置、日志、转储、`ExtensionsData`、会话、导出或备份。PEXT 只作为 GitHub Release 附件发布。
