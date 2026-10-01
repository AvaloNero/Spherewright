# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 转角施工run `30dfd2b5106548cda16763ba94c50543`：82215168 /R29/healthy | 同一owned identity；不是保存tick |
| 最近正常保存 | **82109940**，ownedSaveState=saved，覆盖消费者六带与sorter `5236` | 早于新建11带转角段；真实重启未验证 |
| durable Journal | J96 durable，条目与先前一致、无pending/error | 不由revision或accepted推算；不表示新转角段已保存或实际物料流 |
| 外部写窗口 | accepted **7/10 OPEN**，无在途 | 当前窗口未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 主干转角与下一接口

消费者侧六带及 sorter `5236` 已正常保存于82109940，仍见[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。之后新建的 L1 自由端转角段顺序为 `5243→5242→5240→5239→5238→5237→5241→5244→5245→5246→5247`，共11带；库存 `2001` 为 `335→324`，其他物料数量与增量不变。root 对11个新实体及其守卫对象的原回执审计为局部核验，不是全厂 census。[紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md)。

最新观察82215168/R29/healthy，同一owned identity；J96 durable、无pending/error。正常保存仍为**82109940**，早于这11带施工，尚未由保存覆盖；accepted **7/10 OPEN**、无在途。此前已保存的源端带段仍见[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

下一接口 `5247→未来 East1 新头` 尚未原生验证；源端尾 `5227` 也尚未接到转角段起点。仓库供料、紫糖到货、持续产出和真实重启均未证明；已建前缀不重做。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
