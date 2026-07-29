# Playtime Insights 发布检查清单

更新日期：2026-07-29

## 当前候选

- 插件版本：0.9.4；
- 程序集版本：0.9.4.0；
- 目标框架：.NET Framework 4.6.2；
- Playnite SDK：6.16.x；
- Release 构建：0 警告、0 错误；
- 自动化回归：61/61；
- 中英文资源：各 271 个键；
- 发布包：`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_4.pext`。
- DLL SHA-256：`4BF5CE09C4A89B904FE67E0FF41F9A5822899F4D7863DCE68BC3D0355FC0E904`；
- PEXT SHA-256：`15BAFE3225882D430129722EC2B9B54210F317591D0A9AFB9CFF1F99247C384D`；
- PEXT 大小：191,488 字节；
- PEXT 内容：9 个预期文件。

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
4. 确认 Release 目录只有 9 个预期发布文件；
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

## 公共发布前待决定

- Git 仓库、MIT LICENSE、`origin/main` 和上游跟踪已建立；
- 当前功能改动尚未提交，必须在客户端最终验收后提交并推送；
- 0.9.4 已移除界面发布候选字样并清理公开 README；正式首发时再统一为 1.0.0 / 1.0.0.0；
- 清理 DLL/PDB 中的本机绝对路径，并将 LICENSE 纳入 PEXT；
- 公开 README 已重写；仍需补最新中英文截图；
- 创建 `v1.0.0` Git 标签和 GitHub Release，PEXT 作为 Release 附件；
- 创建并用 Toolbox 验证 Installer manifest 与 Add-on manifest；
- 向 `JosefNemec/PlayniteAddonDatabase` 提交 Add-on manifest PR；
- 完整审查见 `docs\RELEASE_READINESS_1.0.md`。

## 2026-07-28 忽略规则验证

- 已在一次性临时 Git 仓库中加载项目根 `.gitignore`；
- `bin`、`obj`、`dist`、`staging`、`.pext`、`sessions.json` 和 `ExtensionsData` 样例均被忽略；
- README、XAML、正式图标和 `docs\audit` 样例保持可跟踪；
- 临时仓库验证后已删除；此项完成后项目已另行初始化为正式 Git 仓库。
