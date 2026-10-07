# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本阶段证据见[102空矿机源接续维护](evidence/2026-10-08/p102-empty-source-binding-maintenance.md)。整案验收仍未通过。

## 当前 owned 状态

- 最新正常保存为 `99927096/R157/J100`；root R80 独立复核 `ebfc32ee3cff4248a8474cdea108a663:7`（SHA-256 `C94C08A790A3F44091FEDB870B04651EA9DB70F4499DA0CA8255B1878FBEB9F2`）确认当前P102/R157、完整背包/inc/held与J100匹配、健康primary ticket可用、无在途或unknown，external `4/10`、lifetime `264`。当前普通写冻结，下一笔normal Save仍为预留动作。
- R74回收新建但未接线的Ti矿机1；它的缓冲含 `1004×50`，正常拆除退回 `2301×1` 与该批矿石。完整玩家背包读回确认回收后的 `1004×50` 为inc0；不据此声称矿机有inc状态。旧footprint/yaw120候选的Native Build prepare因传送带对象7碰撞拒绝，0 build commit；此前R68同候选族也遇到一次碰撞，该候选族已退役。R77完整capture复核180 built/0 prebuild，仅矿机1移除，对象7传送带仍保留out8。
- R77同tick切片覆盖162个支持对象、5条完整Native路径及151条belt；同tick物料库存cut以station44为锚点。其slot0为Ti矿石 `1004×158/max200`，slot1为Si矿石 `1003×500/max500`，当时无订单、运输船或无人机；该读数不是Ti锭1106，也不证明自动补给。N1为17 nodes/9 consumers/10 generators、full-serve、capacity `55000 J/t`。完整证据见事件页。
- 当前安装仍为cohort `d744b8e6eb5b898b9e9484b2209c30380e40ddda`（64 tools/1 resource）。同星远端只读、64物料采样及空Ti矿机源端连接支持已通过离线测试/构建，未部署，尚无新原生实机读取或施工结果。

## Gate 2 边界

- 既有油源5949→3964保留。G源4450经快速分拣器6198接至6090；R47读回证明11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334的实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C只是短窗诊断，没有连续验收credit。
- P102 Ti自动来源仍未接通：原新矿机已拆除，对象7传送带仍有out8。R77同tick物料cut以station44为锚点；库存/机队状态不解释发船阈值，也不证明远端自动供给。Si miner17及其4节点保持原状。
- 源端空矿机例外目前只通过离线实现、策略测试与29104引用的Release构建；未部署或live-verified。实现只允许经身份绑定的Ti vein miner（`networkId=0`、实际`networkServes[0]=0`、`productCount=0`及受限产品类型）作为SOURCE路径唯一head feed；目标路径仍须保持完整、互异、空货且未接入设备feed。Native碰撞、占位、材料和原生prepare规则不变；详见事件页。下一有限施工/接线尚未获本页预告。
- shared supply、净物料/功率分配、自动补给、连续≥36000-tick验证、保存恢复整合和同一干净SHA双候选包均未验收，整案保持 `executable=false`。无新的业务动作获root重开，正常Save槽位仍保留。
