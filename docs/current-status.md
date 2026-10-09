# Spherewright 当前快照

更新：2026-10-10（Asia/Singapore）。当前已安装执行批次来自 `bd749ee5380d6c374dbe43483f01b81c550892bb`：同源冷部署228个文件（4个Plugin与224个MCP文件）、64项工具和1项资源；该提交的2732项测试通过，CI `37954973052`成功。R508对同一源码SHA的双包离线预检通过；这不是实际Mod Manager安装、游戏运行验收或最终发行批准。

## 当前 owned 状态

- 用户确认的固定LastExit恢复已完成。当前同一和平、非沙盒、1×的owned P104/R1健康，正常主档保存tick `104944169`，Journal历史与版本链完整且durable 102；固定LastExit header tick为 `104944137`。R514独立核验恢复身份、连续性和保存覆盖。当前external计数2/20、lifetime 401，没有当前在途或未决动作；普通写入仍冻结，未开启新窗口。
- 首次菜单只读请求曾遇到Native `REQUEST_TIMEOUT`，当时为0 prepare、0 commit、0 accepted。root核清后在同一进程续接，一次resume accepted并Native完成；没有重启或重放。早先Build仍作为历史 `outcome_unknown` 保留，未改判成功；当前恢复事务无在途或未知结果。
- R514完整64页工厂读回为6313 built / 0 prebuild；相对核验基线无新增、删除、静态差异或非互惠连接。35个相关对象和5条完整Native货物路径保持；回程153格链仍空载、未接新源。玩家位置及背包count/inc/held保持，当前2001×33、2011×4；本地电网点读满供。
- R508离线预检确认手动包与Thunderstore包记录同一干净源码SHA，4个Plugin文件哈希一致，Windows PowerShell 5.1静态检查和隔离MCP协议探针通过。未做真实Mod Manager安装或游戏/Bridge黑盒验证，双包仍不是最终候选。
- 新矿源尚未接入，持续来源条件仍未通过；`wholeSupplyPassed=false`、continuous credit为0。29项物料、Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收均未通过。已通过的蓝图生命周期和Governor 2×范围保持，封闭的H侧候选不重开。下一候选是先fresh Native核5316→87过滤1005分拣接口，再核材料与不同group 19的277矿源；尚未执行。

阶段索引：[R514固定LastExit恢复与双包预检](evidence/2026-10-10/stone-fixed-recovery-r514.md)、[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472隔离状态](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
