# Playtime Insights 1.0 正式发布就绪审查

审查日期：2026-08-11

结论：0.9.8 本地工程和安装包已达到正式发布候选质量；版本冻结、LICENSE 入包、PDB/本机
路径清理、当前客户端验收、Portable 安装/卸载、正式截图和两层 manifest 源文件已经完成。
当前仅剩最终提交及 GitHub/上游外部发布动作。

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

1. **提交并推送最终 0.9.8 修订**
   - 0.9.8 基线 `e018001` 已推送并与 `origin/main` 同步；
   - 2026-08-10 可复现构建修复、manifest 日期、文档和当前 README 改写尚未提交；
   - 客户端复验后应创建最终提交并推送。

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
   - 2026-08-11 当前客户端复验完成，星期选中态和下钻封面未发现问题；
   - 中文/英文分析页和中文设置页三张正式截图已归档；会话管理页截图不附加；
   - 独立 Playnite Portable 环境的安装和卸载验收已完成。

5. **创建公开 GitHub Release**
   - GitHub 仓库已公开；匿名 HTTP 对仓库、raw installer、图标和 Add-on manifest 均返回 200；
   - 当前远程没有 `v0.9.8` Git 标签，Release 页面和 PEXT 附件仍返回 404；
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
   - 两层 YAML 已通过一次性本机 HTTP 完整联动校验；
   - 正式 Toolbox 校验目前仍因 PEXT 附件 URL 404 失败；新截图 URL 也需等待最终提交推送；
   - 上传 PEXT 并推送最终清单/截图后，使用 `Toolbox.exe verify installer ...` 和
     `Toolbox.exe verify addon ...` 完成联动校验，再向数据库 `addons\generic` 提交 PR。

### P1：强烈建议

1. **公开 README（已完成基础重写）**
   - 0.9.8 已补齐功能、数据口径、兼容性、安装升级、使用入口、隐私、已知限制、构建、
     问题反馈和许可证；
   - 已删除旧版本号以及聚合柱形图、按日柱形等过时描述；
   - 正式发布前可再补英文长版，或保持当前英文摘要；最终截图已加入。

2. **补拍正式版截图（已完成基础集合）**
   - 0.9.8 已归档中文分析页、英文分析页和中文插件设置页；会话管理页截图不附加；
   - README 和 Add-on manifest 已引用稳定路径；
   - 当前 Thumbnail 与原图使用同一 URL，后续可选生成较小缩略图以降低列表加载流量。

3. **统一作者身份（已完成）**
   - 用户确认公开 Author 使用 GitHub 用户名 `SHINKU1506`；
   - extension、程序集、LICENSE、README 和 Add-on manifest 已统一；
   - `extension.yaml` 已包含源码、变更日志和问题反馈 `Links`。

4. **改善公开构建可复现性**
   - 两个 csproj 默认使用本机 `D:\software\Playnite`；
   - 当前可通过 `PlayniteInstallDir` 覆盖，0.9.8 README 已提供覆盖参数的构建和测试命令；
   - 建议增加可选 CI，至少自动执行 Release build、61 项测试、Toolbox 之外的
     包内容检查与敏感路径扫描。

## 推荐发布顺序

1. 提交并推送最终源码、截图和发布元数据；
2. 创建 `v0.9.8` 和 GitHub Release，上传最终 PEXT；
3. 用 Toolbox 完成 Installer/Add-on manifest 的远程 URL 联动校验；
4. 向 Playnite Add-on Database 的 `addons\generic` 提交 Add-on manifest PR。

## 当前正式包证据

- 当前 0.9.8 候选 DLL SHA-256：
  `9BEFE2370DA5BA3E21F5E5E55862B59497EC6DA8CE6840BD268942F900DB5AB4`；
- 当前 0.9.8 DLL 大小：282,112 字节；
- 当前 0.9.8 PEXT SHA-256：
  `09ACBD2CE1B62346AC658C4FE3C2539FA456394C7CC6773EAE46BBAA3BAB4B82`；
- 当前 0.9.8 PEXT 大小：134,031 字节；
- PEXT 含 LICENSE、不含 PDB，Release/staging/安装目录哈希一致。
