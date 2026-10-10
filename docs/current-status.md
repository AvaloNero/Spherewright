# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。本页记录当前运行状态；来源与持续供给门仍未通过。

## 当前运行与窗口

- 当前 owned 世界为P104/R54，最后正常主档Save `106055923`、连续durable Journal 102；固定20窗口external `18/20`、整案lifetime `452`。整案累计53次accepted；R753阶段内18笔唯一accepted均已由正常Save覆盖，无在途或unknown，窗口冻结。完整工厂为6492 built/0 prebuild。执行 cohort仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`，Native版本29104，无代码或部署变化。
- R729已证明到达reserve超过100 MJ；dryland仍未证明。Iron节点40曾在玩家建造范围内；Stone6仍为6，来源未归因、获取/供给信用为0，不要求或假设用户曾手动操作。

## 来源与供需观察

- R682独立核验覆盖29项库存、20个当前有限矿点（均有Native正剩余）、钛源169与本地钛254、16个燃料发电实体、2座实验室及3座已加载工厂的供电；观察到0个预建筑。该只读覆盖确认了当前状态，不是连续产量或全案供料通过。
- R699封存的完整工厂只读核验为6475 built/0 prebuild；相对R672无新增、移除、静态变化或非互惠边，P104本地网络成员与容量保持且满供。计数更正：R729为6475 built/0 prebuild，R743才是6476 built/0 prebuild。R704只fresh读了1213、1216与1496等选定实体：Iron页含91个正剩余矿点，root R705确认group 3内有9个未占用正剩余点距磁铁炉1216约18.428–26.487 m、合计221723，其中node 40剩余28076。R743正常建成新矿机6476，消耗矿机2301×1；group 3五个正剩余节点为29、31、34、38、40。矿机已由现有N3供电，Native serve ratio为1，无需新增电杆；旧对象静态保持。R704对1213、1216、1496的旧读回仍是选定实体观察，不据此断言周边矿点耗尽或唯一供料原因。
- R753完成矿机6476到原铁路的15带接入，并以2011/filter1001普通分拣器6492接入旧带1220；完整工厂6492 built/0 prebuild，旧路线保留，N3满供且未新增电杆。该阶段在R746接受的带路动作只提交一次；调用方180秒到限后，R750只读看到3个预建筑与3架工作无人机，R751查询并核销同一原动作/key后完成剩余分拣器和Save，没有重放或扩预算。物理连接已完成并保存，但`sourceAdmitted=false`、持续供料和产量仍未验证。
- R681两个相互重叠的600-tick观测窗都记录到磁铁新产出为0，未求和或拼接。R683只读路径核对确认所选磁铁炉1216铁矿输入与磁铁输出均为0，其旧上游路径1213→1218–1222→1223→1216为空；当前仍工作的铁源1496经6270→6269→6267→6268→6271→6272→6273→6274→1507→1508→1509连至铁块炉1500。部分中间实体来自既有R672快照，并非此次全路径fresh覆盖；当前缺口是1216缺铁与共享供给问题。
- 最近一次Warp、Rod及跨组库存细节来自R682/R647各自own-tick读回，不是R697当前库存快照，不能据此判断当前产率或连续窗口结果。

## 验收边界

- `wholeSupplyPassed=false`，continuous credit为0。Warper与Rod各至少1/min、连续36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复和最终同SHA双候选包验收仍未通过；历史有限送达、库存或额定容量不能替代这些门。
- R690地形预览中的多数位置位于水下；Walk速度为0不能代替dryland/上岸证据。旧orthogonal 4 m候选族的两次拒绝仍退役；R695旧采煤候选有一次Harvest prepare `STALE_STATE`，不能称为第二次拒绝。后续R717原Harvest在caller等待超时后由R720核为成功；R722又正常加燃料并保存。R729阶段已满足≥100 MJ到达reserve，但不证明dryland。石矿来源仍未归因、信用为0，不把用户手动操作作为阻塞。
- R706离线设计的接带接口已由R753按原生施工完成：矿机6476经15条新带及分拣器6492/filter1001接入旧带1220，并保存覆盖；R736→R737的自由Native `grid/2001`侧段及普通分拣器正例仍只是此前资格。当前`sourceConnected=true`但`sourceAdmitted=false`，尚未证明实际到料、持续产出或完整路线供给。下一步针对现有来源与下游消费者做fresh供料核验；不把建成或连接当作供给通过。
- R732接口参数错误经核为0 accepted；R734的1222重叠候选已关闭。固定36,000-tick验收门不变，`wholeSupplyPassed=false`且continuous credit为0。
- R508双包检查仍只是离线预检，不是实际Mod Manager安装或最终发行验收。

阶段索引：[铁矿来源接入与保存](evidence/2026-10-10/iron-source-connection-and-save-r753.md)、[Iron40矿机与已资格侧接缝](evidence/2026-10-10/iron40-miner-and-qualified-seam-r743.md)、[近铁源有限到达与保存核销](evidence/2026-10-10/near-iron-source-arrival-r729.md)、[煤矿接近与库存变化观察](evidence/2026-10-10/coal-fuel-approach-state-change-r697.md)、[完整来源只读核验与铁源差异](evidence/2026-10-10/full-source-readiness-r682.md)、[漂移状态与来源边界](evidence/2026-10-10/warehouse-return-recovery-r665.md)、[来源书挡与准备清单](evidence/2026-10-09/full-source-bookends-r441.md)。
