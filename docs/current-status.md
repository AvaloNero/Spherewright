# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页按R303封窗更新；详见[ILS准备与十写审计](evidence/2026-10-08/departure-ils-ten-r303.md)。R282及更早历史仍见其[事件记录](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。

## 当前 owned 状态

- 当前为和平、非沙盒、1× owned 世界，R14 / J102，normal Save `101323617`。R303独立审计确认十笔Native终态均闭合且由该Save覆盖；无在途或unknown。external `10/10 FROZEN`、lifetime `330`，普通写入仍冻结。
- 当前完整工厂为`6274 built / 0 prebuild`，相对前一封存帧无新增、删除或静态差异。核对覆盖remote source对象169、Ti对象254；29种物料、147条Native路径、16台燃料机，电网点读224节点/563消费者/134发电机且full-serve。分组库存样本保留各自tick，不跨tick求和。
- J100旧记录完整保留；J101首次手工制作PLS `2103`，J102首次手工制作ILS `2104`。当前ILS `2104×1`已备齐，但剩余便携套件和后续资格未完成。
- 已安装源码cohort仍为`863d35546f6cb49fcdaec5b5814869d1e13af42b`，游戏版本`0.10.35.29104`；当前文档基线源码HEAD为`c31c53d8a77461d234f334d0892e9fa3b7cf7101`（CI `37749806589` success）。

## Gate 2 边界

- R303独立十写审计：`50a28d54f0d548cbb44096abb7995f2a:1`，SHA-256 `DEB6F794051CAAA0AC6A8FFAC675699A09A1BC924FB020A0100E85FA75764F11`。当前完整快照：`56c41569f4564b2cb0aa2aa1650f500c:82`，SHA-256 `2265CD4BD76E5C98FEE27C9763B24AA5F1B4C10140BA972A0D06D6DDAA1B0F01`。这核销本窗，不代表整案供需通过；continuous credit为`0`，`wholeSupplyPassed=false`。
- R299调用方因固定J100断言报告`passed=false`，R301仅核对原回执的自然追加并关闭原Save intent；保留R299原失败，不改写为业务失败或新成功。此前R220、R250、R273、R274、R280等调用方边界也按原记录保留。
- H/D/Fe自动共享来源、完整便携套件、整合保存恢复、持续至少36,000 ticks及最终同SHA双候选包仍未验收。Ti路径已纳入完整观测，持续供給及共享平衡待长窗核验。既有蓝图生命周期与Governor 2×门仅保持先前通过的范围，不重开或扩大；整案仍为`executable=false`。完成本窗文档提交与准确SHA绿CI后，root按持续目标授权重开并继续。
