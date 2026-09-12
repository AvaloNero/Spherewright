# v0.4 Overseer DSP API 证据

本文记录 v0.4 只读多行星监督链路采用的当前《戴森球计划》运行时接口。它补充 [game-api-m0.md](./game-api-m0.md)，不改变 owned-world、Unity 主线程或普通玩法写入边界。

## 2026-09-13：长周期制造台停机满输出被固定短窗永久跳过

IFX-123现场为3403/r41：raw-3658d4fc于51465517读到replicating对应的isWorking=false、time/timeSpend=7200000/7200000、1802输出20、三种输入齐备且供电比1；后续600tick原生统计产出0，既有诊断却无finding。当前Assembly-CSharp SHA-256再次核为`AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，并重新从该DLL只读反编译`AssemblerComponent`：`InternalUpdate`在time>=timeSpend时先令replicating=false，Assemble输出满足produced>productCounts×9即返回0，未开始下一轮。r41每批2棒，满缓冲20足以证明原生拒绝；当前7500速度对应960tick一轮，固定600tick诊断永远达不到旧整周期门。

修复只在Core允许已经停止、known-zero、供电已知充足、输出达到既有满缓冲阈值的非采矿设备报告output_blocked。完整周期、原窗口、原输出数量和保守整批阈值不改；对未知上游历史速率、正在工作、未知/不足供电、未满输出及其他故障仍保留原门。本切片不扩展到部分腾位时的每一个原生批量准入阈值，不把600tick零产本身当故障。没有新增DSP访问、公开观察字段、动作或工具；MCP说明及嵌入playbook同步。19项新增Core和1项MCP回归后，Debug/Release各1825测试通过、完整当前DSP Release零警告错误；冷部署及同一现场的新诊断读回仍待，现有实机反例不冒充修复后正例。

## 2026-09-07：混合入站带的尾端对齐货包观察（实现前证据）

同一当前DLL（0.10.34.28529，Assembly-CSharp SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，本次再次核验）中，`CargoPath.pathLength`直接返回私有`bufferLength`。`StationComponent.UpdateNeeds()`仅在count<max时把槽itemId加入needs；`UpdateInputSlots(CargoTraffic,SignData[],bool)`仅对Input端调用该path的`TryPickItemAtRear(int[],out int,out byte,out byte)`，成功后写storageIdx=needIdx+1（最后接收槽，不是输入过滤配置）。后者只在`buffer[bufferLength-6]==250`时取对齐的十字节货包，按四位base100货物ID读取，只有物品匹配needs才清除货包/RemoveCargo；不会跳过不需要的尾端货物。

采用边界：在既有detail-only beltCargo的同帧、同buffer锁及≤530字节副本内，只有选中段恰好止于open path尾部才报告`rearPickup`。复用完整十字节校验和原生`GetCargoAtIndex(int,out Cargo,out int,out int)`只读核对；匹配cargo pool身份、item/stack/inc并深复制。不调用有副作用的TryPickItemAtRear、不扫描整条路径、不改库存/needs/连接。非尾段和闭环明确not_applicable；没有对齐货包是no_aligned_packet，不是整条带为空；损坏/缺失原生证据为unavailable，不返回猜测货物。与既有分段聚合、实际吞吐和动作hash分开。

触发现场：102:44硅300/max300、钛8/max200、needs仅1004，110尾段聚合含硅和钛。175把硅插入同一钛带是可见拓扑；旧分段聚合没有顺序，因此不足以确认尾端具体物品。本切片只补观察，不把上述推断写成已确认阻塞、不认为腾一次容量就是持续修复。新增适配和测试完成后仍须正常冷部署与实机复读，旧1420安装态不具备该字段。

离线实现结果：新增28 Core/5 Contracts/2 MCP回归，1455项Debug/Release和完整当前DSP Release零警告错误通过；真实源码MCP64工具/1资源/45564字符同批指南一致，正常exit0/额外stdout0。既有hash和写面不变；此处仍不宣称新适配live通过，待正常保存、冷部署和同档恢复后复验。

本机复验：1455正常冷部署/同档protected resume27241993后，真实源码MCP raw-ca722021在27249207/304/391分别读取110 rearPickup=observed、marker1509、1003×1/inc0；44三个邻近tick均硅300/max300、钛8/max200、neededItemIds=[1004]、12GJ且110↔44保持。102中段返回not_applicable/selected_segment_is_not_path_rear/null item。184tick跨度只证明该观察区间的具体挡料，不能代替600tick窗或修复吞吐。玩家hash/revision1保持、0游戏写、正常exit0/额外stdout0；未知/closed/畸形分支仍为离线验证，新ZIP/异机未验。

## 2026-09-07：运输船起送阈值的原生反证（只读研究）

同一当前Assembly-CSharp（SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`）中，`StationComponent.DetermineDispatch(float,float,int,int,StationComponent[],FactoryProductionStat[],PlanetFactory[],GalaxyData,TrafficStatistics)`首先计算整数阈值`(shipCarries-1)*deliveryShips/100`。在本地供给发船和本地需求取货分支中，若供应槽max不大于阈值，会再把阈值夹到`max(0,supply.max-1)`；之后同时要求实际count、remoteSupplyCount、totalSupplyCount大于该阈值，而需求侧remoteDemandCount/totalDemandCount须大于0。因此不能只看到“source.max200小于船舱容量”就认定永远不能起送，更不能据此随意调高限额或降低起送设置。

该检查仍受已生成的远端配对、优先锁、路径范围、翘曲/收集器条件、空闲船、站能量及实际行程成本约束；count不是可分配供给的替代。`GameHistoryData.logisticShipCarries`由当前配置初值、升级和存档维护，现有公开`VesselCapacity`只是站内船位数，不是这一载货量。本研究没有新增读取/写入，也没有取得此次远端站的全部瞬时发货条件；不把公式或历史库存写成当前未发货根因。后续先读已部署的新诊断边界与现场事实。

## 2026-09-07：有库存的物流边界不等于上游根因

原1405安装态在26654934的同tick bundle（raw-183242d2，完整3工厂、600tick窗口）把530/767/774缺钛链追到102:1矿机50/50输出缓冲，返回其confirmed output_blocked，途中1657→44的物流库存/运单证据却被最终finding替换。这个输出只证明矿机的局部症状，未单独证明44当前库存或未发货原因。

源码复核：`GameStateReader.OverseerDiagnostics.CreateDiagnosticInputs` 已通过 `ApplyRouteEvidence` 复制 `SourceInventoryKnown/SourceInventoryCount`；后者是同一物料、匹配供需模式的所有候选供应站count之和，公开路径仅展示选中的一个供应站，不能当成该站库存或可分配量。原 `ProductionRootCauseTracer` 只要直接finding为material_shortage就递归，可能跨过已有库存，把其上游的满仓症状当成下游原因。本切片只在Core对“expected/configured/known positive stock”加因果停止边界，保留当前消费者缺料、精确物流端点和原始总库存，并明确dispatch_state=unproven；不读取新增DSP字段、不猜船舱/起送阈值、不新增远端详情或加载工厂。空/未知源、正常运单进展、无fleet和合格stall仍走原规则。

合成回归先复现4个错误分支，再全部通过；新增11 Core+2 MCP后1418项Debug/Release、完整当前DSP Release零警告错误、真实源码stdio64 tools/1 resource/42693字符同批指南、exit0/额外stdout0通过。随后1420同批开发DLL正常冷部署、protected resume重存26879615；Bridge raw-9cbaff8f在26890053取得完整3工厂/600tick窗口。真实源码MCP raw-4cf7d8f8在26949464（窗口26948865–26949464）再次对1106/1118/6003保留confirmed material_shortage、source_inventory=8、stocked_logistics_boundary、configured_route_supply_total及dispatch unproven，路径终止在104:530→1657→102:44；读取前后player hash和revision1保持，0游戏写、exit0/额外stdout0。远端102:1的50/50满缓冲仍是直接局部finding，不再冒充下游缺钛的已证实原因。

以上是源码MCP对已安装开发DLL的本机只读实测，不是新ZIP/异机验证。8是匹配路线供应总库存，未扣预留，仍不等于路径展示的单个44精确库存；不得沿用历史或合成200库存、把阈值公式当成实际发货诊断，或宣称钛供应已经恢复。

## 验证基线

- 游戏版本：`0.10.34.28529`
- `Assembly-CSharp.dll` SHA-256：`AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`
- 反编译工具：ILSpyCmd `9.1.0.7988` 与 Mono.Cecil
- 最近复验：2026-09-04

以下访问只能在 Unity 主线程、当前进程精确 owned `GameData` 实例仍成立且调用方 session 匹配时执行。DTO 必须在返回后台线程前完成深复制。

## 已创建工厂的只读边界

当前程序集公开：

```text
public PlanetFactory[] GameData.factories
public int GameData.factoryCount
public PlanetFactory GameData.GetOrCreateFactory(PlanetData planet)
public PlanetFactory PlanetData.factory
public int PlanetData.factoryIndex
public bool PlanetData.factoryLoaded
public void PlanetData.LoadFactory()
public void PlanetData.UnloadFactory()
```

`GameData` 为新世界分配 `starCount * 6` 个 factory 槽位。`GetOrCreateFactory` 会创建新 `PlanetFactory`、写入 `factories[factoryCount]`、绑定 `planet.factory/factoryIndex`、创建同索引生产统计并增加 `factoryCount`。保存会导出 `0 .. factoryCount - 1` 的全部工厂，读取会恢复同样的顺序。因此 Overseer 只枚举当前 owned `GameData.factories[0..factoryCount)` 中已经存在且身份自洽的对象；绝不调用 `GetOrCreateFactory`、`LoadFactory`，也不扫描星球或存档来发现未访问世界。

`PlanetData.factoryLoaded` 只表示该星球的显示、物理和音频侧已加载，不表示持久工厂是否存在。`UnloadFactory` 卸载显示对象并调用 `PlanetFactory.FlushPools()`；当前 `FlushPools` 只在没有预建筑时缩减过大的预建筑池，不删除实体、物流、供电或生产系统。因此远端已访问星球可以从 `PlanetData.factory`/`GameData.factories` 读取，但结果要显式报告 `factoryDisplayLoaded=false`，不能把它误写成“工厂未加载/不存在”。

拒绝方案：

- 不以 `GameMain.data.localLoadedPlanetFactory` 代表全部工厂；它只适合既有本地星球工具。
- 不用 `GetOrCreateFactory` 补齐缺失星球；观察不得创建游戏状态。
- 不枚举磁盘存档或读取非活动世界；v0.4 只读能力仍受当前 owned `GameData` 身份约束。

## 原生生产统计窗口

2026-09-09 / Governor低速测量阻塞的实现前复核：当前Assembly-CSharp仍为`AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`。重新检查`FactoryProductionStat.GameTick(long)`及`ComputeTheMiddleLevel(int)`：每`time % 6 == 0`执行level1，生产从level0下一写入游标倒数1..6格求和，写入count[600..1199]并更新total[1]；消耗对称写入count[4200..4799]/total[8]。两环均600格，因此覆盖3600个自动游戏tick，最新末端为`time - time % 6`；其后未满6tick的新样本不在level1内，不能把末端写成当前capture tick。level1值同样正常Export/Import；手搓路径只改lifetime，不进入此环。不得读取total[6]/[13]差值作自动产消，亦不读取有UI刷新依赖的ref速度缓存。

已实现的最小范围只为Governor事前声明的3600tick选项；Overseer公共默认及600tick原有声明不变。新选项绑定session/现场/周期，排除绑定前历史、明确真实start/end，并保留同帧短窗诊断和健康检查；不得把长窗均值等同所有瞬时tick恒速。`NativeMinuteProductionCounters`仅同步读取并逐值校验两环（每item1200格，最多64item），不保留数组、不改原生状态，身份/数组/游标/负数/total不一致时fail-closed。57新回归、1662项及完整Release通过；仍无冷部署或长窗实机通过声明。

当前程序集公开：

```text
public ProductionStatistics GameData.statistics.production
public FactoryProductionStat[] ProductionStatistics.factoryStatPool
public int[] FactoryProductionStat.productIndices
public ProductStat[] FactoryProductionStat.productPool
public int FactoryProductionStat.productCursor
public int[] FactoryProductionStat.productRegister
public int[] FactoryProductionStat.consumeRegister

public int[] ProductStat.count       // 7200
public int[] ProductStat.cursor      // 12
public long[] ProductStat.total      // 14
public int ProductStat.itemId
public float ProductStat.refProductSpeed
public float ProductStat.refConsumeSpeed
```

`ProductionStatistics.Init` 令 `factoryStatPool` 与 `GameData.factories` 等长；创建工厂时 `CreateFactoryStat(factoryIndex)` 使用相同索引。每个游戏 tick，`PrepareTick` 清空本 tick register，工厂正常模拟向 register 加入自动生产/消耗，`ProductionStatistics.GameTick` 再遍历 `0 .. factoryCount - 1` 调用每个 `FactoryProductionStat.GameTick(time)`。

`FactoryProductionStat.GameTick` 的 level 0 对每个物品执行以下滚动更新：

```text
production sample = productRegister[itemId]
consumption sample = consumeRegister[itemId]
ProductStat.count[cursor[0]] = production sample
ProductStat.total[0] = prior total[0] - overwritten sample + production sample
ProductStat.count[cursor[6]] = consumption sample
ProductStat.total[7] = prior total[7] - overwritten sample + consumption sample
cursor advances modulo 600
```

游戏基准为每秒 60 tick，所以 `total[0]` / `total[7]` 是精确最近 600 个游戏 tick（10 游戏秒）的自动生产/自动消耗计数，换算每分钟为 `count * 6`。它自然排除暂停、退出游戏和进程未运行的墙钟时间。`ProductStat.Export/Import` 持久化 `count`、`cursor`、`total` 和 `itemId`，因此该窗口随正常 owned save 保存/恢复，不需要 Spherewright 为生产速率另建可分叉的旁路计数器。

`Mecha.AddProductionStat` / `AddConsumptionStat` 分别只调用 `FactoryProductionStat.AddProductionToTotalArray` / `AddConsumptionToTotalArray`，而这两条路径只增加 `total[6]` / `total[13]`。因此玩家手搓、背包和机甲活动会污染 lifetime 累计量，但不会进入 level-0 `total[0]` / `total[7]`。Overseer 的自动产线实际速率采用 `total[0]` / `total[7]`，拒绝将 `total[6]` / `total[13]` 当作自动生产窗口。

窗口输出必须声明：

- `source=native_factory_statistics_level_0`
- `durationGameTicks=600`
- `durationGameSeconds=10`
- `wallClockSeconds` 不参与分母
- 数据来自当前 owned save 内原生持久化统计

若统计池、factory 索引、product 索引或 `ProductStat.itemId` 任一不自洽，该行 fail closed，不从相邻池槽猜测。

## 理论速率与利用率

`ProductionExtraInfoCalculator.CalculateFactory` 会重置并填充 `ProductStat.refProductSpeed/refConsumeSpeed`。对 assembler/lab，它使用每分钟 `3600 * speed / recipeExecuteData.timeSpend` 的基准周期并乘配方数量、增产或加速修正；矿机按矿点数、采矿速度和周期计算，水泵不乘矿点数，油井另含油速倍率。分馏塔、发电机和采集器另有独立分支。

这些 `ref*` 字段是按 UI 请求重算的可变缓存，`ProductStat.Import` 不恢复它们，也没有可验证的新鲜度标记。因此 Overseer 不读取或调用共享 `ProductionExtraInfoCalculator`，而是在 Unity 主线程从当前 owned factory 的身份绑定组件深复制所需输入，逐项重现当前程序集所有理论“产出”分支：

```text
assembler / matrix lab:
  baseCyclesPerMinute = 3600f * speed / recipeExecuteData.timeSpend
  if incUsed and productive and not forceAccMode: base *= unlockedProductMultiplier
  else if incUsed:                                 base *= unlockedAccelerationMultiplier
  output[item] += base * productCount

vein miner:
  output[item] += float(3600 / period * miningSpeedScale * speed * veinCount)
oil extractor:
  output[item] += float(3600 / period * miningSpeedScale * speed
                        * vein.amount * VeinData.oilSpeedMultiplier)
water pump:
  output[planet.waterItemId] += float(3600 / period * miningSpeedScale * speed)

fractionator:
  output[productId] += 1800f * (incUsed ? accelerationMultiplier : 1)
                       * produceProb * stackMultiplier
gamma receiver:
  output[productId] += 3600f * capacityCurrentTick / productHeat
orbital collector:
  output[item] += 3600f * collectionPerTick[item] * collectorSpeedFactor
```

增产/加速倍率与游戏相同：从 `item 2313` 的 `prefabDesc.incItemId` 中选择已解锁物品的最大 `Ability`，再读 `Cargo.incTableMilli/accTableMilli`。分馏塔 stack multiplier 同样逐 IL 复现；当前 `0.10.34.28529` 的第二个条件再次比较 `inserterStackOutput > multiplier` 后才可能赋 `stationPilerLevel`，而不是比较 `stationPilerLevel`。这看似是本体分支瑕疵，但 Spherewright 为保持同版本 UI 理论值一致而原样保留，并把来源版本化为 `current_runtime_component_formula_v1`；DSP 更新后必须重新反编译，不能自行“修正”旧公式。

只有连接到 `networkId > 0` 的生产组件进入容量，与本体 UI 规则一致；缺料、输出堵塞或当前供电比例低不降低设计容量。普通矿机 `veinCount == 0` 是矿脉耗尽后的合法状态，即使来源数组已经释放也返回 0，而不是让整份快照失败。其他活动组件必须同时通过 component pool、entity、power consumer/generator、network、recipe、source node、station 和 planet 的双向身份检查。当前版本所有能增加 `refProductSpeed` 的类别都包含在上述六域；任何未知 miner 类型、不一致数组/身份、非有限数或预算超限都会使整份理论快照 fail closed，不能用 0 冒充覆盖。

完整扫描成功后，每行返回：

- `theoreticalProductionPerMinute`：该星球所有已连接当前组件的理论产出和，可为 0；
- `theoreticalRateSource=current_runtime_component_formula_v1`；
- `theoreticalCoverage=complete`；
- `utilization=actualProductionPerMinute/theoreticalProductionPerMinute`，仅在原生 600-tick 窗口 `ready` 且理论容量大于 0 时提供。

实际窗口只有 10 游戏秒，离散配方产物可能恰落在窗口边界，所以利用率可暂时超过 `1`；该比值不钳制，以免掩盖采样粒度。窗口 warm-up 或没有已连接理论容量时返回 `null`，不把它解释为 0% 利用率。

拒绝方案：

- 不调用共享 `ProductionExtraInfoCalculator` 只为读取而改写 UI 统计缓存。
- 不读取未带时间戳的 `refProductSpeed` 后声称它是当前容量。
- 不用实际峰值近似理论值；这会把长期缺料误当成低设计容量。

## 供电、物流与科研

每个已存在 `PlanetFactory` 持有自己的 `PowerSystem`、`PlanetTransport`、`FactorySystem` 和存储/运输池。v0.4 只从已经通过上述 owned factory 身份检查的对象继续读取；任何 component ID、entity ID、planet ID、pool cursor 或数组长度不自洽都 fail closed。

### 电网

当前程序集的关键字段为：

```text
public PowerNetwork[] PowerSystem.netPool
public int PowerSystem.netCursor
public PowerGeneratorComponent[] PowerSystem.genPool
public int PowerSystem.genCursor

public long PowerNetwork.energyRequired
public long PowerNetwork.energyServed
public long PowerNetwork.energyCapacity
public long PowerNetwork.energyExport
public long PowerNetwork.energyStored
public double PowerNetwork.consumerRatio
public double PowerNetwork.generaterRatio
public List<int> PowerNetwork.generators

public int PowerGeneratorComponent.id
public int PowerGeneratorComponent.networkId
public long PowerGeneratorComponent.capacityCurrentTick
public long PowerGeneratorComponent.generateCurrentTick
```

`PowerSystem.GameTick` 先汇总本 tick 发电容量和消费者需求，再计算供电、充放电与交换器状态，最后逐个发电组件写入 `generateCurrentTick`。`energyCapacity` 是该网络当前发电容量；实际发电量必须把网络 `generators` 中身份和 `networkId` 均匹配的组件 `generateCurrentTick` 相加。`energyExport` 不是发电量：它只是在网络有余量且 `exportDemandRatio > 0` 时送入 `PlanetATField` 的防御场导出能量。早期本地电力 DTO 曾把它误映射成 `energyGenerated`；v0.4 实机出现“33 台发电设备、消费者满供电、generated=0”的反证后，现已改为真实组件和，并把导出量单列为 `energyExported`。不再用 `energyCapacity × generaterRatio` 近似实际值，也不把防御场余电冒充发电。

每个网络还返回节点/消费者/发电机/蓄电器/交换器数量、原生需求/供给/容量/储能和两个 ratio。星球摘要聚合全部有界扫描到的网络，但只返回最多 64 个网络详情，并明确 `networkDetailsTruncated`；`minimumConsumerRatio` 只在至少有一个消费者的网络间计算。

2026-09-08 氢去路设计复核：当前游戏及本地编译引用哈希仍同为上述AE0BA95F…EF85。`PowerSystem.GameTick`先由`EnergyCap_Fuel()`等路径汇总实际可用容量，随后按网络需求、储能及交换器状态计算分担比例，并将分配后的能量传给`GenEnergyByFuel(long energy, int[] consumeRegister)`。无增产路径扣热量为整数`energy * useFuelPerTick / genEnergyPerTick`，消耗新燃料件才更新统计；有增产时另有原生分支。因此增加一台已供料火电也会降低其他发电机的分担比例，不能简单线性放大原耗氢速率。本次只复核现有路径，没有新增字段或游戏调用。当前目录实测氢热值9000000J（raw-09ae34b3）；不凭记忆填入8MJ或未经观察的火电参数，长期净消耗仍由独立产消/库存窗口验收。

2026-09-07 发电详情单位修正：当前DSP0.10.34.28529的Assembly-CSharp SHA-256复核仍为`AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`。`PowerSystem.GameTick`把分配给该发电机的本tick能量`num57`传给`PowerGeneratorComponent.GenEnergyByFuel(long energy, int[] consumeRegister)`，再赋给`generateCurrentTick`；后者不是物品数量。`GenEnergyByFuel`按能耗扣`fuelEnergy`，实际开始消耗一件时才令`fuelCount--`、`consumeRegister[fuelId]++`并更新`curFuelId`。因此原有`CapturePower`的buffer角色`power-generation-current-tick`必须标成`joules_per_tick`，`unitsPerItem=0`表示无物品换算；保留现有int上限投影和fuelId语境，不把它当燃料堆栈。需要完整能量总值时使用既有long电网摘要。普通库存/研究点/货物字段不变，也不改native写入路径、动作hash或统计窗口。

本机旧安装态183火电在27697702返回count18686/item1120/错误items单位，为直接反例；这不是18686件氢。Governor当前库存和历史窗口库存复用同一物理计数判定，既排除新能量单位，也按role排除旧版误标items的发电量。该修正只改观察语义；新源码冷部署/live单位复读另验，不用单元测试冒充修复后实机。

### 物流

`PlanetTransport.stationPool/stationCursor` 中的每个活动 `StationComponent` 必须同时满足 station pool、`factory.entityPool`、`EntityData.stationId` 和本地/星际 planet 规则。当前读取的原生字段包括：

```text
StationComponent.isStellar / isCollector / isVeinCollector
StationComponent.energy / energyMax / pcId
StationComponent.storage[]
StationStore.itemId / count / localLogic / remoteLogic / localOrder / remoteOrder
StationComponent.idleDroneCount / workDroneCount
StationComponent.idleShipCount / workShipCount / warperCount
StationComponent.workDroneDatas[] / workDroneOrders[]
DroneData.endId / direction / maxt / t / itemId / itemCount / gene
LocalLogisticOrder.itemId / thisIndex / otherStationId / otherIndex / thisOrdered / otherOrdered
StationComponent.gid / workShipDatas[] / workShipOrders[]
ShipData.stage / planetA / planetB / uPos / uSpeed / warpState
ShipData.otherGId / direction / t / itemId / itemCount / shipIndex / gene
RemoteLogisticOrder.itemId / thisIndex / otherStationGId / otherIndex / thisOrdered / otherOrdered
```

普通行星塔、星际塔、轨道采集器和大型采矿机的 vein collector 是四个互斥类别，不能把 `isVeinCollector=true` 的采矿机重复算进行星物流塔。空槽必须同时保持 item/count/order/logic 为空；供需槽、库存和订单幅度只从身份自洽的槽聚合。非轨道采集器的受电状态通过 `station.pcId -> EntityData.powerConId -> PowerConsumerComponent.networkId -> PowerSystem.networkServes` 重新绑定；轨道采集器没有普通地面 consumer，故不进入 powered/underpowered 分母。该摘要只观察真实库存、订单、机队与能量，不派单、不补货，也不创建远端 factory。

### 科研

科研是 active owned `GameMain.history` 的全局状态，不复制成每星球各一份。当前程序集提供 `currentTech`、`techQueue`、`techStates`，每个 `TechState` 含 `unlocked/curLevel/maxLevel/hashUploaded/hashNeeded`；`LDB.techs` 的 `TechProto.Items/ItemPoints` 给出当前等级的矩阵要求，且 `TechProto.kPointPerItem=3600`。总件数和剩余件数沿用游戏自身的整数公式：

```text
itemCount = hashCount * pointsPerHash / 3600
```

摘要返回一份当前科技、上传/所需/剩余 hash、有序队列、运行时 tech-state 总数、已解锁数及逐物品预算；队列中的每个非零 ID 必须同时存在于当前 `LDB.techs` 和 `techStates`，负数或残留身份会 fail closed，矩阵身份只按当前 `TechProto.matrixIds` 判断。所有星球页都引用首屏捕获的同一份深复制科研状态和同一 `capturedAtGameTick`，不会把后续 tick 的科研进度拼进旧游标快照。独立调用本地电力、物流或科研工具可能在相邻 tick 执行；除非 `capturedAtGameTick` 相同，否则逐字段差异是正常的动态状态，不能拿来否定或拼接当前 Overseer 快照。最终实机对照仅相隔 2 tick，母星需求/实际发电就从 `90688` 变为 `79388`，而各自快照内部仍满足 generated/served 和 exported 语义。

## 同 tick 诊断包与脱敏边界

独立调用生产和跨域摘要会在相邻主线程任务执行，不能保证 `capturedAtGameTick` 相同，因此调用方不应自行拼接为一份原子观察。`get_overseer_diagnostic_bundle` 在同一个 Unity 主线程委托内先完成全部生产/根因深复制与受保护物流窗口提交，再完成供电/物流/科研深复制；调用链没有 `await`、游戏对象或后台延迟。组合器仍逐 planet 强制校验 factory index、planet ID/name、local/display flags 与双方 `capturedAtGameTick`。任何字段不一致都让整份请求 `NOT_READY`，不会把相邻帧或不同 factory 拼起来。

包的公开契约是 `schemaVersion=1`、`privacyProfile=public_allowlist_v1`。它只从现有公开 `OverseerPlanetProductionSnapshot`、`OverseerPlanetSummarySnapshot` 和全局 research DTO 复制白名单字段，不接受或输出真实 save name、save-derived protected key、runtime descriptor/path、auth token 或写动作 plan token。`sessionId`、短时 `snapshotId` 和 continuation cursor 仍是现有读协议的会话/分页身份，不授予游戏写入能力。独立 snapshot store 只有在结果实际需要 continuation 时才保留最多 8 份、60 秒；cursor 绑定 session、排序后的精确 item 集合和 page size。这个聚合端点不会调用新的 DSP API、创建/载入 factory 或改变现有诊断覆盖率。

## 直接设备故障与物流路径证据

当前程序集的装配、矩阵制造和采集组件在完成周期前先检查自己的输出缓冲；这些门决定“满输出”何时真正阻止下一次产出：

```text
AssemblerComponent:
  Smelt                 produced + productCount <= 100
  Assemble              produced <= productCount * 9
  Refine/Chemical/other produced <= productCount * 19

LabComponent matrix mode:
  produced + productCount <= 10 * ceil(speedOverride / 10000)

MinerComponent:
  productCount < 50
```

因此公开容量分别规范化为冶炼 `100`、制造 `productCount * 10`、其他装配配方 `productCount * 20`、矩阵站 `10 * ceil(speedOverride / 10000)`、采集器 `50`。这不是猜测仓格上限，而是与当前 `InternalUpdate` 是否允许下一批产出的比较边界一致。配方完整周期以 `ceil(timeSpend / speed)` 个游戏 tick 计算；采集器使用当前 `period / (speed × miningSpeedScale × sourceMultiplier)` 的向上取整。诊断只有在原生窗口 ready 且覆盖至少一个完整周期后运行。

装配机和矩阵站的 `served[]` 会在原生周期开始时先扣除一批输入。故 `replicating=true` 且 `served` 小于下一批需求并不证明当前周期缺料；诊断必须等设备停止后再判断。类似地，物品级 600-tick 窗口已经有正产量时，不能因为同类设备中的某一台恰好空闲就把整个物品标为停产。当前实现按同一物品的聚合实际产率做保护，只有实际值为零才逐个检查其直接生产设备。

物流证据不能用“同星球上有同物品塔”代替物理连接。当前实现从每个消费者的输入 sorter `pickTarget` 反向遍历 `PlanetFactory.ReadObjectConn` 的入边，只穿过 belt、splitter、piler、spraycoater、inserter、storage 和 tank；只有最终命中 `StationComponent.slots` 中精确 `Output`、非零 `storageIdx` 所绑定的 belt/entity，才把对应站槽视为该输入的 demand 端。随后才按该槽的 local/remote demand 模式，在全部已创建 owned factory 中寻找同 item 的 local/remote supply 端，并汇总库存、订单和去重后的 idle+work drone/ship 数量。首轮实机追踪曾在中转仓停止，遗漏真实路径；加入 storage/tank/inserter 后，实际链 `assembler 530 <- sorter 532 <- storage 259 <- sorter 1784 <- ... <- station 1657 output belt 1783` 才得到母星 demand `1657` 和远端 planet `102` supply `44` 的证据。

递归生产者绑定复用同一条反向货运图，但不能把一次“为所有输入遍历的节点集合”无条件共享。实现对配方中的每个 item 分别从 input sorter 起步；起始 sorter 和所有中间 sorter 的 `filter` 必须为 `0` 或精确等于该 item。分流器还必须保留反向进入时的 exact connection slot：`SplitterComponent.GetSlotBelt(slot)` 必须与下游 belt/component/entity 三重身份一致，并且只命中 `output0..3` 中一个输出。当前程序集的 `SetPriority` 会把被设为优先的输出 belt 移到 `output0` 并写 `outFilter`；`CargoTraffic.UpdateSplitter` 在 `outFilter != 0` 时只把匹配物送入 `output0`，同时只把不匹配物送入 `output1..3`。因此当前读取对 priority output 使用 `item == outFilter`，对其他输出使用 `item != outFilter`；无过滤才允许全部物料。身份不一致使整个读取 fail-closed，过滤不允许的分支则不继续遍历。通过这些门后，才把命中的、同星球且实际输出该 item 的 assembler/lab/miner 绑定为上游候选。Core 以 `(planetId, objectId, itemId)` 去重，最多进入 8 层、访问 64 个生产者并检测环；达到上限、遇环或 resolver 身份不一致时，用 `upstream_trace_stop_reason` 明示路径没有继续证明。上游设备不具备与请求物品相同的 per-device 原生产量窗口，因此其 `ActualProductionStateKnown=false`；分类仍可用同 tick 缓冲、电力、矿源和工作态，但不能拿另一个 item 的聚合速率误消除当前故障。

跨星边不能把供应塔上次输入留下的 `SlotData.storageIdx` 当成固定物品选择器。当前程序集的 `StationComponent.UpdateNeeds` 会从所有未满的 station storage 构造 `needs[0..4]`（星际站另有翘曲器 `needs[5]`）；`UpdateInputSlots` 对每个 `IODir.Input` belt 调用 `TryPickItemAtRear(needs, out needIdx, ...)`，成功取货后才写 `slot.storageIdx = needIdx + 1`。所以当前读取按精确 supply endpoint 的 item，从该塔每条身份一致的 Input belt 反向复用上述 item-aware 图；只有经 sorter/splitter 过滤后真正可达的 assembler/lab/miner 才成为该供应端候选。所有 owned factory 先在同一主线程读取中完成深复制，再用全局 planet index 按 planet/object/output-item 绑定生产者。公开 path 中的 supply station 与用于递归的输入带属于同一 endpoint，不会把其他候选供应塔的生产者拼接到该节点后面。如果供应塔是人工填充、没有 Input belt，或输入带背后只有另一座塔，就不猜测生产者；多段塔中继仍需要每一段的独立物理证据。

同档正例从黄糖 `6003` 的 matrix lab `774` 起步，缺金刚石 `1112` 后沿真实入料仓/分拣器链命中唯一熔炉 `715`，再由该炉当前输入读到缺高能石墨 `1109`。最终 path 为 `matrix_lab 774 / 6003 -> material 1112 -> assembler 715 / 1112 -> material 1109`；finding 的 object/item 指向当前最深的已诊断设备和产物，而 path 首节点仍保留调用方请求目标。该样本没有触发停止原因，说明是在自然叶节点结束，而不是预算截断。

同档跨星正例在最终部署后的 tick `14414535` 从母星钛块熔炉 `104:530` 的缺钛石 `1004` 开始，先命中 demand station `104:1657` 与 supply station `102:44`，再从供应塔的真实 Input belt 继续到未显示的 planet `102` 工厂中矿机 `1`。最终 finding 为该矿机钛石输出 `50/50` 的 confirmed `output_blocked`，完整 path 为 `104:530/1106 -> material 1004 -> demand 104:1657 -> supply 102:44 -> resource_extractor 102:1/1004`。独立请求 item `1004` 在 tick `14417684` 又对远端同一矿机给出理论 `60 min⁻¹`、实际 `0` 和相同 50/50 输出堵塞，不是从母星路径单独推测出的身份。

运输进展需要跨 tick 比较。当前程序集在派出供给端无人机时，把货物和目标站写入 `DroneData`，令 `direction=1`、`t=-1.5`；飞行段按每 tick 增加 `t`，到站后交货并改为 `direction=-1` 返回。需求端派出的无人机以空载去取货，仍由同一 `endId/itemId/t/direction` 描述。`LocalLogisticOrder` 同时绑定本端/对端 storage index 和两个 signed reservation，交货或取消时由原生路径扣回 `StationStore.localOrder`。星际船对应使用全局 `otherGId` 和 `RemoteLogisticOrder`；`stage=-2,-1,0,1,2` 覆盖起降/航行阶段，主航行阶段即使 `t` 不变也持续更新 universal `uPos`，所以只观察 `t` 会漏掉真实进展。供给端船在 dispatch 时立即扣源库存并带货出发，需求端船则空载出发后在远端装货；两种路径都会在需求槽保留正 `remoteOrder`，送达后撤销。

第六个切片因此只为已经由物理 output belt 证明的精确 demand route 建窗口，并要求消费者输入确实不足、需求端 order 为正、匹配 supply 总库存为正、供需站去重后的 idle+work fleet 大于零。需求槽是订单正 reservation 的权威端；不能把供给端的负 reservation 或任意非零值冒充仍待满足的需求。消费者缓冲恢复到至少一周期用量时，该样本明确使旧停滞基线失效，避免短暂供足后再次缺料复活旧窗口。每个活动 carrier 还必须通过工作数组边界、item、目标站、direction、阶段/进度的非有限值检查；route fingerprint 只纳入该 demand 与候选 supply 之间、同 item 的 carrier。无人机用 `direction/maxt/t/itemCount/order`，星际船还用 `stage/uPos/uSpeed/warpState/shipIndex/order`，任一变化、需求库存增长、需求订单幅度缩减或 active route-carrier 数变化都算进展并重置停滞起点。没有 active carrier 但存在 fleet/order/source 也可进入计时，因为这正是“有可用机队却未派单”的潜在故障；单次快照和不足 600 游戏 tick 的静止窗口都不下结论。连续 600 tick 无进展才返回 suspected `logistics_blocked`，而非 confirmed；正处于活动或 warm-up 的路线不会回退成 generic `material_shortage`。

窗口文档位于既有 current-user-protected runtime 根下，文件名和文档身份只含专域 SHA-256 save key，route key 同样是模式、item、精确 demand/supply station slot 拓扑的 SHA-256；公共 DTO、finding 和日志均不返回这些 key、真实 save 名或路径。一次 Overseer 读取先深复制所有 owned factory 的路线样本，再用一次 secure-new-file、flush、同卷原子 replace 提交整个批次；只有成功落盘的 analysis 才写入公共 DTO，避免按设备/路线反复同步写盘，也避免把未持久化状态当作 durable 证据。文档最多保留 4096 条 route、16 MiB。正常重启后相同 owned save 可跨新 session 继续；`crossedSessionBoundary` 明示这一点，速率仍只按 game tick，墙钟离线时间单列排除。save identity 变化、tick 回退、route topology 变化、同 tick 状态突变或相邻观察超过 3600 tick 均将窗口标为 discontinuous 并从当前点重建，不能把缺采样时间冒充停滞。

当前同档部署已经创建 3 条实际物理塔路由的受保护基线；文档为 version 1、DSP `0.10.34.28529`，route key 全部哈希，未包含原始高熵 save identity，DACL 禁止其他 SID。后续真实钛运输把 `102:44 -> 104:1657` 的活动分支完整闭合：供应槽达到 200 后源/需 reservation 为 `-200/+200`，运输船移动超过 2100 tick 时没有 finding；源钛在取货后 `200 -> 79`，送达后双方订单归零、船归队，母星钛块实际速率恢复到 `12 min⁻¹`，远端矿机为 `30 min⁻¹`。因此活动/warm-up/送达清除已经有实机正例；仍缺的是故意连续 600 tick 无进展、形成 suspected stall 后再恢复的正例。fractionator、gamma receiver 和 orbital collector 已计入理论产能，但尚未接入同等级直接缓冲诊断；请求物品若由这些设备直接生产，`directDiagnosticCoverage=partial`。

后续同档用两轮活动运输专门复验跨保存边界。硅 route 在普通保存 tick `17572610`、正常退出和 exact-primary 自动重存 `17572642` 后继续移动并送达；钛 route 又在保存 tick `17579665`、恢复/自动重存 `17579696` 后继续。新进程首个受保护钛样本 tick `17580795` 同时为需求订单 200、消费者缺料、源库存 60、fleet 1/active 1，且 `stagnantSinceGameTick=lastGameTick=17580795`、finding 0，直接证明离线墙钟未被累计。随后送达 90、订单归零，钛块实际速率保持 `12 min⁻¹`；最终普通保存 tick `17584412` 和审计 tick `17585687–17600202` 均为 healthy、0 blocker/checkpoint，Journal `49/49` durable、三网 ratio 1、0 power/logistics finding。

当前程序集的 `StationComponent.InternalTickRemote` 对活动运输船每个 game tick 都推进阶段、位置、速度或曲速状态，站点断电不冻结已出发 carrier；`tripRangeShips` 只控制派单。暂停游戏不会增加 game tick，改需求/路线或拆塔会改变 qualifying route，只有直接改写 `stage/t/uPos/uSpeed/warpState` 才能强行造出静止指纹，而这违反普通玩法边界。因此真实 600-tick stalled-carrier/recovery 仍明确标为未实机覆盖；Core 自动测试继续锁定该状态机，但不能替代或冒充 live 证据。

所有 finding 都包含同 tick 的设备、物料和可证明物流节点；它们是瞬时诊断，不是跨 tick 不变事实。实机中同一制造台 `715` 曾在网络服务率约 `0.94038` 时返回 `insufficient_power`，供电恢复后又自然变为缺 item `1109` 的 `material_shortage`，证明调用方必须按 `capturedAtGameTick` 使用证据，不能缓存旧根因继续写入。

## 有界输出与诊断限制

- 生产行必须由调用方提供去重后的有效物品 ID，首个切片最多 64 个；不接受“返回所有物品”的无界请求。
- snapshot 容量只保留能够由 continuation cursor 再次引用的多页记录。`items.Count <= pageSize` 的完整首屏直接返回且 `nextCursor=null`，不进入 60 秒 store；真正分页仍绑定 session/scope/filter/page-size/expiry 并受硬容量限制。live 压力矩阵已证明 16 次完整三星球首屏不占槽，8 个真实分页快照占满后第 9 个返回 `SERVER_BUSY`，同时完整首屏和已签发 continuation 仍可用。
- 工厂遍历同时受 `factoryCount`、数组长度和 512 个已创建 factory 的显式上限约束；每页最多 16 个 planet，快照保持 60 秒且绑定 session、请求类型与页大小。
- 同 tick 诊断包完整继承生产与跨域摘要各自的扫描、finding 和 item 上限，不把两套预算合并成一个更大的隐式额度；任一底层域失败就不返回部分包。它有独立的 8-slot continuation store，完整首屏不占槽。
- 跨域摘要最多扫描 4096 个 power-network pool 槽、65536 个发电组件引用和 4096 个 station pool 槽；每站最多 64 个 storage slot，科研队列最多扫描 4096 项、返回 64 项，runtime tech catalog 最多扫描 12000 项。理论产能另限制最多扫描 131072 个 assembler/lab/miner/fractionator/generator/station pool 槽和 262144 个 recipe input/output、矿点及采集物引用。超过预算返回明确的非重试 `SERVER_BUSY`，不静默截断聚合总量。
- 直接诊断另受 131072 个组件/拓扑节点和 262144 个配方、来源及站槽引用的总预算；每个 item 最多返回 16 条 finding，每个 planet 最多返回 16 条无已知 item 身份的基础设施 finding，同时保留未截断总数和 `truncated` 标志。预算溢出会让整份首屏 fail closed，不能返回不完整总量。
- 当前立即生产者诊断和递归生产者都只覆盖 assembler、matrix lab 和三类 miner；fractionator、gamma receiver 与 orbital collector 作为请求物品的直接生产者时会显式令 `directDiagnosticCoverage=partial`。递归只沿物料兼容且物理可达的受支持生产者，或沿一个精确 demand/supply station route 进入同一 supply endpoint 的真实 Input belt；最多 8 层/64 个 producer。人工填塔、无输入带、未证明的多段塔中继和未支持设备不会被猜测补全，路径预算/环路停止会进入 evidence。
- 具备载具的物流订单已经接入跨 tick、按存档保护的窗口。真实活动 shipment 已覆盖派单、2100+ tick carrier 移动、取货、送达、下游 `12 min⁻¹` 恢复，以及两轮活动状态普通保存/正常退出/exact-primary 恢复；恢复后的首个样本从新 session 当前 game tick 建基线，退出期间墙钟时间没有形成假 stall。受控缺料、断电和物流配置阻塞三类故障的制造、区分、正常修复、finding 清除与最终 healthy 保存均已有 live 证据。真实 carrier 连续 600 tick 完全静止再恢复仍未实机发生；当前 DSP 没有保持相同订单/路线的安全普通停航控制，因此该分支只保留自动测试和明确覆盖限制，不得用直接运行时字段写入伪造。
- 原始 owned save 名、由它派生的持久化 key、auth token、plan token、绝对路径和运行时描述文件不得进入公共 DTO 或诊断包。

## 已完成的运行时切片

第一条 v0.4 纵向切片在精确 owned session 中，以一个有界物品 ID 集合读取全部已创建工厂的原生 600-tick 自动生产/消耗窗口，并为每行返回星球身份、实际每分钟速率和窗口来源。

第二条纵向切片用独立的 session/页大小绑定快照返回每星球电网与物流聚合，以及一份全局当前科研摘要。前两条切片都不创建 factory、不加载远端显示、不写共享统计缓存。它们已经提供故障诊断的输入，但尚未把设备缓冲、物流路径和矿源余量组合成最终根因，因此不提前声称完整 v0.4 验收完成。

第三条纵向切片把当前程序集的理论产出公式作为纯 Core 计算器接回第一条生产快照；Plugin 只负责有界、身份绑定的运行时输入扫描。实机先暴露并修正“耗尽矿机 `veinCount=0` 时数组可为空”的合法边界，随后同一三工厂存档完整返回 `complete`：母星蓝/红/黄矩阵分别为 `20/10/7.5 min⁻¹`，铁/铜/石/煤矿由实际覆盖 `14/2/3/7` 个矿点闭合为 `420/60/90/210 min⁻¹`，水泵为 `50 min⁻¹`，油井按当前矿量为 `133.15919494628906 min⁻¹`；远端未显示工厂仍给出硅/钛 `120/60 min⁻¹`。冶炼、化工和制造配方也与实体数量闭合。理论/利用率现已完成，仍待把设备缓冲、物流边和矿源状态接入运行时故障分类与上游根因图，因此不提前声称完整 v0.4 验收完成。

第四条纵向切片把纯 Core 首因分类器接到真实 assembler/lab/miner 缓冲、power consumer/network、recipe、vein 和 station/belt 拓扑。最终同档 tick `13945617` 的自然现场同时返回：铁矿机 `1213/1496` 与冶炼炉 `10` 的 `output_blocked`，制造台 `530` 缺 item `1004` 的 `material_shortage` 并附带 `104:1657 -> 102:44` 物流供需路径，matrix lab `774` 缺 item `1112`，以及三台已丢失产品身份的耗尽矿机基础设施 finding。较早一次同版本读取还在制造台 `715` 上捕获约 `0.94038` 供电比的 `insufficient_power`；供电恢复后标签随同 tick 状态改为缺 item `1109`。item `6002` 的实际产量为 `12 min⁻¹` 时没有因瞬时设备输入状态产生误报。完整 solution 0 warning/0 error，174 项测试通过；最终部署 Plugin SHA-256 为 `D40D6BEA4E76697EB14C5F1DE3B0CC61532E4BF634125E1A9488D5024FDF59E1`，normal save `13943810`、exact-primary resume/auto-save `13943842` 均保持同一 owned world。最终 `limit=1` 三页在 tick `13986388` 共享快照并以 `STALE_CURSOR` 拒绝错绑 filter；源码 MCP `0.4.0.0` 通过协议 `2025-06-18` initialize、50-tool list 和 live call。只读审计 tick `13990990+` 仍为 healthy、Journal durable、无 blocker/checkpoint/prebuild。该切片完成直接设备层，不等于递归上游、时间型物流停滞和受控故障门已经完成。

第五条纵向切片把缺料 finding 递归到同 tick、同星球、物料过滤允许且物理可达的上游生产者。首个候选已在 tick `14028962` 从 lab `774` 追到 diamond assembler `715` 的高能石墨短缺；复核后先把每种输入拆成独立遍历并要求路径上每个 sorter filter 相容，最终又按本机程序集核对 `SplitterComponent.SetPriority` 与 `CargoTraffic.UpdateSplitter`，把 exact output slot/belt 身份和“优先口只收过滤物、其他口排除过滤物”的双向规则纳入遍历。Core 为根因图与 splitter policy 提供 14 项回归，完整 suite 为 188 项（Contracts 17、Core 150、MCP 21），solution 0 warning/0 error。最终源码相等部署为 Plugin `46E62CC930CAD0756BBFB06625C9585F04A074B25FEDC15C4D7DCE2A322F4B70`、Contracts `507C57A10C49435C8D0AEF71F49DA5C6255711C9093D651CBD1A57F91088055F`、Core `CA8B33DD66330211ECD78E535CBD05932933278AF6E640BE67AF7AFD6301E7C5`；普通保存 tick `14109460` 后正常关窗，exact-primary 只恢复 planet `104` 并自动重存 tick `14109491`。最终 Bridge live tick `14111293` 与最终构建后的源码 MCP tick `14138110` 都返回 `lab 774 / 6003 -> material 1112 -> assembler 715 / 1112 -> material 1109`，无 trace-stop evidence；MCP `0.4.0.0` 完成协议 `2025-06-18`、50-tool list、明确 sorter/splitter filter 的工具描述和同一路径调用。最终三页生产/摘要分别共享 tick `14119083/14119093`，错绑 filter cursor 以 `STALE_CURSOR` 拒绝；审计 tick `14118310+` 为和平、非沙盒、1×、healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、0 prebuild、无 blocker/checkpoint。跨星生产者、时间型物流停滞和三类受控故障门仍未完成，本切片没有 tag 或 Release。

第六条纵向切片实现物流时间窗的 Core 状态机、Plugin 原生 carrier 深复制和按档受保护持久化。Core 覆盖 600-tick mature、移动/送达/订单缩减、消费者供足重置、跨 session 离线排除、无订单/无源/无 fleet、save/route/tick/gap discontinuity、same-tick 突变和重复成熟读取；完整 suite 提升为 204 项（Contracts 17、Core 166、MCP 21），solution 仍为 0 warning/0 error。最终审查把逐 route 同步写盘改为一次读取一次原子批量替换，并把需求判定收紧为 demand endpoint 的正 reservation；只有成功持久化的 analysis 才进入公共 DTO。部署前同一 owned world 正常保存 tick `14290235`，旧进程接受 `CloseMainWindow` 正常退出；四个运行 DLL source/deployed SHA-256 全部一致，Plugin/Core 为 `A66033BFC60DBCAC8B2E798F815E7A22E635AAFCBFDD7F604E5256F191E3CDC5` / `EE9F5519C23A1EC9BC21987D78D29D90A79E561EBEE80F72702A74678B8E492E`。新进程只消费 minimum tick `14290235` 的 exact-primary ticket 并自动重存 tick `14290266`。live `get_overseer_production(6003)` 在 tick `14293735+` 仍返回三座 factory 与原有黄糖递归根因；保护文档含 3 条哈希 route、2942 bytes、current-user-only DACL、无原始 save identity，样本 session 已切到恢复后的新 session，并同时出现 `consumerInputMissing=false/true`。生产/摘要三页共享 tick `14304692/14304700`，错 filter cursor 继续 `STALE_CURSOR`；源码 MCP `0.4.0.0` 用协议 `2025-06-18` 列出 50 tools 并在 tick `14302792` live 返回三厂。因为现场三条 route 均没有订单和 active carrier，本切片状态仍是“实现与持久化已验证，活动/停滞 shipment 待受控实机”，不提前关闭物流故障验收，也没有 tag 或 Release。

第七条纵向切片把递归生产者图跨过精确物流 endpoint。当前程序集证明 Input port 按整站 `needs[]` 动态取货、`storageIdx` 只是上次成功取货结果；Plugin 因此从每个 supply endpoint 的真实 Input belt 按 item 反向复用 sorter/splitter 过滤图，等所有 owned factory 深复制完毕后再用 planet/object/item 全局解析。最终审查还修正 IFX-021：聚合库存/机队可来自多座 supply，但公开 path 与递归候选只能使用同一座精确 supply；有 demand route 时也不再跳入同一消费带上的另一条本地直连候选。完整 suite 为 205 项（Contracts 17、Core 167、MCP 21），solution 0 warning/0 error。最终同档普通保存 tick `14413801`、正常关窗、四 DLL source/deployed 零差异后，exact-primary 只恢复 minimum tick `14413801` 的 planet `104` 并自动重存 `14413832`；Plugin/Contracts/Core SHA-256 分别为 `344614FE3B827BE8397D5D6DC77C3CCB90C8991C01D088E3108B6F473AB11869`、`8E2FB3205B54972180540D6A6C9B08F62B028453DA5E44BBC36CA96E04F56991`、`9507C3AAEE729ACF13693573C2AB53466B8043F53B762B054F2DEEBD59AAF412`。live tick `14414535` 将母星钛块熔炉 `530` 经 `104:1657 -> 102:44` 追到未显示远端工厂的钛矿机 `102:1`，最终报告其 50/50 `output_blocked`；同一响应中的黄糖四节点路径保持。独立 item `1004` 于 tick `14417684` 再次闭合该矿机理论 `60 min⁻¹`、实际 `0`；三页共享 tick `14415270` 并拒绝错 filter cursor。源码 MCP SHA-256 `E86BE095EA8FDF10D7487C65876EEFD534CE3E4684F9EC31C378F3A737A4E70E` 以协议 `2025-06-18`、版本 `0.4.0.0` 列出 50 tools，描述明确 supply Input-belt 语义，并在 tick `14416829` 返回同一路径。最终审计 tick `14418919+` 为 peaceful/non-sandbox/1×、healthy、0 blocker/checkpoint/prebuild、Walk/0、满核心、3/3 drone idle、Journal `49/49` durable；日志无 error。活动/停滞 shipment 与三类受控故障门仍开放，本切片没有 tag 或 Release。

第八条实机切片先闭合物流窗口的活动分支，再修复高频观察暴露的分页容量漏洞。远端站 `102:44` 的钛/硅供应上限从 `100/100` 调整为 `200/300` 后，真实钛订单经历源/需 `-200/+200`、运输船持续移动、源库存 `200 -> 79`、送达归队和母星钛块 `12 min⁻¹` 恢复；超过 2100 个移动 tick 内 item `1106` 的 finding 始终为 0。远端 save `14535735` 后，原生返航动作 `3515d9f4-8a65-404b-b7bd-79f75ed7a7bc` 稳定落到 planet `104`，checkpoint 已撤销，主档 save `14575384` 覆盖结果。监控期间发现 `SnapshotPageStore` 会保留没有 continuation 的完整首屏；最终实现只保存 `items.Count > pageSize` 的记录。Core 新回归和完整 suite 共 206 项（Contracts 17、Core 168、MCP 21），Release solution 0 warning/0 error。普通关窗后四 DLL source/deployed 一致，Plugin/Contracts/Core SHA-256 为 `66D6E4631D0AF8DD3B6C7D6AE11DFF02AE37B13EE8A7A8D77DD2BDCF598B38C9`、`D0F580634540C486BFE865A8C6E50402647AD6A435F7D66205D6DDCAB9C2353E`、`DE36BF4028D3C3FED0A5E7CA871F7CA040E66EF0682521A2BF5A25E6BE62AA32`；exact-primary 自动重存 `14575416`，Steam 启动链继续同一世界。live tick `14577940–14577989` 连续 16 个完整三星球页均无 cursor，8 个真实分页快照占满后第 9 个正确 `SERVER_BUSY`，满载时完整页和已有 continuation 仍成功。最终审计 tick `14585723+` 保持 healthy、Journal `49/49` durable、2254 built/0 prebuild、Walk/0、3/3 drone idle、无 blocker/checkpoint/BepInEx error。活动 shipment 已完成；受控 stall/recovery 和三类故障门仍开放，本切片没有 tag 或 Release。

第九条离线切片把此前两个独立 Overseer 读取合成为单个同 tick、白名单诊断包。新 Bridge/MCP 方法要求 1–64 个当前物品 ID 和 1–16 个 planet page limit；首次调用在一个主线程任务中捕获两域，只有 factory/planet/name/local/display/tick 全部一致且公共域集合完整时才创建不可变分页快照，continuation 继续绑定 session/filter/page size。Contracts 序列化回归证明 schema/profile 以及 save/auth/plan/path 字段缺失，Core 组合器覆盖正常合并及 factory/planet/tick/name/runtime flag 错配和缺失集合拒绝，MCP 覆盖注册和参数映射。Release 完整 solution 0 warning/0 error，Contracts/Core/MCP `19 + 181 + 23 = 223` 项通过，公共工具面为 53。该批没有启动或部署 DSP、没有游戏/存档写入，也没有把离线测试冒充 live 证据；三厂同 tick 分页、错 cursor、公共 JSON 脱敏和最终安装版 MCP 仍待下一次安全部署复验，受控 stall/recovery 与三类故障门不变。
