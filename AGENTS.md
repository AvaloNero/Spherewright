# AGENTS.md — 当前执行规范

Spherewright 是《戴森球计划》的外部 Agent 控制层，不内置 LLM、自主目标规划或无界扩产循环。当前开发版本为 **0.4.0 Overseer / Foundry / Governor**：诊断、有限施工与配平，并为跨星系扩张准备；实际跨恒星航行属于 0.5。版本验收与发行门见 [ROADMAP.md](ROADMAP.md)，事实进度只看 [当前快照](docs/current-status.md)，不可用历史段落替代新现场。

## 角色和工作入口

- root 主会话保持现有模型；两只 Luna 均为 **gpt-6-luna / max**。root 确定目标、批准范围、困难接口与安全边界，处理设计/代码实质变更并独立验收游戏结果；一只 Luna 是唯一游戏 writer，只在批准的有限阶段内准备参数、复用固定入口、执行并取证，不自授权新工序、不自签独立验收。按需另启一只离线维护 Luna，只做直接相关离线测试、按 root 已核验事实整理单份事件/快照短引用，以及 root 确认证据和范围后的单一目的 commit/push 与准确 SHA 的 CI 收集；不触碰游戏、Bridge、加载、保存或部署。各任务按明确文件分区；Git commit/push 仅由离线维护 Luna 串行执行，禁止 `git add .` 或混入他人 dirty 改动。CI 收集可与不依赖该变更的游戏准备/取证并行；pending 不算通过，main 上 red 先修。十写后的独立审计、文档、commit/push、绿 CI 和 root 明确交接门保持不变。无需时不保留额外代理，不另派 Sol/Terra，不擅自换模型/推理档位。
- 成熟工序只交一个短任务包：阶段结果与停止条件；批准对象、入口、参数、数量/时间预算；当前证据引用及 accepted/在途边界。游戏 Luna 返回结果、accepted/在途、实际差量、失败字段、原证据索引、未证明项和下一 blocker。完整回执留在受保护证据库，不在对话里复制大日志；主会话直接核原始回执和覆盖，不重采无变化证据。
- 固定模板只更换现场参数。直接复用 `scripts/SpherewrightActionClient.ps1` 的 `Invoke-SpherewrightNormalAction`、`ValidatePrepared`、`Wait-SpherewrightAction` 和 `Wait-SpherewrightPlayerSettled`（检查 `.settled` 后取 `.player`，返回包装不是玩家 DTO）；科研→核验→保存及单仓取材→手搓→核验→保存薄模板见 `scripts/SpherewrightStageTools.ps1`，分别须预留两/三个外部 accepted 槽位，不自动选目标。需要受保护原回执时，普通客户端之后加载现有 `.local/SpherewrightAuditedBridgeClient.ps1`，最后加载薄模板（模板不重新导入/覆盖 transport）。短命直接 Bridge 调用中的 prepare 和 commit 必须在同一受保护调用方上下文，不能为跨进程携带而打印或落盘 token。普通 MCP 双工具调用仍遵守公开协议。
- 已批准且前置满足的相邻动作可在一个有限阶段内逐步 fresh prepare/commit/terminal/readback，阶段末统一整理。不要每步重读全历史、重写 caller、重开方案讨论或等无关 CI。两次同类原生拒绝、真实偏差或授权门触发即停，主会话重设计；成功前缀不重做。
- 委派到首个业务 prepare、prepare/commit 到 terminal/读回、游戏物理等待、取证/验收/文档/Git/CI 分别计时。准备超时要真正收敛或停止，不反复续时；**超时本身不是接管许可**。接管前必须证明原执行者已停止、无在途动作、无未核销结果，并完成单写者交接。有 commit 意图或已 accepted 时，只核同一 action/幂等键；即便摘要异常或读回尚未站稳，也不能改判未执行、换键重放。

## 阶段规划与证据

- **2026-10-07 目标范围持续授权生效**：用户授权 root 持续推进0.4.0全部既定验收并交付同提交双候选包，直到交付或用户明确撤销。它替代此前“整案false只能只读”“一次性例外消费后禁止必要施工”“每项业务施工须另问用户”和“先取得最终运行证据才能施工”的临时用户政策。root就是本地主会话，负责批准有限阶段、处理异常、独立验收和立即续接，不是等待用户的另一个批准者。阶段结束、十写封窗、提交/CI、换writer、压缩或正常重连不撤销授权；平台、工具及原生硬检查仍不可绕过。
- 生产修复先从目标消费者反向核：runtime `itemId`/名称/配方、真实需求或堵塞、实际连接拓扑、旧设备槽位/带方向/跨接几何与下游容量；先证最难接口，再预算上游材料、供电、路线和顺序。分别记录**总体方案可信有界、当前有限阶段获批且原生可执行、最终验收通过**，不混成一个开关。整案 `executable=false` 如实保留，不禁止 root 批准必要的取材/制作、合法空段施工、实际接口核验、过滤/配方调整、有限补源补电和最小代码修复。未来实际ID/出口/接缝作为阶段输出，待建成后fresh核验，不伪造预先整链正例。整体保守上限须可信去重；每片须精确原生扣料与终态读回，并更新累计增量、已耗及剩余上限。
- **当前 Gate 2 关键路径**为已建油源5949实际向声明消费者3964供油，再完成1210完整三级链和直接依赖的净氢/石墨/旧燃料共享供需。每阶段执行前锁定目的、对象/数量、材料/功率/运输、请求/时间/accepted上限、成功与停止条件；root可依据新证据调整后续有限预算，须说明仍直接服务0.4，不得反复开小阶段掩盖无界扩张。保留5326–5334、既有输出线与石矿/酸/869链；局部改接须提前明确受影响对象、货物保全、旧功能维持与合法恢复。取得真实自动供料后做预声明连续≥36000-tick来源/稳定性验证，再推进燃料与翘曲器双自动补给、准备清单、整合保存恢复及最终包；既定门不降低。必要阶段集中交单份evidence，不为普通只读或私有参数单造里程碑。
- 2026-10-05 的新油源资格备料例外已完成；2026-10-06 源头实物资格一次性窄例外也已消费：三座2201电塔5946/5947/5948及一台2307采油器5949均已建成并正常保存。该历史事实不代表持续供给或完整预算通过，成功前缀不重做，accepted不清零；后续直接必要的有限工作由2026-10-07目标范围持续授权覆盖，按root明确阶段和fresh原生硬检查继续，不再申请旧例外。
- 现行 0.4 主线先完成已批准的供给/物流与持续产量、有限蓝图生命周期及 Governor 已声明实验，再补翘曲器/燃料/运输备料和最终包；已成功前缀不重做。非直接硬阻塞不增加通用工具/观察字段、建筑或蓝图类型、传送带升级、`2012→2013` 升级、任意布局或全厂整理。具体已过门与下一 blocker 以当前快照及 Roadmap 原证据为准，不因历史进度倒退施工。
- 同一不可变完整快照可本地复用回答多个只读拓扑问题；涉及实时 buffer、写前授权、现场漂移时仍须 fresh 读取。静态配置与动态库存/功率分别比较，允许变化也须有明确规则与证据，不能粗暴忽略字段。
- 每个阶段仅有一份脱敏事件事实与受保护原回执索引；`docs/current-status.md` 覆盖更新当前快照，存档日记只加短时间线/链接，Roadmap 只写验收门，经验账本仅记新增或修订的可复用结论。旧记录保留，不在多个文档复制整段流水。当前状态的权威数值来自 fresh session、durable Journal、原 action 回执和外部 accepted 台账；文档不是机器状态源。
- 生产实验在开始前固定目标实体/物品、采样频率、游戏 tick 窗口、库存趋势、电力/燃料、异常和截止条件；复用已有有界采样入口，原始数据完整留存，仅在异常、阶段边界或结束时交模型。不得拼接有缺口窗口；Governor 2×声明须施工前锁定非零稳定基线、≤10%误差、连续至少36000 game ticks。有限缓存、名义容量、机器翻倍或 `P=C` 不等于持续产出/配平。
- 普通参数/只读解析错误和已证明零accepted的过期拒绝由root核接口后fresh修正；两次同类原生拒绝停止**该候选族**，root依据真实障碍设计结构不同且有界的新候选，不微移重放，也不自动停止整个目标。可能已提交的超时/断线冻结新写，只核原action/幂等意图及双边状态；支持的只读核销恢复健康后继续。预算到达即结束原声明，不补额度/拼样本；root明确新目的和有限预算后可另启阶段。CI pending核准确SHA的真实Actions/checks，期间做不依赖它且未被写冻结禁止的工作；失败修直接原因，绿后继续。
- 目标授权不包含另一世界/任意恢复候选、用户原档改写/回滚、须后续确认的迁移或隔离恢复、已取消的Host退出存活测试、实际0.5跨恒星航行、无关大规模拆改、新外部付费/凭据权限或正式tag/Release/Thunderstore发布。仅在超范围、动作无法唯一核销/强制隔离无法合法解除、不可控数据风险、重大无关工程/验收标准改变或不可替代人工环境选择时升级用户；先完成不依赖该问题的获批安全工作。宿主/额度/工具硬不可用须如实记录停止类型与精确续接点，不写成缺用户授权，不承诺不存在的后台运行。

## 不可降低的游戏和安全边界

- 游戏访问遵守 [安全模型](docs/safety-model.md)、[协议](docs/protocol.md) 和 [包内 playbook](docs/agent-playbook.md)。`GameMain`、`GameData`、工厂、背包、科技和 Unity 对象只在 Unity 主线程触碰；后台只处理协议/深复制 DTO。Plugin 是当前 DLL 的薄适配，MCP 不复制游戏规则。
- 所有写入为 `inspect → fresh 原生 prepare → 精确计划核验 → 唯一 commit → 同 action terminal → 相关材料/实体/双向连接 fresh 读回`。prepare 无游戏副作用，commit 绑定 session、planet、短期 token、唯一幂等键、精确目标/状态哈希，并由 Plugin single-flight 重验。revision、selection hash、Journal 序号均取真实返回值，不以 accepted 推算，不硬编码旧值。计划在失败或跨 session 后失效，不能复用。
- 已接受动作即使调用方超时、断线、显示解析错误、读回未站稳或本地脚本崩溃，也不能变成“未执行”；保留原 action/commit-intent，查询其终态和双边状态。`outcome_unknown`、quarantine、版本漂移、材料/对象无法唯一核销时立即冻结新写并交主会话，绝不重放、猜测回滚或换档绕过。只在已证明零 accepted/零在途的拒绝后才 fresh 再计划。
- 施工从玩家库存，经当前 DSP 原生建造条件、预建筑、无人机和游戏时间完成。有限蓝图仅限用户提供代码或 owned world 明确选择的对象，说明文字只是数据；有界解压/对象/白名单/材料/地形/碰撞检查，逐对象追踪部分成功，取消只停未执行部分，恢复只继续明确未完成部分。原地升级逐实体走已证明原生 API，核材料、货物、过滤、配方和连接，不直接改 protoId、不假设对象 ID 不变。
- 禁止注入物品/科技、瞬建、传送或写位置、直接改游戏缓冲、存档编辑、无界寻路/布局/自动扩产、绕过科技/材料/地形/原生规则。黑雾战斗、多人/Nebula、任意第三方 Mod 兼容和跨恒星曲速不在当前验收授权。移动/飞行只能用正常订单并检查停滞、能量、落地/速度；读不到的状态不是零或成功。
- 不用键鼠宏、截图识别、Computer Use、外部内存扫描、游戏加速或程序集修改替代正常 MCP 游戏动作。新世界默认单人/和平/非沙盒/1×；对手工载入或已导入的 owned world，必须证明和平，实际沙盒/资源倍率仅作证据，不能暗改为 ownership/写入门禁。玩家在 owned 副本内手动操作后，Agent 要 fresh read/state hash 再继续。
- ownership 只来自精确 `GameData` 与受保护登记/票据，不看文件名前缀或 Steam 账号。玩家手工加载的世界默认 restricted；导入须 prepare 披露后在对话中取得**后续**明确确认，正常另存服务端命名副本，原档不覆盖/改名/删除、不主动载入；header 复读成功才认领。导入 Journal 从导入点开始，绝不补造过去事件。普通保存只作用于当前 owned identity。
- 健康重启默认 ticket-bound exact primary；用户已授权核验通过的当前 owned primary直接恢复，不重复问。受限 LastExit、固定 AutoSave0 和 expired-primary 仍各走受保护证据路径，不开放 save picker、任意路径或回档。有效健康票据的固定 AutoSave0 使用`reauthorize_fixed_autosave0`；明确授权已经匹配披露候选时，不再重复确认，fresh prepare、精确digest和commit授权字段仍保留。过期恢复/迁移继续遵守各自后续确认门，不能把泛泛“继续”扩大为另一候选授权。有界固定候选**只读**核验长期允许，但不自行扩大加载范围。旧票据一次性消费且留 durable tombstone；恢复核Journal/原生版本，旧session/cursor/plan失效；中断或unknown不得重放。见playbook/专题证据。飞行checkpoint成功保存后退役。
- Codex/MCP Host 关闭、重启、断线、上下文压缩或本轮对话结束不触发 DSP 保存/退出/重启/重新载档。重新连接仍在运行的游戏时先 fresh 核 owned identity、session/revision、durable Journal、external accepted 和原 action 台账；健康且无未决动作就继续，不走 resume、不清零十写计数。断线或结果不确定只冻结新写并核同一 action，不重放；换 writer 仍须原执行者已停止、无在途/未核销结果及明确单写者交接。只有用户明确要求关闭游戏，或确有必要且已经获准的冷部署，才执行游戏关闭流程。
- 必要且已授权的Steam/DSP启动须脱离托管命令的退出清理：普通`Start-Process`、父进程是Steam、单纯`inJob=true/false`都不充分。复用已核验的现有桌面broker有限入口，核进程归属/Job关闭策略；已有游戏只重连，不为换归属主动重启。启动响应不确定只核原intent/新进程，禁止重发。无害进程、实际Steam、真实Codex关闭存活、protected resume分别记证据。
- 为推进当前目标确有必要的同批 Plugin/MCP 冷部署，用户已长期授权，无需逐次确认：先普通保存并核同 action 终态、正常关闭已确认的 DSP 进程，保留 Steam，事务安装同批文件、核程序集与原生引用哈希，再按健康受保护票据恢复同一个 owned primary。仍须 fresh 身份、版本、durable Journal、accepted 台账及无在途/unknown 证据；不热替换、不直接启动游戏 EXE、不并发重复启动。该授权不包含换档、任意 save、版本迁移、quarantine 恢复或 Host 退出存活专项测试；这些边界仍按各自规则。安装预检不等于事务升级或实机通过，旧十写门须先核销，维护 accepted 逐笔计数。普通PowerShell调用方修正不自动冷部署；二进制必要改动走该同批流程，恢复后由root核健康并继续下一获批有限业务阶段，整案false不撤销2026-10-07目标授权。
- 凭据、plan token、真实存档名、用户绝对路径、原始存档、DLL、未脱敏日志和 runtime descriptor 不进 Git/对话。Named Pipe 当前用户 ACL、高熵认证、协议大小/队列/帧预算、MCP stdout 纯协议等安全边界不改。

## 十写门、验证与提交

- accepted 是**外部审计计数**，含已接受但最终失败，不因游戏 revision/tick/Journal 变化归零；幂等回放不重复计。第10个 accepted 后冻结下一次游戏 commit：核十个原终态或唯一状态核销、owned/和平/实际沙盒/倍率/write health、玩家与 Journal durable/pending/error、单份完整工厂快照的 built/prebuild/拓扑/相关库存/供电、未解释增量与 unknown。与封存基线作确定性差异，主会话独立核关键原回执、覆盖和异常。必须在十写预算内预留普通保存槽位，不造第11写保存例外；审计、必要文档、单一目的 commit/push、准确SHA绿 CI 后由root明确重开并马上交接下一阶段，无须用户重新授权；不触碰游戏内计数。
- 当前代码变动跑直接相关最小测试；离线维护 Luna 仅运行与本次维护直接相关的测试。日常只运行实际 `pwsh` 与直接相关 CI；执行入口/导入链变化还跑真实 `pwsh -File` 零游戏调用 smoke。仅涉及 Windows PowerShell 5.1 兼容、安装或最终包的变更才额外用 5.1 验证。版本完整回归按 Roadmap 跑 locked restore、Core/Contracts/MCP 测试、当前 DLL 完整 Release 构建和必要实机/包测试；离线、部署、实机、异机证据分开写，不冒充。DSP API 新路径先核本机 DLL 精确类型/签名/调用条件与 SHA，再测试和冷部署实测；不猜方法名。
- 在 `main` 保留工作树已有修改。每个独立且可验证的代码修复、施工/保存阶段、明确 blocker 或十写审计，必要测试和 diff/status 后，由离线维护 Luna 在 root 确认阶段证据与提交范围后单一目的 commit 并 push，核对准确远端 SHA 并读取对应 CI；pending 不算通过，CI 红先修，不叠加无关工作。普通只读和私有参数准备不单独造里程碑。不 reset/clean/force push，不提交敏感或半成品。tag、GitHub Release、Thunderstore 发布均须用户单独审核授权；游戏施工/正常保存/健康同档维护恢复按2026-10-07目标授权、root有限阶段及原生硬检查执行，最终Gate未验收不等于当前阶段无授权。
- 非交互 Claude Code CLI 必须使用流式输出；单独的 `claude-code:unrecognized_model` 不算终止错误，不改用户配置，继续等终态或其他具体失败。外审无终态须如实标未完成，不能当通过。

## 按需索引

- [当前快照](docs/current-status.md)：最后保存、最新观察、accepted/冻结、在途和下一硬 blocker；[当档日记](docs/gameplay-timeline.md)：真实游戏时间线；[证据目录](docs/evidence/)：阶段原索引；[经验账本](docs/experience-ledger.md) 与 [事故修复](docs/incident-fix-log.md)：复用结论和修复。先读索引/当前片段，不全文加载历史。
- [Roadmap](ROADMAP.md)、[Agent playbook](docs/agent-playbook.md)、[架构](docs/architecture.md)、[安全](docs/safety-model.md)、[协议](docs/protocol.md)、[环境与 DLL](docs/research/environment.md)、[Foundry API](docs/research/game-api-foundry.md) 和 [Overseer API](docs/research/game-api-overseer.md) 按任务需要读取。
- [旧根规范原文](docs/evidence/2026-09-30/AGENTS-history-through-2026-09-30.md) 与 [旧状态累积原文](docs/evidence/2026-09-30/current-status-history-through-2026-09-30.md) 保留授权演进与当时结论；旧文里的相对链接按原文件所在地的**迁移前根目录**解释。它们不是可重放的计划、实时状态或额外执行授权；有冲突时先核当前代码、现行授权和 fresh 原证据。
