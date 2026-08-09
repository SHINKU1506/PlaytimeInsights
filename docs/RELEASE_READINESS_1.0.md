# Playtime Insights 1.0 正式发布就绪审查

审查日期：2026-07-29

结论：0.9.8 本地工程和安装包已达到正式发布候选质量；版本冻结、LICENSE 入包、PDB/本机
路径清理和两层 manifest 源文件已经完成。当前仍需客户端最终验收及 GitHub/上游外部发布动作。

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
- PEXT 包含 MIT LICENSE，不包含 PDB，DLL 敏感路径扫描通过；
- 部署前后 `ExtensionsData` 内容指纹一致；
- MIT `LICENSE` 已存在于源码仓库；
- `main` 已配置跟踪 `origin/main`，生成目录与 PEXT 没有进入 Git 跟踪。

## 正式发布阻塞项

### P0：必须完成

1. **提交并推送 0.9.8**
   - 0.9.4 基线已提交为 `b141707`，当前分支领先 `origin/main` 一个提交；
   - 当前 0.9.8 版本、发布配置、manifest 和文档改动仍需在客户端最终验收后提交；
   - 推送后远程源码才会包含最新 UI、滚轮修复和可供 Add-on manifest 引用的 installer。

2. **冻结正式版本号（已完成）**
   - 两个页面不显示版本、发布候选或阶段功能说明；
   - README、清单、程序集和安装清单已统一为 `0.9.8` / `0.9.8.0`。

3. **修正正式包的许可证与调试路径（已完成）**
   - Release 不生成 PDB，并用 `PathMap` 固定编译路径；
   - DLL 敏感路径扫描未发现本机用户名、开发目录或 PDB 路径；
   - MIT `LICENSE` 已入包；PEXT 仍为 9 个预期文件。

4. **完成最终客户端验收**
   - 2026-07-30 用户确认现有配置客户端检查未发现问题；
   - 0.9.4 → 0.9.8 原位升级、主要分析页、会话页和星期 Tooltip 修复视为通过；
   - 仍需补一次独立空数据目录的干净安装、卸载/重装和正式中英文截图。

5. **创建公开 GitHub Release**
   - 当前远程没有 Git 标签，且 Toolbox 无法匿名访问当前 GitHub 源码 URL；
   - 正式发布前需确认仓库为公开状态；
   - 创建 `v0.9.8` 标签和 GitHub Release；
   - 上传最终 PEXT，发布页记录 SHA-256、最低 Playnite API 版本、安装方法、已知限制和变更摘要；
   - PEXT 作为 Release 附件，不提交到源码历史。

6. **验证并提交 Playnite Add-on Database 两层清单**
   - `manifests\addon.yaml` 和 `manifests\installer.yaml` 已创建；
   - Add-on manifest 至少需要：
     `AddonId`、`Type: Generic`、`Name`、`Author`、`ShortDescription`、
     `InstallerManifestUrl` 和 `SourceUrl`；
   - Installer manifest 至少需要：
     `AddonId` 以及包含 `Version`、`PackageUrl`、`RequiredApiVersion`、
     `ReleaseDate` 和 `Changelog` 的 `Packages`；
   - 本插件编译引用为 `Playnite.SDK 6.16.0.0`，建议首包声明
     `RequiredApiVersion: 6.16.0`；
   - PackageUrl 已指向 `v0.9.8` Release 中的最终 PEXT 文件名；
   - 当前 Toolbox 对 installer 报告 Release 包 URL 不可达，对 add-on 报告源码、图标和
     installer URL 均不可达；
   - 上传 PEXT 并推送 installer 后，使用 `Toolbox.exe verify installer ...` 和
     `Toolbox.exe verify addon ...` 完成联动校验，再向数据库 `addons\generic` 提交 PR。

### P1：强烈建议

1. **公开 README（已完成基础重写）**
   - 0.9.8 已补齐功能、数据口径、兼容性、安装升级、使用入口、隐私、已知限制、构建、
     问题反馈和许可证；
   - 已删除旧版本号以及聚合柱形图、按日柱形等过时描述；
   - 正式发布前可再补英文长版或保持当前英文摘要，并加入最终截图。

2. **补拍正式版截图**
   - 现有 6 张截图均为 0.9.2 UI 审查证据；
   - 缺少 0.9.8 当前趋势 Crosshair、最新会话表格、高级菜单和最终英文界面；
   - Add-on Database 截图虽非强制字段，但正式发布应至少提供主看板和会话页各一张当前截图，
     并生成较小的 Thumbnail URL。

3. **统一作者身份**
   - `extension.yaml` 与程序集显示 `chan`，LICENSE 使用 `Chen Xiaoyang`，GitHub 用户为
     `SHINKU1506`；
   - 三者可以不同，但正式发布前应决定面向用户显示的 Author；
   - 0.9.8 的 `extension.yaml` 已包含源码、变更日志和问题反馈 `Links`；
   - 正式发布前仍需决定面向用户显示的 Author。

4. **改善公开构建可复现性**
   - 两个 csproj 默认使用本机 `D:\software\Playnite`；
   - 当前可通过 `PlayniteInstallDir` 覆盖，0.9.8 README 已提供覆盖参数的构建和测试命令；
   - 建议增加可选 CI，至少自动执行 Release build、61 项测试、Toolbox 之外的
     包内容检查与敏感路径扫描。

## 推荐发布顺序

1. 用户完成最新客户端验收；
2. 补正式截图、确认 Author；
3. 完成 0.9.8 客户端升级、干净安装和英文界面验收；
4. 提交并推送源码；
5. 创建 `v0.9.8` 和 GitHub Release，上传最终 PEXT；
6. 用 Toolbox 完成 Installer/Add-on manifest 的远程 URL 联动校验；
7. 向 Playnite Add-on Database 的 `addons\generic` 提交 Add-on manifest PR。

## 当前正式包证据

- 当前 0.9.8 候选 DLL SHA-256：
  `0A214AA04597B0AD7B853835FAAAF9156E236B9DA422A29474731B7322634787`；
- 当前 0.9.8 PEXT SHA-256：
  `62539010D9DC2F255181D08C416CB71F2962CAA58814E70AD050411470FC7201`；
- 当前 0.9.8 PEXT 大小：189,577 字节；
- PEXT 含 LICENSE、不含 PDB，Release/staging/安装目录哈希一致。
