# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新观察 | run `b72b179587f54856ba235c0362ed840e` ord31：tick80572803 / R41；同一owned planet104、healthy | 新分拣器附件尚未保存，不代表重启恢复 |
| 保存 | lastSaved tick80526460；J96 durable仅见于较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3 | 本run未fresh读取Journal；新附件未保存 |
| 外部写窗口 | accepted **7/10**，无在途或未决 | 新附件配置已有唯一成功终态；不得重放 |
| Git / 发布 | 文档提交前基线 `766861e` 的Windows Core CI已绿 | 本批提交按准确SHA另验CI；不代表安装或发布 |

run `b72b179587f54856ba235c0362ed840e` 建成分拣器5199（item2011、filter6004、pick5197、insert84），配置读回网络3 ratio1。slot4↔slot1和slot0↔slot2新增四条有向互返边，旧边及静态字段保持；背包2011为2→1，其余15种未变。action `50860492-6692-447b-86ca-f186eb78678c` 在tick80572781成功终态；fresh R41在80572803，lastSaved仍80526460。完整索引及读回边界见[科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md)。此前短带施工和caller读回契约见[科研消费者短带事件](evidence/2026-09-30/science-consumer-stub.md)。

## 下一消费者接口与边界

run `b72b179587f54856ba235c0362ed840e` 已完成近端5197→Lab84 slot2分拣器的原生附件，四条新有向互返边读回成功；这只关闭消费者接口，不证明物料流入。ord30玩家Walk/speed0、3架无人机中2 idle、1 returning、0 pending；该returning状态不改变已完成动作。此前J96 durable只见于只读run `062cd8fce1024bbf805f67a0381da62a` ord3，本run没有fresh Journal。下一唯一阻塞为源3051出口与独立空belt，并验证向5198的完整中间输送；尚未证明紫糖到达、完整流路或研究吞吐。Lab84紫糖科研点最后fresh读数仍止于run `2e501713…` 的0，本次未重读。

本次未保存新分拣器、未测试重启恢复或持续科研；单次network3 ratio=1不能替代持续供电/负载验证。

证据入口：[科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [科研消费者短带事件](evidence/2026-09-30/science-consumer-stub.md) · [紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md) · [Roadmap](../ROADMAP.md)。
