# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本文是覆盖式状态摘要，不是机器状态源；完整 receipt 锚点与历史基线见[阶段证据](evidence/2026-10-02/fuel-startup-and-owned-resume.md)。燃料验证顺序仍为 Gate 1（定位唯一直接根因、实施最小修复并取得稳定启动正例）、Gate 2（建设 1210 链）、Gate 3（此后联合连续运行至少 36000 ticks 且两链均 ≥1/min）；持续门不是前两 Gate 的准入条件。

## 最近保存、观测与计数

- 最近确认的正常保存是新窗 Save：run `c8913aa1b7fb4981a0dccc15071237ab` 的唯一 accepted 为 `b16f243f-a153-4de3-840d-97001815ccfe`；commit ordinal 27、terminal ordinal 28 成功，保存 tick 84577452。ordinal 29 收尾 session 为 R8 / observe tick 84577456 / saved=true / owned / healthy / restartResumeAvailable=true；ordinal 30 Journal durable 97、pending=false、error=null。
- 后续完整快照 run `1bf0a4a449f24781970df6311864e4ac` 的 54 页 / 5325 实体快照在 tick 84601331；prebuild=0，收尾 session ordinal 71 为 R8 / observe tick 84601862，lastOwnedSaveGameTick 仍为 84577452，J97。独立全厂审计 PASS；关键差异与 proof 锚点见阶段证据。
- 旧写窗 9/10 已 PASS 并冻结。当前新窗 **4/10 提前冻结**，本阶段限额 7 accepted（矿机、belt、sorter、Save 合计）已用 4，lifetime accepted 13；四项均有 terminal receipts，0 replay / unknown / in-flight。最近一段 fresh production window 结束于 84603249；它是单窗观测，不是 fuel Gate 通过。下一写窗要等本次文档 commit/push、对应 CI 绿色和 root 明确交接，不能自动归零。
- 新矿机 object 86 只覆盖 node 203；9 段新 belt 经 sorter 5325 接入 87，再沿旧的 87→88 路径运输。旧厂线未改。详细对象、receipt 和边界见阶段证据。该施工不等于燃料 Gate 通过。

旧窗独立 proof 核读 45 条 receipts，核销 9 个 unique accepted：9/9 terminal succeeded、0 replay、0 unknown、0 in-flight、0 unresolved commit；其 PASS 仅确认写窗与连续性，不是 fuel Gate PASS。本轮不含重装或部署。

## 下一阻塞项

较早采样汇总为 `samples=0 / requests=19`，但原始 production ordinal 20 有完整的 600-tick native 观测；为什么该响应未被汇总为有效 sample 尚未定位。离线 `test-production-sampling` 为 27/27 PASS，protected-serialization 另有 1 条 fixture PASS；两者都不代表多窗实验通过。fresh run `65858d773c7c4b589ecac6d8eba98168` 又提供一段 600-tick counts 短窗，仍不足以证明持续产率。独立全厂快照审计已 PASS；保存后尚无真实 restart，但 restart 不作为 Gate 1 准入条件。

石矿源点替换、材料路径与新接线已完成四写独立核验；这仍未证明燃料端稳定启动正例或当前最小直接 blocker 已排除。Gate 1 尚未通过，Gate 2 的 1210 链建设尚未开始，Gate 3 的 ≥36000-tick 联合运行尚未执行。四写施工阶段有 4 个 caller errors；另一次后续采样在 ordinal 22 因私有校验读取了错误字段 `sample.state.planetId`（session 字段为 `localPlanetId`）而本地失败；该 run 有 session/entity/power/production 等只读回执，没有新的游戏写入，0 accepted。该读取已修正，受保护回调 AST fixture 为 2/2，Game 调用 0；新有限只读采样尚未完成。上述调用方问题与原生几何/覆盖拒绝、计划拒绝和无人机正常返航等待分开计数，旧历史解析错误总数仍未知。

当前阶段源码 pin 为 `2c34bdcc68e7215f8a3b9980d4cae385a6385bc2`，installed runtime/cohort source 为 `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）；完整版本与安装核对见阶段证据。本轮不声称部署、fresh MCP handshake、持续燃料达标或最终验收。
