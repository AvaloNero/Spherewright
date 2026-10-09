# R492 固定隔离恢复与当前主档核销

更新：2026-10-09（Asia/Singapore）。用户“确认继续”覆盖此前明确描述的固定LastExit隔离恢复方案。本记录只核销该次恢复与其后状态；不把历史未知action改写为成功，不扩大到其他恢复候选。

## 恢复与保存

旧DSP进程在恢复操作前已退出，退出原因不作断言。经既有桌面入口只启动一次新进程。首次Native只读请求返回`REQUEST_TIMEOUT`，当时0 prepare、0 commit；同一进程随后在菜单报告healthy。使用默认quarantine-ticket路径做一次fresh prepare和唯一commit，恢复被accepted。最初调用方错误要求恢复DTO提供其未提供的幂等键；root核对接口后查询原action，没有重提交。

原Native恢复完成后，正常主档保存`103307910`覆盖固定LastExit `103307878`和此前四笔回收前缀。Journal保持durable 102，完整历史与版本链连续。R467中5322的原action/key仍保留为历史`outcome_unknown`；没有重放，也不声称那笔原生拆除action曾成功。恢复后的当前主档经独立完整核验为P104/R1、peaceful、非sandbox、1×资源倍率、healthy且无当前隔离。

## 快照与差异

完整工厂快照为6311 built、0 prebuild，无新增/移除实体或非互惠边。全库存count/inc/held、玩家位置、J102全部条目均保持连续。唯一跨时间静态差异是矿机1213的`resourceNodeIds`从`[36,37]`变为`[37]`。fresh Native读到36已不存在，37为铁矿、剩余1384、`minerCount=1`；这与正常采掘一致，但只作为解释性推断。Stone节点198读到40320、`minerCount=0`。

35对象同tick货物cut确认剩余六带`5321→5320→5319→5318→5317→5316`形成115格完整空载路径，87与3725仍存活。3座已加载工厂的全部网络满供。完整工厂、物料cut及玩家/J核验均为独立读回；仅在各自原生时间范围内采用，不跨tick拼接。

## 窗口与验收边界

本窗口在7/20封存，lifetime 399；不清计数、不转移未用13笔，新20窗口尚未开启。当前正常写入冻结，后续由root界定有限阶段；下一石料来源方案应采用结构不同的有界布局，不盲重试node198旧占位yaw345/135或空带未知失败族。R438/R441来源失败保持，continuous credit为0、`wholeSupplyPassed=false`；恢复不证明29物料共同供需、连通氢、Ti来源、Warper/Rod目标、双自动补给、整合恢复或最终同SHA双候选包通过。已通过的蓝图生命周期与Governor 2×范围保留，封闭的H侧候选不重开。

## 原件索引

- R489 writer：`80e294cc305d421fb84d1f54dbce8456:73`，SHA-256 `8CE57D952279F26EC6FE873FC5FADDD7E64B992B25E636DB8AF11C3A9B915E1D`。
- 恢复原件9条：`d524b182db034e409dd7fb13ec3bf2fd`，SHA-256 `CC9ACDA551412A05246F0AF9D652EFAB5BEE1CDF33AAE26C5F5CEEF55189F262`。
- 原生完成3条：`727fe8a89e014773aeaa7d1eb3eaf957`，SHA-256 `E9B08DDB7D41F0676D02D7EE2A860B8AD106645AF7443F5201C40F071D9D8069`；节点核验4条：`c1beeae608ad46f9958c5e449197ccec`，SHA-256 `DE38ECBB1AF118B628781BA2880DB96A51D22615D796AB8E9714A04FB0637C6A`。
- R492独立审计：`2a204011c46b4450a7138fbcfb7a18d7:1`，SHA-256 `F9694A29909A798E686127759F123AF5256E432A381CDFCF8BAD0883F33A9CA7`。
