# 原档恢复与卡住 Move 的第十写审计

日期：2026-09-29（Asia/Singapore）。沿用[29104 实机恢复](dsp-29104-owned-primary-live-recovery.md)的已安装 Plugin/MCP；没有新建档、施工、升级或安装切换。本文件审计当前外部 accepted 窗口，不改变游戏的 session revision。

## 十笔已接受动作

1. #1–5：五次正常取料，受保护完成证明 `action-3f861…/0057`，SHA-256 `D76328D9624BCD9732BF86D5D87F96F783DF07BB116CD5F7CD95D9ACFF2C9AA4`；五个唯一 action 和 terminal tick 在原证明中。
2. #6–7：两次普通手搓，证明 `action-a506…/0032`，SHA-256 `4D81404AEAF2567D005A2825A1E36722D97EF1F5EFDEEBC148E258EBB4FED3D1`；两个唯一 action 均已完成。
3. #8：玩家要求暂停时的唯一 normal save `4339e5a4-33b0-4b0a-b5b1-2f20f95da451` 终态成功，主档 `77676444 / R103 / J91`；受保护证明 `action-4e805…/0011`，SHA-256 `226229176EED138734B7C8852FFE95DCB6638D638CCC74C278732AE9760C227A`。
4. #9：精确原 owned 主档恢复 `bd88bb62-7558-484b-96c8-df6a4f03543a` 终态成功，正常保存 `77676475 / J91`；同 action 内的保存不另计 accepted。受保护原结果 SHA-256 `C042CCFAD698EC9A45D1DADA58AF770EE6C1B910D49E83CCD1C374EC1023CDD7`。
5. #10：普通 Move `a956e26b-e599-4e8d-b6f2-dda301f79dcd` 终态 `action_failed/position_stalled`，`181` 停滞 tick、剩余 `16.133 m`，`doNotRetrySameTarget=true`，见[试跑负例](coal-sorter-approach-stall-pilot.md)。失败仍计 accepted；没有重复提交、quarantine 或 outcome unknown。

前八笔沿用受保护原回执及其哈希，不把本次只读复查说成逐个重新运行。主会话独立重读 #9/#10 的终态和 #1–8 的完成证明字段；当前窗口是 `10`，尚未在游戏中重置或执行下一笔 commit。

## 同档、玩家与整厂

新鲜会话仍在星球 `104`，DSP `0.10.35.29104`，owned、和平、非沙盒、1×、write health `healthy`、无 blockers/flight checkpoint，protected resume 可用；revision `2`，最后主档仍 `77676475`。玩家 `Walk`、速度 `0`、核心能量充足，手搓/手持/待施工为空，三架无人机均 idle。与 #8 暂停记录相比，完整背包 `itemId/count/inc` 逐项相同；91 条 Journal 事件逐项相同，durableThrough `91`、无 pending/error，版本迁移链仍为 `28529→29088→29104`。本次 Move 的约 `3.9 m` 位置变化还未保存。

受保护补采的 `52` 页建成实体属于同一 tick `77979961` 快照，`5170` 个唯一 ID `1…5170`；另一独立读取确认 `0` 预建筑，完整 `10078` 条有向连接的目标和互返都有效。与前一轮封存的 tick `77515149` / `5170` / `10078` / `0` 基线逐对象比较：**5169 个对象的静态配置及连接完全相同**。矿机 `2440` 是唯一例外，其 `resourceNodeIds` 从 `[161,162,163,164,165,167,168,171,172]` 变为 `[162,163,164,165,167,168,171,172]`，其余已比较字段保持；其输出物仍为铜矿 `1002`，接线、network `3` 和供电比 `1` 不变，后续两个目标字段另行核对也未变。原生 `inspect_resource_node(vein,161)` 返回 `INVALID_ENTITY`，相邻 `162` 仍是被一台矿机覆盖的铜矿。该证据证明矿点 `161` 现已不在 factory vein pool 中，**与正常采掘耗尽相容，但不证明其消失的精确原因或时刻**。没有新增 miner 节点、工厂对象、连接或玩家物品。目标矿机 `5162` 与分拣器 `5170` 均满额供电，后者仍过滤 `1006` 且保留 `5163/5167` 两端；两张电网的 consumer ratio 均为 `1`。

旧审计证明 `action-918c67…/0060` SHA-256 `4566C8D48B9F3BE6C87526B7ED936E57A017B4BE483626824863BC734DFF109B`。本次受保护读取与比较证明 `action-ff876…/0059` SHA-256 `45A736A601ABF2AD7E6A897444F713439C7733C5AA54932D4D74CF2CAA6FFCE9`：机械 `Assert-AuditConfig` 因矿点列表变化报告 `passed=false / differenceCount=1`，不是整厂无差异通过。随后对矿点 `161/162` 的原生只读回执分别为 `action-4b8044…/0002` SHA-256 `E6E5ABDB8F4F73B9B52292D53DB3358CC7A117ABE40B2807BC93E62B2CFC90F6` 和 `0003` SHA-256 `254B83C0342416F9E2192ECE644A9C03E832FD53906813ABB5DBB53A8FE04B3C`。整厂快照 tick、独立 prebuild tick 与收尾 session tick `77981031` 不混作同一瞬间。

先前直接比较的 `configurationStateHash/endpointStateHash` 跨恢复都变化，并不构成额外静态配置异常：当前[规范编码](../../../src/Spherewright.Bridge.Core/Safety/CanonicalStateHash.cs)两种哈希都包含新 `SessionId`，配置哈希还包含动态 Buffers。故本次改以同口径静态字段逐对象比较。初次只读采集未保留完整逐对象原件，导致必要的受保护补采；今后规定的十写审计先启用既有受保护记录器，避免重扫 52 页。补采及矿点核查均为只读，未增加 accepted。

当前审计可核销为**无不明游戏写入、无新增/失联建成实体或连接、无背包异常正增量及无时间线分叉；唯一静态差异为已定位的矿点消失**。这不证明矿点具体耗尽机制、附近卡住障碍、煤/石墨输送或持续生产，也不等于新的保存。仍先冻结下一游戏 commit；本审计文档推送、对应绿 CI 与主会话明确封窗后，才允许新的 fresh 短目标预检/提交。
