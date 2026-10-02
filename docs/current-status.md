# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。覆盖式摘要；一份权威阶段事实与原件索引见[Gate 2 证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。状态权威仍是原 session/action/Journal 和外部 accepted 台账，不是本文。

## 运行与封窗

- **DSP 保持原进程运行。** Codex/Host 退出、重启、断线、压缩或回合结束不触发保存、关游戏、重载或清零 accepted。重连先核 owned identity、session/revision、durable J 与原 action；健康、无未决就直接继续。真实 Codex 退出测试未做。
- source main/origin **81a670a**，准确 SHA CI **37052070990 success**；本次阶段文档尚待 commit/push/新 SHA CI。installed **3fe31d1**、DSP **0.10.35.29104**，此前4 Plugin＋224 MCP＋2 native哈希、64 tools/1 resource已匹配；本窗不部署，不复用已消费维护许可。
- 最近正常保存 **86729135**；保存回执 `8bb185…:38/39`。最新完整图为 **86730410**；closing S **86730665 / R14**，J采集 **86730652 / 97条 / durable97 / pendingfalse / errornull**。正常保存覆盖本窗全部 accepted 施工；观察晚于保存，不声称动态库存也停在保存点。
- external **10/10**、lifetime **70**，**冻结新 commit**。十个原 accepted 均成功终态，0 replay/unknown/在途/未保存 accepted施工；尚未做本窗保存后的 restart/resume。Luna已停止新 Game 调用，等文档、push、绿CI与root明确交接；不改游戏R/tick/J。

## 唯一目标与已完成差量

**Gate 2：1210完整自动三级链。** 整体有限计划已批准 `executable=true / wholeNativePreflightPassed=false / wholeSustainedRatePassed=false`；未来actual-ID接口、联合原生碰撞及持续余量必须现场核。保留5326–5334及既有输出、石矿/酸/869链；不做支线或任意布局。

本窗已完成 **63条2001＋1个2011** 的Graphene空输送路径：

- A12、D13、middle9、left14 NEW、right15 NEW，两个actual-ID双cover均保留原端点和旧边，五笔独立原commit均终态。
- 实际有向顺序 A→left→middle→right→D；自由首带 **5366**，末带 **5388**；分拣器 **5429**，filter1123，`5388.slot4→5429→884.slot10`，N3/serve1。虚拟belt slot−1并非实际slot0；真实slot4由双边扫描核验。
- **871源端尚未接**，不能把完整空路线说成物料送达、1206或1210产出。5329→5331的既有1210输出线已保存，仍无1210货物正例。
- 背包带 **582→519**、2011 **28→27**，其他库存/inc保持；Walk/低速/能量正、手搓空、pendingBuild0。无Move、批量取材、手搓、拆改、关闭/重载或新部署。

## 独立审计与尚待现场核验

完整捕获 `3bf6a4…:2–56`：**55页 / 5429 built / 0 prebuild / 10578互逆边**。root proof `5f7a97…:1`（SHA `1EB99710B362943707BD3C1AB3AA858550088B538A8ED93D9416B14457580E26`）核全页、十个原终态、材料、配置、连接、供电与J连续，0额外Game调用。

相对基线 `6477dc…` 仅新增上述64对象，旧对象只允许884增加本次slot10连接、1213移除已另证耗尽的39；其余静态无未解释变化。303个对象在已声明动态字段变化，不与配置混比。旧baseline跨两次获准恢复的玩家y差2微米；本session首笔施工P与封窗P位置完全相同，不放宽全厂pose比较。

- 完整九路预测554、硬上限571带/20sorter/2个2201；剩余材料要按已完成扣除，并在每笔fresh核，不囤token。Cu/Coal/Container/D等已有site及端点正例，但未施工部分不能写成接通。
- 自动源图已核Fe1496→1500→1511、Cu2440→10→26→562、Turbo724/725→814→827→3467、acid/graphite→870→869→871、coal5171→5187，以及H→3074→3073→D；保留旧分支。库存与D短窗不证明持续可分配余量。
- 本窗fresh N3 serve1，仅证明当前供电。整案峰值预算及必要两塔native正例已有原件；两塔未建，未来consumer覆盖、实际供料及持续燃料未证。

## 下一边界

完成本窗文档/单目的commit/push/准确SHA绿CI后，root才开启新的外部写窗。第一业务动作为 fresh `871.slot8→5366/filter1123`，证明883/r99真实收到Graphene并产出1206，再连续推进同一已批准三级链，不重做已建前缀。

Gate结束仍须 **全链自动供料→1210非零→正常保存→protected restart→恢复后再次非零**；之前不进联合36000-tick、燃料长窗、远征或最终双包。正常Gate验收重启与Codex退出清理不是一回事，不能借Host生命周期重载健康运行世界。

直接相关离线测试已通过59/59＋8/8；本地普通caller smoke为0Game，sorter/save的18个schema正负例不是native实机。A早先180秒观察超时已按同action核销，后续有界600秒观察不改native watchdog；旧R1资格快照不是新动作固定revision。模型用量、完整端到端耗时/提速比例仍unknown。
