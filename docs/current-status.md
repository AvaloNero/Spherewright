# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前安装批次来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508对同一源码SHA的双包离线预检通过，但不是实际Mod Manager安装、游戏运行验收或最终发行批准。

## 当前 owned 状态

- R536封存后当前为和平、非沙盒、1×的owned P104/R12健康状态；正常主档Save tick `105093150`，Journal完整且durable 102。external计数9/20、lifetime 408；本固定20窗口9笔写入经独立核验并提前关闭，余额不转移，尚未开启新窗口。当前无在途或未决动作；历史R498的 `outcome_unknown` 仍保留，不改判成功。
- R529已建矿机5325；其首次caller错误要求未接网的Native `serveRatio=0`，实际值为network 0、比率不可用（`null`），这是读回断言不匹配，不是Native建造失败。R532/R533只读核销同一原action、未重放。R534建成出口带86与6317并保存；R536独立核验全窗口与覆盖。
- 完整工厂读回6317 built/0 prebuild。相对R519仅新增5325、86、6317；旧静态配置、互惠连接及5条完整Native货物路径未变。链路为 `5325:0→86:1`、`86:0→6317:1`；尾端6317仍空闲，新路径空载。矿机列出的group 19节点262、264、267、271、273、277有正余量，但矿机尚未开采、network为0，服务比率不可用；不代表自动供料。
- 玩家位置及其他背包count/inc/held保持；矿机从2件减至1件，2001带材净减2、当前31件。此阶段未新增电力对象或移动；完整货物路径保持空载。
- 下一项已授权的有限工作为在原整体预算内取材、制作并处理运输供电；具体动作仍按阶段逐项绑定。持续来源条件仍未通过：`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。已通过的蓝图生命周期和Governor 2×范围保持，已封闭的H侧候选不重开。

阶段索引：[R536石料矿机出口与封窗核销](evidence/2026-10-10/stone-native-source-outlet-r536.md)、[R528石料来源接近资格](evidence/2026-10-10/stone-source-approach-r528.md)、[R519石料过滤分拣器与保存核销](evidence/2026-10-10/stone-filter-sorter-r519.md)、[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
