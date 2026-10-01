# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件为覆盖式摘要，不是机器状态源；accepted 与原生终态以 fresh 状态和受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet 104，DSP 0.10.35.29104 | 同一已核 owned identity；不代表 0.4 发行验收。 |
| 最近正常保存 | save tick 83923751；session R5→R6，观察 83923765 | root proof 208d3de8 核验 terminal 与 J97；实际重启未做。 |
| 外部写窗口 | accepted **7/10 OPEN** | 本阶段保存新增 1 accepted；无 unknown 或在途写。 |
| 本轮基线与安装 | pinned main source `c1540b88219956e682475dbe152d0afe81615655`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | 228 files / 64 tools / 1 resource；DSP 29104，未新部署。 |

## 燃料持续性门

本轮唯一权威事件为[氘燃料持续性就绪复核](evidence/2026-10-01/fuel-continuous-readiness-check.md)。连续窗口为 36273 ticks、73 样本、1146 次原生读取成功；预声明门槛 ≥1 棒/分钟，重叠窗口 produced 计数之和 8 仅为覆盖区间产量上界，对应速率上界 0.794 棒/分钟，门槛未通过。73 个样本中的 network 3 consumerRatio 均 ≥0.999，但只是采样点，不是连续功率历史证明。重氢供给/分配仍未满足目标速率，具体物理修复未定位；不据单帧 `working` 或重叠采样相加宣称持续生产。旧 180 秒实验与本次新连续计划彼此独立，旧窗不获连续信用。

## 紫糖需求与未完成目标

2104 的 6001–6004 各 500 需求已由历史库存完成，technology 已解锁；它不再是新紫糖供给链的消费门。既有[紫糖关键路径事件](evidence/2026-10-01/purple-critical-path-source-route-demand.md)中的 1402→4743→6004→Lab84 端到端目标仍未完成。燃料采样不是紫糖修复，也不授权改选其他科技或扩建紫糖链。

## 保存与调用边界

正常保存 run `4c6736540ddb455287a92896221fa0b3` 的 action `e51499fc-84d0-4fdb-b075-e7e285da8c65` 已由 root proof `208d3de801194eb58860f58e5d528aae` 独立核销：8 条原生回执成功、J97 durable，保存 83923751，accepted 6→7。实际 restart/resume 未验证。此前 2104/Journal 快照见紫糖事件；不从本次燃料采样推断科研、其他库存或重启结果。

发送前 ordinal 防护只阻止已占用证据序号的请求；它不是原子事务，也不能补救发送后的 I/O 故障。本地调用方/索引摘要修正均不等于原生生产验收；没有新 Plugin/MCP surface 或 runtime 部署。

## 安全边界

只对精确核验的 owned identity 执行有界动作，遵守 fresh prepare、计划校验、唯一 commit、同 action terminal/readback 与 durable Journal。accepted 不因 revision、tick 或 Journal 变化清零；断线或摘要异常不授权重放。遇到 unknown、quarantine、身份/版本漂移或结果无法核销时冻结新写并交 root。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
