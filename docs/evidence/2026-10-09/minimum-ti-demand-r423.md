# 钛需求上限与供给边界（R423）

R419完成19个接口的fresh读取；R420独立核验 `57c4adbb1453475984f8b29bdd6a2f0d:1` / SHA-256 `7602F8C01F47F5D99A2F18670DAC759A22AE81CB5882C76364ED2EC865CAA2C4`。配置前，P104站1657的slot0钛矿石1004库存308、上限300；站916的slot0钛锭1106库存108、上限100。P102站44钛矿石库存200/上限200，P104站918钛锭库存100/上限100。既有home1运输船、本地10架无人机与供电也在原读取中观察到。

R421仅调整需求上限：P104站1657 slot0从300到400，站916 slot0从100到200；物品、逻辑、其他槽位与运输参数保留。阶段共3笔accepted（两次配置与普通Save）、33次请求，无失败、unknown或未决意图。writer原件 `8b51dc4178904a7782dbc6be0e5bea71:47` / SHA-256 `6C7C55417F739DF3FD4804AE534BC26C3EC608B9463D45E25AEBAD68389FCF37`。

R423独立核对3组fresh plan、intent、ACK、计数与终态，完整背包及inc/持货，各站其余设置、belt ports、旧拓扑、3次供电读取、J102与Save覆盖。审计原件 `3283e55a0632497584c193dfe07ccafc:1` / SHA-256 `E129F22119ED90B9E4FDAB336862664ADD1AD642738FF9AE791B7932A830E431`。当前普通保存tick `102745200`、revision `101`、durable J102，external `10/20`、lifetime `385`；同一健康owned world，无加载/重建、在途或unknown，写入冻结待下一有界阶段。

这些配置只恢复站点正需求，不证明新采矿、送达或持续产线通过。R397发现矿工86的resourceNode 203返回Native `INVALID_ENTITY`，仅该节点从`resourceNodeIds`移除并按自然耗尽处理，另16个源节点均有正证据；R398 whole-current资格核验通过，R395停止记录保留。R398同期的高能石墨1109=708、磁铁1102=6950、奇异物质1127=87、连通氢1120=136、钛锭1106=495；全局氢4845与连通氢不同，legacy4419排除。这些读数不构成持续窗口。

R389原定36,000-tick连续观察因sample间33 ticks缺口，在32,295 ticks后自动失败；不拼接或延长，continuous credit仍为0，`wholeSupplyPassed=false`。R391前缀的库存差异和Native计数仍只是诊断，详见[联合来源缺口与R391前缀核验](joint-source-gap-and-prefix-r391.md)。既往H支路候选拒绝与零写核销保留：R410固定朝向候选被拒，R413参数错误由R414核销，R415的两格侧线与3955碰撞，root R416核为0 commit/accepted（`0ded7040bcc44390b5a8d0296fb8e79e:1` / SHA-256 `4AA1FBFA3665A07E58670D59C03F4A5B9D4D76816E3F4821D189B5022B4FE89C`）。未重放、微移或拆旧燃料仓；H额外建设和headroom尚未批准。

当前源码基准与已安装cohort未变，Native版本 `0.10.35.29104`，DLL SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`。后续先fresh核验完整供给与终端headroom，再由root确定有限需求/连续窗口；完整29物料、连通氢、远端与本地Ti供给、warper和rod各至少1/min且连续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过。
