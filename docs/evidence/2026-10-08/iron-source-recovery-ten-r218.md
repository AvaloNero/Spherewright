# 铁源回收、限量备料与十写封窗

日期：2026-10-08（Asia/Singapore）。本事件汇总1496矿机回收重建、有限手采、四个分拣器端点和R215保存边界。R218独立十写审计原件 `843f19ac13ca427785b6693d611135e7:1`，SHA-256 `D080190D580BC8B729B9E4719085E52C2782AE99E4F9D8B482F76FA384F2FD90`，逐笔核对10次Native prepare、唯一intent/ACK、首轮poll前计数和原action终态，全部成功且无重放；Save `100924863/R42/J100`覆盖全部10笔。该审计不代表铁线或Gate 2已完成。

## 原动作核销与接线前缀

R20原执行调用方曾超时。R195只读核对原action `ae976e6a-d6d6-4e34-9844-c9d0ac8a4b3a`已到终态，未重放，也未新增accepted。

R13原件 `bdb49966c9634b21af5e0aea266f8199:113`，SHA-256 `9BC53D91ABA1AED8E8F9C73FC082A3FE20E2468D1C1982F347A3EEF20E9B220B`；四分拣器原件 `0d1a6612ca384edb829dec8b90a5e640:151`，SHA-256 `401B806A6D3917C151E9D55FB885EBFAB22571F447CEFD1C14D708A5CDFE086F`。保留的连接为`6263:6229→6249`、`6264:6230→6251`、`6265:6262→3280`、`6266:4064→6218`（源端最后）；均为2011/filter1114、物理slot4。

## 矿机回收与重建

R204正常回收空载或矿竭的1496，原件 `9a409ad4dbaf4de28cf10da9bb728a6c:24`，SHA-256 `6CE5419274185870F854BD4CF5D3FBE4CE0C8E47EAF7475349F23E5C0FCF23DB`。只移除1496→1501旧边，返还2301×1、货物差量为0。R208消费回收设备后复用实际实体ID1496重建，原件 `add1f731a94e4b7aaa5ba8f25fd3873c:34`，SHA-256 `9016FB196855B2102F53087394A1CBB3F7D2718FE6BAC67CB13829EE982BEC0B`。Native yaw165、覆盖Iron节点51/56、接入N3；矿机出口connections仍空，旧带1501–1508/1509与铁炉1500保持。没有把重建当作矿石已输送。

## R215取材与保存

R215正常手采非共享Iron节点54的10 Ore，并包含预留的第10笔普通Save。原件 `e30637ca8d6f455cbb4dd03cb32d9b22:65`，SHA-256 `E53FF883FF1A2F0AA1C0983940BD67F46254248E8D050FFA7BAF523402A4FD35`。矿存量`42671→42661`；玩家在正常接近过程中移动约10.7 m。55 requests/70.6秒后，普通Save `100924863/R42/J100`覆盖窗口，external `10/10 FROZEN`、lifetime `310`，十笔均为成功终态，无intent/unknown/在途。R185此前封存窗口另有一笔已知Move停滞失败，不属于本窗十笔；它未到目标、无recovery且未重试。

末玩家库存为Ore `1001×10`、Fe `2`、belt `2001×4`、fast belt `2002×6`、sorter `2011×1`；其他物品、inc/held保持。该10 Ore只作为以后手搓施工材料，不是持续采矿或自动供料。

## 结构快照与候选边界

R217完整快照 `f50c66ea51804192bddb7c4fcee1e665:76`，SHA-256 `58EB3663E11EFC53FF4880C79B6AF847A09A737246A233E0A2BB29A3D42144F1`，6229→6266 built、0 prebuild，新增33 belt及4 sorter，无删除/0非互惠边。R218完整审计 `843f19ac13ca427785b6693d611135e7:1`、SHA-256 `D080190D580BC8B729B9E4719085E52C2782AE99E4F9D8B482F76FA384F2FD90`确认静态变化仅限6个对象的10项已声明字段：矿机1496的position、rotation、connections、resourceNodeIds、insertTargetObjectId，belt1501的connections，以及belt3280、4064、6218、6229各自的一条新增互惠接缝；P102源169和pole187静态未变。现有本地typed114与远端P102的169个物料对象、6条Native路径和pole187按各自cut tick解读；该证据不是远端完整电网姿态帧。N3点读为224节点/562消费者/134发电机、full-serve。

未提交候选保持原失败类型与边界：R211短斜线在planned point6与对象1522重叠，Native `BUILD_LOCATION_INVALID`；R212三带Native预检为positive，但root复核尾端到旧head的实际距离为7.4955 m，超过声明reach上限6.5 m，因此未commit，这不是Native失败；R213北侧直连炉的候选因fast belt库存不足而Native `INVENTORY_INSUFFICIENT`，材料补足前不重试；R214首个`PreparedHarvestDTO.targetObjectId=null`令调用方断言停止，修正后fresh资格为positive、0 accepted。以上不构成已施工线路。

R219仅对r84×4作Native资格，原件 `c9cea3ecc00a4256ad942c37102a462b:9`、SHA-256 `334A76CF9932909E6B2C55DE1C48E8682E19FACA6CE44DC502E345D82A4BB130`为positive、0 accepted；顶层bill为8 Fe+4 gear→12 belt，递归预估10 Ore+2 Fe。尚无制作提交/终态读回，因此未计实际消耗。准确提交绿CI并由root重开后，候选为从1496北侧free leg最多铺12带，以filter1001普通sorter接入铁炉1500空输入；不新增pole/miner。新的Native source连接、到炉读回、Fe三级链产出及共享供需尚未验证。当前continuous credit为0；Gate 2、连续≥36000 tick和最终同SHA双候选包仍未通过。已通过的蓝图生命周期与Governor 2×门保持原有范围，不因本事件重开。
