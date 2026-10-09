# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前执行 cohort 仍来自源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`，Windows Core CI `37992232537` 为 success；对应构建有 2,762 项测试、228 个文件、64 个 MCP tools 和 1 个 resource。此处记录现场状态与尚未保存的动作，不把预检或旧基线当作新证明。

## 当前运行与窗口

- 当前 owned 世界为 P104/R13、J102；最后正常主档保存 tick `105305314`。固定 20 窗为 9/20，lifetime 427；R626 核验点没有在途动作或 unknown。R624 的五笔施工与此前两笔燃料转入尚未被该保存覆盖，正常保存槽仍预留，普通写入冻结。
- 本案累计 28 accepted、158 条新带材、11 个跨度、3 次移动、1 个分拣器、1 台矿机、0 台风机、0 座电塔、6 次普通保存、2 次普通燃料转入。窗口累计值不重置；封窗、来源或整体验收不会因这些施工动作自动通过。
- R618 转入重氢20（1121），R621 转入氘核燃料棒5（1802）；两者仍未保存覆盖，出发清单的5根 carried rods 需要重新补齐。

## 北向路线与来源

- R624 完成5笔 accepted，新增105条带材；所有5个 Native 建造点均读回对应实际对象。R626 独立核实接收端含162条带材、237格、3,379个cells的 Native 路径为空载；199对象切面显示源6317至新头6471的间距为2.5158 m。没有移动或新电源施工。
- 当前读回的矿机5325仍是 network 0、服务比例不可用（`null`）、缓冲为空；旧网络的成员关系和满供状态保持。R626 是局部实体/路径/库存核验，不是新的全厂快照。R598 的6,366 built/0 prebuild仍是封存基线，不能推称本轮全厂 built 达到6,471。
- R627 的实际接缝与固定风机/电塔 prepare-only 阶段正在进行，尚无终态；不得写成已建、已接源或已供料。

## 执行 cohort 与漂移边界

- 被动漂移修复只对显式 opt-in 的 `prepare_move` 生效，默认关闭；绑定最近一次外部不可变玩家检查，要求速度不高于0.15 m/s、位移不超过0.05 m、检查不超过2秒，并精确核对其他状态与容量、订单、锻造和无人机条件。
- R592 复算确认公开完整 PlayerAction 哈希的历史差异来自 Q0.01 m 位置量化；只将关闭读回位置替换为原检查位置时，前后哈希一致。prepare 内部快照未返回，不能推断其精确位置。
- R599 文件名大小写错误与 R603 缺少历史 operationId 均在发出 RPC 前停止（零 RPC）；R606 发出3次只读 RPC 后因普通关闭前的 speed 门停止，0 accepted、0 close/install/launch/load。它们是调用方停止，不是 Native 拒绝。

## 尚未通过的验收

- `sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 为0。Warper与Rod最低产率、连续36,000 ticks、双自动补给、完整材料与来源供需、整合保存恢复和最终同 SHA 双候选包验收仍未通过；已通过的蓝图生命周期与 Governor 2× 范围保持。
- R508 双包检查只是离线预检，不是实际 Mod Manager 安装或最终发行验收。来源接电、实际供料和完整持续供给仍待核验。

阶段索引：[燃料与北向路线核验](evidence/2026-10-10/stationary-north-route-r626.md)、[固定北向资格与燃料状态](evidence/2026-10-10/finite-mecha-fuel-stationary-north-r623.md)、[被动漂移恢复与状态绑定核验](evidence/2026-10-10/passive-drift-maintenance-r605.md)。
