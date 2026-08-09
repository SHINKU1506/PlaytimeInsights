# Playtime Insights 发布检查清单

更新日期：2026-08-09

## 当前候选

- 插件版本：0.9.8；
- 程序集版本：0.9.8.0；
- 目标框架：.NET Framework 4.6.2；
- Playnite SDK：6.16.x；
- Release 构建：0 警告、0 错误；
- 自动化回归：61/61；
- 中英文资源：各 271 个键；
- 发布包：`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_8.pext`。
- DLL SHA-256：`0A214AA04597B0AD7B853835FAAAF9156E236B9DA422A29474731B7322634787`；
- PEXT SHA-256：`62539010D9DC2F255181D08C416CB71F2962CAA58814E70AD050411470FC7201`；
- PEXT 大小：189,577 字节；
- PEXT 内容：9 个预期文件。
- Release 隐私：无 PDB，DLL 敏感路径扫描通过，MIT LICENSE 已入包。

## Git 边界

项目根目录的 `.gitignore` 排除：

- `bin`、`obj`、测试结果和覆盖率文件；
- `dist` 和 `staging` 生成物；
- `.pext`、NuGet 生成包和 IDE 用户设置；
- 日志、转储、临时文件；
- 意外复制进源码树的 `ExtensionsData`、会话数据、导出和备份。

以下内容应纳入源码：

- C#、XAML、项目和解决方案文件；
- `extension.yaml`、`PRIVACY.md`、README、CHANGELOG；
- `Localization`；
- 正式图标和 `Assets` 设计源文件；
- `docs` 与 `docs\audit` 审查证据。

## 发布前必须执行

1. 确认 `extension.yaml` 与程序集版本一致；
2. 完全退出 Playnite；
3. 执行 Release 构建和完整测试；
4. 确认 Release 目录只有 9 个预期发布文件，包含 LICENSE 且不含 PDB；
5. 将同一 Release 文件部署到 `staging\<version>` 和安装目录；
6. 使用 Playnite Toolbox 从 Release 目录打包；
7. 核对 Release、staging、安装目录逐文件 SHA-256；
8. 核对 PEXT 只包含 9 个预期文件；
9. 确认 `ExtensionsData` 文件数量、时间戳和内容未变化；
10. 更新 DEVELOPMENT、ROADMAP、IMPLEMENTATION_STATUS 和 CHANGELOG。

主看板滚轮手工检查：

- 在星期分布、趋势图和排名区域滚动，确认整页移动；
- 在 24 小时分布、星期 × 小时热力图和日历热力图区域滚动，确认纵向滚轮不会被横向容器吞掉；
- 在异常列表和会话钻取列表内部滚动，确认列表可滚时优先滚列表；
- 当内部列表到达顶部或底部时继续同方向滚动，确认滚轮自动接力给外层页面。

0.9.8 完整客户端验收步骤见 `docs\CLIENT_ACCEPTANCE_0.9.8.md`。

## 公共发布前剩余动作

- Git 仓库、MIT LICENSE、`origin/main` 和上游跟踪已建立；
- 0.9.4 基线已本地提交；当前 0.9.8 发布改动仍需在最终客户端验收后提交并推送；
- 0.9.8 已冻结版本、移除界面候选字样并清理公开 README；
- DLL/PDB 本机路径和许可证阻塞已解决：正式包无 PDB，LICENSE 已纳入 PEXT；
- 公开 README 已重写；仍需补最新中英文截图；
- 现有配置下的 0.9.4 → 0.9.8 原位升级及主要客户端交互已于 2026-07-30 验收通过；
- 2026-08-09 下钻会话封面修复已部署，需复验按日趋势周期与日历热力图下钻条目的缩略图；
- 仍需补独立空数据目录干净安装、正式中英文截图和最终 Author 确认；
- 确认 GitHub 仓库可匿名访问；当前 Toolbox 无法访问源码、图标和 installer URL；
- 创建 `v0.9.8` Git 标签和 GitHub Release，PEXT 作为 Release 附件；
- 两层 manifest 已创建；上传 PEXT 且推送 installer 后，用 Toolbox 完成 URL 联动校验；
- 向 `JosefNemec/PlayniteAddonDatabase` 提交 Add-on manifest PR；
- 完整审查见 `docs\RELEASE_READINESS_1.0.md`。

## 2026-07-28 忽略规则验证

- 已在一次性临时 Git 仓库中加载项目根 `.gitignore`；
- `bin`、`obj`、`dist`、`staging`、`.pext`、`sessions.json` 和 `ExtensionsData` 样例均被忽略；
- README、XAML、正式图标和 `docs\audit` 样例保持可跟踪；
- 临时仓库验证后已删除；此项完成后项目已另行初始化为正式 Git 仓库。
