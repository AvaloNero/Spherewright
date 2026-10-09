# 有限机甲燃料补给与北向接收端资格（R623）

日期：2026-10-10（Asia/Singapore）。本页记录 R615、R618、R621 的有限读写与 R623 的 Native prepare-only 预检，不把燃料缓冲或施工资格当作持续供给验收。

## 燃料与保存状态

R615 独立审计核实：前序 R614 只有 3 次只读请求、0 prepare；Walk 为 0、核心能量约 0.2967 MJ，未满足移动保留量。随后 R618 以一次唯一 accepted 将 20 个重氢（1121，9 MJ/个）从玩家转入燃料缓冲；玩家 20→0、缓冲 0→20，有限读回核心能量 42.85 MJ，低于 100 MJ 目标。R621 再以一次唯一 accepted 转入 5 个氘核燃料棒（1802，600 MJ/个）；玩家 5→0、燃料缓冲/反应堆为 4/1，核心能量读回 104.54 MJ。两次转入均未保存，当前最后正常主档仍为 tick `105305314` / R3 / J102，保存槽保持预留。出发清单要求的 5 根 carried rods 必须重新备齐，旧携带记录不抵扣当前需求。

当前固定 20 窗为 4/20、lifetime 422；R618 与 R621 各计 1 accepted。此前两笔为正常保存和健康恢复。两项燃料转入将全案 accepted 上限从 34 调至 36，当前全案累计 23 accepted，其中 refuel 两笔。无在途动作或 unknown。

## 北向接收端预检

R623 对真实实体 6350 的固定北向非拆除 empty-destination cover 共发出 8 次请求（7 次只读、1 次 Native prepare），0 commit、0 accepted。21 个采样点均通过，最远点距当时玩家 74.51 m；该结果只是接收端资格，不是移动或建造。读回期间 Walk 为 0，核心能量从 643.77 增至 644.704 MJ。

本次受影响切面覆盖 57 个空载开放接收成员与 94 个相关对象，旧路径和静态关系保持。矿机 5325 仍为 network 0、服务比例不可用（`null`）、空缓冲。没有新全厂快照；完整工厂引用沿用 R598 的 6,366 built/0 prebuild 封存基线。R624 固定五段北向施工尚未有终态，来源未接电、未获准作为来源。

## 验收边界

`sourceAdmitted=false`、`wholeSupplyPassed=false`、continuous credit 为 0。Warper/Rod 产率、连续 36,000 ticks、双自动补给、完整材料与来源供需、整合保存恢复及最终同 SHA 双候选包验收均未通过。R508 是离线包预检，不是实际 Mod Manager 安装或发行验收。

## 原证据索引

- R615 移动 reserve 只读资格：`578c526f18d8462aaef843091bba621f:1`，SHA-256 `E9E466AC31C8007FBFE83CDD12A37AD358C0EC716AD36FEB1A87F6928805235F`。
- R618 重氢转移：`b309537d8fe548d6912caf04e3a314d2:1`，SHA-256 `9BCEBCCAAE54317586C668C7446AB98C8E96A92F4316183ACF336452487F3A2F`。
- R621 氘核燃料棒转移：`f224089608c44f65b436c64033ddf76c:1`，SHA-256 `EB9840B26EC962C73A95FDF5D6852366FF4D6B17CE4D51F6298B32DFA380532F`。
- R623 北向接收端 Native prepare-only：`e996323e20284f44a4b4bd94a8812e3e:1`，SHA-256 `FD68FA8B97979A1A7021A1EDE4425FD8654B57EB5A9E5792CDAB90FF76D770C9`。
