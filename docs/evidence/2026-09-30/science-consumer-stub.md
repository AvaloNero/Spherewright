# 科研消费者短带施工与原生读回

日期：2026-09-30（Asia/Singapore）。记录一次短带施工成功终态、caller在后续玩家读回处的DTO包装误用，以及独立只读快照。短带尚未保存；不代表分拣器附件、紫糖整线、科研供料或重启恢复已通过。

## 施工与成功终态

run `bacc806ec604461491e462b5ad5e399f`：ord5 prepare、ord6 intent、ord7 commit；action `2da4f06d-abb0-4c0d-a68c-eeb98c97a043` 经ord8–26 polls，在ord26 `completed` / `succeeded=true`，终态tick 80538928，开始tick 80538234，共694 game ticks，`receiptWallMs=35220.356`。

建成两段belt：5198远端→5197近端，位置与此前原生预检方案相符。ord27/28分别读回各新段仅一条互返连接；带头、带尾空，无隐式外接。Lab84 ord3→29与紫糖分拣器5103 ord4→30的静态字段及连接保持。背包item2001由372降至370，其余15种物品数量及增量均保留。

commit终态已经成功，但入口随后在ord31玩家读回将`Wait-SpherewrightPlayerSettled`返回包装误当作PlayerState DTO，故命令exit1。root已按真实契约补充函数注释和入口索引：先检查`.settled`，再读取`.player`；这是caller读回修正，不改变公开方法行为，也不撤销accepted。**不得重跑施工动作。**

## 独立只读快照与写窗口

run `062cd8fce1024bbf805f67a0381da62a`：ord1 session为tick 80548608 / R39、同一owned planet104、healthy，`lastSaved=80526460`；ord2 player为Walk、speed 0、core energy 800 MJ、3架无人机idle、0 working/0 pending，库存有370 belt与2 sorter；ord3 Journal J96 durable、无error。ord4为功率采样：network3容量1822000、需求408546、ratio1；network4容量10000、需求1800、ratio1。仅为单次采样，不代表持续供电或满基础负载。

root核验施工run的21条索引记录：1个唯一accepted且有成功终态，0 unresolved、unknown或replay。当前窗口accepted **6/10**，无在途或未决；5197/5198尚未保存，最近保存仍为80526460。未证明重启恢复、sorter attachment、完整紫糖输送或研究吞吐。

## 下一唯一阻塞

先对近端5197→Lab84 slot2做最小原生分拣器附件预检。此前run `0abcbd57d7544a8aaba610ca788cf916` ord5仅证明面向消费者的两个`native_grid`连接（2001×2）可放，0 commit且plan token已丢弃；它不是实际附件或整线通路证据。
