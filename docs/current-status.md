# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新观察 | run `af819fc5861742c9ada6a731a708ce17` ord6：tick80627903 / R43；accepted仍8/10 | 主干预检原生拒绝，0对象创建 |
| 保存 | lastSaved tick80526460；J96 durable仅见于较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3 | 保存入口在descriptor查找失败；没有save prepare/commit |
| 外部写窗口 | accepted **8/10**，无在途 | 原生拒绝不新增accepted；此前成功前缀不重做 |
| Git / 发布 | 文档提交前基线 `c4bc7b0` 的Windows Core CI已绿 | 本批提交按准确SHA另验CI；不代表安装或发布 |

run `af819fc5861742c9ada6a731a708ce17` ord5对planned point3返回`BUILD_LOCATION_INVALID`；该点与既有belt1932重叠，0对象创建。cached factory已核1932为input1931/output1934旧带；不能同址重试或移除端点绑定。此前成功前缀`5198→5197→5199→84`与`5202→5201→5200`保持但未保存。保存尝试`398b62002d954c71a1ed33c8752f340d`在descriptor查找阶段exit1，root核对无业务回执、无save prepare/commit/accepted；当前进程数0、无在途。完整边界见[紫糖主干碰撞与保存不可用事件](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md)。

## 下一消费者接口与边界

最近一次主干预检在点3碰撞后被原生拒绝；不得同址重试或通过删除路径端点规避。保存未发生：`lastSaved=80526460`，J96仅来自较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3；旧保护票据最低tick虽在有效期内但落后进度，不能回滚或自动重放。当前没有活跃DSPGAME进程。**进行中的下一阶段仅限Steam正常启动至菜单并只读核恢复状态；没有加载批准。**确认后再由root设计绕行。5198主干、仓库出料、Lab84实际收料及重启恢复都未证明。

本次未创建主干对象，保存入口未到业务prepare/commit；未测试加载、重启恢复或持续科研。

证据入口：[紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
