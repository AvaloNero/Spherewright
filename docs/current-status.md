# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | root独立审计核证：81734184 /R18/healthy，J96 durable | 同一owned session；不是新保存证明 |
| 最近正常保存 | **81634733**，ownedSaveState=saved；其后西侧九带未被正常保存覆盖 | 未验证西侧九带的保存/重启恢复 |
| durable Journal | J96 durable，96条与审计基线相同 | 不由revision或accepted推算，也不证明本次施工已保存 |
| 外部写窗口 | accepted **10/10 FROZEN**，94条索引；全有终态、0重放、0在途 | 十写审计已完成；须等root明确交接后才可开新窗口 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 十写封存与当前边界

独立十写审计确认西侧九段带 `5219–5227` 完成后，完整快照从 `5205` 延伸至 `5227`，比基线新增22、删除0；10186条有向边全互返。旧静态差异仅两项：`5198` 增加指向已批准 sorter `5218` 的入边；`resourceNodeIds` `[312,325]→[312]`。只读补证显示325当前不存在、312仍为煤3225，但未查明325消失原因/时间。玩家背包相对基线仅 `1301−6`、`2001−21`、`2011+5`，物品增量无变；J96的96条记录不变。[西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md)。

第十写虽然已成功并核销，但西侧九带未由新正常保存覆盖；最近保存仍为 **81634733**。最新观察81734184/R18/healthy，J96 durable且历史96条不变。外部 accepted **10/10 FROZEN**，94条索引、全有终态、0重放/在途；不得自行提交下一次游戏写入，须等 root 完成文档/CI并明确交接。功率仅是采样点正常，不代表持续实验；紫糖到货、科研推进与重启恢复仍未证明。之前的源尾、消费者接口和高层前缀历史证据保留。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
