# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前安装执行批次仍来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`；R508双包仅离线预检通过，不是实机安装验收或最终发行批准。

## 当前 owned 状态

- 当前为健康owned P104/R29，最后正常Save为 `105244881`，Journal J102。固定20写窗口以10笔唯一accepted提前封存（opening lifetime 408，closing lifetime 418），这10笔均由该Save覆盖并经R585独立核销；累计计数不归零，未开新窗口。当前无在途或unknown，等待下一有限阶段。
- R578的Move原动作Native成功，但玩家持续漂移、没有满足稳定落地条件，因此没有实施后续五段北接；同一原动作已核销且不重放。R582的Save调用者因错误地期待返回`planetId`而停止轮询；只读检查确认同一Save终态成功，没有再次保存或重放。
- R585全厂快照为6366 built/0 prebuild；相对封存基线新增49条带材，旧5323 Native锚点旋转及slot 1接入6330，未删除对象且无非互惠边。57成员接收路径仍开放、空载；矿机5325仍network 0、服务比率不可用（`null`）、缓冲为空，尚未接电或获准作为来源。
- 石料整案累计19笔accepted、53条带材、6个跨度、3次移动、1个分拣器、1台矿机、2次手工制作和5次Save；未建风机或电塔。R556原16/1100/2400 scope在使用6笔、698请求、1329032.8576毫秒后退役，未使用的10笔、402请求和1070秒不转移。
- `sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 0。来源接电与过滤、实际开采、持续≥36000 ticks、Warper与Rod产率、双自动补给、整合保存恢复和最终同SHA双候选包验收均未通过；已验证的蓝图生命周期与Governor 2×范围保持。
- 下一步是有界原生地面预览，尚未新增预算。R585封存的是当前阶段结果，不代表后续施工、来源或持续供给已经通过。

阶段索引：[R585石料路由前缀保存与固定窗口封存](evidence/2026-10-10/stone-route-prefix-save-r585.md)、[R577高端双接缝Native核验](evidence/2026-10-10/stone-raised-dual-cover-r577.md)、[R573上坡动作核销与余段预算](evidence/2026-10-10/stone-uphill-prefix-r573.md)、[R568下坡接入与关键接口复核](evidence/2026-10-10/stone-overpass-critical-interface-r568.md)、[R546石料有限备料与保存核销](evidence/2026-10-10/stone-finite-material-kit-r546.md)、[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史细节留在各自事件页。
