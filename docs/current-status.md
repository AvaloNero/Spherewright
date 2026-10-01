# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件为覆盖式摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态和受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet 104，DSP 0.10.35.29104 | 同一已核 owned identity；不代表 0.4 发行验收。 |
| 最近保存与 closing | save 83307746；tick 83307763 / R5 | 同一 owned identity、healthy；`restartAvailable` 不等于真实重启验证。 |
| Journal | J97 / 97 条完整 durable | 无 pending/error；原 J96 前缀保持一致。 |
| 科研状态 | Drive Engine IV（tech 2904）已选入队列；hash 15/720000，仍 locked | J97 仅记 `upgrade_first_selected`；选择不等于解锁，两个 action 的观察 elapsed ticks 均为 0。 |
| 外部写窗口 | accepted **6/10 OPEN** | 本阶段两笔 action 均唯一成功终态；无 unknown 或在途写。 |
| 阶段基线与安装 | pinned main `37c91eadde3e654802a9c31ef844bde557e0c24d`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | 228 files / 64 tools / 1 resource；DSP 29104，未新部署。 |

## 科研选择与当前观察

本阶段唯一权威记录为 [Drive Engine IV 科研选择与正常保存](evidence/2026-10-01/drive-engine-four-selection-save.md)。tech 2904 已被选择并进入当前队列，但仍 locked、hash 15/720000；不把选择动作或队列状态说成解锁、持续科研或生产通过。正常保存 83307746 与 closing R5、durable J97 已核，保存后真实 restart/resume 尚未验证。无新施工、库存转移或 Move。

选择前的科研基线为 1704、2104、2903 已解锁，2904 为 0/720000 locked，三种科研糖各 2000；选择后只核到目标入队和 hash 15/720000。两个 action 各自观察到的 elapsed ticks 均为 0；选择与保存不构成持续科研实验或产出证明。

此前石料/紫糖关键供给链只短引[既有阶段事件](evidence/2026-10-01/purple-critical-path-source-route-demand.md)：其原料/物流问题仍不能靠静态线路推断为有供给，不在本快照重写历史。本阶段尚未证明持续燃料或曲速供给、Foundry Level 3 整厂施工或最终双包验收。下一项准备 blocker 是既有实体 3403 的氘燃料入口；只读诊断仍在进行，尚无结论。

## 安全边界

仅对精确核验的 owned identity 执行有界动作，遵守 fresh prepare、精确计划校验、唯一 commit、同 action terminal/readback 与 durable Journal。accepted 不因 revision、tick 或 Journal 变化清零；断线或摘要异常不构成重放授权。出现 unknown、quarantine、身份/版本漂移或结果无法核销时冻结新写并交 root。正常保存只作用于当前 owned identity；`restartAvailable` 不是重启验收。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
