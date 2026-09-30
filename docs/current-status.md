# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 保存闭环后观察：81820702 /R19/healthy | 同一owned identity；restartAvailable=true不表示已重启 |
| 最近正常保存 | **81820689**，ownedSaveState=saved，覆盖西侧九带 | 尚未实际重启验证 |
| durable Journal | 保存闭环前后identity匹配；**J96 durable**，96条exact unchanged、无pending/error | 不由revision或accepted推算 |
| 外部写窗口 | accepted **1/10 OPEN**，无在途 | 上一窗口十写审计已交接关闭；当前窗口尚未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 保存状态与下一接口

正常保存 **81820689** 已覆盖西侧九个带实体 `5219–5227`；最新观察81820702/R19/healthy，保存状态restartAvailable，但真实重启未验证。Journal前后identity匹配，J96全部96条exact unchanged、durable、无pending/error。root在上一十写审计、文档和绿CI后已明确交接；新外部窗口accepted **1/10 OPEN**，无在途。[西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md)。

下一未闭合接口是 sorter `5203→5219` / filter6004：只读原生预检返回`BUILD_CONNECTION_INVALID/TooSkew`，16 seeds、nativeChecks=0、admittedSeeds=0、bestFacing未知，无plan/commit/accepted。既有两边`5204→5203→5205`和`5219→5220`仍在，但九带仍free/free、源端不连；不得放宽角度或重试同一候选，root重新设计中。现有带段保存成功不证明源端接入、紫糖到货、科研推进、持续吞吐或真实重启恢复。上一窗口十写独立审计和快照差异见[独立审计事件](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md)。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
