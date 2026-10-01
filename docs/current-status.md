# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 82812633 /R50 | 同一owned identity；最近保存为82810176，不是观察tick |
| 最近正常保存 | **82810176**，ownedSaveState=saved，覆盖已审计紫糖供料路线 | `resumeAvailable=true` 不等于真实重启验证 |
| durable Journal | J96精确条目 durable、无pending/error | 不由revision或accepted推算；不表示供料或持续产量 |
| 外部写窗口 | accepted **10/10 FROZEN**，无在途/未核销 | 十写独立审计已完成；需本提交绿CI及root明确交接后才可开新窗口 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 紫糖供料十写审计与产出诊断

十写独立审计已完成：对比基线5,267 built与完整快照5,315 built，新增48实体、零删除、10,366条互返有向边；完整 `3051→5315→5202→…→5199→Lab84` 定向路径已核。保存 **82810176 / J96 durable**，closing **82812633 / R50**，同一 owned identity 健康、和平、非沙盒、1×。十个 unique accepted 动作均成功终态、无在途或未核销结果；accepted **10/10 FROZEN**，本提交绿CI且root明确交接前不得开窗/重置。完整差异、96条Journal和材料边界见[紫糖供料十写审计与持续产出诊断](evidence/2026-10-01/purple-supply-ten-write-audit-and-output-diagnostic.md)。

同链三个彼此间隔的600-tick只读窗中，星球104原生统计的6004 production/consumption为0/1（约0/6每分钟）；4743自身读回紫糖产出0、`isWorking=false`。配方55为 `1303×2+1402×1→6004×1`，且4743原生1402输入buffer为0，诊断已确认缺料；下一项是核查1402缺料的上游原因，再有界验证补给。科研点和源仓库存读数均不单独归因持续物流或研究进展，窗口不可拼成36,000连续ticks。真实重启、持续供料、科研增量和稳态产量仍未证明。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖供料十写审计与持续产出诊断](evidence/2026-10-01/purple-supply-ten-write-audit-and-output-diagnostic.md) · [紫糖源仓出口分拣器与正常保存](evidence/2026-10-01/purple-source-warehouse-outlet-and-save.md) · [East1—East2分拣器连接与正常保存](evidence/2026-10-01/purple-east1-east2-powered-interface-and-save.md) · [紫糖East接口电塔施工](evidence/2026-10-01/purple-east-interface-power-tower.md) · [East2消费端双覆盖与保存](evidence/2026-10-01/purple-east2-consumer-cover-and-save.md) · [紫糖 East2 原生带段](evidence/2026-10-01/purple-east2-native-span.md) · [紫糖北向原生覆盖带段](evidence/2026-10-01/purple-north-native-cover.md) · [紫糖主干十写独立审计](evidence/2026-10-01/purple-raised-window-ten-write-audit.md) · [紫糖转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md) · [紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
