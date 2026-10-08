# 流程开销收敛与可配置写窗

起始源码 `cf481ca`，`main`，开始时工作树干净。此次只改外部脚本、离线fixture和指南；不施工、不加载/保存/退出游戏、不部署、不改Plugin/MCP协议或发布。root设计及独立复核，一只gpt-6-luna/max负责分区离线测试和经批准的Git交付。当前游戏窗口不会因此扩容、归零或解除冻结。

## 有界核查结论

- 已有普通caller、两条薄阶段、显式runId action索引、不可变工厂投影、有限生产实验和真实Journal边界均直接复用，不另建执行器/状态源/监控平台。科研成功fixture仍14次RPC、两次唯一commit；安全步骤未减少。
- [R303原证据](departure-ils-ten-r303.md)：窗口只有取材、Walk、PLS/ILS手搓和save，没有改工厂拓扑，却又核29种全厂物料、147条物流路径、16台燃料机。这些是供需验收，不必绑定每次封窗。受影响库存、材料守恒、完整静态快照、身份、save及独立核销仍必需。
- R299把J固定为100造成假blocker，J101/J102其实是合法首次产物记录；R301仅核原save而未重放。共享`Get-SpherewrightDurableJournalBoundary`已正确验证真实连续durable序号，本次不重做实现，只禁止再写私有固定J断言。
- 上一轮执行记录统计中，一次全目录`action-*.json`解析耗6858.77秒。成熟动作不应再用这种无界历史搜索；保留run索引完整覆盖，再按确切文件读取关键原件。

## 本次实际变化

1. `SpherewrightStageTools.ps1`三个入口支持`AuditWindowLimit=10/20/50`；省略仍10，研究/save预留2槽，材料/制作/save预留3槽，容量不足在任何请求前拒绝。结果报告声明上限与实际accepted，失败仍保留原action/accepted前缀及禁止重放。参数不是授权，也不允许改已有窗口上限。
2. `Read-SpherewrightEvidenceRecord`只读索引指定的一个标准原文件，8MiB上限、身份/UTF8/JSON及可选SHA-256校验；hash与解析使用同一份bytes。坏的无关文件不影响读取。它不签完整覆盖、独立验收或fresh现场；原始记录不得贴对话/进Git。
3. root规则与包内playbook停止要求每个门重跑无关全厂供需验证；一份不可变快照/投影和原回执可复用，不为改报告重采。生产连续36000-tick、供需来源归因和业务验收不降低。
4. 正常保存和root独立审计仍先完成；仅事实交付的文档/Git/CI可由离线Luna及时并行，不阻塞已核验、执行cohort未变的下一阶段。`Get-SpherewrightCommitImpact`以精确SHA保守分类：只有新增/修改的日期化evidence、incident、current-status、gameplay-timeline事实路径可判交付；AGENTS/playbook/安装/安全/代码/未知/删除或空结果都保留执行门。路径分类不是内容批准或CI成功；root仍核内容，pending不算绿，已知红main先修。

## 20/50写的边界与收益假设

三次普通十写门（R152/R185/R218），从第十笔terminal到下一窗首业务prepare平均23.15分钟，范围22.33–23.81；其中快照3.66、独立核查7.99、文档/commit6.17、CI2.03、续接3.30分钟。是含调用方/调度等待的墙钟，不是纯CPU审计耗时。两次普通触发间隔平均106.11分钟，不能概括全部历史；R282还混有恢复，单列特殊样本。

| 固定accepted额度 | 相同写总数下的封窗次数（相对10） | 边界 |
|---|---:|---|
| 10 | 100% | 保持旧调用兼容 |
| 20 | 50% | 推荐作为下一已核销窗口的试行值 |
| 50 | 20% | 单次独立审计变更量也增大，不作为默认建议 |

这只是频次算术，不是实测总耗时/token节省：更大窗的审计可能更重，阶段早停和unknown仍随时冻结。现有窗口按原声明核销后，root才可批准新上限；不借改脚本清零游戏revision/tick/Journal或外部accepted。此次未进行20/50写的实机周期对比。

## 离线验证与计量

AGENTS由19446→17215 bytes（仍超过8–12KB软预算，不为凑体积删安全边界）；current-status保持2384 bytes，不复制或重写另一任务的当前快照。历史事实仍在原专题与Git，未新建并列状态文件。未取得可比provider usage，root+Luna的缓存输入/未缓存输入/输出及token收益均unknown；不把字节当token、不把提交数当提速。

实际pwsh直接相关回归：action87，stage43+storage28+material50，reader15，index11，factory26，belt资格100，包面策略37，共397检查通过，全部零游戏调用。执行入口使用真实`pwsh -File`离线fixture，不再日常重复跑5.1；此次不涉及安装/最终包/5.1兼容。新增reader已接入现有Windows CI，不另建流水线。真实本地Git读取也验证了纯事实提交分类；一次无效SHA诊断明确拒绝，不签放行。

| 同输入离线比较 | 全目录解析后选择 | exact reader | 证据边界 |
|---|---:|---:|---|
| 200条干净合成回执、同一目标 | 每次读200条，中位88.027ms | 每次读1条，中位5.648ms | 同结果；预热1次、计时3次；`test-evidence-reader.ps1 -Benchmark`可重复 |

基准额外1个一致性检查通过，总398（含可选基准）；耗时随磁盘/进程波动。它没有测真实游戏、root/Luna调度、取证全周期或总token，不把局部读取改善宣称整体提速。现有64实体/3问题投影复用fixture也通过：6→2次投影，约983.906→385.328ms，仅本机离线。

本次未减少逐动作fresh原生prepare、精确计划、唯一commit、同action terminal、材料/实体/双向连接读回；保留单写者交接、独立验收、完整原始证据、owned身份与保存恢复、unknown/quarantine/版本漂移硬停止。没有新的live生产或部署结论。

## CI同进程回归补记

首次CI run `37760152389`（`a4c9cd9`）中，相关脚本均打印passed，但Actions多行同一pwsh step最终退出1：Git分类负例mock泄漏了`$global:LASTEXITCODE=1`。测试现以`try/finally`恢复原Git函数和原退出码（若此前不存在则移除），并断言两者恢复；不硬置0、不吞真实失败。新增3项状态恢复断言后，stage结果为46+28+50；结合先前397项完整本地回归，更新总数为400（此前397项未在本次重跑）。Actions同进程风格的六脚本本地离线复验全通过，最终`LASTEXITCODE=0`、零游戏/Bridge调用；这只复现脚本进程行为，不代表修复后的GitHub CI已通过，需以新提交对应run为准。
