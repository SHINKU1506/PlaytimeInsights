# Playtime Insights 发布检查清单

更新日期：2026-07-28

## 当前候选

- 插件版本：0.9.3；
- 程序集版本：0.9.3.0；
- 目标框架：.NET Framework 4.6.2；
- Playnite SDK：6.16.x；
- Release 构建：0 警告、0 错误；
- 自动化回归：58/58；
- 中英文资源：各 272 个键；
- 发布包：`dist\PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_3.pext`。
- DLL SHA-256：`FE0CF4CB4841B0DB9299E9BB73197CB66E560C06E0B38AA2D6F15D1A5EF9F9B7`；
- PEXT SHA-256：`5E211CB4BB93C33A62EFF0AC1B7E7C65E8472B67F24FBCEE0B86A4BEEE487224`；
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

## 公共发布前待决定

- 当前源码目录尚未初始化为 Git 仓库；
- 尚未提供 `LICENSE`。公开发布前需由维护者选择许可证，不能由构建流程自动推断；
- 远程仓库地址、发布页和版本标签策略尚未配置；
- 建议初始化仓库后先检查 `git status --ignored`，再创建首个提交；
- 建议标签使用 `v0.9.3`，PEXT 作为发布页附件而不是提交到源码历史。

## 2026-07-28 忽略规则验证

- 已在一次性临时 Git 仓库中加载项目根 `.gitignore`；
- `bin`、`obj`、`dist`、`staging`、`.pext`、`sessions.json` 和 `ExtensionsData` 样例均被忽略；
- README、XAML、正式图标和 `docs\audit` 样例保持可跟踪；
- 临时仓库验证后已删除，项目源码目录没有执行 `git init`。
