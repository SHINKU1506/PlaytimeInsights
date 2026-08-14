# Playtime Insights 新版本预发布流程

更新日期：2026-08-14

本文是版本无关的发布入口。版本专属证据记录在 `RELEASE_CHECKLIST.md`、
`CLIENT_ACCEPTANCE_<version>.md` 和 `RELEASE_NOTES_<version>.md`。任何步骤没有证据时必须标记
为待完成，不能以计划替代结果。

## 1. 发布边界与变量

发布前先固定以下值：

```powershell
$Version = '1.0.0'
$AssemblyVersion = '1.0.0.0'
$AddonId = 'PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd'
$PackageName = "${AddonId}_1_0_0.pext"
$Tag = "v$Version"
```

版本必须是可由 .NET `Version` 解析的纯数字格式。`AddonId` 不得改变；
`RequiredApiVersion` 只在依赖审计证明需要时提高。

## 2. 版本一致性

以下文件必须一致：

- `extension.yaml`：`Version: 1.0.0`；
- `Properties/AssemblyInfo.cs`：`AssemblyVersion` 与 `AssemblyFileVersion` 为 `1.0.0.0`；
- `README.md` 当前版本；
- `CHANGELOG.md` 和版本 Release Notes；
- `manifests/installer.yaml` 顶部 package；
- `Tests/Program.cs` 发布元数据回归。

installer manifest 应保留历史 package。若新版本提高最低 API，旧 package 可继续服务旧版 Playnite。

## 3. 自动化门禁

```powershell
dotnet restore .\Tests\PlaytimeInsights.Tests.csproj
dotnet build .\Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore --disable-build-servers
& .\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe
dotnet build .\PlaytimeInsights.csproj -c Release --no-restore --disable-build-servers
git diff --check
git status --short
```

要求：全部回归通过；两个 Release 构建均为 0 warning / 0 error；只出现已授权源码/文档改动；
`perf_test.ps1`、`dist`、`staging`、用户会话与备份不得进入提交。

## 4. 客户端门禁

完全退出 Playnite 后部署同一组九个 Release 文件。保存部署前后 `ExtensionsData` 的文件数、长度、
时间戳与 SHA-256 联合指纹，并执行版本专属客户端清单：

- 从当前公开版本原位升级；
- 简体中文与 English；
- 默认深色、Seaside、浅色和高对比度；
- 100%、125%、150%、200% DPI；
- Dashboard 连续缩放、指标卡 1–4 列、筛选/下钻/滚轮；
- 侧边栏重复切换、滚动与焦点保留、封面缓存失效；
- 会话导入导出、备份恢复、软删除和诊断报告；
- Playnite 重启后的数据与设置兼容。

未执行项必须留在 `CLIENT_ACCEPTANCE_<version>.md`，不得写成通过。

## 5. 确定性打包

```powershell
.\scripts\Pack-Deterministic.ps1 `
  -SourceDirectory .\bin\Release\net462 `
  -OutputDirectory .\dist `
  -ToolboxPath D:\software\Playnite\Toolbox.exe
```

连续执行两轮并要求：

- Release 目录与 PEXT 都严格包含九个预期文件；
- 包含 LICENSE、PRIVACY 和两个本地化 XAML；
- 不含 PDB、Playnite SDK DLL、绝对路径、父级路径或用户数据；
- 两轮 DLL SHA-256 和 PEXT SHA-256 分别一致；
- PEXT 内 `extension.yaml` 的 AddonId 与版本正确。

## 6. GitHub Release 与安全激活

`PlayniteAddonDatabase` 已指向本仓库 `main/manifests/installer.yaml`。推送含新 package 的 `main`
会立即向 Add-on Browser 宣告更新，因此必须先让附件可下载。

推荐顺序：

1. 提交发布候选，但暂不推进远端 `main`；
2. 在该提交创建 `v1.0.0` 标签，并仅推送标签：

   ```powershell
   git tag -a v1.0.0 -m "Playtime Insights 1.0.0"
   git push origin v1.0.0
   ```

3. 基于该标签创建 GitHub Release，上传精确名称的 PEXT；
4. 匿名请求 PackageUrl，要求 **PEXT URL returns HTTP 200**；
5. 校验下载文件大小、SHA-256 和包内版本；
6. 运行 Toolbox：

   ```powershell
   D:\software\Playnite\Toolbox.exe verify installer .\manifests\installer.yaml
   D:\software\Playnite\Toolbox.exe verify addon .\manifests\addon.yaml
   ```

7. 只有以上步骤通过后才推送发布提交到远端 `main`，以激活自动更新；
8. 再次从公开 raw URL 验证两层 manifest 和 Playnite 内更新检测。

若附件、manifest 或升级验证失败，停止推进 `main`；修正或删除未激活的标签/Release，旧 0.9.8
package 继续保持可用。

## 7. AddonDatabase 判定

| 变化 | 是否修改 AddonDatabase |
|---|---|
| Package-only release：同一 AddonId、同一 installer URL，仅增加 package | 否 |
| 名称、作者、类型、AddonId | 是 |
| SourceUrl、InstallerManifestUrl、协议 URL | 是 |
| 简介、描述、标签、图标、截图 | 是 |
| 仅更新本仓库 CHANGELOG 或 Release Notes | 否 |

需要修改时，只改
`addons/generic/SHINKU1506_PlaytimeInsights.yaml`，保持与本仓库 `manifests/addon.yaml` 一致，
运行 Toolbox `verify addon` 后提交上游 PR。Package-only release 不应为了版本号重复提交目录 PR。

## 8. 发布后核验

- `main`、标签与 Release 指向同一发布提交；
- PackageUrl、installer/addon raw URL、图标、截图、CHANGELOG、PRIVACY 均匿名 HTTP 200；
- Playnite Add-on Browser 展示 1.0.0，并能从 0.9.8 更新；
- Git 工作区无被误提交的构建物或用户数据；
- `RELEASE_CHECKLIST.md` 记录最终 DLL/PEXT 大小、SHA-256、测试数、客户端验收和远程 URL 证据。
