# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 保存run `b08ae5b1bf7d4cb9a724fec2baf3f870` ord8：82109952 /R27/healthy | 同一owned identity；不是保存tick |
| 最近正常保存 | **82109940**，ownedSaveState=saved，覆盖本阶段六带与sorter `5236` | `resumeAvailable` 不等于真实重启已验证 |
| durable Journal | J96 durable，96条exact unchanged、无pending/error | 不由revision或accepted推算；不证明实际物料流或科研进展 |
| 外部写窗口 | accepted **6/10 OPEN**，无在途 | 当前窗口未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 消费者接口分段与下一步

消费者侧 L1 六带 `5230→5232→5231→5233→5234→5235` 已施工；本阶段新建受电 sorter `5236`，以 item `2011` / filter `6004` 从 `5235` slot4 接入既有 `5217` slot4。选定对象读回核实该连接及原端点配置保全，但这是局部审计，不是整厂 census。库存 `2011` 为 `6→5`、`2001` 保持335，其他物品数量与增量不变。[东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。

最近正常保存 **82109940** 覆盖六带和 sorter `5236`；最新观察82109952/R27/healthy，同一owned identity。J96 durable、96条exact unchanged、无pending/error。accepted **6/10 OPEN**、无在途；未做真实重启。此前保存的源端带段仍见[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

下一硬 blocker 是尚未连接已保存的源端尾 `5227` 与消费者头 `5230` 的 raised 主干；之后再处理仓库 `3051→5202` 供料。仓库供料、raised 主干连通、紫糖到货、科研推进、持续产出及真实重启均未证明。`resumeAvailable` 只是恢复可用状态，不是重启验证。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
