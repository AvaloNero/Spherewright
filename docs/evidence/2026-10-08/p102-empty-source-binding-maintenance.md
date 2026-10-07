# P102 空 Ti 矿机源端接入维护

日期：2026-10-08（Asia/Singapore）。此记录归并R74回收、R78保存、R80健康复核和后续离线源端支持。它不代表新矿机已建、路径已接通、自动Ti供料已恢复或Gate 2通过。

## 原生失败、回收与保存边界

R74原始run `929350f7c58a454680bc43651c38ce11:30`，seal SHA-256 `323E8507133FCAD21E144D7DC1478B65E2FF833A27077F9F9251E8C3484497A1`：同一普通拆除动作回收新建但未接线的Ti矿机1；拆除前其缓冲含 `1004×50`，玩家正常收到 `2301×1` 和这批矿石。完整玩家背包读回显示回收后的 `1004×50` 为inc0，这不是矿机的inc读数。之后旧footprint/yaw120候选的Native Build prepare因传送带对象7碰撞拒绝，0 build commit；R68已有一次同类足迹碰撞，故该候选族按两次原生拒绝退役，不再重放。矿机1没有留在现场。

root独立复核 `e188ee0a6ac148418cb5f9bd6dcc214f:14`，SHA-256 `F95CA31F088C60CD9EB336ECF250D016C157210A3B2B6FA01F0FE7FAFCD6A572`：完整factory为180 built/0 prebuild，仅矿机1被移除；对象7是仍保留out8的传送带，既有Si17路线保留。覆盖162个支持对象、5条完整Native路径和151条belt的同tick material cut以station44为锚点。N1为17 nodes/9 consumers/10 generators、full-serve、capacity `55000 J/t`。station44 slot0是Ti矿石 `1004×158/max200`，slot1是Si矿石 `1003×500/max500`；该tick dispatch阈值199，无活动订单、运输船或无人机。该库存不是Ti锭，也不证明或否定持续自动补给的根因。

R78普通Save原件 `b00aec127d8844259a3ba9a26285d736:16`，SHA-256 `0F6B79BFB0CECD48D6F01E91C2887228FC89CAC3168D735C93E57D4B1222D855`：10个请求/2.83秒，保存动作于tick `99927096`完成，物料差为0，覆盖矿机回收。root R80独立健康复核 `ebfc32ee3cff4248a8474cdea108a663:7`，SHA-256 `C94C08A790A3F44091FEDB870B04651EA9DB70F4499DA0CA8255B1878FBEB9F2`：当前为P102/R157、Save `99927096/R157/J100`，完整背包/inc/held及J100一致，健康primary ticket可用，owned健康且无在途/unknown；external `4/10`、lifetime `264`。普通写仍冻结，未来normal Save槽位保留。

## 离线 Native 核验与有限源端条件

只读IL依据为DSP 0.10.35.29104 `Assembly-CSharp.dll`，SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`。`PowerSystem.OnConsumerAdded(Int32)`在没有匹配的正数网络时留下 `networkId=0`；`PowerSystem.GameTick(Int64,Boolean,Boolean,Int32)`建立生产网络的 `networkServes` 从ID 1开始，索引0为零且不持久化，后续ID0遍历结果只是显示。`FactorySystem.GameTick`与`GameLogic._miner_parallel`向`MinerComponent.InternalUpdate(PlanetFactory,VeinData[],Single,Single,Single,Int32[])`传入 `networkServes[consumer.networkId]`；Native IL在供电比低于0.1时从该矿机更新返回0。`MinerComponent.insertTarget`传给`PlanetFactory.InsertInto(Int32,Int32,Int32,Byte,Byte,Byte ByRef)`，后者先索引entityPool再解析beltId，因此该字段是目标object ID，不是belt ID。Native MinerComponent没有物品inc字段，不能声称矿机inc为0；R74回收后玩家完整背包读回中的 `1004×50/inc0` 是玩家库存证据。以上是离线代码语义核验，不是当前运行中矿机的生产证明。

对应离线源端策略要求fresh复制并核对矿机、consumer、factory、节点及head身份；仅限item2301、vein miner、有效且非空的Ti1004节点、consumer `networkId=0`且真实 `networkServes[0]=0`、`productCount=0`、`productId`为0或1004。它只允许矿机slot0到新head belt slot1这一条互惠连接；`insertTarget`必须等于head的object ID。目标路径继续要求独立、完整、空货、开放且没有设备feed；原生路径几何、碰撞、占位、材料、旋转和prepare检查均保留。配对的可空miner身份/hash只扩展现有plan echo，未增加MCP工具或参数。

当前改动已通过离线策略/覆盖验证（Core 169/169、Contracts 4/4、MCP belt cover 9/9）；root报告Plugin Release按29104原生引用构建0 warnings/errors。安装仍是旧cohort `d744b8e6eb5b898b9e9484b2209c30380e40ddda`，新同星远端读取、64物料采样和矿机源端支持均未部署或实机验证。没有新Native source join prepare/build/readback。

## 下一步和未通过项

后续若root批准新有限阶段，先按同批维护流程部署已审代码，再fresh读取原生来源、库存、设备身份和空接缝；施工后逐项读回。只有源端接通且核验后，才另行fresh计划给新矿机供电。此记录不授予上述游戏动作。

现有5949→3964原油、4450→快速分拣器6198→6090的G线及R47三台新thermal实际供料/发电观察保留；新diamond支路5334供料仍未证明。P102 Ti自动运输、共享供需与净功率分配、连续≥36000-tick验证、整合保存恢复和同SHA双候选包均未通过，整案保持 `executable=false`。
