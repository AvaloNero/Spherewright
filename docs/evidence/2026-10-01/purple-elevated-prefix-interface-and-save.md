# 紫糖高层四带前缀、分拣器接口与正常保存

日期：2026-10-01（Asia/Singapore）。本事件分别核销成功的四带高层路径、其后被几何预检拒绝的sorter接口，以及正常保存；不证明紫糖供给、跨层附件或重启恢复。

## 四带高层路径

施工run `1af813c9e5104569ac6c21752e35bd61` ord6为fresh prepare：`full_path_stage1/native_elevated_grid/L1→L1`、无covers、NEW4、2001×4；ord7 intent、ord8唯一accepted。action `f26f8f32-3f5e-4775-9d33-c25ba150e1a3` ord15 `completed/terminal/succeeded`，tick **81175279**。ord16–19核实路径`5206→5208→5207→5209`：两端自由、内部边互返；4点相对批准的native几何误差≤0.001m。旧实体5205/5198姿态、配置和连接保持。完整玩家库存只将2001从364减至360，所有inc不变。ord22回读为Walk=0、标量`pendingBuildTargets=0`、`working=1`；保留原值，不能改写为`working=0`。

临时caller基线曾多算一条连接，摘要`[2,3,3,2]`与原始native数据`[1,2,2,1]`不符；root核原回执后确认这是摘要计数误差，不是施工失败，不重放成功动作。

## Sorter几何预检未通过

独立预检run `66a6b018a98c479faffd2a12bded26fe` ord5对`5205→5206`、filter6004返回`BUILD_CONNECTION_INVALID/no_facing_interpolated_pair`。16次exact attempts均未调用DSP placement validator：`nativeChecks=0`、`lastNativeRejection=none`、`lastPreNativeRejection=TooSkew`、`bestFacingDegrees=20.704`（要求<11°）。这是原生放置检查前的几何拒绝，**不是**已执行`CheckBuildConditions`后的native拒绝。无成功plan、commit、token重试或accepted；该边和实际送达仍未证明。

合法的高层belt不证明sorter能升层。不能凭距离合规、把端点挪近或放宽角度重试；任何新坡道和同高度/跨层sorter接口都须分别fresh证明。成功的四带和既有前缀保持，不盲试第三个地面候选、不省略端点、不拆除已建对象。

## 正常保存与当前边界

保存run `8b55f9337ce34fda89d5b2a1e784518b` ord6为唯一accepted、无replay；action `90368aa0-e6e8-48fc-a439-295a9f0a657a` ord7完成正常保存，tick **81208004**。ord8为同session `41828547-0873-415e-a6aa-b90cbead076a`、版本29104、owned/healthy、`lastSavedTick=81208004`、restartAvailable；最新观察**81208018/R7**。ord9为J96 durable、无pending/error；root核96条事件与前一保存run `472da18b646a4a5dbf1e7d643c139cdf` ord9完全一致。

外部写窗口当前**3/10**，无在途或未核销。四带已保存，但未证明5205→5206 sorter附件、实际紫糖入料/持续科研或从本次保存重新恢复。`docs/agent-playbook.md`新增经验是仓库源码指南，尚未部署为新的MCP资源。
