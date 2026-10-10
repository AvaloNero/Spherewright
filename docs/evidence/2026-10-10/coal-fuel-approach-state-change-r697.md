# 煤矿接近与库存变化观察（R692–R699）

本记录核销一次短程移动、记录后续采煤候选拒绝及一次未执行保存的只读前置检查。它不构成采煤、加燃料或持续供给通过。

R692普通Move按28 m目标及5 m到达容差完成，root R694核销唯一原动作成功、tick `105914332`、物品无变化。writer原件 `b0ba0f0ea12f4a078697c6ad219b854e:52`，SHA-256 `F0B4981845B93A0BDAC82AD8AE3C56909923F3CD44E1A8DFB5E2738DA8364A08`；root R694 `73d61ea7ff2d4270a09608488738d0d2:1`，SHA-256 `64AC9C64CAD6B38D7E17848E4D5B7896E03B588AF304B1DC413322B36D332881`。

随后Harvest prepare返回Native `STALE_STATE`，0 commit、0 accepted；没有采煤、加燃料或Save。R695 Save-only入口在三次只读请求后因玩家石矿数量与调用方基线不同而停止，0 prepare、0 commit、0 accepted。writer原件 `3f94f2fd4bc34bfa8ac8ded71c37518f:6`，SHA-256 `5B185367F954CB0E2872B1B87B3B4B2C29B8EF47588CB395597EB3D2E3DF1F6B`；root R697 `13ba2054ce1c486f90e632f010eca814:1`，SHA-256 `699A91D5EDFF674675C099649B37FD6AB98802D89B5A20C79CA9A8813439A272`，独立核对完整背包、增量、held及位置。

R697观察tick `105944276`：只有石矿1005由0增至6、inc为0，held及其他背包物品不变；玩家位置已变化、Walk速度为0、CoreEnergy为`14,505,333.3333354 J`，fuel与reactor为0。Walk速度0不证明已在陆地停稳，石矿增量来源也未明，需确认是否来自手动操作。R690预览中的多数候选位置位于水下。旧orthogonal 4 m移动候选族已有两次拒绝并退役；采煤候选只有一次Harvest prepare拒绝，不能与旧候选族合并称为第二次拒绝。

当前为P104/R26、正常Save `105708553`、durable Journal 102；固定20窗口external `1/20`、整案lifetime `435`。R692成功Move尚未被保存覆盖；当前无在途或unknown，新写冻结。已有R682/R683铁矿至磁铁差异仍未解决；wholeSupplyPassed为false、continuous credit为0，Warper/Rod各至少1/min及连续36,000-tick等验收门保持不变。

R698 writer原件 `c1df7c272fc94d6fb9e81b79f5d52ff9:74`，SHA-256 `D6E83AA5D3F6129B0A1441299488D248451CFBC26F9B60092AFEA8BD2F47E085`，73次只读请求、0 accepted、无prepare或commit。root R699 `13138399745b4a0b96c5aa965d9fa347:1`，SHA-256 `8C62304C57CE4EEE7927D26F5F2891459B501CEA0B76E2B972DF0C100BC8BCCE`，独立核对完整65页、6475 built/0 prebuild；相对R672拓扑无新增、移除、静态变化或非互惠边，P104本地Native网络成员及容量保持且满供。煤点438剩余58069、minerCount为0、资源hash不变，距玩家61.145658548078586 m，未采煤。两次玩家书挡Walk速度均为0，位置与R697石矿6件读回一致；末次观察tick `105947358`、CoreEnergy `18,614,666.66666808 J`、fuel与reactor为0。这些读数没有解释石矿增量来源，也不证明dryland。

下一步先核清石矿增量是否来自手动操作，再按计划fresh执行普通Save及燃料/供料接口预检；本次全厂只读结果不放开冻结。R682/R683定位的铁源分流问题及wholeSupply、continuous credit、Warper/Rod各至少1/min和连续36,000-tick等验收门仍未通过。
