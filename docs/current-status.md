# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、Journal、accepted 和终态以 fresh 状态及受保护原回执为准。历史详见[迁移前状态史](evidence/2026-09-30/current-status-history-through-2026-09-30.md)与[游戏时间线](gameplay-timeline.md)。

## 版本与当前档案

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重新安装、恢复或发行验证 |
| 最新状态 | run d049453bbbdd4049ac142b27e850555b ord59：tick80377608 / revision30；healthy、零 blocker | 同一 owned primary；peaceful、非沙盒、1×，无 quarantine/checkpoint |
| 保存与日志 | lastSaved tick80273796；Journal durable J96，无 pending/error | f322467c 的两次转移晚于该保存，尚未保存覆盖 |
| 玩家 | Walk / 0、约800 MJ；无人机均 idle、手搓队列空 | 只代表本次读回，不证明重启恢复 |
| Git/发行 | 文档批次前 main 为7287a45且对应CI已绿；0.4.0尚未打tag、未发Release或Thunderstore | 旧本地候选不算最终包；精确安装 Plugin/MCP SHA 未在本次重验，文档提交不代表已部署 |

## 第十写审计

外部 accepted 为 **10/10**。十个原始 action 都有唯一 completed/succeeded terminal；无 replay、unresolved intent/action、unknown 或不配对终态。提交审计资料与绿 CI、root 明确交接之前，下一次游戏 commit 仍冻结；回执索引显示 newWriteBlocked=false 不解除外部计数门。

完整工厂对比：旧基线 8a060d67b80944bf8315f1ff8871e969 为5194实体；当前单快照 d049453bbbdd4049ac142b27e850555b ord2–53，tick80377177、52页、5196 built。新增5195/5196，无移除；静态差异仅3068的itemId 2011→2012及3365、5187、5189、5193四组互返连接。resourceNodeIds无变化，10122→10130条端点记录均互返，零nonreciprocal边；另有247个对象的动态字段变化，不抵销静态审核。主会话已按原始终态读回核对相关对象与当前快照一致。完整回执、连接端点与材料边界见[第十写事件](evidence/2026-09-30/core-materials-ten-write-audit.md)。

## 材料与科研缓存

相对8418a1e5bef240b5a436314ec877842a ord2玩家背包：电路板1301净增1000；普通分拣器2011净减1（施工耗2、升级返1）；高速分拣器2012净减1；其余14种物品不变。f322467c ord7→8 电路板源仓3600→2600、玩家0→1000；ord18→19 紫糖6004源仓2324→1824、玩家0→500。两笔均为成功普通转移，不是生产证明。

随后读回的自动科研缓存为1,800,000点、500整件等值、0余数，autoManageResearchItems=true，而背包紫糖为0；这不是物品丢失。缓存wholeItemCount不等于背包实体，也不是可再次转入仓库的库存。必须分开看inventory、pointCount与remainderPoints；不得据缓存宣称实际科研供料或2104解锁。

## 下一步与未证明项

当前尚未保存 f322 的两笔取料；待本次文档commit/push、准确SHA CI绿灯、root明确交接后，优先只做一次正常保存。已被原生拒绝的消费者stub旧坐标不原样重放；科研自动供给另行设计。当前材料审计不宣称2104解锁、连续燃料/生产、保存后重启恢复或完整产线通过。历史蓝图/Governor门不倒退重做；正式发行仍须按[Roadmap](../ROADMAP.md)逐门验收。

证据入口：[第十写材料与工厂差异](evidence/2026-09-30/core-materials-ten-write-audit.md) · [旧生产诊断](evidence/2026-09-30/hydrogen-upgrade-three-window-diagnostic.md) · [分拣器升级与保存](evidence/2026-09-30/hydrogen-exit-upgrade-save.md) · [Agent playbook](agent-playbook.md) · [Roadmap](../ROADMAP.md)。
