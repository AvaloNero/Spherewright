# Warper plan and material reserve

记录：2026-10-02（Asia/Singapore）。root 已完成本阶段独立审计并 PASS；本文记录材料储备动作、快照守恒和仍未完成的 Gate2 物流缺口。审计通过不等于计划可执行、工厂链施工完成或最终 Gate 验收。

## 阶段边界

燃料 Gate 1 的 native 启动正例已通过；这不证明持续 ≥1/min 或 Gate 3 的 ≥36000-tick 联合运行。Gate 2 目标是建设 1210 链，但当前修正计划所处 phase 为 `material_plan`、`executable=false`。旧四写窗 4/10 已冻结；root 开启的新外部写窗以 0/10、lifetime accepted 13 起始，随后材料预备阶段冻结于 10/10、lifetime 23。10 项预备动作的终态/index 与全图材料变化已由 root 独立审计 PASS；Gate2 物流与实际工厂建设仍未完成。

## Gate 1 启动正例

受保护 run `7609eca884a9452281a224342d1fa58f` 有 56 次只读请求。总耗时 112049.6478 ms，其中模拟等待 105000 ms、读取 6152.665 ms。result ordinal 61 记录 3 samples、1 qualifying、56 read-only；root 独立 proof 为 `action-3be9f9c9f1474c86b50bb9156db0b264-0001-root-fuel-startup-independent-audit.json`，SHA-256 `E9E4ECC49356E3D23EE14D512499C151AF8491C32EB35AD1CB11511504D889B9`。

三段 native production 均为 600 ticks，氘核燃料棒（item 1802）计数如下；数字为窗口累计产/耗，不是每分钟速率：

| Ordinal | 游戏 tick 窗口 | 产 / 耗 |
|---:|---:|---:|
| 20 | 84642858–84643457 | 0 / 0 |
| 39 | 84643513–84644112 | 0 / 0 |
| 58 | 84644165–84644764 | 2 / 0 |

第三窗附近的 fuel-3403 inspect ordinal 56 显示 recipe 41，buffer Alloy 2、Deuterium 4、Ring 2、燃料棒 output 0；network 3 ratio 1.0。root 判定 native 启动正例通过。该结果不是持续 ≥1/min 的证明；保存后未做真实 restart，Gate 3 也未开始。

## 1210 材料与计划

fresh reserve 读取来自 run `489c463731ab4cdfa9961b52d96a7aea`，catalog 6，game tick 84658752，session R8，last owned save tick 84577452，Journal 97。

- object 883（recipe 99）需要 item 1204、1104、1123 各 2；当前对应缓冲为 1 / 0 / 0，output 0。
- object 884 的引用来自旧完整快照 `1bf0a4a449f24781970df6311864e4ac`，只有静态拓扑证据：3 个 output 分别连接 887、888、889，再通向 883；未见 input。object 884 本体在当前 reserve run 中尚未 fresh 检查，不能把旧静态图当作本次实时供货。
- 原 Foundry 41 计划请求因 `unused_choice` 被拒。root 核实 catalog item 1112 标记 `isRaw=true`，compiler 默认将其视为外部供给；显式选择 `1112:r60/2302`、`1209:r101/2303`、`1210:r78/2303` 后，计划校验通过，未改应用代码。

修正后的计划响应 `6541301fae6747c3b63582f66f7a0e12` 为 3 stages / depth 3、目标 1/min、900 kW；计划构件为 item 2302 ×1、2303 ×2，外部供给含 item 1109 4/min、item 1127 1/min。该响应的 phase 为 `material_plan`、`executable=false`：item 1127 还依赖 recipe 104 的自动供给，以及 item 1206、铁和氘的输入。相关科技已解锁，无需新 research；这并不消除材料缺口，也不意味着整条 1210 链可施工。

## 已完成的有限材料预备动作

受保护 run `cc0393576a524fbe99c719756ba189ed` 核销 10 个 unique accepted actions：1 Move、4 Transfer、4 Craft、1 Save。terminal 均为 completed；index 为 0 replay / unknown / unresolved。Save ordinals 175/176 成功，保存 tick 84712123、revision 23，Journal durable 97。summary ordinal 181 记录 160 requests、elapsed 163111.96 ms。新外部写窗冻结于 10/10，lifetime accepted 23。

独立审计 proof 为 `action-a05c20ca1e1e4c01b7d531dc100f6dfe-0001-warper-reserve-independent-audit.json`，SHA-256 `778848A7EB0F1BE5396BE873FCDDCC31100F4BD030E8339B39A97F9213D2D74B`；纯离线、0 Game calls、耗时 25.420 s。受保护 inventory/index 的材料净变化为：Fe +135、stone +10、magnet +18、copper +9、board −41、ASM item 2303 +2、small item 2302 +1、storage item 2101 +2、sorter item 2011 +29；全物品 count/inc 与 native 原 itemDeltas 守恒一致，其他项目无未解释变化。本阶段没有新建工厂实体，不能把材料备货写成 1210 链施工。

## 完整快照与独立审计

full capture run `be016a9413bf4a87960e9fc0b7b87d34`：snapshot tick 84714108，ordinals 2–55 共 54 页、5325 对象；ordinal 56 player、57 Journal、58 power、59 prebuild=0 成功。root 独立审计确认：旧静态 15 字段与 station configuration 全部不变；10386 条有向边均有唯一 reciprocal；0 新实体、0 configuration 变化。全玩家物品 delta/inc 与 native action result itemDeltas 守恒一致；Move 落点距离 ≤1.6m、Walk=0、core 1.592G、forge 空、drone 0。Journal identity 与 97 entries 全部不变；power configuration 全部一致、ratio=1。

ordinal 60 的 detail 调用返回 `INVALID_ENTITY`，原因是本地 PowerShell `int[]` 参数把 `1511,95,26` 绑定成一个整数 `15119526`（逗号按千分位处理），不是三座实体丢失。原调用只请求了这个错误 ID。另一次本地 typed-foreach/parser 失败没有发出 Game call；同语法离线 smoke 显示 `explicitDetails=1`，随后共享 caller 逐 int 验证 1511、95、26 都存在。

补充只读 run `1b8b7e7adbe940629458d6c5075ab66b`：ordinal 2 与 6 的 fresh session 相同，均匹配 R23、save tick 84712123、version 0.10.35.29104、healthy 与同 SID；ordinals 3/4/5 分别检查 1511、95、26，配置均与 full snapshot 一致。该 run 共 5 read-only、0 writes，不重采 snapshot；closing observed tick 84726744，snapshot tick 仍为 84714108，最近 Save 仍为 84712123。

本阶段审计 PASS 的范围是 receipts、inventory/action-delta 守恒与全图快照一致性；它不证明 1210 链已经施工。Gate2 当前仍是 `material_plan`、`executable=false`，item 1127 的来源物流与供电未闭合。保存后没有真实 restart，持续 ≥1/min 与 36000-tick Gate3 仍未证明。阶段在 10/10、lifetime 23 冻结，需等本次 docs push、绿色 CI 与 root 明确 handoff 才能开放下一窗口。

上一阶段文档提交 `bbdb660f2375cb68d80727de783e97e5210be93b` 已在 main，Windows Core CI run `36918528217` 成功；当前源码 pin 为该 SHA，installed runtime/cohort source 仍为 `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`。本次审计 proof 本身为离线检查，无 Game 请求。参见[当前快照](../../current-status.md)及[上一阶段四写证据](fuel-startup-and-owned-resume.md)。
