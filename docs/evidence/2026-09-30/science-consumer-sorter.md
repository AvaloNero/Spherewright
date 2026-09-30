# 科研消费者分拣器原生附件读回

日期：2026-09-30（Asia/Singapore）。记录紫糖消费者端一次分拣器配置与连接读回；本次未保存，不证明紫糖实际送达、整线持续供料、科研吞吐或重启恢复。

## 唯一accepted动作

run `b72b179587f54856ba235c0362ed840e`：ord5 prepare、ord6 intent、ord7 commit；action `50860492-6692-447b-86ca-f186eb78678c` 在ord26 `completed` / `succeeded=true`，终态tick 80572781、开始tick80572094，共687 game ticks，`receiptWallMs=33157.064`。

新建entity5199（building item2011），filter=6004、pick=5197、insert=84；网络3在本次读回ratio=1。source5197 slot4↔5199 slot1，以及5199 slot0↔Lab84 slot2，新增共四条有向互返边；其余既有边及静态字段保持。独立预检的原生接口现已接通，但没有证明物料已到达Lab84。

背包ord2→ord30中item2011为2→1，其余15种物品数量与累计增量均不变。ord31 fresh session：tick80572803 / R41、同一owned planet104、healthy；`lastSaved`仍为80526460。ord30玩家为Walk、speed 0，3架无人机alive（2 idle、1 returning）、0 pending；动作已成功终态，返航状态不改变结果。本run没有fresh Journal；J96 durable仅沿用此前只读run `062cd8fce1024bbf805f67a0381da62a` ord3的观察。

root核验该施工run共21条索引记录：1个唯一accepted且终态齐全，0 replay、unknown或未决intent。当前accepted **7/10**、无在途；本次附件尚未保存。单次ratio=1不表示持续供电。

## 下一唯一阻塞

消费者端slot已接通；下一步是核验源3051出口与一段独立空belt，并验证向5198的完整中间输送。尚无紫糖实际进入、完整流路、持续科研或保存后恢复的证据。
