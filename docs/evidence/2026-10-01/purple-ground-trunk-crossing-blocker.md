# 紫糖主干地面跨线占位阻塞

日期：2026-10-01（Asia/Singapore）。本事件记录一次受保护只读预检的原生拒绝及已核静态基线；不构成成功路线、分拣器附件或可施工方案。

## 原生拒绝与边界

预检run `9b0b480da23645a5b364d2df61383220` ord1–6及summary ord7；ord5为固定的独立NEW候选。root核原始error为 `BUILD_LOCATION_INVALID`：planned point 7与旧实体1299（item 2001 belt）占位冲突，完整NEW端点占位约束（>0.25 m）未通过。无对象创建、无commit、accepted保持1/10，无在途。响应没有成功plan，不能据此声称routing、full stage 1、预算或sorter attachment通过。

候选点Q为 `(-133.464014689,-59.467723649,-136.861194745)`，绑定目标5198；待验证的 `5205→Q` sorter连接仍未证明。另一旧地面候选run `af819fc5861742c9ada6a731a708ce17` ord5是与实体1932的独立碰撞，不能与本次1299混为一个位置或重试旧点。相同occupancy类型的两个新地面候选均为零写；执行者已停止。不可盲试第三次、移除绑定端点或拆除成功前缀。

root复用既有完整快照run `6207d5a744894c11b60f654163aee38b` ord2–54确认：1299是位置 `(-127.317413,-59.46772,-142.596985)` 的旧竖向belt，静态链为 `1298→1299→1300`。该快照不证明当前动态cargo；本次没有新增游戏采集。

预检开闭session相同：owned/healthy、版本29104，tick **81078735→81078767/R4**，`lastSavedTick=81057002`。J96 durable仅来自此前正常保存run `472da18b646a4a5dbf1e7d643c139cdf` ord9；本预检run未重读Journal。

## 下一边界

root重新设计前，先审查已有 `native_elevated_grid` 有界跨线方案及未来实际sorter接口；不新增原语。本记录不批准施工，也不宣称该绕行可行。
