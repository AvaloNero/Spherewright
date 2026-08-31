# AGENTS.md — Spherewright 开发 Agent 执行规范

> 仓库名：`spherewright`  
> 项目名：**Spherewright**  
> 当前定位：面向外部 Agent 的《戴森球计划》结构化控制工具。初版通过 MCP 暴露观察与受控动作，不内置 LLM。  
> 当前范围：单人、关闭黑雾、Windows、原版生产玩法。  
> 当前里程碑：**M0 — First Red Matrix（第一颗红糖）**。

本文档是编码 Agent 的执行约束、实施顺序和验收标准。它高于仓库中的普通计划文档；如果实现、README 或旧代码与本文冲突，应先停止扩展并使实现与本文一致。

---

## 0. 给编码 Agent 的执行指令

将下面这段作为本地编码 Agent 的任务入口：

```text
阅读仓库根目录 AGENTS.md，并严格执行其中的范围、门槛和安全规则。

Spherewright 当前是 tool-first、agent-ready 的 DSP 控制层：
外部 Agent 通过 MCP 调用它；本轮不要在工具内部接入 LLM，也不要实现自主规划循环。

M0 — First Red Matrix 是一个可跨多次 Agent 会话完成的里程碑，分为四个验收门：
A. 环境、构建、BepInEx 加载与安全 status 链路；
B. Spherewright 创建的普通和平新档和结构化观察；
C. 遵守正常游戏规则的移动、采集、制造、施工、配置和科研动作；
D. 仅通过上述 MCP 原语，从新档端到端产出至少 1 个红色矩阵并完成复读证明。

每次执行先判断最早尚未完成的 M0 门槛，从该门槛继续。不得因为一次会话时间有限而跳到 M1，也不得在 M0 未完成时把“下一步”写成 M1。

M0 的游戏内操作必须完全通过 Spherewright 的结构化 Bridge/MCP 完成。禁止调用 Computer Use，禁止视觉识别和键鼠宏；不得把它们作为启动、兜底或验收的一部分。

M0 必须使用普通游戏规则：`isSandboxMode=false`、`GameMain.sandboxToolsEnabled=false`、正常资源倍率、正常配方/科技解锁、正常物品/能源/时间消耗。禁止物品注入、凭空补充背包、直接解锁科技、瞬间完成建筑、直接填写生产缓冲、修改游戏速度、修改程序集或存档。

本轮禁止实现：
- 内置 Agent/LLM、自主 Goal Planner、蓝图批量施工和物流塔；
- 飞行、跨星球自动驾驶、黑雾、战斗、多人、Nebula；
- Cheat Engine、外部内存扫描、视觉识别、Computer Use 或键鼠宏；
- 读取、枚举或载入任何不是 Spherewright 自己新建并登记的存档。

不要猜测 DSP 内部字段或方法名。必须检查本机当前版本 Assembly-CSharp.dll，把类型、签名、调用路径和哈希证据记录到 docs/research/。

禁止 git reset --hard、git clean -fd、force push、覆盖用户已有改动、自动发布包、创建远程仓库、PR 或 Release。

缺少游戏、BepInEx、权限或必要程序集时，把相应的游戏内验证标为 blocked，但继续完成不依赖游戏 DLL 的 Core、MCP 和自动化测试。最终报告必须指出最早未完成的 M0 门槛。
```

---

## 1. 项目定义与边界

Spherewright 不是内置大模型的“AI 角色”，也不是存档修改器、作弊器或鼠标宏。它首先是一个**可编程、可验证、可供 Agent 调用的 DSP 控制层**。

目标链路：

```text
外部 Agent / MCP Host
        │ stdio MCP
        ▼
Spherewright.Mcp                  net8.0
        │ 受认证的本机 Named Pipe
        ▼
Spherewright.Plugin               net472 / BepInEx 5
        │
        ├─ Spherewright.Bridge.Core    netstandard2.0
        │  协议、帧、计划、幂等、安全状态机
        │
        └─ DSP Game Adapter
               │ Unity 主线程
               ▼
       当前版本 Assembly-CSharp
```

### 1.1 支持范围

- 单人游戏。
- 创建时关闭黑雾的存档。
- 原版物品、配方、建筑、科技和生产规则。
- Windows 为 M0 的唯一正式运行平台。
- 当前 M0 在同一台本机完成开发、构建和 DSP 运行验证。局域网独立游戏验证机属于延期架构，本轮不得实现远程部署、远程启动、网络 Bridge 或远程测试编排。
- 使用 BepInEx 5 加载游戏内 Plugin。
- MCP Server 使用官方 C# SDK，通过 stdio 暴露工具。
- 初期由 Claude、Codex 或其他外部 Agent 负责决策；Spherewright 负责提供真实状态、合法动作和明确结果。

### 1.2 明确不支持

以下内容不进入 M0，也不应为其提前建立复杂抽象：

- 多人游戏、Nebula、网络同步、主机权威或状态复制。
- 黑雾、战斗、防御、敌人目标、仇恨和战损维修。
- 内置 LLM、Prompt 编排、自主 Goal Planner 或长期 Agent 循环。
- CommonAPI、LDBTool、DSPModSave 等额外功能 Mod 依赖。
- 新物品、新配方、新建筑或自定义 Proto。
- 直接修改 `Assembly-CSharp.dll`、Unity DLL 或游戏存档。
- 原生 DLL 注入器、Cheat Engine、外部内存扫描。
- Computer Use、视觉识别和键鼠模拟，包括所谓临时兜底。
- 沙盒模式、无限资源、物品注入、瞬建、直接科研解锁或修改游戏速度。
- 蓝图批量施工、物流塔写入、飞行和跨星球。

M0 明确纳入为产出红色矩阵所必需的最小能力：地表步行、手动采集、机甲手动制造、单体建筑/传送带/分拣器施工、建筑配置、正常科研选择，以及矿机、原油萃取站、精炼厂、冶炼设备、制造台、矩阵研究站和供电物流的结构化观察与控制。外部 Agent 负责规划，Spherewright 不内置自主规划循环。

---

## 2. 不可违反的工程原则

### 2.1 游戏状态只能在 Unity 主线程访问

Named Pipe、MCP、序列化和工作线程不得直接读写：

```text
GameMain
GameData
PlanetFactory
FactorySystem
CargoTraffic
PlanetTransport
玩家背包和科技状态
任何 UnityEngine.Object
任何 DSP 内部数组、池或可变引用
```

统一流程：

```text
后台线程收到请求
    ↓
解析为不可变 Command
    ↓
加入有界 MainThreadDispatcher
    ↓
Plugin.Update() 按数量和时间预算执行
    ↓
主线程读取或调用 DSP
    ↓
复制为 Spherewright DTO
    ↓
后台线程只处理 DTO 的序列化和返回
```

读操作也必须进入主线程。离开主线程前，必须完成深复制；不得把 DSP 内部数组、池、组件引用或 Unity 对象传到后台线程。

### 2.2 Plugin 只保留游戏适配层

所有无需游戏 DLL 的逻辑必须放入：

```text
Spherewright.Contracts
Spherewright.Bridge.Core
```

包括但不限于：

```text
FrameCodec
Envelope 与版本校验
认证握手状态机
计划令牌记录与过期
写入 blocker 计算
目标指纹比较
幂等结果缓存
single-flight
动作状态机
超时语义
写入 quarantine 状态机
错误码映射
```

`Spherewright.Plugin` 只负责：

```text
BepInEx 生命周期
DSP 当前版本适配
Unity 主线程调度
当前游戏状态快照
调用经验证的游戏原生流程
运行时文件与 Named Pipe 宿主
```

不得把可测试的协议和安全逻辑塞进 `BaseUnityPlugin` 或依赖游戏 DLL 的类中。

### 2.3 不直接拼装游戏内部状态

除非当前版本不存在更安全的正式流程，并且已经通过反编译、调用链和运行测试证明必要，否则禁止直接写：

```text
entityPool
assemblerPool
factorySystem 内部缓冲区
cargoPath 缓冲区
玩家库存数组
科技解锁位图
```

M0 还明确禁止通过以下方式绕过普通玩法：

```text
TryAddItemToPackage 等凭空发放物品
PlanetFactory.InsertIntoStorage 等直接注入生产原料
由 Spherewright 直接调用 BuildFinally 瞬间完成预建筑
直接增加科技 hash、矩阵计数或解锁标志
直接增加矿物、原油、氢、石墨或红色矩阵数量
修改沙盒、无限资源、无限电力、无消耗制造或游戏速度标志
```

正常动作必须复用当前版本中真实玩家流程所调用的业务路径。例如建筑只创建合法预建筑并消耗玩家已有物品，随后由机甲施工无人机和游戏 tick 完成；手搓必须进入机甲制造队列并消耗真实原料和时间；科研必须选择科技并由矩阵研究站消耗真实矩阵。对制造台配方切换，优先复用游戏 UI 或业务代码实际使用的设置流程，不得只写一个 `recipeId` 就宣称完成。

M0 不实现自动“字段回滚”。发生异常后，不得根据猜测手工拼回若干字段。

### 2.4 不猜 DSP API

所有 DSP 类型、字段和方法必须以用户本机当前版本程序集为准。

Agent 必须维护：

```text
docs/research/environment.md
docs/research/game-api-m0.md
```

`game-api-m0.md` 至少记录：

- 游戏版本和 `Assembly-CSharp.dll` SHA-256。
- 存档载入、卸载和当前星球的可靠判定。
- 黑雾/战斗模式三态的判定路径及证据。
- 普通新档创建、跳过开场动画与正常初始状态的等价性证据。
- 玩家位置、移动状态、背包、机甲能量、施工无人机和手动制造队列的读取路径。
- 矿脉、原油、植被/可采集物、矿机覆盖和采集结果的读取路径。
- 配方、科技、解锁状态、研究队列和矩阵消耗的读取路径。
- 建筑、预建筑、传送带、分拣器、供电、生产设备及输入输出缓冲的读取路径。
- 地表步行、手采、手搓、创建预建筑、施工完成、连接物流、设置配方和选择科研的候选方法、真实调用方、完整签名和采用理由。
- 每类动作执行后需要复读的资源守恒、身份、状态和完成不变量。
- 红色矩阵在当前版本中的原型、配方、前置科技和完整上游链路；ID 必须来自当前 LDB，不得凭记忆硬编码。
- 每个关键符号的完整类型名与签名。

不得提交反编译得到的整段游戏源码、游戏 DLL 或存档。

### 2.5 默认只读；prepare 始终可用；commit 显式开启

Plugin 默认配置：

```ini
[Bridge]
Enabled = true
PipeNamePrefix = Spherewright
MaxConnections = 1
MaxQueuedRequests = 64
MaxInFlightRequests = 8
MaxMainThreadQueue = 32
MaxRequestsPerFrame = 4
FrameBudgetMs = 2
MaxFrameBytes = 1048576
ReadRequestTimeoutSeconds = 10
CommitWaitTimeoutSeconds = 15

[Security]
RequireCurrentUserAcl = true
RuntimeDescriptorDirectory = %LOCALAPPDATA%\Spherewright\runtime
RotateBridgeTokenOnStart = true

[Safety]
AllowWrites = false
RequirePeacefulSave = true
PlanTokenLifetimeSeconds = 60
IdempotencyRetentionMinutes = 30
MaxIdempotencyEntriesPerSession = 1024
```

M0 中“先 prepare、后 commit”是协议硬约束，不提供绕过开关，也不再使用无法证明的 `RequireDryRunBeforeWrite` 布尔配置。

规则：

- 所有 M0 `prepare_*` 在 `AllowWrites=false` 时仍应正常执行。
- 写入关闭、和平状态未知等条件通过 `commitBlockers` 返回，而不是让 prepare 报 `WRITES_DISABLED`。
- 只有 commit 会产生副作用。
- 幂等键只用于 commit；prepare 不接收也不复用幂等键。
- `SANDBOX_MODE_ACTIVE`、非 Spherewright 所有的 session、控制器不可用或无法证明正常资源消耗时必须阻止 commit。

### 2.6 写入必须经过两阶段计划

所有 M0 写入都遵循：

```text
inspect
  ↓
prepare：重新读取、校验、生成短期 planToken
  ↓
commit：携带 planToken + idempotencyKey
  ↓
主线程再次复读全部身份与状态
  ↓
执行一次经验证的游戏流程
  ↓
复读 before / after 和完整不变量
  ↓
缓存终态并返回
```

`planToken` 必须：

- 使用 CSPRNG 生成至少 256 bit 的不可预测随机值，采用 Base64Url 或等价无歧义编码。
- 是不透明引用，实际计划记录保存在 Plugin 进程内；不得把可信状态完全交给客户端回传。
- 绑定 `bridgeInstanceId`、`sessionId`、`planetId`、动作类型、完整目标身份、动作参数、before 状态、`expectedStateHash`、资源预算、创建时间和过期时间。
- 在切档、退出存档、Plugin 重启或过期后失效。
- 首次 commit 被某个 `idempotencyKey` 接受后，与该幂等键绑定；其他幂等键不得复用。

### 2.7 写目标必须使用完整身份、资源预算与状态指纹

写入不能只依赖 `sessionId + entityId + 全局 revision`。每种动作必须定义版本化规范指纹；全局 revision 只能用于快速拒绝，不能代替逐项复读。

所有目标至少包含 `sessionId`、`planetId`、`actionType`、`expectedStateHash` 和 `stateHashVersion`。动作专用身份至少覆盖：

| 动作 | 必须绑定的身份与状态 |
|---|---|
| move | 玩家身份、起点、移动状态、目标球面坐标、容差 |
| harvest | 资源类型、pool/object ID、proto、位置、剩余量、玩家/背包状态 |
| handcraft | recipe、数量、解锁状态、原料/容量、当前制造队列 |
| build/connect | building item、位置/朝向、预期连接、附近碰撞、玩家库存、施工队列 |
| transfer | 来源/目标容器完整身份、item/count、双边库存摘要 |
| configure | entity/component/building item、当前配方/模式、进度和全部相关缓冲 |
| research | tech ID/level、前置科技、当前 queue/hash、矩阵需求 |

`expectedStateHash` 必须由版本化、确定性的规范编码生成，并覆盖所有可能影响动作合法性、成本或结果分类的字段。

不得依赖 JSON 属性顺序或运行时默认 `GetHashCode()`。

对移动、采集、手搓、施工、物品转移、配方和科研动作，指纹还必须按动作覆盖：玩家位置/移动状态、目标资源身份与剩余量、背包原料与容量、配方和科技解锁状态、预建筑/建筑/连接身份、机甲能量、施工队列、研究状态和计划消耗量。不得只校验一个实体 ID。

commit 必须在主线程复读 `sessionId`、`planetId`、动作专用完整身份、`expectedStateHash`、动作合法性、解锁状态，以及正常资源、距离、能量、施工和时间前置条件。

任何不一致都返回 `STALE_STATE` 或更具体错误，不执行副作用。

### 2.8 黑雾/战斗状态使用三态并 fail-closed

禁止继续使用 `darkFogEnabled: bool`。

统一 DTO：

```text
combatModeStatus = confirmedPeaceful | combatEnabled | unknown
```

规则：

- `confirmedPeaceful`：可以继续评估其他写入条件。
- `combatEnabled`：写入 blocker 为 `PEACEFUL_MODE_REQUIRED`。
- `unknown`：写入 blocker 为 `PEACEFUL_MODE_UNKNOWN`，必须 fail-closed。
- prepare 仍可返回读取结果和 blockers。
- commit 只有在状态为 `confirmedPeaceful` 时才可能执行。

### 2.9 M0 只允许普通玩法原语，不允许结果注入

M0 的写入范围主动收窄为产出第一颗红色矩阵所需的原语：

- 创建 Spherewright 所有的普通和平新档；新档必须是 `isSandboxMode=false`、1x 资源，且不得载入用户已有存档。
- 以结构化目标控制地表步行；禁止传送、直接改坐标、飞行和跨星球。
- 对可达且经复读确认的矿脉/植被执行正常手工采集；禁止直接增加背包物品。
- 对已解锁配方执行正常机甲手动制造；必须进入制造队列并消耗原料和游戏时间。
- 使用玩家实际持有的建筑物品创建合法预建筑；由游戏施工系统完成，禁止瞬建。
- 逐段施工传送带和分拣器并复读连接；不得直接拼写货物缓冲。
- 对合法设备设置配方，对合法科技选择研究；不得清槽、迁移物料或直接解锁。
- 允许正常玩家 UI 能完成的、经当前版本调用链证明的玩家与容器物品转移；必须同时复读来源减少和目标增加。

每类动作必须有独立的 prepare/commit、幂等、结果查询和前后状态证明。只要无法证明正常游戏会接受该动作、真实成本已发生或结果可确定，就拒绝 commit 或进入 quarantine。

### 2.10 不做尽力回滚；不确定结果时冻结后续写入

M0 不自动回滚。

结果分类：

- `ACTION_FAILED`：能够复读并证明目标仍与 before 完全一致，因此可确认没有副作用。
- `ACTION_OUTCOME_UNKNOWN`：执行已经开始，但无法证明最终状态是 before 或预期 after；可能存在部分副作用。
- 成功：after 与目标配方及全部不变量一致。

一旦出现 `ACTION_OUTCOME_UNKNOWN` 或无法证明状态一致：

```text
当前 session 的 writeHealth = quarantined
```

此后：

- 允许读取和 prepare。
- 所有 commit 返回 `WRITE_SUBSYSTEM_QUARANTINED`。
- prepare 的 `commitBlockers` 必须包含 quarantine。
- 不提供通用“强制解除”、客户端声明成功、猜测字段回滚或重放原动作的工具。
- 仅当当前进程仍保留造成隔离的精确 action record，且通过新的两阶段 reconciliation 对实际物品成本、全部新实体/组件和有向拓扑得到唯一 proof 时，才允许把该 `outcome_unknown` 收敛为成功并清除隔离；任一歧义都保持 quarantine。reconciliation 本身不得再次执行原写入。
- 若当前进程无法保留上述 proof，只允许通过受保护的一次性票据、固定 `LastExit`、高熵 owned save identity、最小 tick、planet、和平、非沙盒和 1x 全部匹配后恢复同一存档形成新 session；不得枚举、选择或尝试其他存档。
- 审计日志必须记录触发原因，但不得包含认证令牌或 planToken。

### 2.11 MCP stdout 禁止写日志

`Spherewright.Mcp` 的 stdout 只用于 MCP stdio 协议。所有日志必须写 stderr；禁止普通 `Console.WriteLine`。

---

## 3. 技术决策

### 3.1 目标框架与引用边界

每个项目独立声明目标框架；不要在根 `Directory.Build.props` 统一设置 `TargetFramework`。

```text
Spherewright.Contracts            netstandard2.0
Spherewright.Bridge.Core          netstandard2.0
Spherewright.Plugin               net472
Spherewright.Mcp                  net8.0

Spherewright.Contracts.Tests      net8.0
Spherewright.Bridge.Core.Tests    net8.0
Spherewright.Mcp.Tests            net8.0
```

依赖方向：

```text
Contracts
   ↑
Bridge.Core
   ↑              ↑
Plugin           Mcp
```

约束：

- `Contracts` 不引用 BepInEx、Unity、DSP、MCP SDK。
- `Bridge.Core` 只引用 `Contracts` 和通用 BCL；不引用 BepInEx、Unity、DSP、MCP SDK。
- `Plugin` 引用 `Contracts`、`Bridge.Core` 和本机游戏/BepInEx 程序集。
- `Mcp` 引用 `Contracts`、`Bridge.Core` 和 MCP SDK；不得引用游戏 DLL。
- 三个 `net8.0` 测试项目只测试 `Contracts`、`Bridge.Core` 和 `Mcp`，不得直接引用 `net472` Plugin。
- Plugin 的游戏调用通过 `Bridge.Core` 中的纯接口抽象，由游戏内手工测试验证。

为了让无游戏 DLL 环境真正可测试，仓库必须包含：

```text
Spherewright.sln          # 全部项目，包含 Plugin
Spherewright.Core.slnf    # 排除 Plugin，仅含 Core、MCP 和三个测试项目
```

无游戏 DLL 的标准命令是：

```bash
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
```

不得再声称根目录裸跑 `dotnet test` 一定能在缺少游戏引用时成功。

### 3.2 BepInEx 加载层

M0 使用 BepInEx 5。入口保持极薄：

```csharp
[BepInPlugin(
    "dev.spherewright.bridge",
    "Spherewright",
    PluginVersion)]
public sealed class SpherewrightPlugin : BaseUnityPlugin
{
    private SpherewrightBridgeHost? _host;

    private void Awake()
    {
        _host = SpherewrightBridgeHost.Create(/* adapters */);
        _host.Start();
    }

    private void Update()
    {
        _host?.PumpMainThread();
    }

    private void OnDestroy()
    {
        _host?.Dispose();
        _host = null;
    }
}
```

不要在 M0 实现 Doorstop、自定义注入器或第二套 Loader。未来是否替换 BepInEx，不影响 Core 协议和游戏适配接口。

BepInEx 5 的实现依据应固定到 BepInEx 5 模板或固定版本文档，不得使用展示 BepInEx 6 API 的 `master` 教程作为唯一依据。

### 3.3 Named Pipe 安全模型

M0 同时使用 Windows 当前用户 ACL 和一次性 bridge token。

#### 启动时

Plugin 必须：

1. 获取当前 Windows 用户 SID。
2. 生成随机 `bridgeInstanceId`。
3. 使用 CSPRNG 生成至少 256 bit 的 `bridgeToken`。
4. 生成不可预测的 Pipe 名，例如：

   ```text
   Spherewright-<pid>-<random>
   ```

5. 用 `PipeSecurity` 仅允许当前用户 SID 连接。
6. 在以下目录创建运行时描述文件：

   ```text
   %LOCALAPPDATA%\Spherewright\runtime\bridge-<pid>.json
   ```

7. 描述文件使用当前用户专属 ACL，并通过“临时文件 + 原子重命名”写入。

描述文件最少包含：

```json
{
  "processId": 12345,
  "bridgeInstanceId": "...",
  "pipeName": "...",
  "authToken": "...",
  "protocolVersion": 1,
  "pluginVersion": "0.1.0",
  "createdAtUtc": "..."
}
```

安全规则：

- `bridgeToken` 不得写入普通日志、MCP Tool 结果或错误消息。
- token 在每次 Plugin 启动时轮换。
- Plugin 正常退出时删除描述文件；启动时可清理 PID 已不存在的陈旧文件。
- 描述文件中的 PID 必须与实际存活的 DSP 进程核对。
- 如果当前用户 ACL 无法可靠设置，M0 Bridge 启动失败并报告明确错误，不降级成无 ACL 的写服务。
- token 只在首次握手中传递；认证失败立即关闭连接并限速记录。

#### MCP 查找描述文件

优先级：

1. 命令行显式 `--bridge-descriptor <path>`。
2. 环境变量 `SPHEREWRIGHT_BRIDGE_DESCRIPTOR`。
3. 默认 runtime 目录中指向存活 DSP 进程的唯一最新描述文件。

不得把 token 放在命令行参数中。

#### 资源限制

M0 默认：

```text
最大已认证连接数：1
请求队列：64
每连接在途请求：8
主线程队列：32
单帧最大长度：1 MiB
```

达到上限时返回 `SERVER_BUSY` 或 `QUEUE_FULL`，不得无界分配内存、Task 或线程。

### 3.4 Bridge 帧与 Envelope

`FrameCodec` 位于 `Spherewright.Bridge.Core`。

帧格式：

```text
4 字节 little-endian 非负 payload length
+ UTF-8 JSON payload
```

要求：

- 长度超过配置上限立即断开。
- 覆盖半帧、粘包、零长度、负数解释、畸形 UTF-8 和畸形 JSON。
- 每个请求有唯一 `requestId`。
- 请求、响应和事件使用统一 Envelope。
- 不允许客户端提供任意 .NET 类型、方法名或反射表达式。

握手请求示例：

```json
{
  "protocolVersion": 1,
  "messageType": "handshake",
  "requestId": "uuid",
  "payload": {
    "bridgeInstanceId": "...",
    "authToken": "...",
    "clientName": "Spherewright.Mcp",
    "clientVersion": "0.1.0"
  }
}
```

普通请求示例：

```json
{
  "protocolVersion": 1,
  "messageType": "request",
  "requestId": "uuid",
  "sessionId": "optional-save-session-id",
  "method": "get_session_state",
  "payload": {}
}
```

### 3.5 MCP SDK 与日志

M0 使用官方 NuGet 包 `ModelContextProtocol`。AGENTS.md 不硬编码可能随时间过期的版本号；编码 Agent 必须在 Gate A 从官方 NuGet 元数据、仓库已有 lock file 或用户明确指定版本中验证一个具体稳定版本，然后：

- 将精确版本写入 `Directory.Packages.props`；
- 禁止 `*`、版本范围和未经明确批准的 prerelease；
- 启用并提交 `packages.lock.json`；
- 在 CI/验收命令中使用 locked mode；
- 将最终采用的精确版本和验证来源记录到 `docs/research/environment.md`。

首次生成 lock file 时可以执行一次 `dotnet restore -p:RestorePackagesWithLockFile=true`；Gate 验收必须随后以 `--locked-mode` 重新恢复成功。

提交到仓库的 `Directory.Packages.props` 不得保留 `<latest>`、占位符或浮动版本。

stdio Server 基础配置：

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

M0 不实现 HTTP、SSE 或 Streamable HTTP。

### 3.6 游戏目录解析优先级

显式输入必须优先于环境变量和自动探测。

统一优先级：

1. 脚本参数或配置中的显式 `DspDir`，例如：

   ```powershell
   ./scripts/sync-game-refs.ps1 -DspDir "D:\SteamLibrary\steamapps\common\Dyson Sphere Program"
   ```

2. 环境变量 `SPHEREWRIGHT_DSP_DIR`。
3. Steam 注册表和 `libraryfolders.vdf` 自动探测。

目标目录必须包含：

```text
DSPGAME.exe
DSPGAME_Data/Managed/Assembly-CSharp.dll
```

不得把开发者绝对路径提交进项目文件。

---

## 4. 仓库结构

M0 创建：

```text
spherewright/
├─ AGENTS.md
├─ README.md
├─ .editorconfig
├─ .gitignore
├─ Directory.Build.props
├─ Directory.Packages.props
├─ Spherewright.sln
├─ Spherewright.Core.slnf
│
├─ src/
│  ├─ Spherewright.Contracts/
│  │  ├─ Protocol/
│  │  ├─ Sessions/
│  │  ├─ Factory/
│  │  ├─ Actions/
│  │  └─ Errors/
│  │
│  ├─ Spherewright.Bridge.Core/
│  │  ├─ Framing/
│  │  ├─ Routing/
│  │  ├─ Authentication/
│  │  ├─ Plans/
│  │  ├─ Idempotency/
│  │  ├─ Safety/
│  │  ├─ Actions/
│  │  └─ Abstractions/
│  │
│  ├─ Spherewright.Plugin/
│  │  ├─ Bootstrap/
│  │  ├─ Hosting/
│  │  ├─ Game/
│  │  ├─ Threading/
│  │  ├─ Transport/
│  │  ├─ RuntimeDescriptor/
│  │  └─ SpherewrightPlugin.cs
│  │
│  └─ Spherewright.Mcp/
│     ├─ BridgeClient/
│     ├─ Tools/
│     ├─ Mapping/
│     └─ Program.cs
│
├─ tests/
│  ├─ Spherewright.Contracts.Tests/
│  ├─ Spherewright.Bridge.Core.Tests/
│  └─ Spherewright.Mcp.Tests/
│
├─ scripts/
│  ├─ locate-dsp.ps1
│  ├─ sync-game-refs.ps1
│  ├─ install-dev-plugin.ps1
│  ├─ run-mcp-inspector.ps1
│  └─ smoke-test.ps1
│
├─ docs/
│  ├─ architecture.md
│  ├─ protocol.md
│  ├─ safety-model.md
│  ├─ manual-test-m0.md
│  ├─ m0-status.md
│  ├─ remote-validation.md          # 延期架构；M0 不实现
│  └─ research/
│     ├─ environment.md
│     └─ game-api-m0.md
│
└─ .local/                         # 必须 gitignore
   ├─ game-refs/
   ├─ publicized-refs/
   ├─ runtime/
   ├─ logs/
   └─ test-output/
```

不要在 M0 创建空壳的 `Planning`、`Agent` 或 `LLM` 项目。

### 4.1 许可证

用户尚未明确确认许可证时：

- 不创建 `LICENSE`。
- README 只能写“License: not selected yet”。
- 可在最终报告中把 MIT 作为候选，但不得擅自落地。

用户明确确认 MIT 后，再创建标准 MIT `LICENSE` 并同步 README。

---

## 5. 本机环境审计

### 5.1 检查游戏与 BepInEx

检查：

```text
<GameRoot>/DSPGAME.exe
<GameRoot>/DSPGAME_Data/Managed/Assembly-CSharp.dll
<GameRoot>/BepInEx/core/BepInEx.dll
<GameRoot>/BepInEx/plugins/
```

若 BepInEx 不存在：

- 不要静默下载或修改游戏目录。
- 继续完成 Core、MCP、协议和自动化测试。
- 生成清晰安装说明。
- 把 Gate A 的游戏内加载验证标为 blocked。

### 5.2 同步本机编译引用

`scripts/sync-game-refs.ps1` 必须：

- 接受最高优先级的显式 `-DspDir`。
- 从游戏目录复制实际需要的 DLL 到 `.local/game-refs/`。
- 计算 SHA-256。
- 必要时通过已记录版本的 publicizer 生成 `.local/publicized-refs/`。
- 永不覆盖游戏原文件。
- 永不把游戏 DLL 加入 Git。
- 找不到依赖时返回非零退出码和清晰错误。

优先只引用：

```text
Assembly-CSharp.dll 或 publicized 版本
UnityEngine.dll
UnityEngine.CoreModule.dll
BepInEx.dll
0Harmony.dll（仅在已证明需要 Harmony 时）
```

不得为了省事复制整个 `Managed` 目录。

### 5.3 记录环境

`docs/research/environment.md` 必须包含：

```text
操作系统
.NET SDK 版本
DSP 版本
游戏目录来源：explicit / environment / auto-detected
提交时脱敏后的路径表达
BepInEx 版本
关键程序集 SHA-256
目标框架
构建命令
Plugin 安装目录
MCP SDK 精确版本
```

---

## 6. M0 — First Red Matrix：四个验收门

M0 可以跨多次 Agent 执行。每个门必须有独立证据；不得把代码存在、构建通过、旧沙盒演示或截图当作红色矩阵完成证据。

`docs/m0-status.md` 必须记录 Gate A/B/C/D 的 `not-started | in-progress | blocked | complete` 状态和证据链接。每次执行从最早未完成的门继续。旧的和平沙盒基础线只保留为历史研究证据，不计入任何新 Gate 的完成状态，也不得在 M0 验收中调用。

M0 最终目标是：DSP 到达主菜单后，不再接受人工游戏内操作；外部 Agent 只调用 Spherewright MCP，在 Spherewright 本次创建的普通和平 1x 新档中，遵守真实物品、能源、时间、距离、科技和施工规则，产出至少 1 个红色矩阵。

---

## Gate A — 环境、加载与安全 status

Gate A 保留现有基础设施范围：四个正式项目、三个测试项目、Core solution filter、BepInEx 5 薄入口、有界主线程调度、当前用户 ACL、启动轮换 token、Named Pipe、stdio MCP 和 `get_bridge_status`。

标准验收命令：

```powershell
dotnet restore Spherewright.Core.slnf --locked-mode
dotnet build Spherewright.Core.slnf --no-restore
dotnet test Spherewright.Core.slnf --no-build
dotnet build Spherewright.sln --no-restore
```

Gate A 完成还要求：Plugin 只加载一次；主菜单和退出无崩溃；错误 token、超大帧、畸形帧和队列满被有界拒绝；游戏未启动时 MCP 返回 `BRIDGE_NOT_READY`；stdout 无日志污染；日志不包含 bridgeToken、planToken、绝对游戏路径或存档敏感信息。

---

## Gate B — 普通新档、所有权与结构化观察

### B1. 创建普通和平新档

实现两阶段新档创建：

```text
prepare_new_game
commit_new_game
```

M0 请求只允许选择合法 seed 和 star count；资源倍率固定 1x，和平模式固定开启，沙盒固定关闭。服务端生成不可预测的 save name，客户端不能指定可能覆盖用户存档的名称。

commit 必须使用当前版本正式新游戏路径，并复读：

```text
GameDesc.isPeaceMode == true
GameDesc.isSandboxMode == false
GameMain.sandboxToolsEnabled == false
resourceMultiplier == 1.0
当前 GameData 是本次 prepare/commit 所创建的精确实例
```

允许跳过纯开场飞行动画，但必须在 `game-api-m0.md` 证明跳过路径只建立与正常落地等价的出生点/着陆舱状态，不发物品、不解锁科技、不加速生产。无法证明时改用完整开场流程并以结构化动作完成。

### B2. 存档隐私与 session

- 不枚举 Save 目录、游戏读档列表或任何既有存档名。
- 不载入非 Spherewright 创建并登记的存档。
- 当前进程只通过精确 `GameData` 对象身份拥有新建 session；进入其他 session 时只返回受限 bridge 状态，不读取 save name、星球、玩家、背包或工厂内容。
- 重启后续玩只能通过 Spherewright 自己生成的高熵 save name、受保护的一次性恢复票据和固定 `LastExit` 精确载入；必须校验来源进程已退出、最低 game tick、planet、和平、非沙盒和 1x，成功后立即消费票据。不得枚举或让客户端选择存档。
- 切档、退出、Plugin 重启使所有未接受 planToken 失效；每次载入生成新 `sessionId`。

`get_session_state` 至少返回：所有权、session/planet/tick、`combatModeStatus`、`sandboxModeStatus`、资源倍率、write health、write blockers 和当前能力。和平或沙盒状态为 unknown 时 fail-closed；沙盒为 enabled 时所有 M0 commit 返回 `SANDBOX_MODE_ACTIVE`。

### B3. M0 观察面

所有快照在 Unity 主线程生成并深复制。至少实现：

```text
get_player_state
get_progression_state
get_recipe_catalog
get_build_catalog
list_resource_nodes / inspect_resource_node
list_factory_entities / inspect_factory_entity
get_power_summary
get_action_result
```

观察数据至少覆盖：

- 玩家位置、地表/飞行状态、机甲能量、背包物品、手动制造队列、施工无人机状态。
- 当前星球矿脉、原油、可手采对象的身份、位置、类型、剩余量和可达性基础数据。
- 物品、配方、建筑、科技的当前 LDB 身份与解锁状态。
- 实体/预建筑身份、位置、朝向、物流连接、配方、缓冲、生产进度和供电状态。
- 当前研究目标、hash 进度、矩阵需求和已解锁科技。
- M0 原料到红色矩阵的当前运行时依赖图；只读 helper 可以做确定性配方展开和几何候选计算，但不能替外部 Agent 自主决定目标。

列表使用短期不可变快照和不透明 cursor，绑定 session、planet、过滤条件、snapshot identity、offset 和 expiry。详情和所有 prepare 必须重新读取实时状态。

Gate B 验收：普通新档复读全部非沙盒不变量；主菜单/进入/退出状态正确；受限 session 无内容泄漏；空列表和无效目标有明确错误；所有 cursor 跨 session/planet/filter 使用被拒绝；没有调用 Computer Use。

---

## Gate C — 正常玩法动作原语

### C1. 统一动作协议

所有动作继续使用：

```text
inspect -> prepare_* -> commit_* -> get_action_result / inspect
```

prepare 永远无副作用并返回计划、真实成本预算、预计完成条件和 blockers。commit 需要 `sessionId`、`planetId`、`planToken`、UUID `idempotencyKey`，进入 Plugin 级 single-flight 后在主线程复读完整目标、玩家状态、资源预算、模式和解锁状态。

动作状态至少包括：

```text
reserved
queued
executing
waiting_for_game
completed
action_failed
outcome_unknown
```

游戏 tick 驱动的移动、采集、手搓、施工和科研可以返回 `waiting_for_game`；客户端取消只停止等待，不得偷偷中断已经开始的正常游戏流程。响应丢失后必须使用同一幂等键恢复，不能换键重做。

### C2. 地表移动

- 只允许当前星球地表步行目标；禁止直接写 `position/uPosition`、瞬移、飞行和跨星球。
- 使用经当前版本证明的玩家控制/导航业务路径，并受正常移动速度、碰撞和地形约束。
- prepare 返回距离、预计可达性和到达容差；commit 启动一个可查询动作。
- 完成必须复读玩家仍在同一 planet、处于允许状态且到目标球面距离在容差内。

### C3. 手工采集与机甲制造

- 手采绑定具体 vein/vege/对象身份、剩余量、玩家距离、背包容量和采集类型。
- 采集必须经过正常玩家采集流程并消耗游戏时间/能量；结果证明目标剩余量下降与背包增加守恒。
- 手搓只接受已解锁且允许手工制造的配方，进入正常 replicator 队列；原料、产物和耗时必须来自运行时配方。
- 不允许调用任何“添加物品”API补齐材料，也不允许直接写队列完成量。

### C4. 正常施工与物流

- 建筑、传送带和分拣器必须来自玩家已有库存，位置/朝向/连接经过 DSP 正式 build condition 校验。
- commit 只创建正常预建筑；禁止由 Spherewright 调用 `BuildFinally`、直接创建 entity 或凭空补充建筑物品。
- 施工由机甲无人机和游戏 tick 完成；动作必须区分 `prebuild_created` 与 `completed`，并复读库存消耗、预建筑消失、实体出现及组件身份。
- 确定性几何 helper 可以给出矿机覆盖、建筑候选、传送带路径和分拣器端口候选，但不得跳过正式校验与逐段资源消耗。
- 合法拆除或玩家/容器转移如被 M0 使用，也必须走正常业务路径并复读双边变化。

### C5. 配方、科研和设备配置

- 配方必须存在、已解锁且适用于精确设备；改变正在生产或有不兼容缓冲的设备时 fail-closed。
- 科研动作只选择/排队合法科技；不得直接写 unlocked、hash 或矩阵计数。
- 矩阵研究站的生产/研究模式、精炼厂、冶炼设备、制造台、矿机和原油萃取站的配置都必须复用真实 UI/业务调用链并复读。
- 电力不足、物流未连通、配方锁定或原料不足是正常可观察状态，不得用注入或瞬建掩盖。

### C6. Gate C 安全矩阵

至少验证：写入关闭、和平 unknown、沙盒 enabled、非 owned session、过期/伪造 plan、旧 session、目标变化、距离不足、资源不足、背包满、配方/科技未解锁、非法建筑位置、连接变化、同键并发、同键冲突、响应丢失、确定失败和 outcome unknown quarantine。

每类合法 commit 都必须有 before/after 和资源守恒证明。无法证明 before 或 expected after 时返回 `ACTION_OUTCOME_UNKNOWN` 并隔离当前 session；不得通过猜测回滚玩家、库存、科技或工厂字段。

---

## Gate D — 普通模式端到端产出第一颗红色矩阵

Gate D 不实现“一键红糖”或 Plugin 内置规划器。外部 Agent 必须组合 Gate B/C 的原语完成以下可审计阶段，具体原型 ID、配方和科技依赖由当前 LDB 决定：

1. 在主菜单通过 MCP 创建普通和平 1x 新档，并确认 sandbox 始终关闭。
2. 通过正常步行、手采和手搓获得第一批合法材料与建筑。
3. 建成由真实矿脉/煤/原油供料、正常供电的采矿、冶炼、制造和物流设施。
4. 自动生产蓝色矩阵，并通过正常研究流程解锁红色矩阵所需科技。
5. 通过煤的正常加工获得高能石墨，通过原油萃取和精炼获得氢；不得从背包或仓储注入原料。
6. 在矩阵研究站选择红色矩阵配方，让游戏生产系统产出第一颗红色矩阵。
7. 保存 Spherewright 自建存档，并生成结构化验收记录。

Gate D 完成证据必须同时证明：

- 同一 owned session 从普通新档推进，`isSandboxMode=false`、`sandboxToolsEnabled=false`、1x 资源在整个运行期间成立。
- DSP 到达主菜单后没有 Computer Use、视觉识别、键鼠宏或人工游戏内协助。
- 没有物品/科技/建筑/缓冲注入，没有 Spherewright 直接 `BuildFinally`，没有存档修改或加速游戏时间。
- 红色矩阵总量从 0 增加到至少 1；产出来自合法矩阵研究站 recipe tick，且设备在完成时仍连接、供电、配方正确。
- 上游铁、铜、石、煤、原油及中间件都能从动作审计和快照追溯到正常采集/生产来源；数量守恒允许游戏配方和正常损耗，不允许来源不明的正增量。
- 用 MCP 复读最终玩家、科技、建筑、物流、供电、缓冲和红色矩阵快照；同键重试没有第二次副作用；正常保存成功。

截图可以作为额外展示，但不是验收要求；结构化状态、动作结果、审计日志和游戏内复读才是主要证据。

---

## 7. 稳定错误码

M0 至少定义：

```text
BRIDGE_NOT_READY
AUTH_FAILED
SERVER_BUSY
QUEUE_FULL
GAME_NOT_LOADED
NO_LOCAL_PLANET
UNSUPPORTED_GAME_VERSION
PEACEFUL_MODE_REQUIRED
PEACEFUL_MODE_UNKNOWN
SANDBOX_MODE_ACTIVE
SANDBOX_MODE_UNKNOWN
NORMAL_RESOURCE_MULTIPLIER_REQUIRED
WRITES_DISABLED
WRITE_SUBSYSTEM_QUARANTINED
SESSION_NOT_OWNED
INVALID_REQUEST
INVALID_ENTITY
INVALID_RESOURCE_TARGET
TARGET_IDENTITY_MISMATCH
INVALID_RECIPE
RECIPE_NOT_SUPPORTED_BY_BUILDING
RECIPE_LOCKED
INVALID_TECHNOLOGY
TECHNOLOGY_LOCKED
TECHNOLOGY_PREREQUISITE_NOT_MET
TARGET_OUT_OF_RANGE
TARGET_UNREACHABLE
PLAYER_STATE_UNAVAILABLE
PLAYER_BUSY
INVENTORY_INSUFFICIENT
INVENTORY_FULL
MECHA_ENERGY_INSUFFICIENT
BUILD_LOCATION_INVALID
BUILD_ITEM_MISSING
BUILD_CONNECTION_INVALID
PREBUILD_NOT_COMPLETED
NO_POWER
ASSEMBLER_NOT_IDLE
ASSEMBLER_BUFFERS_NOT_EMPTY
STALE_SESSION
STALE_STATE
STALE_CURSOR
PLAN_NOT_FOUND
PLAN_EXPIRED
PLAN_ALREADY_BOUND
IDEMPOTENCY_CONFLICT
IDEMPOTENCY_CAPACITY_EXCEEDED
ACTION_NOT_FOUND
ACTION_NOT_STARTED
ACTION_IN_PROGRESS
ACTION_FAILED
ACTION_OUTCOME_UNKNOWN
REQUEST_TIMEOUT
INTERNAL_ERROR
```

错误响应必须包含：

```json
{
  "code": "STALE_STATE",
  "message": "The target or resource state changed after preparation.",
  "retryable": true,
  "recovery": "Inspect the current state again and create a new plan."
}
```

不得把 token、堆栈、本机绝对路径或 DSP 私有内部对象序列化给 MCP 客户端。

---

## 8. M0 MCP Tool

M0 的目标工具面如下；实现可按 Gate 递增暴露，但未实现的工具不得用假数据或沙盒替代：

```text
spherewright_get_status
spherewright_prepare_new_game
spherewright_commit_new_game
spherewright_get_session_state
spherewright_get_player_state
spherewright_get_progression_state
spherewright_get_recipe_catalog
spherewright_get_build_catalog
spherewright_list_resource_nodes
spherewright_inspect_resource_node
spherewright_list_factory_entities
spherewright_inspect_factory_entity
spherewright_get_power_summary
spherewright_prepare_move
spherewright_commit_move
spherewright_prepare_harvest
spherewright_commit_harvest
spherewright_prepare_handcraft
spherewright_commit_handcraft
spherewright_prepare_build
spherewright_commit_build
spherewright_prepare_dismantle
spherewright_commit_dismantle
spherewright_prepare_transfer
spherewright_commit_transfer
spherewright_prepare_configure_building
spherewright_commit_configure_building
spherewright_prepare_select_research
spherewright_commit_select_research
spherewright_get_action_result
spherewright_get_m0_progress
```

旧 `spherewright_prepare_test_world` / `commit_test_world` 名称已经迁移为 `prepare_new_game` / `commit_new_game`，并固定为普通非沙盒新档。旧 `basic_production_line` 三个沙盒复合工具已经从 MCP 公共工具面移除；历史实现可以暂留用于研究，但不得作为 M0 证据。

规则：

- Tool 名称、参数和 schema 描述使用清晰英文。
- 每个参数使用 `[Description]`。
- 返回结构化对象，不返回难以解析的长文本。
- prepare 与 commit 是两个明确 Tool，不使用同一个 `dryRun` 布尔参数混合两种请求体。
- 除主菜单新档创建外，commit 必须显式提供 `sessionId`、`planetId`、`planToken` 和 `idempotencyKey`。
- Tool 超时不能卡死 MCP Server。
- commit 的不确定超时映射为 `ACTION_OUTCOME_UNKNOWN`，不是普通超时。
- MCP 不暴露 bridge token、运行时描述文件路径、plan 内部记录或堆栈。
- 不提供任意方法名、任意字段写入、任意物品 ID 注入、任意坐标传送或“完成红糖”复合写工具。

---

## 9. 测试要求

### 9.1 `Spherewright.Contracts.Tests`

覆盖：

- DTO 序列化契约。
- 枚举和错误码稳定值。
- combat mode 三态。
- sandbox mode 三态。
- 玩家、资源、背包、科技、实体、连接、生产和动作 DTO。
- 各动作规范状态哈希、资源预算和版本字段。
- 向后兼容性测试样例。

### 9.2 `Spherewright.Bridge.Core.Tests`

必须脱离 BepInEx、Unity 和游戏 DLL，覆盖：

- FrameCodec：正常帧、半帧、粘包、零长度、超大帧、畸形 UTF-8、连接中断。
- Envelope 和协议版本校验。
- 认证握手状态机和认证前请求拒绝。
- 计划令牌随机性接口、绑定、过期、切 session 失效。
- prepare 在写入关闭时仍成功并返回 blocker。
- `confirmedPeaceful / combatEnabled / unknown` 的 blocker 计算。
- 完整目标身份、资源预算和 state hash 比较。
- cursor 绑定 session、planet、snapshot 和过滤条件。
- 幂等缓存跨逻辑连接保持。
- 同键 single-flight。
- 同键异请求冲突。
- action 状态转换。
- commit 超时后的 `ACTION_OUTCOME_UNKNOWN` 语义。
- quarantine 触发及后续 commit 拒绝。
- Fake Game Adapter 下移动、采集、手搓、施工、转移、配置和科研的成功、确定失败与未知结果分类。
- 普通模式 blocker：sandbox、非 owned session、资源不足、距离不足、未解锁和非法施工。
- 资源守恒检查和禁止来源不明正增量。

### 9.3 `Spherewright.Mcp.Tests`

不得启动真实 DSP。使用 Fake Bridge Server 验证：

- 描述文件解析优先级。
- 认证握手。
- 连接成功、拒绝和重连。
- 结构化 Bridge 错误到 MCP 结果的映射。
- 所有已暴露 Tool 的参数映射和 schema。
- prepare/commit schema 不混用。
- 读超时与 commit outcome unknown 的区别。
- stdout 无普通日志，日志进入 stderr。
- 多次调用不泄漏连接、Task 或 CancellationTokenSource。

### 9.4 Windows 安全与文件集成测试

在 Windows CI 或本机运行，覆盖：

- runtime 目录和描述文件仅当前用户可访问。
- Pipe ACL 仅允许当前用户 SID。
- token 启动时轮换。
- Plugin 正常关闭删除描述文件。
- 陈旧 PID 描述文件被安全忽略。

这些测试可以是标记为 Windows-only 的集成测试，但不得用纯 mock 代替全部 ACL 验证。

### 9.5 游戏内集成测试

只允许由当前 Spherewright 进程新建的普通和平 1x 存档。不得打开 Load Game 页面、枚举存档或读取用户已有存档；不得调用 Computer Use，也不得要求用户替 Agent 完成任何游戏内动作。

`docs/manual-test-m0.md` 至少包含：

1. 安装 Plugin，确认源代码默认 `AllowWrites=false`，启动 DSP 到主菜单。
2. 验证 status、错误 token 和写入关闭 blocker。
3. 显式启用本地写入后，通过 prepare/commit 创建新档并复读 peaceful、non-sandbox、1x、owned session。
4. 逐类验证观察、移动、手采、手搓、预建筑施工、传送带/分拣器、物品转移、设备配置和科研选择。
5. 每类动作验证无副作用 prepare、过期/stale 拒绝、同键重试和 before/after 资源守恒。
6. 使用同一套工具从新档完成蓝色矩阵、正常科研、煤/原油处理和红色矩阵生产。
7. 复读至少 1 个红色矩阵、完整上游状态和正常保存结果。
8. 退出并确认无未处理异常、runtime descriptor 已删除；恢复 `AllowWrites=false`。

故障注入只允许 Fake Adapter 或可证明不会污染游戏状态的专用路径。禁止在用户主要存档或非 Spherewright 存档上测试。

---

## 10. 性能、并发和生命周期

必须遵守：

- `Plugin.Update()` 按 `MaxRequestsPerFrame` 和 `FrameBudgetMs` 双重预算处理。
- 主线程不等待 Pipe I/O。
- Pipe 线程不无限等待主线程 Future。
- 一旦写 action 进入 `executing`，客户端取消只停止等待，不中断游戏调用。
- list 使用 limit 和短期快照 cursor。
- 日志不得每帧刷屏。
- 退出游戏时停止接收新请求，完成或标记在途 action，再在有限时间内释放线程、Pipe 和 CTS。
- 不创建无界 Channel、队列、永久 Task 或每请求独立线程。
- 容量达到上限时明确拒绝，不通过增加后台线程绕过。

若出现卡顿，先降低主线程工作量、缩小快照和批次；不得把游戏状态访问搬到后台线程。

---

## 11. Git、文件和许可证安全

编码 Agent 必须：

- 开始前运行 `git status --short` 并记录现有改动。
- 不覆盖、不回滚用户已有修改。
- 禁止 `git reset --hard`、`git clean -fd`、强制 checkout 和 force push。
- 不提交游戏 DLL、反编译源码、存档、runtime descriptor、token、BepInEx 日志或本机路径。
- 不自动创建 GitHub 远程仓库、Issue、PR、Release 或 Thunderstore 包。
- 不自动 commit，除非用户明确要求。
- 生成文件必须可由脚本重建。
- 引用社区代码时记录仓库、文件、许可证和采用方式；不得把 GPL 实现复制进计划采用宽松许可证的代码库。
- 用户未确认许可证前不创建 `LICENSE`。

### 11.1 实现经验账本与持续复验

`docs/experience-ledger.md` 是实现、游戏 API、运行环境、安全处置和正常玩法控制经验的权威账本。编码 Agent 必须：

- 将本次实现中产生的每一条可复用经验在同一次执行内落盘；涉及安全边界、动作结果不确定或下一次写入前提的经验，必须在下一次游戏写入前先记录。
- 每条经验至少记录：稳定 ID、日期、状态、适用范围、当前结论、直接证据、限制或反例、复验触发条件、关联代码/测试/文档和最近复验时间。
- 只使用 `observed | validated | superseded | invalidated` 四种状态。单次现场现象先记为 `observed`；只有独立复读、测试或当前版本实机证据足以支持适用范围时才升级为 `validated`。
- 新证据与旧经验冲突时，先降低旧条目的状态或标记 `superseded` / `invalidated`，再写当前结论；不得让相互矛盾的“现行结论”并存。保留修订记录和替代条目链接，不静默抹去历史。
- 在每个实现批次结束、每累计 10 个成功游戏写动作、Plugin 部署或重启、DSP/程序集版本变化、写入隔离或恢复、M0 Gate 状态变化以及最终交接前，复核新增条目和所有受影响旧条目；以先到的触发点为准。
- 复验失败时立即更新账本及受影响的计划/安全判断，不能继续依赖已经失效的经验。
- 账本只保存脱敏证据；不得写入 token、存档内容、runtime descriptor、用户私密路径或其他被本文件禁止提交的材料。

`docs/research/` 保存程序集/API 的详细证据，`docs/m0-status.md` 保存 Gate 状态；经验账本链接它们，但不以摘要替代原始研究或验收证据。

---

## 12. M0 完成定义

### Gate A 完成

- `Spherewright.Core.slnf` 在无游戏 DLL 环境完成 restore/build/test。
- 有游戏引用时完整 Solution 构建成功。
- BepInEx 成功加载 Plugin，主菜单和退出无崩溃。
- Named Pipe 使用当前用户 ACL、随机 pipe 名和启动轮换 token。
- status 链路工作，MCP stdout 未污染。

### Gate B 完成

- prepare/commit 创建的是 owned、peaceful、1x、non-sandbox 新档；无法创建或复读时不得降级沙盒。
- 不枚举或读取非 Spherewright 存档，受限 session 不泄漏内容。
- `combatModeStatus` 和 `sandboxModeStatus` 为三态，unknown fail-closed。
- 玩家、资源、配方/科技、工厂、物流、供电和生产快照全部来自主线程并深复制。
- state hash 使用版本化规范编码；cursor 绑定 session、planet、snapshot 和过滤条件。

### Gate C 完成

- 地表移动、手采、手搓、正常预建筑施工、物流连接、物品转移、设备配置和科研选择都具有可用的结构化动作。
- prepare 始终无副作用，并返回 planToken、expiry、真实资源预算和 commitBlockers。
- commit 只接受有效 planToken 和幂等键，复读完整身份、玩家状态、资源预算与 state hash。
- 所有动作遵守距离、库存、机甲能量、配方/科技解锁、施工、供电和游戏时间规则。
- 幂等缓存跨连接、single-flight，响应丢失可通过同键恢复结果。
- 不用普通 timeout 暗示无副作用。
- 不实现猜测式回滚。
- outcome unknown 会冻结当前 session 后续写入。
- 每类游戏内合法写入都有 before/after、完成条件和资源守恒复读验证。

### Gate D 完成

- 从 Spherewright 创建的普通和平 1x 新档开始，全程没有 Computer Use、视觉/键鼠自动化或人工游戏内协助。
- 全程没有沙盒、物品注入、直接科技解锁、瞬建、直接缓冲写入、存档修改或时间加速。
- 外部 Agent 通过 MCP 原语完成采集、手搓、供电、采矿、冶炼、制造、蓝色矩阵、正常科研、煤/原油加工和红色矩阵生产。
- 结构化复读证明红色矩阵从 0 增加到至少 1，生产设备与完整上游链在完成时状态一致且正常保存成功。
- 审计证据可以追溯每个来源不明的正增量检查；旧沙盒基础线证据不参与 Gate D。

只有 A、B、C、D 全部满足，M0 才算完成。

必须存在且与实现一致：

```text
README.md
docs/architecture.md
docs/protocol.md
docs/safety-model.md
docs/manual-test-m0.md
docs/m0-status.md
docs/remote-validation.md
docs/research/environment.md
docs/research/game-api-m0.md
```

---

## 13. Agent 最终汇报格式

每次执行结束必须按以下格式汇报：

```text
## 当前 M0 门槛
- Gate A / Gate B / Gate C / Gate D

## 已完成门槛
- ...

## 本次完成内容
- ...

## 未完成 / Blocked
- 门槛：...
- 项目：...
- 原因：...
- 已完成的替代工作：...

## 关键实现决策
- ...

## 修改文件
- path: purpose

## 执行过的命令
- command

## 测试结果
- Core solution restore/build/test: ...
- Windows ACL integration: ...
- full solution build: ...
- in-game verification: ...

## 游戏 API 证据
- 游戏版本：...
- Assembly-CSharp SHA-256：...
- 和平、沙盒和 1x 普通新档判定路径：...
- 玩家移动、采集、手搓和施工路径：...
- 资源、科技、工厂、物流、供电与生产读取路径：...
- 配方配置、科研选择与红色矩阵产出路径：...

## 风险
- ...

## 下一步
- 列出最早未完成的 M0 门槛及其第一项具体工作。
- 只有 M0 A/B/C/D 全部完成后，才允许列 M1 的第一项。
```

不得只说“已完成”；必须提供构建、测试、日志或游戏内复读证据。

---

## 14. 后续路线图——M0 不实现

Spherewright 保持 **tool-first**。以下阶段仍由外部 Agent 调用；是否加入内置 LLM 是更晚的可选产品层，不属于底层控制协议的前置条件。

### 延期工程化事项 — LAN 游戏验证机

在本机 M0 第一颗红色矩阵可以从普通新档稳定复现后，才允许根据用户新的明确指令实现 `docs/remote-validation.md` 中的局域网验证机架构。Named Pipe 必须继续留在游戏机本地；不得为了远程验证把 Plugin 暴露成未认证网络服务。本阶段不计入 M0，也不是当前最早工作项。

### M1 — Overseer

```text
生产摘要
电力摘要
物流摘要
生产链追踪
更完整的生产告警
安全扩产建议
```

目标：外部 Agent 能改善 M0 红色矩阵产线的瓶颈并稳定扩大产量。

### M2 — Foundry

```text
参数化工厂模板
候选选址
蓝图预览
合法性检查
资源核算
prepare / commit
动作状态与取消
```

目标：安全扩建基础材料产线。

### M3 — Governor

```text
确定性生产配平 helper
生产链分解
扩产规模计算
执行后稳定产量观测
失败分类和重新规划所需状态
```

目标：让外部 Agent 通过高层工具完成闭环扩产，而不是让 LLM 手写底层坐标和机器数量。

### M4 — Voyager

```text
起飞、航行、着陆、曲速
跨星球访问
跨星球物流与能源预算
```

目标：支持外部 Agent 从无黑雾新档解锁星际物流。

### M5 — Ascension

```text
跨星系资源规划
太阳帆和运载火箭产业链
戴森壳规划与建设
长期产能与故障恢复
```

目标：外部 Agent 可以通过 Spherewright 从和平模式新档建成持续输出功率的戴森球系统。

### 可选后续：内置 Agent Runtime

只有 Observation、Action、蓝图、移动和长期动作恢复稳定后，才评估：

```text
Spherewright.Agent
Spherewright.Planning
可选 LLM provider adapters
```

内置 Agent 必须是 Spherewright Core 的消费者，不能把 LLM 逻辑反向耦合进 Plugin、Bridge 协议或游戏适配层。

多人、Nebula、黑雾和战斗不在路线图内。

---

## 15. 参考资料

实现时优先使用官方或上游项目；二手博客只能作为线索，不能作为 DSP API 事实依据。

- BepInEx 5 Plugin 模板：<https://github.com/BepInEx/BepInEx.Templates/blob/master/BepInEx.Templates/templates/BepInEx5.PluginTemplate/Plugin.cs>
- DSP 社区 BepInEx 包：<https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/BepInEx/>
- 官方 MCP C# SDK：<https://github.com/modelcontextprotocol/csharp-sdk>
- MCP C# SDK Getting Started：<https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md>
- DSP Mod 编译引用和 publicized assembly 示例：<https://github.com/starfi5h/DSP_Mod>
- 当前维护的 DSP Mod 工程组织示例：<https://github.com/soarqin/DSP_Mods>

社区代码只用于理解调用方式和工程实践；复制代码前必须检查许可证。
