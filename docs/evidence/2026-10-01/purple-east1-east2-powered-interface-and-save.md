# 紫糖 East1—East2 分拣器连接与正常保存

日期：2026-10-01。本事件记录一只分拣器的原生连接、实际供电读回及保存；不代表上游仓库送料或持续生产。

## 分拣器施工与读回

施工 run `e079699701794f7083ef1b6624084b28`：prepare ordinal 5、commit ordinal 7、terminal ordinal 14。action `75281886-595f-42f7-96e3-e91a343be824` 于 tick **82657257** 完成。新增 `5314`，item `2011`、filter `6004`；实际端口为 `5266` slot4 → `5314` slot1，以及 `5314` slot0 → `5284` slot4。两端旧邻居 `5265` 与 `5286` 保持；新增4条有向互惠边。sorter 读回 `powerNetwork=3`、`ratio=1`，这是本次采样点读数，不是持续供电或燃料证明。

玩家 `2011` 从4减至3，`2001` 保持260，其他库存增量不变，J96保持连续。root proof `fc943985eeb5462fa49f83c9c63ba0a3` ordinal 1，SHA-256 `A6F9418262745967FD376705489AAB2F50DF884930B54FD82CA4EF29DEC6A541`，独立核对原计划、同 action 终态、设备端口、旧结构、背包与 Journal。

## 正常保存

保存 run `16e2476b466648cfb0c345688dcc0ee1`：Journal 前读 ordinal 3、commit ordinal 6、terminal ordinal 7、fresh session ordinal 8、Journal 后读 ordinal 9。action `3b984bd3-f961-41cb-a8b3-6cc45c6a076d` 正常保存于 tick **82658561**；后读 tick **82658575 / R46**，J96的96条记录 exact durable、无 pending/error，`resumeAvailable=true`。上段所引 root proof `fc943985eeb5462fa49f83c9c63ba0a3` ordinal 1 也核验了这组保存回执；此保存覆盖电塔 `5313` 与分拣器 `5314`，但 `resumeAvailable` 不等于真实重启验证。

外部写窗口从5升至 **7/10 OPEN**，无在途。最近只读状态为 **82658575 / R46**，最近保存为 **82658561**。真实重启尚未进行。

## 上游供料仍未闭合

只读检查 run `f36fb22fce7d4a74bc020a488fc22c13` ordinal 1 显示 storage `3051` 的 item `6004` 为1824、slot4输入来自 `5113`、无输出；ordinal 2 显示 `5202` 只有 slot0 向 `5201` 输出，无输入且 cargo为0。它们不是仓库到主干的供料证明。唯一下一接口是 `3051→5202`；root仍在设计 `5202` slot1 原生方案（约2.596m、facing dot 0.999991），尚未 prepare 或 commit。紫糖到货、科研推进与持续产量均未证明。
