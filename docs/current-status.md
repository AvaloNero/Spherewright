# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖R320阶段保存与独立核销；详见[Rod carry与七写审计](evidence/2026-10-08/departure-rod-carry-seven-r320.md)。R312关于铜节点164自然消失的观察保留在[便携套件与十写封窗](evidence/2026-10-08/departure-small-kit-ten-r312.md)，更早窗口见[R303](evidence/2026-10-08/departure-ils-ten-r303.md)和[R282](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。

## 当前 owned 状态

- R320核销R315的7笔唯一Native动作，均由normal Save `101620229`覆盖；当前R41/J102、external `7`、lifetime `347`，无intent、unknown或在途动作。固定20写窗口剩余13笔；后续阶段仍须预留普通Save槽位。
- 玩家库存净增Rod `1802×5`，inc未变；储存端Native数量同期`5→0`。分拣器3956、4386暂时使用filter `1107`，之后恢复`1802`，端点及静态配置保全。
- 正常Move耗时246 game ticks（`101606342→101606588`），最终相对目标偏差不超过1.6。核心储能为`1.6 GJ / 1.6 GJ`；5根Rod按`600 MJ`计共`3 GJ`。本次等待`13267` ticks / `309` wall seconds；过滤暂停累计`13489` ticks（`101606652→101620141`），低于声明预算`21600`。原样本中四处fusion燃料缓冲与loaded剩余量满足固定暂停及额外1根Rod估算，采样点loaded电力均full-serve；这不构成长窗保证。
- R320相关核对覆盖173个受影响对象与6条完整Native Rod路径；本阶段没有建造/拆除调用，这些对象静态保全，未做全厂fresh快照。无建材支出或额外额定功率。R312最后一次完整工厂快照为`6274 built / 0 prebuild`。已备便携套件包括ILS `2104×1`、船`5002×1`、矿机`2301×1`、电塔`2201×2`、风机`2203×2`、belt `2001×30`、分拣器`2011×2`、Warper `1210×30`，现在另有Rod `1802×5`。
- 当前执行源码基准为`e5d95d34297a468e11d61509d03f04a38c70aaf4`（Windows CI `37763171765` success）；已安装二进制仍为cohort `863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R320独立审计：`abd052817c8a4eeaab5c48c9240971de:8`，SHA-256 `CD0CF60C4B4F9B04AA164558FC29782472176F5779EF45C0AC121CA4729FC6FE`；原R315 writer：`f92d90d1dbb249af95bb77595db72d40:208`，SHA-256 `DF307F3A7404E826C21C51DA7D690A8CF2A49D5B0DA8EE1AE77CBF68D29CDB79`。该审计只闭合本窗七笔，不代表整案供需通过。
- H/D/Fe自动共享来源、双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收；continuous credit为`0`，`wholeSupplyPassed=false`。既有蓝图生命周期与Governor 2×门保持先前通过范围，不重开或扩大；整案仍为`executable=false`。
