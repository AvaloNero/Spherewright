# Rod carry与七写审计（R320）

日期：2026-10-08（Asia/Singapore）。本页记录R315有限七写阶段的保存与独立审计，不复制私有回执。

R320独立审计核销R315七笔唯一Native prepare→intent→ack→terminal→readback动作，全部由normal Save `101620229`覆盖：审计`abd052817c8a4eeaab5c48c9240971de:8`，SHA-256 `CD0CF60C4B4F9B04AA164558FC29782472176F5779EF45C0AC121CA4729FC6FE`；原writer `f92d90d1dbb249af95bb77595db72d40:208`，SHA-256 `DF307F3A7404E826C21C51DA7D690A8CF2A49D5B0DA8EE1AE77CBF68D29CDB79`。当前R41/J102、external `7`、lifetime `347`，无intent、unknown或在途；固定20写窗口尚余13笔，后续阶段仍须预留普通Save槽位。

玩家库存净增Rod `1802×5`且inc不变；仓储Native数量同期`5→0`。分拣器3956和4386临时切至filter `1107`，之后恢复filter `1802`，端点与静态配置均保全。正常Move用时246 game ticks（`101606342→101606588`），最终目标偏差不超过1.6。核心储能`1.6 GJ / 1.6 GJ`，五根Rod按`600 MJ`计为`3 GJ`。

本次等待`13267` ticks / `309` wall seconds；原过滤暂停累计`13489` ticks（`101606652→101620141`），低于声明预算`21600`。原样本中四处fusion燃料缓冲与loaded余量足够覆盖固定剩余暂停并额外留一根Rod的估算；采样点loaded电力均full-serve。该估算只针对已声明的暂停，不证明自动补给或长窗稳定性。

R320相关核对覆盖173个受影响对象与6条完整Native Rod路径；本阶段没有建造/拆除调用，这些对象静态保全，未做全厂fresh快照。无建材支出或额外额定功率。R312此前记录对象2440丢失铜节点164，并按自然矿竭表现分类；详见[R312事件](departure-small-kit-ten-r312.md)。该分类不确定精确耗尽tick，也不认证长期铜产能。

当前便携基础库存含ILS `2104×1`、物流运输船`5002×1`、矿机`2301×1`、电塔`2201×2`、风机`2203×2`、belt `2001×30`、普通分拣器`2011×2`、Warper `1210×30`和Rod `1802×5`。完整清单的速率、运输与供电余量，自动Warper/Rod双补给，共享来源与副产物、16台旧燃料机及远端Ti的联合验收，连续至少36,000 ticks、整合保存恢复和最终同SHA双候选包仍未通过；continuous credit为`0`，`wholeSupplyPassed=false`。
