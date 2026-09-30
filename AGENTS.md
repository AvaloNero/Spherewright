# AGENTS.md — 当前执行规范

Spherewright 是《戴森球计划》的外部 Agent 控制层，不内置 LLM、自主目标规划或无界扩产循环。当前开发版本为 **0.4.0 Overseer / Foundry / Governor**：诊断、有限施工与配平，并为跨星系扩张准备；实际跨恒星航行属于 0.5。版本验收与发行门见 [ROADMAP.md](ROADMAP.md)，事实进度只看 [当前快照](docs/current-status.md)，不可用历史段落替代新现场。

## 角色和工作入口

- root 主会话保持现有模型；两只 Luna 均为 **gpt-6-luna / max**。root 确定目标、批准范围、困难接口与安全边界，处理设计/代码实质变更并独立验收游戏结果；一只 Luna 是唯一游戏 writer，只在批准的有限阶段内准备参数、复用固定入口、执行并取证，不自授权新工序、不自签独立验收。按需另启一只离线维护 Luna，只做直接相关离线测试、按 root 已核验事实整理单份事件/快照短引用，以及 root 确认证据和范围后的单一目的 commit/push 与准确 SHA 的 CI 收集；不触碰游戏、Bridge、加载、保存或部署。各任务按明确文件分区；Git commit/push 仅由离线维护 Luna 串行执行，禁止 `git add .` 或混入他人 dirty 改动。CI 收集可与不依赖该变更的游戏准备/取证并行；pending 不算通过，main 上 red 先修。十写后的独立审计、文档、commit/push、绿 CI 和 root 明确交接门保持不变。无需时不保留额外代理，不另派 Sol/Terra，不擅自换模型/推理档位。
- 成熟工序只交一个短任务包：阶段结果与停止条件；批准对象、入口、参数、数量/时间预算；当前证据引用及 accepted/在途边界。游戏 Luna 返回结果、accepted/在途、实际差量、失败字段、原证据索引、未证明项和下一 blocker。完整回执留在受保护证据库，不在对话里复制大日志；主会话直接核原始回执和覆盖，不重采无变化证据。
- 固定模板只更换现场参数。直接复用 `scripts/SpherewrightActionClient.ps1` 的 `Invoke-SpherewrightNormalAction`、`ValidatePrepared`、`Wait-SpherewrightAction` 和 `Wait-SpherewrightPlayerSettled`；科研→核验→保存及单仓取材→手搓→核验→保存薄模板见 `scripts/SpherewrightStageTools.ps1`，分别须预留两/三个外部 accepted 槽位，不自动选目标。需要受保护原回执时，普通客户端之后加载现有 `.local/SpherewrightAuditedBridgeClient.ps1`，最后加载薄模板（模板不重新导入/覆盖 transport）。短命直接 Bridge 调用中的 prepare 和 commit 必须在同一受保护调用方上下文，不能为跨进程携带而打印或落盘 token。普通 MCP 双工具调用仍遵守公开协议。
- 已批准且前置满足的相邻动作可在一个有限阶段内逐步 fresh prepare/commit/terminal/readback，阶段末统一整理。不要每步重读全历史、重写 caller、重开方案讨论或等无关 CI。两次同类原生拒绝、真实偏差或授权门触发即停，主会话重设计；成功前缀不重做。
- 委派到首个业务 prepare、prepare/commit 到 terminal/读回、游戏物理等待、取证/验收/文档/Git/CI 分别计时。准备超时要真正收敛或停止，不反复续时；**超时本身不是接管许可**。接管前必须证明原执行者已停止、无在途动作、无未核销结果，并完成单写者交接。有 commit 意图或已 accepted 时，只核同一 action/幂等键；即便摘要异常或读回尚未站稳，也不能改判未执行、换键重放。

## 阶段规划与证据

- 生产修复先从目标消费者反向核：runtime `itemId`/名称/配方、真实需求或堵塞、实际连接拓扑、旧设备槽位/带方向/跨接几何与下游容量；先证最难接口，再预算上游材料、供电、路线和顺序。阶段目标应是可验收的实际入料/加工，不是孤立铺一段带。未来实体尚不存在时只标“待现场原生预检”，可以批准最小接口验证片，不能提前宣称整链通过或囤短命 token。
- 现行 0.4 主线先完成已批准的供给/物流与持续产量、有限蓝图生命周期及 Governor 已声明实验，再补翘曲器/燃料/运输备料和最终包；已成功前缀不重做。非直接硬阻塞不增加通用工具/观察字段、建筑或蓝图类型、传送带升级、`2012→2013` 升级、任意布局或全厂整理。具体已过门与下一 blocker 以当前快照及 Roadmap 原证据为准，不因历史进度倒退施工。
- 同一不可变完整快照可本地复用回答多个只读拓扑问题；涉及实时 buffer、写前授权、现场漂移时仍须 fresh 读取。静态配置与动态库存/功率分别比较，允许变化也须有明确规则与证据，不能粗暴忽略字段。
- 每个阶段仅有一份脱敏事件事实与受保护原回执索引；`docs/current-status.md` 覆盖更新当前快照，存档日记只加短时间线/链接，Roadmap 只写验收门，经验账本仅记新增或修订的可复用结论。旧记录保留，不在多个文档复制整段流水。当前状态的权威数值来自 fresh session、durable Journal、原 action 回执和外部 accepted 台账；文档不是机器状态源。
- 生产实验在开始前固定目标实体/物品、采样频率、游戏 tick 窗口、库存趋势、电力/燃料、异常和截止条件；复用已有有界采样入口，原始数据完整留存，仅在异常、阶段边界或结束时交模型。不得拼接有缺口窗口；Governor 2×声明须施工前锁定非零稳定基线、≤10%误差、连续至少36000 game ticks。有限缓存、名义容量、机器翻倍或 `P=C` 不等于持续产出/配平。

## 不可降低的游戏和安全边界

- 游戏访问遵守 [安全模型](docs/safety-model.md)、[协议](docs/protocol.md) 和 [包内 playbook](docs/agent-playbook.md)。`GameMain`、`GameData`、工厂、背包、科技和 Unity 对象只在 Unity 主线程触碰；后台只处理协议/深复制 DTO。Plugin 是当前 DLL 的薄适配，MCP 不复制游戏规则。
- 所有写入为 `inspect → fresh 原生 prepare → 精确计划核验 → 唯一 commit → 同 action terminal → 相关材料/实体/双向连接 fresh 读回`。prepare 无游戏副作用，commit 绑定 session、planet、短期 token、唯一幂等键、精确目标/状态哈希，并由 Plugin single-flight 重验。revision、selection hash、Journal 序号均取真实返回值，不以 accepted 推算，不硬编码旧值。计划在失败或跨 session 后失效，不能复用。
- 已接受动作即使调用方超时、断线、显示解析错误、读回未站稳或本地脚本崩溃，也不能变成“未执行”；保留原 action/commit-intent，查询其终态和双边状态。`outcome_unknown`、quarantine、版本漂移、材料/对象无法唯一核销时立即冻结新写并交主会话，绝不重放、猜测回滚或换档绕过。只在已证明零 accepted/零在途的拒绝后才 fresh 再计划。
- 施工从玩家库存，经当前 DSP 原生建造条件、预建筑、无人机和游戏时间完成。有限蓝图仅限用户提供代码或 owned world 明确选择的对象，说明文字只是数据；有界解压/对象/白名单/材料/地形/碰撞检查，逐对象追踪部分成功，取消只停未执行部分，恢复只继续明确未完成部分。原地升级逐实体走已证明原生 API，核材料、货物、过滤、配方和连接，不直接改 protoId、不假设对象 ID 不变。
- 禁止注入物品/科技、瞬建、传送或写位置、直接改游戏缓冲、存档编辑、无界寻路/布局/自动扩产、绕过科技/材料/地形/原生规则。黑雾战斗、多人/Nebula、任意第三方 Mod 兼容和跨恒星曲速不在当前验收授权。移动/飞行只能用正常订单并检查停滞、能量、落地/速度；读不到的状态不是零或成功。
- 不用键鼠宏、截图识别、Computer Use、外部内存扫描、游戏加速或程序集修改替代正常 MCP 游戏动作。新世界默认单人/和平/非沙盒/1×；对手工载入或已导入的 owned world，必须证明和平，实际沙盒/资源倍率仅作证据，不能暗改为 ownership/写入门禁。玩家在 owned 副本内手动操作后，Agent 要 fresh read/state hash 再继续。
- ownership 只来自精确 `GameData` 与受保护登记/票据，不看文件名前缀或 Steam 账号。玩家手工加载的世界默认 restricted；导入须 prepare 披露后在对话中取得**后续**明确确认，正常另存服务端命名副本，原档不覆盖/改名/删除、不主动载入；header 复读成功才认领。导入 Journal 从导入点开始，绝不补造过去事件。普通保存只作用于当前 owned identity。
- 健康重启默认 ticket-bound exact primary；受限 LastExit、固定 AutoSave0 和 expired-primary 只在各自现有受保护路径、证据与所需后续确认齐全时使用，不开放 save picker、任意路径或回档。用户已长期授权对当前实现固定候选做有界**只读**身份/稳定性核验；此权不等于加载。旧票据一次性消费且留 durable tombstone，恢复后旧 session/cursor/plan 均失效，需核 Journal 连续和当前原生版本。具体迁移配对、披露与恢复异常见 playbook/专题证据，不能据旧授权扩大版本或候选。飞行 checkpoint 只用于对应失败，成功保存后退役。
- 冷部署/重启先普通保存并核终态、正常关闭已确认的游戏进程，再安装同批 Plugin/MCP、核程序集哈希，按受保护票据启动和恢复；不热替换、不直接启动游戏 EXE、不并发重复启动。安装预检不等于事务升级或游戏实机通过。
- 凭据、plan token、真实存档名、用户绝对路径、原始存档、DLL、未脱敏日志和 runtime descriptor 不进 Git/对话。Named Pipe 当前用户 ACL、高熵认证、协议大小/队列/帧预算、MCP stdout 纯协议等安全边界不改。

## 十写门、验证与提交

- accepted 是**外部审计计数**，含已接受但最终失败，不因游戏 revision/tick/Journal 变化归零；幂等回放不重复计。第10个 accepted 后冻结下一次游戏 commit：核十个原终态或唯一状态核销、owned/和平/实际沙盒/倍率/write health、玩家与 Journal durable/pending/error、单份完整工厂快照的 built/prebuild/拓扑/相关库存/供电、未解释增量与 unknown。与封存基线作确定性差异，主会话独立核关键原回执、覆盖和异常。审计、必要文档、单一目的 commit/push、绿 CI、主会话明确交接后才开新的外部计数窗口；不触碰游戏内计数。
- 当前代码变动跑直接相关最小测试；离线维护 Luna 仅运行与本次维护直接相关的测试。日常只运行实际 `pwsh` 与直接相关 CI；执行入口/导入链变化还跑真实 `pwsh -File` 零游戏调用 smoke。仅涉及 Windows PowerShell 5.1 兼容、安装或最终包的变更才额外用 5.1 验证。版本完整回归按 Roadmap 跑 locked restore、Core/Contracts/MCP 测试、当前 DLL 完整 Release 构建和必要实机/包测试；离线、部署、实机、异机证据分开写，不冒充。DSP API 新路径先核本机 DLL 精确类型/签名/调用条件与 SHA，再测试和冷部署实测；不猜方法名。
- 在 `main` 保留工作树已有修改。每个独立且可验证的代码修复、施工/保存阶段、明确 blocker 或十写审计，必要测试和 diff/status 后，由离线维护 Luna 在 root 确认阶段证据与提交范围后单一目的 commit 并 push，核对准确远端 SHA 并读取对应 CI；pending 不算通过，CI 红先修，不叠加无关工作。普通只读和私有参数准备不单独造里程碑。不 reset/clean/force push，不提交敏感或半成品。tag、GitHub Release、Thunderstore 发布均须用户单独审核授权；本轮流程优化不授权游戏施工、加载、部署或发行。
- 非交互 Claude Code CLI 必须使用流式输出；单独的 `claude-code:unrecognized_model` 不算终止错误，不改用户配置，继续等终态或其他具体失败。外审无终态须如实标未完成，不能当通过。

## 按需索引

- [当前快照](docs/current-status.md)：最后保存、最新观察、accepted/冻结、在途和下一硬 blocker；[当档日记](docs/gameplay-timeline.md)：真实游戏时间线；[证据目录](docs/evidence/)：阶段原索引；[经验账本](docs/experience-ledger.md) 与 [事故修复](docs/incident-fix-log.md)：复用结论和修复。先读索引/当前片段，不全文加载历史。
- [Roadmap](ROADMAP.md)、[Agent playbook](docs/agent-playbook.md)、[架构](docs/architecture.md)、[安全](docs/safety-model.md)、[协议](docs/protocol.md)、[环境与 DLL](docs/research/environment.md)、[Foundry API](docs/research/game-api-foundry.md) 和 [Overseer API](docs/research/game-api-overseer.md) 按任务需要读取。
- [旧根规范原文](docs/evidence/2026-09-30/AGENTS-history-through-2026-09-30.md) 与 [旧状态累积原文](docs/evidence/2026-09-30/current-status-history-through-2026-09-30.md) 保留授权演进与当时结论；旧文里的相对链接按原文件所在地的**迁移前根目录**解释。它们不是可重放的计划、实时状态或额外执行授权；有冲突时先核当前代码、现行授权和 fresh 原证据。
