# Spherewright 当前快照

更新：2026-10-11（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`（Windows Core CI `37992232537` success；2,762 项测试、228 个文件、64 个 MCP tools、1 个 resource）。当前有效的完整来源观察为 R778，独立审计见 R783；来源条件未通过。

## 当前保存与窗口

- 最近已核验保存仍为 R770 的普通 Save `106238371`，durable Journal 102。固定 20 写窗口从 lifetime 454 开始，external `7/20`、整案 lifetime `461`；无在途或未知动作。窗口累计与保存边界均未重置。
- 当前运行 cohort 与已安装二进制未因来源观察变化；本阶段只读，0 writes。R785 仅列出缺件预算，尚未执行，也不代表出发准备完成。

## 完整来源观察

- R778 的原声明区间为 tick `106254705–106290704`。108 个样本覆盖连续 36,087 ticks、reset 为 0；共 2,644 次只读请求、3,372.129 秒、0 writes。未延长、拼接或重跑原窗口。
- R783 审计确认 Warp 下界 `4.5/min`、Rod 下界 `1.8/min` 达标；P102 Ti Ore（1004）与 P104 Ti Ingot（1106）均为 `0/min`，来源条件未通过。完整覆盖包括 29 项物料、当前 24 个正剩余 Native 资源节点及 3 座已加载工厂的满供电；连接氢变化 `+3` 达标。
- 六项库存变化超过单批容差：1109 `3452→3404`（−48，容差3）；1101 `9205→9039`（−166，容差3）；1102 `3263→3238`（−25，容差3）；1106 `683→675`（−8，容差4）；1007 `814→811`（−3，容差2）；1127 `106→73`（−33，容差1）。Warp/Rod 速率通过不能抵消这些库存失败。

## 下一步与验收边界

- `wholeSupplyPassed=false`，continuous credit 为 0。Root 将基于封存原截面诊断六个亏损池及钛源/运输，再批准一个有界 fresh source-route-sink 读取；这是持续授权内的工程工作，无需新增用户授权。
- Warper 与 Rod 各至少 1/min、连续 36,000 ticks、双自动补给、完整来源竞争与材料供需、整合保存恢复及最终同 SHA 双候选包验收仍未通过。不得拼接有缺口窗口或降低门槛。
- 已通过的蓝图/Governor 等历史门保持有效。R508 双包检查仅为旧源码 SHA 的离线预检，不是最终候选、实际 Mod Manager 安装或发行验收。

阶段索引：[R778 连续完整来源观察与 R783 审计](evidence/2026-10-10/current-full-source-long-r783.md)、[R770 Warp 取料与此前来源截面](evidence/2026-10-10/warp-demand-and-current-source-r777.md)、[R766 Warp 携带量与保存核销](evidence/2026-10-10/warp-carry-approach-and-save-r766.md)、[R763 窗口关闭和出发材料截面](evidence/2026-10-10/rod-carry-and-window-closure-r763.md)。
