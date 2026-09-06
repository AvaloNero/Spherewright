# Spherewright 首次问题与代码修复记录

更新时间：2026-09-07（Asia/Singapore）

本文件专门记录项目第一次遇到的可复用工程问题：现场症状、根因、代码或协议
修复、验证证据和仍有限制。它不是逐局流水账，也不是当前规则的唯一来源。
事故在发生存档的日记中仍可保留；修复形成的现行规则以
[experience-ledger.md](./experience-ledger.md) 为准，DSP API 事实以
[`docs/research/`](./research/) 为准。

状态取 `fixed | mitigated | open`。`fixed` 只表示写明范围内已有代码和验证证据，
不代表跨 DSP 版本永久成立。
需明确验证层级时使用子状态`fixed_offline`或`fixed_offline_live_pending`，不能将其读作实机已通过。

## IFX-001 — 已接受动作被客户端展示错误误报为失败

- 首见：2026-08-31，`owned-world-001`。
- 症状：游戏动作已经完成，但 PowerShell/客户端访问不存在字段或处理空集合时报错；
  若按调用失败重新提交，会重复采集、转移、建造或保存。
- 根因：传输/展示层失败与 Plugin 是否接受、执行动作是两个不同事实。
- 修复：统一执行 `prepare → 单一幂等 commit → action result/fresh 双边读回`；
  accepted 后禁止换键重放，客户端错误即停并进入证据化核销。
- 验证：采集、转移、建造和保存均出现过“客户端失败、fresh 状态已完成”的独立样本，
  没有发生重放。
- 关联：EXP-007、EXP-013；状态：`fixed`。

## IFX-002 — 同位置/同设备分拣器被错误归属或覆盖槽位

- 首见：2026-08-31，精炼厂同源输出；2026-09-01 在熔炉上复现槽覆盖。
- 症状：仅凭姿态把旧 sorter 当作新 sorter，或新 sorter 占用设备已有 slot，造成旧连接断开；
  belt 虚拟 slot `-1` 还曾导致已成功连接被错误隔离。
- 根因：候选集合未排除既有实体/已占设备槽，完工只校验新 sorter 自身而未反查设备端。
- 修复：prepare 排除已占 slot 和旧实体；commit 后验证双向端点及设备实际持有本次 sorter；
  belt 端的虚拟 slot 改为扫描真实连接槽，非 belt 端保持精确槽验证。
- 验证：同端点旧/新 sorter 能被唯一归属；熔炉输入槽 8 与输出槽 0 同时保留；
  三条 machine↔belt 连接通过双向读回。
- 关联：EXP-012、EXP-027、EXP-068、EXP-070；状态：`fixed`。

## IFX-003 — 移动卡在基座/设备之间并持续耗能

- 首见：2026-08-31，密集设施区；随后在液罐基座、带区和处理器区复现。
- 症状：订单仍 active，但位置或到目标的最佳距离不再改善，最终可能耗尽能量；
  早期终态还会残留不属于当前动作的底层订单。v0.3.1 虽已有看门狗，线上 ZIP 却没有
  playbook/experience/ledger，失败只用自然语言说明；普通外部 Agent 因而不知道开局着陆舱和
  密集基座应如何有界脱困，容易重放相同目标。
- 根因：最初只看全局 timeout，未区分位移停滞、目标进展、能源饥饿和订单归属；随后验证出的
  5 m 单障碍背离、四个 4 m 正交探测和“业务 prepare 已过就别撞中心”仍只存在约 600 KB 的仓库
  experience ledger，没有进入 MCP capability discovery 或发行包。`prepare_move` 本身也只验证
  球面目标，不是碰撞路径预演器。
- 修复：保留位移/最佳距离双 watchdog、能源暂停与恢复窗口重置和精确 `OrderNode` 所有权；
  action result 增加 failure kind、停滞 tick、剩余距离、禁止同目标重试和有界恢复参数。新增可由
  普通 Host `resources/list/read` 直接取得的精简 MCP playbook，并把同一份约 2.6 KB 文档放入发行包；
  所有候选仍使用现有 prepare/commit Move，不在 Plugin 内寻路、传送或写位置。
- 验证：既有实机多次证明 180-tick 满能量卡脚会在耗尽前终止且只清理 owned order；新增默认
  180/600-tick、结构化 advice、Resource 发现/读取、新世界提示和包内文件自动测试通过，dirty
  0.4.0 预演包以真实 stdio 读回 1 个 resource。候选二进制的新档完整实测中，飞行舱只在
  vegetation resource 可见；首个正交 4 m Move 于 181 tick 返回结构化 `position_stalled`，未重放
  原目标，第二方向成功，随后直接通过 harvest prepare 并完成首份铁矿采集。全程无键鼠、传送或
  位置写入，所有 committed action 均有 terminal 结果。
- 关联：EXP-036、EXP-039、EXP-057、EXP-061、EXP-076、EXP-179；状态：`fixed`。

## IFX-004 — 飞行检查点可在成功后回滚数小时进度

- 首见：2026-09-01，对早期 flight checkpoint 生命周期复核时发现。
- 症状：旧检查点文件在飞行成功和后续主档保存后仍可被 reload，可能让世界倒退，
  而外部 Journal 不随之回滚，形成时间线分叉。
- 根因：票据只有“结构有效”，没有 flight 绑定、retired/consumed/superseded 生命周期。
- 修复：检查点绑定单次飞行；明确失败可反复重载；稳定抵达即撤销 reload capability，
  覆盖主档成功后 durable retire，并从 session 移除旧 token/capability。
- 验证：真实失败重试仍可用；后续成功飞行与保存后 fresh 状态不再暴露旧 checkpoint。
- 提交：`3f8f2be`；关联：EXP-082–084；状态：`fixed`。

## IFX-005 — Journal 内存事件被误当作已经持久化

- 首见：2026-09-01，逐存档 Journal 首次实装后的耐久性复核。
- 症状：写盘失败时事件已在内存 Entries 中，读取者可能据此提交里程碑；进程退出后事件消失。
- 根因：观察 DTO 没有区分已生成序号和 durable-through 边界。
- 修复：暴露 `durableThroughSequence`、`persistencePending`、`persistenceError`；
  里程碑只接受 durable 序号，并以运行态、普通保存和生产差量交叉证明。
- 验证：后续首次科技/升级/产线事件均复读 durable-through 与无 pending/error。
- 提交：`96c9232`、`3f8f2be`；关联：EXP-048；状态：`fixed`。

## IFX-006 — 健康恢复可能优先加载不相关的 LastExit，票据还能复活

- 首见：2026-09-01，planned-restart 复核。
- 症状：仅凭 LastExit 文件时间可能先加载另一世界再做身份后验；一次性票据若删除失败可复活。
- 根因：健康重启和隔离恢复共用选择逻辑，消费缺少 durable tombstone。
- 修复：健康 planned restart 只加载 ticket-bound exact primary；quarantine 的 LastExit 候选先读
  header 并满足 minimum tick；消费写入 token-hash tombstone，恢复后重新签发 session/票据。
- 验证：多轮“保存签发→正常关闭→exact-primary 恢复→重新签发”通过；旧 token 被拒绝。
- 提交：`3f8f2be`；关联：EXP-069、EXP-071、EXP-083、EXP-084；状态：`fixed`。

## IFX-007 — 物流塔充电实时请求量被误作配置上限

- 首见：2026-09-02，首座物流塔配置准备阶段。
- 症状：塔在正常充电时 requested power 持续变化，使配置哈希 stale，且可能把读数错误展示成上限。
- 根因：混淆 station `energyPerTick` 与 consumer `workEnergyPerTick`。
- 修复：DTO 和哈希分离实时需求与配置最大值；配置只允许已验证的 3 MW UI 步进。
- 验证：首塔 12→6 MW 和 ILS 60→30 MW 均保持库存不变并通过配置/供电复读。
- 提交：`c61f58f`、`1aff9b3`；关联：EXP-099、EXP-101；状态：`fixed`。

## IFX-008 — 活跃科研上传让选择动作持续 stale

- 首见：2026-09-02，粒子磁力阱研究期间。
- 症状：科技上传每 tick 改变完整 progression hash，安全追加下一科技连续被拒绝。
- 根因：选择动作把无关的实时上传量纳入并发前提。
- 修复：新增稳定 selection hash，只绑定队列、解锁和前置条件；完整进度哈希仍用于观察。
- 验证：活跃研究下安全追加成功，队列 fresh 复读为预期顺序。
- 提交：`b185d1e`；关联：EXP-063、EXP-100；状态：`fixed`。

## IFX-009 — 活跃 sorter 不能安全改过滤导致串料

- 首见：2026-09-02，蓝矩阵被无过滤出口送入堵塞环带。
- 症状：已有拓扑正确但货物去向错误；直接修改带货 sorter 又可能改变在途货物语义。
- 根因：缺少只针对 cargo-free 稳定窗口的配置计划与哈希。
- 修复：仅在无携货且排除返程进度的窗口允许配置，并绑定实体、拓扑、filter 和携货状态。
- 验证：蓝矩阵 sorter 与后续旧石转硅入口均在空载窗口改过滤，未重建或复制货物。
- 提交：`368900d`；关联：EXP-102；状态：`fixed`。

## IFX-010 — 本地物流站的原生 planetId=0 被误判为外星实体

- 首见：2026-09-02，首座 PLS 正常施工后。
- 症状：合法本地 PLS DTO 被旧身份规则拒绝。
- 根因：DSP 用 `planetId=0` 作为本地站哨兵，而星际站使用精确星球 ID。
- 修复：本地站只接受 `0` 或当前 planet，ILS 继续要求精确 planet；四个入口共用纯策略。
- 验证：部署恢复后 PLS 实体完整可读，foreign identity 仍被拒绝。
- 提交：`e005ea1`；关联：EXP-108；状态：`fixed`。

## IFX-011 — 矿机采用首个合法角度导致覆盖浪费

- 首见：2026-09-02，远端硅矿机只覆盖 2 个可用节点。
- 症状：建造合法但吞吐显著低于同一站址可达到的覆盖数。
- 根因：候选搜索遇到首个合法 yaw 就停止，没有比较合法方案的 vein coverage。
- 修复：枚举原生合法候选，以覆盖节点数优先并保留确定性 tie-break。
- 验证：正常拆除回收后以 yaw 150° 重建，计划与实体均覆盖 4 个节点。
- 提交：`bc86707`；关联：EXP-112、EXP-130；状态：`fixed`。

## IFX-012 — 海上落点被误当作尚未抵达

- 首见：2026-09-03，planet `104` 返航。
- 症状：伊卡洛斯已经属于目标星球但停在海面，旧逻辑等待陆地状态而无法收尾。
- 根因：稳定抵达判定没有把海面落点与太空飞行分开，也没有原生上岸恢复。
- 修复：识别目标星球海面归属，扫描附近干燥邻域并只下达原生 Drift `MoveTo`，
  上岸后以稳定 Walk 窗口验收。
- 验证：24.6 m 邻近陆地点一次上岸，稳定 600 tick 后正常保存并退役 checkpoint。
- 提交：`59aac03`；关联：EXP-114、EXP-116；状态：`fixed`。

## IFX-013 — 物流塔输出端口选择器的原始索引偏一

- 首见：2026-09-03，PLS 钛块输出线。
- 症状：传送带和 sorter 都完工，但塔端 raw `storageIndex=0` 仍表示 None，库存不出塔。
- 根因：DSP 端口选择器使用一基槽索引；0 不是第一个物品槽。
- 修复：新增受控输出选择器动作，将公开槽映射为原始 `slot + 1`，并在 commit 后双向读回。
- 验证：raw 0→1 后钛块真实流过 36 段带，需求塔无人机补货且钛晶石上游恢复。
- 提交：`3e850d7`；关联：EXP-117；状态：`fixed`。

## IFX-014 — 飞行直线被中间天体捕获

- 首见：2026-09-03，母星到远端资源星航行。
- 症状：两次飞行都被中间气态巨行星捕获，单纯重试相同路径会确定性失败。
- 根因：导航只面向目标星球，没有对中间天体的捕获半径做路径避让。
- 修复：从结构化星系状态计算中间天体，生成确定性的安全绕行段；仍使用原生飞行控制。
- 验证：同一 checkpoint 上部署后成功完成 `104 → 102`，落地保存后 checkpoint 退役。
- 提交：`c365d12`；关联：EXP-141；状态：`fixed`。

## IFX-015 — 长距离 sorter bridge 建成但没有供电

- 首见：2026-09-03，母星 ILS 硅路第二座桥。
- 症状：拓扑、过滤和端点均正确，sorter 却显示 network 0，硅流停住。
- 根因：几何施工成功不证明消费者位于任何电网覆盖中。
- 修复：当次存档通过正常建造电塔 `2031` 接入 network 1；代码层继续要求消费者完工后复读
  `powerNetworkId` 与供电比，自动诊断/修复留给 v0.4 Overseer。
- 验证：电塔落成后 sorter 立即携带硅，熔炉连续工作、成品仓增长。
- 关联：EXP-021、EXP-145；状态：`mitigated`。

## IFX-016 — 发布包与实时 Plugin 报告不同版本

- 首见：2026-09-03，首次 `0.3.0` 干净安装实机回归。
- 症状：ZIP manifest 与自包含 MCP 都是 `0.3.0`，但实时 BepInEx Plugin/Bridge 仍报告
  `0.1.0`；功能握手成功也不能证明装入的是预期发布版本。
- 根因：Plugin 的 BepInEx metadata 和 MCP 握手客户端版本各自保留了早期硬编码常量，
  与 MSBuild `Version`、manifest 和 MCP server assembly 没有共同来源；原包测试只验证 MCP
  初始化和工具表，没有对实时 Plugin 版本设断言。
- 修复：新增 Contracts 中唯一的 `SpherewrightProduct.CurrentVersion`，Plugin metadata 与 MCP
  客户端共同引用；`Directory.Build.props` 为开发构建设置同一版本前缀。打包脚本从已构建
  Contracts 读取该常量并拒绝命令行版本不一致，manifest 同步写入 `productVersion`；包测试
  校验 manifest/MCP 版本，live smoke 新增 `ExpectedPluginVersion` 严格断言，并自动优先使用
  仓库 portable SDK。
- 验证：119 项测试通过、完整 solution 0 warning / 0 error；Mono.Cecil 读回 Plugin assembly
  `0.3.0.0` 与 `BepInPlugin(..., "0.3.0")`。最终 clean commit `a52ff44` 生成的 ZIP 本体经
  重新干净安装后，228 个运行文件与 ZIP payload 零差异；live Bridge 报 `0.3.0`，错误 token
  被拒绝，安装版 MCP `0.3.0.0` 经 stdio 成功调用同一 Bridge，受保护同档恢复并自动保存到
  tick `13516415`。线上 `v0.3.0` Release 的 ZIP digest 与本地最终工件一致。
- 关联：EXP-001、EXP-030、EXP-152；状态：`fixed`。

## IFX-017 — 防御场余电导出被误标为发电量

- 首见：2026-09-03，v0.4 多星球供电摘要首次实机读取。
- 症状：planet `104` 的网络明明有 33 个发电组件、消费者供电比为 1，旧 DTO 却报告
  `energyGenerated=0`；planet `102` 的 10 个发电组件也出现相同矛盾。
- 根因：早期本地电力读取把 `PowerNetwork.energyExport` 映射为 `EnergyGenerated`。当前程序集的
  `PowerSystem.GameTick` 证明该字段只是在有余量且存在防御场需求时送入 `PlanetATField` 的能量，
  实际单机发电写在 `PowerGeneratorComponent.generateCurrentTick`。
- 修复：逐网络验证 `generators` 中每个组件的 ID、network ID、数组边界和非负计数，再以 checked
  sum 形成 `EnergyGenerated`；原生 `energyExport` 单列为 `EnergyExported`。本地电力工具与新的
  Overseer 聚合共用同一捕获路径，重复/失配组件 fail closed。
- 验证：最终修复版的同一 Overseer 快照在 planet `104` 读到 required/served/generated
  `90688/90688/90688`、capacity `191000`、exported `0`；planet `102` 的多次读取均为
  `4050/4050/4050/55000/0`。相邻的本地工具调用在 2 tick 后读到母星
  `79388/79388/79388/191000/0`，既证明新映射不再恒为 0，也证明跨 tick 动态需求不能要求数值相等。
  最终源码二进制重新部署并受保护恢复后，同快照 tick `13773036` 再次返回母星 generated/exported
  `94688/0` 和远端 planet `102` 的 `4050/0`。150 项测试和完整构建通过。
- 关联：EXP-021、EXP-142、EXP-156、`docs/research/game-api-overseer.md`；状态：`fixed`。

## IFX-018 — 耗尽矿机的空来源数组阻断整份理论产能快照

- 首见：2026-09-03，v0.4 理论产能首轮实机读取。
- 症状：三座工厂的只读生产请求整体返回 `BRIDGE_NOT_READY`，消息为
  `An active vein miner has an invalid source-node index`；其余设备无法获得理论值。
- 根因：理论扫描先要求 `MinerComponent.veins` 非空，再判断 `veinCount==0`。当前 DSP 会保留
  已耗尽的满电矿机组件，同时把来源数降为 0 并允许来源数组为空；原生 UI 公式先看
  `veinCount > 0`，否则该矿机自然贡献 0。
- 修复：保留负数为非法，但把 `veinCount==0` 提前作为合法零容量终态；只有正来源数才验证
  数组、当前索引、全部 vein/product 双向身份和扫描预算。纯 Core 同时增加零 source multiplier
  回归测试，未知/越界正来源仍 fail closed。
- 验证：fresh 实体读回证明矿机 `14/263/796` 均为 network `1`、serve ratio `1.0`、
  `resourceNodeCount=0`。修复版经普通保存、正常关窗、7 文件零差异部署和 exact-primary 恢复后，
  同一存档完整返回三厂 `theoreticalCoverage=complete`；有矿机的覆盖点数精确闭合理论速率，
  三台耗尽矿机贡献 0。完整 solution 0 warning/0 error，160 项测试通过。
- 关联：EXP-085、EXP-112、EXP-157、`GameStateReader.TryCaptureMinerTheoreticalRates`；状态：`fixed`。

## IFX-019 — 物流诊断在中转仓处提前停止，漏掉真实塔路径

- 首见：2026-09-03，v0.4 直接设备诊断首次追踪钛晶石制造台 `530` 的缺硅输入。
- 症状：制造台正确返回 item `1004` 缺料，但 finding 只有设备与物料节点，没有已知的母星需求塔
  `1657` 和远端供应塔 `44`；同一现场明明已经由 ILS 把硅送入生产区。
- 根因：首版反向拓扑搜索只把 belt/splitter/piler/spraycoater 当作货运通道。实际链先由 sorter
  `532` 从 storage `259` 取料，而该仓又由 sorter `1784` 从 ILS 长带入库；搜索到仓即停止，
  因而永远碰不到 station output belt `1783`。
- 修复：仍从精确 consumer input sorter 的 `pickTarget` 出发、仍只沿 `ReadObjectConn` 入边反向遍历，
  但把 inserter、storage 和 tank 纳入允许的有向货运中继。只有命中 station slot 精确绑定的 output
  belt/entity 才附加 demand；随后才按 item 和 local/remote 模式寻找 supply，未放宽为同星球或同物品猜测。
- 验证：修复版 live finding 的路径为 `assembler 530 -> material 1004 -> logistics demand 104:1657
  -> logistics supply 102:44`，同时返回 source inventory `28` 和去重 carrier count `2`。由于只有单次
  快照且没有 outstanding order，它保持 `material_shortage`，没有误报 `logistics_blocked`。修复版先普通
  保存、正常关闭，再以 source-equal 七文件部署并通过 exact-primary 恢复；最终 Plugin hash 为
  `D40D6BEA4E76697EB14C5F1DE3B0CC61532E4BF634125E1A9488D5024FDF59E1`，174 项测试与完整构建通过。
- 关联：EXP-117、EXP-123、EXP-144、EXP-159、`TryFindDirectDiagnosticDemandBindings`；状态：`fixed`。

## IFX-020 — 物流时间窗按路线同步写盘会放大只读诊断成本

- 首见：2026-09-03，v0.4 物流时间窗提交前最终代码审查。
- 症状：每个配置过物流输入的生产设备都会调用一次窗口观察；首版观察函数每遇到一条新路线就排序并
  原子替换整个受保护文档。大型工厂的一次只读生产快照因此可能变成多次主线程同步磁盘写入，且后续
  路线失败时前半批已持久化，不能形成一次请求的完整 durable 边界。
- 根因：路线发现、窗口分析和文档提交被合并在单条 `ApplyRouteEvidence` 路径中，没有先完成全部 owned
  factory 的深复制，也没有把公共 DTO 的 temporal evidence 延迟到整批持久化成功之后。
- 修复：先捕获所有工厂的直接诊断和去重路线样本，再由 `TryObserveBatchOnMainThread` 对每条路线计算
  proposed state、统一执行 4096-route 淘汰，并只做一次 secure-new/flush/atomic-replace。只有整批成功后
  才把 analysis 回填各 material；失败则保留普通瞬时诊断、时间证据为 unknown。同时把 qualifying 条件
  收紧为消费者真缺一周期输入和需求端正 reservation，供足样本会清除旧停滞基线。
- 验证：新增消费者供足重置回归后 204 项测试通过、完整 solution 0 warning/0 error。最终四 DLL
  source/deployed 哈希一致；同档恢复后保护文档仍为 3 条哈希路线、current-user-only DACL、无原始
  save identity，并同时记录 `consumerInputMissing=false/true`，三厂分页与黄糖四节点根因不回归。
- 关联：EXP-001、EXP-030、EXP-154、EXP-159、EXP-164、`OverseerLogisticsProgressStore`；状态：`fixed`。

## IFX-021 — 聚合多个供应塔时可能把公开路径与其他塔的生产者拼接

- 首见：2026-09-03，v0.4 跨星生产者递归首版离线审查；该版未部署。
- 症状：路线证据会汇总所有匹配 supply endpoint，但公开 `UpstreamPath` 只能显示一座主 supply station。若把所有供应端的上游候选都注册给同一 material，resolver 可能选到塔 B 后的生产者，而 path 仍显示塔 A。
- 根因：库存/机队是合理的路线集合指标，但生产者递归是必须保留单条物理来源的路径证明；首版把两种语义共用了同一 supply 列表。
- 修复：路线总库存、carrier 与时间窗仍使用全部匹配 supply；另外选定一个将写入公开 path 的精确 endpoint，且只注册该 endpoint 的 Input-belt 反向候选。优先有物理输入路径的站，再按库存与稳定身份排序。
- 验证：编译和 205 项测试通过；live 钛块 path 中显示的 supply `102:44` 与其 Input-belt 后绑定的矿机 `102:1` 处于同一条证据链。
- 关联：EXP-159、EXP-162、EXP-165、`OverseerDiagnosticLogisticsIndex.ApplyRouteEvidence`；状态：`fixed`。

## IFX-022 — 无 continuation 的完整首屏耗尽分页快照容量

- 首见：2026-09-03，为捕获真实星际运输活动而高频读取 Overseer 摘要时。
- 症状：当前世界只有 3 座 factory，`limit=16` 的首屏每次都完整返回且 `nextCursor=null`；连续读取后仍会命中
  `SERVER_BUSY`，仿佛存在尚待翻页的快照。
- 根因：`SnapshotPageStore.TryCreate` 在判断是否需要 continuation 之前就检查容量并保存每个 snapshot。
  完整首屏没有任何 cursor 能再次引用该记录，却仍占用 60 秒有界容量。
- 修复：先构造不可变记录；当 `items.Count <= pageSize` 时直接生成一次性首屏而不写入 continuation store。
  只有确实存在下一页的记录才检查并占用容量，原有 session/scope/filter/page-size/expiry 绑定不变。
- 验证：新增 Core 回归证明 16 个完整首屏不占一个容量槽、真正分页仍占槽且满载拒绝、容量满时完整首屏仍可用。
  Release 完整构建 0 warning/0 error、206 项测试通过。源码相等部署后，同档 live 连续 16 次完整三星球首屏
  全部成功；8 个 `limit=1` 首屏占满容量，第 9 个按设计返回 `SERVER_BUSY`，此时完整首屏与既有
  continuation 仍分别成功。
- 关联：EXP-125、EXP-156、EXP-166、`SnapshotPageStore<T>`；状态：`fixed`。

## IFX-023 — 新版候选包内的安装说明仍把自己称为 v0.3.0

- 首见：2026-09-04，从 clean commit `f43c8ce` 生成并实装首个 v0.4.0 候选包后复读包内 `INSTALL.md` 时。
- 症状：manifest、Plugin 和自包含 MCP 都正确报告 `0.4.0`，但安装说明首句仍写死“`v0.3.0` release package”。安装步骤本身可用，却会让试用者误判包版本，也破坏 Release notes 与工件自描述的一致性。
- 根因：`package-release.ps1` 原样复制仓库的通用 `docs/release-installation.md`；该文档在 v0.3 发布时把版本号写成常量，版本源统一只覆盖程序集和 manifest，没有覆盖这段人类可读文字。
- 修复：把安装说明改成不写死 Spherewright 版本的通用表述，明确以 `manifest.json` 和安装器回显为 exact version 权威；DSP/BepInEx 已验证版本仍显式保留。首个 `f43c8ce` 包降级为预演证据，不能作为最终候选，必须从包含本修复的 clean commit 重新打包并复读包内说明。
- 验证：修复后先用源码检索确认通用说明不再含旧版本常量；最终状态还要求新包 manifest/source commit、包内 `INSTALL.md`、自包含 MCP 版本和 live 安装结果一致。
- 关联：EXP-152、EXP-153、`scripts/package-release.ps1`、`docs/release-installation.md`；状态：`fixed`。

## IFX-024 — 隔离验证恢复了世界票据，却没有恢复同档 Journal

- 首见：2026-09-05，v0.3.3 本机兼容性验证后重回 `owned-world-001`。
- 症状：受保护恢复首先正确载入最后一个早期测试副本，而非长期世界。在使用受保护的归档归属证据恢复长期世界后，世界的 planet/tick/ownership 全部正确，但运行时生成了一份 `attached_existing_save` 的 0-entry Journal，与该档已证明的 `49/49` 时间线冲突。
- 根因：隔离验证把 Plugin/描述文件/交接票据当成一组环境状态，却没有把按 owned-save hash 分开的 Journal 目录纳入同一备份与恢复事务。恢复票据又只绑世界身份、planet 和 minimum tick；`GameplayJournalManager` 对缺失文件按“旧档首次挂接”创建新文档，无法知道票据签发时已存在 49 条记录。单个 current resume ticket 也不是多世界注册表。
- 修复：新签发的健康/隔离恢复票据必须先获取已落盘的 Journal checkpoint，绑定不可逆身份、tracking mode/起点/历史覆盖标志和 minimum durable sequence。prepare 和 commit 在载入前检查保护文件；世界采用后 Journal 挂接再检查一次，通过前不消费票据、不自动保存、不开放普通写入。缺失、重建、截断、断号或身份不匹配会使载入前 fail-closed；极小 TOCTOU 窗口内的变化则在采用后把当前世界降为 restricted，保留原票据以便精确修复重试。旧 version-1 票据保持兼容，下一次健康保存自动升级。
- 恢复与验证：先正常保存并关闭早期测试副本，未对其执行生产/科技/移动写入。只使用明确归属的受保护备份重签短时票据，header 复读后回到 planet `104` 且 tick 不低于 `18143540`。唯一 49-entry 备份的 `journalId`/owned hash/game version 与当前空文档全部相同；DSP 停止后保留空文档证据并恢复该备份，三个精确目标的 DACL 均复读为关闭继承、无外部 allow。Luna Max 再次 protected resume 后于 tick `18145258+` 确认 owned/saved/healthy、Journal `49/49` durable、0 pending/error、无 blocker/checkpoint。修复版 `7e44e48` 随后以旧票据兼容恢复并自动签发 checkpoint-bearing 新票据；它绑定 tracking tick `4428079` 与 minimum sequence `49`。正常保存/关闭后的新票据恢复通过；移走 Journal 和保留连续 sequence `1..48` 的两次负例均在 prepare 返回不可重试拒绝，没有 commit/action/load，token 保持不变。恢复逐字节一致的 `49/49` 文件后，同一票据在 minimum tick `18290246` 上成功复归，fresh tick `18291377` 为 owned/saved/healthy、无 blocker/checkpoint。旧 token 两处 tombstone、新票据重签及临时敏感备份删除均复验通过。
- 关联：EXP-048、EXP-069、EXP-072、EXP-146、EXP-182、`GameplayJournalContinuityPolicy`、`OwnedWorldResumeTicketStore`；状态：`fixed`。

## IFX-025 — 场地预览首稿把所有碰撞体的 ext 当作完整半尺寸

- 首见：2026-09-06，Foundry site preview 的部署前程序集审查；首稿未部署、未引发游戏施工。
- 症状：仅用 `|pos|+|ext|` 包围所有建造碰撞体，会漏掉球体半径和胶囊两端半球；旋转胶囊的 ext 还可能含负分量，不能当作非法 Box 尺寸。
- 根因：`ColliderData.InitFromCollider` 对 Box、Capsule、Sphere 使用不同编码，形状不能只由一组 ext 数值统一解释。
- 修复：以明确 shape 分派 Core helper：Box 用中心偏移加 ext 模长；Capsule 再加独立 radius；Sphere 用中心偏移加 radius。未知形状、非有限值或不合法 Box 半尺寸拒绝，合并主/附加碰撞体最大包络；仍在原生网格吸附后检查计划间净空。
- 验证：Box、Sphere、带负分量的旋转 Capsule 及非法 shape/dimension/radius 回归通过，完整 317 项测试和当前 DSP Release 构建零警告/零错误。此证明是代码/程序集级，不伪称球形或胶囊建筑已实机施工。
- 关联：EXP-188、`FoundrySitePlanner.ColliderBoundingRadius`、`FoundrySitePlannerTests`；状态：`fixed`。

## IFX-026 — 科研 buffer 缺少单位导致把研究点当成矩阵库存

- 首见：2026-09-06，原地升级候选备料/科研只读检查。
- 症状：两个研究站蓝 buffer 各36000被解释为数万个蓝糖，进而推断1202研究供料充足。
- 根因：公共 `FactoryBufferSnapshot.count` 同时承载普通件数和原生 `matrixServed`，没有单位字段；当前 DLL 每矩阵插入3600点，自动进料上限36000实际约十个矩阵等价点。
- 修复：保留原始count/inc以兼容既有读数，新增 `countUnit=research_matrix_points` / `unitsPerItem=3600`，普通 buffer为items/1。包内 playbook要求先看单位，不把部分研究点当可转移库存或完成科技预算。撤销本次错误的“研究材料已充足”结论，未更改游戏研究字段。
- 验证：当前DLL `PlanetFactory` 插入与 `LabComponent` 自动供料阈值交叉证明；契约序列化测试覆盖36000→10等价矩阵而不改变raw count。同批Plugin实机读到84/679研究buffer均points/3600，76/256成品为items/1；真实MCP playbook包含单位规则。初始366项、后续379项测试通过，不冒充科技完成。
- 关联：EXP-191、`GameStateReader.CaptureLab`、`FactoryBufferSnapshot`；状态：`fixed`。

## IFX-027 — 扩充升级白名单不能改变蓝图对象类型判定

- 首见：2026-09-06，基本分拣器升级的部署前交叉审查；含该错误的新增代码未部署。
- 症状：蓝图读取复用了 `BuildingUpgradePolicy.SupportsItem` 来判断制造设备；把2011/2012纳入升级支持后，会误把基本分拣器当作制造台，拒绝合法2–3格跨度并允许不合适的配方字段。
- 根因：可升级对象集合与蓝图二进制形状/参数集合是不同策略，不能互相当类型分类器。
- 修复：蓝图独立明确制造设备2302–2305与分拣器2011–2013；升级仅提供自己的family/pair许可。出口读回另显式比较选择时的过滤器值，不只核对item/recipe。
- 验证：新增三种基本分拣器×三种跨度的9项合法编码回归，以及错误跨度/制造配方4项拒绝回归；完整Release构建零警告错误。该修复防止部署后回退，不声称实机发生过该错误。
- 关联：EXP-189、`BoundedBlueprintReaderTests`、`GameStateReader.Blueprints`；状态：`fixed`。

## IFX-028 — 升级只验证已有边，漏检分拣器缺少必需端点

- 首见：2026-09-06，本机分拣器27的原生升级实测。
- 症状：升级返回成功且即时证明铜块1件、扣料/返还和原有连接保持，但 `verifiedConnectionCount=1`。fresh27仍缓存pick10/insert26，工厂只有27:1↔10:1；仓26没有指回27的边。不能把这一样本写成完整双端验收。
- 根因：预检与读回遍历 `Connections` 并验证每个已存在的边，却没有先要求分拣器必需的输入和输出均存在；检查空/不完整集合会真空通过。原生cached target引用、携货和factory连接池不是同一个证据源。
- 修复：Core先要求恰好slot1输入/pick、slot0输出/insert两条完整正实体连接，Plugin再逐端反向核对；该守卫复用于prepare、commit重验及同步readback。缺端点、重复/额外槽、错误方向/对象、自连接和未解析虚拟slot均拒绝。没有直接修复游戏线路，也没有撤销已证明的27升级。
- 验证：11项新回归、Debug/Release共404测试、完整Release零警告错误。修复版实际对一端23在prepare返回BUILD_CONNECTION_INVALID、无commit；两端749升级于22207214成功，filter1101/verifiedConnectionCount2/扣新1返旧1均明确，同步cargo空。保存22213308、正常退出、protected resume后两端与过滤/供电保持，最终保存22221235/J54/54/healthy。旧27的即时铜1守恒仍为独立历史样本，不伪装成最终版带货双端样本。
- 关联：EXP-028、EXP-190、EXP-193、`BuildingUpgradePolicy.HasCompleteInserterConnections`；状态：`fixed`。

## IFX-029 — 蓝图原生预检并非对覆盖对象无副作用

- 首见：2026-09-06，有限蓝图现场预览的部署前DLL研究；未在实机执行覆盖预检。
- 症状/根因：native paste `CheckBuildConditions` 会在covered belt上临时写入/清除连接；仅在返回后检查cover标志，不能证明读取工具没有改变旧对象。
- 修复：调用前独立排除所有已有entity/prebuild覆盖与内部碰撞，拒绝开放sorter/外部自动匹配；仅对无cover/no-reform新对象使用隔离native预检，显式全部Ok。原生几何与普通逐对象建设分开，不开放整图paste。额外保存/恢复native cursor与inserter提示状态，清理隔离快照/工具。
- 验证：Core图/槽位/碰撞前置与状态机测试，完整Release458项/零警告错误；新路径尚待部署和真实施工，不声称实机旧对象曾被本实现改变。
- 关联：EXP-194/195、`GameStateReader.BlueprintSite`、`NormalGameActionCoordinator.BlueprintSite`、`NativeBuildPreviewUiScope`；状态：`fixed_offline_live_pending`。

## IFX-030 — 有限蓝图预览把分拣器碰撞体过度拉长

- 首见：2026-09-06，458-test开发Plugin的五对象负例预览后复核当前DLL。
- 症状：预览报告对象2/3内部碰撞；对象2同时有独立的native `ErrorInserterData`。两种错误不能合并归因为一项，也没有进行施工。
- 根因：适配器用`span/2 + abs(ext.z) + 1`估计分拣器纵向半尺寸，额外延伸且忽略prefab collider旋转。原生采用`max(.1,span/2+ext.z-.5+.35×带端数量)`，并按两端是否为带调整中心，使用两端朝向up的平均框架与collider quaternion。
- 修复：新增纯Core几何helper并让Plugin遵循当前DLL实际公式；不放宽已存在实体/预建筑守卫，不把任何native错误强制设为Ok。795的slot/端点几何仍需独立复核。
- 验证：九项公式/非法维度回归、完整Release零警告错误、487项总测试通过。修复尚未冷部署，尚无正例模块施工；不能声称live原生几何错误已修复。
- 关联：EXP-197、`NativeInserterColliderGeometry`、`NormalGameActionCoordinator.BlueprintSite`；状态：`fixed_offline_live_pending`。

## IFX-031 — 受保护有限施工记录的空嵌套项会先在哈希处失败

- 首见：2026-09-06，冷部署前的进度存储边界审查；未发生实机记录损坏。
- 根因：校验了连接/预算列表非null，却未检查列表内的null项，计算不可变哈希会先抛NullReferenceException，绕过存储已有的明确数据错误分类。
- 修复：在hash之前检查嵌套项、连接索引/槽位/限额、依赖和参数限额；以InvalidDataException拒绝附件，不恢复权限，也不修补或跳过损坏对象。
- 验证：六项NULL对象/进度/连接/预算、无效端点及参数超限回归；完整Debug/Release510测试通过。没有把自动测试当作损坏存档live恢复。
- 关联：EXP-195、`BlueprintBuildState.Validate`；状态：`fixed_offline`。

## IFX-032 — 客户端审计汇总把逗号列表和范围表达式混用

- 首见：2026-09-06，同档第10次保存前汇总已持久的只读响应。
- 症状：PowerShell报Object[]不能转换为IConvertible，错误光标落在大对象构造的附近字段，让客户端误查entity/采样tick类型；游戏读取已完成，无accepted写丢失或重放。
- 根因：`@(1,2,3,4,5,6,7..41)`的运算符优先级把数组作为范围起点，而非“前六项加7到41”。
- 修复：此处本就是完整1–41证据序号，使用`@(1..41)`；复用已落盘响应，不重新读取整套世界，更不重放动作。涉及分段时需显式括号和数组连接。
- 验证：主会话无游戏的最小表达式原样重现同一异常；替代表达式返回41项、首1末41。客户端汇总成功和最终保存审计另待子Agent回传，不把本地显示错误算成游戏失败。
- 客户端后续：复用已落盘23页factory与其他原始响应完成摘要，随后第10项save终态23140359、J55/55/healthy并完整审计；未重放动作，正常关闭后进程/descriptor清零。
- 关联：EXP-200、十写审计客户端；状态：`fixed`。

## IFX-033 — 有限施工把皮带拾取边误当作皮带出口

- 首见：2026-09-06，首次蓝图施工前的离线原生步骤审查；未发生实机蓝图断边。
- 根因：全图包含belt→belt及belt→sorter边，原preview循环对所有皮带出边写同一个output字段；后出现的虚拟拾取边会覆盖已选下游带，依赖顺序下尚未施工sorter的entityId又为0。
- 修复：Core明确每类对象创建的端点所有权，皮带只选belt→belt出口，分拣器选输入/输出各一条，机器不投影sorter边；多个有效候选明确拒绝。Plugin使用此投影，保留原有全部双向连接和自由端读回，未加旁路字段写入。
- 验证：11项边顺序/单多sorter/自由皮带/机器/双端/歧义/无效索引回归、Debug/Release521项通过，完整Release零警告错误；安装的510-test cohort不含本修复，因此首次blueprint commit继续冻结至正常关闭后的同批更新。
- 关联：EXP-201、`BlueprintSitePolicy.CreationInput/CreationOutput`；状态：`fixed_offline_live_pending`。

## IFX-035 — 普通分拣器绕过了原生候选角度选择

- 首见：2026-09-06，554-test五对象金刚石模块导出成功，但两处site对719/720均报ErrorInserterData，没有blueprint commit。
- 根因：普通建造枚举空闲slot后直接调用CheckBuildConditions；当前原生UI在此前DeterminePreviews中先检查两端面向和相对角度、必要时给TooSkew，后续条件方法不重建这一步。工作中的历史sorter不能因此当作合法蓝图姿态。旧对象具体失败是否同时涉及Refresh位移限制仍待实读，不把推断写成确认。
- 修复：新增无DLL的保守精确直线端点策略（两设备<14度，带端<11度），进入native preview前验证当前实体池身份；拒绝未实现偏移/弯曲子集。site结果增加两端facing dot只读诊断，保留全部原生失败。无既有对象拆除/改位置/改连接，也不直接修整导出的代码。
- 验证：15个几何回归及582 Release测试通过；最终身份守卫/文档更新后的全回归与冷部署待复验。历史719/720诊断和正常合法新分拣器施工仍是独立live门。
- 关联：EXP-205、NativeInserterEndpointGeometry；状态：`fixed_offline_live_pending`。

582-test follow-up：同姿态只读预检的两个output dot为-0.174369559/-0.18947494，input为0.9832634/0.982118845，证实旧输出槽背离另一端；另有965占位。全条件保持拒绝、0蓝图commit。原生合法新sorter施工尚待，不能把诊断成功当成修复实机通过。

## IFX-034 — Governor把正常库存流动误当作产线配置改变

- 首见：2026-09-06，554-test同档恢复后、首个非零基线采样前的代码复核；没有虚构实机三窗失败。
- 根因：adapter使用包含Buffers/InserterStackCount的FactoryConfigurationStateHash绑定选区，每次正常入库或分拣器取放均可能重置基线；空仓零产负例掩盖了这个缺陷。
- 修复：提取纯Core GovernorSourceBinding，仅绑定固定身份、配方、姿态/端点、网络、静态设置。货物、增产点、加工进度和瞬时供电保持独立观测；不会因此忽略供电不足blocker。通用实体新增原生forceAccelerationMode读数，端点hash同时绑定它和sorter filter，以捕获手工配置变化而不受货物波动影响。蓝图导出另核对原生加速参数。
- 验证：13项回归包含实际增长库存的四次600tick采样建立三窗、库存delta仍保留，以及十类真实变化/重复选区/顺序/设置哈希检查；567 Release测试和完整Release零警告错误。安装的554-test不含此修复；Luna已暂停Governor基线/蓝图施工，继续只读仓储验证与必要正常备料，等待正常保存后的冷部署。尚无非零live基线或2×证明。
- 关联：EXP-198/204、`GovernorSourceBinding`、`GameStateReader.Governor`；状态：`fixed_offline_live_pending`。

582-test follow-up：三个独立600tick live窗口全为金刚石30/min，source hash始终相同、唯一生产者归因完整、baseline ready、findings0。配置绑定修复现已`fixed_local_live`，十分钟扩产仍未通过；期间模型汇报造成的真实采样gap会正确重置窗口，改为有界只读采样脚本保留节奏而非放宽gap规则。

## IFX-036 — Governor把尚未归因的全星球矿机告警当成目标链阻断

- 首见：2026-09-06，617冷部署后，金刚石三个30/min非零基线成立但validationBaselineAvailable=false，首次锁定无写入拒绝。
- 根因：observer候选和健康规则要求全局Findings.Count==0；14/263/796的矿竭告警只有各自extractor路径，没有item/选区路径证据，也被强行作为715目标线故障。
- 修复：保留全部Findings，明确TargetChainFindings与UnattributedPlanetFindingCount，只有目标诊断/选中根/选中上游路径才归因；截断仍拒绝。候选、observer与提案哈希同批更新，不靠删告警过关。
- 验证：6项新范围/哈希/健康回归，623 Release测试与完整Release零警告错误；修复仍源码态。另查出115→113真实缺边和石墨亏供，不能把范围修复当作现场修复或十分钟吞吐通过。
- 关联：EXP-207、GovernorPlanCompiler、GovernorThroughputValidation；状态：`fixed_offline_live_pending`。

## IFX-038 — 已识别故障分拣器却没有受控拆除入口

- 日期：2026-09-06；状态：`local_live_basic_sorter_repair_and_resume_passed`。
- 现场：672同档Move `74b3566b-e04c-4d78-952d-797c9291dba6` 于23637299→23638580成功到煤308附近，Walk0/约286.3MJ；115仅有108:4→115:1，113无返回输入边。prepare_dismantle明确拒绝只接受resource-miner，没有commit。
- 根因：早期拆除只为矿机换源开放；缺一端sorter也不能沿用要求完整双端的升级入口。不是延长Move超时或新档能解决的事。
- 修复：复用原生DoDismantleObject，增加普通2011/2012及可回收的stack/cargo状态。允许缺少端点，但存在边和反向引用必须配对，因为原生ClearObjectConn会无条件清对端槽。8192池条目/4邻居上限，拒绝prebuild引用/高级堆叠/活动instantDismantle；核对库存count/inc、目标消失和其他端点配置/连接，不写slot、不自动重建。
- 离线：21项Core策略回归、完整Release通过，MCP复用既有拆除工具。736同批实机拆除115和原生重建均terminal成功，2011为4→5→4、cargo/inc=0；新115的108输入及113:10输出双向完整，邻居原连接和recipe17保留。普通保存23753687及四写审计healthy/J55/55。随后719携带1件金刚石的正常回收、719/720正常重建也成功，货物返还和邻居保留有终态证据，保存23827812及五写审计通过。767同档恢复5381b28d成功并重存23992240，115/719/720实际双端与715配方、716/717仓设置保留；24112905–24113594一写完整审计通过。2012拆除、非零inc货物仍待，不外推为其他类型或连续吞吐已验。
- 修复后独立600tick三窗石墨实际产量18/12/12每分钟，后两窗113仍缺煤；连接已修正不等于产线配平，缺端也未被证明是原亏供唯一根因。

## IFX-039 — 把设备类别与未观测字段误当作消费端不存在

- 首见：2026-09-06，767恢复后的只读诊断。外部Agent看到36个assembler中recipe27为0、下游若干belt的buffers为空，错误建议新建结构矩阵消费端；未因此执行写入。
- 根因：结构矩阵在lab组件中生产，不在assembler列表；GameStateReader只调用已支持设备的CaptureAssembler/Lab/Miner/Storage/Station/Tank/Inserter，未把CargoPath货物写进belt.Buffers。局部类别、未观测字段和未追完的有向路径都不构成“不存在”证据。
- 修复：主会话阻止未经证实的新建建议，要求复读既有Lab774和真实有向下游；MCP三项观察工具描述及包内指南明确lab类别、完整分页、空belt字段的unknown含义和不清仓伪造需求。
- 验证边界：这是读取解释/操作指南修复，不修改游戏货物、拓扑或动作权限，不声称已找到所有末端或恢复持续产量。新增MCP注册/指南契约回归后Release807通过，完整Release零警告错误，源码实际MCP64tools/1resource及25250字符指南一致。767原始bundle24076211已由主会话复核，明确存在774/matrix_lab/6003上游路径，三个目标实际产消均0；128对象的下游追踪仍只是有界前缀，不能补造终点。
- 关联：EXP-214、GameStateReader.TryCaptureFactoryEntity、CaptureLab、MCP观察工具与包内playbook；状态：`guidance_corrected_revalidation_pending`。

- 2026-09-07源码补充：独立beltCargo详情观察以明确局部覆盖/observed或unavailable弥补货物盲点，不改变旧buffers含义，不把未读到当0。分段边界与零ID原生证据、严格界限和测试见EXP-215；未冷部署，不把该补充算成全线路诊断或belt升级已验。

## IFX-037 — 把Move距离终态后的Drift等待和低能量返岸当作安全交接

- 首见：2026-09-06，617-test验收移动fd22bab1已达坐标，但fresh持续Drift；等待稳定耗能，旧指南缺少明确的有界返岸分支。
- 执行偏差：后续14a9af65在fresh已经Walk、核心仅约0.185MJ且无燃料时仍提交返岸。它600tick后因原生能量全空明确失败，没有unknown或重复提交。prepare目标合法不等于足够能量；此错误不归咎为state hash缺陷。
- 修复：指南明确普通业务必须Walk/低速/能源充分；Drift不无限等候，只在能源充足时用已验证Walk锚点做有界正常Move，已自然Walk就不再返岸；只有未accepted stale可最多3次紧邻fresh/prepare，不改变Move/hash/watchdog/幂等/精确订单中止。另为燃料选择公开与既有refuel一致的原生资格，不能只搜索“燃料棒”名称。
- 验证：两次180tick位置停滞和一次600tick断能终态均如实保留。Walk静止735ticks净增980,000能量符合80kW被动补能，不冒充无线塔；正常补给/回充和新指南实机成功仍待。
- 关联：EXP-039/041/053、包内playbook、OrdinaryMechaFuelPolicy；状态：`guidance_corrected_revalidation_pending`。

## IFX-040 — 普通建造缺少初始过滤，事后配置前已发生混料

- 首见：2026-09-07，767安装态供水旁路。本局早期已有空源先过滤经验，此次将其产品化缺口单独登记。
- 症状：2280在24306579建成后默认无过滤，24307575仓2268已有水13/塑料8/油4；至24313451才独立配置filter1000，之后仓仍混有水19/塑料106/油13/氢34。没有清仓或注入，760仍未恢复。
- 根因：事发767安装态的`PrepareBuildRequest`和MCP建造签名都没有初始过滤参数；只有事后`prepare_configure_building`且须等待空载。活跃混料源在这段间隔内照常输送，过去“空源先过滤”经验没有被此次执行遵守。另建两座仓反映端到端布局预检不足，不是原生建造扣料错误。
- 已实现：新增普通2011/2012建造参数 `initialSorterFilterItemId`（默认0），仅接受有界、存在且解锁的过滤物品。复用原生 BuildPreview.filterId→PrebuildData.filterId→正常施工 NewInserterComponent 路径，保留科技/材料/几何/双端/幂等门；prepare 与 commit 的精确计划指纹绑定过滤，原生预建筑和完工 filter/sign 均复读。没有建成后改货物、暂停游戏或自动拆仓。
- 兼容防护：新 MCP 对非零过滤必须收到完全相同的 `plannedSorterFilterItemId`，旧/混装 Plugin 无回显或回显不同时返回 `BRIDGE_NOT_READY`，不暴露旧 plan/token。默认0保持旧请求/响应兼容；直接 Bridge 调用方也须执行该回显检查。包内指南同步要求先过滤、端到端布局和复用已建对象，并保留混带头部堵塞的限制。
- 离线验证：新增25 Core、4 Contracts、7 MCP回归；Debug/Release 共991项（27/920/44）通过，完整Release零警告/错误。真实源码MCP握手保持64 tools/1 resource，资源正文与指南一致，退出及stdout检查通过；不是发行ZIP或跨电脑验收。
- 当前状态：`deployed_initial_filter_build_live_pending`；1018同批冷部署及同档恢复24568281已核对，尚未取得初始过滤建造实机结果，旧混仓和供水仍未修复。原始十写已核销；后续按主会话有界方案执行，不能把安装当修复。关联EXP-065/074/217与存档日记001。

## IFX-042 — 单次腾格会被混料回占，空载过滤限制不能处理已持货的入仓分拣器

- 首见：2026-09-07，1018安装态。主会话一次20油守恒腾格只暂时让水2进入，随后761又被油/塑料占满，三只入仓sorter改持氢；不把这次失败归咎为Luna未执行方案。
- 原因：按需取料与输入容量是两个独立条件，带→仓没有消费者needs。当前源码原本仅允许空载改过滤，无法为被既有货物堵住的入仓sorter限定未来物料。公开storage buffers省略空格，也不能当成可直接编辑的格索引。
- 第一切片：按UIInserterWindow真实三字段路径开放普通2011/2012单向Inserting持货过滤，完整native struct/sign、背包及归一化端点hash立即保全。既有货物仍投原目的端，不清货、不换目标、不重置运动；后续仓格容量动作另行实现。
- 离线验证：1036项Debug/Release和完整Release零警告错误通过；没有新的游戏写入/部署/恢复成功声明。接受写计数6保持，单次修复的4终态与后续600tick零产窗口见日记/EXP-219；修改过滤本身不等于清堵。
- 第二切片：单仓storage-capacity复用原生SetBans/SetFilter/类型切换，私下绑定包含空格的完整顺序、storage组件/链与entity端点。最多100格，明确四类UI操作，既有库存不增减/搬移；超限、无效/无变化、过滤未解锁或stale均拒绝。新增25 Core/2 Contracts/3 MCP后1066项Debug/Release与完整Release通过，仍待冷部署及真正稳定供水，不能忽略2218持续带入氢的上游问题。
- 首次本机正例：同批1066安装及同档恢复后，761在24849780完成bans0→30并保持全部库存/5端点，2218在24855565换成油过滤且保留既有氢1/原目的端和玩家库存。原始terminal及全23页2280built/独立0prebuild/健康状态已主会话核对并十写落盘；实际预约、残货处理、保存保留和持续供水仍未完成。
- 后续预约实测：24936046锁已有格、24939851预约唯一空水格均terminal成功；既有29条非空buffer/5端点/玩家/bans30保持，水count0。五个准入过滤和三次油/氢守恒转移也逐原始核销，整批十写在24946625+完成全厂/健康审计。尾带仍塑料/油/氢混杂，不能用末端单一氢过滤或再增一个仓库宣称永久修复；配置保存恢复、残货去向和真实持续供水仍待验。
- 状态：`held_filter_and_storage_bans_locally_verified_recovery_pending`。关联EXP-219/220/221。

## IFX-041 — 多次接线拒绝时缺少端口几何，并把共享带误称为纯水带

- 首见：2026-09-07，767安装态供水修复。四个source→760候选均在当前保守直槽适配器的facing检查返回TooSkew，没有commit；不能据此声称完整原生UI的所有偏移/弯折路径都不合法。
- 根因：Agent根据中心距离选仓/带，而详情缺少空闲槽位的世界位置与朝向；另只追到水仓753就把2219→2243称作纯水，遗漏2219还有2220上游，及经2215进入的油/氢/塑料共享输送。24517132/139/146分别复读2247/2251/2252持塑料/精炼油/氢，且均来自2243，直接否定纯水标签。
- 修复：用户授权连续失败后由主会话接手规划、Luna负责有界执行，保持同一accepted计数与fresh prepare。新增只读detail `sorterEndpoints`，最多16原生槽位或4传送带虚拟姿态；世界坐标/朝向/占用深复制，未知不猜空闲，固定16连接池步长先验界，不改写/扩大建造规则。指南同时纠正共享带归因和直接Bridge传 `itemId=0` 导致空页的调用方错误。
- 验证：25 Core、1 Contracts、1 MCP新增回归，Debug/Release1018项通过、完整Release零警告错误；源码MCP64工具/1资源、正文28415字符一致、stdout/退出检查通过。正常保存24473406与全厂审计另记入日记，仍未接通760。
- 状态：`endpoint_observation_locally_verified_repair_pending`；1018同批冷部署/同档恢复后22对象原始详情已核对，支持对象的槽位/带姿态和不支持对象的unavailable均实测返回。主会话几何计算排除已选现成直连候选，转而按EXP-219核对复用既有输入sorter的按需取货与一格守恒腾位方案。供水仍未修复；不新增第三仓、不清除物品制造产量，不把观察或预检当施工成功。

## IFX-043 — 详情读取字段误名被报告成建筑消失，导致无效诊断扩散

- 首见：2026-09-07，1066安装态有界预约批次第8项之前，Luna把`inspect_factory_entity`的payload写为`entityId=761`，实际契约是`objectId`。未知字段被忽略后ObjectId默认为0，旧读取器统一报`INVALID_ENTITY`/no longer exists；又只查首100实体且尚有nextCursor，用has761=false继续寻找“消失”的原因。
- 主会话处置：从实际请求和当前契约定位字段错误，通知Luna只修正读取字段、沿原accepted计数继续原有计划，不重新执行已accepted的氢转移、不换档、不增建。configure/transfer的entityId属于其它方法，不机械改名。后续第8项原始核销由Luna继续，整批终态/仓格审计另记日记。
- 产品修复：保留owned访问门在前，对读取的0/缺失和Int32.MinValue返回带正确字段/恢复建议的`INVALID_REQUEST`；合法正负ID继续原来的native存在性检查。MCP参数说明和内嵌playbook明确逐方法schema、第一页不足以证明不存在、多次本地字段错误应交主会话处理。
- 验证：9 Core/1 MCP新增回归；Debug/Release1115项（30 Contracts/1035 Core/50 MCP）、完整Release零警告错误通过。真实源码MCP64 tools/1 resource、31150字符指南一致、退出0/stdout纯净。当前游戏仍1066；新错误码分支尚未冷部署/live，不冒充存档或游戏数据修复。
- 状态：`request_validation_offline_verified_caller_corrected`；关联EXP-223。

## IFX-044 — 成功解禁后正常投递，被跨tick库存相等断言误判为失败

- 首见：2026-09-07，1066安装态761的set-bans0在25011091成功，稍后库存氢1→4、原三只held氢正常入仓，Luna的外部断言却要求库存不变。
- 根因：Plugin已在原生配置调用内证明逐格保全，但公开terminal没有固定该瞬间的结构化配置/库存；调用者把后续fresh inspect当成同一时刻。已接受动作本身没有失败。
- 处置：主会话逐raw核对terminal、设置/连接/玩家和三只原持货去向后交回Luna继续，不重放、不把本地异常变成隔离或换档。
- 产品修复：可选storageConfigurationReadback记录即时tick、操作、前后设置和独立非空buffer；只在现有native不变量全部通过后生成，Core再次验证实际格投影。不新增写入口，不改state hash/幂等/unknown；指南和MCP描述明确跨tick差异须另核物流。
- 验证：1132项Debug/Release（31 Contracts/1050 Core/51 MCP）、完整Release零警告错误、源码MCP64 tools/1 resource/32057字符指南一致/stdout纯净；新增字段未冷部署/live。旧set-bans动作的真实投递证据不冒充新字段实机通过。
- 后续实机：1144同批冷部署后，775 set-bans30动作b017ef0f在25142861成功，新DTO的同步前后设置、各30条金刚石100/inc0和3条连接均由主会话核销，玩家/外部连接保持。这是新字段的set-bans本机正例，不补造旧unban字段或外推其他操作。
- 状态：`instant_readback_offline_and_local_set_bans_verified`；关联EXP-224。

## IFX-045 — 错误哈希域被反复解释成现场陈旧

- 首见：2026-09-07，1066安装态2280的两次sorter-filter prepare均使用before.stateHash，被旧通用检查报STALE_STATE；没有commit，不能计为写入失败或消耗accepted额度。
- 根因：同一expectedFactoryStateHash参数在不同配置模式使用不同域，外部调用者机械沿用了普通仓配置的完整hash。现有工具长描述虽说明配置hash，参数位置未直说区别，错误也没有可操作的定位。
- 处置：主会话核对真实请求代码，纠正为根configurationStateHash，再交回Luna继续同一有界计划；后续动作在25042120正常完成，未扩散候选、重做已接受动作或换档。
- 产品修复：在原sorter配置检查前区分可确证的当前full-hash错误域（非重试INVALID_REQUEST）与无法确证来源的不匹配（保留STALE_STATE/fresh read）。缺失/空配置证据不通过；ordinal比较不折叠大小写。MCP参数和包内playbook明确sorter-filter/configurationStateHash与storage-capacity/stateHash，不修改原哈希及commit语义。
- 验证：11 Core/1 MCP新回归，1144项Debug/Release（31 Contracts/1061 Core/52 MCP）、完整Release零警告错误；源码MCP64 tools/1 resource、32678字符指南一致、exit0且无额外stdout。当前游戏仍1066，新诊断分支尚未live验证。
- 后续实机：1144冷部署后，对2280的一次fresh完整stateHash负向prepare实际返回INVALID_REQUEST/retryable=false及正确域恢复提示；没有commit/action，不计accepted写入。
- 状态：`hash_domain_diagnostic_offline_and_local_rejection_verified`；关联EXP-225。

## IFX-046 — 只枚举传送带中心，无法使用原生带段接点微调

- 首见：2026-09-07，主会话接手Luna重复拒绝的供水方案。753/761附近现有原生槽角度不满足旧直线子集；移近后外侧20段带可放，但这不构成两端连接批准。
- 根因：旧普通分拣器prepare没有复用DeterminePreviews的单belt插值分支，只有实体中心四向姿态；不能以持续修改业务路线掩盖这一适配限制。另发现private build fingerprint遗漏了InputOffset/OutputOffset，当前旧子集都为0，但开放偏移前必须补齐。
- 修复：在原执行器内增加一个明确belt与2101仓/assembler/lab的有界几何候选，最多64区间；不调用含UI/输入副作用的完整方法，不扫描邻带。保留11/24.1度和原生全预检，静态几何及offset参与绑定，提交前重查，原生预建筑/实体双位姿、offset、身份、过滤和双边连接核验。
- 验证：1201项Debug/Release、完整Release零警告错误、源码MCP64 tools/1 resource/33408字符指南一致通过；55项Core与Contracts/MCP各1新增回归。尚未冷部署或取得新适配实机正例。
- 安装态复验：0c59dab的1201同批DLL已冷部署，protected resume及J55/55连续性通过；753→2219、2246→761、2277→761、753→2232仍被原生接点准备拒绝，没有commit。不能把安装成功或这些负例当作接线修复正例；尚须更明确的失败阶段证据来指导有界选址。
- 状态：`bounded_native_attachment_offline_verified_live_positive_pending`；关联EXP-226。持续供水、模块复制、Governor和最终包门未减少。

## IFX-047 — 接线失败未区分几何捕获、候选搜索与原生检查阶段

- 首见：2026-09-07，1201冷部署后主会话四个有界接线预检仍拒绝，没有commit。
- 根因：普通prepare把最后一个精确槽的TooSkew和通用fallback失败串联输出，未报告几何是否取得、是否存在有效投影、最接近角度或候选校验是否执行；恢复说明一律建议移近或换位置，易诱导无效重试。
- 修复：复用原候选和纯角度公式，补充固定捕获reason、实际阶段计数、最小有效偏差及明确unknown。最后精确槽结果单独标注；candidateChecks不声称原生放置检查次数。只对明确OutOfReach建议移近，暂时buffer busy只允许等待后一次fresh prepare；不更改原生阈值、计划/状态hash、材料/施工或commit语义。
- 验证：1217项Debug/Release（32/1131/54）、完整Release零警告错误，源码MCP64 tools/1 resource及同批指南/stdout通过；当前1201安装态不热替换，新阶段live尚待。单元测试不作为供水恢复或偏移施工成功证明。
- 本机后续：1217同批冷部署/同档恢复及33详情保持通过；四个明确接点分别返回两次无有效投影和59.768°/19.745°两个超界角度，geometry捕获均成功、候选校验和commit均0。新诊断没有放宽原生拒绝，也没有把未测角度填0。主会话停止重复候选，转向完整供料方案。
- 状态：`rejection_stage_diagnostics_offline_and_local_verified`；关联EXP-227。新接线施工与持续供给未通过。
