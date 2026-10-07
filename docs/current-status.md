# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本阶段证据见[102 Ti源端维护与R97十写审计](evidence/2026-10-08/p102-empty-source-binding-maintenance.md)。整案验收仍未通过。

## 当前 owned 状态

- 最新正常保存为 `100030212/R10/J100`；root R97独立十写审计 `4d8ac5064a224b22bca5b07018a6569d:14`（SHA-256 `28625CA94354418BA31A539FE97F12FAB1BEBFF823007885AC22169DBF391DB7`）确认8个原始run中的10笔unique accepted均completed、无replay/unknown/in-flight，Save覆盖全部10笔。当前P102，external `10/10 FROZEN`、lifetime `270`，无intent/unknown；十写门仍冻结，须本次准确SHA绿CI及root明示后才重开。
- R97 fresh full factory为181 built/0 prebuild；相对R77只增加未供电Ti矿机1，没有旧静态漂移或非互惠边。163个支持对象的同tick cut覆盖5条完整Native路径和151个belt；station44为同tick物料cut外层锚点，N1保持17 nodes/9 consumers/10 generators、full-serve、capacity `55000 J/t`。
- Ti矿机1由R88实际Native build完成，覆盖9个Ti节点，现仍未接线、未上电、没有端口；`powerServeRatio=null`表示未知，不得当作0。R90/R97完整读回确认该矿机network0、空状态/无连接；SOURCE路径的精确Native接缝尚未实机施工。P104远端factory尚未loaded，其物料读数是unknown而非零；须正常返回后fresh复核。
- 本窗全量背包/inc/held与J100一致。相关末态库存为Fe矿石2、belt16、Ti矿石50/inc0、Ti矿机2301为0，magcoil1保留供未来单电塔使用。
- commit `f6694ee11e17a5b32c52c8499c495cbf2a97d301`（Windows Core CI `37675299409` success）所含4+224同源批次已实际冷部署并核对228个哈希；MCP为64 tools/1 resource。精确primary恢复健康，恢复只计1笔accepted；见事件页。该部署/离线cohort验证不等于最终双候选包验收。

## Gate 2 边界

- 原油5949→3964持续保留。G源4450经快速分拣器6198接至6090；R47读回证明11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C只是短窗诊断，没有连续验收credit。
- P102新Ti矿机有9个Ti节点，但未接空带主路或供电；真实Ti自动运输与末端source-feed join尚未验证。station44同tick库存/机队读数不解释发船阈值，也不证明自动补给。旧Si miner17及其4节点保留。
- P104 remote factory未loaded，不能把缺失页映射为零库存或零需求。正常返回后仍须fresh读取Ti来源、站内库存/订单/机队及消费者链，再决定后续有限范围。
- 下一候选顺序为补齐pole材料与有界煤补能、fresh Native验收并施工空Ti接缝，最后另行fresh计划单塔供电；这不是当前施工授权。提交CI绿后仍须root显式重开。
- shared supply、净物料/功率分配、自动补给、连续≥36000-tick验证、保存恢复整合和同一干净SHA双候选包均未验收，整案保持 `executable=false`。
