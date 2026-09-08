# Spherewright 首次问题与代码修复记录

更新时间：2026-09-08（Asia/Singapore）

本文件专门记录项目第一次遇到的可复用工程问题：现场症状、根因、代码或协议
修复、验证证据和仍有限制。它不是逐局流水账，也不是当前规则的唯一来源。
事故在发生存档的日记中仍可保留；修复形成的现行规则以
[experience-ledger.md](./experience-ledger.md) 为准，DSP API 事实以
[`docs/research/`](./research/) 为准。

状态取 `fixed | mitigated | open`。`fixed` 只表示写明范围内已有代码和验证证据，
不代表跨 DSP 版本永久成立。
需明确验证层级时使用子状态`fixed_offline`或`fixed_offline_live_pending`，不能将其读作实机已通过。

## IFX-072 — 临时保存调用受Shell别名与可选配置访问阻断

- 首见：2026-09-08，第三氢消费者接通后的普通保存；状态`mitigated`，仅调用端，不是原生保存或公开接口缺陷。
- 症状/根因：执行子Agent先后报告短函数名R被Invoke-History别名解析、无条件访问非仓储配置触发严格模式。root独立确认当前PowerShell的r别名定义；两次本地失败没有形成accepted保存，不能据此宣称游戏失败或换幂等键重放已成功动作。
- 处理与证据：复用既有Invoke-SpherewrightNormalAction完成唯一21285f48，raw-9bf0ea36/save32115526；root使用有明确前缀的只读函数和既有可选属性helper，raw-24406aec核销11对象前后及当前、全玩家/J56及唯一原始/fresh终态。恢复源选择5项回归通过，无Plugin/MCP改动。
- 后续同类客户端复验：取料包装raw-a82053d4在任何prepare前错误拒绝handcraftQueue=[]，原因是helper经函数管道返回空数组时折叠为null。root改为直接数组属性检查，并删除“两个异步仓inspect必须精确相差取料量”的错误前提，保留同步action守恒、全玩家和源仓结构核销。原DTO、三个非空/缺失反例、无人机忙碌、原始转移预算和自动回补1件的真实后读等15项离线检查通过；新取料实机仍待，不将执行端错误写成游戏故障。
- 实机后置核销：新raw-ce78169c三动作实际成功；末尾root写死revision68再次误报，原始session显示Move推进65→67，两个transfer后69。root移除固定增量假设，raw-b4991782只读逐原始/fresh终态及全玩家/两仓/J56核销通过，accepted4保留；没有重跑已成功前缀。这次仍是私有客户端处理错误，不是游戏回滚、quarantine或公开协议失败。
- 审计函数加载复验：raw-9158af44完成金刚石仓端2711实体/88配置核对后，本地调用的仓库存Sum函数未定义；其定义位于被dot-source脚本的offline提前return之后。补充只读审计把所需纯函数在调用前定义，零仓/混合角色两项测试通过，raw-453668dc重核原始结构、唯一终态及fresh边/玩家/J56，于32584024通过；accepted4/revision89不变，没有重跑c82249c5或新增游戏写。离线分支PASS不能替代对后续所需函数实际加载的验证。
- 限制：本次普通保存成功，不代表临时包装已成为产品能力。后续继续复用已核对调用与DTO读取，避免为每次保存重新发明守卫；仍需实际重启验证支路生产。关联EXP-267、存档日记001。

## IFX-071 — dot-source 的同名参数把实际执行误切为离线验证

- 首见：2026-09-08，两处油路升级的私有固定执行器；状态`fixed`，仅本次入口分派，不是Plugin/MCP错误。
- 根因：外层和被dot-source的备料脚本均声明ValidateOfflineOnly。导入时显式置true污染外层作用域，外层无参数运行也进入离线分支。Luna只看到两条离线PASS、exit0；没有Bridge调用、accepted或action，不能当作升级成功，也不需要回滚或重放材料动作。
- 修复与验证：外层改用独立的ValidateOilUpgradesOfflineOnly；保留共用的已测试断言，不重写原语。AST检查通过；两份原生升级预算正例、预算变更反例及结果ID/互反槽映射回归通过；额外的零Bridge调度探针证明正常模式能越过离线分支。Luna重新授权后raw-702e1744实际完成dacff662/22272557，root raw-d1884e61于31722944独立核销两原始/fresh终态、原位/双端/持货/材料/J56/healthy；dispatch探针本身仍不计入实机。
- 限制：共享作用域导入不能只测试被导入函数，还要测试调用入口。进程exit0不是业务完成证据，仍以独立accepted/action终态和原始读回核销。关联EXP-264、存档日记001。

## IFX-070 — 私有保存守卫把数值零当成一个集合元素

- 首见：2026-09-08，新氢支路保存前检查；状态`fixed`，仅本次私有执行脚本，不是Plugin或MCP故障。
- 根因：临时Count-Value对数值调用集合Count，导致Int64的0变成1。raw-35e3b22e（31488053）和raw-4baba595（31498451）都明确记录working、pendingBuildTargets、pendingRepairTargets为0；两次都在prepare/commit之前停止，accepted保持2。撤销“短暂原生待施工导致拒绝”的解释；第二次真实raw前缀以落盘文件为准，不使用执行器误报的摘要变量。
- 修复与验证：主会话给出唯一固定保存脚本，直接要求三个原生计数是非空整数且等于0；原始DTO通过、三个计数分别设1均拒绝，离线分支没有Bridge调用。Luna原样执行一次后，0b103b86正常保存31518812，revision33/accepted3；root raw-1a6dd894独立核销原始及fresh终态、11对象、全背包、日记和健康恢复票据。
- 边界：没有延长超时、绕过原生检查、重放成功动作或修改公开工具。普通保存成功不等于已验证重启恢复或长期产量；继续使用已核对的执行器，不为每个阶段重写未经测试的DTO守卫。关联EXP-007/261/262、存档日记001。

## IFX-069 — 临时只读摘要猜测DTO字段并混合量纲

- 首见：2026-09-08，2617源分拣器成功后的有限观察；状态`mitigated`，非Plugin故障。
- 根因：临时客户端使用不存在的working而非isWorking，首样本raw-8d6ed94f摘要中断。静态复核还发现state/amount假设、非belt的null cargo解引用，以及可能将power-generation-current-tick混入氢物品汇总；后几项未实际运行到，不伪造第二次实机失败。
- 处理与证据：保留原raw并禁用该私有脚本，不逐字段盲补后重跑过期窗口。改用已多次实测的既有prospective观察器和显式物品/对象选区，重新声明未来窗口。root raw-3f9efae8已独立证明源/桥/尾带和2606有真实氢；源施工唯一成功不受摘要失败影响，accepted1不变。
- 边界：raw-261039e9随后用既有观察器完成四个独立600tick未来窗，未重放游戏动作；只是客户端恢复观察，不是补填原窗口或黄糖稳态通过。不能把发电J/tick当作氢件数，亦不能用氢合计P<C忽略满罐和其他库存。关联EXP-245/261、存档日记001。

## IFX-068 — 外部施工计划只验证带可放、遗漏附件朝向与跨度

- 首见：2026-09-08，氢支路9/3条斜带已建成但2600→183被TooSkew拒绝；状态`mitigated`，格轴替代支路现已供料并正常保存，长期收支仍待。
- 根因：主会话只比较末点到设备槽的连线，漏掉带四向端口本身的旋转和两端反向误差；跨线还忽略双belt同时受5m/3.2格两项限制。不是材料、游戏坏档或需要延长施工超时。
- 修复：保留旧对象和原生约束，按格轴重新作有限计划；固定短移后完整预检，先正常建北端5带并接上2606→183。消费端双向/过滤/满供电已由raw-097b73ad独立核销，但南段/桥/源尚未完成。
- 产品化与离线验证：公共beltPathMode说明及包内/内嵌playbook明确全链端口/跨度检查、真实实体复读、先关键连接后其余路线、offset fallback范围及失败不重放。新增Schema和嵌入资源回归，MCP项目89项通过。当前是源码指导修正；未冷部署新MCP、不新增工具或Plugin写路径、不声称完整供氢或版本门通过。关联EXP-259、存档日记001。
- 后续实机边界：2616/2617完成桥和源连接后，raw-3f9efae8证明新线各段与消费端实际持氢；旧678另经正常动作限定只取氢。正常save31518812及root raw-1a6dd894复核通过。上述早期“未供料”保留为当时截面，不再作为当前状态；满罐、塑料亏缺、保存后重启生产仍不能抵扣。

## IFX-067 — 私有只读几何计算选中整数夹取重载

- 首见：2026-09-08，氢支路替代路线的私有只读推演；状态`fixed`，仅计算层。
- 根因：Math.Min/Max的边界写成整数1/−1，PowerShell重载绑定将余弦小数舍入，三组不同朝向都被误报0°。不是游戏端口变化、原生预检放宽或插件故障。
- 修复与验证：边界明确为1.0/−1.0，并在读取现场前用0°/45°/90°/180°校准。raw-abf8239a的错误只读计算由raw-694f803d/3aaeeeef替代，原EXP-259的浮点计算保持有效；未依据错误角度施工。原生放置通过仍只证明带能放，附件必须在真实实体建成后fresh验证。
- 边界：私有脚本修复，不增加公共工具、不改Plugin，不称氢支路已接通；关联EXP-260、存档日记001。

## IFX-066 — 调用端把有效配置预检误判为回包失败

- 首见：2026-09-08，塑料供油906的有限过滤配置；状态`fixed_offline_live_pending`。
- 根因：调用端先把确认过的inserter.filterItemId=null误当无效（当前reader明确把原生0映射为null），继而要求普通配置plan回显targetObjectId、要求expectedStateHash等于请求哈希。后两次原生prepare均已通过，却被本地新增断言拒绝；没有commit。主会话按两次上限中断，没有让第三次猜字段继续。
- 修复：包内/MCP内嵌playbook明确null与请求0的区别、可选目标字段及服务端计划哈希；保留正确请求hash、prepared/token/commitAllowed/blockers和各mode明示检查。MCP回归用不同服务端hash和缺省target验证现有转发，不改Plugin或放宽原生校验。
- 证据：reader的原生0→null、AddPreparedPlan和MCP配置转发；raw-71780d94的通过预检、raw-da22630d于30782352仍revision51/906无过滤。accepted2、主档30704534/J56保持。塑料持续供油为何停滞仍未证明，不能把调用端修复当作实际送油恢复。
- 验证层级：MCP套件88项通过、零失败，包含新的内嵌指南回归及更新的配置转发正例；仅离线源码验证。关联EXP-257、IFX-001、存档日记001；新指南尚未冷部署。
- 调用端实机闭环：固定执行器5199f19d正常配置、root raw-4ed5c42b独立核销通过，因此仅此调用端故障状态更新为`fixed`；当前四独立窗塑料仍0，不能将供油停滞并入已修复范围。

## IFX-065 — 私有旧连接检查的单元素数组展开假负例

- 首见：2026-09-08，氢分拣器2531正常建成后的私有断言，raw-faa5aac2。
- 状态：`fixed`（仅私有比较/独立核销范围；无Plugin/MCP修改）。
- 根因：if分支输出将只剩一个旧连接的数组展开为PSCustomObject，与另一侧数组序列化比较时误报不同。真实旧连接2530 slot1←2528 slot0未改变，不能据此重放已成功的b0d28a8e。
- 修复与验证：比较入口统一显式数组化。raw-10c531b4用原DTO离线复现原假负例、验证修正正例及篡改邻居ID负例；raw-4981833c独立复读原始/fresh唯一终态、8对象末态、分拣器真实设备槽/双边/过滤/满电、全背包仅2011−1和J56，PASS。
- 边界：仅核销这一施工动作，不把精炼油仍堵塞或黄糖产0说成已修复；EXP-254、IFX-001和存档日记001。

## IFX-064 — 私有预算检查误用PowerShell自动变量

- 首见：2026-09-08，owned-world-001氢发电支路备料，raw-0a46c1bc。
- 状态：`fixed`（仅本次私有执行器范围；不是Plugin/MCP代码变更）。
- 症状与根因：recipe64原生prepare已经通过，私有函数却称铁输入预算错误。函数把$input用作foreach变量，随后Where-Object管道作用域中的自动变量语义导致错误比较；不能据本地异常认定原生材料规则有错或回滚已经成功的铜转移。
- 修复：主会话用记录DTO复现并改用明确字典/非自动变量，逐项验证当前5/6/50/64/85配方、现存中间库存与两批净额；只执行未提交的recipe64×1、recipe85×2。无通用工具、hash放宽、旧token重用或已完成转移重放。
- 验证：AST检查及记录DTO纯函数正例通过，篡改铁10→11负例拒绝；raw-10fc41f5还证明原函数失败、仅变量更名即通过。ab1cc3be/a4ed473c分别于29744839/29745048正常制造完成，root raw-61ef0a00四个唯一终态/全部玩家材料/来源设置/原生燃料/J56核销PASS，accepted8保持。
- 边界与关联：不证明氢支路已建成或黄糖已恢复；EXP-252、IFX-001和存档日记001。

## IFX-063 — 普通带缺料被误导为场地错误

- 首见：2026-09-07，owned-world-001磁铁供料规划，raw-4b21e124；零commit。
- 状态：`fixed_offline_live_pending`。
- 症状与根因：原生全路径检查在临时物品预算耗尽时返回NotEnoughItem，适配器却把全部带拒绝统一归为BUILD_LOCATION_INVALID并提示不要重试该位置。原生缺料先于后续范围/几何检查，既不能叫碰撞，也不能反推位置已通过。
- 修复：新增有界纯错误分类，仅对全部Ok/NotEnoughItem且至少一项缺料、源cover身份匹配、NEW无覆盖/移除的候选返回INVENTORY_INSUFFICIENT；其他错误保持拒绝。MCP/指南明确正常备料后fresh完整复验，旧token、端点和原生规则均不放宽；commit变化仍按stale拒绝。
- 验证边界：新增14 Core/2 MCP回归后Debug/Release各1508项全通过，完整当前DSP Release零警告错误，真实源码MCP64工具/1资源、50349字符指南一致且stdout纯净。两条另路的真实占位732/730保留为负例；取证期间没有重放/施工或游戏写。新错误码未冷部署/live，不视为铜/磁持续供给已修好。
- 关联：EXP-249、存档日记001、game-api-foundry。

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

2026-09-07复现与产品化：四带施工terminal成功，外部脚本仅因post-read仍有1台非空闲无人机而停在下一sorter之前，不能据此重做已完成带。GameStateReader的working只是alive-idle；新增公开player说明与包内playbook的有界只读待机、只续未提交对象、fresh核验原生复用ID规则。root完整178→179实体、逐对象材料/连接和Luna两次唯一terminal均通过；新增2项MCP测试后1457项Debug/Release、完整Release零警告错误及真实MCP resource读回通过。未修改Plugin准入、没有新ZIP；关联EXP-242。

2026-09-07空库存复现：旧27回收f274a9a9已于28106933成功，后置PowerShell直接访问空Measure-Object的Sum而停止，尚未prepare重建。raw-2010d15c证明2012为2→3、铜为0→1，缺少的是before铜项，不是设备丢失。公共ActionClient新增Get-SpherewrightInventoryCount，完整inventory缺项返回0，缺失/null/畸形条目、非整数/负数和聚合越界拒绝；不改prepare/commit/poll/幂等。独立scripts/test-action-client.ps1有21项离线检查且不访问游戏，本次原始库存再次核算通过；Luna随后只续未提交重建e4cae01f，28135338→28135738、2012为3→2，双端和filter1104正确。root raw-10f35253全2351实体/389配置、五写审计通过，无重复回收/重建。既有1488项Release回归复跑通过，21项PowerShell检查单列，不将两者混称新的.NET测试数量。

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

## IFX-025 — 场地预览首稿把所有碰撞体的 ext 当作完整半尺寸

- 首见：2026-09-06，Foundry site preview 的部署前程序集审查；首稿未部署、未引发游戏施工。
- 症状：仅用 `|pos|+|ext|` 包围所有建造碰撞体，会漏掉球体半径和胶囊两端半球；旋转胶囊的 ext 还可能含负分量，不能当作非法 Box 尺寸。
- 根因：`ColliderData.InitFromCollider` 对 Box、Capsule、Sphere 使用不同编码，形状不能只由一组 ext 数值统一解释。
- 修复：以明确 shape 分派 Core helper：Box 用中心偏移加 ext 模长；Capsule 再加独立 radius；Sphere 用中心偏移加 radius。未知形状、非有限值或不合法 Box 半尺寸拒绝，合并主/附加碰撞体最大包络；仍在原生网格吸附后检查计划间净空。
- 验证：Box、Sphere、带负分量的旋转 Capsule 及非法 shape/dimension/radius 回归通过，完整 317 项测试和当前 DSP Release 构建零警告/零错误。此证明是代码/程序集级，不伪称球形或胶囊建筑已实机施工。
- 关联：EXP-188、`FoundrySitePlanner.ColliderBoundingRadius`、`FoundrySitePlannerTests`；状态：`fixed`。

## IFX-026 — 科研 buffer 缺少单位导致把研究点当成矩阵库存

- 首见：2026-09-06，原地升级候选备料/科研只读检查。
- 症状：两个研究站蓝 buffer 各36000被解释为数万个蓝糖，进而推断1202研究供料充足。
- 根因：公共 `FactoryBufferSnapshot.count` 同时承载普通件数和原生 `matrixServed`，没有单位字段；当前 DLL 每矩阵插入3600点，自动进料上限36000实际约十个矩阵等价点。
- 修复：保留原始count/inc以兼容既有读数，新增 `countUnit=research_matrix_points` / `unitsPerItem=3600`，普通 buffer为items/1。包内 playbook要求先看单位，不把部分研究点当可转移库存或完成科技预算。撤销本次错误的“研究材料已充足”结论，未更改游戏研究字段。
- 验证：当前DLL `PlanetFactory` 插入与 `LabComponent` 自动供料阈值交叉证明；契约序列化测试覆盖36000→10等价矩阵而不改变raw count。同批Plugin实机读到84/679研究buffer均points/3600，76/256成品为items/1；真实MCP playbook包含单位规则。初始366项、后续379项测试通过，不冒充科技完成。
- 关联：EXP-191、`GameStateReader.CaptureLab`、`FactoryBufferSnapshot`；状态：`fixed`。

## IFX-027 — 扩充升级白名单不能改变蓝图对象类型判定

- 首见：2026-09-06，基本分拣器升级的部署前交叉审查；含该错误的新增代码未部署。
- 症状：蓝图读取复用了 `BuildingUpgradePolicy.SupportsItem` 来判断制造设备；把2011/2012纳入升级支持后，会误把基本分拣器当作制造台，拒绝合法2–3格跨度并允许不合适的配方字段。
- 根因：可升级对象集合与蓝图二进制形状/参数集合是不同策略，不能互相当类型分类器。
- 修复：蓝图独立明确制造设备2302–2305与分拣器2011–2013；升级仅提供自己的family/pair许可。出口读回另显式比较选择时的过滤器值，不只核对item/recipe。
- 验证：新增三种基本分拣器×三种跨度的9项合法编码回归，以及错误跨度/制造配方4项拒绝回归；完整Release构建零警告错误。该修复防止部署后回退，不声称实机发生过该错误。
- 关联：EXP-189、`BoundedBlueprintReaderTests`、`GameStateReader.Blueprints`；状态：`fixed`。

## IFX-028 — 升级只验证已有边，漏检分拣器缺少必需端点

- 首见：2026-09-06，本机分拣器27的原生升级实测。
- 症状：升级返回成功且即时证明铜块1件、扣料/返还和原有连接保持，但 `verifiedConnectionCount=1`。fresh27仍缓存pick10/insert26，工厂只有27:1↔10:1；仓26没有指回27的边。不能把这一样本写成完整双端验收。
- 根因：预检与读回遍历 `Connections` 并验证每个已存在的边，却没有先要求分拣器必需的输入和输出均存在；检查空/不完整集合会真空通过。原生cached target引用、携货和factory连接池不是同一个证据源。
- 修复：Core先要求恰好slot1输入/pick、slot0输出/insert两条完整正实体连接，Plugin再逐端反向核对；该守卫复用于prepare、commit重验及同步readback。缺端点、重复/额外槽、错误方向/对象、自连接和未解析虚拟slot均拒绝。没有直接修复游戏线路，也没有撤销已证明的27升级。
- 验证：11项新回归、Debug/Release共404测试、完整Release零警告错误。修复版实际对一端23在prepare返回BUILD_CONNECTION_INVALID、无commit；两端749升级于22207214成功，filter1101/verifiedConnectionCount2/扣新1返旧1均明确，同步cargo空。保存22213308、正常退出、protected resume后两端与过滤/供电保持，最终保存22221235/J54/54/healthy。旧27的即时铜1守恒仍为独立历史样本，不伪装成最终版带货双端样本。
- 关联：EXP-028、EXP-190、EXP-193、`BuildingUpgradePolicy.HasCompleteInserterConnections`；状态：`fixed`。

## IFX-029 — 蓝图原生预检并非对覆盖对象无副作用

- 首见：2026-09-06，有限蓝图现场预览的部署前DLL研究；未在实机执行覆盖预检。
- 症状/根因：native paste `CheckBuildConditions` 会在covered belt上临时写入/清除连接；仅在返回后检查cover标志，不能证明读取工具没有改变旧对象。
- 修复：调用前独立排除所有已有entity/prebuild覆盖与内部碰撞，拒绝开放sorter/外部自动匹配；仅对无cover/no-reform新对象使用隔离native预检，显式全部Ok。原生几何与普通逐对象建设分开，不开放整图paste。额外保存/恢复native cursor与inserter提示状态，清理隔离快照/工具。
- 验证：Core图/槽位/碰撞前置与状态机测试，完整Release458项/零警告错误；新路径尚待部署和真实施工，不声称实机旧对象曾被本实现改变。
- 关联：EXP-194/195、`GameStateReader.BlueprintSite`、`NormalGameActionCoordinator.BlueprintSite`、`NativeBuildPreviewUiScope`；状态：`fixed_offline_live_pending`。

## IFX-030 — 有限蓝图预览把分拣器碰撞体过度拉长

- 首见：2026-09-06，458-test开发Plugin的五对象负例预览后复核当前DLL。
- 症状：预览报告对象2/3内部碰撞；对象2同时有独立的native `ErrorInserterData`。两种错误不能合并归因为一项，也没有进行施工。
- 根因：适配器用`span/2 + abs(ext.z) + 1`估计分拣器纵向半尺寸，额外延伸且忽略prefab collider旋转。原生采用`max(.1,span/2+ext.z-.5+.35×带端数量)`，并按两端是否为带调整中心，使用两端朝向up的平均框架与collider quaternion。
- 修复：新增纯Core几何helper并让Plugin遵循当前DLL实际公式；不放宽已存在实体/预建筑守卫，不把任何native错误强制设为Ok。795的slot/端点几何仍需独立复核。
- 验证：九项公式/非法维度回归、完整Release零警告错误、487项总测试通过。修复尚未冷部署，尚无正例模块施工；不能声称live原生几何错误已修复。
- 关联：EXP-197、`NativeInserterColliderGeometry`、`NormalGameActionCoordinator.BlueprintSite`；状态：`fixed_offline_live_pending`。

## IFX-031 — 受保护有限施工记录的空嵌套项会先在哈希处失败

- 首见：2026-09-06，冷部署前的进度存储边界审查；未发生实机记录损坏。
- 根因：校验了连接/预算列表非null，却未检查列表内的null项，计算不可变哈希会先抛NullReferenceException，绕过存储已有的明确数据错误分类。
- 修复：在hash之前检查嵌套项、连接索引/槽位/限额、依赖和参数限额；以InvalidDataException拒绝附件，不恢复权限，也不修补或跳过损坏对象。
- 验证：六项NULL对象/进度/连接/预算、无效端点及参数超限回归；完整Debug/Release510测试通过。没有把自动测试当作损坏存档live恢复。
- 关联：EXP-195、`BlueprintBuildState.Validate`；状态：`fixed_offline`。

## IFX-032 — 客户端审计汇总把逗号列表和范围表达式混用

- 首见：2026-09-06，同档第10次保存前汇总已持久的只读响应。
- 症状：PowerShell报Object[]不能转换为IConvertible，错误光标落在大对象构造的附近字段，让客户端误查entity/采样tick类型；游戏读取已完成，无accepted写丢失或重放。
- 根因：`@(1,2,3,4,5,6,7..41)`的运算符优先级把数组作为范围起点，而非“前六项加7到41”。
- 修复：此处本就是完整1–41证据序号，使用`@(1..41)`；复用已落盘响应，不重新读取整套世界，更不重放动作。涉及分段时需显式括号和数组连接。
- 验证：主会话无游戏的最小表达式原样重现同一异常；替代表达式返回41项、首1末41。客户端汇总成功和最终保存审计另待子Agent回传，不把本地显示错误算成游戏失败。
- 客户端后续：复用已落盘23页factory与其他原始响应完成摘要，随后第10项save终态23140359、J55/55/healthy并完整审计；未重放动作，正常关闭后进程/descriptor清零。
- 关联：EXP-200、十写审计客户端；状态：`fixed`。

## IFX-033 — 有限施工把皮带拾取边误当作皮带出口

- 首见：2026-09-06，首次蓝图施工前的离线原生步骤审查；未发生实机蓝图断边。
- 根因：全图包含belt→belt及belt→sorter边，原preview循环对所有皮带出边写同一个output字段；后出现的虚拟拾取边会覆盖已选下游带，依赖顺序下尚未施工sorter的entityId又为0。
- 修复：Core明确每类对象创建的端点所有权，皮带只选belt→belt出口，分拣器选输入/输出各一条，机器不投影sorter边；多个有效候选明确拒绝。Plugin使用此投影，保留原有全部双向连接和自由端读回，未加旁路字段写入。
- 验证：11项边顺序/单多sorter/自由皮带/机器/双端/歧义/无效索引回归、Debug/Release521项通过，完整Release零警告错误；安装的510-test cohort不含本修复，因此首次blueprint commit继续冻结至正常关闭后的同批更新。
- 关联：EXP-201、`BlueprintSitePolicy.CreationInput/CreationOutput`；状态：`fixed_offline_live_pending`。

## IFX-035 — 普通分拣器绕过了原生候选角度选择

- 首见：2026-09-06，554-test五对象金刚石模块导出成功，但两处site对719/720均报ErrorInserterData，没有blueprint commit。
- 根因：普通建造枚举空闲slot后直接调用CheckBuildConditions；当前原生UI在此前DeterminePreviews中先检查两端面向和相对角度、必要时给TooSkew，后续条件方法不重建这一步。工作中的历史sorter不能因此当作合法蓝图姿态。旧对象具体失败是否同时涉及Refresh位移限制仍待实读，不把推断写成确认。
- 修复：新增无DLL的保守精确直线端点策略（两设备<14度，带端<11度），进入native preview前验证当前实体池身份；拒绝未实现偏移/弯曲子集。site结果增加两端facing dot只读诊断，保留全部原生失败。无既有对象拆除/改位置/改连接，也不直接修整导出的代码。
- 验证：15个几何回归及582 Release测试通过；最终身份守卫/文档更新后的全回归与冷部署待复验。历史719/720诊断和正常合法新分拣器施工仍是独立live门。
- 关联：EXP-205、NativeInserterEndpointGeometry；状态：`fixed_offline_live_pending`。

582-test follow-up：同姿态只读预检的两个output dot为-0.174369559/-0.18947494，input为0.9832634/0.982118845，证实旧输出槽背离另一端；另有965占位。全条件保持拒绝、0蓝图commit。原生合法新sorter施工尚待，不能把诊断成功当成修复实机通过。

## IFX-034 — Governor把正常库存流动误当作产线配置改变

- 首见：2026-09-06，554-test同档恢复后、首个非零基线采样前的代码复核；没有虚构实机三窗失败。
- 根因：adapter使用包含Buffers/InserterStackCount的FactoryConfigurationStateHash绑定选区，每次正常入库或分拣器取放均可能重置基线；空仓零产负例掩盖了这个缺陷。
- 修复：提取纯Core GovernorSourceBinding，仅绑定固定身份、配方、姿态/端点、网络、静态设置。货物、增产点、加工进度和瞬时供电保持独立观测；不会因此忽略供电不足blocker。通用实体新增原生forceAccelerationMode读数，端点hash同时绑定它和sorter filter，以捕获手工配置变化而不受货物波动影响。蓝图导出另核对原生加速参数。
- 验证：13项回归包含实际增长库存的四次600tick采样建立三窗、库存delta仍保留，以及十类真实变化/重复选区/顺序/设置哈希检查；567 Release测试和完整Release零警告错误。安装的554-test不含此修复；Luna已暂停Governor基线/蓝图施工，继续只读仓储验证与必要正常备料，等待正常保存后的冷部署。尚无非零live基线或2×证明。
- 关联：EXP-198/204、`GovernorSourceBinding`、`GameStateReader.Governor`；状态：`fixed_offline_live_pending`。

582-test follow-up：三个独立600tick live窗口全为金刚石30/min，source hash始终相同、唯一生产者归因完整、baseline ready、findings0。配置绑定修复现已`fixed_local_live`，十分钟扩产仍未通过；期间模型汇报造成的真实采样gap会正确重置窗口，改为有界只读采样脚本保留节奏而非放宽gap规则。

## IFX-036 — Governor把尚未归因的全星球矿机告警当成目标链阻断

- 首见：2026-09-06，617冷部署后，金刚石三个30/min非零基线成立但validationBaselineAvailable=false，首次锁定无写入拒绝。
- 根因：observer候选和健康规则要求全局Findings.Count==0；14/263/796的矿竭告警只有各自extractor路径，没有item/选区路径证据，也被强行作为715目标线故障。
- 修复：保留全部Findings，明确TargetChainFindings与UnattributedPlanetFindingCount，只有目标诊断/选中根/选中上游路径才归因；截断仍拒绝。候选、observer与提案哈希同批更新，不靠删告警过关。
- 验证：6项新范围/哈希/健康回归，623 Release测试与完整Release零警告错误；修复仍源码态。另查出115→113真实缺边和石墨亏供，不能把范围修复当作现场修复或十分钟吞吐通过。
- 关联：EXP-207、GovernorPlanCompiler、GovernorThroughputValidation；状态：`fixed_offline_live_pending`。

## IFX-038 — 已识别故障分拣器却没有受控拆除入口

- 日期：2026-09-06；状态：`local_live_basic_sorter_repair_and_resume_passed`。
- 现场：672同档Move `74b3566b-e04c-4d78-952d-797c9291dba6` 于23637299→23638580成功到煤308附近，Walk0/约286.3MJ；115仅有108:4→115:1，113无返回输入边。prepare_dismantle明确拒绝只接受resource-miner，没有commit。
- 根因：早期拆除只为矿机换源开放；缺一端sorter也不能沿用要求完整双端的升级入口。不是延长Move超时或新档能解决的事。
- 修复：复用原生DoDismantleObject，增加普通2011/2012及可回收的stack/cargo状态。允许缺少端点，但存在边和反向引用必须配对，因为原生ClearObjectConn会无条件清对端槽。8192池条目/4邻居上限，拒绝prebuild引用/高级堆叠/活动instantDismantle；核对库存count/inc、目标消失和其他端点配置/连接，不写slot、不自动重建。
- 离线：21项Core策略回归、完整Release通过，MCP复用既有拆除工具。736同批实机拆除115和原生重建均terminal成功，2011为4→5→4、cargo/inc=0；新115的108输入及113:10输出双向完整，邻居原连接和recipe17保留。普通保存23753687及四写审计healthy/J55/55。随后719携带1件金刚石的正常回收、719/720正常重建也成功，货物返还和邻居保留有终态证据，保存23827812及五写审计通过。767同档恢复5381b28d成功并重存23992240，115/719/720实际双端与715配方、716/717仓设置保留；24112905–24113594一写完整审计通过。2012拆除、非零inc货物仍待，不外推为其他类型或连续吞吐已验。
- 修复后独立600tick三窗石墨实际产量18/12/12每分钟，后两窗113仍缺煤；连接已修正不等于产线配平，缺端也未被证明是原亏供唯一根因。

## IFX-039 — 把设备类别与未观测字段误当作消费端不存在

- 首见：2026-09-06，767恢复后的只读诊断。外部Agent看到36个assembler中recipe27为0、下游若干belt的buffers为空，错误建议新建结构矩阵消费端；未因此执行写入。
- 根因：结构矩阵在lab组件中生产，不在assembler列表；GameStateReader只调用已支持设备的CaptureAssembler/Lab/Miner/Storage/Station/Tank/Inserter，未把CargoPath货物写进belt.Buffers。局部类别、未观测字段和未追完的有向路径都不构成“不存在”证据。
- 修复：主会话阻止未经证实的新建建议，要求复读既有Lab774和真实有向下游；MCP三项观察工具描述及包内指南明确lab类别、完整分页、空belt字段的unknown含义和不清仓伪造需求。
- 验证边界：这是读取解释/操作指南修复，不修改游戏货物、拓扑或动作权限，不声称已找到所有末端或恢复持续产量。新增MCP注册/指南契约回归后Release807通过，完整Release零警告错误，源码实际MCP64tools/1resource及25250字符指南一致。767原始bundle24076211已由主会话复核，明确存在774/matrix_lab/6003上游路径，三个目标实际产消均0；128对象的下游追踪仍只是有界前缀，不能补造终点。
- 关联：EXP-214、GameStateReader.TryCaptureFactoryEntity、CaptureLab、MCP观察工具与包内playbook；状态：`guidance_corrected_revalidation_pending`。

- 2026-09-07源码补充：独立beltCargo详情观察以明确局部覆盖/observed或unavailable弥补货物盲点，不改变旧buffers含义，不把未读到当0。分段边界与零ID原生证据、严格界限和测试见EXP-215；未冷部署，不把该补充算成全线路诊断或belt升级已验。

## IFX-037 — 把Move距离终态后的Drift等待和低能量返岸当作安全交接

- 首见：2026-09-06，617-test验收移动fd22bab1已达坐标，但fresh持续Drift；等待稳定耗能，旧指南缺少明确的有界返岸分支。
- 执行偏差：后续14a9af65在fresh已经Walk、核心仅约0.185MJ且无燃料时仍提交返岸。它600tick后因原生能量全空明确失败，没有unknown或重复提交。prepare目标合法不等于足够能量；此错误不归咎为state hash缺陷。
- 修复：指南明确普通业务必须Walk/低速/能源充分；Drift不无限等候，只在能源充足时用已验证Walk锚点做有界正常Move，已自然Walk就不再返岸；只有未accepted stale可最多3次紧邻fresh/prepare，不改变Move/hash/watchdog/幂等/精确订单中止。另为燃料选择公开与既有refuel一致的原生资格，不能只搜索“燃料棒”名称。
- 验证：两次180tick位置停滞和一次600tick断能终态均如实保留。Walk静止735ticks净增980,000能量符合80kW被动补能，不冒充无线塔；正常补给/回充和新指南实机成功仍待。
- 关联：EXP-039/041/053、包内playbook、OrdinaryMechaFuelPolicy；状态：`guidance_corrected_revalidation_pending`。

## IFX-040 — 普通建造缺少初始过滤，事后配置前已发生混料

- 首见：2026-09-07，767安装态供水旁路。本局早期已有空源先过滤经验，此次将其产品化缺口单独登记。
- 症状：2280在24306579建成后默认无过滤，24307575仓2268已有水13/塑料8/油4；至24313451才独立配置filter1000，之后仓仍混有水19/塑料106/油13/氢34。没有清仓或注入，760仍未恢复。
- 根因：事发767安装态的`PrepareBuildRequest`和MCP建造签名都没有初始过滤参数；只有事后`prepare_configure_building`且须等待空载。活跃混料源在这段间隔内照常输送，过去“空源先过滤”经验没有被此次执行遵守。另建两座仓反映端到端布局预检不足，不是原生建造扣料错误。
- 已实现：新增普通2011/2012建造参数 `initialSorterFilterItemId`（默认0），仅接受有界、存在且解锁的过滤物品。复用原生 BuildPreview.filterId→PrebuildData.filterId→正常施工 NewInserterComponent 路径，保留科技/材料/几何/双端/幂等门；prepare 与 commit 的精确计划指纹绑定过滤，原生预建筑和完工 filter/sign 均复读。没有建成后改货物、暂停游戏或自动拆仓。
- 兼容防护：新 MCP 对非零过滤必须收到完全相同的 `plannedSorterFilterItemId`，旧/混装 Plugin 无回显或回显不同时返回 `BRIDGE_NOT_READY`，不暴露旧 plan/token。默认0保持旧请求/响应兼容；直接 Bridge 调用方也须执行该回显检查。包内指南同步要求先过滤、端到端布局和复用已建对象，并保留混带头部堵塞的限制。
- 离线验证：新增25 Core、4 Contracts、7 MCP回归；Debug/Release 共991项（27/920/44）通过，完整Release零警告/错误。真实源码MCP握手保持64 tools/1 resource，资源正文与指南一致，退出及stdout检查通过；不是发行ZIP或跨电脑验收。
- 当前状态：`deployed_initial_filter_build_live_pending`；1018同批冷部署及同档恢复24568281已核对，尚未取得初始过滤建造实机结果，旧混仓和供水仍未修复。原始十写已核销；后续按主会话有界方案执行，不能把安装当修复。关联EXP-065/074/217与存档日记001。

## IFX-042 — 单次腾格会被混料回占，空载过滤限制不能处理已持货的入仓分拣器

- 首见：2026-09-07，1018安装态。主会话一次20油守恒腾格只暂时让水2进入，随后761又被油/塑料占满，三只入仓sorter改持氢；不把这次失败归咎为Luna未执行方案。
- 原因：按需取料与输入容量是两个独立条件，带→仓没有消费者needs。当前源码原本仅允许空载改过滤，无法为被既有货物堵住的入仓sorter限定未来物料。公开storage buffers省略空格，也不能当成可直接编辑的格索引。
- 第一切片：按UIInserterWindow真实三字段路径开放普通2011/2012单向Inserting持货过滤，完整native struct/sign、背包及归一化端点hash立即保全。既有货物仍投原目的端，不清货、不换目标、不重置运动；后续仓格容量动作另行实现。
- 离线验证：1036项Debug/Release和完整Release零警告错误通过；没有新的游戏写入/部署/恢复成功声明。接受写计数6保持，单次修复的4终态与后续600tick零产窗口见日记/EXP-219；修改过滤本身不等于清堵。
- 第二切片：单仓storage-capacity复用原生SetBans/SetFilter/类型切换，私下绑定包含空格的完整顺序、storage组件/链与entity端点。最多100格，明确四类UI操作，既有库存不增减/搬移；超限、无效/无变化、过滤未解锁或stale均拒绝。新增25 Core/2 Contracts/3 MCP后1066项Debug/Release与完整Release通过，仍待冷部署及真正稳定供水，不能忽略2218持续带入氢的上游问题。
- 首次本机正例：同批1066安装及同档恢复后，761在24849780完成bans0→30并保持全部库存/5端点，2218在24855565换成油过滤且保留既有氢1/原目的端和玩家库存。原始terminal及全23页2280built/独立0prebuild/健康状态已主会话核对并十写落盘；实际预约、残货处理、保存保留和持续供水仍未完成。
- 后续预约实测：24936046锁已有格、24939851预约唯一空水格均terminal成功；既有29条非空buffer/5端点/玩家/bans30保持，水count0。五个准入过滤和三次油/氢守恒转移也逐原始核销，整批十写在24946625+完成全厂/健康审计。尾带仍塑料/油/氢混杂，不能用末端单一氢过滤或再增一个仓库宣称永久修复；配置保存恢复、残货去向和真实持续供水仍待验。
- 预约竞争复发及产品指导补全：723锁已有格后，持续入铁占用了最后filter0格，第二步预约铜被storage_configuration_unchanged正确拒绝。不能归因于执行器失败，也不能以未过滤推断为空。主会话复核TakeItem前向取料，撤回未执行的“仅禁尾格”方案；正常暂禁全仓自动输入、取100铁、预约实际空格、恢复原bans四步在28402147–28402300完成，root raw-eb0224fe独立核销终态、数量/inc/6边。MCP工具说明和同批playbook加入前置预留、有限守卫/转移/恢复、部分成功不重放与禁止重复清库制造需求的规则。3 Core与1 MCP新回归后Debug/Release各1492项及完整Release通过，真实MCP64工具/1资源/49550字符指南一致。现有Plugin逻辑未改；这只是有限容量修复和指南可发现性正例，不能代替持续入料、保存恢复或扩产验收。
- 状态：`held_filter_and_storage_bans_locally_verified_recovery_pending`。关联EXP-219/220/221。

## IFX-041 — 多次接线拒绝时缺少端口几何，并把共享带误称为纯水带

- 首见：2026-09-07，767安装态供水修复。四个source→760候选均在当前保守直槽适配器的facing检查返回TooSkew，没有commit；不能据此声称完整原生UI的所有偏移/弯折路径都不合法。
- 根因：Agent根据中心距离选仓/带，而详情缺少空闲槽位的世界位置与朝向；另只追到水仓753就把2219→2243称作纯水，遗漏2219还有2220上游，及经2215进入的油/氢/塑料共享输送。24517132/139/146分别复读2247/2251/2252持塑料/精炼油/氢，且均来自2243，直接否定纯水标签。
- 修复：用户授权连续失败后由主会话接手规划、Luna负责有界执行，保持同一accepted计数与fresh prepare。新增只读detail `sorterEndpoints`，最多16原生槽位或4传送带虚拟姿态；世界坐标/朝向/占用深复制，未知不猜空闲，固定16连接池步长先验界，不改写/扩大建造规则。指南同时纠正共享带归因和直接Bridge传 `itemId=0` 导致空页的调用方错误。
- 验证：25 Core、1 Contracts、1 MCP新增回归，Debug/Release1018项通过、完整Release零警告错误；源码MCP64工具/1资源、正文28415字符一致、stdout/退出检查通过。正常保存24473406与全厂审计另记入日记，仍未接通760。
- 状态：`endpoint_observation_locally_verified_repair_pending`；1018同批冷部署/同档恢复后22对象原始详情已核对，支持对象的槽位/带姿态和不支持对象的unavailable均实测返回。主会话几何计算排除已选现成直连候选，转而按EXP-219核对复用既有输入sorter的按需取货与一格守恒腾位方案。供水仍未修复；不新增第三仓、不清除物品制造产量，不把观察或预检当施工成功。

## IFX-043 — 详情读取字段误名被报告成建筑消失，导致无效诊断扩散

- 首见：2026-09-07，1066安装态有界预约批次第8项之前，Luna把`inspect_factory_entity`的payload写为`entityId=761`，实际契约是`objectId`。未知字段被忽略后ObjectId默认为0，旧读取器统一报`INVALID_ENTITY`/no longer exists；又只查首100实体且尚有nextCursor，用has761=false继续寻找“消失”的原因。
- 主会话处置：从实际请求和当前契约定位字段错误，通知Luna只修正读取字段、沿原accepted计数继续原有计划，不重新执行已accepted的氢转移、不换档、不增建。configure/transfer的entityId属于其它方法，不机械改名。后续第8项原始核销由Luna继续，整批终态/仓格审计另记日记。
- 产品修复：保留owned访问门在前，对读取的0/缺失和Int32.MinValue返回带正确字段/恢复建议的`INVALID_REQUEST`；合法正负ID继续原来的native存在性检查。MCP参数说明和内嵌playbook明确逐方法schema、第一页不足以证明不存在、多次本地字段错误应交主会话处理。
- 验证：9 Core/1 MCP新增回归；Debug/Release1115项（30 Contracts/1035 Core/50 MCP）、完整Release零警告错误通过。真实源码MCP64 tools/1 resource、31150字符指南一致、退出0/stdout纯净。当前游戏仍1066；新错误码分支尚未冷部署/live，不冒充存档或游戏数据修复。
- 状态：`request_validation_offline_verified_caller_corrected`；关联EXP-223。

## IFX-044 — 成功解禁后正常投递，被跨tick库存相等断言误判为失败

2026-09-07普通transfer复现及提示补齐：c16e3f7a在25899642的成功终态证明仓水600→580、玩家0→20；晚20tick仓581令Luna本地跨tick断言误停，未重放。root核原始响应后交回执行第二段479a0b57，终态仓0→20/player20→0；晚12tick仓19正确单列为自然取料。原transfer DTO已有即时beforeTargetAmount/afterTargetAmount/itemDeltas，问题是外部脚本未正确使用且工具/指南对此不明确。新增MCP描述、包内说明与直接Bridge的storageEntityId更正，3项回归、1327项Debug/Release及完整Release和实际源码MCP指南握手通过。未修改原生扣料、状态hash或幂等；无新Plugin安装/最终包声明。EXP-224持续复核，不重复创建首次事故编号。

- 首见：2026-09-07，1066安装态761的set-bans0在25011091成功，稍后库存氢1→4、原三只held氢正常入仓，Luna的外部断言却要求库存不变。
- 根因：Plugin已在原生配置调用内证明逐格保全，但公开terminal没有固定该瞬间的结构化配置/库存；调用者把后续fresh inspect当成同一时刻。已接受动作本身没有失败。
- 处置：主会话逐raw核对terminal、设置/连接/玩家和三只原持货去向后交回Luna继续，不重放、不把本地异常变成隔离或换档。
- 产品修复：可选storageConfigurationReadback记录即时tick、操作、前后设置和独立非空buffer；只在现有native不变量全部通过后生成，Core再次验证实际格投影。不新增写入口，不改state hash/幂等/unknown；指南和MCP描述明确跨tick差异须另核物流。
- 验证：1132项Debug/Release（31 Contracts/1050 Core/51 MCP）、完整Release零警告错误、源码MCP64 tools/1 resource/32057字符指南一致/stdout纯净；新增字段未冷部署/live。旧set-bans动作的真实投递证据不冒充新字段实机通过。
- 后续实机：1144同批冷部署后，775 set-bans30动作b017ef0f在25142861成功，新DTO的同步前后设置、各30条金刚石100/inc0和3条连接均由主会话核销，玩家/外部连接保持。这是新字段的set-bans本机正例，不补造旧unban字段或外推其他操作。
- 状态：`instant_readback_offline_and_local_set_bans_verified`；关联EXP-224。

## IFX-045 — 错误哈希域被反复解释成现场陈旧

- 首见：2026-09-07，1066安装态2280的两次sorter-filter prepare均使用before.stateHash，被旧通用检查报STALE_STATE；没有commit，不能计为写入失败或消耗accepted额度。
- 根因：同一expectedFactoryStateHash参数在不同配置模式使用不同域，外部调用者机械沿用了普通仓配置的完整hash。现有工具长描述虽说明配置hash，参数位置未直说区别，错误也没有可操作的定位。
- 处置：主会话核对真实请求代码，纠正为根configurationStateHash，再交回Luna继续同一有界计划；后续动作在25042120正常完成，未扩散候选、重做已接受动作或换档。
- 产品修复：在原sorter配置检查前区分可确证的当前full-hash错误域（非重试INVALID_REQUEST）与无法确证来源的不匹配（保留STALE_STATE/fresh read）。缺失/空配置证据不通过；ordinal比较不折叠大小写。MCP参数和包内playbook明确sorter-filter/configurationStateHash与storage-capacity/stateHash，不修改原哈希及commit语义。
- 验证：11 Core/1 MCP新回归，1144项Debug/Release（31 Contracts/1061 Core/52 MCP）、完整Release零警告错误；源码MCP64 tools/1 resource、32678字符指南一致、exit0且无额外stdout。当前游戏仍1066，新诊断分支尚未live验证。
- 后续实机：1144冷部署后，对2280的一次fresh完整stateHash负向prepare实际返回INVALID_REQUEST/retryable=false及正确域恢复提示；没有commit/action，不计accepted写入。
- 状态：`hash_domain_diagnostic_offline_and_local_rejection_verified`；关联EXP-225。

## IFX-046 — 只枚举传送带中心，无法使用原生带段接点微调

- 首见：2026-09-07，主会话接手Luna重复拒绝的供水方案。753/761附近现有原生槽角度不满足旧直线子集；移近后外侧20段带可放，但这不构成两端连接批准。
- 根因：旧普通分拣器prepare没有复用DeterminePreviews的单belt插值分支，只有实体中心四向姿态；不能以持续修改业务路线掩盖这一适配限制。另发现private build fingerprint遗漏了InputOffset/OutputOffset，当前旧子集都为0，但开放偏移前必须补齐。
- 修复：在原执行器内增加一个明确belt与2101仓/assembler/lab的有界几何候选，最多64区间；不调用含UI/输入副作用的完整方法，不扫描邻带。保留11/24.1度和原生全预检，静态几何及offset参与绑定，提交前重查，原生预建筑/实体双位姿、offset、身份、过滤和双边连接核验。
- 验证：1201项Debug/Release、完整Release零警告错误、源码MCP64 tools/1 resource/33408字符指南一致通过；55项Core与Contracts/MCP各1新增回归。尚未冷部署或取得新适配实机正例。
- 安装态复验：0c59dab的1201同批DLL已冷部署，protected resume及J55/55连续性通过；753→2219、2246→761、2277→761、753→2232仍被原生接点准备拒绝，没有commit。不能把安装成功或这些负例当作接线修复正例；尚须更明确的失败阶段证据来指导有界选址。
- 状态：`bounded_native_attachment_offline_verified_live_positive_pending`；关联EXP-226。持续供水、模块复制、Governor和最终包门未减少。

## IFX-047 — 接线失败未区分几何捕获、候选搜索与原生检查阶段

- 首见：2026-09-07，1201冷部署后主会话四个有界接线预检仍拒绝，没有commit。
- 根因：普通prepare把最后一个精确槽的TooSkew和通用fallback失败串联输出，未报告几何是否取得、是否存在有效投影、最接近角度或候选校验是否执行；恢复说明一律建议移近或换位置，易诱导无效重试。
- 修复：复用原候选和纯角度公式，补充固定捕获reason、实际阶段计数、最小有效偏差及明确unknown。最后精确槽结果单独标注；candidateChecks不声称原生放置检查次数。只对明确OutOfReach建议移近，暂时buffer busy只允许等待后一次fresh prepare；不更改原生阈值、计划/状态hash、材料/施工或commit语义。
- 验证：1217项Debug/Release（32/1131/54）、完整Release零警告错误，源码MCP64 tools/1 resource及同批指南/stdout通过；当前1201安装态不热替换，新阶段live尚待。单元测试不作为供水恢复或偏移施工成功证明。
- 本机后续：1217同批冷部署/同档恢复及33详情保持通过；四个明确接点分别返回两次无有效投影和59.768°/19.745°两个超界角度，geometry捕获均成功、候选校验和commit均0。新诊断没有放宽原生拒绝，也没有把未测角度填0。主会话停止重复候选，转向完整供料方案。
- 状态：`rejection_stage_diagnostics_offline_and_local_verified`；关联EXP-227。新接线施工与持续供给未通过。

## IFX-048 — 普通续带把既有锚点作为新对象再次建造

2026-09-07保存恢复补验：正常save25827878→正常关闭→同档protected resume/重存25827909，root全2283对象/47详情证明源2269、新链和原邻边/朝向、玩家43条带保持，未重复扣料或建造。该2001三段案例状态为`source_cover_local_constructed_saved_resumed`；其他带等级、目标合流和生产吞吐不在此正例范围。

2026-09-07本机续接正例：1324冷部署后#8 e1574d6f由正常无人机完成2269→2283→2281→2282，2001仅46→43，源2269保留、原邻边保持、新尾端无连接。root完整2283对象/47详情审计通过；旧2269朝向由IFX-050精确原生证明接纳，未造同点NEW、未拆旧实体、未隔离。状态更新为`source_cover_local_constructed_save_resume_pending`；仍不能外推目标合流/换级/闭环或持续供料，保存恢复另验。下列源码截面保留历史语义。

2026-09-07后续切片：1258安装态已证明完整stage1自由路径可prepare、754同点NEW拒绝且玩家/端点保持。1309源码加入正确的源端non-removing cover，旧端点留在原生预检/创建、预算只算NEW；完整旧路径即时货物和原邻边保全、同帧源→预建筑及无人机后源→实体双向验证、唯一源中心守卫和MCP兼容echo齐备。仅支持同级水平开放路径的自由出口向空地续接，不开放目标合流/换级/抬高/闭环；51项新增回归、1309全量Debug/Release及完整Release通过。状态为`source_cover_offline_verified_live_pending`；后续仍须冷部署、材料/货物/连接/保存恢复正例。下文是初次遏制时的历史截面，不能覆盖这一窄范围实现或把它当作实机已验。

- 首见：2026-09-07复核供水路径和EXP-149，主会话接手规划；本次未新增belt/blueprint施工。
- 根因：TryCreateBeltSteps为全部SnapLine点生成NEW BuildStepPlan，首末点强制等于旧端点坐标；CreatePreview保留cover0。原生DeterminePreviews实际上将旧belt作为non-removing cover，CreatePrebuilds不另扣料而复用ID。旧完工归属/有向连接证明不能替代缺少的原生preview形成步骤，EXP-149的“原生转接层”解释撤回，实测物流事实保留。
- 最小遏制：完整本地实体/预建筑扫描和纯Core有界同中心检查，无source例外；原生预检前/后、commit重验和施工前都检查。稳定reason/点索引/有符号对象ID指导外部Agent停止同点重试，MCP与指南明确既有belt锚点续接暂不支持。旧实体不自动拆除、归档不迁移，不用传送/注入或改玩家cmd绕过。
- 验证：1250项Debug/Release（32Contracts/1163Core/55MCP）、完整Release零警告错误，源码MCP64tools/1resource/35210字符指南、正常exit0及纯stdout通过。32个新Core案例含源/末/中间/预建筑、相邻格、精确距离边界、自交、坏输入/限额、陈旧现场和坐标调整后重查；1MCP发现性回归。新防护暂未安装/live。
- 未完成：完整native cover复用、原生cmd.stage依赖的无玩家副作用预检、成功正常续接和材料/拓扑/货物保存恢复回归。不能把暂时拒绝风险路径冒充完整修复或0.4通过。
- 状态：`new_belt_overlap_containment_offline_verified_live_pending`；关联EXP-228（替代EXP-149施工解释）。

## IFX-049 — 原生路径检查借用了玩家的锚点预览阶段

后续独立完成阶段风险见IFX-050；它不撤销本条完整stage1无玩家命令副作用的预检证据。

2026-09-07本机补验：1258同档受保护恢复与完整审计通过；20个新点的自由路径完整stage1 prepare成功，754旧源NEW重叠明确拒绝，均无commit，玩家hash和源endpoint保持、无新增prebuild或BepInEx异常。阶段预检的局部live已证明，正常施工和cover复用仍未验；下文offline状态保留初始切片语义。

- 首见：2026-09-07，主会话继续复核IFX-048的原生路径前置条件。
- 根因：_Init绑定真实controller，CheckBuildConditions在cmd.stage0跳过后续弯折/坡度/接入检查；旧适配未提供独立阶段且只填源startObjectId，未填目标castObjectId。原生CreatePrebuilds还可修改阶段，不能把真实玩家命令当临时工具状态。
- 修复：工具自己创建inactive/disabled的合法Unity组件容器，独立stage1命令；仅重绑定私有未注册BuildTool，检查真实玩家命令值不变、两端ID完整，finally恢复工具绑定/预览UI和释放自己创建的host。拒绝改玩家cmd、new/clone MonoBehaviour、跳过原生角度或拆除旧实体。原生普通材料/预建筑/无人机/终态语义保持。
- 验证：1258项Debug/Release（32Contracts/1170Core/56MCP），完整Release零警告错误；真实源码MCP64tools/1resource/35797字符指南、exit0且无额外stdout。7项纯策略和1项MCP回归不证明Unity lifecycle或游戏正例；当前DSP/Unity调用证据见game-api-foundry。
- 未完成：冷部署后只读正/负预检、正常自由路径施工回归、原生旧belt cover复用及其材料/货物/连接/保存恢复证明。该修复不是接线/产线验收完成。
- 状态：`full_native_path_stage_offline_verified_live_pending`；关联EXP-229/IFX-048。

## IFX-050 — 源带复用的完工验证把原生朝向重算误当成身份变化

2026-09-07保存恢复补验：第10写同档恢复后，2269的新原生朝向与全部2283保存前姿态/连接一致，正常源带续接的材料/配置/拓扑持久性通过；状态更新为`native_rotation_local_constructed_saved_resumed`。仍不据单个2001案例宣称所有带等级或持续产量完成。

2026-09-07本机补验：1324同档恢复后首次3NEW续接成功，2269的rotation确有变化，其他2279个旧建筑rotation保持；当前精确path/collider证明、旧身份/位置/邻边与材料证明全部通过，未触发隔离。root原始终态与完整工厂复读相互支持，不再仅是离线推测；保存恢复仍待。状态更新为`native_rotation_local_constructed_save_resume_pending`，不是产线或版本完成。

- 首见：2026-09-07，在1309源码冷部署/source-cover prepare正例后、首个施工commit前的DLL审计发现；尚未以实机动作触发此失败。
- 根因：原生AlterBeltConnections/AlterBeltRenderer会在无人机接入后重算旧源/邻带entity.rot和collider.q/pos。1309的原邻域哈希包含旧旋转，可能误拒正常原生结果；不能通过随意忽略旋转来修复。
- 修复：1324源码分开严格prepare/即时路径货物绑定与completion topology；后者仅允许通过有界当前native分段公式、明确碰撞体身份、精确entity/collider旋转与中心证明的belt朝向变动，非belt、位置、identity、旧边保持原约束。只认同一native计算或q/-q等价，不使用宽松角度容差，不写任何姿态。新增sourcePreservationMode确认，旧cover echo不暴露token。
- 验证：15项新增Core/MCP回归、1324项Release与完整Release零警告错误；当前DLL签名/公式见game-api-foundry。未冷部署、无cover施工成功/失败及保存恢复实机声明。
- 状态：`fixed_offline_live_pending`；关联EXP-231/230、IFX-048。

## IFX-051 — 本地PowerShell大帧被逐字节展开，完整审计游标到期

- 首见：2026-09-07，恢复#7和成功续带#8后的两个只读完整分页run分别在60秒边界返回STALE_CURSOR；游戏写已经成功，无重放或拼页。
- 可确证的客户端缺陷：Read-SpherewrightExactBytes直接return byte[]会被PowerShell展开成Object[]，增加大帧装箱/转换开销，Count0则无返回对象。未将所有耗时归因于该一行，也未把审计失败说成建造失败。
- 修复：一元逗号保留单个byte[]对象；认证、帧长度、完整读取循环、EOF失败、Plugin页上限/TTL均不变。本地审计按单日志、无额外分页等待读取，不改变游戏速度或数据。
- 验证：旧实现类型测试先失败；修复后9项独立离线断言通过，包含中文/256KiB往返与非法/截断帧。root新23页2283对象同snapshot在9784ms读取完成，47详情和原始#8终态/材料/拓扑独立审计通过，0新游戏写。不宣称任何宿主上分页永不到期或MCP性能通用倍数。
- 状态：`client_frame_shape_fixed_offline_and_local_verified`；关联EXP-232。

## IFX-052 — 原生球面带路径未暴露，外部方案只能得到固定网格折线

- 首见：2026-09-07，主会话在反复接线拒绝后检查外侧753→761候选及当前DLL；不是一次新的失败commit。
- 确认的限制：旧TryCreateBeltSteps固定path1/geodesic=false，而native UI支持球面直线。固定折线不能表达部分建筑朝向走廊，但尚未证明这是所有拒绝的原因，不能预先声称新模式已修复供水。
- 修复：只开放明确两端空地、普通2001/2002/2003、贴地1.5–30m原生geodesic；模式/几何绑定、MCP回显拒绝旧Plugin静默降级、十点保留空间/完整端点/原生调整后地表检查。默认grid哈希和正常成本/施工/幂等/终态不变，不混cover/合流/抬高/自动搜索。
- 验证：43项新增回归后1370 Debug/Release与完整Release零警告错误；实际源码MCP64tools/1resource/40458字符指南和stdout通过。当前1324游戏正常保存26074522并完成root三写/完整2283结构审计，尚待冷部署与有界现场正负例/施工/持续供给验证。
- 后续实机：1370同批正常冷部署、同档resume11bac49a重存26074553；753/761外侧15NEW方案原生stage1通过，增加source绑定负例INVALID_REQUEST/无plan。两次前后player/endpoint hash和pre0/rev1保持；还没有施工或两端接线/持续供水证据。
- 施工复验：885dff01在26134213→26140140完成15NEW、2001仅43→28；root完整2298实体/62详情证明旧2283结构和47设置保持，新15链与空端符合计划。两端2011水过滤预检均通过，尚未实际接线、持续供水或验证新链保存恢复。
- 供料复验：随后2299/2300以初始水过滤正常建成并满电；root完整2300对象核验材料和双端。四个独立600tick前瞻窗中水产消均非零、有机晶体/钛晶石加工，仓769在详情样本间105→110；正常保存26239001后全量复核通过。窗口有小空隙，不冒充Governor连续十分钟；黄糖仍堵塞，新链跨恢复另验。
- 状态：`native_geodesic_local_water_supply_saved_resume_pending`；关联EXP-233。

## IFX-053 — 恢复前等待已加载世界，混淆菜单前置与恢复后证明

- 首见：2026-09-07，1370冷启动后 Luna 在菜单空等90秒；没有 prepare/commit，没有消费票据。该等待错误不是游戏故障或存档损坏。
- 根因：外部流程错误要求 `gameLoaded=true` 才准备恢复；公开描述没有区分前后阶段，并残留已被实现替换的 LastExit-first/fallback 说明。
- 修复：session 与恢复工具描述、包内 playbook 明确菜单 `gameLoaded=false` 正常，票据可用不等于 native ready；由 fresh prepare 检查 preload/菜单/无loader/header/Journal。终态后才要求已加载且 owned/saved/healthy。健康重启精确 primary 与隔离 LastExit 分开说明，不改变实际恢复算法或放宽检查。
- 实机证据：root fresh prepare 通过，Luna 新计划恢复11bac49a成功并重存26074553；完整2283实体/47详情、Journal55/55与库存保持，没有重复恢复、新档或任意选档。
- 自动验证：两个新MCP测试覆盖未加载菜单可调用prepare及指南的前后阶段/源选择说明，指导文本断言先失败后通过。1372项Debug/Release与完整Release零警告错误；源码64工具/1资源、41250字符指南一致且stdout纯净。此为既有实机原因纠正和新文案离线证明，不冒充再次实机恢复。
- 状态：`resume_guidance_fixed_offline_with_existing_local_resume_evidence`；关联EXP-234。

## IFX-054 — 无建筑碰撞的接近方案仍越过水面，Move预检缺少地表风险证据

- 首见：2026-09-07，新窗Move343bbf15在26375077按距离完成后仍Drift；主会话路线距已知非带/分拣器中心有17m以上净空，却不代表陆地。晚起观察消耗了能源，不得冒充紧随动作的落地窗口。
- 根因：已有Move预检没有公开地表证据；外部规划把几何净空当成可步行。原生Move终态本来只承诺距离，watchdog并非全局地表规划器，不能以延长超时修复。
- 修复：当前原生双向下射线依据落实为可选surfacePreview，32m/33点/66次硬上限；Core只解释已复制的命中、距离及高度，无自动选路/位置写入。丢失证据仍unknown，采样未发现风险不声称路径无障碍；MCP描述、resource playbook、协议/用户文档同步。恢复准备前再fresh确认，若自然Walk则不提交多余回退。
- 离线验证：完整Release零警告错误，Debug/Release1405测试，源码64tools/1resource及42052字符指南实际握手一致。补齐PhysicsModule本地编译引用且Private=false，Plugin输出仍仅原四DLL，无游戏程序集进入产物。
- 实机边界：0956f6b四DLL正常冷部署/同档恢复后，实际MCP对旧水面/直接铁仓短目标给出6/11风险点，40m为unavailable/unknown；四向短目标N/W有风险、E/S未发现。root指定东北30°/24m候选，Luna fresh prepare后唯一Move55c910d9在26575313完成，26575339 Walk0/398.353MJ，后续取铁在77.214m成功。root完整十写审计保持2299结构/71设置/J55/55/healthy。未宣称全局寻路、所有无风险目标必达、永久供给或0.4版本完成。
- 状态：`bounded_surface_preview_locally_validated`；关联EXP-236。最终ZIP与异机仍待，旧水面Move不是新代码验证。

## IFX-055 — 开发客户端把MCP参数形状直接传给Bridge

- 首见：2026-09-07，Luna准备固定24mMove时向Bridge传入targetX/Y/Z，未形成DTO的target对象；原生表面坐标检查以INVALID_REQUEST拒绝，raw-91446c19无accepted或commit。
- 根因：MCP公开参数与Bridge内部DTO并非同一形状；这是开发客户端错误，不是新路线再次卡住。
- 修复与验证：root提供精确target={x,y,z}及原有hash绑定；Luna fresh预检后55c910d9唯一Move成功，终态和Walk读回齐备。只纠正调用方，没有更改模型/游戏状态准入、重放已接受的动作或改动公共MCP工具。关联EXP-237。
- 状态：`caller_corrected_and_locally_verified`；通用参数诊断产品化不在此条声称完成。

## IFX-056 — 递归根因越过物流库存，丢掉未发货的因果边界

- 首见：2026-09-07，raw-183242d2的26654934同tick诊断将母星缺钛递归到远端矿机满50/50；它证明局部满缓冲，未展示供应站库存或证明未发货原因。
- 根因：Plugin已复制匹配供给总库存，但Core对material_shortage不区分中间有/无库存，直接采用更深finding并丢弃当前material evidence；合成有库存200/1的回归证明会错误跨越这道边界。
- 修复：保留消费者confirmed material_shortage和已有物流证据，以stocked_logistics_boundary停止递归，并明确供给总库存非可分配量、dispatch unproven。不把它改成confirmed logistics_blocked，不改变空/未知源追踪、正常运单/进展、无fleet及600tick stall规则，也不增加游戏读取/写入。
- 验证：新增11 Core+2 MCP后1418项Debug/Release和完整Release通过，真实源码MCP指南与64工具/1资源一致、stdout纯净。随后1420同批正常冷部署/同档恢复通过；Bridge26890053及实际源码MCP26949464在完整3工厂/600tick窗口中，对1106/1118/6003保留缺料、聚合stock8、stocked_logistics_boundary及unproven。公开MCP前后player hash/revision不变、exit0/额外stdout0；不能把测试中的200写成live库存，或把8称为精确可分配量。
- 状态：`fixed_local_live`（诊断停止边界，不是钛供给修复、新ZIP或异机验证）；关联EXP-238。

## IFX-057 — 背包-only审计漏掉已存在的机甲科研库存

- 首见：2026-09-07，root核销11带施工时背包比前组多77蓝/40黄，先停止新写，没有放宽守恒。
- 根因：跨tick比较只看inventory，忽略早已公开的mechaResearchItemBuffer；旧缓存277200/144000点均为整件，原生研究结束可按3600点/件取回。首次背包记录26656859早于建带prepare26694984，动作本身仅扣11带。
- 修复：私有审计逐项核对旧缓存/新背包/建带前已存在数量和队列完成证据；公开工具描述及包内playbook提示这条既有读取路径，不开放任意增量或改hash。精确退回tick/调用未捕获，仅记录数量守恒和原生机制/科技时序一致。
- 验证：2新MCP测试先失败后通过，1420项Debug/Release、完整Release和实际64工具/1资源指南通过；root完整2310实体/82详情审计通过，计数保持1。没有新增游戏写入、ZIP或异机验收。
- 状态：`mitigated`；关联EXP-239。

## IFX-059 — 分段物品汇总不能定位物流入站的阻塞货包

- 首见：2026-09-07，owned-world-001当前102，站44硅满、钛仅8，110带同时观察到硅/钛。
- 根因：原beltCargo只给item-sorted分段聚合，不能确定原生TryPickItemAtRear正在检查哪个对齐包；直接下“硅挡钛”的已确认结论会越过公开证据。
- 修复：既有inspect增加可选rearPickup，open path尾段才观察对齐货包；no_aligned_packet/not_applicable/unavailable分开，复用有界副本、完整格式与原生只读核对。MCP描述、协议和包内playbook要求fresh入站拓扑/needs/容量及重复证据，不自动改槽、清库存或宣称持续修复。
- 验证：28 Core/5 Contracts/2 MCP新增，1455项Debug/Release、完整Release零警告错误、真实源码64工具/1资源/45564字符指南通过。新适配尚未冷部署；实际堵塞物品与供应修复都未宣称通过。
- 实机：1455冷部署后源码MCP三次110尾包为硅1、44硅槽满/needs仅钛且双向端点保持；102中段给not_applicable/null item，player hash/revision与stdout正常。跨度184tick，未冒充长期吞吐；root同档恢复175实体/28配置/库存/Journal审计通过。
- 状态：`fixed_local_live`（当前只读可观测性，不是产线修复、全场景或异机验证）；关联EXP-241。

## IFX-058 — 主会话几何方案先建整条带却未证明两个连接

- 首见：2026-09-07，11条油候选带正常建成，下游no_finite_projection；root复核上游也无<11°配对。Luna按既定边界停在prepare，没有重复commit。
- 根因：沿用先前水线成功的方式，将带路全NEW占位/原生放置正例错误外推为足以施工的端到端业务方案；真实两仓朝向与路径不同。原生5.5m和3.499跨格上限还独立于角度/投影，单纯延长不能保证修复。
- 当前处理：完整保留并审计11带/材料，记录ID复用和两端失败；不再提交北向/南向试铺、不放宽原生角度、不拆旧线。下一方案必须重新证明业务端点，仍只通过既有fresh prepare/commit执行。
- 状态：`open`，未连接油候选不是供油产线；关联EXP-240。

## IFX-060 — 短暂Walk提前取消自有上岸订单，随后误报订单丢失

- 首见：2026-09-07，939a954a同星系返航在27513071为明确recovery_required；此前短暂Walk/2.67m/s，随后实际Drift。
- 根因：Walk着陆分支无条件AbortPlayerOrderIfOwned，而当前DLL PlayerOrder.Abort只Dequeue、不标记原订单targetReached。保留的未到达action.PlayerOrder在下一Drift帧被当成外部提前清除。原raw与静态路径一致，但没有逐帧引用调用trace。
- 修复：纯Core订单门将未到达、被替换或缺失的shore order先送回原有运动/能源/停滞/争用核销路径；仅无当前订单或已到达的精确自有订单进入稳定Walk验证。仍保留600tick稳定、7200tick总界、3次有界上岸、exact-order abort及无传送/注入。
- 验证：14项Core及1项MCP新回归，1472项Debug/Release、完整当前DSP Release零警告错误、真实64工具/1资源/46812字符指南通过；未冷部署/live。失败后的a50e84b6精确checkpoint恢复及root全179对象/28详情/物资/Journal55审计通过，保存的分流修复保留，无目的地保存或盲重放。
- 状态：`fixed_offline_live_pending`；关联EXP-243和存档日记001。
- 后续窄live正例：a157a0b冷部署/同档primary恢复后，532d165b于27593800→27599682正常返航并通过600稳定tick；全2310结构/387非belt新基线/材料/Journal十写审计通过。原27507953因更新primary被startup retire，不是相同checkpoint复测；中途optional局部read遇NO_LOCAL_PLANET后只有终态补证，尚无故障shore分支的连续trace，故针对性live pending不撤销。

## IFX-061 — 发电详情把每tick能量标为燃料件数

- 首见：2026-09-07，规划氢消耗时183火电公开buffer为item1120/count18686/items，raw-15665ee0/tick27697702；不是18686件氢。
- 根因：CapturePower将generateCurrentTick写入FactoryBufferSnapshot.Count，却继承默认items/1；curFuelId只描述燃料类型。Governor按默认单位汇总时也可能把发电量和真实物资相加。
- 修复：共享纯Core语义显式标joules_per_tick/unitsPerItem0，并在Governor当前库存和历史窗口库存统一排除发电role（包括旧版误标items）。保留原计数的int饱和投影和原生字段读取，不新增游戏写入、不改普通物品/研究点或action/hash协议。
- 验证：当前DLL SHA/PowerSystem.GameTick→GenEnergyByFuel已复核；15Core/1MCP新增，1488项Debug/Release、完整当前DSP Release零警告错误、真实源码64工具/1资源/48248字符指南、stdout纯净通过。fa29180同批正常冷部署和protected resume27804980后，raw-4ffacceb真实MCP读风电1/火电183为joules_per_tick/0，普通仓28保持items/1；玩家hash/revision保持、进程exit0。root raw-a7bf4900全2310/387配置与库存/日记恢复对照通过，accepted7不归零。这不是最终包或燃料生产验收。
- 状态：`fixed_local_live`；关联EXP-245、存档日记001。

## IFX-062 — 本地PowerShell几何辅助脚本把夹角误算为零

- 首见：2026-09-07，入口施工后只读比较三个既有取料点，raw-19ce719e把不同朝向都汇总为0°；未依据该结果commit。
- 根因：Math.Min/Max传入整数-1/1，PowerShell选中整数重载，余弦发生舍入。此处只影响私有规划helper；C#原生端点规则未改且正确拒绝1583→2311。
- 修复：边界显式-1.0/1.0、浮点累加，游戏读取前增加60°已知向量自检，并修正另一只读路径helper的同类clamp。
- 验证：raw-9f49ba0c三对端点得25.959°/1.081°/44.205°；raw-80bc4404独立原生prepare接受1582→2311，1583拒绝保留。没有放宽产品阈值、重放失败commit或执行额外Move。
- 状态：`fixed_local_helper`；关联EXP-248、存档日记001。辅助结果仍不能代替原生准入。
