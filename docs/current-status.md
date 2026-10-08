# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。R323联合供需长窗与R324独立核销见[本阶段事件](evidence/2026-10-08/joint-supply-long-window-r324.md)。前一轮观察缺口仍保留在[R321/R322事件](evidence/2026-10-08/joint-observation-gap-r321.md)；更早的Rod carry与七写阶段见[R320事件](evidence/2026-10-08/departure-rod-carry-seven-r320.md)。

## 当前 owned 状态

- 固定20写窗口仍为external `7`、lifetime `347`、R41；普通Save `101620229`/J102保持。R323完成只读连续观察，0 Game writes、0 accepted；双边玩家/J102读回一致，owned和平、非沙盒1×、healthy。无intent/unknown，窗口冻结，未重开。
- R323在同一健康区间采得71个样本、连续`36,316` game ticks、reset `0`，共1,032次Native读取、总耗时约`1085.39`秒。pairwise-disjoint计数下Warp `1210`保守下界为8件（`0.793038880934/min`），Rod `1802`为10件（`0.991298601168/min`）。这是下界证据不足，不能推成实际产率低于1/min，也没有达到声明目标的验收要求。
- 29物料池中，`1109`由`1614→1562`（−52，允许3）、`1120`由`5382→5340`（−42，允许10）、`1203`由`2467→2452`（−15，允许2）；接通D的独立H池`46→46`，排除legacy H混计。P102钛石`1004`与P104钛块`1106`输出均为0，但原矿机output读数为50、钛熔炉output读数为100且上下游库存满；不能据此判断矿竭或容量不足。远端节点剩余量仍未知。
- 采样点电力均full-serve，原静态与完整路径保留；R324判定source conditions未通过，continuous credit为`0`、`wholeSupplyPassed=false`。下一步先按本次原件定位三个库存下降及计量局限，只fresh检查相关生产端接口，再决定最小修复与新的有界长窗；不做无目标全厂重采。
- 执行源码基准仍为`e5d95d34297a468e11d61509d03f04a38c70aaf4`（Windows CI `37763171765` success）；已安装二进制仍为cohort `863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R324独立审计：`0ea664292d784e6aa7ed9dd6c6ab0922:20`，SHA-256 `EEBF1E979B48674B09B75270DE7AA46ABF6C305C296620376C63DE32E3225FB6`；审计完成，但`sourceConditionsPassed=false`。R323原记录：`8e075abd2cc74a068c98e54bce586984:1–1145`，SHA-256 `C73B3337A99E26C0547FCACD3483F96F043A02C7BE93EF55DBFE4AE820098CD8`；caller SHA-256 `A4C525E6254D7FEF56635A25B00480318F557156EE70CEE7A0A21BD44EC112DF`。
- H/D/Fe自动共享来源、双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收；既有蓝图生命周期与Governor 2×门保持先前通过范围，不重开或扩大；整案仍为`executable=false`。
- 既有阶段证据仅保留原件链接，不在当前快照重述旧流水：[Gate 2生产证据](evidence/2026-10-02/warper-automatic-source-and-build.md)、[R282](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)、[R303](evidence/2026-10-08/departure-ils-ten-r303.md)、[R312](evidence/2026-10-08/departure-small-kit-ten-r312.md)、[R320](evidence/2026-10-08/departure-rod-carry-seven-r320.md)、[R321/R322](evidence/2026-10-08/joint-observation-gap-r321.md)。
