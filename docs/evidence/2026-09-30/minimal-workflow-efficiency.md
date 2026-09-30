# 最小流程优化：离线证据与边界

日期：2026-09-30（Asia/Singapore）。起始源码 `5fa0a2c`，工作树干净；不新增施工、加载、部署或发行。只保留 root 与既有 gpt-6-luna/max，后者已停止：新增 accepted=0、无在途/未核销动作、无活动命令；原写窗口仍为4/10。

后续分工：root 保持现有模型，负责目标、代码设计、安全和独立游戏验收；两只 Luna 均为 gpt-6-luna/max，一只为唯一游戏 writer，按界限执行 prepare/commit/terminal/readback/取证；按需另启离线维护 Luna，仅处理直接相关测试、根据 root 已核验事实整理单份事件/快照短引用，并在 root 确认证据与范围后按文件分区串行 commit/push、收集准确 SHA 对应 CI。十写门、不重放、独立验收与完整原始证据要求不变。

## 核查：复用与未解决项

- `5fa0a2c` 已将根规则74141→12359 bytes、当前状态50918→4504 bytes（本轮开始值）；旧原文有归档，不再次迁移。规则/快照/历史已分离；共享 caller 已有 exact `ValidatePrepared`、唯一 commit、同 action 轮询、站稳复读和阶段计时。
- [科研1607](technology-1607-selection-save-caller-delay.md) 记录过旧 hash 字段、错误显示 J91 的调用方延迟；[短移](coal-short-move-revision-readback.md) 记录过固定 revision 的误报。当前普通 caller 不硬编码这些值，但历史私有采样入口仍绑定旧 session/revision/J 和多层导入，不能仅改目标物品便安全复用。
- 明确选定的只读 run `58687b5685714196b502be2ac9afb170/0001–0020` 有重复边界读取，三窗实验未开始，无 prepare/commit。原回执0004–0006证明 session tick80053207/R22、player tick80053776、Journal tick80053780/J95 durable/no pending/error；正常保存仍80012591。物品1205/1802分别为超级磁场环/氘核燃料棒，不能用1209或1121代替产出身份。以上只用于核查调用流程，不宣称产量通过。

## 第一批：成熟科研阶段与不确定结果

- 一个薄模板 `Invoke-SpherewrightResearchAndSave`，导入本身零请求；普通 caller、既有 protected transport、模板依次加载，模板不覆盖 transport。不选择自主目标，不硬编码身份、revision 增量或 J 序号；最多两次原生动作，第二步失败不重放第一步。只返回差量/计时/未证明项，无 token；不签独立验收、不取得单写者租约。
- caller 检查返回 action 身份，保留终态失败结构字段；已接受、显式拒绝、响应不确定分别留元数据。不增加请求/重试；超时、失败摘要与读回异常都不能变成“未执行”。
- 证据索引复用显式 runId 读取，新增 unmatched commit-intent 与 unclassified response，防止响应丢失或内部错误被当作零 accepted。未知仍阻止新写，完整原回执/快照/独立核验仍必须保留。UTC时间直接按datetimeoffset计算，避免PowerShell7自动日期解析后转字符串丢毫秒。
- 离线结果：pwsh 与 Windows PowerShell均通过 action78、stage15、index4检查；三者均零游戏调用。科研成功fixture14请求、唯一两次commit，实际revision2→7→11/J91→95也通过；丢失保存响应、后读异常、九写预算均不重放。新入口实际游戏仍待验证，未生成提速/token节省比例。
- 只读重算原写run `8418a1e5bef240b5a436314ec877842a` 的62个commit-intent/commit/action回执：4个独立accepted、全部终态、无待核销。旧日期转字符串给出0/48000/48000/0ms，修复后为63.665/48020.091/47926.094/90.786ms；是同一证据的计时精度修复，不是游戏动作加速或物理耗时分解。
- 非交互Claude外审启用流式、禁工具、单轮。只得到thinking事件，未取得实质终态，在工作期限内停止；不算外审通过，不因`unrecognized_model`事件重试或更改配置。主会话按原回执与离线负例独立核验本批。

第一批提交 `37ba48d` 已推送，CI [36670565864](https://github.com/AvaloNero/Spherewright/actions/runs/36670565864)通过。它不代表安装或实机新入口已验证。

## 第二/三批：同一证据复用、反向阶段与有限实验

- 不另造任务状态源、planner或协议。现有playbook固定短任务包/差量返回；新供应阶段必须先标清最难消费者接口的runtime身份、需求、实际拓扑和原生验证状态，再批准上游；未来对象端口仍是“待现场验证”，不囤token。既有Foundry/Governor和独立验收规则不改。
- `SpherewrightFactoryEvidence.ps1` 从明确提供的单快照完整页及封存count构建纯离线投影；复用既有静态配置分类，动态buffer/能量单列，反向边检查保留。未知字段/混页/缺页/缺字段拒绝；允许的矿点变化也需精确before/after+原回执引用，实际差异不丢弃。跨session的owned identity须另核，投影不是完整十写审计或fresh preflight。
- 选定原十写run `8a060d67b80944bf8315f1ff8871e969` 的52页/5194对象/tick79995323离线适配测试：旧快照与自身比对，static0、dynamic0、非互返边0，约18704.89ms；不采新现场、不重新宣布旧审计，也不把自比对当新实机通过。
- `SpherewrightProductionSampling.ps1` 只是原有600-tick native生产读取的有限调度：固定≤32对象/≤8物品/时限/请求与样本预算/条件，整个窗在开始tick之后；原transport留全回执，样本只存引用及派生量。独立窗不得重叠；连续模式缺口/未合格状态重置信用，不能少于36000ticks。无逐样本模型决策，无写入，不自动续时/重开；实验结束或异常才交模型。已阻塞的legacy读请求不能由此取消，不能启动第二执行者。不是源归因、Governor声明/2×、连续健康或保存恢复验收替代品。
- 复用`gh`一次读取准确40位提交的CI状态/链接；没有run仍为unknown，status不是独立签字。caller新增原action的`observedExecutionGameTicks`，与轮询wall time分开；缺原生tick字段则null，不由墙钟换算。

## 科研入口的原生预算样例修正

回到0.4科研准备时，主会话核对`NormalGameActionCoordinator.PrepareSelectResearchOnMainThread`：`itemBudget`包含未来研究消耗，direction为`research-consumption`，不是空的即时背包交易。原stage测试的空预算样例不符合该返回，照抄会制造调用方假阻塞；模板本身未假定空预算。本次只修样例并在模板旁说明语义，改用已核原生2104的蓝/红/黄/紫矩阵各500预算；缺预算、错方向、错数量、重复物品均在commit前拒绝。普通保存仍要求空预算。`pwsh -File scripts/test-stage-tools.ps1` **26检查通过、零游戏调用**，成功fixture仍14请求/两次唯一commit，已有不确定结果与不重放负例保留。没有改Plugin、权限、协议或部署，也未据此声称科技解锁。日常仅实际PowerShell7与相关CI；5.1兼容/安装/最终包改动才另做双版本验证。

## 可重复离线比较与未验证项

| 同输入fixture | 改造前 | 改造后 | 边界 |
|---|---:|---:|---|
| 3次相同64对象快照问题 | 构建6次投影 | 构建2次投影，结果完全相同 | 已warm-up；pwsh约1203.885→691.984ms，Windows PowerShell约3583.313→1254.399ms；局部运行有波动，不是总工序/游戏加速 |
| 3个相同独立600-tick窗 | 3次入口、18请求 | 1次入口、14请求 | 原窗完全一致；保留每窗health及最终session边界，无游戏调用，不是假装3次真实模型调用 |
| 连续原生窗fixture | — | 60窗覆盖36000ticks、242只读请求 | gap/power不合格重置，期限/预算/版本/未知页拒绝；不能据此标实机Governor通过 |

最终规则/当前状态字节数：本轮开始12359/4504，结束12500/4693（包含必需入口引用与较新只读边界；不再加入历史）。前一轮74141/50918的压缩已存在，本轮不重复计作收益。没有合适tokenizer/provider usage的可比样本；root+Luna缓存输入/未缓存输入/输出及总调用、token、成本节省均未知，未编造比例。阶段派发时间可由真实`DispatchedAtUtc`传入，否则dispatch→首prepare为null；取证、独立验收、文档与Git/CI总耗时尚无成对实机样本。

最终直接回归：两种PowerShell的action79、stage17、index4、factory17、sampling18（135检查）及包面策略37通过，全部离线/零游戏调用；不声称完整DLL构建、ZIP验证或live验收。新脚本也以真实`-File`入口跑过，未发现descriptor/pipe或触碰游戏。

保持单写者核销、精确prepare/commit、原结果不重放、十写冻结、完整原始证据、独立验收和提交授权不变。没有安装/加载/施工或新生产实机验收；本轮有限只读实验与其失败边界如下。剩余高价值项仅：新模板与采样的同类实机前后比较、in-flight legacy transport读的有限取消能力、更多成熟材料工序仅换参数复用现caller（不由本次扩大实现）。至此停止本次优化，不推进游戏目标或发行。

## 有限采样的实际边界与慢速回归

第二批提交 `91200e9` 已推送，CI [36674478783](https://github.com/AvaloNero/Spherewright/actions/runs/36674478783)通过。随后在既有运行中owned世界执行一次有限只读实验；不加载、部署、移动、保存或施工，新增accepted0，仍4/10。Luna的原命令句柄已经exit1/结束，无在途写；不自动重开。主会话直接读取同一run原回执，未重新采集现场。

- 原run `47f2c7af593f440e8fa95cf997385905`：0001–0005为起始session/player/Journal/catalog/progression，0006起始边界，0007预声明3个独立600-tick窗、10实体/8物品、180秒/90请求。0008–0098中有90个sampler请求（0059为派生事件，非请求），全run共95个只读请求，无prepare/commit。
- **0059保留一个完整样本**，原读回0046–0058，窗口tick80141063–80141662，600ticks、ready、无session跨界。全星球石墨72/min、超级磁场环0、氘核燃料棒12/min、红糖6/min、黄糖0；仅为一个planet生产窗口，**不证明指定新矿/炉来源、持续入料、黄糖恢复或Governor通过**。第二次采集仅有部分实体读回，不能补算完整窗口；“stdout没列窗口”不等于无原始样本。
- 最后原session0092：tick80142282/R22/healthy，正常保存仍80012591；初始Journal0003为tick80141009/J95 durable/no pending/error。最后0098实体读回之后无最终session/Journal核销，不据此宣称当前fresh。0007→0098原记录时间相差76513.3455ms；没有用游戏tick推算物理等待或把它当实际进程总耗时。
- 根据**实际90个sampler请求**及未到180秒deadline确定请求预算耗尽，而非原生拒绝或生产链失败。每秒一次session轮询与约15tick/s的实际推进不匹配，既有瞬时fixture没覆盖这个情况。失败前读取的完整原窗口保留；本地脚本异常不触发新worker、扩大预算、重新提交或清零accepted。
- 最小修复：默认poll5秒，并在intent记录；预算、deadline、最多3窗均不增加。初始读/后续异常均保留`failureKind`、实际请求数、已完成样本数、未证明结果与原异常；失败取证本身出错也不能吞掉原失败或新发请求。continuous模式仍按实际game窗口判断，缺口/健康失败照常重置；5秒不是适用于任意游戏速度的连续验收保证，必须预声明适用节奏。
- 同输入离线15tick/s、10实体、3窗、180秒/90请求：poll1秒在90请求后失败并保留1窗；poll5秒完成3窗，67请求、模拟墙钟130秒。**130秒是fixture虚拟时钟，不是实机提速测量**，完整任务的前后实际耗时/模型调用/token变化仍未知。修复后的真实游戏实验未重开，待验证。
- 追加回归：两种PowerShell的action79、stage17、index4、factory17、sampling27（144检查）与包面策略37均通过，零游戏调用；采样新增慢速、初始身份/隔离、严格请求上限、保留部分证据、失败取证再失败、不完整item覆盖不提前计数、无写入/重试。此前独立/连续36000tick/gap/power覆盖不减少。真实`pwsh -File`导入仍零请求；不冒充DLL/ZIP/实机改后验收。再次仅对本修复diff做单轮禁工具流式Claude外审，仍未取得实质终态，限定期限内停止；不算外审通过、不重试或改配置，主会话直接核diff与回归。
