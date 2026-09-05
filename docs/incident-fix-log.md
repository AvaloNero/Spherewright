# Spherewright 首次问题与代码修复记录

更新时间：2026-09-05（Asia/Singapore）

本文件专门记录项目第一次遇到的可复用工程问题：现场症状、根因、代码或协议
修复、验证证据和仍有限制。它不是逐局流水账，也不是当前规则的唯一来源。
事故在发生存档的日记中仍可保留；修复形成的现行规则以
[experience-ledger.md](./experience-ledger.md) 为准，DSP API 事实以
[`docs/research/`](./research/) 为准。

状态取 `fixed | mitigated | open`。`fixed` 只表示写明范围内已有代码和验证证据，
不代表跨 DSP 版本永久成立。

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
