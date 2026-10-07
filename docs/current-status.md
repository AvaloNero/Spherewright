# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本阶段证据见[P102 Ti源端五带、回收与十写封窗](evidence/2026-10-08/ti-source-five-belts-reclaim-ten.md)。整案验收仍未通过。

## 当前 owned 状态

- 当前为P102/R27/J100，normal Save `100241992`覆盖本窗全部10笔。root R119独立十写审计 `d7c67ad29bb946749233efe69e8e7b8b:16`（SHA-256 `80EB28343E9E4557EC1A882BD0D1795F5A9CAD9514979D32D4A5C5A9DAF11F97`）确认10笔unique accepted均completed、无重放或unknown；external `10/10 FROZEN`、lifetime `280`，无在途。提交后的准确SHA须绿CI并由root明示后才重开。
- R119 fresh full factory为185 built/0 prebuild；相对181基线新增belt `182–186`、拆除旧空分拣器16。旧静态差异仅矿机1的connection与`insertTargetObjectId`改指182、旧分拣器14和15移除指向16的连接；其他旧配置无漂移，互惠连接完整。新源端方向为`1→182→184→183→186→185`，185仍是自由端，既有`61→…→110→44` Ti路径保留。
- 同tick切片覆盖167个支持库存对象、6条完整Native货物流路径和156条唯一belt；Ti物流储存与路径货物为空，9个矿脉节点仍在覆盖范围内，不据此声称耗尽。Ti矿机1为network0/无库存，只接新feed182，尚未通电。N1维持17 nodes/8 consumers/10 wind generators、full-serve、capacity `55000 J/t`。`powerServeRatio=null`仍是未知，不可当作0。
- 本窗净材料差为Fe矿石−2、磁线圈−1、电塔+1、煤净0、belt−5、分拣器+1；当前belt11、电塔1、分拣器1。P104远端factory未loaded，其缺失读数为unknown而非零，须正常返回后fresh复核。
- 现安装仍为提交`f6694ee11e17a5b32c52c8499c495cbf2a97d301`（Windows Core CI `37675299409` success）对应的同源4+224 cohort，已冷部署并核对228个哈希，MCP为64 tools/1 resource；精确primary恢复健康，恢复只计1笔accepted。它不是最终双候选包验收。

## Gate 2 边界

- 原油5949→3964持续保留。G源4450经快速分拣器6198接至6090；R47读回证明11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C只是短窗诊断，没有连续验收credit。
- P102新Ti矿机的尾带尚未接入既有Ti路径。R115仅核对新空带几何、既有Ti路径和分拣器回收条件；直接分拣器185→61仍待Native施工，之后才单独评估最后一座电塔给矿机与分拣器供电。station44读数不证明自动补给或远端派船。
- P104 remote factory未loaded，不能把缺失页映射为零库存或零需求。正常返回后仍须fresh读取Ti来源、站内库存/订单/机队及消费者链，再决定后续有限范围。
- R118被动供给是离线条件模型：2台矿机、7台分拣器、station44闲置负载和充电器合计估算19800 J/t，低于N1的55000 J/t容量；这不是实际长窗或远端运输验收。597300 J/t全负荷理论需求仍超过容量，Foundry full-load baseline为false。实际P104运输、供电稳定性及库存守恒必须用后续fresh连续证据确认。
- 下一候选为准确SHA绿CI且root明示重开后施工185→61直接分拣器，再另行fresh计划单塔供电；这不是当前施工授权。shared supply、净物料/功率分配、自动补给、连续≥36000-tick验证、保存恢复整合和同一干净SHA双候选包均未验收，整案保持 `executable=false`。
