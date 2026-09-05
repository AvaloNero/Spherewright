# AGENTS.md — Spherewright 开发规范

## 当前目标

Spherewright 是面向外部 Agent 的《戴森球计划》结构化控制层。Plugin 负责当前 DSP 版本的薄适配，Bridge 负责本机安全协议，MCP Server 暴露观察与受控动作；项目不内置 LLM、自主 Goal Planner 或长期自主循环。

当前版本是 **v0.4.0 — Overseer & Foundry**。`v0.3.0 — Logistics Towers` 已由 commit `a52ff44` 发布为 annotated tag 和 GitHub Release，只是历史能力锚点；M0 首颗红色矩阵同样不再构成当前阶段门禁。当前版本范围、后续版本边界和发行门以 [ROADMAP.md](./ROADMAP.md) 为准。

本轮新增允许：以只读方式采样并汇总多星球生产、供电、物流与科研状态，计算有明确定义窗口的实际/理论速率和利用率，分类缺料、输出堵塞、供电不足、物流阻塞与矿脉耗尽，追踪上游根因，并生成继承既有脱敏边界的诊断包。另允许实现一次明确的“用户授权导入/认领当前存档”入口：玩家必须已经在 DSP 内手工载入目标世界；Agent 先为受限 session 做无游戏副作用的导入预检，向用户说明原档不变、新建 owned 副本和 Journal 从导入点开始，再在对话中单独询问。只有用户后续明确确认，Agent 才可提交绑定当前进程、session、revision 与精确 `GameData` 的短时单次计划。Spherewright 随后只可用 DSP 正常保存 API 创建服务端命名的新 owned 副本，绝不覆盖、改名、枚举或主动载入原档。既有普通生产、科研、供电、地表移动、同星系原生飞行、受保护恢复和物流塔动作只可继续通过已经证明的原语执行，并可用于受控制造和修复 v0.4 验收故障；本轮不因诊断目标新增批量或旁路写入能力。

2026-09-05 项目所有者明确把原 0.5.0 Foundry 合并到 0.4.0 一起实现，本版本恢复为开发中。新增允许：在无游戏 DLL 的 Core 中复用运行时配方/依赖图，生成含目标速率、设备、材料、供电、物流、候选位置与有界动作图的确定性空地建厂计划；Plugin 只提供现场证据、正常逐动作适配和按 owned identity 保护的计划进度。外部 Agent 每次只选择、prepare、commit、轮询并复读一个步骤；跨重启重新验证实体与现场，旧 token 不恢复，结果不确定时不得重做。当前普通 belt/sorter 原语优先完成三级生产链；配送型计划的专用 add-on/配置/fleet 原语仅在当前程序集证据、测试和逐动作读回齐备后开放。游戏实操继续交给 Luna Max 子 Agent；主会话负责接口和代码。

本轮禁止：

- 内置 Agent/LLM、自主规划循环和一次调用批量盖章的蓝图施工；
- 黑雾、战斗、多人、Nebula 和跨恒星曲速自动化；
- Computer Use、截图识别、键鼠宏、外部内存扫描或游戏程序集修改；
- 调用或依赖沙盒工具、物品注入、直接填写设备/物流塔缓冲、直接解锁科技、瞬建、瞬移、游戏加速或存档编辑；存档处于沙盒模式本身不再构成拒绝理由；
- 枚举、主动读取或载入任何非 Spherewright 自建并登记的存档；唯一例外是玩家已在 DSP 中手工载入、完成预检并在对话中随后明确确认的当前内存世界，可按本文件第 5 节另存为新 owned 副本。

## 1. 权威文档

- [AGENTS.md](./AGENTS.md)：当前开发、安全和发行约束。
- [ROADMAP.md](./ROADMAP.md)：`0.3–0.9` 能力范围与版本验收门。
- [docs/protocol.md](./docs/protocol.md)：协议、动作和工具语义。
- [docs/safety-model.md](./docs/safety-model.md)：安全边界与写入隔离。
- [docs/architecture.md](./docs/architecture.md)：组件和进程边界。
- [docs/research/environment.md](./docs/research/environment.md)：本机版本、程序集与部署证据。
- [docs/research/game-api-m0.md](./docs/research/game-api-m0.md)：从早期基线延续至当前版本的 DSP API 调用证据；文件名是历史来源，不是当前阶段门禁。
- [docs/research/game-api-overseer.md](./docs/research/game-api-overseer.md)：v0.4 多行星统计、窗口、理论速率和诊断数据源证据。
- [docs/research/game-api-foundry.md](./docs/research/game-api-foundry.md)：v0.4 建厂物料、设备规模计算与现场适配证据。
- [docs/save-diaries/README.md](./docs/save-diaries/README.md)：逐存档日记索引、命名和证据边界。
- [docs/gameplay-timeline.md](./docs/gameplay-timeline.md)：`owned-world-001` 的存档日记。
- [docs/incident-fix-log.md](./docs/incident-fix-log.md)：首次问题、根因、代码/协议修复和验证记录。
- [docs/experience-ledger.md](./docs/experience-ledger.md)：实现与实机经验的权威账本。
- [docs/release-installation.md](./docs/release-installation.md)：最终用户安装、升级和卸载说明。
- [CONTRIBUTING.md](./CONTRIBUTING.md)：1.0 前的问题反馈、PR 和隐私政策。

状态不得散落在临时接手文档中。当前游戏进度写入该档日记，首次工程事故写入问题与修复记录，复用结论写入经验账本，版本完成条件写入 Roadmap；不要再创建阶段 Gate 状态页、手工测试状态页或跨电脑 handoff 状态页。

## 2. 架构与线程边界

目标链路：

```text
External Agent / MCP Host
        │ stdio MCP
        ▼
Spherewright.Mcp                 net8.0
        │ authenticated local Named Pipe
        ▼
Spherewright.Plugin              net472 / BepInEx 5
        │ bounded Unity-main-thread work
        ▼
DSP native gameplay systems
```

所有 `GameMain`、`GameData`、`PlanetFactory`、`FactorySystem`、`CargoTraffic`、`PlanetTransport`、玩家背包、科技状态和 Unity 对象的访问都必须在 Unity 主线程执行。后台线程只处理协议、排队和已经深复制的 DTO；不得把 DSP 数组、池、组件引用或 Unity 对象带出主线程。

无需游戏 DLL 的逻辑放在 `Spherewright.Contracts` 或 `Spherewright.Bridge.Core`，包括帧协议、规范哈希、计划存储、幂等、安全策略、几何和纯计算 helper。`Spherewright.Plugin` 只保留当前版本的游戏读取、业务调用、主线程调度与 BepInEx 生命周期适配。`Spherewright.Mcp` 不引用游戏程序集，也不复制游戏规则。

## 3. DSP API 证据

不得凭印象猜测字段、方法、枚举值或 UI 语义。每次新增或改变 DSP 调用路径时：

1. 以本机当前 `Assembly-CSharp.dll` 为准，记录版本和 SHA-256。
2. 查明精确类型、签名、字段语义、调用方和前后置条件。
3. 优先复用 UI 或游戏业务层真实路径；若只能写字段，必须证明官方 UI 对同一字段的精确写法，并把 callable subset 缩到可验证范围。
4. 把证据、采用理由和拒绝的替代方案写入 `docs/research/`。
5. 增加无游戏 DLL 的策略/哈希测试，并在安全重启后做当前版本实机复读。

游戏程序集、反编译源码、存档、运行时描述文件、认证 token、plan token 和私密绝对路径不得提交。

## 4. Bridge 与 MCP 安全

- Named Pipe 只允许当前 Windows 用户，使用随机 pipe 名和每进程轮换的高熵认证 token。
- 描述文件必须使用当前用户保护的目录和文件 ACL；不得把凭据写入日志、stdout、文档或工具结果。
- 帧长度、连接数、队列、主线程每帧工作量、分页快照和计划存储都必须有界。
- MCP 的 stdout 只允许协议帧；日志走 stderr 或受保护日志文件。
- MCP 不暴露任意方法调用、任意字段写入、任意存档名、任意物品注入或“完成某目标”的复合作弊工具。
- 公共工具面以运行时 `tools/list`、契约和 [docs/protocol.md](./docs/protocol.md) 为准，不在本文件维护容易过期的静态工具清单或数量。

## 5. Owned world 与恢复

- Spherewright 新建世界的基准默认仍是单人、和平、1× 资源、关闭沙盒。已存在或已导入的 owned world 必须能证明和平模式；沙盒状态和资源倍率只是运行证据，不是写入、导入或恢复门禁。
- 保存名由服务端生成并内部保留，客户端不能指定、枚举或选择存档。
- 当前进程以精确 `GameData` 实例和受保护登记证明所有权；进入其他世界时只能返回受限状态，不读取其内容。
- 导入只能从玩家已经手工载入的当前世界发起。Agent 必须先调用 prepare 做无游戏/存档副作用的预检并展示返回的确认语义，然后在对话里单独询问；先前“继续”“接手”等请求不能替代这次预检后的确认。
- 只有用户在预检之后的消息中明确同意，Agent 才能在 commit 中声明 `userConfirmedInConversation=true` 并同时确认“原档不变”和“Journal 从导入点开始”。Plugin 不声称能读取聊天记录；该字段是 MCP 调用方对当前对话证据的声明，工具规范禁止推断或预先填写。
- 导入计划必须短时、单次、只绑定当前进程、session、revision 和精确 `GameData`；切换世界、revision 变化、过期或开始保存尝试后不能复用。commit 还要复核和平模式、实际沙盒/倍率证据、本地工厂 ready、写开关和同一对象身份；沙盒/倍率值不用于拒绝。
- 导入只调用 DSP 正常保存 API，把当前内存世界另存为服务端生成的高熵 owned 名称；原始保存名和路径不得进入公共 DTO、日志或文档，原存档不得覆盖、改名、删除或成为恢复目标。保存及 header 复读证明成功前不得取得 ownership。
- 导入是显式时间边界：逐档 Journal 从导入时开始，标记历史覆盖不完整；不得根据导入时已有的物品、科技、升级或设备补造此前的“首次”事件。
- 正常保存只允许当前 owned identity，并调用 DSP 正常保存 API。
- 健康的计划重启只载入 ticket-bound exact primary；只有隔离恢复可以采用已经在读取 header 时满足最低 tick 的受限 LastExit 路径。
- 恢复票据必须一次性、可过期，并有 durable consumed tombstone；恢复后重新生成 session，旧 cursor、plan 和 capability 全失效。
- 新签发的恢复票据还必须绑定该 owned save 当前已落盘 Journal 的身份、跟踪边界和最小 durable sequence。prepare/commit 在载入前检查，世界采用后再检查一次；Journal 缺失、被重建或序列倒退时不消费票据、不自动保存且不开放游戏写入。旧票据仅为兼容可无此水位，下一次健康保存必须升级为新语义。
- 星际飞行前单独保存绑定该次飞行的 checkpoint。失败时可反复读取同一 checkpoint；飞行成功后立即撤销 reload capability，并在覆盖主档保存成功后 retire checkpoint，绝不能让旧 checkpoint 回滚后续进度。
- 写入隔离时停止新的 commit，先复读精确 action；不能证明结果时正常关闭并通过受保护的同档恢复路径重启。不得因隔离开新档或加载其他档。

Steam 和 Windows 账号不是存档所有权证据。更换账号后仍只依赖上述 owned identity 和恢复票据。

## 6. 观察与写入协议

所有写入使用：

```text
inspect → prepare_* → commit_* → get_action_result / fresh inspect
```

prepare 必须无游戏副作用，返回短期计划、真实资源预算、完成条件和 blockers。commit 必须绑定 `sessionId`、`planetId`、计划 token、UUID 幂等键、状态哈希版本和精确目标身份，并在 Plugin 级 single-flight 内重新验证。

accepted 之后不得因为本地超时、输出格式错误或响应丢失而换键重做。先用同一 action ID/幂等键或 fresh 双边状态核销；只有确定未接受的 prepare/commit 拒绝才允许重新 inspect 和新建计划。

合法动作可以跨多个游戏 tick 处于 queued、executing 或 waiting 状态。调用方取消等待不能暗中撤销已经开始的普通游戏过程。无法证明 expected after 时返回 outcome unknown 并冻结后续写入；不得猜测回滚玩家、库存、科技或工厂字段。

state hash 使用版本化、无歧义规范编码；列表 cursor 绑定 session、planet、过滤器、快照身份、offset 和 expiry。详情读取与 prepare 不能依赖分页快照继续代表实时状态。

## 7. 普通玩法约束

- 移动只下达原生玩家移动/飞行订单，不写位置。持续检测位移、目标进展、能量饥饿和移动状态；断能恢复后重置 watchdog 窗口。卡住时结构化终止当前订单，再重新规划安全路径。
- 玩家低能量时优先走到已验证无线输电覆盖内自动充电；不能写机甲能量。燃料补充只走原生 transfer/refuel 并证明数量守恒。
- 手采绑定具体资源节点、距离、剩余量和玩家背包；手搓进入正常 replicator 队列，原料、产物和耗时来自运行时配方。
- 建筑必须来自玩家库存，通过 DSP 原生 build condition 创建预建筑，再由施工无人机和游戏 tick 完成；禁止直接 `BuildFinally` 或直接创建实体。
- 传送带的完整 planned path 必须与当前实体占位交叉检查；分拣器需双向端点、设备槽位和完工反查。施工成功不等于可运行，每个消费者还要复读 `powerNetworkId`、供电比和真实物流。
- 配方和科技必须存在、已解锁且适用于精确设备。科研动作只选择/排队，不写进度或完成标志。
- 物流塔配置只走已证明的正常 UI/业务路径；库存、无人机和运输船只允许经正常 transfer、belt/sorter 或原生调度变化，禁止直接写站内缓冲。
- 电力不足、原料不足、输出堵塞、在途运输和配方锁定都是正常状态，必须诊断和修复，不能用注入掩盖。

## 8. 当前 v0.4 验收

完整版本门以 Roadmap 为准。发布 `v0.4.0` 前至少同时满足：

- 多星球生产、供电、物流和科研摘要在同一受保护存档上稳定、分页有界且身份明确。
- 每个速率字段都声明采样周期和窗口，区分实际值、理论值与利用率，不把暂停、离线或采样缺口计为产量。
- 诊断能区分缺料、输出堵塞、供电不足、物流阻塞和矿脉耗尽，并从目标物品追到有证据的上游设备、物流边与根因。
- 诊断包不包含 auth token、plan token、绝对路径或原始存档身份；时间窗口复用 per-save 受保护持久化。
- 在受控条件下分别制造缺料、断电和物流阻塞，Overseer 稳定区分三者；外部 Agent 依据诊断修复至少一个场景，随后清除全部故障。
- 相关产线恢复到稳定非零实际产量，并以 healthy writes 普通保存；正常保存/恢复后窗口语义保持一致。
- Core/Contracts/MCP 自动测试、完整 solution 构建、当前 DSP 实机回归和经验账本审计通过。
- 用户授权导入满足“预检后单独询问、用户后续明确确认”、计划单次/过期/session/revision/对象绑定、原档不变、正常另存、header 复读、失败不认领和导入前 Journal 历史未知等安全门；对应 `v0.3.1` 回移候选不得削弱这些不变量。
- Foundry 为至少一条三级生产链生成含设备、供电、物流和成本的完整不可变计划，并通过逐实体正常库存/施工无人机动作完成。
- 计划施工中途普通保存、正常关闭和同档恢复后继续，已完成实体必须复读去重，无重复建造、重复扣料或不明拓扑差异；现场/资源变化和旧 token 均不能绕过 fresh prepare。
- `v0.4.0` 合并版自包含 Windows 包在干净受支持环境启动并完成 MCP 握手；旧 Overseer-only 候选仅保留为阶段证据。

完成当前版本后，先用独立提交把本文件“当前目标”切换到下一版本，再开始新增动作域。

## 9. 测试与发行

常规无游戏 DLL 回归：

```powershell
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
```

当前 DSP 完整构建：

```powershell
./scripts/sync-game-refs.ps1
dotnet build Spherewright.sln --no-restore
```

发行包：

```powershell
./scripts/package-release.ps1 -Version 0.4.0
./scripts/test-release-package.ps1 -PackagePath ./artifacts/Spherewright-0.4.0-win-x64.zip
./scripts/test-thunderstore-package.ps1 -PackagePath ./artifacts/Spherewright-0.4.0-thunderstore.zip -ExpectedVersion 0.4.0
```

`package-release.ps1` 必须同批产生手动安装包和 `Arcueid_77-Spherewright` Thunderstore 包；两者使用同一版本和 source commit。Thunderstore 包只保留四个 Plugin 侧 DLL 与单文件自包含 MCP，禁止捆绑游戏或 BepInEx 程序集。静态包体校验不等价于 Mod Manager/异机实机验收，验证状态必须分开陈述。

测试至少覆盖 DTO/错误码兼容、规范哈希、计划过期、幂等、single-flight、cursor 绑定、和平模式 fail-closed、沙盒/倍率非门禁、资源预算、动作 outcome、恢复票据/checkpoint 生命周期、MCP 注册与 stdout 纯净。Windows 集成测试覆盖当前用户 ACL、错误 token、畸形/超大帧、队列满、描述文件权限与退出清理。

实机验收必须使用当前支持的 DSP/BepInEx，记录脱敏 before/after、动作终态、供电/物流/产量、保存与恢复。截图只能补充展示，不能代替结构化证据。

## 10. Journal、存档日记、问题记录与游戏写审计

运行时 Journal 是逐存档的机器可读原始证据。每个 owned save 使用独立的首次手搓计数、首次流水线产出寄存器、科技/升级首次选择记录和持久化文件；必须公开 `durableThroughSequence`、`persistencePending` 与 `persistenceError`。不得跨档继承首次状态，也不得在旧档迁移时补造未知历史。用户授权导入的副本从导入点建立新 Journal，必须持久化 `attached_existing_save`/历史覆盖不完整语义，并且只把导入后真实观察到的首次事件记为该 Journal 的首次。

仓库内还必须为每个 owned save 建立一份人类可读存档日记，并在 `docs/save-diaries/README.md` 登记公开别名。日记整理 Journal、运行态、关键决策、事故和保存/Git 里程碑，但不得记录真实存档名、恢复凭据或私密路径。第一次遇到的问题可以保留在当档日记；其根因、代码/协议修复和验证还要同批抽取到 `docs/incident-fix-log.md`。

实现或实机过程中产生的所有可复用经验都在同一工作批次写入 `docs/experience-ledger.md`。每条包含稳定 ID、日期、`observed | validated | superseded | invalidated`、适用范围、结论、证据、限制、复验触发、关联项和最近复验。

新证据冲突时先降级或替代旧结论，保留修订记录，不让矛盾的现行经验并存。以下事件必须复核受影响经验：实现批次结束、累计 10 个 accepted 游戏写动作、Plugin 部署/重启、DSP 或程序集变化、隔离/恢复、版本门变化和最终发布。

累计第 10 个 accepted 游戏写动作后，先冻结下一次游戏 commit，完成严格审计并更新账本。审计至少包括：

- 10 个动作的终态或唯一 fresh 状态核销；
- 和平、实际沙盒状态、实际资源倍率、owned、write health、blockers 和 checkpoint；
- 玩家移动/能量/手搓/施工状态；
- journal durable-through、pending 和 persistence error；
- 工厂 built/prebuild、相关有向拓扑、设备供电和关键库存；
- 未重放 outcome unknown、未解释正增量、串料或时间线分叉。

无异常时持续使用同一存档。每完成一种产物或持续供应流水线，先证明实际物流/生产，正常保存，再把实现与经验作为单一 Git 里程碑提交并推送。

## 11. Git 与文件安全

- 用户已授权直接在 `main` 开发、提交和推送；提交保持单一目的、可审阅，不夹带无关改动。
- 不使用 `git reset --hard`、`git clean -fd`、破坏性 checkout、force push 或覆盖用户改动。
- 不提交游戏 DLL、存档、BepInEx 日志、runtime descriptor、token、个人路径、构建缓存或可重建 artifact。
- 发行使用标准 MIT License；README、包元数据和 `LICENSE` 保持一致。
- 每个 `0.x.0` 全部验收通过、工作区干净且最终提交已推送 `main` 后，先向用户提交候选 commit、测试/实机证据、工件哈希和 Release notes 审核；只有用户明确通过后才创建 annotated tag `v0.x.0`，再从该 tag 创建同版本 GitHub Release。
- Release 至少包含 commit、支持版本、工具/协议变化、安装/升级、已知限制、测试结果、脱敏实机证据、手动安装 zip、Thunderstore zip、manifest 和各自 SHA-256。
- 不提前打 tag，不把先前版本的发布授权外推到下一版本；Thunderstore 或其他注册表发布需要用户另行明确授权。

## 12. 最终汇报

汇报必须明确：

- 当前版本和最早未完成验收项；
- 本次完成的代码、游戏里程碑和关键决策；
- 修改文件与提交/推送/tag/release；
- 自动测试、完整构建、发行包和实机结果；
- blocked 项的具体原因、已完成替代工作和下一步；
- 新增或修订的经验及仍需复验的限制。

不得只说“完成”，也不得把历史 M0/v0.3 产线证据、一次采样、一次故障标签或单点瞬时速率冒充当前版本完成。
