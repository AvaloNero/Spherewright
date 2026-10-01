# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本文为覆盖式快照，不是机器状态源；当前阶段证据见[Warper 计划与材料储备](evidence/2026-10-02/warper-plan-material-reserve.md)，上一阶段燃料施工详见[四写证据](evidence/2026-10-02/fuel-startup-and-owned-resume.md)。最近文档基线 `bbdb660f2375cb68d80727de783e97e5210be93b` 已推送且 Windows Core CI `36918528217` 成功。

## Gate 与现场

- Gate 1 的 native 启动正例已通过：受保护 run `7609eca884a9452281a224342d1fa58f` 第三段 600-tick 窗口产出氘核燃料棒 2、消耗 0。该正例不证明持续 ≥1/min 或 36000-tick 联合运行；Gate 3 未执行。
- Gate 2 目前仅完成材料储备，尚无新工厂施工：修正计划为 3 stages、depth 3、目标 1/min、900 kW，阶段为 `material_plan`，但 `executable=false`；item 1127 的源头物流与供电仍未完整，1210 链尚未建设。
- 材料预备 run `cc0393576a524fbe99c719756ba189ed` 共 10 个唯一 accepted（1 Move、4 Transfer、4 Craft、1 Save）；均为 completed terminal，index 为 0 replay / unknown / unresolved。Save ordinals 175/176 保存 tick 84712123、revision 23，Journal durable 97。root 独立全图审计已 PASS；proof 和库存变化见阶段证据。

## 写窗与后续动作

- 上一写窗 4/10 已冻结；新外部窗口从 0/10、lifetime 13 起始后，材料预备阶段冻结于 10/10、lifetime 23。proof `action-a05c20ca1e1e4c01b7d531dc100f6dfe-0001-warper-reserve-independent-audit.json`（SHA-256 `778848A7EB0F1BE5396BE873FCDDCC31100F4BD030E8339B39A97F9213D2D74B`）确认独立审计通过。窗口保持冻结，直到本次 docs push、对应 CI 绿色及 root 明确 handoff。
- Gate 1 native 启动正例已通过，但不等于持续 ≥1/min；保存后没有真实 restart，restart 不作为 Gate 1 准入条件。Gate 3 的 36000-tick 联合目标尚未证明或执行。
- 最新 capture run `be016a9413bf4a87960e9fc0b7b87d34` 的 snapshot tick 84714108、5325 对象；follow-up run `1b8b7e7adbe940629458d6c5075ab66b` 的 observed tick 84726744，Save tick 84712123。ordinal 60 的 `INVALID_ENTITY` 来自本地 PowerShell 参数数组绑定；后续逐项只读确认实体存在，全图独立审计已 PASS，详见阶段证据。
- 源码 pin `bbdb660f2375cb68d80727de783e97e5210be93b`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。当前摘要不代表新部署、MCP 握手或最终验收。
