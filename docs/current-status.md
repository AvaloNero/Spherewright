# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 82726431 /R49 | 同一owned identity；最近保存为82726418，不是观察tick |
| 最近正常保存 | **82726418**，ownedSaveState=saved，覆盖North、East2、消费者连接、电塔5313及分拣器5314/5315 | `resumeAvailable=true` 不等于真实重启验证 |
| durable Journal | J96精确条目 durable、无pending/error | 不由revision或accepted推算；不表示供料或持续产量 |
| 外部写窗口 | accepted **9/10 OPEN**，无在途 | 最近分拣器与正常保存均有终态；真实重启未验证 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 源仓出口与主干保存后的开放窗口

原有消费者六带与 sorter `5236` 已保存于82109940，见[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。随后11带转角与19带East1首段也已由正常保存82280505覆盖。新 sorter `5267` 接通 `5247` slot4 至 `5257` slot4，item `2011` / filter `6004`；库存 `2011` 为 `5→4`，belt `2001` 保持305，其他数量与增量不变。[转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md)。

正常保存82280505覆盖早期转角与East1首段；之后North、East2及消费者连接保存至82523330，再保存电塔 `5313` 与 East1—East2 分拣器 `5314` 至82658561。其后建成源仓出口分拣器 `5315`：`3051` slot1 → `5315` input1 → `5315` output0 → `5202` slot4，item `2011` / filter `6004`；读回 network3/ratio1，玩家 `2011` 为3→2、`2001`保持260。正常保存 **82726418 / J96 durable** 覆盖 `5315` 与本次连接；最新只读观察 **82726431 / R49**。当前accepted **9/10 OPEN**，无在途。详见[紫糖源仓出口分拣器与正常保存](evidence/2026-10-01/purple-source-warehouse-outlet-and-save.md)。

本次 sorter network3/ratio1 只证明设备采样点状态，不等于持续供电或物料流。后续只读快照见 `5315` 持有 `6004×1` 且工作中，`5202×1`、`5197×2`，Lab84为37824 `research_matrix_points`；这些读数不构成连续输送或科研进展归因。`3051` 此次读数1776，先前为1824，差值来源未唯一确定。`resumeAvailable=true` 也不等于真实重启验证。

`3051→5202` 出口接口已建成并保存；当前下一待核为 `4743` 上游有界样本及持续生产/消耗归因，采样尚在进行，本文不预记结果。紫糖持续送达、研究进展归因、稳态产量和真实重启仍未证明。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖源仓出口分拣器与正常保存](evidence/2026-10-01/purple-source-warehouse-outlet-and-save.md) · [East1—East2分拣器连接与正常保存](evidence/2026-10-01/purple-east1-east2-powered-interface-and-save.md) · [紫糖East接口电塔施工](evidence/2026-10-01/purple-east-interface-power-tower.md) · [East2消费端双覆盖与保存](evidence/2026-10-01/purple-east2-consumer-cover-and-save.md) · [紫糖 East2 原生带段](evidence/2026-10-01/purple-east2-native-span.md) · [紫糖北向原生覆盖带段](evidence/2026-10-01/purple-north-native-cover.md) · [紫糖主干十写独立审计](evidence/2026-10-01/purple-raised-window-ten-write-audit.md) · [紫糖转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md) · [紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
