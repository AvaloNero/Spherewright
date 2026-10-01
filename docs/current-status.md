# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | 当前owned-world-001 /104，DSP 0.10.35.29104 | 已从精确固定AutoSave0恢复，和平/非沙盒/1×；不是0.4发行验收 |
| 最新观察 / revision | 审计closing `11ad8a939a564de4a229776846ae106b`：82348777 /R34/healthy | 同一owned identity；不是保存tick |
| 最近正常保存 | **82280505**，ownedSaveState=saved，覆盖11带转角、19带East1首段及sorter `5267` | `resumeAvailable` 不等于真实重启验证 |
| durable Journal | J96精确条目 durable、无pending/error | 不由revision或accepted推算；不表示供料或持续产量 |
| 外部写窗口 | accepted **10/10 FROZEN**，无在途 | 十写独立审计已通过；仍须文档/提交/CI绿及 root 明确交接，不得自行报OPEN |
| 代码 / 安装 | `6bf35b7`已push，CI36743257284 green；同批228文件已冷部署，64tools/1resource | 本地候选包通过；未tag/release/Thunderstore发布 |

用户明确授权的候选80731193已通过有效票据`reauthorize_fixed_autosave0` /v4原生prepare、唯一commit、同action终态及正常save/J连续闭环；不改到期、不换槽位、不回档、不重复问。第一次prepare仅因启动preload未完成零accepted，等待后fresh续试成功。root核原回执，见[有效票据恢复事件](evidence/2026-10-01/active-fixed-autosave-recovery.md)。

## 已保存接口与十写审计后的冻结窗口

原有消费者六带与 sorter `5236` 已保存于82109940，见[消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md)。随后11带转角与19带East1首段也已由正常保存82280505覆盖。新 sorter `5267` 接通 `5247` slot4 至 `5257` slot4，item `2011` / filter `6004`；库存 `2011` 为 `5→4`，belt `2001` 保持305，其他数量与增量不变。[转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md)。

正常保存82280505覆盖11带转角、19带East1首段及 sorter `5267`；最后单快照为82295726，closing观察82348777/R34/healthy，同一owned identity。J96 durable、无pending/error；`resumeAvailable` 不等于真实重启验证。十写独立审计已通过：10个原accepted均为唯一成功且有同action终态，123次原始poll；基线与快照新增40实体、删除0，10266条有向边均互返，材料仅`2001`减少38至305、`2011`减少2至4，其他数量/inc exact。此前比较器的`false`判定仍保留；补充原生前后态对照独立核销`5205/5219`朝向差异，不能因此通用忽略rotation。只读capture guard的`-File`数组参数折叠是本地调用边界错误，不是游戏故障；53页已保留并完成有限补读。

外部写窗口仍 **10/10 FROZEN**、无在途。审计通过不自动开新窗口；待本次文档提交、对应CI green及root明确交接后再另行决定。此前已保存的源端带段仍见[西侧源端带段覆盖与保存](evidence/2026-10-01/purple-source-west-cover-and-save.md)。

下一未闭合的物流边界为源尾 `5227→转角起点5243` 和East1尾 `5266→消费者头5230`；仓库 `3051` 供料、紫糖送达、科研推进、持续产量及真实重启均未证明。继续施工须等待本次文档/CI门及 root 明确交接。

调用方错误元数据修复的离线fixture：action-client87、stage26（storage28/material41），私有smoke均0游戏调用/写入；有效票据模式Core120/MCP13、旧恢复66、包面37通过。离线结果与上表实机闭环分开；错误元数据不含响应body/token，不自动重试或改变accepted语义。

证据入口：[紫糖主干十写独立审计](evidence/2026-10-01/purple-raised-window-ten-write-audit.md) · [紫糖转角—East1接口与正常保存](evidence/2026-10-01/purple-corner-east-interface-and-save.md) · [紫糖 East1 首段原生带段](evidence/2026-10-01/purple-east1-native-span.md) · [紫糖主干原生转角带段](evidence/2026-10-01/purple-raised-corner-native-span.md) · [消费者分拣器接口与正常保存](evidence/2026-10-01/purple-consumer-interface-sorter-and-save.md) · [东侧消费者接口最小带段](evidence/2026-10-01/purple-east-consumer-interface-span.md) · [西侧源端带段覆盖与正常保存](evidence/2026-10-01/purple-source-west-cover-and-save.md) · [西侧九带正常保存与源端接口边界](evidence/2026-10-01/purple-west-ramp-normal-save.md) · [西侧九带施工与十写独立审计](evidence/2026-10-01/purple-west-ramp-ten-write-audit.md) · [紫糖分拣器备料与正常保存](evidence/2026-10-01/purple-sorter-material-kit-and-save.md) · [消费者下坡段、分拣器与正常保存](evidence/2026-10-01/purple-consumer-down-ramp-and-save.md) · [紫糖主干碰撞与保存入口未达业务层](evidence/2026-09-30/purple-trunk-collision-and-save-unavailable.md) · [紫糖仓库出口短带](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [Roadmap](../ROADMAP.md)。
