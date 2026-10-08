# 联合供需连续长窗与独立核销（R323/R324）

日期：2026-10-08（Asia/Singapore）。本事件记录一个连续观察窗口、其独立物料核验和未通过的来源条件；不把下界不足改写为实际产率不足，也不将本轮只读观察视作供给通过。

## 观察与核销

R323原记录`8e075abd2cc74a068c98e54bce586984:1–1145`，SHA-256 `C73B3337A99E26C0547FCACD3483F96F043A02C7BE93EF55DBFE4AE820098CD8`；caller SHA-256 `A4C525E6254D7FEF56635A25B00480318F557156EE70CEE7A0A21BD44EC112DF`。同一owned、和平、非沙盒1×健康区间内完成71个样本、连续`36,316` game ticks、reset `0`，总计1,032次Native读取、约`1085.3903517` wall seconds。19份原始capture包含18次采样实体观察和1次收尾读回；没有拼接缺口窗口。所有Native读取成功，0 Game writes、0 accepted，Journal与玩家双边读回一致为J102；仍是R41、Save `101620229`，无intent/unknown。

R324独立核验`0ea664292d784e6aa7ed9dd6c6ab0922:20`，SHA-256 `EEBF1E979B48674B09B75270DE7AA46ABF6C305C296620376C63DE32E3225FB6`，标记`auditCompleted=true`、`sourceConditionsPassed=false`。pairwise-disjoint计数的保守下界为Warp `1210` 8件 / `0.793038880934/min`，Rod `1802` 10件 / `0.991298601168/min`。这两个下界不足以证明达到至少1/min；只能说明证据不够，不能据此断言实际产率低于1/min。

## 来源条件与边界

29物料池有三项超过允许差量：高能石墨`1109`由`1614→1562`（−52，允许3），氢`1120`由`5382→5340`（−42，允许10），电动机`1203`由`2467→2452`（−15，允许2）。接通D的独立H池`46→46`，已排除legacy H混计。全程采样点电力均full-serve，静态配置与完整路径保留。

Native对P102钛石`1004`和P104钛块`1106`的观测输出均为0；同时原矿机output读数为50、钛熔炉output读数为100且上下游库存满。该组合不支持判定矿竭或容量不足。17个本地有限资源节点前后均有Native正例；远端节点的逐项剩余量仍未知。物料趋势和rate lower bound使source conditions未通过，但没有给出单一根因或修复证明。

当时固定窗口仍为20写，external `7`、lifetime `347`、R41、Save `101620229`；本轮未accepted、未重开窗口，写入继续冻结。后续应先用封存原件定位差量与计量限制，仅fresh读取相关生产端接口，再选择最小修复并声明新的有界长窗。连续至少36,000 ticks、联合供需、双自动补给、整合保存恢复及同SHA双候选包仍未验收，continuous credit为`0`、`wholeSupplyPassed=false`。
