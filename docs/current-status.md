# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 目标/上次owned-world-001 / planet104；DSP 0.10.35.29104 | 当前菜单未加载，不代表重启恢复或发行验证 |
| 最新只读状态 | run `9ad0821ccfce4bc597b55a676578ca90` ord1：BridgeConnected，gameLoaded=false，version29104 / R0 / healthy / restartResumeAvailable | session/tick/localPlanet为空；DTO无`menuReady`字段，不能据此判断原生载入就绪 |
| 恢复候选 | 只读run `6a6264c35dbf46e9bdf62277af6a13f5` ord1：固定别名`fixedAutoSave0` tick80731193，覆盖已知进度下限80627903 | 身份/版本/和平非沙盒匹配；仍未加载，DLL默认路径未由native `GameSave.SavePath`复核 |
| 保存与Journal | lastSaved tick80526460；J96 durable仅见于较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3 | 不代表当前候选已恢复或本次状态已持久化 |
| 外部写窗口 | accepted **8/10**，无在途 | 原生拒绝不新增accepted；此前成功前缀不重做 |
| Git / 发布 | 文档变更待提交 | 不代表安装或发布 |

只读候选比对中，旧primary tick80526460低于已知进度下限80627903；LastExit读取返回`InvalidDataException`（native save length/header version unsupported），不能据此判为身份错误或崩溃。固定`fixedAutoSave0`候选为同owned身份、同版本、和平且非沙盒，tick80731193；其路径仅来自当前DLL默认映射，native `GameSave.SavePath`尚未通过prepare重绑。root正在修复固定候选的有效票据恢复路径；候选尚未加载，不扩展到任意路径或其他候选。详情见[只读恢复检查与错误字段事件](evidence/2026-09-30/owned-recovery-readonly-and-error-fields.md)。

## 恢复边界与下一步

早前主干施工前缀`5198→5197→5199→84`与`5202→5201→5200`尚未由正常保存覆盖或恢复读回核实；碰撞点不得重试或删去路径端点。旧票据下限80526460落后已知进度，不用于回滚。保留已接受动作不重放；菜单级DTO不是native恢复证明。当前root只补固定`fixedAutoSave0`的有效票据路径，**本候选尚未加载**；恢复后仍须核身份、Journal连续性及原生状态。仓库出料、Lab84实际收料和持续科研也未证明。

本轮只做离线调用方错误元数据与fixture测试：action-client 87项、stage 26项（storage 28 / material 41检查），私有入口smoke通过；均为0游戏调用，smoke为0游戏写入。错误元数据不包含响应body/token、不自动重试，也不改变accepted语义。

证据入口：[只读恢复检查与错误字段事件](evidence/2026-09-30/owned-recovery-readonly-and-error-fields.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
