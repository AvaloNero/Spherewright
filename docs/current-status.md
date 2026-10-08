# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。R421钛需求上限修正及R423独立核验见[钛需求门槛与供给边界](evidence/2026-10-09/minimum-ti-demand-r423.md)；R398资格与R407分拣器升级的历史细节见[联合来源缺口与R391前缀核验](evidence/2026-10-09/joint-source-gap-and-prefix-r391.md)。R323/R324长窗来源条件失败仍在，见[联合长窗原件](evidence/2026-10-08/joint-supply-long-window-r324.md)。

## 当前 owned 状态

- 当前同一健康owned-world、无加载或重建；普通Save `102745200`/revision `101`/durable J102，external `10/20`、lifetime `385`，无在途或unknown，写入冻结。
- R421把P104的1657 slot0钛矿石1004上限由300调至400，把916 slot0钛锭1106上限由100调至200。R423独立核销3笔唯一计划、意图、ACK、计数和终态，确认全背包/inc/持货、其余站点设置与belt ports/旧拓扑、3次电力读取及J102/Save覆盖。此改动恢复站点正需求，不证明新采矿、送达或持续产线。审计`3283e55a0632497584c193dfe07ccafc:1` / SHA-256 `E129F22119ED90B9E4FDAB336862664ADD1AD642738FF9AE791B7932A830E431`；writer原件`8b51dc4178904a7782dbc6be0e5bea71:47` / SHA-256 `6C7C55417F739DF3FD4804AE534BC26C3EC608B9463D45E25AEBAD68389FCF37`。
- R419的19个fresh接口由R420独立核验。配置前P104需求站1657库存308、上限300；916库存108、上限100；P102站44钛矿石200/200、P104站918钛锭100/100。既有home1运输船、本地10架无人机与电力已观察；本次只改需求上限，未验证新采矿或新交货。原件`57c4adbb1453475984f8b29bdd6a2f0d:1` / SHA-256 `7602F8C01F47F5D99A2F18670DAC759A22AE81CB5882C76364ED2EC865CAA2C4`。
- R397发现矿工86的resourceNode 203返回Native `INVALID_ENTITY`，仅该节点从`resourceNodeIds`移除并按自然耗尽处理，其余16个源节点均有正证据；R398 whole-current资格核验通过，R395停止记录保留。R398的历史cut为高能石墨1109=708、磁铁1102=6950、奇异物质1127=87、连通氢1120=136、钛锭1106=495；全局氢4845不等于连通氢，legacy4419排除。
- R389原定36,000-tick连续声明因sample间33-tick缺口，在32,295 ticks后自动失败；未拼接或延长，continuous credit仍为0。原R391前缀库存差异与计数只作诊断；source conditions与`wholeSupplyPassed`仍为false。已通过的蓝图生命周期与Governor 2×结论不变。
- H侧此前候选保留为历史：R410固定朝向拒绝、R413零yaw参数由R414核销，R415两格侧线与3955碰撞并由R416以0 commit/accepted关闭；未重放、微移或拆旧燃料仓。R417参数错误由R418核销，R419完成接口fresh读取。执行源码基准`e5d95d34297a468e11d61509d03f04a38c70aaf4`与已安装cohort `863d35546f6cb49fcdaec5b5814869d1e13af42b`未变；Native版本0.10.35.29104，DLL SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`。

## Gate 2 边界

- R423通过的是全当前站点/拓扑与保存核销；需求上限修正、R398资格和R407的名义周期提升均不构成持续来源/产量通过。原长窗失败保留，continuous credit为0、`wholeSupplyPassed=false`，整案仍为`executable=false`。
- 完整29物料、连通氢、远端新Ti与本地新炼Ti、warper和rod各至少1/min并持续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；历史已通过的蓝图生命周期与Governor 2×范围保持原结论。
- 下一步先fresh诊断完整供给与终端headroom，再由root锁定新的有限需求/连续窗口。H3345→3074与3074→3073的并行普通分拣器仍在设计中，未批准施工；既有3083已接入3073，不重复铺设，石墨5196/5581已有名义能力，不盲目升级。

历史原件见[Gate 2生产阶段](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R334铁入口升级](evidence/2026-10-08/iron-admission-upgrade-r334.md)、[R340铁矿上游升级](evidence/2026-10-08/iron-ore-feeder-r340.md)、[R353油源分拣器升级](evidence/2026-10-08/oil-source-admission-r353.md)、[R355/R356窗口封闭](evidence/2026-10-08/fixed20-window-close-r355.md)、[R365油路绕行材料资格](evidence/2026-10-08/oil-detour-material-r365.md)、[R372油路覆盖前缀](evidence/2026-10-08/oil-cover-prefix-r372.md)、[R375油路施工结构核验](evidence/2026-10-09/oil-route-completion-r375.md)、[R377油路到料](evidence/2026-10-09/oil-arrival-window-close-r377.md)、[R388 warper headroom](evidence/2026-10-09/warper-output-headroom-r388.md)及[R323/R324联合长窗](evidence/2026-10-08/joint-supply-long-window-r324.md)；当前快照不复述旧阶段流水。
