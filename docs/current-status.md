# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最近 root 核实 session / closing | 82996677 /R53 | 同一owned identity；与稍后的单资源读数分开 |
| 最近单资源观察 | tick 83000303，node196 remaining46310/miner0 | 不是新session或完整工厂快照 |
| 最近 root 核实正常保存 | **82996662**，保存已核回收动作 | `resumeAvailable=true` 不等于真实重启验证 |
| 最近 root 核实 Journal | J96完整前缀 durable、无pending/error | 由本阶段保存后读回核实；不表示持续产量 |
| 外部写窗口 | accepted **2/10 OPEN**，无unknown/在途 | 十写交接后新窗；R/tick/J未重置 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 石料源短缺与空矿机回收阶段

此前紫糖供料十写审计与完整路线已闭环，见[紫糖供料十写审计与持续产出诊断](evidence/2026-10-01/purple-supply-ten-write-audit-and-output-diagnostic.md)。十写交接后新窗从accepted **0/10 OPEN** 开始；现已核两项accepted成功终态，当前为 **2/10 OPEN**，R/tick/J未重置。回收空实体 `86` 后 `2301` 为0→1，其余已核库存与位置保持；正常保存82996662，closing82996677/R53，J96完整前缀durable。

先前原生生产结果显示1124的1402实际产耗为0，native上游路径追至factory实体861；raw-stone1005可用5、硫酸recipe24每周期需求8。861不是资源节点，5/8也不是1124直接石料需求。回收前复用完整快照证实 `86→87→…→128→859→95→…→861` 拓扑路径；带局部1005×1属于128，859 held为空。回收后没有新的完整工厂 census。两项组15矿机预检已停止，ordinal23与101 overlap、`3c6c3d78b01d4576884a27d61fc0b560` ordinal3与4920 overlap，均 `BUILD_LOCATION_INVALID`、无plan/commit。Native搜索记录的是末尾拒绝，不证明其他候选都被这两实体阻挡；组15位置需重新设计。回收、保存与细节见[石料源短缺诊断与空矿机回收](evidence/2026-10-01/stone-source-exhaustion-and-recovery.md)。未证明持续采石、硫酸或紫糖生产及真实重启。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[石料源短缺诊断与空矿机回收](evidence/2026-10-01/stone-source-exhaustion-and-recovery.md) · [紫糖供料十写审计与持续产出诊断](evidence/2026-10-01/purple-supply-ten-write-audit-and-output-diagnostic.md) · [紫糖源仓出口分拣器与正常保存](evidence/2026-10-01/purple-source-warehouse-outlet-and-save.md) · [East1—East2分拣器连接与正常保存](evidence/2026-10-01/purple-east1-east2-powered-interface-and-save.md) · [紫糖East接口电塔施工](evidence/2026-10-01/purple-east-interface-power-tower.md) · [East2消费端双覆盖与保存](evidence/2026-10-01/purple-east2-consumer-cover-and-save.md) · [紫糖 East2 原生带段](evidence/2026-10-01/purple-east2-native-span.md) · [紫糖北向原生覆盖带段](evidence/2026-10-01/purple-north-native-cover.md) · [紫糖主干十写独立审计](evidence/2026-10-01/purple-raised-window-ten-write-audit.md) · [紫糖转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md) · [紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
