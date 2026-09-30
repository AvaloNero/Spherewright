# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 保存run `4e8d5b0d35aa40c1a7e4c0903fc78930` ord8：81944582 /R22/healthy | 同一owned identity；resumeAvailable=true不表示已重启 |
| 最近正常保存 | **81944570**，ownedSaveState=saved，覆盖西侧源端带段 | 尚未实际重启验证 |
| durable Journal | 保存run `4e8d5b0d35aa40c1a7e4c0903fc78930` ord9：**J96 durable**，96条exact unchanged、无pending/error | 不由revision或accepted推算 |
| 外部写窗口 | accepted **3/10 OPEN**，无在途 | 当前窗口未冻结 |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 西侧覆盖与下一接口

正常保存 **81944570** 已覆盖 `5205→5229→5228→5219` 非移除式双带覆盖及西侧九带；最新观察81944582/R22/healthy，owned identity相同、peaceful、sandbox disabled、1×。J96的96条记录精确不变且durable、无pending/error；`resumeAvailable` 不等于真实重启验证。新窗口accepted **3/10 OPEN**、无在途。[西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

root 对17个选定对象（15旧、2新）的局部读回核验得到32条互返有向连接，不是全厂 census；新增带段和旧边组成局部链 `5202→5201→5200→5204→5203→5205→5229→5228→5219→…→5227`。唯一核验的静态变化是5205/5219的connections/rotation及5219–5227带段pathId 202→198。背包仅`2001 343→341`变化；5202无供料，5227尾端free。高层孤立前缀与消费者坡道未重做。

`5203→5219` / sorter item `2011`、filter6004 的只读预检仍以`BUILD_CONNECTION_INVALID/TooSkew`拒绝（16 seeds、nativeChecks=0、admittedSeeds=0、bestFacing未知，无plan/commit）。这是有效负例；成功的`5205→5219`是 item2001 的双带非移除式覆盖，不证明 sorter 接口或整链通过。仓库3051供料、抬高主干、紫糖到货、科研推进、持续吞吐及真实重启均未证明；同候选不重试，由 root 重新设计。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
