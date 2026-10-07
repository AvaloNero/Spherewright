# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；完整阶段索引见[持续授权与油路事件](evidence/2026-10-07/continuing-oil-supply.md)。整案验收仍未通过。

## 当前 owned 状态

- 最新正常保存仍为 `99724071/R150/J100`；之后完成的两笔普通动作尚未由新Save覆盖。R73独立核对 `19d9202f3573437bbb70dc54f6431054:18`（SHA-256 `BCEB4F602F812E4255D90DB111976C0F0F1042FC42A472B61895CD764D7AA0EE1`）确认当前在P102/R154、external `2/10`、lifetime `262`，无在途或unknown。普通业务保持冻结，normal Save仍为预留动作。
- R73本地factory读回为181 built/0 prebuild；相对R61仅见1项pose/resources变化与7项input移除，无其他静态变化。同tick cut覆盖163个支持对象、5条完整Native路径及151个belt成员；station 44以同tick点读核对，unsupported对象未当作零库存。完整阶段记录见[持续授权与油路事件](evidence/2026-10-07/continuing-oil-supply.md)。
- R66拆除旧空miner1后，R71 Native Build完成新miner ID1并消耗 `2301×1`；19个Ti节点的5组读数为 `318/326/309/310/312`。N1为full-serve、实际work `7000 J/t`；旧stage检查把idle读值400当作work，因此 `oldStagePowerConditionPassed=false`。新miner的 `connections[]` 为空，旧7仍保留out8，Ti自动来源尚未接通。
- 安装cohort仍为 `d744b8e6eb5b898b9e9484b2209c30380e40ddda`，64 tools/1 resource；same-star factory读取和64-item sampler改动仍只经过离线验证，尚未部署或实机验证。Steam保留运行。

## Gate 2 边界

- 旧油路5949→3964继续保留。G源4450已通过6198接至6090；R47只读原件观察到11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334的实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C仅为短窗诊断，没有连续验收credit。
- 行星102的Ti来源仍未接通：R71新miner的出口 `connections[]` 为空，旧7保留out8；同tick station44读回已记录，但不据此宣称自动补给。旧R61关于矿机空resources、站内158/200和199阈值的读数只代表当时截面，根因仍未证实。Si miner17及其4节点保持原状。
- shared supply、净物料/功率分配、自动补给、连续≥36000-tick验证、保存恢复整合和同一干净SHA双候选包均未验收，整案保持 `executable=false`。R66/R71动作尚无新Save覆盖，R73核对后普通业务继续冻结；待root确定并批准新的有界范围后再推进，不预告未执行阶段。
