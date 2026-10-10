# Spherewright 当前快照

更新：2026-10-11（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`，Plugin DLL 未变。R839 封闭了原固定 20 写窗口；来源条件和整体持续供需验收仍未通过。

## 当前保存与窗口

- 当前正常保存为 tick `107238380`、revision `79`、durable Journal `102`。原窗口从 lifetime `454` 开始，共有 `16/20` 笔 accepted、lifetime `470`；余下 4 槽退役，不转移。15 笔动作成功，另 1 笔 accepted Move 失败并已唯一核销；16 笔均由该 Save 覆盖，无在途或未知动作，状态健康且冻结。
- 当前完整工厂截面为 `6492 built / 0 prebuild`。相较 R802 基线没有新增/移除对象、静态差异或非互惠连接。此次核验了 P104 本地三个电网满供；未 fresh 核验其他星球电网。玩家包内 Ti Ore `50`、Ti Ingot `0`、Warp `400`、Rod `5`、Stone `6`；本阶段没有取用 Ti Ore、Ti Ingot 或 Warp，也未启动短采样。

## 本阶段核销与流程

R839 阶段包括 R825 fresh 诊断、R827 超过 32 m 的 Native 地表预览在零 accepted 时停止、R830 水面字段解析问题在零 accepted 时停止、R834 一笔正常 Move accepted 后以 `position_stalled` 结束、R835 原动作只读核验、R836 独立核销、R837 普通保存与 65 页完整工厂读取，最后由 R839 独立封窗。失败目标与既有 orthogonal/microshift 候选族保持退役，不重放。详见 [R839 移动停止、保存与流程核验](evidence/2026-10-11/native-route-stop-save-and-process-r839.md)。

流程改进已落实单一 writer、固定 20 笔窗口、复用拓扑缓存、采样等待不触发模型决策，以及事实文档/CI 与同 cohort 阶段并行；但端到端提速目标尚未达到。不同端点的阶段墙钟间隔分别为：R813 审计结束至下一次消费者读取声明 `901.7697889 s`；最近 fresh audit 至首个业务 prepare `1665.2289724 s`；批准至首个 prepare `86.5060243 s`。这些不是命令执行时长，也不构成严格同比。测得 fresh 读取 `3879.7172 ms`、保存与完整工厂读取 `27976.2434 ms`、独立审计 `27642.6247 ms`。本次短采样尚未开始。

## 来源与验收边界

- `sourceConditionsPassed=false`、`wholeSupplyPassed=false`，continuous credit 为 `0`。R783 的连续来源长窗未通过；R807 的短窗仍只是有限供料观察，不能替代来源门。相关失败与未通过边界保持，不得降低 Warper/Rod 连续产量、钛供应、全物料稳定性或其他既定门槛。
- Ti/Warp 取料及来源确认尚未执行。下一阶段只能基于已核验几何与当前可用的正常移动接口，先 fresh 资格化结构不同的有限路线；此记录不表示存在本地 flight 能力，也不表示路线、供料或持续生产已通过。蓝图/Governor 等先前已通过门保持有效；双自动补给、整合保存恢复及最终同 SHA 双候选仍未通过。

阶段索引：[R839 移动停止、保存与流程核验](evidence/2026-10-11/native-route-stop-save-and-process-r839.md)、[R783 完整来源长窗](evidence/2026-10-10/current-full-source-long-r783.md)、[R807 磁铁供料短窗观察](evidence/2026-10-11/magnet-feed-short-r807.md)。
