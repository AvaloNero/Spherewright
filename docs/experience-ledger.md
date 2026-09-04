# Spherewright experience ledger

更新时间：2026-09-04（Asia/Singapore）

本文件是 Spherewright 实现、DSP 实机控制、运行环境与安全处置经验的权威账本。它记录“目前为什么这样做”以及“什么情况下必须重新检查”，不是成功日志，也不替代 `docs/research/` 的 API 证据、逐档日记、`docs/incident-fix-log.md` 的首次问题/修复记录或 `ROADMAP.md` 的版本验收门。

## 维护协议

- 状态只取 `observed | validated | superseded | invalidated`。
- `observed` 表示证据真实但适用范围尚窄；不得自行外推为稳定 API 或通用阈值。
- `validated` 表示当前写明的适用范围内已有独立复读、自动化测试或当前版本实机证据。
- `superseded` 必须指向替代条目；`invalidated` 必须说明反证。历史不删除。
- 每个实现批次结束、每累计 10 个成功游戏写动作、Plugin 部署或重启、DSP/程序集版本变化、写入隔离或恢复、版本验收状态变化以及最终发布前，复核新增条目和所有受影响条目，并更新“最近复验”。
- 安全相关新经验和会影响下一动作前提的新经验，必须先写入本文件，再执行下一次游戏写入。
- 证据只记录可复核的脱敏摘要、动作/实体 ID 或代码测试位置；不记录 token、存档内容或 runtime descriptor。

## 当前经验

### EXP-001 — 部署前显式构建完整解决方案

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 `Spherewright.sln` 与 Plugin 开发部署流程。
- 当前结论：`dotnet test` 可能不重建未被测试项目引用的 Plugin；部署前必须显式执行完整 `dotnet build Spherewright.sln --no-restore --nologo`，再以构建产物安装。
- 直接证据：一次仅运行测试后 Plugin DLL 未包含最新改动；显式完整构建后部署哈希与输出一致，构建为 0 warning / 0 error。
- 限制或反例：若未来测试项目显式引用 Plugin，该现象可能改变，但完整构建仍是部署前的明确证据。
- 复验触发：解决方案/测试引用图、构建脚本或 Plugin 输出路径变化。
- 关联：`scripts/install-dev-plugin.ps1`、`src/Spherewright.Plugin/Spherewright.Plugin.csproj`。
- 最近复验：2026-09-03（物流时间窗最终批次先完整构建，再离线部署同批零差异程序集；0 warning / 0 error）。

### EXP-002 — DSP 应通过 Steam 启动链启动

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 Windows Steam 安装的 DSP `0.10.34.28529`。
- 当前结论：直接启动 `DSPGAME.exe` 会很快退出；本机可靠启动路径是 Steam `-applaunch 1366540`，随后再发现实际游戏进程和 Bridge descriptor。
- 直接证据：直接启动进程退出；Steam 启动后 DSP、BepInEx 和 Spherewright Plugin 正常加载。2026-09-03 的 v0.4 第四批部署再次复现：直接 `DSPGAME.exe` 报 `SteamAPI_Init()` 失败并退出，尚未消费的受保护恢复票据保持有效；改由正在运行的 Steam 客户端 `-applaunch 1366540` 后，Plugin 启动并只恢复同一 exact primary。第八批监控修复部署又证明，即使直接 EXE 已短暂完成一次 exact-primary 恢复和自动重存，它仍会随短命启动包装退出；新一代票据保持可恢复，改用 Steam `rungameid/1366540` 后进程持续运行并再次只恢复该精确主档。
- 限制或反例：非 Steam 发行版或未来启动器未验证。
- 复验触发：游戏安装来源、Steam app ID、启动脚本或游戏版本变化。
- 关联：`scripts/locate-dsp.ps1`、`docs/research/environment.md`。
- 最近复验：2026-09-03（直接 EXE/短命包装退出与 Steam `rungameid` 持续运行形成第三组正反样本；两次均未枚举其他存档）。

### EXP-003 — 运行时描述文件使用 `bridge-*.json`

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 RuntimeDescriptorPublisher 与本机 MCP 发现流程。
- 当前结论：descriptor 的实际文件模式是 `bridge-*.json`；自动化脚本不得猜测为 `spherewright-*.json`。
- 直接证据：当前 Plugin 发布并由 MCP 成功发现的文件名与代码模板一致。
- 限制或反例：若发布协议显式改名，脚本与文档必须原子更新。
- 复验触发：RuntimeDescriptorPublisher、协议或发现脚本变化。
- 关联：`src/Spherewright.Plugin/RuntimeDescriptor/RuntimeDescriptorPublisher.cs`、`src/Spherewright.Mcp/BridgeClient/NamedPipeBridgeClient.cs`。
- 最近复验：2026-09-03（源码 MCP `0.4.0.0` 继续从唯一 live `bridge-*.json` 发现当前 Plugin，并完成 initialize/list/live call）。

### EXP-004 — 跨进程恢复票据需要可见且受保护的固定交接目录

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 Codex 文件层、外部 DSP 进程和本机开发 Plugin 部署目录之间的一次性 owned-world resume 交接。
- 当前结论：由工具层写入 `%LOCALAPPDATA%` 的票据在本环境中可能对外部 DSP 进程不可见；安装目录下固定的 `runtime-handoff/owned-world-resume.json` 对 DSP 可见，但目录必须禁用继承并限制为当前用户，票据必须一次性消费。
- 直接证据：前者可由工具读回但 Plugin 报告缺失；受保护的固定交接文件被 Plugin 读取并成功恢复精确 owned world，随后按消费语义删除。
- 限制或反例：这是当前宿主文件可见性现象，不应推断所有 Codex/Windows 环境都相同；安装目录写权限可能不同。
- 复验触发：宿主、权限模型、部署目录、恢复协议或 Windows 用户变化。
- 关联：`src/Spherewright.Plugin/RuntimeDescriptor/OwnedWorldResumeTicketStore.cs`、`src/Spherewright.Plugin/RuntimeDescriptor/WindowsCurrentUserSecurity.cs`、`docs/safety-model.md`。
- 最近复验：2026-08-31。

### EXP-005 — 只有严格绑定的 LastExit 恢复才能延续同一 owned world

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前一次性重启恢复协议和当前自建普通和平存档。
- 当前结论：重启后继续必须同时验证高熵内部存档身份、最低 game tick、planet、和平、非沙盒、1x 和一次性票据；验证成功后立即消费票据并重新绑定新 session，不能枚举或让客户端选择存档。
- 直接证据：当前恢复动作通过上述约束恢复同一存档，并在更高 tick 正常复读/保存；未读取或枚举其他存档。
- 限制或反例：只验证了 DSP 原生 `LastExit` 指向精确目标的路径；LastExit 改变或任一断言失败都必须 fail closed。
- 复验触发：恢复契约、GameSave API、owned-world 身份字段、DSP 版本或 LastExit 行为变化。
- 关联：`src/Spherewright.Plugin/Game/OwnedWorldResumeCoordinator.cs`、`src/Spherewright.Contracts/Sessions/OwnedWorldResumeContracts.cs`、`docs/safety-model.md`。
- 最近复验：2026-08-31。

### EXP-006 — 主菜单 demo 状态没有暴露仍可用的恢复票据

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 `GameSessionTracker` 的未拥有/main-menu-demo 分支。
- 当前结论：票据存储中仍有有效恢复票据时，主菜单 demo 返回的 SessionState 仍可能省略 `restartResumeAvailable`、token 和 `owned-game.resume` capability；这是可观察性/API 缺口，不等于票据不存在。
- 直接证据：票据存储日志确认已加载票据，而同一进程结构化 session 响应未显示恢复字段；使用受保护票据完成恢复。
- 限制或反例：尚未补回归测试；必须限定在安全主菜单状态，不能因此向任意未拥有游戏开放 capability。
- 复验触发：修复 `GameSessionTracker` 后、主菜单状态判定变化、恢复契约变化。
- 关联：`src/Spherewright.Plugin/Game/GameSessionTracker.cs`、`src/Spherewright.Plugin/Game/OwnedWorldResumeCoordinator.cs`。
- 最近复验：2026-08-31（仍待修复）。

### EXP-007 — 客户端包装失败不代表游戏动作未执行

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：所有 prepare/commit 后的客户端脚本、格式化和结果提取。
- 当前结论：commit 返回后，脚本可能在打印不存在字段等后处理阶段报错；不得据此重试。必须用 action result、实体/节点/背包复读判断 before、expected-after 或 outcome-unknown。
- 直接证据：油井建造已创建实体 `129` 且节点 miner count 从 0 变 1，但包装脚本因读取不存在的 `createdObjectIds` 报错；正确字段为 `targetObjectIds`。后续铁矿采集又在打印不存在的 `resourceDeltas` 时抛错，复读仍证明背包铁矿 `3 -> 12`、节点 `7886 -> 7877`，因此没有重试。
- 直接证据：钻石线备料时，第一次 `storage-to-player` 已提交成功，但后处理对不存在的空聚合 `.Sum` 取值产生 PowerShell 非终止错误，遮住了动作输出。下一次 fresh read 已明确看到玩家持有 200 石墨、源仓为 2683；只有在这个新终态已经明确后，才以新意图额外取 200，动作 `51e4ff7b-b2f7-4ab9-a0f3-2a9b54e17eea` 守恒得到玩家 400/源仓 2483。随后单独动作 `97da398f-a37b-412f-8a90-18ff1ffc1bd5` 把玩家 `400 -> 0`、钻石输入仓 `716` 的石墨 `0 -> 400`。这再次证明“包装报错”不能作为未执行证据，也暴露非终止 PowerShell 错误仍可能让同一脚本继续运行；公共 action client 因此默认设置 `$ErrorActionPreference = 'Stop'`。
- 直接证据：抽水站 build commit 完成后，一次性报告代码在访问 ActionResult 中不存在的 `createdObjectIds` 时因 StrictMode 抛错，因而没有显示已取得的 action 终态。没有重放 commit；fresh player read 证明背包 pump `1 -> 0`，按 item `2306` 的实体快照又只找到准备坐标上的唯一新泵 `752`，且其内部已有 30 水、网络 1 供电比 1.0。后续输出中再次对空 `Measure-Object` 结果直接取 `.Sum` 也报错，但只读泵列表不受影响。一次性验证脚本今后应先单独保留 `actionId/state`，对可选字段先用 `PSObject.Properties` 或数组计数保护，再做展示；任何展示异常仍回到 fresh 结构化终态，不把“异常发生在 commit 之后”猜成动作失败。
- 直接证据：粒子容器备料时，`storage-to-player` helper 已完成后，调用方因把 PowerShell 的 `return $r.result` 误写成 `return$r.result` 才抛出本地语法/命令错误。没有重放整段；fresh 三端复读显示玩家已持有 20 个 item `1204`、活跃源仓仍有 11 个（生产并发继续补货）、目标仓仍为原先 20 个，明确证明只完成了取货半程。随后仅提交一次 `player-to-storage`，目标仓精确 `20 -> 40`、玩家归零。以后多段搬运的 commit 后客户端异常必须复读玩家、源仓、目标仓三端，并只补结构化状态明确缺失的半程；不能从脚本退出位置推断动作边界。
- 直接证据：最终关机保存的唯一 `prepare_save/commit_save` 已返回到调用方后，展示代码才因读取不存在的 `prepared.expectedRevision` 在 StrictMode 下报错。没有重放保存；fresh session 将旧边界 `lastOwnedSaveGameTick=8123715`、revision `676` 明确推进为 tick `8340400`、revision `677`，同时报告 `ownedSaveState=saved`、`writeHealth=healthy` 和 `restartResumeAvailable=true`。这把相同规则扩展到 save API：结果展示失败也必须先用主档 tick/revision/票据核销，不能生成第二次保存来“确认”。
- 直接证据：v0.3 物流站备料的 `player-to-storage` 已正常返回后，报告代码又对玩家空钛块集合直接读取 `.Sum`，StrictMode 才抛错。没有重放；fresh 三端复读证明玩家钛块为 `0`、目标仓 `899` 新增且仅有 `40` 钛块，会话 `writeHealth=healthy`、revision `29`。随后普通保存动作 `dd2fc858-6720-46fb-86d9-12a9b11d525e` 把该终态持久化到 tick `8474115`、revision `30`。这再次确认可选聚合必须先判断数组计数，并且客户端后处理错误只用 fresh 终态核销。
- 直接证据：前往钛块仓 `531` 的第三个窄口 Move 已由 action client 等到 terminal 后，报告代码才误读 session 上不存在的 `sessionRevision` 字段并在 StrictMode 下退出。没有重放；fresh player 精确位于 `(-91.80632,-88.05244,-154.01015)`、Walk/速度 0、核心 `400/400 MJ`，session revision 已从前一动作后的 `42` 推进到 `44` 且 write health healthy，证明这次 Move 已完整提交并结算。动作 ID 未因展示失败而猜测或补写，审计以 fresh 终态和单调 revision 核销。
- 直接证据：为进入自动铁仓 `1511` 的 transfer 半径，唯一 Move 已由公共 action client 等到完成，随后一次性展示代码却误读 `$action.terminal`，而 helper 的终态实际位于 `$action.result`，StrictMode 因而在 commit 后报错。没有重放；fresh player 已从 `(-84.008,-94.47305,-155.256317)` 到达 `(-105.052,-86.7283249,-146.740692)`、Walk/0、距仓 77.69 m、write revision 单调推进，紧接着 normal transfer 又从该仓守恒取得 174 铁块。这个样本再次把“动作执行”和“调用方展示”分开；一次性脚本应只读取 helper 已定义的 `prepared/committed/result` 三层字段。
- 直接证据：母星 ILS 到钛矿输入仓的 124 格长带已由游戏接受并持续施工约 8 分钟；中途只读状态为 `8 prebuild / 6 pending / 3 working drones`、核心约 `288.6 MJ`，证明动作仍在正常推进。终态返回后，报告代码才访问不存在的 `builtObjectIds` 并抛错，因而没有取得可展示的 action ID；没有重放。fresh 拓扑从 ILS `1657` 的 port `0` 唯一追出 `1783 -> … -> 1771` 共 124 格有向单链，末端距输入仓 `259` 为 `2.516 m`，同时 `prebuild=0`、玩家传送带 `326 -> 202`、write health healthy。随后独立动作 `541ee6f7-784c-4a5e-8440-e0c96e77601d` 建成 sorter `1784`，反查为 `belt 1771 -> storage 259`，且未覆盖仓原有输出 sorter `532`。这个样本同时说明长施工应以 prebuild/无人机/能量的只读进展判断是否停滞，终态后的可选报告字段缺失仍只做 fresh 核销。
- 直接证据：母星硅线一段 6 格外绕带已经完成 commit，随后一次性展示表达式才把 `Where-Object itemId-eq2001` 错写成无法解析的属性名而退出，因此调用方没有保留 action ID。没有重放；紧邻 fresh 读明确显示传送带 `17 -> 11`、源带 `1967` 已接出预建筑、3 架无人机工作，随后等待 `pending=0/working=0` 后两次有向遍历均稳定得到 191 实体链和自由末端 `1975`。后续独立两格带、sorter `1981` 与 37 格续线又都建立在该唯一末端之后，最终审计为 234 实体无环链，排除了这 6 格未执行或执行两次。
- 直接证据：0.4 受控故障准备时，煤节点 `402` 的 20 件 harvest 已由公共 helper 等到 terminal，调用方随后才访问 ActionResult 中不存在的 `afterPlayerState` 并在 StrictMode 下退出，因此没有取得可展示的 action ID。没有重放；fresh 节点由 `52072 -> 52052`、玩家煤为精确 20、距节点 `1.19 m` 且 Walk/0，revision 单调推进、写健康保持 healthy。随后唯一 refuel 动作 `e92fa9ed-a4f5-4e51-aa5c-afb82a40b165` 又把玩家煤 `20 -> 0`，燃料舱出现 19 件、反应堆开始消耗第 20 件，完整核销了前一动作。
- 限制或反例：若 prepare 在任何 commit 前明确失败且无 action ID，可按普通 prepare 失败处理。
- 复验触发：客户端响应模型、ActionResult 字段或脚本 helper 变化。
- 关联：`src/Spherewright.Contracts/Actions/ActionResultContracts.cs`、`docs/protocol.md`、`docs/safety-model.md`。
- 最近复验：2026-09-03（harvest terminal 后才发生 `afterPlayerState` 展示字段错误；节点 -20、玩家 +20 的 fresh 状态及随后唯一守恒 refuel 共同核销，未重复采集）。

### EXP-008 — 施工无人机会使玩家状态哈希短时变化

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前玩家 canonical state hash 和依赖该哈希的移动、建造、转移动作。
- 当前结论：玩家 hash 包含施工无人机状态；刚完成施工时，即使位置几乎不变，hash 仍可能在读与 prepare 之间变化。下一动作前应等待无人机回收，并取得两次一致 hash，而不是放宽 stale-state 校验。
- 直接证据：连续建造后候选放置多次出现 `STALE_STATE`；等待无人机/玩家稳定后相同类型动作可正常准备。
- 限制或反例：两次一致 hash 是当前调度下的操作准则，不是对所有帧率的时间保证。
- 复验触发：CanonicalStateHash 玩家字段、无人机系统或 stale-state策略变化。
- 关联：`src/Spherewright.Bridge.Core/Safety/CanonicalStateHash.cs`、`src/Spherewright.Plugin/Game/GameStateReader.cs`。
- 最近复验：2026-08-31。

### EXP-009 — 移动 action 终态不等于物理速度已经归零

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前正常地表移动 coordinator 与后续依赖玩家 hash/位置的动作。
- 当前结论：移动 action 达到到达容差后可以终结，但机甲仍可能处于 Drift；后续写入前必须复读速度接近 0 且玩家 hash 连续稳定。
- 直接证据：移动 action 已完成，而紧随其后的速度仍约 `4.22`，15 秒后仍有约 `0.19` 的 Drift。
- 限制或反例：具体衰减时长和阈值受地形/帧率影响；当前 `speed < 0.05` 是操作稳定阈值，不是 DSP API 常量。
- 复验触发：移动终止条件、玩家 DTO、物理/导航实现或游戏版本变化。
- 关联：`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`docs/protocol.md`。
- 最近复验：2026-08-31。

### EXP-010 — 三台风机只够覆盖当前油井的紧负载

- 状态：`invalidated`
- 日期：2026-08-31
- 适用范围：当前普通 1x 世界、实体 `129` 油井和当前版本电力数值。
- 当前结论：旧结论已失效，由 EXP-017 替代。`14000` 是本轮早期记录错误，不是当前 DTO 的油井每 tick 需求。
- 直接证据：10 动作复核时，实体 `129` 的 `powerDemandPerTick=400`；合网后网络 3 的两个消费者（油井与分拣器）总 `energyRequired=550`、`energyCapacity=51000`。热电接入前同一风电网容量为 `15000`，不是“仅余 1000”的紧负载。
- 限制或反例：保留本条用于防止旧数字再次传播；不得继续用于容量规划。
- 复验触发：供电建筑、科技加成、网络拓扑、DSP 版本或油井参数变化。
- 关联：`docs/gameplay-timeline.md`、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（复验失败，已 invalidated）。

### EXP-011 — 当前储仓/热电姿态在约 6.41 m 成功、8.90 m 与 12.8 m 失败

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前版本、小型储仓 `135` 到热电站 `134` 的具体端点姿态。
- 当前结论：储仓 `135`（约 12.8 m）和储仓 `136`（实测约 8.90 m）到热电站 `134` 均为 `TooFar`；储仓 `137`（实测约 6.41 m）到热电站的同类基础分拣器则通过 prepare、正常建成实体 `138` 并实际输送燃料。后续仍必须让 DSP 原生校验决定，不能把 6.41 m 直接当作通用最大距离。
- 直接证据：两次失败 prepare 均未消耗分拣器；成功动作 `8130b214-e4c8-47d4-9a78-cd5975341725` 消耗 1 个分拣器并创建 `137 -> 134` 的实体 `138`，供电后储仓石墨从 18 降至 8、热电站出现石墨燃料读回。
- 限制或反例：判定可能取端口、建筑旋转、碰撞或网格姿态而非建筑中心距离；三个距离都只约束当前建筑类型和具体姿态。
- 复验触发：成功建立更近连接、端口距离计算研究、建筑模型或 DSP 版本变化。
- 关联：`docs/protocol.md`、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（当前姿态的成功/失败边界已实机复读）。

### EXP-012 — 同位置旧分拣器必须从新实体归属候选中排除

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前版本 `CreatePrebuilds` 后的分拣器实体归属与完成复读。
- 当前结论：仅按源端姿态寻找新分拣器会误选同位置旧实体；创建前必须快照同位置既有 ID，完成归属时排除它们并再验证目标拓扑。
- 直接证据：旧进程曾把新实体 `213` 误归属为 `211` 并正确隔离；回归测试复现两实体同位并证明只选择 `213`。当前进程动作 `613f0889-6d15-4fcb-bc79-0ed7834ee396` 又在旧分拣器 `164` 已存在时创建新实体 `181`；两者位置完全相同且 source 均为 `141`，读回仍唯一证明 `164 -> 163`、`181 -> 170`，action target 只包含 `181`，write health 保持 `healthy`。
- 限制或反例：当前版本的同位置双输出范围已实机验证；若实体扫描、DSP 建造完成顺序或 attribution key 变化仍需复验。
- 复验触发：首次同位双输出实机复验、实体扫描/建造完成逻辑或 DSP 版本变化。
- 关联：`src/Spherewright.Bridge.Core/Safety/BuildEntityAttribution.cs`、`tests/Spherewright.Bridge.Core.Tests/BuildEntityAttributionTests.cs`。
- 最近复验：2026-08-31（离线与当前版本同位置双输出实机均 validated）。

### EXP-013 — outcome unknown 后只允许证据化协调，不允许猜测重试

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：所有游戏写动作与 quarantine/restart reconciliation。
- 当前结论：无法证明 before 或 expected-after 时必须冻结该 session 写入；隔离本身不代表必须弃档。只有通过精确 owned-save 身份、动作/状态复读和严格恢复协议完成协调后，才能在同一存档的新 session 继续，绝不能直接重试、回滚或另开档。
- 直接证据：分拣器归属不确定时旧 session 隔离且未继续写；修复部署后通过一次性严格 LastExit 恢复同一自建存档，当前新 session 健康继续。
- 限制或反例：若无法证明精确存档身份、世界约束或动作后状态，仍必须停止；协调机制不能把 unknown 猜成成功/失败。
- 复验触发：任何 quarantine、恢复失败、动作审计模型或安全状态机变化。
- 关联：`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.QuarantineReconciliation.cs`、`src/Spherewright.Plugin/Game/OwnedWorldResumeCoordinator.cs`、`docs/safety-model.md`。
- 最近复验：2026-08-31。

### EXP-014 — PowerShell helper 要避开保留名、别名并显式处理空聚合

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：本仓库 PowerShell 自动化和临时实机调用 helper。
- 当前结论：PowerShell 变量名大小写不敏感，`$pid` 会碰撞只读自动变量 `$PID`；星球变量使用 `$planetId` 等任务专用名。函数名 `Move` 会与 `Move-Item` 命令解析冲突，应使用 `Invoke-GameMove` 等任务专用动词名。StrictMode 下空集合聚合不能直接访问 `.Sum`，必须显式处理空结果。
- 直接证据：`$PID`、空聚合和 `Move`/`Move-Item` 三类 helper 错误均在本轮出现；最后一次冲突发生在任何 Bridge prepare/commit 前，未产生游戏写入。改用任务专用名称和空集合分支后继续。
- 限制或反例：不是 Bridge 行为，不应把脚本异常解释为游戏动作失败。
- 复验触发：helper 固化进仓库、PowerShell 版本或脚本执行策略变化。
- 关联：`scripts/`、EXP-007。
- 最近复验：2026-08-31。

### EXP-015 — M0 执行顺序以可持续生产线为主

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 M0 First Red Matrix 外部 Agent 规划。
- 当前结论：优先建设并验证自动生产链；仅在机器等待产量、科研或移动稳定期间处理恢复 API、黄矩阵远期方案等旁支。手搓只用于解锁或补齐生产线所需的最小启动资源，不能替代自动化验收。
- 直接证据：用户明确指定“优先继续生产线，等待产量的时候再考虑别的问题”，并要求红糖同时规划跨星球钛与黄糖，但先完成行星物流站前置。
- 限制或反例：安全故障、outcome unknown 或会影响下一动作正确性的实现缺口优先于继续写入。
- 复验触发：用户调整优先级、当前产品里程碑完成或出现安全阻断。
- 关联：`AGENTS.md`、`ROADMAP.md`、`docs/gameplay-timeline.md`。
- 最近复验：2026-08-31。

### EXP-016 — 孤立热电站不能依靠无电分拣器完成冷启动

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前热电站 `134`、燃料储仓 `137`、输入分拣器 `138` 和网络 4。
- 当前结论：热电站无燃料时孤立网络容量为 0，输入分拣器也因此无电并停在 `Picking`，不能把首份燃料送入；必须先用已有风电网络/电线杆覆盖分拣器，或采用另一条已有电源的正常物流路径完成冷启动。
- 直接证据：连续 10 秒结构化复读中，储仓始终有 18 个高能石墨，分拣器 `137 -> 134` 拓扑正确但网络 ID 为 0、阶段为 `Picking`，热电网络 4 容量和产出均为 0；新增电线杆 `139`、`140` 后网络合并为 3，分拣器 serve ratio 为 1.0，储仓降到 8，网络容量由 15000 增至 51000。
- 限制或反例：热电站一旦已有燃料或网络已连接其他电源，行为会不同；不能据此推断所有发电设备的启动规则。
- 复验触发：网络 4 接入启动电源后、首份燃料进入后、电力读取或 DSP 版本变化。
- 关联：`docs/research/game-api-m0.md`、`docs/protocol.md`。
- 最近复验：2026-08-31（冷启动前后均已复读）。

### EXP-017 — 当前油井与基础分拣器负载远低于三台风机容量

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前版本、实体 `129` 油井、实体 `138` 基础分拣器和网络 3 的 DTO 单位。
- 当前结论：油井 `powerDemandPerTick=400`，基础分拣器加入后网络总需求为 `550`；三台风机容量 `15000` 对这两个负载有明显余量。热电仍提供额外容量和后续精炼扩展，但不是维持油井本身所必需。
- 直接证据：实体与网络在同一 tick 附近的结构化复读；serve ratio 和 consumer ratio 均为 1.0。EXP-010 的 `14000` 已被反证。
- 限制或反例：DTO 数值是每 tick 内部单位，不能直接当作 UI 瓦数；新增精炼厂、更多分拣器或科技/版本变化后必须重新汇总。
- 复验触发：精炼厂接电、网络拓扑或消费者变化、DTO 单位映射或 DSP 版本变化。
- 关联：`src/Spherewright.Contracts/Power/`、`src/Spherewright.Plugin/Game/GameStateReader.cs`、EXP-010。
- 最近复验：2026-08-31。

### EXP-018 — 批量手搓原料会进入复制队列缓冲，终态才代表整批完成

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前正常 replicator handcraft action 与 PlayerState 的 `handcraftQueue` / inventory 读回。
- 当前结论：批量手搓开始后，整批输入会从可用背包计数进入队列的 `bufferedCount`，成品随批次逐步增加；只有 action terminal 和最终物品差量复读才能证明整批完成。中途不能只看背包减少就判定物品丢失，也不能把已缓冲原料重复预算给下一动作。
- 直接证据：20 批传送带进行中，队列剩 8 批、铁块/齿轮缓冲分别为 16/8，背包已有 42 条带；最终动作证明铁块 `83 -> 43`、齿轮 `20 -> 0`、传送带 `6 -> 66`。
- 限制或反例：只验证了当前 replicator 和这些普通配方；队列取消、背包满或多级自动子配方行为未覆盖。
- 复验触发：handcraft coordinator、PlayerState queue DTO、复制器 API 或 DSP 版本变化。
- 关联：`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`src/Spherewright.Contracts/Players/PlayerStateSnapshot.cs`。
- 最近复验：2026-08-31。

### EXP-019 — 当前两座电线杆约 22.57 m 不会自动连线

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前版本、电线杆 `133` 与 `142` 的具体网格落点。
- 当前结论：两塔实测中心距约 `22.57 m` 时没有合网；精炼厂 `141` 虽被塔 `142` 覆盖，但落在独立网络 4，serve ratio 为 0。加入中继塔 `143` 后，两段约 `12.31 m`、`12.91 m` 均成功连线并把精炼厂并入网络 3。规划电网应保留明显余量并通过消费者/网络汇总复读确认。
- 直接证据：塔 `142` 建成后，power summary 同时存在网络 3 和网络 4；精炼厂报告网络 4、需求 400、供电 0。中继塔 `143` 建成后仅剩网络 3，精炼厂网络变为 3、serve ratio 为 1.0，总网络需求 `950/51000`。
- 限制或反例：精确判定可能受网格吸附后的距离、模型连接半径或节点类型影响；成功/失败样本仍只限定于当前电线杆姿态。
- 复验触发：中继塔建成后、成功/失败的更近距离样本、电网 API 或 DSP 版本变化。
- 关联：`src/Spherewright.Plugin/Game/GameStateReader.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（同一路径中继合网已复读）。

### EXP-020 — 油井到精炼厂不能把两台建筑都直接绑定为传送带端口

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前油井 `129`、未配置精炼厂 `141` 和基础传送带 prepare。
- 当前结论：油井 `129` 存在可用直接输出 belt port；未配置精炼厂 `141` 不提供可绑定的直接输入 belt port。正确的当前路径是“油井直接出带，带末端再用分拣器喂精炼厂”。
- 直接证据：双端绑定 prepare 失败且未消耗物品；随后只绑定油井 source、以自由 `PathEnd` 结束的动作 `d39d8a9b-3f49-4ff5-9e1a-61a654834b22` 成功，消耗 18 条带并创建实体 `144`–`161`，证明先前失败端是精炼厂 destination；动作 `a870028b-2434-4cc3-bf60-f83576450edd` 又成功创建末端 `161 -> 141` 的输入分拣器 `162`。
- 限制或反例：结论限定当前建筑类型/版本/未配置状态；配方启动后的原油实际入料仍需复读。
- 复验触发：只绑定油井的 belt prepare、自由带末端到精炼厂的输入分拣器、建筑或 DSP 版本变化。
- 关联：`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（油井出带与精炼厂输入分拣器均已实机验证）。

### EXP-021 — 自动燃料输入仓的旧余量不能保留给后续手工预算

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：热电站 `134`、输入仓 `137` 和分拣器 `138` 的当前连续物流。
- 当前结论：分拣器接电后会继续把输入仓燃料装入热电站；几分钟前复读到的剩余 8 个石墨随后已变为 0。任何手工取料或跨动作预算都必须在 prepare 前重读自动物流端仓库，不能依赖旧快照。
- 直接证据：从 `137` 取 4 个石墨的 prepare 返回 `INVENTORY_INSUFFICIENT` 且未写入；紧接着复读确认仓 `137` 为空、热电站燃料读回增加，主石墨仓 `114` 仍有 3000。
- 限制或反例：热电站 buffer 的 `count` 是当前 DTO 的发电燃料读数，不能直接当作仓库物品格计数；结论重点是输入仓余量随自动物流变化。
- 复验触发：给输入仓补货、分拣器停机/过滤、发电机燃料模型或状态哈希变化。
- 关联：EXP-016、`src/Spherewright.Plugin/Game/GameStateReader.cs`。
- 最近复验：2026-08-31。

### EXP-022 — 长距离施工前可从已验证主仓正常补充机甲燃料

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前主石墨仓 `114`、正常 transfer/refuel 原语与伊卡洛斯燃料系统。
- 当前结论：低核心能量会拖慢移动和无人机施工时，可从可审计的正常产线主仓取少量高能石墨，再经原生 mecha refuel 路径补入燃料舱；必须分别证明仓库、背包和燃料舱守恒，不能直接写核心能量。
- 直接证据：动作 `35b6fe2b-518f-4f84-ba14-92ffcad2ff9b` 使主仓 `3000 -> 2996`、背包 `0 -> 4`；动作 `213d41ed-9ada-4068-a8f5-e7a7192e078d` 使背包 `4 -> 0`、燃料舱 `0 -> 4`，随后读回一个石墨正在反应、三个仍在燃料格，核心能量正常上升。
- 限制或反例：这是无线输电塔建成前的临时续航，不算“伊卡洛斯无线充电”验收；燃料来源必须已有正常产线证据。
- 复验触发：燃料类型、refuel coordinator、玩家燃料 DTO 或 DSP 版本变化。
- 关联：`docs/safety-model.md`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-08-31。

### EXP-023 — 无线输电必须用独立于燃料的核心能量与电网差量验收

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前无线输电塔 `180`、网络 1 与伊卡洛斯当前版本能源 DTO。
- 当前结论：无线塔建成不能只看实体存在；应同时证明塔接入有容量的电网、玩家在覆盖范围内、反应堆/燃料为空，并在连续快照中看到核心能量上升。普通电力感应塔 `2201` 只可当作电网覆盖建筑，绝不能因为中文名称含“电力”就代替无线输电塔 `2202` 作为机甲充电目标。当前无线塔 `180` 的历史有效样本距玩家约 `10.42 m`，满足完整证据链。
- 直接证据：运行时配方链正常消耗 12 铁矿、7 铜块、18 石矿以及 2 铁块，逐级产出 14 磁线圈、9 玻璃、6 棱镜、3 电浆激发器、1 电力感应塔和 1 无线输电塔；动作 `efb211e2-2d4b-4917-b55e-3cdf31b3506a` 创建实体 `180`。建成后网络 1 节点 `18 -> 19`、需求 `6350 -> 7850` 且全供电；在 `reactorEnergy=0`、燃料格为空的约 10 秒内，核心能量从约 `35.77M -> 36.61M`。
- 直接证据：2026-09-02 等待科研时，动作 `d02fe609-0850-4357-9edd-580d151cd824` 在风机 `262` 旁建成普通电力感应塔 `915`，落点距风机 `7.33 m`、距玩家 `6.13 m`；但它没有形成可接受的无线充电证据，活跃科研下核心仍由 `293.30 -> 292.54 MJ`。策略随即回到已验证无线塔 `180`；密集工厂路径被看门狗终止后，则按 EXP-022 用动作 `d71b78dd-eeae-4019-b4c6-f6630ef12101` 守恒补入 20 氢，核心连续 `206.39 -> 210.60 MJ`，没有把普通塔的存在误报为充电成功。
- 直接证据：行星物流科研等待期再次复验燃料回退。动作 `e770a42f-bbaf-4b28-b361-f51072f5747d` 通过正常机甲燃料流程把玩家氢 `148 -> 128`；fresh 读回为燃料格 19 个、反应堆正在消耗第 20 个且 `reactorEnergy=8,986,666.67`，核心随后由约 `345.76 -> 373.58 MJ`。这证明当前自动科研耗能窗口仍可由守恒燃料续航，但不把它冒充无线输电样本。
- 限制或反例：`10.42 m` 只证明该点在覆盖范围内，不是无线塔最大半径；网络需求差量的内部单位不能直接当 UI 瓦数。普通塔 `915` 的对照发生在机甲科研持续耗能时，负斜率本身不能量化它收到多少电，只足以判定“没有正向充电闭环，不能验收”；无线塔仍要在空反应堆或可扣除负载的独立窗口重测。
- 复验触发：玩家离开/进入覆盖范围、无线塔或电网参数、能源 DTO 或 DSP 版本变化。
- 关联：`docs/research/game-api-m0.md`、`src/Spherewright.Plugin/Game/GameStateReader.cs`、EXP-019、EXP-022。
- 最近复验：2026-09-02（普通电力感应塔 `915` 未被误当无线充电证据；两次独立守恒氢燃料回退均维持科研/移动安全余量）。

### EXP-024 — 建筑有电不代表相邻分拣器处于电塔覆盖范围

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：精炼厂 `141`、原油输入分拣器 `162` 与网络 3 的当前布局。
- 当前结论：精炼厂 `141` 已在网络 3 且 serve ratio 为 1.0，但位于另一侧的输入分拣器 `162` 仍可为 `network=0`、停在 `Picking`；生产设备的供电网络不会通过机器本体给分拣器供电。每个关键分拣器必须单独复读网络/阶段，并由电塔覆盖。新增塔后还必须以分拣器实际携货和下游产物增长闭环，不能只看塔存在。
- 直接证据：配方 16 启动后连续约 40 秒原油输入仍为 0；油井 `129 -> 151…161` 拓扑完整且油井缓冲 50，末端分拣器 `162 -> 141` 拓扑正确，但 `162 network=0`、不工作。动作 `48a48476-2f04-4080-825b-fa64461c0688` 在距分拣器约 7 m 的候选处创建塔 `182` 后，`162` 进入网络 3、阶段变为 `Sending` 并携带 1 个原油；精炼厂配方 16 开始推进，精炼油仓 `163` 从空增长到 15。
- 限制或反例：覆盖半径取决于电塔类型和具体姿态；不能从本例推导固定半径。
- 复验触发：电力或布局变化、分拣器再次停滞、建筑/电塔参数或 DSP 版本变化。
- 关联：EXP-016、EXP-019、`src/Spherewright.Plugin/Game/GameStateReader.cs`。
- 最近复验：2026-08-31（新增塔 `182` 后以携货、配方推进和成品增长完成闭环复验）。

### EXP-025 — 精炼链启动后必须重新按整网峰值校核容量

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前网络 3、精炼厂 `141`、油井与三处分拣器的运行态负载。
- 当前结论：空闲/未供电时的网络需求不能用于生产态容量规划。网络 3 在精炼链启动后需求升至 `19721`，而四台风机容量只有 `15000`，服务率约 `0.7606`；热电站 `134` 无燃料时不会贡献其额定容量。可用正常副产物给新增热电站供料恢复容量，但仍须验证新增物流未破坏既有连接。
- 直接证据：塔 `182` 接通输入分拣器后，同一轮结构化复读显示网络 3 有 10 个节点、6 个消费者、4 个发电机，`energyRequired=19721`、`energyCapacity=15000`、`consumerRatio=0.7606105`；精炼厂 `141` 与分拣器 `162` 的 `powerServeRatio` 同为该值。动作 `82489838-ecdd-4e8e-a95f-83156f0671db`、`b613e28d-b904-4155-8507-d4452ddfbdb2` 建成热电站 `183` 与精炼油输入分拣器 `184` 后，发电燃料读回为精炼油，网络 3 容量变为 `51000`、需求约 `20021`、服务率恢复 `1.0`。
- 限制或反例：需求随分拣器工作阶段和机器停启波动；这里的内部每 tick 数值不能直接映射成 UI 瓦数，也不能代表未来扩线容量。
- 复验触发：热电补燃料、新增发电机/消费者、电网合并拆分、科技或 DSP 版本变化。
- 关联：EXP-016、EXP-017、EXP-019、`src/Spherewright.Contracts/Power/`。
- 最近复验：2026-08-31。

### EXP-026 — 建造完成后的首次单体查询仍应允许一次只读重读

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 `commit_build` action 终态之后紧接的 `inspect_factory_entity` 调度窗口。
- 当前结论：action 已证明建造完成并返回目标 ID 后，紧接的首次单体查询仍曾短暂返回 `INVALID_ENTITY`；这不能授权重试建造。应保持写入不重放，改用只读列表/单体重读确认实体是否已经稳定可见，并保留 action 结果作为协调证据。
- 直接证据：动作 `48a48476-2f04-4080-825b-fa64461c0688` 已以 `completed` 返回目标 `182` 和物品 `1 -> 0`，随后第一次 `inspect 182` 报 `INVALID_ENTITY`；不进行任何写重试，稍后的 item-filter 列表明确包含实体 `182`，位置与 action 的吸附后计划坐标完全一致，分拣器也已接电运行。
- 限制或反例：目前只有一个样本，尚不能断定是跨 tick 可见性、请求时序还是读取侧竞态；不得把所有 `INVALID_ENTITY` 都视为瞬时错误。
- 复验触发：再次出现 action 终态后首次查询失败、读取调度或 action 完成条件变化、DSP 版本变化。
- 关联：EXP-007、EXP-013、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-08-31（单样本，保持 observed）。

### EXP-027 — 新分拣器验收必须证明端点既有连接未被覆盖

- 状态：`invalidated`
- 日期：2026-08-31
- 适用范围：当前普通分拣器 prepare/commit/completion、带既有分拣器连接的储仓端点。
- 当前结论：本条把 `FactoryEntitySnapshot.connections` 的缺省误判成运行链断开，已被物料流反证。不得据此修改端点选择或重启游戏；由 EXP-028 替代。
- 直接证据：动作 `b613e28d-b904-4155-8507-d4452ddfbdb2` 后，储仓 `163` 的公开连接槽显示新 `184`，旧分拣器 `164` 显示 `connections=[]`，一度被误判为静默断线。但后续连续只读采样中，`164` 从 `Picking` 变为 `Sending`、实际携带 1 个精炼油，`pickTarget=141`、`insertTarget=163` 保持；仓库存量从 78 增至 96，再在 10 秒内从 96 增至 97，同时新增热电仍在耗油。
- 限制或反例：保留该条用于阻止把展示层连接列表错误升级为安全缺陷。
- 复验触发：无；如出现真实物料不流动，应按 EXP-028 的证据顺序重新诊断。
- 关联：EXP-012、EXP-028、`src/Spherewright.Plugin/Game/GameStateReader.cs`。
- 最近复验：2026-08-31（被连续运行态流量反证，已 invalidated）。

### EXP-028 — 分拣器运行拓扑以目标字段和物料流为准

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前普通分拣器 DTO、共享建筑端点姿态和 `FactoryEntitySnapshot.connections` 展示。
- 当前结论：多个分拣器共享建筑侧姿态/槽位时，公开 `connections` 列表不保证列出每个仍可运行的分拣器关系；不能仅凭某个旧分拣器 `connections=[]` 判定断线。诊断顺序应是 `pickTargetObjectId/insertTargetObjectId`、阶段/携货变化、上下游库存差量，最后才把连接列表作为辅助拓扑信息。
- 直接证据：新 `163 -> 184 -> 183` 建成后，旧 `141 -> 164 -> 163` 的 `connections` 为空，但 `164` 的目标字段保持 `141/163`，连续采样出现 `Picking -> Sending` 和携货 `0 -> 1`；仓库在新增热电持续取油时仍由 78 增至 96，再由 96 增至 97，证明旧输入链仍运行。
- 限制或反例：belt path 的逐段方向仍由连接字段直接验收；本条只约束分拣器共享建筑端点的公开表示，不能推广到任意组件。
- 复验触发：分拣器目标 DTO、连接读取实现、共享端点行为或 DSP 版本变化。
- 关联：EXP-012、EXP-020、EXP-027、`src/Spherewright.Plugin/Game/GameStateReader.cs`。
- 最近复验：2026-08-31。

### EXP-029 — 储液罐的公开工厂快照此前未采集流体缓冲

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前已安装 Plugin 的 `GameStateReader` 与 DSP `0.10.34.28529` 的 `TankComponent`。
- 当前结论：储液罐 `buffers=[]` 不能证明罐内没有流体；已安装版本只调用 `CaptureStorage`，没有读取 `entity.tankId` 对应组件。源码已新增 `CaptureTank`，把 `fluidId/fluidCount/fluidInc` 映射为 `tank-fluid` buffer，但在不具备健康会话计划重启票据时不为只读增强中断当前存档；部署前保持 observed，当前氢链改由下游分拣器携货和红矩阵研究站输入/产出闭环验收。
- 直接证据：罐 `165` 长期返回空 buffers，但上游氢分拣器 `181` 反复实际携带 item `1120`。当前 `Assembly-CSharp.dll` 的元数据明确显示 `FactoryStorage.tankPool/tankCursor` 以及 `TankComponent.fluidId/fluidCount/fluidInc`；新增读取代码完整解决方案构建 0 warning/0 error、49 项测试通过。
- 限制或反例：源码尚未装入当前 DSP 进程，未取得罐 `165` 的新字段实机读回；不能提前标为 live-validated。
- 复验触发：下次具备严格健康会话计划重启能力并部署后、储液罐堆叠/开关行为、DSP 版本变化。
- 关联：`src/Spherewright.Plugin/Game/GameStateReader.cs`、EXP-028、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（程序集字段与离线编译已验证，live 部署待办）。

### EXP-030 — 当前仓库使用本地 SDK，程序集字段研究优先 Mono.Cecil

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 Windows 工作区的离线构建和 DSP 程序集元数据检查。
- 当前结论：系统 PATH 的 `C:\Program Files\dotnet\dotnet.exe` 只有 runtime；本仓库构建必须显式使用 `.local\dotnet\dotnet.exe`。检查 `Assembly-CSharp.dll` 字段时优先用 BepInEx 自带 `Mono.Cecil.dll` 做静态元数据读取，不要给 PowerShell 注册会递归处理 satellite assembly 的脚本式 `AssemblyResolve`。
- 直接证据：PATH dotnet 报 `No .NET SDKs were found`，而 `.local\dotnet` 的 SDK `8.0.424` 完整构建成功；一次 PowerShell `AssemblyResolve` handler 因资源程序集递归触发 stack overflow，未修改游戏，改用 Mono.Cecil 后稳定列出 `TankComponent` 和 `FactoryStorage` 精确字段。
- 限制或反例：换机或重新安装 SDK 后路径可能变化；静态元数据只能证明签名，行为仍需实机复读。
- 复验触发：工作区 SDK 布局、构建脚本、BepInEx/Mono.Cecil 或 DSP 程序集变化。
- 关联：`.local/dotnet/`、`scripts/sync-game-refs.ps1`、`docs/research/environment.md`。
- 最近复验：2026-09-03（继续使用本地 SDK `8.0.424` 完成 204 项测试与当前游戏引用的完整构建；DSP 程序集哈希未变，并用本地 IL 工具复核物流载具/订单行为）。

### EXP-031 — 范围内 harvest 会通过正常玩家动作接近资源点

- 状态：`observed`
- 日期：2026-08-31
- 适用范围：当前 `prepare_harvest/commit_harvest`、伊卡洛斯地表状态和约 63.76 m 的铁矿目标。
- 当前结论：资源节点在当前正常交互/建造范围内时，不必预先单独调用 move；harvest action 会让伊卡洛斯走正常接近与采集流程，并在整批采集完成后终结。必须使用资源读取返回的 `nodeId`，不能把工厂 `objectId` 当成同一命名空间；源码现已要求 `withinPlayerBuildArea=true`，范围外目标应先用有界 move waypoint 接近。仍应在终态后复读玩家位置/速度，不能假定所有地形都能无阻导航。
- 直接证据：提交 6 个铁矿的动作 `4e7d6aba-e3de-44c1-87e8-91e454649590` 前，节点 `4` 距玩家 `63.76 m`，玩家在约 `(-43.08,-103.46,-165.92)`；动作正常完成并守恒产出 6 铁矿后，玩家稳定位置约 `(-63.14,-47.00,-184.10)`，即目标矿脉旁。
- 限制或反例：已有一个 63.76 m 无阻到达样本；旧 DLL 曾错误接受星球另一侧的资源节点并走满全局窗口，详见 EXP-040。超出交互范围、跨障碍、基座卡脚或飞行/航行状态必须改用显式有界 waypoint。
- 复验触发：下一次远距 harvest、导航受阻、交互范围/移动实现或 DSP 版本变化。
- 关联：EXP-009、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-08-31（单个 63.76 m 样本，保持 observed）。

### EXP-032 — 从活跃货带末端接续路径会把端点货物回收到玩家

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前普通多段传送带 build action、玩家 before/after inventory 差量与正在输送氢或磁铁的带路。
- 当前结论：从一段正在输送物品的末端 belt 继续构造下一段时，当前路径实现会在接续点创建同位的新首段，端点上的 1 件货物可按正常建造行为回收到玩家背包。build commit 仍严格验证建材精确消耗；非建材正差量必须单独记账，不能算作配方产出或生产线验收，也不能据此重放建造。
- 直接证据：第二段氢主干动作 `3870525c-14ce-4e41-936c-36984d560858` 消耗 25 条带并使玩家氢 `0 -> 1`；第三段动作 `8f733ab2-a4da-4616-ae03-ee5778299ba3` 消耗 22 条带并使氢 `1 -> 2`。两次均从已有活跃氢带末端继续建造、均恰好回收 1 氢。2026-09-03 的独立货物复验中，动作 `7ea82222-bd7c-4c54-ad44-26a24f7be20c` 从正在输送磁铁的末带 `1392` 续建 23 格，精确消耗传送带 `23 -> 0`，同一 action DTO 记录磁铁 `14 -> 15`。fresh 拓扑证明 180 格新带仅有预期单链、末端未接旧工厂，该 `+1` 不能是下游自动产出。三个动作的 session 均保持 `healthy`；代码在 `CreatePreparedPrebuildsOnMainThread` 对建材执行 `baseline - previews.Count` 精确检查，最终 action DTO 汇总完整施工窗口的库存差量。
- 限制或反例：已验证的是当前版本、基础传送带和氢/磁铁两种货物的同位接续样本；内部究竟在 `CreatePrebuilds` 哪一步回收货物尚未以 IL 单独归因。这些正差量都不用于任何首产或产线验收。
- 复验触发：其他货物/带级接续、改用非同位续带、动作库存审计或 DSP 版本变化。
- 关联：EXP-007、EXP-018、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`。
- 最近复验：2026-09-03（第三个独立续带动作以磁铁 `+1` 复现，并以全链无外接拓扑排除下游来源）。

### EXP-033 — 首个自动红矩阵必须以同一研究站的 0→正数闭环验收

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前普通 1x 世界、研究站 `256`、配方 `18` 与能量矩阵 item `6002`。
- 当前结论：首个自动红矩阵的充分证据是同一生产研究站在接入原料前输出为 0，接入两条正常物流后输入/进度/输出连续变化，并且输出增长到至少 1；玩家背包或手搓结果不能替代该闭环。完成后还要通过显式 save action 保存精确 owned world。历史 M0 里程碑的自动产出、最终保存和下游连续出料均已完成。
- 直接证据：配置动作 `dc3e404f-9c69-48d9-860f-897bcea2f834` 后，研究站 `256` 明确为配方 18，石墨/氢/能量矩阵 buffers 全为 0、网络 2 全供电。动作 `bfe37097-76ff-4195-b16e-b450f1a3e568` 创建 `114 -> 257 -> 256` 石墨输入，动作 `7ceb1cf9-345d-4d74-9b55-abc1954dbd18` 创建 `255 -> 258 -> 256` 氢输入。随后只读快照中输出 `6002` 为 3，20 秒后为 6，之后累积到 10；显式保存动作 `b399facb-48cd-4838-b7ab-9c9762b6def7` 由 DSP 正常 save API 确认 tick `2499658`。后续动作 `750c7803-c967-4996-a056-63fcb0efcac8` 建成输出仓 `260`，动作 `7ae664fe-246f-41e8-bf85-f68270bf3262` 建成 `256 -> 261 -> 260` 出料分拣器；复读时仓内能量矩阵已为 22，研究站输出缓冲为 0、`isWorking=true`、双输入各 4，证明满缓存恢复为连续生产。
- 限制或反例：玩家背包曾因两次活带续接存在 2 个另行记账的氢，已按 EXP-032 排除。输出仓内的 22 个矩阵包含此前缓存的 10 个和接通后新生产的至少 12 个，不能把整个 22 都记作接通后的新增产量；但相同设备恢复工作、输入减少和仓储超过原缓存上限共同证明连续流成立。
- 复验触发：配方/研究站/上游改造、输出取走、后续显式保存或 DSP 版本变化。
- 关联：EXP-015、EXP-020、EXP-028、EXP-032、`docs/gameplay-timeline.md`。
- 最近复验：2026-08-31。

### EXP-034 — 红矩阵运行态会暴露电网容量瓶颈，新增风机后已恢复满供电

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前网络 2、研究站 `256` 与输入分拣器 `257/258` 的运行态。
- 当前结论：建筑空闲/刚落地时的电网余量不能代表配方运行态。研究站刚建成、未接料时网络 2 需求为 `1100/15000`；红矩阵生产开始后总需求曾达到约 `16378–16702`，服务率约 `0.898–0.916`。新增一台已确认并入同网的风机后容量升至 `20000`，当前红矩阵站和三只分拣器同时运行时仍可保持 1.0 供电；后续扩线仍必须按运行态复读，不能沿用本次余量。
- 直接证据：研究站 `256` 未配方运行时网络 2 为 `1100/15000`、ratio 1.0；两路输入运行后连续快照为 `16378/15000` 与 `16702/15000`，服务率约 0.9。动作 `135158e1-b16b-4ebb-9dbe-85d358f05217` 建成风机 `262`，实体复读明确 `powerNetworkId=2`；同一时间片网络 2 由 4 节点/3 发电机/`15000` 容量变为 5 节点/4 发电机/`20000` 容量，在 `energyRequired=16528` 时 `energyServed=16528`、`consumerRatio=1.0`。
- 限制或反例：分拣器瞬时工作使需求和单体 serve ratio 波动；数值是内部每 tick 单位，不能直接映射 UI 瓦数。当前约 17% 容量余量只覆盖现有红矩阵线，不足以证明黄矩阵或额外研究站可直接接入。
- 复验触发：新增发电/电塔合网、研究站停启、消费者变化或 DSP 版本变化。
- 关联：EXP-017、EXP-025、`src/Spherewright.Contracts/Power/`。
- 最近复验：2026-09-03（新铁矿机 `1496` 在实际开采时需求 `7000`、单风机网络容量 `5000`，瞬时服务率仅 `0.7143`；缓存满后空闲需求降至 `400`、表面服务率回到 `1.0`，再次证明必须按运行态而不是空闲态验电）。

### EXP-035 — 长途移动前必须自动做能量预算并保留回充余量

- 状态：`observed`
- 日期：2026-08-31
- 适用范围：当前普通地表 `prepare_move/commit_move`、伊卡洛斯核心能量/燃料舱读回与已有无线输电塔 `180`。
- 当前结论：低电时不再等待人工提醒。长途移动前必须先复读核心能量、燃料舱和最近无线输电塔实体；充电目标必须取该塔的实时 `position`，不能把“生产网络附近”或某个历史路点等同于无线覆盖。若无法带着余量到达，就先用玩家已拥有的正常燃料应急，或去更近的正常燃料仓补给，再按短 waypoint 自动回充。能量耗尽导致的正常移动终止是 `action_failed`，不是 quarantine，也不能据此重放同一移动。原先“核心高于 50% 即可启动返程”的单阈值已被 2026-08-31 新样本推翻：当前旧 DLL 的成功 move 可能残留订单并继续耗能，因此在修复版安全部署前，返程必须从满电或带可用燃料开始；每段终态后立即复读速度、位置和能量趋势，目标是抵达真实无线塔时仍保留至少 20% 容量。若终态后速度未收敛或能量异常下降，不得等待固定窗口，应立即用一个有界正常订单覆盖并复读。能量预算与卡路/残留订单检测必须并行，不能把单次移动折算成固定 MJ/m。
- 直接证据：动作 `1255b3ce-bfbf-4b9a-b1c7-d2ea40b00c3e` 从约 `(-27.88,-88.40,-176.46)` 向 40.83 m 目标正常移动，途中在约 `(-6.18,-101.01,-172.14)` 因核心、反应堆和可用燃料连续 600 tick 均不足而以 `action_failed` 终止；会话未进入隔离。后续动作 `b64a5abf-3afe-4c9b-a5b7-f134b85979f5` 在前半段实际移动约 6.5 m 后位置连续不变、仍持续耗能，证明同一能量现象也可能来自实体碰撞。远端恢复时先等基础发电到 `78.31 MJ`，40 m waypoint 正常到达，再在 77.70 m 合法范围内由动作 `83ad93d2-3c84-4f3c-8bff-cc06a374ad7a` 采 20 煤并守恒加注；补采/加注后核心 120 MJ、燃料仓 42 煤。约 297.5 m 返程被拆为 8 个约 37 m 球面航段，最终核心仍为 120 MJ、燃料仓余 23 煤，并由范围内铁矿动作 `6d2df711-199e-4466-9ce9-1fa6204cf220` 明确清除旧 DLL 的末段移动订单。
- 限制或反例：当前移动耗能受速度、机甲科技、地形、动作状态和旧 DLL 残留订单影响；50% 起步阈值已经失效并撤销，20% 到达余量仍只是当前运行目标，不是 DSP 固定参数。修复版部署后必须重新采样正常终止的分段耗能，再决定是否恢复低于满电的出发阈值。
- 复验触发：下一次自动回充、机甲能量/移动科技变化、无线塔布局变化、燃料 DTO 或 DSP 版本变化。
- 关联：EXP-009、EXP-022、EXP-023、EXP-031、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`。
- 最近复验：2026-08-31（新增驱动引擎 I 后的返程反例，撤销 50% 单阈值；真实无线塔目标与约 2.60 MJ/s 回充完成闭环，保持 observed）。

### EXP-036 — 移动必须用位移和剩余距离双窗口提前判停

- 状态：`observed`
- 日期：2026-08-31
- 适用范围：当前源码中的普通 `move` player order、同一 owned world 的地表实体碰撞，以及下一次安全部署后的实机复验。
- 当前结论：移动开始时记录真实位置和目标剩余距离。连续 180 game ticks 内累计位移不足 0.75 m，判为物理卡住；即使玩家仍在侧移，连续 600 ticks 未把历史最佳剩余距离减少至少 1 m，也判为路线无进展。两种情况都只中止 Spherewright 精确归属的 order，并以可重规划的 `action_failed` 结束，不等到全局超时或能量耗尽。完全断能期间暂不套用 180-tick 碰撞分类，保留原有 600-tick power-starved 原因。DSP 只有一个当前 player order，因此未终止的 move/harvest 之间采用 single-flight，后提交者返回可重试的 `SERVER_BUSY`，不得默默覆盖前一个 order。
- 直接证据：旧运行 DLL 下，动作 `b64a5abf-3afe-4c9b-a5b7-f134b85979f5` 从 `(-86.64,-47.84,-174.05)` 前进约 6.5 m 后停在 `(-81.45,-51.78,-175.42)`；三秒外部只读采样已足以证明零位移，但动作仍等到旧全局窗口才失败。并发动作 `b65825d1-d6c2-4953-b4d0-50bbb118a38a` 没有取得独立 player order，也等到超时。待两者终止后，动作 `ac1354b5-5125-4a18-a0ff-0d90c38c44d9` 配合一次跳跃越过实体，正常到达 `(-73.78,-57.49,-177.00)`，终态剩余 1.48 m，核心能量约 71.9M；这把碰撞卡住与目标无效、断能区分开。`MovementProgressWatchdogTests` 覆盖硬卡、有效位移、侧移无目标进展、目标进展复位和非法窗口。
- 限制或反例：当前游戏进程仍加载旧 Plugin DLL；新增 watchdog 与 single-flight 已离线构建/测试，但尚未在安全重启后的新进程实机触发，故不得标为 live validated。上述一次跳跃由 post-M0 脱困时的 Computer Use 输入完成，只用于保住当前存档并确认碰撞根因，明确排除在 M0 验收和结构化移动能力证据之外；后续不把该手段作为自动执行路径。
- 复验触发：下一次可安全恢复同档并部署 Plugin、移动速度/科技变化、阈值调整、order 归属逻辑或 DSP 版本变化。
- 关联：EXP-009、EXP-031、EXP-035、`src/Spherewright.Bridge.Core/Safety/MovementProgressWatchdog.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`。
- 最近复验：2026-08-31（离线 5 个专项测试与一次旧 DLL 碰撞现场，保持 observed）。

### EXP-037 — 产线里程碑必须同时保存游戏并提交推送工程经验

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前用户要求下的同一 Spherewright owned world 与后续每一种新产物的自动生产流水线。
- 当前结论：无安全/版本/存档归属问题时持续使用同一个存档。每完成一种产物的生产流水线，必须先用同一生产设备的结构化快照证明目标产物从 0 增长到正数或继续累积，再执行普通 `prepare_save/commit_save`；随后把对应实现、现场证据和经验账本作为一个 Git 里程碑提交并推送。输出缓冲满后暂时停机不否定“首次产出”里程碑，但应立即把下游储存或消费列为该产线的第一项完善工作。不得为了部署离线补丁而热替换运行 DLL；只有同档恢复条件可证明时才重启。
- 直接证据：红矩阵研究站 `256` 已由 `0 -> 3 -> 6 -> 10` 正常累积 item `6002`，其首次里程碑由保存动作 `02f50a58-276c-4b90-be62-bb9645920abf` 保存在 tick `2710106`，并以 Git 提交 `d4768d3` 推送到 `origin/main`。后续 `256 -> 261 -> 260` 连续出料后，仓库复读到 22 个能量矩阵、研究站恢复工作，风机 `262` 使网络 2 满供电；保存动作 `13b305c2-d979-4a9f-a181-bcf71a9b71ec` 再次正常保存精确 owned world 于 tick `3540979`，session revision `250 -> 251`、`writeHealth=healthy`。下一种产物动力引擎由制造台 `285` 配方 `105`、输入分拣器 `289`、输出分拣器 `288` 和专用仓 `287` 构成；仓内产量从 9 继续增至 30，制造台与网络 1 均为满供电运行。保存动作 `901f4289-0155-484e-ac14-4c6ecb442aa3` 由正常 save API 确认 tick `3746997`，会话 revision 为 `307`、`ownedSaveState=saved`、`writeHealth=healthy`。
- 直接证据：最新电动机里程碑中，齿轮/磁线圈/电动机三层设备与全部过滤输入、带路和输出 sorter 均结构化读回，仓 `727` 自动累积 39 个 item `1203`，逐存档日记序号 12 记录首个产线电动机。动作 `d0516fbf-b333-4266-aed1-dbdd5cd53e37` 随后正常保存同一主档到 tick `6221009`，revision `107 -> 108`、写入健康；本次 README、交接状态和 EXP-074 随同该产物边界作为独立 Git 里程碑提交并推送。
- 直接证据：紧接着的水里程碑由普通手搓、原生水面建造、无人机完工、泵口五段带和末端 sorter 构成；仓 `753` 的 item `1000` 在独立观察窗 `9 -> 31`，日记序号 14 记录首次产线水。动作 `44f2f0d0-9713-4e35-9073-45f1ce5c7787` 保存 tick `6267723`、revision `123 -> 124`、写入健康；对应 README、研究证据与新 EXP-075 随同水产物边界作为下一独立 Git 里程碑提交并推送。
- 直接证据：有机晶体里程碑以空载三过滤共享输入仓、配方 `25` 化工厂和专用输出仓构成；三个原料均来自既有自动产物并通过正常 transfer 守恒备料，仓 `762` 的 item `1117` 由 `1 -> 7`，日记序号 15 捕获首次产线事件。动作 `06cfa947-25da-490f-9e52-895989ff8e7a` 保存 tick `6315704`、revision `189 -> 190`、写入健康；对应状态、EXP-074/076 与有机晶体边界作为下一独立 Git 里程碑提交并推送。
- 限制或反例：`restartResumeAvailable=false` 时，即使已有正常保存也不能假定关闭后可由工具安全续接；应保持当前进程，直到恢复票据链可用或用户明确结束本次运行。若出现 quarantine、版本不匹配、存档身份不明或无法证明产出，则不得用提交标签掩盖未完成状态。
- 复验触发：每个新产物首次自动产出、每次 save/commit/push、会话健康变化、恢复协议变化或用户调整里程碑定义。
- 关联：EXP-005、EXP-033、`docs/gameplay-timeline.md`、`docs/incident-fix-log.md`。
- 最近复验：2026-09-01（有机晶体按“自动来源守恒备料 → 过滤化工全链 → 专用仓增长 → 日记 → 普通保存 → 独立工程提交推送”流程验收）。

### EXP-038 — 主菜单加载动作必须在副作用前证明幂等容量并收敛票据副本

- 状态：`observed`
- 日期：2026-08-31
- 适用范围：当前 `new-game` / `resume-owned-game` 主菜单加载协调器、quarantine reconciliation 状态转换、按 scope 的 `IdempotencyCache`、两个固定 owned-world resume ticket 位置和脱敏日志。
- 当前结论：任何可能让 DSP 开始载入世界或清除 quarantine 的 commit，都必须在副作用前确认其幂等 scope 仍有容量；不能先触发加载/状态转换，再发现 action result 无法登记。这个容量检查只因这些协调器都在同一 Unity 主线程串行执行而安全，不能外推为多线程 reservation。reconciliation 若在预检后仍意外无法登记，必须立即重新 quarantine 并返回 `ACTION_OUTCOME_UNKNOWN`。一次性恢复 token 若在 runtime 与受保护 handoff 两个固定位置存在相同副本，消费时必须清除当前副本和其余同 token 副本；不同 token 文件不得误删。票据路径、底层 IO 异常消息和保存路径不得进入正常日志或 MCP 错误，只保留异常类型。
- 直接证据：提交前代码复核发现两个主菜单 coordinator 都在 `DSPGame.StartGame*` 返回后才调用 `TryAdd`，当 scope 已满时会出现“副作用已开始但缓存拒绝”的窗口；reconciliation 也曾先缓存“成功”再尝试清 quarantine，失败重放会产生错误成功语义。新增 `IdempotencyCache.HasCapacity(scope)` 在锁内先清理过期项并按 scope 判断；`IdempotencyCache_HasCapacityPrunesExpiredEntriesWithinScope` 证明满 scope、独立 scope 和过期回收。主菜单 coordinator 在消费 plan/调用 DSP 前使用检查；reconciliation 先预检、再清除、再登记，并对理论上的登记失败重新隔离。`OwnedWorldResumeTicketStore.Consume` 现在对另一个固定路径只在 constant-time token 匹配时删除；对相关运行时/保存/加载错误的源码扫描未再发现路径字段或 `exception.Message` 外泄。完整解决方案 0 warning / 0 error，55 tests passed。
- 限制或反例：双路径同 token 的实际文件消费尚无独立自动化测试或下一次重启实机样本；`HasCapacity` 不是通用并发预留 API，如果未来把 commit 移出 Unity 主线程，必须改为原子 reservation。当前运行 DLL 也未包含本次脱敏/预检改动。
- 复验触发：下一次 quarantine/resume、ticket store 路径变化、commit 调度线程变化、idempotency cache API 变化、日志格式或 DSP 版本变化。
- 关联：EXP-004、EXP-005、`src/Spherewright.Bridge.Core/Safety/IdempotencyCache.cs`、`src/Spherewright.Plugin/Game/TestWorldCoordinator.cs`、`src/Spherewright.Plugin/Game/OwnedWorldResumeCoordinator.cs`、`src/Spherewright.Plugin/RuntimeDescriptor/OwnedWorldResumeTicketStore.cs`。
- 最近复验：2026-08-31（容量语义单元测试和完整构建通过；双路径消费保持 observed）。

### EXP-039 — player order 必须用对象引用证明归属并在动作终态精确终止

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：DSP `0.10.34.28529` 的 `Player.Order(OrderNode, false)`、普通 move/harvest action 和当前源码的订单终止路径。
- 当前结论：move/harvest 下单时必须保存传给 DSP 的同一个 `OrderNode` 对象；完成、卡路、断能和全局超时只能在 `ReferenceEquals(player.currentOrder, action.PlayerOrder)` 成立时调用 `AbortOrder()`。不能用请求坐标与游戏内部目标坐标的近似相等来证明归属，也不能只因当前订单类型相同就无条件停止。动作终态后还应短窗复读位置、速度和核心能量趋势；正常 Abort 后仍可能保留数秒物理惯性，下一段移动必须等速度降到小阈值再读取 optimistic hash。若动作已成功但超过该 settling 窗口仍周期性位移/耗能，先判断精确订单是否残留，再决定是否用另一个有界正常订单覆盖，禁止把它误判成充电或继续盲目叠加 move。
- 直接证据：旧运行 DLL 的路点动作 `e8aaaacc-e007-439a-bd5b-8bdc8401261e` 在距请求路点小于 `1.5 m` 时返回成功，但随后玩家从约 `(-43.11,-12.00,-195.94)` 持续发生小位移，核心能量从约 `101 MJ` 降到接近零；零距离 move `4ad897c1-58f3-4841-a607-9d3083eaad41` 也返回成功但未消除周期性掉电。Mine 动作 `c57c3a2d-8526-4cc5-bad8-fb267a1d72d5` 以明确的 600-tick 断能失败覆盖旧 Move，之后位置稳定、核心能量从约 `1.14 MJ` 连续单调升到 `2.28 MJ`。第二次现场复验中，约 297.5 m 返程的末段 move 到达后仍必须用范围内 Mine 动作 `6d2df711-199e-4466-9ce9-1fa6204cf220` 覆盖；该动作正常产出 1 铁、终态位置/速度稳定。当前程序集 IL 证明 `PlayerOrder.Order` 把传入 `OrderNode` 同一引用直接设为 `currentOrder`，`Player.AbortOrder` 委托 `PlayerOrder.Abort`；源码已改为保存并比较精确引用。部署修复版后的动作 `ed605c94-10df-409b-91db-08c6aea4e0d5` 在仍距目标 `27.36 m`、连续 180 tick 位移少于 `0.75 m` 时于约 3 秒内明确 `action_failed`，只终止自己的订单；玩家停在 `(-91.71,-50.10,-170.78)`、速度 `0`，核心能量仍为 `400/400 MJ`，未再等全局超时或耗尽能源。planet `102` 的最后一段正常 move `985c9a1e-bb9f-4893-9f12-6c277f6ef4fa` 终态即时速度仍约 `4.18 m/s`，约 4 秒后自动降至 0、位置稳定且没有持续耗能，给出了“惯性 settling”与旧残留订单的直接对照。
- 限制或反例：本次 live 样本验证了物理停滞窗口和精确终止；600-tick 最佳目标进度窗口、部署后的 harvest 冲突以及断能分支仍保留各自复验触发。Mine 覆盖动作同时把玩家带近电网，单调回充证据不能单独量化普通电塔/无线塔的充电机制。
- 复验触发：下一次安全部署、任一 move/harvest 成功或失败终态、DSP `PlayerOrder` 实现变化、single-flight 规则变化或再次出现终态后位移/掉电。
- 关联：EXP-031、EXP-035、EXP-036、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（修复版 180-tick 物理停滞窗口和精确订单终止已实机验证）。

### EXP-040 — factory objectId 与 resource nodeId 是独立命名空间，harvest 必须限距

- 状态：`observed`
- 日期：2026-08-31
- 适用范围：当前工厂/资源 DTO、`prepare_harvest`、DSP `0.10.34.28529` 的实体池与矿脉池。
- 当前结论：任何工厂 `objectId` 都不得直接作为资源 `nodeId` 使用；要采矿机覆盖的矿脉，必须读取该矿机的 `resourceNodeIds`，再逐个 `inspect_resource_node`。手采只接受当前复读为 `withinPlayerBuildArea=true` 的资源，范围外目标返回 retryable `TARGET_OUT_OF_RANGE`，必须先通过短距离 move waypoint 接近。调用方还应同时核对 `resourceType`、位置、距离和 state hash，不能只因整数 ID 存在就提交。
- 直接证据：工厂实体 `106` 是红矩阵站旁、位置约 `(19.03,-130.50,-150.63)` 的煤矿机，覆盖资源节点 `308/309/312/313/318/321/325`；资源节点 `106` 实际是位置约 `(-133.22,130.92,72.03)` 的铁矿脉。误把前者 ID 用于 harvest 后，动作 `e5795a21-9b9a-4e62-9183-b03678c8f8e9` 在 7200-tick 有界窗口内未产出并明确 `action_failed`，玩家从红糖线附近走到该远端铁矿旁，核心降至约 `17.87 MJ`，session 仍 healthy。恢复时通过资源列表明确选择煤矿节点 `346`，复读为 `resourceType=Coal`、距离 `77.70 m`、`withinPlayerBuildArea=true` 后，动作 `83ad93d2-3c84-4f3c-8bff-cc06a374ad7a` 与 `02b53268-84cc-4bab-907d-755b4db70c61` 分别守恒产出 20/40 煤。源码已新增 `WithinPlayerBuildArea` 强制检查并强化 MCP 参数说明。
- 限制或反例：运行中仍是旧 DLL，因此新的 `TARGET_OUT_OF_RANGE` 尚未实机触发；目标限距不能替代 move 自身的能量预算、stall watchdog 和订单精确终止。整数值相同本身不是错误，错误是跨 API 命名空间推断身份。修改后的完整解决方案已离线构建为 0 warning / 0 error，55 tests passed。
- 复验触发：下一次安全部署、首次范围外 harvest 拒绝、资源/工厂 DTO 或 DSP pool 变化、任何自动从矿机选择矿脉的逻辑。
- 关联：EXP-009、EXP-031、EXP-035、EXP-036、EXP-039、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`、`src/Spherewright.Mcp/Tools/SpherewrightTools.cs`。
- 最近复验：2026-08-31（旧 DLL 远端误采现场、当前 DTO 交叉复读和完整离线构建已验证；新源码 live 待办）。

### EXP-041 — 无燃料机甲仍有低速原生基础发电，但只可作为应急等待

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：DSP `0.10.34.28529` 的 `Player.GameTick -> Mecha.GenerateEnergy(deltaTime)` 与当前机甲基础发电属性。
- 当前结论：存活且静止的伊卡洛斯即使 `reactorEnergy=0`、`reactorItemId=0`、燃料仓为空，也会先按 `corePowerGen * deltaTime` 原生恢复核心能量，再进入燃料反应堆分支。当前实测约 `80 kW`，可在没有附近燃料/电网时安全等待出最低移动预算；但从零补满当前 `120 MJ` 容量约需 25 分钟，不能把它当作常规补能方案。自动策略仍应优先范围内正常燃料、已建无线塔或就地合法建造充电点，并在短 waypoint 前维持 EXP-035 的 50%/20% 保守阈值。
- 直接证据：无订单、位置固定约 `(-133.54,130.87,71.12)`、反应堆与燃料仓均为零的 10 次采样中，核心能量在约 20 秒内从 `36.13 MJ` 单调增至 `37.61 MJ`，每 tick 增量稳定且 player action hash 不变。当前程序集 IL 显示 `Player.GameTick` 调用 `Mecha.GenerateEnergy`；该方法在任何燃料判断前执行 `coreEnergy += corePowerGen * deltaTime` 并封顶，随后才处理 `reactorEnergy/reactorStorage`。
- 限制或反例：约 `80 kW` 是当前科技/机甲配置的现场值，升级可能改变 `corePowerGen`；移动、采矿、无人机或战斗消耗会同时发生，净能量趋势不能替代进度/卡路判断。远端没有电网时的基础恢复也不验证无线充电。
- 复验触发：机甲能源科技升级、corePowerGen DTO 增补、DSP 版本变化、任一“无燃料仍掉电/不回升”现场或自动充电策略调整。
- 关联：EXP-022、EXP-023、EXP-035、EXP-039、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（当前程序集 IL 与独立 20 秒现场趋势相互验证）。

### EXP-042 — 原始桥 DTO 与 MCP 参数形状不同，精确建造必须先核对 plannedPosition

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 `scripts/SpherewrightBridgeClient.ps1` 直接调用 `prepare_build`、`PrepareBuildRequest` JSON DTO，以及 MCP `spherewright_prepare_build` 包装层。
- 当前结论：直接调用原始桥方法时，首选坐标必须写成 `preferredPosition = @{ x; y; z }`；`preferredPositionX/Y/Z` 只是 MCP 工具签名为了标量参数提供的包装层字段，不是原始 DTO 属性。未知 JSON 字段当前会被忽略，从而静默回退到“玩家附近自动选址”。所有精确布局必须拆成手动 prepare/检查/commit：确认 `plannedPosition` 与请求点差量、目标设备距离、端点方向和 `buildKind` 后才能提交；不能用会立即提交的便捷 helper 跳过该检查。
- 直接证据：一次原始桥请求误用 `preferredPositionX/Y/Z`，意图在红糖站约 `(32.80,-132.39,-146.78)` 建仓，prepared 结果却明确回退到玩家附近 `(24.67,-70.18,-185.87)`，动作 `f09f39a0-3941-4cb1-8881-3eaf3be2d833` 建成仓库 `259`。随后改用 `preferredPosition` 向量；右侧候选被原生 click-build 以 `NeedGround` 安全拒绝且无副作用，再按既有输入的切向轴枚举。动作 `750c7803-c967-4996-a056-63fcb0efcac8` 最终在 `(26.22,-137.05,-143.56)` 建成仓库 `260`，请求/落点差 `0.21 m`、距实验室 `6.29 m`；动作 `7ae664fe-246f-41e8-bf85-f68270bf3262` 随后建成 `256 -> 261 -> 260`，仓内能量矩阵增长到 22。
- 限制或反例：仓库 `259` 是一次正常、守恒但位置不合预期的建造，当前没有安全拆除 API，故保留作附近资源中转，不能把它计入红糖输出拓扑。若以后 JSON 反序列化改为拒绝未知字段，错误会从静默回退变成显式失败；无论哪种行为，仍必须核对 prepared 计划。
- 复验触发：桥 DTO/MCP 包装签名变化、客户端 helper 改造、下一次带首选坐标的核心建筑、belt path 或资源建筑。
- 关联：EXP-007、EXP-020、`src/Spherewright.Contracts/Actions/NormalActionContracts.cs`、`src/Spherewright.Mcp/Tools/SpherewrightTools.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-08-31（错误回退、显式原生拒绝和正确向量落点三类现场均已复读）。

### EXP-043 — 枯竭矿机不能阻断旧生产设备，应用新矿带侧向续入

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前网络 1 的枯竭铁矿机 `14`、旧矿带 `15…20`、熔炉 `21/22` 与附近铁矿簇 group `1`。
- 当前结论：旧矿机资源列表变空但仍占用既有传送带输入口时，不重放旧建造，也不把工厂实体 ID 当成矿脉。应从资源列表选择范围内、`minerCount=0` 的真实 node，建新矿机后用短的可验证多段 belt 绕开障碍，最后通过分拣器把新带侧向送入仍可用的旧带段。每段先检查 `plannedPath`、建材预算和 source identity；验收看矿机覆盖节点、分拣器实际携货以及旧下游仓储增长。
- 直接证据：节点 `4` 在玩家旁约 `1.25 m`、余量 `7868`、group `1`、`minerCount=0`。动作 `b26c7feb-e783-4d20-a6a1-a67a9df5b7a2` 建成矿机 `263`，复读覆盖节点 `2/4` 且开始积累铁矿。动作 `c5916029-f67e-48d0-8822-75074ccb3239` 与 `136fd72c-4362-4b8e-864f-e6b3729a50fc` 分别建成 10 段和 8 段 belt，末端 `274`；动作 `feb6c798-c8a6-4422-afd3-838e1b4e7855` 建成 `274 -> 282 -> 17`。随后 `282` 明确携带铁矿，磁铁仓 `30` 从 0/11 持续增长到 149，证明旧熔炉链恢复。
- 限制或反例：当前路径绕过的是已知风机、旧矿机和密集建筑；不能把两段坐标硬编码到其他星球或矿簇。旧铁矿仍同时供给铁块熔炉 `21` 与磁铁熔炉 `22`，后续扩产必须重新检查分流吞吐。
- 复验触发：矿簇耗尽、旧带端口变化、新矿机/路径重建、下游熔炉停滞或 DSP 版本变化。
- 关联：EXP-020、EXP-031、EXP-040、EXP-042、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（新矿机、两段 ordinary belt、侧向分拣器和磁铁库存增长闭环成立）。

### EXP-044 — 混料公共带的背压必须先隔离上游灌入源

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前网络 1 的公共带 `69…42`、铁块仓 `28`、磁铁仓 `30`、主仓 `26`、磁线圈制造台 `73`。
- 当前结论：上游分拣器持续持货、注入点无空位，而远端目标分拣器长期空载时，应先检查公共带是否被另一种物品灌满，而不是拆线或重复建造。需要暂停一个持续运行的 source sorter 时，`sorter-filter` 只能在它空闲且空载时配置；可先把其源仓精确搬空，等已持货送完，再把过滤项设为源仓中不存在的已解锁物品，最后把原物料放回源仓。隔离后必须以原堵塞货物排出、新货物通过、下游产物增长三层证据验收。
- 直接证据：分拣器 `70` 持续把仓 `28` 的铁块注入 belt `69`，末端 `71` 又向已满的主仓 `26` 回送铁块；磁铁输入 `83` 因 belt `55` 无空位而长期持有 1 磁铁，`283` 在 belt `46` 无货，制造台 `73` 只有 1 磁铁并停机。先从主仓取出 200 铁块、300 铜块释放容量，再把仓 `28` 的 1583 铁块精确转到玩家；`70` 空闲后由动作 `19b0e9c6-dc42-479e-9e6c-51069853cb09` 设置过滤项 `2001`（传送带），再把 1584 铁块放回。复读确认 `70` 空闲、空载且 filter 为传送带，铁块不再灌入；随后公共带排出旧铁块，主仓先出现 20 磁铁/18 磁线圈，之后增至 66 磁铁/60 磁线圈，制造台 `73` 恢复满电工作。
- 限制或反例：该过滤项是有意暂停当前铁块公共带，不是永久最优布局；钢材等依赖这条铁块带的设备会失去新供料。后续应改成专线或可证明容量充足的物流后再清除 filter，不能在现有背压未解决时直接恢复 `70`。
- 复验触发：清除/改变 `70` 过滤项、公共带重新混料、主仓接近满载、钢材扩产或 DSP 版本变化。
- 关联：EXP-015、EXP-021、EXP-028、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-08-31（背压诊断、空仓停源、原生 filter 配置、回填与磁线圈增长完整闭环）。

### EXP-045 — 建筑落点合法不代表后续分拣器可达

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前精确核心建筑 prepare、制造台/小型储物仓布局和基础分拣器连接。
- 当前结论：`prepare_build` 对核心建筑只证明该建筑本身在所选位置合法，不证明它与未来设备的分拣器距离合法。多设备产线必须给物流连接留余量：先用切平面环形候选做无副作用 prepare，核对吸附后的 `plannedPosition`；建筑完成后立即对计划端点单独 prepare sorter。若返回 `TooFar`，不得提交分拣器或猜测重放，应保留已建普通建筑作独立仓储，并另找更近候选。
- 直接证据：动力引擎制造台 `285` 在 `(-83.80,-48.57,-175.21)` 合法建成并配置配方 `105`。第一座输出仓 `286` 在约 `(-82.95,-55.85,-173.44)` 也被原生 click-build 接受，但后续 `285 -> 286` 的分拣器 prepare 明确返回 `BUILD_CONNECTION_INVALID/TooFar`，未提交分拣器。围绕 `285` 以约 5.1 m 切平面半径枚举候选后，仓 `287` 在约 `(-79.61,-46.12,-177.81)` 合法建成，动作 `9ff505dd-02d2-42cf-9ba2-b09911964180` 建成 `285 -> 288 -> 287`；输入 `26 -> 289 -> 285` 同样经过独立 prepare。仓 `287` 的动力引擎随后从 9 增到 30，远仓 `286` 保持空置工具仓。
- 限制或反例：约 5.1 m 是本次候选搜索半径，不是所有建筑姿态的固定上限；真实判定取决于两端 slot pose、建筑旋转、地表网格和分拣器等级。当前没有安全拆除 API，故错误但合法的仓 `286` 不删除。
- 复验触发：任何多设备精确布局、新分拣器等级、建筑旋转/碰撞模型、拆除能力或 DSP 版本变化。
- 关联：EXP-011、EXP-020、EXP-042、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（同一制造台的一个 TooFar 仓位和一个成功仓位形成直接对照）。

### EXP-046 — 自动回充必须绑定真实无线塔，并在动作终态检查残留移动

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前旧部署 DLL、无线输电塔实体 `180`、机甲核心 II 前的 `200 MJ` 容量和普通 `prepare_move/commit_move`。
- 当前结论：自动回充是一个闭环，不是“向生产区走一次”。先用工厂实体读回取得无线塔的实时坐标；移动终态必须复读 `speed`、位置和至少一个短窗能量趋势。若只有约 `80 kW` 的基础恢复，说明尚未进入无线覆盖；只有远高于基础恢复且位置落在目标塔附近，才算回充成功。旧 DLL 下，动作成功后若仍有非零速度或持续掉电，应立即处理残留订单，禁止继续等待；完全断能时则利用 600-tick 原生看门狗形成明确终态，再从新状态发下一段。移动到普通生产路点不等于进入无线塔覆盖。
- 直接证据：从煤节点返程时，核心先在 60 煤燃料下充至 `100.27/200 MJ`，首段途中继续烧煤并到达满电；后续动作 `8c710799-539a-47ad-ab10-1e010248f66d` 正常到达第二路点，但旧 DLL 残留移动使第三路点附近核心从约 `113.26 MJ` 继续降到 `27.80 MJ`。零距离覆盖动作 `376cf3d9-9def-4804-b12a-a400fd2a4390` 返回完成时仍读到 `speed=1.56 m/s`、核心仅 `0.011 MJ`，约 4 秒后才停稳。断能动作 `5ebac28f-9fc2-4e82-bc63-fde265fd887a` 以明确 600-tick 无能量原因失败并把玩家带到生产路点约 9.18 m 内；静止 5 秒只增长约 `0.405 MJ`，与约 80 kW 基础恢复一致，证明该路点不在无线覆盖。实体 `180` 实时位置为约 `(-108.25,-28.83,-165.93)`；动作 `91d7e745-4397-4371-ad1d-e2f4e387b871` 到达其 2.47 m 内后，8 秒核心从约 `71.996 -> 92.761 MJ`，净增 `20.765 MJ`、约 `2.60 MJ/s`，随后充满 `200 MJ`。
- 限制或反例：约 `2.60 MJ/s` 是当前塔、当前电网和机甲科技的现场净值，不是固定无线功率；实体 `180` 的 `inspect_factory_entity` 当前对 power-node 的 `powerNetworkId/isWorking` 字段不完整，故验收依赖实体位置、玩家静止和能量斜率三者联合证据。断能看门狗只提供有界失败，不是常规导航策略。
- 复验触发：精确 `OrderNode` 修复部署、无线塔移动/增建、机甲能源科技升级、电网欠供、充电 DTO 增补或再次出现动作完成后移动。
- 关联：EXP-023、EXP-035、EXP-036、EXP-039、EXP-041、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`。
- 最近复验：2026-08-31（错误生产路点的基础恢复与实体 `180` 下的无线净充电形成直接对照）。

### EXP-047 — 星际航行必须先落独立检查点，失败只重复加载同一票据

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 DSP `0.10.34.28529`、同星系飞行实现、`GameSave`/`DSPGame` 原生保存加载路径与钛星往返。
- 当前结论：实际起飞提交必须把“独立保存成功”作为进入 `Fly/Sail` 之前的硬前置：内部生成高熵 `Spherewright_PreFlight_*` 名称，调用 `GameSave.SaveCurrentGame`，用 `GameSave.ReadHeader` 证明精确 tick，再把检查点 ID/token、内部档名、主 owned-save 身份、源 session/revision、起点/终点和状态哈希原子落到当前用户保护票据。任何一步失败都不得起飞。航行明确失败后只通过该可重复 token 调用 `DSPGame.StartGame` 加载同一档；加载后继续保留票据，下一次失败仍回到同一检查点，不枚举、猜测或接受外部存档名。采用阶段必须排除主菜单演示 `GameData`，等待 `GameLoader` 结束且本地星球就绪；`GameData.Import` 会在读取存档 tick 前主动清空 `DSPGame.LoadFile`，所以该瞬态字段不能作为加载完成后的身份证据。最终证明改为提交时再次校验精确文件/header、唯一内部 `StartGame` 调用、嵌入的主 owned-save 身份、起点/模式和 `[savedTick,savedTick+3600]` 采用窗口。
- 直接证据：当前程序集反编译确认 `SaveCurrentGame(string)`、`ReadHeader(...)`、`DSPGame.StartGame(string)` 以及 `GameData.Import(BinaryReader)` 清空 `DSPGame.LoadFile` 的顺序。首次实飞在 planet `104 -> 102` 前保存并复读检查点 tick `4617708`；同一保护票据在多次失败尝试后由动作 `dce307ac-9c46-4768-8f0e-25b4e4ebde05`、`a51f8653-cdab-478e-bd3b-558498e0f190` 等重复加载，均重新采用同一 tick 窗口、起点星球和主 owned-save 身份，而非创建新档。最终动作 `e6ba15c2-b04b-420a-8b9c-977671a63395` 以该检查点起飞并在 planet `102` 物理着陆。返航前又创建独立检查点 tick `4808424`；旧 DLL 物理落地但误报后、首版修复暴露瞬时 Walk 后，动作 `829be384-6740-45db-b3a2-5a8320e1b7a3` 与 `a27a51de-abde-4f0f-b378-cfacde88b25b` 继续从同一票据恢复。最终动作 `d95955e7-cd86-48dd-b79f-4cb54734863c` 成功返航并通过 600-tick 稳定落地，证明双向“先存—失败复读同一档—继续飞行”闭环成立。
- 限制或反例：`ReadHeader` 只证明文件/tick，不能替代载入后的身份与模式校验；瞬时 `Walk` 也不能替代稳定着陆，详见 EXP-052。超出保守飞行时长时控制保持活跃，不能把估算超时本身误判成坏档。当前保护票据仍可复用，但主档已经在稳定返航后正常保存，后续不得无理由回滚到该旧检查点。
- 复验触发：下一次安全 Plugin 部署、首次独立起飞检查点、任一飞行失败/重载/重复重试、成功着陆、DSP 保存头或飞行能耗实现变化。
- 关联：EXP-004、EXP-005、EXP-036、EXP-038、`src/Spherewright.Plugin/RuntimeDescriptor/FlightCheckpointStore.cs`、`src/Spherewright.Plugin/Game/FlightCheckpointReloadCoordinator.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.InterplanetaryFlight.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（去程检查点 `4617708`、返程检查点 `4808424`、跨进程同票据重复加载和稳定双向着陆均已 live）。

### EXP-048 — 首次手搓与首次产线产出必须使用两个原生计数域

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：当前 DSP `0.10.34.28529`、逐 owned-save 日记、机甲复制器、工厂生产统计以及科技树选择。
- 当前结论：不能用背包增量区分“手搓”和“产线”。手搓以 `GameHistoryData.GetFeatureValue(2140000 + recipeId)` 的持久完成次数乘 runtime `Results/ResultCounts`；产线以刚完成游戏 tick 的 `FactoryProductionStat.productRegister` 为独立信号。两类 item ID 分别去重，所以同一物品可以各有一次首次记录。科技/升级首次选择从正常 `currentTech/techQueue` 观察，并按 `TechProto.page` 的 `0/1` 分类。每条记录同时保存带时区的 ISO 实际时间、原始 `GameMain.gameTick` 与 60 tick/s 格式化局内时间。
- 直接证据：当前程序集 IL 显示每个 `ForgeTask.Produce` 后调用 `AddFeatureValue(2140000 + recipeId, 1)`，包括不触发顶层 `onTaskDelivery` 的嵌套前置手搓；`Mecha.AddProductionStat` 直接调用 `AddProductionToTotalArray`，不写 `productRegister`，而矿机、制造、分馏、研究站、物流、电力与戴森生产 tick 均引用该寄存器。`TechProto.page` 对 ID `<2000` 返回 0，否则返回 1。实现以 owned-save 内部身份的 SHA-256 派生日记 ID，在当前用户保护目录原子持久化，并新增 owned-only 只读 MCP 工具；完整解决方案 0 warning/0 error，62 tests passed（Contracts 4、Bridge.Core 45、MCP 13）。修复版部署并严格恢复同档后，日记以 `attached_existing_save`、`historicalCoverageComplete=false` 从 tick `4428079` 挂接，保护目录生成一个 SHA-256 派生文件；正常点选基础化工 `1121` 后新增 `technology_first_selected`，实际时间 `2026-09-01T00:49:36+08:00`、游戏 tick `4462081`、局内时间 `000d 20:39:28` 三者同时可读，未补造任何旧事件。此后日记已跨多轮正常保存/重启持续到 sequence `36`：化工厂/抽水站/高速分拣器给出 `manual_item_first`，塑料到推进器给出独立 `production_line_item_first`；首次选择升级页的垂直建造 `3701` 又在 tick `8155733` 记录 `upgrade_first_selected`（`2026-09-01T22:24:19.1590085+08:00`、本局 `001d 13:45:28`），随后粒子磁力阱 `1703` 于 tick `8244528` 形成 sequence `36` 科技选择事件，两条均 durable、无 pending/error。
- 限制或反例：当前主档早于该功能存在，无法从统计恢复过去事件的真实墙钟时间。首次附着会把已有手搓、生产和科研 ID 作为无时间的 historical seed，并明确返回 `historicalCoverageComplete=false`；不得把迁移时刻伪称旧物品的首次时刻。新档从 Spherewright 采用帧开始才具有完整覆盖。当前已验证文件创建、旧档迁移、跨进程持续、手搓/产线各自首次事件以及科技/升级首次选择；但还没有在 live 中让同一个新物品先后获得手搓和产线两条独立事件，也没有新档完整覆盖样本，因此本条仍保留 `observed`。
- 复验触发：本次安全部署、首次日记文件创建/读取、首个此前未出现物品的手搓与产线双事件、首次科技/升级选择、跨进程恢复、DSP 生产统计或 feature ID 行为变化。
- 关联：EXP-005、EXP-037、`src/Spherewright.Plugin/Game/GameplayJournalManager.cs`、`src/Spherewright.Bridge.Core/Journals/GameplayFirstOccurrenceDetector.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（日记已 durable through sequence `36`；垂直建造首次升级选择、粒子磁力阱科技选择、四类事件和跨进程持续均已 live；同物品双来源及新档完整覆盖仍待复验）。

### EXP-049 — 互为必需的两种原料不能无约束共用短带

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前基础传送带、基础分拣器、蓝矩阵站 `76` 与短距离双原料补给。
- 当前结论：当配方必须同时取得 A/B 两种原料时，不得让两个无过滤源分拣器向容量很小的同一短带灌料；任一原料先占满全带后，设备因缺另一种原料无法启动，也就无法消费前者释放位置，形成稳定背压死锁。应使用两条独立输入带、已验证的配比/过滤方案，或足够长且具有受控混流的线路。只看源分拣器 `Working=true` 或持货不能证明物品到达设备。
- 直接证据：新建 `73 -> 296 -> 295…291 -> 297 -> 76` 后磁线圈正常到达；随后专用电路仓 `298` 的分拣器 `299` 也接入同一 5 段带，蓝矩阵站先出现 `coil=6/circuit=0`，随后出现 `coil=0/circuit=6`，两只源分拣器分别保持携带 1 件、末端无可消费目标。独立电路带 `298 -> 311 -> 309…300 -> 312 -> 76` 与独立磁线圈带 `73 -> 330 -> 329…325 -> 331 -> 76` 接通后，蓝矩阵站恢复工作；同一基础化工研究哈希从 `720 -> 1123`，研究站出现 `6710` 的蓝矩阵内部缓冲，证明两条专线实际送达并被消费。
- 限制或反例：这不是“所有混料带都禁止”；具有排序、过滤、优先级、足够缓冲或已证明配比的 sushi belt 仍可能可靠。当前结论限定于无过滤、短带、互为启动前提的双输入。
- 复验触发：升级传送带/分拣器、引入堆叠或过滤、改变带长、配方速率、输入拓扑或出现再次背压。
- 关联：EXP-015、EXP-044、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（蓝矩阵双输入由混料死锁改为两条独立专线后恢复产出和研究哈希增长）。

### EXP-050 — 原生 Sail 切换与离开母星必须分成两个受控阶段

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：DSP `0.10.34.28529` 的 `PlayerMove_Fly.GameTick`、`PlayerMove_Sail`、同星系近距离星际航行和当前 planet `104 -> 102` 样本。
- 当前结论：当前程序集没有公开 `SwitchToSail`。进入 Sail 必须先让普通 Fly 路径满足高度、水平速度、推进器和建造 UI 条件，并持续通过原生 `input1.y/input0.y` 维持上升与前进；仅把 `targetAltitude` 写成 50 会被 native tick 降为 49.9 而永久错过阈值。满足条件后可严格复现 `PlayerMove_Fly.GameTick` 的同一分支：清空 Build command、设 `movementStateInFrame=Sail`、调用 `ResetSailState`、同步相机、通知 scenario 和移动状态变化，不改位置或能量。进入 Sail 后不能立即直指目的星：若射线穿过母星，必须先以径向外飞和切向绕行清除遮挡，再转入目的星制导。
- 直接证据：实机 Fly 阶段在目标高度约 100 m、水平速度约 14.3–14.7 m/s、推进器等级 2、blueprint `None` 时进入 Sail。若立即指向 planet `102`，玩家首先向 planet `104` 表面回落；新增 departure 控制后，离表高度约 `132 -> 345 -> 499 m`，相对速度约 `129 -> 195.6 -> 199.2 m/s`，随后 `localPlanet=null`，目的星表面距离持续从约 `61106 -> 56424 -> … -> 2123 m` 并最终原生着陆。全程转向、加速和刹车均调用 `UseSailEnergy`，没有位置、星球或能量直接赋值。
- 限制或反例：目前仍只有一个星系和同一对行星；控制频率使首航高能石墨从 87 降至 44，返航也消耗约 24 煤并把满核心压到接近零，说明能量节流仍需结合 tick 频率、轨道相位和更多距离样本优化。目的星瞬时 Walk 还可能回跳 Drift，航行成功必须叠加 EXP-052 的稳定落地窗口。
- 复验触发：下一次返航、飞行距离/相对轨道变化、出生点不遮挡目的星、能量消耗异常、DSP Fly/Sail 实现变化。
- 关联：EXP-035、EXP-046、EXP-047、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.InterplanetaryFlight.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（`104 -> 102` 去程与从同一 `102 -> 104` 检查点重复返航，均覆盖原生 Sail、离场、巡航、刹车和着陆）。

### EXP-051 — 长距离地表移动使用有界球面分段，并在每段后等待惯性归零

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：当前 `prepare_move/commit_move`、半径约 200 m 的 planet `102`、无显著工厂障碍的矿区间移动和 `scripts/invoke-surface-route.ps1`。
- 当前结论：跨越数百米的地表目标不应一次提交或用直角坐标线性插值穿过球体。先把起点/终点归一化，在球面做 slerp，并把每个路点重新投影到当前玩家半径；每段弧长暂限 30 m、单独 fresh read→prepare→commit，任一已提交动作失败立即停下。动作终态后等待速度降至 `<=0.1 m/s` 再读取下一段 hash；只有明确发生在 commit 前的 `STALE_STATE` 可以重新只读准备，不能重放已提交动作。最终还要复读目标距离、Walk、速度和写入健康。
- 直接证据：首次临时 10 段路线把玩家从钛矿约 `216.7 m` 外带到节点 `322` 的 `6.70 m` 内，随后守恒手采 1000 钛。固化脚本从该矿区到煤节点 `380` 的剩余球面弧长 `246.2 m` 分为 9 段，动作目标误差均约 `1.93–2.23 m`，核心约 `85.7 -> 70.5 MJ`；最终复读距煤节点 `2.04 m`、范围内、Walk、速度 0、写入健康。脚本初版暴露两类可复验错误：PowerShell 浮点夹取/逗号优先级会产生错误向量；最后一段终态即时速度约 `4.18 m/s` 会让下一次位置 hash stale。当前版改为显式标量 clamp/括号、逐段 settling 和仅 prepare-stale 重读。
- 限制或反例：这不是全局寻路器，也不会绕过建筑、悬崖或复杂碰撞；密集工厂仍由 EXP-036/039 的停滞 watchdog 在首个失败段终止，随后必须换侧向路点。当前只有 planet `102` 两条开阔路线样本，因此保持 `observed`，30 m 也只是保守经验值。
- 复验触发：首次密集工厂路线、明显高差/水面、不同星球半径、任一段 stall、连续三条不同地形路线成功或 DSP Move 订单变化。
- 关联：EXP-031、EXP-035、EXP-036、EXP-039、EXP-040、`scripts/invoke-surface-route.ps1`。
- 最近复验：2026-09-01（钛矿接近和钛矿→煤矿两条球面分段路线；后一条由固化脚本 live 完成）。

### EXP-052 — 飞行动作不能把瞬时 Walk 当成稳定着陆

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前 DSP `0.10.34.28529` 的目的星 Fly/Drift/Walk 过渡、`player.speed`、返航动作终态和可复用飞行检查点。
- 当前结论：`localPlanet/player.planetId` 已指向目的星且某一 tick 出现 `Walk`，只证明发生地表接触，不证明着陆稳定。完成条件必须要求连续 600 game ticks 同时满足：目的星身份一致、玩家存活、`Walk`、速度 `<=0.1 m/s`；任何 Drift、Fly、Sail 或超速都会清零连续计时。首次目的星接触后给 7200 ticks 的有界 settling 窗口，仍不稳定则明确失败并保留原检查点。动作成功后还要短窗复读位置、速度与核心/燃料趋势。
- 直接证据：只修复判断顺序的返航动作 `deb65f97-117d-463a-bf52-2da2b9091086` 在首次 Walk 瞬间返回 completed，但即时速度仍约 `3.41 m/s`；5 秒后复读为 `Drift`、约 `0.10 m/s`，燃料仓 `79 -> 67`、核心仅约 `0.2 MJ`，因此该“成功”被否决并由动作 `829be384-6740-45db-b3a2-5a8320e1b7a3` 重载同一检查点。部署连续窗口后，动作 `d95955e7-cd86-48dd-b79f-4cb54734863c` 先报告 grounded `201/600` ticks，最终只在 `600/600` 后完成；之后 10 秒三次样本位置完全一致、Walk/速度 0，核心 `32.84 -> 37.30 -> 41.74 MJ` 正常回充，未再漂移。
- 限制或反例：600/7200 ticks 是当前碰撞与返航样本的保守阈值，不保证所有水面、极端地形或未来 DSP 版本都最优；稳定窗口解决“过早宣布完成”，不替代航行能量预算或碰撞物理。目的星若长期 Drift，必须失败并重载，而不是延长到无界等待。
- 复验触发：下一次不同落点/星球返航、水面或高坡着陆、任何成功终态后位置变化、DSP Fly/Drift/Walk 转换变化。
- 关联：EXP-039、EXP-047、EXP-050、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.InterplanetaryFlight.cs`。
- 最近复验：2026-09-01（同一返航检查点的瞬时 Walk 反例与 600-tick 稳定成功正例）。

### EXP-053 — 地表低速不等于落地，Drift 必须转向已验证的陆地锚点

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：planet `104` 的水面/浅滩、`prepare_move/commit_move`、`scripts/invoke-surface-route.ps1`、工厂实体与矿脉坐标形成的陆地锚点。
- 当前结论：逐段移动结束后，只有 `movementState=Walk` 且速度 `<=0.1 m/s` 才能继续；`Drift` 即使速度接近 0 也会持续耗能，不能当作 settled。路线首次进入 Drift 时立即停止并复读附近实体/矿脉，优先移动到已现场证明为 Walk 的精确落点，而不是只瞄准建筑中心；建筑另一侧可能仍在水里。即使起终点都是已验证陆地，两点间的球面 slerp 弧段仍可穿过水面；陆地身份不具有“连线仍在陆地”的传递性。Drift 中位置每 tick 变化，普通 read→prepare→commit 会频繁 `STALE_STATE`，恢复动作应在同一进程内有界重复“fresh read→prepare→立即 commit”，只重试未提交的 stale，取得 commit 后绝不重放。新增边界：若对岸精确锚点已现场证明为陆地、核心能量足够且单个订单仍受停滞/能量看门狗约束，可以从 Walk 直接把一个连续移动订单绑定到对岸锚点；不得把水面中点设为分段终点，也不能因玩家进入 Drift 就在原订单外无界续跑，只有同一订单最终稳定回到 Walk 才算跨水成功。
- 直接证据：母星铁矿节点 `53` 的最后路线终态为 Walk、距节点 `1.84 m`；至无线塔 `180` 的三段路线也保持 Walk，8 秒核心净增约 `21.98 MJ`。随后直达红糖仓的球面路线在第三段检测到 `Drift`、速度约 `0.099 m/s` 后立刻停止；核心仍足以由动作 `5d8dfc76-b998-4e9f-ba3c-ab4234466285` 在第 2 次原子绑定后抵达风机 `82` 一侧并恢复 Walk。密集基座处动作 `e745c98e-e992-4a06-afcc-854eeebe3b63` 又被 180-tick 看门狗以剩余 `13.07 m` 提前判停，侧移到未占用铜矿节点后绕行成功。水面带末端 `468` 附近再次出现 Drift；瞄准电塔 `143` 的另一侧仍未落地，改用此前已证明的 Walk 坐标后，动作 `bd2842e0-4f05-4c15-9723-24a98ef7c839` 在第 7 次未提交 stale 重试后恢复 Walk。
- 限制或反例：资源节点、风机或电塔“存在”只说明实体建在星球表面，不保证实体中心的每一侧都可行走，也不保证两个陆地锚点的球面插值不跨水；优先复用已有 Walk 坐标与已验证连续锚点，首次新锚点/弧段仍需短窗状态/能量复读。原子 stale 重试只解决漂浮快照变化，不绕过碰撞、能量预算、单飞订单或动作失败。
- 复验触发：新增地形类型、不同星球海洋比例、目标建筑两侧落地差异、连续三次无需重试的 Drift 恢复、Move 哈希或玩家运动状态实现变化。
- 关联：EXP-035、EXP-036、EXP-039、EXP-041、EXP-046、EXP-051、`scripts/invoke-surface-route.ps1`。
- 最近复验：2026-09-01（母星两次 Drift 早停、同进程 stale 绑定和精确 Walk 落点恢复均已 live）。

### EXP-054 — 跨区长带必须先扫起点方向，再按施工前沿分段并从陆地等待

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：普通基础传送带/分拣器、已有输出仓到远端研究站的跨区供料、DSP 原生 path-build 校验和建造无人机。
- 当前结论：仓库已有输入/输出设备时，不能直接猜一个朝目标的首段。先在切平面多个角度做无副作用 `prepare_build`，比较 `Collide`、`plannedPath` 和吸附后的首尾点；选中后先建自由带，再单独 prepare/commit `仓库→首带` 分拣器。后续每段最长 30 m，以前一段精确末端实体及 `endpointStateHash` 继续，提交后等所有预建筑变为实体并复读新的末端。带可以跨水，但玩家不应站在水面等长时间施工；一旦施工前沿为 Drift，退到 80 m 建造范围内的已验证陆地锚点。末端落在目标设备约 3 m 后，再独立验证 `末带→设备` 分拣器。
- 直接证据：红矩阵仓 `260` 周围 8 个起点方向中，朝目标的 `-90/-45/0` 等候选因现有红糖设备碰撞而被原生 validator 拒绝；`-135°` 候选合法并建成首带 `332…341`。分拣器 `342` 随后明确读回 `pick=260/insert=332` 且携带 1 个 item `6002`。路线以 `10+30+33+32+31+30+30=196` 条普通带分段延伸，最终末端为 `528`；末端分拣器 `529` 读回 `pick=528/insert=84` 且携带红矩阵。研究站 `84` 的红矩阵内部缓冲从 0 增至 `5900`、供电比例 1.0、`isWorking=true`，高分子化工 `1122` 的 hash 从 `0 -> 428`；仓 `260` 同时被正常线路搬空，证明不是玩家手工塞入。保存动作 `3d69b565-341d-491f-b0ee-2f5aba93abdc` 把该闭环写入同一主档 tick `5046241`，写入健康。
- 限制或反例：196 是本条弯曲路线的现场建材数，不是 153 m 球面距离的固定换算；网格方向、弯道、续接端重复点和碰撞都会改变段数。当前只验证一条跨水长带，尚未证明自动选路或最短路，故保持 `observed`。带上仍在途的红矩阵会让源仓先清空，验收必须看末端携货、目标缓冲和研究 hash，不能只看仓库差量。
- 复验触发：下一条跨区长带、自动候选/建材预算脚本、不同纬度网格、升级传送带、拆线或终端连接改变。
- 关联：EXP-011、EXP-020、EXP-032、EXP-037、EXP-042、EXP-051、EXP-053、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（红矩阵仓 `260` 到研究站 `84` 的 196 带、双分拣器和研究消耗闭环 live 成立）。

### EXP-055 — 核心建筑合法落位不代表分拣器可达，短带可以守恒中继

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：当前基础分拣器、普通传送带、储物仓与化工厂等核心建筑的近距离接料布局。
- 当前结论：核心建筑的 `prepare_build` 只证明自身碰撞体和地面合法；即使建筑中心相距约 7 m，后续分拣器仍可能由 DSP 原生校验返回 `TooFar`。建筑已经正常落成时，不应拆除、重放或放弃它；可在两端之间先扫描一条自由短带，建成后分别对“仓库→首带”和“末带→设备”做独立 prepare。两端都通过后才提交分拣器，并以实际携货、设备输入及输出增长验收。
- 直接证据：石墨仓 `114` 到化工厂 `552` 的直接分拣器准备返回 `BUILD_CONNECTION_INVALID/TooFar`。两者中点的两格普通带 `553…554` 合法建成，随后分拣器 `555` 明确连接 `114 -> 553`，分拣器 `556` 明确连接 `554 -> 552` 并现场携带 item `1109`。化工厂配置配方 `23` 后满功率工作，输出仓 `558` 的塑料从 0 增至 2、随后增至 38，证明短带不是孤立建筑而是连续原料中继。
- 限制或反例：当前只有一组储仓/化工厂样本，约 7 m 不是所有建筑姿态的统一阈值；端口方向、建筑碰撞体、纬度网格和分拣器等级都会改变可达性。中继带自身也必须在当前建造范围内逐段验收，不能把“中点看起来够近”当作连接证明。
- 复验触发：下一次核心建筑直连 `TooFar`、不同设备类型/分拣器等级、超过两格的中继、端口已占用或 DSP inserter 校验变化。
- 关联：EXP-011、EXP-020、EXP-042、EXP-045、EXP-054、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（石墨仓 `114` 经两格带和双分拣器向化工厂 `552` 连续供料，塑料输出闭环成立）。

### EXP-056 — 多产物设备会被任一满仓出口反压，先守恒腾位再判断上游故障

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前原油精炼配方 `16`、精炼油/氢共同产出、独立输出分拣器和有限容量储仓。
- 当前结论：共同产物设备即使原料、电力和另一条出口都正常，也会在任一输出无法继续排出时停机；下游缺少氢不一定是氢分拣器或带路断线。先复读设备两个输出、目标仓容量和分拣器拓扑；若某共同产物满仓，优先通过正常、守恒的转移腾出容量，再观察设备进度与另一产物下游是否恢复。被取出的物品应继续用于后续生产，不得丢弃或注入。
- 直接证据：精炼厂 `141` 满功率但停机时，精炼油目标仓 `163` 已达到 600，设备氢输出为 0，红矩阵生产站 `256` 因氢不足停产。通过正常 storage-to-player 转移守恒取出 200 精炼油后，精炼厂恢复工作，氢链和红矩阵生产重新推进；这 200 精炼油随后又经正常 player-to-storage 转移全部进入塑料输入仓 `557`，最终产出塑料，没有丢弃或直接写缓冲。
- 直接证据：高强度晶体 `1123` 后续再次停在约 `30240/108000`；两座研究站 `84/679` 都有 36000 蓝矩阵、红矩阵为 0，红矩阵站 `256` 只有 1 氢，完整下游端点仍存在。源端复读发现仓 `163` 再次以 580 精炼油+20 氢占满 600，精炼厂 `707` 积 39 油/0 氢并停机。一次取 400 油先被玩家库存容量 prepare 安全拒绝、无 commit；改成两批各 200，经玩家中转全部守恒进入原本空的仓 `286`，形成 400 纯油储备。20 秒后 `707` 满供电工作；再等约 35 秒让氢走完长带后，sorter `258` 实际携氢，站 `256` 的氢 `1 -> 5` 且恢复工作，科技由 `31320 -> 31860`。这构成同一因果链的第二次独立复现与恢复。
- 直接证据：同一科技推进到 `63360/108000` 后，仓 `163` 又以约 560 油+40 氢满载，两座精炼厂分别积约 39 油并停止，研究站仍只缺红矩阵。先把 200 油守恒转入已空的有机晶体输入仓 `761`，再用普通配方 `86` 手搓一座仓并通过原生地面校验建成纯油缓冲仓 `773`；三批各 200 油使 `773` 达到 600，最后一批 200 使既有纯油仓 `286` 由 400 达到 600。每批都由 fresh 玩家/源仓读取、一次幂等 commit 和双边读回完成。腾位期间两座精炼厂、红矩阵站 `256` 和研究站 `84` 重新工作，科技连续推进到 `86040`；油没有删除或注入，`761/286/773` 中的纯油仍保留给有机晶体等后续生产。
- 限制或反例：该结论只验证当前配方和储仓拓扑的反压原因；若腾位后进度仍不变，必须继续检查电力、原油输入、过滤项和分拣器携货，不能反复搬运库存。腾位恢复仍不是永久副产物消费方案；仓 `286` 只有 600 容量，后续应把其纯油用于有机晶体或建立经过空载过滤的连续下游。
- 复验触发：下一次精炼厂停机、其他共同产物配方、输出使用储液罐、仓容量未满但设备仍停机或连续三次同类诊断成立。
- 关联：EXP-024、EXP-025、EXP-028、EXP-029、EXP-033。
- 最近复验：2026-09-01（第三次反压恢复增加 600 容量的纯油仓 `773`，并把科技从 `63360` 连续推进到 `86040`）。

### EXP-057 — 贴住建筑基座时沿局部切平面背离障碍脱困

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：当前 `prepare_move/commit_move`、已由停滞看门狗确认无进展、玩家与单个可识别建筑基座相距约 1–3 m 的地表卡碰撞。
- 当前结论：同一目标连续被 180-tick 位移看门狗判停后，不应继续直线重试并消耗核心。先复读玩家与附近实体坐标，找出最近的实际碰撞体；把“障碍中心→玩家”的方向投影到当前位置的球面切平面，沿背离障碍方向取约 5 m 的短目标，重新投影到星球表面后只提交一次 fresh move。脱离基座并复读为 `Walk`、速度 `<=0.1 m/s` 后，再从未占用的陆地锚点绕行。
- 直接证据：玩家曾停在电塔 `117` 基座约 `0.8 m` 处，两次直达目标都被看门狗以无进展终止；沿上述局部切向外移约 5 m 后恢复 Walk，并经风机 `118/119/120` 继续移动。后来又在仓库 `163` 约 `2.16 m` 处、同时压住传送带 `166/167`，相同外移方法再次脱困；随后改走电塔 `182 -> 143 -> 133` 等锚点，最终稳定到达无线塔 `180` 并充满核心。第三次对照中，液罐 `165` 中心目标在剩余 `2.54 m` 时由动作 `627bcadd-901d-406b-8967-9f6ddcb63292` 以 181 tick 位移不足 `0.75 m` 明确终止；玩家当时几乎压在传送带 `178` 中心。未重放该目标，改用背离液罐的下一生产锚点后，动作 `684bc4b4-5b43-49c4-ab4b-e3901336e453` 首段成功并继续到风机 `713`。三组失败都是明确的 `action_failed`，没有隔离、结果未知或重复提交。
- 限制或反例：该方法只处理最近碰撞体清楚的局部卡脚，不是全局避障算法；多建筑夹缝、水面 Drift、悬崖或切向目标仍碰撞时必须停下重新扫描，不能增大到无界距离。约 5 m 是两次现场样本的保守值，仍需更多建筑类型复验。
- 复验触发：下一次单建筑基座卡停、不同半径星球、切向外移仍被判停、多障碍距离接近或第三次精确 5 m 切向脱困样本。
- 关联：EXP-035、EXP-036、EXP-039、EXP-051、EXP-053。
- 最近复验：2026-09-01（液罐 `165`/传送带 `178` 的第三组明确卡停及背离基座的后续锚点恢复）。

### EXP-058 — 混料仓的无过滤输出会污染专线，过滤应在进料前绑定或改走纯源旁路

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：混装储仓、普通分拣器、目标配方设备前的长传送带，以及 `sorter-filter` 对空载空闲分拣器的安全前置条件。
- 当前结论：下游设备只接受某种配方输入，不代表上游无过滤分拣器会替整条带筛料；从混料仓接出的传送带出口必须在接触物料前设置并复读过滤项，或直接从只含目标物品的仓/生产设备另建旁路。EXP-102 的现行部署已允许连接完整且当前零携货的 Returning/Picking 窗口，但带货时仍必须等待自然放货或守恒泄压；不能绕过 stale、带货强写或换键重放。
- 直接证据：分拣器 `551` 为无过滤的 `26 -> 542`，现场明确处于 `Inserting` 且携带 item `1102` 磁铁，证明它已把混料仓内容送入原计划的电路板带；研究站 `76` 因电路板断供停止。20 秒只读观察没有出现合格空闲窗口；增加回收仓 `562` 和分拣器 `563` 后，25 秒内虽捕获 7 个候选空窗，但所有过滤 prepare 都被 fresh readback 以 `STALE_STATE` 拒绝，没有提交任何过滤写入。回收仓随后持续接收磁铁，现场从至少 373 增至 381。
- 直接证据：独立旁路使用 `36 -> 572 -> 571…565 -> 573 -> 76` 输送电路板，并用纯铁块仓建立 `28 -> 594 -> 593…580 -> 595 -> 36` 的 20 带输入。两端分拣器均先单独 prepare 通过后才提交；`594/595` 实际携带 item `1101`，组装机 `36` 输出 item `1301`，研究站 `76` 的电路板输入为 6 且持续工作。高分子化工 `1122` 在 15 秒内从 `35643 -> 36180/72000`，随后继续到 `37800`，证明研究恢复而非一次性手塞。
- 直接证据：现行部署先以动作 `6c65e8d4-d7cc-45f8-bf85-effbe75f7c87` 在空载 Returning 窗口把 `551` 临时过滤为源仓中不存在的蓝矩阵以停止继续污染，确认回收端不再增长后，再以动作 `1ca6cf72-4724-4d8b-a07a-3ab92c3ed4f0` 把它永久收紧为铜块 `1104`。随后动作 `09a2176a-59a2-435b-ba81-393d332732ac`、`ee7e7f42-1d6c-4e9a-a399-2d9422e509de`、`51f1f74a-bfb5-4964-b620-0a088fbc62a0` 守恒腾出仓 `26` 的两个铜槽、从回收仓 `562` 取 200 电路板并放入 `26`；目标设备 sorter `868` 只取实际需要的电路板，仓 `26` 为 `200 -> 198`、站 `76` 恢复满供电工作，科研站 `84` 随后出现蓝矩阵缓冲并与红矩阵共同推进 `1703`。传送带出口必须过滤，与设备到设备目标需求选择是两个不同边界。
- 限制或反例：当前旁路仍借用混料仓侧的既有铜块输入，长期还需观察仓 `26` 原料耗尽/背压；回收仓只保存污染物，不自动把它们分类送回生产。恢复后的最新复读又发现输出分拣器 `572` 的取料端为空，制造台 `36` 的连接表也不再包含 `572`，因此这条旁路的历史成功不能继续当作当前运行证据，详见 EXP-067。EXP-102 只扩展“已连接且当前零携货”的过滤窗口，不推翻“尽量在通料前配置”的布局原则，也不授权删除现有实体。
- 复验触发：下一条混料仓输出、首次在连接前成功设置过滤、活跃分拣器出现稳定空窗、回收仓满载、旁路铜料耗尽或研究再次停止。
- 关联：EXP-011、EXP-012、EXP-028、EXP-044、EXP-049、EXP-054。
- 最近复验：2026-09-02（新过滤窗口 live 收紧 `551` 为铜；守恒搬运 200 电路板后蓝糖站和科研流量恢复）。

### EXP-059 — 扩研究消费者后必须重测原料斜率，交替工作通常是供料而非研究站数量瓶颈

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：矩阵研究站、独立矩阵支线、同一科技的多研究站消费，以及按固定时间窗口比较 `hashUploaded` 的现场诊断。
- 当前结论：新增研究站只能扩大消费上限，不能替代矩阵生产与末端输送。新站落成后应同时复读各站 `isWorking`、各矩阵缓存、源生产站成品缓存、输出分拣器携货，并在同一科技内测量上传斜率；若研究站交替工作或某种矩阵反复归零，优先补该矩阵的生产/出口，不能继续盲目堆研究站。已有末端带不能直接分叉时，可以用“活带经分拣器送入独立短带”，并从纯生产站另建第二输出带补充吞吐；每个端点仍要独立 prepare。
- 直接证据：研究站 `679` 在合法空地建成并配置研究模式，蓝支线 `313 -> 692 -> 683…684 -> 693 -> 679`、红支线 `528 -> 694 -> 691…687 -> 695 -> 679` 均完整回读。两站都收到矩阵后，20 秒窗口内科技只增加 448 hash，站 `84/679` 交替工作；蓝站 `76` 同时留有成品缓存。随后新增 `76 -> 706 -> 702…705 -> 683` 的十带第二出口，站 `76` 的成品从 7 降至 3，证明出口吞吐确实增加；但两个研究站随后同时出现蓝缓存 36000、红缓存 0，最终把瓶颈定位到氢/红矩阵而非蓝矩阵或研究站数量。
- 限制或反例：当前只有一套双研究站现场；短窗口可能跨配方周期或科技切换，不能只取一个瞬时 `isWorking`。比较斜率必须绑定同一 `techId`，跨科技窗口应废弃重测。独立支线必须保持单品纯源，本条不允许把混料带直接分叉。
- 复验触发：下一次增加研究站、矩阵生产站或输出分拣器，科技切换窗口，研究站缓存充足但仍不工作，或多站同时连续工作的独立样本。
- 关联：EXP-042、EXP-049、EXP-054、EXP-058。
- 最近复验：2026-09-01（第二研究站、蓝红独立支线、蓝站第二出口和红矩阵归零对照）。

### EXP-060 — 双产物设备扩容要先过滤空出口、再通原料，并把端点供电与下游电网分开验收

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：原油精炼配方 `16`、多个输出分拣器、共享产物主带/储仓及相互独立的供电网络。
- 当前结论：扩建双产物设备时，先配置空设备，再在尚未进料时依次建造输出分拣器并设置精确过滤，最后才连接输入；这样可在分拣器空载稳定时绑定过滤，避免氢和精炼油串线。设备本体有电不证明所有分拣器在覆盖内，输出分拣器和更远的下游生产网必须分别读回供电。产物到达主带后，还要以最终消费者的输入增长和整网 `required/served/capacity` 验收。
- 直接证据：第二精炼厂 `707` 配方 `16` 先保持无原料；输出 `708` 连接氢带 `170` 并过滤 item `1120`，输出 `709` 连接油仓 `163` 并过滤 item `1114`，最后输入 `710` 才连接原油末带 `161` 且实际携油。厂 `707` 满功率工作并一度积 4 氢，但 `708` 的供电读数为空；电塔 `711` 建成后，`708` 供电比 1.0 且实际携带 1 氢。下游网络 2 当时只有 `20000/29428`、消费者比约 0.68；风机 `712/713` 把容量依次提高到 25000、30000，最终 `29428/29428`、比率 1.0，红矩阵站 `256` 满功率且氢输入达到 6。
- 直接证据：早先的输送旁路 `163 -> 675 -> 596…674 -> 676 -> 557` 由电塔 `677` 补齐末端供电，曾让塑料输入仓从 0 增至 41；但后续复读推翻了“该旁路是纯油永久线”的判断。源仓 `163` 再次同时包含精炼油 459 与氢 13，无过滤源分拣器 `675` 已把输入仓 `557` 污染为精炼油 33、氢 271。旧热电分拣器 `184` 无取料目标，替换连接 `163 -> 678 -> 183` 后网络 3 恢复 `51000` 容量和满供电；这部分供电证据仍成立。
- 限制或反例：当前只验证精炼配方 `16`；其他多产物设备的输出选择规则仍要按运行时配方与实际携货复验。过滤配置要求分拣器已连接、空载且空闲；若已通料后持续 stale，不得强写。任何从混料仓出发的无过滤线，即使短期实际携带目标物，也不能称为专线。电塔 `powerNetworkId` 对 power-node 快照可能为空，应以消费者供电比和全网摘要确认接网。
- 复验触发：第三座精炼厂、X 射线裂解、输出换成储液罐、共享主带背压、过滤器在通料后修改或任一网络再次欠供。
- 关联：EXP-019、EXP-024、EXP-025、EXP-028、EXP-033、EXP-056。
- 最近复验：2026-09-01（第二精炼厂与两级供电仍成立；所谓永久纯油旁路被 `557` 的 271 氢反证并由 EXP-065 取代）。

### EXP-061 — 多基座夹缝脱困使用有界四向短探测，不把单障碍背离法无限外推

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：180-tick 看门狗已确认零/低位移、附近同时存在两个以上建筑或带体、单一障碍背离方向仍失败的地表 Walk。
- 当前结论：EXP-057 的单障碍切向背离在多基座夹缝里可能把玩家推向另一个碰撞体。先复读半径约 5–7 m 内实体；若多个大碰撞体方向接近，最多枚举当前位置切平面的四个正交 4 m 短目标，每个方向只提交一次 fresh move，首个成功立即停止。全部失败就重新扫描；若按用户规则走正常保存/重启恢复，必须把它理解为清除进程和旧订单的恢复边界，不能假定读档会移动玩家或消除已保存的几何夹缝。恢复后只可基于 fresh 几何再做新的有界探测，不能照抄上一轮四向重放。脱困后先等 `Walk` 且速度 `<=0.1 m/s`，再走显式侧向 waypoint 绕开已识别障碍。
- 直接证据：玩家先贴住电塔 `39` 约 0.8 m，按 EXP-057 背离 5 m 成功；随后落在仓 `287` 与制造台 `285` 夹缝，单仓背离和三障碍合成排斥方向都被看门狗以零位移终止。四向 4 m 探测中 east 失败、west 动作 `d3794796-d0e1-4c7a-a537-d5b31990e89e` 成功，之后正常到达风机 `82`。另一处玩家压住原油带 `151`、前方有油井 `129`；east 再次失败，west 动作 `393942fc-5736-4a55-afc5-431d4c29b021` 成功，随后经侧向 waypoint 绕过油井并到达塔 `182`。
- 直接证据：钛晶石预建后，玩家处在电塔 `133`（约 2.12 m）、新输入仓 `768`（约 1.76 m）和 sorter `770/771`（约 2.8 m）之间；前往风机 `82` 的长 Move `a7e44dbd-4e13-40de-8608-bbf453a280cb` 几乎没有位移并被看门狗终止，剩余约 39.42 m。fresh 几何读回后，把多个近端实体的合成排斥方向投影到局部切平面，仅提交第一条 4 m 候选；动作 `8707b409-12b8-4400-83dc-044e48eaf94b` 即成功到达 `(-75.90817,-100.361412,-155.7321)`，终态 Walk、速度 0，核心仍为 `399.97/400 MJ`。本例证明四向上限不要求固定枚举完四条；几何给出的首个自由候选一旦成功就立即停止。
- 直接证据：结构矩阵研究等待期间，玩家又先后被仓 `286`/制造台 `285`/仓 `287` 的外围碰撞面和带 `689/690`/矿机 `3` 卡停。第一处根据三座大基座计算的 4 m 合成切向动作 `5bd0b7ae-6259-4252-9dc7-8b6c62acaa12` 一次完成，并把三者中心距同时扩大；第二处的首条合成排斥方向动作 `ae6cb304-d9bc-42a3-9459-ab6e10a9f9b3` 被 181-tick 看门狗明确终止，正交且背离矿机的第二候选动作 `4885c756-6f9e-415b-89e4-69b10dc1f8e0` 才一次脱困。两组对照表明“合成排斥”不是必然首选成功方向，仍须保留至多四向、每向一次且首个成功即停的边界。
- 直接证据：2026-09-02 从红糖区返往无线塔时，长动作 `5b562e70-9b17-4f5e-99e8-e5a0f2aca953` 在电塔 `39`、研究站 `84`、制造台 `285` 的新夹缝中剩余 `29.90 m` 时被 180-tick 看门狗终止，核心仍约 `254.5/400 MJ`。按 fresh 实体中心距枚举的首个 6 m 正交自由候选由动作 `896bce4a-cba3-4a70-b293-ffbe962854a4` 一次脱困；后续保守障碍圆路线走过多个 waypoint 后又在剩余 `2.53 m` 被动作 `3286c97c-7070-47da-855a-f7bf4c9fbe3c` 明确终止，证明“网格看起来有空隙”仍不能替代原生碰撞终态。没有重放；动作 `2e155568-c1d2-4c15-bc4a-dd34699116fd` 再沿另一侧 4 m 候选离开，随后优先完成范围内蓝糖供料修复并用守恒燃料保持余量。
- 直接证据：后续为钢材线补上游铁矿时，范围内 harvest 先在电塔 `39` 约 `0.80 m` 处停滞；返回已验证落点后，侧向长 waypoint 又把玩家带到仓 `286`/制造台 `285`/仓 `287` 夹缝。合成排斥、两条正交滑移和单仓背离四个 fresh 短动作分别由 `5f524835-d3d4-4238-93e7-124ac4680167`、`0ba69040-a5d0-478c-8019-1eae23bafc58`、`7b73545b-e5e7-4243-8d8e-1100128ab149`、`0b229fd5-af68-4104-9b4a-54f66b258ef8` 明确判停，没有继续随机探路。正常保存到 tick `7027343` 并按 protected ticket 恢复后，玩家仍在约 `(-81.28,-53.32,-175.04)`，证明重启不改变已保存碰撞位置；该边界由 EXP-081 单独固化。
- 直接证据：物流科技等待期从粒子容器区前往锚点 `750` 时，动作 `67dd8bc7-36d9-4f4f-8cea-0874dbfa49bd` 已走完大部路径，却在仓 `827` 约 1.76 m、制造台 `814` 约 2.98 m 的新夹缝中以剩余 `5.75 m` 被 180-tick 看门狗明确终止；核心仍满电、write health healthy。fresh 非带实体几何选出的 6 m 自由候选由动作 `2297a338-be30-4ac8-b92d-ccb6145ec38a` 一次离开，随后正常到达风机 `130`。在 `768/767/133/143/129` 的另一密集环中，长动作 `526b2bf9-5e08-4849-b3e1-6619286bc5a2` 与 `2e5f80bc-8118-43d3-baf3-10ee25dfc517` 又分别在剩余约 `18.10 m` 时停止；两次都没有重放或隔离，证明确认“路上被卡”应依赖物理进展窗口，而不是等到耗电或全局超时。
- 限制或反例：四向探测只用于已明确终止的短距离 Walk，不适用于水面 Drift、悬崖、飞行或能量不足；方向成功只证明离开当前碰撞，不证明通往最终目标。每次失败都是新的已知终态动作，仍要保留能量余量并禁止相同方向重放。
- 复验触发：下一次多基座夹缝、四方向全部失败、不同建筑半径、短探测成功后速度未归零或可从实体几何直接算出唯一自由扇区。
- 关联：EXP-035、EXP-036、EXP-051、EXP-053、EXP-057。
- 最近复验：2026-09-03（旧 `141 -> 183` 直线在玩家距炼油厂 `141` 中心约 2.10 m 时被 181-tick 看门狗以剩余 12.59 m 终止；沿已走通轨迹退回 Walk 点后，一个约 6 m 侧绕点和随后的 `183` 外缘目标均一次成功，未重放失败订单）。

### EXP-062 — 锁定配方的预建产线只在科技解锁后激活，里程碑以自动产出、日记和普通保存三重验收

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：科技尚未解锁但建筑、输入库存、端点和电力可先行准备的普通生产线，以及用户要求的逐产物提交节奏。
- 当前结论：允许在配方锁定时先建空闲设备和物流，但不得配置锁定配方或宣称产物完成。科技解锁后 fresh inspect 设备，正常配置配方，再以输入仓减少、输入分拣器实际携货、生产设备配方/供电、输出分拣器工作、专用输出仓增长共同证明自动产出；随后核对逐存档 `production_line_item_first`，普通保存同一主档，最后才提交并推送代码/经验。
- 直接证据：钛矿冶炼 `1413` 在 tick `5692950` 解锁后，空闲熔炉 `530` 才配置配方 `65`。15 秒后输入仓 `259` 的钛矿 `1000 -> 986`，分拣器 `532` 实际携 item `1004`，熔炉满功率，输出分拣器 `533` 工作，仓 `531` 出现 6 个 item `1106` 钛块。逐存档日记序号 8 记录 `production_line_item_first`，实际时间 `2026-09-01T08:21:33.164002+08:00`、tick `5702401`、本局时间 `001d 02:24:00`；保存动作 `996eb4ad-92fe-4c81-b42f-3da74ed52e85` 随后把同一主档保存到 tick `5705293`，revision `590 -> 591`、写入健康。
- 直接证据：第二个独立样本金刚石线在晶体冶炼 `1403` 未解锁时只预建空仓 `716/717`、空熔炉 `715` 和 sorter `720/719`，并提前把 400 高能石墨守恒放入输入仓，熔炉仍保持 recipe `0`。科技在 tick `6081424` 正常解锁后，动作 `fd487817-9943-4249-8485-18b9c612a3bc` 才配置运行时配方 `60`。输入仓 `400 -> 354 -> 350`，输入 sorter 曾实际持有 item `1109`，熔炉连续工作且供电比 1.0，输出仓 `0 -> 42 -> 47`。日记序号 9 在 tick `6083748` 记录首个自动 item `1112` 金刚石（实际时间 `2026-09-01T12:29:17.0019946+08:00`、本局时间 `001d 04:09:55`）；保存动作 `2e9ca24b-57dc-40c6-900b-a210b8fc03e7` 随后持久化 tick `6090507`，revision `18 -> 19`、写入健康。
- 直接证据：第三个独立样本为钛晶石线。高强度晶体 `1123` 未完成时只预建制造台 `767`、输入仓 `768`、输出仓 `769` 和 sorter `770–772`；两个输入 sorter 在空仓时分别过滤有机晶体 `1117` 与钛块 `1106`，随后才守恒装入 40/120，制造台始终保持 recipe `0`。科技在 tick `6509179` 完成且运行时配方 `26` 明确 unlocked 后，动作 `74b451af-ed4b-47df-b0cf-8bd5b0b5a933` 才配置生产。输入仓降至 29/88，制造台满供电工作，输出仓出现 8 个 item `1118`；日记序号 16 在 tick `6511499` 记录首个自动钛晶石（实际时间 `2026-09-01T14:28:07.1667461+08:00`、本局 `001d 06:08:44`）。结构矩阵科技 `1124` 随后由正常队列选择并写入日记序号 17；保存动作 `39cee465-5520-4a8c-a1e3-68ac8e6208ab` 持久化 tick `6518917`，revision `270 -> 271`、写入健康。
- 直接证据：第四个独立样本为粒子容器线。科技 `1703` 在 tick `8696391` 正常完成后，动作 `d2209d94-80bd-4be8-a753-452534fe4c5a` 才给预建制造台 `883` 配置 recipe `99`。输入仓 `884` 中电磁涡轮、铜块、石墨烯各下降 `80/80/40 -> 70/70/30`，三个 exact-filter sorter 均保持 `884 -> 883`，制造台满供电工作，输出仓 `885` 出现 2 个 item `1206`。日记序号 38 在 tick `8698306` 持久化首个自动粒子容器（实际时间 `2026-09-02T12:34:53.509099+08:00`、本局 `001d 16:16:11`），`persistencePending=false` 且无错误；保存动作 `0252c2a1-4618-43cc-bd1a-8fa6d0ca105c` 随后持久化 tick `8699182`、revision `52`、写入健康。
- 直接证据：第五个独立样本为加力推进器线。星际物流前置已解锁后，完全空载的旧基础推进器制造台 `876` 由动作 `d779eebf-31d4-463a-ad35-ff0426b6c39c` 正常改为 recipe `21`；两只零携货输入 sorter `880/881` 分别由动作 `7742d3de-b8ab-4818-b691-e5b6bf70f9bf`、`c9b16da4-62dc-4d94-8c55-fd8e759e8bb8` 过滤为钛合金/电磁涡轮。20+20 原料经四次双边守恒 transfer 进入仓 `877`，随后仓、制造台输入均归零，满供电输出 sorter `879` 把 4 个 item `1406` 送入仓 `878`。journal sequence `46` 在 tick `11827563`（实际 `2026-09-03T08:29:09.1992358+08:00`、本局 `002d 06:45:26`）durable，保存动作 `189abb87-27e2-4ba8-82a3-cff55ad5eb32` 持久化 tick `11830296`、healthy。
- 直接证据：第六个独立样本为处理器线的精确批量复产。动作 `904e954f-f641-4021-afc6-55b1b7cf10a8` 从仍在自动补货的混合仓 `26` 取得 200 个电路板，动作自身精确报告玩家 `1 -> 201`、仓 `243 -> 43`；动作 `c6bdd644-7d83-4ea1-9d2d-5cee5992fa0d` 随后把 200 个电路板完整交给仓 `849`。制造台 `853(recipe 51)` 满电自动消耗 200 电路板和 200 微晶元件，输出仓 `854` 达到精确 100 个处理器；仓 `849` 与制造台最终只剩合计 20 个微晶元件。日记没有新增事件，因为同一生产线的首次自动处理器已由 sequence `29` 在 tick `7704459` 持久化；普通保存动作 `199f3d02-01a1-4fd4-937b-7411ef06cb94` 固化 tick `11863820`、healthy。
- 直接证据：第七个独立样本为星际物流运输站线。科技 `1605` 与 recipe `95` 已正式解锁、制造台及 sorter 全空载后，制造台 `898` 和两条旧输入过滤才改为 recipe `95`、PLS `2103` 与钛合金 `1107`，粒子容器 `1206` 过滤保持。2 座 PLS、80 钛合金、40 粒子容器经六次双端守恒 transfer 进入仓 `899`，最终仓和制造台三项输入均归零，输出仓 `900` 得到精确 2 座 item `2104`；journal sequence `48` 在 tick `11921722` durable，普通保存动作 `1766766a-c0c0-44d2-b9bc-8d0f87bcda48` 固化 tick `11926992`、healthy。
- 限制或反例：预建端点可能在等待科技期间被其他线路占用，激活前必须重新 inspect；输出设备内部缓存不等于专用仓积累，手工产物也不能替代生产线事件。同一物品再次自动生产不会产生第二条 first journal，必须用配方投入、设备状态、专仓产量和普通保存验收。若日记是中途挂接旧档，历史覆盖仍明确为不完整，但挂接后的新事件可作为前瞻证据。
- 复验触发：下一条预建配方线、首次有机晶体/金刚石/钛晶石/结构矩阵产线、科技解锁期间端点变化、同一物品批量复产或日记事件缺失。
- 关联：EXP-015、EXP-037、EXP-048、EXP-055。
- 最近复验：2026-09-03（空载 PLS 单元在 recipe `95` 解锁后安全重配，完整三料预算自动转为精确 2 座 ILS；sequence `48` durable、正常保存 tick `11926992`、审计 revision `576`）。

### EXP-063 — 进度哈希包含实时上传量，活跃研究时追加队列会安全 stale

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：当前 `get_progression_state`、`prepare_select_research` 与仍在上传 hash 的研究站。
- 当前结论：当前 `CanonicalStateHash.Progression` 包含每项科技的 `HashUploaded`；研究活跃时，读取与 prepare 跨过一个上传 tick 就会返回 `STALE_STATE`。这不是隔离，也不得通过移除全部并发校验、猜测重放或强停生产来绕过。短期只在研究自然停顿/队列切换的稳定窗口追加，或等待已排队科技结束；长期应为选科技动作引入只绑定队列、解锁和前置条件的稳定专用哈希，并补测试后随正常重启部署。
- 直接证据：高分子化工完成后，队首已切换为钛矿冶炼 `1413`，队列为 `[1413,1403,1701]`；fresh progression 后追加高强度晶体 `1123` 的 prepare 仍返回 `Technology queue or state changed after inspection`，随后复读 `1413` 已从 0 推进到 3679，队列未被修改、写入健康。源码复核确认 `CanonicalStateHash.Progression` 对每项科技串入 `HashUploaded`，prepare 和 commit 又要求完整哈希相等。
- 直接证据：晶体冶炼 `1403` 与电磁驱动 `1701` 完成后，progression 处于 `currentTechId=0`、空队列的稳定窗口。fresh read 后动作 `acaa5327-5e2b-4139-b0cc-49c0b18c5d40` 正常把高强度晶体 `1123` 设为当前唯一科技，没有弱化哈希或停止矩阵生产；逐存档日记序号 10 同时记录首次选择 tick `6102006`、实际时间 `2026-09-01T12:34:21.3647316+08:00`、本局时间 `001d 04:15:00`。这与活跃上传时的 stale 形成正反对照，验证了当前短期规程。
- 限制或反例：若科研正好停料，现实现仍可能成功；本条不证明任何弱化并发绑定都安全。专用哈希必须继续绑定 session、planet、当前科技、队列、目标解锁状态和前置条件，且 commit 仍调用 `CanEnqueueTech`。
- 复验触发：科研稳定停顿时追加成功、实现队列专用哈希并部署、科技自然切换、目标科技前置变化或队列容量变化。
- 关联：EXP-008、EXP-037、EXP-048、EXP-059。
- 最近复验：2026-09-01（活跃上传时安全 stale；空队列稳定窗口 fresh 选择 `1123` 正常完成并写入日记）。

### EXP-064 — 主菜单恢复票据可见性与最新交接 tick 必须分层验证

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：同一 Windows 用户、正常关闭后的 fixed LastExit、已受当前用户 ACL 保护但位于工具层 runtime 的旧 owned-save 身份票据，以及主菜单 `restartResumeAvailable` 可观察性缺口。
- 当前结论：主菜单未显示 `restartResumeAvailable` 不等于固定身份票据不可用；若工具层 `%LOCALAPPDATA%` 文件对 DSP 进程不可见，只能把同一未解密/未改写的票据复制到 Plugin 已保护的 `runtime-handoff` 固定目录，并在文件层再次关闭继承、确认没有其他 SID 规则。票据只提供高熵主档身份下限，不能单独证明最新交接进度；恢复动作完成后，在任何新游戏写入前还必须同时验证 owned session、目标星球、和平/非沙盒/1x、健康写入、`gameTick >=` 最新普通保存 tick，以及逐存档日记的最新已知事件。任一不符都停止，不能回退到旧飞行检查点或继续写入。
- 直接证据：新 Plugin 主菜单显示 `owned-game.resume` capability 但 `restartResumeAvailable=false`；工具层 runtime 票据 prepare 返回“票据不存在”。同一票据未经内容修改复制到已保护 handoff 目录后，prepare/commit 动作 `633c24f3-c2d8-4b64-b798-ee2d1edebf41` 通过 fixed LastExit 原生加载与高熵主档身份验证。新 session `4b666389-d33d-46f2-ad2c-2d6fdd8fee6d` 随后读到 planet `104`、和平、非沙盒、1x、写入健康、自动重存 tick `5751758`；该 tick 高于交接保存 `5731056`，日记序号 8 仍是 tick `5702401` 的自动钛块首产，排除了本次现场的回档。
- 限制或反例：这次票据的内部 minimum tick 早于最新交接点，所以其自身不足以防止同主档旧版本回滚；成功依赖“刚完成的唯一正常关闭、期间未载入其他世界”和恢复后的额外最新 tick/日记硬门槛。长期应实现由健康 owned session 在关机前直接签发最新 minimum tick 的 planned-shutdown handoff，不能把任意过期、不同主档或来源不明票据复制进固定目录。票据/token/原始主档名均不得写入文档或日志。
- 复验触发：planned-shutdown handoff 实现、主菜单恢复字段修复、票据路径/ACL 变化、不同 Windows 用户、LastExit 被其他世界覆盖、最新 tick 或日记门槛失败。
- 关联：EXP-004、EXP-005、EXP-006、EXP-047、EXP-048。
- 最近复验：2026-09-01（新 DLL 启动、受保护 handoff、原生恢复、最新 tick 与日记双重验收）。

### EXP-065 — 双产物混料仓下游必须先纯化，短期目标流量不等于永久专线

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：同时可能含精炼油 `1114` 与氢 `1120` 的普通储仓、无过滤分拣器和下游化工原料仓。
- 当前结论：从双产物混料仓接出的无过滤分拣器必然保留未来串料风险；即使初次观察只运输精炼油，也不能把它验收为永久油路。修复应先停止继续进料，用普通 transfer 守恒移出错误物品，再选择以下之一：从纯物料仓供料；或在源分拣器尚空载稳定时绑定目标过滤并复读；或把双产物在进入共享仓前分别过滤。不得依赖下游配方自动拒料来替整条带筛选。
- 直接证据：恢复后的源仓 `163` 含精炼油 459 与氢 13；无过滤路线 `163 -> 675 -> 596…674 -> 676 -> 557` 已使目标油仓 `557` 变成精炼油 33、氢 271。塑料仓 `558` 虽已积累 993 塑料，但化工厂 `552` 当前停机，因此先前的短期油流量不能证明长期隔离。该反例与 EXP-058 的磁铁污染电路板线独立一致。
- 直接证据：结构矩阵研究期间按本条完成了正向修复。先把仓 `557` 的 271 氢全部守恒取回，使其只剩精炼油；随后又临时取空全部 233 油，才新建并空载配置 `557 -> 783(filter 1114) -> 552`，再装入 633 纯油。化工厂立即以配方 `23`、满供电恢复工作，塑料仓 `558` 从 793 增至 800，随后在长带补油时增至 944。源端没有重用混料仓 `163` 的无过滤出口，而是新建空纯油仓 `784`、五格带中继 `789…785 -> 596` 和空载过滤 sorter `790`；500 油经 normal transfer 进入 `784` 后，`790` 只携 item `1114`，长带末端继续进入已纯化的 `557`。这把“清污染、空仓过滤、纯源接带”从规则变成了现场闭环。
- 直接证据：后续自动补油改造再次触发本条，说明“一次纯化”不能永久替代输入边界。fresh 逐槽聚合发现仓 `163` 有 12 氢，名义纯油中继 `784` 也已有 55 氢；其根因边界是 `163 -> sorter 906(no filter) -> 784`，而 `784 -> sorter 790(filter 1114)` 只保护旧塑料线，没有保护仓本身。两次正常 transfer 将 `12 + 55` 氢完整移到玩家隔离；此后审计中 `163/784` 均为精炼油 600，且 `163` 的唯一输入 sorter `709` 已过滤 item `1114`。新油出口 sorter `2218` 在同一快照满电携 item `1114`；这证明当前源链已纯化。后续第十写又把隔离的 67 氢完整转入专用氢仓 `136`，仓由 7 增至 74、玩家氢归零，未把污染物遗留或计入里程碑物料。
- 限制或反例：本条只证明普通混料仓的无过滤输出不安全；若上游容器由结构保证永远单品，或已在空载分拣器上复读精确 filter，则不需要额外重复过滤。清理动作仍必须受距离、容量、双边计数和 player hash 约束。
- 复验触发：`557` 清理完成、`675` 成功空载过滤、改用纯油仓、任一共享副产物仓新增下游或塑料线再次停机。
- 关联：EXP-021、EXP-028、EXP-056、EXP-058、EXP-060。
- 最近复验：2026-09-03（发现后来补建的无过滤 sorter `906` 已让 `784` 再次混入 55 氢；守恒清除 `163/784` 共 67 氢并复读两仓各 600 纯油，随后全部归入专用氢仓。旧“永久纯仓”表述被收紧为逐输入边界验证）。

### EXP-066 — 已验证陆地锚点不证明中间弧段可通行，首次 Drift 即停路线并返回精确落点

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：planet `104` 的长距离步行、已验证建筑锚点、球面 slerp 分段和无线塔回充。
- 当前结论：“起点和终点都曾以 Walk 站稳”只证明两个点，不证明其球面短弧全程是陆地。每段都要以终态 Walk/速度复读作为是否继续的门槛；首次出现 Drift 就终止整条路线，不等待核心能量耗尽。对距离不超过单段上限的已知 Walk 终点，优先一次直达并验收最终陆地，不要自动生成未经验证的几何中点；这不证明直达路径全程是陆地，仍只在终态 Walk 时继续。能量保底继续使用 50%；返回时用距已知 Walk 落点数米的短目标和更严到达容差，Drift 中只有界重试未提交的 `STALE_STATE`。恢复 Walk 后优先按已验证的连续锚点路由到无线塔充电，再继续生产。
- 直接证据：从风机 `713` 向风机 `82` 的球面分段动作 `7749799c-44d6-41b6-9741-680db8978e88` 在首段后进入 Drift，20 秒复读速度仍约 `0.188 m/s`且核心能量继续下降；动作 `ee2cead1-51a2-4374-91b1-ccedd1fe35d9` 以 fresh 原子绑定返回风机 `713` 附近 Walk。之后 `713 -> 165 -> 182 -> 143 -> 133` 的连续陆地锚点成功，但 `133 -> 82` 的几何插值再次跨水；改用动作 `caaef048-056f-4f7d-b83b-464498f2a312` 和 `bbaa36fb-5cf0-4e1f-a1aa-6bdf2adbca4e` 逐步贴近已知落点后恢复 Walk，再经风机 `82` 到无线塔 `180` 的三段路线站稳。25 秒后核心能量恢复至约 `399.4/400 MJ`。
- 直接证据：路由脚本随后加入可配置 50% 核心能量保底和 Drift 首帧立即停止；从石脉 `188` 返回无线塔 `180` 的单段及 `180 -> 82` 的反向三段已在新门槛下完成，全部终态均为 Walk/速度 0，核心仍为约 `397/400 MJ`。这只验证护栏不阻断已知安全路线，尚未为了测试而主动进入 Drift。
- 直接证据：从风机 `82` 到电塔 `133` 的直线距离约 `41.4 m`；先前的球面分段在这两个端点间产生了水面中点，本次动作 `ae98beb3-e49f-4d71-be7b-3f7ffa6718e8` 则以单一已知陆地终点直达，在距目标约 `2.00 m` 处读回 Walk/速度 0，核心仍约 `392.2/400 MJ`。该对照支持“不额外插入未验证中点”，不支持无条件放大单段距离。
- 直接证据：返往钻石预建区时，反向锚点 `133 -> 143 -> 182 -> 165 外缘 -> 713 -> 120` 由五个独立动作 `1948b8e0-f450-4c3d-a3c7-395886c76218`、`5320ed47-4ad5-4258-ba11-23efe7bc821a`、`ae9c6488-4daa-4622-b443-419bb850712a`、`8849b027-d330-4e81-9e0b-e7189c216643`、`1fd9201e-0f6d-4f60-ba2b-f47c47aac0d9` 完成。每段均终态 Walk/速度 0，核心 `399.5 -> 392.2 MJ`；液罐 `165` 使用 5 m 容差，停在 4.54 m 外，避免再次压进基座/带体。由此这条连续锚点现在具有双向现场证据，但仍不能外推到任意中间点。
- 直接证据：从风机 `35` 外侧到电塔 `143` 的 54.4 m 两段 slerp 在第一中点即进入 Drift；路线没有执行第二段，并通过只重试未提交 `prepare/commit STALE_STATE` 的 bounded loop 返回原 Walk 落点，动作 `6e2a6e1c-d574-43f4-b8db-ec4cb54734863c` 完成后为 Walk/速度 0。随后为绕开仓 `286` 选的南侧 17.6 m 候选也在首段进入 Drift；同样立即终止并以动作 `55d6de44-7cfa-4f1b-ad24-ffe7cc9a31dc` 回到风机 `35` 外侧 Walk，核心仍约 339.5/400 MJ。两次独立反例把该处南缘收敛为水岸，不再重复直线或南绕；后台生产/科研未被隔离。
- 直接证据：后续携油前往红糖区时再次从风机 `82` 分别以 10 m 上限朝 `133`、`143` 分段，两次都在第一段进入 Drift；每次立即停止，并仅对明确返回 `STALE_STATE`、尚未接受的 commit 做有界重读，最终分别在第 3 次取得唯一 accepted return，恢复到 `82` 附近 Walk。随后 `82 -> 133` 的 39.5 m 单段动作 `8d0e1a47-3cf1-46e0-85c8-06ef083e8c73` 和返程 `143 -> 82` 的 41.3 m 单段动作 `f692839e-523b-4d74-a8c6-88f6d61349dd` 都成功。这是同一区域第三、第四个“机械插入中点反而入水，已知端点单段可走”的对照，进一步收紧路线生成器的适用范围。
- 直接证据：研究等待时复用旧 `82 -> 无线塔 180` 三段路线，第一段动作 `83398f2d-68bb-42d3-bb23-f6d0d442a32e` 虽正常到达，却把玩家留在后来已密集铺设的带 `689/690` 与矿机 `3` 一侧；第二段动作 `52c0e3fd-4e92-4004-95dc-ab0a32f30e05` 随即以剩余 14.51 m 的零进展明确终止。局部脱困后写健康仍为 healthy。由此旧锚点序列也只是在当时工厂拓扑上的证据；新建建筑或传送带跨过其走廊后，必须按 fresh 邻近实体重新验证，不能因历史成功而永久白名单。
- 直接证据：从返航落点约 `(159.79,12.38,-121.13)` 朝煤脉 `363` 机械拆成五段时，第一段落回 Walk，第二个约 17 m 的几何中点却进入 Drift；脚本立即断路，并只回到第一段精确落点。随后从同一落点把煤脉 `363` 当作单一已知陆地终点直接提交，约 68 m 后正常读回 Walk/速度 0，核心仅约 `293.1 -> 285.8 MJ`；再以石脉 `303` 和铁脉 `6` 为陆地端点直达，最终在铁区 Walk 站稳。该新对照再次证明“首次 Drift 停分段”与“直达已知陆地端点”并不矛盾：禁止的是把未经证明的水面中点当停靠点，不是禁止有看门狗保护的跨水直达。
- 直接证据：2026-09-02 从风机 `82` 朝红矩阵仓 `260` 的首个 20 m 几何中点再次进入 Drift，脚本立即停止；动作 `ce6360c0-7ef8-46fc-afc2-7951e2a780fc` 先返回原 Walk 落点。随后唯一的有看门狗直达动作 `36bd0058-4652-46e9-8c64-5e28dc83c129` 把已建仓 `260` 外 5 m 作为地面终点，正常跨越约 133 m 并以 Walk/速度 0 收敛，核心约 `362 -> 334.4 MJ`。返程朝无线塔 `180` 的动作则在进入已增建的母基地后由看门狗以剩余 `29.90 m` 明确终止；这再次把“水面中点错误”和“终点附近工厂碰撞”拆成两种独立故障，后者转交 EXP-061。
- 直接证据：物流等待期在 `768/767/133/143/129` 密集区内，候选点 `(-65.23,-92.36,-164.90)` 对全部非带/非分拣器实体仍有至少约 `8.17 m` 中心净距，但动作 `38a51544-65e1-44d9-8e30-2f0b3e9d6171` 到达后 fresh 状态明确为 `Drift`。路线立即停止；返回前一精确 Walk 落点的唯一提交 `d207197c-827e-458e-a0a7-130628407d8e` 首次 fresh 绑定即获接受，最终回到 Walk/速度 0、核心满电且 write health healthy。这证明“实体碰撞净空”与“地形可站立”是两条独立检查，不能把几何自由扇区当作陆地判据。
- 限制或反例：当前只有母星两组跨水弧段反例，还没有地形可通行网格或沿岸自动规划器；“已验证连续锚点”仅对现场中完成复读的有向段成立。精确落点恢复不处理建筑碰撞；如果终态是 Walk 但无位移，仍应改用 EXP-057/061 的局部脱困流程。
- 复验触发：下一条未验证的陆地锚点弧段、跨水路线自动检测、不同星球、沿岸绕行或第三次独立反例。
- 关联：EXP-035、EXP-036、EXP-046、EXP-051、EXP-053、EXP-057、EXP-061、`scripts/invoke-surface-route.ps1`。
- 最近复验：2026-09-03（从已验证 Walk 空地发出、全程非带实体中心净空至少 13.17 m 的约 50 m 候选仍在末端进入 Drift；在没有可验证干燥地形之前，不继续把几何自由空间当作陆地）。

### EXP-067 — 历史产出不代表恢复后拓扑仍完整，研究停滞先追到上游满缓冲和端点

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：普通保存/恢复后的既有制造线、制造台→分拣器→传送带端点、多矩阵研究站的停滞诊断。
- 当前结论：某产线曾经自动产出只是历史里程碑，每次恢复后和长时间等待后都要用当前输入/输出缓冲、双端拓扑、分拣器携货和下游增长重新验收。科技 hash 停止时不直接增加研究站：先读研究站缺失的矩阵，再逐级上溯到“生产设备输出已满但输出端不取货”的首个不变量破坏点。修复要新建合法连接并读回流量，不手动塞矩阵掩盖故障。
- 直接证据：科技 `1403` 在 game tick `5800693 -> 5836843` 长窗内仍停在 `41940/90000`。研究站 `84/679` 都有 `36000` 能量矩阵缓冲、都缺电磁矩阵；蓝矩阵生产站 `76` 的磁线圈缓冲为 6、电路板为 0。其上游制造台 `36` 仍满供电且铁/铜输入分别为 `6/3`，但电路板输出缓冲已满 20；原输出分拣器 `572` 只剩 `insertTarget=571`、`pickTarget=null`，制造台 `36` 的连接表仅含输入分拣器 `595`，不包含 `572`。末端 `565 -> 573 -> 76` 拓扑仍完整但无货，把断点精确收敛到 `36 -> 572`。
- 直接证据：新分拣器 `714` 经正常 prepare/commit 精确建为 `36 -> 571`，初次复读即为满供电、`Sending`、携带 1 个电路板，制造台输出 `20 -> 19`。20 秒对照窗内，蓝矩阵站 `76` 电路板缓冲 `3 -> 6`，研究站 `679` 蓝矩阵缓冲 `1600 -> 1880`，科技 `1403` 由 `44179 -> 45228`。这完成了断点诊断与自动修复的流量闭环，但对“端点为何丢失”的原因判定仍保持 observed。
- 直接证据：连接槽修复部署后，旧输入 sorter `595` 也已失去制造台 `36` 一端。新 sorter `721` 恢复 `580 -> 36` 后，制造台重新生产并通过 `714` 输出电路板；继续向下复读却发现研究仍停在 `47160`，蓝矩阵站 `76` 此时缺磁线圈。线圈制造台 `73` 有 6 个磁铁、0 个铜块，旧输入 sorter `284` 同样只剩单边端点。新 sorter `722` 以独立槽恢复 `26 -> 73` 后实际携带铜块 `1104`；25 秒内 `76` 的线圈/电路板缓存均由 `5 -> 6`，研究科技 `1403` 由 `47354 -> 48923`。这把方法从“修一个已知断点”扩展为“每修一层都继续追踪，直到最终科技/产物流量恢复”。
- 限制或反例：后续新熔炉的独立现场样本已把当前 Plugin 的端点丢失原因收敛为非带建筑的分拣器槽位重用，详见 EXP-068；不再把它归因于换 Steam 账号、保存、加载或游戏本身。单独 `pickTarget=null` 仍不足以诊断所有分拣器；本次还有制造台端连接缺失、输出满和下游无增长三项独立对照。
- 复验触发：下一次普通保存/恢复、其他曾验收产线停滞、端点读取实现变化、科技再次停止增长或找到可重现的断连时机。
- 关联：EXP-028、EXP-033、EXP-037、EXP-049、EXP-054、EXP-058、EXP-059、`docs/gameplay-timeline.md`。
- 最近复验：2026-09-01（科技长窗停滞、两座研究站缺蓝、制造台满输出与分拣器端点缺失的完整追踪）。

### EXP-068 — 非带建筑的分拣器端口必须排除已占槽位，否则后建连接会覆盖旧端点

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前已部署 Plugin 的 `prepare_build` inserter 选端、具有多个 `slotPoses` 的制造/冶炼设备、先输入后输出或反向的多分拣器拓扑。
- 当前结论：非传送带端点不能把所有模型 `slotPoses` 都作为候选；prepare 前必须用当前工厂连接表排除已被其他分拣器占用的 slot，并把该连接摘要绑入 endpoint hash。施工完成后不能只读新分拣器的 `pickTarget/insertTarget`；还要复读源、目标各自预定 slot 都指向新实体，防止“新线成功但旧线被顶掉”被误分类为 completed。修复版已部署并完成同一设备双向端口实机验收；旧档中已断开的输入仍必须新建合法连接，不能把 sorter 自报端点当成修复完成。
- 直接证据：旧蓝糖线中，制造台 `36` 当前连接表仅剩后建输入 `595`，先建输出 `572` 的 `pickTarget` 已为空。独立新熔炉 `715` 先由动作 `e5d0fb0c-f4a2-4639-ae9d-1c51278ed682` 建入口 `716 -> 718 -> 715`，再由 `219018d0-d917-4f93-89af-fdd544a81123` 建出口 `715 -> 719 -> 717`。第二动作被报 completed 后，熔炉 `715` 连接表却只剩 slot `0` 的输出 `719`，输入 `718` 虽仍自报 `insertTarget=715`却不再被目标端持有。两组相反方向、不同设备的样本形成独立复现。
- 直接证据：源码 `GetInserterEndpointPoints` 的传送带分支单独生成方位；非带分支则遍历全部 `item.prefabDesc.slotPoses` 并无任何 `ReadObjectConn`/已占槽过滤。对照方法 `GetFreePortPoints` 已会逐 slot 读连接并跳过 `otherObjectId != 0`；当前 inserter 准备路径漏掉了同一不变量。`VerifyBuiltTopology` 又只验新 sorter 自身的 `pickTarget/insertTarget`，未校验两个 endpoint slot 的反向持有，解释了为何覆盖仍被报成功。
- 直接证据：修复版重启后，动作 `6fbfdd4c-d7ec-43a4-976d-8570d228b693` 在同一熔炉上新建 `716 -> 720 -> 715`。施工前源仓 `716` 仅有 slot `2 -> 718`，熔炉 `715` 仅有输出 slot `0 -> 719`；施工后源仓同时保留 slot `2 -> 718` 并新增 slot `3 -> 720`，熔炉同时保留输出 slot `0 -> 719` 并新增输入 slot `8 -> 720`。新 sorter 反向读回 `pick=716/insert=715`，两端连接均指向实体 `720`。这证明已占槽过滤和双端反向验收在当前部署 DLL 上共同生效。
- 限制或反例：传送带允许多个分拣器从不同附着方位取/放货，不能直接套用“每个 belt slot 只用一次”的简化规则；本次修复范围先限于非带设备。旧存档中已失去反向持有的 sorter 不会被代码自动修复，新版部署后仍需以新建合法端口逐条修复并重验流量。
- 复验触发：belt 端多 sorter、另一种非带制造设备的第二端口、当前版本 DSP 连接槽语义变化或任一已验收旧端点再次消失。
- 关联：EXP-012、EXP-028、EXP-037、EXP-058、EXP-062、EXP-067、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（修复版在熔炉 `715` 上保留既有输出并新增独立输入槽的实机闭环）。

### EXP-069 — 健康同档重启应在正常保存时签发最新 tick 的受保护一次性交接票据

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：为部署新 Plugin 而对健康 owned 世界进行的计划内正常关闭/重启、fixed LastExit 恢复和受保护 runtime-handoff。
- 当前结论：不应只在 quarantine 时才签发 resume ticket。每次正常 save API 成功且 session 仍为 owned/healthy/和平/非沙盒/1x 时，Plugin 应用高熵 token 绑定当前高熵主档身份、session/process/bridge/game version、planet 和该次正常保存 tick，原子写入 Plugin 可见的固定受保护 handoff 目录。健康 planned restart 在源进程正常退出后只加载 ticket 内封存的 exact primary，不再依赖 LastExit 新鲜度；只有 quarantine recovery 才允许 fresh fixed LastExit 保存未正常落盘的进度，详见 EXP-084。两类恢复后都要复读 `gameTick >= minimumGameTick`、目标星球/模式/健康与日记门槛。票据一次消费，不枚举也不读取任何其他存档。
- 直接证据：当前正常保存动作 `fc75686b-6399-4bb1-bfa8-a63bd0ba9e33` 将同一 owned 主档保存到 tick `5938815`，revision `100 -> 101`、`ownedSaveState=saved`、`writeHealth=healthy`。已过期的旧 quarantine ticket 没有被复用；新 `scripts/arm-planned-restart-handoff.ps1` 只从当前认证 Bridge 的结构化 session/descriptor 生成 version 1 交接，未打印 token 或主档名。它已在固定 handoff 目录成功落下 planet `104`、minimum tick `5938815` 的新票据，目录与文件均关闭继承且无其他 SID 允许规则。
- 直接证据：源码已增加 `ArmFromHealthySavedOwnedSession`；健康 save 成功后以当前 `_lastOwnedSaveGameTick` 签发，并同时原子持久到运行目录和固定 handoff 目录。完整 solution 编译 0 warning/0 error；连接槽同批回归后 Core/Contracts/MCP 共 65 测试通过。这部分尚未部署到当前进程，所以本条在新 DLL 首次自动签发与二次重启复验前保持 observed。
- 直接证据：新 DLL 以 SHA-256 `AE418B0DE09A6FF8812175BE714720F95777E688E8B13EC41340E821A7E5F45B` 安装后，主菜单直接读到 bootstrap `restartResumeAvailable=true`。动作 `3a5bead7-3521-490d-8279-8d82eb04ad18` 经 fixed LastExit 完成，新 session `8d8c930c-f483-454b-9f3d-552072459918` 为 planet `104`、和平/非沙盒/1x、healthy，初次 tick `5965040 >= 5938815`；日记仍仅 8 条且序号 8 是自动钛块，新熔炉/仓/分拣器 `715…719` 也全部存在，排除回档。恢复后的自动正常保存到 tick `5965043`，session 随即读到新 `restartResumeAvailable=true`；固定 handoff 文件在该时刻更新、ACL 关闭继承且无其他 SID allow。
- 直接证据：第二次部署前没有再运行 bootstrap；旧进程虽因已明确的虚拟 belt 槽验收缺陷进入 quarantine，仍接受正常窗口关闭并由 DSP 更新 fixed LastExit。新版 Plugin 安装后，主菜单直接提供上一次 Plugin 自签发票据；恢复 prepare 保留 planet `104` 且 minimum tick 不低于 `5965043`，动作 `4098eea2-82bf-4546-929d-aa6c675e9aa4` 完成。新 session `9698faef-9cf1-4d0f-bba4-f4abad92b69f` 在 tick `6028386` 为和平/非沙盒/1x、healthy；隔离前新建的 sorter `720/721`、背包仅余 4 sorter、玩家位置和 8 条日记均保留，随后 `lastOwnedSaveGameTick=6028336` 且再次可见新的 restart handoff。这完成“Plugin 自签发→正常关闭→一次消费→同档精确恢复→再次自签发”的闭环。
- 直接证据：本次关机前的普通保存把主档推进到 tick `8340400`、revision `677`；fresh session 明确报告 `restartResumeAvailable=true`。进程 `35504` 随后接受 `CloseMainWindow` 并正常退出，运行时 descriptor 降为 `0`，固定受保护 handoff 票据在退出后仍存在。只读的脱敏票据复核确认 minimum tick `8340400`、planet `104`、非 quarantine，并将现行 24 小时期限落实为本地 `2026-09-02T23:16:11.8559867+08:00`。没有运行 bootstrap、没有打印 token/主档名、没有强杀进程。
- 限制或反例：历史首次部署曾需要由已认证健康 session 生成 bootstrap handoff，因为旧 DLL 没有计划内签发能力；当前部署已经自动签发，不能再运行 bootstrap 覆盖它。现行票据固定 24 小时过期，过期后不能在离线状态延长或伪造；若长期关机需求反复出现，应在下一次仍有效的正常恢复后把 planned-restart lifetime 改为受测试的配置，再由新的健康保存签发新票据。任何路径都不授权从存档文件推断身份。planned 与 quarantine 的载入源必须按 EXP-084 分流。
- 复验触发：ticket ACL/路径变化、LastExit 时间门槛失败、不同宿主/Windows 用户，或日记/最新 tick/实体门槛不匹配。
- 关联：EXP-004、EXP-005、EXP-006、EXP-038、EXP-047、EXP-064、`src/Spherewright.Plugin/RuntimeDescriptor/OwnedWorldResumeTicketStore.cs`、`src/Spherewright.Plugin/Game/GameSessionTracker.cs`、`scripts/arm-planned-restart-handoff.ps1`。
- 最近复验：2026-09-03（新增一轮健康保存、正常关窗、exact-primary 一次消费和恢复后自动重存闭环；最终 `14290235 -> 14290266`，下一张票据可用）。

### EXP-070 — 传送带分拣器附着方位是虚拟 slot，完工反查必须扫描实际连接槽

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前 `prepare_build` 的 belt↔sorter 端点、完工拓扑验收与 outcome-unknown 隔离核销。
- 当前结论：传送带端在 prepare 阶段用 `slot=-1` 表示围绕同一 belt pose 的四个候选附着方位，DSP 原生施工后才把连接写入真实的 `0…15` 槽。完工验收不能对 `-1` 调用 `ReadObjectConn`；对虚拟 belt 端应扫描当前 16 个连接槽，要求其中恰有方向正确且指向本次唯一新 sorter 的连接。非带设备仍必须核对 prepare 时选定的精确空闲槽，不能放宽为任意槽。
- 直接证据：动作 `41476613-6a9b-4b1a-98d5-a6d77acbbccf` 恰好消耗 1 个分拣器（背包 `5 -> 4`），唯一新实体 `721` 读回 `pick=580/insert=36`；源带 `580` 新增 output slot `5 -> 721`，目标制造台 `36` 新增 input slot `1 -> 721`，而旧输出 slot `0 -> 714` 保持。动作仍被当前 DLL 以“Prepared source slot -1 does not point to sorter 721”标为 outcome_unknown；针对同一 revision/action 的只读 reconciliation 也只因同一句虚拟槽错误而拒绝，世界证据本身没有分歧。
- 直接证据：修复源码把 `slot < 0` 的验收候选限定为当前 16 个真实连接槽，具体非负槽仍只验自身；新增 3 个候选选择测试后，Contracts/Core/MCP 共 `4 + 51 + 13 = 68` 项测试通过，完整 solution build 为 0 warning/0 error。在当时尚未重启的旧进程中，本条仍保持 observed；后续同批部署和下述 live actions 已补齐该缺口。
- 直接证据：修复版部署后，电动机线连续三条含 belt 虚拟端的 sorter 均正常 `completed`：动作 `7fc4c623-ebbf-48ee-9f67-d5c649d66a32` 创建 `726 -> 743 -> 740`，动作 `86be36bc-c9d6-491b-b978-a03730c47155` 创建 `735 -> 744 -> 724`，动作 `a5adefad-4327-4bcb-b3e6-50e86a83b52b` 创建 `732 -> 745 -> 726`。每条都由新实体唯一归因、sorter 自身双端读回和两端真实连接槽共同验收，写入保持 healthy；这补齐了修复版 live completion 缺口。
- 限制或反例：扫描真实槽只适用于 prepare 明确保留 `-1` 的 belt 虚拟附着端，并且仍须与新实体唯一归因、sorter 自身 `pickTarget/insertTarget`、物品精确差量和另一端精确槽联合证明。不得把它用于掩盖非带 slot 被覆盖、多个候选实体或方向不符。
- 复验触发：隔离核销再次遇到虚拟槽、连接槽上限变化、另一类 belt 附着方向或任一新 belt↔sorter 被错误判为 outcome unknown。
- 关联：EXP-012、EXP-028、EXP-038、EXP-062、EXP-068、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（修复版三条 machine↔belt sorter 连续正常完成并读回双端拓扑）。

### EXP-071 — LastExit 未刷新时只能回到票据绑定的最新健康主档，不能伪造关闭证据

- 状态：`superseded`
- 日期：2026-09-01
- 适用范围：健康 owned session 已签发一次性重启票据、随后 Unity 主线程停滞且正常窗口关闭未更新 fixed LastExit 的恢复。
- 当前结论：本条关于“不伪造 LastExit、不枚举存档、只接受票据内精确主档”的证据仍成立，但“健康重启优先 fresh LastExit”已由 EXP-084 替代。现行规则按票据类型分流：健康 planned restart 只加载票据内精确 primary；只有 quarantine recovery 才允许 fresh LastExit 保留未保存进度，且两者都要在 commit 前以 header tick 验证 minimum game tick。
- 直接证据：新 sorter 提交后的 `get_action_result` 首次返回 Unity main-thread `REQUEST_TIMEOUT`，随后三次 `get_session_state` 均在相同边界超时；进程仍存活且 BepInEx 无动作异常。正常 `CloseMainWindow` 被接受并退出，但 `_lastexit_.dsv` 的 UTC 修改时间保持 `2026-09-01T03:55:16.1927201Z`，旧恢复器因此正确返回 `STALE_STATE: LastExit predates ticket`，没有加载。
- 直接证据：新增 `OwnedWorldResumeSourceSelector` 后，只有 fresh LastExit 优先，或 fresh exact owned primary 兜底，二者都过期则拒绝；Core 新增 4 项策略测试，Contracts/Core/MCP 合计 `4 + 55 + 13 = 72` 项测试通过，完整构建 0 warning/0 error。部署后动作 `f3d5586f-9ede-49b5-8b88-d2dd191f7377` 明确报告 exact ticket-bound primary 通过，minimum tick `6028336`，新 session `905747a6-21cd-4782-81c0-9abeb5b5536a` 在 tick `6028418`、planet `104`、和平/非沙盒/1×、healthy，并再次签发下一张票据。
- 直接证据：恢复后 sorter 背包仍为 4，既有修复实体 `721` 及其 `580 slot5 -> 721 -> 36 slot1` 拓扑存在，而未能证明且未落盘的候选实体 `722` 返回 `INVALID_ENTITY`。这证明兜底严格回到 ticket minimum 对应健康保存点，没有把超时请求猜成成功，也没有重复提交。
- 限制或反例：兜底不保留票据签发后的未保存进度；如果 exact primary 也早于签发容差、源进程仍活着、加载后 tick/身份/星球/模式任一不符，必须拒绝。它不接受调用方存档名、不列目录、不解析/修改存档，也不替代正常保存与正常 LastExit 更新。
- 复验触发：下一次主线程停滞关闭、primary 时间容差变化、不同文件系统时间精度、同名身份/tick 验收变化，或 exact primary 兜底被错误用于正常 fresh LastExit。
- 关联：EXP-004、EXP-005、EXP-006、EXP-038、EXP-064、EXP-069、`src/Spherewright.Plugin/Game/OwnedWorldResumeCoordinator.cs`。
- 最近复验：2026-09-01（历史 live 对照仍有效；源选择优先级已由 EXP-084 的新安全边界替代）。

### EXP-072 — Plugin 引用的新 Core 类型要求同批部署所有 Spherewright 程序集

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：BepInEx `plugins/Spherewright` 下的 `Spherewright.Plugin.dll`、`Spherewright.Bridge.Core.dll`、`Spherewright.Contracts.dll` 部署。
- 当前结论：只替换 Plugin DLL 并不等于部署了当前构建；当 Plugin 开始引用本批新增的 Core 类型/方法时，旧 Core 会在主线程首次触达该路径时抛 `TypeLoadException` 或 `MissingMethodException`。部署必须在 DSP 完全退出后，把同一 Release 输出中的 Plugin/Core/Contracts 三个程序集一起替换并逐个校验 SHA-256；PDB 可同批更新但不参与运行身份。不得在游戏运行时覆盖依赖程序集。
- 直接证据：首次只安装 Plugin hash `0B5D3D2144C637827995D6AE2C44B1219D6BC5403D7D1FFE4AA65621EB8468E2` 后，恢复 prepare 返回 `INTERNAL_ERROR`；BepInEx 精确记录无法从旧 `Spherewright.Bridge.Core` 解析 `OwnedWorldResumeSourceKind`。票据未消费且未加载存档。正常关闭主菜单后，同批复制 Plugin/Core/Contracts，安装 hash 分别与构建产物完全一致；下一次 prepare/commit 即完成 EXP-071 的恢复。
- 限制或反例：如果某次改动只涉及 Plugin 内部且公共依赖二进制未变，旧依赖可能碰巧可用，但部署流程仍不应猜测 ABI 兼容；Newtonsoft/BepInEx/游戏程序集有各自来源，不能无条件覆盖。
- 复验触发：Contracts/Core 公开类型或方法变化、部署脚本实现、Release 打包、任一安装 hash 不一致或再次出现类型/方法加载错误。
- 关联：EXP-001、EXP-002、EXP-030、EXP-069、EXP-071。
- 最近复验：2026-09-03（新增 Core logistics-window 公开类型后，Plugin/Core/Contracts/Newtonsoft 均在 DSP 完全退出时同批替换；四文件与最终构建输出哈希一致且 live 调用成功）。

### EXP-073 — 上游修复不能以首个局部流量为终点，必须逐层复读到最终消费者

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：多级制造链、矩阵生产与研究消费，以及一个故障修复后可能立即暴露下一处既存断点的恢复现场。
- 当前结论：某个上游设备重新工作或某种中间件重新到达下游，只能证明这一段恢复，不能证明整条目标链已经恢复。每次修复后应沿目标方向逐层复读：源库存下降、输入 sorter 实际携货、生产设备输入/输出变化、输出 sorter 携货、下游设备各配方缓存，最后用最终产物或同一科技的 hash 正增长验收。若最终指标仍为零，就从第一个缺失缓存反向追到下一处断点；不得用手动补料掩盖它，也不得因首段成功就结束等待。
- 直接证据：新 sorter `721` 恢复了电路板制造台 `36` 的铁块输入，制造台和输出 sorter `714` 均出现当前流量，但科技仍停在 `47160`。继续逐层读取发现蓝矩阵站 `76` 已不缺电路板而缺磁线圈；制造台 `73` 有 6 个磁铁、0 个铜块，旧 sorter `284` 自报目标 `73`，但目标连接表不再持有它。动作 `3c99aa3d-0399-412c-9c7a-34ba545f5cba` 新建独立槽 sorter `722` 后，实体立即以 `Sending` 携带铜块 `1104`，并保留仓 `26` 的既有三个输出与制造台 `73` 的既有输出。25 秒对照窗中蓝矩阵站 `76` 的磁线圈/电路板缓存均 `5 -> 6`，研究站恢复工作，科技 `1403` 由 `47354 -> 48923`，形成最终消费者闭环。
- 直接证据：行星物流 `1604` 等待期形成第二个独立恢复窗。旧矿枯竭后，3000 自动铁块仍保存在仓 `829`，而电路板制造台 `36` 的铁输入为 0；500 铁块经玩家守恒进入既有上游仓 `28` 后，sorter `594`、长带末端 sorter `721` 和制造台 `36` 依次恢复工作。随后回收仓 `562` 的 500 自动电路板又守恒进入仓 `26`，蓝矩阵站 `76` 保持满供电且双输入各约 5–6；同一科技在这些恢复动作期间由至少 `22643 -> 28505/144000` 持续增长。修复没有停在“铁已上带”或“电路板已入仓”，而是追到最终科研 hash。
- 限制或反例：一次短窗正增长可能来自既有缓冲，不能单独证明永久稳定；仍需在更长窗口确认源库存、两种矩阵供给和科技持续增长。若观察窗口跨科技切换，必须按科技 ID 分段，不能把两个科技的 hash 相加。
- 复验触发：当前 `1403` 的下一次持续窗口、科技切换到 `1701`、钻石线激活、黄矩阵多级链或任何“局部已工作但最终指标不增长”的现场。
- 关联：EXP-028、EXP-037、EXP-049、EXP-054、EXP-059、EXP-067、EXP-068。
- 最近复验：2026-09-02（两个独立科研窗口均从上游库存/分拣器/制造设备逐层追到蓝矩阵工作与指定科技 hash 持续增长）。

### EXP-074 — 混合备料仓必须在空载时先完成全部出口过滤，再按物料逐项装仓

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：一个普通储仓向同一多级配方提供两种以上物料、多个源分拣器共享储仓端点，以及需要在不建多座输入仓的情况下保持每条支线单品纯净的紧凑产线。
- 当前结论：共享备料仓可以服务多物料产线，但安全顺序是硬前提：储仓保持空载，先建完所有源分拣器；逐个在空闲且不携货时设置精确 `filterItemId` 并复读连接、过滤和供电；最后才按物料分批守恒装仓。若物料已经提前进入无过滤共享仓，应先全部守恒取回，不能赌分拣器恰好取到目标物，也不能在载货中强改过滤。验收必须追到每条过滤分拣器的实际携货、中间产物和最终成品增长。
- 直接证据：电动机备料仓 `723` 最初误提前装入铁块/磁铁/铜块；在任何未过滤源分拣器取货前，三种物料均通过普通 transfer 完整取回玩家。随后空仓状态下建成 `746/747/748/749`，依次绑定铁块 `1101`、磁铁 `1102`、铜块 `1104`、铁块 `1101`，复读均为空载、双端连通且满供电。此后才依次装入 100 铜块、200 磁铁和 500 铁块；后续快照中四个分拣器只携各自过滤物，仓内相应降至铜 66、磁铁 132、铁块 341，未出现跨支线物料。
- 直接证据：过滤后的铁带驱动制造台 `726(recipe 5)`，其齿轮经 `743 -> 740…735 -> 744` 到制造台 `724`；磁铁/铜块驱动 `725(recipe 6)`，磁线圈经 `741` 到 `724`；直连铁 sorter `749` 同时供料。日记序号 11/12 分别捕获首个自动齿轮和电动机，成品仓 `727` 达到 39 个 item `1203`，把“没有串料”追到了最终消费者而非只看源端过滤字段。
- 直接证据：第二个独立样本是有机晶体输入仓 `761`。化工厂 `760` 已空载配置配方 `25`，随后从仍为空的仓 `761` 连续建出 sorter `763/764/765`，分别设置塑料 `1115`、精炼油 `1114`、水 `1000` 过滤并复读空载/满供电；只有此后才通过普通 transfer 守恒装入塑料 200、油 100、水 100。实际流量中 `763` 只携塑料，三种仓内计数分别下降，化工厂三项输入均正增长并连续工作，输出仓 `762` 的有机晶体 `1 -> 7`。该样本与电动机的制造链不同，补齐了化工三原料共享仓的独立正例。
- 限制或反例：两个正样本均为普通一级储仓和一级分拣器；它们不证明储液罐、多层仓、物流站或载货分拣器可沿用同一配置时机。过滤只能保证物品选择，不能替代端口占用、距离、电力、带方向和下游背压验收。若共享仓未来加入新物品或新出口，仍需重新做空载/携货风险评估。
- 复验触发：有机晶体/钛晶石多物料线、从现有混料油仓重建纯油出口、共享仓新增第五出口、过滤分拣器停电或任一支线读到非目标物。
- 关联：EXP-021、EXP-028、EXP-037、EXP-058、EXP-060、EXP-062、EXP-065、EXP-073。
- 最近复验：2026-09-03（磁铁单料只恢复磁线圈后，又从自动铁仓守恒续入 300 铁；齿轮/电机支路与最终涡轮线随后正常工作，涡轮专仓 `23 -> 44`，四过滤仍无串料）。

### EXP-075 — 抽水站由原生水面校验放置，并从专用泵口先接带再接仓

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：当前 DSP build 的 item `2306` 抽水站、`EMinerType.Water` 目录分类、普通 click-build/无人机施工，以及水 item `1000` 的自动输出。
- 当前结论：`water-pump` 是目录角色，不是特殊注入动作；仍以普通手搓获得物品，让 `BuildTool_Click.CheckBuildConditions` 在候选位置执行原生水域判定，再由施工无人机完成。泵体不暴露普通 inserter attachment pose，不能强行直连仓库分拣器；正确出口是泵的固定 belt slot，先建由泵口出发的有向带，再由末端带经普通分拣器送入仓。产线完成必须同时复读泵体满供电/工作、泵口反向连接、末端 sorter 水货和专用仓增长。
- 直接证据：从自动电动机仓 `727` 守恒取出 4 个 item `1203`，连同玩家已有铁块、石材和电路板由普通配方 `49` 手搓出 1 个 item `2306`；日记序号 13 记录首次手搓抽水站。无指定坐标的 native prepare 在玩家附近找到了 `(-58.5872536,-100.825363,-162.732346)`，施工动作只消耗这 1 个泵体并产生唯一实体 `752`。泵体接入 network 1、`powerServeRatio=1.0`、`isWorking=true`，首次复读内部已有 30 水，证明水域判定、施工和采集均走普通路径。
- 直接证据：`752 -> 753` 的 sorter prepare 返回“no current-version inserter slot”，且没有 commit；这不是距离故障。源绑定 belt 动作 `9d81bf19-c0b3-4693-8202-9bd78cfcf8e5` 正常创建 `752 -> 758 -> 757 -> 756 -> 755 -> 754`，泵体 slot 0 反向持有带 `758`。动作 `d43a9c52-59e7-4ea8-97bb-b51f59ebebe8` 再建 `754 -> 759 -> 753`；sorter `759` 满供电并实际携带水。专用仓先读到 9 水，12 秒后为两个 stack 共 31，日记序号 14 在 tick `6245078` 记录 `production_line_item_first` 水（实际时间 `2026-09-01T13:14:06.2593487+08:00`、本局 `001d 04:54:44`）。保存动作 `44f2f0d0-9713-4e35-9073-45f1ce5c7787` 持久化 tick `6267723`、写入健康。
- 限制或反例：当前只验证一处母星水域与一级带/分拣器；候选仓在朝玩家方向 5.5–7.5 m 仍返回 `NeedGround`，说明泵周围水面不能直接当陆地仓位。最终仓位是在泵周围 5.5 m 的另一个方位经原生校验找到，不能把坐标或方位外推到其他海岸。泵内水增长不替代出仓证明，带端方向和电力仍须分别读取。
- 复验触发：另一星球/水类型、另一处海岸、泵体模型 slot 变化、直接接物流设施、带路反向、断电泵或 DSP build 变化。
- 关联：EXP-007、EXP-008、EXP-012、EXP-020、EXP-028、EXP-037、EXP-042、EXP-062、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（普通泵体物品、原生水面施工、固定带口、仓 `9 -> 31` 和日记/保存完整闭环）。

### EXP-076 — 移动被基座提前终止时，若业务目标已在操作范围内就不要继续撞目标

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：为了 transfer、inspect、build 等范围型业务而接近仓库/设备，180-tick 物理停滞看门狗已明确终止本次 Move，且玩家仍为 Walk、速度 0、能量充足。
- 当前结论：接近建筑的 Move 只是取得业务范围的手段，不是必须把几何剩余距离压到 arrival tolerance 的独立目标。看门狗若在基座前终止，应先 fresh 读取玩家到实际业务实体的距离并直接 prepare 原本操作；若操作的原生范围检查通过，就完成业务后沿已验证反向锚点离开，不再向同一基座重放 Move。只有原本操作仍因 TooFar 拒绝时，才按 EXP-057/061 选择有界侧向脱困点。这样同时避免耗尽能量和无意义的“最后半米”碰撞。
- 直接证据：从钻石仓 `717` 前往塑料仓 `558` 时，Move `2f126632-525e-4b72-b395-5fb81d0c208c` 已前进大部分路程，但在剩余 5.47 m 时连续 180 tick 位移不足 0.75 m，被看门狗以 `action_failed` 精确终止；核心仍约 391.6/400 MJ，fresh player 为 Walk/速度 0。没有重放或四向探测；仓 `558` 的 fresh 距离为 5.473 m，普通 `storage-to-player` prepare/commit 随即通过，动作 `d8210cd4-db07-46bf-bf5d-3777f601ddf` 守恒取得 200 自动塑料（仓 `993 -> 793`、玩家 `0 -> 200`）。随后第一段反向移动到仓 `717` 外 4.36 m 正常完成，并沿既有锚点返程。
- 限制或反例：本条不把 5.47 m 定义为任何通用操作半径，也不允许忽略 Move 的未知结果；必须先有明确 terminal `action_failed`、fresh Walk/速度/能量和原业务 prepare 的原生范围通过。若玩家仍在 Drift、动作 outcome unknown、业务目标不是范围型，或离开方向也被堵，应回到对应隔离/落地/脱困规程。
- 复验触发：下一次仓库/制造台/电塔基座前停滞、不同业务操作半径、Move 停在 arrival tolerance 外但业务 prepare 仍 TooFar，或反向首段也无位移。
- 关联：EXP-007、EXP-009、EXP-035、EXP-036、EXP-053、EXP-057、EXP-061、EXP-066。
- 最近复验：2026-09-01（塑料仓前 5.47 m 停滞后直接完成守恒 transfer，并一次反向离开）。

### EXP-077 — 紧凑产线施工验收还要包含玩家撤离空间，原生放置合法不等于不会夹脚

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：玩家站在既有电塔、仓库或制造设备附近连续预建多座建筑和 sorter，所有单体都通过 DSP 原生放置校验的紧凑地表施工。
- 当前结论：原生 build check 能证明单座建筑合法落地，却不证明施工后玩家还有通往下一路点的可行走出口。连续建造前应把 fresh 玩家位置也作为施工几何输入：避免在玩家两侧约 1–3 m 内同时新增大基座，并保留一条朝已验证陆地锚点的开放切向走廊。每完成一组会改变局部碰撞面的建筑，应先读回玩家与半径约 5–7 m 的实体；若撤离长 Move 无进展，立刻按 EXP-061 做有界 4 m 探测，而不是继续建造或等待能量耗尽。成功撤离到 Walk/速度 0 的安全点后再继续物流施工。
- 直接证据：钛晶石线的制造台 `767`、输入仓 `768`、输出仓 `769` 和 sorter `770–772` 均分别通过原生施工与端点验收；但施工结束时，玩家被留在仓 `768` 约 1.76 m与既有电塔 `133` 约 2.12 m之间，另有两条 sorter 距离约 2.8 m。前往已验证风机 `82` 的 Move `a7e44dbd-4e13-40de-8608-bbf453a280cb` 被停滞看门狗明确终止。fresh 扫描后仅用一条 4 m 局部切向动作 `8707b409-12b8-4400-83dc-044e48eaf94b` 脱困，终态位置 `(-75.90817,-100.361412,-155.7321)`、Walk/速度 0、核心 `399.97/400 MJ`；无需重启或消耗到低电。
- 直接证据：后续精炼油扩容首次主动应用本条。朝仓 `163` 的 8 m 候选被 DSP 原生 `NeedGround` 拒绝且没有 commit；把方向与已验证的风机 `34` 陆地切向合成后，prepare 落点只偏离请求 0.28 m，距玩家 7.72 m、距业务源仓 `163` 63.92 m。动作 `e6214391-3da3-4291-9318-c97e352515ed` 建成唯一新仓 `773`，玩家仍保有朝风机 `35/34` 的开放侧向空间；随后三批守恒转移正常把 600 精炼油放入该仓，没有先因施工夹脚而触发长 Move。
- 直接证据：黄矩阵预建把这条经验前移到施工阶段。研究站 `774` 建成时距玩家 26.67 m；输入仓两个候选分别因风机重叠和研究站重叠被无副作用拒绝，最终仓 `775` 与研究站相距 7.42 m并先完成双过滤 sorter。输出仓朝水岸的候选由 `NeedGround` 拒绝，改到另一陆地方向后以 7.30 m 站距建成 `778`；电塔 `780` 距两条输入 sorter 约 4.81 m，使其全部满供电。整组施工后玩家仍保有开放走廊，没有新增夹脚。
- 直接证据：返程再次进入旧仓 `768`/塔 `133` 夹缝时，长 Move 被 180-tick 看门狗在剩余 39.42 m 停止。第一条合成排斥短移虽然成功，但随即直达又被外围建筑阻挡；复读后增加背向目标、最小中心净距约 4.2 m 的第二个 4 m 侧向点，再回到塔 `143`，最后 `143 -> 82` 单段成功。由此“第一条短移完成”只证明脱离当前碰撞面，不证明已经获得朝终点的直出线；必须在长移前再次复读并必要时走显式侧向 waypoint。
- 限制或反例：当前只是一组紧凑仓/电塔/sorter 现场，1–3 m 不是通用禁建半径；不同建筑碰撞体和星球地形仍须由 fresh 几何与实际 Move 复验。本条不要求为玩家清场或拆除已建产线，也不替代 DSP 放置、物品守恒、端点和供电验收。
- 复验触发：下一条紧凑产线施工、玩家同时邻近两座以上大基座、施工后首次撤离、不同建筑组合或能够在 prepare 阶段自动评估撤离扇区。
- 关联：EXP-036、EXP-042、EXP-053、EXP-057、EXP-061、EXP-068、EXP-076。
- 最近复验：2026-09-01（黄矩阵预建主动保留撤离空间；旧钛晶石夹缝复现后补充“短移后再复读、必要时侧向绕出”）。

### EXP-078 — 混合副产物仓无法安全直出时，用空纯源中转仓恢复长带并追到最终科研增长

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：双产物设备的混料仓已污染、旧长带仍物理连通但源 sorter 无过滤或失去源端、下游存在能持续消费单一副产物的自动产线。
- 当前结论：不能为了复用长带而从混料仓直接新建会立刻取货的无过滤 sorter。安全恢复顺序是：先清理并纯化下游目标仓；在长带源端建一个保持空载的专用中转仓；若仓到旧带原生返回 `TooFar`，用自由短带接入旧带主输入，再建 `中转仓 -> 首带` sorter；在中转仓仍为空时设置目标过滤并复读供电/端点；最后才按 item 精确 transfer 把混料仓中的目标副产物搬到中转仓。验收要同时看到源仓下降、中转 sorter 只携目标物、长带目标仓保持纯料、下游设备与最终消费者增长。该方案先消除跨星球或跨半球人工搬运，但混料仓到中转仓仍是人工守恒搬运；在真正完成上游分产过滤前不得称为全自动。
- 直接证据：旧路线 `163 -> 675 -> 596…674 -> 676 -> 557` 已因无过滤把 271 氢混入塑料油仓，且 sorter `675` 后来只剩 `insertTarget=596`。修复先把 `557` 清为纯油并用空载过滤 sorter `783` 接回化工厂 `552`。源端新仓 `784` 以玩家净空 21.03 m 建成；它到带 `596` 的中心距约 6.81 m，但直接 sorter prepare 仍由 DSP 返回 `TooFar`，没有 commit。五格普通带 `789…785` 随后从仓侧 2.84 m 处接入 `596` 的真实主输入，sorter `790` 在仓空时设为精炼油 `1114`、供电 1.0。混料仓 `163` 的 500 油经两次 exact transfer 进入 `784` 后，`784` 立即 `500 -> 499`、`790` 携 1 油、末端仓 `557` 保持纯油且化工厂继续工作；两座炼油厂从停机恢复工作，结构矩阵研究由 `74100` 连续推进到 `103272`、随后 `120316`。
- 直接证据：同一科技推进到 `223566/240000` 时再次出现可预判反压：仓 `163` 有 560 油/15 氢、纯油中继 `784` 已空，两座精炼厂油输出均积 40 且至少一座停机，氢罐降至 38。动作 `c36a0b8d-761a-4bdc-99d2-b8479c88dcb9` 与 `93d34f39-c198-4e09-b76a-cd82811d2a10` 守恒执行第二轮 `163 -> player -> 784` 的 500 油；玩家油 `0 -> 500 -> 0`，源仓油降到 62，中继首读 499。8 秒后两座精炼厂均恢复工作、源仓油为 98，研究增长到 `229338`。这次复验把触发条件收敛为“中继接近排空且精炼油输出达到 40”，无需等氢耗尽或研究停摆。
- 限制或反例：当前上游 `163 -> 784` 仍需玩家按目标 item 搬运，不能误报为炼油副产物永久全自动分离；当 `163` 再满时仍要复读油/氢、容量和研究进度再搬。短带实体数、2.84/6.81 m 和坐标只属于当前姿态；其他现场仍需 native prepare。若旧带本身污染、反向、断端或下游仓未纯化，本流程不能直接套用。
- 复验触发：`163` 再次满载、把炼油厂油输出在进入混料仓前过滤、另一双产物配方、长带目标仓出现非油物品、塑料/红糖/科研停止增长，或真正实现全自动上游分离。
- 关联：EXP-021、EXP-028、EXP-055、EXP-056、EXP-065、EXP-067、EXP-070、EXP-074、EXP-077。
- 最近复验：2026-09-01（第二轮 500 油守恒补中继；精炼油输出满 40 时提前解除反压并恢复双厂工作）。

### EXP-079 — 锁定科技的黄矩阵线先空载过滤预建，解锁后以输入下降、输出增长、日记和保存验收

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：配方仍被科技锁定，但建筑、输入成品和普通物流已经可用的矩阵或其他多输入产线。
- 当前结论：继续沿用 EXP-062 的“先空载、后激活”边界，但矩阵线还要把两个同仓输入分别过滤并证明独立供电。科技未完成时只建空设备、空输出和过滤后的输入端，生产设备保持 recipe `0`；原料可在所有 filter/端点复读后守恒备入。科技正常解锁后重新读取运行时配方，只对仍空载的精确设备提交一次配置。里程碑同时要求源仓两种原料下降、设备满供电并工作、目标仓至少两次增长、逐存档首次产线事件，以及随后一次普通同档保存。人工把既有自动产物搬入输入仓只证明“最后转换段自动”，不能扩写为跨星球供应全自动。
- 直接证据：科技 `1124` 未完成时，研究站 `774` 保持 recipe `0`；输入仓 `775` 先由 sorter `776/777` 分别设置钛晶石 `1118` 与金刚石 `1112` 过滤，输出为 `774 -> 779 -> 778`，电塔 `780` 使两条输入 sorter 满供电。随后才把已有自动产线的 40 钛晶石与 40 金刚石守恒备入同仓。科技于 tick `6894549` 正常解锁，运行时 recipe `27` 明确为 `1×1112 + 1×1118 -> 1×6003`；动作 `c5e65c3c-11b0-4b03-9f00-36b3a95d98f3` 对 fresh 空设备配置一次。研究站随即满供电工作、两种输入各缓存 6，源仓两种原料均 `40 -> 26 -> 23`，输出仓结构矩阵 `7 -> 10`。日记序号 18 在 tick `6898014` 记录首次产线 item `6003`（实际 `2026-09-01T16:15:29.8733116+08:00`、本局 `001d 07:56:06`）；保存动作 `d6e2d8d5-9675-4eb9-a64b-b05403d0af9f` 持久化 tick `6905142`、revision `455 -> 456`、写健康。
- 限制或反例：钛晶石来自同档跨星球飞行带回并在母星自动加工，最终 40 个钛晶石/金刚石仍由玩家做 exact transfer 进入 `775`；当前只完成黄矩阵转换线，不是行星物流站或跨星球钛供应自动化。输入仓共存两物成立依赖两个空载精确 filter，不能删掉过滤后复用。当前 40 组原料是有界缓冲，耗尽后仍需补料。
- 复验触发：下一条科技锁定的多输入配方、黄矩阵输入耗尽、行星物流站接管钛晶石或钛块供应、同仓过滤被修改、恢复后 recipe/端点变化，或下一次自动矩阵里程碑。
- 关联：EXP-021、EXP-028、EXP-062、EXP-073、EXP-074、EXP-077、EXP-078。
- 最近复验：2026-09-01（结构矩阵 recipe 27 解锁后一次激活，自动输出 `7 -> 10`，日记序号 18 与正常保存共同闭环）。

### EXP-080 — 活跃仓并发补货或取货会掩盖 transfer 的聚合净差量

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：`storage-to-player` 或 `player-to-storage` 的目标仓仍被自动产线持续输入/输出，动作前后只读取聚合库存总数的现场。
- 当前结论：正常 transfer 的动作终态和玩家端精确差量可以证明玩家确实取得或交付目标物，但活跃仓在两次快照间的并发生产或分拣可能抵消源端预期负差量，也可能在目标端交付后立刻取走一部分。此时不得把聚合数量偏离动作 count 解释为未执行并重放，也不能把跨活跃窗口的 `before -> after` 伪写成完整双边守恒。应把玩家中转、源/目标仓和可识别的设备/分拣器在途缓存一起复读；若里程碑必须证明静态双边守恒，则使用不再通料的仓、短时隔离物流，或把同一窗口的已证明生产/消费增量纳入记账。
- 直接证据：为修复钢材上游而从持续收纳电路板的仓 `26` 取 1 个 item `1301`。动作完成后 fresh player 明确 `0 -> 1`，但仓内电路板聚合仍为 `400 -> 400`，与该仓正在被电路板产线补货一致。客户端守恒断言因此报错，但没有重放；后续背包仍保留该 1 个电路板。这个样本不否定 transfer 的内部精确检查，只否定“跨活跃窗口的两个聚合快照必然显示相反净差量”。
- 直接证据：粒子容器续料时，涡轮仓 `827` 经玩家中转精确交付 42 个 item `1204` 到正在供给制造台 `883` 的输入仓 `884`。两段 action 均 completed，三端复读为源仓 `42 -> 0`、玩家 `0 -> 42 -> 0`，但目标仓首读只有 41；同一 fresh 工厂状态表明已启用的 recipe `99` 和对应过滤 sorter 正在消费涡轮。这与第一个“源端并发补货”样本形成相反方向的独立对照。
- 直接证据：把回收仓 `562` 的 500 自动电路板送入同时仍由制造台补货、又被蓝矩阵站取货的仓 `26` 时，两段 action 精确读到源 `592 -> 92`、玩家 `0 -> 500 -> 0`，目标聚合却为 `63 -> 565`，即净增 502。fresh 读回同时看到蓝矩阵站 `76` 工作且持有电路板，证明聚合多出的 2 个来自并发生产窗口；没有把它误报为 transfer 注入或重放。
- 直接证据：恢复钛晶石线时，60 个有机晶体经动作 `235d8924-a17b-402c-8559-c78ba10ade18`、`31e04538-a318-4d36-9778-8f73baca4d90` 完成源仓 `762 -> player ->` 活跃输入仓 `768` 的两段转运。即时目标仓只显示 59，后续显示 58，但 fresh 制造台 `767` 的正常输入缓冲精确为 2、源仓和玩家均为 0，闭合 `58 + 2 = 60`。未因目标仓少 1/2 个而重放，给出同一目标持续预取下的第二组完整守恒样本。
- 直接证据：第二座 PLS 备料再次给出三组同窗口正样本。`100` 铜块从玩家投入活跃仓 `843` 后首读仅剩 `98`，随后被微晶元件线全部正常消耗；`80` 电路板从仓 `562` 经玩家进入活跃仓 `849` 后首读为 `78`，制造台同时已取得 2，最终仓 `854` 自动产出 `40` 处理器；`20` 粒子容器投入仓 `899` 后首读 `19`，后续全部进入制造台 `898`。同批 `40` 钢材投入后，fresh 分布为仓 `899` 内 31、制造台内 8、sorter `902` 携带 1，精确闭合 40。所有动作均只提交一次，未把即时仓存少 1/2 解读为未执行。
- 直接证据：处理器百件批次再次验证源端并发补货边界。动作 `904e954f-f641-4021-afc6-55b1b7cf10a8` 内部精确读取活跃混合仓 `26` 的电路板 `243 -> 43`、玩家 `1 -> 201`；随后 200 件完整投入仓 `849` 并由 recipe `51` 消耗。完整审计时仓 `26` 已被上游自然补回到 268，并不推翻动作当时的精确取货；目标端最终由输出仓 100 处理器和剩余 20 微晶元件闭合，不把跨窗聚合回升误判为动作未执行。
- 限制或反例：静态仓或已证明无并发通料的窗口仍应要求双边相反差量和增殖点守恒；不能借本条放宽 prepare/commit 的 fresh hash、物品 ID、数量、容量或终态要求。聚合差额只有在设备/在途缓存能唯一解释时才可归因于并发，不能把任何缺口都笼统归为生产波动。
- 复验触发：下一次活跃仓 transfer、引入仓库事件计数/生产寄存器差量、把产线输入临时隔离后复测，或 transfer DTO 增加动作内部双边明细。
- 关联：EXP-007、EXP-021、EXP-028、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-03（从活跃仓 `26` 取 200 电路板时，动作内部精确报告 `243 -> 43`；完整审计时上游已自然补回到 268。以唯一动作终态、玩家中转和处理器百件批次闭合，没有用跨窗口聚合数否定已完成动作或重放）。

### EXP-081 — 正常保存重启清理进程状态，但不会把玩家移出已保存的碰撞夹缝

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：玩家在地表多基座夹缝中由 180-tick 看门狗明确停单、随后按 protected restart ticket 正常保存/关闭/恢复同一 owned world。
- 当前结论：计划内重启是进程、桥 session、旧订单和恢复证明的边界，不是几何脱困或重生接口。正常保存会保留玩家坐标；同档读回后必须先验证 tick/星球/模式/日记/关键实体，再把保留坐标视为新的 fresh 起点。可以因旧订单已清空而重新计算一个有界方向，但不得假定重启自动消除建筑碰撞，也不得无界重复上一轮失败方向。
- 直接证据：四向脱困全部明确失败后，动作 `975afb12-fc7e-4356-a760-30efe2279729` 正常保存同一主档到 tick `7027343`、revision `487 -> 488` 并签发恢复票据。DSP 接受正常窗口关闭；直接启动可执行文件被 Steam 立即结束，改用当前 Steam 客户端的正式 `-applaunch 1366540` 后新进程启动。动作 `4e74efd5-2408-495f-bf27-44328e5cb461` 只消费受保护票据并恢复 planet `104`，首次 tick `7030072 >= 7027343`，和平、非沙盒、1×、healthy 全部成立；钢炉/仓/分拣器 `791–794` 和日记序号 20 均存在。恢复前后玩家都在约 `(-81.28,-53.32,-175.04)`，因此没有发生几何重定位。
- 限制或反例：本条不证明重启对所有碰撞都无帮助；Unity 物理状态和旧订单确实被重建，fresh 短探测仍可能成功。Steam 启动要求属于本机当前账号/客户端现场，换机或启动器变化后必须重新探测，不能硬编码为通用游戏 API。
- 复验触发：下一次卡脚重启、读档后首个短探测、玩家死亡/重生、DSP 保存玩家姿态实现变化、Steam 启动行为变化或恢复门槛不一致。
- 关联：EXP-005、EXP-036、EXP-039、EXP-061、EXP-069、EXP-071。
- 最近复验：2026-09-01（保存/正常关闭/Steam 启动/protected resume 完整闭环；玩家位置被精确保留）。

### EXP-082 — 星际飞行失败只能重载同一绑定检查点，失败分类必须结构化

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：同星系 interplanetary-flight 已创建独立 checkpoint，返航在启动或落地验证阶段失败，主档尚未接受本次飞行成功。
- 当前结论：飞行 commit 一旦被接受，轮询或客户端后处理失败都不能用新幂等键重放；只有 terminal 明确失败后，才用该动作返回且方向/tick 与本次飞行一致的 reload token 回到同一 checkpoint，再做 fresh 玩家/星系/能量复读并准备下一次尝试。失败应以 `recovery_required=true` 和可选 `stalled=true` 结构化返回，不能只把“请 reload”藏在 message；成功稳定落地后立即取消 reload capability，待覆盖成功 tick 的精确主档保存完成再永久 retire ticket。
- 直接证据：返航 `102 -> 104` 首次动作在目标星球接触后因限定窗口内未保持 grounded 而 terminal `action_failed`；严格恢复本次 checkpoint 后，第二次动作又因 3600 tick 内未进入 native Sail 而失败。两次都未重放 commit，也未生成新 checkpoint。再次恢复同一 checkpoint 的第三次尝试在 tick `7128712` 于 planet `104`、Walk 状态正常完成，随后精确主档保存到 tick `7146048`、write health healthy。相同起点先后覆盖“落地不稳、未入 Sail、成功落地”三种结果，证明重试对象必须是同一 checkpoint，而失败类型不能从调用方猜测。
- 限制或反例：第三次成功不证明当前飞行控制对所有姿态稳定；现部署 DLL 仍只有文本恢复提示，新结构化状态和 lifecycle 要在新 DLL 部署后复验。checkpoint 重试不会回滚外部 journal，因此成功主档保存后继续暴露旧 token 会造成时间线分叉，必须由 EXP-083 的 retire 规则消除。
- 复验触发：新 DLL 首次 flight failure、结构化 `recovery_required/stalled` 读回、成功主档保存后 session capability 消失、ticket 过期或进程崩溃中断飞行。
- 关联：EXP-004、EXP-005、EXP-047、EXP-050、EXP-052、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.InterplanetaryFlight.cs`、`src/Spherewright.Plugin/RuntimeDescriptor/FlightCheckpointStore.cs`。
- 最近复验：2026-09-01（同一返航 checkpoint 两次明确失败、第三次成功、随后主档正常保存）。

### EXP-083 — 新主档时间线一旦覆盖飞行 checkpoint，旧回档能力必须立即失效

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：pre-flight checkpoint、成功飞行、外部逐存档 journal 与成功后的精确 primary save。
- 当前结论：checkpoint 不能只凭“文件仍在且 header tick 相等”永久有效。ticket 至少需要 24 小时过期、`active -> recovery_required -> flight_succeeded -> retired` 生命周期；成功飞行先把 token 从 SessionState/capability 移除，覆盖成功 tick 的 primary save 再持久化 retired。Plugin 启动时若精确 primary header 已新于 checkpoint tick，也必须把旧/legacy ticket 视为被主时间线 supersede，防止升级前遗留 token 回滚已经保存的世界而让外部 journal 留在未来。
- 直接证据：源码复核确认旧 `FlightCheckpointStore` 的有效性只有 version/gameVersion/字段/文件 header，`TryValidateReloadContext` 在当前游戏只核对 owned save、在主菜单只核对 ready；旧返航 checkpoint tick `4808424` 因而理论上仍可覆盖已保存到 `6905142` 之后的黄糖世界。当前修复在 ticket 中加入生命周期/expiry/attempt tick，成功时封存、主档保存后 retire，并在启动时用票据内唯一 owned 主档的 header tick 识别已覆盖时间线。
- 直接证据：部署前同一主档再次保存到 tick `7198197`。新 Release 启动日志随后明确记录已载入 legacy flight ticket，并因精确 primary header 更新而将其 retire；主菜单和恢复后的 SessionState 均为 `flightCheckpointAvailable=false`，capabilities 不含 reload，且没有读取或选择其他存档。这补齐了“更晚主档淘汰旧 checkpoint”的 live 证据；新版本自身的 `flight_succeeded -> primary-save -> retired` 路径仍等下一次真实飞行复验。
- 限制或反例：启动时只读取 ticket 内唯一精确主档 header，不枚举存档、不解析内容；primary tick 新于 checkpoint 是单向 supersede 证据，不能反过来证明较旧 primary 可替代失败重试。当前条目在完整构建、测试和新 DLL 实机复验前保持 observed。
- 复验触发：本批 build/test、正常重启部署、session 不再暴露旧 checkpoint、下一次新 flight 成功/保存/重启完整闭环。
- 关联：EXP-005、EXP-047、EXP-048、EXP-069、EXP-079、EXP-082、`src/Spherewright.Plugin/RuntimeDescriptor/FlightCheckpointStore.cs`、`src/Spherewright.Plugin/Game/GameSessionTracker.cs`。
- 最近复验：2026-09-01（Release 部署后 legacy ticket 被精确主档 header 自动 retire，Session capability 双次复读均消失）。

### EXP-084 — 健康重启与隔离恢复必须选择不同的唯一载入源

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：一次性 owned-world resume ticket 的 planned restart 与 quarantine recovery 两种来源语义。
- 当前结论：健康 planned restart 已在签票前完成精确 primary save，因此只能加载 ticket 内 sealed high-entropy primary；fresh LastExit 不提供额外进度，反而可能由另一世界刷新。只有 quarantine recovery 为保留尚未正常保存的进度才使用 fixed LastExit。两类候选都必须在 prepare/commit 期间读取 header 并证明 `gameTick >= MinimumGameTick`，加载后仍做 owned identity/planet/peaceful/non-sandbox/1× 后验采用。消费则先写 token-hash 专属 durable tombstone，再 best-effort 删除双副本；启动从 runtime/handoff 中按最新 issued generation 选择并拒绝任一目录存在 tombstone 的 token，避免删除失败复活。
- 直接证据：旧 selector 的纯函数和测试明确无条件优先 fresh LastExit，coordinator 也只比较文件 mtime 后直接 `StartGame`；旧 `Consume()` 先清内存、后 best-effort 删除。当前源码已按 `QuarantineActionId` 分流候选、把 header tick 纳入纯 Core 选择器，并改成每 token 独立 tombstone 与双副本最新 generation 选择。
- 直接证据：Release 完整 solution 0 warning/0 error，Contracts/Core/MCP 共 `4 + 59 + 13 = 76` 项测试通过。部署后的 planned restart prepare 绑定 minimum tick `7198197`，commit 终态 message 明确为 ticket-bound primary owned save；fresh session 在 planet `104`、tick 不低于门槛、和平/非沙盒/1×、healthy，并立即重签下一 generation。旧 token 的 SHA-256 命名 tombstone 在 runtime 与 handoff 两处各有一个，删除结果不再是唯一消费证据。quarantine-only LastExit 分支尚未故意触发。
- 限制或反例：LastExit header 本身不含已证明的 high-entropy owned identity，quarantine 路径仍依赖加载后的严格采用；若 header/timestamp 任一不足必须拒绝。镜像副本不是文件系统跨目录事务，安全性来自同一 token generation、至少一个 durable replica 与全局 tombstone 拒绝，而不是声称两个 rename 原子同步。
- 复验触发：本批 Core selector 测试、双副本/删除故障注入测试、下一次健康 planned restart、下一次真实 quarantine、tombstone 删除失败模拟。
- 关联：EXP-005、EXP-038、EXP-064、EXP-069、EXP-071、EXP-072、`src/Spherewright.Bridge.Core/Safety/OwnedWorldResumeSourceSelector.cs`、`src/Spherewright.Plugin/RuntimeDescriptor/OwnedWorldResumeTicketStore.cs`。
- 最近复验：2026-09-01（健康 exact-primary 选择、header minimum、双 tombstone 与新 generation 均完成 live；quarantine 分支待自然触发）。

### EXP-085 — 满供电矿机没有资源节点时先判定矿脉耗尽，再用独立出料侧接存量主干

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：普通矿机、可分页资源节点读回、已有主带仍完整但上游矿簇已经耗尽的生产恢复。
- 当前结论：矿机 `isWorking=false` 且满供电并不自动等于输出端断线；若 `resourceNodeIds=[]`、内部无 mined-output，而其旧出料拓扑仍完整，应先通过资源节点列表确认附近是否仍有未占用同类矿脉。续采不必改写已有主带输入：可以在新矿机的原生输出口铺独立短带，再用空载分拣器从自由端侧向注入已有带的实际虚拟槽。验收必须贯穿新矿机缓冲下降/补充、侧接分拣器实际携矿、下游冶炼输入与最终产品仓增长，不能只看新矿机开始转动。
- 直接证据：旧铁矿机 `263` 满供电但 `resourceNodeIds=[]`、无缓冲，既有 `263 -> 267…274 -> 282 -> 17…20` 连接仍完整；同组铁脉 `1/3/6` 分别仍有约 `6224/1150/4014` 且 `minerCount=0`。新矿机 `796` 经原生资源建造覆盖三脉、接入网络 1，并在无出料带时先累积 31 铁矿。其后 `796 -> 806…805 -> 811…807 -> 812 -> 264` 由两段合法带路和一个高速分拣器组成；`812` 双端反查为 `807 -> 264` 且实际携带 item `1001`。20 秒现场窗内熔炉 `21` 恢复出铁，新钢炉 `791` 取得铁块并工作，专用仓 `792` 从 `0 -> 2` 钢材，磁悬浮研究 hash `24300 -> 24480`，完成从新矿脉到最终消费者的闭环。
- 限制或反例：`resourceNodeIds=[]` 只在实体读取、供电和局部资源列表都可信时支持“当前不再覆盖有效矿脉”；它不能单独区分耗尽、错误实体或未来 DSP 采矿实现变化。侧向注入也要求目标带主方向仍通畅、DSP inserter prepare 成功并在完工后扫描到真实连接槽；不得凭中心距离猜测可达。
- 复验触发：下一座矿机停产、任一矿簇自然耗尽、矿机资源 DTO 变化、侧接带背压或新铁/钢仓停止增长。
- 关联：EXP-028、EXP-051、EXP-066、EXP-067、EXP-068、EXP-070、EXP-073。
- 最近复验：2026-09-01（新矿机覆盖三条剩余铁脉，独立带侧接旧主干，钢材仓与科研 hash 同窗增长）。

### EXP-086 — 制造设备没有原生带口时，用自由短带和两端分拣器完成独立输入支路

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：普通制造台、设备输出到另一设备输入、现有端口密集且直接分拣器距离不足的短支路。
- 当前结论：制造台仍有空闲 sorter 槽不等于它暴露可绑定的原生 belt port；以制造台为 `sourceObjectId` 直接 prepare 传送带被原生端口校验拒绝时，不应覆盖旧端口或猜测提交。应在两台设备侧面先扫描并建造一条不绑定设备的自由短带，再分别对“源设备→首带”和“末带→目标设备”独立 prepare/commit。目标设备保持 recipe `0` 时，先配置各输入 sorter 的精确过滤，再启用配方；验收同时要求源输出下降/携货、目标输入增长、设备满供电工作、专用产物仓增长和 durable journal 事件。
- 直接证据：线圈制造台 `725` 虽有多个 sorter 槽，但所有 `sourceObjectId=725` 的 belt prepare 都返回 `BUILD_CONNECTION_INVALID`，说明它没有当前版本可用的原生带口。随后不绑定设备的 9 段自由带按真实方向形成 `824 -> 823…816 -> 818`；sorter `825` 把 `725` 的 item `1202` 送入首带，过滤为电动机的 sorter `815` 从仓 `727` 向制造台 `814` 供 item `1203`，过滤为磁线圈的 sorter `826` 从末带供 item `1202`，普通 sorter `828` 把 recipe `98` 的 item `1204` 输出到仓 `827`。启用配方后，`815/825/826/828` 均在真实工作或携货，制造台 `814` 满供电且双输入各读到 3，产物仓先从空仓增长到 8、随后到 22。日记 sequence `22` 在 tick `7414129` 记录首次自动电磁涡轮，并已 durable through `22`、无 pending/error；普通保存确认 tick `7419065`、revision `84`、写入健康。
- 限制或反例：这只证明当前 DSP 版本中普通制造台之间的 sorter–belt–sorter 中继；泵、矿机等具有专用原生带口的设备仍应优先使用其原生端口。自由带本身的 prepare 只证明路径合法，不能替代两端 sorter 完工后的设备端反向槽验证。目标设备一旦开始取料，配置 sorter 可能因运行态持续变化而 stale，所以过滤应在 recipe 仍为 `0`、sorter 空载时完成。
- 复验触发：下一条设备间短中继、不同制造设备类型、DSP belt-port 元数据变化、任一中继 sorter 停止携货或产物仓不再增长。
- 关联：EXP-011、EXP-020、EXP-042、EXP-067、EXP-073、EXP-079。
- 最近复验：2026-09-01（电动机与磁线圈双支路驱动 recipe `98`，电磁涡轮仓持续增长并越过 durable journal 与正常保存边界）。

### EXP-087 — 扩建资源链前先追踪现有矿机和有向带，优先复用真实自由端

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：长期运行世界中的原料扩建、旧矿机/旧带仍存在但交接文档没有完整记录、低效本地替代配方的阶段性生产。
- 当前结论：准备新矿机前先按资源节点的 `minerCount` 反查现有 miner，再从其原生输出沿 `isOutput=true` 逐实体追踪到真实自由端；只要矿机满供电、资源节点仍有效、主带有向连接完整，就应复用自由端，而不是凭文档缺项重复施工。旧侧支 sorter 可以失去设备端持有而主带仍然有效，必须分别判断。对于本地石矿转硅，只把 `10 石矿 -> 1 硅石 -> 高纯硅` 当作首座物流塔前的阶段性自动供应，不把它误称为跨星球硅物流的最终方案。
- 直接证据：准备第二座石矿机前，fresh Stone 节点列表显示 `197/201/202` 均已有 `minerCount=1`；反查得到矿机 `86` 覆盖三脉、network 1 满供电且内部有 50 石矿。沿真实有向连接追踪得到主带 `86 -> 87…92 -> 128…121`，其末端 `121` 仍是自由输出；旧支路 `88 -> 94` 的 sorter 只保留 belt 端、目标字段仍指向熔炉 `93`，不影响主带复用。新链为 `121 -> 844 -> 841(recipe 34) -> 845 -> 842(recipe 59) -> 846 -> 843`，电塔 `847` 补齐下游供电。矿机缓冲从 50 降至 37，熔炉 `841` 工作并向 `842` 送硅石，专用仓 `843` 从空仓增长到 5、随后 7 个 item `1105`。日记 sequence `26` 在 tick `7514032` 记录首次自动高纯硅并 durable through `26`、无 pending/error；普通保存确认 tick `7517473`、revision `169`、写入健康。
- 限制或反例：节点 `minerCount>0` 只证明当前有矿机覆盖，不能替代 miner 实体、资源集合、供电、缓冲和输出连接的复读。旧带 DTO 不显示带上货物，验收仍需上下游缓冲/产物变化。石转硅的 10:1 消耗很高，只适合首批处理器和物流塔；行星物流可用后应转向外星硅矿自动输入。
- 复验触发：任一交接不完整的矿种扩建、旧支路与主带状态不一致、石矿缓冲积压、高纯硅仓停止增长或行星物流上线。
- 关联：EXP-028、EXP-042、EXP-067、EXP-073、EXP-085、EXP-086。
- 最近复验：2026-09-01（复用矿机 `86` 与主带自由端 `121`，两级熔炼自动产出高纯硅并越过 durable journal/正常保存边界）。

### EXP-088 — 混合输入仓先锁定双过滤再装第二种物料，可安全驱动两原料制造台

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：一个普通 storage 同时承接自动中间产物和守恒转入的第二原料、两个 sorter 向同一制造设备供料。
- 当前结论：混合仓可以安全服务双原料配方，但必须在目标设备 recipe 仍为 `0`、两个 sorter 空载时分别配置精确 filter，并完成源仓与设备端双向槽验证；只有随后才把第二种物料装入仓并启用配方。验收不能只看设备工作，应同时证明两种源计数下降、两个过滤 sorter 只携各自物料、设备双输入及专用输出仓增长。
- 直接证据：高纯硅仓 `843` 自动接收 item `1105` 后，先建立 `843 -> 850(filter 1105) -> 848` 与 `843 -> 851(filter 1104) -> 848`，再建立输出 `848 -> 852 -> 849`；三个 sorter 完工后均由设备端反向持有。随后从既有自动铜仓 `26` 守恒转入 100 铜到 `843`，最后才把制造台 `848` 配置为 recipe `53`。现场中仓内铜 `100 -> 89`，高纯硅持续由上游补充并被 `850` 携带，制造台满供电且双输入为高纯硅 3/铜 2，专用仓 `849` 从空仓增长到 8、随后 15 个 item `1302`。日记 sequence `27` 在 tick `7542841` 记录首次自动微晶元件并 durable through `27`、无 pending/error；普通保存确认 tick `7545277`、revision `200`、写入健康。
- 限制或反例：本证据中的铜仍由现有自动仓经玩家做一次守恒搬运，不是物流塔级持续补给；高纯硅上游则为连续自动输入。若 filter 在装料后或 recipe 启用后才配置，sorter 的携货/进度变化会产生 stale 窗口，不能用本结论放宽配置前置条件。
- 复验触发：下一条双原料混合仓、加入第三种物料、仓持续并发输入、filter/slot DTO 变化或微晶元件仓停止增长。
- 关联：EXP-049、EXP-057、EXP-067、EXP-073、EXP-079、EXP-086、EXP-087。
- 最近复验：2026-09-03（原高纯硅/铜双过滤与 recipe `53` 保持有效；玩家现有 200 铜守恒续入后，混合仓仅按正确支路消费，制造台满电持续工作、专仓微晶元件 `16 -> 187`，没有重复首次事件）。

### EXP-089 — 三原料化工线应先完成空仓过滤和输出，再守恒装入自动来源

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：一个普通 storage 为三原料化工配方供料、多个 sorter 共享同一目标设备、原料由已有自动产物仓守恒中转。
- 当前结论：三原料化工线应在输入仓为空时先配置设备 recipe、建立专用输出，再逐条建立输入 sorter、设置精确 filter 并复读设备端反向槽；只有四条物流都完成后才装料。若某种原料没有现成 storage，可在纯物料主带上建立到普通仓的独立 sorter，先形成自动缓冲，再通过 normal transfer 守恒中转；不得把手采或直接写设备 buffer 当作产线输入证据。
- 直接证据：石矿主带 `87…128` 本身只有 item `1005`，新增 sorter `859` 从带 `128` 向旧仓 `95` 自动分流；仓内腾位后石矿由 10 增到 165、随后 411，形成可转移的自动石矿缓冲。化工厂 `861` 在空载时配置 recipe `24`，输出先接为 `861 -> 864 -> 863`；空输入仓 `862` 再依次建立 `865(filter 1114)`、`866(filter 1005)`、`867(filter 1000)`。过滤和双端槽完成后，才从纯油仓 `286`、自动石矿仓 `95`、水仓 `753` 分别守恒装入 120 精炼油、160 石矿、80 水。15 秒现场窗内三种源计数均下降，四个 sorter 满供电且只携各自物料，化工厂满供电工作，专用仓 `863` 从空仓增长到 7 个硫酸。日记 sequence `28` 在 tick `7661594` 记录首次自动 item `1116`（`2026-09-01T20:06:51.7681504+08:00`、本局 `001d 11:28:13`），并 durable through `28`、无 pending/error；普通保存确认 tick `7663628`、revision `283`、写入健康。
- 限制或反例：`859` 建成时目标仓仍满，已先从纯石矿带抓取 1 个石矿，因此空载 filter prepare 被正确拒绝且没有 commit；本例只因整条上游带由单一石矿机供给、实际携货也为 item `1005` 才保留无 filter 分流，不能推广到混料带。三种原料目前仍经玩家执行守恒中转，不是物流塔级连续补给。
- 复验触发：硫酸仓停止增长、石矿主带加入第二种物料、输入仓改为持续并发补货、任一 sorter 端点变化或物流塔接管原料。
- 关联：EXP-049、EXP-058、EXP-065、EXP-067、EXP-073、EXP-079、EXP-088。
- 最近复验：2026-09-03（有机晶体线复用同一空载过滤拓扑：输入仓原有 98 精炼油，新增 25 水和 50 自动塑料后，化工厂 `760(recipe 25)` 在 network 1/full service 自然完成 25 轮；最终塑料/水归零、油剩 73+设备 2，专仓精确得到 25 有机晶体。两段 normal transfer 随后把 25 件完整送入钛晶石线，未直接写设备缓冲）。

### EXP-090 — 旧混料带堵塞时，可用混合仓到目标设备的原生需求直供恢复关键链

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：研究/生产设备缺少当前明确需求、旧运输支路存在历史混料和回收竞争、普通混合仓仍保有足量正确自动产物。
- 当前结论：若旧混料带的永久修复会延误关键科技或产物，而普通混合仓里已有足量正确物料，可以先对“普通仓 -> 目标设备”做独立 sorter prepare/commit。DSP 目标设备的当前配方/矩阵需求会约束直连 sorter 的实际取料；完成后必须读回 sorter 双端、供电、实际携货、目标 buffer 和最终进度，不能仅凭连接成功推断恢复。这个规则仅限 storage 到设备的直接原生需求选择，不适用于先把混料送上 belt。
- 直接证据：处理器科技 `1302` 长时间停在 `44100/144000`；研究站 `84/679` 都有红矩阵但蓝矩阵为 0，蓝矩阵站 `76` 则因电路板输入为 0 停机。旧终端 `300` 同时接研究 sorter `312` 和回收 sorter `563`，`563` 实际携带电路板进入几乎满载的混料回收仓 `562`，上游 `311` 也停在携板插入态。普通仓 `26` 仍有 600 个既有自动蓝矩阵；新增 sorter `860` 经端点 prepare 后直接连接 `26 -> 84`，满供电运行。8 秒后研究站蓝矩阵内部缓冲由 0 增至 15700、设备恢复工作，科技由 `44100 -> 45479`，随后连续增至 `88189/144000`。第二个独立样本在同一混合仓与蓝矩阵生产站之间新增直连 sorter `868`；它实际只携 item `1301`，站 `76` 的电路板 buffer 由 0 增至 4、设备恢复工作，现有线圈支路随消费解堵，处理器科技继续由 `119237` 正常完成到 `144000`。两条旁路共同证明目标需求实际生效，而不是静态连接。
- 限制或反例：两个样本只覆盖研究站当前矩阵需求和 recipe `9` 的电路板缺口；不得据此宣称任意制造设备会替上游混料仓或混料 belt 筛选，也不得撤销 EXP-058。旧 `551 -> 542…300 -> 563 -> 562` 混线仍需单独清理；旁路 `860/868` 在目标配方或科技需求变化时都要重新复读实际携货。
- 复验触发：科技矩阵需求变化、仓 `26` 正确物料耗尽、sorter `860/868` 携非目标物料、目标设备再次停工或旧蓝矩阵产线被永久重构。
- 关联：EXP-049、EXP-054、EXP-058、EXP-059、EXP-073、EXP-079。
- 最近复验：2026-09-01（研究 sorter `860` 与生产 sorter `868` 两个独立直供样本共同恢复蓝矩阵供给并完成处理器科技；旧混料带仍明确保留为待修问题）。

### EXP-091 — 受科技门控的双原料产线可保持 recipe 0 完成全套预建，解锁后只启用一次

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：科技未解锁但制造设备、输入原料和物流设施已经可用的双原料制造配方。
- 当前结论：科技门控期间可以让制造设备保持 recipe `0`，先完成输入仓、两个精确过滤 sorter、输出 sorter、专用输出仓和供电；待运行时科技明确 `unlocked=true` 后，用 fresh 设备 hash 只配置一次目标 recipe。验收须同时证明科技正常完成、两种输入下降、设备双输入/工作/供电、输出 sorter 实际携货、专用仓增长以及 journal durable，预建本身不算产线完成。
- 直接证据：处理器科技 `1302` 未解锁时，制造台 `853` 始终为 recipe `0`；混合输入仓 `849` 先建立 `855(filter 1302)` 与 `856(filter 1301)`，输出预接为 `853 -> 857 -> 854`，电塔 `858` 覆盖整组。仓中守恒备好 100 电路板，微晶元件则由上游制造台 `848` 连续自动补充到 58。科技在 tick `7702430` 正常完成后，动作 `c6f09d62-a352-4e7d-862c-d740c2b1fe4f` 才把制造台配置为 recipe `51`。15 秒窗内微晶元件 `58 -> 45`、电路板 `100 -> 82`，制造台满供电工作且双输入各为 4，输出 sorter `857` 实际携 item `1303`，专用仓 `854` 从空仓增长到 6、随后 17。日记 sequence `29` 在 tick `7704459` 记录首次自动处理器（`2026-09-01T20:18:50.8692405+08:00`、本局 `001d 11:40:07`）；sequence `30` 随后记录应用型超导体 `1131` 首次选择，journal durable through `30`、无 pending/error。普通保存确认 tick `7707489`、revision `306`、写入健康。
- 限制或反例：当前电路板由既有自动仓做一次守恒备料，高纯硅仍来自临时 10:1 石转硅，因此只证明处理器转换线和科技门控顺序，不宣称行星物流级持续原料自动化。若预建期间误设 recipe 或先装混料再配置 filter，本结论不提供补救授权。
- 复验触发：下一条科技门控产线、处理器输入仓耗尽、recipe 解锁状态/设备 slot 变化、物流塔接管输入或处理器仓停止增长。
- 关联：EXP-049、EXP-067、EXP-073、EXP-079、EXP-087、EXP-088、EXP-090。
- 最近复验：2026-09-01（recipe 0 全套预建跨越科技解锁，单次启用后处理器连续增长并越过 durable journal/正常保存边界）。

### EXP-092 — 科技门控的双原料化工线可先锁过滤，再在交互半径内远程守恒装料

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：科技尚未解锁的双原料化工配方、一个混合输入仓、原料已有自动产物仓但尚无物流塔连续运输。
- 当前结论：化工厂可在 recipe `0` 时完成输入仓、两个精确过滤 sorter、输出 sorter、专用输出仓和供电预建；任何原料进入共享仓前，必须复读两个输入 sorter 的过滤项和设备端独立槽。科技明确解锁后只配置一次目标 recipe。原料仓与目标仓若都在玩家正常交互半径内，可以用玩家作守恒中转而不必走到设备中心；每一半转运仍必须绑定 fresh 玩家/仓库 hash，活跃目标仓只对明确写前 `STALE_STATE` 有界重绑。完成验收要求两种输入下降、输入/输出 sorter 实际工作、满供电、专用仓增长和 durable journal；远程装料本身不等于自动物流。
- 直接证据：应用型超导体 `1131` 未解锁时，化工厂 `869` 保持 recipe `0`；空仓 `870` 先建立 `873(filter 1109)` 与 `874(filter 1116)`，输出预接 `869 -> 872 -> 871`，电塔 `875` 把最初 network `0` 的输出 sorter 一并接入 network `1`，四个设备供电比均为 `1.0`。科技在 tick `7745153` 正常完成后，化工厂才单次启用 recipe `31`。主石墨仓 `114` 的 180 个自动高能石墨经正常转运进入 `870`；硫酸仓 `863` 的 60 个自动硫酸也守恒进入同仓。投料后 15 秒内仓内石墨 `173 -> 154`、硫酸 `59 -> 50`，设备曾明确处于 working，sorter `872/873` 现场携货，专用仓 `871` 从空仓增长到 14 个石墨烯，设备输出槽另有 1。日记 sequence `32` 在 tick `7849705` 记录首次自动 item `1123`（`2026-09-01T20:59:13.2038998+08:00`、本局 `001d 12:20:28`），并 durable through `33`、无 pending/error；正常保存确认 tick `7854029`、revision `403`、写入健康。
- 限制或反例：高能石墨和硫酸仍由玩家在正常交互半径内做一次守恒搬运，因此只证明自动化工转换，不宣称持续原料物流。最终读回恰好位于设备等待下一组三石墨的瞬时边界，`isWorking=false` 但输出 sorter 正在搬运、输入 sorter 正在送石墨、仓和 journal 已增长；不能把单 tick `isWorking` 当作唯一产出判据。若目标仓不再处于交互半径、过滤项或设备槽变化，必须停止并重新规划，不能扩大范围或直接写 buffer。
- 复验触发：物流塔接管两种原料、输入仓自然耗尽、第三种物料加入、sorter 过滤/槽变化、专用仓停止增长或下一条科技门控化工线。
- 关联：EXP-049、EXP-067、EXP-073、EXP-079、EXP-088、EXP-089、EXP-091。
- 最近复验：2026-09-01（recipe 0 预建跨越科技解锁，双过滤后远程守恒装料，石墨烯增长并越过 durable journal/正常保存边界）。

### EXP-093 — 未知地形长途先用非带实体构造陆地锚点链，跨水只把已验证对岸作为连续终点

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：密集工厂与水面混合地形中的普通地表移动、已有大量风机/电塔/生产设备可供只读定位、跨区域取料。
- 当前结论：直达球面分段在未知地形中失败后，不应继续猜同一弧线。分页读取 factory entities，排除 belt/inserter 后可用实体位置构造近邻/最小瓶颈图，把风机、电塔和生产区作为候选陆地链；每个短锚点仍以 `arrivalTolerance=4–5 m` 停在基座外，并在下一段前复读 Walk、速度、能量。密集设备正好落在直线上时，先按最近碰撞体的局部切向背离约 5 m，或取设备北/南侧约 6 m 的切向绕点。若图上两片陆地之间只剩一个大缺口，目标应直接绑定已验证的对岸陆地锚点，让同一个受控订单穿过水面并最终稳定 Walk；把水面中点当作分段终点会制造不必要的 Drift 恢复。到达一个同时覆盖源/目标 storage 的正常交互位置后即可完成守恒转运，不必再穿越最后一片密集厂区。
- 直接证据：从新石墨烯区直达石墨仓 `114` 的首段先被矿机 `263`/带组卡停；退回已验证 Walk 点后，另一条分段路线把约 22 m 的中点放在水面，玩家进入 Drift 并立即停下。恢复只对明确写前 `STALE_STATE` 有界重绑，第二轮第 12 次才取得唯一实际 commit 并回到 Walk。对 135 个非带/非分拣器节点做最小瓶颈追踪后，陆地链为当前区 → 风机 `82` → 电塔 `750` → 制造区 `724/723` → 风机 `130` → 电塔 `133` → 原油区 `129/141` → 火电站 `183`；设备直线相交处分别用侧移和设备北侧约 6 m 切向点绕过。`183 -> 713` 的最小陆地缺口约 `50.76 m`，直接以风机 `713` 为终点的单订单去返两次都稳定落地，能量未逼近阈值。返程在风机 `130` 外侧读得目标输入仓 `870` 距 `65.91 m`、硫酸仓 `863` 距 `64.51 m`，随后无需返回密集新厂区便完成 180 石墨与 60 硫酸的守恒装料。
- 直接证据：同一旧锚点链在物流等待期已因新增工厂布局部分失效：前往电塔 `750` 的长订单先被仓 `827`/制造台 `814` 夹缝截停；`143 -> 182` 方向又被位于连线附近的矿机 `129`/钛晶石设施环截停。fresh 几何找到的一个非带实体最小中心净距约 `8.17 m` 的候选仍落入 Drift，说明图搜索只能生成“待原生验证”的候选，不能同时替代碰撞和地形判定。失败动作均由精确订单看门狗或首次 Drift 断路，没有继续沿旧白名单推进。
- 直接证据：第二座 PLS 取钛时，玩家从 `768/769/767/130/133/129/761` 密集环内出发，直达仓 `531` 的切向方向明确穿过油井 `129`。先用 4 m 局部背离和 8 m 后撤把最近中心距由约 5.1 m 扩到可规划窗口，再把 917 个 factory entity 投影到当前位置切平面，排除 belt/inserter 后按建筑类型保守膨胀为障碍圆。首条局部路线只把几何结果当候选，依次经约 `(-4,-6) -> (-4,-12) -> (-2,-14)` 三个切平面窄口；对应 Move 均以原生 Walk、速度 0、核心满电终止，最终 fresh revision 为 `44`。这证明局部障碍搜索可减少向同一油井反复顶撞，但仍须逐段由原生碰撞/地形终态裁决。
- 直接证据：同一路线随后给出地形反例与改进正例。仅按建筑障碍圆选出的下一个 4 m 候选虽以“到达坐标”结束，2 秒复读却为 Drift；动作没有被当成安全落点，立即回到上一精确 Walk 坐标。分页扫描本星全部 `15444` 个 vegetation 节点后，以草地 Detail 节点 `16230` 作为可证明陆地终点；从回收点到该节点的单个 109.24 m 球面订单避开所有非带设备（最小中心距约 5.48 m），途中不制造水面停靠点，最终稳定 Walk/速度 0、核心满电，并进入钛仓 `531` 的 38.31 m 交互范围。返程再以草地节点 `15416` 为终点走 34.43 m，稳定停在目标仓 `899` 的 61.10 m 范围内。vegetation 只证明目标地面可承载天然物，不证明整段无水；成功仍来自同一订单最终原生落地。
- 限制或反例：factory entity 存在只说明建筑落位，不单独证明其每一侧可行走，也不证明相邻锚点连线全是陆地；首个新锚点仍可能进入 Drift，必须沿 EXP-053 立即停下。最小瓶颈图只优化实体间最大间距，不是地形寻路器，也不考虑建筑碰撞半径；每段原生移动和现场状态仍是最终裁决。`50.76 m` 与约 `80 m` 交互范围都是当前 planet `104`/当前运行时样本，不能硬编码成跨星球常量。
- 复验触发：下一次跨区域取料、不同星球半径/海洋、实体图出现多条候选链、连续跨水订单失败、交互范围变化或物流塔上线后不再需要玩家中转。
- 关联：EXP-035、EXP-039、EXP-051、EXP-053、EXP-057、EXP-073、EXP-092、`scripts/invoke-surface-route.ps1`。
- 最近复验：2026-09-03（从无线塔恢复满电后，旧陆地链 `180 -> 82 -> 133 -> 129 -> 141` 仍逐段 Walk/0 成立；`141 -> 183` 直线被新布局卡住，只在退回 fresh Walk 点后重算 6 m 侧绕点，随后稳定到达 `183` 外缘。旧图仍是候选骨架，不是免复验白名单）。

### EXP-094 — 配方可在科技门控期预建，但未解锁物品的 sorter filter 必须延后

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：目标配方保持 recipe `0` 的科技门控预建，其中至少一种输入物品本身由尚未完成的科技解锁。
- 当前结论：空设备、空仓、端点和供电仍可按 EXP-091 预建；已经存在且可用的输入物品也可先设置精确 sorter filter。但若某种输入物品本身尚未解锁，`sorter-filter` prepare 会把它视为当前不可用，不能为了完成预建而绕过校验、提前装料或重复提交。应保留该 sorter 为空载 recipe `0`，等待科技结构化读回 `unlocked=true` 后 fresh inspect，只补这个缺失过滤，再装入对应物料或启用下游配方。
- 直接证据：物流运输机制造台 `891` 在 recipe `94` 尚未解锁时保持 recipe `0`；空输入仓 `892` 的 sorter `895/896` 分别成功设置铁块 item `1101` 与既有自动处理器 item `1303`。第三条空载 sorter `897` 请求推进器 item `1405` 时，推进器科技 `1113` 尚未完成，prepare 明确返回 `ACTION_REJECTED: requested filter item is unavailable`，没有 action ID、没有 commit；fresh 复读确认前两个 filter 已保留，`897` 仍为空载 `filterItemId=null`，全部双端槽保持完整。科技 `1113` 在 tick `8098237` 解锁并完成推进器里程碑后，对同一空载 `897` 的 fresh prepare/commit 成功，过滤项读回为 `1405`，阶段仍为 `Picking`、携货 `0`，连接仍是 `892 slot 4 -> 897 -> 891 slot 2`，写入健康。
- 直接证据：同样的延后过滤边界在行星物流站预建上再次成立。粒子容器 recipe `99` 解锁并自然产出 20 个 item `1206` 后，先通过正常双段 transfer 把它们从仓 `885` 守恒送入仓 `899`；随后动作 `57e3dc7d-2af9-4f35-9657-ce64599e50e7` 才把空载 sorter `905` 从无过滤配置为 `1206`。fresh 读回仍是 `899 slot 7 -> 905 -> 898 slot 10`、阶段 `Picking`、携货为空、目标制造台 recipe `0`，写入健康。
- 限制或反例：目前只有推进器样本同时覆盖“解锁前拒绝、解锁后成功”；粒子容器样本只独立复验了解锁后的成功路径。拒绝也使用通用的 idle/empty/unavailable 文案，不能外推为所有科技、建筑或过滤 UI 的固定错误码。若物品已解锁而仍拒绝，必须分别检查 sorter 是否空载、是否空闲、端点是否变化，不能一律归因于科技。
- 复验触发：推进器 `1113` 解锁后对同一 sorter `897` 成功设为 item `1405`、下一种锁物品过滤、错误契约细分或 sorter UI 行为变化。
- 关联：EXP-021、EXP-062、EXP-074、EXP-091。
- 最近复验：2026-09-02（推进器与粒子容器两个锁定物品均只在结构化解锁/产出后配置，fresh 双端和空载状态保持不变）。

### EXP-095 — 新设备投产后要把电网容量纳入产线验收

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：科技门控预建的制造台在解锁后启用，以及同一电网已有较多并发生产负载的现场。
- 当前结论：recipe `0` 预建仍应等科技结构化解锁后只启用一次；但“设备有电网 ID”不等于满功率，首产验收必须同时读取电网总需求/容量与设备 `powerServeRatio`。若新增制造台把既有网络推入欠供电，应先用正常手搓和原生建造补足发电，再以消费者供电比 `1.0`、输入下降、输出增长、durable journal 和普通保存共同闭环，不能把降速首产当成长期健康状态。
- 直接证据：推进器科技 `1113` 在 tick `8098237` 正常解锁后，预建设备 `876` 才由 recipe `0` 单次配置为 recipe `20`；拓扑为 `877 -> 880(filter 1103)/881(filter 1104) -> 876 -> 879 -> 878`。首个 18 秒窗内输入仓钢材 `120 -> 106`、铜块 `180 -> 164`，专用仓 `0 -> 4 -> 10`，随后增至 55、最终复读为 60 个 item `1405`；日记 sequence `34` 在 tick `8101277` 记录首次产线推进器（`2026-09-01T22:09:09.3196038+08:00`、本局 `001d 13:30:21`）并 durable。启用时 network `1` 仅有 `80000` 容量而需求约 `102622`，设备供电约 `0.825`；普通手搓并原生建造风机 `910–914` 后，容量升至 `105000`，消费者供电比和制造台供电比均为 `1.0`。普通保存确认 tick `8123715`（保存后复读 `8123723`）、revision `639`、写入健康。
- 限制或反例：当前五座风机的瞬时总发电随风能读数变化，`105000` 是额定容量而非每 tick 恒定出力；后续继续扩建前仍要 fresh 读整个网络，不能永久复用本次余量。输入仓后来耗尽而制造台 idle 是正常终态，不推翻已完成的输入下降与连续产出证据。
- 复验触发：下一台高功耗制造设备、network `1` 再次出现供电比低于 1、发电设备被拆除/失联、产线长期停顿或物流站投运。
- 关联：EXP-017、EXP-019、EXP-023、EXP-062、EXP-091。
- 最近复验：2026-09-01（推进器首产后发现全网容量不足，补五座风机恢复满供电，产物增至 60 并越过 durable journal/普通保存边界）。

### EXP-096 — 满仓是槽位语义；新 sorter 可能在过滤前预取混料

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：双产物精炼厂汇入混料仓、试图把混料仓自动分流到两个普通储物仓，以及目标仓接近或达到槽位上限。
- 当前结论：普通储物仓的可插入性按空槽/同物品堆叠共同决定，不能只看聚合总数或“600/600”就推断任意物品都无法进入。新建无过滤 sorter 一旦完工会立即按源仓当时可取物预取；之后即使目标暂时不能接受，它也可能长期持有该物品，使 sorter 不再满足空载过滤前提。永久分产必须在源仓稳定空、目标仓按物品留有容量且所有 sorter 均 `stack=0` 的 fresh 窗口中，先分别配置精炼油/氢过滤，再恢复流量；不能依赖“先建无过滤、以后再改”。
- 直接证据：源仓 `163` 曾同时残留精炼油/氢。纯油中继 `784` 读到聚合 600 后，新 sorter `906` 仍先预取 1 氢并等待；只取 1 油没有形成空槽，取满一整栈 20 油后才开放一个格，sorter 随即把氢填入该格并改为手持 1 油。尝试捕获动态空窗时，候选 hash 在 prepare/commit 间变化而被正常 stale/reject，没有放宽校验。临时混料缓冲 `907` 与并行 sorter `908/909` 随后将源仓排空；两条空载 sorter 已分别在有界 fresh prepare 重试后成功设为 filter `1120`。中继中的 62 氢又经一次 exact transfer 守恒移入玩家，虽然 commit 后的空集合 `.Sum` 展示报错，fresh 复读仍明确为中继氢 `0`、玩家氢 `168`，没有重放。进一步反查当前设备图发现，`707 -> 709(filter 1114) -> 163` 是唯一现役输入，氢由 `707 -> 708(filter 1120) -> 170` 和 `141 -> 181(filter 1120) -> 170` 在上游分离；因此 `163` 当前新流入其实是纯油，原混料是有限历史残留。下游 `790` 也保持 filter `1114`，但 `907` 仍是混合库存、玩家仍暂存氢，sorter `906` 尚未取得空载配置窗口，故仍未宣称整理全部完成。
- 限制或反例：本样本没有完成 sorter `906/908/909` 的最终双过滤与两仓纯度验收，不能把临时缓冲视为完成的生产物流。活跃双精炼厂会继续改变源仓 hash；过滤前仍须逐个 fresh 读 sorter 的 `filterItemId/inserterStage/inserterStackCount`，任何非空载对象都要先自然送达或守恒清理。
- 复验触发：`163` 持续为空的稳定窗口、三个 sorter 全部空载、`784/907` 分别完成纯油/纯氢清理，或改用液罐/物流塔原生物品过滤。
- 关联：EXP-056、EXP-065、EXP-074、EXP-078、EXP-094。
- 最近复验：2026-09-01（600 聚合满仓仍因槽位变化接受异物；泄压后确认现役上游已按油/氢过滤，中继氢清零且两条临时出口锁氢，但历史混料清仓与 sorter `906` 过滤仍待收敛）。

### EXP-097 — 物流塔观察必须交叉绑定实体、站点池和星球身份，并区分实时与配置指纹

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、Assembly-CSharp SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`、v0.3 行星/星际物流塔只读观察。
- 当前结论：不能只用工厂实体 ID 或 `EntityData.stationId` 单点认领物流塔。读取必须同时证明正实体仍存在、stationId 位于当前 `PlanetTransport.stationPool/stationCursor`、池项 `id` 等于索引，并且池项的 `entityId/planetId` 与当前实体和本地工厂一致；随后只在 Unity 主线程把站点能量、舰队、原始运输设置、每个 `StationStore` 和 `SlotData` 深拷贝到 DTO。实时能量/库存/订单/舰队变化与配置变化采用两个独立哈希，避免未来配置 prepare 因正常运输 tick 无意义失效，同时仍能识别槽位、供需和带口被改动。
- 直接证据：当前程序集元数据确认 `PlanetFactory.transport -> PlanetTransport.stationPool`、`EntityData.stationId`、`StationComponent.id/gid/entityId/planetId/storage/slots` 和 `StationStore`/`SlotData` 的完整字段；源码已在现有 factory list/inspect 响应中加入 `logisticsStation` 深拷贝，并用 Core 测试证明仅实时 count/energy 变化不改变 configuration hash、槽位上限变化会改变它。Release 完整 solution 为 0 warning / 0 error，78 tests passed（Contracts 5、Core 60、MCP 13）。修复本地站 0 哨兵身份后，实体 `916` 在当前版本连续返回 entity/station/planet、4 个槽、12 个带口、能量、供电、无人机容量及三类独立哈希；后续自然充电、槽位配置、充电上限配置和 fleet 往返均只改变各自应变的字段/哈希。
- 限制或反例：当前只完成本地 PLS 的 live 观察；星际站、运输船、在途订单与带口实际接入仍待后续样本。`tripRange*`、`warpEnableDist`、`delivery*` 继续按 raw/setting 暴露；其 UI 缩放虽已完成源码复核，路线参数写入仍未实现或授权。
- 复验触发：同档首座行星物流站完工、首座星际物流站完工、首次站点槽位配置、运输机/运输船活动、UI 缩放语义完成反编译或 DSP/程序集版本变化。
- 关联：`src/Spherewright.Contracts/Logistics/LogisticsStationContracts.cs`、`GameStateReader.CaptureLogisticsStation`、`CanonicalStateHash.LogisticsStation*`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-02（实体 `916` 完整 DTO、自然充电、槽位/充电配置和无人机 fleet 往返均已结构化 live 复读）。

### EXP-098 — 物流塔槽位配置只能采用 SetStationStorage 的不换品子集

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、行星/星际物流站的物品选择、容量上限和本地/远程供需配置；不覆盖轨道采集器、矿机型物流站、直接装货或路线参数。
- 当前结论：`PlanetTransport.SetStationStorage(...)` 是当前 UI 选择物品、拖动容量和切换供需逻辑共同使用的业务方法，但它不是无条件安全的“配置 setter”。当目标 item 与原槽不同且原槽有货时，它会调用 `Player.TryAddItemToPackage(..., throwTrash:true)`，随后清空 count/inc/orders，背包不足还可能产生地面掉落。因此 Spherewright 只允许空槽选品或同 item 改配置，永不暴露清槽/换品；item 必须正常解锁、不与同塔其他槽重复，容量必须为当前科技容量内的正 100 步进，行星站 remote 必须 `none`，且槽位不能有未结订单。prepare 绑定独立 configuration hash，避免正常能量/运输 tick 导致无意义 stale；commit 前再次验证，调用一次后要求 item/max/logic 精确命中，并证明槽 count/inc 及背包/手持每个 item/count/inc 元组均未改变。
- 直接证据：`UIStationStorage.OnItemPickerReturn`、`OnMaxSliderValueChange`、`OnOptionButton*Click` 均反编译到同一 `SetStationStorage` 调用；当前方法体证明最大值会按 model capacity + 已研究 bonus 截断、行星站强制 remote None、换品分支会退货/可能 throwTrash、同 item 分支只改 max/localLogic/remoteLogic。源码已将 `logistics-station-storage` 接入既有 configure prepare/commit，Contracts/MCP 映射测试覆盖独立配置哈希和槽位意图；完整 Release solution 0 warning / 0 error，80 tests passed（Contracts 6、Core 60、MCP 14）。
- 实机复验：实体 `916` 的空槽 0 以动作 `d6937f52-c03e-4045-93ac-5025b7ebdba1` 一次配置为钛块 `1106`、上限 `100`、本地需求、远程无；fresh 读回 `count/inc/order=0`、其他三槽仍空、玩家背包和写健康不变。随后充电配置和三次 fleet 转移后，该槽仍精确保持上述配置与空库存。
- 限制或反例：当前只 live 验证“空槽首次选品”的最窄子集；同 item 且已有库存时调整上限、已有真实订单时的拒绝、保存后的重启恢复持久性和星际站 remote 逻辑仍待验证。充电、航程、最低配送、补充开关、分组和路线优先级不包含在本动作。
- 复验触发：首座行星物流站完成后的空槽首次配置、配置后装货、保存/恢复后配置持久、同 item 调整上限、任何背包/槽位差异或 DSP/程序集版本变化。
- 关联：EXP-021、EXP-097、`docs/research/game-api-m0.md`、`BuildingConfigurationModes.LogisticsStationStorage`。
- 最近复验：2026-09-02（PLS `916` 空槽 0 的钛块/100/本地需求首次 prepare、commit 与多轮 fresh 不变量读回通过）。

### EXP-099 — 物流塔 energyPerTick 是实时请求而非充电上限，配置值在 PowerConsumer

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：物流塔充电读取、实时/配置哈希拆分，以及未来最大充电功率 prepare；DSP `0.10.34.28529`。
- 当前结论：不能把 `StationComponent.energyPerTick` 命名或哈希为稳定充电配置。`StationComponent.SetPCState(PowerConsumerComponent[])` 每 tick 根据 `energy/energyMax` 调用 consumer 的 `SetRequiredEnergy(...)`，随后把 `consumer.requiredEnergy` 复制到 `station.energyPerTick`；塔接近充满时它会自然变化。UI 的最大充电滑块读写的是同一 `pcId` 对应 `consumer.workEnergyPerTick`，显示功率为该值乘 60。DTO 必须分别暴露 current requested energy/power 与 configured maximum energy/power，实时哈希包含前者，配置哈希只包含后者。
- 直接证据：当前程序集方法体明确显示 `SetPCState` 的赋值顺序；`UIStationWindow._OnOpen` 以 `workEnergyPerTick/50000` 初始化滑块并显示 `workEnergyPerTick*60`，`OnMaxChargePowerSliderValueChange` 写 `round(50000*slider)`。源码已把旧 `EnergyPerTick` 拆为 `RequestedChargeEnergyPerTick/PowerWatts` 和 `MaximumChargeEnergyPerTick/PowerWatts`，并扩展 Core 测试证明 requested 变化只改变 live hash、maximum 变化必定改变 configuration hash；完整构建/80 tests 仍通过。
- 实机复验：PLS `916` 在未接电时为 `0/180 MJ`；电塔 `917` 接入 network 1 后自然充到 `147470607/180000000`，当时 requested 为 `2814780 W`、maximum 仍为 `12000000 W`；充满后 energy 保持 `180000000`、requested 降为原生保底 `60000 W`。随后只把 maximum 改为 `6000000 W`，energy/requested 未被写入，证明实时请求与配置上限是不同状态。
- 限制或反例：当前只覆盖一座 PLS 的低电到满电和一次最大功率调整；`workEnergyPerTick` 的 UI min/max 仍取决于具体建筑 prefab 的 `workEnergyPerTick / 2` 到 `*5`，不能把 PLS 数值外推到 ILS 或接受任意瓦数。
- 复验触发：首站从低电量充满的连续采样、调整最大充电功率、保存恢复、consumer/network 身份变化或 DSP 版本变化。
- 关联：EXP-017、EXP-097、EXP-098、`StationComponent.SetPCState`、`UIStationWindow.OnMaxChargePowerSliderValueChange`。
- 最近复验：2026-09-02（PLS `916` 从 0 经 network 1 自然充满，并独立完成 12 MW→6 MW 配置；requested/maximum live 分离得到实证）。

### EXP-100 — 选科技必须使用不含自然科研上传量的专用状态哈希

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：研究站正在持续上传 hash 时，通过 `get_progression_state -> prepare_select_research -> commit_select_research` 向 DSP 原生队列追加后续科技。
- 当前结论：完整 progression 哈希适合精确观察，但不能直接作为只修改队列的并发指纹，因为其中的 `HashUploaded` 会随正常科研每 tick 增长。专用 `selectionStateHash` 应绑定 session/planet、当前科技、队列顺序、科技解锁/等级/所需 hash、实验室/排队分类和前置科技，排除自然增长的上传量；prepare 与 commit 仍必须分别调用 `GameHistoryData.CanEnqueueTech`。这样只消除与决策无关的活跃科研竞争，不放宽队列、解锁或前置条件的 stale 校验。
- 直接证据：当前同档在粒子磁力阱 `1703` 持续研究时，先进行 1 次普通 fresh inspect/prepare，再进行有界 80 次 fresh 重试，全部在 commit 前返回 `STALE_STATE`；期间队列始终只有 `[1703]`，没有 action ID、没有 commit、没有写副作用，研究则自然推进。源码定位到旧 `CanonicalStateHash.Progression` 串入 `HashUploaded`，现已新增独立 selection hash 并把 MCP 参数、plan payload、prepare/commit 复验全部迁移；Release solution 0 warning / 0 error，83 tests passed（Contracts 7、Core 61、MCP 15）。测试证明上传量变化只改变完整哈希，而队列或解锁变化必定改变 selection hash。
- 实机复验：正常关停部署后，粒子磁力阱 `1703` 仍停在 `242820/288000` 且保持当前科技；fresh progression 同时返回专用 `selectionStateHash`。动作 `e54dddc4-aac3-40a8-a3bd-beacae3c80c9` 用该哈希一次 prepare/commit 成功把 `1604` 追加到 DSP 原生队列，fresh 复读由 `[1703]` 精确变为 `[1703,1604]`，当前科技仍为 `1703`。这证明活跃/未完成科技的上传字段不再令纯队列决策无关 stale。
- 实机复验：`1604` 持续上传到至少 `22643/144000` 时，两个新的 fresh selection hash 动作 `aeea55dd-bc4b-4770-abb1-bd7f0917ce41`、`273c2ed7-f605-407d-8838-83eaa6056f9a` 又依次一次成功追加 `1114/1414`。队列精确经历 `[1604] -> [1604,1114] -> [1604,1114,1414]`，当前科技和上传均未被中断；日记 sequence `39/40` 已 durable 且无 pending/error。
- 限制或反例：专用哈希只排除 `hashUploaded` 自然增长；队列顺序、当前科技、解锁、等级、前置或 `CanEnqueueTech` 变化仍必须 stale。当前 `1604` 持续上传窗口已补齐这一正例，但尚未用真实并发队列变更制造 commit-time stale 反例，也不能把成功追加外推为任意前置未满足科技都可入队。
- 复验触发：下次安全部署、活跃研究期间追加 `1605` 或其他后续科技、DSP `CanEnqueueTech` 行为变化、科技等级/前置规则变化。
- 关联：EXP-063、EXP-074、`CanonicalStateHash.ProgressionSelection`、`PrepareSelectResearchRequest.ExpectedSelectionStateHash`。
- 最近复验：2026-09-02（在两段独立活跃科研窗口中连续三次使用专用哈希追加后续科技，队列与当前研究均精确保持）。

### EXP-101 — 物流塔最大充电功率要绑定 prefab UI 刻度与 power consumer 身份

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529` 的行星/星际物流站最大充电功率；不覆盖当前实时请求功率、充电能量注入、采集器或其他运输参数。
- 当前结论：`StationComponent.energyPerTick` 不能写作配置；最大值只位于 station `pcId` 对应的 `PowerConsumerComponent.workEnergyPerTick`。安全动作必须同时绑定 entity/station/planet、`station.pcId == entity.powerConId`、consumer `id/entityId` 和具体 item prefab。输入使用 UI 显示瓦数，只接受 3 MW 整步进，并按 UI 的整数范围限制在 `prefab workEnergyPerTick` 的 0.5×–5×。prepare 绑定独立 configuration hash；commit 只执行与 `UIStationWindow.OnMaxChargePowerSliderValueChange` 相同的 field assignment，并立即证明 maximum readback，同时保持 consumer identity/current required energy、station requested energy/存能/全部槽字段以及玩家背包不变。
- 直接证据：当前程序集反编译确认 UI 打开时把 slider `min/max/value` 分别设为 `(prefab/2)/50000`、`(prefab*5)/50000`、`consumer.workEnergyPerTick/50000`，回调只写 `round(50000*value)`，显示为 `round(3000000*value)` W。源码新增 `logistics-station-charge` 模式、UI 范围纯函数、交叉身份与同主线程不变量读回；Core 测试覆盖 6/12/60 MW 合法和 7/63 MW 拒绝，Contracts/MCP 覆盖显式 watts 与 configuration hash。Release solution 0 warning / 0 error，86 tests passed（Contracts 8、Core 62、MCP 16）。
- 实机复验：满电 PLS `916` 以动作 `cd332730-12ef-45a2-947f-0716a4eb9ca6` 一次把 maximum 从默认 `12000000 W` 调为合法 UI 刻度 `6000000 W`。即时和 fleet 往返后的 fresh 读回均保持 `6000000 W`，同时 energy `180000000`、requested `60000 W`、钛块槽全部字段、其余槽、玩家物品和写健康不变。
- 限制或反例：当前只验证 PLS 的 12→6 MW 单步配置，保存后的重启恢复持久性、ILS prefab 范围及其他合法刻度仍待验证。无人机航程、运输船航程、曲速距离、最低配送和自动补充仍保持只读。
- 复验触发：首座物流站完工、首次 charge prepare/commit、塔从低电充满、保存恢复、prefab 默认功耗或 DSP UI 变换变化。
- 关联：EXP-017、EXP-097–099、`LogisticsStationChargePolicy`、`UIStationWindow.OnMaxChargePowerSliderValueChange`。
- 最近复验：2026-09-02（PLS `916` 的 12 MW→6 MW prepare/commit、即时读回及后续跨动作不变量通过）。

### EXP-102 — 活跃分拣器过滤应绑定配置指纹与零携货，不绑定返程进度

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、已连接非空源容器的普通分拣器、`sorter-filter` 的 inspect/prepare/commit 指纹与混料回收；不允许带货修改过滤。
- 当前结论：`Factory` 完整哈希包含每 tick 变化的 `InserterComponent.time/stage`，并且旧安全检查只接受 `stage=Picking,time=0`；当源容器持续可取料时，分拣器一返回就会立即再取货，客户几乎无法跨请求捕获稳定窗口。当前版本 UI 的 `UIInserterWindow.OnItemPickerReturn` 本身只写 `filter` 和实体 sign，不查 `stage/time`。Spherewright 采用更窄且可证明的子集：连接双端存在，prepare 和 commit 时 `itemId/itemCount/stackCount/itemInc` 全部为 0；配置哈希绑定实体、旋转、双向拓扑、当前 filter 和携货缓冲，但排除无副作用的空载 `Returning` 进度。若 prepare 后又取到任何物品，指纹和独立零携货检查都会使 commit stale，不会改 filter。
- 直接证据：研究供料仓 `26` 的 244 个自动蓝矩阵在给 `26 -> 860 -> 84` 设为蓝矩阵过滤后从仓内消失，但两座研究站蓝缓冲仍为 0、红缓冲精确不变、科技仍为 `242820/288000`，证明蓝矩阵没有被研究消耗。拓扑逐段复读发现同仓的无过滤出口 `551 -> 542…300`，末端只有通往蓝矩阵生产站的 sorter `312` 和回收仓 sorter `563`；`563` 因回收仓满载而手持 1 铜，`551` 也因环带堵塞手持 1 铜。先后动作 `cd89fa7f-7dad-484f-8b70-367eb414de8b`、`2ba39691-f644-4414-b41a-c50dddb44fcb`、`58187c19-e898-49b5-8419-d3f0226fdff9` 把回收仓原有 293 蓝、两批各 100 铜守恒暂存到玩家；随后回收仓铜 `801 -> 869`、继续增长，证明环带在原生排空。当前 belt DTO 不读 cargo path，因此“244 蓝全部仍在环带”只是拓扑与守恒下的待复读推断，不先写成已回收事实。
- 直接证据：当前程序集反编译确认 `UIInserterWindow.OnItemPickerReturn(ItemProto)` 只执行 `inserter.filter=item.ID`、`sign.iconId0=item.ID`、`sign.iconType=1`，没有 stage/time/cargo 分支；源码复核确认旧 `CanSetSorterFilter` 额外要求 `Picking/time=0`，而完整工厂指纹又包含 progress/stage。修复源码已新增 `FactoryConfiguration`、`SorterFilterPolicy` 与 prepare/commit 双重复验；离线测试要求空载 Returning/Picking 共用指纹，filter/拓扑/携货任一变化必须改变指纹。完整 Release solution 已为 0 warning / 0 error，90 tests passed（Contracts 10、Core 64、MCP 16）。
- 实机复验：正常保存 tick `8640914` 后完成正常关停、停机部署并用受保护票据恢复同一主档。新 DLL 对 sorter `551` 的首次 fresh 检查恰逢 `Inserting/stack=1`，客户端没有 prepare；随后在 `Returning/stack=0` 窗口用配置指纹准备并提交动作 `6c65e8d4-d7cc-45f8-bf85-effbe75f7c87`，终态成功，fresh 复读保持拓扑 `26 -> 542` 且过滤精确为蓝矩阵 `6001`。这同时证明“活跃但空载返程可配置”和“带货不进入准备”两个边界。
- 实机复验：星际物流科技完成后，电路板主带到蓝矩阵站的 sorter `573` 在 `Picking/stack=0` 窗口由动作 `58e89d75-09dd-44fd-b227-a0834f0a03b8` 设置为只取铜块，随后仓 `26` 到同一蓝矩阵站的 sorter `868` 先被读到正在携带 1 个电路板，没有提前 prepare；等其自然放货、重新读到零携货后，动作 `cff5f844-a3d8-4c40-8d4c-dd2932b77b07` 才把过滤设为磁线圈。两次即时读回都保持原双端拓扑和满供电，电路板开始只流入回收仓。
- 限制或反例：如果 sorter 正在 Sending/Inserting 或任一携货字段非零，仍必须等待自然放货或先守恒泄压；绝不因所携物品“恰好匹配”而带货修改。修复也不代替上游混料设计的长期整理；sorter `563` 与堵塞环带仍需完成同样的守恒排空和最终过滤验收。
- 复验触发：下次普通保存/正常重启部署、首次在空载 Returning 窗口对 `551/563` 成功 prepare+commit、任一带货 stale 反例、蓝矩阵全部回收并研究恢复、DSP/程序集版本变化。
- 关联：EXP-058、EXP-090、EXP-096、`CanonicalStateHash.FactoryConfiguration`、`SorterFilterPolicy`、`UIInserterWindow.OnItemPickerReturn`。
- 最近复验：2026-09-03（处理器备料再次先避开 `868` 的带货窗口，再在零携货窗口配置；`573/868` 的过滤与双端拓扑均即时和审计复读成立）。

### EXP-103 — 自动管理研究物品会先把背包矩阵保留到 MechaLab，背包减少不等于消耗或丢失

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、`GameHistoryData.autoManageLabItems=true`、玩家背包内当前科技所需矩阵、`MechaLab.itemPoints` 隐藏保留容器；不覆盖工厂矩阵研究站的 `matrixServed`。
- 当前结论：`MechaLab.GameTick` 每 tick 先调 `AutoManage`。自动管理开启且有当前科技时，`ManageSupply` 按“剩余 hash × 该物品 pointsPerHash”计算需求，减去已保留 points，再以 3600 points/个向上取整，从 `player.package` 尾部取出实物进入 `itemPoints`。这一步发生在机甲研究功率/能量检查之前；因此即使 hash 完全不动，背包也可能先减少。当无适用科技或自动管理关闭时，`ManageTakeback` 把整个物品守恒退回背包并清空保留。以后对矩阵做物品守恒时必须把 player package、MechaLab 与 factory lab 三类容器分开复读。
- 直接证据：动作 `cd89fa7f-7dad-484f-8b70-367eb414de8b` 精确证明仓 `562` 的蓝矩阵 `293 -> 0`、玩家 `0 -> 293`；后续 fresh 复读玩家只剩 42，即少 251。同时科技 `1703` 仍精确为 `242820/288000`，工厂研究站 `84/679` 的蓝缓冲均为 0、红缓冲分别仍为 `37220/36580`。当前程序集 `MechaLab.ManageSupply` 的整数计算给出 `ceil((288000-242820)*20/3600)=251`，与背包差量完全相符；这同时排除工厂研究消耗和传送动作当场不守恒。
- 直接证据：结构化玩家 DTO 已新增 `autoManageResearchItems`、`mechaResearchPower` 和 `mechaResearchItemBuffer`；每项同时报原生 points、整个物品数与余数，不暴露存档身份，也不提供保留/消耗写入。完整 Release solution 0 warning / 0 error，90 tests passed（Contracts 10、Core 64、MCP 16）。
- 实机复验：同档恢复后 `get_player_state` 在 tick `8642633` 直接返回 `autoManageResearchItems=true`、机甲研究功率 `300000`，并在隐藏缓冲中精确读到蓝矩阵 `pointCount=903600`、`wholeItemCount=251`、余数 `0`；背包仍有 42。科技同时缺少另一种必需矩阵，所以即使研究功率非零也没有把这 251 个误判成已消耗。
- 实机复验：星际物流系统 `1605` 在 tick `11808407` 达到 `216000/216000` 并退出队列后，fresh player 明确为 `mechaResearchItemBuffer=[]`，背包同时持有 77 个蓝矩阵和 40 个黄矩阵。此前研究期间这些物品不在背包，完成后由原生 `ManageTakeback` 返回；没有 transfer 动作、地面仓正增量或注入写入可解释这次容器迁移。
- 限制或反例：若机甲研究功率大于 0、所有必需矩阵齐全且有足够能量，points 会正常消耗并上传 hash，不能要求其静态不变。完成后退回数量取决于当时仍保留的完整物品和背包容量，不能把本次 77/40 外推成固定数量。
- 复验触发：下次普通保存/重启部署后首次 player 复读、科技 `1703` 完成后自动退回、机甲研究功率非零、自动管理开关变化、玩家背包容量不足或 DSP/程序集版本变化。
- 关联：EXP-007、EXP-048、EXP-090、EXP-102、`MechaLab.ManageSupply/ManageTakeback/GameTick`、`PlayerStateSnapshot.MechaResearchItemBuffer`。
- 最近复验：2026-09-03（`1605` 完成后 MechaLab 缓冲清空，77 蓝/40 黄回到背包，验证无当前科技时的原生自动退回分支）。

### EXP-104 — 自包含发布包必须在干净提交上完成清单、整包哈希和 MCP 协议三层复验

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 Windows `win-x64` 自包含 MCP 发布包的可重建性、静态完整性与脱离 .NET SDK 的进程级协议冒烟；不等于已完成真实 BepInEx 安装或游戏内握手。
- 当前结论：发布候选必须从干净 Git 提交以 locked restore 和完整 Release build 生成，包内每个文件由 manifest 单独绑定 SHA-256，zip 另有 sidecar SHA-256。解包后必须重新校验路径安全与所有文件哈希，再实际启动包内自包含 MCP，完成 JSON-RPC `initialize` 和 `tools/list`。只要目标 RID 未进入 lock file、源码不干净、任一哈希不符、MCP 未启动或工具面不完整，均不能把包作为版本 Release 资产。
- 直接证据：第一次 `0.3.0-preview.1` 预演在 locked restore 阶段以 `NU1004` 安全失败，暴露 `win-x64` 尚未进入 lock file；为三个可发布项目显式声明 RuntimeIdentifier 并重建 lock groups 后，预演包成功。随后在干净提交 `5cb465a` 上重新生成 `0.3.0-preview.2`，manifest 顶层报告 `sourceDirty=false`、233 个总文件和已验证整包 SHA-256；独立解包复验确认 232 个 manifest 条目全部匹配，并从包内可执行文件完成协议版本 `2025-06-18` 初始化，服务名 `Spherewright.Mcp`、版本 `0.3.0.0`、44 个工具且 session/station 工具存在。打包过程完整 solution build 为 0 warning / 0 error；关联测试共 90 项通过。
- 限制或反例：游戏当前仍在运行，安装器按设计拒绝覆盖已加载 Plugin，因此本条只验证“构建、包完整性、自包含 MCP 启动与工具面”，不验证全新 BepInEx 目录安装、Plugin 加载、Bridge 握手、卸载或升级。`preview.2` 只是本地预发布验证资产，不是 tag 或 GitHub Release；v0.3.0 仍须完成全部物流塔实机门槛后重新从最终干净提交构建正式包。
- 复验触发：任一项目/RID/依赖/锁文件/manifest/安装布局/MCP 工具面改变，创建任何 tag 或 GitHub Release 前，以及首次在干净受支持游戏安装上验证 installer 时。
- 关联：`scripts/package-release.ps1`、`scripts/test-release-package.ps1`、`scripts/install-release.ps1`、`docs/release-installation.md`、`Directory.Packages.props` 与三个可发布项目的 `packages.lock.json`。
- 最近复验：2026-09-02（干净提交 preview.2 生成、独立完整性复验和包内 MCP 协议冒烟均通过；真实游戏安装仍待版本完成前单独验证）。

### EXP-105 — 物流塔载具装载必须绑定工作中数量、原型容量和增产点损失边界

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 DSP `0.10.34.28529` 的普通行星/星际物流站无人机与运输船槽、玩家背包双向转移；不覆盖轨道采集器、矿物采集站、翘曲器槽或塔内货物槽。
- 当前结论：无人机/运输船容量必须按 `idle + working` 占用计算，运输船只适用于 `isStellar`，取出只能减少 idle。塔内载具计数不能保存增产点，因此玩家到塔的载具只在该物品聚合 inc 为 0 时接受。安全动作必须独立绑定 fleet hash，提交前以背包副本证明精确容量，提交后证明玩家与 idle 等量反向变化、working 与另一类载具不变、总数守恒，且塔货物/订单/能量/翘曲器/配置、手持物和无关背包格均不变。
- 直接证据：当前程序集反编译的 `UIStationWindow.OnDroneIconClick(int)` / `OnShipIconClick(int)` 分别固定 item `5001/5002`，从建筑 prefab 读取 `stationMaxDroneCount/stationMaxShipCount`，以 idle+work 计算余量，存入时只增加 idle 并从手持扣除 `split_inc`，取出时只使用 idle；`StorageComponent.TakeItem/AddItemStacked` 与 `Player.NotifyPackageAddItem` 的完整签名也已复核。源码新增纯策略、独立 fleet hash、DTO 容量字段、Bridge/MCP prepare/commit、主线程双向守恒与无关状态复读；完整 solution 0 warning / 0 error，94 tests passed（Contracts 11、Core 66、MCP 17），MCP 注册面为 46。产品里程碑保存后已正常关闭旧进程并同批部署新 Plugin/Core/Contracts，逐文件部署哈希与 Release 输出一致；受保护恢复动作 `ba335eeb-d6b6-47b5-8e29-4eb133d0dba4` 成功，证明含该动作的新 Bridge build 已进入当前健康会话。
- 实机复验：自动产线仓 `893` 的 10 架未增产无人机先经普通 storage→player 动作 `105e2c4e-003a-4ce6-a590-f5069afaa1c3` 守恒进入背包；fleet 动作 `b2c9f159-c2de-494d-8ef9-01b6e1dc8867` 令 player `10->0`、idle `0->10`，动作 `f3579b24-f55c-4a88-8d43-e7ab94cb6a0c` 令 player `0->1`、idle `10->9`，动作 `b1927cf7-eefb-43a2-9946-700537f4e34d` 再令 player `1->0`、idle `9->10`。三次均保持 working `0`、energy `180 MJ`、charge `6 MW`、全部货槽/订单及写健康不变。
- 限制或反例：当前只 live 验证无在途订单的 PLS 无人机；运输船、ILS、working>0、自动补充和容量边界反例仍待验证。原生 UI 的 shift/control 取出路径会把全部 idle 清零，Spherewright 只采用经背包副本证明的有界精确子集，不依赖其可能部分接收的行为。
- 复验触发：下一次正常保存/关闭后的整组 DLL 部署、首座 PLS/ILS 完成、首次装入与取出无人机/运输船、载具在途时、自动补充开关变化或 DSP/程序集版本变化。
- 关联：`LogisticsStationFleetTransferPolicy`、`CanonicalStateHash.LogisticsStationFleet`、`NormalGameActionCoordinator.LogisticsStationFleet.cs`、`docs/research/game-api-m0.md`、ROADMAP v0.3。
- 最近复验：2026-09-02（PLS `916` 完成无人机 `0→10→9→10` 与玩家对应 `10→0→1→0` 的三次双边守恒 live 闭环）。

### EXP-106 — 物流运输机产线仍以科技门控、过滤输入、自动首产与普通保存闭环

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 owned 普通和平 1× 非沙盒世界中 item `5001`、recipe `94`、制造台 `891` 及其过滤备料/专用输出；不等于载具已装入物流塔或形成实际运输路线。
- 当前结论：物流运输机可以和其他科技门控产品一致，先在 recipe 0 下完成空载拓扑与三项过滤备料，等行星物流 `1604` 原生解锁后只启用一次。里程碑必须同时证明三类输入下降、制造台/输出 sorter 实际带货、专用仓正增长、首次产线事件持久化以及同一主档普通保存；仅看到科技解锁或 recipe 字段不足以完成验收。
- 直接证据：`1604` 于 tick `8836460` 正常解锁；动作 `532666d4-de0d-4ea9-abfc-6a44657fe555` 把制造台 `891` 从 recipe 0 配置为 `94`。链路 `892 -> 895(filter 1101)/896(filter 1303)/897(filter 1405) -> 891 -> 894 -> 893` 满供电运行，首次复读时三项设备 buffer 为铁块 3、处理器 4、推进器 4，sorter `894` 正携带 item `5001`；最终输入仓与设备输入均清空，专用仓 `893` 达到 10 个、inc 0。日记 sequence `41` 在 tick `9346766`、实际时间 `2026-09-02T20:02:42.8990067+08:00`、本局 `001d 19:16:19` 持久化；保存动作 `e8d96a21-75d7-4f7c-a60b-598646f7f754` 确认 tick `9369181`、revision `112`、healthy。
- 限制或反例：本批只用预先守恒备好的 50 铁块、20 处理器、20 推进器生产 10 个物流运输机，证明自动转换与过滤拓扑，不证明三种上游已由物流塔持续补给。10 架成品后来已从仓 `893` 守恒装入 PLS `916`，但尚无第二站和实际运输订单，不能声称物流路线闭环。
- 复验触发：正常重启部署后首次读取仓 `893`，首座 PLS 完成并装入无人机时，任一输入链重接/矿枯竭/断电，或 DSP/程序集版本变化。
- 关联：EXP-062、EXP-074、EXP-094、EXP-095、EXP-105、`docs/gameplay-timeline.md`、ROADMAP v0.3。
- 最近复验：2026-09-02（自动产出 10、日记 durable sequence 41、普通保存 tick 9369181；随后仓到玩家再到 PLS 的总数守恒闭环完成）。

### EXP-107 — 多数量建筑配方必须等待完整批次到位，并以产物仓而非瞬时 working 判定完成

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 DSP `0.10.34.28529`、owned 普通和平 1× 非沙盒世界中的行星物流运输站 item `2103`、recipe `93`、制造台 `898` 及四输入过滤拓扑；可类推为验收方法，但不直接证明其他高数量建筑配方。
- 当前结论：高数量建筑配方启用后，sorter 会先把完整一轮原料逐步搬入制造台；中途 `isWorking=false/progress=0` 只说明批次尚未凑齐，不能误判断电或失败。必须继续复读供电和各输入总量，等完整批次开始并完成，再以来源耗尽、专用输出仓增加、durable journal 和普通保存共同收口。`production` 模式配置绑定设备完整 `stateHash`；专用 `configurationStateHash` 只用于 sorter filter 等明确模式，误用会在 prepare 阶段安全返回 `STALE_STATE` 而无副作用。
- 直接证据：启用前仓 `899` 有钢材/钛块/处理器/粒子容器 `40/40/40/20`，sorter `902–905` 分别过滤 `1103/1106/1303/1206` 且空载。一次误用 configuration hash 的 prepare 被 `STALE_STATE` 拒绝；fresh 完整 state hash 随后令动作 `8f90d632-b90b-4ff1-b9d1-fe20850153c2` 一次启用 recipe `93`。首轮读回制造台各输入仅 19 且不工作，稍后增至钢/钛/处理器 31、粒子容器 20，供电比始终 1.0；完整批次到位后进度读到 `6690000/12000000`，最终源仓和设备输入清零、仓 `900` 增至 1 个 item `2103`。日记 sequence `42` 在 tick `9410766`（实际 `2026-09-02T20:20:29.8130148+08:00`、本局 `001d 19:34:06`）durable，保存动作 `8dedea0d-2003-4e3f-80be-71db3e5a176e` 确认 tick `9413535`、revision `115`、healthy。
- 实机复验：第二批 PLS 先在粒子容器 20、钢材 40、自动处理器 40 已到位但钛块为 0 时保持满供电等待；钢材曾以 `31 仓存 + 8 设备 + 1 sorter = 40` 闭合。随后钛仓 `531` 经动作 `89e44048-2b48-4dd0-a98c-dab23fb55d63` 守恒交出 40（`160 -> 120`），动作 `8ce84596-da17-4421-94e7-c09810c3b6ba` 把玩家 `40 -> 0` 并交付活跃仓。即时分布为 `39 仓 + 1 设备`，稍后为 `7 + 33`；完整批次进入后四种输入同时归零、制造台满供电推进到 `4245000/12000000` 并完成，输出仓 `900` 增至 1。普通保存动作 `c73789bf-f51d-4573-ac4b-c51860f6f954` 持久化 tick `9783554`、revision `55`、healthy；journal 保持 `42/42` 是因为同一物品首次产线事件早已记录，不能把“不新增首次事件”误判为未生产。
- 实机复验：为星际物流站前置件生产的双 PLS 批次提供第三个独立高数量样本。动作 `ae947265-42f7-4e49-9c5f-f34a769ded22` / `7d4e99b7-02c3-471a-9218-6555cbc673ee` 把处理器专仓 `854` 的 80 件经玩家完整送入活跃仓 `899`；sorter `904` 逐件预取期间，制造台在处理器不足 40 时持续 `isWorking=false/progress=0`，但处理器输入由 11 稳定升至 33、另三项仍为钢/钛/粒子容器 `80/80/40`，证明只是凑批。最终制造台 `898(recipe 93)` 四项输入及输出缓存全部归零，仓 `900` 在原有 80 钛合金外新增精确 2 座 item `2103`。同一主档由动作 `93dfa668-ec95-441f-8ca1-e05f642b1288` 正常保存到 tick `11896243`；journal 仍沿用 sequence `42`，符合首次事件去重。
- 限制或反例：两轮都消耗一次性备料，证明配方转换可重复，不证明上游持续补料。两座成品后来已施工并完成 100 件钛块的真实本地运输，但该路线仍消耗一次性钛仓，不把本条扩张成持续站体原料补给或跨星球自动化；制造台处于批次边界时 `buffers=[]`/`isWorking=true`、完成后 `isWorking=false` 都是合法瞬时状态，必须结合前后数量、输出仓和保存判断。
- 复验触发：首次生产星际物流运输站、任一高数量建筑配方出现长时间等待、设备断电/配方重配、生产模式哈希规则变化，或 DSP/程序集版本变化。
- 关联：EXP-062、EXP-074、EXP-094、EXP-095、`NormalGameActionCoordinator.Configure.cs`、`docs/gameplay-timeline.md`、ROADMAP v0.3。
- 最近复验：2026-09-03（第三批一次备齐双份配方；预取等待、完整消耗、专仓精确新增 2 座和普通保存 tick `11896243` 全部闭合，journal 正确不重复 sequence `42`）。

### EXP-108 — 本地 PLS 的 StationComponent.planetId 使用 0 哨兵，不能套用星际站身份规则

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 DSP `0.10.34.28529` 的 `PlanetTransport` 本地行星物流站身份读取，以及观察、槽位配置、充电配置和载具转移的共同入口；星际站仍要求精确 planet ID。
- 当前结论：`entity.stationId -> PlanetTransport.stationPool[id]`、`station.id` 和 `station.entityId` 是本地站的主身份链；非星际站允许原生 raw `station.planetId == 0` 哨兵或精确本星 ID，但必须拒绝其他正 planet ID。星际站不允许 0，仍须等于当前 factory planet。公开 DTO 应使用已由 session/factory 绑定的本星 ID，不能把 raw 哨兵暴露成“未知星球”。
- 直接证据：首座站体通过普通仓到玩家守恒转移和原生预建/施工完成为实体 `916`，位置与 prepare 候选一致、站体背包 `1 -> 0`、无预建残留且写健康；旧读取将其识别为 `componentKind=station`，但因 `station.planetId != factory.planetId` 返回 `logisticsStation=null`。当前程序集反编译证明 `StationComponent.Init(...)` 不赋 `planetId`，`PlanetTransport.NewStationComponent(...)` 只对 `isStellarStation` 调用 `GalacticTransport.AddStationComponent(planet.id, station)`。源码把这一判定集中到 `LogisticsStationIdentityPolicy`，四个读写入口共用，并用 7 个正反用例覆盖 local 0/exact/foreign、stellar exact/0/foreign 和非法 factory；完整 Release build 0 warning / 0 error，101 tests passed（11/73/17）。正常保存 tick `9462208` 后同批部署 Plugin `0086A970…`、Contracts `0A244DCA…`、Core `058E3EFD…`，恢复动作 `df1ae62a-548a-49fe-a9a1-fbd6d1aca764` 只载入 exact primary；fresh inspect 随即返回实体 `916` 的完整 DTO：公开 planet `104`、station `1`、gid `0`、`isInterstellar=false`、4 个空槽、无人机容量 50、运输船容量 0、能量上限 180 MJ及独立配置/fleet hash。
- 实机复验：同一实体 `916` 的归一化身份随后通过槽位配置动作 `d6937f52-c03e-4045-93ac-5025b7ebdba1`、充电配置动作 `cd332730-12ef-45a2-947f-0716a4eb9ca6` 以及三次无人机 fleet 往返；四个读写入口都接受本地 raw 0 哨兵并持续公开 planet `104`，未出现错误跨星球认领。
- 限制或反例：本次仍只 live 验证本地 PLS；0 哨兵绝不能用于星际站，后者仍必须精确匹配当前 planet。其他 DSP 版本变化后必须重新反编译与实机验证。
- 复验触发：下一次正常保存/关闭/同批部署后首次 inspect 实体 `916`，首次 PLS 槽位/充电配置、首次无人机装入与取出、首座 ILS 读回，或 DSP/程序集版本变化。
- 关联：`LogisticsStationIdentityPolicy`、`GameStateReader.CaptureLogisticsStation`、两个站配置入口、`TryGetFleetStation`、`docs/research/game-api-m0.md`、EXP-097/098/099/101/105。
- 最近复验：2026-09-02（修复版同批部署、exact-primary 恢复，以及实体 `916` 的观察、槽位、充电、fleet 四入口 live 收口）。

### EXP-109 — 首座 PLS 投运必须拆分产物、站体、供电、配置、机队和真实路线六道证据门

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 owned 普通和平 1× 非沙盒世界中首座 PLS 的最小投运顺序与里程碑宣称；不把一座本地站外推为双站运输或星际物流。
- 当前结论：物流站产物、站体完工、接入电网并充能、槽位/充电配置、载具装入、真实订单搬运是六个可分别失败的状态；验收和保存不能用其中一个替代另一个。对空新塔应先证明原生施工和身份，再正常补电并等待稳定能量，随后用独立 configuration/fleet 哈希做配置和载具守恒；只有第二个可达站点和实际货物形成订单、无人机 working/往返、源减目标增后，才可声称行星物流路线完成。
- 直接证据：item `2103` 已由 recipe `93` 自动产出并原生施工为实体 `916`；初始 `powerNetworkId=0`、energy `0/180 MJ`。从仓 `829` 守恒取得 2 铁、以真实原料手搓电塔并原生施工实体 `917` 后，站点接入 network 1、service `1.0`，能量自然经历至少 `147470607 -> 180000000`。随后槽 0 配为钛块/100/本地需求、最大充电功率 12→6 MW、10 架无人机从产线仓守恒装入并完成取 1/还 1 复验；全程货物 count/order 仍为 0。普通保存动作 `0cdbefd4-3c57-4c9b-abbf-4b958814350c` 已把该单站状态持久化到 tick `9522204`；故当前只完成“可运营单站”，没有误报真实路线。
- 实机复验：第二批自动站体从仓 `900` 经动作 `a1f7bccc-fe64-4803-b73a-e79fc0a6fdd4` 守恒进入玩家，随后 DSP 建造校验在钛仓 `531` 外约 18 m 接受草地候选，动作 `23099397-0b3d-4192-b786-95e982792678` 由施工无人机完成实体 `918`。站体背包 `1 -> 0`、无残留 prebuild，公开身份为 planet `104` / local station `2` / entity `918`；初始 network `0`、energy `0/180 MJ`、四槽空、fleet 0，和首塔状态完全分离。现有 network 4 风机距它 12.81 m 但覆盖未到，因而先通过正常库存转移及手搓取得 1 座电力感应塔，再由动作 `b4435a4d-19da-41fc-8486-e98a398016de` 原生施工实体 `919`；塔随即接入 network 4。动作 `8347066c-50d0-4fdf-9718-a5e546d13e5a` 把最大充电功率从 12 MW 调为 6 MW，动作 `b6576624-0b27-437d-8218-9b7e518fd83a` 把 0 号槽配为钛块/100/本地供应。fresh 复读时能量已自然增长到约 `115.46/180 MJ`，配置稳定且槽库存仍为 0。
- 真实路线复验：动作 `529f9496-6d96-43a5-b6bb-08f778e905c3` 用 18 条普通带建成 `937 -> 936…920 -> 922 -> 921 -> 918`，并把末段原生接入站点 Input 口；动作 `0bf4d65a-3b92-4f81-9478-58de8fc45ed2` 建成 `531 -> 938 -> 937`。源仓随即 `120 -> 107 -> 64 -> 16 -> 0`，供应塔依次出现库存、`localOrder=-25`，需求塔则从 10 idle/0 working 变为 9/1、8/2，并以每架 25 件的真实订单把库存 `0 -> 50 -> 75 -> 100`。最终需求塔恢复 10 idle/0 working、双方订单归零，供应塔余 20；`0 + 20 + 100 = 120` 完整守恒。普通保存动作 `142d6c1c-98fe-4f9c-aab4-3ae51fadc9a4` 已持久化 tick `9862572`、revision `89`、healthy，journal 保持 `42/42` durable。
- 限制或反例：本条现在已经证明同一星球双 PLS 的真实无人机运输，不再只是互补配置。它仍不证明远端矿物自动采集、ILS、运输船或跨星球塔运；供应塔本轮依赖已有钛仓的一次性 120 件库存，长期补货也尚未验收。
- 复验触发：第二座 PLS 完工、首次真实本地订单、无人机 working>0、源站/目标站货物变化、首座 ILS 或保存恢复。
- 关联：EXP-017、EXP-021、EXP-097–099、EXP-101、EXP-105–108、ROADMAP v0.3。
- 最近复验：2026-09-02（双塔均满电；需求塔 10 架无人机完成四批各 25 件运输，目标钛块达到 100、源余 20、订单与 working 均归零，并普通保存）。

### EXP-110 — PLS 可作为 belt destination，带口 storageIndex 是一基编号且需求侧 fleet 足以取货

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、本地 PLS、普通基础带/分拣器、当前站点槽 0 的本地 supply/demand 路线。
- 当前结论：给 PLS 装货不需要也不允许直接写站内库存。先配置目标货物槽，再以站体为 `destinationObjectId` 让原生 belt-port 校验选择空闲 Input 口；自由起点应放在源仓的分拣器可达范围，再单独 prepare/commit `storage -> first belt`。DTO 中 `beltSlots[*].storageIndex` 保留 DSP raw 一基编号：值 `1` 对应 `storageSlots[0]`，不能误当数组下标 1。供应塔本身可以没有无人机；只要需求塔有正常供电和 idle fleet，它会前往供应塔取货。
- 直接证据：以钛仓 `531` 外约 2.7 m 的自由起点绑定站 `918` 时，11 条带只返回无副作用 `NotEnoughItem`；补至 23 后 prepare 明确给出 18 点路径，动作 `529f9496-6d96-43a5-b6bb-08f778e905c3` 消耗 18 并创建带 `920–937`。有向读回为首带 `937 -> 936`，末带 `921 -> 918 slot 0`；站点 `beltSlots[0]` 为 `direction=Input`、`beltEntityId=921`、raw `storageIndex=1`，而实际增长的是已配置钛块的 `storageSlots[0]`。分拣器 `938` 双端为 `531 -> 937` 且实际携带 item `1106`。供应塔 fleet 始终 0/0，需求塔 fleet 从 10/0 到 8/2，再回 10/0；目标库存最终 100、供应塔余 20、源仓 0。
- 限制或反例：raw `storageIndex=1 -> slot 0` 目前只在首个 PLS Input 口和第一存储槽验证；不同端口、输出带、多槽映射或 DSP 版本变化都必须重新读回，不能泛化为固定偏移。需求侧单独派车也不表示所有供需/距离/最低载量设置都等价；本轮只覆盖同星球约百米、每架 25 件和默认 10% 配送设置。
- 复验触发：第二个 PLS 带口、非零 storage slot、站点输出带、不同无人机运力/配送比例、保存恢复后的继续运输、首座 ILS 或 DSP 版本变化。
- 关联：EXP-021、EXP-028、EXP-068、EXP-070、EXP-097–099、EXP-105、EXP-109、`NormalGameActionCoordinator.StructuredActions.cs`、`GameStateReader.CaptureLogisticsStation`。
- 最近复验：2026-09-02（18 段带直入供应塔、源仓分拣器实际携钛、需求侧 10 架 fleet 独立完成 100 件运输并归队）。

### EXP-111 — 阶段转入持续跨星物流时，资源航行应携带或就地补齐完整远端矿站包

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：当前 DSP `0.10.34.28529`、同星系普通资源航行、无 ILS 前的硅/钛星前哨；不把一条远端仓储矿线外推为已完成跨星物流。
- 当前结论：为一次科技解锁手采定额矿物可以是合理短期策略，但目标切换为持续 ILS 后，下一趟资源航行必须按“矿机 + 足额发电 + 配电 + 出料带 + sorter + 仓储”完整包规划，不能只按返航载荷规划。若已经漏带，不必废档或立即空返：先盘点随身建筑和本地基础资源，用普通手采与 replicator 守恒补齐，再以矿机覆盖节点、独立电网满供电、真实出料拓扑、仓存增长和普通保存验收。物品 ID 必须从运行时目录确认；本趟背包 item `2301` 是采矿机，而 `2020/2030` 分别是四向分流器/流速监测器，不能凭编号印象误当电塔或 sorter。
- 直接证据：第二次抵达 planet `102` 时玩家带有 1 台采矿机、5 条带、1 个磁线圈和 499 铜块，但没有风机、仓、普通 sorter 或电塔；先完成中的 600 硅手采，再从矿脉守恒取得 100 铁矿和 8 石矿。DSP replicator 随后真实消耗铁/石/铜，产出第二台矿机、4 风机、2 仓、8 条带、2 sorter 和 2 电塔。首台矿机实体 `1` 覆盖钛脉 `315/322`；电塔 `2` 距矿机约 12 m 时只能连接风机网络、不能覆盖消费者，补建更近的电塔 `5` 后，network 1 读到 2 generator/1 consumer、容量/需求 `11000/7000` energy per tick、serve ratio `1.0`。矿机原生出口的真实方向由 `1 -> belts 7…14` 读回，故没有强接预估位置的仓 `6`，而是在自由端放仓 `15` 并建 sorter `16`；仓中钛石从 6 增至 26。journal sequence `43` 在 tick `9985749`（实际 `2026-09-02T23:13:42.240493+08:00`、本局 `001d 22:13:49`）durable 记录首次产线钛石。后续硅矿机 `17` 经正常拆除回收，从仅覆盖 `252/256` 重建为精确覆盖 `245/249/252/256`，满供电链 `17 -> belts 30…39 -> belts 18…24 -> sorter 26 -> storage 25` 使仓存 `16 -> 27 -> 50 -> 287`。保存 tick `10126918` 后，两种矿线继续增长；返航又把 1100 钛石/651 硅石完整带回 planet `104`，并由普通保存 `a3944276-ad65-4e04-b680-4e267e26b056` 持久化到 tick `10182419`。至此“漏带后就地补齐完整双矿前哨”的计划已经完整复现并升级为 validated。
- 限制或反例：仓 `6` 是出口朝向误判后保留的空工具仓，不属于产线；两台风机的 660 kW 只证明当前黑石盐滩上的一台普通矿机满供电，不保证其他星球风力倍率或多消费者负载。600 硅曾是手采货物，但后来独立的自动硅矿仓增长才构成产线证据。两条矿线仍只送到远端仓；没有 ILS/运输船时，返航货物依然由伊卡洛斯人工搬运，不能把前哨完成外推为持续跨星物流完成。
- 复验触发：下一次跨星资源航行、远端电网增加消费者、首次远端 ILS 接仓、保存恢复后仓存斜率，或 DSP/程序集版本变化。
- 关联：EXP-018、EXP-021、EXP-028、EXP-037、EXP-042、EXP-045、EXP-047、EXP-087、EXP-095、ROADMAP v0.3。
- 最近复验：2026-09-03（planet `102` 钛/硅完整前哨均满供电持续增仓，1100/651 原矿返航守恒并普通保存到 tick `10182419`）。

### EXP-112 — 资源矿机选址必须最大化原生覆盖节点，不能接受第一个合法角度

- 状态：`validated`
- 日期：2026-09-02
- 适用范围：DSP `0.10.34.28529`、普通固体矿机的无显式姿态自动选址，以及为纠正低覆盖姿态而进行的正常拆除；不外推到大型矿机、水泵或任意建筑拆除。
- 当前结论：原生 `CheckBuildConditions` 接受一个姿态只证明能造，不证明矿点覆盖合理。无显式 preferred pose 时必须先验证完整的有界距离/角度候选集，以 native preview 的唯一正数 resource-node 参数数量为第一排序键；prepare 要把最终节点集公开给调用方，commit 完工后还要读取矿机实际 vein 集合并要求精确一致。已落位的低覆盖矿机只能通过游戏正常 `PlayerAction_Build.DoDismantleObject` 回收后重建，拆除前后需绑定实体端点、玩家状态、范围、容量、实体消失和建筑/缓冲库存守恒。
- 直接证据：旧选择器遍历候选时在首个合法姿态立即返回，planet `102` 的硅矿机 `17` 因而只绑定 `252/256`；玩家明确拒绝把该姿态作为持续前哨。源码改为收集全部通过原生校验的候选，再按覆盖数、到绑定矿点距离、yaw 和候选序稳定选择；`plannedResourceNodeIds` 进入 prepare DTO，完工验收由“包含绑定节点”收紧为“实际节点集等于计划节点集”。完整解决方案 0 warning/0 error，105 项测试通过（Contracts 12、Bridge.Core 75、MCP 18），同批部署后动作 `94e0416f-3092-490f-a7d9-664ec0ed0535` 通过 `DoDismantleObject` 正常回收矿机 `0 -> 1` 与内部硅石 `600 -> 650`，目标读回 `INVALID_ENTITY`；动作 `eb8a4a5c-eab2-492c-8c26-7aff79df316e` 以 yaw `150°` 重建，prepare/完工节点集均为 `245/249/252/256`。风机 `29`、电塔 `42` 和 12 段新带随后恢复满供电闭环，仓 `25` 在独立窗口中 `16 -> 27 -> 50`，保存动作 `73f7dd77-e057-46ce-8156-0b7b3a3736f3` 持久化 tick `10126918`、revision `26`、healthy。保存后又于 tick `10145866+` 独立复读：矿机仍精确覆盖四节点、network 2/serve `1.0`/working，sorter `26` 正在携带硅石，仓 `25` 已自然增至 287。
- 限制或反例：节点数优先不等于整条产线布局最优；新姿态可能使现有出料带、电塔或仓失配，仍须逐项重新 prepare 和读回。当前拆除容量检查按所有回收物都需要新空槽保守估计，可能拒绝本可叠入现有栈的情况；这是安全假阴性，不允许据此绕过正常路径。若所有现有径向候选仍只覆盖 2 个节点，必须扩充候选生成到矿簇几何范围后再造，不能为了省事接受 2 点覆盖。
- 复验触发：矿机 `17` 正常拆除、同一硅矿簇重新 prepare、`plannedResourceNodeIds` 超过 2、完工实际集合相等、原出料/供电重接、硅仓增长、普通保存，或 DSP/程序集版本变化。
- 关联：EXP-007、EXP-021、EXP-037、EXP-042、EXP-045、EXP-068、EXP-070、EXP-111、`ResourceCoverageSelection`、`NormalGameActionCoordinator.Dismantle.cs`。
- 最近复验：2026-09-02（正常拆除精确回收、两节点→四节点重建、满供电出料、仓持续增长和普通保存均完成 live）。

### EXP-113 — 飞行 prepare 的保守能量下限只是准入门，不是着陆余量保证

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、当前 same-star flight 控制器、planet `102 -> 104` 返航、400 MJ 核心、49 氢与 1100 钛石/651 硅石负载的单次现场样本；不外推为固定航程油耗。
- 当前结论：`requiredFlightEnergy = max(1.5 × coreCapacity, distance × 1000)` 可用于拒绝明显无法起飞的状态，但通过 prepare 不得宣称抵达时仍有制动/着陆余量。若航行在核心归零后才接触目标表面并无法收敛到稳定 Walk，必须以 `recovery_required` 终止，只重载绑定检查点；不得将表面接触冒充成功或保存主档。后续用户现场确认两次落点其实都在海面，手动横走两步即可上岸，故现有证据更直接指向“控制器缺少 Drift 靠岸收尾”，不能把零核心误写成唯一根因。
- 直接证据：动作 `1555c331-b5fb-4f29-b0ee-aa524304fcc7` 先确认独立检查点 tick `10173149`，再从 planet `102` 原生起飞。玩家在距 planet `104` 表面约 2502 m、速度约 868.1 m/s 时已为核心 `0/400 MJ`；后续进入目标表面 Drift，但在着陆窗口内始终未达成连续 600 tick Walk，终态为 `recovery_required`。恢复动作 `719d56d0-cd8f-485c-8c44-7309aba9b5a5` 随后只载入该精确检查点，新 session `9c8b74c0-24fc-4546-8d7e-b35860ba9eee` 复读为 planet `102`、Walk/0、核心 `400/400 MJ`、燃料格 49 氢、背包 1100 钛石/651 硅石且写入健康。同 checkpoint 第二次动作 `0e913bc5-1b70-4ad1-afcd-62c522075547` 虽以不同采样时序再次进入 planet `104` 表面，仍未从 Drift 收敛并于 tick `10188952` 返回 `recovery_required`。票据仍绑定原起点/终点和该精确 tick，主档未被任一失败航迹覆盖。
- 限制或反例：本样本不能单独证明失败唯一由燃料不足造成；用户观察到的海面落点已经提供更强的替代解释。不得擅自把阈值硬编码为“必须 80 氢”；应先在同检查点验证原生 Drift 横移靠岸，再依据抵达后的真实能量决定是否仍需调整预算。
- 复验触发：本次精确 checkpoint reload、同检查点重试、补充正常燃料后重试、稳定着陆/再次失败、飞行能量预算或 DSP/程序集版本变化。
- 关联：EXP-035、EXP-047、EXP-050、EXP-052、EXP-079、EXP-082、`NormalGameActionCoordinator.InterplanetaryFlight.cs`、`FlightCheckpointStore.cs`。
- 最近复验：2026-09-03（同一 49 氢 checkpoint 已两次接触目标表面但均以 Drift 收敛超时进入 `recovery_required`）。

### EXP-114 — 海面 Drift 接触后应通过原生 MoveTo 横移到已证明的近岸地形

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、Assembly-CSharp SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`、实体星球着陆后的 `EMovementState.Drift`，不适用于气态巨行星或跨恒星航行。
- 当前结论：目标星球身份已成立而玩家处于 Drift 时，不能原地等待 Walk。当前程序集的 `PlayerMove_Drift.GameTick` 明确读取 `player.currentOrder`，把 `OrderNode.MoveTo` 的目标投影到球面切向，以机甲 walkSpeed 生成 `rtsVelocity`，再经 `UseDriftEnergy` 和普通碰撞推进；`DetermineDrift` 会在离开水面条件后自然切回 Walk。因此安全收尾应从 `PlanetRawData.vertices` 与 `QueryModifiedHeight` 选择最近且有干燥邻域的地形，只签发有界、精确归属的原生 MoveTo，继续保留全局着陆超时、停滞 watchdog、断能检测和 checkpoint 失败回滚，禁止写位置或随机模拟按键。
- 直接证据：用户现场确认前两次返航实际已落在海洋上，手动横走两步即可落地。针对当前 DLL 的 IL 复核同时确认 Drift 原生订单路径、`PlayerOrder.ReachTest` 的球面距离判定、`PlanetRawData.QueryModifiedHeight` 和 `PlanetAlgorithm.CalcLandPercent` 的地形高度依据。源码实现搜索范围最多 120 m，中心地形至少高出 realRadius 0.2 m，2 m 八方向邻域最低不低于 -0.05 m；最多 3 个原生订单，每个都做精确引用所有权、180-tick 物理/目标进展、断能与 120-tick Drift→Walk 过渡检查。部署后，恢复动作 `155f95a7-4c80-4e19-800c-f6f29f0581a1` 只载入同一 tick `10173149` checkpoint；返航动作 `f6b60009-3b77-4b34-b14c-fd89d4b13d71` 再次接触 planet `104` 海面后，结构化消息证明首次订单选中 24.6 m 外、中心 clearance 0.20 m 的最近干燥邻域。该原生 MoveTo 第一次即使状态转为 Walk/0，并连续稳定 600 tick 完成；额外 10 秒复读位置恒为 `(132.706726,-8.28753,-149.304672)`、速度 0，1100 钛石和 651 硅石完整守恒。普通保存 `a3944276-ad65-4e04-b680-4e267e26b056` 随后持久化 tick `10182419`，写入健康并确认 checkpoint capability 已移除。完整 solution 0 warning / 0 error，112 tests passed。
- 限制或反例：地形高度只能证明干燥近岸候选，不能提前证明沿途没有临时建筑碰撞；因此 native order 仍必须受物理停滞 watchdog 约束。当前只有 planet `104` 的一个 24.6 m 海岸样本；120 m 搜索范围、0.2/-0.05 m 高度门槛和最多 3 单仍应在不同海洋/地形上复验，不能外推为全局寻路。
- 复验触发：部署同批 Plugin/Core/Contracts、从 tick `10173149` 的同一 checkpoint 重试、出现 Drift 靠岸消息、稳定 Walk 600 tick、普通保存退役 checkpoint；或地形数组/DSP 程序集版本变化。
- 关联：EXP-007、EXP-021、EXP-035、EXP-047、EXP-050、EXP-052、EXP-079、EXP-082、EXP-113、`LandingShoreSelection`、`NormalGameActionCoordinator.InterplanetaryFlight.cs`。
- 最近复验：2026-09-03（同一 tick-`10173149` checkpoint 的第三次返航以 24.6 m 原生靠岸订单首试成功，稳定 Walk 600 tick并保存退役检查点）。

### EXP-115 — 多矩阵科技必须把全部必需矩阵送入同一研究站

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、两座彼此独立且未堆叠的矩阵研究站，以及同时需要蓝/红/黄矩阵的科技 `1414`；不外推到垂直堆叠研究站的原生共享规则。
- 当前结论：研究所需矩阵不是跨独立研究站全局合并的。单座满供电研究站即使已配置当前科技并拥有黄矩阵，只要本机蓝/红缓冲为零就不会工作；另一座站的蓝/红缓冲和机甲隐藏矩阵缓冲也不能替它补齐。应把缺少的矩阵送入已经拥有其余必需矩阵的同一研究站，或另行证明原生垂直堆叠共享，不能把“任意站各有一种矩阵”误判为可研究。
- 直接证据：新研究站 `939` 已接入 network `1`、供电比 `1.0` 并配置为当前科技 `1414`；sorter `941` 从纯黄糖仓 `778` 向它送入 `36000` 内部点（10 个黄矩阵）后，站内蓝/红仍为 `0/0`、`isWorking=false`，科技 hash 保持 `0`。随后用 35 段普通带 `942…976`、源 sorter `977` 和末端 sorter `978` 把同一纯黄糖仓接入已有蓝/红输入的研究站 `84`；复读确认站 `84` 同时具有三种正缓冲、满供电且 `isWorking=true`，科技 hash 连续从 `0 -> 1181 -> 6523/144000`。普通保存动作 `25800a05-d63b-4489-b2e3-4d259a6b44e5` 已持久化 tick `10312259`，而独立站 `939` 仍保持黄矩阵正缓冲但停止，形成同一世界内的正反对照。
- 限制或反例：矩阵站内部 `count` 是 3600 points/item 的原生点数，不得直接当物品个数。当前未拆除试验站 `939`，其 10 个黄矩阵仍是守恒的设备缓冲；也未测试上下堆叠后的矩阵分配。机甲缓冲中的 40 黄矩阵同样独立，不能计入工厂站 `84` 的可用输入。
- 复验触发：垂直堆叠矩阵站、科技切换、试验站正常回收能力扩展、研究输入拓扑变化、DSP/程序集版本变化，或任一站出现跨实体矩阵共享证据。
- 关联：EXP-048、EXP-053、EXP-079、EXP-090、EXP-100、`MechaLab.ManageSupply`、矩阵站 `84/939`。
- 最近复验：2026-09-03（独立黄矩阵站停工与三矩阵同站正常上传构成同档正反对照，并已普通保存）。

### EXP-116 — 普通 Drift 脱困要重试未提交的状态竞争并退回已验证陆地点

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、地面 Move 在水面终止后形成的低速 `Drift`、仍有充足核心能量且存在刚离开的已验证 Walk 坐标；不替代跨星飞行自己的 checkpoint/靠岸状态机。
- 当前结论：地面路线每段后发现 `Drift` 时必须立即停止剩余路点，不能继续按原直线耗能。可把刚才稳定的 Walk 坐标作为原生 `OrderNode.MoveTo` 回退目标；但 Drift 会让精确玩家位置持续变化，外部 inspect→prepare→commit 可能在任一无副作用阶段返回 `STALE_STATE`。只允许在没有 action ID 时重读并重试；一旦 commit 被接受就只轮询该 action，绝不能换 idempotency key 重放。窄海岸上的“坐标已验证”仍不足以允许宽到达容差：`3 m` 可让动作在已知陆地点旁的水面提前完成；回退应采用当前契约下最窄的 `0.5 m` 容差，并在动作后独立复读 `Walk`、速度 0 和能量。
- 直接证据：从黄糖区向仓 `768` 的 73.9 m 三段路线只执行首段动作 `9fe119a4-a391-4333-b3e4-fe84f8c9ee29`，段后立即读到位置约 `(-66.76,-75.42,-173.94)`、`Drift`、速度约 `0.185 m/s`，脚本因此未提交第 2/3 段。回退到先前稳定点 `(-46.11048,-39.17191,-190.849472)` 时，首次 commit 及随后三次紧凑尝试均只返回无 action ID 的状态竞争；第 4 次原子绑定由动作 `07296b3e-9299-41a1-96e5-43ba4510ef36` 正常接受并在 2.91 m 容差内完成。3 秒后独立复读位置 `(-47.8027534,-40.71567,-190.1175)`、`Walk`、速度 `0`、核心约 `286.22/400 MJ`，写入仍健康。第二个独立样本从稳定点 `(-78.19336,-59.88548,-174.1149)` 直切锚点 `143`：动作 `7461e6bb-ccb4-498e-8f5e-483d04f11324` 虽在 4.87 m 内完成，3 秒后却为 `Drift`、约 `0.135 m/s`；回退动作 `f9d00301-9845-4575-b610-0c61acd5e860` 使用 `3 m` 容差并在 2.91 m 内完成，复读仍为 `Drift`、约 `0.126 m/s`。只把同一已知 Walk 坐标的容差收紧为 `0.5 m` 后，唯一动作 `c549a315-7c8c-4852-9dd1-f42eb592cc58` 在 0.45 m 内完成，3 秒后位置 `(-78.2105,-59.9879,-174.1529)`、`Walk`、速度 0；所有未被接受的尝试都只有明确 `STALE_STATE` 且无 action ID。
- 限制或反例：本条只证明回到已知陆地点，不证明任意目标是陆地，也不证明现有 `invoke-surface-route.ps1` 会绕海。`0.5 m` 是当前这类窄海岸回退的实机安全值，不应外推为所有普通移动的统一容差；过窄目标在建筑旁仍可能触发碰撞看门狗。当前紧凑重试仍可能在持续高速位移时长期 stale；达到有界次数后必须停下，不得放宽状态哈希或直接写位置。两个样本都消耗了显著核心能量，说明即使不会卡到零能，也应避免重复横穿同一水面。
- 复验触发：下一次普通 Move 落水、不同 Drift 速度、连续 120 次仍无法取得 action ID、路线脚本增加干燥地形预检、玩家订单/哈希量化变化或 DSP 版本变化。
- 关联：EXP-007、EXP-047、EXP-052、EXP-061、EXP-066、EXP-079、EXP-114、`CanonicalStateHash.PlayerAction`、`invoke-surface-route.ps1`。
- 最近复验：2026-09-03（一次 Drift 中的 commit 明确在接受前返回 `STALE_STATE`，只重新绑定未提交意图；三个已接受短移虽分别 completed，fresh 却始终为 Drift，故在核心从约 379.6 MJ 降到 250.8 MJ 时中止连续试探，改为返回上一精确 Walk 落点）。

### EXP-117 — 物流站输出带连接与货物选择是两个独立状态

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、Assembly-CSharp SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`、普通行星/星际物流站的已连接 Output belt port；不适用于采集器或主动替换既有选择器。
- 当前结论：把传送带从物流站向外连接只会建立端口方向与 belt identity，不会自动指定输出哪一个仓储槽。`SlotData.storageIdx` 使用原生一基选择器：raw `0` 是 None，raw `1` 对应公开的 `storageSlots[0]`。安全配置应把公开零基 `stationBeltStorageIndex` 转成 `+1`，且只允许 raw `0 -> 目标值`；已有非零值不得直接清除或重定向。
- 直接证据：母星需求 PLS `916` 已有槽 0 钛块 100、Output port 0 与首带 `1015`，36 段有向链完整到末带 `998`，末端 sorter `1016` 又精确连接仓 `768`；但 10 秒后站内仍为 100、末带空、仓中钛块为 0。端口快照同时给出 `direction=Output`、`beltEntityId=1015`、raw `storageIndex=0`。当前程序集的 `StationComponent.UpdateOutputSlots` 先执行 `storageIdx - 1`，而 `UIBeltBuildTip.SetFilterToEntity` 与 `UISlotPicker.SetFilterToEntity` 都把选择列表索引直接写入 `station.slots[outputSlotId].storageIdx`；列表 0 为 None、列表 1 为 storage 0。源码据此新增现有 configure 双阶段模式 `logistics-station-belt`：绑定 station configuration hash、端口/带双向身份、公开仓槽与实际 item，只允许空选择器和 `counter=0`，提交后保持站库存/订单/能源/fleet、玩家库存、端口拓扑与其他选择器不变。完整 Release solution 0 warning/0 error，114 tests passed（Contracts 13、Core 82、MCP 19）。主档先正常保存到 tick `10449537`，旧进程正常退出；新同批 DLL 哈希为 Plugin `941951FA0F5B8ADDEE16EF1B66B0ABA98664BF79474F49AEFACD6668071B0C76`、Contracts `1DD0C244463FBB78EA4C769990341E8699E3372E4F4155693869961B77097274`、Core `F032394C60CC9840FEE854A2ADC3702D7EA942378DBA75D939EE24894999DDF6`，逐文件与部署目录一致。恢复动作 `2d524b3d-63f2-4f16-a083-7d5708e15390` 只采用该 tick 的 ticket-bound exact primary；配置动作 `26a83d4b-ebf2-49b8-9d06-5e6c1321b78f` 随后一次完成 raw `0 -> 1`，目标 item `1106`。12 秒后站存 `100 -> 22`、sorter `771` 实际携带钛块、制造台 `767(recipe 26)` 满电工作；独立完整周期后钛晶石仓 `769` 从空增长至 9，需求站又正常派出无人机补货并恢复到 94 钛。保存动作 `e963b45e-b849-4523-a0fa-81c3e2af13da` 已持久化 tick `10456408`、write health healthy。
- 限制或反例：本次只 live 验证 port 0 选择 storage slot 0；非零仓槽、其他端口、星际站和输入口的映射仍需分别复读。`counter=0` 是本版主动采用的安全假阴性，若未来要在活跃端口切换，必须先独立证明货物归属与重定向守恒，不能放宽现有子集。
- 复验触发：正常保存当前主档、停机同批部署、exact-primary 恢复、端口 0 raw `0 -> 1`、PLS 钛库存下降、仓 `768`/制造台 `767` 出现钛块、钛晶石输出增长、普通保存，或 DSP/程序集版本变化。
- 关联：EXP-007、EXP-021、EXP-028、EXP-068、EXP-070、EXP-097、EXP-110、`BuildingConfigurationModes.LogisticsStationBelt`、`NormalGameActionCoordinator.StructuredActions.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-03（正常停机部署后 exact-primary 恢复；PLS port 0 raw `0 -> 1`，钛块自动流入钛晶石线、专用仓 `0 -> 9` 并正常保存）。

### EXP-118 — 双原料专用支线可经 sorter 注入共享主干并由过滤仓安全分流

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、两个专用源仓、以 belt-to-belt sorter 汇入既有有向普通带、目标仓已在无货窗口配置独立物品过滤；不证明无限上游供给。
- 当前结论：不同原料不必各铺一条全长终点带；可以让第二条专用支线通过 sorter 的带侧虚拟槽注入既有共享主干，再由末端双过滤仓按物品分流给同一双原料设备。验收必须同时包含两条唯一有向路线、连接端点、源库存下降、目标仓/设备输入、最终产物消费者增长和普通保存，不能只看主干“有货”。
- 直接证据：钛晶石路线为 `storage 769 -> sorter 1115 -> belts 1057…1114 -> sorter 1116 -> storage 775`，有向遍历为恰好 86 个唯一 belt；金刚石路线为 `storage 717 -> sorter 1212 -> belts 1117…1210 -> sorter 1211 -> trunk belt 1053`，支线有向遍历为恰好 94 个唯一 belt，注入 sorter 使用带侧虚拟槽且没有覆盖主干原有前后连接。独立运行窗口中金刚石源仓从 `300 -> 259 -> 194 -> 145`，目标仓 `775` 最终保有 88 钛晶石和 11 金刚石，lab `774` 以 `6/6` 输入、满供电工作；研究站 `84` 同时保有蓝/红/黄矩阵点，其中黄矩阵为 8124，科技 `1414` 从 `90000 -> 91303 -> 93121/144000`。保存动作 `eb58a029-d5c9-47d0-9b80-afcc682c28f7` 正常持久化 tick `10637902`，fresh 会话仍为 healthy、无 checkpoint，journal `43/43` durable。
- 限制或反例：金刚石制造台 `715` 因高能石墨为 0 已停，钛晶石制造台 `767` 的有机晶体输入也已耗尽，仓 `769` 已清空；当前成功窗口依赖仓 `717/769/775` 的有限存量。因此本条只证明本地运输拓扑和最终消费者，不证明 v0.3 要求的持续跨星供应。共享主干以后若增加第三种物品，必须重新验证目标过滤、带容量和无串料，不能由本次双物料结果外推。
- 复验触发：任一源仓重新接入永久上游、共享主干扩容或混入第三种物品、目标仓过滤变化、save/restart 后持续流量复验、双星球 ILS 接管钛/硅或 DSP 版本变化。
- 关联：EXP-021、EXP-028、EXP-037、EXP-068、EXP-070、EXP-073、EXP-079、EXP-090、EXP-115、EXP-117、ROADMAP v0.3。
- 最近复验：2026-09-03（86 格钛晶石主干与 94 格金刚石支线均完成；源、在途、仓、lab、研究与正常保存形成完整证据链）。

### EXP-119 — 已验证锚点链会被后续扩建改变，反向通行不能继承旧结论

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、曾经逐段验证为 Walk 的工厂内陆地锚点链，以及链路附近后来新增了建筑、传送带或 sorter 的情况。
- 当前结论：锚点只证明当时、该方向和该落点的普通移动终态；工厂扩建后必须逐段 fresh 复验，不能因旧路线正向成功就假设反向仍可通行。某段被 180-tick 位移看门狗截停后，应先读当前位置周围实体并退回上一稳定锚点，不能重放同一直线，也不能把失败误归因于能量。
- 直接证据：从风机 `713` 沿旧链反向移动到液罐 `165`、电塔 `182`、电塔 `143` 和电塔 `133` 的前四段均由正常 MoveTo 完成并复读 Walk。紧接着从 `133` 直达旧锚点风机 `82` 的动作 `cc32b8bd-6add-4046-8599-78740c8e0a93` 在 191 tick 内物理位移不足 0.75 m、剩余 39.42 m，被专用看门狗明确终止；fresh 玩家仍为 Walk/0、约 340 MJ。周围复读显示当前位置距新 PLS 出料末带 `998`/sorter `1016` 仅 0.84 m、仓 `768` 1.76 m、电塔 `133` 2.52 m；这些对象属于旧路线首次验证之后的扩建。没有重放失败动作，唯一回退 `db3ed711-b10f-4531-9f2c-19e6f6407394` 返回上一稳定锚点 `143` 外缘并复读 Walk/0。
- 限制或反例：本条没有证明哪一个实体单独造成碰撞，也不意味着所有锚点链都必须废弃；未变化且双向重新验证的段仍可复用。实体距离只是碰撞解释证据，不能替代动作看门狗终态或地形状态。
- 复验触发：锚点附近任何施工/拆除、同一段换向、到达容差变化、移动看门狗或碰撞半径变化、DSP 版本变化。
- 关联：EXP-021、EXP-036、EXP-061、EXP-066、EXP-076、EXP-077、EXP-116、`CanonicalStateHash.PlayerAction`。
- 最近复验：2026-09-03（扩建后的 `133 -> 82` 反向段立即碰撞终止；读取近邻实体后退回 `143`，没有重放）。

### EXP-120 — 复用休眠纯料仓和既有有向带可最小化恢复下游，但有限补料不等于永久上游

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、目标生产设备和既有传送带/sorter 拓扑仍完整、旧源仓为空且本次只装入单一正确物料的恢复场景。
- 当前结论：遇到下游断料时先追踪旧源仓到最终消费者的完整有向拓扑；若链路仍完整，只需向空纯料仓守恒补料即可恢复整段生产，不应重复造平行带或直接往最终研究站塞成品。验收仍须追到中间设备工作和最终消费者增长，并明确有限补料的库存边界。
- 直接证据：空仓 `30` 的既有拓扑保持 `30 -> sorter 83 -> belts 55…42 -> sorter 71 -> mixed storage 26 -> sorter 722 -> assembler 73`；assembler `73` 的两条输出又分别经 belts `295…291` / `329…325` 和 sorters `297/331` 进入蓝矩阵站 `76`。动作 `37c550a6-5a9c-4e1c-b900-7bafe41c2787` 从混合库存仓 `562` 守恒取得 200 磁铁，下一唯一 transfer 把玩家 200 磁铁全部装入空仓 `30`。8 秒后仓 `30` 尚有 187、磁线圈台出现磁铁输入、蓝矩阵站开始工作；随后仓 `30` 清空，磁线圈台输出达到 15、蓝矩阵站同时持有 7 磁线圈/6 电路板并工作，研究站蓝矩阵点恢复到 37540，科技 `1414` 从停点 `97457 -> 99100 -> 112415 -> 115920/144000`。
- 限制或反例：本次 200 磁铁来自已有仓储，不是新矿持续流量；仓 `30` 已再次清空，所以只证明既有链路可恢复和物料去向正确。磁铁永久上游仍需新铁矿机、冶炼和自动进仓；研究随后又因红矩阵暂时只余 10 点停下，说明恢复一个瓶颈后必须继续追到新的最早缺项。
- 复验触发：新铁矿磁铁线接入仓 `30`、旧带或 sorter 改造、源仓混入第二种物料、save/restart 后恢复、最终研究不增长或 DSP 版本变化。
- 关联：EXP-021、EXP-028、EXP-067、EXP-073、EXP-078、EXP-090、EXP-115、ROADMAP v0.3。
- 最近复验：2026-09-03（纯磁铁补入空仓后，既有磁线圈→蓝矩阵→研究链逐层恢复；有限库存耗尽边界已保留）。

### EXP-121 — 原生可建不等于玩家净空，带路应按完整 plannedPath 复核建筑距离

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、密集设备区内的自由传送带、`prepare_build.plannedPath` 与玩家后续步行净空。
- 当前结论：传送带 prepare 成功只证明 DSP 接受这条带，不能证明带与风机、电塔、矿机等基座之间留有玩家可通行空间。提交前应对完整 `plannedPath` 的每个落点计算到现有非带建筑的最小距离，优先选择更空旷的候选；若直达方向穿过基座，则先向空旷侧建短引出段，再从末端转向最终目的地。该检查补充原生碰撞校验，不能替代它。
- 直接证据：围绕熔炉 `1216` 与仓 `1217` 的无副作用候选中，多条原生合法带与风机 `1214` 的最近离散点仅约 `2.52 m`；最终选择的 9 格出料带 `1231 -> … -> 1232` 保持约 `3.77 m` 的非带建筑净空，并由 sorter `1233/1234` 完整接成 `1216 -> belt -> 1217`。从仓 `1217` 直向旧蓝链仓 `30` 的原生合法候选距风机仅约 `1.26 m`，另一个候选甚至与电塔 `1215` 的观测中心重合；未提交这些路线。实际先向反侧建立 6 格引出带，plannedPath 到既有对象的最小离散距离约 `7.47 m`，再以 sorter `1241` 从仓出料。后续从带 `1312` 向仓 `30` 的直达候选虽全部获 DSP 接受，却会贴近仓 `854/862/871` 至 `0–1.26 m`、制造台 `853/861/869` 至 `1.23–2.52 m`；这些候选均未提交。实际依次采用非带最小净空约 `7.40 m` 的 30 格段、`8.64 m` 的 24 格段、`5.17 m` 的 26 格段与 `3.77 m` 的 23 格段绕出设备群，玩家全程留在已验证陆地点，不以带端充当移动锚点。下一批又先手采/手搓补足建材，再用 15/9/6/4/6/10/4/4 格八段穿过电塔与旧制造台之间的原生窄口；每次提交前都比较完整 plannedPath，拒绝了与设备中心重合或仅 `0–1.3 m` 的直切方案。窄口没有兼具前进量和 `>3 m` 净空的候选时，只在人物不需要沿带行走、施工距离仍小于 80 m 的前提下采用最低约 `2.52–2.76 m` 的短段，随后立即从对侧重扫；最终主干增至 238 格且仍为单链。
- 限制或反例：这里的距离由实体中心和 plannedPath 离散落点计算，不是 DSP 碰撞盒边界，也尚未用玩家沿新带全程行走完成独立复验；`3.77 m` 不能固化为普适安全阈值。长带续接、设备旋转或新增建筑后都要重新计算和实走核验。
- 复验触发：从当前末带 `1393` 继续向仓 `30` 延长、玩家实际经过新通道、附近新增/拆除设备、原生碰撞盒或移动半径变化、DSP 版本变化。
- 关联：EXP-036、EXP-061、EXP-066、EXP-077、EXP-119、`PrepareBuildRequest.PathEnd`。
- 最近复验：2026-09-03（长带扩到 238 格时逐段复核完整 plannedPath；拒绝设备重合和 0–1.3 m 直切候选，机甲始终留在独立验证的陆地 Walk 点）。

### EXP-122 — 传送带可跨水，沿带移动仍必须逐段验证 Walk

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、普通地表传送带路径、以带端坐标作为玩家移动锚点的场景。
- 当前结论：传送带的原生合法性、设备净空与玩家脚下陆地是三项独立条件。沿 plannedPath 建成的长带可以跨越海面；即使同一路线上前三个锚点都稳定 Walk，也只能逐段继承到最后一个已复验锚点。每段移动完成后必须 fresh 读取 `movementState/speed`，发现 Drift 时从当前点做几米有界侧移找岸，不能把动作的“到达距离”终态当作落地证明，也不需要回滚存档或重走整条路线。
- 直接证据：玩家沿已完成磁铁主干依次移动到带 `1239`、`1254`、`1281`，动作 `02849b63-66fa-4f18-b5b5-bf15f3022e00` / `32c57a5f-299b-45dd-b4c0-2a2a26a46a9f` / `266eb059-1ed1-4c08-a86b-7ea63a098c18` 均在 3 m 内完成，2 秒后各自为 Walk/0。动作 `ece33010-6572-4480-b9b7-8054de500126` 同样报告距带 `1312` 仅 2.76 m，但 fresh 读回为 Drift、速度约 `0.14 m/s`；附近 21 m 内没有非带建筑，排除了本次基座卡脚解释。从该点发出的 6 m 和 10 m 有界移动 `b38792db-9ea8-4711-9a79-03ba1beb631c` / `6efe904a-29f4-4d68-b308-f3d643694f18` 仍为 Drift；第三次仅瞄准仓 `863` 外 4.5 m 的落点，动作 `8861f201-3345-4b71-90eb-23febe93a419` 完成后 fresh 为 Walk/0。随后 `2b9d4274-b3b3-4c56-9637-2f05ff8968b5` 到无线塔 `180` 外缘仍为 Walk/0，核心由约 `186.11 MJ` 自然充满到 `400/400 MJ`。全程没有回档、重放或开新档。
- 限制或反例：当前没有地形高度/水深只读字段，所以“向某方向几米必然上岸”仍不能泛化；本次前两次短移仍在水中就是反例。只能以每次 fresh Walk/Drift 终态逐步收敛。带上货物运输不受本条否定。
- 复验触发：任何新的 Drift 上岸、沿带巡检、地形/移动实现或 DSP 版本变化。
- 关联：EXP-053、EXP-061、EXP-066、EXP-114、EXP-116、EXP-121。
- 最近复验：2026-09-03（末带浅海 Drift 经两次未上岸短移和第三个仓外落点收敛为 Walk，后续无线充满）。

### EXP-123 — 带路实体 ID 顺序不是拓扑顺序，自由末端必须以连接和位置复读

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、从已有 belt source 续建的多段 `prepare_build/commit_build`、`plannedPath`与 action `targetObjectIds`。
- 当前结论：不能用“最大 objectId”或 action 返回数组的首末顺序猜带路方向。每段完工后应以 `plannedPath` 最后坐标附近的实体为候选，再确认其只有入连接、没有输出；续建时绑定该实体的 fresh `endpointStateHash`。完整验收还应沿有向连接检查无分支和无意外外接。
- 直接证据：从 belt `1312` 续建的 30 格动作生成实体 `1313…1342`，但最大 ID `1342` 位于源端且连回 `1312`，真正自由末端是位于 planned end 的 `1322`。动作 `7ea82222-bd7c-4c54-ad44-26a24f7be20c` 的 `targetObjectIds` 则为 `1415…1393`，自由末端是 `1393`。后续八段继续出现正序、倒序和非单调返回；例如动作 `a026ebbc-7ea6-44ff-916d-4c6da5d792c8` 返回 `1470/1473/1472/1471`，只有 planned end 上的 `1471` 是自由输出端。fresh 全厂复读从 sorter `1241` 起共得到 239 个对象，其中 238 个为 belt；没有多输出、环路或旧工厂外接，唯一源仍是 `1217 -> 1241 -> 1236`，唯一自由末端已推进到 `1471`。
- 限制或反例：当前只观测到基础传送带的多段施工，不将 ID 分配规律推广到分流器、垂直带或其他建筑。坐标近似只用于找候选，不能替代最终端点哈希和连接读回。
- 复验触发：下一段长带、目标绑定 belt build、分流/垂直带实现、完工对象返回结构或 DSP 版本变化。
- 关联：EXP-007、EXP-018、EXP-027、EXP-032、EXP-070、EXP-121、`PrepareBuildRequest.PathEnd`。
- 最近复验：2026-09-03（新铁路线 22 格实体 ID `1513…1534` 仍非拓扑顺序；稳定后有向遍历唯一得到 `1534 -> … -> 1532`，再次否定按 ID 推断首尾）。

### EXP-124 — 原生带路 prepare 可能回折到既有输入带，候选器必须排除旧带重叠

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、从自由 belt 末端生成多角度短路径候选、以 `prepare_build.plannedPath` 做无副作用路线比较。
- 当前结论：原生 prepare 成功只表示候选可解释为合法带路，不保证它是在向新的空网格延伸；球面量化和转角可能让 plannedPath 的后续点回到既有 belt，而原生 commit/完工读回也可能接受并生成同坐标的第二条独立 belt。因而重叠检查是 commit 前的强制安全门：忽略第一个 source 点后，把其余 plannedPath 与全部既有 belt 坐标复核并排除重叠，同时显式报告相对目标的净前进量。最终 commit 仍必须 fresh 读取 source/player 哈希；若已误建，先按对象连接重建两条有向拓扑并停止延伸，不能因画面或坐标重合而假定它们已经并线。
- 直接证据：从自由末端 `1455` 扫描时，原生 prepare 接受了 planned end `(-100.510437,-43.672276,-167.5423)`，该坐标实际已经属于上游 belt `1443`；从 `1462` 的另一个候选也量化回既有带段。两者都没有 commit。只读脚本 `scripts/find-belt-route-candidates.ps1` 随后增加 `0.25 m` 既有 belt 重叠排除、`minimumNewEntityCenterClearance` 与 `destinationProgress`。后续从新铁线末端 `1561` 提交的 22 格原生施工动作 `48fe4427-d32e-4f6b-b01b-3d599cbb4268` 虽以 22/22 预建筑和计划连接读回正常完成，却实际生成了与既有磁铁线同坐标、彼此不连接的重复 belt：`1582=1444`、`1583=1445`、`1581=1442`、`1580=1441`，`1579` 又与 `1431/1440` 同点。fresh 拓扑证明旧线末端 sorter `1488` 仍只携带过滤磁铁，而新线 `1535` 只携带铁块；随后候选器从 `1582` 对直线和侧向方案全部因旧带重叠拒绝，反证原生成功不能替代全厂 belt 占位检查。
- 限制或反例：`0.25 m` 只用于识别同一网格中心附近的既有基础带，不是通用碰撞半径；合法并线、分流器、垂直带或显式连接既有 belt 的方案需要不同语义，不能直接复用这个排除规则。当前普通 dismantle 只允许资源矿机，无法用受支持写入口回收重复 belt，因此不得为了清理而旁路游戏规则；候选器仍只读，也不替代 commit 后的旧实体冲突复读。
- 复验触发：下一次候选扫描、显式接入既有 belt、分流/垂直带支持、网格量化或 DSP 版本变化。
- 关联：EXP-018、EXP-027、EXP-042、EXP-070、EXP-121、EXP-123、`PrepareBuildRequest.PathEnd`。
- 最近复验：2026-09-03（新增一次真实 commit 反例：五个以上新铁带与既有磁铁带精确同坐标但拓扑独立；候选器随后全部拒绝继续重叠，故本条升级为 validated）。

### EXP-125 — 无副作用 prepare 仍占短期计划容量，密集候选扫描必须分批等待过期

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 `NormalGameActionCoordinator`、默认 60 秒 plan token 生命周期、128 项 `PreparedPlanStore` 容量，以及自动生成大量 `prepare_build` 候选但只 commit 一个方案的工具。
- 当前结论：prepare 不创建预建、不消耗物品，但每个成功候选都会占用一个 normal-game plan 槽直到 commit、显式移除或过期；因此“只读/无副作用”不等于“无资源成本”。候选扫描应把尝试数限制为明显低于容量的批次，等本批最晚 token 过期后再继续，最终返回前也默认等待清空；真正施工随后仍要重新读取 player/source 并生成一个全新的 plan。
- 直接证据：从末端 `1471` 执行大角度候选扫描后，下一次唯一施工的 fresh `prepare_build` 在没有 action ID、没有库存或拓扑变化的情况下返回 `SERVER_BUSY: Too many normal-game plans are active.`。源码复核确认 `NormalGameActionCoordinator` 使用容量 128 的 `PreparedPlanStore`，配置默认 `PlanTokenLifetimeSeconds=60`，`Add` 会先清理已过期 token。该失败没有 commit，也未计入游戏写动作；`scripts/find-belt-route-candidates.ps1` 随后改为默认每批最多 64 次尝试、批间等到最晚过期时间并刷新 player/source、最终返回前默认等待过期。
- 限制或反例：等待过期依赖当前配置和本机单客户端假设；若另一个合法调用者同时创建计划，仍可能短暂出现 `SERVER_BUSY`。失败 prepare 可以在确认无 action ID 后重新 fresh 规划，但绝不能把这个规则用于重放已接受 commit。未来若公开安全的 plan-discard API，应优先显式释放而不是等待。
- 复验触发：下一次超过一批的扫描、plan 生命周期/容量配置变化、并发客户端、公开 discard API 或 DSP/插件版本变化。
- 关联：EXP-007、EXP-009、EXP-018、EXP-027、EXP-042、EXP-063、EXP-123、EXP-124、`PreparedPlanStore<T>`。
- 最近复验：2026-09-03（真实容量拒绝发生在 prepare 阶段且无 action；源码参数与脚本分批/过期策略一致）。

### EXP-126 — 长带连通不代表末端分拣器有电，通料前必须逐个复读供电

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、跨越多个电网覆盖区的长传送带、以分拣器桥接两段 belt 或把 belt 分流到制造设备的恢复施工。
- 当前结论：传送带本身不需要供电，因此完整有向带拓扑和成功的 sorter 施工都不能证明货物会跨过最后一跳。接通上游前应逐个复读所有新 sorter 的 `powerNetworkId/powerServeRatio`；接通后若源仓、源 sorter 与长带均有流量而末端长期不取货，应优先检查末端 sorter 是否为 `network=null/0`，再判断运输延迟或带路断点。补塔后仍须以 sorter 实际携货、目标设备输入增长和最终消费者增长验收，不能只看电塔落位。
- 直接证据：新磁铁源保持 `1217 -> 1241 -> 1236` 出料，仓 `1217` 从 1594 自动增至审计时 2190，`1241` 持有 item `1102`；完整分页复读又证明 `1236 -> … -> 1479` 是连续 246 格有向 belt、无断点或回环。新旁路按安全顺序建成并过滤 `1486 -> sorter 1488(filter 1102) -> belt 63 -> … -> belt 48 -> sorter 1487(filter 1102) -> assembler 73`，两只下游 sorter 均为 network 1、serve ratio 1.0。最后接通的 `1479 -> sorter 1489 -> 1482` 虽通过双端连接验收，却连续多个观察窗保持 `Picking/stack 0`，其 `powerNetworkId=null`；制造台 `73` 仍停在 1 磁铁/3 铜块，蓝矩阵站 `76` 与研究站 `84` 的蓝矩阵均为 0，科技 `1414` 保持 `130937/144000`。这排除了“连接成功即有流量”，并把最早故障定位为 1489 未覆盖供电，而不是 246 格带路断裂。随后普通 replicator 动作 `2d614591-0fc2-47d0-91d2-c1e54a3dc33f` 递归手搓 1 座电力感应塔，唯一施工动作 `d2d0d17a-9d7e-40ba-9e11-7164c80b18d3` 将其建为 `1490`；fresh 复读立即看到 sorter `1489` 进入 network 1、serve ratio 1.0 并处于 Sending、实际携带 1 个磁铁。下游 `1488/1487` 同时工作，制造台 `73` 恢复磁线圈，蓝矩阵站 `76` 恢复生产，研究站 `84` 的蓝矩阵点从 0 连续增至 12340；科技 `1414` 从 `130937 -> 132152 -> 135842 -> 139161 -> 142345 -> 144000`，并于 tick `11098574` 正常完成。保存动作 `267a063c-dc87-4a44-a183-efa1a436096b` 又把该闭环持久化到 tick `11099621`，write health 保持 healthy。
- 限制或反例：`network=null` 不应被推广为所有停在 Picking 的唯一原因，源带无货、过滤不匹配、目标满载或端点被改写仍须分别复读。传送带传播时间也不能用固定秒数替代实际货物证据；本次验证的是当前普通电力感应塔覆盖和基础 sorter，增删电网设施或 DSP 版本变化后仍需复验。
- 复验触发：任何跨电网长带、增删电塔、save/restart 后恢复、供电 DTO 或 DSP 版本变化。
- 关联：EXP-024、EXP-025、EXP-028、EXP-060、EXP-073、EXP-095、EXP-120、EXP-123。
- 最近复验：2026-09-03（仅 8 格的新矿机带路末端 sorter `1509` 也在两端设备都属主网时落到 network 0；补塔 `1510` 后立即满电，熔炉 `1500` 从零输入恢复工作并产出铁块，证明问题不限于长距离主干）。

### EXP-127 — 供料链持续工作不等于具备下一科技的吞吐余量

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、单条磁铁→磁线圈→电磁矩阵链、连续切换到更高矩阵需求科技后的研究吞吐判断。
- 当前结论：产线恢复、源仓有货、末端 sorter 持续工作，只能证明供料连续，不能证明产速高于新科技的消费速率。选择新科技后必须复读同一研究站各矩阵缓冲和科技上传斜率；若某色从正数清空而其他颜色保留，应把它分类为吞吐瓶颈，而不是误判为线路再次断开。后续可以并行扩产或接受限速等待，但两种选择都要保留“持续性”和“吞吐余量”的证据边界。
- 直接证据：高强度钛合金完成后，研究站 `84` 在 tick `11121093` 尚有电磁/能量/结构矩阵点 `26740/36010/36000`。动作 `9e3a2adb-0b12-41cf-85c6-5aac3cff523b` 正常选择星际物流系统 `1605`，journal sequence `44` 在 tick `11122095` 持久化。到 fresh 审计 tick `11148734`，科技只上传 `1337/216000`，同站能量/结构矩阵仍有 `38070/36926`，电磁矩阵已为 0；同时桥接 sorter `1489` 仍在 network 1、serve ratio 1.0、实际携带磁铁，磁铁源仓 `1217` 仍有 2651，排除了供磁链断裂。该正反对照证明当前限制是单线蓝矩阵吞吐，而不是电网或拓扑故障。
- 限制或反例：当前只有一项三色科技和一条蓝矩阵线的样本；研究站内部 count 的单位不能直接等同于背包物品数，也不能仅凭两个离散读数推导稳定每分钟产率。红/黄上游仍有有限缓存，未来也可能先后转为新瓶颈。
- 复验触发：增加磁线圈/蓝矩阵产能、1605 继续上传、任一矩阵缓冲归零、save/restart 后恢复、研究功率或 DSP 版本变化。
- 关联：EXP-028、EXP-033、EXP-073、EXP-115、EXP-120、EXP-126、ROADMAP v0.3。
- 最近复验：2026-09-03（1605 选择后蓝缓冲从 26740 清空而红/黄保留；供磁 sorter 仍满电携货）。

### EXP-128 — Move 目标未达不等于已取得的安全位移必须作废

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 180-tick 物理停滞看门狗、动作已明确 `action_failed`、玩家在终点前取得了显著位移且 fresh 状态仍为地表 Walk。
- 当前结论：移动动作失败只证明请求目标没有到达，不会自动回滚此前的普通物理位移。不得重放或把该动作改称成功；但 fresh 复读若证明当前位置为 Walk/速度 0、能量安全且已进入下一业务对象的原生操作范围，可以把当前位置作为新的独立起点继续工作，不必为了形式上碰到旧锚点而额外脱困。反之若 Drift、仍有残留订单或紧贴碰撞体妨碍下一动作，仍按既有移动恢复规程处理。
- 直接证据：从 `(-137.4763,-30.5663,-142.3228)` 前往无线塔 `180` 的动作 `9bcc4013-4395-41d6-bb27-a7f7551dadae` 前进约 27 m 后，在剩余 10.46 m 时因 181 tick 内位移不足 0.75 m 被明确终止。fresh 玩家位于 `(-116.7463,-28.9110,-159.8327)`、Walk/0、核心约 `397.8/400 MJ`；2.10–6.87 m 内是既有仓 `849`、制造台 `848/853` 和 sorter `855/856/852/857`，解释碰撞风险但不构成能量或隔离故障。更重要的是，新位置距钛块仓 `768` 78.06 m、钢材仓 `792` 69.16 m，已进入普通操作范围；没有重放失败 Move，下一唯一写动作直接从该 fresh 起点把 100 钛块由仓 `768` 守恒取入玩家，双边 `364 -> 264` 与 `0 -> 100` 成立。
- 限制或反例：这是一个“失败终点恰好可继续业务”的样本，不意味着所有停滞点都安全，也不能把 80 m 附近的观测距离固化成通用有效半径；具体 action prepare 仍会以当前 build area、地形、玩家哈希和目标状态独立拒绝或接受。
- 复验触发：下一次长 Move 中途停滞、失败后直接执行非移动动作、当前位置为 Drift/低能量/密集夹缝、移动或建造范围实现变化、DSP 版本变化。
- 关联：EXP-009、EXP-021、EXP-036、EXP-061、EXP-066、EXP-077、EXP-116、EXP-119。
- 最近复验：2026-09-03（携带 100 铁块前往旧铁仓的 Move 在剩余 47.98 m 时由 180-tick 看门狗终止；fresh 停点距仓 `870` 1.81 m、sorter `873` 0.92 m，确认是碰撞而非断能。向实体更稀疏侧横移 8 m 的独立 Move 成功，再从新起点直达目标并完成守恒投料；失败动作没有重放）。

### EXP-129 — 复用过滤多料仓能防串料，但不能保证同物料的产线分配优先级

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：一个已有多种精确过滤出口的混合仓，同时给两台不同配方设备新增或保留相同物料出口的普通生产场景。
- 当前结论：所有 sorter 在空载窗口先锁定正确 filter 后，混合仓中的无关物料不会误入新设备；但当两台设备都合法需要同一种物料时，过滤只保证类型，不保证配额或优先级。投料验收必须把两台设备缓冲和在途 sorter 都纳入去向解释。若某批材料必须保留给指定产品，应使用独立仓、独立带或按可证明配额分批投料，不能把 filter 当成调度器。
- 直接证据：仓 `899` 原有 651 硅石，并通过 `902/903/904/905` 分别以钢材、钛块、处理器、粒子容器 filter 供给行星物流站制造台 `898(recipe 93)`；新钛合金线只在 `1493/1494/1495` 分别锁定钛块、钢材、硫酸后才启用熔炉 `1491(recipe 66)`。20 硫酸进入后只在仓和钛合金熔炉间形成 `4 + 16`，硅石始终保持 651，证明无关物料没有串入。随后各 100 钛块/钢材装入同仓，两条配方的同物料 sorter 同时合法取货：里程碑保存后的连续读回中，源仓仍有钛 30/钢 41，熔炉有钛 8/钢 8/酸 4，PLS 制造台已有钛 54/钢 42，旧 sorter `902/903` 仍各携 1。钛合金线实际消耗 8 钛/8 钢/16 酸并在仓 `900` 产出 8 item `1107`；journal sequence `45` 于 tick `11172619` durable 记录首次产线产出。该现场同时证明过滤防串料和同物料竞争确实并存。
- 限制或反例：跨对象快照按顺序读取而非同 tick 原子快照，运行中的 sorter 会造成一件级瞬时差异；不能用这一组离散数值推导固定分流比例。PLS 预装的钛/钢是后续所需材料，不算损失，但下一批钛合金不能假设仍有同样份额。
- 复验触发：继续向仓 `899` 投钛/钢、PLS 制造台获得处理器/粒子容器并完成生产、改用独立钛合金仓、增加同物料第三出口、save/restart 或 DSP 版本变化。
- 关联：EXP-028、EXP-037、EXP-049、EXP-062、EXP-074、EXP-079、EXP-103。
- 最近复验：2026-09-03（PLS 制造台仍缺处理器/粒子容器且已预装的 80 钛/80 钢不变；共享仓续入 80 酸和各 32 钛/钢的两个精确批次后全部由钛合金支路消耗，651 硅石不变，专仓 `80 -> 100 -> 120`。该排程继续避免合法同料竞争，但不把过滤误称为通用优先级控制）。

### EXP-130 — 单个矿点锚定的最优姿态不等于整簇矿脉的最优姿态

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、普通固体矿机、同一 `groupIndex` 内多个未占用矿点，以及当前按绑定矿点生成有界径向/角度候选的 `prepare_build`。
- 当前结论：EXP-112 的“比较全部合法姿态”必须明确限定候选域。只绑定最近矿点时，规划器只比较围绕该锚点生成的姿态；若要优化整簇覆盖，应对同组每个 `minerCount=0` 的矿点分别执行无副作用 prepare，按 `plannedResourceNodeIds` 的唯一数量全局排序，再只提交最佳 token。完工后仍要求实际节点集合与计划集合精确相等。这样优化的是完整离散候选域，不是把第一个锚点的局部最优误称为矿簇最优。
- 直接证据：planet `104` 的铁矿 group `4` 有 14 个未占用节点。只以最近节点 `55` prepare 时，原生局部最优计划仅覆盖 5 点，因此没有 commit；随后以完全相同的玩家状态逐个检查该组 14 个锚点，最佳计划覆盖 `44/45/46/47/48/49/52/53` 共 8 点。唯一 build 动作 `ab79b65f-bffc-4c43-9c98-63673c9eba66` 建成矿机 `1496`，fresh 实际集合与该 8 点计划仅顺序不同、集合完全一致；接入风机后矿机真实产出并把内部铁矿缓冲填到 50。
- 限制或反例：这仍是规划器当前径向/角度网格上的离散最优，不证明连续球面上的数学全局最优；大矿簇或批量建筑不能无界 prepare，否则会触发 EXP-125 的短期计划容量。候选扫描还不替代后续出料、供电和玩家净空验收。
- 复验触发：下一座多节点矿机、不同矿簇几何、候选生成算法变化、单组节点数接近计划容量、显式 preferred pose 或 DSP 版本变化。
- 关联：EXP-037、EXP-042、EXP-045、EXP-068、EXP-070、EXP-112、EXP-121、EXP-125、`ResourceCoverageSelection`。
- 最近复验：2026-09-03（同一 14 点铁矿簇中，单锚点 5 点计划被拒绝；全锚点扫描找到并落成 8 点矿机，计划/实际集合精确一致）。

### EXP-131 — 普通矿机的原生出料端先接传送带，不能假设可直接接分拣器

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、普通固体采矿机、当前版本的 belt/inserter endpoint 读取与 `prepare_build`；不外推到大型矿机、油井、水泵或物流站。
- 当前结论：矿机实体可公开一个自由的原生 belt output port，但当前版本不公开可供普通 sorter 使用的 inserter attachment pose。即使矿机与熔炉中心距只有约 4.5 m，也必须先用 source-bound belt 从矿机原生输出端引出，再从自由末带以 sorter 接入熔炉；不能因几何距离近就反复尝试矿机→设备直连。失败 prepare 没有 action ID、物品或拓扑变化，不计写入也不得转成旁路写状态。
- 直接证据：8 点铁矿机 `1496` 与铁块熔炉 `1500(recipe 1)` 中心距约 4.5 m，二者 endpoint hash 均为 fresh；直接 prepare 普通 sorter 明确返回 `BUILD_CONNECTION_INVALID: One bound endpoint exposes no current-version inserter slot or belt attachment pose`，没有 commit。随后以同一矿机为 source、自由 `PathEnd` 为终点的唯一动作 `2e71eedb-1efb-4505-9f65-bddf9e7c364a` 正常消耗 8 条带并建成 `1496 -> belts 1501…1508`；矿机 slot 0 双向指向首带，末带 `1508` 只有来自 `1507` 的输入、仍是自由输出，且距熔炉 2.81 m。审计时熔炉输入仍为 0，故本条只确认端口和正确拓扑，不提前宣称铁块开产。
- 限制或反例：当前只验证一台普通矿机；未来 endpoint reader 若显式支持矿机 inserter pose，或 DSP 本体更改建筑端口，直连结论必须重测。自由末带在接 sorter 前仍不是完整产线，后续必须以 sorter 双端反查、实际携矿和熔炉输入/输出增长验收。
- 复验触发：`1508 -> sorter -> 1500` 完工并通料、另一台普通矿机、其他资源建筑、endpoint 提取逻辑或 DSP 版本变化。
- 关联：EXP-018、EXP-027、EXP-037、EXP-045、EXP-070、EXP-103、EXP-112、EXP-123、`GetFreePortPoints`、`GetInserterEndpointPoints`。
- 最近复验：2026-09-03（矿机→熔炉直接 sorter 被无副作用拒绝；矿机原生口成功建成 8 格有向自由带，末端进入熔炉 sorter 范围）。

### EXP-132 — 未取得动作终态时，单次 fresh 实体扫描仍可能处于预建筑收尾中

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：本地调用显示窗口先于长带 build 结束、未保留 action ID、施工由无人机继续完成，以及依赖首尾 belt 身份的后续连接。
- 当前结论：展示进程超时后不重放是必要条件，但一份 fresh 工厂快照并不自动等于施工已经稳定。若 action ID 未取得，必须同时等待 `constructionDrones.pendingBuildTargets=0/working=0`，并用至少两次稳定实体/连接读回确认没有新 belt 从所谓“自由起点”继续出现，才能把首尾用于下一次连接。若已经提前接到后来变成中段的 belt，必须以 fresh 双端和有向遍历重新证明注入仍朝正确方向，不能沿用第一次的临时端点判断。
- 直接证据：从仓 `1511` 向电路板方向的首段 belt build 在本地 30 秒窗口内无输出，因而没有重放。第一次 fresh 扫描只看到实体 `1513…1533`，其中 `1533` 当时没有组内上游而被暂认为起点；稍后再读时新 belt `1534` 已完成并以输出连接到 `1533`，证明第一次扫描发生在预建筑收尾阶段。随后 sorter `1535` 已接入 `1511 -> 1533` 的侧向虚拟槽；最终在 pending/working drone 均为 0 后，有向遍历确认 22 格稳定单链为 `1534 -> 1533 -> … -> 1532`，sorter `1535` 满电处于 Inserting，仓中铁块也持续被取走，因此现有注入仍合法，但最初的“1533 是首带”判断已被新实体反证。
- 直接证据：硅线 6 格外绕段在 commit 后因本地展示表达式错误丢失 action ID。第一次 fresh 读仍处于 `3 pending / 3 working`，只用于确认库存 `17 -> 11` 和 source 已接出预建筑；没有把它当终态。等待到 `pending=0/working=0` 后，完整有向遍历连续两次均为 191 实体、自由末端 `1975`、无输出，随后所有新建段都只从该末端继续。最终十写审计的完整链为 234 实体且 0 prebuild，证明“两次稳定遍历后才使用末端”在第二个独立样本中成立。
- 限制或反例：本样本没有取得原 build action ID，无法从 `get_action_result` 直接给出终态时刻；`pendingBuildTargets=0` 也必须和实体/连接稳定性联合使用，不能单独证明任意复杂施工完成。侧向注入中段在这条单向基础带上成立，不表示所有中段、分流器或垂直带都可安全接入。
- 复验触发：下一次本地输出窗口先结束的长施工、未知 action ID 的 fresh 核销、预建筑计数公开增强、侧向注入非首带、分流/垂直带或 DSP 版本变化。
- 关联：EXP-007、EXP-018、EXP-021、EXP-028、EXP-070、EXP-073、EXP-123。
- 最近复验：2026-09-03（第二个未知 action-ID 样本先读到 3 个 pending，等待施工机清零后再做两次一致的 191 实体遍历；后续唯一续线和最终 234 实体链证明未把施工中的临时端点当成稳定末端）。

### EXP-133 — 既有带占位无法绕开时，可用独立带段和受电分拣器做显式拓扑桥

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：DSP `0.10.34.28529`、两条同坐标但拓扑独立的基础传送带、普通分拣器能够分别绑定旧线末带与新建自由带，以及目标侧需要保持物料纯度的恢复施工。
- 当前结论：发现新带已与既有带重叠后，不得继续沿重叠拓扑施工，也不能把坐标相同误当成已并线。若当前写入口不支持安全回收这些带，可先在全厂占位检查通过的空地建一段完全独立的自由带，再用一只满电分拣器把目标物料从旧线末端显式送入新线；此后所有续段仍逐次做 fresh plannedPath 对全厂旧带的重叠排除，目标端再用独立满电分拣器接仓。验收必须同时包含两只桥接分拣器的双端反查/实际携货、源头持续生产和最终消费者的多窗口增长，不能只看几何绕开或目标仓瞬时库存。
- 直接证据：错误重叠的新铁线末带 `1582` 与旧过滤磁铁线在同一区域保持独立拓扑。动作 `58c0b1e5-c172-4cf4-9150-b059b895efc6` 在最小旧带中心距 1.758 m 的空位建成独立 9 格线 `1585…1592/1584`；动作 `a792c8cd-3ed8-450e-87ae-585a3b9b0c4d` 再建 sorter `1593`，明确连接 `1582 -> 1585`，network 1、serve ratio 1.0，并反复实际携带铁块。四段动作 `53a7acd3-963d-4b56-9b35-3901aa85152d`、`b45d4ee3-4885-4897-ae58-ce0d54c85dcd`、`a35aab94-4953-41a8-991d-deb5935727e5`、`b6d8db8-f6b2-40ff-8abb-9060fe86eb02` 又以每次 fresh 全厂 0.25 m 占位排除分别续建 13/26/18/5 格，末带 `1652` 距旧铁仓 `28` 2.805 m。动作 `5ca88abe-1912-4710-91b4-6e79e37c8e10` 建成满电 sorter `1656`，双端为 `1652 -> 28` 且实际携铁。源端矿机 `1496`、熔炉 `1500`、仓 `1511`、sorter `1535/1593/1656` 连续工作；目标仓因旧链即时抽取可在离散快照中为空，但电路板台 `36` 得到铁、蓝矩阵台 `76` 恢复工作、输出 sorter `78` 实际携带蓝矩阵，科技 `1605` 在多个独立窗口由 `12317 -> 15702 -> 16637 -> 17357 -> 18101/216000`，排除了只消耗一次性管道库存。正常保存动作 `966b3f17-ee67-4e0e-86b0-bdae86228224` 已持久化到 tick `11388372`。
- 直接证据：母星硅线给出第二个独立结构样本。旧铁带 `1521` 保持原连接 `1520 -> 1521 -> 1523`；新硅线先停在相邻末端 `1976`，动作 `c5fed871-cb1d-4051-a9f4-6c691c58111b` 在旧带另一侧建立独立 `1980 -> 1979`，动作 `30ffb884-c60a-4836-85a9-de4eda0de178` 再建 sorter `1981`，双端反查为 `1976 -> 1980`、network `1`、serve ratio `1.0`。其后 29+8 格只从 `1979` 继续，完整有向遍历已跨过 sorter 到自由末端 `2018`，共 234 个实体、无环、0 prebuild；旧铁线未被覆盖或改向。硅口尚未启用，因此该样本只验证跨带结构，不把空载 sorter 冒充实物流；实际输送证据仍由前述铁线样本提供。
- 限制或反例：这里只验证基础带之间的一次普通 sorter 桥；吞吐受两只基础 sorter 限制，不代表适合高吞吐量，也不外推到垂直带、分流器或物流塔端口。目标仓瞬时为空既可能是即时消费，也可能是断流，必须用跨窗口最终消费者斜率区分。当前重叠旧带仍留在世界中，未来若支持普通带的安全拆除，应重新评估清理而非永久保留旁路。
- 复验触发：save/restart 后恢复、任一桥接 sorter 停料、提升吞吐、增加过滤、普通带 dismantle 支持、垂直/分流器桥接或 DSP 版本变化。
- 关联：EXP-024、EXP-028、EXP-037、EXP-070、EXP-073、EXP-120、EXP-123、EXP-124、EXP-126、EXP-127。
- 最近复验：2026-09-03（第二条独立硅线用 `1976 -> sorter 1981 -> 1980` 跨过未改动的旧铁带 `1521`，后续形成 234 实体无环链；sorter 满电且双端正确。当前仍以首个铁线样本的实际携货和最终消费者增长作为流量证据）。

### EXP-134 — 十写审计不能假定较早动作结果仍由运行时保留

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 Plugin 进程中的 action-result 有界存储、持续数千 tick 的建造/等待/保存批次，以及累计 10 个已接受写动作后的延迟审计。
- 当前结论：动作曾经返回 terminal/succeeded 并不保证到第十写审计时仍可由 `get_action_result` 重取；`ACTION_NOT_FOUND` 只表示当前进程不再保留该结果，不能据此把已确认动作改判为失败或重放。每次动作结束时应立即保存脱敏的 action ID、state、message 和关键 readback；批次审计再结合这些即时证据与 fresh 实体/库存/拓扑核销。若既没有即时终态，也无法由 fresh 状态唯一判断 before/expected-after，才进入 outcome-unknown 规程。
- 直接证据：从上一审计到 tick `11401837+` 的 10 写批次中，九个动作在审计时仍可查询为 terminal/completed/succeeded；5 格末段带动作 `b6d8db8-f6b2-40ff-8abb-9060fe86eb02` 则返回 `ACTION_NOT_FOUND`。该动作提交当时已经由客户端取得 completed/succeeded，且随后末带 `1652`、最终 sorter `1656` 的双端连接、71 格旁路连续拓扑、背包带数量和最终消费者增长均反复 fresh 核销，正常保存 tick `11388372` 也覆盖其终态。因此本次拒绝的是“晚查仍必然存在”的假设，不是否定该动作已经完成。
- 限制或反例：当前只观察到一个旧结果被淘汰，尚未由公开配置确定保留数量、时间或淘汰顺序；不能据此预测某个 action 会保留多久。`ACTION_NOT_FOUND` 也不能单独证明“只是淘汰”，必须存在此前保存的终态证据或唯一 fresh 核销；新 session、错误 session ID 和从未登记的 action 也可能给出相同外观。
- 复验触发：下一批 10 写审计、action store 容量/生命周期公开或变化、Plugin 重启、任何 `ACTION_NOT_FOUND`、审计自动化实现。
- 关联：EXP-007、EXP-013、EXP-018、EXP-026、EXP-037、EXP-132、`get_action_result`。
- 最近复验：2026-09-03（母星硅线批次审计时，较早的高纯硅块取货动作 `34c3130a-6c6b-4ea4-913d-ea2c3e2fcd9` 已返回 `ACTION_NOT_FOUND`；该动作提交时已确认守恒取出 400 件，当前玩家仍持有 400 件，源仓及其并发生产/消费由 fresh 快照解释。其余 8 个已知 ID 均仍为 terminal/completed/succeeded，未知 ID 的 9 格长带则由稳定拓扑、库存差量和零预建筑核销；未重放任何动作）。

### EXP-135 — 为范围型业务移动时，可在交互半径内优化短弧的非带实体净空

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：目标仓超出普通 transfer 距离但只需进入交互半径、玩家已稳定 Walk/0 且能量充足、全厂实体位置可结构化读取的母星短途移动。
- 当前结论：不要把仓体中心直接当移动终点。先沿球面目标方向生成“刚好进入交互半径”的前向点，再在局部切向上生成多个有界侧偏候选；对每条候选弧采样并按非 belt/inserter 实体中心最小距离排序，同时把 arrival tolerance 收紧以保留交互余量。只提交一个最佳候选，并继续依赖 180-tick 物理看门狗给出真实失败终态。成功后必须等到 Walk/速度 `<=0.1 m/s`，再以目标实体实际距离和 transfer prepare 复验；几何评分不能替代游戏碰撞、地形或业务范围检查。
- 直接证据：玩家从 `(-67.85983,-33.17086,-185.42108)` 到自动铁仓 `1511` 的中心距约 103.47 m。直向及 ±5/10/15/20/25 m 候选对非带实体的离散最小中心净空仅约 0.24–1.90 m；侧偏 28 m 的候选终点 `(-95.84962,-4.98309,-175.71072)` 保持到仓约 78.98 m、离散最小净空约 2.19 m。唯一 Move `cfa19955-e0fc-4f4b-9c9a-fd0f664d5eb3` 以 0.5 m 容差一次完成，settle 后 Walk/0、距仓 78.96 m；随后 transfer `238d0f15-a9d8-48f5-afa9-e64a02142e00` 正常取得 300 自动铁块。反向 Move `57ee0139-3b95-4bb2-8dc6-aee19c6ae40d` 沿同一已验证通道回到原稳定点并停稳，未触发碰撞、Drift、低能量或重放。
- 直接证据：已验证风机 `713` 的外缘 Walk 点同时位于塑料仓 `558` 和有机晶体输入仓 `761` 的普通 transfer 半径内。玩家沿既有陆地骨架到达后，实际距离分别约 44.45 m 和 73.43 m；动作 `5fcdb8d7-b1f8-42fe-94d0-cc6c15499e47` 在前者取得 50 自动塑料，随后无需返程或额外 Move，动作 `3be91d08-9920-4c79-8378-1141536d6985` 直接把这 50 塑料送入后者。以后范围型任务应先求多个业务半径的已验证陆地交集，减少移动写入，但每个业务仍由自己的 prepare 距离复验。
- 直接证据：同一自动铁仓的第二次范围接近从新的稳定起点 `(-84.008,-94.47305,-155.256317)` 开始，中心距约 99.30 m。直前 24 m 候选终点距制造台 `726` 仅 3.16 m；全厂 1656 实体的球面弧离散评分则把侧偏 `-20°`、24 m 候选排在可进入 78 m 业务范围方案首位，起点 6 m 后的最小非带中心距约 2.33 m。唯一已接受 Move 抵达 `(-105.052,-86.7283249,-146.740692)`、Walk/0、距仓 77.69 m；174 铁块随后正常取出，反向同弧也一次回到原 Walk 点。两次不同起点的正样本支持“排序后只提交一个候选”，但 2.33 m 仍不是通用碰撞阈值。
- 限制或反例：中心距不是碰撞体距离，2.19 m 不是通用安全阈值；本次还排除了 belt/inserter，并未证明它们永远不会阻挡。离散采样可能漏过两点之间的近碰，候选也可能落水；任何看门狗失败都必须按 fresh 停点重新计算，不能重复同一路径。只为进入交互范围时应保留明显余量，不能把 79–80 m 的边缘成功外推到其他实体或接口。
- 复验触发：下一次为 transfer 接近远端仓、候选仍停滞、地形进入 Drift、不同建筑半径、采样密度/碰撞 DTO 改进或 transfer 范围变化。
- 关联：EXP-035、EXP-051、EXP-053、EXP-057、EXP-061、EXP-076、EXP-121、EXP-128。
- 最近复验：2026-09-03（第二个自动铁仓范围接近样本从另一密集区起点复现：全厂非带实体评分选择 24 m 侧偏短弧，唯一 Move 与反向 Move 均稳定 Walk/0，并由 77.69 m 实际距离和 174 铁块守恒 transfer 验收。中心净空和业务半径仍只为候选排序，不能替代碰撞体、陆地、看门狗和业务 prepare）。

### EXP-136 — 科研剩余矩阵数必须按科技的 pointsPerHash 换算

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` 的实验室科技、`get_progression_state` 科技 DTO、研究站 point buffer 与玩家 `mechaResearchItemBuffer`。
- 当前结论：不得用玩家研究缓冲的 `pointCount / wholeItemCount` 比值直接推算某项科技每个矩阵可提供的 hash，也不得把玩家缓冲中的矩阵默认视为当前地面研究站可消费库存。应读取目标科技 `matrixRequirements[].pointsPerHash` 和 `requiredItemCount`：每个普通矩阵含 3600 内部 point，单件对该科技可贡献 `3600 / pointsPerHash` hash；再用 `hashRequired - hashUploaded` 向上取整得到仍需件数，并把研究站、带路和在途缓存作为独立供给状态复读。
- 直接证据：星际物流系统 `1605` 的 runtime DTO 明确为 `hashRequired=216000`，黄矩阵 `pointsPerHash=2`、`requiredItemCount=120`；因此每个黄矩阵只贡献 1800 hash。科技停在 `126000/216000` 时剩余 90000 hash，真实需求是 50 个黄矩阵，不是根据玩家缓冲 `40 items / 144000 points` 误算出的 25 个。首批 25 有机晶体完整转为 25 钛晶石并沿现有带路进入黄糖/研究链，只能覆盖 45000 hash；必须再补第二批 25，不能在第一批后提前宣布解锁预算充足。
- 限制或反例：3600 是当前普通矩阵 item 的内部 point；不同矩阵、增产、研究速度、机甲研究或未来 DSP 版本可能改变实时消耗与上传速度。`pointsPerHash` 给出总预算换算，不代表上传速率；完成仍以科技 `unlocked=true` 和 unlock tick 为准。
- 复验触发：下一项科研选择、不同矩阵类型、增产矩阵、机甲直接研究、研究 DTO 变化或 DSP 版本变化。
- 关联：EXP-042、EXP-063、EXP-100、EXP-103、`get_progression_state`、`get_player_state`。
- 最近复验：2026-09-03（按 `pointsPerHash=2` 补齐的第二批使科技从 126000 最终推进到 `216000/216000`，并在 tick `11808407` 结构化读回 `unlocked=true`、`isQueued=false`；预算换算与完成门槛闭环）。

### EXP-137 — 按数量取仓库物品不保证立刻释放物品格

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` 的小型储物仓、`storage-to-player` 正常 UI 业务路径，以及需要为新物品类型腾出空格的混装仓；不覆盖带过滤格、物流站或其他容器类型。
- 当前结论：给混装仓“腾 N 格”不能只按 `N × stackSize` 取走总数量，也不能只看取走前后聚合总数。原生 storage 取物可能从多个既有格扣减并留下若干非满栈，使总数下降但非空 buffer 数不降；必须 fresh 复读 `buffers` 条目数和目标新物品是否真正入仓。若仍无空格，应再做有界守恒转移，直到条目数明确小于容器格数；不得直接重放上一 transfer 或把手持分拣器货物算成已入仓。
- 直接证据：回收仓 `26` 初始 30/30 个非空 buffer，含 2900 铜块和 200 磁铁；动作 `98057aa3-5070-4612-965f-0cb9122b7ca1` 守恒取走 300 铜并由动作 `49da45e7-688a-4f65-b4cd-b6301c86cec7` 存入静态仓 `136`，但 fresh 仓 `26` 仍有 30 个非空 buffer，电路板输出 sorter `75` 继续手持 1 个成品。第二组动作 `59b370f7-dd48-4d00-91e5-55cf4a8058af` / `6ba6713f-4777-4845-b1b3-62a31793d31c` 再守恒挪走 600 铜后，仓 `26` 降至 26 个条目，首个电路板随即入仓；后续无需 transfer 或重建，自动电路板自然增长到 47。
- 限制或反例：本次只证明当前普通仓的实际取物顺序会留下分散小栈，不证明每次都如此，也不授权为了空格无限搬空生产原料。目标仓容量、玩家背包容量、交互范围、并发 sorter 和双边哈希仍须逐次 fresh 校验；仓 `136` 的 1653 铜只是守恒暂存，后续仍要按用途复核。
- 复验触发：下一次混装仓腾格、堆叠/排序规则变化、存储过滤格、不同仓库规格、自动整理或 DSP 版本变化。
- 关联：EXP-007、EXP-021、EXP-058、EXP-080、EXP-102、`prepare_transfer`、`inspect_factory_entity`。
- 最近复验：2026-09-03（两轮共 900 铜守恒迁移构成反例/正例对照；非空条目 `30 -> 30 -> 26`，自动电路板 `0 -> 1 -> 47`）。

### EXP-138 — 空载物流载具单元可在科技解锁后重配为星际运输船线

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、制造台 `891`、共享输入仓 `892`、输出仓 `893`，以及从物流运输机 recipe `94` 切换到星际物流运输船 recipe `96` 的完整空载窗口；不证明运输船已经装塔或形成跨星订单。
- 当前结论：复用旧物流载具单元前必须先证明输入仓、制造台输入/输出与全部 sorter 携货均为空，并确认新配方已由科技正常解锁；随后一次切换配方，并只在 sorter 零携货时把旧输入过滤改为新配方物料。产品验收仍需完整输入预算经普通物流进入设备、输出专仓精确增长、首次产线 journal durable 和普通保存，不能把配方字段或手工成品当作自动产线。
- 直接证据：科技 `1605` 正常解锁 recipe `96` 后，动作 `370413b3-41ac-4076-bc98-7bd8b838fdd9` 将空载制造台 `891` 从物流运输机切换为星际物流运输船；动作 `701ef730-01b1-4867-8467-579d41f6834a` / `d8b630ab-2bfc-4b05-8bf5-44dba788b246` 把零携货 sorter `895/897` 分别改为钛合金 `1107` 和加力推进器 `1406`，原 sorter `896` 保持处理器 `1303`。20 钛合金先经动作 `f69dded4-f2f3-468e-be52-a38b9bf94bcc` / `170c31a4-08e1-4331-af71-d93b6b9da44b` 守恒投入；随后动作 `59e2e4db-726b-4a38-b142-f9a89c6886c2` / `0c583515-06fa-467a-bb18-da06e337c08d` 和 `1c6c6884-3e0f-4af9-afee-baa9694747b8` / `091cd5c9-b4ed-4722-b201-6766cb34fa74` 又分别投入 20 处理器和 4 加力推进器。最终仓 `892`、制造台三项输入及输出缓存全部归零，满供电输出链使仓 `893` 得到精确 2 艘 item `5002`。journal sequence `47` 在 tick `11890591`（实际 `2026-09-03T08:47:27.6896746+08:00`、本局 `002d 07:02:56`）持久化首次产线运输船，保存动作 `93dfa668-ec95-441f-8ca1-e05f642b1288` 固化 tick `11896243`、revision `563`、healthy。
- 限制或反例：本批三种输入都是已有产线或库存的一次性精确预算，证明最终自动转换而非三条持续上游。两艘成品仍在仓 `893`，尚未经过 fleet 专用动作装入 ILS；运输船容量、工作中数量、跨星订单与往返守恒仍需首条真实航线逐项验收。
- 复验触发：首次把运输船装入 ILS、首次 working vessel、跨星货物订单、配方再次切换、输入 sorter 改线、save/restart 后复读或 DSP 版本变化。
- 关联：EXP-062、EXP-074、EXP-094、EXP-105、EXP-107、`docs/gameplay-timeline.md`、ROADMAP v0.3。
- 最近复验：2026-09-03（空载旧物流载具单元经配方/过滤重配和精确三料预算自动产出 2 艘，sequence `47` durable、普通保存 tick `11896243`）。

### EXP-139 — 星际物流站升级线必须把站体、合金和粒子容器作为独立守恒输入

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、制造台 `898`、共享输入仓 `899`、输出仓 `900`，以及从行星物流站 recipe `93` 切换到星际物流站 recipe `95` 的空载复用；不证明成品已施工或配置跨星航线。
- 当前结论：ILS 不是 PLS 的原地配置升级，而是以一座 PLS、40 钛合金和 20 粒子容器为每批投入的新生产物。复用 PLS 制造单元时，必须在仓外有效原料、制造台输入/输出和相关 sorter 携货全部核销后，fresh 验证 recipe 已解锁，再分别重配 recipe 与需要变化的输入过滤；三类预算必须各自经过普通 transfer 守恒，不能把输出仓中的 PLS/合金共存或旧过滤理解成已经装料。
- 直接证据：运行时 recipe `95` 明确为每座 `1×2103 + 40×1107 + 20×1206 -> 1×2104` 且 `unlocked=true`。动作 `1119b3aa-1154-4df7-b2ea-c2c9f90674d2` 将空载制造台 `898` 从 recipe `93` 切到 `95`；动作 `3ecb5953-fd23-4913-8aa4-e3f6e4837792` / `e390cddf-5ab3-4d6b-98b9-3019ab12f54f` 只在 sorter `902/903` 零携货时把过滤改为 PLS/钛合金，sorter `905` 保持粒子容器。随后六个 transfer 动作把仓 `900` 的 2 座 PLS 与 80 钛合金、仓 `885` 的 40 粒子容器分别经玩家完整送入仓 `899`。首次设备读回已取得 PLS 2、合金 6、粒子容器 6；最终输入仓只余原有 651 硅石，制造台三项输入/输出均为 0，仓 `900` 出现精确 2 座 item `2104`。journal sequence `48` 在 tick `11921722`（实际 `2026-09-03T08:56:28.0954174+08:00`、本局 `002d 07:11:35`）durable；保存动作 `1766766a-c0c0-44d2-b9bc-8d0f87bcda48` 固化 tick `11926992`、revision `576`、healthy。
- 限制或反例：本批是精确两座的一次性预算，证明站体自动转换，不证明 PLS、合金和粒子容器上游持续供应。仓 `900` 中的两座 ILS 仍只是物品；必须通过正常建造、供电、站槽/充电配置、运输船 fleet transfer、跨星订单和两星库存守恒后，才能宣称星际物流完成。
- 复验触发：首次施工 ILS、首次配置远程供需槽、首次装入运输船、跨星货物 working/order、save/restart 后复读，或 recipe/配置 API/DSP 版本变化。
- 关联：EXP-062、EXP-074、EXP-105、EXP-107、EXP-108、EXP-109、EXP-117、EXP-138、ROADMAP v0.3。
- 最近复验：2026-09-03（recipe/两过滤空载重配、三类预算独立守恒、精确双 ILS 输出、sequence `48` durable 和普通保存全部闭环）。

### EXP-140 — ILS 接入既有小电网后应立即降到本机型原生最低充电档

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、母星 planet `104` 的首座 ILS `1657`、既有 network `1` 与电力感应塔桥接；不把当前距离、塔数或功率档外推到其他站址和其他物流塔型号。
- 当前结论：ILS 施工后必须先按实体位置计算与既有电网边缘的球面距离，并用原生建造/供电读回证明实际接网；不能仅凭电塔实体存在或直线示意认定已供电。当前 ILS 接网后原生默认请求 60 MW，远超现有小电网容量，应立即通过同一原生 UI 刻度把最大充电功率降到该 ILS prefab 的最低合法档 30 MW，再配置货槽和机队。此前针对 PLS 得到的 3–6 MW 经验不能套用到 ILS。
- 直接证据：ILS `1657` 位于约 32 m 建筑净空的原生合法站址，与旧电塔 `711` 相距约 42.7 m；本次沿球面弧正常手搓并施工电塔 `1658/1659` 后，站点从 `powerNetworkId=0` 变为 network `1` 并开始充电。60 MW 默认请求时一次快照为 network `1` required/served/capacity `1055980/151000/151000`、consumer ratio 约 `0.1430`；动作 `e1dc0b2d-58f6-4a87-95f3-c3b8bdcd2f27` 只把上限改为原生最低 30 MW，后续审计时站能量已增至约 `828.64 MJ`，network `1` ratio 约 `0.2037`。钛石/硅石两槽随后各以 `100` 上限配置为远程需求，运输船经仓→玩家→fleet 两段守恒后为 `1 idle / 0 working`。
- 限制或反例：当前热电燃料和负载会让 network capacity/ratio 随时间变化，前后快照不能只用 ratio 推导固定发电量；30 MW 只是当前 ILS prefab 的 UI 下限，仍高于此网总容量并会在充能期压低工厂供电。两塔是当前 42.7 m 间隙的已验证方案，不证明所有相同距离都必须两塔。跨星订单和远端供给尚未完成。
- 复验触发：普通保存/重启后读回、远端 ILS 接电、首次 working vessel、站能量达到派船门槛、电网扩容、不同 prefab 或 DSP 版本变化。
- 关联：EXP-018、EXP-021、EXP-095、EXP-097、EXP-099、EXP-101、EXP-105、EXP-109、EXP-138、EXP-139、ROADMAP v0.3。
- 最近复验：2026-09-03（首座 ILS 经两塔接入 network `1`、60→30 MW、双远程需求槽和 1 艘 idle 船均由 fresh 结构化读回确认；严格十写审计通过，随后普通保存到 tick `11974991`、revision `594`）。

### EXP-141 — 星际直线航向必须避开中间天体的原生 1000 m 捕获层

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、同星系原生 Sail、从卫星 planet `104` 前往 planet `102` 时中间经过气态巨行星 `103` 的航线；绕行策略尚待新 DLL 实机飞行升级为 `validated`。
- 当前结论：只避让起飞星球不够。每个 Sail tick 都应在当前星系内排除起点/终点后检查最近的中间星体；若当前位置到目的地的线段进入该星体 `realRadius + max(1000 m, 0.75 × realRadius)` 的中心净空，就先瞄准确定性侧向航点。航点选择必须证明“当前位置→航点”和“航点→目的地”两段都至少保留捕获层的 `1.05×` 净空；绕行阶段把相对速度限制为 200 m/s，直达净空恢复后才重新加速。不得用盲目重复读取检查点碰运气。
- 直接证据：同一飞前检查点的两次 `104 -> 102` 尝试都被 planet `103` 捕获，终态均为 `recovery_required`，且两次都精确读取同一检查点回到 planet `104`，没有覆盖主档。失败时观测到的相对几何为目的方向约 `(49236, 3507, -49947)`、气态巨行星方向约 `(1731, 25, -1954)`，直线最近中心距约 174 m，远小于巨行星 800 m 实体半径。对当前 `Assembly-CSharp.dll` 的 Mono.Cecil 复核又证明 `GameData.GetNearestStarPlanet` 在失去当前行星后以 1000 m 表面距离搜索最近行星，而当前行星要到表面距离大于 400 m 才释放；`PlayerMove_Sail.GameTick` 还会在普通星体周围应用 `realRadius + 40…400 m` 的 SoftLimit。新增纯 Core 几何策略和 4 个测试覆盖失败几何、清晰航线、中心穿越的稳定侧向选择及最近障碍排序；完整 solution 随后构建为 0 warning / 0 error。修复版部署后的首飞发生在巨行星已转到航线后方的无遮挡相位；新 checkpoint 正常创建，飞行期间 `localPlanet=null`，随后直接进入目标 planet `102` 并稳定为 Walk/0，checkpoint 按成功生命周期撤销，证明新控制器未破坏无遮挡飞行，但不单独证明绕行分支。
- 限制或反例：当前实时轨道已经转到直线最近中心距约 2408 m，说明同一对星球的遮挡会随公转变化；这不能推翻两次失败，也不能把某一时刻“当前无遮挡”当作永久安全。新策略尚未实机证明能在移动天体、原生转向惯性及离开卫星重力井的组合下成功；任何失败仍必须读取该次飞行绑定的检查点，不能继续写世界。
- 复验触发：新 DLL 首次实飞、任何 unexpected-planet/local-planet 变化、绕行阶段超时或无进展、星系/飞行控制器/API 阈值变化、DSP 版本或程序集哈希变化。
- 关联：EXP-047、EXP-050、`src/Spherewright.Bridge.Core/Safety/InterplanetaryFlightPathAvoidance.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.InterplanetaryFlight.cs`、`tests/Spherewright.Bridge.Core.Tests/InterplanetaryFlightPathAvoidanceTests.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-03（修复版同档首飞已在无遮挡轨道相位成功到达 planet `102`，Walk/0 且 checkpoint 撤销；中间天体绕行分支仍等待下一次实际遮挡相位复验）。

### EXP-142 — 电网连通与建筑受电是两个独立条件

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` 的风机、电力感应塔、无线输电塔、普通用电建筑和 ILS；球面距离以实际实体位置为准。
- 当前结论：新电塔出现在同一 power network 只证明它接入电网，不证明目标建筑落在其供电覆盖内。施工后必须分别复读“电塔是否并网”和“目标实体 `powerNetworkId/powerServeRatio` 是否改变”。无线输电塔的长连接范围适合合并相邻小网并为伊卡洛斯补能，但目标站体仍须由覆盖半径足够近的节点供电。设计时可先用长距节点搭骨干，再把末端普通电塔放到目标中心约 10 m 处；不能凭示意线或节点数量宣称站体有电。
- 直接证据：planet `102` 的无线塔 `43` 在所有既有实体之外通过原生预演/施工，立即把原 network `1/2` 合并为一个 9 节点、4 风机网络；玩家走到距塔约 2.52 m 后，15 秒核心从约 `245.921 -> 265.450 MJ`，增加 `19.529 MJ`，证明自动充电有效。远端 ILS `44` 建成时为 network `0`；第一座末端电塔 `45` 距站约 15 m，自己加入 network `1`，但站仍为 network `0`、能量 0。第二座电塔 `46` 经原生预演落在站约 10 m 处后，ILS 才变为 network `1` 并自然充能。充电上限随后由 60 MW 降到最低合法 30 MW，站能量在审计时自然达到约 `82.99 MJ`。后续风机 `124…129` 都由原生校验选位、施工机完工，且逐台读回 network `1` 和 `5500 J/tick`；入网后单网发电机 `4 -> 10`、容量 `22000 -> 55000 J/tick`，玩家同时在无线覆盖中最终回充到 `400/400 MJ`。母星硅路的第二座跨线 sorter `2022` 又给出更小尺度的反例：端点双向反查和施工均成功，但完工读回为 `powerNetworkId=0`，硅矿在上游堵塞。电塔 `2031` 落在 sorter 约 6.74 m、既有塔 `847` 约 6.27 m 处后，sorter 立即变为 network `1`、ratio `1.0` 并携带 item `1003`。因此每个远距 sorter 也必须逐一验电，不能用邻近生产设备有电替代。
- 限制或反例：小型风电网原先只有 `22000 J/tick` 容量；ILS 以 30 MW 上限充电时 consumer ratio 仅约 `0.04377`。加到 6 台风机、`33000 J/tick` 后，审计瞬时 ratio 仍只有 `0.07152`，sorter `123` 仍是 Picking/stack 0，钛仓/站内仍为 `3000/0`。加到 10 台风机后 ratio 约 `0.1254`，sorter 才开始往返，ILS 钛石 `0 -> 1 -> 3`，后续增至 49。因此“并网且慢充”不等于“生产设备已跨过原生工作阈值”；必须以源/目标实物流验收，不能写能量值或只看网络 ID。
- 复验触发：更换电塔/发电机/站型、节点位置或 prefab 覆盖参数变化、电网合并/拆分、目标实体仍显示 network `0`、DSP 版本或程序集变化。
- 关联：EXP-021、EXP-105、EXP-140、无线输电塔 `43`、ILS `44`、电塔 `45/46`。
- 最近复验：2026-09-03（母星硅路 sorter `2022` 的 network `0` 堵塞由电塔 `2031` 修复；目标 sorter 随即读回 network `1`/ratio `1.0`、实际携硅，证明“施工成功后逐设备验电”同样适用于短跨线分拣器）。

### EXP-143 — 带路交叉时在重叠点前用受电分拣器显式汇流

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、普通传送带、分拣器、纯料源仓与 ILS 多物品输入槽；不外推到流体、分流器优先级或满槽长期阻塞。
- 当前结论：原生带路预演通过不代表新路径没有和旧带占同一球面格。提交前必须把 `plannedPath` 与当前全厂 belt 坐标交叉检查；若只在一个交叉点重叠，可以在该点前停下独立带，再用经双端反查且已受电的 sorter 向旧带显式汇流。这不是隐式重叠，而是可读的有向拓扑。
- 直接证据：硅仓 `25` 到 ILS `44` 的 54 带直连预演虽返回 allowed，但 planned index `44` 与已有钛带 `102` 仅相距 `0.00393 m`，因此该 token 被丢弃且没有 commit。新方案以 44 格零重叠独立带 `173 -> … -> 148` 在交叉前停下，末端距 `102` 约 `1.232 m`；sorter `174` 完工反查为 `25 -> 173`并实际携硅，sorter `175` 为 `148 -> 102`、network `1`、实际携货。ILS 硅石槽在观察窗内 `2 -> 9 -> 14 -> 19`，同时钛石 `40 -> 42 -> 44 -> 49`，证明混合末段被站内两个已配槽正确接收。
- 限制或反例：两种物料共用末段会共享带宽；本次只验证了钛槽先满、硅仍继续入站以及两种物料各一次取货，尚不能把一个短周期外推成任意供需比例下都不会长期阻塞。若后续提高矿速、扩容槽位或改变带速，仍须复读站口在途货物和两槽增长，不能只看上游仓有库存。
- 复验触发：首次任一槽到达 100、运输船首次 working/返航、两槽完成一次补货、保存恢复、新增站口或更换更高带宽。
- 关联：EXP-070、EXP-117、EXP-123、EXP-124、EXP-126、钛带 `102`、硅带 `148…173`、sorter `174/175`、ILS `44`。
- 最近复验：2026-09-03（钛槽到 100 后硅仍由 71 连续增至 94/99/100；随后两种货物分别生成远程订单并被运输船取走，首次满槽周期没有堵死混合末段）。

### EXP-144 — 跨星取货必须用货槽订单与源库存下降共同验收

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、同星系两座 ILS 各一艘运输船、货槽上限 100、运输船容量 200、远程需求/供应配对；源站位于 planet `102`，目标站位于 planet `104`。
- 当前结论：源站出现负 `remoteOrder` 只证明远程需求已经调度，不能单独证明运输船抵达；源站自己的 `workingVesselCount=0` 也不能否定订单，因为需求站派出的来船不计入源站本地 working。取货至少要看到同一槽先出现负订单，再看到源库存显著下降且订单归零；最终跨星闭环还必须到需求星复读目标库存增长。槽上限小于运输船容量不会天然死锁：当前原生调度会把阈值收紧到槽上限以下，抵达后按实际可取数量装载。
- 直接证据：远端钛槽由 49 增至 100 后出现 `remoteOrder=-200`，同时源站仍为 `1 idle / 0 working`；随后钛槽降到 23、订单归零，再自然补到 45/77/100。硅槽在钛满载期间仍由 71 增至 94/99/100，随后同样出现 `remoteOrder=-200`，取货后降到 16、订单归零，再补到 71。两次下降都远大于单个 sorter 的瞬时携货量，且与各自负订单先后对应，因此可证明来船已两次抵达源站并装货。当前 `StationComponent.DetermineDispatch` 反编译也显示，当货槽 `maximumCount` 不高于按运输船容量计算的发船阈值时，阈值会降为 `maximumCount - 1`；抵达后的 `TakeItem` 只取得当时实际可用数量。
- 限制或反例：母星钛输出已接入冶炼与本地 PLS，硅输出也已通过两座显式跨线 sorter 接入高纯硅熔炉；但首批母星硅槽一次性 `100 -> 0` 只证明输出和消费链成立，不能单独证明下一艘运输船已经完成第二轮补货，也不能替代黄糖终端的持续运行验收。源库存取货后迅速回补，不能用任意两个相隔很久的计数做差；必须保留订单阶段和紧邻下降窗口。调度阈值、订单符号和来船计数均属于当前版本，版本变化后需重新复核程序集和 live 行为。
- 复验触发：返航后首次读取母星 ILS、目标库存增长、运输船归队、第二轮重复订单、货槽或运输船容量变化、曲速阈值/星距变化、DSP 版本或程序集变化。
- 关联：EXP-007、EXP-047、EXP-083、EXP-109、EXP-110、EXP-140、EXP-141、EXP-143、ROADMAP v0.3。
- 最近复验：2026-09-03（母星硅 port `1` 选择 storage slot `1` 后，硅槽 `100 -> 0`；三座关键 sorter 先后实际携带 item `1003`，高纯硅熔炉进入连续工作周期并正常保存到 tick `12841158`。下一次复验聚焦硅槽第二轮跨星补货和重启后连续性）。

### EXP-145 — 密集旧厂区的长带必须做全路径占位排除并分段提交

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、母星 ILS `1657` 到既有高纯硅熔炉 `842` 的普通基础带施工，以及存在长距离回环旧带的密集厂区；不把中心距阈值外推为游戏碰撞体规则。
- 当前结论：原生 `prepare_build` 返回可提交，只证明 DSP 接受这批预建筑，不证明 plannedPath 没有与另一条现有 belt 占用同一球面格。提交前必须把除首个续接点外的完整 plannedPath 与全厂现有带做坐标占位排除，并同时检查非 belt/inserter 设备中心净空；有任一旧带近重合就丢弃 token，不得用“看起来会穿过去”推断拓扑。长线应从 fresh 自由末端和 `endpointStateHash` 分段续建，每段终态后重新遍历有向链。即使用 sorter 跨过眼前一条带，远侧新带的完整路径仍可能再次撞到同一旧链的后续回环，必须重新做全路径检查。
- 直接证据：ILS port `1` 先以 9 格短桩从 `1793` 引出；随后 59 格、42 格和 28 格安全段依次落成，最后一段动作 `33a07c22-f0ac-4573-b1b4-260616bb59ba` 精确消耗 28 条带并实体化为 `1895…1922`。fresh 有向遍历从 `1793` 到自由末端 `1922` 共 138 个实体、无环、0 prebuild。相反，从旧末端 `1873` 直达熔炉的多个原生成功预演各与现有 246 格磁铁带重合 2–4 个点；在带 `1263` 远侧生成独立起点后，直达目标的候选仍与该旧链后续回环重合 2–3 个点。这些 token 均未 commit。选中的 28 格外绕段最近新建路径到非带设备中心约 15.2 m，终态连接为 `1873 -> 1895 -> … -> 1922`。
- 直接证据：上一审计后继续按全路径排除分段施工 23/18/6/6/3 格；其中最后 3 格把旧铁带 `1521` 前的末端停在 `1976`，没有重叠。独立两格 `1980 -> 1979` 与 sorter `1981` 显式跨线后，29 格主推进段及 8 格接近段又全部通过旧带占位排除。当前从 ILS belt `1793` 沿有向连接、跨过 `1981` 到自由末端 `2018` 共 234 个实体、无环、0 prebuild；末端距高纯硅熔炉 `842` 约 11.89 m。所有 9 个已知 action ID 在十写审计时均为 terminal/completed/succeeded；丢失 ID 的 6 格段由两次稳定遍历核销。
- 直接证据：自由末端 `2018` 前方实际连续占用旧带 `1354` 与同坐标串联的 `1322/1344`；普通续带候选全部被全厂占位检查排除。远侧独立三格 `2020 -> 2019 -> 2021` 先通过原生施工，再由 sorter `2022` 显式连接 `2018 -> 2020`；后续 3 格侧移与 4 格直线接近段形成自由末端 `2027`，最终 sorter `2030` 占用熔炉 `842` 的独立输入 slot `1`，原 slot `0` 的旧 sorter `845` 未被覆盖。ILS port `1` 选择硅槽后，硅槽 `100 -> 0`，两座桥和末端 sorter 依次携带 item `1003`，熔炉 `842` 连续工作，仓 `843` 从 2806 增至 2820；普通保存动作 `0999d164-5a6e-4131-a401-ff9e5ef63767` 固化 tick `12841158`。这把“全路径排除→分段→显式跨线→终端实物流”完整闭合。
- 限制或反例：`0.25 m` 旧带坐标排除和设备中心净空只用于当前候选筛选，不是 DSP 碰撞盒或通用安全距离；它不会检测带面高度、未来垂直带、分流器优先级、供电覆盖或玩家可步行性。本次 sorter `2022` 的端点与施工均合法但最初处于 network `0`，再次说明拓扑验证不能替代供电复读；电塔 `2031` 补电后才形成实际流量。
- 复验触发：下一段硅带、首次 sorter 跨带、最终接入熔炉、普通带 dismantle 支持、路径规划器新增碰撞体数据、垂直带/分流器或 DSP 版本变化。
- 关联：EXP-007、EXP-018、EXP-028、EXP-070、EXP-123、EXP-126、EXP-132、EXP-133、EXP-143、`scripts/find-belt-route-candidates.ps1`。
- 最近复验：2026-09-03（有机晶体三料共线施工再次证明直穿方案虽然原生 prepare 成功，却会与旧带重合 4–21 格。改用 5 格自由起点、14 格侧向脱离段、23 格推进段和 10 格再侧绕后，得到 `2037 -> … -> 2085` 共 52 格无环单链；各段在 commit 前都完成全厂旧带占位检查，严格十写审计为 0 prebuild、healthy）。

### EXP-146 — 运行时 Journal、逐档日记和工程事故簿必须分层

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：Spherewright owned save 的首次事件证据、仓库进度文档和跨存档工程经验；不把仓库文档当作 Plugin 自动持久化能力。
- 当前结论：运行时 Journal 是 Plugin 按 owned save 隔离、自动落盘的机器可读首次事件源；逐档日记是一档一份的人类可读时间线，整理 Journal、运行态、决策、事故和保存/Git 边界；工程事故簿跨存档记录第一次遇到的问题、根因、代码/协议修复和验证。事故可以同时保留在当档日记，但三者不得互相冒充。现行操作规则及复验触发仍统一进入本经验账本。
- 直接证据：当前 `owned-world-001` 的 Journal 以 save identity 派生独立文件并在恢复后保持 sequence `49/49` durable；`docs/save-diaries/README.md` 登记该档及后续建档规则，`docs/gameplay-timeline.md` 明确改为该档日记，`docs/incident-fix-log.md` 从历史证据抽取首批 15 个问题/修复。提交 `18f0a69` 同时删除旧阶段状态/手工验收/接手页并修复全部本地 Markdown 链接。
- 限制或反例：Plugin 只自动维护运行时 Journal；仓库内逐档日记和事故簿由开发流程维护。Journal 上线前的旧档事件只能记录可证明上界或未知，不能补造点击实际时间或精确首次产出。
- 复验触发：创建下一个 owned save、Journal 文件/身份派生或 durability 语义变化、存档退役、事故分类与经验账本发生冲突、发布文档重组。
- 关联：EXP-037、EXP-048、`docs/save-diaries/README.md`、`docs/gameplay-timeline.md`、`docs/incident-fix-log.md`、提交 `18f0a69`。
- 最近复验：2026-09-03（运输船引擎首次升级选择把当前档 Journal 推进到 `49/49` durable、无 pending/error；存档索引仅使用公开别名，不含真实保存名或恢复凭据）。

### EXP-147 — 专用输出仓可经受电分拣器汇入带过滤消费者的既有混合入料带

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、有机晶体输出仓 `762`、sorter `2032`、既有钛块输入带 `986 -> … -> 998 -> sorter 1016 -> storage 768`，以及目标仓 `768` 已分别用 item `1117/1106` 过滤两只出料 sorter 的布局；不外推到未过滤消费者、多目标分流或物流塔带口。
- 当前结论：若新产物输出仓紧邻一条已有入料带，且该带的终端仓以独立过滤 sorter 向下游消费不同物品，可以用一只受电 sorter 把新产物显式汇入该带，避免另铺长线。提交前仍须确认新 sorter 双端、目标带有向链、终端仓过滤、无意外外接和供电；验收必须再投入一小批正常上游原料，现场抓到新 sorter 携带目标物，并观察过滤消费者实际取料与下游设备运行，不能只凭空载拓扑宣称成功。
- 直接证据：普通施工把实体 `2032` 建成 `storage 762 -> belt 986`，双端连接反查一致、network `1`、0 prebuild。随后从自动塑料仓 `558`、自动水仓 `753` 和既有精炼油仓 `163` 分别守恒取得 `20/10/10` 并投入仓 `761`；化工厂 `760(recipe 25)` 正常消耗后，两次采样抓到 sorter `2032` 处于 `Sending` 且携带 item `1117`/stack `1`。钛晶石制造台 `767(recipe 26)` 随即由停机转为工作并取得有机晶体，完整 10 件批次最终耗尽；黄糖 lab `774(recipe 27)` 同步运行，金刚石仓 `775` 从 `94 -> 84`，证明 10 轮下游配方完成。全程玩家未持有有机晶体，普通保存动作 `201a6e19-6031-41f8-9d33-72d6a50c942d` 固化 tick `12996056`。
- 限制或反例：早期样本只永久化了有机晶体输出短桥，上游三料仍由玩家搬运；该限制已由 tick `13444822` 前建成的塑料/精炼油/水永久共享路消除。现有证据仍只覆盖目标仓 `768` 带过滤消费者的当前拓扑；瞬时带快照未必抓到移动货物，必须继续用 sorter 携货、下游设备取料、配对金刚石消耗和普通保存组合验收。若目标仓过滤或共享带连接被改动，可能重新串料。
- 复验触发：目标仓 `768` 过滤或带路变化、第二个新物品汇流、出现带阻塞/串料、当前上游三料自动配送拓扑变化、DSP 或程序集版本变化。
- 关联：EXP-007、EXP-018、EXP-028、EXP-086、EXP-088、EXP-118、EXP-129、EXP-133、EXP-142、EXP-143。
- 最近复验：2026-09-03（第二个独立长窗由永久塑料/精炼油/水上游自动供料；sorter `2032` 再次真实携带有机晶体，钛晶石与黄糖设备连续运行，结构矩阵输出 sorter `779/977` 携货，主档保存到 tick `13444822`，因此升级为 `validated`）。

### EXP-148 — 路由候选器的空输出必须区分重叠、材料和计划容量拒绝

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：`scripts/find-belt-route-candidates.ps1`、普通 `prepare_build` 传送带预演、玩家带库存和 128 项短期 plan store；不改变 Plugin 写入协调器的拒绝语义。
- 当前结论：候选脚本输出空集不能直接解读为“几何无路”；它可能是所有原生成功路径都与旧带重叠，也可能是 `SERVER_BUSY`、`NotEnoughItem`、超出建造范围或其他 prepare 拒绝被 `catch` 汇总前吞掉。空集后必须先读玩家带库存、等候已知 plan token 过期，并对一个代表性候选直接 prepare，不得盲目改变几何路线。脚本现在在零可用候选时向 warning 通道汇总旧带重叠数及按错误码分组的 prepare 拒绝数，不混入可管道化的候选对象。
- 直接证据：从末端 `2098/2114/2134` 多轮扫描时，无输出曾先后由未过期的大量 prepare token、玩家仅余 69 条带时的长路 `BUILD_LOCATION_INVALID: ... NotEnoughItem`，以及真正的旧带占位排除造成。等待过期、手搓补带并对代表性直线单独 prepare 后，可精确观测首批重叠发生在 planned index `10/11` 的旧带 `2062/1695`，而不是全局无路。修订后用单个 200 m、材料不足的候选做无副作用复验，脚本稳定输出 `overlap=0; prepare=BUILD_LOCATION_INVALID=1` 警告，同时可管道中没有伪候选对象。
- 限制或反例：warning 只解释本次枚举为何没有结果，不会自动补充材料、等待 token、改写候选或提交动作；若同时存在部分成功候选，当前不额外输出拒绝汇总。
- 复验触发：下一次空候选集、带库存不足、`SERVER_BUSY`、脚本输出对象改版或 plan store 容量/过期时间变化。
- 关联：EXP-007、EXP-124、EXP-125、EXP-132、EXP-145、`scripts/find-belt-route-candidates.ps1`。

### EXP-149 — 同坐标传送带必须以彼此拓扑区分原生转角和游离重复层

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、从既有自由末端急转续带的原生 build 路径、完整实体连接读回；不放宽自由新线或与非源旧带重叠的排除规则。
- 当前结论：两条 belt 的观测中心完全相同不能单独判定为危险重复层。若后一条由原生续建生成，且旧自由末端唯一输出到新 belt、新 belt 又唯一输出到下一格，则它可以是急转所需的有向转接实体；此前新旧线路同坐标却彼此不连接的平行重复仍然是必须拒绝的反例。候选筛选继续排除 planned 新点与任意非源旧带重叠，但续建源点是否产生原生转接层必须在完工后用双向连接复读分类。
- 直接证据：从自由末端 `2202` 向侧方续建 3 格后，DSP 生成 `2205/2207/2206`，其中 `2205` 与 `2202` 的位置都精确为 `(-44.24442,-109.388145,-161.730347)`。fresh 拓扑不是两条游离平行带，而是唯一链 `2201 -> 2202 -> 2205 -> 2207 -> 2206`；随后远侧带 `2212 -> … -> 2208` 经满电 sorter `2213` 接入，审计同时抓到 `2203/2213` 各携带 1 个塑料，证明该转角未阻断实物流。
- 限制或反例：这是一次急转单样本，只证明“同坐标不等于必然游离重复”，不证明任意连接式重叠都合理，也不允许忽略环路、多输出、非相邻旧带重合或垂直层级。若拓扑中不存在 `旧源 -> 新转接` 的直接唯一连接，仍按 EXP-132 的危险重复处理。
- 复验触发：下一次急转生成同坐标实体、带链出现环路/多输出/停流、候选脚本改动源点排除策略，或 DSP 版本变化。
- 关联：EXP-132、EXP-133、EXP-145、EXP-148。

### EXP-150 — 满吞吐 sorter 可能没有可配置空窗，过滤失败后必须改走源链纯化

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、已连接且持续搬运的基础 sorter、只允许零携货配置的 `sorter-filter` 写入，以及能由全部入边证明单品的普通仓；不适用于仍存在未过滤混料输入的源仓。
- 当前结论：活跃 sorter 的 `Returning` 名称不保证外部请求能取得零携货配置窗口；高吞吐或目标背压下，它可能在每次 main-thread 复读时都持有 1 件物品。限定时间内无法取得空窗时应无副作用停止重试，不能强写过滤或拆毁现场。替代方案只有在可证明条件下成立：先守恒移出源链全部错误物品，再复读每一条剩余输入只可能带目标物，使下游无过滤出口的来源结构性单品；后续若输入拓扑变化，纯度证明立即失效。
- 直接证据：sorter `906` 在连续油流中 45 秒始终未出现可提交空窗；脚本做了 694 次 fresh 复读/prepare 尝试，没有 commit 或 action。新油出口 `2218` 建成后同样在 20 秒、694 次复读中持续携货，过滤尝试再次以零 commit 结束。没有放宽 cargo-free 校验；随后改为两次 normal transfer 清除 `163/784` 的 `12 + 55` 氢，并证明上游唯一输入 `709` 已过滤精炼油。严格审计中两仓都只有 600 精炼油，`2218` 实际携油，写健康保持 healthy。
- 限制或反例：源链纯化不是过滤器的普遍替代品；任何未过滤的双产物输入、新人工投料或连接变化都会使证明失效。观察次数只说明当前吞吐下没有外部可捕获空窗，不证明 DSP sorter 永远不会空载。
- 复验触发：`906/2218` 自然停流、任一输入连接或 filter 改变、仓再次出现第二物品、实现原子暂停/配置能力，或 DSP 版本变化。
- 关联：EXP-028、EXP-065、EXP-070、EXP-102。
- 最近复验：2026-09-03（源链纯化后仓 `784` 保持只有精炼油并由 `600 -> 372` 持续供料；隔离的 67 氢守恒转入专用仓 `136`，玩家背包归零，目标三料仓精炼油增长到 179）。

### EXP-151 — 多源共享带的单 sorter 后置桥会被上游满流量饿死

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529`、基础传送带和基础 sorter、塑料/精炼油/水依次注入同一条共享带，并连续经过三处 sorter bridge 的有机晶体入料路；不外推到高速/极速 sorter、分流器、堆叠货物或不同带速。
- 当前结论：多种物料在共享带上依次注入时，即使每个源 sorter 都满电并真实携带正确物品，下游单只同级 sorter bridge 仍可能被最上游的满流量物料持续占满，从而让后注入物料长期没有过桥空窗。诊断必须同时看到“源端携货、下游只增长上游物、后注入源库存不降或目标库存不增”；修复时应在每个已确认的跨带吞吐瓶颈增加并行 sorter，使总桥接能力至少匹配聚合注入能力，再以目标多物料增长和最终消费者连续运行验收，不能把物理连通当作公平调度。
- 直接证据：sorter `2247/2248` 完成最后连接后观察 50 秒，仓 `761` 只有塑料由 `35 -> 75`，精炼油和水均保持 0；与此同时上游 `2218/2229` 已为 network `1`、ratio `1.0` 并分别实际携带 item `1114/1000`，排除了缺源和断电。三个连续瓶颈 `2237 -> 2240`、`2243 -> 2245`、`2246 -> 761` 各补两只并行 sorter（`2249…2254`）后，目标仓先达到塑料/油/水 `270/40/25`，随后增长到 `304/86/72`；化工厂 `760`、钛晶石制造台 `767` 和黄糖 lab `774` 连续工作，金刚石输入从 `84 -> 74`，sorter `779/977` 均抓到结构矩阵 item `6003`。普通保存覆盖 tick `13444822`。
- 限制或反例：三只并行 sorter 是本布局的实证修复，不是通用最小数量，也不保证物料公平或下游永不背压；带速、注入顺序、货物间距和各配方消耗都会改变所需容量。增加 sorter 前仍须按设备槽位规则准备/反查并确认供电，不能用无限并行掩盖错误拓扑或错误过滤。
- 复验触发：保存/恢复后的持续运行、任一桥接 sorter 断电、上游产率变化、升级 sorter/传送带、增加第四种物料、目标仓背压或 DSP 版本变化。
- 关联：EXP-028、EXP-068、EXP-070、EXP-118、EXP-126、EXP-133、EXP-142、EXP-143、EXP-147、EXP-150。
- 最近复验：2026-09-03（新增 `2249…2254` 后三料持续增长并穿过有机晶体、钛晶石和结构矩阵完整消费者链，保存后采样仍抓到 sorter `779` 携带 item `6003`）。

### EXP-152 — 正式包必须以单一产品版本源并经实时握手校验

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：Spherewright 的 MSBuild assembly 版本、BepInEx Plugin metadata、Bridge status/handshake、MCP client/server、release manifest、打包测试和当前 Windows 干净安装流程。
- 当前结论：ZIP 名称、manifest 或 MCP initialize 中任一处显示目标版本，都不能单独证明游戏实际加载了同版 Plugin。产品版本必须来自 Contracts 中唯一的编译期常量，Plugin metadata 和 MCP 客户端共同引用；打包时把命令行版本与已构建常量做 fail-closed 比较，包测试再核对 manifest 与 MCP server version。最终实机门还必须在正常关闭后的干净 Plugin 目录上安装该包，并要求 live Bridge 精确报告同一版本、错误 token 被拒绝、安装版 MCP 能调用 live Bridge、受保护 owned save 能恢复。
- 直接证据：首次由干净 commit `e2d7cd1` 生成并安装的 `0.3.0` 包，其 manifest 和 MCP server 为 `0.3.0`/`0.3.0.0`，但 `get_bridge_status` 明确返回 Plugin `0.1.0`；因此没有创建 tag。修复后 119 项测试与完整 solution 构建通过，Mono.Cecil 从实际 Plugin DLL 读到 assembly `0.3.0.0` 和 `BepInPlugin` 版本 `0.3.0`。第二次把旧 Plugin/MCP 目录整体移到可恢复备份后从候选 ZIP 干净安装，live smoke 证明 wrong-token rejected 且 Bridge Plugin `0.3.0`；安装版自包含 MCP 以协议 `2025-06-18` 初始化为 `0.3.0.0`，`spherewright_get_status` 成功返回同一 live Plugin `0.3.0`。
- 限制或反例：live 版本一致只证明装载身份，不替代 manifest 文件哈希、自动测试、游戏版本、存档恢复和功能回归；preview 包可用于验证修复，但 `sourceDirty=true` 不能作为最终 Release 工件。未来预发布后缀必须保留 manifest/product exact 比较，同时只用三段 numeric core 与 CLR assembly version 比较。
- 复验触发：每次版本号变化、package/release 脚本变化、BepInEx metadata 或握手字段变化、安装目录迁移、最终 tag/Release、任何报告版本不一致。
- 关联：EXP-001、EXP-030、IFX-016、`scripts/package-release.ps1`、`scripts/test-release-package.ps1`、`scripts/smoke-test.ps1`。
- 最近复验：2026-09-03（最终 clean commit `a52ff44` 工件完成零差异安装、实时 Plugin/MCP 双版本握手、同档 protected resume，并以匹配 SHA-256 发布为 `v0.3.0`）。

### EXP-153 — 候选包实机通过不能替代最终 clean 工件本体复验

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：同一 Spherewright 版本在 preview/dirty 工作区与最终 clean commit 间的重新构建、发行元数据、Windows 安装和 GitHub Release 上传；不假定不同源码身份的构建二进制相同。
- 当前结论：即使候选包已经通过实机，最终 clean commit 重打的包也必须视为新的待测工件。先比较候选与最终 payload；只要任一运行文件不同，就必须正常保存并关闭游戏、安装最终 ZIP 本体、逐文件核对安装目录，再重跑 protected resume、错误 token、live Plugin 和安装版 MCP。不能把“同版本号”或“源码看起来只改了元数据”当作二进制等价证明。
- 直接证据：preview 与最终 clean 包比较时，4 个 Plugin 文件中 3 个、224 个 MCP 文件中 4 个不同，因此没有沿用 preview 的实机结论。最终包从 commit `a52ff440b47830f2f3a06a5ae97c7ff11bd15833`、`sourceDirty=false` 生成；重新干净安装后 228 个运行文件与 ZIP payload 的 mismatch 为 0。受保护恢复同一 planet `104` 世界并自动保存到 tick `13516415`；live smoke 通过错误 token 拒绝、119 项测试与 Plugin `0.3.0` 断言，安装版 MCP `0.3.0.0` 成功调用该 Bridge。GitHub Release `v0.3.0` 的 ZIP digest 与本地 SHA-256 `705081710b7061c6a00c4c8836a7d2869b13bd8b8fb6f42bfb24b7f0d62783c1` 一致。
- 限制或反例：本次差异包含 commit/sourceDirty 等构建身份变化，不证明每次仅改文档都会改变所有二进制；它证明的是不能在比较前假定相同。若未来提供可复现构建证明，仍须验证 Release 下载资产与已测工件哈希一致。
- 复验触发：每次最终 clean rebuild、源码 commit 或 dirty 状态变化、重新打包、重新上传资产、安装脚本变化或 Release digest 不一致。
- 关联：EXP-001、EXP-030、EXP-104、EXP-152、IFX-016、`scripts/package-release.ps1`、`scripts/test-release-package.ps1`、`scripts/smoke-test.ps1`。
- 最近复验：2026-09-03（最终 `v0.3.0` 工件完成 clean install、同档恢复、live Plugin/MCP 调用与线上 digest 核对）。

### EXP-154 — 诊断速率只按连续游戏 tick 计算且根因分类须等待完整周期

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：v0.4 Overseer 的纯 Core 相邻计数采样、实际产消速率/理论利用率计算，以及单生产单元的首要停机原因分类。速率、理论容量、直接 assembler/lab/miner 故障输入、同星球与单段精确跨星递归、物流时间窗后来已由 EXP-155/157–165 接入 DSP 运行时；时间型物流活动正例已经完成，受控停滞正例仍未完成。
- 当前结论：实际速率的分母只能是连续流逝的游戏 tick（当前固定 60 tick/s），墙钟中多出的离线/暂停时间单独标为 excluded，不能稀释或伪造吞吐。相同受保护存档跨新 session 可保持连续，但 owned identity 改变、tick/累计计数回退、同 tick 计数前进或相邻采样超过明确上限都必须把窗口标成 discontinuous 并归零本段速率。故障分类只有在 ready 窗口至少覆盖一个完整配方周期后才运行；当前首因优先级为本机矿源耗尽、断网/供电比不足、满输出缓冲、上游矿源耗尽、物流未配置/有源库存但订单无进展，最后才是一般缺料。已有非零实际产量时不把瞬时空输入误报为停机。
- 直接证据：新增的无游戏 DLL 测试证明 600 tick/20 件产出恒为 120/min，即使墙钟跨 1 小时也只把额外时间计入 `excludedNonGameSeconds`；同档跨 session 保持 ready，而换档、tick 回退、计数回退、601 tick 采样断层和同 tick 计数前进全部失效。独立分类测试覆盖未满周期不报、供电不足、输出满、矿源耗尽、物流未配置、订单停滞、一般缺料及已有产量不误报。Core/Contracts/MCP 总计 135 项通过（101 + 15 + 19），完整 solution 0 warning / 0 error；源码产品版本同时切到 `0.4.0`。
- 限制或反例：原生实际窗口已由 EXP-155 固定为 600 tick，理论产率由 EXP-157 接入；输出缓冲、矿量、电力、物理相关物流端和递归生产者分别由 EXP-158/159/162/165 接入。EXP-164 只在消费者缺料、需求端正 reservation、源库存、机队与连续 600 tick 无进展同时成立时给出 suspected stall；真实活动、送达和产量恢复已经实机闭合，但故意停滞仍未完成，不能把 suspected 升级为 confirmed。跨星只覆盖一段精确 demand/supply route 及供应塔真实 Input belt，人工填塔、多段塔中继和未支持设备仍不在递归范围。
- 复验触发：接入 DSP production statistics、改变采样频率/持久化格式/窗口长度、引入多星球聚合或上游图、DSP tick rate/配方速度语义变化，以及首次受控实机故障注入。
- 关联：EXP-030、EXP-062、EXP-127、EXP-142、EXP-144、EXP-153、ROADMAP v0.4、`OverseerCounterWindowAnalyzer`、`ProductionFaultClassifier`。
- 最近复验：2026-09-03（真实钛运输完成 2100+ tick 活动不误报、送达后钛块恢复 `12 min⁻¹`；206 项回归和完整构建通过，停滞正例仍待受控实机）。

### EXP-155 — 自动产线实际速率优先复用 DSP 随档持久化的 600-tick 原生环

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 对精确 active owned `GameData` 中已创建工厂的自动物品生产/消耗实际速率；不涵盖理论容量或故障根因。
- 当前结论：`FactoryProductionStat` 与 `GameData.factories` 使用相同 factory index；每 tick 的自动 `productRegister/consumeRegister` 被滚入 `ProductStat.total[0]/total[7]` 的 600 槽 level-0 环，当前 60 tick/s 下正好是 10 游戏秒，实际每分钟为计数乘 6。环的 `count/cursor/total/itemId` 随正常存档导出/导入，暂停和离线没有游戏 tick，玩家手搓/机甲活动只增加独立 lifetime `total[6]/total[13]`。因此自动产线速率直接读原生环比另建 Plugin 累计旁路更强；读取必须交叉验证 factory/planet/stat/product 身份，不自建或加载未访问星球。
- 直接证据：当前程序集反编译确认 `GameData.GetOrCreateFactory`、`ProductionStatistics.CreateFactoryStat/PrepareTick/GameTick`、`FactoryProductionStat.GameTick/AddProductionToTotalArray/AddConsumptionToTotalArray` 和 `ProductStat.Export/Import` 的上述精确语义。新增接口以最多 64 个 item、每页最多 16 个 planet 和 60 秒 session/filter/page-size 绑定游标返回数据；Contracts/Core/MCP `16 + 106 + 20 = 142` 全通过，完整 solution 0 warning / 0 error。开发 Plugin `0.4.0` 经正常保存/关窗和受保护恢复回到同一 planet `104` 世界；planet `104/102/103` 三个 factory 全部通过同一 snapshot 的分页复读，远端两厂在 `factoryDisplayLoaded=false` 时仍可读。更关键的是主档保存 tick `13626113` 前，红糖 level-0 计数为 2；正常关闭并恢复后仅 16 tick 的首次读取仍为 2，远短于重新生产两颗所需周期，直接证明原生窗口随档恢复而非 Plugin 内存重建。源码 MCP `0.4.0.0` 实际完成 initialize、49-tool list 和新工具 live call。
- 限制或反例：`ProductStat.refProductSpeed/refConsumeSpeed` 仍是 UI 按需重算且无新鲜度标记的缓存，不能直接读取；本切片最初返回的 `theoreticalCoverage=unavailable` 已由 EXP-157 的独立当前组件公式取代，而不是放宽为信任该缓存。原生环只能给出物品/星球级实际流量，不能单独确认具体设备的输出堵塞、物流阻塞或上游矿脉耗尽；这些仍需设备容量、订单、源库存和路径证据。当前只在一个三 factory 存档和一个 DSP 版本上实机验证。
- 复验触发：DSP 版本或程序集哈希变化、统计环长度/tick rate/序列化变化、factory/stat 索引模型变化、扩大 item/page 上限、引入理论速率或设备/物流根因，以及最终 v0.4 clean 工件安装。
- 关联：EXP-030、EXP-048、EXP-062、EXP-079、EXP-104、EXP-144、EXP-154、`docs/research/game-api-overseer.md`、`NativeProductionRateCalculator`、`GameStateReader.GetOverseerProductionOnMainThread`。
- 最近复验：2026-09-03（物流时间窗最终部署后生产窗口仍 ready，三 factory 分页共享 tick `14304692`，204 项测试、完整构建及 live MCP 调用通过）。

### EXP-156 — 跨星球摘要必须共享一个有界快照并区分真实发电与防御场导出

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 对 active owned `GameData` 中已创建工厂的电网、物流和全局科研摘要；不涵盖未访问星球或最终故障根因。
- 当前结论：跨星球供电与物流必须在 Unity 主线程一次深复制，后续分页只能读取同一 session/页大小绑定快照；全局科研只捕获一份并随每页保持相同 tick。电力的实际发电量应逐个验证网络 generator identity 后汇总 `generateCurrentTick`，而 `PowerNetwork.energyExport` 是送往 `PlanetATField` 的防御场余电，必须单列。物流站要交叉绑定 station/entity/planet/consumer，并把 PLS、ILS、轨道采集器和 vein collector 互斥分类；总量扫描必须有明确预算，不能以静默截断换取成功响应。
- 直接证据：当前程序集反编译确认 `PowerSystem.GameTick` 的容量、供电、导出和组件实际发电赋值顺序，以及 `StationComponent`、`StationStore`、`GameHistoryData/TechState/TechProto` 的字段和 `hash × pointsPerHash / 3600` 整数公式。实机以 `limit=1` 分三页完整返回 planet `104/102/103`，三页 `snapshotId/capturedAtGameTick` 一致，末页无 cursor；远端 `102/103` 均为 `factoryDisplayLoaded=false` 但无需创建/加载即可读。planet `104` 有 3 网络、33 generator、3 站（2 PLS/1 ILS），planet `102` 有 1 网络、10 generator、1 ILS，planet `103` 为零网络/零站；修复后两颗有电星的 generated 与各自快照实际 served 对齐且 exported 均为 0。母星 Overseer tick `13730405` 为 `90688/90688/191000/90688/0`，相邻本地电力 tick `13730407` 为 `79388/79388/191000/79388/0`，证明字段语义正确，也证明独立调用跨 tick 时不能要求动态需求数值相等。全局科研唯一返回升级 `3401`、同一队列及蓝/红/黄矩阵预算。最终源码又增加队列 ID 对 runtime catalog/tech-state 的双重身份检查；普通保存 tick `13767062` 后正常关窗，7 个 Debug 部署文件与构建输出 mismatch `0`，其中最终 Plugin SHA-256 为 `3766E3A770FFB7BAA24FA870CA569BD90F5BE776802A04F213EB2634B79E9C6E`。受保护恢复只采用该 exact primary 并自动重存 tick `13767093`；最终三页共享 tick `13773036`，队列 `[3401]` 正常通过新门，母星 generated/exported 为 `94688/0`，生产窗口仍为 ready。错绑页大小 cursor 与 17-planet 请求分别以 `STALE_CURSOR/INVALID_REQUEST` 无副作用拒绝；源码 MCP `0.4.0.0` 完成 initialize、50-tool list 和 live call，Contracts/Core/MCP `17 + 112 + 21 = 150` 全通过，完整 solution 0 warning / 0 error。
- 限制或反例：这是聚合快照，不返回完整 station/设备图，也不会仅凭低产量确认缺料、堵塞、断电、订单停滞或矿脉耗尽。网络详情最多返回 64 个但聚合全部预算内网络；超过 factory/network/generator/station/storage/tech 预算会明确失败。独立本地工具与 Overseer 是不同捕获，只有 `capturedAtGameTick` 相同才可逐字段比较。当前只在一个三 factory 存档和一个 DSP 版本上验证，未来防御系统或电网算法变化必须重查字段语义。
- 复验触发：DSP 版本/程序集哈希变化、电网 tick 算法或 station/tech 序列化变化、调整分页/扫描预算、增加故障分类或上游图、最终 v0.4 clean 工件安装。
- 关联：EXP-021、EXP-030、EXP-104、EXP-142、EXP-154、EXP-155、IFX-017、`docs/research/game-api-overseer.md`、`OverseerPowerSummaryCalculator`、`GameStateReader.GetOverseerSummaryOnMainThread`。
- 最近复验：2026-09-03（snapshot 修复版在 tick `14577940–14577989` 连续返回 16 个完整三星球页；8 个真实分页快照保持硬容量、满载时已有 continuation 仍可读，206 项测试通过）。

### EXP-157 — 理论产能必须从当前组件公式重算，耗尽矿机是合法零容量

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 对 exact active owned `GameData` 中 assembler、matrix lab、miner/oil/water、fractionator、gamma receiver 和 orbital collector 的理论物品产出；不涵盖理论消耗或最终故障根因。
- 当前结论：无时间戳的 `ProductStat.refProductSpeed` 不能作为可靠输入，也不应为只读查询调用会改写共享缓存的 `ProductionExtraInfoCalculator`。应在 Unity 主线程有界扫描全部当前生产组件，交叉验证 component/entity/power/network/recipe/source/station/planet 身份，再用纯 Core 逐 float 运算顺序复现当前程序集所有 `AddRefProductSpeed` 分支。只有整厂六域全部通过才报告 `theoreticalCoverage=complete` 和版本化来源；断网设备计 0，缺料/堵料/欠供电设备仍保留设计容量。普通矿机 `veinCount=0` 是耗尽后的合法零容量，即使 `veins` 已为空也不能让整份快照失败。利用率只在原生窗口 ready 且容量大于 0 时为 `actual/theoretical`，10 秒离散窗口可短暂超过 1，不钳制也不冒充稳定超产。
- 直接证据：当前程序集反编译确认装配/矩阵配方、三种 miner、分馏塔、gamma receiver 和 orbital collector 公式，以及分馏 stack 分支会再次比较 `inserterStackOutput` 的当前版本行为。首个部署 Plugin `76DAEAA470554EE3A80A36F23E264672A4C19E720F0C09DFC9FBFA439BBBCB82` 在真实存档安全返回 `BRIDGE_NOT_READY: An active vein miner has an invalid source-node index`；fresh miner 快照随后证明实体 `14/263/796` 均为满电、0 source-node 的耗尽矿机。校验顺序改为先接受 `veinCount==0` 后，完整 solution 再次 0 warning/0 error；普通保存 tick `13831872`、正常关窗、7 个文件零哈希差异部署最终 Plugin `5AC257D5AB8013E7D088A8609D08A9FA7FD83A633D4D2DA2F0F549BA53815DC1`，protected exact-primary 恢复并自动重存 tick `13831903`。同档三厂返回 complete：母星蓝/红/黄矩阵 `20/10/7.5 min⁻¹`；铁矿机 6+8 点、铜 2 点、石 3 点、煤 7 点分别闭合 `420/60/90/210 min⁻¹`，三台耗尽矿机为 0，水/油为 `50/133.15919494628906 min⁻¹`；两座铁炉为 `120 min⁻¹`，塑料/有机晶体/钛晶石为 `20/10/11.25 min⁻¹`；远端未显示 factory 的硅/钛为 `120/60 min⁻¹`。三页共享 snapshot tick `13837732`，错 filter cursor 与 limit 17 分别安全返回 `STALE_CURSOR/INVALID_REQUEST`。源码 MCP `0.4.0.0` 完成 initialize、50-tool list 和同一生产工具 live call；Contracts/Core/MCP `17 + 122 + 21 = 160` 项测试通过。最终审计 tick `13861096+` 为 confirmed peaceful/sandbox disabled/1×、healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、无 blocker/checkpoint且 restart 可用。
- 限制或反例：当前存档为 assembler、lab、普通矿机、油井和水泵提供了正值实机样本，但没有活动分馏塔、gamma receiver 或 orbital collector；后三支只有当前 IL 与纯 Core 自动测试证据，不能写成 live positive-output 验收。理论值是“已连接设备按当前配置的设计容量”，不是供料可持续性、可用功率或实际峰值；单凭低利用率不能区分缺料、堵塞、物流或矿耗尽。当前只绑定一个 DSP 程序集哈希，版本变化必须重查 float 顺序、组件类别和分馏 stack 分支。
- 复验触发：DSP 版本/程序集哈希变化、增加生产组件类型或增产等级、首次出现分馏塔/gamma/轨道采集器实机样本、改变理论扫描预算、扩展直接诊断覆盖或最终 v0.4 clean 工件安装。
- 关联：EXP-030、EXP-085、EXP-112、EXP-154、EXP-155、`docs/research/game-api-overseer.md`、`OverseerTheoreticalProductionCalculator`、`GameStateReader.GetOverseerProductionOnMainThread`。
- 最近复验：2026-09-03（物流时间窗仍复用同一理论公式；最终同批 Plugin 零差异部署、exact-primary 恢复、204 项测试与完整构建通过）。

### EXP-158 — 输出堵塞必须复现当前组件允许下一周期的原生缓冲门

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，assembler、matrix lab 和 miner 的直接输出堵塞诊断。
- 当前结论：`produced[]`/`productCount` 不能只用“非零”或猜测仓格上限判断堵塞；必须复现当前组件在完成周期时是否允许再放入一批的精确门。冶炼容量为 100，制造配方为 `productCountPerCycle × 10`，其他装配配方为 `× 20`，矩阵站为 `10 × ceil(speedOverride/10000)`，采集器阈值为 50。只有原生 600-tick 窗口 ready、覆盖至少一整个当前周期且实际产量为零时，缓冲达到该门才是 confirmed `output_blocked`。
- 直接证据：当前程序集 `AssemblerComponent`、`LabComponent` 和 `MinerComponent` 反编译分别显示上述比较；纯 Core calculator 测试覆盖冶炼/制造/其他、矩阵速度向上取整、周期向上取整和非法/溢出。live 同档中铁矿机 `1213/1496` 各为 `50/50`，冶炼炉 `10` 为 `100/100`，均在零实际产量窗口返回 `output_blocked`；其余缺料设备没有被该分支抢占。
- 限制或反例：多产物设备任一输出满都可能阻止整周期，但 finding 当前只给首个满输出；分馏塔、gamma receiver 和 orbital collector 缓冲门尚未接入，覆盖会显式为 `partial`。程序集变化必须重新反编译，不能沿用这些常数。
- 复验触发：DSP/程序集变化、生产组件缓冲逻辑变化、新增诊断设备类别、首次受控清空/填满输出试验或最终 v0.4 clean 工件安装。
- 关联：EXP-056、EXP-154、EXP-157、`ProductionOutputBufferCapacityCalculator`、`GameStateReader.OverseerDiagnostics.cs`。
- 最近复验：2026-09-03（物流时间窗仍保持输出堵塞优先级；204 项回归与最终同档 live 读取通过）。

### EXP-159 — 物流根因只能沿消费者的真实有向货运拓扑绑定到塔

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：v0.4 设备输入从 assembler/lab input sorter 反向追到当前 owned factories 的行星/星际物流站 output slot，以及 EXP-162/165 的同星球或单段精确跨星生产者候选绑定；不等同于任意全厂依赖发现。
- 当前结论：同星球或跨星球存在同 item 的物流塔不能证明它供应目标设备。必须从消费者 input sorter 的精确 `pickTarget` 出发，只沿 `ReadObjectConn` 的入边穿过 belt、splitter、piler、spraycoater、inserter、storage/tank，最终命中 station slot 精确绑定的 output belt/entity；此后才按 demand 模式寻找同 item 的 supply 端。中转仓是货运图节点，不是追踪终点。每种候选 input item 必须独立遍历，路径上的每个 sorter filter 都只能为空或精确等于该 item。
- 直接证据：首版 live 在 assembler `530` 缺 item `1004` 时只返回一般缺料；逐段读回证明真实链为 `530 <- sorter 532 <- storage 259 <- sorter 1784 <- belts <- station 1657 output belt 1783`。加入 storage/tank/inserter 有向中继后，同一 finding 只在该物理链成立时附加 demand `104:1657`、supply `102:44`、source inventory `28` 和 carrier count `2`。没有绑定到其他仅同 item 的站。
- 限制或反例：当前只从输入 sorter 起步，不覆盖无 sorter 直连或尚未支持的生产类型；storage/tank 的多入边会保留所有真实上游候选，但只按目标 item 与全路径 sorter filter 绑定塔输出或 producer。跨星递归还要求公开主 supply endpoint 的真实 Input belt，人工填塔和多段塔中继不会被猜测补全。EXP-164 已为命中的精确供需候选增加跨 tick 窗口；真实活动路线已闭合，但单次拓扑命中仍不能证明运输停滞，受控停滞仍待实机。
- 复验触发：DSP 连接槽语义变化、新增 cargo transit 类型、station belt selector 变化、多塔同 item 路由、递归上游图、物流时间窗路由键变化或活动/停滞实机试验。
- 关联：EXP-117、EXP-123、EXP-144、EXP-164、IFX-019、`TryFindDirectDiagnosticDemandBindings`。
- 最近复验：2026-09-03（精确 `102:44 -> 104:1657` 钛路线完成派单、取货、送达和最终产量恢复；没有把其他同 item 站拼入路径，受控停滞仍待实机）。

### EXP-160 — 活跃周期和物品级正产量必须阻止瞬时空输入误报

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：v0.4 使用 DSP 原生 600-tick item 产量与 assembler/lab `replicating`、`served[]` 的直接停机诊断。
- 当前结论：assembler/lab 在启动周期时先消费输入，所以 `replicating=true` 期间 `served < requireCounts` 只表示下一批尚未备齐，不表示当前周期已停产。先让供电/矿耗尽等更窄证据生效，再在 active cycle 时停止缺料判断；此外，只要同一 planet/item 的聚合实际产量为正，就不对同类某个瞬时空闲设备输出停机 finding。
- 直接证据：纯 Core 回归明确覆盖 active cycle + 空 `served` 不报 shortage；live item `6002` 实际为 `12 min⁻¹`、理论 `10 min⁻¹` 时 finding 为 0，而零产量的 assembler `530` 和 lab `774` 才分别返回缺 item `1004/1112`。这避免把批次边界和并行设备错当成全线停机。
- 限制或反例：聚合保护会暂时隐藏“部分并行设备停机但其他设备仍产出”，这是当前 direct-stop 视图的保守选择；后续若增加降速/利用率诊断，需要单独定义持续窗口，不能移除本保护后直接复用停机标签。
- 复验触发：引入部分产能降级诊断、改变 item 聚合范围、DSP 输入消费时点变化、增加 per-device production counter 或最终 v0.4 clean 工件安装。
- 关联：EXP-154、EXP-155、EXP-157、`ProductionFaultClassifier`、`ApplyOverseerDirectDiagnostics`。
- 最近复验：2026-09-03（活动/warm-up 物流不回退为一般缺料的 Core 回归通过；黄糖无订单路径仍按同 tick 本地根因分类）。

### EXP-161 — 故障 finding 是 captured tick 的快照，不是可跨 tick 固化的根因

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：v0.4 当前单次生产诊断中的动态 power serve ratio、设备缓冲和物料状态。
- 当前结论：finding 必须与响应的 `capturedAtGameTick` 一起消费。设备供电、输入和工作态可能在相邻调用间自然变化；旧 finding 不能脱离 tick 缓存后继续驱动写操作，修复前要 fresh inspect。单次物流快照同理不能声明“有订单但无进展”。
- 直接证据：同一制造台 `715` 在 network `2` 的 serve ratio 约 `0.94038` 时返回 `insufficient_power`；电网随后自然恢复后，fresh 调用不再保留断电标签，而按当时现场返回缺 item `1109` 的 `material_shortage`。分类器只使用同一主线程捕获中的 component/network/buffer 值。EXP-164 后，物流 progress 只在保护窗口满足精确资格时成为 known；真实钛运输的 carrier 持续变化超过 2100 tick 时 item `1106` 始终 finding-free，送达后又以 fresh `12 min⁻¹` 产量继续保持无 finding。
- 限制或反例：目前只有一个自然供电波动正反样本，尚未以受控断电和恢复重复多轮；因此保持 `observed`。它不意味着历史 finding 无价值，只意味着写入前必须刷新并重新满足同一证据门。
- 复验触发：受控断电/复电试验、活动/停滞物流时间窗、自动修复客户端或最终 v0.4 clean 工件安装。
- 关联：EXP-007、EXP-142、EXP-154、EXP-164、ROADMAP v0.4 受控故障门、`ProductionFaultClassifier`。
- 最近复验：2026-09-03（真实运输活动/送达/产量恢复的相邻 fresh finding 随 tick 正确变化；自然供电波动仍只有一个正反样本，保持 observed）。

### EXP-162 — 递归根因只沿物料兼容的物理上游，图边界必须显式可见

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 从 assembler/lab 缺料点递归进入受支持 assembler/lab/miner 生产者；同星球使用连续 cargo graph，跨星球使用 EXP-165 的精确 demand/supply endpoint 与供应塔 Input-belt 物理边。
- 当前结论：上游生产者不能仅凭“生产同一种物料”关联。每个缺失 item 必须从消费者精确 input sorter 独立反向遍历有向货运图；起始和中间 sorter 的 filter 均须为空或匹配该 item，splitter 输出还必须按 EXP-163 的精确 slot/belt 与双向过滤语义放行，候选 producer 再按 planet/object/output-item 三重身份绑定。递归最多 8 层、访问 64 个 producer，并用 `(planetId, objectId, itemId)` 防环；达到深度/访问上限、遇环或 resolver 身份不一致时必须写入 `upstream_trace_stop_reason`，不能把截断点冒充完整根因。上游没有与调用方目标 item 对应的 per-device 历史速率，因此把实际速率标成 unknown，继续使用同 tick 缓冲、电力、矿源和工作态分类。
- 直接证据：7 项纯 Core tracer 回归覆盖四节点缺料链、更深输出堵塞、环路、resolver 身份不符、未知上游速率以及深度/访问上限；EXP-163 另有 7 项 splitter policy 回归，完整 suite 为 Contracts/Core/MCP `17 + 150 + 21 = 188`，solution 0 warning / 0 error。首个 live 候选在 tick `14028962` 从黄糖 lab `774` 进入 diamond assembler `715`；最终源码相等部署 Plugin/Core hash 为 `46E62CC930CAD0756BBFB06625C9585F04A074B25FEDC15C4D7DCE2A322F4B70` / `CA8B33DD66330211ECD78E535CBD05932933278AF6E640BE67AF7AFD6301E7C5`。普通保存 `14109460`、exact-primary 恢复自动重存 `14109491` 后，Bridge tick `14111293` 与最终构建后 MCP tick `14138110` 都返回 `lab 774 / 6003 -> material 1112 -> assembler 715 / 1112 -> material 1109`，无 trace-stop evidence；源码 MCP `0.4.0.0` 的 50-tool surface 返回同一四节点路径。
- 限制或反例：`directDiagnosticCoverage` 仍只描述请求物品的立即生产者，不声明递归图完整。跨星递归当前只沿一个公开且精确的 demand/supply endpoint 进入供应塔真实 Input belt；人工填塔、无输入带、未证明的多段塔中继，以及 fractionator、gamma receiver 或 orbital collector 都不会被猜测补全。没有 producer 候选时保留当前已确认的局部缺料 finding。多个并行生产者按稳定身份顺序返回首个可诊断分支，不等于已做全图关键路径排序。storage/tank 的内容兼容性仍沿当前 native 拓扑而非额外抽象为递归 coverage，相关设备语义变化时需重查。
- 复验触发：DSP 连接槽/filter 语义变化、新 cargo transit 或生产类型、跨星球生产者边、递归预算/选择策略变化、temporal logistics 接入、受控故障修复或最终 v0.4 clean 工件安装。
- 关联：EXP-154、EXP-159–161、EXP-163–164、`ProductionRootCauseTracer`、`TryFindDirectDiagnosticDemandBindings`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-03（同一跨星钛路径随后真实派单、取货和送达，黄糖同星路径不回归；206 项测试和完整构建通过）。

### EXP-163 — 分流器过滤必须按精确输出口双向约束递归路径

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 反向生产/物流拓扑穿过 `SplitterComponent` 的只读 item reachability 判定。
- 当前结论：不能把 splitter 当成对所有 item 都透明的四通节点。`outFilter=0` 时全部输出均可承载该 item；`outFilter>0` 时 priority `output0` 只允许等于过滤值的 item，`output1..3` 则只允许不等于过滤值的 item。反向遍历必须保留进入 splitter 的 exact `otherSlot`，用 `GetSlotBelt` 对齐下游 belt，并要求它只匹配一个 `output0..3`；component/entity/belt/slot/filter 任一身份异常都应 fail-closed，而合法但不允许该 item 的分支只停止该分支。
- 直接证据：当前程序集 `SplitterComponent.SetPriority` 在选择输出优先级时把相应 belt 交换到 `output0` 并写 `outFilter`；`CargoTraffic.UpdateSplitter`/`UpdateSplitterAsync` 先以 `cargo.item == outFilter` 判定，只让匹配物尝试 `output0`，并仅让不匹配物进入 `output1..3`。`ProductionSplitterFilterPolicyTests` 的 6 个真值表样本和 1 个非法身份样本全部通过；完整 suite 为 `17 + 150 + 21 = 188`。最终 Plugin/Core 源码与部署哈希分别为 `46E62CC930CAD0756BBFB06625C9585F04A074B25FEDC15C4D7DCE2A322F4B70` / `CA8B33DD66330211ECD78E535CBD05932933278AF6E640BE67AF7AFD6301E7C5`；同档恢复后完整生产图读取成功，黄糖四节点根因路径保持不变，三厂分页与 MCP 调用通过。
- 限制或反例：该规则只描述当前程序集 splitter 的物品选择，不证明 storage/tank 当前内容、跨星物流或时间进展；当前自然黄糖四节点结果本身不含可见 splitter 节点，因此行为正反分支主要由当前 IL 与纯 Core 真值表锁定，最终 v0.4 clean 工件仍需回归。
- 复验触发：DSP/程序集哈希变化、`SplitterComponent` 字段/重排规则、`CargoTraffic.UpdateSplitter*`、连接槽编码、图遍历状态键、增加 splitter 路径公开证据或最终 v0.4 clean 工件安装。
- 关联：EXP-030、EXP-159、EXP-162、`ProductionSplitterFilterPolicy`、`TryValidateDiagnosticSplitterOutput`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-03（当前程序集双实现路径、7 项 policy 回归，以及物流时间窗最终源码相等部署后的同档 live/MCP/分页/审计均通过）。

### EXP-164 — 物流停滞只能由按档保护的连续游戏 tick 窗口给出 suspected 结论

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 对 EXP-159 已由物理带路绑定的精确行星/星际物流需求路线；不等同于全局物流监控。
- 当前结论：单个订单快照不能证明停滞。只有消费者输入少于一周期用量、需求端 reservation 为正、候选供应槽总库存大于零、供需站去重后的 idle+work fleet 大于零时，路线才进入时间窗；供给端负 reservation 不是待满足需求。消费者重新供足、需求库存增加、需求订单幅度缩减、精确路线 active-carrier 数变化，或无人机 `direction/maxt/t` 与运输船 `stage/uPos/uSpeed/warpState/direction` 指纹变化都重置基线。连续 600 游戏 tick 没有任何进展才允许输出 suspected `logistics_blocked`，而不是 confirmed；活动或仍在 warm-up 的路线不回退成 generic shortage。换档、路线拓扑变化、tick 回退、同 tick 状态突变或相邻采样超过 3600 tick 均使证明失连续；跨进程只累计 game tick，墙钟离线时间单列排除。一次读取的所有路线只做一次原子批量替换，且只有 durable analysis 才进入公共 DTO。
- 直接证据：当前程序集反编译确认 `StationComponent.workDroneDatas/workDroneOrders` 与 `workShipDatas/workShipOrders` 的派单、飞行和交货更新语义；纯 Core 回归覆盖 600-tick 成熟、carrier 移动、送达/订单缩减、消费者供足重置、跨 session 离线排除、无订单/无源/无 fleet、save/route/tick/gap discontinuity、same-tick 突变和重复成熟读取。最终 suite 为 Contracts/Core/MCP `17 + 166 + 21 = 204`，完整 solution 0 warning / 0 error。最终审查把逐 route 同步写盘改成同次读取一次原子批量替换，落盘失败不公开 temporal analysis；需求端只认正 reservation。普通保存 tick `14290235`、正常关窗和四 DLL 零差异部署后，exact-primary 只恢复 minimum tick `14290235` 的 planet `104` 同档并自动重存 `14290266`；最终 Plugin/Core SHA-256 为 `A66033BFC60DBCAC8B2E798F815E7A22E635AAFCBFDD7F604E5256F191E3CDC5` / `EE9F5519C23A1EC9BC21987D78D29D90A79E561EBEE80F72702A74678B8E492E`。live 文档为 version 1、2942 bytes、3 条哈希 route，DACL 禁止其他 SID allow，不含原始 save identity，恢复后样本全部属于新 session，且消费者输入充足/不足状态均出现；tick `14293735+` 的黄糖读取仍返回三厂和既有四节点根因。
- 补充实机证据：把远端 ILS `102:44` 的钛/硅供应上限从不够一船的 `100/100` 调整为 `200/300` 后，钛槽达到 200 并出现 `remoteOrder=-200`，母星需求端同时出现 `+200` 且 1 艘船工作。运输船移动超过 2100 tick 时 item `1106` 的 `findingCount` 始终为 0，证明 carrier 指纹变化持续重置窗口；随后源钛 `200 -> 79`、订单归零，母星工作船归队，钛块实际产量恢复到 `12 min⁻¹`，远端矿机为 `30 min⁻¹`，仍无 finding。结果在远端 save `14535735`、成功返航动作 `3515d9f4-8a65-404b-b7bd-79f75ed7a7bc` 和母星 save `14575384` 中固化。
- 补充跨会话证据：两条真实 shipment 分别在普通保存 tick `17572610/17579665` 时保持活动，正常退出后由 exact-primary 恢复并自动重存 `17572642/17579696`。第二次恢复后的首个钛 route 样本在 tick `17580795` 同时满足 200 需求订单、消费者缺料、源库存 60、fleet 1/active 1，但 `stagnantSinceGameTick` 与 `lastGameTick` 都从该新 session 当前 tick 起算且 finding 为 0；退出期间的墙钟时间没有被计入。随后真实送达 90、订单清零，钛块继续为 `12 min⁻¹`，最终普通保存 tick `17584412`。
- 限制或反例：真实活动、取货、送达、跨会话离线排除和产量恢复正例已经闭合，但尚未故意制造连续 600 tick 无进展并再修复的 stalled 正例，故整体仍保持 `observed`。EXP-177 证明当前普通游戏接口没有可逆方式冻结已出发 carrier；不得用直接字段写入伪造此证据。候选 supply 是当前物理需求配置下的有界集合；距离、起送阈值、曲速器与电量等更细分原因仍统一落在 suspected logistics block，不在本条中猜测子因。
- 复验触发：首次真实自然停滞与修复、route key 或 carrier fingerprint 变化、持久化损坏/权限错误、DSP station/ship/drone 结构变化、出现保持路线/订单的原生停航开关或最终 v0.4 clean 工件安装。
- 关联：EXP-030、EXP-069、EXP-072、EXP-154、EXP-159、EXP-161–163、IFX-020、`LogisticsProgressWindowAnalyzer`、`OverseerLogisticsProgressStore`、`GameStateReader.OverseerDiagnostics.cs`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-04（两轮活动运输跨普通保存/恢复后都从新 session tick 建基线，离线墙钟未误报；送达与钛块 `12 min⁻¹` 闭环，停滞/修复正例仍保留为未实机覆盖限制）。

### EXP-165 — 跨星生产者只能从精确供应塔的真实输入带继续证明

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：当前 DSP `0.10.34.28529` / `Assembly-CSharp.dll` SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`，v0.4 从已物理命中的 demand/supply station route 继续进入另一座 owned factory 中的 assembler/lab/miner。
- 当前结论：供应塔的 Input port 不是固定仓储选择器。`UpdateNeeds` 按所有未满仓槽构造 `needs[]`，`UpdateInputSlots` 从每条 Input belt 按整站 needs 取物，取成功后才把 `storageIdx` 写成 `needIdx + 1`。因此必须以 supply endpoint 的精确 item 从每条输入带反向追踪，复用 sorter/splitter 物料过滤与 component/entity/belt 身份门；然后等所有 owned factory 捕获完成，再按 planet/object/output-item 全局绑定。公开 supply 节点和后续 producer 必须来自同一 endpoint，不能与其他候选塔拼接。
- 直接证据：ILSpyCmd 对当前程序集读回 `UpdateNeeds` 的 `storage[i].count < storage[i].max ? itemId : 0` 与 `UpdateInputSlots -> TryPickItemAtRear(needs) -> InputItem -> storageIdx = needIdx + 1`。最终 Plugin 经普通保存 `14413801`、正常关窗和四 DLL 零哈希差部署后，受保护 exact-primary 只恢复 planet `104` 同档并自动重存 `14413832`；Plugin/Contracts/Core SHA-256 为 `344614FE3B827BE8397D5D6DC77C3CCB90C8991C01D088E3108B6F473AB11869` / `8E2FB3205B54972180540D6A6C9B08F62B028453DA5E44BBC36CA96E04F56991` / `9507C3AAEE729ACF13693573C2AB53466B8043F53B762B054F2DEEBD59AAF412`。tick `14414535` 的 item `1106` finding 路径为 `assembler 104:530 -> material 1004 -> demand 104:1657 -> supply 102:44 -> resource_extractor 102:1`，最终定位远端矿机钛石输出 `50/50` 堵塞；tick `14417684` 独立请求 item `1004` 在未显示 planet `102` 工厂上再次返回同一矿机、理论 `60 min⁻¹`、实际 `0`。同一最终部署的黄糖同星路径保持；`limit=1` 三页共享 tick `14415270` 并以 `STALE_CURSOR` 拒绝错 item filter。源码 MCP SHA-256 `E86BE095EA8FDF10D7487C65876EEFD534CE3E4684F9EC31C378F3A737A4E70E` 以协议 `2025-06-18`、版本 `0.4.0.0`、50 tools 在 tick `14416829` 返回同一路径。纯 Core 新增显式跨星路径回归，完整 suite 为 Contracts/Core/MCP `17 + 167 + 21 = 205`，solution 0 warning / 0 error。
- 限制或反例：当前只继续一个公开精确 supply endpoint 的物理边；手工填充、无 Input belt 和未独立证明的多段塔中继不会猜测生产者。多个并行生产者仍按稳定身份顺序选取首个可诊断分支，不是全局瓶颈排序。fractionator、gamma receiver 与 orbital collector 仍不参与递归。
- 复验触发：DSP `UpdateNeeds/UpdateInputSlots/InputItem`、站点 port 语义、连接图编码、新生产类型、多段塔中继、多 supply endpoint 路径表达或最终 v0.4 clean 工件安装。
- 关联：EXP-030、EXP-117、EXP-159、EXP-162–164、IFX-019、IFX-021、`TryTraceDiagnosticCargoUpstream`、`OverseerDiagnosticLogisticsIndex`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-03（同一 supply Input belt 在钛/硅容量调整后形成真实钛派单、取货、送达和钛块恢复；snapshot 修复版同档恢复、206 项测试与 tick `14585723+` healthy/Journal `49/49`/0 prebuild 审计通过）。

### EXP-166 — 无 continuation 的完整首屏不应占分页快照容量

- 状态：`validated`
- 日期：2026-09-03
- 适用范围：`SnapshotPageStore<T>` 支撑的资源、工厂对象、制造台和 v0.4 Overseer 多星球分页；当前实现容量只保护仍可由 continuation cursor 引用的不可变快照。
- 当前结论：有界容量的对象是“待续读快照”，不是所有首屏响应。若 `items.Count <= pageSize`，首屏已经完整且 `nextCursor=null`，记录进入 store 只会制造 60 秒不可达占位；应直接返回一次性页。只有 `items.Count > pageSize` 才保存记录并在容量满时 fail closed。这样既避免只读轮询自我耗尽，也不削弱 session/scope/filter/page-size/expiry 绑定和真正分页的硬上限。
- 直接证据：旧实现的 `TryCreate` 在判断页是否完整前先检查 `_snapshots.Count` 并无条件 `_snapshots.Add`；真实三星球 `limit=16` 轮询因此曾把 8 槽 Overseer store 填满。Core 回归现在证明 16 个完整首屏不占单槽 store、一个真正分页快照占槽、第二个分页首屏被拒绝、容量满时完整首屏仍成功。Release 完整 solution 0 warning/0 error，Contracts/Core/MCP 为 `17 + 168 + 21 = 206` 项测试。源码相等部署后，live tick `14577940–14577989` 连续 16 个三星球完整首屏全部 `nextCursor=null`；8 个 `limit=1` 首屏均得到 cursor，第 9 个按设计返回 `SERVER_BUSY`，同一满载时刻的 `limit=16` 完整首屏和第一个已签发 continuation 都成功。
- 限制或反例：一次性页仍生成响应级 snapshot ID/expiry 以维持 DTO 形状，但它没有 continuation 能力，也不会在服务端保留。该结论不允许提高真正分页容量、放宽 cursor 身份或在多页间重新捕获动态状态。
- 复验触发：快照存储容量/生命周期、首屏 DTO、cursor 编码、分页调用方、跨域默认页大小或最终 v0.4 clean 工件变化。
- 关联：EXP-125、EXP-156、IFX-022、`SnapshotPageStore<T>`、`SnapshotPageStoreTests`。
- 最近复验：2026-09-03（206 项测试、Release 四 DLL 源/部署一致、同档 exact-primary 恢复和 16/8/1 live 容量矩阵全部通过）。

### EXP-167 — 混合输入带的满槽物料会造成头部阻塞

- 状态：`observed`
- 日期：2026-09-03
- 适用范围：当前 planet `102` ILS `44` 的单条动态 needs 输入带、钛石/硅石两个远程供应槽、1 艘星际运输船及当前 200 件船容量/100% 起送设置；不直接外推到其他物流等级或独立输入带。
- 当前结论：共享一条 Input belt 的多物料站受带头物料和站槽余量共同约束。任一槽装满后，对应物料停在共享带头会阻断其后的另一物料；临时增大上限只能制造 headroom，不能永久消除问题。长期应让每种物料都有持续需求/足够余量，或拆成独立输入带。不能把本轮恢复归因于“凑满一整船”：EXP-144 已由反编译和实机证明，当槽上限不高于按运输船容量计算的阈值时，原生会把派单阈值收紧到 `maximumCount - 1`。判断恢复必须看订单、船状态、源库存下降、目标送达和最终产量，不能只看矿机 working。
- 直接证据：初始 ILS 槽为钛/硅 `100/100`，硅已满、单 Input port 最近一次动态 `storageIndex` 指向硅，钛矿机 `50/50` output blocked 且两槽均无订单。这个无订单快照只能证明当时没有活动货运，不能证明是 200 件船容量造成。钛上限改为 200、硅改为 200 后，硅先满又把钛卡在 194；硅上限再改为 300 后钛达到 200，出现源端 `-200`/需求端 `+200` 和工作船。取货使钛 `200 -> 79`，送达后母星订单/工作船归零，钛块恢复 `12 min⁻¹`。稍后硅再次达到 `300/300`，共享带会重现同类头部阻塞，证明扩容是受限缓解而非永久拓扑修复。
- 限制或反例：本条只证明共享带头部阻塞及扩容带来的临时余量，不把初始无订单的单帧状态归因于起送阈值，也不证明提高母星硅需求一定是最佳长期方案。派单阈值、订单和取货语义以 EXP-144 为准。拆带前要重新 inspect 空闲 port、完整占位、供电和物料过滤；受控物流停滞试验可暂时利用这一已理解状态，但完成后必须恢复正常运输。
- 复验触发：物流船容量/起送百分比、站槽上限、Input port 数量、共享带顺序、科技升级、DSP 版本、远端站重建或长期修复实施。
- 关联：EXP-049、EXP-065、EXP-117、EXP-144、EXP-164、`StationComponent.UpdateNeeds/UpdateInputSlots`。
- 最近复验：2026-09-03（真实钛订单、取货、送达和母星钛块恢复闭合；硅满槽后的再次头部阻塞仍待拆带或持续需求修复）。

### EXP-168 — 人工读档交接采用预检后的对话确认边界

- 状态：`observed`
- 日期：2026-09-04
- 适用范围：玩家已在 DSP 中手工载入、但尚未由当前 Spherewright 进程认领的和平/非沙盒/1×世界；当前 `prepare_import_current_game` / `commit_import_current_game` 与对应 MCP 工具。
- 当前结论：人工读档后不能直接把最初的“继续/接手”当成导入授权。Agent 先读取只含 opaque session/revision 的受限状态，prepare 只在内存绑定当前进程/session/revision/精确 `GameData` 并返回“原档不变、新建 owned 副本、Journal 从导入点开始”的确认问题；收到用户在下一条消息中的明确同意后，commit 才声明对话确认与两个边界确认。commit 再复核对象身份、和平、非沙盒、1×和本地工厂 ready，生成内部高熵名称，通过 DSP 正常保存与精确 header tick 复读后才认领。无需快捷键或验证码；若 save 已返回 true 但 header 证明失败，同一未认领 session 进入隔离，只保留结果查询，必须由玩家主动重载原档形成新 session 后再开始。
- 直接证据：当前程序集 SHA-256 `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85` 的 `GameMain.gameName` setter、`GameSave.SaveCurrentGame`、`GameData.Export` 与 `GameSave.ReadHeader` 调用链已复核；Contracts DTO 不含原档名/路径/验证码，Core 策略覆盖三项声明缺一即拒绝，MCP 注册与参数映射覆盖 prepare/commit。main 完整 solution 0 warning / 0 error、`18 + 174 + 22 = 214` 项测试通过；经项目所有者明确批准的最小回移 tag `v0.3.1` 位于 commit `33a733f`（直接父为 v0.3.0 `a52ff44`），prerelease 自包含包 `sourceDirty=false`、232 manifest entries、MCP `0.3.1.0` / 50 tools、127 项回移测试通过，ZIP SHA-256 `b05eabb20928e98850f6792ea001149fd2e30c92082994e6d9c43254e611cdcf`。
- 限制或反例：Plugin 无法读取聊天历史，`userConfirmedInConversation` 是 MCP 调用方对当前对话证据的声明，真正的“先问再确认”由工具规范与 Agent 行为保证，不应描述为密码学证明。保存失败或在新文件创建后中断可能留下不可达的内部 orphan 副本；此时保持 unowned、不得猜测认领或删除文件。当前 main 与 v0.3.1 只有程序集/代码/自动测试证据，仍需另一台电脑上的当前 DSP 实机证明原档 header 不变、新副本可保存恢复、重新进入原档仍受限，才能升级为 `validated` 或把 prerelease 视为实机验收通过。
- 复验触发：MCP 工具说明或确认字段、plan/session/revision 绑定、GameData 身份、保存/header API、Journal attached-save 初始化、DSP 版本或 v0.3.1 回移候选变化。
- 关联：`UserSaveImportCoordinator`、`GameSessionTracker.TryImportCurrentSessionAsOwnedCopyOnMainThread`、`UserSaveImportConfirmationPolicy`、`docs/research/game-api-m0.md`、ROADMAP `0.3.1`。
- 最近复验：2026-09-04（v0.3.1 最小回移已由用户明确批准并发布为 prerelease；工件/manifest/MCP/127 测试通过，跨电脑实机仍待用户验证）。

### EXP-169 — 跨域诊断只能在同一主线程 tick 经身份全匹配后合并

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：v0.4 `get_overseer_diagnostic_bundle` 对已有生产/根因与供电/物流/科研 DTO 的聚合；不改变底层设备或物流诊断覆盖率。
- 当前结论：外部调用方分别读取 production 和 summary 后自行拼接，可能把相邻 tick 的电力、库存、订单和 finding 当成同一现场。可信诊断包必须在一个无异步让出的 Unity 主线程任务内捕获两域，并逐 factory 要求 index、planet ID/name、local/display flags 与 `capturedAtGameTick` 全相等；任一不符都整包 fail closed。对外只复制版本化白名单 DTO，不能为了“便于调试”附带内部 save key、真实 save identity、runtime path、auth 或 plan credential。
- 直接证据：`OverseerDiagnosticBundleComposerTests` 覆盖正常同 tick 合并，以及 factory、planet、tick、name、display flag 错配和公共域集合缺失时的拒绝；Contracts JSON 回归固定 `schemaVersion=1` / `privacyProfile=public_allowlist_v1` 并检查敏感字段缺失；MCP 注册/映射覆盖新工具。Release 完整 solution 为 0 warning / 0 error，Contracts/Core/MCP `19 + 181 + 23 = 223` 项通过，公共工具面为 53。源码相等部署后，live 完整页在 tick `17051000` 返回三厂；三页 continuation 共享 snapshot/tick，错 filter 与错 page size 均返回 `STALE_CURSOR`。12,156-byte 原始 JSON 的敏感字段名、Windows/UNC 绝对路径和内部存档标记审计均为 0；源码 MCP `0.4.0.0` / 53 tools 成功调用 live endpoint。
- 限制或反例：bundle 继承现有 fractionator/gamma/orbital direct coverage partial、单段物流边和每域扫描上限，不因聚合而变完整；本次只验证当前同档三座 factory，最终 clean `v0.4.0` 工件仍须独立重验。
- 复验触发：任一 Overseer DTO/捕获顺序、Unity 主线程 dispatcher、snapshot store、cursor binding、隐私字段、生产/摘要预算、DSP 版本或最终 v0.4 clean 工件变化。
- 关联：EXP-125、EXP-154–166、`OverseerDiagnosticBundleComposer`、`GameStateReader.GetOverseerDiagnosticBundleOnMainThread`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-04（同一 owned 三工厂世界完成 source-equal 部署、分页/cursor/JSON/MCP live 复验与健康审计；223 项自动测试和完整 Release 构建通过）。

### EXP-170 — 受控物流故障应切换正常路由配置并以真实送达恢复

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：v0.4 在同一 owned world 制造/修复可逆物流故障；当前实机对象为 planet `104` 的钛块 PLS `918 -> 916` 与钛晶石制造台 `767`。
- 当前结论：验证“物流阻塞”不需要删除建筑、注入物品或冻结游戏。先确保源物料真实到达，再在订单归零窗口用 DSP 正常 `SetStationStorage` 路径把精确供应槽从 Supply 改为 None；需求站、物料、机队和物理带路均保留，使诊断可以把“路由未配置”与普通缺料分开。恢复时只把同一槽改回 Supply，随后必须看到原生订单/无人机送达、finding 清零和下游实际产量恢复，最后普通保存。配置故障正例不能替代 600-tick 在途 carrier 停滞正例。
- 直接证据：母星硅需求上限 `100 -> 300` 后出现 `+200` 原生订单，硅 `133 -> 333`，远端共享入口释放并送回 189 钛石；本地钛块源塔开始积累。动作 `8cc1d727-e8e8-4643-a2a3-d6c45b40de59` 把 `918:slot0` 的本地逻辑 `Supply -> None`，bundle tick `17130476` 在完整 600-tick 窗口把 `767/item 1118` 分类为 `logistics_blocked / confirmed`，证据含 `logistics_configured=false` 且路径到需求站 `916`。动作 `63745c00-788c-47c5-9bf4-a307fdf345ee` 恢复 Supply；无人机订单/送达后 finding 为 0、钛晶石实际速率达到 `12 min⁻¹`。fresh 状态证明普通保存 tick `17136808`、revision 8、healthy、无 blocker/checkpoint 且 restart ticket 可用。
- 限制或反例：本条证明的是“路由配置被撤销”这一 confirmed 物流故障，不证明有正 reservation 的 carrier 在 600 tick 内完全不动；后者仍须保持 source inventory、需求缺料、订单和非零机队同时成立，不能用本条替代。共享远端硅/钛单输入带仍可能在本地需求填满后再次头部阻塞。
- 复验触发：物流站 storage/route UI 语义、`ProductionFaultClassifier` 的 route-not-configured 优先级、物理路径解析、DSP 版本、受控故障验收定义或最终 v0.4 clean 工件变化。
- 关联：EXP-098、EXP-144、EXP-159、EXP-164、EXP-167、`ProductionFaultClassifier`、`docs/research/game-api-overseer.md`。
- 最近复验：2026-09-04（同一 save 正常制造、诊断、恢复、实际产量与普通保存闭环）。

### EXP-171 — 受控缺料应阻断一条空载输入配置并按真实补料恢复

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：v0.4 同一 owned world 的可逆 `material_shortage` 制造/修复；当前实机对象为 planet `104` 原油精炼厂 `141`、输入 sorter `162` 与原油 item `1007`。
- 当前结论：选择已有连续产量、输入带物料单一且 sorter 正处于空载窗口的设备，通过 DSP 正常 sorter UI 字段把输入过滤器临时设成该带不存在的已解锁物料，可以在不搬运/注入物品、不改缓冲且不拆建筑的条件下制造真实缺料。验收必须等设备吃完原输入，看到精确缺料 item/available/required 和 600-tick 实际产量归零；恢复必须清回原过滤器、看到真实货物被 sorter 搬入、finding 清除和后续完整窗口恢复非零产量。
- 直接证据：动作 `a470c63b-9422-4554-968b-4179b9ca0ab6` 在 sorter `162` 空载时把无过滤改成铁矿 `1001`；源带只运原油。bundle tick `17177994` 将 refinery `141` 分类为 `material_shortage / confirmed`，证据为原油 `input_item_id=1007`、`input_available=0`、`input_required_per_cycle=2`，精炼油 600-tick 实际产量为 0。动作 `84f54718-8c66-4413-a1ab-e903fef1ba46` 清回 filter 0；随后原油输入恢复为 4、finding 清零，独立完整窗口达到精炼油生产/消耗各 `12 min⁻¹`。
- 限制或反例：若源带混料，把 sorter 过滤成另一种实际存在的物料可能串料，不能使用；若设备原本输出堵塞或另一原料已经短缺，也不能把结果归因给目标输入。当前短窗口后来可能因批次边界显示 0，但已取得一次完整恢复窗口，验收不依赖单帧 `isWorking`。
- 复验触发：sorter UI 赋值字段、cargo-free guard、原料带拓扑、生产诊断优先级、DSP 版本或受控故障验收定义变化。
- 关联：EXP-021、EXP-104、EXP-154、EXP-155、EXP-170、`SorterFilterPolicy`、`ProductionFaultClassifier`。
- 最近复验：2026-09-04（同档正常配置、缺料、清除配置、真实补料与完整速率窗口闭环）。

### EXP-172 — 物流塔充电上限不是负载，供电试验必须先证明真实能量缺口

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：当前 DSP 版本的物流塔最大充电功率配置与 v0.4 `insufficient_power` 受控试验设计。
- 当前结论：`maximumChargePowerWatts` 只是上限；满电物流塔仍只请求 60 kW idle floor。单独提高上限不会拉低电网，必须先以原生运输制造可观察的站内能量缺口，并同时证明 station requested charge 上升和同网生产者尚有完整输入/输出空间，才能把低功率 finding 归因于受控供电。扩大需求但没有可用远端供应也不会发船或耗能；即使本地快照显示 working vessel，也不能推断能量一定由本地塔支付，必须连续读回本塔 energy/request。
- 直接证据：动作 `a95b2580-5f08-4bbc-933d-24263b6a27d6` 把满电 ILS `1657` 的充电上限从 30 MW 调到 150 MW，能量仍为 `12,000,000,000/12,000,000,000`，requested charge 仍为 60 kW，三网供电比均为 1。动作 `e262a00a-4dba-44a9-82b0-3c17d53cdde0` 又把钛需求上限 `100 -> 300`，但远端没有满足起送条件，订单保持 0、运输船 `1 idle / 0 working`、能量不变。后来硅需求 `300 -> 500` 延迟建立 `+200` 原生订单，本地快照经历 `0 idle / 1 working`，并真实送达 `333 -> 533`；低频连续采样只得到本塔 minimum energy 12 GJ、maximum requested charge 60 kW、network ratio minimum 1.0。后续同端点高频样本在原生派船瞬间读到本塔约 59.7 MJ 的扣能和 8.30 MW 请求，证明前一轮是采样漏过短窗口，不是“运输不扣能”。再以“150 MW 上限 + 火电自然停料至 output 0 + 533/700 硅需求”组合时，远端无现货而不派单，仍没有低压。
- 限制或反例：运输扣能窗口可短于普通轮询周期；有订单、working vessel 或完整往返都不能替代同一次启程附近的 station energy/request 读回。另一端也可能有舰队，因此仍需建立单端归属条件。不得直接写 station energy、consumer request 或电网字段来制造结果。
- 复验触发：物流塔 UI 充电语义、运输耗能、派送阈值、供电汇总/诊断公式、DSP 版本或新建安全供电原语变化。
- 关联：EXP-069、EXP-098、EXP-144、EXP-156、EXP-170、`PowerNetworkSnapshot`、`ProductionFaultClassifier`。
- 最近复验：2026-09-04（无现货、满电塔与低频漏窗均作为反例；后续高频原生派船捕获 energy/request 缺口并完成同 tick 低压/恢复闭环）。

### EXP-173 — 飞行预算通过不代表核心全程有余量，终态仍以原生航迹和稳定落地为准

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：当前 DSP 与 flight controller、planet `104 -> 102` 约 61 km 同星系航段、400 MJ 核心和煤燃料样本。
- 当前结论：飞行 prepare 的 600 MJ 保守总储备门能阻止明显不足，但燃料发电功率与推进瞬时消耗是另一维度；低功率煤可让核心在加速期降至 0，同时机甲仍靠反应堆继续加速和航行。不得看到核心 0 就自行中止已经持续接近目标的原生 Sail，也不得把本样本外推为所有航线安全。成功仍要求距离持续下降、速度/状态合理、目标星落地后连续 600 tick 保持 alive、grounded、Walk/0，并撤销 checkpoint capability。
- 直接证据：18 煤时 prepare 只算出约 449 MJ 并以 `ACTION_REJECTED` 无副作用拒绝。节点 `402` 随后按正常采集共减少 80 煤，原生 refuel 后 fresh 燃料仓为 91；飞前普通保存 tick `17296462`。动作 `2209a388-9f77-41f7-bd31-d32f7d9e6066` 创建独立 checkpoint 后从 Fly 进入 Sail，距离约 `63.5 km -> 59.0 -> 51.8 -> 41.7 -> 28.5 -> 13.5 km`，速度约 `228 -> 387 -> 578 -> 774 -> 980 -> 1000 m/s`；核心中段为 0 后仍恢复至约 7.1 MJ。最终在 planet `102` grounded/Walk 连续 600 tick，于 `17304833` completed，checkpoint 消失；落地普通保存 tick `17305571`。
- 限制或反例：该航段消耗后玩家只剩 35 煤且核心约 76.7 MJ，返航前必须在远端满充或补足高功率燃料并重新通过 prepare；煤样本不替代氢燃料的更高功率经验。任何距离不降、状态错误或结构化 `recovery_required` 仍须回载本次 flight checkpoint，而不是硬撑。
- 复验触发：flight energy budget、燃料发电功率、推进/加速公式、600-tick 落地门、DSP 版本或返航样本变化。
- 关联：EXP-047、EXP-051–053、EXP-080、EXP-083/084、`PrepareInterplanetaryFlightRequest`。
- 最近复验：2026-09-04（同档 104→102 煤动力航行、稳定落地、checkpoint retirement 与普通保存闭环）。

### EXP-174 — 要归因需求端运输耗能，应先让供给端有货但无可用运输船

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：v0.4 受控 `insufficient_power` 试验与同星系 ILS 双端派船归因；当前端点为 source `102:44`、demand `104:1657`。
- 当前结论：仅从需求塔看到 working vessel 不能证明是需求端支付航行能量。可验证的单端派送前提应同时满足：供给塔已有目标库存且保持 Remote Supply，供给塔 idle/working vessel 都为 0，需求塔仍有自己的 idle vessel，且开放需求前双方 order 为 0。供给端运输船只能通过正常 fleet transfer 守恒取入玩家，不能改计数或删船；保存后再返航开需求，才能把需求端 energy/request/电网变化与该批运输绑定。
- 直接证据：远端 ILS `44` 在 tick `17309480+` 有钛 `200/200`、硅 `109/300`、两槽 Remote Supply、0 order、`1 idle / 0 working` vessel。八段约 27 m 的球面短弧全部正常完成并停在距塔约 45 m；动作 `d0ac4683-8c05-4d07-83a7-f29b3a0e3e02` 使玩家运输船 `0 -> 1`、站内 idle `1 -> 0`，working 保持 0、站能量保持 12 GJ、configuration hash 不变。普通保存 `9a77399b-84d4-4821-95b8-ef3dfd9073ac` 固化 tick `17334563`。十写审计确认玩家持 1 船、塔为 `0/0` fleet、供给库存/逻辑/订单不变。返航后，母站 `1657` 有自己的 `1 idle / 0 working` vessel，供给站仍为 0/0 fleet。低频读数在已有 working ship 时仍看见 12 GJ/60 kW；但后续原生硅派船瞬间于 tick `17472191` 捕获母站约 59.7 MJ 扣能和 8.30 MW 请求。硅送达 `733 -> 933` 后，母站同一艘船立即切入下一笔 200 钛订单；tick `17505966` 再次捕获母站 `11,873,487,813/12,000,000,000` 和 9.14 MW 请求。供给塔保持 0/0 fleet，因此两次扣能均可归属母站船启程。
- 限制或反例：本条证明的是舰队归属与需求端扣能，不代表所有航线或供需模式都由需求端付能。普通秒级/十秒级采样会漏过启程后的短充电窗口；必须以高频只读监控捕获 energy/request，并与同 tick 订单、fleet 和供给端无船证据绑定。
- 复验触发：station dispatch 归属、fleet transfer UI 路径、route order 语义、远端库存、返航/母站发船结果或 DSP 版本变化。
- 关联：EXP-098、EXP-105、EXP-140、EXP-144、EXP-172/173、`LogisticsStationFleetTransferPolicy`。
- 最近复验：2026-09-04（两次原生母站派船瞬态、供给端 0/0 fleet、energy/request 扣能归因与真实送达闭环）。

### EXP-175 — 受控供电故障必须同 tick 证明负载、网络和设备分类，并按原配置恢复

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：v0.4 `insufficient_power` 受控制造/恢复、planet `104` network `1`、母站 ILS `1657`、火电 `183` 与燃料 sorter `678`；数值不外推到其他电网。
- 当前结论：可逆断电正例应只用正常配置和原生负载：在 sorter 空载窗口暂时过滤火电燃料，等机组自然停机；把满电 ILS 最大充电上限提高但不直接改能量；再用已归属的原生运输制造塔能量缺口。验收必须在同一短窗口读取塔能量/request、网络 required/served/capacity/ratio，并让诊断包在相邻同 tick 对真实可诊断生产者返回 `insufficient_power / confirmed`。恢复必须清回原 sorter 过滤、恢复正常充电上限，并证明网络 ratio 1、塔回满且 power finding 清零。
- 直接证据：火电 `183` 在 sorter `678` 临时过滤为铁矿后自然降到 output 0，ILS 上限保持 150 MW。硅第一次派船 tick `17472191` 已显示 `12 GJ -> 11.940307058 GJ`、请求约 8.30 MW；硅最终送达 `733 -> 933`。紧接着下一次钛派船在 tick `17505966` 将 ILS 降至 `11.873487813 GJ`、请求升至 `9.13776 MW`。tick `17505968` 的 network `1` 为 required `197786`、served/capacity `115000/115000`、ratio `0.5814365`；tick `17505969` 的同 tick bundle minimum ratio `0.5814953`，并将对象 `841/842/141/707/767/774` 对应的硅石、高纯硅、精炼油、钛晶石和结构矩阵生产分类为六条 `insufficient_power / confirmed`，每条都带 network id `1` 与同一 ratio。动作 `0f7c84b7-847b-4502-81b2-5384db1e4bfa` 清回 sorter filter 0，动作 `427099af-62d2-41fb-a1ad-75d6314d55cd` 把 ILS 上限恢复 30 MW；tick `17512163` 塔已回满 12 GJ/60 kW、network ratio 1，tick `17512817` bundle minimum ratio 1、underpowered station 0、power finding 0。
- 限制或反例：这个试验故意让整张 network `1` 短时欠供，不能在用户未授权的档或不可恢复生产现场复用。仅看到低 ratio 不足以证明分类；仅看到 finding 也不足以证明负载归因。恢复后的其他 `material_shortage`/`output_blocked` 属于独立现场状态，不是断电恢复失败。
- 复验触发：station trip-energy/charge request、power network 汇总、classifier 阈值/优先级、sorter cargo-free guard、DSP 版本或受控故障门变化。
- 关联：EXP-021、EXP-069、EXP-098、EXP-144、EXP-156、EXP-171/172/174、`ProductionFaultClassifier`、`PowerNetworkSnapshot`。
- 最近复验：2026-09-04（真实派船扣能、同 tick 六设备低压分类、正常配置恢复与 finding 清零闭环）。

### EXP-176 — 活跃运输跨保存/恢复时必须从新会话游戏 tick 重建连续窗

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：当前 DSP `0.10.34.28529`、同一 owned save 的 v0.4 星际物流进展窗口，以及运输船在途时的普通保存、正常退出和 exact-primary 恢复；不外推到换档、回档或路线拓扑变化。
- 当前结论：运输船在途时可以正常保存并恢复，但跨进程后的首个持久化样本必须把停滞基线重置到新 session 的当前 game tick；退出期间的墙钟时间不得折算成 600 个游戏 tick，也不得让一条仍在正常移动的路线立即产生 suspected stall。恢复后 carrier 指纹、订单和库存继续按原生进展更新，送达后订单清零和下游非零产量共同核销结果。
- 直接证据：把母站硅需求上限正常扩到 1600 后触发真实 200 硅订单；运输中普通保存已由 fresh `lastOwnedSaveGameTick=17572610` 唯一核销，随后正常关窗并由动作 `2030dc9a-2eab-482c-a350-de1389e26c6c` 精确恢复、自动重存到 `17572642`。tick `17574886` 仍见 1 艘活动船和 200 订单而没有 logistics finding，硅最终送达至 1533。下一笔钛订单活动时，保存动作 `8498e7c8-a6fd-4035-be1e-19f6efcc56e4` 固化 tick `17579665`；再次正常退出后，动作 `ce9e0a04-131e-458a-8358-e86aac678d69` 只恢复该 exact primary 并自动重存 `17579696`。恢复后的首个 bundle tick `17580795` 返回钛块实际产量 `12 min⁻¹`、0 logistics finding；受保护路线同时为 `stagnantSinceGameTick=lastGameTick=17580795`、需求订单 200、消费者缺料、源库存 60、fleet 1/active 1，证明离线墙钟没有成熟成旧停滞窗。钛送达 90、订单归零后，tick `17583913` 仍为 `12 min⁻¹`；最终保存动作 `1c27da6e-aaf0-4706-98a9-e041110d8f97` 固化 tick `17584412`。
- 限制或反例：该样本验证活动 carrier、两次保存/恢复、离线排除和送达，不证明真实 carrier 连续 600 game tick 完全静止的分类分支；当前自然物流仍会继续产生新订单，不能把审计时存在正常在途船当成残留故障。
- 复验触发：session/tick 连续性、持久化文档版本、carrier fingerprint、resume coordinator、DSP 物流存档字段或最终 v0.4 clean 工件变化。
- 关联：EXP-005、EXP-030、EXP-069、EXP-072、EXP-125、EXP-144、EXP-154、EXP-164、`LogisticsProgressWindowAnalyzer`、`OverseerLogisticsProgressStore`。
- 最近复验：2026-09-04（两轮活动运输普通保存/正常退出/exact-primary 恢复；新会话基线、无离线误报、送达与钛块 `12 min⁻¹` 闭环）。

### EXP-177 — 当前普通游戏接口不能安全冻结已出发 carrier，停滞正例不得伪造

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：当前 DSP `0.10.34.28529` 的 `StationComponent` 远程运输更新、Spherewright 已采用的正常 UI 等价配置动作和 v0.4 stalled-carrier 实机覆盖声明。
- 当前结论：当前版本没有一个可逆的普通游戏配置能让已经出发的运输船保持同一路线、同一订单和同一 carrier 指纹连续静止 600 game tick。`InternalTickRemote` 在每个游戏 tick 推进活动船，站点供电不参与已经出发船只的运动；`tripRangeShips` 只限制新派单。暂停游戏不会推进 game tick，撤销需求、改路线或拆塔会使路线不再同一且重置窗口。直接改写 `stage/t/uPos/uSpeed/warpState` 才能强行冻结，但违反“不直接写运行时物流字段”的边界，因此不得为了过门制造假证据。
- 直接证据：当前程序集反编译显示远程 tick 对活动 `workShipDatas` 持续更新，派单范围检查与在途推进分离；实机多轮正常运输在供电正常、受控欠压、保存恢复和源站无船等条件下都持续改变 carrier 指纹并最终送达。把远程逻辑、需求或拓扑改掉会立即改变 qualifying route；没有正常 API 能同时保留全部停滞前提。Core 的 600-tick mature/recovery 分支仍有自动测试，但文档不把它冒充 live 正例。
- 限制或反例：未来 DSP 若加入“停航/禁用运输”且保持订单和路线的原生开关，或 Spherewright 采用新的正常玩法动作，该结论需要重验。真实自然故障也可能产生停滞样本；在发生前只能把该分支列为未实机覆盖限制。
- 复验触发：DSP 版本、`StationComponent.InternalTickRemote`、舰船 UI/配置、物流动作面或 temporal classifier 验收策略变化。
- 关联：EXP-030、EXP-069、EXP-144、EXP-164、EXP-176、`StationComponent.InternalTickRemote`、`LogisticsProgressWindowAnalyzer`。
- 最近复验：2026-09-04（程序集控制流与多轮在途/欠压/重启实机对照；拒绝直接字段冻结）。

### EXP-178 — 候选包验收必须复读包内人类文档，不能只校验二进制版本

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：`scripts/package-release.ps1` 生成的版本化 ZIP、manifest、包内 `INSTALL.md` 与发布前 clean-install 复核。
- 当前结论：程序集、manifest、自包含 MCP 和文件哈希全部一致，仍不能证明候选包的版本自描述正确；发布前必须从解压后的最终 ZIP 复读安装说明和 Release notes 中的版本/兼容性文字。任何旧版本常量都使该包降级为预演，修正文档、提交 clean source 后重新生成工件。
- 直接证据：clean commit `f43c8ce` 首包为 `sourceDirty=false`、manifest 232 entries、ZIP SHA-256 `b586a79452c50b94282e08f4ea09adc6766abbe08a7b0386ef6a0a2a493392a3`；独立 smoke 返回 MCP `0.4.0.0` / 53 tools，包内 4 个 Plugin 文件和 224 个 MCP 文件实装哈希 mismatch 0，exact-primary 恢复与 live 三厂诊断均通过。但包内 `INSTALL.md` 首句仍写死 `v0.3.0`，因此它不是最终候选。IFX-023 把文档改为以 manifest/installer 版本为权威的通用表述后，clean commit `4e3824e` 的修正版包为 233 files / 232 manifest entries，ZIP SHA-256 `4fb13a25ddd261f776e81a7449afdbe1c3ba0f67b6daab640bc23922b122f86a`；包内 `INSTALL.md` 已无旧版本常量，独立包测、逐文件安装 mismatch 0、错误 token 拒绝、Plugin `0.4.0`、MCP `0.4.0.0` / 53 tools、同档恢复和 live 分页/脱敏均再次通过。
- 限制或反例：通用文档避免每版手改，但具体 Release notes 仍必须写明版本；包内文档复读不替代 manifest 哈希、安装版 MCP/Plugin 握手和同档恢复。
- 复验触发：每个版本候选包、打包包含文件、版本源、安装说明或 Release notes 变化。
- 关联：EXP-001、EXP-152、EXP-153、IFX-023、`scripts/package-release.ps1`、`scripts/test-release-package.ps1`。
- 最近复验：2026-09-04（修正文档后从 clean `4e3824e` 重打；包内说明、安装文件、同档恢复、Plugin/MCP、三厂分页与公共 JSON 脱敏均通过）。

### EXP-179 — 开局移动恢复规则必须随 MCP 和发行包交付

- 状态：`validated`
- 日期：2026-09-04
- 适用范围：当前 DSP `0.10.34.28529`、新档跳过序章后的着陆舱附近移动，以及所有由 180/600-tick 看门狗终止的普通地表 Move。
- 当前结论：`prepare_move` 只验证 owned session、当前星球、fresh player hash、有限球面目标和容差，不预判沿途碰撞；因此外部 Agent 在首次动作前必须取得一份与二进制同版本的操作规则。每个 commit 返回的 `actionId` 都要轮询到 terminal；`position_stalled/route_stalled` 后禁止重放同一目标。单一可识别障碍只做一次约 5 m 局部切向背离；多障碍或无法可靠识别着陆舱时最多做四个正交约 4 m 候选、每向一次，成功即停。成功后 fresh 复读 `Walk`、速度约 `<=0.1 m/s` 与能量；原业务 prepare 已通过时直接执行业务，不继续撞设备/资源中心。
- 直接证据：既有 EXP-057/061/076 已在电塔、仓、液罐、制造台、带和矿机夹缝形成多组实机正反样本，180-tick 看门狗多次在核心耗尽前终止并仅清理精确 owned `OrderNode`。2026-09-04 的候选二进制新档实测又完成完整闭环：新世界在 tick 38 成为 `Spherewright_New_*` owned、和平、非沙盒、1×；全量 18,595 个 vegetation 的 186 页里，最近节点是距玩家 1.439 m 的飞行舱 `protoId=9999`，同时 factory entity 为 0。朝最近铁矿的第一个正交 4 m Move 在实际移动约 2.7 m 后被舱体边缘挡住，于第 181 tick 以 terminal `position_stalled` 结束，返回 `doNotRetrySameTarget=true` 和完整有界恢复字段；该目标未重放。第二个正交 4 m 目标一次成功，终点误差 0.207 m，fresh 状态为 `Walk`、速度 0、能量约 99.67%。最近铁矿此时距 12.445 m 且 `prepare_harvest` 已通过，因此没有继续撞矿脉中心；首次正常采集 terminal/completed/succeeded，矿量 `-1`、背包铁矿 `+1`，fresh 为 `Walk`/0、能量约 98.99%。最终普通保存 terminal/completed/succeeded，tick 22222、revision 7、write health healthy。全程只走 Bridge 的 prepare/commit/poll/fresh，没有键鼠、传送、位置写入或失败目标重复提交。线上 v0.3.1 ZIP SHA-256 `b05eabb20928e98850f6792ea001149fd2e30c92082994e6d9c43254e611cdcf` 的 235 个 ZIP entry 中没有 playbook/experience/ledger 文件。当前实现新增 MCP direct resource `spherewright://agent/playbooks/opening-movement-v1`、结构化失败字段和约 2.6 KB 的发行文件；dirty 0.4.0 预演包的真实 stdio `resources/list`/`resources/read` 已通过，且不包含完整账本。
- 限制或反例：这仍不是全局寻路；四向全失败必须重新观察并停止扩张，不能无限尝试。飞行舱在当前版本可由 vegetation resource 明确认出，但 factory entity API 看不到它，且未来版本仍不能假设名称本地化文本稳定；水面 Drift、悬崖、飞行和断能另走各自状态机。
- 复验触发：每个新档首次 Move、任一 structured stall、DSP 的 landing capsule 表示变化、watchdog 阈值/订单归属变化、MCP Resource 或发行包内容变化。
- 关联：EXP-007、EXP-009、EXP-035、EXP-036、EXP-039、EXP-057、EXP-061、EXP-076、IFX-003、`docs/agent-playbook.md`。
- 最近复验：2026-09-04（默认 180/600-tick、恢复 advice、MCP Resource 注册/内容、新世界工具提示和发行包 stdio/resource smoke 共 246 项自动测试及完整 Release build 通过；候选二进制 live 新档完成“着陆舱可见→首方向第 181 tick 结构化 stall→不同正交方向脱困→业务 prepare 通过即采集→矿量/背包守恒读回→正常保存”闭环）。

### EXP-180 — GitHub 手动包与 Thunderstore 包同版本同源码，但安装布局必须分开

- 状态：`observed`
- 日期：2026-09-04
- 适用范围：Spherewright Windows x64 自包含发行、Thunderstore/r2modman 的 BepInEx 安装规则，以及 `Arcueid_77-Spherewright` 包命名空间。
- 当前结论：两个分发渠道必须使用同一 SemVer 和同一 clean source commit，但不能复用同一个 ZIP。GitHub 手动包保留顶层版本目录、安装脚本和多文件自包含 `mcp/`；Thunderstore ZIP 根目录必须直接提供 exact-case `manifest.json`、`README.md`、`icon.png`，并通过 `plugins/` 映射到 `BepInEx/plugins/<Team>-<Package>`。Thunderstore 侧 MCP 应发布成一个自包含 EXE，避免把 200 余个 .NET 运行库 DLL 放进 BepInEx 的递归 Plugin 扫描范围。静态结构/哈希验证与 Mod Manager/异机运行验收必须分开陈述。
- 直接证据：已从干净 annotated tag `v0.3.2` / commit `da11e4478b2940baf50d61395c324c3a093d0fd2` 生成 `Spherewright-0.3.2-thunderstore.zip`。四个 Plugin DLL 逐字节取自 SHA-256 `144add858a16becd17cd8b842108e9c3397d5e9b04700e05db0f967e8e890260` 的既有 GitHub 工件，MCP 从同一 tag 发布为单文件；最终 ZIP 为 12 files、SHA-256 `7e9d6d8bcb3457fe6ca44e686e20ce3d80ff7e60ee731f59806fb57f3d28b192`，标准 manifest、BepInEx 精确依赖、256×256 PNG、禁止程序集和逐文件哈希静态检查通过，并已作为 v0.3.2 GitHub Release 资产上传。`package-release.ps1` 同批产出两种 ZIP，ILLink 构建工具显式进入锁文件。
- 限制或反例：按项目所有者要求，本机不启动该 Thunderstore 包的 MCP、不做本机游戏黑盒；首次实际安装、MCP 握手、Plugin 加载和游戏状态读取由另一台电脑完成。Thunderstore 网页版本尚未最终提交时，不得把 GitHub 资产上传等同于注册表已发布。
- 复验触发：Thunderstore/r2modman 安装规则、Team/Package 名、BepInEx 扫描规则、.NET 单文件发布、每次版本发布、首次异机验收或任一渠道工件来源不一致。
- 关联：EXP-001、EXP-152、EXP-153、EXP-178、`scripts/package-release.ps1`、`scripts/package-thunderstore.ps1`、`scripts/test-thunderstore-package.ps1`、`packaging/thunderstore/`。
- 最近复验：2026-09-04（v0.3.2 clean tag 组包、静态校验与 GitHub Release 双资产对齐；Thunderstore/异机 runtime 待完成）。

### EXP-181 — 存档模式证据不应与动作授权混为同一门禁

- 状态：`observed`
- 日期：2026-09-05
- 适用范围：`v0.3.3` 和后续版本的 owned peaceful world，包括导入、普通写入、planned restart 和 flight-checkpoint adoption。
- 当前结论：沙盒状态和资源倍率应持续在 session DTO 中如实报告，但不应决定是否可以调用 Spherewright 已有的有界普通动作。真正的安全边界是工具面和不变量：仍禁止沙盒工具、物品/能量/科技注入、瞬建、瞬移和缓冲直写。当前战斗域未实现，因此可读且确认和平模式仍是门禁。
- 直接证据：旧实现在 `GameSessionTracker` 的全局 write blockers、导入采用、计划重启采用、飞行检查点采用及 handoff 脚本中重复拒绝 sandbox/non-1×，导致同一存档可能在某个阶段可见、恢复阶段却失去所有权。`GameplayModePolicy` 现将五项模式输入集中为“描述符存在 + 和平”唯一授权条件，6 组 sandbox/倍率组合正样本和 2 组缺失/战斗反样本均通过；`v0.3.3` 回移共 158 项测试通过（`120 + 15 + 23`），main 共 254 项通过（`209 + 19 + 26`），两条线的完整 Release solution 构建均为 0 warning / 0 error。
- 限制或反例：当前只有离线策略与编译证据；沙盒存档、沙盒工具已开和非 1× 存档尚需在异机验证导入、普通动作、保存和恢复；未经证明前不宣称其与基准档具有相同实机兼容性。
- 复验触发：异机首次安装，沙盒或非 1× 档的首次导入/动作/保存/恢复，DSP 版本变化，任何新动作依赖 sandbox flag 或 resource multiplier，以及后续发布门复核。
- 关联：EXP-153、EXP-180、`GameplayModePolicy`、`GameSessionTracker`、`UserSaveImportCoordinator`、`OwnedWorldResumeCoordinator`、`FlightCheckpointReloadCoordinator`。
- 最近复验：2026-09-05（源码门禁全面检索、两条线的新策略用例/全量回归与完整 Release 构建通过；等待异机实机证据）。

## 修订记录

- 2026-09-04：EXP-179 升级为 validated，IFX-003 升级为 fixed。隔离候选插件使用独立 descriptor 与 handoff 目录创建和平、非沙盒、1× 新档；飞行舱在 vegetation resource 中为 `protoId=9999`、距出生点 1.439 m，factory entity 为空。第一个正交 4 m Move 于 181 tick 返回结构化 `position_stalled` 且未重放，第二个正交目标完成；fresh `Walk`/0/充足能量后，在 12.445 m 处直接通过 harvest prepare 并完成首次铁矿采集，矿量 `-1`、背包 `+1`。最终保存 tick `22222`、revision `7`、healthy。整个验收无键鼠、无传送、无位置写入，所有 committed action 均轮询至 terminal。随后正常关闭候选进程，逐字节恢复原配置、原 Plugin 和原 handoff 目录；原 protected resume 也以 terminal/completed/succeeded 返回长期 owned planet 104，fresh tick `18081842`、revision `1`、和平/非沙盒/1×、healthy，证明隔离验收没有消费或替换长期档的恢复链。
- 2026-09-04：复验 EXP-001/069/072/125/152/153/154/156/166/169/176–178。第七次审计后的第 3 个 accepted 写是最终候选安装前普通保存 `3a05b5ca-9fd7-4702-b429-b590e73791b4`，固化 tick `17665205`；第 4 个为安装修正版包后的 exact-primary 恢复 `a231843e-9ebc-4992-8bd0-4052022a6c7d`，自动重存 tick `17665255`。安装态 Plugin `0.4.0` 拒绝错误 token，MCP `0.4.0.0` / 53 tools；三页 planet `104/102/103` 在 tick `17674814` 共享唯一 snapshot，错 item/limit cursor 均为 `STALE_CURSOR`，公开 JSON 无禁止字段或 Windows 路径，且没有 confirmed power/logistics finding。严格只读审计到 tick `17681699`：同一 owned 和平/非沙盒/1× world、healthy、0 blocker/checkpoint，玩家 Walk/0、核心满、空手搓、3/3 施工机 idle，本地 2254 built/0 prebuild，Journal `49/49` durable、0 pending/error；三颗已建 factory 的有效电网 minimum ratio 均为 1，0 underpowered station，BepInEx 0 error。当前钛块 600-tick 窗口为 0，唯一基础设施提示是远端矿脉耗尽；这是自然资源/满载状态，不是电力或物流恢复回退。当前 accepted 写计数 4，未触发十写审计。
- 2026-09-04：新增 EXP-178/IFX-023。第七次审计后第 1 个 accepted 写是普通保存动作 `a6ac2996-7673-49b2-b323-c2e5c7656276`，固化 tick `17635167`；DSP 正常退出后，clean commit `f43c8ce` 的首个 v0.4.0 预演包生成成功，manifest 232 entries、ZIP SHA-256 `b586a79452c50b94282e08f4ea09adc6766abbe08a7b0386ef6a0a2a493392a3`，独立包测为 `0.4.0.0` / 53 tools。包内 4 个 Plugin 运行 DLL 和 224 个 MCP 文件安装后逐文件 mismatch 0；旧开发目录的 3 个 PDB 因宿主删除策略拒绝而保留，但不在 package manifest、也不参与运行程序集哈希。第 2 个 accepted 写为 exact-primary 恢复 `adaabff9-3b8f-4d91-97da-c02d7407af57`，自动重存 tick `17635198`；安装版 Plugin 拒绝错误 token、报告 healthy，安装版 MCP live 在 tick `17640449` 返回 ready/600-tick/三星球完整页，并在 tick `17647128` 三页共享 snapshot/tick、错 item/limit cursor 均 `STALE_CURSOR`、无禁止字段或绝对路径。最终复读发现包内 `INSTALL.md` 仍写死 v0.3.0，因此该工件只保留为预演证据，不能交付审核；源码已改为通用版本表述，必须提交后重打。当前写计数 2，未触发十写审计。
- 2026-09-04：新增 EXP-176/177，并完成第七组累计十个 accepted 游戏写动作强制审计。十项依次为硅需求上限扩到 900/1200、火电 sorter 清回无过滤、ILS 上限恢复 30 MW、硅需求扩到 1600、运输中普通保存 tick `17572610`、exact-primary 恢复/自动重存 `17572642`、钛运输中保存 `17579665`、再次恢复/自动重存 `17579696`、最终保存 `17584412`；首个保存的 action ID 因 commit 后展示字段错误遗失，以 fresh tick/revision 唯一核销且未重放，其余已知 action 均 terminal/completed/succeeded、0 stalled/recovery/reconciliation。两次跨进程都保持同一 owned planet `104`，恢复后活动船继续移动且首个受保护样本从新 session 当前 tick 建基线，离线墙钟未产生假 stall；硅送达至 1533，钛送达 90 并恢复钛块 `12 min⁻¹`。严格审计 tick `17585687–17600202`、revision `2`：confirmed peaceful/non-sandbox/1×、healthy、0 blocker/checkpoint、restart 可用；玩家 Walk/0、核心 `400/400 MJ`、空手搓、3/3 施工机 idle，仍守恒持有远端取回的 1 船；2254 built/0 prebuild（1873 belt、200 sorter），Journal `49/49` durable、0 pending/error，三网 ratio 1，火电拓扑 `163 -> 678 -> 183` 已恢复且 ILS 为 12 GJ/30 MW。same-tick bundle `17600202` 为 ready、三星球完整页、母星 minimum ratio 1、钛块 `12 min⁻¹`、0 power/logistics finding；当前新订单属于正常自动运输。当前 BepInEx 进程日志 0 error。审计后写计数归零。
- 2026-09-04：EXP-174 升级为 validated，新增 EXP-175，并修正“完整往返未见扣能”的过时推断：那是轮询漏过短启程窗口。第六次强制审计后的 accepted 游戏写目前为 4：`191b9971-09fe-4ad9-94bd-867b506bbb0d` 将硅需求上限扩至 900 并触发首个可见约 59.7 MJ 扣能；硅送达 `533 -> 733` 后，`1ca254f1-6f1a-4b87-8e5a-4543d3d9e885` 将上限扩至 1200。随后的原生硅送达 `733 -> 933` 与下一笔钛派船在 tick `17505966–17505969` 形成 11.873 GJ/9.14 MW、network `1` ratio 约 0.5815 以及六条同 tick `insufficient_power / confirmed`。恢复动作 `0f7c84b7-847b-4502-81b2-5384db1e4bfa` / `427099af-62d2-41fb-a1ad-75d6314d55cd` 清回火电 sorter filter 0、恢复 ILS 30 MW；tick `17512817` 已回到 ratio 1、underpowered station 0、power finding 0。尚未达到十写，当前无需冻结；下一门仍是真正 600-tick carrier stall/recovery。
- 2026-09-04：第六组累计十个 accepted 游戏写动作后复验 EXP-001/007/021/030/035/036/047/069/072/080/083/084/098/140/144/156/172–174。本窗从煤节点 `379` 正常采 200 煤开始；长动作完成但本地回显丢失，已以节点 `31788 -> 31588`、背包 `0 -> 200`、revision `90 -> 92` 和未重放唯一核销。加注 `4c7b3f2e-dbe9-4855-b47d-a60687c9dda2` 先守恒转入 100；因首格边烧边空，另一次 100 的 prepare 以“当下只会移动 4”无副作用拒绝。核心充满后，`4bc076c4-e3b0-4074-ae7a-3f80a39da46f` / `196f63f9-7353-4c01-9c67-25f09755945b` 按 `71 + 29` 精确加注，最终核心 400 MJ、燃料仓 129 煤、背包煤 0；飞前普通保存 `74166db9-19a6-4d2d-a49b-bc807fff735f` 固化 tick `17409786`。返航 commit 后本地回显再因读取不存在字段中断，未重放；从受保护 ticket 只读恢复 action `efabaa9f-7e11-49f6-8f4a-27d018239b67`，该动作于 tick `17418292` 在 planet `104` 稳定 Walk/0 连续 600 tick 后 completed，checkpoint 撤销；落地保存 `f88836b3-d5d1-4e03-aaab-d7e93ac0286e` 固化 tick `17420046`。母站充电上限 `e8302062-7e22-4d3f-8107-db0cd9b1d776` 调为 150 MW，燃料 sorter `2f0ae8c7-b94f-46dd-9bd1-2fb1212cfecc` 过滤后火电 `183` 自然归零，钛需求 `b6041ba9-a2f0-4e1f-9639-fe4945e2b625` 使母站唯一船发出并形成 200 订单。强制审计 tick `17442891+`、revision `106`：9 个可寻址 action 全部 unique/terminal/completed/succeeded、0 stall/recovery/reconciliation，采煤由 fresh 状态唯一核销；同一 owned 和平非沙盒 1× planet `104`，healthy、0 blocker/checkpoint、2254 built/0 prebuild、Journal `49/49` durable、0 pending/error、0 BepInEx error。玩家 Walk/0、核心约 305.2 MJ、空燃料/手搓、3/3 施工机 idle，仍持远端取回的 1 船。母站为 12 GJ/60 kW、0 idle/1 working、钛订单 200，供给星 bundle 为 0/0 fleet/0 order；三网 ratio 1，尚无 `insufficient_power`。本审计后账本归零，先只读观察取货/返程阶段，不冒充断电正例。

- 2026-09-04：第五组累计十个 accepted 游戏写动作后复验 EXP-001/007/021/030/035/036/051/069/072/098/140/144/172–174。十段到煤节点 `379` 的短弧 move 均为独立 action，10/10 unique/terminal/completed/succeeded，0 stall/recovery/reconciliation，完成 tick 范围 `17350562…17354438`。严格审计 tick `17371613+`、revision `90`：同一 owned 和平非沙盒 1× planet `102`，healthy、0 blocker/checkpoint、175 built/0 prebuild、network 1 `4050/4050` ratio 1、Journal `49/49` durable、0 pending/error、0 BepInEx error。玩家 Walk/0、核心约 `213.6/400 MJ`、空燃料仓/手搓队列、3/3 施工机 idle、背包仍精确持 1 艘船；煤节点 `379` 剩 `31788`、距玩家 `7.444 m`。ILS `44` 仍满电、0/0 fleet、钛 `200/200`、硅 `109/300`、Remote Supply、0 order。本审计不执行游戏写；账本落盘后写计数归零，下一写只正常采煤/加注以满足返航门。

- 2026-09-04：新增 EXP-174，并在第四组十个 accepted 游戏写动作后复验 EXP-001/007/021/030/069/072/098/105/140/144/172–174。八段球面短弧 move 与 fleet transfer `d0ac4683-8c05-4d07-83a7-f29b3a0e3e02`、save `9a77399b-84d4-4821-95b8-ef3dfd9073ac` 均为 terminal/completed/succeeded，无 stall/recovery/reconciliation。严格审计 tick `17338179+` 为同一 owned 和平非沙盒 1× 世界、healthy、0 blocker/checkpoint、restart 可用、175 built/0 prebuild、network 1 ratio 1、Journal `49/49` durable、0 pending/error、0 BepInEx error。玩家 Walk/0、核心约 193.4/400 MJ、空燃料仓、空手搓、3/3 施工机 idle，背包精确持 1 艘运输船；ILS `44` 满电、0/0 fleet、钛 200/200、硅 109/300、Remote Supply、0 order。下一游戏写前账本已更新；后续先正常补足返航能量，不改变供货库存或重新装回远端船。

- 2026-09-04：新增 EXP-173，并在第三组累计十个 accepted 游戏写动作后复验 EXP-001/007/021/030/047/069/072/080/083/084/144/156/172。八个保留 action 全部 `completed/succeeded`；展示失败前已经完成的 20 煤采集/加注由节点 `51992 -> 51972`、背包 `20 -> 0`、飞前燃料仓 fresh 91 与未重放共同核销。飞行 `2209a388-9f77-41f7-bd31-d32f7d9e6066` 在独立 checkpoint 后稳定落地 `102`，checkpoint 自动撤销；第十项普通保存 `04239b93-aa4e-46b4-8b89-aa78fc4793c2` 固化 tick `17305571`。严格审计 tick `17309480+` 为同一 owned 和平非沙盒 1× 世界、healthy、0 blocker/checkpoint、Walk/0、Journal `49/49` durable、175 built/0 prebuild、network 1 ratio 1、0 BepInEx error。远端 ILS `44` 满电、30 MW、钛 `200/200`、硅 `109/300`、两槽 Remote Supply、0 order、`1 idle / 0 working` vessel；玩家核心约 76.7/400 MJ、35 煤、空手搓、3/3 施工机 idle。下一写前账本已更新；后续先安全接近站体并正常取走远端唯一 idle vessel，建立母站单端派船条件。

- 2026-09-04：第二组累计十个 accepted 游戏写动作后复验 EXP-001/007/021/030/069/072/098/144/156/170–172。九个可寻址 action 全部 `completed/succeeded`，组合脚本在第一个槽已成功后因第二槽自然出现订单而停止；未重放的第十项由钛槽 `item1004 / 0/300 / remote None / orders0` 与 revision 唯一核销。两轮供电试验均按事实判为负例：一次 `+200` 硅真实往返使库存 `333 -> 533`，但母塔连续 minimum energy 12 GJ、maximum request 60 kW、minimum ratio 1；第二次火电 output 自然归零并把上限升至 150 MW，`533/700` 却因远端无现货不派单。恢复火电后的严格审计 tick `17272033+` 为同一 owned 和平非沙盒 1× 世界、healthy、0 blocker/checkpoint、2254 built/0 prebuild、三网 ratio 1、Walk/0、满核心、空手搓、3/3 施工机 idle、Journal `49/49` durable、0 pending/error、0 BepInEx error。火电 sorter 已恢复 `163 -> 678 -> 183` 无过滤，机组输出 `18335/tick`；ILS 满电、0 order，150 MW 上限仍只是无负载暂存值，下一写先恢复 30 MW。

- 2026-09-04：新增 EXP-171/172，并在累计十个 accepted 游戏写动作后复验 EXP-001/007/021/030/069/072/098/104/144/154–156/159/164/167/170。七个仍在当前 action store 的配置动作全部为 `completed/succeeded` 且未 reconciliation/stall/recovery；三次跨进程保存/恢复由 exact-primary tick `17048233/17048265` 与 fresh 主档 tick `17136808` 唯一核销。严格审计 tick `17196640+` 为同一 owned planet `104`、和平/非沙盒/1×、healthy、0 blocker/checkpoint、2254 built/0 prebuild、三网 ratio 1、Walk/0、满核心、空手搓、3/3 施工机 idle、Journal `49/49` durable、0 pending/error、0 BepInEx error。sorter `162` 已恢复无过滤的 `161 -> 141` 拓扑，PLS `918` 保持 Supply；ILS `1657` 满电、0 order，150 MW 只是暂存的已证明配置，下一游戏写将先恢复 30 MW。

- 2026-09-04：新增 EXP-170，并复验 EXP-001/007/021/030/069/072/098/144/159/164/167。先以母星硅 `+200` 真实订单清开远端共享输入，使 189 钛石正常送回并恢复本地钛块源。随后只通过正常站点 UI 路径在订单归零窗口暂时撤销 `918:slot0` 的 Supply；完整 600-tick bundle 将 `767/item1118` 精确分类为 `logistics_blocked / confirmed`。恢复 Supply 后原生无人机送达，finding 清零、钛晶石实际产量达到 `12 min⁻¹`。保存结果由 fresh `LastOwnedSaveGameTick=17136808`、revision 8、healthy、0 blocker/checkpoint 和新 restart ticket 核销；本地展示误读了不存在的 action 字段后没有重放保存。真正的在途 600-tick stall、缺料与断电试验仍开放。

- 2026-09-04：EXP-169 由 `observed` 升级为 `validated`，并复验 EXP-001/030/069/072/125/154–166。旧进程普通保存 tick `17048233` 后正常关窗；Release Plugin/Contracts/Core 以 SHA-256 `90408AD2BC9ED88335853F09695BB75A0900522D9CA022AF86E644DC393B1B16` / `583AFCFCC3995C80278679CA891191DA43CFC7B370B6EB2DD4E5CD197524BAF7` / `98DFF4CDA2F192691070892F94AD6FDDB47901E594D410C37AC5222E33131A3F` 零差异部署，exact-primary 只恢复同一 planet `104` 并自动重存 tick `17048265`。live 完整页与三页 continuation、两类错绑 cursor、12,156-byte JSON 脱敏、源码 MCP `0.4.0.0` / 53 tools 调用均通过；最终审计 tick `17059827+` 为 healthy、Journal `49/49` durable、2254 built/0 prebuild、Walk/0、满核心、3/3 idle drone、三网满服务、0 blocker/checkpoint/BepInEx error。受控停滞与三类故障门仍开放。

- 2026-09-04：复验 EXP-168。用户已明确批准 v0.3.1 先发包到另一台电脑实测；annotated tag `v0.3.1` 指向最小回移 commit `33a733f`，直接父是 v0.3.0 `a52ff44`，没有混入 v0.4。GitHub prerelease ZIP 为 `sourceDirty=false`、232 manifest entries、MCP `0.3.1.0` / 50 tools、127 tests、SHA-256 `b05eabb20928e98850f6792ea001149fd2e30c92082994e6d9c43254e611cdcf`。仍明确保留跨电脑 DSP 实机门，不把发布动作冒充原档/副本/恢复验证。

- 2026-09-04：新增 EXP-169，并复验 EXP-001/125/154–166。第九个 v0.4 离线切片新增独立第 53 个 MCP 只读工具，把生产/根因与供电/物流/科研在同一主线程任务、同一 tick、同一 factory/planet 身份下合并为 `public_allowlist_v1` 诊断包；独立 cursor 保持 session/item-filter/page-size/expiry/容量约束。Contracts/Core/MCP `19/181/23` 共 223 项、完整 Release solution 0 warning/0 error。未启动或部署 DSP、未执行游戏/存档写，live 三厂分页/脱敏复读和受控故障门保持开放。

- 2026-09-04：新增 EXP-168。用户明确将人工存档交接定义为“prepare 预检后在对话中询问，下一条明确确认后 commit”，因此删除未发布的快捷键/验证码设计，改为三项显式提交声明、短时单次 plan、受限 session/revision/对象绑定、正常另存/header 复读和 attached-existing-save Journal。完整 solution 0 warning/0 error，Contracts/Core/MCP `18/174/22` 共 214 项通过；本批未部署 Plugin、未触发游戏写、未修改任何存档，也未创建 tag 或 Release。

- 2026-09-03：完成上一审计后的 10 个 accepted 游戏写动作复核。前八项为两次返航补煤采集/加注、动作 `3515d9f4-8a65-404b-b7bd-79f75ed7a7bc` 成功返航、主档保存 `dd745e09-3f18-48da-89d9-e35e77f241e8`、直接 EXE 短命进程与 Steam 进程各一次 exact-primary 恢复；后两项为煤节点 `402` 的唯一 20 件采集和守恒加注 `e92fa9ed-a4f5-4e51-aa5c-afb82a40b165`。采集终态已返回后仅展示字段失败，fresh 节点 `52072 -> 52052`、玩家煤 20，再由加注核销为玩家 0、燃料舱 19 且反应堆消耗第 20 件，没有重放。严格审计 tick `14874643+` 为同一 owned planet `104`、confirmed peaceful/sandbox disabled/1×、healthy、0 blocker/checkpoint、restart ticket 可用；玩家 Walk/0、核心 `400/400 MJ`、无手搓、3/3 施工机 idle；Journal `49/49` durable、无 pending/error；母星 `2254 built/0 prebuild`，组件计数与上一审计完全一致，三厂全部分页返回，所有有负载电网最低供电比 1.0，双星 ILS 各 1 idle/0 working vessel 且无订单。600-tick Overseer 窗口 ready；现存缺料/输出堵塞均有结构化 finding，未出现 quarantine、outcome unknown、串料、未解释正增量或时间线分叉。EXP-001/002/007/030/035/047/069/072/083/125/144/154/156/164/166/167 与现场一致；计数在本审计后归零，下一写入才允许创建飞前保存/checkpoint。

- 2026-09-03：新增 EXP-166/167 与 IFX-022，并复验 EXP-001/002/007/030/069/072/083/125/144/154/156/159/161–165。远端共享输入带暴露“满硅堵钛”的头部阻塞；复核时否决了“100% 整船阈值导致 100 上限不派单”的推断，因为 EXP-144 已证明原生会把阈值收紧到槽上限以下。把源站钛/硅上限调整到 `200/300` 后，真实钛 route 经 `-200/+200` 派单、运输船长时移动、源库存 `200 -> 79`、送达归队和母星钛块 `12 min⁻¹` 完成活动正例，移动 2100+ tick 全程没有误报 stall。远端 save `14535735` 后，返航动作 `3515d9f4-8a65-404b-b7bd-79f75ed7a7bc` 成功落到 planet `104` 并在主档 save `14575384` 固化。高频摘要轮询又发现完整首屏占满不可达 snapshot；修复后只有真正分页记录占容量。四个 Release 文件源/部署一致，Plugin/Contracts/Core 为 `66D6E4631D0AF8DD3B6C7D6AE11DFF02AE37B13EE8A7A8D77DD2BDCF598B38C9` / `D0F580634540C486BFE865A8C6E50402647AD6A435F7D66205D6DDCAB9C2353E` / `DE36BF4028D3C3FED0A5E7CA871F7CA040E66EF0682521A2BF5A25E6BE62AA32`；exact-primary 自动重存 `14575416` 后由 Steam 进程继续同档。live 16 个完整页、8 个分页首屏、满载拒绝、满载完整页和 continuation 全部符合预期。最终只读审计 tick `14585723+` 为 peaceful/non-sandbox/1×、healthy、0 blocker/checkpoint/prebuild、2254 built、Walk/0、3/3 drone idle、Journal `49/49` durable；206 项测试、完整构建和 BepInEx 零 error 通过。活动 shipment 已闭合，受控 stalled shipment 和三类故障收尾仍开放；没有 tag 或 Release。

- 2026-09-03：新增 EXP-165/IFX-021，并复验 EXP-001/030/069/072/117/154/159/161–164。第七个 v0.4 切片按当前程序集 `UpdateNeeds/UpdateInputSlots` 的动态 needs 语义，从精确 supply endpoint 的 Input belt 反向复用 item/sorter/splitter 图，再在所有 owned factory 捕获后跨 planet 绑定生产者。最终审查把聚合 supply 证据与单条公开路径分开，且有 demand route 时不再跳入另一本地直连候选。普通保存 `14413801`、正常关窗和四 DLL 零哈希差部署后，exact-primary 只恢复同一 planet `104` 世界并自动重存 `14413832`。live 钛块路径从 `104:530` 经 `104:1657 -> 102:44` 到达未显示工厂的矿机 `102:1`，定位其 50/50 输出堵塞；独立 item `1004`、黄糖同星路径、三页游标、源码 MCP 50-tool live call 与最终 healthy/Journal `49/49`/0 prebuild 审计均通过。205 项测试和完整构建通过；活动/停滞 shipment 与受控故障门仍开放，未打 tag、未发布。

- 2026-09-03：新增 EXP-164/IFX-020，并复验 EXP-001/030/069/072/154–163。第六个 v0.4 切片把物流 order/carrier/delivery 进展接入按 owned-save 哈希隔离、current-user ACL 保护且原子替换的 600-tick 状态机；最终审查又收紧为消费者确实缺料、需求端正 reservation，并把同次读取的全部路线合成一次原子持久化，只有 durable analysis 才进入公共 DTO。最终普通保存 `14290235`、正常关窗、四文件零差异部署后，只消费 exact-primary ticket 恢复同一 planet `104` 世界并自动重存 `14290266`。保护文档为 3 条哈希 route/2942 bytes、无外部 SID allow 和原始 save identity，且消费者充足/不足样本都已出现；黄糖三厂读取与四节点递归不回归。204 项测试和完整构建通过。因三路现场均无订单/active carrier，活动与停滞正反例继续开放；没有生产写、开新档、隔离、tag 或 Release。

- 2026-09-03：新增 EXP-163，并复验 EXP-001/030/069/072/154–162。第五个 v0.4 切片最终复核发现 splitter `outFilter` 不能被“物理相连”概括；按当前程序集把 exact output slot/belt 身份以及 priority-only match / non-priority exclusion 双向规则接入遍历。普通保存 `14109460`、正常关窗、源码相等部署后，只消费 exact-primary ticket 恢复同一 planet `104` 世界并自动重存 `14109491`。Bridge/MCP 四节点黄糖路径保持不变，三厂分页分别共享 tick `14119083/14119093`，错绑 cursor 继续返回 `STALE_CURSOR`；188 项测试、完整构建和最终 healthy/Journal `49/49`/0 prebuild 审计通过。没有生产写、开新档、隔离、tag 或 Release。

- 2026-09-03：新增 EXP-162，并复验 EXP-001/002/030/069/072/154–161。第五个 v0.4 切片把黄糖缺料从 matrix lab `774` 递归到 diamond assembler `715` 的高能石墨短缺；复核时把输入拓扑改为逐 item 独立遍历，并要求所有中间 sorter filter 匹配。Core 明确限制 8 层/64 producer、检测环和 resolver 身份，截断时返回结构化 stop reason，上游实际速率保持 unknown。最终普通保存/恢复为 `14059914/14059946`，Plugin/Core hash `A1D23E1BA3EEE6DB5FB05C3B92F783E5026591734C43FC186D65F350D8790791` / `CA27E4240A89BAD6013A8E71FDFF9D28AAED080673A362B8850D38EB2D8E7B93`，live tick `14061471` 四节点路径无截断。181 项测试、完整构建、50-tool MCP live call、三厂生产/摘要分页和最终 healthy/Journal `49/49`/0 prebuild 审计通过。跨星递归、时间型物流停滞与受控故障门仍开放；没有 tag 或 Release。

- 2026-09-03：新增 EXP-158–161 与 IFX-019，并复验 EXP-002/030/069/072/142/144/154–157。第四个 v0.4 切片把首因分类接入真实 assembler/lab/miner 缓冲、电网、矿源和有向物流拓扑；首轮 live 在中转仓 `259` 停止，补全 storage/tank/inserter 入边后才精确命中母星 demand `1657` 与远端 supply `44`。三轮都先普通保存、正常关闭、七文件零差异部署，再只消费 exact-primary ticket 恢复同一 owned world；最终保存/自动重存为 tick `13943810/13943842`，Plugin hash `D40D6BEA4E76697EB14C5F1DE3B0CC61532E4BF634125E1A9488D5024FDF59E1`，最终构建后仍与部署文件一致。自然现场闭合 `output_blocked`、`material_shortage`、瞬时 `insufficient_power` 和无 item 身份的 `vein_exhausted`，但没有把单快照有载具路线误报为物流停滞。最终三页共享 tick `13986388` 并安全拒绝错绑 cursor，源码 MCP `0.4.0.0` 完成 50-tool initialize/list/live call；审计 tick `13990990+` 为 peaceful/non-sandbox/1×、healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、0 prebuild、无 blocker/checkpoint。完整 solution 0 warning/0 error，174 项测试；递归上游、跨 tick 运输进展和受控故障/修复仍未完成，本批不打 tag 或发布。

- 2026-09-03：新增 EXP-157/IFX-018，并按新证据缩小 EXP-154/155 的过期限制。第三个 v0.4 切片逐 IL 重现理论产出且不触碰 UI `refProductSpeed` 缓存；首轮 live 由三台合法耗尽矿机暴露校验顺序缺陷，修正为 `veinCount=0 -> 0 capacity` 后重新完整构建、正常保存、关窗、零哈希差异部署和 exact-primary 恢复。最终实机闭合母星矩阵、矿点、水/油、冶炼/化工/制造以及远端硅钛理论值，三厂分页和边界拒绝继续成立；共 160 项测试。故障分类、上游图和受控故障仍未完成，本次没有打 tag 或发布。

- 2026-09-03：新增 EXP-156、记录 IFX-017，并复验 EXP-001/021/030/069/072/104/142/152/154/155。第二个 v0.4 只读切片在同一 owned world 的三座已创建工厂上完成电网/物流/全局科研分页；首次 live 读数以“33 个 generator 且满供电、旧 generated 却为 0”证伪早期 `energyExport` 映射，改为逐组件 `generateCurrentTick` checked sum 并单列防御场导出。修正字段后先保存/恢复到 tick `13696182+` 做正例，再补互斥 collector 分类、空槽一致性和全域扫描预算；普通保存 tick `13725278`、正常关窗、7 个文件同批安装零哈希差异，并只消费 exact-primary ticket 恢复 planet `104`、自动重存 tick `13725324`。最终审阅再补科技队列 runtime 双身份检查，重新构建后普通保存 tick `13767062`、正常关闭并以零哈希差异部署最终 Plugin `3766E3A770FFB7BAA24FA870CA569BD90F5BE776802A04F213EB2634B79E9C6E`，受保护恢复自动重存 tick `13767093`。最终三页快照 tick `13773036`、生产窗口回归、队列 `[3401]`、边界拒绝、150 项测试、完整构建和 50-tool MCP live call 均通过；审计 tick `13775095+` 为 healthy、Journal `49/49` durable、Walk/0、满核心且无 blocker/checkpoint。全程未开新档或隔离。后续 tag/Release 仍须按用户新增门禁先提交候选证据审核，本次不发布。

- 2026-09-03：新增 EXP-155，并复验 EXP-001/007/030/069/072/104/152/154。先以旧 Plugin 的普通 save API 保存到 tick `13617247`；一次结果展示因访问不存在的 `savedGameTick` 失败后，只用 fresh revision/tick 核销且未重放。开发 Plugin 两轮均通过正常关窗、同一 protected primary resume 和健康自动重存；最终保存边界 `13626113` 在恢复后 16 tick 即保留原生红糖/有机晶体/钛晶石窗口，证明离线未计入且统计随档恢复。三厂分页、游标错绑/重复 item/越界页大小拒绝、49-tool MCP live call、142 项测试和完整 solution 均通过；本批新增三次普通保存和三次受保护恢复，均为同一 owned world，写健康未隔离。

- 2026-09-03：新增 EXP-154，开始 v0.4 Overseer 首个只读基础切片。新增脱敏窗口/速率/故障证据契约、相邻累计计数连续性分析和五类首因分类；跨 session 仅凭同一受保护存档身份延续，回档、计数回退、同 tick 异常增量和超限采样缺口均 fail-closed。新增 16 项测试后 Contracts/Core/MCP 为 `15 + 101 + 19 = 135` 全通过，完整 solution 0 warning / 0 error；未部署 Plugin、未重启 DSP、未对存档执行写动作。用户新增发行规则已独立提交：今后任何 tag/Release 都必须先提交候选 commit、证据、工件哈希与 Release notes 供用户明确审核。

- 2026-09-03：新增 EXP-153，并完成 v0.3 最终发布复核。最终工件从 clean commit `a52ff440b47830f2f3a06a5ae97c7ff11bd15833` 生成，manifest 为 232 个文件、包内含 manifest 共 233 个文件，工具面 48，SHA-256 为 `705081710b7061c6a00c4c8836a7d2869b13bd8b8fb6f42bfb24b7f0d62783c1`。由于 preview 与最终 payload 实际出现 Plugin `3/4`、MCP `4/224` 文件差异，没有复用候选包的实机结论；普通保存到 tick `13516383` 并正常关闭后，安装最终 ZIP 本体，228 个运行文件逐一核对 mismatch `0`。受保护恢复同一 planet `104` 世界并自动重存 tick `13516415`，119 项测试、错误 token 拒绝、live Plugin `0.3.0` 和安装版 MCP `0.3.0.0` 调用均通过。最终审计 tick `13520330` 为 peaceful/non-sandbox/1×、healthy、0 blocker/checkpoint、0 prebuild、Journal `49/49` durable、Walk/0、核心满电、3/3 drone idle。annotated tag `v0.3.0` 精确指向 `a52ff44`，GitHub Release 已发布且线上 ZIP digest 与本地一致。v0.3 至此关闭，当前目标切换为 v0.4 Overseer。

- 2026-09-03：新增 EXP-152 并记录 IFX-016。首次 clean `0.3.0` 工件的 233 文件哈希、MCP initialize 和 48 工具均通过，但实机 Bridge 揭示 Plugin 仍报告 `0.1.0`，主动阻止 tag。版本来源统一后新增第 119 项测试，Core/Contracts/MCP 为 `86 + 14 + 19` 全通过，完整 solution 0 warning / 0 error，Mono.Cecil 证明 BepInEx metadata 为 `0.3.0`。主档先普通保存到 tick `13494061`，游戏正常关闭；修复候选包将旧 Plugin 和 MCP 目录分别整体移入可恢复备份后做第二次干净安装。新进程 live Bridge 在主菜单即报告 `0.3.0`；prototype preload 完成前的 resume prepare 以 `BRIDGE_NOT_READY` 无副作用拒绝，等待 ready 后只消费同一 protected ticket，恢复 planet `104`、和平/非沙盒/1×并自动重存到 tick `13494092`。随后 wrong-token 拒绝、正确 Bridge 握手、安装版 MCP `0.3.0.0 -> spherewright_get_status -> Plugin 0.3.0` 全部通过。fresh 审计 tick `13504262` 为 Walk/0、核心 `400/400 MJ`、3/3 drone idle、0 prebuild、Journal `49/49` durable、玩家不持有水/油/有机晶体/钛晶石/结构矩阵；有机晶体和钛晶石设备仍满电运行，sorter `2254` 携精炼油、`977` 携结构矩阵。黄糖 lab 此刻因本批金刚石正常耗尽而停机，不把最后在途矩阵冒充无限供料；此前跨窗持续生产证据仍成立。最终 tag 前必须从 clean Git commit 重打非 dirty 工件。

- 2026-09-03：完成 v0.3 无玩家搬运黄糖链的最后十写审计与里程碑保存，新增 EXP-151，将 EXP-147 升级为 `validated`，并复验 EXP-007/018/021/028/037/062/065/068/070/079/102/118/126/133/142/143/147/150。上一审计后的第 1–2 项 sorter `2247/2248` 分别连接 `2243 -> 2245` 与 `2246 -> storage 761`；50 秒内目标只有塑料 `35 -> 75`，油/水均为 0，而源 sorter `2218/2229` 满电携正确物，定位为三处单 sorter 后置桥被上游塑料持续占满。第 3 项递归手搓 6 sorter；第 4–9 项把 `2249/2250`、`2251/2252`、`2253/2254` 分别并联到三处瓶颈，fresh 反查六只均为正确双端、network `1`、ratio `1.0`。第 10 项将玩家隔离的 67 氢守恒转入专用氢仓 `136`，仓由 7 增至 74、玩家氢归零；另一次对满仓 `907` 的 transfer prepare 以容量不足无副作用拒绝，不计写。严格审计 tick `13432430`、revision `150`、2254 built/0 prebuild（1873 belt、200 inserter），目标仓三料为 `270/40/25`，化工厂 `760` 与制造台 `767` 均工作，healthy。其后只读长窗看到三料增至 `304/86/72`、lab `774` 连续工作、金刚石 `84 -> 74`，并多次抓到 sorter `779/977` 携带 item `6003`；全程玩家没有水、油、氢、有机晶体、钛晶石或结构矩阵。第 1 个新写动作是普通保存 `3215a7f5-2d8a-4c85-af89-c3a8201f71f2`，精确覆盖 tick `13444822`。保存后审计 tick `13446315`、revision `151`、peaceful/non-sandbox/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint、Journal `49/49` durable、2254 built/0 prebuild；仓 `761` 为 `345/179/164`，lab `774` 仍工作，金刚石余 55，采样再次抓到 sorter `779` 携黄糖。电网 `1/4` 满供电，网络 `2` 因风力瞬时波动低于 1，但本链全部关键设备和 sorter 位于满供电 network `1`；未把瞬态概括成全网满供电。v0.3 游戏内容门由此闭合，写计数归零；下一步只做发行回归与打包，不并行启动 v0.4。

- 2026-09-03：完成三料共享主干接近目标仓的严格十写审计，新增 EXP-150，并复验 EXP-007/018/021/028/037/065/070/102/124/125/133/142/143/145/148/149。第 1 项从 `2208` 续建 4 格到纯油中继旁的 `2215`；随后读回发现 `163/784` 分别含 12/55 氢，撤回“仍为纯油仓”的先验。sorter `906` 在 45 秒、694 次复读中没有出现 cargo-free 配置窗，全部停在 commit 前，不计写；第 2–3 项用两次 normal transfer 将 67 氢守恒隔离到玩家。第 4 项 sorter `2218` 建成 `784 -> 2215`、network `1`/ratio `1.0` 并携 item `1114`；它自身的 20 秒过滤尝试同样零 commit。第 5–6 项以 10 格带到水仓旁并建 sorter `2229`，实际携 item `1000`。第 7 项从 `2219` 外绕 8 格到 `2237`；第 8–9 项在旧带远侧建独立 6 格 `2240 -> … -> 2243`，以满电 sorter `2244` 接回并实际携塑料；第 10 项在下一条旧带远侧建 `2245 -> 2246`，首带距 `2243` 2.64 m、末带距目标仓 `761` 1.82 m。fresh 审计 tick `13405919`、revision `131`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；2246 built/0 prebuild（1873 belt、192 inserter），玩家余 37 belt/2 sorter/1 power-node并隔离持有 67 氢，3/3 施工机 idle，三网 consumer ratio 均 `1.0`，Journal `49/49` durable、无 pending/error。仓 `163/784/753` 分别为 600 油/600 油/600 水，仓 `761` 仍空；审计后写计数归零，下一两写才完成桥接与入仓，v0.3 内容门仍开放。

- 2026-09-03：完成下一组严格十写审计，新增 EXP-149 并复验 EXP-007/018/021/028/035/036/070/124/125/133/142/143/145/148。第 1–2 项沿先前成功路线分两段向旧电塔 `143` 前进；第 3 项只差 0.87 m 时连续 181 tick 位移不足 0.75 m，被专用看门狗明确终止，fresh 仍为 Walk/0 且没有残留订单。第 4 项在正常范围内从自动铁仓 `28` 守恒取得 100 铁；第 5–6 项递归手搓 2 电塔和 3 sorter，队列清空。第 7 项电塔 `2204` 落在 sorter `2203` 约 9.2 m、旧节点 `142` 约 19.0 m 处，随后 `2203` 为 network `1`/ratio `1.0` 并实际携 item `1115`。第 8 项把主线从 `2202` 侧移 3 格至 `2206`；第 9 项在旧带远侧建 5 格独立带 `2212 -> … -> 2208`；第 10 项 sorter `2213` 精确连接 `2206 -> 2212`，立即满电携塑料。fresh 审计 tick `13364570`、revision `113`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；2213 built/0 prebuild（1843 belt、189 inserter、39 power-node），玩家余 67 belt/5 sorter/1 power-node，3/3 施工机 idle，三网 consumer ratio 均 `1.0`，Journal `49/49` durable、无 pending/error。两只新桥 `2203/2213` 在同一快照中均为 Inserting/stack 1/item `1115`；当前自由末端 `2208` 距精炼油仓约 9.2 m。审计后写计数归零，v0.3 内容门仍开放。

- 2026-09-03：在第 9 个成功写动作主动提前完成下一次健康审计，并把 EXP-148 升级为 `validated`。本窗第 1 项从 `2135` 续建 2 格至首道双旧带墙前的 `2144`；第 2–3 项在墙外建独立 3 格带 `2148 -> … -> 2146`，再以 sorter `2149` 精确跨接 `2144 -> 2148`。初读为 network `0` 后，第 4–5 项正常手搓并建成电塔 `2150`，复读 `2149` 已进入 network `2`、实际携 item `1115`。第 6–7 项沿零旧带重叠路线续建 27/16 格到 `2193`；第 8 项在旧带 `433` 外侧建 9 格独立带 `2194 -> … -> 2202`；第 9 项 sorter `2203` 完成 `2193 -> 2194` 双端连接。fresh 复读同时证明 `2203` 仍为 network `0`、Picking/stack `0`，因此没有把拓扑成功冒充通料，也没有为了凑第十写继续施工。审计 tick `13310101`、revision `95`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；2203 built/0 prebuild（1835 belt、188 inserter、38 power-node），玩家余 75 belt/3 sorter，3/3 施工机 idle，三网 consumer ratio 均 `1.0`，Journal `49/49` durable、无 pending/error。当前塑料主路共 162 格带，前两只桥 `2115/2149` 已受电携货，第三只桥 `2203` 待补电；审计后写计数归零，v0.3 内容门仍开放。

- 2026-09-03：完成有机晶体三料共线的下一组严格十写审计，新增 EXP-148 并复验 EXP-007/018/021/028/037/062/070/124/125/133/142/143/145/147。第 1–2 项从 `2085` 以 11/10 格短段试探绕开回环，形成无环末端 `2098`；首条 4 格独立带 `2110 -> … -> 2107` 与末端相距 5.64 m，之后的 sorter prepare 明确 `TooFar`，没有 commit，该独立带保留为无输入、不属于主链的试验段。第 4 项在 3.77 m 处建第二条 4 格独立带 `2111 -> … -> 2114`；第 5 项 sorter `2115` 正常建成 `2098 -> 2111`、network `2`/ratio `1.0` 并实际携 item `1115`，完成显式跨线。第 6–7 项再从 `2114` 施工 10/9 格外绕到 `2134`。第 8 项手搓 24 批基础带，普通 replicator 消耗 72 铁并产出 72 条带；第 9 项递归手搓 3 个 sorter，中间电路板由玩家现有铁/铜正常制造，手搓队列最终清空。补料后的直线预演精确定位首批障碍为 planned index `10/11` 的带 `2062/1695`；第 10 项只施工障碍前 9 格、最小新路径设备中心净空约 13.8 m，停在自由末端 `2135`。fresh 审计 tick `13246759`、revision `77`、planet `104`、和平/非沙盒/1×、Walk/0、核心约 `396.81/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；2135 built/0 prebuild（1778 belt、186 inserter）、3/3 施工机 idle、三网 ratio 均 `1.0`、Journal `49/49` durable、无 pending/error。主路由 `2037 -> … -> 2098` 73 格、sorter `2115`和 `2111 -> … -> 2135` 32 格组成；末端尚未接近油/水/目标仓，v0.3 门仍开放。审计后写计数归零，下一写从 `2135` 的双带障碍前做显式跨线。

- 2026-09-03：完成有机晶体三料共线首五段的严格十写审计，复验 EXP-007/018/021/028/037/062/070/124/125/133/145/147。第 1–5 项为到无线塔区的往返移动、从自动铁仓守恒取 200 铁块、普通递归手搓 50 条带和 4 个 sorter；所有动作均有终态，手搓队列清空。第 6 项先在纯塑料仓 `558` 外侧建 5 格自由带；第 7 项建成 sorter `2038`，反向连接精确为 `558 -> 2038 -> 2037`，实际携带 item `1115`；第 8–10 项依次施工 14/23/10 格外绕带。直达精炼油仓的多组原生成功候选与旧带重合 4–21 格，全部丢弃；所选段均先做 `0.25 m` 全厂占位排除和非带设备净空复核。fresh 遍历为 `2037 -> … -> 2085` 共 52 格、无环、唯一自由末端；玩家余 117 条带/3 sorter。审计 tick `13126837`、revision `57`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；2071 built/0 prebuild（35 assembler、1721 belt、185 inserter），3/3 施工机 idle，Journal `49/49` durable、无 pending/error。电网 2 在严格快照曾因风力波动短暂为 `0.9779`，随后 fresh 复读三网均回到 `1.0`；暂不冒充持续缺电。账本落盘后写计数归零，下一写只从末端 `2085` 继续全路径排除后的安全段；三料尚未接到仓 `761`，v0.3 内容门仍未闭合。

- 2026-09-03：新增 EXP-147，完成永久有机晶体桥的实物流复验并复验 EXP-007/018/021/028/037/048/062/080/086/088/103/118/129/133/142/143/146。账本归零后的 6 个写动作均为普通双端守恒转移：`8c7fde6d-5708-4d39-80bb-b23c6169eabc` / `dc9af015-8eb5-4251-832d-a84af594fa37` 把 20 自动塑料从仓 `558` 送入 `761`，`90ba2dc7-1e42-44b0-b7d1-a22495a57a5a` / `d212bb63-e07f-44f3-bffa-e2544b3b3bb9` 送入 10 精炼油，`659fd232-4db2-458d-afa6-9ed54dd583ee` / `472121d3-67e0-49f1-83ba-0270ec5b1920` 送入 10 自动水；一次误把塑料 ID 写成 `1116` 的 prepare 以 `INVENTORY_INSUFFICIENT` 无 action/commit/副作用返回，不计写动作且没有改变物品 ID。只读连续采样在 tick `12981203` 和 `12981892` 两次抓到 sorter `2032` 处于 `Sending`、携带 item `1117`/stack `1`；钛晶石制造台 `767` 随后实际取得有机晶体并持续工作，批次完成后三料、有机晶体和钛晶石中间缓存均归零，黄糖 lab `774` 运行且金刚石仓 `775` 从 `94 -> 84`。第 7 项普通保存 `201a6e19-6031-41f8-9d33-72d6a50c942d` 固化 tick `12996056`。保存后主动提前审计 tick `12998411`、revision `38`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；Journal `49/49` durable、无 pending/error，2032 built/0 prebuild（184 sorter），3/3 施工机 idle，三张电网 consumer ratio 均为 1.0。ILS `1657` 满能并保持钛/硅远程需求，唯一运输船仍在工作；PLS `916` 有 1 架无人机在钛需求订单中。运输船引擎仍为 `1928/36000`，当前瓶颈是蓝矩阵而非黄糖桥。该里程碑主动按 7 写提前审计并再次归零；下一步进入 v0.3 发布门，不并行启动 v0.4。

- 2026-09-03：完成上一审计后的 10 个成功游戏写动作复核，复验 EXP-007/018/021/028/037/048/062/070/073/079/095/103/115/140/144/145/146。第 1 项已接受的 100 塑料投料在动作完成后被本地空集合 `.Sum` 展示错误遮住，未重放；fresh 状态唯一核销玩家塑料 `100 -> 0`、输入仓出现 92 且化工厂已预取 2。第 2–7 项把本批自然产出的 50 有机晶体分成 `12 + 19 + 19`，经 `storage 762 -> player -> storage 768` 六次正常转移守恒接入钛晶石线：`9fda05e0-7f7f-473b-ad91-83dd40dcd9fc`、`d0704d16-912e-4096-a7ae-e95113833bd5`、`34aaeb20-9959-418e-8063-aace230da0bd`、`df39bfab-d7d7-4901-b4ea-882da32d71da`、`32cd2431-dfb2-4821-999a-a1e213af6b22`、`0c695c34-26ca-49d6-82f5-5b3eb45133bf`；钛晶石经既有长带自动进入 `775`，黄糖 lab `774` 多次满电工作，金刚石总量由 150 精确降至 100。第 8 项 `9880ef1e-9c07-48de-8703-1712840eda0a` 由普通 replicator 手搓 1 个 sorter；第 9 项 `f17779f1-e15f-422d-afad-7d8acf3d71ca` 把它原生建成实体 `2032`，双端反查为 `storage 762 -> sorter 2032 -> belt 986`，并复用既有 `986 -> … -> 998 -> sorter 1016 -> storage 768` 钛块输入带，network 1、serve ratio 1.0、0 prebuild。该桥当前结构成立但源仓已空，真实携货仍等待下一批输入后复验。第 10 项 `1cf6573b-0c2f-46a3-8f07-df3d9d106687` 正常选择物流相关升级 `3401` 运输船引擎；Journal sequence `49` 在 tick `12956512` durable 记录首次升级选择。只读等待后研究已上传 `1928/36000`，统一研究站 `84` 的结构矩阵点从选前 36000 增到 38080，证明本批黄糖已经通过旧输出带在研究消费窗口补入，而不是只消耗输入后消失。fresh 严格审计 tick `12959635`、revision `31`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；Journal `49/49` durable、无 pending/error，2032 built/0 prebuild（184 sorter），3/3 施工机 idle，三张电网 consumer ratio 均为 1.0。玩家原有 40 黄糖已经由自动研究管理守恒移入 MechaLab 的 144000 点，不能把背包减少误判为丢失。账本落盘后写计数归零；下一写先用自动来源补一小批有机晶体上游原料，以 `2032` 实际携货、`768` 有机晶体增长和下游黄糖/研究继续增长完成永久桥复验，再做普通保存。

- 2026-09-03：完成 exact-primary 恢复后的首组 10 个已接受游戏写动作复核，复验 EXP-007/021/035/036/048/053/061/066/080/093/140/144。第 1 项从自动水仓 `753` 守恒取 50 水，动作已经完成但本地展示误读嵌套结果后退出，未重放；fresh 读回以玩家水 50、源仓并发补水和 revision 前进唯一核销。第 2 项 `6f1521b7-10af-445f-8cee-18805295e082` 把 50 水守恒送入有机晶体输入仓 `761`。第 3–5 项 Move `e821f6a0-dbd1-4ec0-9de3-3cb37f36b769` / `efde9fd1-7b3c-41d3-90e4-8f166f37d2ba` / `bcc42918-cf9f-44ba-9245-fcad6a4266b4` 沿已验证 `133 -> 129 -> 141` 锚点保持 Walk/0；旧 `141 -> 183` 直线已被新设备占据，因此只读投影全厂实体后，第 6 项 `a167c070-b40b-42d9-9394-67a5f5859212` 先走约 9.9 m 局部侧绕点，第 7–8 项 `785ddf28-f09a-4c7f-a565-e8cee0c998f7` / `9badc992-1bbd-4219-9116-bbd53dccb961` 稳定到达热电站 `183` 外缘。第 9 项把已验证风机 `713` 作为唯一跨水终点，单订单稳定落地；动作已完成但报告代码误读 `$a.terminal` 后退出，未重放，fresh 位置距目标 4.37 m、Walk/0、核心约 394.59 MJ、healthy。第 10 项 `93fef591-5029-4d74-a2a3-189824487a9f` 从自动塑料仓 `558` 守恒取得 100 塑料，动作内部精确报告玩家 `0 -> 100`、仓 `3000 -> 2900`；审计时产线已把仓并发补到 2931，不用跨窗净值否定已完成动作。fresh 严格审计 tick `12910631`、revision `18`、planet `104`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；Journal `48/48` durable、无 pending/error，2031 built/0 prebuild，3/3 施工机 idle，三张电网 consumer ratio 均为 1.0。母星 ILS `1657` 满能、双远程需求与双 output selector 保持，唯一运输船仍在工作；PLS `916/918` 继续以无人机搬运钛块，钛输入仓 `768` 已自然增至 980。输入仓 `761` 的 50 水已经原生预取为仓 48 + 化工厂 2，并与 50 精炼油同样闭合；当前只欠把玩家 100 塑料送入该仓。账本落盘后写计数归零，下一写入只执行这次守恒投料。

- 2026-09-03：完成母星硅线里程碑后的正常关闭、exact-primary 恢复和严格审计，并新增 EXP-146、复验 EXP-007/021/037/048/069/083/084/140/144/145。上一审计后的 9 个写动作已经在关机前分别由终态或 fresh 状态唯一核销；随后正常窗口关闭被接受，DSP 进程退出且 runtime descriptor 清零，没有强杀，也没有伪造关闭 action。新进程只消费受保护 planned-restart 票据，动作 `0bfeea50-772a-4190-b0c6-00f1fad8314d` terminal/completed/succeeded，载入 ticket-bound exact primary 并自动重存到 tick `12841190`，不低于关机前保存 `12841158`。fresh 审计为 planet `104`、和平/非沙盒/1×、owned、healthy、0 blocker、planned restart 可用、无 checkpoint；玩家 Walk/0、核心 `400/400 MJ`、手搓队列空，3/3 施工无人机 idle、无 build/repair target。全厂不可变快照为 2031 built/0 prebuild（1669 belt、183 inserter、35 assembler、6 lab、10 miner、33 generator、37 power node、3 station、54 storage、1 tank），三张电网 consumer ratio 均为 1.0；Journal `48/48` durable、无 pending/error，科研队列空。母星 ILS `1657` 保留钛/硅双远程需求、两个正确 output selector、network 1/full service，并由唯一运输船继续执行硅订单；sorter `1981/2022` 在恢复后仍满电实际携硅，熔炉 `842(recipe 59)` 工作，高纯硅仓已增至 2848。黄糖实验室 `774(recipe 27)` 当前有 6 金刚石、0 钛晶石；直接上游 `767(recipe 26)` 保有 6 钛块但缺有机晶体，进一步上游 `760(recipe 25)` 保有 2 精炼油但缺塑料/水。因此恢复门已通过，下一写入从已有自动塑料/水仓向有机晶体线做普通守恒补料，不把缺料停机误判成恢复或物流故障。

- 2026-09-03：完成母星 ILS 硅口到高纯硅熔炉的实物流闭环并把 EXP-145 升级为 validated，同时以新反例复验 EXP-142/144。上一严格审计后的第 1 项 `634140f0-bf88-4069-af6e-0b5f1ba45aa0` 在两层旧带墙远侧建 3 格自由带；第 2 项 `2f292fe3-ca99-4a4c-8b38-69db6f48aed5` 建 sorter `2022` 跨接 `2018 -> 2020`；第 3 项 `def613c0-2e41-43b5-9f38-b4b6e9ea83d7` 侧移 3 格；第 4 项 `4227c307-adf3-4720-9856-88e48bb5d0e9` 直线接近 4 格，等待包装超出本地窗口但 fresh action 复读为 terminal/completed/succeeded，未重放；第 5 项 `343d5094-48ce-4823-9ec7-4c7d2868b75e` 建 sorter `2030` 接入熔炉独立 slot `1`，提交后仅因展示零库存条目缺失报错，fresh 双端反查核销且未重放；第 6 项 `5267bb14-786a-43a8-bd50-3e8451d84ca2` 在零携货窗口把旧石转硅 sorter `844` 过滤为 item `1003`，残余缓冲随后清空；第 7 项 `1e41bbbb-61fb-4cf4-a879-a736549871e6` 将 ILS port `1` 从 raw selector `0 -> 2`，硅槽立即 `100 -> 99` 并最终清空；第 8 项 `d4add305-de63-4e34-b32d-b89d4e3af906` 建电塔 `2031`，使此前 network `0`、堵塞的 sorter `2022` 变为 network `1`/ratio `1.0` 并立即携硅；第 9 项正常保存 `0999d164-5a6e-4131-a401-ff9e5ef63767` 固化 tick `12841158`、revision `205`、healthy、无 checkpoint。实物流依次经过 sorter `1981/2022/2030`，末端明确携带硅石；熔炉 `842(recipe 59)` 多次工作，成品仓 `2806 -> 2820`。当前写计数为 9；下一写只做正常关闭，恢复后必须先完成第十写严格审计，才允许继续补有机晶体和黄糖持续性。
- 2026-09-03：完成硅线穿越旧铁生产簇并接近高纯硅熔炉后的严格十写审计，EXP-133 升级为 validated，复验 EXP-007/008/018/028/070/123/126/132/133/134/143/145。第 1–3 项 `b6a1ed59-c16e-45f2-a497-a5a62eaf95d7`、`ab39a8a6-46ef-4b2f-98fb-fd2715878d40`、`c9c15662-a29a-40ad-aa93-44c30361b6df` 分别续建 23/18/6 格；第 4 项 6 格外绕已 commit，但展示表达式随后报错而未保留 action ID，未重放，库存 `17 -> 11`、施工机清零后的两次 191 实体稳定遍历及后续唯一续线共同核销；第 5 项 handcraft `237f7c6b-e4d0-40e2-877f-56ed74f289b3` 递归消耗 60 铁块并精确产出 60 条带，forge 清空；第 6 项 `0b70bdaf-46c8-4bc5-8680-2eedb5abd660` 建 3 格接近段到 `1976`；第 7–8 项 `c5fed871-cb1d-4051-a9f4-6c691c58111b` / `30ffb884-c60a-4836-85a9-de4eda0de178` 在旧铁带 `1521` 另一侧建成独立两格 `1980 -> 1979`，再以满电 sorter `1981` 显式连接 `1976 -> 1980`，旧铁线连接未改变；第 9–10 项 `7af778cb-175b-4d78-8ffa-f1b138b01386` / `7c0e8e0f-6c7a-48c8-a3e4-0f0f355f599c` 又续建 29/8 格到 `2018`。fresh 审计 tick `12761666+`、revision `188`、planet `104`、和平/非沙盒/1×、Walk/0、核心约 `352.93/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；玩家余 29 带/2 sorter/37 铁块，forge 空、3/3 施工机 idle。全厂 2018 built/0 prebuild（1659 belt、181 inserter），三张电网 consumer ratio 均为 1.0，journal `48/48` durable、无 pending/error；9 个已知 action ID 全部仍为 terminal/completed/succeeded、无 stall/recovery。硅路径从 `1793` 跨 sorter `1981` 到 `2018` 共 234 实体、无环，末端距熔炉 `842` 约 11.89 m；ILS 硅槽仍为 100、port `1` 未选择货槽。钛块输入仓同期自然增至 713，高纯硅仓由旧石矿入口增至 2761。账本落盘后写计数归零；下一写入只继续最终接近段，接炉前先停旧石矿入口并排空残余以建立外星硅的独立差量证据。
- 2026-09-03：完成母星钛持续入链和硅线前四段后的严格十写审计，新增 EXP-145，并复验 EXP-007/008/009/018/021/028/070/095/132/133/134/140/143/144。第 1 项 `b89941f0-f53c-4bd1-ae92-2b6986815ade` 把母星 ILS port `0` 选择为钛槽；第 2 项 Move `495356f6-4f17-4994-83b2-f19cc54f31a9` 沿已验证路径返回；第 3 项 `34c3130a-6c6b-4ea4-913d-ea2c3e2fcd9` 从仓 `843` 守恒取出 400 高纯硅块以释放容量，晚审计时 action 已淘汰但即时终态和 fresh 玩家 400 件共同核销；第 4 项 `95ac5d96-ae2b-4145-b3c4-8a830444ed45` 向同仓守恒投入 100 铜块供微晶元件线消费；第 5–6 项 Move `ac5b53a9-fc07-4fd8-8bbb-6b9affaf82f7` / `00d33a3c-6c85-4de6-a99d-c04f34cb651f` 到稳定施工点；第 7 项 9 格 ILS 硅口短桩的等待包装超过本地窗口而未保留 action ID，未重放，fresh 证明 `1657:port1 -> 1793 -> … -> 1785`、库存 `202 -> 193`；第 8–10 项 `9024d38b-63e4-4cab-9f28-80caa0e0d17f`、`d9b67042-9d3d-46f0-b6f2-9993c06b1035`、`33a07c22-f0ac-4573-b1b4-260616bb59ba` 分别安全续建 59/42/28 格，最终形成 `1793 -> … -> 1922` 的 138 实体无环单链。fresh 审计 tick `12659867+`、revision `168`、planet `104`、和平/非沙盒/1×、Walk/0、核心约 `347.23/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；玩家余 64 带/3 sorter，3/3 施工机 idle，工厂 1922 built/0 prebuild，三张电网 consumer ratio 均为 1.0，journal `48/48` durable、无 pending/error。母星 ILS 已满能，钛槽出空并出现新一轮 `remoteOrder=100`、`1 working vessel`；其 port `0` 已选钛槽且完整物理链通到本地 PLS，钛块仓自然增至 577。硅槽仍有 100，port `1` 尚未选择货槽；高纯硅仓现有 2676，旧石矿入口仍工作。账本落盘后写计数归零；下一写入只从自由末端 `1922` 继续全路径占位检查后的安全段，不提前启用硅口。
- 2026-09-03：完成远端首批矿接入母星钛冶炼前的严格十写审计，并复验 EXP-007/008/009/018/021/028/070/095/140/143/144。第 1–3 项为远端普通保存 `64c1ecfc-f18e-4fa4-8339-9a066bc81036`、成功返航 `ee51b297-e749-4da4-adf3-0db58cd86f25` 与母星普通保存 `f374d28a-bc56-4a5c-9eff-e547fd8945ed`；第 4 项 Move `c6781a00-f94a-4634-8cb0-0632966931c6` 回到已验证陆地锚点；第 5 项 `5a955548-75d6-40cc-a303-7489de4d79b6` 从自动铁仓守恒取得 400 铁块；第 6–7 项 `9940fc4a-f3bf-43f6-a69f-d5ede8dac970`、`c29a0a63-efad-42e2-a55d-367230e48732` 由普通 replicator 递归手搓 300 条带和 3 个 sorter；第 8 项 Move `b00f3a76-1ac1-4b03-8e18-98ab7cdcf6f4` 到施工中点；第 9 项 124 格长带已终态完成但调用方随后访问不存在的 `builtObjectIds` 报错，未重放，fresh 唯一链 `1657:port0 -> 1783 -> … -> 1771`、建材 `326 -> 202`、`prebuild=0` 完成核销；第 10 项 `541ee6f7-784c-4a5e-8440-e0c96e77601d` 建成 `1771 -> 1784 -> storage 259`，并保留原输出 sorter `532`。fresh 审计 tick `12486349`、revision `150`、planet `104`、和平/非沙盒/1×、Walk/0、核心约 `293.23/400 MJ`、healthy、0 blocker、planned restart 可用、无 checkpoint；1784 built/0 prebuild，三张电网全部满供电，journal `48/48` durable。ILS `1657` 仍有钛石/硅石各 `100/100`，新 port 当前 raw selector 仍为 0，故尚未把“连通”冒充“出料”。本条落盘后写计数归零；下一写入只把 port `0` 绑定钛矿槽 `0`，再用 ILS/带/仓/熔炉/PLS 的连续差量验收。
- 2026-09-03：EXP-144 升级为 validated。远端起飞前普通保存动作 `64c1ecfc-f18e-4fa4-8339-9a066bc81036` 固化 tick `12371702`；返航 `ee51b297-e749-4da4-adf3-0db58cd86f25` 从 planet `102` 原生 Sail 至 planet `104`，在 tick `12382194` 连续 600 tick 保持 Walk/0，未 stalled/recovery，独立 checkpoint capability 随成功消失。母星 ILS `1657` 随后 fresh 读到钛石/硅石各 `100/100`、双远程需求订单均为 0、`1 idle / 0 working`，与源端两次订单/取货共同闭合跨星运输。动作 `f374d28a-bc56-4a5c-9eff-e547fd8945ed` 再把母星验收点保存到 tick `12384194`；fresh revision `137`、healthy、planned restart 可用、无 checkpoint、journal `48/48` durable。当前新窗口成功写计数为 3；下一产品工作是把 ILS 两种原矿通过正常 output port、传送带和 sorter 接入现有黄糖上游，不把塔内收货冒充最终持续产线。
- 2026-09-03：完成远端首轮跨星取货与返航备能前的严格十写审计，新增 EXP-144，并把 EXP-143 升级为 validated。只读等待先记录钛槽 `49 -> 100`、`remoteOrder 0 -> -200`，再记录取货后的 `100 -> 23` 与订单归零；钛满槽期间硅仍由 `71 -> 94 -> 99 -> 100`，随后同样出现 `-200` 并在取货后降到 16，证明混合末段跨过首次满槽周期且两艘来船先后完成源端装货。第 1–6 项写动作沿预先检查、最近非带设备净空至少 20.8 m 的 278.1 m 球面路线到煤脉 `380`，动作 `6acea0c5-0523-4599-9479-51adca712e58`、`5f827769-90e9-44ed-a777-c7f95bc1ee12`、`1678d0b8-c694-4b85-89bc-fbbef14ba35c`、`a58137bb-34ef-44f2-869f-b1d0da394260`、`0f809b04-252f-4937-8089-d782fcb99145`、`6be35890-8a5e-4aeb-b118-f5d82ac7df87` 均以 Walk/0、约 2 m 误差终止；第 7 项原生手采 200 煤，结果展示因读取不存在字段失败但 fresh 双边复读确认节点 `30859 -> 30659`、玩家 `0 -> 200`，未重放；第 8–10 项 refuel `ed836fe7-ea9d-4088-8a18-670f71df5da5`、`29ab23a5-287b-410c-ba6a-da97807556a7`、`a4fa26cf-ca1b-4b67-a4b7-ca235ce758bc` 依次守恒转入 `100 + 20 + 80` 煤。两次请求 100/99 的 prepare 因原生当时只会补 3/20 而无副作用拒绝，不计写。fresh 审计 tick `12367446+`、revision `132`、planet `102`、和平/非沙盒/1×、Walk/0、核心 `400/400 MJ`、燃料仓 180 煤、healthy、0 blocker、planned restart 可用、无 checkpoint；175 built/0 prebuild，单网 17 节点/10 风机/8 消费者，journal `48/48` durable、无 pending/error。远端 ILS 约 `8.932 GJ`、`1 idle / 0 working`，取货后已再补到钛/硅 `100/71`。本条落盘后写计数归零；下一写入才允许普通保存返航起点，随后由飞行动作另建独立 checkpoint。
- 2026-09-03：远端钛/硅自动供料跨过里程碑门并主动提前完成 8 写审计，新增 EXP-143，复验 EXP-007/018/021/028/070/095/117/123/124/126/140/142。第 1–4 项原生施工风机 `126…129`，每台均反查 network `1`/`5500 J/tick`，使总容量达 `55000 J/tick`、ratio 约 `0.1254`；钛 sorter `123` 由静止转为往返，ILS 钛石开始 `0 -> 1 -> 3`。直连硅路的一次只读 prepare 发现 planned point 与带 `102` 重合 `0.00393 m`，未 commit。第 5 项改为 44 格无重叠独立带 `173 -> … -> 148`；第 6 项 sorter `174` 连接 `storage 25 -> belt 173`；首次准备末端 sorter 后因无线充电使 player hash 变化，commit 被 `STALE_STATE` 无副作用拒绝，不计写且未重放；第 7 项用新鲜状态紧接 prepare/commit 建成 sorter `175`、反查 `148 -> 102`。两只新 sorter 均在 network `1` 且实际携硅，ILS 硅石 `2 -> 9 -> 14 -> 19`，钛石同窗口 `40 -> 49`。第 8 项正常保存动作 `89f7689d-ce99-4719-be14-9bacbda85d7c` 固化 tick `12278450`、revision `115`。fresh 审计 tick `12280630+`、planet `102`、Walk/0、核心 `400/400 MJ`、燃料仓空、healthy、0 blocker、planned restart 可用、无 checkpoint；175 built/0 prebuild（147 belt、5 sorter、2 miner、10 generator、7 node、1 station、3 storage），单网 ratio `0.15708`。ILS 约 `4.412 GJ`、`1 idle / 0 working`，钛/硅 `49/19`；journal `48/48` durable、无 pending/error。本次提前审计后写计数归零；下一阶段只读等首次满槽/派船，真实跨星运输验收前不报 v0.3 完成。
- 2026-09-03：完成远端增发第四组严格十写审计，并复验 EXP-007/018/021/028/061/095/140/142。第 1 项从铁脉 `19` 原生手采 60 铁矿，节点与背包反向差量精确；第 2 项由 DSP replicator 递归手搓 5 台风机，第 3 项手搓 36 条带，手搓队列最终归零。第 4–8 项把返回前哨的约 146.1 m 球面路程拆为 5 段不超过 30 m 的原生 MoveTo，全部在 2.1 m 内以 Walk/0 完成；第 9–10 项原生施工风机 `124/125`，施工机完工和实体反查均成立。fresh 审计为 tick `12222086+`、revision `100`、planet `102`、Walk/0、核心约 `354.02/400 MJ`、燃料仓空、healthy、0 blocker、无 checkpoint、planned restart 可用；journal `48/48` durable、无 pending/error。工厂为 125 built/0 prebuild（103 belt、3 sorter、2 miner、6 generator、7 node、1 station、3 storage）；新风机均在 network `1`，容量为 `33000 J/tick`。钛/硅仓仍各 3000，ILS 为钛/硅 `0/0`、站能量约 `1.604 GJ`、1 idle/0 working 船；sorter `123` 端点仍为 `15 -> 122`、ratio `0.07152`、Picking/stack 0，所以仍不报钛线投产。本条落盘后写计数归零；下一写入才继续余下 4 台风机，再以仓/站数量而不是理论 ratio 验收。
- 2026-09-03：完成远端 ILS 初始配置与钛带施工的严格十写审计，并复验 EXP-018/021/095/097/105/140/142。第 1–2 项把站 `44` 的槽 `0/1` 分别配为钛石/硅石、各 100、远程供应；两槽立即读到母星各 100 的真实远程需求。第 3 项把玩家唯一运输船守恒装入站内，形成 `1 idle / 0 working`。第 4 项由 76 条普通带构成钛仓附近到 ILS port 0 的连续定向路径，背包带 `110 -> 34`；第 5 项施工 sorter `123`，精确连接 `storage 15 -> belt 122`。第 6–10 项以 5 段不超过 30 m 的正常 MoveTo 到最近铁脉 `19`，核心仍约 390 MJ。fresh 审计为 tick `12189151+`、revision `80`、planet `102`、Walk/0、healthy、0 blocker、无 checkpoint、planned restart 可用；journal `48/48` durable，123 built/0 prebuild（103 belt、3 sorter、2 miner、4 generator、7 node、1 station、3 storage），10/10 action 均成功且无 reconciliation/stall/recovery。负向证据同样明确：30 MW 站充电把单网压到约 `0.04464`，sorter `123` 虽为 network `1` 且端点正确，却持续处于 Picking/stack 0；一分钟内钛仓保持 3000、站钛保持 0，所以当前不能宣称钛线已投产。站能量自然增至约 `838.48 MJ`。本条落盘后写计数归零；下一写入先采集足额铁矿，手搓 5 台追加风机与约 36 条补充带，再把总风机从 4 提到 10，使理论供电比超过 0.10 后复验。
- 2026-09-03：完成远端供电边界的严格十写审计，新增 EXP-142，并复验 EXP-018/021/069/105/140。第 1–4 项走完前哨最后约 112.2 m，均在 2.52 m 内正常落点；第 5 项施工无线塔 `43`，把原两张风电网合并；第 6 项走入无线范围并以 15 秒 `+19.529 MJ` 证明自动充电；第 7 项施工远端 ILS `44`；第 8 项电塔 `45` 仅接网而未覆盖站体；第 9 项在站约 10 m 处补建电塔 `46`，ILS 才从 network `0` 接入 network `1`；第 10 项把站最大充电功率从 60 MW 降到最低合法 30 MW。fresh 审计为 tick `12152975+`、revision `61`、planet `102`、Walk/0、核心满载、燃料仓空、healthy、0 blocker、无 checkpoint、planned restart 可用；玩家余 110 带、4 sorter、1 普通电塔、1 风机和 1 运输船，forge 空、3/3 施工机 idle。journal `48/48` durable；工厂为 46 个 built entity、0 prebuild（新增 3 power node、1 station），单一 network `1` 有 11 节点/4 风机/5 消费者。ILS 为 network `1`、约 `82.99 MJ / 12 GJ`、30 MW、5 空槽、0 船；充电时 ratio 约 `0.04377`。10/10 action 均 terminal/completed/succeeded、无 reconciliation/stall/recovery。本条落盘后写计数归零；下一写入才配置钛硅远程供应并装船。
- 2026-09-03：完成下一组严格十写审计。第 1 项通过正常 replicator 递归手搓 1 座无线输电塔：18 石矿、12 磁铁、6 铜块和 1 座电力感应塔精确消耗，所有玻璃/棱镜/磁线圈/电浆激发器中间产物均归零，手搓队列清空；第 2–10 项把通往钛硅前哨的前 360.0 m 拆成 9 段不超过 40 m 的原生 MoveTo，全部在 2.99 m 内完成，核心约 `250.1 -> 228.0 MJ`。fresh 审计为 tick `12129577+`、revision `41`、planet `102`、Walk/0、核心约 `229.93/400 MJ`、燃料仓空、healthy、0 blocker、无 checkpoint、planned restart 可用；玩家仍有 1 无线塔、1 ILS 和 1 运输船，forge 空、3/3 施工机 idle、0 pending target。journal `48/48` durable、无 pending/error；远端 42 个 built entity、0 prebuild 和两张 consumer ratio `1.0` 电网均未漂移；10/10 action 可重取且 terminal/completed/succeeded。本条落盘后写计数归零；距选定双网覆盖的无线塔候选点还约 125 m。
- 2026-09-03：完成 planet `102` 成功落地后的严格十写审计，并复验 EXP-007/018/021/069/105/141。第 1 项是已由航行期 checkpoint、目标星 Walk/0 和 checkpoint 撤销三段 fresh 状态核销的成功飞行；第 2 项正常保存落地 tick `12071759`；第 3–9 项把约 191.9 m 球面路程拆成 7 段不超过 30 m 的原生 MoveTo，全部在 3.00 m 内完成且核心约 `235.5 -> 224.2 MJ`；第 10 项从石脉 `358` 原生采集 18 石矿，节点减少量与背包增量均精确为 18。审计为 tick `12097849+`、revision `21`、planet `102`、Walk/0、核心约 `229.09/400 MJ`、燃料仓空、write health `healthy`、0 blocker、无 flight checkpoint、planned restart 可用；研究队列空且已解锁 39 项。journal `48/48` durable、无 pending/error；远端仍是 42 个 built entity、0 prebuild（27 belt、2 sorter、2 miner、4 generator、4 power node、3 storage），两张电网 consumer ratio 均为 `1.0`，施工机 3/3 idle 且无 pending target。可重取的保存、7 个移动和采集共 9 个 action 均 terminal/completed/succeeded；飞行动作 ID 因 commit 后客户端展示错误未取得，但未重放且由唯一世界终态核销。本条落盘后写计数归零；下一写入才允许递归手搓无线输电塔。
- 2026-09-03：成功抵达 planet `102` 后，正常保存动作 `3e413eab-6745-41d9-a4eb-a52f487bce37` 将目标星落地状态固化到 tick `12071759`、revision `5`。fresh 复读仍为 Walk/0、目标星、write health `healthy`，flight checkpoint 已按成功生命周期撤销，精确主档 planned-restart 能力已重新签发；远端施工包及已有钛硅前哨均保持不变。该保存是当前严格窗口第 2 个已接受写动作（第 1 个为成功飞行）；后续先取得本地石料并递归手搓无线输电塔，十写时完整审计，再靠近已有满供电前哨接电、补能和施工远端 ILS。
- 2026-09-03：修复版首个飞行动作已被 DSP 接受并创建新的独立 checkpoint；调用脚本只在 commit 返回后读取不存在的 `estimatedTicks` 字段报错，严格按 EXP-007 没有重放。fresh session 先证明航行中 `localPlanet=null`、checkpoint 绑定 `104 -> 102`、healthy，随后在 tick `12066184+` 进入 planet `102`、Walk/0、checkpoint 不再可用；再等待后 tick `12069036+` 的位置/速度保持稳定。玩家全部远端施工包仍在，氢由 36 正常消耗到 11，核心从落地约 33.4 MJ 经普通燃料恢复到约 113.3 MJ。目标星资源只读复核确认至少 20 个钛节点（合计约 1.414 M）和当前页 100 个硅节点（合计约 6.681 M）；已有 1 台矿机覆盖钛节点 `315/322`、1 台矿机覆盖硅节点 `245/249/252/256`。远端工厂为 42 个 built entity、0 prebuild：27 belt、2 inserter、2 miner、4 generator、4 power node、3 storage，两张电网 consumer ratio 均为 1.0。当前新窗口写计数为 1；下一写入先普通保存成功着陆，再等待/补充正常能量后施工远端 ILS。
- 2026-09-03：计划重启恢复动作 `1fcd6f42-6827-4fe1-94f7-adf180acd572` 只消费精确主档能力，成功载入 tick `12052876 >=` 保存门槛的同一 planet `104`；和平、非沙盒、1×、owned、healthy 均成立，旧失败 checkpoint 没有重新暴露。该恢复作为上一窗口第 10 个已接受写动作，已在继续前完成完整审计：fresh tick `12054612+`、revision `1`、主档又由恢复流程自动保存到 `12052890` 并重新签发 planned restart；玩家 Walk/0、核心满载、36 氢、施工包 110 带/4 sorter/4 电塔/1 风机/1 ILS/1 船，forge 空、施工机全 idle；journal `48/48` durable、无 pending/error，研究队列空。全厂不可变快照含 1659 个 built entity、0 prebuild（1302 belt、179 inserter、35 assembler、6 lab、10 miner、33 generator、36 node、3 station、54 storage、1 tank）。母星 ILS 双需求/1 idle 船仍在，能量约 `9.137 GJ`；network 1 在充能期瞬时 ratio 约 `0.5131`，其他两网为 1.0。恢复 action terminal/completed/succeeded、无 reconciliation，账本复验后写计数归零；下一写入才允许由修复版创建新的独立 checkpoint 并实飞。
- 2026-09-03：在两次失败航行均恢复到同一飞前状态后，第 9 个已接受写动作 `08ca5e1a-b1c2-4368-9b80-7b18b196a2c8` 通过正常 save API 将安全起点覆盖保存到主档 tick `12052859`、revision `2`，并签发精确主档的计划重启能力。fresh 审计到 tick `12056395+` 仍为 planet `104`、Walk/0、核心 `400/400 MJ`、36 氢、write health healthy、journal `48/48` durable 且无 pending/error；玩家施工包为 110 带/4 sorter/4 电塔/1 风机/1 ILS/1 运输船，forge 空、3 架施工机全 idle。母星 ILS `1657` 仍是 network `1`、钛/硅各 100 远程需求、1 艘 idle 船，能量约 `9.277 GJ / 12 GJ`；充能期 network 1 瞬时 consumer ratio 约 `0.7264`。旧失败 checkpoint 因生命周期仍是 recovery-required 而未被这次主档保存退役，但当前 session revision 已不再满足复用条件；部署恢复时只消费精确主档计划重启能力，不调用旧 checkpoint，下一次 flight 会先创建/绑定新的检查点。本窗口当前计数 9；计划重启恢复动作将作为第 10 项，恢复后必须先做完整审计和账本复核再飞。
- 2026-09-03：远端施工包已用正常 replicator 完成：玩家持有 110 条带、4 个 sorter、4 座电塔、1 座风机、1 座 ILS 和 1 艘运输船，核心满载且燃料仓保有 36 氢。随后同一飞前检查点的两次 `104 -> 102` 原生飞行都被中间气态巨行星捕获并进入 `recovery_required`，每次均通过绑定能力精确恢复到该检查点，没有盲重放、覆盖主档或继续污染失败时间线。新增 EXP-141，将中间天体原生 1000 m 捕获层、确定性绕行航点和 200 m/s 绕行限速先落为实现/测试；当前已恢复在 planet `104` 的安全起点，下一游戏写入前先完成新 DLL 构建、正常保存与部署边界。
- 2026-09-03：完成首座 ILS 保存后的下一组严格 10 写审计，并复验 EXP-007/021/047/069/105/111/140。第 1 项是母星 ILS 部署的正常保存；第 2 项把附近混合仓 `163` 的 40 氢精确取入玩家，第 3–4 项遵守原生单栈补给限制，各把 20 氢守恒移入机甲燃料仓。审计时燃料仓为 39 氢、reactor 约 `7.48 MJ`，少 1 氢对应正常燃烧中的能量，不是库存丢失。第 5 项沿此前两次验证的 24 m 侧偏短弧稳定移动到自动铁仓交互范围；第 6–8 项分别从仓 `1511/723/26` 守恒取得 72 铁块、50 磁铁、10 电路板；第 9–10 项把仓 `900/893` 最后一座 ILS 和最后一艘运输船守恒取入玩家。fresh 审计为 tick `11989033+`、revision `604`、planet `104`、Walk/0、核心 `400/400 MJ`、write health healthy、研究队列空、journal `48/48` durable 且无 pending/error、无 flight checkpoint；10/10 action 均 terminal/completed/succeeded、无 reconciliation/stall/recovery。玩家空手且 forge queue 为空，携 1 ILS、1 运输船、72 铁块、63 铁矿、59 磁铁、129 铜块、11 电路板和原有 5 带/1 sorter；母星 ILS 已充至约 `2.848 GJ`，仍为双需求和 `1 idle / 0 working`。本条落盘后写计数归零；下一写入先用普通 replicator 把原料转成远端带/分拣器/配电/发电包，只有物资与玩家状态再次复读无误后才提交飞行动作并由它创建独立 checkpoint。
- 2026-09-03：首座母星 ILS 的十写审计落盘后，下一窗口第 1 个写动作 `c1bcf77b-c56a-40b7-ae90-c9ee072db81b` 通过 DSP 正常保存 API 固化 tick `11974991`。fresh 复读 revision `594`、write health healthy、无 flight checkpoint、journal `48/48` durable 且无 pending/error；ILS `1657` 仍为 network `1`、30 MW、钛/硅各 100 远程需求、`1 idle / 0 working`，能量已自然增至约 `1.463 GJ`。EXP-069/083/140 与保存后状态一致；该保存计入新一组第 1 写，下一步才允许取出另一座站/船并在实际起飞前创建独立 checkpoint。
- 2026-09-03：完成首座母星 ILS 投运的严格第 10 写审计，新增 EXP-140，并复验 EXP-018/021/095/097/099/101/105/109/138/139。第 1–2 项把仓 `900` 的一座 ILS 守恒取入玩家，并在全厂非带实体净空排序后的约 32 m 空区正常施工为 `1657`；第 3 项由 DSP replicator 递归消耗随身铁矿/磁铁/铜块，手搓 2 座电力感应塔；第 4–5 项沿旧电塔 `711` 到站点的球面弧施工 `1658/1659`，使 ILS 从 network `0` 接入 network `1`。第 6 项把该 ILS 的原生默认 60 MW 充电上限降到本 prefab 最低合法 30 MW；第 7–8 项把槽 `0/1` 配成钛石/硅石、各 `100`、远程需求；第 9–10 项把仓 `893` 的一艘运输船经玩家完整装入 ILS fleet。fresh 审计为 tick `11969310+`、revision `593`、planet `104`、Walk/0、核心 `400/400 MJ`、write health healthy、无 checkpoint、研究队列空，journal `48/48` durable 且无 pending/error；10/10 action 均 terminal/completed/succeeded、无 reconciliation/stall/recovery。站点能量约 `828.64 MJ`、`1 idle / 0 working`，双需求各缺 100；仓 `900/893` 各保留另一座 ILS/另一艘船。network `1` 在 30 MW 充能期 consumer ratio 约 `0.2037`，故不能把当前电网称为满供电。本条落盘后写计数归零，下一写入必须先普通保存该母星部署边界。
- 2026-09-03：完成双 ILS 生产与严格第 10 写审计，新增 EXP-139，并复验 EXP-062/074/080/107/138。第 1–3 项只在设备/分拣器全空载且 recipe `95` 已解锁时，将制造台 `898` 从 recipe `93` 切到 `95`，并把 sorter `902/903` 从钢/钛块改为 PLS/钛合金；粒子容器 sorter `905` 保持不变，旧处理器 sorter `904` 因无处理器输入而不参与。第 4–9 项分别把 2 座 PLS、80 钛合金和 40 粒子容器从仓 `900/885` 经玩家完整送入仓 `899`；六次 action 内部均为精确相反差量。设备首次读回为 PLS/合金/粒子容器 `2/6/6`，最终输入仓只剩原有 651 硅石，制造台三项输入和输出缓存全归零，仓 `900` 得到精确 2 座 ILS。第 10 项保存 `1766766a-c0c0-44d2-b9bc-8d0f87bcda48` 固化 tick `11926992`；journal sequence `48` 在 tick `11921722` 持久化首次自动 ILS。fresh 审计为 tick `11928178+`、revision `576`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint、研究队列空，三张电网 consumer ratio 均为 `1.0`，journal `48/48` durable、无 pending/error；10/10 action 均 terminal/completed/succeeded 且无 reconciliation。运输船仓 `893` 仍有 2 艘；无重放、Drift、quarantine、outcome unknown、串料或未解释正增量。本条落盘后写计数归零，下一写入进入 ILS 正常施工与投运门槛。
- 2026-09-03：在上一审计后第 7 个已接受写动作完成双 PLS/双星际运输船产品边界，并主动提前完成全审计，复验 EXP-062/080/105/107。前两项把处理器专仓 `854` 的 80 件经玩家精确送入 PLS 输入仓 `899`；sorter 渐进预取时制造台按 EXP-107 合法等待完整批次，最终 80 钢、80 钛、80 处理器和 40 粒子容器全部归零，仓 `900` 在保留 80 钛合金的同时新增 2 座 PLS。后四项又把专仓余下 20 处理器与 4 加力推进器经玩家送入运输船输入仓 `892`，配合制造台已有 20 钛合金自动产出仓 `893` 的 2 艘 item `5002`；journal sequence `47` 在 tick `11890591`（实际 `2026-09-03T08:47:27.6896746+08:00`、本局 `002d 07:02:56`）持久化首艘自动运输船。保存动作 `93dfa668-ec95-441f-8ca1-e05f642b1288` 固化 tick `11896243`。fresh 审计为 tick `11900188+`、revision `563`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint、研究队列空，三张电网 consumer ratio 均为 `1.0`，journal `47/47` durable、无 pending/error；7/7 action 均 terminal/completed/succeeded。两次只读轮询脚本的结果汇总表达式报错发生在所有读请求之后，没有创建游戏 action，也没有重放六次 transfer 或保存。外部仍有 80 钛合金和 40 粒子容器，恰与两座 PLS 合成两座 ILS；本条落盘后写计数提前归零，下一写入先把制造台 `898` 从空载 recipe `93` 安全切换为 recipe `95`。
- 2026-09-03：完成下一组 10 个已接受游戏写动作复核，并复验 EXP-062/080/102/103/137。前两项把活跃仓 `26` 的 200 个自动电路板经玩家精确送入处理器输入仓 `849`；动作自身闭合源仓 `243 -> 43`、玩家 `1 -> 201 -> 1`，完整审计时源仓已自然补回 268，符合 EXP-080。制造台 `853(recipe 51)` 满电自动消耗 200 电路板和 200 微晶元件，输出仓 `854` 达到精确 100 个处理器，另余合计 20 微晶元件。随后 40 粒子容器经玩家送入 PLS 输入仓 `899` 并已由制造台 `898` 预取；运输船制造台 `891` 在配方 `96` 解锁后单次启用，sorter `895/897` 分别锁定钛合金/加力推进器，20 钛合金经玩家送入仓 `892` 并已预取。普通保存动作 `199f3d02-01a1-4fd4-937b-7411ef06cb94` 固化 tick `11863820`。fresh 审计为 tick `11865044`、revision `556`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 全为 `1.0`，journal `46/46` durable、无 pending/error；10/10 action 均 terminal/completed/succeeded。100 处理器现按 80 座体/20 船体精确分配，PLS 台另已缓存 80 钢/80 钛/40 粒子容器，船体台已缓存 20 钛合金；无重放、Drift、quarantine、outcome unknown、串料或未解释正增量。本条落盘后写计数归零，下一写入先向 PLS 台交付 80 处理器。

- 2026-09-03：完成加力推进器产线并在 8 个写动作后主动审计，复验 EXP-062/080/102/103/136。动作 `d779eebf-31d4-463a-ad35-ff0426b6c39c` 将空载满电制造台 `876` 从基础推进器改为 recipe `21`；动作 `7742d3de-b8ab-4818-b691-e5b6bf70f9bf` / `c9b16da4-62dc-4d94-8c55-fd8e759e8bb8` 只在零携货窗口将输入 sorter `880/881` 改为钛合金/电磁涡轮。四次 transfer 把专仓 `900` 的钛合金 `120 -> 100`、涡轮仓 `827` 的电磁涡轮 `23 -> 3`，各 20 件完整送入输入仓 `877`；自动配方随后把仓/机内两输入都耗尽，输出仓 `878` 在既有 40 个基础推进器外新增 4 个加力推进器。journal sequence `46` 已 durable 记录 tick `11827563` 的首次产线产出；正常保存 `189abb87-27e2-4ba8-82a3-cff55ad5eb32` 固化 tick `11830296`。fresh 审计为 tick `11831252+`、revision `543`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint，三网 consumer ratio 全为 `1.0`，journal `46/46` durable、无 pending/error，8/8 action 均 terminal/completed/succeeded。同期自动电路板已增至 186，粒子容器 80、剩余钛合金 100、PLS 台 80 钢/80 钛和微晶元件 216 均符合后续双 ILS/双船预算；本条落盘后写计数提前归零，下一写入在电路板达到 200 后送入既有处理器线。

- 2026-09-03：在 9 个已接受写动作后主动完成全审计，并新增 EXP-137、复验 EXP-062/080/102/103/136。动作 `98057aa3-5070-4612-965f-0cb9122b7ca1` / `49da45e7-688a-4f65-b4cd-b6301c86cec7` 首次把 300 铜从满载回收仓 `26` 守恒移到静态仓 `136`，但仓仍保持 30 个非空格；动作 `59b370f7-dd48-4d00-91e5-55cf4a8058af` / `6ba6713f-4777-4845-b1b3-62a31793d31c` 再移 600 后才降到 26 格，证明聚合数量下降不等于释放格子。动作 `152e668e-377b-4ce2-bb50-bb17169192b2` 将已满仓 2373 钢材的支线 sorter `793` 临时停在不匹配的硅石过滤，避免继续抢铁；科技 `1605` 随后在 tick `11808407` 精确达到 `216000/216000`、`unlocked=true`、退出队列。动作 `58e89d75-09dd-44fd-b227-a0834f0a03b8` 与 `cff5f844-a3d8-4c40-8d4c-dd2932b77b07` 只在零携货窗口将蓝糖的直供/回取 sorter `573/868` 临时过滤到铜/磁线圈，电路板因而自动回收到仓 `26` 并从 0 增至 47。科技完成还使 MechaLab 原生退回 77 蓝矩阵和 40 黄矩阵，fresh 缓冲为空。普通保存 `2126c142-a83c-46a2-81c1-e3d08f6c7fef` 持久化 tick `11811914`；审计为 tick `11814520+`、revision `532`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint，三网 consumer ratio 全为 `1.0`，journal `45/45` durable、无 pending/error，9/9 动作均 terminal/completed/succeeded。粒子容器 80、钛合金 120、涡轮 23、PLS 台 80 钢/80 钛及微晶元件 216 均保持；本条落盘后写计数提前归零，下一写入继续给自动电路板链补足铁并把 200 电路板送入既有处理器线。

- 2026-09-03：粒子容器专仓在上一十写审计后继续从 77 自然增长到精确 80；输入仓归零，制造台 `883(recipe 99)` 与三只过滤 sorter 保持 network 1/full service，配方投入和输出数量闭合。普通保存动作 `08874056-69ac-415f-8541-e0209cebaa4f` 确认 tick `11775970`、revision `521`、write health healthy、无 flight checkpoint，journal 仍为 `45/45` durable。涡轮仓保留 23，足够覆盖四个加力推进器所需 20；钛合金 120、PLS 台 80 钛/80 钢和微晶元件 216 不变。该保存是审计后新窗口的第 1 个写动作；下一写入继续处理器自动供料，累计到 10 时重新全审计。

- 2026-09-03：完成正常保存 tick `11721771` 后的下一组 10 个已接受游戏写动作复核，并复验 EXP-007/037/062/073/080/089/093/129/134/135/136。前 3 项将涡轮专仓 `827` 的 51 个电磁涡轮经玩家精确送入粒子容器输入仓 `884`，并向四过滤共享仓 `723` 投入 58 铜块。第 4 项用全厂非带实体净空排序后的 24 m、`-20°` 侧偏短弧进入自动铁仓 `1511` 的 77.69 m 交互边缘；commit 后展示误读 `$action.terminal`，没有重放，fresh 位置、Walk/0、单调 revision 与随后 transfer 唯一核销该动作。第 5–7 项将 `1511` 的 174 自动铁块经玩家守恒送入 `723`，反向 Move 回到原稳定点；第 8 项再补 18 铜。齿轮、磁线圈、电机与涡轮链恢复后，第 9–10 项把 28 个新增涡轮经玩家送入 `884`，源仓 action 自身为 `49 -> 21`、玩家 `0 -> 28 -> 0`、目标 action 自身为 `0 -> 28`。fresh 审计为 tick `11769696+`、revision `520`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 全为 `1.0`，journal `45/45` durable、无 pending/error；9 个保留 action ID 均 terminal/completed/succeeded，第 4 项由 fresh 业务终态核销。粒子容器专仓已由本批开始前的 41 增至 77，制造台 `883(recipe 99)` 满电工作且仍有可解释涡轮/铜/石墨烯输入；涡轮专仓仍有 23，其中至少 20 明确保留给四个加力推进器。科技 `1605` 继续到 `195754/216000`，黄矩阵预算仍足够但尚以 `unlocked=true` 为门槛；钛合金 120 与 PLS 台的 80 钛/80 钢均未受影响。无重放、Drift、quarantine、outcome unknown、串料或未解释正增量；本条落盘后写计数归零，下一写入先等粒子容器自然达到 80，再建立不挤占研究蓝矩阵的电路板/处理器供料。

- 2026-09-03：完成第二批黄矩阵上游转化、粒子容器铜补料与正常保存后的下一组 10 写审计，并复验 EXP-037/062/073/080/089/093/135/136。动作 `504e8529-9f97-4d20-ae4e-f9ac76167258` / `3eccab9b-489e-4ff5-bcf7-562c210a8c7a` 将专仓 25 个自动有机晶体经玩家精确送入钛晶石输入仓 `768`。制造台 `767(recipe 26)` 随后满电自然耗尽本批 25 有机晶体和 75 钛块，输出经既有长带全部进入黄糖实验室；审计时制造台只保留预取的 6 钛块、输入仓仍有 14 钛块，黄糖实验室持有 5/5 输入且继续工作。Move `003a2b52-f33e-43fc-8917-b896b91006bc` / `6ae104f5-9567-4cf0-800f-1be03d920eac` / `78303a22-6ff7-4753-82b9-b737b182eaac` / `f70b7f0d-d594-44ee-b1da-f9581bd8dffc` / `6a68c337-9ae9-499a-a5c8-fed653bc9206` / `ba2695c8-350f-4556-9aca-be8a8dce74b8` 沿 `713 -> 183 -> 侧绕 -> 141 -> 129 -> 133 -> 130` 的验证骨架逐段返回并保持 Walk/0。动作 `ae212de1-987b-4ced-9e8e-85dfcf7d2704` 将玩家 60 铜精确送入粒子容器输入仓 `884`，使仓内铜 `14 -> 74`；该线仍只缺电磁涡轮。第 10 项普通保存 `0fcaf459-6ef6-43a3-bebb-c854f85c2597` 确认 tick `11721771`。fresh 审计为 tick `11722480+`、revision `508`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error；10/10 action 均即时 terminal/completed/succeeded。两批合计 50 个黄矩阵等价上游物料现已全部进入自动链，科技 `1605` 从补料前的 `126000` 推进到 `170155/216000`，研究站仍有 37634 黄矩阵 point、实验室及在途带路继续供给，预算按 EXP-136 足以覆盖剩余 45845 hash，但尚未把预算充足提前写成科技解锁。钛合金专仓仍精确 120，PLS 制造台的 80 钢/80 钛未受影响。无重放、Drift、quarantine、outcome unknown、物品丢失或未解释正增量；审计和本条落盘后写计数归零，下一写入继续补粒子容器电磁涡轮并并行等待科技 `unlocked=true`。

- 2026-09-03：完成第二批黄矩阵上游原料补给的下一组 10 写审计，并复验 EXP-037/080/089/093/135/136。动作 `ab4ca1bd-4717-4cff-85f2-9ccec93113ea` / `c5f6df0f-9c64-4e34-90b6-85f276331cf8` 将自动水仓 `753` 的第二批 25 水经玩家精确送入有机晶体输入仓 `761`。Move `4ee4043a-29dd-4283-a881-4d07aa3defb4` / `05965a56-169e-453b-a013-ab140cebb77a` / `68ab58a9-d5a5-46cf-b44a-fcc28c74367b` / `806c3747-d2b2-473c-9bcb-a58968af7f4f` / `2083de51-7c83-443a-be87-26d05c00b3b3` / `08234dde-0f3e-40cb-ac6d-2126d0936cd6` 再沿 `130 -> 133 -> 129 -> 141 Walk 点 -> 侧绕点 -> 183 -> 713` 的既有陆地骨架逐段完成并保持 Walk/0。动作 `6c3b9ad8-1d50-4fd8-b6c6-52cbd535aec5` / `15a985f4-b16c-4f57-a188-4608caa9e78f` 利用同一公共交互点，把自动塑料仓 `558` 的第二批 50 塑料经玩家完整送入仓 `761`。fresh 审计为 tick `11703118+`、revision `492`、Walk/0、核心约 `395.72/400 MJ`、write health healthy、无 blocker/checkpoint；三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error，10/10 action 均即时 terminal/completed/succeeded。有机晶体化工厂 `760` 在 network 1/full service 工作，输入仓与设备合计仍有 73 精炼油、25 水和 45 塑料，另 5 塑料已用于首轮且专仓 `762` 出现 1 有机晶体；这恰是第二批 25 件预算。首批黄矩阵继续把科技 `1605` 推进到 `150792/216000`，研究站尚有 36770 黄矩阵 point。粒子容器线耗尽首批电磁涡轮后正常停机，专仓由 40 增至 41，铜/石墨烯仍有可解释余量；没有把缺料停机误判为拓扑故障。无重放、Drift、quarantine、outcome unknown、物品丢失或未解释正增量；审计和本条落盘后写计数归零，下一写入待第二批 25 有机晶体完成后送入钛晶石线。

- 2026-09-03：完成首批 25 黄矩阵上游补给与粒子容器石墨烯续料后的下一组 10 写审计，并新增 EXP-136、复验 EXP-037/062/073/080/089/093/135。动作 `b84efd3c-bc9e-4020-a76f-d8732bf5017e` / `475dc974-34cc-4b3a-aaf9-e27bb16d0c1f` 将专仓 25 个自动有机晶体经玩家精确送入钛晶石输入仓 `768`。制造台 `767(recipe 26)` 满电自然耗尽 25 有机晶体和 75 钛块，25 个钛晶石经既有长带全部进入黄糖实验室；审计时制造台只剩预取的 6 钛块，输入仓仍有 89 钛块，黄糖实验室已把本批钛晶石全部转化并送往研究站。返程 Move `d3612cf8-d6ed-4cac-9c3d-8410eac45c27` / `5217be70-344e-482a-87f4-e7c804457263` / `4ba5a668-ae74-438e-bcd7-a7e2d04ea3c9` / `162221cf-36e8-4b77-85ee-b3968d83facf` / `0b4ea47a-b384-49f2-872e-956615aeec56` / `7ac3e847-5a58-44c0-bda4-952854917bcd` 沿 `713 -> 183 -> 侧绕 -> 141 -> 129 -> 133 -> 130` 的已验证陆地骨架逐段完成并保持 Walk/0。动作 `15cc7763-1cf6-4151-86bc-7612d80ac378` / `c0eb7824-4b2b-415b-839b-ee8562971b9f` 再把石墨烯专仓 `871` 的 120 件经玩家精确送入粒子容器输入仓 `884`；制造台 `883(recipe 99)` 随即满电工作，粒子容器仓 `885` 由 20 增至 22。fresh 审计为 tick `11689289+`、revision `476`、Walk/0、核心约 `397.26/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error；10/10 action 均即时 terminal/completed/succeeded。科技 `1605` 已恢复到 `136964/216000`，研究站仍有 39224 黄矩阵 point 并继续工作。此前把剩余 90000 hash 按玩家缓冲比值误算为 25 黄矩阵；runtime `pointsPerHash=2` 证明真实需求为 50，首批只完成一半，已由 EXP-136 纠正。无重放、Drift、quarantine、outcome unknown、物品丢失或未解释正增量；审计和本条落盘后写计数归零，下一写入补第二批 25 有机晶体，并继续补粒子容器的铜与涡轮。

- 2026-09-03：完成有机晶体精确补料的下一组 10 写审计，并复验 EXP-037/080/089/093/135。动作 `dbd5922d-2704-4db2-849e-31a7fbc4bb30` / `4dfb3032-3837-4691-a9a6-87fddfd0ac33` 将自动水仓 `753` 的 25 水经玩家守恒送入有机晶体输入仓 `761`。随后 Move `814bfa2c-263d-4079-a768-bc95e3e91d15` / `a92ebde6-e86f-4f95-a22f-f1a773f804cb` / `86a3ddeb-25bf-4efe-84dc-3d3cedc0b6e5` / `5cb4ab4d-9a93-4a2c-9683-679b380df8e2` / `1284c9a8-cebe-44c5-a187-3d242d090f29` / `19e27e02-a9b4-4d8a-94e1-477cfd0df5f6` 沿 `130 -> 133 -> 129 -> 141 Walk 点 -> 侧绕点 -> 183 -> 713` 的既有陆地骨架逐段完成并在每段后稳定 Walk/0。动作 `5fcdb8d7-b1f8-42fe-94d0-cc6c15499e47` 从距玩家约 44.45 m 的自动塑料仓 `558` 精确取得 50 塑料；同一 `713` 外缘点距目标仓 `761` 约 73.43 m，因此第 10 项 `3be91d08-9920-4c79-8378-1141536d6985` 无需返程便将玩家 50 塑料全部送入该仓。fresh 审计为 tick `11664647+`、revision `460`、Walk/0、核心约 `396.38/400 MJ`、write health healthy、无 blocker/checkpoint，journal `45/45` durable、无 pending/error；10/10 action 均即时 terminal/completed/succeeded。化工厂 `760(recipe 25)` 在 network 1/full service 工作，仓/机内合计仍有 98 精炼油、25 水、45 塑料，另 5 塑料已按首轮配方消耗且专仓 `762` 出现 1 有机晶体；投入与产出均可解释。电网 1/4 consumer ratio 为 1.0，电网 2 在本审计瞬时为 `30000/30678 = 0.9779`，因此没有沿用上一审计“全部满供电”的结论，后续须复读并在必要时补电。科技 `1605` 仍为 `126000/216000`，等候 25 个有机晶体继续黄糖供给。无重放、Drift、quarantine、outcome unknown、物品丢失或未解释正增量；审计和本条落盘后写计数归零。

- 2026-09-03：完成石墨烯补料往返与自然生产后的下一组 10 写审计，并复验 EXP-037/062/080/089/093/135。Move `7f980ea5-a449-47cf-a206-d7d336cbe07e` 从火电站 `183` 外缘跨越约 54.7 m 到风机 `713` 的已验证陆地邻域；动作 `5e607c5d-6a7f-40f1-9d8a-df66e5ba40b9` 将高能石墨仓 `114` 的 180 件精确取到玩家，源仓 `3000 -> 2820`、玩家 `0 -> 180`。Move `bb8db318-80f9-47d9-a7f6-ec902c812bb4` 反向返回 `183` 外缘，随后 `6f3db182-9955-4cdb-8fdb-ecf4ad2fdcc2` / `766f39ce-e4cb-4a23-add2-d8ec7c88c705` 复用已验证的 6 m 侧绕与旧 `141` Walk 点，`ae9fda10-dba5-4774-b7e6-b3068846c4d1` / `39e3d2a8-bef4-4c7b-8836-03902e8bcfef` / `973bbfda-9247-45be-bf34-f53cfb39e7e1` 再沿陆地实体骨架逐段抵达风机 `130` 外缘；每段后均 fresh 确认 Walk/0。动作 `7fee2bc3-49a1-4ed7-bd0b-c8a5a5c5a7f7` 在 65.93 m 业务范围内把玩家 180 石墨完整送入石墨烯输入仓 `870`。化工厂 `869(recipe 31)` 随后以 network 1/full service 自然消耗精确 180 石墨和 60 硫酸，输入仓、设备和三只 sorter 最终均无残留，输出仓 `871` 精确得到 120 石墨烯；没有用 transfer 或设备缓冲直接伪造输出。第 10 项正常保存 `cb7060c0-5e68-49bf-aa0e-404b3f4a586a` 确认 tick `11651581`。fresh 审计为 tick `11652523+`、revision `444`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。科技 `1605` 停在 `126000/216000`，研究站结构矩阵为 0；当时曾按玩家研究缓冲的 40 件/144000 point 比值把剩余 90000 hash 误算为 25 个黄矩阵，后续 runtime `pointsPerHash=2` 已证明实际需要 50 个并由 EXP-136 纠正。本批 10/10 action 均即时 terminal/completed/succeeded；无重放、Drift、quarantine、outcome unknown、物品丢失或未解释正增量；审计和本条落盘后写计数归零。

- 2026-09-03：完成上岸回收后的下一组 10 写审计，并复验 EXP-053/061/066/093/116。Move `d1edf320-334f-4e08-92d2-dd5f5c6ef1c5` 沿原路返回上一精确 Walk 落点，虽在剩余 0.51 m 时被看门狗明确终止，fresh 已为 Walk/0；没有重放这个订单。动作 `bb05b34a-d6ed-4fa2-b925-5e24a59afabd` 到已验证无线塔 `180` 外缘，核心由约 122.88 MJ 自然回充到约 399.94 MJ 后才继续。陆地链 `b5a531a9-1ca9-4dd4-ac08-4ce49587f466` / `33a4ecc5-96ff-46c9-9bab-e97609078a6c` / `d0126b9a-3213-4fcc-a3c8-c3d3682ae151` / `a60c8998-c9f3-43ef-adf5-7728102e8db6` 逐段通过 `82 -> 133 -> 129 -> 141`，每段后都独立复读 Walk/0。旧直线 `502f49d6-49eb-4d25-8f81-8cbaeb3a380d` 向 `183` 时在距炼油厂 `141` 中心 2.10 m 的基座旁被 181-tick 看门狗以剩余 12.59 m 停止。动作 `c62d48ea-365b-41fe-b3ae-dcb4aae995eb` 先退回上一 Walk 点，`f61de60a-e239-48be-81f5-1422a5170130` 使用局部候选侧绕约 6 m，`fd350522-24d5-47a8-940d-ab44ce72f9ca` 随即稳定到达火电站 `183` 外缘。fresh 审计为 tick `11612295+`、revision `427`、Walk/0、核心约 `396.78/400 MJ`、write health healthy、无 blocker/checkpoint，10 个 action 均有 terminal 结果（8 成功、2 明确失败）；三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。硫酸仍守恒分布在石墨烯仓/化工厂 `58+2`，高能石墨仓 `114` 仍为 3000，科技 `1605` 继续至 `126000/216000`。无重放、quarantine、outcome unknown、物品丢失或未解释正增量；本审计后写计数归零，下一写入由 `183` 单订单跨越约 50.76 m 缺口到已验证陆地锚点 `713`。

- 2026-09-03：完成钛合金 120 件保存后的下一组 10 写审计，并以新的碰撞/地形反例复验 EXP-053/061/066/114/116/135。动作 `9baf77b0-7291-4746-9e09-d0efd9798a4c` / `aa6061d2-1e2e-45fc-b225-bb70afc865b5` 将硫酸仓最后 60 件经玩家守恒送入石墨烯输入仓 `870`；审计时为仓 58+化工厂 2，物品完整且配方 `31` 只因缺高能石墨而未启动。Move `ec36ef58-9e53-4b81-b2e0-2cf4d9c72648` 在经过新建密集区时于余 14.81 m 被 180-tick 物理看门狗终止；从 fresh 停点发出的侧偏净空候选 `97e37ad0-3c01-4776-9c66-2a4031bfdd88` 又因起点实际碰撞体几乎无法起步，于余 31.97 m 明确失败；两者都只取消自己的订单，未重放。动作 `dac0db32-ac61-447d-b967-1474f6764d1f` 沿已走通轨迹反向回到原 Walk 点，`d08baf53-a1dc-4938-a633-96a4dbca7446` 再稳定进入非带实体空地。后续候选路径的非带实体中心净空至少约 13.17 m，Move `83c98bfe-198e-4485-a9e0-a8ed368f8b88` 已几乎全程到达，但在余 0.51 m 时明确停下且 fresh 为海面 Drift。三个短回退 `0c9f4395-96c0-451c-913f-2c0c44bb126a` / `962fbb78-a789-4e30-91c1-170b8497b165` / `2330837a-14c6-47f4-8496-a86836fa171c` 均为 terminal/completed，但独立 fresh 运动状态仍是 Drift，不得把“到达短目标”冒充为“已上岸”。一次 commit 前状态竞争只返回无 action ID 的 `STALE_STATE`，仅重新绑定未提交意图。fresh 审计为 tick `11584078+`、revision `409`、write health healthy、无 blocker/checkpoint，10 个 action 均有 terminal 结果（7 成功、3 明确失败），三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。科技 `1605` 继续至 `111176/216000`。玩家在 Drift/约 0.0073 m/s，核心约 `250.81/400 MJ`、反应堆/燃料格为空；因此已停止无地形证据的继续短移，下一写入必须直接返回上一精确 Walk 落点，再前往无线塔充电。本审计后写计数归零。

- 2026-09-03：在提交 100 件钛合金边界前重新核对运行时配方，发现旧口径只计了两座 ILS 本体的 80 合金和两艘物流运输船本体的 20 合金，漏计四个加力推进器配方还需 20 合金；因此及时将双站+两船的真实总合金目标从 100 纠正为 120，未把部分库存提交成错误里程碑。上一完整审计后的保存 `8f75ea6f-bbb5-4dc4-bd97-74912868df5e` 先固化 100 件中间边界；动作 `1d11481e-cf3e-4482-9a5d-39a21248d9ad` / `0d210943-482d-4890-8981-a79f75da8443` 精确续入 40 硫酸，`cc003eaa-0cfa-4058-9def-37c37fb428a4` / `4cb027aa-01da-41c9-a906-d480ff61e105` 续入 20 钛块，`21c05afb-2e77-4842-b3b5-ed19ff038e86` / `dec9d37a-6ab2-47c0-ac05-f469d95929e6` 续入 20 钢材。满电熔炉 `1491(recipe 66)` 恢复五轮生产，专仓 `900` 由 `100 -> 120`；共享仓最终只剩 651 硅石，熔炉输入/输出归零，PLS 制造台 `898` 的 80 钢/80 钛保持不变。普通保存 `fd0a8ded-52da-40d7-a7a6-b9c62bed2a56` 确认 tick `11525780`；随后主动提前结算本窗口的 8 个写动作，8/8 均可查为 terminal/completed/succeeded。fresh 审计为 tick `11529693+`、revision `394`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。同期科技 `1605` 继续至 `90992/216000`，粒子容器 20，电磁涡轮专仓 71，微晶元件 216，硫酸仍余 60。无重放、quarantine、outcome unknown、串料或未解释正增量；本次提前审计后写计数归零，下一步继续石墨烯/粒子容器、处理器和加力推进器供料。

- 2026-09-03：新增 EXP-135，完成钛合金 100 件库存里程碑前的下一组 10 写审计，并复验 EXP-037/062/073/074/080/088/089/129/134。Move `cfa19955-e0fc-4f4b-9c9a-fd0f664d5eb3` 采用全厂非带实体净空评分后的侧偏短弧，一次进入仓 `1511` 的 78.96 m 交互边缘；`238d0f15-a9d8-48f5-afa9-e64a02142e00` 守恒取得 300 自动铁块，反向 Move `57ee0139-3b95-4bb2-8dc6-aee19c6ae40d` 返回原稳定点，`e19d1393-ec94-4f84-bc32-3562428ccd9c` 再把 300 铁装入四过滤共享仓 `723`。电机与齿轮支路恢复后，涡轮专仓由保留的 23 增至 44。动作 `08490302-eaa2-46a8-9f5c-15a2854f32eb` / `22506a85-b479-4427-a61c-343e2f4e5c0b` 向过滤共享仓 `899` 守恒续入 40 硫酸，`ef29f010-971d-4a29-bef0-04f3a8c10a2c` / `9ca0a2b4-238d-42fc-98e4-10fd230a53c5` 续入 12 钛块，`5f8441b7-35fc-4d8a-9251-2f29a7bce125` / `deb26af8-aa97-4f89-8560-180d0e659190` 续入 12 钢材；连同熔炉原有 8/8 金属，恰好完成 5 轮 `4 Ti + 4 steel + 8 acid -> 4 alloy`，钛合金专仓 `80 -> 100`，共享仓和熔炉三项投入均归零，PLS 制造台预装的 80 钛/80 钢保持不变，651 硅石未串料。10/10 action 在 tick `11504835+` 全部可查询为 terminal/completed/succeeded；fresh 状态为 revision `386`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。同期硫酸批次完成后仍余 100，微晶元件由 16 持续增至 187，粒子容器保持 20；科技 `1605` 到 `79934/216000`。无重放、quarantine、outcome unknown、串料或未解释物品正增量；本审计后写计数归零，下一写入先普通保存该库存边界，再继续石墨烯/粒子容器和处理器供料。

- 2026-09-03：完成下一组 10 个已接受写动作复核，并复验 EXP-037/062/073/074/088/089/134。动作 `10c8a84e-3868-45ba-988e-346ebaacfa20`、`7f4acd59-63ea-422b-8a47-8ec43be5a18e`、`738def33-510a-43f6-9f7d-bf555d2ed60e` 把玩家原有 61 油与自动油仓 119 油合并守恒送入硫酸仓 `862`；`235a475d-2201-4733-8700-cd842b8dabaa` / `0c4e60f4-3b7d-484f-9d97-48b0774879db` 转入 240 自动石矿，`9821708b-f87c-41c9-b0bd-dd246078c80f` / `14355aa8-4695-4435-8d96-bd9857ee3883` 转入 120 自动水，形成 180/240/120 的 120 硫酸预算。动作 `1ba409d5-da20-4961-a922-a948d9113bce` / `572e8f8a-40e7-414a-8c29-21cb161b8658` 从过滤仓 `562` 向电机共享仓 `723` 守恒续入 500 磁铁；第 10 项 `c2f8cb8e-cad6-47c1-8799-3bbd06b19e1a` 把玩家现有 200 铜守恒装入高纯硅混合仓 `843`。10/10 action 在 tick `11478464+` 全部仍为 terminal/completed/succeeded；fresh 状态为 revision `374`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。硫酸线三料均已下降，设备内出现 2 酸、专仓 `20 -> 21`；微晶元件制造台满电工作、专仓 `16 -> 22`。磁铁仓 `500 -> 491` 且磁线圈设备输出由先前 6 增至 14，证明过滤支路恢复，但电动机仍因共享仓没有铁块而停机，故没有把磁铁单料补给误报成电磁涡轮全链恢复。科技 `1605` 已推进到 `55517/216000`。无重放、quarantine、outcome unknown、串料或未解释物品正增量；本审计后写计数归零，下一步先补共享仓铁块并生产石墨烯，再补处理器专用电路板。

- 2026-09-03：完成上一批剩余 2 写后的完整 10 写审计，并复验 EXP-062/080/134。前 8 项为两座 ILS 的钛合金精确补料与正常保存：`513b7126-0af9-439c-815b-4702be602b9b`、`fa987b6f-e626-4e1d-960a-745b1f3f2ef2`、`e1bacccb-b5e7-4094-ab5e-43599ce7af82`、`7b0103be-e552-4923-8466-28a00e959039`、`8565d7cd-a8de-4f8c-9a4e-70b90850eaeb`、`3466a549-e0fc-451b-be74-2f66fcbd20e9`、`09a121e4-cd12-48b4-bc8f-d2089fa6513f`、`e15c0b6b-7873-451e-b2e4-b950c0d5f5ba`；后两项 `18ea33c7-e4f0-471d-baf1-d0561d052f97` / `a91e8e01-cde9-47e3-8db1-d13ad598a494` 把 80 个电磁涡轮从仓 `827` 经玩家送入粒子容器输入仓 `884`，玩家 `0 -> 80 -> 0`，源仓 `103 -> 23`。10/10 action 在 tick `11469043+` 仍可查询为 terminal/completed/succeeded；fresh 现场为 revision `364`、planet `104`、Walk/0、核心 `400/400 MJ`、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。粒子容器线保持满电自动工作，目标仓 `884` 当时有涡轮/铜/石墨烯 `51/69/9`、制造台各缓存 2，专仓 `885` 已由 `1 -> 12`；静态源仓保留 23 个涡轮供加力推进器。科技 `1605` 已推进到 `52277/216000`，钛合金仍为 80、硫酸仍为 20。目标活跃仓的跨窗聚合少于 80 由制造台输入和 11 个新增产物的正常消耗解释，未重放；无 quarantine、outcome unknown、串料或未解释物品正增量。本审计后写计数归零；下一写入优先补粒子容器石墨烯/铜和处理器电路板，同时保留加力推进器材料。

- 2026-09-03：完成两座 ILS 所需钛合金库存里程碑并复验 EXP-037/062/073/089/129。上一审计后只补已在玩家手中的 32 硫酸半程：动作 `513b7126-0af9-439c-815b-4702be602b9b` 将玩家 `32 -> 0`、过滤共享仓 `899` 的硫酸 `0 -> 32`，三只输入 sorter 随即分别实际携带钛块/钢材/硫酸，满电熔炉 `1491(recipe 66)` 工作，专仓 `900` 从 `8 -> 11 -> 31 -> 49`。硫酸化工厂同期持续自然产出；随后 `fa987b6f-e626-4e1d-960a-745b1f3f2ef2` / `e1bacccb-b5e7-4094-ab5e-43599ce7af82` 守恒续入 40 酸，`7b0103be-e552-4923-8466-28a00e959039` / `8565d7cd-a8de-4f8c-9a4e-70b90850eaeb` 续入 24 酸，`3466a549-e0fc-451b-be74-2f66fcbd20e9` / `09a121e4-cd12-48b4-bc8f-d2089fa6513f` 最后精确续入 44 酸。连同设备原有 4 酸，总供给 144，恰好支持 18 轮 `8 acid -> 4 alloy`；目标仓最终精确为 80 钛合金，设备内保留 8 钛/8 钢、酸为 0，共享仓只剩 651 硅石，硫酸专仓另余 20。动作 `e15c0b6b-7873-451e-b2e4-b950c0d5f5ba` 随后由 DSP 正常 save API 确认 tick `11439545`；fresh 保存后为 tick `11440147+`、revision `362`、write health healthy、无 blocker/checkpoint，journal `45/45` durable、无 pending/error，科技 `1605` 继续到 `39677/216000`。本里程碑没有新增首次事件，因为钛合金 sequence `45` 已在首批记录；没有把“无新 journal”误判为产线失败。上一审计后共 8 个已接受写动作，均已即时 terminal/completed/succeeded；未出现重放、quarantine、outcome unknown、串料或未解释增量。下一次再接受 2 个写动作后必须先做完整 10 写审计。

- 2026-09-03：完成永久铁供给审计后的下一组 10 个已接受写动作复核，并实机复验 EXP-037/073/080/089/129。动作 `302892c1-9fa0-4917-9052-1c66906edc41` 把玩家既有 120 精炼油守恒装入硫酸输入仓；`e40eddce-453b-4d3f-aa8a-7bd1c3d5d647` / `8f774b04-3ac0-4d6c-b8b7-3055c9990dd4` 从持续自动补货的混合仓 `95` 经玩家转入 320 石矿；`1de52f3d-6c51-48a2-9579-effb0de18259` / `e170adc9-55d3-4710-a4b4-659b5c34b766` 从自动水仓 `753` 转入 160 水。连同上一批 120 油，输入总预算为 240 油/320 石矿/160 水，对应 160 硫酸。三过滤 `865/866/867` 与输出 `864` 均保持 network 1、serve ratio 1.0，化工厂 `861(recipe 24)` 工作，三种输入均下降、输出仓 `863` 从 0 连续增至 38+。等待期间，`f3a38318-222d-4ccf-bbf4-c55dddcff6ed` / `93bb68b5-3a8f-480c-b4b7-7cb7e3a93d24` 从钛块仓 `768` 向过滤多料仓 `899` 守恒转入 68 钛块（`4 -> 72`）；`b9a466d9-996e-41a3-968c-1a42da2e4f82` / `fb33d7cc-7cc1-44d6-b1da-59c9eb3815ba` 同样转入 68 钢材（`4 -> 72`），源钢仓在动作前受自动补货影响由离散快照 1739 增到动作前 1742，action 内部仍精确 `1742 -> 1674`，继续验证活跃源必须以动作内守恒而非跨窗净差量判断。第 10 项 `5e79cbfc-9899-40fe-8031-5c7b518dcfdb` 只把首批 32 硫酸从仓 `863` 守恒取到玩家（仓动作内 `38 -> 6`、玩家 `0 -> 32`），尚未向目标仓写入，下一批不得重取这一半程。10/10 action 均仍可查询为 terminal/completed/succeeded；fresh 审计为 tick `11415411+`、revision `354`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error，手持物/手搓/施工均为空。科技 `1605` 继续到 `28877/216000`。玩家只新增并持有待转的 32 硫酸，目标仓仍有 72 钛/72 钢和 651 硅石，钛合金仓仍为 8；没有重放、quarantine、outcome unknown、串料或未解释增量。本审计后写计数归零；下一写入只补 `player -> storage 899` 的 32 硫酸半程。

- 2026-09-03：完成上一审计后的 10 个已接受写动作复核，新增 EXP-134 并复验 EXP-073/120/124/126/127/133。前 7 项为永久铁供给旁路：独立 9 格桥接带、`1582 -> 1593 -> 1585`、13/26/18/5 格四段全厂无重叠续带，以及 `1652 -> 1656 -> storage 28`；第 8 项为正常保存 `966b3f17-ee67-4e0e-86b0-bdae86228224`，后两项 `a73f4411-b64d-4c50-905f-2fc073b433bb` / `96fc938d-35e1-4980-bc76-030b93d813a2` 把 120 精炼油从自动油仓 `286` 经玩家精确守恒送入硫酸输入仓 `862`（玩家 `181 -> 301 -> 181`、源仓 `500 -> 380`、目标仓 `0 -> 120`）。审计时九项 action 仍为 terminal/completed/succeeded；5 格带 action `b6d8db8-f6b2-40ff-8abb-9060fe86eb02` 已不在运行时保留，但其即时成功终态、末带 `1652`、后继 sorter 双端、背包消耗和已保存最终消费者增长共同唯一核销，未重放。fresh 审计为 tick `11401837+`、revision `344`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint；三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error，施工无人机 3/3 idle、无 pending build，手搓队列和手持物均空。永久铁源仓 `1511` 有 546、`1535/1656` 满电实际携铁、蓝矩阵 sorter `78` 实际携货，科技 `1605` 已由上次保存后的 `18101` 继续到 `23837/216000`。硫酸仓链的三过滤和四只 sorter 均保持满电，输入仓已有 108 油且化工厂已预装 12 油；仍缺石矿和水，所以没有把备料误称为复产。无 quarantine、outcome unknown、混料或未解释增量。本审计后写计数归零；下一写入按守恒批次补石矿与水。

- 2026-09-03：新增 EXP-133，完成永久铁供给恢复产品里程碑。上一审计后共接受 8 个写动作：先以 `58c0b1e5-c172-4cf4-9150-b059b895efc6` 在旧带占位之外建立独立 9 格桥接线，`a792c8cd-3ed8-450e-87ae-585a3b9b0c4d` 用满电 sorter `1593` 把仅含铁块的新线末端 `1582` 显式送入新带 `1585`；再以 `53a7acd3-963d-4b56-9b35-3901aa85152d` / `b45d4ee3-4885-4897-ae58-ce0d54c85dcd` / `a35aab94-4953-41a8-991d-deb5935727e5` / `b6d8db8-f6b2-40ff-8abb-9060fe86eb02` 分四段续建 13/26/18/5 格无重叠旁路到末带 `1652`，每次 commit 前均 fresh 比较 plannedPath 与全部既有 belt，并忽略唯一绑定 source 后以 0.25 m 排除重叠；`5ca88abe-1912-4710-91b4-6e79e37c8e10` 最终建成 `1652 -> sorter 1656 -> storage 28`。源仓 `1511` 保持 545+ 铁块、熔炉 `1500` 输出缓冲 100，`1535/1593/1656` 均在 network 1、serve ratio 1.0 且实际携铁。旧入口仓 `28` 因下游即时消费在离散快照中常为空，因此没有以仓库存冒充闭环；跨三个独立观察窗继续证明电路板台 `36` 获得铁、蓝矩阵台 `76` 工作、sorter `78` 实际持有蓝矩阵，科技 `1605` 从 `12317 -> 15702 -> 16637 -> 17357 -> 18101/216000`。第 8 项正常保存 `966b3f17-ee67-4e0e-86b0-bdae86228224` 已确认 tick `11388372`；fresh 保存后状态为 tick `11389602+`、revision `342`、planet `104`、Walk/0、核心 `400/400 MJ`、write health healthy、无 checkpoint，三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error，背包剩 5 条带和 1 个 sorter。没有重放、quarantine、outcome unknown、混料或未解释增量。本审计窗口尚为 8 个已接受写动作；再接受 2 个写动作后必须先做完整 10 写审计。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并把 EXP-124 升级为 validated、复验 EXP-128。动作 `564e0dee-f48a-4bca-8c87-8a317219d806` / `a4b8828b-8e45-4049-8882-e0a8ee691af8` / `48fe4427-d32e-4f6b-b01b-3d599cbb4268` 分别从稳定末带续建 13/13/22 格；第三段虽由原生施工和计划连接正常验收，却 fresh 发现末端区域与既有过滤磁铁线生成同坐标、独立拓扑的重复 belt（`1582=1444`、`1583=1445`、`1581=1442`、`1580=1441`、`1579=1431/1440`），后续只读候选器因此拒绝所有继续重叠方案，未再 commit。动作 `5232602e-1514-42f1-8aef-efccd9846d64` 从自动铁仓守恒取得 60 铁块，`9126d0ee-73be-4692-baac-d8a508509623` 递归手搓 60 条带，背包带 `16 -> 76`；动作 `4d807148-df7b-42d5-885b-b1eba082fb31` 又守恒取得 100 铁块作为临时科研缓冲。Move `6b6bf563-9f48-4fdd-b4d0-527b53423c82` 前进约 39 m 后在剩余 47.98 m 处由 180-tick 物理停滞看门狗明确失败；fresh 识别停点紧邻仓 `870` 和 sorter `873/874`。没有重放，动作 `ae72e20b-4853-4ac5-ace6-d5d4d14d4bf5` 横移约 8 m 到稀疏侧，`04e58b84-de35-439a-84ac-def401c4fe17` 再正常到达旧铁仓附近，`1c1ca00c-7d44-454d-94f5-dcd17a8d6761` 最终把 100 铁块守恒装入仓 `28`。fresh 审计为 tick `11328546+`、revision `327`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint；三张电网 consumer ratio 均为 `1.0`，journal `45/45` durable、无 pending/error。9 项成功、1 项明确失败均 terminal；无重放、quarantine、outcome unknown、混料或未解释增量。新矿机/熔炉/仓/送料 sorter 保持满电自动工作，仓 `1511` 审计时已有 286 铁块、熔炉输出缓冲 100；旧仓 `28` 已从 100 消耗到 28，旧送料 sorter `594/721` 满电工作，电路板台 `36` 满电产出缓冲 12，蓝矩阵台 `76` 与研究台 `84` 均恢复工作，科技 `1605` 从 `1337 -> 1345 -> 3355/216000`。本审计后写计数归零；下一写入不得继续重叠带，先利用科研恢复窗口设计永久无重叠铁供给。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-132、复验 EXP-123/126。动作 `592ccbe6-beb5-4a74-879e-8f321886be41` 建成 `1508 -> sorter 1509 -> smelter 1500`，双端正确但 sorter 为 network `0`、熔炉仍零输入；动作 `01c1f7ea-e373-40ed-8588-6860931b9179` / `6c09d4db-dd32-4ca8-bb81-3697cf736c5f` 普通手搓并在 2.49 m 外建成电塔 `1510`，sorter 随即进入 network `1`、熔炉取得铁矿并产出 5 铁块。动作 `867e5d57-5a85-4cfb-9bb7-1bbb392fdd65` 建成专用仓 `1511`；`d8488068-85fe-49b2-8483-4e36032ec97a` 建成满电输出 sorter `1512`，10 秒窗中仓 `0 -> 5`、熔炉输出已有 47，自动铁块闭环成立。动作 `786cbd7f-6804-4c5a-b45f-4bf723879414` 递归手搓 4 个 sorter。下一首段长带 build 在本地 30 秒窗口内无 action ID，未重放；背包带 `26 -> 4` 与最终实体 `1513…1534` 唯一核销 22 格施工。第一次 fresh 扫描尚缺最后完成的 `1534`，故临时认为 `1533` 是起点；动作 `05115a87-aca1-4f3d-9c5b-1b9fe06372d4` 已把仓以满电 sorter `1535` 注入 `1533`，最终有向复读为稳定单链 `1534 -> 1533 -> … -> 1532`，pending build/working drone 均为 0，当前中段注入仍沿正确输出方向。活跃仓 transfer 的第一次 commit 仅以无 action ID 的 `STALE_STATE` 拒绝；下一次 fresh 原子重试由动作 `3201e887-8256-432f-99d7-3e5752713f13` 守恒取得 60 自动铁块（仓 `67 -> 7`、玩家 `0 -> 60`）。第 10 个普通 handcraft 同样超过本地显示窗口且未重放；最终队列为空、铁块 `60 -> 0`、传送带 `4 -> 64`，唯一核销 60 条带。fresh 审计为 tick `11267891+`、revision `311`、planet `104`、Walk/0、核心约 `382.36/400 MJ`、和平/非沙盒/1×、write health healthy、无 checkpoint；journal `45/45` durable、无 pending/error，主网 `85620/151000`、ratio `1.0`。8 个可查询 action 均 terminal/completed/succeeded，另两项由唯一库存/实体/队列核销；未出现重放、quarantine、outcome unknown、材料丢失或未解释增量。矿机/熔炉/三只 sorter 满电，仓 `1511` 已重新积 34 铁块，熔炉输出缓冲 100；科技 `1605` 仍为 `1337/216000`，因为新带尚未抵达电路板台。本审计后写计数归零；下一写入只从稳定末带 `1532` 继续向制造台 `36` 延伸。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-131。动作 `c1d73c83-ae9f-45be-92a2-17705538c579` 递归手搓 2 座电力感应塔；`e80ffadf-0863-4a3e-9f80-d488aba7adc9` / `6b1cb564-8171-4d86-a900-8657b9f2458a` 将其沿新矿机到既有电塔 `847` 的路线建成 `1498/1499`，使孤立 network `3` 并入主网 `1`。fresh 主网为 `56892/151000`、consumer ratio `1.0`，矿机 `1496` 满供电且 8 点集合不变。动作 `6427b56c-c391-4e43-9439-04a7552a7562` / `e14b39ee-17c6-4850-b869-593f31408f21` 分别递归手搓 1 座熔炉和 2 个 sorter；动作 `2f9d5eac-81a3-4afa-ba68-49c8abf9d217` 从活跃混合仓 `95` 向玩家精确转移 4 石矿，仓的离散前后读为 `1400 -> 1397` 是上游同期补入 1 件，action 自身玩家 delta 为 `0 -> 4`。动作 `846d517e-cf08-4322-9bcd-74061ee077db` 又手搓 1 座小型仓。动作 `11e7b7f1-1e43-4141-a97f-0b4063d27f13` 在矿机 4.5 m 外建成满电熔炉 `1500`，`b998227d-796c-456f-ad31-41dff9c8864f` 只在空设备上配置铁块 recipe `1`。矿机→熔炉的直接 sorter prepare 以 `BUILD_CONNECTION_INVALID` 无 action ID 拒绝；没有重试同一路径。第 10 个动作 `2e71eedb-1efb-4505-9f65-bddf9e7c364a` 从矿机原生出口建成 8 格带 `1501…1508`，背包带 `34 -> 26`，末端距熔炉 2.81 m 且保持自由。fresh 审计为 tick `11241678+`、revision `292`、planet `104`、Walk/0、核心约 `372.00/400 MJ`、和平/非沙盒/1×、write health healthy、无 checkpoint；journal `45/45` durable、无 pending/error。10/10 action 均 terminal/completed/succeeded，所有 building/item delta 与当前背包 26 带、2 sorter、1 仓精确闭合；未出现重放、quarantine、outcome unknown、串料或未解释增量。科技 `1605` 仍为 `1337/216000`，熔炉尚因末带未接 sorter 而保持输入/输出 0，没有把原生带路冒充铁块恢复。本审计后写计数归零；下一写入只接 `1508 -> sorter -> 1500` 并以 sorter 实际携矿及熔炉输入/输出增长复验。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-130、复验 EXP-034/112/125。前 4 项是钛合金产品里程碑的钛块入仓、钢材两段守恒转运和正常保存 `16d52320-bdd3-4283-bc3f-9105ee3669e7`；后 6 项继续恢复蓝矩阵的铁源。动作 `129980b1-f097-4d82-90a5-19654c9bc040` 正常到达铁节点 `55` 外 2.35 m。下一唯一 harvest 在本地 30 秒显示窗口结束时尚未返回，因此没有重放；中间 fresh 只见矿脉 `37282 -> 37220`、背包铁矿 `2 -> 64`，最终读回为矿脉 `37174`、背包在递归手搓矿机后为 102。矿机配方实际消耗 8 铁矿、2 磁铁和 2 铜块并保留各 1 个配方批次余出的电路板/磁线圈，故 `2 + 108 - 8 = 102` 完整闭合。动作 `0529063a-03b0-4d27-bc55-17c637ee807b` 产出矿机；最近节点 `55` 的单锚点 prepare 仅覆盖 5 点且未 commit，随后检查 group `4` 全部 14 个未占用锚点，动作 `ab79b65f-bffc-4c43-9c98-63673c9eba66` 只提交覆盖 8 点的最佳方案并建成矿机 `1496`，实际节点集合与计划精确相等。动作 `dc805a41-5ef3-4398-9276-5c09e1fbc0b1` / `6869344a-abc2-4814-837c-2a966c0bfae0` 再递归手搓并于矿机 5.30 m 外建成风机 `1497`。运行瞬间 network `3` 为 `5000/7000`、矿机服务率 `0.7143`；缓存填满 50 后空闲需求降至 400、读回 ratio `1.0`，不能用空闲满供电掩盖运行缺口。fresh 审计为 tick `11218609+`、revision `273`、planet `104`、Walk/0、核心约 `356.29/400 MJ`、和平/非沙盒/1×、write health healthy、无 checkpoint；journal `45/45` durable、无 pending/error。9 个可查询 action 均 terminal/completed/succeeded，harvest 由唯一矿脉/背包/配方差量核销；未出现重放、quarantine、outcome unknown、物品丢失或未解释增量。科技 `1605` 仍为 `1337/216000`；fresh 诊断已把当前蓝矩阵断点定位为电路板制造台 `36` 缺铁而非磁铁链中断。本审计后写计数归零；下一写入先补第二台风机使 8 点矿机运行态满供电，再接铁矿冶炼与电路板链。

- 2026-09-03：新增 EXP-129 并完成钛合金产品里程碑。上一审计后动作 `eb31cefa-a94a-4433-abf6-6d142e89560f` 把玩家 100 钛块守恒装入仓 `899`；`e5a7d2e6-d587-4b8f-a44b-554f04be57a2` / `fab8dca0-6d0a-4371-93de-9c1c2be50a3b` 又把钢材仓 `792` 的 100 钢材经玩家守恒装入同仓。所有新旧 sorter 已预先过滤，硅石 651 不变；满电熔炉 `1491(recipe 66)` 实际工作，专用输出路径 `1491 -> 1492 -> 900` 使 item `1107` 从 0 增至 8。journal sequence `45` 在 tick `11172619`（本局 `002d 03:43:30`）durable 记录首次产线钛合金，无 pending/error；动作 `16d52320-bdd3-4283-bc3f-9105ee3669e7` 随后通过 DSP 正常 save API 持久化 tick `11175248`、revision `261`，write health healthy、无 checkpoint、exact-primary restart 可用。边界明确：20 硫酸只支持 2 轮、当前产线已因硫酸耗至设备 4 而停，且共享仓的钛/钢同时预装进 PLS 制造台；这是成立的首次自动产物线，不是 ILS 物料总量完成。本审计窗口当前累计 4 个已接受写动作。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-128。动作 `934ce161-2d87-4400-899c-be57c4e5da7a` 先把首条输入 sorter `1493` 在空载窗口锁为钛块；`313326e0-0512-4e53-b79f-c34496529711` / `41dc0a4c-1c17-4a66-9adf-1d4065e40f9c` 与 `99b45ce2-8e46-484a-bf9d-3d2de851daba` / `faf14436-11d7-447e-ba7e-e4320e9e5566` 再分别建成并过滤钢材 sorter `1494` 和硫酸 sorter `1495`。动作 `2436e80b-c461-4b5e-ab69-25adde10d934` 只在全部三过滤成立、设备仍空时配置钛合金 recipe `66`。两次 transfer `e90ede3e-3354-4829-be78-d65b75af5271` / `56a1cbea-fcc9-4264-a771-6c4a50e17743` 把硫酸仓 `863` 的 20 硫酸经玩家守恒装入多料仓 `899`；审计时 16 已进入熔炉、4 仍在仓，三条过滤均无串料。第 9 项 Move `9bcc4013-4395-41d6-bb27-a7f7551dadae` 前进约 27 m 后在距无线塔 10.46 m 处由物理停滞看门狗明确 `action_failed`，没有重放；fresh Walk/0 停点已进入钛/钢仓范围，第 10 项 `bc8634e2-8bca-4460-adcb-94605ed3fb83` 因而直接守恒取得 100 钛块。fresh 审计为 tick `11166447+`、revision `257`、planet `104`、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint；三张电网 consumer ratio 均为 1.0，journal `44/44` durable、无 pending/error。9 项成功动作与 1 项明确失败动作均 terminal，未出现 quarantine、outcome unknown、串料或未解释增量。研究 `1605` 仍为 `1337/216000`，蓝矩阵吞吐限制不变；熔炉 `1491` 满电、recipe/四 sorter/双端反查成立，但缺钛和钢尚未产出。本审计后写计数归零；下一写入先把玩家 100 钛块装入仓 `899`，再从当前点取钢材并装入，追踪 item `1107` 首产和 journal durability。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-127。前 3 项是 EXP-126 的补塔、供料闭环与正常保存；第 4 项动作 `9e3a2adb-0b12-41cf-85c6-5aac3cff523b` 正常选择星际物流系统 `1605`，journal sequence `44` 在 tick `11122095` durable。第 5 项采铁的本地 10 秒输出窗口没有显示 action ID，因此没有重放；fresh 双边读回精确闭合矿脉 `55` 的 `37294 -> 37282` 与背包铁矿 `0 -> 12`。第 6/7 项 `2adb5e7f-00aa-4ec9-ab54-2b295adc94fc` / `58dcc092-61fc-4424-8cca-2cb26b727b8b` 由普通 replicator 递归手搓 1 台电弧熔炉和 4 个 sorter；第 8 项 `8d0bcb40-2693-4577-9031-d402a3cdd0fb` 在现有多料输入仓 `899` 与空输出仓 `900` 之间原生施工满电熔炉 `1491`，第 9/10 项 `3daff38f-72f6-4b5e-9b04-521fb67fb3c6` / `7a711200-bef1-4394-a2c7-7457ed8644d2` 分别建成双端反查成立的 `1491 -> 1492 -> 900` 和 `899 -> 1493 -> 1491`。fresh 审计为 tick `11148734+`、revision `241`、planet `104`、玩家 Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint；三张电网 consumer ratio 均为 1.0，journal `44/44` durable、无 pending/error。9 个可查询 action 均为 terminal/completed/succeeded，第 5 项由唯一矿脉/库存差量核销；没有重放、quarantine、outcome unknown、材料丢失或未解释增量。熔炉 `1491` 仍为 recipe 0，首条输入 sorter `1493` 尚未过滤且保持空载，因此没有把合法预建冒充钛合金产线；下一写入必须先把 `1493` 锁为钛块，再以剩余两个 sorter 分别锁钢材/硫酸，最后才配置 recipe `66` 和装料。本审计后写计数归零。

- 2026-09-03：EXP-126 由 observed 升为 validated。审计后普通手搓 1 座电力感应塔并以唯一施工建成 `1490`；fresh 读回证明 sorter `1489` 从 `powerNetworkId=null` 变为 network 1、serve ratio 1.0，并实际携带磁铁。下游 sorter、磁线圈制造台 `73`、蓝矩阵站 `76` 和研究站 `84` 依次恢复，研究站蓝矩阵点从 0 增至 12340；高强度钛合金 `1414` 在 tick `11098574` 正常完成。普通保存动作 `267a063c-dc87-4a44-a183-efa1a436096b` 持久化 tick `11099621`，write health healthy。本审计窗口当前累计 3 个已接受写动作，尚未达到下一次 10 写审计门。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并新增 EXP-126。两次续带动作把末端从 `1471` 推进到 `1479`，均按 EXP-032 各回收 1 磁铁；独立 5 格中继带 `1482…1486` 避开了旧带重叠。普通 replicator 用 2 铁矿和 2 电路板递归手搓 2 个 sorter；随后 `48 -> 1487(filter 1102) -> 73` 与 `1486 -> 1488(filter 1102) -> 63` 均完成双端反查，最后 `1479 -> 1489 -> 1482` 接通上游。第 10 项动作只从范围内铁节点 `55` 手采 2 铁，矿脉 `37296 -> 37294`、背包 `0 -> 2`，为补一座电力感应塔备料。10/10 action 均为 terminal/completed/succeeded、无 stalled/recovery required；fresh 审计为 tick `11075068+`、revision `222`、planet `104`、Walk/0、核心 `400/400 MJ`、和平/非沙盒/1×、write health healthy、无 blocker/checkpoint。journal `43/43` durable、无 pending/error；三张电网均 serve ratio 1.0。分页拓扑复读证明 `1236 -> … -> 1479` 为连续 246 格单链，但桥接 sorter `1489` 为 `powerNetworkId=null`，所以磁铁尚未跨入中继；下游 `1487/1488` 均为 network 1，制造台 `73`、蓝矩阵站 `76` 和科技 `1414` 仍停在补塔前状态。没有把施工完成、源仓增长或等待时间冒充产线恢复。本审计后写计数归零；下一写入先递归手搓 1 座电力感应塔，再在 1489 附近以原生候选补电并逐层复读到科研增长。

- 2026-09-03：新增 EXP-125。审计后的下一次施工没有进入 commit：fresh prepare 明确以 `SERVER_BUSY` 拒绝，原因是上一轮候选扫描在 60 秒生命周期内累计占满 normal-game plan store；没有 action ID、物品变化或拓扑变化，不计游戏写入。源码确认容量 128、默认生命周期 60 秒；只读候选脚本已按最多 64 次尝试分批，并默认等待本批最晚 token 过期后才返回。下一次施工仍从 fresh player/source 重新 prepare。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，新增 EXP-124 并复验 EXP-032/046/120/121/123。动作 `a11282de-bd8a-4587-8fe5-538dbb89b4fe` 在铁节点 `55` 正常手采 100 铁矿，节点 `37396 -> 37296`、背包 `1 -> 101`；`3b5c27ac-3059-42bb-bf83-58bc99b36771` 用普通 replicator 将 99 铁矿转为 99 条带。八次唯一施工 `a2653b0e-408d-4d0a-99f2-5c94548a7379`、`9f6129c3-6c83-4fe5-8045-b517f7d9aeac`、`ed29090c-2369-4f5a-b916-d86aefac2f46`、`dc51f264-06cf-4e0e-9404-51b393bcc14b`、`8f483804-6533-45bd-ad21-facfc404834b`、`0a44ed25-9ddb-41ae-a1d7-29cdda61ed57`、`31b19a1e-6248-4667-aaa8-9f32f6fc1e04`、`a026ebbc-7ea6-44ff-916d-4c6da5d792c8` 分别延伸 15/9/6/4/6/10/4/4 格，共 58 格；每次续带均按 EXP-032 回收末端 1 磁铁，背包磁铁 `15 -> 23`，没有把它误算作新产线产量。全批 10/10 action 均为 terminal/completed/succeeded、无 stalled/recovery required；fresh 全厂遍历证明 `1217 -> 1241 -> 1236 -> … -> 1471` 为 238 格唯一有向 belt 单链，无分支、环路或意外外接，自由末端距仓 `30` 约 20.4 m。审计终态 tick `11001789+`、revision `202`、planet `104`、玩家仍在独立陆地点 Walk/0、核心约 `344.43/400 MJ`、healthy、无 blocker/checkpoint；journal `43/43` durable、无 pending/error。磁铁仓 `1217` 共 1594，熔炉输出缓冲 100 且因未接最终消费者暂时背压；仓 `30` 仍空，磁线圈台 `73` 的 1 磁铁/3 铜块未动，蓝矩阵站 `76` 和研究站 `84` 均为蓝 0，科技 `1414` 因此仍是 `130937/144000`。本审计后写计数归零；下一写入从 `1471` 继续绕旧 lab/assembler 接仓 `30`，接通前不宣称蓝糖恢复。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，新增 EXP-123 并复验 EXP-032/046/114/121/122。前四项为浅海恢复：`b38792db-9ea8-4711-9a79-03ba1beb631c` / `6efe904a-29f4-4d68-b308-f3d643694f18` 两次有界短移仍为 Drift，`8861f201-3345-4b71-90eb-23febe93a419` 收敛到仓 `863` 外稳定 Walk，`2b9d4274-b3b3-4c56-9637-2f05ff8968b5` 到无线塔 `180` 外缘后自然充满。第五个唯一提交的 30 格带施工已完成，但结果摘要在动作终态后访问不存在的 `plannedBuilds` 字段而未显示 action ID；未重放，fresh 实体 `1313…1342`、背包带 `43 -> 13`、`1312 -> … -> 1322` 有向连接已唯一核销。动作 `048eec88-6b24-486e-bb64-e789e37cebc5` 普通手搓 60 带；`8daaa9aa-224f-4256-a86f-7f037b341ce2` / `2b14ad35-b44e-4e03-aa47-7a56922a2ac2` / `7ea82222-bd7c-4c54-ad44-26a24f7be20c` 分别建成 24/26/23 格续段；`20a00158-8f3c-4636-a0ad-f65693313ce9` 用剩余 6 铁矿普通手搓 6 带。所有已接受动作均已由终态或唯一实体/库存/拓扑核销；新带累计 180 格，只有预期源 `1217 -> 1241 -> 1236` 和自由末端 `1393`，无分支/意外外接。`7ea82222…` 的磁铁 `14 -> 15` 是 EXP-032 的续带回收复现，不是产线产出；末端仍距仓 `30` 约 52 m，仓 `30` 为空，旧蓝链的磁线圈台 `73` 内 1 磁铁在 12 秒观察中不变，未把残料冒充新流量。审计终态为 tick `10902330+`、revision `182`、planet `104`、玩家在无线塔旁 Walk/0、核心 `400/400 MJ`、healthy、无 blocker/checkpoint；journal `43/43` durable、无 pending/error。六节点矿机 `1213` 与熔炉 `1216` 满电工作，磁铁仓 `1217` 已积 902；科技 `1414` 仍为 `130937/144000`，研究站蓝矩阵为 0。本审计后写计数归零；下一写入先安全补铁/传送带，再从 `1393` 继续绕建筑外缘接仓 `30`。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核并新增 EXP-122。动作 `bde8fa47-ff3f-4ce4-be20-d4a8da9bf55c` 从带 `1239` 建成 13 格外圈续段至 `1254`；`ff3f0d2f-f04b-468a-ab2e-d0c96cd77c96` 普通手搓 30 条带。动作 `8158bf8f-7689-4cf6-83c9-37b246eb29c7` 以全厂实体净空筛选后建成 27 格续段至 `1281`。库存不足时，动作 `1d8c4663-eca4-4a45-924a-3b4ddbd20a5e` 在原铁节点 `43` 正常手采 100 铁矿，节点减少 108、背包实得 100；动作 `0781d14d-34a3-4998-b3ad-7eea3da0ec71` 由普通 replicator 递归加工 60 条带。动作 `bfadcfdc-3028-49c0-91bc-684b2a41843f` 再建成 31 格外圈续段至 `1312`。随后四次分段 Move 中，前三次到 `1239/1254/1281` 均 fresh 复读 Walk/0；第四次 `ece33010-6572-4480-b9b7-8054de500126` 虽在 2.76 m 内到达 `1312`，但 fresh 为 Drift、约 `0.14 m/s`，因此未把它计作陆地锚点。审计终态为 tick `10825701+`、revision `162`、planet `104`、位置约 `(-132.0117,-45.2252,-144.7226)`、Drift、核心约 `232.99/400 MJ`、healthy、无 blocker/checkpoint；journal `43/43` durable 且无 pending/error。10 个 action 均为 terminal/completed/succeeded，节点、背包与 71 格新带完整闭合；磁铁仓 `1217` 已积 `448`，矿机/熔炉满电工作。科技 `1414` 从 `127086 -> 130937/144000` 后停住，研究站此时红/黄仍有 `38070/36926` 点而蓝为 0，证明当前最早缺项已重新切到蓝矩阵。未出现 quarantine、outcome unknown、材料丢失、串料或未解释增量；EXP-007/018/021/028/061/066/073/120–122 与现场一致。本审计后写计数归零；下一写入先从当前 Drift 点做几米侧移恢复 Walk，再继续接通 `1312 -> storage 30`。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核并新增 EXP-121。前四项为输入带 `02f16ddf-db5f-49bb-b4f6-9814b2ab22c5`、输入 sorter `fcad3762-1070-4c52-9fbf-69e7bf052bdf`、补搓 1 个 sorter `0b61d5cb-d44c-4aec-87e6-91b79e719bc6` 和 30 条带 `38b6e51f-2b4c-4134-b0f9-52eb82fa6fca`；它们建立并供给 `1213 -> 1218…1222 -> 1223 -> 1216(recipe 2)`。下一唯一 9 格带施工正常形成 `1231 -> … -> 1232`，结果展示只因访问不存在的 `provedObjectIds` 字段报错；背包传送带 `39 -> 30`、唯一实体 `1224…1232` 和完整有向连接核销了已接受动作，没有重放。动作 `48c89f22-5a48-488d-a65e-c9db94c11aa9` / `18952c9d-d093-47fd-ba71-0126104b144a` 分别接成 `1216 -> 1233 -> 1231` 与 `1232 -> 1234 -> 1217`；动作 `78d604af-3985-4ece-868a-7389ae770bc1` 补搓 2 个 sorter。为避开直达旧蓝链方向上的风机/电塔，动作 `b793ccbd-146b-4dbb-b8e9-57e35156dfa3` 先向空旷侧建成 `1236 -> … -> 1239` 六格引出带，动作 `7623354b-310f-4ae8-92e7-b7f25e237ab8` 再接成 `1217 -> 1241 -> 1236`。fresh 审计为 tick `10763934+`、revision `142`、planet `104`、位置 `(-102.7192,-114.9241,-127.7968)`、Walk/0、核心约 `346.93/400 MJ`、healthy、无 blocker/checkpoint；journal `43/43` durable 且无 pending/error。network 1 为 `78592/78592`、ratio 1.0；六节点矿机 `1213`、熔炉 `1216` 及全部 sorter 满电，磁铁仓 `1217` 已从 9 增至 75，熔炉仍在工作，且 sorter `1241` 已实际携带磁铁。科技 `1414` 从 `118980 -> 127086/144000`，研究站最早缺项仍是红矩阵 10 点。全部 9 个可查询 action 均为 terminal/completed/succeeded；第 10 个是由唯一对象/库存/拓扑核销的展示后处理样本。未出现 Drift、quarantine、outcome unknown、材料丢失、串料或未解释增量；EXP-007/018/021/028/037/061/068/070/073/112/120/121 与现场一致。本审计后写计数归零，下一写入从末带 `1239` 继续向仓 `30` 延伸，同时处理红矩阵的油反压瓶颈。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核。矿机 prepare 比较全部原生合法姿态后选择覆盖铁节点 `35/36/37/39/42/43` 的 6 节点方案；唯一 build 动作正常完成为矿机 `1213`。结果展示脚本在已完成施工后错误地把对象数组直接强转为单个整数，只做 fresh miner 列表核销并确认背包矿机归零，没有重放。动作 `4603a85a-1df0-40ec-9ecf-728f605d74c3` 建成风机 `1214`；动作 `b5b6dd40-9f62-493e-9c67-58ecf9e9a691` 手搓电塔，动作 `b8f90f81-c677-4bdb-9619-f15d801499f4` 将其建为 `1215`，随后矿机进入 network 1、供电比 1.0、输出缓冲开始增长。动作 `2554abcb-084f-4548-9133-3597fd54f9a8` 从附近旧仓守恒取得 6 石矿，`b26a0a45-144a-419a-bd4a-feb3cd19a951` 递归手搓 1 座熔炉，`292ad5cf-1e00-4f82-a4bd-c05558e43494` 手搓 2 个 sorter。动作 `91279abc-e244-4494-a552-e949e193ce8d` 建成满电熔炉 `1216`，`1180d1bf-0b8c-47d9-9546-010dcfd224a1` 在空设备上配置磁铁 recipe `2`，`41672615-d332-4249-b9da-f06f6eb5beaf` 建成专用空仓 `1217`。fresh 审计为 tick `10719905+`、revision `122`、planet `104`、位置 `(-102.7348,-114.9059,-127.7944)`、Walk/0、核心约 `300.91/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。矿机 6 节点身份不变、满电工作、缓冲已达 50 铁矿；节点 `43` 在采集和自动开采后为 29739。风机与矿机/熔炉都归 network 1，网络总供电 `46574/46574`、ratio 1.0；空熔炉/仓尚未接料，未把合法建筑冒充产线完成。科技 `1414` 由上一审计 `115920 -> 118980/144000`；蓝/黄矩阵点仍有 `37540/39240`，当前最早缺项仍是红矩阵 10 点。材料、对象、配方、节点覆盖和能源均闭合，未出现 Drift、quarantine、outcome unknown、串料或未解释增量；EXP-007/018/021/028/037/062/068/070/095/112/120 仍适用。本审计后写计数归零，下一写入只建立 `1213 -> belt -> 1216(recipe 2) -> 1217` 的有向磁铁链并追到最终蓝糖/科技。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核并新增 EXP-120。动作 `6dcac25a-3038-4a18-b91a-c801a269c102` 从 `143` 绕开新仓/带密集区直达风机 `82`，复读 Walk/0；动作 `37c550a6-5a9c-4e1c-b900-7bafe41c2787` 从仓 `562` 守恒取 200 磁铁，下一唯一 transfer 将其全部装入空仓 `30`。该动作完成后的展示脚本只因玩家空磁铁集合读取 `.Sum` 报错，没有重放；fresh 两端复读闭合玩家 `200 -> 0`、仓 `30` 首读 187 且旧有向带已开始搬运。动作 `750b9cd2-dab5-4196-b9d4-c3d6469a6b8f` / `ed0dba0e-e300-4da8-9750-6f29a0f3419c` 分别守恒取得 20 电路板和 20 磁铁；`c0723f98-7242-4bff-b390-47e57aca20ed` 经普通 replicator 递归手搓 1 台矿机。动作 `64151b4f-3eda-45f8-9a72-973c15da642a` 正常抵达未采铁矿节点 `43` 外缘；下一唯一 harvest 在本地等待窗口返回前仍持续原生采集，外部只以 fresh 读回跟踪 `4 -> 41 -> 88 -> 104` 铁矿与节点 `29847 -> 29747`，没有重开采集。动作 `fe004435-8515-4f73-bef4-f564e9d73ee2` 把 100 铁矿普通手搓为铁块，闭合 `ore 104 -> 4`、`iron 1 -> 101`；动作 `01ae49e7-0c72-4537-8e69-8606c0865455` 再递归手搓 1 台风机，余 94 铁块、14 磁铁、1 磁线圈、18 电路板及各 1 台矿机/风机。审计终态为 tick `10704715+`、revision `103`、planet `104`、位置 `(-102.8847,-114.5563,-127.9786)`、Walk/0、核心约 `284.20/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。旧蓝链已把 200 磁铁逐层转为磁线圈和蓝矩阵，科技 `1414` 从 `97457 -> 115920/144000`；此刻研究站蓝/黄仍有 `37540/38160` 点，最早缺项转为红矩阵仅 10 点，红矩阵站 `256` 满电工作但供料间歇。未出现 Drift、quarantine、outcome unknown、材料丢失或未解释正增量；本审计后写计数归零，下一动作只在节点 `43` 的 fresh resource hash 上准备最大覆盖矿机，再补电和建设永久磁铁上游。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核并新增 EXP-119。前四项是金刚石支线末段、主干注入 sorter、源 sorter 和 tick `10637902` 普通保存，证据已由 EXP-118 闭合；后六项为从黄糖区返回蓝糖区的逐段普通移动。动作 `9c1df4c6-dbf4-4e9c-b6c4-a3e82ab10d99`、`c1caafce-4d53-43d1-83be-c75a1576ab1b`、`19692797-e2e9-4b4c-aea6-b4bec4f66cbd` 依次沿旧链到液罐 `165`、电塔 `182`、电塔 `143`，下一段到 `133` 也正常完成；随后动作 `cc32b8bd-6add-4046-8599-78740c8e0a93` 从 `133` 反向去风机 `82` 时因 180-tick 物理停滞在剩余 39.42 m 明确失败，且未重放。fresh 近邻读到新 PLS 末带/sorter、仓 `768` 和电塔基座距玩家仅 `0.84/1.76/2.52 m`；动作 `db3ed711-b10f-4531-9f2c-19e6f6407394` 只退回上一稳定锚点 `143`。审计终态为 tick `10673358+`、revision `87`、planet `104`、位置 `(-70.2035,-98.4685,-159.5020)`、Walk/0、核心约 `344.02/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。研究 `1414` 停在 `97457/144000` 的唯一直接缺项是研究站 `84` 蓝矩阵为 0；红/黄点仍为 `38070/39086`，黄糖设备 `774` 满电工作。蓝糖台 `76` 有 6 电路板但无磁线圈，磁线圈台 `73` 有铜无磁铁；仓 `562` 仍有 2869 磁铁，空仓 `30` 的既有输出带可把纯磁铁送回混合仓 `26` 和磁线圈台。未出现 Drift、能量枯竭、quarantine、outcome unknown、物品变化或未解释增量；本审计后写计数归零，下一动作只从新读状态绕开 `768/133` 密集区到蓝糖区，再用守恒转移恢复现有生产线。

- 2026-09-03：新增并验证 EXP-118。动作 `1cf7bf0c-46a4-4e82-a96a-7c4993a6e727` 完成金刚石支线最后 24 格，fresh 有向遍历确认 `1117 -> … -> 1210` 恰为 94 个唯一 belt；动作 `08b8fe1d-ffe4-4ad2-bba7-73d401635a8a` 建成 `1210 -> sorter 1211 -> trunk belt 1053`，动作 `20c31d1e-e34a-43bc-b96a-14d33578fc40` 建成 `storage 717 -> sorter 1212 -> belt 1117`。独立运行窗口中金刚石仓 `300 -> 145`、目标仓 `775` 保有 88 钛晶石/11 金刚石、lab `774` 以 `6/6` 工作、研究站 `84` 持有 8124 黄矩阵点并工作，科技 `1414` 从 `90000 -> 91303 -> 93121/144000`。普通保存动作 `eb58a029-d5c9-47d0-9b80-afcc682c28f7` 持久化 tick `10637902`；fresh 复读为 healthy、无 checkpoint、journal `43/43` durable。当前写计数自上一组 10 动作审计后为 4，并明确保留“源仓有限、上游已停”的边界；下一主线是完成当前科技并进入双星 ILS/运输船，而不是把本地缓存误报为 v0.3 持续性完成。

- 2026-09-03：完成上一审计后的下一组 10 个成功游戏写动作复核。动作 `87dbf164-dda4-420c-b86f-086d58464e46` 先沿局部切向移到距所有当前实体至少约 6.45 m 的开阔候选，避开精炼厂 `707` / 电塔 `711`；`299b423d-d762-47f2-a46f-998514404470` 随后正常到液罐 `165` 外缘，`abe6d8a3-e816-4add-94b2-779de360113a`、`dac736d0-b3c3-466e-80e5-591d1773902f`、`181d7dc0-4f44-4418-89bc-2fb24779b7a7` 沿已验证陆地锚点 `713 -> 120 -> 717` 抵达金刚石仓，全部动作后均独立复读 `Walk/0`。三次唯一带施工 `02802b4e-0876-48b9-b297-743d6005454f`、`3499497a-37c9-4394-83e3-d4713ea872f1`、`2b46c077-6327-4033-bc85-ccbc2caf5ca5` 依次以末端 endpoint hash 续接 23/24/23 格；fresh 有向遍历证明 `1117 -> … -> 1186` 恰为 70 个唯一 belt、无断点/环路，且首末端都尚未错误接到仓或钛晶石主干。为让最后一段回到 80 m 建造范围，动作 `8678be77-fd4d-4073-a3a4-c4419fa5ddae` / `ff9a3c3a-87aa-4110-95a4-911af0c8d0b2` 只沿原路 `717 -> 120 -> 713` 返回，均为 Walk/0。fresh 审计为 tick `10611490+`、revision `69`、planet `104`、位置 `(3.0401,-115.6197,-163.4361)`、Walk/0、核心约 `302.75/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。背包仍有 7 铁块、2 分拣器，传送带由 108 精确减至 38，与 70 格施工完全闭合；钻石仓 `717` 的 300 个自动金刚石未动。科技 `1414` 已由上一审计 `89640 -> 90000/144000`；研究站红矩阵点已恢复到 36010、蓝为 37540，但黄矩阵归零，故当前唯一研究门槛回到黄糖。黄糖输入仓仍有 94 钛晶石、设备有 6，金刚石为 0；没有把前三段空带误报为产线恢复。未出现 Drift、碰撞失败、quarantine、outcome unknown、材料丢失、串料或未解释正增量；EXP-007/009/018/021/028/036/061/066/068/070/073/079/090/116 与局部脱困、已验证锚点、材料守恒、唯一带路和最终消费者证据一致，计数在本审计后归零。下一写入只从 belt `1186` 续接最后一段到主干 `1053` 附近，再以两个端点 sorter 让仓 `717` 的金刚石实际进入黄糖站并验收科技继续增长。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并把有明确终态的浅水/碰撞失败保守计入风险窗口。动作 `5918b792-529e-4a38-ac40-e70720a027d3` 建成 `belt 1114 -> sorter 1116 -> storage 775`；独立复读看到 sorter 实际携钛晶石、仓和黄糖站输入分别出现 6/6，稍后 100 个源仓存量全部守恒转入在途带、仓和设备，最终仓 `775` 保有 94、设备保有 6。动作 `3fa251b0-fb17-4650-aa28-7d2e7d0edd68` 从仍在自动出料的铁仓取得稳定窗口并守恒转移 100 铁块，`88d02b0a-5eac-4e8d-86f3-4df3cd555023` 递归手搓 99 条带；材料精确闭合为原有 6 铁/9 带加本批 100 铁，手搓后背包 7 铁、108 带，既有 2 个 sorter 不变。移动 `7461e6bb-ccb4-498e-8f5e-483d04f11324` 用 5 m 容差去锚点 `143`，动作虽 completed，3 秒后却在邻接浅水中为 Drift；回退 `f9d00301-9845-4575-b610-0c61acd5e860` 的 3 m 容差也在水面提前完成。动作 `c549a315-7c8c-4852-9dd1-f42eb592cc58` 只把同一已知 Walk 坐标收紧到 0.5 m，随后复读才恢复 Walk/0；全部未接受尝试都只有无 action ID 的明确 stale。再去 `143` 的动作 `bba2d0b6-c5a5-4338-93ec-00f62842393c` 在距塔 0.97 m 时由 181-tick 看门狗明确失败，但 fresh 已是陆地 Walk/0，故没有继续撞塔；错误改走抽水站夹缝的 `7cc2f79d-9cc9-474c-82b1-77b52bc78cf2` 又在余 8.12 m 时终止。回到旧验证链后，`144db904-fe07-43ee-8fa7-86d9d17b8786` 正常抵达锚点 `182` 外缘；第 10 项 `55ef3c4e-081a-45b9-b155-f2ace44e71e2` 向液罐 `165` 前进后在余 13.89 m 由看门狗停止。fresh 审计为 tick `10577957+`、revision `49`、planet `104`、位置 `(-62.8944,-117.1507,-149.6881)`、Walk/0、核心约 `309.90/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。当前位置紧邻电塔 `711` 和精炼厂 `707`，解释最后一次停滞但不是能量或写隔离。科技 `1414` 已由上一审计 `81720 -> 89640/144000`；研究站当前蓝/红/黄点为 `37540/10/720`，红矩阵生产站 `256` 有 6 高能石墨但仅 1 氢，故短期科研瓶颈是氢→红矩阵，黄糖永久线的剩余瓶颈则仍是 300 金刚石所在仓 `717` 尚未接入。未出现 quarantine、outcome unknown、物品丢失、串料或未解释正增量；EXP-007/009/018/021/028/036/048/053/061/066/073/079/090/116/117 与动作终态、材料守恒、Drift 回收、碰撞假阴性和最终设备缓冲一致，计数在本审计后归零。下一写入从当前 Walk 点绕开 `707/711`，沿已验证的 `165 外缘 -> 713 -> 120 -> 717` 陆地链到金刚石仓；不重放本批任何失败目标。

- 2026-09-03：修订 EXP-116。直切旧陆地锚点再次进入浅水 Drift；返回刚离开的已知 Walk 坐标时，`3 m` 到达容差在距目标 2.91 m 的水面提前完成并被动作后复读否决，只有 `0.5 m` 容差把机甲送到距目标 0.45 m、3 秒后稳定 `Walk/0` 的位置。所有 Drift 状态竞争只在无 action ID 的明确 stale 上重读；两个已接受回退动作均未重放。下一次游戏写入前已把“窄海岸回退必须收紧容差并复读”的反例和正例落盘。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核。动作 `e6a2acb6-b4b7-41a4-ad4a-7be4489ba6d2` / `d20f5e6f-f736-4302-ac51-6f5095762763` 分别建成 `829 -> 1027 -> belt 1018` 与 `belt 1026 -> 1028 -> 28`，双端槽位、方向和实体身份完整，末端 sorter 实际携带铁块；研究科技 `1414` 随后从 `64157 -> 65775` 并继续增长，证明仓 `28` 虽被既有出口即时抽空，电路板、蓝矩阵和最终研究消费者均已恢复。为建设黄糖持续供料，活跃铁仓第一次 transfer commit 在无 action ID 的明确 `STALE_STATE` 上终止，没有重放；随后只重读完整玩家/仓状态并在新的稳定窗口由动作 `b0292f87-4a8c-4354-b527-f2666fd6ba18` 守恒取得 100 铁块（接受时仓 `1258 -> 1158`），动作 `0d718f8a-8885-4c75-901f-49e520ff6712` 又从静态混合仓守恒取得 4 电路板。普通 replicator 动作 `9f2e6d93-69e4-4573-8baf-18e525cd6c82` / `37a470b7-5a00-4e23-828f-6e1ac5cceff9` 分别递归手搓 90 条带和 4 个分拣器；三次唯一施工 `40a1f61d-927f-435b-9629-fb294ad6254c`、`fa64e063-1315-438b-9f44-a9481a223f16`、`4b607db3-a58a-4e9f-846b-5e93d5a566a3` 依次用前段末端 endpoint hash 续接 29/28/29 段，fresh 有向遍历证明从 `1057` 到 `1114` 恰为 86 个唯一 belt、无断点/环路；动作 `069c680d-26c6-4e9d-9655-432bf6a8dcab` 再建成 `storage 769 -> sorter 1115 -> belt 1057`。材料闭合为 100 铁块转成 90 条带、4 分拣器并余 6 铁块，原有 5 条带合并后施工 86 段余 9，源 sorter 消耗 1 后余 3；4 电路板归零。fresh 审计为 tick `10538657+`、revision `33`、planet `104`、Walk/0、核心约 `380.66/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。铁回流持续工作，自动铁仓在另行守恒取料和产线并发消费后为 876；科技已增至 `81720/144000`，当前研究站蓝矩阵点为 37540，但红矩阵只余 10，故此刻停止原因已从蓝断供切换为红断供。钛晶石主干源侧接通后，仓 `769` 从 100 降至 64，其余物料在带上守恒前进；末带 `1114` 尚未连接、黄糖输入仓 `775` 仍为空，因此没有把“主干带货”误报成黄糖恢复。PLS `916` 钛块已耗至 0、当前无运输订单，钛晶石台另因有机晶体为 0 暂停；这两个后续供给缺口也未被现有 64 仓存和 86 格在途缓存掩盖。未出现 quarantine、outcome unknown、串料或未解释物品正增量；EXP-007/018/021/028/036/048/062/068/070/073/079/097/110/115/117 与 stale-only 重试、材料守恒、端点续接和最终消费者证据仍一致，计数在本审计后归零。下一写入只接 `belt 1114 -> storage 775`，验证钛晶石进入黄糖设备后再处理金刚石永久供料和红矩阵新瓶颈。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核。第 1 项恢复动作 `2d524b3d-63f2-4f16-a083-7d5708e15390` 在主档 tick `10449537` 正常保存、旧进程正常退出、同批 Release Plugin/Core/Contracts 逐文件哈希一致部署后，只采用 ticket-bound exact primary；第 2 项 `26a83d4b-ebf2-49b8-9d06-5e6c1321b78f` 把 PLS `916` port 0 的钛块选择器 raw `0 -> 1`，第 3 项 `e963b45e-b849-4523-a0fa-81c3e2af13da` 正常保存已验证流量到 tick `10456408`。后 7 项为铁锭自动回流施工：动作 `0d25e555-8663-4160-9de0-579db11c2559` / `ecedc6e3-af73-48ab-8d9a-e98f500dfab0` 守恒取得 5 铁块/2 电路板，普通 replicator 动作 `866b8a1a-3477-4e18-a06b-5d3042b24723` / `447bc762-fc83-4004-b613-2334fae85ed7` 分别手搓 1 批传送带和 2 个分拣器；动作 `dc5ac7da-01f6-442f-b9ba-bab21fe33969` 再守恒取得 6 铁块，`a91f0837-24f4-4475-938c-504131a96df9` 手搓 2 批传送带，施工动作 `cf630266-c362-4568-8e21-5c1f41b00abd` 消耗 10 条带并形成唯一有向链 `1018 -> 1017 -> 1019 -> … -> 1026`。材料闭合为自动铁仓 `829` 的铁块 `1394 -> 1383`，11 铁块与 2 电路板全部由原生递归配方转换成 9 条带和 2 个分拣器；连同原有 6 条带再施工 10 段后，背包精确剩余 5 条带、2 个分拣器且无铁块/电路板。fresh 审计为 tick `10485860+`、revision `15`、planet `104`、Walk/0、位置 `(-78.1934,-59.8855,-174.1149)`、核心 `400/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用；journal `43/43` durable 且无 pending/error。PLS `916` 钛块 87、1 架运输机工作且 port 0 仍为 Output/raw 1，供应站 `918` 保有 91；钛晶石台 `767` 持续工作，专仓 `769` 已由上次 32 自然增至 95。科技 `1414` 停在 `64157/144000`，研究站 `84` 的红/黄矩阵点仍分别为 `38070/37286`，唯一缺项是蓝矩阵为 0；电路板台 `36` 缺铁、蓝矩阵站 `76` 缺电路板。仓 `829 -> 28` 的 10 段带尚未接任何新 sorter，故没有把“合法带路”误报成恢复供料。未出现 quarantine、outcome unknown、串料或未解释物品正增量；EXP-007/018/021/028/036/048/062/068/070/073/079/097/110/115/117 与恢复、选择器、保存、手搓守恒、施工拓扑和生产/科研终态仍一致，计数在本审计后归零。下一写入只接 `829 -> belt 1018` 与 `belt 1026 -> 28` 两个 sorter，并以仓 `28` 铁块增长、电路板/蓝矩阵/科技上传依次恢复验收。

- 2026-09-03：EXP-117 升级为 `validated`。主档正常保存到 tick `10449537` 后正常关闭，114-test 同批 DLL 逐文件哈希一致部署；恢复动作只采用该精确主档。唯一 selector commit 把 PLS `916` port 0 raw `0 -> 1`，站存 `100 -> 22`、钛 sorter 实际携货、制造台 `767` 工作，独立周期后专仓 `769` 达到 9 钛晶石，需求塔无人机同步补货；普通保存 tick `10456408` 覆盖该状态。

- 2026-09-03：新增 EXP-117。PLS `916 -> belts 1015…998 -> sorter 1016 -> storage 768` 的实体拓扑完整但 raw 输出选择器仍为 0，10 秒独立窗口没有一件钛块流出；当前程序集进一步证明输出循环采用 `storageIdx-1`，UI 以 None+仓槽列表的一基索引写同一字段。源码将最小安全子集接入现有 configure prepare/commit，离线完整 solution 0 warning/0 error、114 tests passed；部署和实机流量复验前保持 observed。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核。移动 `38c2489a-e554-4d68-b0f0-3bec2608396d` 抵达仓 `287` 外缘；直切仓 `286` 的 `43e94e5d-3092-40ea-a56c-6356085a5976` 与侧向 `0f056085-d422-4168-b204-00b76a3a9f3c` 均被 180-tick 位移看门狗明确终止，未重放。动作 `d226d4df-4a40-48a8-816c-12a4a6ac845a` 沿已走路径退回，随后 `a121608a-45e1-4fe6-a798-58cbe17f672d`、`38a9735a-f2dc-416d-86c8-496e3be31c90`、`646d8ee0-6e93-4a5c-8a63-2f41b919da49` 从仓基座南侧绕到距 PLS `916` 约 66 m 的稳定 Walk 建造位。动作 `d73fcc41-a1a4-4b9c-b565-b13375aa6edf` 守恒取得 30 铁块，普通 replicator 动作 `ebf3f3b1-c90b-4e41-8fcc-8133f8fef2ee` 递归手搓 30 传送带；长施工动作 `50634721-2bbe-4145-86b1-593666cead62` 唯一提交 36 段 PLS 出料带并在本地等待期间持续复读预建筑 `15 -> 3 -> 0`，没有因未立即返回 action ID 而重放。fresh 审计为 tick `10403369`、revision `103`、planet `104`、Walk/0、位置 `(-78.1934,-59.8855,-174.1149)`、核心约 `316.14/400 MJ`、healthy、无 blocker/checkpoint、restart 可用，journal `43/43` durable 且无 pending/error。站 `916` 满电、10 idle/0 working、钛块槽仍为 100；belt slot `0` 已读为 `Output`、`storageIndex=0`、首带 `1015`，有向拓扑唯一连续 `1015 -> … -> 998` 共 36 段，末端距钛晶石输入仓 `768` 约 3 m，背包传送带精确 `42 -> 6`。钛块仓 `531` 已自然增至 268，科技 `1414` 由上一审计 `37318` 增至 `54494/144000`；此刻研究站 `84` 因红矩阵只余 10 内部点而停止，蓝输入修复仍有效。EXP-007/018/021/036/061/066/070/073/080/100/110/115/116 与本批动作终态、材料守恒、长施工唯一性、站点槽和科研缓冲一致；计数在本审计后归零。下一写入只接 `belt 998 -> storage 768` sorter 并以 PLS 钛库存下降、仓/制造台钛输入和钛晶石产出验收。

- 2026-09-03：完成上一审计后的下一组 10 个已接受游戏写动作复核，并保守地把有明确终态的失败/Drift 移动也计入风险窗口。动作 `7c59f955-830e-408c-8c8d-dfb821b83e40` 在混合仓 `26 -> 制造台 36` 建成 sorter `979`，随后动作 `e75b317c-9cd0-48d5-a753-6d500e03240c` 在空载 Picking 窗口锁定铜块 filter `1104`；动作 `f287c250-bbd3-42a4-990d-a07c0a356131` / `fc14a311-f9bd-4c14-a957-40daeb8e065b` 又把 500 铁块从自动仓 `829` 守恒补入原铁带源仓 `28`。电路板台 `36`、蓝矩阵站 `76` 与研究站 `84` 随后依次恢复，科技 `1414` 从 `21317` 推进到 `37318/144000`。移动方面，向风机 `35` 的订单 `ef9b4ed7-88c1-4bb9-bf85-d250f1c67552` 在矿机 `796` 基座旁以 180-tick/0.75 m 看门狗明确失败并只取消自身；短距反向脱离 `2a389995-6111-4e81-a583-b6d81b6af7e5` 与下一步 `406a664d-1a4c-4d4b-886f-2ddd9be95e7c` 均稳定 Walk。再向开阔方向的 `49e78f75-5606-4489-a4ef-2feac44e5795` 进入 Drift 后立即停止路线，唯一回退 `1e1b57e2-a00f-4d29-bb36-b54b12b91682` 回到干地，最后 `60e5252f-1ec2-4366-b0e6-2a5d01ab4c20` 沿工厂内侧稳定前进。fresh 审计为 tick `10370577`、revision `86`、planet `104`、Walk/0、位置 `(-72.2350,-54.6047,-178.5835)`、核心约 `321.45/400 MJ`、healthy、无 blocker/checkpoint、restart 可用，journal `43/43` durable 且无 pending/error。钛矿仓 `259` 尚有 601、钛块仓 `531` 已自然增至 131；未出现 quarantine、outcome unknown、串料或未解释物品正增量。EXP-007/021/028/036/053/058/061/066/068/073/080/090/102/115/116 与本批过滤、上游恢复、科研增长、看门狗和 Drift 回退证据一致；计数在本审计后归零。

- 2026-09-03：完成上一审计后的下一组 10 个成功游戏写动作复核。前五项为海面/地形处理和钛晶石续料：首段普通 Move 进入 Drift 后立即取消剩余路线、唯一接受的回退 Move `07296b3e-9299-41a1-96e5-43ba4510ef36` 恢复到已验证陆地点、Move `6686941c-b586-4b23-83e1-3e05be966ab6` 到仓 `773` 外缘，以及有机晶体 `storage 762 -> player -> storage 768` 的两段守恒转移 `3b0b836a-583c-4720-970e-1dffc1c4323a` / `39563ce2-dc1d-47cd-aa8b-d73b550bb7e0`。后五项为蓝矩阵断供修复备料：动作 `b9ba78e9-f3d6-4081-8469-10decf3c6b5f`、`670a6437-fa04-495c-af29-8abc47a464c0`、`39bf553d-6838-4605-a575-0358f901738a` 分别守恒取得 4 石材、5 铁块、1 电路板；普通 replicator 动作 `b2c69fce-4a4f-4d2f-b6d8-353ca03d4823` / `b1ee2b86-e91a-489c-9882-59d63cc91936` 手搓 1 小型储物仓和 1 分拣器。fresh 审计为 tick `10342003`、revision `69`、planet `104`、Walk/0、核心约 `304.19/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用，journal `43/43` durable 且无 pending/error。钛矿仓 `259` 仍有 840、熔炉 `530(recipe 65)` 满电工作、输出仓 `531` 已出现 12 钛块；钛晶石仓 `768` 保有 98 有机晶体且制造台 `767` 等待钛块。研究 `1414` 停在 `21317/144000` 的直接上游原因已收敛为电路板制造台 `36(recipe 50)` 缺少持续铜输入，蓝矩阵站 `76` 因电路板为零停止；下一写入只把新纯铜缓冲仓落在制造台附近并完成过滤 sorter，不能从混合仓直接无过滤供料。EXP-007/018/021/028/037/053/058/061/066/080/090/103/115/116 与动作终态、守恒、设备缓冲和研究停点一致；计数在本审计后归零。

- 2026-09-03：新增 EXP-116。普通地面路线在首段落水后没有继续剩余 2 段；回退动作只对没有 action ID 的 prepare/commit stale 做 fresh 重试，最终由唯一已接受动作恢复到刚离开的陆地点。该经验已在下一次游戏写入前落盘；下一步只沿近距离、已有建筑证明的陆地锚点移动到仓 `773` 外缘，不再重复原跨水直线。

- 2026-09-03：完成上一审计后的 10 个成功游戏写动作复核并新增 EXP-115。动作 `f465733e-ac32-4bfd-95b3-55fd5b6413f4` 建成 `778 -> 941 -> 939`，证明独立站仅有黄矩阵仍不研究；35 段普通带施工的本地等待输出在 30 秒后结束而未返回 action ID，未重放，fresh 读回以背包传送带 `47 -> 12`、预建筑全部消失、唯一连续实体 `942 -> … -> 976` 和 revision `41` 核销；动作 `192a190b-34e8-46f8-99f8-a7a6a30000c6` 建成 `778 -> 977 -> 942`。随后动作 `c8c5bb00-68db-445e-8c45-4b095e5c3bde` / `7fac5eda-2775-4705-ad74-0a80931aaa3f` 守恒取 1 铁块/1 电路板，动作 `a101ab09-31c0-41df-b6a7-25c156b9ca0d` 通过普通 replicator 手搓 1 sorter，动作 `eb755bf9-6ea8-44ae-9d84-59a8b2bdae7e` 建成 `976 -> 978 -> 84`。研究站 `84` 三矩阵同站后满电工作，`1414` 由 `0 -> 1181 -> 6523/144000`；站 `939` 则保有 36000 黄矩阵点但继续停止。最后动作 `b325830f-8eed-44e8-ba11-92e453758615` / `d80e6f66-08c2-4c65-a29d-435a3880268c` 把返航的 1100 钛石从混合仓 `899` 守恒转入钛冶炼输入仓 `259`，熔炉 `530` 已满供电恢复工作；第 10 项保存动作 `25800a05-d63b-4489-b2e3-4d259a6b44e5` 确认 tick `10312259`。fresh 审计为 revision `52`、planet `104`、Walk/0、核心 `400/400 MJ`、healthy、无 blocker/checkpoint、exact-primary restart 可用，journal `43/43` durable 且无 pending/error；钛矿仓已由 1100 降至 1080 并持续冶炼，651 硅石仍完整。EXP-007/018/021/028/037/048/053/062/079/080/090/100/103/115 与唯一施工、守恒、端点、研究缓冲和普通保存证据一致；计数在本审计后归零。

- 2026-09-03：完成永久黄糖研究输入施工前的下一组 10 个成功写动作复核：正常手搓 2 个 sorter `6ac91008-cb8e-4f66-b333-d682a509eb81`；1100 钛石和动作 `a2e623d6-91aa-4ea1-b375-dbdc8a192dcf` 的 651 硅石分别守恒转存到仓 `899`；动作 `285b82ab-a52f-42c1-983a-69e37513a925` 取 8 石矿；动作 `5bde5886-b1cc-441e-a8c0-e784ece05739` 由普通 replicator 递归手搓矩阵站；原生施工矩阵站 `939`；动作 `5a87ddcc-a307-4e5c-9aa4-93f87987d879` 取 1 铁块；动作 `12382aad-8774-47a0-87cf-916f34df2ac8` 手搓电塔；动作 `fc09c935-0f1c-4dad-87d1-bfc8f5817afd` 施工电塔 `940`；动作 `d9c4a6f9-bab7-405a-ae41-059a28aa57c5` 把空矩阵站 `939` 配置为当前科技研究模式。两次展示后处理分别因空聚合 `.Sum` 与不存在的 `createdObjectIds` 字段报错，均未重放；fresh 读回证明仓 `899` 精确保有 1100/651 原矿，站 `939` 唯一位于计划坐标且背包站体消失。新站初建时 network 0，故没有把“合法落位”冒充可用；电塔 `940` 完工后站体为 network 1、serve ratio `1.0`。审计终态 revision `37`、tick `10270860+`、planet `104`、Walk/0、核心约 `383.78/400 MJ`、healthy、无 blocker/checkpoint、restart ticket 可用，journal `43/43` durable。黄糖线已把输入全部转换为仓 `778` 的 60 与机甲缓冲 40，研究站 `939` 仍为空，因此 `1414` 尚为 0；下一写入只建立纯黄糖源仓 `778 -> 939` sorter。EXP-007/018/021/023/037/042/045/062/077/079/095/103 与递归手搓、唯一施工、补电、模式配置、守恒和研究边界一致；计数在本审计后归零。

- 2026-09-03：完成海岸返回后的下一组 10 个成功写动作复核：Move `68180f9f-417e-442b-9ad8-46d07335dfd4` 从煤脉到黄糖输入仓；60 金刚石守恒进入仓 `775`；Move `ac8e1dce-e145-4776-83ce-1d42e0074160` 到钛晶石仓；动作 `37341112-342b-4652-8fd5-ae7f6698415e` 取出 60 钛晶石；Move `583cde57-96fa-4b37-849f-f9d67ab3fe38` 返回；动作 `35d65497-6e4e-49cc-b3e6-27417adbf7ab` 把 60 钛晶石装入双过滤输入仓；动作 `6f430e7c-4b75-42fc-90ab-8cc42a6721fc` 取出首批 40 黄糖；动作 `38ecdbc2-2733-4e7e-94db-c7091cee5a44` 取出 60 铁块；普通 replicator 用 45 铁块递归手搓 45 条带；动作 `ff3e2d1c-3ae9-4498-8b0d-d8252b4fc4f9` 取出 2 电路板。金刚石转移的展示脚本因目标物已从玩家消失而访问空对象 `.count`，手搓动作则超过本地 30 秒首次输出窗口；两次都没有重放，fresh 双边/队列读回分别闭合 `60 = storage 55 + lab 5` 与 `iron 60 -> 15`、`belt 2 -> 47`。审计终态为 revision `21`、tick `10246010+`、planet `104`、Walk/0、write health healthy、无 blocker/checkpoint、restart ticket 可用；1100 钛石/651 硅石不变，journal `43/43` durable。黄糖线满电工作，源仓金刚石/钛晶石各 23、设备各缓存 5、输出仓 29；首批 40 黄糖已在机甲缓冲，但 research lab `84` 黄糖仍为 0，故 `1414` 尚未上传。EXP-007/018/021/028/037/062/079/080/103 与动作守恒、异步结果、产线增长和研究缓冲边界一致；计数归零后继续永久黄糖研究输入。

- 2026-09-03：完成上一审计后的第 10 个已接受游戏写动作复核。该窗口包含两次明确进入 `recovery_required` 的返航 `1555c331-b5fb-4f29-b0ee-aa524304fcc7` / `0e913bc5-1b70-4ad1-afcd-62c522075547`、每次只重载同一 tick-`10173149` checkpoint 的对应恢复、部署后精确恢复 `155f95a7-4c80-4e19-800c-f6f29f0581a1`、成功返航 `f6b60009-3b77-4b34-b14c-fd89d4b13d71`、普通保存 `a3944276-ad65-4e04-b680-4e267e26b056`，以及用于验证海面靠岸/返回生产区的有界原生移动；第十项为直达已验证煤脉 `363` 的 Move `187f3954-5e25-4745-8fff-8eccd20584a0`。fresh 复读为 revision `7`、tick `10215751+`、planet `104`、Walk/0、位置 `(102.214478,-15.4884872,-171.470215)`、核心约 `382.23/400 MJ`、write health healthy、无 blocker、无 flight checkpoint、exact-primary restart 可用；背包 1100 钛石/651 硅石/60 金刚石与 39/40 槽占用守恒。journal `43/43` durable、无 pending/error；研究 `1414` 的 hash 仍为 0，研究站蓝/红库存保持正数、黄矩阵为 0，黄糖仓 `778` 仍为 40。EXP-007/009/021/035–037/047/051–053/061/066/080/082–084/113/114 与当前失败分类、精确恢复、靠岸、保存和移动终态一致；计数在本审计后归零，下一写入可继续生产供料。

- 2026-09-03：EXP-111 升级为 `validated`。旧条目的“仅钛前哨完成、硅线待建”限制已经被后续实机证据推翻：硅矿机正常拆除并从两节点重建为四节点，满供电有向出料仓独立增长到 287；钛/硅两条远端线同时保留，1100/651 原矿返航守恒并保存到 tick `10182419`。当前边界改为“两座矿业前哨完成，但 ILS/运输船尚未完成持续跨星物流”。

- 2026-09-03：EXP-114 升级为 `validated`。正常关闭旧进程后，同批 Release Plugin/Core/Contracts 经 SHA-256 一致性校验部署；恢复动作 `155f95a7-4c80-4e19-800c-f6f29f0581a1` 只载入原 tick `10173149` checkpoint。返航 `f6b60009-3b77-4b34-b14c-fd89d4b13d71` 在海面 Drift 自动选择 24.6 m 外干燥邻域，首个原生 MoveTo 即转入 Walk/0 并保持 600 tick；10 秒后位置仍完全一致，1100 钛石/651 硅石守恒。普通保存 `a3944276-ad65-4e04-b680-4e267e26b056` 确认 tick `10182419`、revision `5`、healthy，并退役该 checkpoint。用户对根因的判断得到实机证实。

- 2026-09-03：新增 EXP-114，并修订 EXP-113 的因果边界。用户确认失败航迹其实已落到目标星球海面，手动横走两步就能上岸；当前 DLL 也证明 Drift 会消费普通 MoveTo。源码因此改为从原生地形高度选最近干燥邻域并签发最多 3 个有界订单，同时保留订单所有权、停滞、断能、总着陆超时和精确 checkpoint 回滚。离线完整 solution 0 warning / 0 error、112 tests passed；部署前仍只标记 observed。

- 2026-09-03：EXP-113 增加第二个同检查点样本。动作 `0e913bc5-1b70-4ad1-afcd-62c522075547` 在精确恢复后再次原生起飞，同样接触 planet `104` 表面但没有达成连续 600 tick Walk，于 tick `10188952` 明确进入 `recovery_required`。当时曾保守计划补一栈煤再试；用户随后确认落点就在海面、横走两步即可上岸，因此该补煤计划被新证据降为备选，首选改为 EXP-114 的有界原生靠岸控制。任一失败仍只回 tick `10173149` 的同一 checkpoint，这不是宣称固定氢阈值或放弃失败档。

- 2026-09-03：新增 EXP-113。返航动作 `1555c331-b5fb-4f29-b0ee-aa524304fcc7` 已在原生起飞前确认 tick `10173149` 的独立检查点，但 400 MJ 核心加 49 氢只足以在归零后接触 planet `104` 表面，未能在有界窗口内从 Drift 收敛到连续 600 tick Walk。本次不写主档、不宣称到达；恢复动作 `719d56d0-cd8f-485c-8c44-7309aba9b5a5` 已仅载入票据内同一 checkpoint，完整恢复负载/能源并保留重试 capability。

- 2026-09-03：完成上一次提前审计后的 10 个成功游戏写动作复核：Move `a9bc0d39-6845-46b6-90d3-2a3c2157aff8` 到铁脉、手采 10 铁 `b9dfc8e3-f0db-4799-901e-26260006710d`、手搓电塔 `689ee3e3-f217-46d4-aa41-dd7d1749855a`、Move `886f626d-5a7d-47ad-8849-1f5f4bc8499b` 返回硅矿锦标、原生施工电塔 `c0a68108-f2fb-4726-b8d6-97e6304f4a62`、硅矿里程碑保存 `73f7dd77-e057-46ce-8156-0b7b3a3736f3`、三次原生单栈加氢 `6b60c009-a2ea-4810-9380-7e90777943ca` / `c915f3f2-e0df-4d9d-b8e3-d2084c8e818c` / `df0595b8-6d36-4a70-af88-386a0d82a51e`，以及仓 `15 -> player` 守恒转移 1100 钛石 `d87f3741-67c8-4762-b2ce-8d5356dbad94`。首个请求一次加注 49 氢被 prepare 以“原生本次只移动一栈 20”安全拒绝，无 commit；随后精确 `20 + 20 + 9` 使背包氢 `49 -> 0`、燃料格 `0 -> 49`，总数不变。fresh 复读为 session revision `30`、tick `10169058+`、planet `102`、Walk/0、核心 `400/400 MJ`、healthy、journal `43/43` durable、无 flight checkpoint；玩家背包有 1100 钛石、651 硅石且仍留 1 个空格，钛仓仍有 1090 且矿机持续供料。EXP-007/018/021/028/047/080/111/112 与当前动作守恒、航行能源、远端供料和保存边界一致；审计后才允许创建返航独立检查点并起飞。

- 2026-09-02：EXP-112 升级为 `validated`。live 动作正常回收旧矿机及 50 个内部硅石，新 prepare 给出 `245/249/252/256` 四点覆盖且完工集合精确一致；两台风机与电塔 `27/42` 使矿机/sorter 满供电，12 段新带接回旧主干后仓 `25` 从空仓增长到 50。普通保存动作 `73f7dd77-e057-46ce-8156-0b7b3a3736f3` 确认 tick `10126918`、revision `26`、healthy。journal 保持 `43/43` 而未造新事件，因为硅石生产寄存器在本次远端矿机前已有历史；保留真实边界而不把“本次新矿机”冒充“本局第一次产线硅石”。

- 2026-09-02：在上一批 10-write 审计后提前完成部署/重摆批次复核：普通保存把旧姿态保护到 tick `10087465`、revision `75`；旧进程正常退出后同批 Release DLL 哈希逐一匹配安装，动作 `3ea8d575-bdda-4a25-9711-caa5e164187d` 只消费受保护票据并恢复 planet `102` 的精确主档。动作 `94e0416f-3092-490f-a7d9-664ec0ed0535` 通过 `DoDismantleObject` 正常拆除矿机 `17`，背包矿机 `0 -> 1`、硅石 `600 -> 650`，原实体读回 `INVALID_ENTITY`；动作 `eb8a4a5c-eab2-492c-8c26-7aff79df316e` 随后以 yaw `150°` 重建同号实体，计划和实际节点均为 `245/249/252/256`，相对旧 `252/256` 翻倍且健康。风机 `29` 使矿机 network 2 恢复 service `1.0`；三次普通 replicator 动作共产出 12 条带，其中前两次 raw payload 误用未知 `craftCount` 字段而各安全采用 contract 默认 `count=1`，第三次显式 `count=2`，无重复动作或不明差量。动作 `342cfa7d-a5d7-4032-bcec-670e57d37947` 以 12 条新带恢复 `17 -> 30…39 -> 18…24 -> 26 -> 25` 有向拓扑。fresh revision `15`、tick `10108940`、Walk/0、核心约 `399.07/400 MJ`、healthy、journal `43/43` durable；唯一未闭环项是 sorter `26` 仍为 network `0`，矿机缓存因而满 50、仓 `25` 为空。本次在下一次移动/采矿写入前主动审计并重置计数；EXP-007/018/021/028/037/042/045/068/070/111–112 与恢复、守恒、覆盖、端点及电力读回一致。

- 2026-09-02：新增 EXP-112。用户在硅矿现场指出矿机 `17` 只覆盖 `252/256` 的角度浪费；根因复核为自动选址在第一个通过原生校验的候选处提前返回。源码改为比较全部合法候选并最大化 native preview 节点数，prepare/完工均绑定完整节点集；同时依据当前程序集的 `DoDismantleObject` 正常业务路径补齐仅限资源矿机的两阶段拆除与精确回收验证。完整 solution 0 warning/0 error，105 tests passed；同一主档已正常保存到 tick `10087465`、revision `75`、healthy 并签发计划重启票据，等待正常关闭、同批部署和 live 重摆后再升级为 validated。

- 2026-09-02：完成上一审计后的 10 个成功游戏写动作复核：手搓小型储物仓 `f4758135-c584-4991-8344-2f5b313047e9`、手搓 9 条传送带 `99de5db7-49c8-41f4-9344-f1103bffd226`、手搓电塔 `f63def21-0e92-4f2b-8171-34b09c631131`、Move `755f4f08-6528-4cdc-a7db-e78e88758707` 到硅脉、原生施工矿机 `7c080df4-479b-4377-adfc-d8c337cda812`、7 格矿机出料带 `9d34001e-a325-40c1-afa9-1fb60bc74e06`、末端仓 `7e052aad-a154-484e-a293-b578e41d6031`、`24 -> 26 -> 25` sorter `1759dcf8-0e79-4446-b286-c305c333fd24`、近矿机电塔 `3ceb95c7-6be4-4b87-ad59-94fda7980818` 与首台风机 `f215b9f8-5de7-4890-a3dc-9cd18f88f37f`。仓位第一候选被现有 belt `19` 的精确碰撞安全拒绝且无 commit，反侧候选才施工。fresh session revision `74`、tick `10043810`、planet `102`、Walk/速度 0、核心约 `320.80/400 MJ`、healthy、last save tick `9997352`、无 flight checkpoint；journal `43/43` durable、无 pending/error。新矿机 `17` 仅覆盖硅脉 `252/256`，network 2 在单风机下仅有 `5500/7000` energy per tick、serve `0.7857`，矿机缓存已有 13 硅但末端 sorter 尚未入网、仓 `25` 仍空。用户据现场覆盖明确否决该矿机角度；因此本复核不把硅线标为完成，并在第 10 次写后暂停第二风机，下一写入前先解决普通拆除/返还与带角度重建，不能用“合法落位”替代矿点覆盖效率验收。EXP-007/018/021/028/037/042/045/070/111 的守恒、端点、选址与完整前哨前提仍成立，但 EXP-111 尚不能升级为 validated。

- 2026-09-02：完成远端钛矿里程碑前后的 10 个成功写动作审计：风机 `3/4`、近矿机电塔 `5`、预估出口侧工具仓 `6`、矿机原生出料带 `7…14`、按真实自由端放置的出料仓 `15`、sorter `16`、普通保存 `d65edba6-4174-48d8-a058-07c7040c97b2`、Move `5e1853b6-5e03-422f-9f71-0aff686dde11` 回石脉，以及动作 `561e1b2a-089d-49ec-9a0c-dde02f9f182c` 手采 4 石矿。一次 `preferredDistance=3` 的 sorter prepare 被参数下限安全拒绝且无 commit，fresh 后用 5 m 参数正常施工。审计时钛矿机仍为 network 1、serve `1.0`、working；仓 `15` 已从里程碑前的 26 自然增长到 `100 + 85`，sorter 仍为 `14 -> 16 -> 15` 且实际携钛。fresh session revision `54`、tick `10008668`、planet `102`、Walk/速度 0、核心约 `295.22/400 MJ`、healthy、last save tick `9997352`、无 flight checkpoint；玩家空手且 600 硅/29 铁/4 石仍守恒。journal 新增 sequence `43`，在 tick `9985749` durable 记录首个产线钛石，`persistencePending=false`、无 error；此前文档沿用 `42` 的滞后已立即改正。EXP-007/021/028/037/042/047/070/095/111 与当前端点、电力、首次事件、保存和库存一致；审计后才允许硅矿组件手搓。

- 2026-09-02：完成第二次资源航行提前复核后的首组 10 个成功写动作审计：石脉 `373` 手采 8 个、手搓第 2 台采矿机 `92dc9c4d-4ccb-404d-9d1a-1c188085815d`、手搓 4 台风力涡轮机 `31ff889a-00e1-4b4a-b0cf-a3007fbfe4c3`、手搓 2 座小型储物仓 `cbc5e82f-2722-4a13-9cec-e50ffc8711f0`、补搓 3 条带 `9c6c2d6a-da19-4987-a39c-b8e4f9e3c23b`、手搓 2 个分拣器 `cff528a6-efdf-42bc-af6f-3ce25652bc99`、手搓 2 座电力感应塔 `1e40381a-b5b7-4cf7-bfb2-7e2774c3afb0`、Move `c2816368-f493-4cea-9354-1209b4b16016` 到钛脉、原生施工首台远端采矿机，以及原生施工电塔 `9c38f913-b28c-4b46-9e2d-db3eb7c644f6`。矿机 build 的结果展示访问了不存在字段而遗漏 action ID，但没有重放；fresh 唯一实体 `1`、背包采矿机 `2 -> 1`、覆盖钛脉 `315/322` 和 revision 共同核销。所有手搓均由 DSP replicator 从真实 100 铁矿、8 石矿和随身铜递归加工：审计时背包仍有 29 铁矿、489 铜块、600 硅石、4 风机、2 仓、8 带、2 sorter、1 电塔、1 矿机，玩家空手。fresh 状态为 revision `35`、tick `9981196`、planet `102`、Walk/速度 0、核心约 `272.00/400 MJ`、healthy、journal `42/42` durable 且无 pending/error、无 flight checkpoint；矿机实体 `1` 位于 `(-118.07,137.05,85.78)`、尚未供电/出料，电塔实体 `2` 位于 `(-126.92,128.58,86.25)`。EXP-007/018/021/028/047/061/080 与递归手搓、施工唯一性、库存守恒和审计边界一致；审计后才继续接入风电和出料仓。

- 2026-09-02：第二次资源航行在上一审计后提前完成一轮保守复核，共覆盖 9 个成功写动作和 1 个已接受但明确进入 `recovery_required` 的飞行动作：两次原生单栈加氢把燃料仓 `40 -> 60 -> 80`；首个 `104 -> 102` 动作 `e84153dc-d718-4a9b-ade7-45ebe291e138` 在保存独立检查点 tick `9895450` 后未能在 3600 tick 窗口保持原生 Sail，并继续惯性经过错误的 planet `103`，没有把该航迹当作成功。恢复动作 `4c72350e-d877-4f4a-a511-e00a4d24604d` 严格重载同一 checkpoint `eda216a09b0349b48d61ac574b0a21f8`，新 session 从相同 tick、planet `104`、满核心和 80 氢重新采用；重试动作 `5b2fadb3-4a38-4615-b25c-5a8c4c92a793` 进入原生 Sail，约 `30162 -> 1613 m` 接近并在 planet `102` 连续 600 tick 保持 Walk 后完成，checkpoint capability 随即消失。随后直达已知硅脉的 Move 与 600 硅手采动作虽被本地结果展示遗漏 action ID，但 fresh 双边读回证明矿脉 `64591 -> 63991`、背包 `0 -> 600`；Move `a4442be7-1b63-46a3-b8ee-7bf9757658cd` 到铁脉后，动作 `6f4e5df3-0cfc-4bcb-a02b-e813f9ea07f2` 使矿脉 `51125 -> 51025`、背包铁矿 `0 -> 100`，Move `cb60ca7b-5924-480c-a8f2-4eb371f315ab` 再稳定到达石脉。fresh 审计为 revision `15`、tick `9960669`、planet `102`、Walk/速度 0、核心约 `296.69/400 MJ`、healthy、和平/非沙盒/1×、journal `42/42` durable 且无 pending/error、无 flight checkpoint；玩家空手，600 硅和 100 铁均仍在背包。EXP-007/047/050–053/061/080/083/084 与当前重试、守恒和稳定着陆证据一致；本轮提前审计后才继续远端矿站施工。

- 2026-09-02：完成上一审计后的 10 个成功游戏写动作复核：手搓 9 条带 `848ea4d6-de94-47a2-967f-e7340758ab72`、手搓 1 个分拣器 `a12a95aa-8ddf-49f5-9ba4-d3667d966abc`、铁块 `829 -> player` 12 个、手搓 4 齿轮、再手搓 12 条带、原生施工 18 段 PLS 输入带 `529f9496-6d96-43a5-b6bb-08f778e905c3`、原生施工 `531 -> 938 -> 937`、里程碑保存 `142d6c1c-98fe-4f9c-aab4-3ae51fadc9a4`，以及两次原生单栈加氢 `6a027a70-99c9-424d-b2a6-0732747c8cd2` / `3bdb78c1-8cb0-4890-81f5-e60816fa8710`。第一次请求 79 氢被 prepare 以“当前原生单栈只会移动 19”安全拒绝且无 commit，随后精确 `19 + 20` 使背包氢 `128 -> 89`、燃料格 `1 -> 40`，总数始终 129。fresh 核验为 revision `91`、healthy、last save tick `9862572`、journal `42/42` durable 且无 pending/error、尚无 flight checkpoint；玩家 Walk/0、核心 `400/400 MJ`、空手。物流路线保存后仍为需求塔 100/10 idle/0 working、供应塔 20/0 working、双方订单 0、双塔满电，sorter `938` 保持 `531 -> 937`。EXP-007/018/021/028/047/080/109/110 与现场一致，无 Drift、quarantine、outcome unknown、动作重放或未解释物品正增量；此复核完成后才允许继续加氢与创建下一次独立飞行检查点。

- 2026-09-02：行星内真实物流路线完成并正常保存。第二座站 `918` 接入 network 4、限充 6 MW、槽 0 配钛块本地供应；18 段普通带与 sorter `938` 把仓 `531` 的 120 钛块全部送向塔内。首塔 `916` 仅凭自己的 10 架无人机形成 working/order，四批各 25 件把需求库存由 0 增至 100，供应塔余 20，最终双方订单归零、无人机全归队、双塔满电。保存动作 `142d6c1c-98fe-4f9c-aab4-3ae51fadc9a4` 确认 tick `9862572`、revision `89`、write health healthy，journal `42/42` 且无 pending/error，flight checkpoint capability 不存在。EXP-109 已越过真实路线证据门并新增 EXP-110；下一主线切换为 v0.3 双星球 ILS 与运输船闭环。

- 2026-09-02：完成上一审计后的 10 个成功游戏写动作复核：补取第 2 个铁块（动作结果已完成但被本地输出访问不存在字段遮住，随后以玩家铁块 `1 -> 2`、仓 `829` 的 `2015 -> 2014` 和 revision `60` 双边核销）、磁铁 `26 -> player` 动作 `ccb4b258-924f-42c0-b44c-e017927a2b52`、手搓磁线圈 `58ae2684-69c9-4ec9-83d7-39cff99f52db`、手搓电塔 `75a259cd-7a48-4feb-9258-01f7657af72d`、原生施工电塔 `919`、物流塔限充与供应槽配置、再取 10 铁与 1 电路板、手搓 3 齿轮。fresh 核验为 revision `75`、healthy、last save tick `9783554`、journal `42/42` durable 且无 pending/error、无 flight checkpoint；玩家 Walk/0、核心 `400/400 MJ`、空手。首塔 `916` 仍为 network 1、满电、10 idle、钛块需求；第二塔 `918` 已为 network 4、约 `115.46/180 MJ`、最大充电 6 MW、钛块供应、0 idle/working，钛仓 `531` 仍为 120。EXP-007/017/018/021/061/080/098/099/107–109 与新证据一致；唯一展示异常由 fresh 双边守恒核销，未重放，无 Drift、quarantine、outcome unknown 或未解释物品正增量。此复核完成后才允许下一次游戏写入。

- 2026-09-02：完成上一审计后的 10 个成功游戏写动作复核：一次落到 Drift 的几何候选 Move `aa803dd4-e117-424f-8eb3-456869639f9c`、精确 Walk 回收 `1724f981-fc8e-4f3b-906e-1be2b2a30f0c`、以草地节点为终点的 109 m 连续 Move `7a1a8363-6f88-41eb-ae53-e632d390f9e0`、钛块 `531 -> player`、返回草地节点 Move `eb8fc07f-3e26-48d3-b25f-059abb6b5744`、钛块 `player -> 899`、普通保存 `c73789bf-f51d-4573-ac4b-c51860f6f954`、站体 `900 -> player`、原生施工实体 `918`、铁块 `829 -> player`。fresh 核验为 revision `59`、healthy、last save tick `9783554`、journal `42/42`、无 flight checkpoint；玩家 Walk/0、满核心、持 1 铁与 500 铜，钛仓 120、站体输出仓空。首塔 `916` 仍满电/10 idle/钛需求，第二塔 `918` 独立为 network 0、0/180 MJ、空槽/空 fleet。EXP-007/061/077/080/093/107–109 与新证据一致；Drift 候选被立即否决并回收，无 quarantine、outcome unknown、重放或未解释物品正增量。此复核完成后才允许下一次游戏写入。

- 2026-09-02：第二座 PLS 批次完成并普通保存。钛块经母星仓 `531 -> player -> 899` 精确守恒，配合已闭合的钢材/处理器/粒子容器使制造台 `898` 满供电完成第二轮 recipe `93`；仓 `900` 得到 1 座待施工站体。保存动作 `c73789bf-f51d-4573-ac4b-c51860f6f954` 确认 tick `9783554`、revision `55`、write health healthy，journal `42/42`、无 pending/error，flight checkpoint capability 不存在。EXP-007/080/093/107/109 已按本轮展示异常、活跃仓守恒、草地锚点跨水路线和第二批高数量配方证据复验；下一步是原生施工第二站并完成真实本地运输。

- 2026-09-02：完成上一复核后的 10 个成功游戏写动作复核：处理器 `854 -> player -> 899` 两段守恒转运，加上从第二座 PLS 区向钛块仓 `531` 方向的 8 段有界 Move（`698a583a-5882-4b19-9d30-db4e29a33558`、`0754d7b5-d46f-4a14-b623-44c879010782`、`2f114377-a02a-418b-b591-8d052aacc787`、`91424f74-d809-47ce-a677-0185133b7162`、`59aa9036-984b-4050-bed6-e447e4d3f042`、`ce1db886-aca9-4103-ad5b-ec529eb6b734`、`c1401887-7341-4a44-86a7-2393c36abc1e` 与一次 action ID 被本地展示异常遮住、但由 fresh revision `42 -> 44` 核销的末段）。全部实际移动均停在 Walk/速度 0，核心恢复 `400/400 MJ`，会话 revision `44`、write health healthy；journal `42/42`、无 pending/error。制造台 `898` 仍满供电且已持有钢材/处理器/粒子容器 `40/40/20`，仅缺钛块 40；首塔 `916` 仍满电、10 idle/0 working、零订单。EXP-007/061/077/080/093/107/109 与 fresh 状态一致，无 quarantine、outcome unknown、动作重放或未解释物品正增量；此复核完成后才允许下一次游戏写入。

- 2026-09-02：完成首座 PLS 保存后的下一组 10 个成功游戏写动作复核：普通保存 `0cdbefd4-3c57-4c9b-abbf-4b958814350c`、两段有界几何脱困 Move `a410ff15-b6e6-4a35-8206-921cf768f467` / `c96310db-a9bc-4fe3-a554-7c627142180a`、铜块入微晶元件仓、电路板 `562 -> player -> 849`、粒子容器 `885 -> player -> 899`、钢材 `792 -> player -> 899`。两段 Move 均以 Walk/速度 0 终止且核心保持 `400/400 MJ`；六次 transfer 的玩家端精确反向差量成立，活跃目标仓的瞬时少量均由制造台或 sorter 闭合。处理器仓 `854` 已自动达到 40，粒子容器全部 20 进入 `898`，钢材以 `31+8+1=40` 闭合；会话 revision `26`、write health `healthy`、journal `42/42` 且无 pending/error，PLS `916` 仍为满电、10 idle/0 working、零订单。EXP-007/061/077/080/107/109 与新证据一致，无 quarantine、outcome unknown、重放或未解释物品正增量；此复核完成后才允许下一次游戏写入。

- 2026-09-02：10 写复核完成后，普通保存动作 `0cdbefd4-3c57-4c9b-abbf-4b958814350c` 把首座 PLS 的 network 1、`180/180 MJ`、钛块/100/本地需求槽、6 MW 上限和 10 架 idle 无人机持久化到 tick `9522204`、revision `15`；fresh 读回仍为 healthy，自动签发的新 exact-primary restart ticket 可用，journal 保持 `42/42`、无 pending/error。保存是新一组成功写计数的第 1 项。

- 2026-09-02：完成当前同批部署后的 10 个成功游戏写动作复核：exact-primary resume/adoption、从仓 `829` 守恒取得 2 铁、正常手搓电塔、原生施工电塔 `917`、PLS `916` 的钛块需求槽配置、12→6 MW 充电配置、仓 `893` 的 10 架无人机守恒转入玩家、fleet 存入 10、取出 1、放回 1。复读确认 planet `104`、和平、非沙盒、1×、站点 network 1/full energy、槽/订单、玩家/仓/机队总数和 write health 全部一致；无 quarantine、outcome unknown 或未解释物品正增量。EXP-007/018/021/069/072/097–101/105–108 均与新证据一致，EXP-097/098/099/101/105 升级为 `validated`，新增 EXP-109。此复核完成后才允许执行下一次游戏写入（普通保存）。

- 2026-09-02：EXP-108 升级为 validated。首塔施工正常保存 tick `9462208` 后，同批部署哈希与新 Release 输出一致；exact-primary 恢复动作 `df1ae62a-548a-49fe-a9a1-fbd6d1aca764` 成功并自动重存 tick `9462240`。实体 `916` 从旧版 `logisticsStation=null` 变为完整本地站 DTO（planet `104`、station `1`、gid `0`、4 空槽、drone capacity 50），journal `42/42`、无 checkpoint、写健康。部署/重启复核确认 EXP-069/072/083/104/108 仍成立；当前新进程成功写计数从 resume/adoption 这一项开始。

- 2026-09-02：新增 EXP-108。首座正常施工 PLS 实体 `916` 暴露本地站 raw `planetId=0` 哨兵，旧“所有 station 都必须等于 factory planet”规则被 live 反例推翻。源修复仅对非星际站接受 0/exact、拒绝 foreign，星际站继续 exact；四入口共用纯策略，完整构建 0 warning / 0 error、101 项测试通过，等待正常部署复验。

- 2026-09-02：完成 Plugin 部署/重启触发复核。产品里程碑主档先正常保存到 tick `9413535`，旧进程正常关闭且 descriptor 清零；Release Plugin/Core/Contracts 逐文件 SHA-256 匹配后同批部署。恢复动作 `ba335eeb-d6b6-47b5-8e29-4eb133d0dba4` 只载入票据绑定的精确主档并自动重存到 tick `9413567`。fresh 读回 planet `104`、和平、非沙盒、1×、healthy、journal `42/42`、仓 `893` 的 10 个 item `5001`、仓 `900` 的 1 个 item `2103`，且无 flight checkpoint capability；EXP-069/072/083/104/107 与新进程一致。EXP-105 更新为“已部署但仍待首塔动作”，不提前升级状态。新进程目前只有 resume/adoption 这一项成功游戏写入；下次累计 10 写仍从此计数。

- 2026-09-02：新增并验证 EXP-107。四输入高数量建筑配方在完整批次到位前保持不工作是正常等待；行星物流站制造台随后满供电完成 recipe `93`，仓 `900` 得到首座 item `2103`、日记 sequence `42` durable，并正常保存到 tick `9413535`、revision `115`。同时记录 production 配置使用完整 state hash，误用 sorter 专用配置哈希会在 prepare 阶段无副作用拒绝。

- 2026-09-02：新增并验证 EXP-106。行星物流 `1604` 原生解锁后，预建过滤链只启用一次 recipe `94`；三输入真实下降、输出 sorter 携带 item `5001`、专用仓达到 10、日记 sequence `41` durable，随后同一主档普通保存到 tick `9369181`、revision `112`、写健康正常。

- 2026-09-02：完成上一账本复核后的 10 个成功游戏写动作复核：一次 Drift 候选及精确 Walk 回收、返回已验证风机 `713`、钛块/钻石/塑料的五次守恒中转、动作 `0ee30c94-8b75-4a9e-a20e-1ba25665882a` 到风机 `130` 的正常 Walk，以及动作 `532666d4-de0d-4ea9-abfc-6a44657fe555` 在科技 `1604` 正常解锁后单次启用制造台 `891(recipe 94)`。EXP-061/066/080/093/100 与 fresh 动作终态、背包/仓储、Walk/Drift、能源、科技和写健康一致；第十次提交后先完成本复核，再允许下一次游戏写入。制造台随后自然取得三类输入并由输出 sorter `894` 携带 item `5001`，未出现 quarantine、outcome unknown、串料或未解释正增量。

- 2026-09-02：新增 EXP-105。当前程序集确认无人机/运输船 UI 以 idle+work 占用 prefab 容量、只从 idle 取出且存入会丢弃载具增产点；源码新增 46-tool 双阶段精确 fleet transfer、专用 fleet hash、容量/类型/范围/空手/背包副本检查和提交后完整守恒复读。完整 solution 0 warning / 0 error，94 项测试通过；运行游戏仍是旧 44-tool DLL，首塔前不提前声称 live。

- 2026-09-02：完成上一复核后的 10 个成功游戏写动作复核（60 有机晶体双段守恒进入钛晶石线、五段有效 Walk/局部脱困、一次 Drift 目标与精确 Walk 回收）。EXP-007/061/066/080/093 与 fresh 玩家、仓/设备缓冲、动作终态、能量和写健康一致；新增证据明确 180-tick 看门狗会在满电时识别路上卡死，而至少 8.17 m 非带实体净空仍不证明陆地。两次碰撞失败和一次 Drift 均未重放，无 quarantine、outcome unknown 或未解释物品差量。

- 2026-09-02：新增 EXP-104。自包含发布预演先由 locked restore 的 `NU1004` 暴露缺失 RID 锁定，再在干净提交 `5cb465a` 生成 `sourceDirty=false` 的 `0.3.0-preview.2`；232 个 manifest 文件、zip sidecar、包内 MCP initialize 与 44-tool surface 独立复验通过。明确保留“真实 BepInEx 安装/Bridge 握手尚未验证”的边界，未创建 tag 或 Release。

- 2026-09-02：上一复核后的第二组 10 个成功游戏写动作复核完成（石墨烯/铜续入粒子容器线，磁铁/铁/铜补回过滤共享仓 `723`，500 自动电路板接回蓝糖仓）。EXP-028/073/074/080 与端点、过滤、玩家中转、设备缓存和科研 hash 一致；EXP-073 由第二个独立最终消费者窗口升级为 `validated`。全程无 quarantine、outcome unknown、串料或未解释正增量。

- 2026-09-02：粒子容器提交后的 10 个成功游戏写动作复核完成（20 粒子容器双段守恒入仓、空载 sorter `905` 延后过滤、氢燃料补给、500 铁块接回电路板产线、两项后续科技入队、42 电磁涡轮续料）。EXP-023/062/080/094/100 与 fresh 终态一致；EXP-080 由目标仓即时消费的相反方向独立样本升级为 `validated`。全程无 quarantine、outcome unknown 或未解释物品正增量。

- 2026-09-02：第四次复验 EXP-062；粒子磁力阱在 tick `8696391` 正常解锁后才激活预建制造台 `883(recipe 99)`。三类输入各下降 10、专用仓出现 2 个粒子容器，日记序号 38 持久化首产且无 pending/error；普通保存动作 `0252c2a1-4618-43cc-bd1a-8fa6d0ca105c` 将同档保存到 tick `8699182`、revision `52`。

- 2026-09-02：复验 EXP-023/058/061/066。普通电力感应塔不再被当作无线充电证据；混料带 `551` 最终过滤为铜并以守恒电路板恢复蓝糖科研；跨水中点、已建地面直达和密集工厂夹缝分别由 Drift、成功 Walk 与 180-tick 停滞终态区分。

- 2026-09-02：验证 EXP-100。停机部署后，专用 selection hash 在当前 `1703` 未完成时一次安全追加 `1604`，队列 fresh 复读为 `[1703,1604]`，完整进度哈希仍保留观察语义。

- 2026-09-02：验证 EXP-102/103。正常保存 tick `8640914` 后以受保护票据恢复同档；MechaLab DTO 精确复读 251 个蓝矩阵预留，活跃 sorter `551` 在带货窗口未准备、在空载 Returning 窗口成功配置蓝矩阵过滤并保持双端拓扑。

- 2026-09-02：新增 EXP-103。仓到玩家的 293 蓝矩阵当场守恒，后续背包减少 251 与当前科技剩余需求的 `MechaLab.ManageSupply` 整数公式完全相符；新 player DTO 把隐藏研究保留容器显式化，完整构建 0 warning / 0 error、90 项测试通过，等待正常部署后 live 复读。

- 2026-09-02：修订 EXP-058 并新增 EXP-102。蓝矩阵未进研究站而被混料仓的另一无过滤出口送入堵塞环带；守恒腾位后回收恢复。源码引入排除空载返程进度、仍绑定身份/拓扑/filter/携货的专用配置哈希；完整构建 0 warning / 0 error、90 项测试通过，部署前仍按旧 DLL 规则处置。

- 2026-09-02：新增 EXP-101。采用物流塔最大充电功率的 3 MW UI 步进安全子集，绑定 prefab/consumer/configuration hash 并保持 station/player 库存不变；完整构建 0 warning / 0 error、86 tests passed，等待首塔 live。

- 2026-09-02：复验 EXP-007。自动补产 10 个处理器后，20 电路板/20 铜块和 40 钛块全部经普通双端守恒转移；钛块入仓后的空集合展示错误由 fresh 三端状态核销，未重放，行星物流站预备仓现有钢/处理器/钛块各 40，普通保存到 tick `8474115`、revision `30`。

- 2026-09-02：新增 EXP-100。活跃科研下 81 次选科技 prepare 均被上传量竞争安全拒绝；新增稳定 selection hash，保留队列/解锁/前置校验，完整构建 0 warning / 0 error、83 tests passed，live 部署待当前产线窗口结束。

- 2026-09-02：新增 EXP-099。修正物流塔充电字段语义：station `energyPerTick` 为实时 requested，consumer `workEnergyPerTick` 才是配置 maximum；DTO 与双哈希已拆分，避免正常充电造成配置 stale。

- 2026-09-02：新增 EXP-098。采用 `SetStationStorage` 的空槽/同物品安全子集，禁止清槽/换品，绑定配置哈希与库存守恒；完整构建 0 warning / 0 error、80 tests passed，等待首座空物流站实机验证。

- 2026-09-02：新增 EXP-097。v0.3 首个切片把物流塔只读状态接入现有 factory list/inspect 工具，采用 entity/station/planet 交叉身份和实时/配置双哈希；完整构建 0 warning / 0 error、78 tests passed，live 仍等待恢复同档并完成首座站点。

- 2026-09-01：复验 EXP-007/069。最终关机保存已经执行后，PowerShell 仅在展示不存在的 `expectedRevision` 字段时失败；fresh session 证明主档 tick `8340400`、revision `677`、healthy 且自动签发续玩票据，没有重放保存。DSP 正常接受窗口关闭并退出，descriptor 清零、固定票据保留；下次唯一恢复门槛更新为该 tick。

- 2026-09-01：复验 EXP-048。垂直建造完成后正常选择粒子磁力阱 `1703`，日记新增 sequence `36`（tick `8244528`、`2026-09-01T22:49:32.806289+08:00`、本局 `001d 14:10:08`）并 durable through `36`、无 pending/error；新增 `docs/gameplay-timeline.md` 汇总从落地到当前的证据边界、全部科技/升级、首次事件和 96 条决策索引。

- 2026-09-01：复验 EXP-048/096。垂直建造 `3701` 首次选择成为首个 live `upgrade_first_selected`（sequence `35`，三种时间字段完整且 durable）；混料区两条临时 sorter 锁氢、中继 62 氢守恒清零，设备图同时证明 `163` 当前仅由 filter `1114` 的 sorter `709` 输入，修正了“仍持续接收双产物”的旧假设。

- 2026-09-01：EXP-094 升级为 `validated`。推进器解锁后，同一空载 sorter `897` 的 filter `1405` 成功应用，组件/sign 与双端连接复读一致，闭合了科技门控前拒绝、门控后只补缺失过滤的正反样本。

- 2026-09-01：新增 EXP-095/096。推进器在科技解锁后启用预建 recipe，钢/铜下降、专用仓增至 60、日记 sequence `34` durable；发现 network `1` 欠供电后补建风机 `910–914` 恢复满供电，并正常保存 tick `8123715`。混料分流实验同时证明普通仓满载是槽位语义，新无过滤 sorter 会在过滤前预取并持货；当前只保留受控泄压和下游油过滤，不把未完成的永久分产误记为闭环。

- 2026-09-01：复验 EXP-007 并新增 EXP-094。涡轮取货 commit 后的 PowerShell 包装错误由玩家/源仓/目标仓三端复读核销，只补了缺失的入仓半程；物流运输机 recipe 0 预建中，已存在的铁块/处理器过滤成功，未解锁推进器 item `1405` 的过滤则在 prepare 阶段安全拒绝，明确要求等科技解锁后只补缺失 filter。

- 2026-09-01：修订 EXP-053 并新增 EXP-092/093。石墨烯化工厂 `869` 以 recipe 0 完成双过滤/输出/供电预建，科技 `1131` 解锁后单次启用 recipe `31`；自动来源守恒装入后，仓 `871` 从空仓增至 14、设备另有 1，日记 sequence `32/33` durable，主档保存到 tick `7854029`、revision `403`。同时用非带实体图、局部切向绕行和 `183 <-> 713` 连续跨水修正了未知地形长途规程。

- 2026-09-01：修订 EXP-090 并新增 EXP-091。直连 sorter `868` 实际携电路板并恢复蓝矩阵站，与研究 sorter `860` 构成第二个目标需求直供样本；处理器科技正常完成后，预建制造台 `853` 启用 recipe `51`，输出仓 `854` 从空仓增至 17，日记 sequence `29/30` durable，主档保存到 tick `7707489`、revision `306`。

- 2026-09-01：新增 EXP-089/090。纯石矿带经 sorter `859` 形成自动缓冲；化工厂 `861` 在三过滤空仓完成后装料，硫酸仓 `863` 从空仓增至 7，日记 sequence `28` durable，主档保存到 tick `7663628`。同期旧蓝矩阵混线使处理器停在 `44100`，直供 sorter `860` 把仓 `26` 的自动蓝矩阵送入研究站 `84`，科技恢复推进到至少 `88189/144000`。

- 2026-09-01：新增 EXP-088。仓 `843` 先建立高纯硅/铜过滤 sorter `850/851`，再守恒装入 100 铜并启用制造台 `848` recipe `53`；专用仓 `849` 从空仓增至 15，日记 sequence `27` durable，主档保存到 tick `7545277`。

- 2026-09-01：新增 EXP-087。状态追踪发现既有石矿机 `86` 和主带 `87…121` 仍健康，取消重复矿机方案；自由端接入熔炉 `841/842`、仓 `843` 与补电塔 `847`，高纯硅仓从空仓增至 7，日记 sequence `26` durable，主档保存到 tick `7517473`。

- 2026-09-01：新增 EXP-086。制造台 `725` 直接绑定 belt 被原生 belt-port 校验拒绝后，改用自由带 `824…818` 和两端 sorter `825/826`，配合电动机 sorter `815` 与输出 sorter `828` 完成电磁涡轮自动线；仓 `827` 从空仓增至 22，日记 sequence `22` durable，主档保存到 tick `7419065`。

- 2026-09-01：复验 EXP-066 并新增 EXP-085。返航落点到煤区的机械中点在第二段进入 Drift 后立即断路，回到首个 Walk 落点再直达已知煤脉只消耗约 7.3 MJ；旧铁矿机资源集合为空被确认是矿脉耗尽，新矿机 `796` 经独立带与 sorter `812` 侧接旧主干，钢材仓 `792` 从 0 增至 2、磁悬浮 hash 增长 180。

- 2026-09-01：本批 Release 完整构建 0 warning/0 error、76 tests passed；同批部署 hash 为 Plugin `3BD98E6CA5A129173F0870259F56286084E43B3C6ECD59D0B027685B13AE7BB9`、Core `22D088E5540B77E69C3A949D0253C6DBE08FFD7BAE8E9E9423C781A147AF3E4A`、Contracts `88C8F76840EF5A214561CCC579BC2923021D828FCFF8F20E9F72AE01921BF4C1`。planned restart 实机只选精确 primary，journal 序号 `20` 已 durable through `20`，旧 flight capability 消失，resume consumption 在两处各有 durable tombstone。

- 2026-09-01：新增 EXP-082–084，并把 EXP-071 标为 superseded。返航用同一 checkpoint 经“落地不稳/未入 Sail”两次明确失败后第三次成功，主档保存到 tick `7146048`；源码复核确认旧 checkpoint 无生命周期、健康 resume 会错误优先 fresh LastExit、消费无 tombstone，现行经验改为 flight 成功封存/主档 retire、planned primary/quarantine LastExit 分流、header tick 前验与 token-hash durable tombstone。

- 2026-09-01：修订 EXP-061 并新增 EXP-080/081。活跃仓 `26` 的并发电路板补货证明 transfer 源端聚合净差量可能被抵消，客户端展示断言失败后没有重放；仓 `286` 多基座夹缝的四向探测全部明确失败，正常保存到 tick `7027343` 后经 Steam 正式启动和 protected ticket 恢复，同档 tick/模式/日记/钢材实体全部越过门槛，但玩家坐标被原样保留，撤销“重启本身会几何脱困”的隐含假设。

- 2026-08-31：创建账本；录入并复核本轮已知的构建、启动、恢复、动作协调、状态稳定、电力、分拣器和执行优先级经验。尚未把 EXP-006、EXP-011、EXP-012 的待实机范围误标为完全验证。
- 2026-08-31：EXP-011 加入储仓 `136` 到热电站 `134` 实测约 8.90 m 仍为 `TooFar` 的反证，撤销任何“约 9 m 可能足够”的隐含假设。
- 2026-08-31：新增 EXP-016；实机复读确认孤立热电站与其燃料分拣器形成冷启动供电死锁。
- 2026-08-31：EXP-011 升级为 `validated`（限定于当前姿态）；约 6.41 m 的实体 `138` 成功输送燃料。EXP-016 补充电线杆 `139`、`140` 合网后的冷启动成功证据。
- 2026-08-31：完成首个累计 10 个成功游戏写动作复核。EXP-010 因当前实体/网络读回反证改为 `invalidated`，新增 EXP-017 作为替代；EXP-008、EXP-011、EXP-015、EXP-016 与最新现场仍一致。
- 2026-08-31：新增 EXP-018；用 20 批齿轮/传送带的队列中途快照和最终守恒结果验证批量手搓的原料缓冲与终态语义。
- 2026-08-31：新增 EXP-019；记录电线杆 `133` 与 `142` 约 22.57 m 未合网及精炼厂独立断电网络的读回。
- 2026-08-31：EXP-019 升级为 `validated`（限定于当前姿态）；中继塔 `143` 的约 12.31/12.91 m 两段完成合网，精炼厂供电率恢复为 1.0。
- 2026-08-31：新增 EXP-020；记录油井与精炼厂双端 belt 绑定被合并端口检查拒绝，保留单端归因待验证。
- 2026-08-31：EXP-020 升级为 `validated`；油井 source-only 路径正常创建 18 段带，从合并错误中排除了油井端，确认精炼厂应由末端分拣器输入。
- 2026-08-31：EXP-020 补充输入分拣器 `162` 的成功建造证据；当前原油输入拓扑已完整，配方仍保持关闭。
- 2026-08-31：新增 EXP-021；一次被安全拒绝的旧库存预算证明自动燃料仓必须在每次转移前重新复读。
- 2026-08-31：新增 EXP-022；记录从正常石墨主仓到背包再到机甲燃料舱的完整守恒补能链，并明确它不替代无线充电验收。
- 2026-08-31：EXP-007 补充第二个独立样本：harvest 已完成但脚本访问不存在的 `resourceDeltas` 后失败，节点与背包复读阻止了一次危险的重复采集。
- 2026-08-31：新增 EXP-023；用无线塔实体、电网节点/负载差量以及零反应堆状态下的连续核心能量上升完成无线充电验收。
- 2026-08-31：完成后续累计 10 个成功写动作复核。EXP-007、EXP-008、EXP-009、EXP-018、EXP-019、EXP-021、EXP-022 与新现场一致；EXP-012 仍只缺当前进程的第二个同源输出分拣器实机复验，未提前升级范围。
- 2026-08-31：EXP-012 完成当前进程实机复验；同位置旧 `164` 与新 `181` 被正确区分，关闭该条目的 live-validation 缺口。
- 2026-08-31：新增 EXP-024；原油链不动的只读诊断证明“精炼厂有电”不能替代对输入分拣器覆盖的独立检查。
- 2026-08-31：EXP-024 由塔 `182`、分拣器实际携油和精炼油增长完成闭环；新增 EXP-025 记录精炼运行态的整网容量瓶颈，新增 EXP-026 保留 action 完成后首次单体查询短暂失败的单样本经验，且未因此重放写入。
- 2026-08-31：EXP-025 补充以精炼油副产物驱动热电站 `183`、把网络 3 恢复满供电的证据。随后用连续流量复读推翻 EXP-027 的“连接槽覆盖导致断线”推断并将其 invalidated；新增 EXP-028，明确共享端点分拣器应以目标字段、阶段/携货和上下游库存差量验收，取消不必要的写入冻结与重启计划。
- 2026-08-31：新增 EXP-029，确认当前空 tank buffers 是读取缺口并完成离线实现，但为保持同一健康存档暂缓部署；新增 EXP-030，固化本地 SDK 与 Mono.Cecil 元数据研究路径。完整解决方案 0 warning/0 error，49 tests passed。
- 2026-08-31：完成下一组累计 10 个成功游戏写动作复核（覆盖补塔、精炼油热电、三次主仓转移、采铁及磁铁/线圈手搓）。EXP-015、EXP-018、EXP-024、EXP-025、EXP-028 与现场仍一致；EXP-029 保持“离线实现、live 待部署”。新增 EXP-031 记录范围内 harvest 的正常接近行为。
- 2026-08-31：完成再下一组累计 10 个成功游戏写动作复核（采石、玻璃/电塔/研究站、40 齿轮、120 传送带、两段移动、两段氢主干）。EXP-008、EXP-009、EXP-015、EXP-018、EXP-020 与现场仍一致；新增 EXP-032，隔离记录长施工窗口中的非预算氢 `0 -> 1`，不把它计入自动红矩阵验收。
- 2026-08-31：第三段氢主干再次在活带续接时使玩家氢 `1 -> 2`，且结构/会话健康；EXP-032 由单样本 observed 收敛为当前基础带+氢续接范围内 validated，2 个回收氢继续从自动红矩阵证据中排除。
- 2026-08-31：历史 M0 里程碑状态变化复核：新增 EXP-033，以研究站 `256` 的配方 18、双输入和能量矩阵 `0 -> 3 -> 6` 完成首个自动红矩阵闭环；新增 EXP-034，记录网络 2 运行态约 90% 的容量瓶颈。EXP-015 的生产线优先级和 EXP-032 的 2 氢排除规则均继续适用。
- 2026-08-31：显式保存动作 `b399facb-48cd-4838-b7ab-9c9762b6def7` 完成 tick `2499658` 的精确 owned-world 保存；EXP-033 补齐最终保存证据，历史 M0 里程碑完成，EXP-034 保留为里程碑后的首要产能优化项。
- 2026-08-31：完成 M0 前后交界的下一组累计 10 个成功写动作复核（第三段氢带、红糖站/配方/双输入、最终保存、两次建材转移、采铁和磁铁）。EXP-031、EXP-032、EXP-033、EXP-034 均与现场一致；未发现新 quarantine、未解释建材差量或存档状态回退。
- 2026-08-31：新增 EXP-035；一次长途移动因真实能量耗尽而正常 `action_failed`，未触发隔离。把“低电自动回充、直达前预算、保留 20% 余量、预算不足先用已有燃料或近端燃料仓”固化为当前运行规则，并保留阈值待后续样本优化。
- 2026-08-31：复核 EXP-035 并加入“实体碰撞也会持续耗能”的反例；新增 EXP-036，以位移/目标进度双窗口、断能原因隔离和 player-order single-flight 替代只等全局超时。新增 EXP-037，固化同档续玩以及每种新产物完成后依次实机复读、普通保存、Git 提交与推送的里程碑规则。
- 2026-08-31：提交前安全复核新增 EXP-038；把 new-game/resume 的幂等容量拒绝前移到 DSP 加载副作用之前，消费相同 token 的固定票据副本，并移除相关路径/底层异常消息泄漏。完整解决方案 0 warning/0 error，55 tests passed。
- 2026-08-31：新增 EXP-039；旧 DLL 的 move 在动作成功后残留底层订单，持续消耗约 101 MJ。Mine 覆盖并明确断能失败后，位置与回充趋势稳定。当前程序集 IL 证明可用 `OrderNode` 对象引用做精确归属，源码已替换坐标近似/类型猜测式终止；完整解决方案 0 warning / 0 error、55 tests passed，等待下次安全部署实机复验。
- 2026-08-31：完成红矩阵里程碑后的下一组累计 10 个成功游戏写动作复核（建材转移/手搓、两次补给、短途 move、范围内 harvest 等）；EXP-018、EXP-021、EXP-022、EXP-031、EXP-035、EXP-037 与守恒读回仍一致。新增 EXP-039、EXP-040 记录旧 DLL 的终态订单残留和跨 ID 命名空间远端误采，后续写入改用精确订单引用与 `withinPlayerBuildArea` 限距。
- 2026-08-31：新增 EXP-041；当前程序集 IL 与无燃料静止采样共同确认 `Mecha.GenerateEnergy` 的基础 `corePowerGen` 先于燃料分支生效，当前约 80 kW。把它限定为无附近补给时的应急等待手段，不放宽 EXP-035 的常规长途能量阈值。
- 2026-08-31：远端恢复与红糖线完善复核：EXP-035 新增满能量、42 煤起步、8 个短 waypoint 的约 297.5 m 成功返程；EXP-039 再次确认旧 DLL 的末段 move 必须由明确 Mine 终态覆盖；EXP-040 以煤节点 `346` 的两次范围内守恒采集验证正确命名空间流程。EXP-033/034 更新为输出仓 `260`、分拣器 `261`、风机 `262` 下的连续红糖与网络 2 满供电现场。新增 EXP-042，区分原始桥 `preferredPosition` 向量和 MCP 标量包装，并要求精确建造在 commit 前校验 `plannedPosition`。
- 2026-08-31：动力引擎里程碑复核：新增 EXP-043，以矿机 `263`、两段新矿带和侧向分拣器 `282` 恢复枯竭铁矿链；新增 EXP-044，以空仓停源和 sorter `70` filter 隔离公共带铁块背压，磁线圈主仓由 18 增至 60；新增 EXP-045，记录仓 `286` 落点合法但 sorter `TooFar`，以及近仓 `287` 的成功对照。动力引擎仓由 9 增至 30，保存动作 `901f4289-0155-484e-ac14-4c6ecb442aa3` 确认同档 tick `3746997`、revision `307`、写入健康。
- 2026-08-31：新增 EXP-046，并据反例修订 EXP-035：机甲核心提升至 200 MJ 后，从煤点返程仍因旧 DLL move 终态残留耗尽；普通生产路点只有约 80 kW 基础恢复，不能当作充电目标。读取无线塔 `180` 的真实坐标后到达 2.47 m，8 秒净增约 20.765 MJ，确认自动回充闭环；撤销“高于 50% 即可返程”的单阈值。
- 2026-08-31：机甲核心 II/驱动引擎 II 分别在 tick `3932513/4013644` 完成；以范围内 Mine 清除旧 Move 后，从石墨仓 `114` 守恒转移并加注 100 高能石墨，核心达到 `400/400 MJ`、燃料格余 91，动作 `0e59ee2f-5d49-44e6-bfd9-119bfe08c8c1` 正常保存主档 tick `4204523`。新增 EXP-047，把用户要求的“起飞前独立存档、失败反复加载同一档”固化为保存头证明、保护票据、严格采用与可重复 reload 的离线实现；完整解决方案 0 warning/0 error、57 tests passed，live 待安全部署。
- 2026-08-31：用户要求关闭游戏前，动作 `387a4629-f1b4-4c40-ad6b-10f15e840219` 通过正常 save API 再次保存同一 owned 主档 tick `4409247`；最终 revision `354`、`ownedSaveState=saved`、`writeHealth=healthy`。该点被指定为下次唯一接续点。随后进程 `24828` 接受正常窗口关闭并退出，未强杀；runtime descriptor 已清理，固定 `_lastexit_.dsv` 于 `2026-08-31T14:54:27Z` 更新。现存两个恢复票据绑定旧进程/旧 session，与本局不匹配，不能用于恢复；下次必须先为该精确主档重建 owned-session 证明。新需求“每个新档分别记录物品首次手搓/首次产线产出、科技首次点击、升级首次点击，并同时记录实际时间和本局时间”只完成初步 API 审计，尚未形成经验条目或代码实现，下一次恢复该档后继续。
- 2026-09-01：新增 EXP-048；把逐存档日记落为独立的手搓 feature counter、自动生产 register、科技页分类和双时间记录，旧档迁移明确不补造历史时间。公开面增至 44 个工具；完整构建 0 warning/0 error，62 tests passed，等待同档安全恢复后的实机复验。
- 2026-09-01：同一主档通过受保护 handoff 与 fixed LastExit 严格恢复为 session `9e626e04-1b5e-452f-a8ab-27c59a450e51`，planet `104`、和平/非沙盒/1x、tick 高于 `4409247` 且自动重存健康；EXP-039 的 180-tick 停滞看门狗完成 live 验证，EXP-048 补上旧档日记挂接和首次科技选择证据。新增 EXP-049，以蓝矩阵混料短带的双向死锁及两条独立输入专线修复形成正反对照。
- 2026-09-01：EXP-047 升级为 live `validated`：独立检查点 tick `4617708` 经同一 token 多次重载后继续起飞并物理着陆 planet `102`。新增 EXP-050，记录当前版本 Fly-to-Sail 精确分支、持续原生输入、母星遮挡判断与径向/切向离场；源码同时修复目的星 Fly 落地阶段被起飞超时误报的顺序问题，等待返航部署复验。
- 2026-09-01：新增 EXP-051；把两次钛星长距离移动归纳为 30 m 球面 slerp 分段、逐段 prepare/commit、终态惯性 settling 和仅 commit 前 stale 重读，并落为 `scripts/invoke-surface-route.ps1`。1000 钛与首批 99 煤均以矿脉减少/背包增加守恒完成，返航燃料补给继续按实际能量读回推进。
- 2026-09-01：新增 EXP-052；首版落地顺序修复被 5 秒复读证伪为瞬时 Walk→Drift，立即从返航检查点 `4808424` 恢复。加入 600-tick 连续稳定与 7200-tick 接触超时后，同一检查点返航动作 `d95955e7-cd86-48dd-b79f-4cb54734863c` 正常完成，10 秒后位置/速度仍稳定；1000 钛带回母星并由正常保存动作 `c6d7c88e-0c36-4c15-af29-3844a124ddc5` 落到主档 tick `4819163`。
- 2026-09-01：新增 EXP-053/054；`invoke-surface-route.ps1` 的 settled 判定收紧为连续等待 Walk 且速度不高于 `0.1 m/s`，实机区分水面 Drift、建筑卡停和陆地到达。红矩阵仓 `260` 经 196 条普通带、源分拣器 `342` 和末端分拣器 `529` 自动接入研究站 `84`，红缓冲 `0 -> 5900`、高分子化工 hash `0 -> 428`，并正常保存同档 tick `5046241`。
- 2026-09-01：新增 EXP-055，以石墨仓到化工厂的直接 `TooFar` 反例和两格带中继、双分拣器、塑料 `0 -> 2 -> 38` 完成现场闭环；新增 EXP-056，把精炼油满仓导致共同产物停机以及守恒腾位后的恢复固化为待继续复验的诊断规则。逐存档日记同时首次实机记录 `production_line_item_first` 塑料事件（tick `5263306`），同档正常保存 tick `5265117`。
- 2026-09-01：新增 EXP-057/058；两次建筑基座卡脚由局部切向外移恢复，蓝矩阵旧线则以 sorter `551` 实际携带磁铁推翻“目标设备会替上游整带筛料”的假设。拒绝绕过活跃分拣器的 stale 安全检查，改用回收仓和纯源双旁路；研究 `1122` 恢复连续增长，正常保存动作 `cb5ced55-36cd-4f80-9d60-6e497bdc732d` 把修复后的同一主档持久化到 tick `5407742`。
- 2026-09-01：新增 EXP-059–063；第二研究站的交替工作把瓶颈重新定位到矩阵供给，第二精炼厂按空载过滤后通料并补齐端点/下游两级供电，多基座夹缝形成有界四向短探测经验；钛锭线在科技解锁后以自动产出、日记和保存完成里程碑。另记录活跃科研上传导致完整 progression 哈希逐 tick 变旧，未放宽任何 stale 校验。
- 2026-09-01：最终交接前再次复核逐存档日记，序号 8 仍是自动钛块首产事件；动作 `507870de-2b19-45bc-af7a-1af2dec0d481` 通过正常 save API 保存同一主档 tick `5731056`，revision `591 -> 592`、`ownedSaveState=saved`、`writeHealth=healthy`。完整解决方案重新构建为 0 warning / 0 error，62 tests passed。进程 `39104` 随后接受正常窗口关闭并退出，未强杀且 live descriptor 清零；关机后才安装新 Plugin，SHA-256 `E6B48E498AABFE2A2EFD28789840684226E3C59A0B9219FD17BFA0AEA6044956`。下次只恢复该精确保存点，先重建 owned-session 证明并 live 验证 `water-pump` 目录分类，再执行任何游戏写入。
- 2026-09-01：新增 EXP-064；新 Plugin 主菜单再次复现恢复字段省略和工具层票据不可见，未降级枚举/选择存档。把原票据原样送入当前用户保护的 handoff 路径后，fixed LastExit 恢复动作 `633c24f3-c2d8-4b64-b798-ee2d1edebf41` 成功；额外以加载 tick `5751758 >= 5731056`、planet `104`、和平/非沙盒/1x、健康写入和日记序号 8 完成最新进度验收。新 DLL 同时 live 读到 item `2306` 的 build-catalog role 为 `water-pump`；建造能力仍待正常物品与原生施工验证。
- 2026-09-01：新增 EXP-065，并修订 EXP-060；恢复后聚合复读发现仓 `163` 再次混有 13 氢，无过滤 sorter `675` 已把原计划油仓 `557` 污染为 271 氢/33 精炼油。撤销“永久纯油旁路已完成”的表述，保留第二精炼厂过滤顺序、热电恢复与两级供电的有效证据；下一写入先按守恒清理和纯源/空载过滤规则处理。
- 2026-09-01：新增 EXP-066，并修订 EXP-053；实机连续两次证明两个已验证 Walk 锚点间的球面 slerp 仍可跨水。把首次 Drift 即终止整条路线、精确 Walk 落点回收和 50% 能量保底固化为下一次移动的前置；未将其误报为全局寻路已完成。
- 2026-09-01：完成恢复后第一组超过 10 个成功游戏写动作复核（陆地锚点移动、Drift 回收、无线充电、建材转移与范围内采石）。EXP-021、EXP-031、EXP-035、EXP-046、EXP-053、EXP-057、EXP-061 与当前守恒/能量/终态读回仍一致；EXP-053 的路线范围已由 EXP-066 的跨水反例收紧，未发现 quarantine、结果未知或不明物品正增量。
- 2026-09-01：新增 EXP-067，并修订 EXP-058 的现行范围；恢复后长窗复读发现科研因蓝矩阵断供停在 `41940/90000`，而既有电路板输出分拣器 `572` 已失去制造台 `36` 一端。保留该旁路曾经连续产出的历史证据，撤销其“当前仍运行”的隐含前提；下一步先正常修复自动输出并观测蓝矩阵/科技增长。
- 2026-09-01：恢复后第二组 10 个成功游戏写动作复核完成（组件/分拣器/熔炉手搓、已验证锚点移动与电路板输出修复）。EXP-018、EXP-031、EXP-037、EXP-053、EXP-058、EXP-062 的手搓守恒、步行终态和自动产出验收前提仍一致；新分拣器 `714` 已让 `1403` 在 20 秒内增长 1049 hash。路由脚本同批落实 50% 能量保底和 Drift 首帧断路，未放宽 stale、能量或终态检查。
- 2026-09-01：恢复后第三组超过 10 个成功游戏写动作复核（钻石/有机晶体/黄矩阵建材手搓和炼油区锚点移动）。EXP-018、EXP-035、EXP-036、EXP-051、EXP-053、EXP-057、EXP-061 与当前结果仍一致；唯一失败动作在液罐 `165` 基座/带 `178` 处由停滞看门狗明确终止，后续背离基座的三段路由全部 Walk/速度 0，写健康仍为 healthy。
- 2026-09-01：新增 EXP-068，并据第二组实机复现修订 EXP-067 的原因范围；新熔炉 `715` 的后建输出 sorter 在同一 slot `0` 覆盖了先建输入，与制造台 `36` 的历史断线完全同型。当前源码又证明非带 inserter 候选漏掉已占槽过滤，而完工读回只看新 sorter 自身。修复版部署前暂停继续多端接线，未将这一确定性拓扑损坏误报为恢复/账号问题。
- 2026-09-01：新增 EXP-069；在正常保存主档 tick `5938815` 后，用当前认证健康 Bridge 签发了不泄漏身份/token、只允许当前用户访问的计划重启 bootstrap handoff。源码同批实现今后每次健康 save 自动按最新 tick 同时签发运行/固定 handoff 票据；完整构建 0 warning/0 error、65 测试通过，live 恢复与下一次自签发待紧接着验证。
- 2026-09-01：EXP-069 完成首次 live 闭环；旧进程正常关闭、新 DLL 安装与 bootstrap fixed-LastExit 恢复均成功，新 session 的 tick/星球/模式/日记/新建实体全部越过交接门槛。更重要的是，恢复后首次自动 save 已按 tick `5965043` 签发新的固定 handoff 票据，可观测性缺口在新 DLL 上暂未复现；保留下一次真实重启作为 validated 触发。
- 2026-09-01：EXP-068 完成修复版 live 复验；新 sorter `720` 为熔炉 `715` 使用独立输入槽 `8`，同时保留原输出 sorter `719` 的槽 `0`，源仓 `716` 也同时保留旧 sorter `718` 并新增槽 `3 -> 720`。下一步用同一规则修复电路板制造台输入并以科技 hash 增长复验生产流量。
- 2026-09-01：新增 EXP-070；修复科研输入时，新 sorter `721`、物品差量和 `580 slot5 -> 721 -> 36 slot1` 均唯一明确，但当前 DLL 把 prepare 的 belt 虚拟槽 `-1` 当成实际槽反查，导致可证明结果被误隔离且 reconciliation 同源失败。源码改为只对虚拟 belt 端扫描真实槽，非带端继续严格核对 prepare 槽位；等待构建、正常同档重启和下一条 live 动作复验。
- 2026-09-01：EXP-069 升级为 `validated`；第二次部署没有 bootstrap，直接消费上一次 Plugin 健康保存自签发的 handoff。fixed LastExit 恢复动作 `4098eea2-82bf-4546-929d-aa6c675e9aa4` 后 tick/星球/模式/日记/实体 `720/721` 全部越过门槛，写入恢复 healthy，并立即再次生成下一张健康重启票据。
- 2026-09-01：新增 EXP-071/072；主线程停滞后的正常窗口关闭没有刷新 LastExit，旧门槛正确拒绝恢复。新增只认 protected ticket 内唯一主档的 fresh-primary 兜底，72 测试与 live 恢复均通过，未落盘候选 `722` 明确不存在。部署途中还用 `TypeLoadException` 证明 Plugin/Core/Contracts 必须来自同批输出并一起校验安装。
- 2026-09-01：新增 EXP-073，并补充 EXP-067；修复电路板铁块入口后没有把局部流量当作完成，而是继续追到蓝矩阵缺线圈、线圈制造台缺铜。新 sorter `722` 保留既有端口并恢复铜块输入，随后蓝矩阵双输入增长、科技 `1403` 在 25 秒内增加 1569 hash，完成最终消费者闭环。
- 2026-09-01：复验 EXP-007/066；钻石备料首次转移已成功却被 PowerShell 空聚合后处理错误遮住，fresh 双边读回明确终态后才执行新的额外转移，公共 action client 同步改为错误即停。`133 -> 143 -> 182 -> 165 外缘 -> 713 -> 120` 五个反向锚点全部 Walk/速度 0，液罐保留 5 m 容差且核心仍为 392.2 MJ；随后 400 石墨守恒进入钻石输入仓 `716`。
- 2026-09-01：第二次复验 EXP-062；晶体冶炼解锁前只预建空闲钻石物流，tick `6081424` 解锁后才配置熔炉 `715` 的配方 `60`。输入仓 400 石墨持续下降、输出仓金刚石 `0 -> 42 -> 47`，日记序号 9 独立记录首个自动金刚石；普通保存动作 `2e9ca24b-57dc-40c6-900b-a210b8fc03e7` 将同一主档持久化到 tick `6090507`。
- 2026-09-01：复验 EXP-063；`1403/1701` 完成后的空队列提供稳定 progression 窗口，动作 `acaa5327-5e2b-4139-b0cc-49c0b18c5d40` 正常选择 `1123`，日记序号 10 记录其首次点击。保留活跃上传时 stale 的完整防并发行为，不为减少科研空档而放宽校验。
- 2026-09-01：完成金刚石里程碑后的 10 个成功写动作复核（解锁后配置、普通保存、稳定窗口选择 `1123`、两批基础设施手搓和五段已验证锚点返程）。EXP-007、EXP-018、EXP-037、EXP-048、EXP-062、EXP-063、EXP-066 与当前动作终态、日记、守恒和保存边界一致；全程无 Drift、quarantine 或未解释物品差量，返程核心仍为 391.9/400 MJ。
- 2026-09-01：EXP-070 升级为 `validated`；修复版在电动机线连续完成 `726 -> 740`、`735 -> 724`、`732 -> 726` 三条 machine↔belt sorter，虚拟 `slot=-1` 都通过扫描真实连接槽完成双端验收，没有再产生错误隔离。
- 2026-09-01：新增 EXP-074；电动机共享备料仓在过早装料后先守恒清空，再于空载状态完成四个精确过滤出口，最后分批装入铜/磁铁/铁。四条源分拣器实际只携目标物，齿轮、磁线圈和最终电动机全链连续工作，专用仓达到 39；日记序号 11/12 独立记录首个产线齿轮与电动机，普通保存动作 `d0516fbf-b333-4266-aed1-dbdd5cd53e37` 持久化 tick `6221009`。
- 2026-09-01：完成电动机建造批次的集中成功写动作复核（基础设施手搓、五座设备/仓与两段带路、两座电塔、共享仓清空/重装、九个分拣器、四个空载过滤和里程碑保存）。EXP-018、EXP-021、EXP-028、EXP-037、EXP-048、EXP-062、EXP-068、EXP-070、EXP-073 与全部终态、库存守恒、双端拓扑、日记和保存边界一致；无 quarantine、outcome unknown、Drift 或未解释物品增量。EXP-065 的混料仓反例仍成立，新增 EXP-074 只在“空仓先过滤”前提下给出正样本。
- 2026-09-01：新增 EXP-075 并再次复验 EXP-007；抽水站 build commit 后只有结果展示访问不存在字段报错，fresh 背包/唯一实体/供电/出水复读证明动作已完成，未重放。原生施工泵 `752` 后，直连 sorter 被端口模型安全拒绝；改走固定泵口 `752 -> 758…754 -> 759 -> 753`，专用仓水 `9 -> 31`，日记序号 13/14 分别记录首次手搓泵和首次产线水，普通保存动作 `44f2f0d0-9713-4e35-9073-45f1ce5c7787` 持久化 tick `6267723`。
- 2026-09-01：EXP-074 升级为 `validated`，并新增 EXP-076；第二个空仓先过滤样本 `761 -> 763/764/765 -> 760` 让塑料/油/水三原料化工线连续产出有机晶体 `1 -> 7`。去塑料仓途中唯一碰撞由 180-tick 看门狗在余 5.47 m、核心约 391.6 MJ 时终止；fresh 原业务 transfer 已在范围内，故没有继续撞仓，正常取得 200 塑料后沿反向锚点离开。日记序号 15 记录首次产线有机晶体，保存动作 `06cfa947-25da-490f-9e52-895989ff8e7a` 持久化 tick `6315704`。
- 2026-09-01：完成水里程碑后的集中成功写动作复核（基础材料转移/手搓、化工厂与双仓建造、配方配置、四个 sorter、三个空载过滤、三原料守恒备料、往返已验证锚点和普通保存）。EXP-007、EXP-018、EXP-021、EXP-028、EXP-031、EXP-035、EXP-036、EXP-037、EXP-048、EXP-061、EXP-066、EXP-068、EXP-070、EXP-073–076 与动作终态、双边差量、端点、电力、日记和保存边界一致。唯一 `action_failed` 是已解释的塑料仓基座停滞；无 Drift、quarantine、outcome unknown 或未解释物品增量。
- 2026-09-01：EXP-056 由第二次“精炼油满仓→守恒腾位→氢长带→红矩阵→科研增长”的完整复现升级为 `validated`；新增 EXP-077，并复验 EXP-061：钛晶石预建区虽然每座建筑均原生合法，却把玩家夹在输入仓 `768`、电塔 `133` 和 sorter 间。长 Move 被看门狗及早终止后，首个 4 m 合成切向候选即脱困，核心仍接近满值；后续紧凑施工将玩家撤离走廊纳入验收。
- 2026-09-01：继续复验 EXP-056/066/077；风机 `35` 南向两条未验证弧均在首段 Drift 即停并精确回收，未把端点合法误当路径合法。油反压则以原生 `NeedGround` 安全拒绝和合成陆地方向正例选出仓 `773`，施工前验证玩家净空、落点误差与业务范围；累计 800 油进入 `773/286` 后科研由 `63360` 推进到 `86040`，全过程保留油库存。
- 2026-09-01：第三次复验 EXP-062；高强度晶体在 tick `6509179` 正常解锁后才激活预建制造台 `767(recipe 26)`，输入仓 40/120 自动下降、输出仓出现 8 钛晶石，日记序号 16 捕获首产。结构矩阵科技 `1124` 随后正常入队并写入序号 17；普通保存动作 `39cee465-5520-4a8c-a1e3-68ac8e6208ab` 将同档持久化到 tick `6518917`。
- 2026-09-01：新增 EXP-078，并复验 EXP-065/066/077；黄矩阵空载物流完成双过滤与独立供电，40 钛晶石/40 金刚石均已守恒备料。污染油仓 `557` 清除 271 氢后重建过滤入口，纯源仓 `784` 经五格中继接回旧长带并恢复塑料、炼油、红糖与结构矩阵研究连续增长。`82` 区域再次证明机械中点会入水；旧仓 `768` 夹缝则补充“短移后仍须复读并侧向绕出”的边界。
- 2026-09-01：再次复验 EXP-061/066；为加速结构矩阵研究而前往石矿时，旧 `82 -> 无线塔 180` 三段路线被后来铺设的带 `689/690` 与矿机 `3` 截断。两次明确停滞均由 180-tick 看门狗停止；仓/制造台夹缝由首个合成切向脱困，带/矿机夹缝则在首候选失败后由正交第二候选脱困。路线证据因此收紧为“绑定当时工厂拓扑”，没有重放失败目标或触发隔离。
- 2026-09-01：再次复验 EXP-078；纯油中继 `784` 排空且仓 `163` 达 560 油时，两座精炼厂的油输出均已积 40。第二轮 500 油 exact transfer 使源仓降到 62、中继首读 499，8 秒后双厂恢复工作、结构矩阵研究增长到 229338；经验触发点从“科研停摆”前移到“中继空且输出缓冲满”。
- 2026-09-01：新增 EXP-079；结构矩阵科技在 tick `6894549` 正常完成后才激活预建 lab `774(recipe 27)`。双过滤输入各由 40 降到 23、输出仓 `7 -> 10`，日记序号 18 捕获首次产线黄糖，普通保存动作 `d6e2d8d5-9675-4eb9-a64b-b05403d0af9f` 将同档持久化到 tick `6905142`、revision `455 -> 456`。
