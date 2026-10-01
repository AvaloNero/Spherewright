# 氘燃料持续性就绪复核

日期：2026-10-01（Asia/Singapore）。本事件记录连续采样窗口、未达到的预声明速率门和当前保存回执摘要；它不是 Governor、生产、0.4 发行或重启验收。

## 连续窗口与判定

新独立实验 run `6526eb0ea08c40d89679b03343cd818b` 共 73 个样本、1146 次原生读取成功。连续窗口为 tick `83821560–83857832`（36273 ticks），0 reset、0 write；采样循环内 0 模型决策。这里的“连续”仅指采样覆盖，不是燃料生产或 Governor 通过。原生 result 为 ordinal 1222，closing 为 ordinal 1221。root proof `1c34fa7fa0174384afe1298f571ee304` ordinal 1 / SHA-256 `CC667513301B8F2B658978FBCCC1D07F4A72C1FB2F775D42AC7CBD191368A5F6` 核对原始请求、窗口、速率、73 个样本的 network 3 consumerRatio ≥0.999，以及五个指定对象的覆盖；recipe 40、41、58 对应设备 3073、3403、3084。这些是采样点，不构成连续功率历史证明。

预声明门槛为每分钟至少 1 棒。不同窗口有重叠，1802 只有 4 个非零样本，不能相加当作总产量；所有窗口 produced 合计 8 只可作覆盖产量上界。故全窗速率上界为 `8×3600/36273 = 0.794` 棒/分钟，低于门槛，连续生产门未通过。3955 库存 min/max 为 203/207，首末 203→207。窗口末 3073 的 H 为 5、D output 为 0；3403 的 D input 从起始 15 到末值 0。alloy/ring 均为 2。3084、3073、3403 分别有 63/73、6/73、10/73 个样本报告 working；这些样本数不等于连续工作时间，不以单帧状态推断全程。

上一独立实验 `6931359d1b2546ae948e002e7be80b8d` 在 180 秒截止，仅 11/12 窗、6600 ticks、113 次原生读取，连续信用为 0；窗口 7 曾产出 2 棒且 3955 从 182 到 184，不证明持续产出。它与本次获批的新连续计划互相独立；本次不是重启或续跑旧 run。旧实验 root proof `93e65efb10584400aebfb48ce6c16f7e` / SHA-256 `5BBFFDF383DED7600A086FA93FE06C6BC87BA9CA76D48F11C69E24DB4A7670D7`。

## 调用方与计量边界

诊断 run `04cb2ae2d69b48cf9f4bb22c81cae10f` 有 7 条 durable 原生读回；第 8 次只读请求可能已发送，但 ordinal 碰撞使结果未持久化，不能用于结论。该 run 无写操作。诊断与 post-gates 的两项调用方摘要错误分别涉及 `infrastructureFindings` 层级和不存在的 `player.movementMode` 字段；root另有两次本地审计脚本错误，均以既有回执修正，不重读、不重放。未持久化的读结果不当作证据；已持久化原生拒绝为 0。

私有 AuditedBridge 调用方现有发送前 `ordinal_collision` 防护；它只避免向已占用证据序号发送，不是原子事务，也不能补救发送后的 I/O 故障；未改变 Plugin、MCP surface 或部署。root 的离线证明 `54b0818ad2cc45d68ba4888fe66f9135` / SHA-256 `F6A4B77AB184B694F83CDF7BE8A737EA7ED8A0A52620FBFFC1A01F990F60B662` 为 8/8 fixture pass、fake backend 3、真实 Game 0。其他摘要与本地审计索引错误均使用已有回执修正，没有额外游戏请求或重放。

本次连续实验墙钟 653264.57 ms，其中 scheduled wait 560000 ms、native read 85680.212 ms、root 本地审计 11093.6776 ms；这些是各自片段，不等于全阶段端到端墙钟。整体墙钟以及模型/provider 总用量未知，不估提速比例。

## 保存与剩余边界

唯一正常保存 run `4c6736540ddb455287a92896221fa0b3`：ordinal 5 prepare、6 intent、7 accepted、8 terminal、9 closing session、10 Journal；action `e51499fc-84d0-4fdb-b075-e7e285da8c65`。save tick `83923751`、观察 `83923765` / session R5→R6、J97 durable、无 pending/error、healthy；external accepted 从 6 增至 **7/10 OPEN**，无在途写。root proof `208d3de801194eb58860f58e5d528aae` / SHA-256 `2224AC37A186AFFD438A1BCC161EA10973BB20FCF7AB25C3C4A1BF6E6D2D669E` 独立核对 8 条原生回执全成功、唯一 `commit_save`、同 action terminal completed/succeeded、0 itemDelta、保存点与 J97→97 连续性及 accepted 6→7。保存 action 546.933 ms，保存阶段 1692.121 ms。保存由 proof 核销；`actualRestart=false`，`resumeAvailable` 不是实际 restart/resume 证明。

失败门是重氢供给/分配未达到预声明燃料速率；唯一物理修复尚未定位，不扩建。紫糖科技 2104 已由历史库存完成且不再是消费门；[既有紫糖事件](purple-critical-path-source-route-demand.md)中的 1402→4743→6004→Lab84 端到端目标仍未完成，本燃料记录不代表修复它。不得把本次采样、latest save、网路 ratio 或单帧 working 当作持续生产/发行/恢复通过。
