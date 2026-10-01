# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件是覆盖式摘要，不是机器状态源；身份、accepted 计数和原生终态以 fresh 状态与受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet 104，DSP 0.10.35.29104 | 同一已核 owned identity；不代表 0.4 发行验收。 |
| 最近正常保存 | tick 83307746；最新 session 83562352 / R5 | J97/97 durable；`resumeAvailable` 不等于真实重启验证。 |
| 最新只读科研观察 | progression tick 83562381；tech 2104 已解锁 4/4、300000/300000，未排队 | 2104 的 6001–6004 各 500 需求已完成；当前队列为既有 tech 2904。 |
| 外部写窗口 | accepted **6/10 OPEN** | 本次新增 accepted 0；无 unknown 或在途写。 |
| 本轮基线与安装 | pinned source `43ff39b1d93c7e84bf47ee6c4495cb761f7a1746`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | 228 files / 64 tools / 1 resource；DSP 29104，未新部署。 |

## 紫糖端点需求复核与停止边界

本阶段唯一权威记录为[紫糖关键供给链、原料路径与端点需求复核](evidence/2026-10-01/purple-critical-path-source-route-demand.md)。本次只读复核确认指定的 tech 2104 需求已经消失：其 6001–6004 各 500 的需求已完成，且 tech 已解锁 4/4、300000/300000、未排队。它由历史库存完成，不能归因于新建的 1402→4743→6004→Lab84 供给链。当前既有队列 tech 2904 仍 locked，latest hash 为 254749/720000；其需求只有 6001、6002、6003 各 2000，没有 6004，故不能作为继续指定紫糖链的消费门。本次未选择该科技。完整端到端目标仍未完成；不得改选其他科研目标或扩建紫糖路线。

本次三个只读请求均成功，检查了 session、Journal 和 progression；J97/97 与先前正常保存回执 ordinal 18 完全一致。最新 2904 进展尚未再次保存；没有本次保存后的真实 restart/resume 验证。此前 861 的石矿 5/8 与 2255→1402 的碳纳米管缺料仅是早先诊断边界，本次未重新读取或修复。A/B/C 多窗以及 36000 tick 持续实验均未核销为通过；本批未请求实体或生产数据，也没有 prepare、commit、施工、Move、采集或保存。

本次 external accepted 仍为 6/10 OPEN，新增 accepted 0，无 unknown 或在途写。原生拒绝数为 0；四项本地摘要/索引字段错误均用已有回执更正，没有额外游戏请求或重放。只读回执跨度 496.6008 ms、成功 proof 片段 374.1968 ms；这不是完整阶段耗时。业务 prepare/commit 耗时、模型/provider usage 与整体墙钟均未知，不据此声称提速。

## 安全边界

仅对精确核验的 owned identity 执行有界动作，遵守 fresh prepare、精确计划校验、唯一 commit、同 action terminal/readback 与 durable Journal。accepted 不因 revision、tick 或 Journal 变化清零；断线或摘要异常不构成重放授权。出现 unknown、quarantine、身份/版本漂移或结果无法核销时冻结新写并交 root。正常保存只作用于当前 owned identity；`resumeAvailable` 不是重启验收。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
