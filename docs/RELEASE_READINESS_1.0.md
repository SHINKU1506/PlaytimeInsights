# Playtime Insights 1.0 正式发布就绪审查

审查日期：2026-07-29

结论：核心实现和本机安装包已经达到发布候选质量，但当前仍不适合直接创建正式公开版本。
完成下列阻塞项后，才建议冻结 `1.0.0`、创建 Git 标签和提交 Playnite Add-on Database。

## 已通过

- .NET Framework 4.6.2 Release 构建：0 警告、0 错误；
- 自动化回归：61/61；
- 5,000 游戏、10 万会话和十年范围性能预算通过；
- schema 1–4 到当前 schema 4 的无损升级回归通过；
- 插件只引用 `Playnite.SDK 6.16.0.0`、WPF 和 .NET Framework 系统程序集，
  没有引用 Playnite 非 SDK 程序集；
- `extension.yaml`、插件类和程序集 GUID 一致；
- Toolbox 可成功生成 PEXT；
- Release、staging 和安装目录的发布文件逐项 SHA-256 一致；
- PEXT 当前只包含 9 个预期条目；
- 部署前后 `ExtensionsData` 内容指纹一致；
- MIT `LICENSE` 已存在于源码仓库；
- `main` 已配置跟踪 `origin/main`，生成目录与 PEXT 没有进入 Git 跟踪。

## 正式发布阻塞项

### P0：必须完成

1. **提交并推送当前实现**
   - 当前工作区仍有本轮及此前功能改动未提交；
   - `HEAD` 与 `origin/main` 仍停留在 `c6469bd`，远程源码不包含会话页重构和滚轮修复；
   - `git diff --check` 已通过，可在客户端最终验收后提交。

2. **冻结正式版本号**
   - 0.9.4 已完成界面身份清理：两个页面不再显示版本、发布候选或阶段功能说明；
   - README 已按当前实现重写，清单和程序集已统一为 `0.9.4` / `0.9.4.0`；
   - 正式首发时仍需统一升级为 `1.0.0` / `1.0.0.0`。

3. **修正正式包的许可证与调试路径**
   - 当前 PEXT 没有包含 MIT `LICENSE`；
   - 当前 DLL 的 CodeView 调试目录嵌入
     `C:\Users\chan\AppData\Roaming\Playnite\Development\PlaytimeInsights\...PlaytimeInsights.pdb`；
   - 正式 Release 应至少把 `LICENSE` 纳入包，并通过 Release 关闭调试符号或设置可复现的
     PDB/PathMap，确保 DLL 和 PDB 不包含本机用户名或绝对源码路径；
   - 重新打包后更新文件数量、哈希和发布清单。

4. **完成最终客户端验收**
   - 尚需实测最新会话管理页的高级菜单、斑马纹、封面、对齐与 Tag；
   - 尚需实测主看板三个横向图表和两个内部列表的双向滚轮边界接力；
   - 正式版本号变更后需用现有 0.9.4 数据执行一次原位升级，确认会话、设置和备份不变；
   - 建议补一次空数据目录的干净安装、卸载/重装和英文界面启动检查。

5. **创建公开 GitHub Release**
   - 当前远程没有 Git 标签；
   - 创建 `v1.0.0` 标签和 GitHub Release；
   - 上传最终 PEXT，发布页记录 SHA-256、最低 Playnite API 版本、安装方法、已知限制和变更摘要；
   - PEXT 作为 Release 附件，不提交到源码历史。

6. **提供 Playnite Add-on Database 两层清单**
   - 项目当前没有 Add-on manifest 和 Installer manifest；
   - Add-on manifest 至少需要：
     `AddonId`、`Type: Generic`、`Name`、`Author`、`ShortDescription`、
     `InstallerManifestUrl` 和 `SourceUrl`；
   - Installer manifest 至少需要：
     `AddonId` 以及包含 `Version`、`PackageUrl`、`RequiredApiVersion`、
     `ReleaseDate` 和 `Changelog` 的 `Packages`；
   - 本插件编译引用为 `Playnite.SDK 6.16.0.0`，建议首包声明
     `RequiredApiVersion: 6.16.0`；
   - PackageUrl 应指向 GitHub Release 中最终 PEXT；
   - 使用 `Toolbox.exe verify installer ...` 和 `Toolbox.exe verify addon ...` 校验，
     再向 `JosefNemec/PlayniteAddonDatabase` 的 `addons` 目录提交 PR。

### P1：强烈建议

1. **公开 README（已完成基础重写）**
   - 0.9.4 已补齐功能、数据口径、兼容性、安装升级、使用入口、隐私、已知限制、构建、
     问题反馈和许可证；
   - 已删除旧版本号以及聚合柱形图、按日柱形等过时描述；
   - 正式发布前可再补英文长版或保持当前英文摘要，并加入最终截图。

2. **补拍正式版截图**
   - 现有 6 张截图均为 0.9.2 UI 审查证据；
   - 缺少 0.9.4/1.0 当前趋势 Crosshair、最新会话表格、高级菜单和最终英文界面；
   - Add-on Database 截图虽非强制字段，但正式发布应至少提供主看板和会话页各一张当前截图，
     并生成较小的 Thumbnail URL。

3. **统一作者身份**
   - `extension.yaml` 与程序集显示 `chan`，LICENSE 使用 `Chen Xiaoyang`，GitHub 用户为
     `SHINKU1506`；
   - 三者可以不同，但正式发布前应决定面向用户显示的 Author；
   - 0.9.4 已在 `extension.yaml` 增加源码、变更日志和问题反馈 `Links`；
   - 正式发布前仍需决定面向用户显示的 Author。

4. **改善公开构建可复现性**
   - 两个 csproj 默认使用本机 `D:\software\Playnite`；
   - 当前可通过 `PlayniteInstallDir` 覆盖，0.9.4 README 已提供覆盖参数的构建和测试命令；
   - 建议增加可选 CI，至少自动执行 Release build、61 项测试、Toolbox 之外的
     包内容检查与敏感路径扫描。

## 推荐发布顺序

1. 用户完成最新客户端验收；
2. 修正 PDB/PathMap、将 LICENSE 加入发布包；
3. 补正式截图、确认 Author；
4. 升级所有版本与界面文字到 1.0.0；
5. Release 构建、61 项回归、干净安装与 0.9.4 原位升级；
6. 重新打包并核对 PEXT 内容、哈希、用户数据；
7. 提交并推送源码；
8. 创建 `v1.0.0` 和 GitHub Release，上传 PEXT；
9. 写入并验证 Installer manifest；
10. 写入并验证 Add-on manifest，向 Playnite Add-on Database 提交 PR。

## 当前正式包证据

- 当前 0.9.4 候选 DLL SHA-256：
  `4BF5CE09C4A89B904FE67E0FF41F9A5822899F4D7863DCE68BC3D0355FC0E904`；
- 当前 0.9.4 PEXT SHA-256：
  `15BAFE3225882D430129722EC2B9B54210F317591D0A9AFB9CFF1F99247C384D`；
- 当前 0.9.4 PEXT 大小：191,488 字节；
- 完成 PDB/PathMap、LICENSE 入包和 1.0.0 冻结后哈希仍必须重新生成。
