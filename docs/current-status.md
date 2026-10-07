# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；完整阶段索引见[持续授权与油路事件](evidence/2026-10-07/continuing-oil-supply.md)。整案验收仍未通过。

## 当前 owned 状态

- 最新正常保存为 `99724071/R150/J100`。R61独立十写审计 `71c09951f02c4d6c8acd2465acf5d532:13`（SHA-256 `B0FB94AF464DAA835693D2407BA9841FE4243AA73C269E893BA3A8E5B9B9AD24`）核销R43、R49、R54、R59共10笔unique accepted：全部completed，0 replay/unknown/unpaired；Save覆盖10笔、Journal 100/100 exact，external `10/10 FROZEN`、lifetime `260`，无在途。普通写冻结。
- 最新本地状态在行星102。R61 fresh capture有181 built/0 prebuild，相对immutable基线无新增、移除、静态变化或非互反边；同tick cut覆盖163个cut支持对象、5条完整Native路径及151个belt成员。物流站不受MaterialInventoryCut支持，已另作点读，未将unsupported对象当作零库存。
- 当前库存：Fe0、gear0、belt1、normal sorter0、fast sorter0、pole0、thermal0、circuit938、brick144、magnetic coil1；inc0、held null。R59移动与Save后玩家已在行星102矿区附近。
- 安装cohort仍为 `d744b8e6eb5b898b9e9484b2209c30380e40ddda`，64 tools/1 resource；本阶段无二进制改动或部署。Steam保留运行。

## Gate 2 边界

- 旧油路5949→3964继续保留。G源4450已通过6198接至6090；R47只读原件观察到11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334的实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C仅为短窗诊断，没有连续验收credit。
- 行星102的Ti供应仍未建立：source miner1在N1 full-serve下读为resources空、库存空、not working；Ti节点仍非空，资源组剩余1,268,791且minerCount为0；资源可经后续有界正常接近/采集取得，不是工具阻塞，不能将miner1的空resources读数归因为矿脉耗尽。source logistics station 44另读到Ti 158/200、remoteSupply、无订单、0 vessels；配置的full-dispatch阈值为199，但尚未证实它是无补货原因。Si miner17及其4节点保持原状。相关飞行与到达原件见事件索引。
- shared supply、净物料/功率分配、自动补给、连续≥36000-tick验证、保存恢复整合和同一干净SHA双候选包均未验收，整案保持 `executable=false`。R62 prepare-only在13次只读中取得source miner1正常拆除positive prepare、0 commit，external仍为10；没有施工或accepted。待本次准确SHA的CI成功并由root明确重开后，再fresh核验实际动作与有限材料/接入范围；当前没有新的飞行或建造批准。
