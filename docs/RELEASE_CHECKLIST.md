# Playtime Insights 1.1.1 发布检查清单

更新日期：2026-10-09。通用顺序见 `PRE_RELEASE_WORKFLOW.md`；1.1.0 检查清单保留在 Git 历史中。
本文件记录发布候选冻结前的实际证据；上传后的下载、清单激活和远端引用证据记录在
[GitHub Release](https://github.com/SHINKU1506/PlaytimeInsights/releases/tag/v1.1.1) 正文中。

## 候选身份

- 插件 / 程序集版本：1.1.1 / 1.1.1.0；
- AddonId：`PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd`；
- 目标框架：.NET Framework 4.6.2；SDK / Required API：6.16.0；
- PR #2 已 Squash Merge 至 `e6cca01c9223f92bf2760125940c8f10e1d29170`，Issue #1 已自动关闭；
- 发布元数据提交：`ef0ad01`；正式制品由该提交的 `git archive` 干净源码副本构建；
- 最终发布提交仅补充本清单，生产源码与构建提交一致；以 `v1.1.1` 标签定位最终提交。

## 自动化与制品证据

- [x] extension、程序集、README、CHANGELOG 和 installer 顶部 package 版本一致；
- [x] installer 保留 1.1.0、1.0.0、0.9.8，AddonId 与最低 API 不变；
- [x] 干净源码测试项目和插件 Release 构建均为 0 warning / 0 error；
- [x] 干净源码完整回归 196/196 通过，包含当前 SDK 元数据、中英文、选项刷新和千项列表测试；
- [x] 两轮独立 Rebuild 的 DLL 均为 401,408 字节、程序集 1.1.1.0，SHA-256：
  `42A8045323D2D298D6423B30A28911BEB9B55A65F92D7F940D3CD5EF7394C5C3`；
- [x] 两轮确定性 PEXT 均为 177,143 字节，SHA-256：
  `65EE7346F09F42CEFC12D0A838C416EFE57E3A2F843CC2FDD70A25067FEBF24F`；
- [x] PEXT 文件名：`PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_1_1_1.pext`；
- [x] 包内严格为九个预期文件，各项哈希与 Release 目录一致，包内版本与 AddonId 正确；
- [x] 包内含 LICENSE、PRIVACY 和两个本地化 XAML，无 PDB、SDK DLL 或用户数据；
- [x] DLL 仅包含九个当前 WPF BAML 资源，无 artifacts/staging 备份资源、本机源码或调试路径；
- [x] 排除了部署备份被 WPF 默认编译项纳入的本地构建；没有用该制品发布。

## 发布与激活证据位置

远端动作在冻结发布提交、创建标签后执行；不能提前勾选通过。最终结果与精确提交号
记录在 GitHub Release 正文中，包括：注释标签、公开非预发布 Release、匿名 PEXT HTTP 200、
下载大小及 SHA-256、下载包内身份、本地 installer Toolbox 校验、推送 main 后的公开
installer/addon 校验，以及 main 与标签提交一致。附件门禁未通过时不推进远端 main。

本次为同一 AddonId 和 installer URL 的 package-only release，不需修改 PlayniteAddonDatabase。

## 人工验证范围与 Git 边界

用户参与了已部署功能的长列表效果检查并授权发布；正式 PEXT 原位升级、完整主题/DPI/
语言矩阵和 Playnite 更新检测仍未确认，逐项保留在 `CLIENT_ACCEPTANCE_1.1.1.md`。

用户保留的 `docs/superpowers/reviews/`、
`docs/superpowers/specs/2026-09-05-dashboard-visual-hierarchy-and-distribution-layout-design.md`、
`perf_test.ps1` 未纳入提交。制品、构建输出、部署备份、会话与日志仅留在本地忽略目录中。
