# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前执行安装批次仍来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508同SHA双包仅离线预检通过，不是实际Mod Manager安装、实机验收或最终发行批准。

## 当前 owned 状态

- 当前为和平、非沙盒、1×的owned P104/R22健康状态，正常主档Save tick `105130853`，Journal完整且durable 102。当前固定20窗口external 6/20、lifetime 414，opening lifetime 408；无在途或unknown，写入冻结，等待同一原阶段余段继续。四笔材料动作已由该Save覆盖，随后两笔施工动作尚未Save覆盖。此前R536窗口已独立关闭且累计未重置；历史R498 `outcome_unknown`保持原判定。
- R558的下坡施工原调用者在240秒后超时，摘要报告的预建筑与扣料字段不能据此判定动作未执行。R562只读核实同一原动作Native终态为completed，R563独立核对Native点位/实体ID、旧48个实体静态与电力及空载路径；没有重放。R564另一施工动作completed/succeeded，新增带材实体为6329、6330，实际扣除带材2。其后无人机返航观察耗尽本阶段请求余量，不是Native施工失败。
- R568复核的61对象切片显示，13条新带材（斜坡11条及接缝2条）与旧带材5316–5323组成Native path 235、444格的空载路径；方向由新段接到旧头5323，再连至5316。旧头新增的接缝是6330/slot 1→旧头输入；源端6328仅新增6329/slot 0输出。两端旋转有Native证据；其余旧48个对象静态字段、电力网络成员与供电状态保持，N3/N4均满供。R567的独立读回见三架无人机空闲且无预建筑。矿机5325仍为network 0、`serveRatio=null`且缓冲为空；未证明接电或开采。
- 原有限阶段目标为16笔accepted、1100请求、2400秒；目前2笔accepted、208请求、317254.227毫秒，余14笔、892请求、2082秒。全局累计预算为15笔accepted、17条带材及4个跨度；R557将本段跨度上限从10调整至11，整体200条带材、30笔accepted和5次保存的预算不变。下一余段仍需核验上坡、较高端Native双接缝、5段北向延伸、移动、过滤分拣器、2风机、至多2电塔及唯一末尾Save；这些尚未验证。
- 持续来源条件仍未通过：`sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。历史R498未知动作不重放；已通过的蓝图生命周期和Governor 2×范围保持。

阶段索引：[R568下坡接入与关键接口复核](evidence/2026-10-10/stone-overpass-critical-interface-r568.md)、[R546石料有限备料与保存核销](evidence/2026-10-10/stone-finite-material-kit-r546.md)、[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
