# Playtime Insights

Playtime Insights 是一个面向 Playnite Desktop 的本地游玩时间分析插件。

它在 Playnite 客户端内提供原生 WPF 仪表盘和会话管理页面，记录精确游戏会话，按日、周、月、
年或自定义范围分析游玩时间，并提供趋势、热力图、时间分布和游戏排名。插件不包含遥测，
不上传游戏库或会话数据，也不依赖远程网页。

当前版本：`0.9.4`

## 主要功能

- 监听游戏开始和停止事件，记录精确到秒的本地会话；
- 运行中每分钟保存恢复检查点，支持 Playnite 异常退出后的会话恢复；
- 今天、本周、本月、本年和自定义日期范围；
- 自动或手动选择日、周、月、年聚合粒度；
- 区间时长、会话数、活跃天数、平均会话、最长会话和连续游玩指标；
- 自适应折线/面积趋势图，支持稀疏日期标签、Crosshair、Tooltip 和会话下钻；
- 日历热力图、星期分布、24 小时分布和星期 × 小时热力图；
- 按时长、会话次数、活跃天数、平均会话或最长会话进行区间游戏排名；
- 独立显示 Playnite 所有游戏累计时长与累计排名；
- 排名显示本地游戏封面、前三名勋章和时长占比背景；
- 按库来源、Playnite 来源、开发者、发行商、类型、标签、分类和安装状态筛选；
- 原生会话管理：搜索、筛选、补录、编辑、软删除和恢复；
- JSON/CSV 导入、导出与预览，兼容部分 GameActivity 会话文件；
- 完整备份、备份恢复和存储索引重建，危险操作前自动创建回滚备份；
- 中文和英文界面、键盘导航、访问键及屏幕阅读器名称；
- 大型会话列表分页与 Recycling 虚拟化。

## 数据口径

| 内容 | 数据来源 | 说明 |
|---|---|---|
| 累计总时长、累计排名 | Playnite `Game.Playtime` | 包含安装插件前已有的累计值 |
| 日期范围、趋势、热力图、时间分布 | 插件精确会话 | 只包含插件记录或用户导入的会话 |
| 会话次数、平均/最长会话、连续游玩 | 插件精确会话 | 不会把未知历史累计时长伪造成历史会话 |

Playnite 的累计时长以分钟为主要展示单位，插件会话内部保存秒数，因此极短会话或四舍五入边界上
可能出现约一分钟的展示差异。这属于统计口径差异，不代表会话丢失。

## 系统与兼容性

- Playnite Desktop；
- Windows；
- .NET Framework 4.6.2；
- Playnite SDK / API 6.16.0 或更高兼容版本；
- 当前不提供 Playnite Fullscreen 专用页面。

插件界面使用 Playnite 公共主题资源，并已在默认主题和 Seaside 深色主题下进行兼容检查。

## 安装与升级

1. 从 [GitHub Releases](https://github.com/SHINKU1506/PlaytimeInsights/releases) 下载 `.pext`；
2. 在 Playnite 中打开安装包并按提示完成安装；
3. 安装或升级插件后重启 Playnite；
4. 从 Desktop 侧边栏打开“Playtime Insights”和“Playtime Insights · 会话”。

升级时保持相同插件 ID，现有会话、设置和备份会继续保存在插件专属数据目录。建议在重大升级前
先从会话页的“高级选项”创建一次完整备份。

## 基本使用

### 分析页

- 选择时间范围、聚合粒度、排名依据和元数据筛选；
- 将鼠标悬停在趋势或热力图上查看精确数值；
- 点击趋势周期或热力格查看对应会话；
- 点击星期分布可筛选下方 24 小时分布，再次点击恢复全部星期。

### 会话页

- 左侧主按钮用于导入及导出当前筛选结果；
- “高级选项”包含软删除/恢复、完整备份、备份恢复、重建索引和诊断报告；
- 导入会先生成预览并检查无效项和重复项；
- 删除采用软删除，勾选“包含已删除”后可以恢复。

## 数据、隐私与诊断

会话数据默认保存在：

```text
%AppData%\Playnite\ExtensionsData\
  7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd\
```

- 所有统计、导入和诊断都在本机完成；
- 插件没有遥测、账户系统或网络上传；
- 只有在用户主动导出、备份或保存诊断报告时，才会写入用户选择的路径；
- 诊断报告不包含游戏名称、会话时间、用户路径、游戏 ID 或会话 ID；
- 完整隐私说明见 [PRIVACY.md](PRIVACY.md)。

## 已知限制

- 插件无法从 Playnite 累计时长还原安装前的逐次历史会话；
- 异常退出恢复精度受一分钟检查点间隔限制；
- 会话存储使用本地 JSON，文件体积会随会话数量增长；
- Fullscreen 模式尚无专用统计界面；
- 当前发布流程仍在进入 Playnite Add-on Database 前的准备阶段。

## 从源码构建

需要安装 .NET Framework 4.6.2 Developer Pack，并提供 Playnite 安装目录：

```powershell
dotnet build PlaytimeInsights.sln -c Release `
  -p:PlayniteInstallDir="<Playnite 安装目录>"
```

运行回归测试：

```powershell
dotnet run --project Tests\PlaytimeInsights.Tests.csproj -c Release `
  -p:PlayniteInstallDir="<Playnite 安装目录>"
```

使用 Playnite 自带 Toolbox 打包：

```powershell
& "<Playnite 安装目录>\Toolbox.exe" pack `
  .\bin\Release\net462 `
  .\dist
```

## 项目文档

- [版本路线](docs/ROADMAP.md)
- [开发与技术实现](docs/DEVELOPMENT.md)
- [当前实现状态](docs/IMPLEMENTATION_STATUS.md)
- [发布检查清单](docs/RELEASE_CHECKLIST.md)
- [1.0 正式发布就绪审查](docs/RELEASE_READINESS_1.0.md)
- [变更日志](CHANGELOG.md)

## 问题反馈

请通过 [GitHub Issues](https://github.com/SHINKU1506/PlaytimeInsights/issues) 提交问题。建议附上：

- Playnite 版本、语言和主题；
- Playtime Insights 版本；
- 复现步骤和截图；
- 必要时附上由插件生成的不含会话明细的诊断报告。

请不要公开上传 `sessions.json`、完整备份或包含私人游戏库信息的日志。

## License

Playtime Insights 使用 [MIT License](LICENSE)。
