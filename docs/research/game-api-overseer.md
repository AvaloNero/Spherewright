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

这些 `ref*` 字段是按 UI 请求重算的可变缓存，`ProductStat.Import` 不恢复它们，也没有可验证的新鲜度标记。Overseer 第一条运行时切片不得直接复用一个可能过期的 `refProductSpeed`；在 Spherewright 以深复制输入重现并测试当前版本公式以前，理论速率与利用率必须返回 `null` 并显式标记 `theoreticalCoverage=unavailable`。后续实现公式时要按组件类型报告 coverage，未知类型不能用零冒充已计算容量。

拒绝方案：

- 不调用共享 `ProductionExtraInfoCalculator` 只为读取而改写 UI 统计缓存。
- 不读取未带时间戳的 `refProductSpeed` 后声称它是当前容量。
- 不用实际峰值近似理论值；这会把长期缺料误当成低设计容量。

## 供电、物流与科研

每个已存在 `PlanetFactory` 持有自己的 `PowerSystem`、`PlanetTransport`、`FactorySystem` 和存储/运输池。现有本地读取已验证 `PowerSystem.netPool/netCursor`、物流塔组件/槽位/订单和生产设备缓冲语义；v0.4 复用相同字段，但工厂来源改为上述精确 owned `GameData.factories` 枚举，并在每个对象上重新验证 component ID、entity ID、planet ID 和数组界限。

科研是 owned `GameData.history` 的全局状态，不复制成每星球各一份。多行星摘要只返回一份当前科技、队列和已解锁状态，并带同一 `capturedAtGameTick`。任何详情读取若跨 tick 重新抓取，必须作为新快照而不是拼接到旧摘要。

## 有界输出与诊断限制

- 生产行必须由调用方提供去重后的有效物品 ID，首个切片最多 64 个；不接受“返回所有物品”的无界请求。
- 工厂遍历上限由当前 `factoryCount` 和数组长度共同约束；结果仍要带 `factoryCount`、返回数和 `truncated/incomplete` 语义，不能静默漏项。
- 当前原生 600-tick 窗口可以稳定给出实际产量，但不能单独解释停产原因。供电不足、缺料、输出堵塞、物流阻塞和矿脉耗尽只有在设备身份、完整周期、缓冲容量、物流订单/源库存和矿脉余量证据齐全时才能标为 confirmed。
- 现有通用 `ProductionFaultClassifier` 已 fail closed 等待 ready window 和完整周期。设备输出缓冲的真实容量尚未完成当前程序集逐类型复核，所以第一条多星球速率切片不输出“confirmed output_blocked”。
- 原始 owned save 名、由它派生的持久化 key、auth token、plan token、绝对路径和运行时描述文件不得进入公共 DTO 或诊断包。

## 首个运行时切片

首个 v0.4 纵向切片只承诺：在精确 owned session 中，以一个有界物品 ID 集合读取全部已创建工厂的原生 600-tick 自动生产/消耗窗口，并为每行返回星球身份、实际每分钟速率、窗口来源和明确的理论 coverage。它不创建工厂、不加载远端显示、不写共享统计缓存，也不提前声称完整 v0.4 故障诊断已完成。
