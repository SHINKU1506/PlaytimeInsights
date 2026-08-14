# Playtime Insights 1.0.0 正式发布就绪审查

审查日期：2026-08-14

结论：1.0.0 的源码、版本、自动化、双 clean 构建、确定性 PEXT、隐私和用户数据保护证据已经完成；
当前候选可进入标签、GitHub Release 上传和远程 manifest 激活阶段。仍需按
`CLIENT_ACCEPTANCE_1.0.0.md` 完成 1.0.0 包原位升级及完整视觉矩阵，不能用自动化替代该项。

## 纳入 1.0.0 的稳定范围

- 架构重构 A–E：Interaction/Coordinator、RelayCommand、Dashboard 组合边界和事件对称护栏；
- Dashboard 选择性刷新与一次性 major-list 发布；
- 响应式指标卡和 Dashboard 语义文本层级；
- 单一 Dashboard View 生命周期、重挂载 Loaded 语义和视觉树稳定性；
- 共享 512 条目封面 LRU 缓存、文件戳失效和冻结 BitmapSource；
- schema 1–4、导入导出、备份恢复、软删除、诊断隐私和双语资源兼容。

## 工程证据

- 测试项目与主项目 Release 均为 0 warning / 0 error；
- 108/108 回归通过；10 万会话与 schema 4 载入均在 30 秒发布预算内；
- DLL 315,904 字节，SHA-256：
  `6ACFACDA528398A263219511B737A6C9699FE7D8D21CAD43EC4CAB86B7EF2790`；
- PEXT 147,824 字节，SHA-256：
  `EBA048A7F71943B22E2566D899E81BB99BFD5570D0F26A65439789A3E081AB34`；
- 两轮 clean 构建和确定性打包哈希一致；
- PEXT 9/9 条目与 Release 逐项一致，路径安全，包含 LICENSE，不含 PDB 或禁止依赖；
- DLL 程序集版本 1.0.0.0，敏感路径/PDB 字符串扫描 0 命中；
- 构建打包前后 7 个用户数据文件联合指纹保持
  `ABEF90B96891A66A0BD89F4EB19F5FCCF27C6F2FD52BFE120D44E50EB71229A6`。

## Manifest 与 AddonDatabase

- AddonId 在插件、PEXT、installer/addon manifest 和 AddonDatabase 条目中一致；
- installer manifest 以 1.0.0 为首包，保留 0.9.8，最低 API 维持 6.16.0；
- AddonDatabase PR #626 已合并，条目稳定指向
  `main/manifests/installer.yaml`；package-only release 不需要新 PR；
- 当前目录截图仍指向 0.9.8。若要展示 1.0.0 响应式界面，需拍摄稳定截图、更新
  `manifests/addon.yaml` 和上游 YAML 后另提元数据 PR；这不阻塞包更新功能。

## 剩余发布门禁

1. 完成 1.0.0 PEXT 从公开 0.9.8 的原位升级及主题/DPI/语言视觉矩阵；
2. 提交候选，创建并推送 `v1.0.0` 标签；
3. 创建 GitHub Release，上传 PEXT；
4. 匿名下载附件并核对大小、SHA-256、包内版本与 AddonId；
5. 对正式公网 URL 执行 Toolbox installer/addon 校验；
6. 只有附件可用后才推送含 1.0.0 installer entry 的 `main`；
7. 在 Playnite Add-on Browser 中复核 1.0.0 更新检测。

任何一步失败都停止激活 `main`；0.9.8 package 必须继续保持可下载。
