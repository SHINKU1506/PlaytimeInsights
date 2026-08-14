# Playtime Insights 发布检查清单

更新日期：2026-08-14

通用顺序、命令、安全激活和 AddonDatabase 判定见 `PRE_RELEASE_WORKFLOW.md`。本文件只记录
1.0.0 的实际证据；0.9.8 历史证据保留在 `RELEASE_NOTES_0.9.8.md` 和 Git 历史中。

## 1.0.0 候选

- 插件版本：1.0.0；
- 程序集版本：1.0.0.0；
- AddonId：`PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd`；
- 目标框架：.NET Framework 4.6.2；
- Playnite SDK / Required API：6.16.0；
- 自动化回归：108/108；
- DLL：315,904 字节；
- DLL SHA-256：`6ACFACDA528398A263219511B737A6C9699FE7D8D21CAD43EC4CAB86B7EF2790`；
- PEXT：147,824 字节；
- PEXT SHA-256：`EBA048A7F71943B22E2566D899E81BB99BFD5570D0F26A65439789A3E081AB34`；
- PEXT 文件名：
  `PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_1_0_0.pext`；
- 两轮独立 clean Release 的 DLL 与 PEXT 哈希分别一致；
- PEXT 严格包含 9 个与 Release 逐项相同的安全条目，含 LICENSE，不含 PDB 或 Playnite SDK DLL；
- DLL 程序集版本为 1.0.0.0，未发现用户名、开发目录、Playnite 安装路径或 PDB 字符串；
- 构建和打包前后用户数据均为 7 个文件，联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`。

## 已通过门禁

- [x] `extension.yaml`、程序集、README、CHANGELOG、installer manifest 和自动化断言版本一致；
- [x] installer manifest 顶部为 1.0.0，并保留 0.9.8 package；
- [x] 测试项目 Release 构建 0 warning / 0 error；
- [x] 108/108 回归通过并输出最终 all-pass marker；
- [x] 主项目两轮 clean Release 构建 0 warning / 0 error；
- [x] 两轮确定性打包哈希一致；
- [x] 九文件、路径安全、许可证、版本、AddonId 和依赖边界检查通过；
- [x] 用户确认侧边栏 View 复用与共享封面缓存性能优化客户端验收通过；
- [x] 架构重构 A–E 和 Dashboard 选择性刷新已有客户端验收记录；
- [x] Add-on Database PR #626 已合并，稳定 installer URL 无需为 package-only release 修改。

## 发布动作门禁

- [ ] 完成 `CLIENT_ACCEPTANCE_1.0.0.md` 中 1.0.0 PEXT 原位升级与视觉矩阵；
- [ ] 提交发布候选并创建注释标签 `v1.0.0`；
- [ ] 推送标签但暂不推进远端 `main`；
- [ ] 创建 GitHub Release 并上传精确名称的 PEXT；
- [ ] 匿名 PackageUrl 返回 HTTP 200，大小和 SHA-256 与本文件一致；
- [ ] Toolbox 对正式公网 installer/addon manifest 完整校验通过；
- [ ] 以上通过后才推送 `main`，激活 Add-on Browser 更新；
- [ ] 远端激活后从 0.9.8 检测并完成 1.0.0 更新；
- [ ] 决定保留 0.9.8 目录截图，或另提 AddonDatabase 截图/描述 PR。

## Git 边界

发布提交不得包含 `bin`、`obj`、`dist`、`staging`、PEXT、测试结果、IDE 设置、日志、转储、
`perf_test.ps1`、`ExtensionsData`、会话、导出或备份。发布附件只上传 GitHub Release，不进入源码历史。
