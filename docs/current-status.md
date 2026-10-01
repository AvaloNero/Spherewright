# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | East1施工run `c7dea08185a843f1ab4141e2888973e8`：82239386 /R31/healthy | 同一owned identity；不是保存tick |
| 最近正常保存 | **82109940**，ownedSaveState=saved，覆盖消费者六带与sorter `5236` | 早于新建11带转角及19带East1首段；真实重启未验证 |
| durable Journal | J96精确条目 durable、无pending/error | 不由revision或accepted推算；不表示新带已保存或实际物料流 |
| 外部写窗口 | accepted **8/10 OPEN**，无在途 | 当前窗口未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## East1首段与下一接口

消费者侧六带及 sorter `5236` 已正常保存于82109940，见[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。之后建成的11带L1自由端转角仍未保存，见[紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md)。本次 East1 首段新建19带：`5257,5256,5253,5251,5249,5248,5250,5252,5254,5255,5258–5266`；库存 `2001` 为 `324→305`，其他物品数量及增量不变。root 独立核对19个实际点位/目标、36条互返有向连接和4个旧对象静态保全；是局部审计，不是整厂 census。[紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md)。

最新观察82239386/R31/healthy，同一owned identity；J96精确条目 durable、无pending/error。正常保存仍为**82109940**，早于转角与East1两个新段；accepted **8/10 OPEN**、无在途。此前已保存的源端带段仍见[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

下一接口 `5247→5257` 的只读预检已通过但未施工，必须作为独立有限阶段核销；源尾 `5227` 到转角段起点仍未连接。仓库供料、紫糖到货、持续产出和真实重启均未证明；已建前缀不重做。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
