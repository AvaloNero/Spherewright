# Spherewright 当前快照

更新：2026-10-05（Asia/Singapore）。本文件覆盖当前状态；本轮唯一权威阶段证据见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。

## 身份与运行边界

- 本次业务源码基线 `eec562b`，Windows Core CI `37227656604` 成功；已安装 cohort 仍为 `b1557bb`（4 Plugin+224 MCP文件匹配），native `0.10.35.29104`，64 tools/1 resource。文档提交不改变运行中的程序集。
- 现有1210链启动、正常保存、protected restart及恢复后再次非零输出正例已过；这不等于全源配平、有限缓存排除或持续供料通过。当前 owned primary 观察 `93424297/R1`，正常保存 `92128739/J100`；Journal `pending=false/error=null`，第10写门仍保留。external accepted `9`、lifetime `149`。无未保存的accepted动作；保存后的自然生产不算已保存进度。所有本轮执行句柄已终态，无unknown/in-flight；新写仍blocked。DSP/Steam保持运行；用户取消的Host存活专项测试未做。

## 本轮只读结论

- 旧5945工厂静态trace显示`3073→3076→3074`重氢共用仓，候选支路经分拣器`3405`到燃料棒装配机`3403/r41`，以及经`5736`和既有输送到`5326`。这是旧快照拓扑，不是本次全路径fresh资格；`3405`是分拣器，不是燃料设备。
- 同tick fresh cut `5ec5e4c6…` 仅读24个明确对象、9读/0写：`3073`氢5（低于10）、`5326`重氢5（低于10）、`3403`重氢8（低于20），当帧均idle/full power；`r41`另两项输入非零。单个600 tick窗口读到氢P/C=11/6（含循环）、重氢0/0、燃料1802为0/0、1210为1/0；不能据此归因单机或证明持续供料。
- 以氢为燃料的火力发电机`2516`观察发电3932 J/t，但燃料库存未知。N3当帧full-serve，容量1,806,000 J/t、需求196,774 J/t；J/t不是燃料件数或可持续供给证明。
- `4171→5523`首次候选只读资格在native拒绝：`BUILD_LOCATION_INVALID`、`belt_destination_cover_unsupported`、`belt_join_requires_empty_open_independent_path`，不等于几何/距离结论。后续共享三段资格在第一次A预览以`planned_endpoint_TooSkew:source`终止，`nativeCheckPerformed=false`，H/D未prepare。不得把两条负例当通路正例，不微移同候选或重放。

## 未闭合项

- 计划目标为1210至少1件/分钟、重氢10/分钟、氢20/分钟；它们是目标值，不是本轮实测供给。旧燃料1802的实际需求率仍未确定；氢循环毛产量不能当净氢供给。
- 此前连续窗仅27378 ticks、1210精确产出4，低于该窗最低要求8，且有155tick gap，未达36000tick验收；不与本轮单个600tick窗拼接。
- 本轮不证明完整源分配、有限缓存排除、持续速率或完整Gate2；整案仍false。下一步只核氢/重氢共享分配、真实源预算与源端原生朝向规则，再评估最小修复。候选失败后不施工、不盲搬料、不重复提交。
- 最新只读观察 `93424297/R1`，保存仍为 `92128739/J100`，external/lifetime不重置。DSP/Steam不因Codex退出或对话结束而关闭；没有进行专项存活测试。
