# 第十写封窗：核心材料与工厂差异审计

日期：2026-09-30（Asia/Singapore）。本记录归档主会话独立核验的十写终态、完整工厂快照差异及材料读回；不含凭据、真实存档名或本机路径。本文是脱敏证据索引，不替代受保护原始回执。

## 十写计数与终态

外部 accepted 台账达到 **10/10**，新写保持冻结。受保护回执索引只读四个明确 run，共80条回执、10个唯一 accepted；replay、rejected commit、unpaired terminal 均为0，无 unresolved action/intent、unknown response 或 uncertain terminal。十个 accepted action 均有唯一终态 `completed` / `succeeded=true`：

| runId | actionId | commit→terminal ord | 类型 | 完成 tick |
|---|---|---:|---|---:|
| 8418a1e5bef240b5a436314ec877842a | 6a5f5827-387f-40f4-ae1c-496ac99d0fe8 | 9→10 | save | 80006447 |
| 8418a1e5bef240b5a436314ec877842a | 3ec210d7-ed63-475a-8800-73cdff814d44 | 20→46 | build | 80008003 |
| 8418a1e5bef240b5a436314ec877842a | 5daf7777-fc51-4da4-8f4c-255058888e8a | 59→85 | build | 80010505 |
| 8418a1e5bef240b5a436314ec877842a | 610a668d-a6be-46bf-8d82-2e08880fa2a0 | 99→100 | save | 80012591 |
| 394acad94a1f4c748cc7b3979d0e0c79 | 72065c44-0afc-4bcc-8eec-6cb727d28bfb | 7→8 | select_research | 80226568 |
| 394acad94a1f4c748cc7b3979d0e0c79 | 2af3af05-65db-46a1-b91c-01bde91c9a83 | 13→14 | save | 80226601 |
| eb17b3db04fa44deaabfebafb144e2fd | abd72651-afb7-4a66-9d6d-96c665e81a23 | 9→10 | upgrade | 80273779 |
| eb17b3db04fa44deaabfebafb144e2fd | 0b6bf765-8c84-4386-bbe1-d4623a9e8fdc | 17→18 | save | 80273796 |
| f322467c06ef4a738a6fb5720e757213 | 3102c920-b3d5-49d3-bcb2-8677fb27b747 | 7→8 | transfer | 80364922 |
| f322467c06ef4a738a6fb5720e757213 | 63daf0ec-7731-4d72-a0ad-f006e3e8f0bb | 18→19 | transfer | 80364935 |

索引的 `newWriteBlocked=false` 仅描述这些动作回执的配对结果，**不解除 10/10 外部冻结**。独立审计已完成；仍须本文件及相关文档 commit/push、精确 SHA CI 绿灯、root 明确交接后，才可开启下一写窗口。

## 完整工厂快照差异

以旧基线 run `8a060d67b80944bf8315f1ff8871e969` 对比本窗 run `d049453bbbdd4049ac142b27e850555b`。旧快照：snapshot `Pfx_fqkkipnFV9V6pffbrbRsibTcUZonNKDjpR8iYY0`，tick 79995323，52页 ord2–53、5194实体。新快照：snapshot `P_w8ElNFG9krkMbDft5P3nmUk-TmkhbENpFYf2dT4t0`，tick 80377177，52页 ord2–53、5196实体。新 run ord54 是另一 snapshot、tick 80377580 的单页0实体 prebuild 读数，未混入完整 built 快照。

只新增 sorter 5195、5196；无移除对象。以下为不加任何 allowance 的完整五项静态差异（连接项列出该对象的全部 before/after slot、方向、对端对象和对端 slot）：

- 3068 itemId：2011→2012。
- 3365 connections：before [slot0 out→3363/slot1, slot1 in←3362/slot0]；after [slot0 out→3363/slot1, slot1 in←3362/slot0, slot4 in←5195/slot0]。
- 5187 connections：before [slot10 in←5188/slot0]；after [slot7 out→5196/slot1, slot10 in←5188/slot0]。
- 5189 connections：before [slot0 out→5190/slot1]；after [slot0 out→5190/slot1, slot4 in←5196/slot0]。
- 5193 connections：before [slot0 out→5194/slot1, slot1 in←5192/slot0]；after [slot0 out→5194/slot1, slot1 in←5192/slot0, slot4 out→5195/slot1]。

除表列项目外无其它静态变化，`resourceNodeIds` 无变化。旧/新快照分别有10122/10130条连接端点记录，即5061/5065条双端物理连接；`nonreciprocalEdges=0`。分类出的动态变化对象数为247，不作为静态差异豁免。

主会话逐原始终态后读回确认当前快照与相关对象原生配置一致：run `8418a1e5bef240b5a436314ec877842a` ord88–93 覆盖 3365、5187、5189、5193、5195、5196；3068 的升级后读回为 run `eb17b3db04fa44deaabfebafb144e2fd` ord11。核对项包括位置、朝向、物品/配方/过滤和连接端点。

## 玩家材料、科研缓存与保存边界

相对玩家基线 `8418a1e5bef240b5a436314ec877842a` ord2，净变化严格为：电路板 item1301 **+1000**；普通分拣器2011 **−1**（两次施工消耗2、升级返还1）；高速分拣器2012 **−1**；其它14种物品不变。run `f322467c06ef4a738a6fb5720e757213` ord7→8 的电路板转移为源仓3600→2600、玩家0→1000；ord18→19 的紫糖 item6004 转移为源仓2324→1824、玩家0→500，两个 action 均成功。两笔新取料都晚于最后保存，尚未被正常保存覆盖。

run `f322467c06ef4a738a6fb5720e757213` ord14 的玩家机甲研究缓存为空；ord20 读取为 `pointCount=1800000`、`wholeItemCount=500`、`remainderPoints=0`，且 `autoManageResearchItems=true`；同读回玩家背包紫糖为0。这与500件等值已进入自动管理的研究点缓存相符，不是物品丢失。**wholeItemCount 是缓存的整件等值，不是背包实体库存，也不能当作可执行 player-to-storage 转移的数量。**分别读取背包 inventory 与缓存 pointCount/余数；不忽略余数、不把整件等值当成科技解锁或完成供料证明。

本窗最终 run `d049453bbbdd4049ac142b27e850555b` ord59 为同一 owned-world-001 / planet104 / DSP 0.10.35.29104；session tick80377608、revision30，主档 lastSaved tick80273796，Journal durable J96、无 pending/error。状态为 healthy、零 blocker，无 quarantine/checkpoint；和平、非沙盒、1×；玩家 Walk/0、约800 MJ，无人机均 idle、手搓队列空。网络3/4满供电仅为采样点证据。此记录不证明本次保存后重启恢复、连续燃料供应、科研实际供料或2104解锁。

## 后续边界

文档提交、准确 SHA CI 绿灯及 root 明确交接之前不新增游戏写入。交接后的优先项仅为对上述两笔 transfer 做一次正常保存；科研自动供给须另行设计，不把研究缓存当作背包库存。原始证据仅确认一处已被原生拒绝的消费者stub旧坐标（run f0f7539f… ord6，BUILD_LOCATION_INVALID），该坐标不原样重放；caller guard失败不是第二个原生拒绝。
