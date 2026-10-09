# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前执行代码来自 `369f35c0d7ea352d505ec686cc22a52294d5601a`，对应 Windows Core CI `37992232537` success；cohort 为 2,762 项测试、228 个文件、64 个 MCP tools 和 1 个 resource。以下将 durable 保存截面与其后尚未保存的实际动作分开记录。

## 当前运行与写入边界

- 当前 owned 世界为 P104/R3、J102；最后正常主档保存 tick `105305314`。R618 与 R621 两次燃料 transfer 已 accepted，但均未被该保存覆盖；正常保存仍预留。当前固定 20 窗为 4/20，lifetime 422，无在途动作或 unknown；新的两个 refuel 动作把全案 accepted 上限从 34 调整到 36，已用 23。
- R618 转入 20 个重氢（1121），每个 9 MJ；玩家库存由 20 降至 0，燃料缓冲由 0 增至 20。有限读回时核心能量为 42.85 MJ，低于 100 MJ 目标。R621 转入 5 个氘核燃料棒（1802），每个 600 MJ；玩家库存由 5 降至 0，燃料缓冲/反应堆读回为 4/1，玩家核心能量为 104.54 MJ。出发清单所需的 5 根 carried rods 目前必须补回；以往携带记录不代表当前满足清单。
- R623 对固定北向落点完成 1 次 Native empty-destination cover prepare、0 commit、0 accepted。预检覆盖 21 个点，最长点距当时玩家 74.51 m；Walk 为 0，核心能量在该次读数区间从 643.77 增至 644.704 MJ。该预检不计作移动、落地或施工。R624 的固定五段北向施工尚无终态，不能写成已建或已接源。
- R623 复核的 57 个接收成员仍为开放空载路径，94 个受影响对象的静态/供电关系保持；矿机 5325 仍是 network 0、服务比例不可用（`null`）、缓冲为空。R598 的 6,366 built/0 prebuild 是旧封存基线，本窗口没有新的全厂快照。

## 被动漂移恢复修复

- 执行代码 cohort 的被动漂移修复只对显式 opt-in 的 `prepare_move` 生效，默认关闭。它绑定最近一次外部不可变玩家检查，要求速度不高于 0.15 m/s、位移不超过 0.05 m、检查不超过 2 秒，并精确核对原有所有非位置状态及容量、订单、锻造和无人机条件；其他动作、默认哈希和 Native 移动规则未改。
- R592 复算确认公开完整 PlayerAction 哈希的历史差异来自 Q0.01 m 位置量化；只将关闭读回位置替换为原检查位置时，前后哈希一致。prepare 内部快照未返回，不能推断其精确位置。
- R599 文件名大小写错误与 R603 缺少历史 operationId 均在发出 RPC 前停止（零 RPC）；R606 发出 3 次只读 RPC 后因普通关闭前的 speed 门停止，0 accepted、0 close/install/launch/load。它们是调用方停止，不是 Native 拒绝；成功前缀和原始记录继续保留。

## 尚未通过的验收

- `sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 为 0。Warper 与 Rod 最低产率及连续 36,000 ticks、双自动补给、完整材料与来源供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；已通过的蓝图生命周期与 Governor 2× 范围保持。
- R508 双包检查只是离线预检，不是实际 Mod Manager 安装或最终发行验收。接收端资格、实际北向施工、来源接电和供料仍待核验。

阶段索引：[北向接收端资格与燃料状态](evidence/2026-10-10/finite-mecha-fuel-stationary-north-r623.md)、[被动漂移恢复与状态绑定核验](evidence/2026-10-10/passive-drift-maintenance-r605.md)、[来源书挡与材料清单](evidence/2026-10-09/full-source-bookends-r441.md)。
