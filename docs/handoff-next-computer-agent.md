# Spherewright 跨电脑 Agent 接手说明

更新时间：2026-08-31（Asia/Singapore）

交接分支：`main`

当前里程碑：M0 — First Red Matrix

## 2026-08-31 M0 完成状态（当前权威）

- 当前精确 owned session 为 `c51a4fd0-5a50-4fb2-8520-f7acf216334d`、星球 `104`；和平、1x、关闭沙盒，当前 session revision `191`，写入健康。它通过严格的一次性 LastExit provenance 流程延续同一 Spherewright 自建世界，没有枚举或加载无关存档。当前 `restartResumeAvailable=false`，不得为了部署源码补丁而关闭游戏或热替换运行 DLL。
- 当前源码/MCP 注册面为 38 个工具；新增的四个工具只覆盖精确 action proof 的 quarantine reconciliation 与受保护一次性票据的 fixed-LastExit 同档恢复，不是通用解锁或任意读档入口。
- Gate C 的同位置分拣器归属修复已部署并实机验证：旧 `164` 与新 `181` 共享精炼厂 `141` 的源端姿态，但分别输出至 `163` 与 `170`，新 action 只归属 `181`，会话未隔离。
- Gate D 已完成。研究站 `256` 配方 `18` 在接料前能量矩阵 item `6002` 为 0；石墨链 `114 -> 257 -> 256` 与氢链 `165 -> 205…255 -> 258 -> 256` 接通后，同一输出 buffer 依次读到 3、6，后续继续积累至 10 并因输出缓冲满而正常停机。
- 两次从活跃氢带末端续接路径各把 1 氢正常回收到玩家，已单独记账并排除在自动产出证据之外；首个红矩阵只以研究站 `256` 的 `0 -> 6` 验收。
- M0 最终显式保存动作 `b399facb-48cd-4838-b7ab-9c9762b6def7` 由 DSP 正常 save API 确认 tick `2499658`，revision `150 -> 151`。后续脱困并复读红糖 10 后，动作 `02f50a58-276c-4b90-be62-bb9645920abf` 再次正常保存同一档于 tick `2710106`，revision `190 -> 191`、`ownedSaveState=saved`、`writeHealth=healthy`。
- 密集工厂内发生了一次 post-M0 碰撞卡路。旧部署版会一直等全局超时；一次 Computer Use 跳跃仅用于从已完成/已保存的现场脱困，并明确排除在 M0 和结构化能力证据之外。源码已新增 180-tick 物理停滞、600-tick 目标无进展、断能原因隔离以及 move/harvest single-flight；尚未热部署。
- 当前离线源码由 `.local\dotnet\dotnet.exe` SDK `8.0.424` 完整构建，0 warning / 0 error，55 tests passed（Contracts 3、Bridge.Core 42、MCP 10）。新增 tank-fluid 和移动 watchdog 均只在源码构建/测试中，等待能证明同档恢复的安全重启再部署。
- 用户要求后续无异常时持续使用同一存档；每完成一种新产物流水线，先结构化复读产出、普通保存，再提交并推送对应代码和经验。下一优先级是为红糖补输出仓储并增加网络 2 运行态供电，然后基于当前存档规划/推进黄矩阵与跨星球钛物流。完整细节以 `docs/m0-status.md` 与 `docs/experience-ledger.md` 为准。

## 2026-08-31 历史接手补充（已被上节取代）

从本节起到文件末尾保留的是旧候选阶段的交接快照；其中所有“当前候选”“尚未完成”“需要新档确认”等状态结论均已被上面的 M0 完成状态 superseded，不得再作为执行判断。不可突破的安全边界与代码结构仍可作为背景参考。

- 用户已经授权并消耗了一次新档机会。当前候选会话为 `73b4019b-c5cc-4f90-b1f4-bc4abc6d49c6`、星球 `104`，确认和平、1x、关闭沙盒。
- 候选已正常完成能量矩阵科技、煤制高能石墨、钢材、原油萃取、精炼厂和分流储仓。分拣器 `211` 的精炼油过滤项 `1114` 已实机验证。
- 第二个合法输出分拣器 `213` 实际拓扑为 `203 -> 212`，但已安装的旧 Plugin 因两个分拣器源端位置完全重合而误选旧实体 `211`，动作 `c3031123-d9c2-40c9-9cdc-3d18cc63bf8b` 进入 `outcome_unknown` 并隔离写入。没有产出红色矩阵，也没有执行最终保存。
- 源码已经在创建分拣器前记录并排除同位置旧实体，`211`/`213` 回归测试通过；完整解决方案 0 warning / 0 error，45 tests passed。修复 DLL 尚未安装到仍在运行的隔离进程。
- 不得继续该候选、热替换 DLL 或自行创建替代新档。正常退出后安装修复版、再次创建验证档，必须重新取得用户明确确认。权威细节见 `docs/m0-status.md`。

## 1. 接手结论

仓库已经具备安全本机 Bridge、34 个结构化 MCP 工具，以及从普通和平 1x、关闭沙盒的新世界推进到红色矩阵所需的读写原语。Gate A、Gate B 已完成；Gate C 仍有隔离后的回归实机检查；Gate D 已推进到精炼厂分流阶段，但当前候选已隔离且尚未产出红色矩阵。

当前源码离线验收结果：

```text
dotnet build Spherewright.sln --no-restore
  0 warnings, 0 errors

dotnet test Spherewright.sln --no-build --no-restore
  45 passed
  - Contracts: 3
  - Bridge.Core: 33
  - MCP: 9
```

最重要的边界是：**用户先前的一次新档授权已经由当前隔离候选消耗，尚未授权创建替代验证档。** 接手 Agent 在用户再次明确回复“确认继续”或等价指令之前，只能读取当前结构化状态、整理代码、构建和运行不接触 DSP 存档的自动化测试；不得创建新档、执行游戏写操作，也不得读取任何已有存档。

## 2. 不可突破的范围

- 不使用 Computer Use、视觉识别、键鼠模拟或宏。
- 不使用沙盒模式，不授予物品，不直接写玩家能源、科技、生产缓冲或存档。
- 不枚举、打开、复制或解析任何已有存档；只允许操作当前 Plugin 进程通过 `prepare_new_game` / `commit_new_game` 自己创建并绑定的世界。
- DSP/Unity 状态只能在 Unity 主线程读写；后台线程只处理深复制 DTO。
- 所有正常玩法写入均须 `prepare`、短期不透明计划令牌、幂等 `commit`、before/after 复读。
- 结果无法证明为 before 或预期 after 时，将当前会话写入系统隔离；不得猜测重试或手工回滚。
- 不使用历史 `BasicProductionLineCoordinator`。该源码只保留为研究材料，项目文件已将它排除出 Plugin 编译，MCP 也不注册相关工具。
- 不在 Spherewright 内加入 LLM、自主规划循环或一键红糖复合动作。由外部 Agent 根据本次运行时目录和状态逐步规划。

权威规则优先级：根目录 `AGENTS.md` 高于其他文档；状态以 `docs/m0-status.md` 为准。

## 3. 当前环境基线

上一台电脑的已验证基线：

| 项目 | 值 |
|---|---|
| Windows | Windows 10 build 26200, win-x64 |
| .NET SDK | 9.0.315；另有 .NET 8 runtime 8.0.28 |
| DSP | `0.10.34.28529`，Steam build ID `23109513` |
| `Assembly-CSharp.dll` SHA-256 | `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85` |
| BepInEx | 5.4.17.0，`xiaoye97-BepInEx-5.4.17` |
| `BepInEx.dll` SHA-256 | `DC1CB6B58B962BDA5AAA1D6B5F9AE14EC174F61836A1A1F96C1A040C7E8381F7` |
| MCP SDK | `ModelContextProtocol` 2.2.0，精确锁定 |
| Plugin / Core / MCP | net472 / netstandard2.0 / net8.0 |

另一台电脑必须重新运行环境探测并核对哈希。若 DSP 版本或 `Assembly-CSharp.dll` 哈希不同，不得把上一台电脑的内部 API 研究当成当前版本已证实；先只读检查程序集并更新 `docs/research/`，再决定是否能继续运行验证。

## 4. 代码结构与调用链

```text
外部 Agent / MCP Host
        | stdio MCP
        v
Spherewright.Mcp                 net8.0
        | 当前用户 ACL + 启动时随机 token 的本机 Named Pipe
        v
Spherewright.Plugin              net472 / BepInEx 5
        | 有界 Unity 主线程调度
        v
DSP 当前版本正常玩法系统
```

关键目录：

- `src/Spherewright.Contracts`：DTO、协议方法、会话/资源/工厂/动作契约。
- `src/Spherewright.Bridge.Core`：帧、认证、计划、幂等、分页、状态哈希、安全状态机和运行时依赖图。
- `src/Spherewright.Plugin`：BepInEx 生命周期、DSP 适配、主线程动作、Named Pipe 和运行时描述文件。
- `src/Spherewright.Mcp`：stdio MCP Server、Bridge 客户端和 34 个工具映射。
- `scripts`：环境定位、最小游戏引用同步、安装和本机 Bridge 测试辅助脚本。
- `docs/research/game-api-m0.md`：当前 DLL 类型、签名、IL 调用路径和采用理由。

公开工具共 34 个：14 个只读/查询工具，以及 10 组两阶段写入工具。两阶段动作覆盖新建世界、移动、采集、手搓、选择研究、建造、建筑配置、物品转移、机甲加燃料和保存当前自建世界。

## 5. Gate 与证据状态

### 已实机验证

- Gate A：BepInEx 单次加载、安全 Pipe、当前用户 ACL、启动 token、错误 token 拒绝、MCP stdio、描述文件清理。
- Gate B：由 Spherewright 创建的普通和平 1x、关闭沙盒世界；会话、玩家、进度、目录、资源、工厂、电力的结构化读取；分页绑定和新游戏幂等。
- 正常玩法早期原语：移动、采集、手搓、研究，以及风机、电线杆、矿机、研究站、熔炉、传送带、分拣器的合法建造。
- 在开发会话 `22962c57-398b-4f80-b4e5-23eef9ece284`、星球 `103` 中，结构化读回观察到真实的“矿机 → 传送带 → 分拣器 → 熔炉”铁矿产线；六段传送带动作 ID 为 `e21ed435-73f8-4314-8fc4-828402451fc2`，熔炉实体 `11`，分拣器实体 `12`。

上述会话仅是非最终开发证据，不能作为 Gate D 红色矩阵验收，也不得在另一台电脑尝试加载它。

### 已实现并通过离线构建/测试，但尚未实机验证

- 科研完成弹窗：主线程检测当前 `UIResearchResultWindow`，调用游戏原生 `FadeOut()`，不合成输入。
- 机甲加燃料：只移动玩家已有燃料，调用 `Mecha.AutoReplenishFuel`；数量和增产点必须双边守恒。
- 显式保存：只调用 `GameSave.SaveCurrentGame`，保存名来自当前进程内部保留的高熵自建世界身份；客户端不能提供存档名。
- 分拣器物品过滤：严格要求已连接、空载、空闲的分拣器，按照当前 `UIInserterWindow` 的真实组件/标志写入路径设置并复读过滤项。
- 红色矩阵依赖图修正：共同产物不再错误屏蔽其他生产分支；Core 测试覆盖等离子精炼、X 射线裂解共同产物和煤制高能石墨分支。
- 原油萃取、原油精炼、煤制高能石墨和矩阵研究/生产所需的通用结构化路径。
- 最终证明链已离线复核：科研选择只证明进入原生队列；科研完成由 progression/lab tick 复读；同一生产研究站的前后快照可绑定会话、星球、实体、红矩阵配方、输出 `itemId/count`、游戏 tick 和完整状态哈希。

### 尚未完成

- 安装并实机复验上述新增路径。
- 在当前运行时配方目录中重新确认红色矩阵依赖图。
- 在同一份新的普通自建世界中，从空白状态正常推进到红色矩阵。
- 结构化证明目标红色矩阵物品从 0 增长到至少 1，并复读完整上游产线、电力、科技、玩家状态和动作结果。
- 通过两阶段 save 动作保存该精确自建世界，记录 `lastOwnedSaveGameTick`，正常退出并恢复 `Safety.AllowWrites=false`。

## 6. 接手机器的离线准备

用户确认建新档之前，只允许执行本节。

```powershell
git clone git@github.com:AvaloNero/Spherewright.git
Set-Location Spherewright

dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
```

如果接手机器已安装 DSP，可运行不读取存档的环境探测：

```powershell
./scripts/locate-dsp.ps1 -AsJson
./scripts/sync-game-refs.ps1
dotnet build Spherewright.sln --no-restore
```

`sync-game-refs.ps1` 只把四个最小编译引用复制到被忽略的 `.local/game-refs`，不会修改游戏 DLL。不要提交 `.local/`、游戏 DLL、运行时描述文件、日志或任何存档。

在等待确认期间，不运行 `install-dev-plugin.ps1`，不启动 DSP，不修改 BepInEx 配置，也不调用任何游戏 Bridge 动作。

## 7. 用户明确确认后的唯一下一次运行

收到明确确认后，再按以下顺序进行：

1. 复核 DSP、BepInEx、关键 DLL 版本和 SHA-256；若不匹配，暂停写入并先更新当前版本研究证据。
2. 在 DSP 未运行时构建并执行 `./scripts/install-dev-plugin.ps1 -NoBuild`。脚本只复制 Spherewright 输出，并在 DSP 运行时拒绝覆盖。
3. 第一次只读启动，确认 `Safety.AllowWrites=false`、Plugin 只加载一次、安全状态和 34 个 MCP 工具；正常退出。
4. 只修改生成配置中的 `Safety.AllowWrites=true`，重新启动并停在主菜单。保留 `Safety.RequirePeacefulSave=true` 和 `Experience.AutoAcknowledgeResearchResults=true`。
5. 使用 `spherewright_prepare_new_game` / `spherewright_commit_new_game` 创建**一份**普通和平 1x、关闭沙盒的世界；保存名由 Plugin 内部生成。
6. 在这同一候选会话尽早烟测 refuel、科研弹窗自动关闭、sorter-filter、显式 save 和刷新后的红色矩阵依赖图。通过后不要换档，继续推进 Gate D。
7. 外部 Agent 只从现场目录选择物品、配方、科技和建筑 ID，不硬编码旧版本原型计划。每个动作先读、再 prepare、再用唯一 UUID commit，并轮询 `spherewright_get_action_result`。
8. 依次完成正常能源/采集/手搓、蓝色矩阵和前置研究、煤制高能石墨、原油萃取与精炼、氢和精炼油分流、红色矩阵研究站生产。
9. 以运行时目录识别出的红色矩阵物品 ID 为准。启动生产前保存目标研究站的 `inspect_factory_entity` 快照并暂不连接输出分拣器；随后复读同一 `sessionId/planetId/objectId`，证明相同红矩阵配方的 `output` 缓冲中该物品从 0 增至至少 1，同时保存前后状态哈希、动作 ID、游戏 tick、输入缓冲、功率、资源节点和玩家物品差量。
10. 两阶段保存当前自建世界，复读 `lastOwnedSaveGameTick`；正常退出 DSP，检查无未处理异常和描述文件清理；最后恢复 `Safety.AllowWrites=false`。

如果候选会话因动作结果未知、写入隔离、版本不匹配或其他故障而失效，**不得自行创建替代新档**。先停止运行、报告证据，再次向用户请求新档确认。

## 8. 成功证据最低要求

最终报告必须同时证明：

- 世界由当前 Plugin 进程创建并拥有，和平模式已确认、资源倍率 1.0、沙盒已确认关闭。
- 全程只使用结构化 Spherewright MCP；没有 Computer Use、宏、人工游戏内协助、注入、存档读取/修改或速度修改。
- 目标红色矩阵 ID 来自当前运行时目录，产出数量从 0 到至少 1。
- 原料来源和中间链路可复读：真实煤/原油节点、矿机/萃取站、熔炉/精炼厂、过滤分拣器、传送带、研究站、电力和缓冲。
- 所有写入均有 prepare/commit、幂等键、action ID、终态和 before/after；没有无法解释的物品差量。
- 最终自建世界由显式 save 动作保存并复读 tick；退出后写入配置恢复为 false。

不要把“代码已实现”“自动化测试通过”或早期铁矿产线证据表述为红色矩阵实机完成。

## 9. 优先阅读顺序

1. `AGENTS.md`
2. `docs/m0-status.md`
3. 本文档
4. `docs/manual-test-m0.md`
5. `docs/red-matrix-capability-audit.md`
6. `docs/safety-model.md`
7. `docs/protocol.md`
8. `docs/research/environment.md`
9. `docs/research/game-api-m0.md`
10. `docs/remote-validation.md`（仅了解后续架构；当前不要实现）
