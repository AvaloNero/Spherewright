# v0.4 Overseer DSP API 证据

本文记录 v0.4 只读多行星监督链路采用的当前《戴森球计划》运行时接口。它补充 [game-api-m0.md](./game-api-m0.md)，不改变 owned-world、Unity 主线程或普通玩法写入边界。

## 验证基线

- 游戏版本：`0.10.34.28529`
- `Assembly-CSharp.dll` SHA-256：`AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`
- 反编译工具：ILSpyCmd `9.1.0.7988` 与 Mono.Cecil
- 最近复验：2026-09-03

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

### 物流

`PlanetTransport.stationPool/stationCursor` 中的每个活动 `StationComponent` 必须同时满足 station pool、`factory.entityPool`、`EntityData.stationId` 和本地/星际 planet 规则。当前读取的原生字段包括：

```text
StationComponent.isStellar / isCollector / isVeinCollector
StationComponent.energy / energyMax / pcId
StationComponent.storage[]
StationStore.itemId / count / localLogic / remoteLogic / localOrder / remoteOrder
StationComponent.idleDroneCount / workDroneCount
StationComponent.idleShipCount / workShipCount / warperCount
```

普通行星塔、星际塔、轨道采集器和大型采矿机的 vein collector 是四个互斥类别，不能把 `isVeinCollector=true` 的采矿机重复算进行星物流塔。空槽必须同时保持 item/count/order/logic 为空；供需槽、库存和订单幅度只从身份自洽的槽聚合。非轨道采集器的受电状态通过 `station.pcId -> EntityData.powerConId -> PowerConsumerComponent.networkId -> PowerSystem.networkServes` 重新绑定；轨道采集器没有普通地面 consumer，故不进入 powered/underpowered 分母。该摘要只观察真实库存、订单、机队与能量，不派单、不补货，也不创建远端 factory。

### 科研

科研是 active owned `GameMain.history` 的全局状态，不复制成每星球各一份。当前程序集提供 `currentTech`、`techQueue`、`techStates`，每个 `TechState` 含 `unlocked/curLevel/maxLevel/hashUploaded/hashNeeded`；`LDB.techs` 的 `TechProto.Items/ItemPoints` 给出当前等级的矩阵要求，且 `TechProto.kPointPerItem=3600`。总件数和剩余件数沿用游戏自身的整数公式：

```text
itemCount = hashCount * pointsPerHash / 3600
```

摘要返回一份当前科技、上传/所需/剩余 hash、有序队列、运行时 tech-state 总数、已解锁数及逐物品预算；队列中的每个非零 ID 必须同时存在于当前 `LDB.techs` 和 `techStates`，负数或残留身份会 fail closed，矩阵身份只按当前 `TechProto.matrixIds` 判断。所有星球页都引用首屏捕获的同一份深复制科研状态和同一 `capturedAtGameTick`，不会把后续 tick 的科研进度拼进旧游标快照。独立调用本地电力、物流或科研工具可能在相邻 tick 执行；除非 `capturedAtGameTick` 相同，否则逐字段差异是正常的动态状态，不能拿来否定或拼接当前 Overseer 快照。最终实机对照仅相隔 2 tick，母星需求/实际发电就从 `90688` 变为 `79388`，而各自快照内部仍满足 generated/served 和 exported 语义。

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

同档正例从黄糖 `6003` 的 matrix lab `774` 起步，缺金刚石 `1112` 后沿真实入料仓/分拣器链命中唯一熔炉 `715`，再由该炉当前输入读到缺高能石墨 `1109`。最终 path 为 `matrix_lab 774 / 6003 -> material 1112 -> assembler 715 / 1112 -> material 1109`；finding 的 object/item 指向当前最深的已诊断设备和产物，而 path 首节点仍保留调用方请求目标。该样本没有触发停止原因，说明是在自然叶节点结束，而不是预算截断。

运输进展需要跨 tick 比较。单个快照只公开当前配置、订单、源库存和载具数；它不会把“此刻有订单但没观察到位移”写成 stalled。当前可以确认的物流故障仅包括：物理需求路径存在但没有匹配 supply 配置，或匹配 source inventory 为正而供需两端可用/工作载具总数为零。具备载具的订单停滞仍需后续持久化时间窗证明。fractionator、gamma receiver 和 orbital collector 已计入理论产能，但尚未接入同等级直接缓冲诊断；请求物品若由这些设备直接生产，`directDiagnosticCoverage=partial`。

所有 finding 都包含同 tick 的设备、物料和可证明物流节点；它们是瞬时诊断，不是跨 tick 不变事实。实机中同一制造台 `715` 曾在网络服务率约 `0.94038` 时返回 `insufficient_power`，供电恢复后又自然变为缺 item `1109` 的 `material_shortage`，证明调用方必须按 `capturedAtGameTick` 使用证据，不能缓存旧根因继续写入。

## 有界输出与诊断限制

- 生产行必须由调用方提供去重后的有效物品 ID，首个切片最多 64 个；不接受“返回所有物品”的无界请求。
- 工厂遍历同时受 `factoryCount`、数组长度和 512 个已创建 factory 的显式上限约束；每页最多 16 个 planet，快照保持 60 秒且绑定 session、请求类型与页大小。
- 跨域摘要最多扫描 4096 个 power-network pool 槽、65536 个发电组件引用和 4096 个 station pool 槽；每站最多 64 个 storage slot，科研队列最多扫描 4096 项、返回 64 项，runtime tech catalog 最多扫描 12000 项。理论产能另限制最多扫描 131072 个 assembler/lab/miner/fractionator/generator/station pool 槽和 262144 个 recipe input/output、矿点及采集物引用。超过预算返回明确的非重试 `SERVER_BUSY`，不静默截断聚合总量。
- 直接诊断另受 131072 个组件/拓扑节点和 262144 个配方、来源及站槽引用的总预算；每个 item 最多返回 16 条 finding，每个 planet 最多返回 16 条无已知 item 身份的基础设施 finding，同时保留未截断总数和 `truncated` 标志。预算溢出会让整份首屏 fail closed，不能返回不完整总量。
- 当前立即生产者诊断和递归生产者都只覆盖 assembler、matrix lab 和三类 miner；fractionator、gamma receiver 与 orbital collector 作为请求物品的直接生产者时会显式令 `directDiagnosticCoverage=partial`。递归只沿同星球、物料兼容且物理可达的受支持生产者，最多 8 层/64 个 producer；跨星球生产者和未支持设备不会被猜测补全，路径预算/环路停止会进入 evidence。
- 单次快照不能确认“有载具的物流订单没有进展”；该分支继续 fail closed，等待跨 tick 的 per-save 时间窗。受控缺料、断电、物流阻塞三类故障制造/修复和保存恢复验收仍未完成。
- 原始 owned save 名、由它派生的持久化 key、auth token、plan token、绝对路径和运行时描述文件不得进入公共 DTO 或诊断包。

## 已完成的运行时切片

第一条 v0.4 纵向切片在精确 owned session 中，以一个有界物品 ID 集合读取全部已创建工厂的原生 600-tick 自动生产/消耗窗口，并为每行返回星球身份、实际每分钟速率和窗口来源。

第二条纵向切片用独立的 session/页大小绑定快照返回每星球电网与物流聚合，以及一份全局当前科研摘要。前两条切片都不创建 factory、不加载远端显示、不写共享统计缓存。它们已经提供故障诊断的输入，但尚未把设备缓冲、物流路径和矿源余量组合成最终根因，因此不提前声称完整 v0.4 验收完成。

第三条纵向切片把当前程序集的理论产出公式作为纯 Core 计算器接回第一条生产快照；Plugin 只负责有界、身份绑定的运行时输入扫描。实机先暴露并修正“耗尽矿机 `veinCount=0` 时数组可为空”的合法边界，随后同一三工厂存档完整返回 `complete`：母星蓝/红/黄矩阵分别为 `20/10/7.5 min⁻¹`，铁/铜/石/煤矿由实际覆盖 `14/2/3/7` 个矿点闭合为 `420/60/90/210 min⁻¹`，水泵为 `50 min⁻¹`，油井按当前矿量为 `133.15919494628906 min⁻¹`；远端未显示工厂仍给出硅/钛 `120/60 min⁻¹`。冶炼、化工和制造配方也与实体数量闭合。理论/利用率现已完成，仍待把设备缓冲、物流边和矿源状态接入运行时故障分类与上游根因图，因此不提前声称完整 v0.4 验收完成。

第四条纵向切片把纯 Core 首因分类器接到真实 assembler/lab/miner 缓冲、power consumer/network、recipe、vein 和 station/belt 拓扑。最终同档 tick `13945617` 的自然现场同时返回：铁矿机 `1213/1496` 与冶炼炉 `10` 的 `output_blocked`，制造台 `530` 缺 item `1004` 的 `material_shortage` 并附带 `104:1657 -> 102:44` 物流供需路径，matrix lab `774` 缺 item `1112`，以及三台已丢失产品身份的耗尽矿机基础设施 finding。较早一次同版本读取还在制造台 `715` 上捕获约 `0.94038` 供电比的 `insufficient_power`；供电恢复后标签随同 tick 状态改为缺 item `1109`。item `6002` 的实际产量为 `12 min⁻¹` 时没有因瞬时设备输入状态产生误报。完整 solution 0 warning/0 error，174 项测试通过；最终部署 Plugin SHA-256 为 `D40D6BEA4E76697EB14C5F1DE3B0CC61532E4BF634125E1A9488D5024FDF59E1`，normal save `13943810`、exact-primary resume/auto-save `13943842` 均保持同一 owned world。最终 `limit=1` 三页在 tick `13986388` 共享快照并以 `STALE_CURSOR` 拒绝错绑 filter；源码 MCP `0.4.0.0` 通过协议 `2025-06-18` initialize、50-tool list 和 live call。只读审计 tick `13990990+` 仍为 healthy、Journal durable、无 blocker/checkpoint/prebuild。该切片完成直接设备层，不等于递归上游、时间型物流停滞和受控故障门已经完成。

第五条纵向切片把缺料 finding 递归到同 tick、同星球、物料过滤允许且物理可达的上游生产者。首个候选已在 tick `14028962` 从 lab `774` 追到 diamond assembler `715` 的高能石墨短缺；复核后先把每种输入拆成独立遍历并要求路径上每个 sorter filter 相容，最终又按本机程序集核对 `SplitterComponent.SetPriority` 与 `CargoTraffic.UpdateSplitter`，把 exact output slot/belt 身份和“优先口只收过滤物、其他口排除过滤物”的双向规则纳入遍历。Core 为根因图与 splitter policy 提供 14 项回归，完整 suite 为 188 项（Contracts 17、Core 150、MCP 21），solution 0 warning/0 error。最终源码相等部署为 Plugin `46E62CC930CAD0756BBFB06625C9585F04A074B25FEDC15C4D7DCE2A322F4B70`、Contracts `507C57A10C49435C8D0AEF71F49DA5C6255711C9093D651CBD1A57F91088055F`、Core `CA8B33DD66330211ECD78E535CBD05932933278AF6E640BE67AF7AFD6301E7C5`；普通保存 tick `14109460` 后正常关窗，exact-primary 只恢复 planet `104` 并自动重存 tick `14109491`。最终 Bridge live tick `14111293` 与最终构建后的源码 MCP tick `14138110` 都返回 `lab 774 / 6003 -> material 1112 -> assembler 715 / 1112 -> material 1109`，无 trace-stop evidence；MCP `0.4.0.0` 完成协议 `2025-06-18`、50-tool list、明确 sorter/splitter filter 的工具描述和同一路径调用。最终三页生产/摘要分别共享 tick `14119083/14119093`，错绑 filter cursor 以 `STALE_CURSOR` 拒绝；审计 tick `14118310+` 为和平、非沙盒、1×、healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、0 prebuild、无 blocker/checkpoint。跨星生产者、时间型物流停滞和三类受控故障门仍未完成，本切片没有 tag 或 Release。
