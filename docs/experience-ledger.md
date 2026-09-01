# Spherewright experience ledger

更新时间：2026-09-01（Asia/Singapore）

本文件是 Spherewright 实现、DSP 实机控制、运行环境与安全处置经验的权威账本。它记录“目前为什么这样做”以及“什么情况下必须重新检查”，不是成功日志，也不替代 `docs/research/` 的 API 证据或 `docs/m0-status.md` 的 Gate 验收状态。

## 维护协议

- 状态只取 `observed | validated | superseded | invalidated`。
- `observed` 表示证据真实但适用范围尚窄；不得自行外推为稳定 API 或通用阈值。
- `validated` 表示当前写明的适用范围内已有独立复读、自动化测试或当前版本实机证据。
- `superseded` 必须指向替代条目；`invalidated` 必须说明反证。历史不删除。
- 每个实现批次结束、每累计 10 个成功游戏写动作、Plugin 部署或重启、DSP/程序集版本变化、写入隔离或恢复、M0 Gate 状态变化以及最终交接前，复核新增条目和所有受影响条目，并更新“最近复验”。
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
- 最近复验：2026-08-31。

### EXP-002 — DSP 应通过 Steam 启动链启动

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 Windows Steam 安装的 DSP `0.10.34.28529`。
- 当前结论：直接启动 `DSPGAME.exe` 会很快退出；本机可靠启动路径是 Steam `-applaunch 1366540`，随后再发现实际游戏进程和 Bridge descriptor。
- 直接证据：直接启动进程退出；Steam 启动后 DSP、BepInEx 和 Spherewright Plugin 正常加载。
- 限制或反例：非 Steam 发行版或未来启动器未验证。
- 复验触发：游戏安装来源、Steam app ID、启动脚本或游戏版本变化。
- 关联：`scripts/locate-dsp.ps1`、`docs/research/environment.md`。
- 最近复验：2026-08-31。

### EXP-003 — 运行时描述文件使用 `bridge-*.json`

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前 RuntimeDescriptorPublisher 与本机 MCP 发现流程。
- 当前结论：descriptor 的实际文件模式是 `bridge-*.json`；自动化脚本不得猜测为 `spherewright-*.json`。
- 直接证据：当前 Plugin 发布并由 MCP 成功发现的文件名与代码模板一致。
- 限制或反例：若发布协议显式改名，脚本与文档必须原子更新。
- 复验触发：RuntimeDescriptorPublisher、协议或发现脚本变化。
- 关联：`src/Spherewright.Plugin/RuntimeDescriptor/RuntimeDescriptorPublisher.cs`、`src/Spherewright.Mcp/BridgeClient/NamedPipeBridgeClient.cs`。
- 最近复验：2026-08-31。

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

- 状态：`observed`
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
- 限制或反例：若 prepare 在任何 commit 前明确失败且无 action ID，可按普通 prepare 失败处理。
- 复验触发：客户端响应模型、ActionResult 字段或脚本 helper 变化。
- 关联：`src/Spherewright.Contracts/Actions/ActionResultContracts.cs`、`docs/protocol.md`、`docs/safety-model.md`。
- 最近复验：2026-09-01（泵体已建成但可选字段/空聚合展示报错；只用背包、唯一实体、供电和出水 fresh 读回确认终态，未重放）。

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
- 关联：`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`docs/manual-test-m0.md`。
- 最近复验：2026-08-31。

### EXP-010 — 三台风机只够覆盖当前油井的紧负载

- 状态：`invalidated`
- 日期：2026-08-31
- 适用范围：当前普通 1x 世界、实体 `129` 油井和当前版本电力数值。
- 当前结论：旧结论已失效，由 EXP-017 替代。`14000` 是本轮早期记录错误，不是当前 DTO 的油井每 tick 需求。
- 直接证据：10 动作复核时，实体 `129` 的 `powerDemandPerTick=400`；合网后网络 3 的两个消费者（油井与分拣器）总 `energyRequired=550`、`energyCapacity=51000`。热电接入前同一风电网容量为 `15000`，不是“仅余 1000”的紧负载。
- 限制或反例：保留本条用于防止旧数字再次传播；不得继续用于容量规划。
- 复验触发：供电建筑、科技加成、网络拓扑、DSP 版本或油井参数变化。
- 关联：`docs/m0-status.md`、`docs/research/game-api-m0.md`。
- 最近复验：2026-08-31（复验失败，已 invalidated）。

### EXP-011 — 当前储仓/热电姿态在约 6.41 m 成功、8.90 m 与 12.8 m 失败

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前版本、小型储仓 `135` 到热电站 `134` 的具体端点姿态。
- 当前结论：储仓 `135`（约 12.8 m）和储仓 `136`（实测约 8.90 m）到热电站 `134` 均为 `TooFar`；储仓 `137`（实测约 6.41 m）到热电站的同类基础分拣器则通过 prepare、正常建成实体 `138` 并实际输送燃料。后续仍必须让 DSP 原生校验决定，不能把 6.41 m 直接当作通用最大距离。
- 直接证据：两次失败 prepare 均未消耗分拣器；成功动作 `8130b214-e4c8-47d4-9a78-cd5975341725` 消耗 1 个分拣器并创建 `137 -> 134` 的实体 `138`，供电后储仓石墨从 18 降至 8、热电站出现石墨燃料读回。
- 限制或反例：判定可能取端口、建筑旋转、碰撞或网格姿态而非建筑中心距离；三个距离都只约束当前建筑类型和具体姿态。
- 复验触发：成功建立更近连接、端口距离计算研究、建筑模型或 DSP 版本变化。
- 关联：`docs/manual-test-m0.md`、`docs/research/game-api-m0.md`。
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
- 复验触发：用户调整优先级、M0 Gate D 完成或出现安全阻断。
- 关联：`AGENTS.md` Gate D、`docs/m0-status.md`。
- 最近复验：2026-08-31。

### EXP-016 — 孤立热电站不能依靠无电分拣器完成冷启动

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前热电站 `134`、燃料储仓 `137`、输入分拣器 `138` 和网络 4。
- 当前结论：热电站无燃料时孤立网络容量为 0，输入分拣器也因此无电并停在 `Picking`，不能把首份燃料送入；必须先用已有风电网络/电线杆覆盖分拣器，或采用另一条已有电源的正常物流路径完成冷启动。
- 直接证据：连续 10 秒结构化复读中，储仓始终有 18 个高能石墨，分拣器 `137 -> 134` 拓扑正确但网络 ID 为 0、阶段为 `Picking`，热电网络 4 容量和产出均为 0；新增电线杆 `139`、`140` 后网络合并为 3，分拣器 serve ratio 为 1.0，储仓降到 8，网络容量由 15000 增至 51000。
- 限制或反例：热电站一旦已有燃料或网络已连接其他电源，行为会不同；不能据此推断所有发电设备的启动规则。
- 复验触发：网络 4 接入启动电源后、首份燃料进入后、电力读取或 DSP 版本变化。
- 关联：`docs/research/game-api-m0.md`、`docs/manual-test-m0.md`。
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
- 当前结论：无线塔建成不能只看实体存在；应同时证明塔接入有容量的电网、玩家在覆盖范围内、反应堆/燃料为空，并在连续快照中看到核心能量上升。当前塔距玩家约 `10.42 m`，满足这条证据链。
- 直接证据：运行时配方链正常消耗 12 铁矿、7 铜块、18 石矿以及 2 铁块，逐级产出 14 磁线圈、9 玻璃、6 棱镜、3 电浆激发器、1 电力感应塔和 1 无线输电塔；动作 `efb211e2-2d4b-4917-b55e-3cdf31b3506a` 创建实体 `180`。建成后网络 1 节点 `18 -> 19`、需求 `6350 -> 7850` 且全供电；在 `reactorEnergy=0`、燃料格为空的约 10 秒内，核心能量从约 `35.77M -> 36.61M`。
- 限制或反例：`10.42 m` 只证明该点在覆盖范围内，不是无线塔最大半径；网络需求差量的内部单位不能直接当 UI 瓦数。
- 复验触发：玩家离开/进入覆盖范围、无线塔或电网参数、能源 DTO 或 DSP 版本变化。
- 关联：`docs/research/game-api-m0.md`、`src/Spherewright.Plugin/Game/GameStateReader.cs`、EXP-019、EXP-022。
- 最近复验：2026-08-31。

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

- 状态：`observed`
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

- 状态：`observed`
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
- 最近复验：2026-08-31。

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
- 适用范围：当前普通多段传送带 build action、玩家 before/after inventory 差量与正在输送氢的带路。
- 当前结论：从一段正在输送物品的末端 belt 继续构造下一段时，当前路径实现会在接续点创建同位的新首段，端点上的 1 件货物可按正常建造行为回收到玩家背包。build commit 仍严格验证建材精确消耗；非建材正差量必须单独记账，不能算作配方产出或生产线验收，也不能据此重放建造。
- 直接证据：第二段氢主干动作 `3870525c-14ce-4e41-936c-36984d560858` 消耗 25 条带并使玩家氢 `0 -> 1`；第三段动作 `8f733ab2-a4da-4616-ae03-ee5778299ba3` 消耗 22 条带并使氢 `1 -> 2`。两次均从已有活跃氢带末端继续建造、均恰好回收 1 氢，session 保持 `healthy`，既有分段边界和完整带路拓扑复读成立。代码在 `CreatePreparedPrebuildsOnMainThread` 对建材执行 `baseline - previews.Count` 精确检查，最终 action DTO 汇总完整施工窗口的库存差量。
- 限制或反例：已验证的是当前版本、基础传送带和氢货物的接续样本；内部究竟在 `CreatePrebuilds` 哪一步回收货物尚未以 IL 单独归因。背包中的 2 氢不用于首个自动红矩阵证据链。
- 复验触发：其他货物/带级接续、改用非同位续带、动作库存审计或 DSP 版本变化。
- 关联：EXP-007、EXP-018、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.cs`。
- 最近复验：2026-08-31（两个独立续带动作均出现精确 `+1` 氢，当前适用范围内 validated）。

### EXP-033 — 首个自动红矩阵必须以同一研究站的 0→正数闭环验收

- 状态：`validated`
- 日期：2026-08-31
- 适用范围：当前普通 1x 世界、研究站 `256`、配方 `18` 与能量矩阵 item `6002`。
- 当前结论：首个自动红矩阵的充分证据是同一生产研究站在接入原料前输出为 0，接入两条正常物流后输入/进度/输出连续变化，并且输出增长到至少 1；玩家背包或手搓结果不能替代该闭环。完成后还要通过显式 save action 保存精确 owned world。当前 Gate D 的自动产出、最终保存和下游连续出料均已完成。
- 直接证据：配置动作 `dc3e404f-9c69-48d9-860f-897bcea2f834` 后，研究站 `256` 明确为配方 18，石墨/氢/能量矩阵 buffers 全为 0、网络 2 全供电。动作 `bfe37097-76ff-4195-b16e-b450f1a3e568` 创建 `114 -> 257 -> 256` 石墨输入，动作 `7ceb1cf9-345d-4d74-9b55-abc1954dbd18` 创建 `255 -> 258 -> 256` 氢输入。随后只读快照中输出 `6002` 为 3，20 秒后为 6，之后累积到 10；显式保存动作 `b399facb-48cd-4838-b7ab-9c9762b6def7` 由 DSP 正常 save API 确认 tick `2499658`。后续动作 `750c7803-c967-4996-a056-63fcb0efcac8` 建成输出仓 `260`，动作 `7ae664fe-246f-41e8-bf85-f68270bf3262` 建成 `256 -> 261 -> 260` 出料分拣器；复读时仓内能量矩阵已为 22，研究站输出缓冲为 0、`isWorking=true`、双输入各 4，证明满缓存恢复为连续生产。
- 限制或反例：玩家背包曾因两次活带续接存在 2 个另行记账的氢，已按 EXP-032 排除。输出仓内的 22 个矩阵包含此前缓存的 10 个和接通后新生产的至少 12 个，不能把整个 22 都记作接通后的新增产量；但相同设备恢复工作、输入减少和仓储超过原缓存上限共同证明连续流成立。
- 复验触发：配方/研究站/上游改造、输出取走、后续显式保存或 DSP 版本变化。
- 关联：EXP-015、EXP-020、EXP-028、EXP-032、`docs/m0-status.md`。
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
- 最近复验：2026-08-31（新增风机 `262` 后运行态满供电）。

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
- 关联：EXP-005、EXP-033、`docs/m0-status.md`、`docs/handoff-next-computer-agent.md`。
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
- 直接证据：当前程序集 IL 显示每个 `ForgeTask.Produce` 后调用 `AddFeatureValue(2140000 + recipeId, 1)`，包括不触发顶层 `onTaskDelivery` 的嵌套前置手搓；`Mecha.AddProductionStat` 直接调用 `AddProductionToTotalArray`，不写 `productRegister`，而矿机、制造、分馏、研究站、物流、电力与戴森生产 tick 均引用该寄存器。`TechProto.page` 对 ID `<2000` 返回 0，否则返回 1。实现以 owned-save 内部身份的 SHA-256 派生日记 ID，在当前用户保护目录原子持久化，并新增 owned-only 只读 MCP 工具；完整解决方案 0 warning/0 error，62 tests passed（Contracts 4、Bridge.Core 45、MCP 13）。修复版部署并严格恢复同档后，日记以 `attached_existing_save`、`historicalCoverageComplete=false` 从 tick `4428079` 挂接，保护目录生成一个 SHA-256 派生文件；正常点选基础化工 `1121` 后新增 `technology_first_selected`，实际时间 `2026-09-01T00:49:36+08:00`、游戏 tick `4462081`、局内时间 `000d 20:39:28` 三者同时可读，未补造任何旧事件。
- 限制或反例：当前主档早于该功能存在，无法从统计恢复过去事件的真实墙钟时间。首次附着会把已有手搓、生产和科研 ID 作为无时间的 historical seed，并明确返回 `historicalCoverageComplete=false`；不得把迁移时刻伪称旧物品的首次时刻。新档从 Spherewright 采用帧开始才具有完整覆盖。当前已验证文件创建、旧档迁移和首次科技选择；首次此前未出现物品的手搓/产线双事件、首次升级选择与下一次跨进程持续性仍待样本，因此本条暂不升级为完全 `validated`。
- 复验触发：本次安全部署、首次日记文件创建/读取、首个此前未出现物品的手搓与产线双事件、首次科技/升级选择、跨进程恢复、DSP 生产统计或 feature ID 行为变化。
- 关联：EXP-005、EXP-037、`src/Spherewright.Plugin/Game/GameplayJournalManager.cs`、`src/Spherewright.Bridge.Core/Journals/GameplayFirstOccurrenceDetector.cs`、`docs/research/game-api-m0.md`。
- 最近复验：2026-09-01（旧档挂接、保护文件和基础化工首次科技选择已 live；物品双事件与升级仍待复验）。

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
- 当前结论：逐段移动结束后，只有 `movementState=Walk` 且速度 `<=0.1 m/s` 才能继续；`Drift` 即使速度接近 0 也会持续耗能，不能当作 settled。路线首次进入 Drift 时立即停止并复读附近实体/矿脉，优先移动到已现场证明为 Walk 的精确落点，而不是只瞄准建筑中心；建筑另一侧可能仍在水里。即使起终点都是已验证陆地，两点间的球面 slerp 弧段仍可穿过水面；陆地身份不具有“连线仍在陆地”的传递性。Drift 中位置每 tick 变化，普通 read→prepare→commit 会频繁 `STALE_STATE`，恢复动作应在同一进程内有界重复“fresh read→prepare→立即 commit”，只重试未提交的 stale，取得 commit 后绝不重放。
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
- 当前结论：下游设备只接受某种配方输入，不代表上游无过滤分拣器会替整条带筛料；从混料仓接出的所谓专线必须在接触物料前设置并复读过滤项，或直接从只含目标物品的仓/生产设备另建旁路。活跃分拣器若持续在 `Picking/Inserting` 与携货间变化，prepare 所绑定的空载状态可能逐帧过期；不得放宽 `STALE_STATE`、强写过滤项或反复提交。此时先给旧带增加守恒回收仓使其排空，再新建纯源输入/输出通道。
- 直接证据：分拣器 `551` 为无过滤的 `26 -> 542`，现场明确处于 `Inserting` 且携带 item `1102` 磁铁，证明它已把混料仓内容送入原计划的电路板带；研究站 `76` 因电路板断供停止。20 秒只读观察没有出现合格空闲窗口；增加回收仓 `562` 和分拣器 `563` 后，25 秒内虽捕获 7 个候选空窗，但所有过滤 prepare 都被 fresh readback 以 `STALE_STATE` 拒绝，没有提交任何过滤写入。回收仓随后持续接收磁铁，现场从至少 373 增至 381。
- 直接证据：独立旁路使用 `36 -> 572 -> 571…565 -> 573 -> 76` 输送电路板，并用纯铁块仓建立 `28 -> 594 -> 593…580 -> 595 -> 36` 的 20 带输入。两端分拣器均先单独 prepare 通过后才提交；`594/595` 实际携带 item `1101`，组装机 `36` 输出 item `1301`，研究站 `76` 的电路板输入为 6 且持续工作。高分子化工 `1122` 在 15 秒内从 `35643 -> 36180/72000`，随后继续到 `37800`，证明研究恢复而非一次性手塞。
- 限制或反例：当前旁路仍借用混料仓侧的既有铜块输入，长期还需观察仓 `26` 原料耗尽/背压；回收仓只保存污染物，不自动把它们分类送回生产。恢复后的最新复读又发现输出分拣器 `572` 的取料端为空，制造台 `36` 的连接表也不再包含 `572`，因此这条旁路的历史成功不能继续当作当前运行证据，详见 EXP-067。若未来原生过滤 prepare 能在稳定空窗通过，应先复读 filter、拓扑与携货，再决定是否拆除旁路；本条不授权删除现有实体。
- 复验触发：下一条混料仓输出、首次在连接前成功设置过滤、活跃分拣器出现稳定空窗、回收仓满载、旁路铜料耗尽或研究再次停止。
- 关联：EXP-011、EXP-012、EXP-028、EXP-044、EXP-049、EXP-054。
- 最近复验：2026-09-01（磁铁污染反例、过滤 stale 安全拒绝、回收仓排空与双专线恢复研究的完整现场闭环）。

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
- 直接证据：后续为钢材线补上游铁矿时，范围内 harvest 先在电塔 `39` 约 `0.80 m` 处停滞；返回已验证落点后，侧向长 waypoint 又把玩家带到仓 `286`/制造台 `285`/仓 `287` 夹缝。合成排斥、两条正交滑移和单仓背离四个 fresh 短动作分别由 `5f524835-d3d4-4238-93e7-124ac4680167`、`0ba69040-a5d0-478c-8019-1eae23bafc58`、`7b73545b-e5e7-4243-8d8e-1100128ab149`、`0b229fd5-af68-4104-9b4a-54f66b258ef8` 明确判停，没有继续随机探路。正常保存到 tick `7027343` 并按 protected ticket 恢复后，玩家仍在约 `(-81.28,-53.32,-175.04)`，证明重启不改变已保存碰撞位置；该边界由 EXP-081 单独固化。
- 限制或反例：四向探测只用于已明确终止的短距离 Walk，不适用于水面 Drift、悬崖、飞行或能量不足；方向成功只证明离开当前碰撞，不证明通往最终目标。每次失败都是新的已知终态动作，仍要保留能量余量并禁止相同方向重放。
- 复验触发：下一次多基座夹缝、四方向全部失败、不同建筑半径、短探测成功后速度未归零或可从实体几何直接算出唯一自由扇区。
- 关联：EXP-035、EXP-036、EXP-051、EXP-053、EXP-057。
- 最近复验：2026-09-01（同一仓/制造台夹缝新增四向全部失败反例；正常重启保留精确位置，未扩大为随机游走）。

### EXP-062 — 锁定配方的预建产线只在科技解锁后激活，里程碑以自动产出、日记和普通保存三重验收

- 状态：`validated`
- 日期：2026-09-01
- 适用范围：科技尚未解锁但建筑、输入库存、端点和电力可先行准备的普通生产线，以及用户要求的逐产物提交节奏。
- 当前结论：允许在配方锁定时先建空闲设备和物流，但不得配置锁定配方或宣称产物完成。科技解锁后 fresh inspect 设备，正常配置配方，再以输入仓减少、输入分拣器实际携货、生产设备配方/供电、输出分拣器工作、专用输出仓增长共同证明自动产出；随后核对逐存档 `production_line_item_first`，普通保存同一主档，最后才提交并推送代码/经验。
- 直接证据：钛矿冶炼 `1413` 在 tick `5692950` 解锁后，空闲熔炉 `530` 才配置配方 `65`。15 秒后输入仓 `259` 的钛矿 `1000 -> 986`，分拣器 `532` 实际携 item `1004`，熔炉满功率，输出分拣器 `533` 工作，仓 `531` 出现 6 个 item `1106` 钛块。逐存档日记序号 8 记录 `production_line_item_first`，实际时间 `2026-09-01T08:21:33.164002+08:00`、tick `5702401`、本局时间 `001d 02:24:00`；保存动作 `996eb4ad-92fe-4c81-b42f-3da74ed52e85` 随后把同一主档保存到 tick `5705293`，revision `590 -> 591`、写入健康。
- 直接证据：第二个独立样本金刚石线在晶体冶炼 `1403` 未解锁时只预建空仓 `716/717`、空熔炉 `715` 和 sorter `720/719`，并提前把 400 高能石墨守恒放入输入仓，熔炉仍保持 recipe `0`。科技在 tick `6081424` 正常解锁后，动作 `fd487817-9943-4249-8485-18b9c612a3bc` 才配置运行时配方 `60`。输入仓 `400 -> 354 -> 350`，输入 sorter 曾实际持有 item `1109`，熔炉连续工作且供电比 1.0，输出仓 `0 -> 42 -> 47`。日记序号 9 在 tick `6083748` 记录首个自动 item `1112` 金刚石（实际时间 `2026-09-01T12:29:17.0019946+08:00`、本局时间 `001d 04:09:55`）；保存动作 `2e9ca24b-57dc-40c6-900b-a210b8fc03e7` 随后持久化 tick `6090507`，revision `18 -> 19`、写入健康。
- 直接证据：第三个独立样本为钛晶石线。高强度晶体 `1123` 未完成时只预建制造台 `767`、输入仓 `768`、输出仓 `769` 和 sorter `770–772`；两个输入 sorter 在空仓时分别过滤有机晶体 `1117` 与钛块 `1106`，随后才守恒装入 40/120，制造台始终保持 recipe `0`。科技在 tick `6509179` 完成且运行时配方 `26` 明确 unlocked 后，动作 `74b451af-ed4b-47df-b0cf-8bd5b0b5a933` 才配置生产。输入仓降至 29/88，制造台满供电工作，输出仓出现 8 个 item `1118`；日记序号 16 在 tick `6511499` 记录首个自动钛晶石（实际时间 `2026-09-01T14:28:07.1667461+08:00`、本局 `001d 06:08:44`）。结构矩阵科技 `1124` 随后由正常队列选择并写入日记序号 17；保存动作 `39cee465-5520-4a8c-a1e3-68ac8e6208ab` 持久化 tick `6518917`，revision `270 -> 271`、写入健康。
- 限制或反例：预建端点可能在等待科技期间被其他线路占用，激活前必须重新 inspect；输出设备内部缓存不等于专用仓积累，手工产物也不能替代生产线事件。若日记是中途挂接旧档，历史覆盖仍明确为不完整，但挂接后的新事件可作为前瞻证据。
- 复验触发：下一条预建配方线、首次有机晶体/金刚石/钛晶石/结构矩阵产线、科技解锁期间端点变化或日记事件缺失。
- 关联：EXP-015、EXP-037、EXP-048、EXP-055。
- 最近复验：2026-09-01（钛锭、金刚石、钛晶石三条锁定配方预建线均在解锁后完成自动产出、日记与保存闭环）。

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
- 限制或反例：本条只证明普通混料仓的无过滤输出不安全；若上游容器由结构保证永远单品，或已在空载分拣器上复读精确 filter，则不需要额外重复过滤。清理动作仍必须受距离、容量、双边计数和 player hash 约束。
- 复验触发：`557` 清理完成、`675` 成功空载过滤、改用纯油仓、任一共享副产物仓新增下游或塑料线再次停机。
- 关联：EXP-021、EXP-028、EXP-056、EXP-058、EXP-060。
- 最近复验：2026-09-01（`557` 清除 271 氢、空仓重建过滤入口，并由纯源仓 `784` 经长带恢复塑料连续生产）。

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
- 限制或反例：当前只有母星两组跨水弧段反例，还没有地形可通行网格或沿岸自动规划器；“已验证连续锚点”仅对现场中完成复读的有向段成立。精确落点恢复不处理建筑碰撞；如果终态是 Walk 但无位移，仍应改用 EXP-057/061 的局部脱困流程。
- 复验触发：下一条未验证的陆地锚点弧段、跨水路线自动检测、不同星球、沿岸绕行或第三次独立反例。
- 关联：EXP-035、EXP-036、EXP-046、EXP-051、EXP-053、EXP-057、EXP-061、`scripts/invoke-surface-route.ps1`。
- 最近复验：2026-09-01（旧 `82 -> 180` 路线被新增带/矿机走廊反证为非永久可通行，明确停滞后局部脱困）。

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
- 关联：EXP-028、EXP-033、EXP-037、EXP-049、EXP-054、EXP-058、EXP-059、`docs/m0-status.md`。
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
- 当前结论：不应只在 quarantine 时才签发 resume ticket。每次正常 save API 成功且 session 仍为 owned/healthy/和平/非沙盒/1x 时，Plugin 应用高熵 token 绑定当前高熵主档身份、session/process/bridge/game version、planet 和该次正常保存 tick，原子写入 Plugin 可见的固定受保护 handoff 目录。只有这张票据签发成功、游戏正常关闭且 LastExit 新于签发时刻后才启动新 DLL；恢复后仍要复读 `gameTick >= minimumGameTick`、目标星球/模式/健康与日记门槛。票据一次消费，不枚举也不读取任何其他存档。
- 直接证据：当前正常保存动作 `fc75686b-6399-4bb1-bfa8-a63bd0ba9e33` 将同一 owned 主档保存到 tick `5938815`，revision `100 -> 101`、`ownedSaveState=saved`、`writeHealth=healthy`。已过期的旧 quarantine ticket 没有被复用；新 `scripts/arm-planned-restart-handoff.ps1` 只从当前认证 Bridge 的结构化 session/descriptor 生成 version 1 交接，未打印 token 或主档名。它已在固定 handoff 目录成功落下 planet `104`、minimum tick `5938815` 的新票据，目录与文件均关闭继承且无其他 SID 允许规则。
- 直接证据：源码已增加 `ArmFromHealthySavedOwnedSession`；健康 save 成功后以当前 `_lastOwnedSaveGameTick` 签发，并同时原子持久到运行目录和固定 handoff 目录。完整 solution 编译 0 warning/0 error；连接槽同批回归后 Core/Contracts/MCP 共 65 测试通过。这部分尚未部署到当前进程，所以本条在新 DLL 首次自动签发与二次重启复验前保持 observed。
- 直接证据：新 DLL 以 SHA-256 `AE418B0DE09A6FF8812175BE714720F95777E688E8B13EC41340E821A7E5F45B` 安装后，主菜单直接读到 bootstrap `restartResumeAvailable=true`。动作 `3a5bead7-3521-490d-8279-8d82eb04ad18` 经 fixed LastExit 完成，新 session `8d8c930c-f483-454b-9f3d-552072459918` 为 planet `104`、和平/非沙盒/1x、healthy，初次 tick `5965040 >= 5938815`；日记仍仅 8 条且序号 8 是自动钛块，新熔炉/仓/分拣器 `715…719` 也全部存在，排除回档。恢复后的自动正常保存到 tick `5965043`，session 随即读到新 `restartResumeAvailable=true`；固定 handoff 文件在该时刻更新、ACL 关闭继承且无其他 SID allow。
- 直接证据：第二次部署前没有再运行 bootstrap；旧进程虽因已明确的虚拟 belt 槽验收缺陷进入 quarantine，仍接受正常窗口关闭并由 DSP 更新 fixed LastExit。新版 Plugin 安装后，主菜单直接提供上一次 Plugin 自签发票据；恢复 prepare 保留 planet `104` 且 minimum tick 不低于 `5965043`，动作 `4098eea2-82bf-4546-929d-aa6c675e9aa4` 完成。新 session `9698faef-9cf1-4d0f-bba4-f4abad92b69f` 在 tick `6028386` 为和平/非沙盒/1x、healthy；隔离前新建的 sorter `720/721`、背包仅余 4 sorter、玩家位置和 8 条日记均保留，随后 `lastOwnedSaveGameTick=6028336` 且再次可见新的 restart handoff。这完成“Plugin 自签发→正常关闭→一次消费→同档精确恢复→再次自签发”的闭环。
- 限制或反例：本次第一次部署仍需一个由当前已认证健康 session 生成的 bootstrap handoff，因为旧 DLL 没有计划内签发能力；这不授权在游戏关闭后伪造/修改票据，也不授权从存档文件推断身份。只有当前活跃 Bridge 能复读健康正常保存时才允许 bootstrap，目标票据已存在则拒绝覆盖。
- 复验触发：ticket ACL/路径变化、LastExit 时间门槛失败、不同宿主/Windows 用户，或日记/最新 tick/实体门槛不匹配。
- 关联：EXP-004、EXP-005、EXP-006、EXP-038、EXP-047、EXP-064、`src/Spherewright.Plugin/RuntimeDescriptor/OwnedWorldResumeTicketStore.cs`、`src/Spherewright.Plugin/Game/GameSessionTracker.cs`、`scripts/arm-planned-restart-handoff.ps1`。
- 最近复验：2026-09-01（Plugin 自签发票据的第二次真实重启消费、退出状态保留和恢复后再次签发）。

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
- 最近复验：2026-09-01（单 DLL TypeLoad 反例与同批三程序集成功恢复的正对照）。

### EXP-073 — 上游修复不能以首个局部流量为终点，必须逐层复读到最终消费者

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：多级制造链、矩阵生产与研究消费，以及一个故障修复后可能立即暴露下一处既存断点的恢复现场。
- 当前结论：某个上游设备重新工作或某种中间件重新到达下游，只能证明这一段恢复，不能证明整条目标链已经恢复。每次修复后应沿目标方向逐层复读：源库存下降、输入 sorter 实际携货、生产设备输入/输出变化、输出 sorter 携货、下游设备各配方缓存，最后用最终产物或同一科技的 hash 正增长验收。若最终指标仍为零，就从第一个缺失缓存反向追到下一处断点；不得用手动补料掩盖它，也不得因首段成功就结束等待。
- 直接证据：新 sorter `721` 恢复了电路板制造台 `36` 的铁块输入，制造台和输出 sorter `714` 均出现当前流量，但科技仍停在 `47160`。继续逐层读取发现蓝矩阵站 `76` 已不缺电路板而缺磁线圈；制造台 `73` 有 6 个磁铁、0 个铜块，旧 sorter `284` 自报目标 `73`，但目标连接表不再持有它。动作 `3c99aa3d-0399-412c-9c7a-34ba545f5cba` 新建独立槽 sorter `722` 后，实体立即以 `Sending` 携带铜块 `1104`，并保留仓 `26` 的既有三个输出与制造台 `73` 的既有输出。25 秒对照窗中蓝矩阵站 `76` 的磁线圈/电路板缓存均 `5 -> 6`，研究站恢复工作，科技 `1403` 由 `47354 -> 48923`，形成最终消费者闭环。
- 限制或反例：一次短窗正增长可能来自既有缓冲，不能单独证明永久稳定；仍需在更长窗口确认源库存、两种矩阵供给和科技持续增长。若观察窗口跨科技切换，必须按科技 ID 分段，不能把两个科技的 hash 相加。
- 复验触发：当前 `1403` 的下一次持续窗口、科技切换到 `1701`、钻石线激活、黄矩阵多级链或任何“局部已工作但最终指标不增长”的现场。
- 关联：EXP-028、EXP-037、EXP-049、EXP-054、EXP-059、EXP-067、EXP-068。
- 最近复验：2026-09-01（电路板输入与磁线圈铜输入连续两层修复，最终科技 25 秒增长 1569 hash）。

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
- 最近复验：2026-09-01（电动机四物料出口与有机晶体三原料化工仓两个独立“空仓先过滤”正样本）。

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

### EXP-080 — 活跃仓并发补货会掩盖 transfer 的源端净差量

- 状态：`observed`
- 日期：2026-09-01
- 适用范围：`storage-to-player` 或 `player-to-storage` 的目标仓仍被自动产线持续输入/输出，动作前后只读取聚合库存总数的现场。
- 当前结论：正常 transfer 的动作终态和玩家端精确差量可以证明玩家确实取得或交付目标物，但活跃仓在两次快照间的并发生产/分拣可能刚好抵消源端预期负差量。此时不得把“源仓总数未变”解释为动作未执行并重放，也不能把聚合 `before -> after` 伪写成完整双边守恒。若里程碑必须证明静态双边守恒，应先使用不再通料的仓、短时隔离输入，或把同一窗口的生产增量纳入明确记账；否则只陈述已经由终态与玩家差量证明的较窄事实。
- 直接证据：为修复钢材上游而从持续收纳电路板的仓 `26` 取 1 个 item `1301`。动作完成后 fresh player 明确 `0 -> 1`，但仓内电路板聚合仍为 `400 -> 400`，与该仓正在被电路板产线补货一致。客户端守恒断言因此报错，但没有重放；后续背包仍保留该 1 个电路板。这个样本不否定 transfer 的内部精确检查，只否定“跨活跃窗口的两个聚合快照必然显示相反净差量”。
- 限制或反例：静态仓或已证明无并发通料的窗口仍应要求双边相反差量和增殖点守恒；不能借本条放宽 prepare/commit 的 fresh hash、物品 ID、数量、容量或终态要求。单个 `400 -> 400` 样本尚未量化并发补货的精确时间顺序。
- 复验触发：下一次活跃仓 transfer、引入仓库事件计数/生产寄存器差量、把产线输入临时隔离后复测，或 transfer DTO 增加动作内部双边明细。
- 关联：EXP-007、EXP-021、EXP-028、`src/Spherewright.Plugin/Game/NormalGameActionCoordinator.StructuredActions.cs`。
- 最近复验：2026-09-01（玩家电路板 `0 -> 1`、活跃仓聚合 `400 -> 400`，展示断言失败后未重放）。

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

## 修订记录

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
- 2026-08-31：M0 Gate D 状态变化复核：新增 EXP-033，以研究站 `256` 的配方 18、双输入和能量矩阵 `0 -> 3 -> 6` 完成首个自动红矩阵闭环；新增 EXP-034，记录网络 2 运行态约 90% 的容量瓶颈。EXP-015 的生产线优先级和 EXP-032 的 2 氢排除规则均继续适用。
- 2026-08-31：显式保存动作 `b399facb-48cd-4838-b7ab-9c9762b6def7` 完成 tick `2499658` 的精确 owned-world 保存；EXP-033 补齐最终保存证据，M0 Gate D 转为 complete，EXP-034 保留为里程碑后的首要产能优化项。
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
