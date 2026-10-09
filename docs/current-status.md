# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。当前运行代码来自`d6517834d4d8c55f48f841525008b4cde0a5f6e5`，已完成同批冷部署（228个文件、64个工具、1项资源；该源码2722项测试通过，CI `37857133940`成功）。R492已完成固定隔离恢复、正常保存和独立审计，详见[固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)。

## 当前 owned 状态

- 当前为和平、非沙盒、1×资源倍率的P104/R1，healthy且无当前隔离；最近正常主档保存`103307910`、Journal durable 102。R492恢复后核验的完整Journal历史与版本链连续。固定20窗口在7/20封存，lifetime 399；未用13笔不转移，新20窗口尚未开启。普通写入暂时冻结，等待root界定下一有限阶段；后续石料来源方案应采用结构不同的有界布局，不盲重试node198旧占位yaw345/135或空带未知失败族。
- 用户确认继续此前说明的固定LastExit隔离恢复方案。旧DSP进程在操作前已退出，退出原因未知。仅启动一次新桌面进程：首次Native只读请求超时，0 prepare/commit；同一进程之后显示healthy。唯一恢复prepare/commit被accepted。最初调用方误要求恢复DTO返回其不提供的幂等键，root核对接口后只查询原action，没有重提交。恢复完成后正常主档保存`103307910`覆盖固定LastExit `103307878`及此前四笔回收前缀；旧5322 action仍保留历史`outcome_unknown`，没有重放，也未伪称原action成功。
- 独立审计确认完整工厂6311 built/0 prebuild，无新增或移除实体、无非互惠边；全库存count/inc/held、玩家位置和Journal保持连续。唯一跨时间静态差异为矿机1213的resourceNodeIds从[36,37]变为[37]：fresh Native读取确认36已不存在，37为剩余1384、minerCount 1；这与正常采掘一致，但属于推断，不代表持续铁供给通过。Stone节点198读到40320、minerCount 0。35对象同tick货物cut确认剩余5321→5320→5319→5318→5317→5316为115格空载路径，87与3725存活；3座已加载工厂的全部网络均满供。
- R492的完整恢复、覆盖保存及当前快照核验不改变来源验收：R438/R441来源条件失败保持，continuous credit为0、`wholeSupplyPassed=false`。29物料、连通氢、远端新Ti及本地新炼Ti、Warper与Rod各至少1/min并连续至少36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；蓝图生命周期与Governor 2×已通过范围保持原结论，封闭的H侧候选不重开。

阶段索引：[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)、[R449石料来源与封窗](evidence/2026-10-09/stone-source-recovery-r449.md)、[R454空带回收适配](evidence/2026-10-09/empty-belt-recovery-adapter-r454.md)、[R472隔离状态与未决动作](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)。当前快照记录现状；历史事实留在各自事件页。
