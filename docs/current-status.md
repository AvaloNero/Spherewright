# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 82631628 /R43 | 同一owned identity；最近保存为82523330，不是观察tick |
| 最近正常保存 | **82523330**，ownedSaveState=saved，覆盖North、East2与East2—消费者六带连接；未覆盖新塔5313 | `resumeAvailable=true` 不等于真实重启验证 |
| durable Journal | J96精确条目 durable、无pending/error | 不由revision或accepted推算；不表示供料或持续产量 |
| 外部写窗口 | accepted **5/10 OPEN** | 电塔 action 已终态；后续分拣器与保存不在此快照截面 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## North—East2 与接口电塔施工后的开放窗口

原有消费者六带与 sorter `5236` 已保存于82109940，见[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。随后11带转角与19带East1首段也已由正常保存82280505覆盖。新 sorter `5267` 接通 `5247` slot4 至 `5257` slot4，item `2011` / filter `6004`；库存 `2011` 为 `5→4`，belt `2001` 保持305，其他数量与增量不变。[转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md)。

正常保存82280505覆盖11带转角、19带East1首段及 sorter `5267`。此后完成North双覆盖16带、East2原生23带、`5306→六带→5230`消费端连接并正常保存至82523330/J96 durable。之后新增接口电塔 `5313`，观察82631628/R43；最近正常保存仍为82523330，尚未覆盖该塔。当前accepted **5/10 OPEN**；本快照截至电塔action已终态，后续批准的2011分拣器与保存不在此记录中。详见[East2消费端双覆盖与保存](evidence/2026-10-01/purple-east2-consumer-cover-and-save.md)和[紫糖East接口电塔施工](evidence/2026-10-01/purple-east-interface-power-tower.md)。

保存覆盖不证明真实重启。`5266→5284` 的 `2011` 接口仍需实际供电读回；`2201` 到旧 pole `1499` 覆盖短0.359m，首个 `2203` 风机候选与旧 `2000` 原生碰撞且零写，保持停止不重试。此前已保存的源端带段仍见[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

仓库 `3051` 出口仍未接通；紫糖送达、科研推进、持续产量及真实重启仍未证明。下游连接与正常保存不等于上游供料或科研恢复。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖East接口电塔施工](evidence/2026-10-01/purple-east-interface-power-tower.md) · [East2消费端双覆盖与保存](evidence/2026-10-01/purple-east2-consumer-cover-and-save.md) · [紫糖 East2 原生带段](evidence/2026-10-01/purple-east2-native-span.md) · [紫糖北向原生覆盖带段](evidence/2026-10-01/purple-north-native-cover.md) · [紫糖主干十写独立审计](evidence/2026-10-01/purple-raised-window-ten-write-audit.md) · [紫糖转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md) · [紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
