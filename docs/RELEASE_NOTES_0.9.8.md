# Playtime Insights 0.9.8

Playtime Insights 在 Playnite Desktop 内提供原生、本地优先的游玩时长分析和会话管理。

作者：[SHINKU1506](https://github.com/SHINKU1506)

## 主要内容

- 按今天、本周、本月、本年或自定义范围统计精确会话；
- 提供自适应趋势面积图、Crosshair、日历热力图、星期与 24 小时分布及星期 × 小时热力图；
- 游戏排名支持封面、前三名勋章、时长占比背景和多维元数据筛选；
- 趋势周期和日历热力图下钻会话显示本地游戏封面缩略图；
- 会话管理支持分页、搜索、编辑、软删除/恢复、JSON/CSV 导入导出、备份和诊断；
- 运行中检查点与异常关闭恢复；
- 中文、英文和原生 Playnite 主题资源；
- 全部分析在本机执行，不包含遥测或网络上传。
- 提供中文/英文分析页与插件设置页正式截图。
- 已通过独立 Playnite Portable 环境的安装和卸载验收。

## 安装

下载
`PlaytimeInsights_7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd_0_9_8.pext`，
在 Playnite 中打开并按提示安装，然后重启 Playnite。

最低要求：Playnite API 6.16.0、Windows、Playnite Desktop。

## 升级说明

插件 ID 和 schema 保持兼容。升级前可在“会话”页的“高级选项”中创建完整备份。
插件不会把运行数据存入安装目录，安装或更新 PEXT 不应覆盖 `ExtensionsData`。

## 已知限制

- 无法从 Playnite 累计时长反推出安装插件前的逐次历史会话；
- 异常退出恢复精度受一分钟检查点间隔限制；
- 会话存储为本地 JSON，文件大小会随会话数量增长；
- 暂无 Playnite Fullscreen 专用页面。

## 完整性

- PEXT SHA-256：
`09ACBD2CE1B62346AC658C4FE3C2539FA456394C7CC6773EAE46BBAA3BAB4B82`
- PEXT 大小：134,031 字节
- DLL SHA-256：
`9BEFE2370DA5BA3E21F5E5E55862B59497EC6DA8CE6840BD268942F900DB5AB4`
- 包含 MIT LICENSE，不包含调试符号或本机源码路径。
