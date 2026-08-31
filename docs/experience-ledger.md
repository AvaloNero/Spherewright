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
- 限制或反例：若 prepare 在任何 commit 前明确失败且无 action ID，可按普通 prepare 失败处理。
- 复验触发：客户端响应模型、ActionResult 字段或脚本 helper 变化。
- 关联：`src/Spherewright.Contracts/Actions/ActionResultContracts.cs`、`docs/protocol.md`、`docs/safety-model.md`。
- 最近复验：2026-08-31。

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
- 限制或反例：`restartResumeAvailable=false` 时，即使已有正常保存也不能假定关闭后可由工具安全续接；应保持当前进程，直到恢复票据链可用或用户明确结束本次运行。若出现 quarantine、版本不匹配、存档身份不明或无法证明产出，则不得用提交标签掩盖未完成状态。
- 复验触发：每个新产物首次自动产出、每次 save/commit/push、会话健康变化、恢复协议变化或用户调整里程碑定义。
- 关联：EXP-005、EXP-033、`docs/m0-status.md`、`docs/handoff-next-computer-agent.md`。
- 最近复验：2026-08-31（动力引擎成为第二个按“连续产出 → 普通保存 → 工程提交推送”流程验收的产物流水线）。

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
- 当前结论：逐段移动结束后，只有 `movementState=Walk` 且速度 `<=0.1 m/s` 才能继续；`Drift` 即使速度接近 0 也会持续耗能，不能当作 settled。路线首次进入 Drift 时立即停止并复读附近实体/矿脉，优先移动到已现场证明为 Walk 的精确落点，而不是只瞄准建筑中心；建筑另一侧可能仍在水里。Drift 中位置每 tick 变化，普通 read→prepare→commit 会频繁 `STALE_STATE`，恢复动作应在同一进程内有界重复“fresh read→prepare→立即 commit”，只重试未提交的 stale，取得 commit 后绝不重放。
- 直接证据：母星铁矿节点 `53` 的最后路线终态为 Walk、距节点 `1.84 m`；至无线塔 `180` 的三段路线也保持 Walk，8 秒核心净增约 `21.98 MJ`。随后直达红糖仓的球面路线在第三段检测到 `Drift`、速度约 `0.099 m/s` 后立刻停止；核心仍足以由动作 `5d8dfc76-b998-4e9f-ba3c-ab4234466285` 在第 2 次原子绑定后抵达风机 `82` 一侧并恢复 Walk。密集基座处动作 `e745c98e-e992-4a06-afcc-854eeebe3b63` 又被 180-tick 看门狗以剩余 `13.07 m` 提前判停，侧移到未占用铜矿节点后绕行成功。水面带末端 `468` 附近再次出现 Drift；瞄准电塔 `143` 的另一侧仍未落地，改用此前已证明的 Walk 坐标后，动作 `bd2842e0-4f05-4c15-9723-24a98ef7c839` 在第 7 次未提交 stale 重试后恢复 Walk。
- 限制或反例：资源节点、风机或电塔“存在”只说明实体建在星球表面，不保证实体中心的每一侧都可行走；优先复用已有 Walk 坐标，首次新锚点仍需短窗状态/能量复读。原子 stale 重试只解决漂浮快照变化，不绕过碰撞、能量预算、单飞订单或动作失败。
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

## 修订记录

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
