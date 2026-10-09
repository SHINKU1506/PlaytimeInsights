# Playtime Insights 1.1.1

Dashboard 的 Additional filters 新增 Playnite Category（分类）与 Platform（平台），
并改善长列表、长名称的显示与滚动。对应请求：[Issue #1](https://github.com/SHINKU1506/PlaytimeInsights/issues/1)。

## 主要变化

- 分类与平台复用既有筛选、排行与图表刷新链路，支持一个游戏关联多个分类或平台；
- 提供 English 和简体中文维度标签及“全部”选项；
- 选择框与下拉框共用 260 DIP 宽度，下拉高度最多 320 DIP；
- 启用 Recycling 虚拟化与滚动，兼容主题模板未启用逻辑滚动的情况；
- 长名称以省略号显示，Tooltip 保留完整名称；滚动遇到更长名称时不会改变下拉框宽度。

## 数据口径与升级

分类与平台读取当前 Playnite 游戏元数据。平台筛选不代表历史会话实际运行环境；
每次仍选择一个附加筛选维度及一个值，名称按完整值匹配。

可从 1.1.0 更新；会话格式、AddonId、.NET Framework 4.6.2 和最低 Playnite API 6.16.0 不变。
PEXT 仅包含插件文件，不包含用户会话、设置或备份。安装后按 Playnite 提示重启。

## 验证范围

Release 构建、完整回归及确定性制品证据见 [发布检查清单](RELEASE_CHECKLIST.md)。
中英文资源、千项列表、混合长短名称、滚动及选择绑定有自动化覆盖；
实机验收范围与未验证项见 [Client Acceptance 1.1.1](CLIENT_ACCEPTANCE_1.1.1.md)。

公开附件的 SHA-256、下载验证和清单激活结果记录在 GitHub Release 正文中。
