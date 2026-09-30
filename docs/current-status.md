# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 施工run `c4d0ed3fc97440f281d8f20831ff28a9` ord39：82022047 /R24/healthy | 同一owned identity；不是保存tick |
| 最近正常保存 | **81944570**，ownedSaveState=saved，早于本阶段东侧六带施工 | 东侧新带段未保存；未实际重启验证 |
| durable Journal | 观察J96 durable、无pending/error | 不由revision或accepted推算，也不证明本次施工已保存 |
| 外部写窗口 | accepted **4/10 OPEN**，无在途 | 当前窗口未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 消费者接口最小带段与下一步

消费者侧最小六带段 `5230→5232→5231→5233→5234→5235` 已在 L1→L1 原生施工成功（`2001×6`），但尚未连接既有 `5217`。8个选定对象（6新、2保全）及10条成对互返的有向连接读回已核，不是全厂 census；库存仅 `2001 341→335` 变化。[东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md)。

最近正常保存仍为 **81944570**，早于本阶段施工；最新观察82022047/R24/healthy，J96 durable、无pending/error，但东侧六带未保存、真实重启未验证。外部 accepted **4/10 OPEN**、无在途。此前已保存的西侧源端双带覆盖及九带保持原记录；该部分曾由17个选定对象局部读回核验，不是整厂 census。[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

下一待核接口为 `5235→5217` / filter `6004` sorter 只读预检；结果仍待确认，本状态不提前宣称通过。此前 `5203→5219` / item `2011` 的 `TooSkew` 是另一条接口的有效负例；不能把它和本阶段 belt `2001` 混为一谈。仓库供料、raised 主干、完整消费者连接、物料送达、科研推进、持续产出和真实重启均未证明。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
