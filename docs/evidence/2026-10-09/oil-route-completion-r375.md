# 油路绕行施工完成与 R375 核验

R373 原件 `0a9d8415d3cd4bf29f1833f92210e71e:119`（SHA-256 `E29C8A1DF54E739CEA9D81803C66494084F2EF46266C444DF19DC92B8D6C7A03`）记录4笔accepted、101个请求、127.70秒：完成最后7条belt、两个filter `1007` 的接入sorter，并正常保存。sorter `6315`（`2011`）连接belt `6314`与旧消费者`2804`；sorter `6316`（`2012`）连接旧`6063`与新带头`6275`，完成source侧接入。普通Save为`102506542/R86/J102`。

Root 的 R375 独立审计 `0cad7503a7e74faa957b1ea99ca1ca38:1`（SHA-256 `D8E94A65D04EBBF56E0CE8FA054B5DD4B6F4C8ED68E286D985BCF2A4590A2A80`）核对原Native prepare、intent、ACK、终态、逐动作材料扣除、全库存/inc/held、两处仓库即时扣款与自然流量边界、供电以及保存覆盖。完整绕行共40条新belt、39条互惠连接，实际Native点与声明一致；旧16个相关实体及前缀静态状态仅有批准的接边变化，原生保留旋转通过核验。新增两个sorter均本地满供。施工全部由正常Save覆盖。

当前为R86、Save `102506542`、durable J102、external `16/20`、lifetime `375`，无在途或unknown并冻结。玩家库存为belt `2001×32`、basic sorter `2011×1`、fast sorter `2012×2`。R361修复范围最多40条新belt与2个sorter，现已达到；其accepted动作上限22笔也已耗尽。外部20写窗口仍未闭合。R355的旧19/20窗口单独闭合，不转移余额。

本次审计确认的是建造、接边、供电和Save覆盖，不是实际到下游`707`的物流或持续生产。R366先前caller等待超时仍由R368只读核销原动作，未重放。R323/R324来源条件失败及R344诊断保持不变；无新增continuous credit，`wholeSupplyPassed=false`，整案仍为`executable=false`。完整到料、双自动补给、连续至少36,000 ticks、整合保存恢复和最终同SHA双候选包仍未通过。
