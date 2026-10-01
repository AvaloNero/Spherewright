# 紫糖关键供给链：原料路径、端点需求与保存边界

日期：2026-10-01（Asia/Singapore）。本文件是本阶段唯一事件摘要；完整回执保存在受保护证据库，下面仅列索引和已核事实，不复制原始正文。

本阶段 pinned main SHA：`0f8a9005a23143d98074021b1811acd6632769dd`。未改 Plugin/MCP 产品代码，不把本文提交后的 docs HEAD 当作已部署版本。

## 本次状态与边界

当前 owned world 为 `owned-world-001` / planet 104，游戏版本 0.10.35.29104。阶段开始时外部 accepted 计数为 2/10。原游戏进程退出原因未知，不能据此称为崩溃或隔离。受保护恢复核验 exact owned primary 后，run `503868043939474795d1c30258ecea5f` 的 prepare/commit 与 action `6cf8dbd7-f075-4232-8a0e-0ac485ba3c1a` 成功；切换到新 SID 后，以正常保存 `82996693` 和完整 durable J96 核对连续性。root proof `fa4ba832007a42c1a0c6fd277006c515` / SHA-256 `4F74244941122264B95DD9403259869D4C6D9CE02780D49DD37E921AD5E20645`。先前一次菜单只读请求超时，随后仅做了一次 fresh 健康核验；未重复启动。

之后唯一正常保存 action `d2539d3f-f2b1-4069-a6fa-cbd91052a38d` 在 run `7dea5acbfd444c23acc738baea25b46e` 完成：prepare 4、intent 5、commit 6、terminal 7、session 8、Journal 9；保存 tick `83066935`，观察 tick `83066952` / R2，J96 durable、96 条记录完整、无 pending/error，`resumeAvailable=true`。独立 save proof `1629d7e5e40e49c5a9431d1f3cd53ce0` / SHA-256 `5853881F86DB56BC0D5F704E1FFD5807848A54CC6A644271E5BA77B7E7CDAFA5` 核对原 save terminal、session 与 Journal。保存任务的 Agent 连接中断后，从原回执核销成功，未重发保存。当前外部窗口为 **4/10 OPEN**，无 unknown 或在途写；`resumeAvailable` 不是此次保存后的实际重启/恢复测试。

保存后最终只读 run `4eb695b3a9b14ce1b4784bb223e8f883`：ordinal1 player、2 session，closing **83113691 / R2**，savedTick仍83066935，owned/healthy、Walk/speed0。独立 proof `de1ecec183ad4f2baec4e84a723365c6` / SHA-256 `6009D18ED95DCF94ABE9A44EF2C6BCF39B06536ABF619FC49921268FCC64473F` 逐项核保存后背包与保存前的itemId/count/inc完全相同。J96权威仍为保存run ordinal9，本批没有再次读取Journal。Luna已停止所有游戏调用。

从诊断到保存前，6004 背包从0到500、mecha科研缓存从1,800,000 points / 500 whole items到空，跨缓冲物料守恒；该变化发生在save之前，不是save产生的转移或新生产证据。其它背包count/inc不变。观测到的是coreEnergyCapacity 800→1600 MJ，最终coreEnergy约914.1 MJ，二者不能混用。没有本阶段新施工、采集、库存转移、配置写或Move；2104需求门不成立后，原拟正常手动采集64件1005石矿并原生转存95的有限试验，以及B多窗/C 36000 tick试验均取消，未生成其prepare token。

## 关键路径与需求核对

实机只读诊断 run `34a8058e28814258b2bed0371ea1f722` 的 ordinals 1–20 均成功；20个回执跨度5.512秒。端点查询run `9a8cdbb621dc4df6b918c09cff7afa84` ordinals1–3，命令约2.28秒、回执跨度约1.833秒。首轮诊断ordinal5的2104为291941/300000、队列仅2104、Lab84 working；端点fresh查询ordinal2为unlocked/300000，currentTechId0、队列空，ordinal3的Lab84 stopped。+8059 hash和科技完成由既有科研库存支撑，不能归因于新1402链，也不能证明新6004端到端送达。

独立诊断proof `58a04ce9ed834836b4d4e31e022084b2` ordinal1 / SHA-256 `91160ECC6CF8B3578F9BCC9EE5704091664F642B2C40577FEF9992502771182D` 核20份诊断、3份需求回执及完整durable J96。Runtime catalog ordinal4与实体buffers共同确认：1005石矿、1116硫酸、1123石墨烯、1124碳纳米管、1402粒子宽带、1303处理器、6004信息矩阵；配方24/31/33/36/55均unlocked。

runtime 配方及原料读数显示：

| 工厂 | 配方与实际输入/产出观察 | 当前约束 |
|---|---|---|
| 861 | 配方24：1114×6 + 1005×8 + 1000×4 → 1116×4 | 石矿5/8、精炼油12、水8；输出0、停机 |
| 869 | 配方31：1109×3 + 1116×1 → 1123×2 | 高能石墨6、硫酸0；输出0、停机 |
| 3050 | 配方33：1123×3 + 1106×1 → 1124×2 | 石墨烯2/3、钛块2；输出0、停机 |
| 2255 | 配方36：1124×2 + 1113×2 + 1115×1 → 1402×1 | 碳纳米管0、晶格硅4、塑料2；输出0、停机 |
| 4743 | 配方55：1303×2 + 1402×1 → 6004×1 | 处理器6、粒子宽带0；输出0、停机 |

额外库存边界：95 中的 2198 是 1108 石材，不是 1005 原石；3052 有 500 Ti；1123/1124 库存为 0。网络 3/4 的本次采样 ratio 均为 1，但相关源输出为 0，这不能证明持续供料。600 tick 观察窗 `83019433–83020032` 中，1005、1116、1124、1402 均为 0 生产/0 消费，6004 为 0 生产/1 消费；没有独立 1123 速率样本，不推算其速率。诊断命令从 dispatch 到首回执约 428 秒，不能与模型总耗时或旧流程比较。

## 静态路线仅作定位

以下边数由既有完整 built 快照 `da57094d67b04849a313ce89176800cb`（54 页、5315 built，tick 82811613）和 root 只读 proof 复用；该快照不是本次 fresh 原生预检，也不是现在的全厂 census。静态投影显示：95→861（1005，42 对象/41 对向边）、861→869（1116，19/18）、869→3050（1123，66/65）、3050→2255（1124，60/59）、2255→4743（1402，66/65）、4743→3051（6004，17/16）、3051→84（116，115）。这些数均为“路径对象/对向边”。补充 proof `008204aac87f4ba59fb064b35066b8d0` / SHA-256 `FFB764AB043B66DC45AFDE04E9B5A5D12BA8F5F1723667EC3B9E68F47E807547` 与 `202b6d897a9f46faad2b848cdfa4d4b2` / SHA-256 `B2B6EA8592E26D0999621C48A853AAAF21C9F7A1FDAE6987B4532BB34D350B0A` 分别核对其余路径段。静态连通关系不能替代 fresh native preflight、动态 buffer、产量或实际入料证据。

## 停止条件与未证明项

当前硬阻塞是石源配方路径不满足输入，尤其 861 的原石 5/8；没有依据新造紫糖路线或提交石矿机方案。此前有限 miner 选址均为 prepare-only 拒绝，未形成 plan/commit：已有记录中的点位碰撞 3727（yaw 90）和 87（yaw 0）只描述候选冲突，不证明其它所有候选都失败。不得重复这些地址、盲目换朝向或改动成功线路。没有开始 harvest、transfer、build、configuration 或 Move；持续采石、硫酸/紫糖生产、端到端供料、36000 tick 配平及保存后的真实重启均未证明。

截至最终只读累计49个Bridge响应：48成功、1个菜单只读超时；accepted新增为恢复和保存各一笔，业务施工0、原生写拒绝0。已知调用层局部错误2次（启动ACL检查FileInfo API不兼容、诊断完成后的汇总表达式错误），均未引起游戏写重发；启动前无唯一descriptor是零游戏调用的环境阻塞。Agent连接失败也未改判已接受动作。模型调用数及provider/root+Luna的缓存输入、非缓存输入、输出用量未知，不以Bridge次数代替、不估token节省。

恢复14份原响应跨度约6.908秒，但启动和精确guard准备另有墙钟开销；诊断委派到首回执约428秒，20次读取仅5.512秒，说明准备仍明显慢于执行，不能声称本次整体提速。保存leaf639.309ms（prepare112.792、commit329.920、terminal read149.673ms）仅为入口内部阶段；原生started=completed=83066935，观察到0个elapsed game tick。完整阶段首次委派墙钟、取证/文档/Git/CI及模型总量尚无完整可比计量，不伪造比例。

本地直接相关证据索引fixture4项通过、0游戏调用；私有恢复guard的AST smoke及12个离线fixture通过，未把它们算实机生产验收。本阶段只有一份事件和覆盖式状态摘要待单一目的提交；没有逐belt/sorter/save提交或重复全量本地构建。安装runtime仍为`6bf35b7b81a2e50c8e9f42feebbc1f15552096de`，228 files / 64 tools / 1 resource；没有新部署、tag或发布。
