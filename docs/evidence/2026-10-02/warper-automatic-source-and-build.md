# Warper 自动供料与建造准备

记录：2026-10-02（Asia/Singapore）。本文保留前置入口/材料事实及有限接口施工记录，当前进入完整整案的只读/prepare-only资格阶段；不是完整自动供料、整链产能或重启验收。追加部分留在本地，按新约束不为材料、单机或零写只读结果另造Gate 2里程碑提交。

## 前置入口与材料窗口（历史封存）

此前窗口从 0/10、lifetime accepted 23 开始，10/10 冻结、lifetime 33。runs `0efb2a182699476396adf21bb0c4c961`、`d78ba66493824f3985229e0c3c1ccd0a`、`beed2d28735243ba9bc5e8d5dc2516b4`、`b72af1dce67d43cfb3630c58e337370c` 合计十个唯一 resolved/completed action，0 replay、unknown、unresolved 或 in-flight。`b72af1...` 的六项包含四次 Transfer（两种物料各取、放一次）：item 1204×20 从 827 取出并放入 884，item 1104×20 从 26 取出并放入 884。884 从 default 改为 lock-occupied，两个输入格分别配置对应物料，其余格为 0，原三路输出保留。normal save tick 84852841 / R37，durable J97。

Root 独立审计 PASS：proof `action-22ffbb33c912411098ccdb40ae3df7f0-0001-warper-inlet-independent-audit.json`，SHA-256 `5B1850ABE88C0AEDDE35732BB1BC9E2DD04531200D4A0A28228C67F06F6C5363`（29.77 s，0 Game calls）。capture `cb17c8eb06bb4ae0b68494356042cc15` 为 54 页、5325 实体；全图 10386 条边均唯一互逆，无实体新增或删除。物品 count/inc 与原生 action deltas 守恒：Fe +292、Mag −4、Cu −2、Tesla +4，其余不变；power topology/capacity 不变、consumer ratio=1，prebuild=0；J97 身份和97条记录连续。

该窗口的设计上下文仍有限：旧北侧图 `c75e8d9b6bf1465ba42d1138cdc3abbd` 中 A3 为 `NeedGround`、B 已取消；power-context 不是完整覆盖证明。石矿 node 203 已由 miner 86 覆盖。只读 site 检查 `2332beaae1d14310ab68046d8efe15c8` 共17次：Lens/Warp `NativeOk`，Diamond 西侧及 object 2310 + Tower A 为 `NeedGround`；Tower/Storage 仅 prepare-only，未建实体。旧列距12布局 snap 后两 ASM bounding overlap，不能据旧布局或 prepare-only 结果宣称全链可执行。

## 前一轮有限接口施工封窗（历史）

外部新窗从 0/10、lifetime 33 开始。stage runs `02944d6232df44fe89801e6a27c5bb68`（先完成两项）与 `7d53ff4826d64af4bdc3224948441d48`（fresh suffix 八项）合计 10 个唯一 completed action；0 replay、unknown、unresolved 或 in-flight，lifetime accepted 43。首 run 完成 recipe 84 Craft×120（belt +360、Fe −360）及 collider 建造（item 2310 / object 5326）；suffix 配置 collider 5326 recipe 104，建造 Tesla 5327/5328、assembler 5329 并配置 recipe 78、Tesla 5330、stock 5331，最后 normal save action `871d078d-6293-4bf1-9990-cceaa2de75ad` 于 tick 84915771 完成。

Root 独立全图审计 PASS（26.61 s，0 Game）：proof `action-a3c3da97a47843b295eb202c04397030-0001-warper-construction-independent-audit.json`，SHA-256 `127F7D65DF39A87064BA02F21284AE6D31E2E05837D49EFC8AA50B36F16284FD`。capture `556086d58ba24144860466df5dad3995` 为54页/5331实体、0 prebuild，capture tick 84916675、closing observed tick 84917231；save/readback 为 tick 84915771 / R56 / durable J97。原5325实体静态配置及10386条互逆边全部保持；新增6实体无物流连接引用，其中两台配方机器正常接入 net3。net3 nodes 206→209、consumers 512→514，发电机与容量不变、ratio=1。玩家物料净额为 Fe/item 1101 −360、belt/item 2001 +360、collider/item 2310 −1、assembler/item 2303 −1、Tesla/item 2201 −3、stock/item 2101 −1；其余 inventory count/inc 不变。J97 identity 与97条 entries 逐字一致。

这只证明一次有限的未来接口施工 qualification。**Gate 2 整体仍未完成**：完整 executable 方案、自动供料、整链供给/覆盖、持续产量、实际保存后 restart 及 Gate 3 连续 36000 ticks 均未证明；Gate 1 虽已有 native 启动正例，持续 ≥1/min 仍未证明。此前的 prepare-only 或 NativeOk 只说明对应查询/位置边界，不代表生产链已运行。

## 石材来源、受保护恢复与旧窗口（历史封存）

本阶段在前一 owned-save 后完成一次中途受保护恢复及完整捕获；独立 proof `00ad7b5cfd7b4d629dbbc1879f402814` SHA-256 `16E3F627DCBC0163A34B453AC455937BFF55E9605051200BD5124275211423FA`。它只通过该次恢复子门，不代表最新 normal save 后已重启。首次恢复 prepare 因 `BRIDGE_NOT_READY` 停止、0 write，fresh 第二次成功。退役的 `SaveActionCache` 曾返回 `ACTION_NOT_FOUND`，随后按原 terminal 核销，没有重放。

object 95 的 Brick/item 1108 库存原为3000（满格），随后出现 Stone→861 acid→869 Graph 堵塞。一次100件 transfer 已成功；之后 sorter/entity 96 很快回填1件石砖（item 1108），令精确储位 prepare 被本地守卫拒绝，未产生配置 commit；成功前缀不重做。之后仅对既有输入做限量 guard（不是冻结所有 I/O），再 fresh 执行 58-unit transfer、预留一格 item 1005、恢复 bans=0 并 normal-save。受保护索引确认 run `122321492928498ebe0f85cdea360609` 的后缀五个唯一 accepted 完成、无 replay/unknown/in-flight；连同先前成功的一项常规 action 与一次恢复，本窗口共7个 accepted（6常规+1恢复），外部 7/10、lifetime 50。normal save tick `84935126` / R10 / J97 覆盖本窗口7个写入；其后自动生产引起的全部动态 buffer 变化不因此视作已保存。

完整 capture `0f1600f4bbd64c57bcbe262555c5c257`：factory tick `84939047`、closing observation `84940013`、5331实体、0 prebuild。root 独立全图审计 PASS（12.448 s、0 Game calls）：proof `action-fd59544676c1441a9390c1f90c0a326f-0001-stone-source-window-independent-audit.json`，SHA-256 `AFC767A8D85ED6E9637818EAEB6B35FEBA97317D3E0658A2E56838EF2DB0BA5B`。除 object 95 获准的 `storageConfiguration` 变化外，所有静态配置相同；10386条边互逆且不变；原三条连接保持；每次配置的同步边界上 native ordered-buffer count/inc 保持；player Brick +158，其余 inventory count/inc 不变；供电拓扑与容量不变、consumer ratio=1；J97 identity及97条历史 entries 连续。

直接采样 run `a3fedfa17f534abbb9364abda3a5a6f6` 的三个互不重叠600-game-tick窗口按窗计数（非速率）：planet 104 的 Stone/item 1005 产出 `5/5/5`、消耗 `0/0/8`；acid/item 1116 产出 `0/4/0`、消耗 `0/0/2`；Graph item 1123 产出 `0/0/4`、消耗 `0/0/0`。861 的 Stone 输入为 `7→5→2`、oil 为 `12→12→10`，miner 86 working、buffer `32/31/32`；object 95 的 Brick/item 1108 库存三窗均为2900。采样时869即时快照均为 `isWorking=false`、acid 0、Graph output 0；另一次同快照 detail 为 `isWorking=true` 且 acid input 1，这是不同tick的状态，不构成冲突。root随后用同R10/session的两项catalog只读记录与既有完整快照确认：runtime的1123输出配方31/32中，唯一实际 producer 为869/r31。归因 proof `action-c22be89a10a54788b8c58972aebcd3f6-0001-graphene-source-native-attribution.json`，SHA-256 `7F1E2768A62DF6FED1DEB5E878A1645526F026C0C5778ED47A912ED67FD5B135`（0额外 Game calls）。该采样62 requests、总墙钟284.370 s（scheduled wait 270 s、读操作13.884 s）；这是观察等待计时，不是持续产率证明。这只确认生产源，不证明自动送达883或完整自动供料。此前独立0产量负窗口不与这三个窗口拼接；以上不证明每窗非零、持续 ≥1/min 或稳定产率。

该历史窗口当时 **7/10 冻结**，lifetime accepted 50；其后在 main 基线 `b7658e2` 的远端与 CI 均绿、并经 root 明确 handoff 后，才开启后续输出端窗口。中途恢复成功不等于最新保存后 restart；Gate 1 持续产量、Gate 2 全链自动供料/供电与覆盖、Gate 3 连续36000 ticks 均未证明。installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104，228 runtime files + 2 native hashes 匹配）在该窗口时点尚未部署；后续获批单次维护与只读接口正例见[本文件新增记录](#获批单次维护与首次只读接口正例)。

## 前置阶段调用方边界（历史）

本次有两类本地调用方问题，不是游戏施工失败：一处递归 FunctionInfo 检查令流程在任何 Game 调用前停止（0 Game），随后固定为使用冻结 ScriptBlock；另一处错误要求 future recipe I/O budget 为空、且要求原生没有提供的 target ID 回显，导致 configure prepare falseguard，没有发出该配置 commit。原前两项 accepted 已核销，再由 fresh suffix 完成其余八项，无重放。18项离线 caller checks 覆盖原生 configure ordinal 315 正例。墙钟计量：prefix run 613.09 s（Craft action 终态 585.64 s、Collider 终态 20.99 s）；suffix 八项 149.71 s（委派入口至首个业务 prepare 4.15 s）；capture 26.81 s；root 独立 audit 26.61 s。各项为各自边界计时，不相加；provider usage unknown。上一材料阶段的 `DetailIds` 数组参数绑定与 `expectedStateHash` 字段修正见[材料储备阶段](warper-plan-material-reserve.md)。

该有限接口窗口 **10/10 冻结**，lifetime accepted 43；后续输出端窗口事实见下节及[当前快照](../../current-status.md)。

## Warper 输出端 29 带资格窗口（历史封窗）

在 `b7658e2` 远端与 CI 绿且 root handoff 后，外部新窗从 0/10、lifetime 50 开始。run `0e6db795c3a14ca3ad41fb0f83885d1f` 完成7个唯一 accepted：Tesla object 5332；将 storage 884 的 filter grid 配成 item 1204 一格、item 1104 一格、其余28格 item 1123，原三条连接及每次配置的同步边界 buffer count/inc 保持；assembler 5333 配方101；furnace 5334 配方60及配置；并完成29条 belt。原 action `399b7870-4bbb-45a1-ba84-20f41777ecc6` 的300秒 caller wait 超时不是 action 失败或 unknown：同 action 后续只读核销 run `d9b3618319644cf09050c105774ca3a4` ordinal 2 得到成功 terminal tick 84997174、29个唯一目标实体ID，玩家 belt/item 2001 为611→582。中间只读 run `14a1769b` 观察14个 prebuild、3架 drone working；`2238ea1` 等待420秒到期；后续读回为0 prebuild、3架 idle，均未重放 action。

原后缀入口的同名 dot-source 参数守卫在任何 Game call 前停止（0 Game writes）。修正 suffix root approval 后，run `ff9acd0ee05f47f6892d28a7877be6c0` 完成3个唯一 accepted：sorter 5364 filter=item 1210、连接 `5329.slot7→首条item 2001 belt`；sorter 5365 filter=item 1210、连接 `末条belt→5331.slot11`；最后 normal save。端点配置不证明货物流动。全窗合计10个唯一成功 completed action、0 replay/unknown/in-flight，external 10/10、lifetime 60；normal save tick 85002321 / R29 / durable J97 覆盖本窗写入。capture `6477dc98015f481a8e3e4f2c3925a07e` 为54页/5365实体/14项 detail/0 prebuild，factory tick 85002688、closing observed tick 85002847。该捕获有1架返航中非 idle drone、pendingBuildTargets=0、pendingRepairTargets=0；依既有 playbook 119–120，不将它视为未决写。

root 独立审计 PASS（11.641 s、0 Game）：proof `action-118ef6e9bd954626953228451f8b1650-0001-warper-output-independent-audit.json`，SHA-256 `7B5444BDE3EA5FD201DF202703CD955F6275D73E726F6CF74EE624BCD7FC7FA0`。原5331实体与10386条互逆边保持；获准变化为 object 884 filter grid 配置及 object 5329/5331 新 sorter 端边；新增34个实体、64条有向互逆边，总10450条边均互逆，无删除、0 prebuild。net3 只增1 node和4 consumers，发电机/容量不变、ratio=1。玩家消耗 belt/item 2001×29、sorter/item 2011×2、Tesla/item 2201×1、assembler/item 2303×1、furnace×1；其余 inventory count/inc 不变。J97 identity 与已存历史连续。

此前 `efc52def18934d92999bd4892fa3fa0d` 共15个只读/prepare调用，其中仅 ordinal 5/10/15 三个 prepare 被 native overlap 拒绝，未提交写入；不把它计作写入，也不盲目试第三条。计时分别为 suffix 120.77 s（首个业务 prepare 4.78 s）、capture 13.877 s；原29带 action 从 commit 到成功被观察为1204.489 s，包含 caller 等待/间隔，不是纯物理施工时长；provider usage unknown。

该输出端窗口当时 **10/10 冻结**，lifetime accepted 60，不填槽。有限接线不证明自动供料、Gate 2 完成、Gate 1 持续 ≥1/min、最新 normal save 后 restart、Gate 3 连续36000 ticks 或 final pack。该封窗已提交至main `20056a0`、Windows Core CI `36958908894`绿；新增整案资格门仍阻止下一写，不作handoff。源码审核基线main `b7658e2`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）在该历史封窗时点未变、未部署；后续获批维护见[本文件新增记录](#获批单次维护与首次只读接口正例)。

## Gate 2追加约束后的整案资格（本地，零施工）

仅1210完整三级链；保留5326–5334、5335–5365已完成输出路和石矿/酸/869链。整案仍`executable=false`、`wholeNativePreflightPassed=false`。只读/prepare-only；不建永久stub、不搬大批材料、不手搓未来库存、不转燃料/紫糖/远征或联合长窗。结束门是完整自动供料→非零1210→normal save→protected restart→恢复后再次非零。除强制十写封窗或必要最小代码修复，只形成一份Gate 2阶段evidence commit；本段尚未单独commit/push。

### 一个源—路—端草案，不是可执行证明

运行时catalog `f685e7aa30ee46c4b9a929a9ea70c3a1:2`确认目标1210≥1/min：r78需1209×1/min；r101需1112×4/min、1127×1/min；r104需1206×2/min、铁1101×2/min、重氢1121×10/min；r99需1204×4/min、铜1104×4/min、石墨烯1123×4/min。**r60金刚石输入1109石墨，不是1123石墨烯**：Diamond需石墨4/min，869/r31的石墨烯支路另需石墨6/min、酸2/min。r40新增重氢需要外部氢20/min，原燃料支路3405的配额保留、单独核预算，不开展燃料实验。

| 路 | 自动来源、方向和目标 | 保留/缺口 |
|---|---|---|
| 1204 | 814/r98→827→3475/filter1204→既有有向带3467→拟新增filter1204 sorter→884.slot1 | `644c74e9be404ffeb04c7e2006088f5f:6`曾完整prepare通过；未commit、token不保留，旧3404支路保留 |
| 铜1104 | 2440铜矿机→既有矿路→2437→2438/filter1002→10/r3→27→26→拟Cu路→884 | 矿路静态78对象追踪完整；最新26无铜。Cu整路未原生通过，不修改已成功矿/炉路 |
| 石墨烯1123 | 石矿/油/水→861/r24→863→既有4417/4416/4415酸路→870→874→869/r31→872→871→拟filter1123带/双sorter→884 | 保留870→873石墨、874酸及旧4644输出；新到884路未通过 |
| 884三料 | 884→887/filter1204、888/filter1104、889/filter1123→883/r99 | 现成三条入料连接保留；真实自动到货未证明 |
| 1206出口 | 883/r99→886→885 | 886未过滤但r99唯一输出就是1206；不能把未设filter本身当错误或新增无必要配置写 |
| 1206到1127 | 885→拟filter1206双sorter/有向带→5326/r104 | 新路、源/端实际slot和跨接未核销 |
| 铁1101到1127 | 1500/r1→1512→1511→拟filter1101双sorter/有向带→5326 | 原1535支路保留；单帧库存3000不是持续可分配率 |
| 重氢1121到1127 | 外部3964/r16氢→既有3347→3074→3075→3073/r40→3076→3074→拟filter1121双sorter/有向带→5326 | 本地循环不算外部来源，旧3405→3403不改；缺实际可分配20/min联合预算及新路原生证明 |
| 石墨1109到Diamond | 3084/3966/r58→既有有向支路→4483→4485/filter1109→870→拟filter1109双sorter/有向带→5334/r60 | 870已满石墨2500；保留873/874与869，库存与r58循环P/C不算持续外供 |
| Diamond1112 | 5334/r60→拟filter1112双sorter/有向带→5333/r101 | NEW内部物流未预检，不重建5334/5333 |
| Strange1127 | 5326/r104→拟filter1127双sorter/有向带→5333/r101 | NEW内部物流未预检，不靠手搓/注入启动 |
| Lens1209 | 5333/r101→拟filter1209双sorter/有向带→5329/r78 | 输入slot必须避开已用输出slot7；新路未通过 |
| Warp1210补给 | 5329.slot7→5364/filter1210→原29条有向belt→5365/filter1210→5331.slot11 | 完成/已保存的物理前缀保留；没有1210货物、最新保存后的restart或恢复后输出 |

以上逐路给出业务方向，**不是精确slot/几何和整图原生通过**。存货、多行buffer或未设filter都必须按真实语义解释；不把belt的null货物当空带，也不为凑计划补画未来实体ID。

材料与场地：已建9个核心设备、29条belt、2个sorter不再采购。最新背包`35e214503c864604b34d6b97596d255e:2`为belt582、sorter28；剩余≤571带/20sorter为暂定上限（含下述未提交的1只石墨并联），不是精确原生清单。跨接新增sorter/电塔和可能的上游升级尚未计入，不擅自把它们当免费或手搓备货。现有范围没有整案正例，未来对象无ID；旧输入/输出、电塔、地形和占位均保留，任何拆改须另行处理。

功率：`54d05be94c4441b38f9dce45b0da40ca:2`给net3额定已有负载1797800 J/t、容量1894000 J/t，已含已建核心与输出sorter。若未来20个sorter全在该网且没有额外负载，增加6000 J/t后余90200 J/t=5.412MW。该Foundry dummy site被5334占用、`nativeCheckPerformed=false`，它只给功率上下文，不给新位置/覆盖正例；最新瞬时满服务不替代额定或连续能源验收。物流实际可分配率、精确带数、所有NEW sorter覆盖均仍是整案blocker。

### 原生预检与当前来源证据

Cu的三个拒绝为`2f6a05151cc34cada6aa295f5d6d3f37:5`（free起点撞3210）、`41d1adfa5a3c4d3892c82b2d59d9d12b:5`（绑定2377源点撞2344）和root重设计的远端4718整路`282ac65469fc42168015d2426ba45894:5`（第7点撞1318）。最后closing ordinal6为85105375/R29/healthy；全部0 commit。不会把前7点当已合格，也不在失败点微移/换直线seed盲试。静态追溯1318经100对象到1217/1241未过滤仓源，实际货物身份未证；不能凭belt原型2001就称Mag/Cu跨接。

只读包`35e214503c864604b34d6b97596d255e`一次22 calls：1 session、2 player、3 Journal、4–19依次861/870/869/871/1500/1511/10/26/3073/3074/883/885/5335/5352/5326/5329，20 production、21 power、22 closing。opening85119600→closing85119623/R29，normalSaved85002321保持；Journal原97条、durable97、pending=false、error=null，0新accepted/新实体/在途，external10/10冻结、lifetime60。相邻belt本端与此前不同tick的device端读取不是新同tick整图审计。

该包的单个完整600-tick local104诊断窗85119022–85119621（3/3工厂、无cursor）P/C：Fe5/9、Cu5/6、石墨8/10、酸4/10、氢24/27、重氢0/20、石墨烯2/3、Turbo1/2。10是唯一local铜producer，本窗产出5；10当帧输入矿/输出铜均0且26铜库存0，**不等于已成功铜矿路线失效**。869当帧working、石墨0/酸2，871和883石墨烯输入空；既有生产活动不等于能持续分配给新链。net3当帧required=served=492499 J/t、capacity1894000、ratio1。前一包`c66a9a83efd345d6aca2d08526e64149:7`的600tick P/C不可与本包拼成连续或稳定验收；全图有10个石墨直接生产者，不能把全局石墨计数只归因于870的两个r58配置来源。

本包22个原回执的写盘时间范围为2.063秒（first/last receipt mtime），这是取证写盘跨度，不冒充委派总墙钟、纯游戏耗时或token节省。provider usage unknown。完整回执留在受保护存储，root直接核关键原件，不重采同一证据。

直接相关的石墨并联接口资格`675d345071214ec1a7438efb5b3f363c:1–6`只读/prepare-only：opening85142360、closing85142366/R29/normalSaved85002321/healthy，0 accepted/commit。ordinal5完整原生正例为2011×1、filter1109、870.slot6→869.slot2、`exact_slots`；root核原回执的精确字段，并将ordinal3/4旧连接与35e:5/6逐项比对不变，869仍r31。token已丢弃，不提前施工；该几何正例不证明石墨实际到货或持续供量。最新Journal证据仍35e:3，没有在675包伪称重读。此项并入整案预算与本份阶段证据，不另造单机里程碑。

### 不能用施工补证的接口边界与后续验收

现有安装态的`TryValidateInserterBuild`要求两个正的已建本地entity ID；仓/核心设备→尚未建成belt的精确原生连接不能由离线slot向量代替。BlueprintSite则明确只支持NEW内部端点；不能复制/覆盖5326–5334来制造联合预览。独立合法的地面/抬升span也不证明未来sorter join。必要最小修复`3fe31d1`已在源码增加无token的`beltEndpointPreview`，离线构建/回归和对应CI已通过；具体边界与外审状态见[Foundry API](../../research/game-api-foundry.md#read-only-existing-endpoint--new-belt-qualification-2026-10-02-live-pending)。在本段接口调研完成时，安装态尚未更新或实机使用，不能把源码能力写成已安装或整案正例；后续一次获批维护及局部只读正例见[本文件新增记录](#获批单次维护与首次只读接口正例)。整案false期间维护权限仍不等于可施工。

当前本机DLL `Assembly-CSharp` SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`已作离线精确调研：`BuildTool_Inserter.CheckBuildConditions()`依据真实`inputObjId/outputObjId`调用`ObjectIsBelt/GetObjectPose`来定长度、网格和碰撞规则，不能传0或假entity ID冒充未来连接。`BuildTool_BlueprintPaste.CheckBuildConditions()`能识别工具内`input/output`预览引用，但该调研时安装态Spherewright的NEW-only检查明确拒绝外部`inputObjId/outputObjId`；底层DLL机制与源码修复都不等于现场支持。该只读适配不开放任意蓝图外部匹配，不改变施工白名单；当时冷部署与实际调用仍待，且该静态调研自身为0 Game calls。之后仅取得一个局部只读原生正例，边界见[本文件新增记录](#获批单次维护与首次只读接口正例)。

执行前必须补齐精确方向/槽位、所有材料与覆盖预算、最难源端/跨接/最终输出的fresh原生正例，才可解除整案资格门；每个真正动作仍fresh prepare/唯一commit/同action终态/双边读回。启动后按源→1206→1127/1112→1209→1210→5331逐级非零取证；三个互不重叠600tick短窗只证明重复活动。Gate 2当前结束门为normal save→exact protected restart→恢复后再次非零；随后另按已声明1210≥1/min、连续≥36000 game ticks、真实输入分配/库存趋势/电力/消费端、gap/session/功率异常重置信用，不能拼窗或用库存脉冲通过。此前不启动联合燃料长窗、远征或最终双包。

### 追加只读源诊断（同一Gate，未另行commit）

run `4e01805a36b345949d029cac5595ce2e`：1 session、2 player、3 Journal，4–13依次3964/3073/3074/3084/3966/870/869/871/883/884；14为固定采样intent，15–33含18次sampler原生读取及1条sample事件，34为到期失败事件。唯一closing为`dcc6eed3c9054eed97f30d00eeddb0e7:1`。root直接核31+1个成功原响应均为只读、身份/R29/保存点未变；opening85219736→closing85221427、normalSaved85002321/healthy/和平，J97原97条/max sequence97/durable97/pending=false/error=null。0 prepare/commit/新accepted/在途/unknown，external10/10及lifetime60保持。没有保存、加载或部署。

采样只取得一个完整ready、非跨session的600tick窗85219812–85220411（ordinal26，sample27），没有三窗或持续验收。local104 P/C为Cu5/4、石墨5/7、酸4/9、氢12/17、重氢5/0、石墨烯4/3、Turbo0/0、1206 0/0；P/C不能代替真实可分配供给。完整直接诊断分别确认814/1204输出buffer10/容量10而停机、883/1206缺石墨烯（0可用、每周期需2）。fresh883仍有铜4/涡轮4但Graph0，869为石墨2/酸2/Graph输出0，870石墨2500、871空；3073当帧氢8/重氢0而窗中实际产重氢5，不能由单帧false宣称永久停产。采样net3 ratio1、required=served=252914 J/t、capacity1894000，不是全额定覆盖或持续功率证明。

边界：没有fresh inspect3347/3405、4485/4483、873/874、887/888/889/886或Cu/Fe/Turbo端点，沿既有静态捕获引用、动态仍未证；1127/1209/1210没有本次采样。派生13-route视图已纠正“已建机器是未来端口”与886未过滤导致假blocker的措辞，固定整案SHA/13路/false边界校验通过，不改变任何实际计划或游戏配置。

计时与调用方结论：原委派清单超过root所设调用预算、12物品又超既有sampler的8物品上限，在0 Game calls时被拒，计1项调用方规划失败而非原生错误；root收窄为10设备与8物品，复用现成sampler，没有改工具/私建框架。基础13读的回执跨度约2秒；开场至closing原回执跨度157.813秒，tick净增1691、观察均速10.715ticks/墙钟秒（非未来恒定速率承诺）。ordinal34证明采样120037ms、实际读取1617ms、scheduledWait117985ms、18/26请求、1/3窗、`deadline_exhausted`；它不是Bridge未知，不重试。该现场速率下三窗至少需要约168秒纯模拟等待，原120秒预算不够，后续新实验应先声明适配实测速率的有限预算而非到期续时。委派/独立验收/文档的完整墙钟与provider usage未知；不从byte、速率或未完成实验推算token节省。整案仍false，唯一下一门仍为完整接口/预算与自动供给资格，不扩大支线。

## 获批单次维护与首次只读接口正例

本节记录一次窄范围获批维护，不代表Gate 2整案通过。源码基线为main `3fe31d165109561d75658578d836e396153b44dc`，Windows Core CI `36982487740` success；此前输出端十写窗仍按root proof `action-118ef6e9bd954626953228451f8b1650-0001-warper-output-independent-audit.json`（SHA-256 `7B5444BDE3EA5FD201DF202703CD955F6275D73E726F6CF74EE624BCD7FC7FA0`）封存，提交基线 `20056a0` 的CI已绿。normal-save回执 `473740dfef9e42dcae96a0030bccf9b9`为tick `85236820` / R30，J97身份与97条历史entries连续。离线cohort `action-450b1bc7f2c44f448dd29428b3547079-0005-belt-head-cohort.json`（SHA-256 `D4668BB158D1B59E02DFBBEB9535EFE6A040183A55A4DC53F7B5804F3081134B`）含228项相对路径、长度和SHA文件索引；4 Plugin + 224 MCP，两个native引用哈希不变，MCP metadata probe为64 tools / 1 resource且playbook与107506字符源文件精确一致。获批本机安装run `7e327e98c9cc4237976cf50b54b0d554`状态committed；首个preloaded resume因`BRIDGE_NOT_READY`拒绝、0 accepted，等待20秒后fresh protected same-primary resume `f4ac6bfb8d25498db51705a27d9eea9f`单个accepted成功。恢复后normal save为tick `85236851` / R1 / durable J97、pending=false、error=null；该resume的`completedAtGameTick=null`，保存tick不是action终态tick。独立维护/恢复审计 `81f70cbb02da45b9bfec83d83b947dc9:1`确认external 2/10、lifetime 62，0施工、unknown、in-flight或replay。

接口正例前，preview run `33e719e…:10`的源/目的朝向错误（source=2、destination=0），两个端点均以`TooSkew`在native评估前被挡（`nativeCheckPerformed=false`）、0 accepted；按`Maths.SphericalRotation`北向基准核对四向后，只把source的`PlannedBeltQuarterTurns`从2改为3、destination从0改为1；位置和2.7m偏移不变，也没有代码修改。corrected run `92afe0c8fb6149a6891c60d641ffdc15:10`返回native performed/passed、无blockers：`5334.slot4→NEW首条belt`及`NEW末条belt→5333.slot10`，filter 1112 / sorter 2011，两个端点均native Ok、span=2，整段为5条belt/2个sorter的只读资格检查。7个native请求产生双层14条receipt，不应双计。opening tick `85298751`至closing `85298791`，R1 / save `85236851`；player hash、inventory count/inc和pose均未改变。端点预览独立审计run `67bfbe9408ba4cbfb779376c545b85c5:1`（SHA-256 `8A9C0B98CEBAF681BA80C7108C8F7AC8319563C3FDE5D069666272B975171CF0`）通过（0 Game calls）。

保护回执会把空`planToken`字段也序列化成`[REDACTED]`；这不是发放非空token的证据。该版本server常量为空，响应`prepared`、`commitAllowed`、`executable`均false，executor也核对空token。本次没有施工、1210输出或整链供料；Cu跨接及其他物流端点、精确材料与来源/供电分配、整案`executable=false`、最终包和异机live仍未通过。宽范围native外审没有终态，仍标未完成；上述正例仅适用于这一次绑定的局部端点只读检查。

同一维护后资格阶段继续只读：`4eaf34ac847a47a3b502fdf81809861a:1–7`为7次原生读取/1.372s，opening85324346→closing85324387/R1/save85236851/healthy。2377真实货物为Cu1104×2，其有向旧连接2378→2377→2379；相邻2344仍为旧belt。2377距当前玩家86.12m而buildArea80，不能从它盲试或绕过范围。884旧887/888/889与过滤配置不变，当帧Cu16/Turbo17。ordinal5的Foundry dummy site已被5334占用，仍`executable=false`；只取其fresh power上下文：net3容量1822000、已有最大保留负载1797800、export0 J/t。假设20个NEW2011均在此网覆盖、无额外负载，增加6000 J/t后余18200 J/t=1.092MW；不是未来覆盖、持续燃料或dummy施工正例，旧5.412MW不得沿用。`3d9ed…:1–2`曾仅在session读取后因调用方误带R29/save85002321而停；实际当前R1/save85236851/identity/health正常。纠正批准参数后才执行4eaf，计caller错误而非原生拒绝，未重新加载或accepted重放。

第二个接口正例`e1961793600d433dbf9f89673543fc14:1–7`：7次原生调用/1.182s，opening85365487→closing85365519/R1/save85236851/healthy。ordinal5为5333.slot1→8条NEW2001→5329.slot8、filter1209、2011×2、planned turns2/0；双方native Ok/span2、performed/passed=true、blockers为空，prepared/commitAllowedNow/executable=false。root按fresh endpoint/player/hash与原件独立审计`ce5553562f21429da239fd7f8eea1bb4:1`，SHA-256 `A597944A8146307E27B2A8A7687AB0975DCBB3E1C71AA31375132E60E0940867`，0 Game calls。仍0游戏写入/扣料/在途/unknown，不证明已建路线或1210输出。

该正例之前`f20920032bcd44b69901f4a77605d214:1–8`完成8次只读但没有prepare：调用方偏角计算误报约180°，随后本地摘要`if`表达式亦报错；原回执不受影响，不把它们算成native拒绝、accepted或unknown。root按Core参数角色独立算得源/目的偏差约0.572°，给定相同2.7m偏移的固定点位并核fresh端口未变化后，e196直接复用既有caller取得正例，没有改插件、放宽原生角度或重放动作。复用结论：`SphericalRotation(pos,0)`以北为基准；source角色用“既有源outward / NEW首端forward”，destination角色用“NEW末端forward / 既有目的outward”，不要把两角色交换；所有最终资格仍取实际native响应，不把离线偏角当场地通过。调用方准备、模型思考及provider usage总量未知；上述1.372s/1.182s是有限只读执行用时，不能称端到端提速比例。

### 铜抬升源端单候选拒绝（未施工）

固定2396西侧0→3抬升候选只作一次资格预检：`0646a99b5838454d9a1365eba9c031b7:1–4`依次session、inspect2396、player、prepare。fresh2396为2001、旧有向连接2395→2396→2397，选定端口姿态与批准值匹配；cargo未观测、virtual slot的null占位未当自由证明。ordinal4返回`BUILD_LOCATION_INVALID / belt_path_existing_overlap`：planned point0与object517重叠，明确`No objects were created`。该错误来自现有NEW带占位保护；响应result=null，没有原生全路径或端点checker通过证据，不能写成已经完成联合native检查。未改变位置/高度、没有第二候选或commit。

closing `a412f2ef234843bc9eda9ca1a6c2c6dd:1–2`独立读取session/player；root核原件并要求非空player hash后比较，opening85418204→closing85419756、SID/R1/lastOwnedSaveGameTick85236851/owned/healthy及player hash均不变。合计6次只读/prepare、0 accepted、0新实体/扣料/在途/unknown，外部2/10、lifetime62；Journal仍引用恢复时J97而非本包fresh读取。静态完整捕获`6477dc…:7`确认517原型2001、旧516→517→518仍是既有线路；保留，不把去掉源绑定或覆盖517作为“修复”。下一步只作完整铜路线重设计和资格核验，不能微移同一失败起点、把独立抬升span拼接当整路通过，或在整案false时建测试stub。两处Diamond→Lens、Lens→Warp只读正例保留，不重做。

### 重新连接与固定铜替代首段（游戏保留运行）

source main `9b10209ee36a89ab9cbe710c120e1829e45f4e1c` 已把Codex/Host和DSP生命周期分离。重连时实际没有DSP进程；旧writer已被停止、确认无在途或未核销accepted后，短上下文gpt-6-luna/max writer只进行一次已授权Steam启动和protected same-primary恢复。root启动前重新核对4/4 Plugin、224/224 MCP、2/2 native引用与既有3fe cohort证明完全一致，没有部署新代码。恢复run `70dbeb28ea624de3a11ab7ddff5a761a` ordinal23为同一action `b79fb598-d78a-468b-8ab9-301f44a10865`成功终态（completedAtGameTick仍null）；24为新session/R1/normal-save85236882、观察85236894、owned/healthy；25为J97/durable97/pending=false/error=null。root独立证明`action-62a0f4e375494373b2d90bd7ba23104c-0001-gate2-reconnect-independent-audit.json`，SHA-256 `6059D10E4E0F06F5812C6B3393686287DF19FFA63A2F7BD8105B580BE088B45D`，直接核97条entries与前次恢复完全相同；0额外Game calls。external变为3/10、lifetime63，无新施工/unknown/replay。后续Codex重启只重连，游戏保持运行；此处真实无进程恢复不是Host关闭触发游戏退出。

2396东侧的旧固定候选`02b61ddd681d45a3a56fe82f7ce45d20:6`为pre-native占位拒绝：generic blocker `planned_endpoint_collision_or_prototype_unavailable`，nativeCheckPerformed/passed=false、span0/not_checked，12条NEW带/1分拣器只读预算。不能把517旧西侧障碍套用为东侧拒绝原因。

root改为从真实Cu仓26至884的一份四段完整替代草案，整案executable=false/wholeNativePreflightPassed=false不变；三个内部跨接及最终端口仍未通过，假设新增3分拣器的整链23预算没有获准，不把静态功率算术当覆盖或持续余量。writer前一条大命令在PowerShell解析阶段失败（`$_ .Exception`），0 Game calls/accepted；停止重写caller后先收四条fresh事实，再使用纯参数JSON和原共享caller。fresh读回run `cce58a282f284344a856db287eb06c28`的3/5/7/9分别session/player/26/884，观察85276867、R1/save85236882、Walk/speed0/buildArea80；source26与sink884均真实storage2101，26.slot8和884.slot0空，端点hash来自原响应而非静态预测。

固定A只做一次prepare：run `fc9b9c9a4f264379a476f5471a4aeef6`的1player、2inspect26、4prepare_build、6closing session、7closing player，共5原生请求/1.324秒。A为26.slot8→NEW首带、filter1104、2011×1、native_elevated_grid 0→3，10条NEW2001/1分拣器预算；response prepared/commitAllowedNow/executable=false、executor当场核token为空。结果仍是generic占位拒绝、native未调用，span0/not_checked。closing85297743、同session/R1/save85236882/owned/healthy；root独立比较opening/closing非空player hash及完整inventory JSON相同。没有B/C/D预检、位置微移、Move、材料转移、手搓、save或commit；external3/10/lifetime63不变。保护logger即使空token也会显示REDACTED，不能只凭该字样推断token是否发放。

现有错误把计划内碰撞、既有entity/prebuild碰撞和原型缺失合并，无法知道哪个对象或守卫分支阻止检查。直接blocker的最小源码修复保留原generic首项并附首个精确reason及零基计划索引/有符号object ID；判定公式、扫描顺序、缺原型fail-closed、材料/范围/cover guard、tokenless和native资格不改。当前仅离线源码，尚未部署/实机读出阻挡对象；不得写成已识别障碍、已通过Cu接口或可施工。游戏保持开启，不为这一诊断或Codex重启自动冷部署。

该最小修复已单一目的提交并推送`c9f34f3b9dd8bcd16e01c3ba4ed4dd13aa10e16a`（`fix: identify planned belt endpoint occupancy blockers`），root独立确认远端main精确SHA和仅3个范围文件：Plugin预检、Core专项测试、包内playbook。离线维护gpt-6-luna/max完成104/104 Core Release专项测试（测试368ms）、完整Release solution build（11.74秒，0warnings/0errors）、1/1 MCP playbook资源测试（781ms）。流式Claude代码/测试外审终态APPROVE；root独立核guide。外审明确新用例测试的是详细失败echo的安全/边界，不是实际驱动factory碰撞扫描，不能当新字段的实机证据；当前没有新cohort安装。GitHub该SHA status/workflow均空，CI不可记通过。原4项dirty文档保留至同Gate阶段闭合，不混进该修复commit。没有游戏调用、保存、关闭、部署、tag或发布。

### 同Gate六端口核查与CI核销（仍未施工）

冷更新尚未获新确认，游戏保持原进程运行。gpt-6-luna/max在一次有界包中仅执行11个Bridge只读请求/2.961秒：run `434c2087d5a24bd7b4aa34ea353914c9` response ordinal2 session、3 player、4 Journal、5–10依次1511/3074/885/5326/870/5334、11 closing session、12 closing player。opening85398646→closing85398744，same session/R1/save85236882、owned/unrestricted/healthy/29104；J97/durable97/pending=false/error=null。root直接核全11原回执、同一玩家非空hash/完整inventory相同，97条Journal与70dbeb恢复原件完全一致；独立证明`action-f12f0202350e4662ba86ab8c8b515847-0001-warper-six-interface-independent-audit.json`，SHA-256 `7492CA3C742157D43D54BFC6F8ADD20BB78271353455BF8B9FDCBF980017F89B`，0额外Game calls。0prepare/commit/施工/材料转移/craft/save/load/close/deploy，external3/10/lifetime63不变。

| 对象 | fresh事实 | 仍未证明 |
|---|---|---|
| 1511 / iron1101 | 30×100=3000；free0/3/4/5/6/7/8/9/10/11；旧1512输入、1535输出保留 | 实际可分配持续铁供量、外侧邻居fresh互逆 |
| 3074 / deuterium1121 | buffers空；free0/2/4/5/8/10/11；3347/3081/3075/3076/3405分支保留 | 真外供氢及共享20D/min持续能力 |
| 885 / container1206 | buffers空；free1–11；886从slot0输入保留 | 883实际产出、至5326路线 |
| 5326 / collider r104 | 1206/1101/1121/1127均0；free0–8；net3/ratio1、idle demand2000 J/t | 三输入物流、实际1127输出 |
| 870 / graphite1109 | 18×100+67=1867；free3/4/5/6/7/8/9/11；873/874输出及4485/4415输入保留 | 持续可分配余量；不跨session用历史2500计算净下降速率 |
| 5334 / diamond r60 | 1109/1112均0；free0–11；net3/ratio1、idle demand200 J/t | 石墨输入、实际金刚石生产 |

全部实际endpoint poses/outward/hash在原件，root核factory objectId而不是调用方误猜的entityId字段；session观察tick来自gameTick，不能用不存在的capturedAtGameTick填null。当前私有整案的旧session/save/accepted证据引用已替换为本包，并把旧维护引用标historicalOnly；这不是可执行状态源，不允许复用hash/token或将readonly库存视为供量证明。wholeNativePreflightPassed/executable仍false，20分拣器暂定上限不变，无Cu第三候选或B/C/D盲试。

CI补充：无缓存GitHub API确认远端main/commit均精确c9f34f3，现有workflow347387595为active，先前列表没有该SHA运行不记通过。root授权离线维护只dispatch已有Windows Core CI一次；run [37007620233](https://github.com/AvaloNero/Spherewright/actions/runs/37007620233) event=workflow_dispatch/head_sha=c9f34f3，最终completed/success，所有job步骤通过，root再次独立API核实。没有改workflow/权限/认证/发布设置，也未触碰游戏；此前空列表结论仅适用于dispatch前，不是当前CI状态。正式双包和其他Gate仍未完成。

### 同一live session的三段供料诊断与铜源核查

仍是source `c9f34f3`、installed `3fe31d1` / DSP29104、normal-save85236882 / R1、external3/10 / lifetime63，整案false。游戏保持运行，不以Codex/Host退出或本轮结束为由保存、关闭、重载或部署。writer复用现有`Invoke-SpherewrightProductionExperiment`，固定6实体、8物品、3个独立600tick窗、90请求/240秒上限；没有模型逐窗决策、延长实验或写入。

run `b6eaf6cb843f46d6b221ad48551a4111` 原生产响应`:14/:27/:40`的窗口为85482247–85482846、85483147–85483746、85484031–85484630；均ready/600tick/10秒/未跨session，三颗已加载工厂覆盖完整、104八物品齐。采样事件`:15/:28/:41`及终态`:43`与原件一致。共38只读，scheduled wait45.000秒、read6.086秒、sampler total52.168秒；三个窗口间有缺口，continuous credit=0，`sampling_completed`只指数据采齐，不是sustained/Governor通过。

| runtime物品 | 窗1 P/C | 窗2 P/C | 窗3 P/C |
|---|---:|---:|---:|
| 1101 铁块 | 30/18 | 30/0 | 30/0 |
| 1104 铜块 | 6/0 | 0/0 | 0/0 |
| 1109 高能石墨 | 42/60 | 36/42 | 36/30 |
| 1121 重氢 | 30/0 | 30/0 | 30/0 |
| 1123 石墨烯 | 12/18 | 12/18 | 24/18 |
| 1204 电磁涡轮 / 1206 粒子容器 / 1210 空间翘曲器 | 各0/0 | 各0/0 | 各0/0 |

表格单位为实际**件/分钟**；原`producedCount/consumedCount`为10秒计数，速率是其6倍，不混写。10/r3三帧Cu output100、ore4、idle；869/r31的1109输入0→5→3、acid/output0、working→idle；3073实际item2310/r40，氢输入7→4→3、重氢输出0、idle；这三生产设备ratio均1。870石墨库存1627→1623→1618是同一session窗口读回，3074及885均空。planet104重氢有产量不证明3074可分配余量，也不能仅凭总产量完成来源归因。

附加收尾`:44`的session为same/R1/save85236882、tick85484652，但`:45 get_player_state`返回STALE_STATE，不能记成玩家闭合成功。执行者确认当次payload为空；保护receipt没有请求body，不宣称原件证明该body。root另核`LocalPlanetRequest.PlanetId`默认0、`GameStateReader.ValidateOwnedPlanetOnMainThread`拒绝<=0，因此是读取参数遗漏，不是已证明的世界漂移。只纠正一次explicit planet104读取：`cf69b30df94c4e78b8d97eec6086c9ec:1–3`成功，同session/tick85539501/R1/save85236882、Walk/speed0/full core，J97/durable97/pendingfalse/errornull。另一次Journal原件`b47c0ca2de10417fba3522ce664833a0:1`也连续；root将两份97条entries与434c208:4逐字比较。没有重跑采样、游戏写入或accepted重放。

root独立采样审计`action-e8c799b4c23c4711bdfa58815fd9d914-0001-warper-source-window-independent-audit.json`，SHA-256 `EFE55E182D3FD32A6EC84C0BC89F60105CB8321AE1B8AA11B69E88397302C35C`：直接核46个保护文件、38原请求唯一、每窗覆盖/单位/摘要/配置与power、纠正收尾及Journal，0.880秒/0 Game calls。52.168秒仅采样执行，不是委派、主会话准备、独立验收和文档总耗时；provider usage unknown。

随后只核既有Cu源，不扩建或prepare：`d66fa78e4b0e46af89aa6d036b3f3eda:2–7`六原请求/1.340秒，`:3/:4/:5`分别10、27、26。10.slot3→27.slot1、27.slot0→26.slot11双向互逆；27 pick10/insert26/filter1104、Inserting、held1104×1。26全storage rows合计Cu100，filtered/30grids/bans0、仅grid1为1104；旧输出分支保留。关闭观察tick85553897 / sameR1/save85236882，player Walk/speed0/full core。调用方摘要`:8`误把六请求写成5，原response记录为权威，不重采或修改原件；这计为摘要计量错误，不是native拒绝或unknown。

root独立Cu源审计`action-2ecfc470336644b3a6c322da3f436ffb-0001-warper-copper-source-independent-audit.json`，SHA-256 `5EDBC8C0D84F1C175105C2F176A02B01F012D3930A70B6976E5545B09C995B08`，直接核六原回执、既有互逆边和全row库存，0 Game calls。已完成Cu线路没有重做；100库存和瞬时配置不证明可分配持续供给，更不证明26→884的NEW端点资格。当前最小阻塞仍是未部署精确诊断的Cu端点占位及整案其他未核条件；额外3sorter预算、全路native、精确材料/覆盖、实际1210输出仍未通过。无在途、unknown、新施工、save/load/close/deploy。本节继续留在同Gate单份阶段证据，不拆普通只读commit。

### 重氢与石墨烯的既有局部运输资格

保持同一live owned session和3fe安装态，root只批准固定六实体的只读包，不批准施工、采样重跑、保存或冷部署。run `af2e99adb8b44b86af9b78005af2e24b` 十个Bridge请求/1.784秒：session`:2`；3073/3076/3074`:3–5`；869/872/871`:6–8`；explicit planet104 player`:9`；closing session`:10`；Journal`:11`。opening85595797、closing session85595853，J采集85595859；R1/save85236882不变。player Walk/speed0/core full，J97/durable97/pendingfalse/errornull，97条entries与cf69b30:3逐字一致；external3/10/lifetime63不变，0新accepted/unknown/在途。

3073实际r40，氢input2、重氢output4、ratio1/idle；3076 pick3073/insert3074/filter1121，Sending且held1121×1。3074所有storage rows没有1121，filtered30grids/bans0、首格1120其余1121。`3073.slot1→3076.slot1`与`3076.slot0→3074.slot7`两侧方向互逆。这证明原生产设备已有产物、原分拣器正在携货；不能因仓空就新增D设备或宣称原路断了，也还不证明货物已入仓或可分配持续20D/min。

869实际r31，石墨input6、硫酸input0、石墨烯output0、ratio1/idle；872 pick869/insert871、filter=null，Picking且held0；871 default30grids/bans0、全storage rows没有1123。`869.slot3→872.slot1`与`872.slot0→871.slot0`互逆。r31只输出1123，空filter本身不是错配；当帧缺酸与此前独立短窗非零产出不冲突，不把瞬时空buffer升级成持续断供根因。没有补无必要filter、改旧连接或扩产。

root直接核十原回执、六实体角色及四组互逆边、session/player闭合与Journal，保护证明`action-e5400064cacf4f21852b39af3f19aad8-0001-warper-two-input-sources-independent-audit.json`，SHA-256 `933F3931535C0A8D532E38C92EFA572D336BCABEBBE6A9016B9F6CA7AD87FC51`，0额外Game calls。source HEAD仍c9f34f3 / installed3fe；所有现场资格均仅用于缩减整案未知，不发可重用token、不给整案executable=true。Cu占位精确诊断待获准冷更新，Fe/D/container/graphite及1127中间路的方向/交叉/准确材料/覆盖仍待；不新增永久实体，不因Host退出关闭游戏。本条并入同Gate阶段证据，无新commit或发布。

### 铁地面整路拒绝与复用高架合接能力

固定1511.slot0至5326.slot7的地面整路只做一次prepare，任务在发送前收敛为最多8请求，不再包含端点子预览。run `6603f1fef6f44794a4db07aad45db4e0`：`:2/:3`为session/player，`:4/:5`为两个真实设备，`:6`为固定请求，`:7`为prepare响应，`:8–10`为session/player/Journal收尾。fresh两端所选slot均空且位置符合批准参数，player hash取`:3`；响应`BUILD_LOCATION_INVALID / belt_path_existing_overlap`明确NEW point23撞既有object4441，result=null/零创建。源码`TryValidateBeltBuild`先执行NEW带占位守卫，后才建立isolated path checker调用`CheckFullPathConditions`，故这是**pre-native拒绝**，不能记成已运行或通过native stage1。保留4441，不微移重试地面候选、不省略端点绑定、不改旧路。

8次Bridge请求执行1.83秒；opening85665424→closing85665477，同session/R1/save85236882/healthy；player位置/完整inventory/手搓队列/in-hand不变，Walk/speed0/full core。fresh Journal tick85665490，97条/durable97/pendingfalse/errornull，与`af2e99a…:11`完全相同。root独立核11个原件及精确候选，保护证明`action-d5589c5166df4f82a373ca446831f6a0-0001-warper-iron-route-independent-audit.json`，SHA-256 `4A5511771278A9BBA0DDEB752EB67ECEA57F14AA7A72252331F0D95AB83FE7CB`，0额外Game calls。0endpoint sub-preview/commit/新accepted/unknown/施工/save/load/close/deploy；external3/10/lifetime63不变。1.83秒不是委派到结果的墙钟，准备/model/provider用量未知。

root复用已实现的`BeltSourceReusePolicy`、`BeltDestinationReusePolicy`与普通`native_grid`同层双cover路径，修正铜四段草案：最终A/D坡道可在**整案批准后**保持空路径，再以真实A尾/D首ID、fresh hash作水平合接，核实体/材料/双向连接；合接完成后才接源仓26与目的884。历史[2026-09-18 elevated join](../../research/game-api-foundry.md#local-live-elevated-join-addendum-2026-09-18)证明这项能力曾实机走正常成本/无人机/保存，但不是当前29104或该新位置的正例。旧B/C只保留几何草案，取消三个分拣器中继假设；Cu仍2个、整链暂定20个，条件功率算术仍余18200 J/t=1.092MW，不宣称实际覆盖或提速。每笔私有计划仍限64 NEW点、全链≤571带；合接若超过64须在施工批准前重设计有界空路径分段，不偷偷扩大预算。未来真实端点尚不存在、native跨接/材料未证，整案false、不建资格stub、不伪造ID或预存token。游戏保持运行，c9诊断不自动冷部署；本条仍并入同Gate证据，不新增只读里程碑commit。

### 铁完整高架草案的三份native正例（仍未施工）

root先以既有1511.slot0/5326.slot7端点姿态固定一份完整草案：源端地面→3层坡道A、3层→地面目的坡道D、两坡道之间3→3跨越H；地面分拣器保留合法角度，不全部抬高、改4441或微移重放原地面路线。writer只换现场hash并连续完成三个既有prepare，run `dde89ffe366946e3b625f739198d3381`：`:7` A12点/source1511.slot0/filter1101/2011×1/turn2，`:10` D12点/destination5326.slot7/filter1101/2011×1/turn0；双方native performed/passed=true、Ok/span2、blockers空，prepared/commitAllowedNow=false。`:13` H30点，ordinary prepare success/prepared/commitAllowedNow=true、full_path_stage1/native_elevated_grid3→3、budget2001×30；原返回first/last精确等于A.last/D.first。没有消费或打印H token，没有commit，false整案的执行权限不因普通prepare可提交而改变。

原摘要`:17`把H写为`nativeFailure`/“span exceeds30m”，这是**调用方计量/分类错误**：实际snapped端点直线27.516514m，折线总长37.168733m；`BeltElevationPolicy.ValidEndpoints`限制前者1.5–30m，`CompleteNativePath`另限4–64完整点、native spacing/层/flat端，不限折线总长30m。A/D实际端点距离各14.5329m，原摘要14.9985/14.9231m也其实是折线长度，不能混为端点span。root直接核原`:13`及代码纠正结论，不改原摘要、重发prepare或放宽任何门禁。本包native拒绝0、caller分类错1。

12个Bridge请求执行2.29秒，opening85765569→closing85765652，same R1/save85236882/owned/healthy；player位置/inventory/手搓队列/in-hand不变且Walk/speed0/full core。Journal采集85765663、97条/durable97/pendingfalse/errornull，与`6603f1f…:10`逐字相同。root直接核17原文件、固定请求/fresh hashes/两个free slots/native附件/路径budget及两端重合，保护证明`action-b38ffb5c9f8b4b519bb5bc20ba620fec-0001-warper-iron-elevated-independent-audit.json`，SHA-256 `9E431FA737ED27C98C42867B3BF7C35051B1A84919838F0D6E5246BD2D555449`，0额外Game calls。external3/10/lifetime63，无新accepted/unknown/在途/施工/save/load/close/deploy，DSP保持原进程。2.29秒只为执行，不把准备/model/取证/Git/provider总量写成已计量。

预测实际A/D保留24带、H重用两旧cover后约28 NEW，总52带/2分拣器；这是预测，不是未来真实ID合接的精确材料账或整链通过。最终施工仍禁用：必须先整案资格批准，保持A/D空且未接料，以实际端点ID/hash fresh做既有普通native_grid同层双cover合接，再接ground sorter并核货物。三份free site预检不证明未来cover join、持续供料或1210产出，其他Cu/输入物流、精确材料与覆盖仍待。包内指南和合成回归补充端点span/折线及caller错误分类，离线通过不充作该future join实机证明；同Gate证据不拆成三个小commit。

该最小防误判改动只涉及`BeltElevationPolicyTests`一例、`NativeGridGuidanceTests`一例及playbook一段；Plugin/原生政策/权限不变。离线gpt-6-luna/max用当前pwsh跑Core专项59/59（测试329ms、命令19.608s）与MCP包内guide专项8/8（230ms、命令14.270s），root直接核两日志末尾及三文件diff，`git diff --check`通过。日志basename为`c9f34f3-20261002-belt-elevation-policy-release-filtered.log`与`c9f34f3-20261002-native-grid-guidance-mcp-release-filtered.log`，留既有ignored test-output。流式Claude窄范围外审终态APPROVE（session`cc5dca69-e371-4b00-96ae-f50efd30a400`），unrecognized_model未触发重试/配置修改；审查备注合成L形样本不覆盖坡道单调半径，既有坡度测试与真实A/D回执是不同证据，不能互相冒充。指南/测试仍为未提交源码，不宣称已冷部署、当前live MCP guide已更新或新CI通过；本Gate统一证据/提交门不变。

### 1127至Lens消费端优先的完整草案资格（未施工）

固定5326.slot8→5333.slot11，保留5326.slot7铁输入、5333.slot10金刚石输入及slot1透镜输出；先核受约束的消费端D，再源端A、跨越H。run `d97c96b59cb4438fa295cefcc1ef17ae`原`:4`是5333、`:5`是5326；`:7/:10`为12/12点坡道，native performed/passed=true、Ok/span2、filter1127/2011×1，NEW turns1/0。`:13`为29点H、full_path_stage1、ordinary prepared/commitAllowedNow=true，无commit/token使用。D/A/H端点直线分别14.5008/14.5329/26.3831m，返回A.last/H.first、D.first/H.last精确重合；三份独立预检仍不证明未来实际ID的普通native_grid合接。

root首次离线审计沿用source-first回执序号，误报“Selected slot invalid”；按原件actual objectId修正，不重发任何游戏调用，也不更改原回执。独立审计核17文件、批准参数/fresh player和endpoint hash、所选空槽/姿态、原生附件/层/材料budget及玩家/J闭合，proof `action-77d2fc8df371462d9dc79867351c6b67-0001-warper-strange-lens-independent-audit.json`，SHA-256 `AE3E73B3EA26340A2516031F2C83F57A13A6667148B199968E240A03BA0783AE`，0额外Game calls。三条路线全部NEW带中心与铁草案的最小距离1.254313m，仅排除该离线中心重合，不代替联合native collider、分拣器交叉或施工后实际ID合接检查。

12次Bridge请求执行2.15秒，opening85858796→closing85858874/R1/save85236882/owned/healthy；Journal采集85858888、97条/durable97、pendingfalse/errornull，与`dde89ff…:16`逐字相同。player/hash、inventory/count/inc、手搓队列、in-hand/fuel均未变化，Walk/speed0。external3/10/lifetime63，0accepted/unknown/在途/施工/save/load/close/deploy；游戏保持运行。51带/2分拣器仅未来两cover重用预测，精确实际账、全链供料/电力、1210非零仍未证明。准备/模型/provider用量未知，2.15秒不冒充端到端提速；原生拒绝0，额外调用方离线审计序号错误1。本Gate只更新同一阶段证据，不拆只读commit。

### Graph消费端拒绝、现有障碍和Cu完整重设计的边界

Graph870.slot9→5334.slot7的第一固定整路草案，只送消费端D，run`e6fb72a6289243e7b4174a1c97cab2e3:7`返回`BUILD_LOCATION_INVALID / belt_path_existing_overlap`：NEW点9撞既有2459，result=null、未创建对象。这是NEW带中心间距的**pre-native**守卫，不是native full-path或sorter已运行；A/H未发送。8请求/1.67秒，closing85918460/R1/save85236882，Journal85918474/J97持续durable，player/hash/inventory不变。root核11原件，proof`a0e04bc8faee422dae38fdd72672c153:1`，SHA-256`A6D438D9201509BD8EF2A1D75F7DD28044D10DD3DA2B568AF0DE7552D19B87E9`。一次离线审计误用PowerShell保留变量Error，改变量后核同一原件，无游戏重试。

限定只读定位run`8ddfa61f3211498997cfa19b778ad24e`为10请求/1.93秒，`:4–8` actual IDs分别2459/517/26/3084/3966，未额外追踪或prepare。2459为正常输Cu矿1002的旧带，位置(-116.645432,2.51572132,-162.6885)，2457→2459→2461保留；517是旧6002运输带，不是已证明的Cu端点占位根因。26实际free仅7/8/9，slot9向West、South8向South，两者同corner位置不同方向；全部East槽已有旧连接，不能假定free。3084/3966均r58、working=true/net3/ratio1，oil输入0/2、H2输入2/4、graphite输出均0；这是瞬时工作/buffer事实，不证明持续余量或持久缺料。closing85928616/J采集85928628/J97连续，root核12原件，proof`3b28a93b78d743bab9e4d7fd767d3253:1`，SHA-256`EF01CE77B20E2EEA7C1257B6AF3F2EBE5576CC710859DAFD0F9884C6B21C3CD5`。

root据此将Cu整路从失败South8改为已观察的West9，源坡道/跨越方向整体改变90°，两坡道16.7m/三个site chord均≤30m，仅2sorter，不扩旧厂或省略绑定。run`8e24dcacbe1d4231b024fcdf2656e350:7`源A生成12点/full_path_stage1/native_elevated_grid0→3，但sorter preview performed/passed=false、span0/not_checked、generic`planned_endpoint_collision_or_prototype_unavailable`。这不是sorter原生几何正例；D/H未发送。与旧`fc9b9c…:4`为两个不同出口同类pre-native拒绝，停止26附近相似候选，不试第三个、不给517强行定责。8请求/1.62秒，closing85959844/R1/save85236882，Journal85959859/J97连续，0写/unknown/在途；root核11原件，proof`29d047adbad54d85b33f54a920006e72:1`，SHA-256`6653D2F4E096B72D43EDF5CDF536B18853F1723A388024A88D7E1626811CE6DF`。c9精确sorter占位诊断尚未冷部署；不因Host生命周期自动关闭游戏或复用3fe已消费的维护授权。

Graph第二完整profile草案保留地面消费端/角度与旧2459，改变D下降起点至16.7m；H全跨度31.6601m，不能放宽30m，分两份固定连续只读site预览，各15.842m。这仍不是批准永久分段或未来真实ID合接通过。原caller写死三段，Luna按root停止条件在任何Bridge调用之前交回；必要共享调用方修复与离线/实机边界单独见[bounded caller](bounded-belt-site-caller.md)。截至本记录，第二Graph预检未执行，所有新实体/accepted/save/load/close/deploy均0；同一Gate保留单份阶段evidence，不拆材料或只读里程碑。

### 2026-10-03：共享caller首个实机包，消费端通过、源端止于3742

仍为原DSP进程、installed3fe/DSP29104，不关闭/重载。共享caller源码SHA-256`546510477E877E1CF6A128971A8003C4FF778081B0F031C10671BF9A8700A1B1`；执行时HEAD为c9f34f3，该必要外部脚本修复随后单独提交为`54a99ce`，不把此源码提交写成Plugin已部署。离线40/26/27检查及流式外审APPROVE见上链接。只读实机与外审并行，未为等Git/CI中断游戏。

run`6b9ddced400f48e9a5adbff3e189e87f`：原`:3/:5/:7`为session/870/5334，`:9/:11`为fresh player/D prepare。D返回12点、3→0层/full_path_stage1，5334.slot7/NEW turn0、filter1109/2011×1、native performed/passed=true、Ok/span2，端点chord14.5329m；该消费端正例保留。原`:14/:16`为fresh player/A prepare；A仅一次报`BUILD_LOCATION_INVALID / belt_path_existing_overlap`，NEW点0撞3742，result=null/未创建对象。这是原生调用前的NEW中心守卫，不能记A full-path或sorter原生已执行。H1/H2未发送；不重放失败点，不以caller异常改判动作accepted。

10个Bridge请求/2.041545秒，首个prepare在入口后943.614ms；caller错误0、Bridge放置拒绝1（pre-native），没有commit/扣料/save/close/resume/deploy。收尾`:18/:20/:22`核same R1/save85236882、session tick86114177、Journal tick86114193/J97全部durable/pendingfalse/errornull，player hash与inventory不变，external3/10/lifetime63保持。摘要`:23` SHA-256`A5650A40B409A43FBA066582A1F85FED7CB5E768E3B4A8D0429C9EF2CD445950`；provider用量与完整委派/准备总耗时未知，2.04秒不冒充总工序提速。

root直接核23原件及逐文件SHA、实际ID/空槽/native附件/材料/层、唯一错误、无H重试、session/player与97条Journal连续，保护证明`4b42cac358bc4921b402475138e50f9a:1`，SHA-256`C54CDA313A81F08E79539D9B124E29A03C815C427CA60EECEDBCC3BE5B1A29A3`，0额外Game calls。整案仍false；下一只读定位3742，保留旧实体及已通过D。未来实际ID合接、供给/覆盖/精确预算和1210产出均未证明。本条并入同Gate单份阶段证据，不另造只读commit。

限定3742诊断`06ad2081d244492e897d2465265c7edc:2–7`为6请求/1.25秒，`:4`为item2001旧带、位置(-110.79213,-35.0396156,-163.025665)。raw方向为3743.slot0→3742.slot1、3742.slot0→3741.slot1；执行者最终短摘要曾颠倒邻居，以原件isOutput为准，无游戏重采。其本段cargo当帧为空，不证明邻居用途、整路闲置或允许cover/拆除。closing86133695、J86133709/97durable、R1/save85236882及player/inventory不变；root核8原件，proof`12638c438bc843c288eb3f643b949903:1`/SHA-256`198638B8DBED8199ADC39E2C827E65329C6A90B11DAC811AB6BBEC9CFC5AA881`，0额外Game calls。

root据此将**整个源出口**转为fresh已知870 East4，保留South6并行Graph输入资格和旧3742，改沿East球面切线的完整A/H1/H2/D草案；消费端D保持已过的相同轮廓。源A是最难未知，先A，仅前三个site成功才重验原D，避免失败时重复已过端。共享caller未改，不手拼DTO/新executor。run`f91a0210b31542b9868185f2f5411be0`唯一A response`:11`报NEW点1/object1584的同类`BUILD_LOCATION_INVALID / belt_path_existing_overlap`，result=null/未创建对象；H1/H2和D均未发送。两种源出口均被前置中心守卫拒绝，停止第三个附近候选，不微移、去掉绑定或继续seed。1584角色未额外核，不凭错误消息补造实体用途；下一步须完整路线重设计。

该包8请求/1.769673秒，首prepare入口后1002ms；`:13/:15/:17`关闭观察86158669、Journal86158687/97durable、R1/save85236882及player/hash/inventory不变，external3/10/lifetime63不变。caller错误0、Bridge pre-native放置拒绝1、0写/unknown/在途；摘要`:18` SHA-256`C935A6F63D032D801448CD4C0443F21AD0E32474348E053C9FCDC536FAABA068`。root直接核18原件和exact source4/hash/filter请求、单一错误、无后续span及J连续，proof`3cd7cdb3b0c04ea5b48270ee45ab1c0d:1`/SHA-256`42A48BB0F968BD5232FBE39D40173AC32017A1E0D4FA746F7CF8222A7E07B987`，0额外Game calls。执行者一次SHA短摘要漏字已读取同一原文件校正，没有重复Bridge调用。

必要caller修复`54a99ce9aeb34d7a26905f41b6d3ccd63f09bd93`仅四获批文件，远端main精确匹配，CI [37031102141](https://github.com/AvaloNero/Spherewright/actions/runs/37031102141)最终completed/success，root独立API复核；未重新dispatch或混入阶段dirty。上述只读包仍未证明整链、未来actual-ID join/精确账/持续余量或1210产出。阶段记录留本地合并Gate证据；当前快照覆盖到86158669，旧历史事实仍在本页。游戏继续原进程，不因Host/回合结束关闭、保存、恢复或清零写窗。

### 2026-10-03：完整输入资格收敛，游戏保持运行

仍为同一 owned live session/R1、normal-save85236882、installed3fe31d1/DSP29104、external3/10/lifetime63，整案false。必要共享caller虚拟belt端口修复已单独推送 `a21015f644ce5c8c6c983fad48b63d1116d7d9cd`，69 fixture/26 stage/AST及流式CC终态通过；root核准确SHA的 [CI37040289500](https://github.com/AvaloNero/Spherewright/actions/runs/37040289500) completed/success。复用外部caller不需部署，c9精确占位诊断仍未安装。完整代码/离线边界见[同一caller证据](bounded-belt-site-caller.md)。

煤制石墨源5187/r17由5171六矿点经5172–5186/5188供煤，保留5196及旧热电分支；新5187.slot4→5334.slot7的 `2a552a5deef3408f8403dca62adbc4a7` 四段12/23/23/12点通过，67唯一点仅预测，root `d36211ac3a2840d4abf0ef9dd4cdbb67:2` 独立证明。重氢 `310f1006b9ec46b6940e7a27b4eedb6a` 为3074.slot2→5326.slot4，20/30/31/20点，98唯一点仅预测；root `4e213e4c8286467b8f42021f0daa77df:4` 核34原件。两条仅site正例，未建、未证未来ID合接或持续分配；原失败870出口不再尝试。

随后以下五包统一独立核验，没有游戏写入：

| run / 原回执 | 结果 | 请求 / caller墙钟 |
|---|---|---:|
| 24b8d4719dc54381a27355dabb6ef2d2:2–17 | runtime目录9配方及2440/827/814/3467/869/883/3074/3075/3347/3064只读核查 | 16 / 2.686s |
| 477f0c72fe564abe8ee12919807fb05b:11 | Cu10 South8源A generic占位、native未执行，停止余段 | 8 / 1.548s |
| c3f165073fa74df1b3733668a4c9fefe:11/16/21/26 | 871 South8→884 West10四段通过；D/A/H1/H2=13/12/20/21点，63唯一点仅预测 | 14 / 2.620s |
| 19ec14d188b94f36a4eadd0a70a18cd1:11/16/21/26 | 885 North2→5326 South6整体2层profile通过；H1/H2/A/D=31/30/21/22点，101唯一点仅预测 | 14 / 2.622s |
| 65128933b6cf453e8162862460cb0257:11 | Cu10 North2第二完整源出口同类generic占位、native未执行；停止第三出口 | 8 / 1.531s |

两个Cu10拒绝均 `planned_endpoint_collision_or_prototype_unavailable`，不是native sorter几何已验证；884端与其H段未送，不能将原型缺失/碰撞合并错误强归因某实体。此前2391两个方案分别OutOfReach和source TooSkew，停止第三虚拟带出口；Cu26两次失败保留。未拆旧路、Move、取材、手搓或无绑定预检。Graphene旧North2消费端被1404拒绝后改为完整West10入场；Container旧3层H1被5047拒绝后整体改2层，并先查最难跨接，未微移重放。

源目录原件确认2440真实采8矿点、铜矿buffer49/working/ratio1；不是历史9点候选。827有3000涡轮、814输出10/idle/ratio1，满仓与buffer事实不等于未读828的因果证明。869/r31当帧Graph2/acid2/output0，883/r99涡轮4/铜4/Graphene0/container0：当前缺石墨烯，不能拿有限旧库存代替完整自动输入。r58为精炼油1+氢2→石墨1+氢3，**净氢+1**；r16+两次r58的净计量为2原油→3氢+2石墨，但不等于现场可分配持续速率。3073实际2310/r40对撞机，不能称分馏器；3064循环回路本身不是外供。

root独立证明 `ae929045d94945a1bd6a2411bb9faeed:1`，SHA-256 `A720B5780E0DC7723811B96B58EC383EBDF4E256B88545FA63E93DAA8ADB20A3`：直接核122原件/60响应和逐文件SHA、固定参数/实际端口/hash/filter/方向/预算、A→H1→H2→D语义连续性、closing player完整库存及97条Journal与434c208:4一致；0额外Game calls，最终校验0.738s。源码caller错误0；root离线审计准备有本地地址、null字段及StrictMode缺key错误，校正后只核同一原件，没有游戏重采。总11.007s仅五个Bridge包执行，未计完整设计/委派/审计准备/文档/Git墙钟，provider usage unknown，不宣称端到端同比提速。

最新S观察86396228、J采集86396233/durable97/pendingfalse/errornull，最近正常保存仍85236882；新增accepted0、commit/action/unknown/未保存新施工均0。DSP原进程保持运行；Codex断线、关闭、重启、压缩或本轮结束均不触发save/close/load/deploy。真实ID合接、铜路线、精确材料与联合碰撞/覆盖/持续分配仍未过，暂定571新带/20sorter不是精确账。已过分段路线445带合计也只是去重预测，不是材料许可。全链1210输出/save/restart/再输出仍未验收；保留本Gate单份本地阶段证据，不把只读包另拆commit。

### 2026-10-03：联合布局冲突核销，保持同一游戏进程

root复用八条实际返回的native点集，按每路≤0.02m去重、`BeltBuildOccupancyPolicy`的0.25m中心门离线两两投影。原方案发现25对：Fe/coal20、Fe/container5，数处距离0；proof `8200463658fa4faaa08186e72f446be7:1`，SHA-256 `6F9612123AD6567AB8D964870A2428E7848323E278B58391F6F958B931B10944`。各自site通过不意味着可相继建造：照这些中心建成后，后续路线会触发已有中心守卫。未以这些草案施工、创建永久资格stub或伪造未来ID。

| 固定实验 / 原回执 | 结果与停止边界 | 请求 / caller墙钟 |
|---|---|---:|
| 3b30b58ba4f64ed194f738ec2bfbf739:11/16/21/26 | 全1层coal的A/H1/H2通过，D在点6撞既有2459；停止、不重放D | 14 / 2.585s |
| 4b5beafccf84478c8a7db3c6f616d3c2:11/16/21/26 | 保留低层A/H1与原高层D，H2改1→3；四段12/23/23/12点通过 | 14 / 2.531s |
| 44ce8820d77f4c70985747556fab280c:11 | 容器整路改West5，长弯D被`belt_elevation_native_path_incomplete`拒绝；其余未发 | 8 / 1.556s |
| aa0bae1c1e924d8281e6a1e2ae479485:11/16/21/26/31 | 保留West5，完整改直坡与三跨越；D/H1/H2/H3/A=13/33/33/33/24点通过 | 16 / 3.022s |

容器使用885.slot2→5326.slot5、filter1206，保留Fe.slot7/D.slot4/1127.slot8；煤石墨使用5187.slot4→5334.slot7、filter1109，旧煤/铜/热电链均保留。所有新site为2001/full_path_stage1、原native层0..3/≤30m chord/≤64 NEW，两端2011、native performed/passed、Ok/span2及精确hash/slot/方向/预算；普通H token脱敏、不用。第五段只需共享外部caller的必要小修复，非新Plugin工具或自动规划器，代码/80+26离线/外审原结论和root核销见[caller证据](bounded-belt-site-caller.md)。该修复单独推送`81a670aaa740d50d5910d81f503cc30c23cc8b16`，root复核 [CI37052070990](https://github.com/AvaloNero/Spherewright/actions/runs/37052070990) completed/success；只含三批准路径、原7dirty保留，不等于Plugin/MCP部署。

root直接核前三run85件/36响应，proof `cf289a442aa243119cc309f28f89ed64:1`，SHA-256 `49EBB6E5DA86FA4D7618B4300449B512388B14492B193096E03B79DB9123A49B`；五段另核39件/16响应，proof `cc5131bda0bc4c44845e9b299e0e69a8:1`，SHA-256 `372CB261DEEEB3B76FEA6CDEC5D61E861B507DB2A3FAFC09D228E9BD34015F34`。核固定参数、原错误/首次停、逐文件SHA、fresh session/player/端点、native层/材料/附件、语义A→H1→H2→H3→D接缝和完整97条Journal连续；0额外Game calls。root本地几何计算一次PowerShell除法/数组表达式错误已修正，未发游戏候选或改原回执，不把错误计算0值当路径资格。

修改后八路共476唯一NEW中心仅预测，联合≤0.25m命中为0；Fe/coal最小2.666666m，D/container最小1.257892m。最终投影proof `6987793e6f2842d4b78822ef824c7b6b:1`，SHA-256 `81D252DED6A4104DE6D77B702E16C86227C6C878141CD9543F58632E22000101`；中间仅coal修正版为263f70…:1。该结果只排除中心重合，不是联合native OBB、未来实际ID合接、精确物料账或供电覆盖正例；571上限余95给未通过Cu与合接差额，不准据此取材/施工。

随后唯一fresh源/功率包 `5ef3746b3e034491bd4db1729563d267:2–21`，20请求、shell7.7s：Fe1511/Turbo827各3000库存、Graphene871有1438；其上游1500/814/869均当帧idle且serve1。883/r99缺Graphene、885空；3073/r40氢input1/outputD4、3074空；5326/r104、5334/r60、5333/r101、5329/r78均0输入/输出、idle/serve1，只有既建5329输出连5364。N3当帧capacity1894000、required/served223656 J/t、ratio1；N4 capacity10000、required1800。不是完整当前基础负载储备、持续燃料或可分配产量证明。root核22件/20响应及97 entries，proof `6e81289a791d4467a4cdd971d72f9e16:1`，SHA-256 `FE47975B579F9F273C269A7F948720614B3F547165D4A29F34EA531B6C7E7EEC`，0额外Game calls。

最新S **86454866**、J采集 **86454870**，R1/save85236882/J97durable/pendingfalse/errornull。外部3/10、lifetime63不变，0accepted/commit/unknown/在途/新施工/取材/craft/save/close/load/deploy，DSP仍原PID22052。离线a210 Release完整构建19.33s/0警告错误、自包含MCP私有stage224文件/64tools/1resource/guide一致/exit0，日志`20261003-offline-readiness-summary.log`及同批build/publish/probe仅为离线就绪，不是当前安装或最终ZIP/发布。四路线包52请求/9.694s只计caller执行，不计root设计、Luna准备、审计、331.676s外审、Git/CI总墙钟；provider用量unknown。铜两出口generic占位、未来ID合接、实际供给/覆盖、1210非零/save/restart/再输出仍未过；整案false，禁止新增施工。Codex/Host生命周期不关闭游戏；必要诊断冷更新尚未获新的明确授权，不自动执行。仍并入本Gate唯一阶段证据，不拆只读里程碑commit。

### 2026-10-03：保留原进程，重氢源诊断与有限短窗

main81a670a/installed3fe31d1、同一owned/R1/normal-save85236882、external3/10/lifetime63保持；整案仍false。未重验已通过八路、未施工/Move/取材/手搓/save/close/load/deploy，无新action、unknown或accepted。原DSP PID22052保持运行；Codex关闭、重启、断开或回合结束不触发游戏退出。

一次源包 `98d6efdd78ba465cac16ca14c0faa1a2`：32请求、caller3.648s；S/P/power`:2–4`，固定26实体`:5–30`，闭包S/P/J`:31–33`，摘要`:34`。2802采油、3964/r16与多台r58存在working正例；3073/r40当时氢input2、Dout0/idle，3403/r41有D19/idle。runtime catalog `24b8d4719dc54381a27355dabb6ef2d2:4`证明起批分别需10氢/20D，故仅能解释当帧不足，不能证明永久停机或持续余量。root核34原件/32响应及原97条J，proof `243c5b39efa146aeb39fb63728e3e22b:1`，SHA-256 `FA870A45942E7FE8E1BE4D5DA7996DFB0AFEB39C7AB3D203C6436AEAEA10BC28`，0额外Game calls。

复用6477dc…同一54页/5365实体不可变图，反向真实连接确认3074的3347氢入口来自3964/r16、3083/3084/3965/3966/r58及同主干储仓3064/3348；3064不是独立生产源。3073经3076向3074回D，3074经3405向旧燃料3403分配D；保留所有原连接。26个fresh端点的身份/姿态/配方/方向/过滤/仓配置与旧图字段一致，但中间带未全量fresh读取，完整长路径仍是历史静态证据。root追踪proof `461cbee48b85496db0311f2123e8cac6:1`，SHA-256 `C1B882EC8B7E35B8E7ADE337767598BA83DB301CB586AC18F78DC73C45D00A7E`，0Game calls；不将其写成整链native通过。

随后直接复用既有 `Invoke-SpherewrightProductionExperiment`，run `e58757fdd97b4c70ac755f89716c29ff`，预声明固定6实体/4物品、3个independent600-tick窗、606-tick间隔、总≤90请求/180秒；内部86/175为边界P/J留预算，循环内0模型决策。原生产回执`:21/:38/:55`，样本证据`:22/:39/:56`，终态`:58`，收尾S/P/J`:57/:59/:60`，阶段摘要`:61`。总54请求/111.994s；sampler50请求/109.924s，其中读4.283s、程序等待105s。仅是调用执行墙钟，不含委派/准备/独立核验/文档/Git；provider用量unknown。

| 原600-tick窗 | 石墨1109 P/C件 | 精炼油1114 P/C件 | 氢1120 P/C件 | 重氢1121 P/C件（另列件/分） |
|---|---:|---:|---:|---|
| 86486659–86487258 | 7/11 | 10/13 | 26/26 | 5/0（30/0） |
| 86487300–86487899 | 6/5 | 8/5 | 22/20 | 0/0（0/0） |
| 86487947–86488546 | 5/8 | 8/12 | 19/23 | 5/20（30/120） |

间隔41/47ticks，连续信用0；其他每分钟值为件数×6，不把count误标成rate，不把样本净耗或一次20D扣料当持续上限/消费率。当前local1121的directProducerCount/diagnosed均1；其他全厂物品有多个producer、氢P/C含r58循环，不据此归因到单设备或宣布可分配供给通过。采样中3073由idle变working、3403末窗working；3074三次是已观察空仓（buffers空数组），D可同时在分拣器及机器buffer中。3965三次石墨output20、idle，是进一步沿旧输出只读诊断的线索，未证明唯一阻塞根因；功率比均1只是采样健康条件。

root核61原件/54响应、完整97条J、固定窗口/页面/单位/功率及明确静态投影，proof `36f1c10c7f7040bbaa06c4e12bb4f6c5:1`，SHA-256 `A0E3B1C0E9885797FFAF4083DD039E7C6345A1B9BC6D5C63F834C03D301D748B`，0额外Game calls。核验初始本地解析、count/rate标签及configurationStateHash当静态指纹的错误已纠正，没有重跑游戏实验或修改原回执。现有FactoryConfiguration包含动态buffers；只读生产比较将身份/姿态/配方/连接/过滤/仓配置逐字段核对，动态buffer独立保留分析，实际写前仍用完整fresh原生hash。现有仓计数helper及empty/null离线fixture已覆盖空数组=0、null/缺失=unknown，不重建功能；belt/tank不能套用仓语义。

最终S **86488548**、J **86488551**、R1/save85236882/J97durable/pendingfalse/errornull，player/hash/inventory不变，external3/10/lifetime63不变，无未决结果。两位Luna均已完成本次有限任务，不持有在途动作；原游戏继续运行。铜占位的精确诊断仍未冷安装，且新的必要维护授权尚未收到；不关闭游戏代替等待授权，不做第三个相似铜候选。1210启动、save/restart/复产、供给余量和完整预算仍未证明；本轮事实留在同Gate阶段证据，未拆commit、未tag/release/发布。

离线交付Luna只跑本机pwsh7直接相关fixtures：stage-tools26检查通过（另storage28/material41、0Game calls）、production-sampling27通过（0Game calls），分别2.84s/2.73s，日志`20261003-stage-tools-latest.log`/`20261003-production-sampling-latest.log`。JSON可解析且最新S/J/写窗/整案false与快照一致；两新审计原件定向SHA匹配、引用唯一，新增行无敏感命中，diff-check通过。没有脚本代码或兼容性改动，不重复5.1/完整solution/CI。main81a670a、index空、原7项dirty保留；只读诊断不另拆阶段commit，现有保存恢复/发布门未核销。

### 2026-10-03：铜换用562既有分支，五段site通过，联合规划仍受阻

用户再次明确Codex关闭/重启不得连带关闭DSP；根规则、包内playbook与当前快照均保留此边界，session entry短引用同一重连规则。代码核查MCP入口仅RunAsync、Plugin host Dispose仅释放自身会话/日志/pipe/dispatcher，未发现Host退出触发游戏关闭的路径；不是声称已做真实Codex关闭测试。DSP仍原PID22052/启动时间2026-10-02T19:41:51+08、Responding=true，没有save/close/load/deploy，不使用尚未批准的新维护流程或已消费的3fe一次授权。

不再重试10/26/2391/2396失败出口。复用既有完整静态图发现不同仓储分支562，fresh固定源包`fc2e4817c9384361af3a507d1c9f5f1f`的13响应`:2–14`（15为摘要）证明26→551/filter1104→542/539/540/538/541/543–550/309/307/308/310/306/305/304/303/302/301/300→563→562；长中间链来自历史完整图，fresh选读10/26/551/300/563/562/884。300当帧Cu2、551/563各heldCu1，562库存Cu1900/magnet1931，入口4←563保留；源选空South8/filter1104，终端884保留GrapheneWest10与TurboNorth1及旧4/5/6输出，Cu选West11。库存/当帧货物不证明持续可分配余量。root核15原件/13响应及完整J，proof`cc87cf482e764d62973fac835e1c166e:1`，SHA-256`2DC9006C9A46303C026D3873382A02C767F55EA2D937C1FE774FD3A8B4F9E508`。

首整路`e96c97f270eb46408239595ea6fc6a60`：10响应/1.813s，源A`:11`12点、562.slot8/2011/filter1104/nativeOk/span2；East3消费D`:16`被planned point7/object2413原生拒绝，H未执行。root核23原件，proof`18811872850d482a972f76e19eb33e37:1`，SHA-256`77FB354D45D03EA773AEEAF25CD5C9CBD2201DC2B4CBD444FD2F1CE9A07AAC2C`。完整West11消费重设计`27e7b7a9006a4fb0b87c82c4f203a972`：12响应/2.386s，D`:10`13点、A`:15`12点均Ok/span2；H1`:20`被planned point13/object5013原生拒绝，H2未执行。root核27原件，proof`f8a7bbc431564a84a090ae29b9186776:1`，SHA-256`C75F9333D717A9955DAED0AB49F59CC6B5E6A9A971747C8676B67F689EE7AD8D`。均prepare-only、accepted0，不改判为已建或重发整个任务。

固定只读5013包`f967a393288040b8b155623926cb27a4:2–5`：4响应/1.161s，5013是三级带、radius204.200007、5014→5013→5012，当帧processor1303×3；虚拟slot未知不当free。root核6原件，proof`59ee884c787f4de289d6b54ff98075c4:1`，SHA-256`4D836C3847F3B8538CE47DF73C033EC7E79C318C08BFDB2E85176275A8A4C31E`。保留旧线和两端正例，固定完整跨接改为H1 3→2 / H2 2→2 / H3 2→3，不再附近盲试。

最终五段`e07cfd3662fa4a9b98f470b874e5c85c`：16响应/3.283s，入口到首prepare1.067s；H1/H2/H3/A/D原`:10/15/20/25/30`分别11/18/28/12/13点，full_path_stage1/native_elevated_grid全部通过，四个native seam差为0。A562.slot8及D884.slot11为tokenless非执行endpoint-preview、Ok/span2；三个free span普通prepare可执行，但本Gate整案false，因此没有commit，任何token均未使用。root核38原件及逐文件SHA、全部方法/端点/层/材料/连接预览、玩家/背包闭合和原97条J，proof`adf91201667b4efc9bbccf636274ed0a:1`，SHA-256`E77F6EA55A82C8415A080FA7EF5FBF3DB8776BC63CBF2D9CBF87BF8881D3068A`，0额外Game calls。未来实际ID连接仍须fresh native_grid非移除cover预检；不能把共享NEW seam的free-to-free坡道原样逐个commit造成重复端点，也不提前宣称已核实际材料/空路径/连接。

复用旧八路原回执，加入Cu五段native点作一次九路联合中心投影：Cu82 raw/78唯一中心预测，合计554；≤0.25m命中8，Graphene/Cu7、Container/Cu1。proof`8455e5d246c64cfd986d1b1e093d6efa:1`，SHA-256`7AA4DEFAE711BE9BE16679632B6F3C97F6D9F510356D3176838BB5CDA2DC428A`，0Game calls。该证据准确说明单线site正例不能充作整链联合正例；不放宽碰撞、不删除成功实体、不做第三个相似候选。下一只读工作是基于整链冲突的完整路线重设计，而非继续猜562/884端口或冷部署c9。554/571及20sorter仍仅预测，实际ID合接、联合native OBB、当前完整功率/覆盖/燃料和源可分配产率未证。

Luna两次PowerShell `false`字面量错误发生于Bridge调用前，另一次post-call index误把metadata当response；root本地glob/空response/脱敏token字段错误也已从同一原件核销，不以此重跑游戏。固定薄入口仅串现有共享qualifier与protected callback，不另写DTO/intent/index；zeroGame ValidateOnly通过，最后两次实际调用入口无上述错误。保护记录中的token字段连原empty也会脱敏，不能从脱敏值推原内容。五个包共55请求/caller10.955s；模型准备、独立审计、文档及provider用量未完整计量，不能据此声称端到端提速比例。

最新S86540076/J86540081、R1/normal-save85236882、97条/durable97/pendingfalse/errornull；external3/10/lifetime63不变，0施工/Move/转移/craft/save/close/load/deploy/accepted/在途/unknown/未保存新施工。main81a670a与既有dirty保留，整案executable=false；本轮普通只读事实并入同Gate，不拆新commit，不tag/release/发布，DSP继续运行。

### 2026-10-03：同层可执行对接设计收敛；Codex 生命周期保留游戏

DSP原进程仍Responding，源码81a670a/installed3fe31d1不变；MCP Program仅运行stdio host，Plugin host Dispose释放自己的会话/日志/pipe/dispatcher，未发现MCP退出关DSP路径。根规则、playbook与快照保留“Codex/Host退出重启不保存/关游戏/重载；健康重连fresh核身份/J/action/accepted后续接”。未做真实Codex关闭试验，没有本轮保存、关闭、加载、部署或施工，3fe一次维护授权已消费，不复用。

之前Cu e07五段虽site通过，中心投影有8重合；整个中间层与D改为1后52c523f1449442d88e8d93f245f16664 site通过、root7ae005ba938140a5a18d22627d75b8e6:1，九路62e159ab31f04773bad457bd12b93e66:1已0重合。但A3→H1降1仍不能按现有实际ID cover子集执行，因此该轮只作历史site正例。

root核当前BeltSourceReusePolicy/TrySeparateNewPoints和BeltDestinationReusePolicy：所有cover点必须与实际锚点同半径≤0.05m；native_elevated_grid只准free-to-free。保留端点、把Cu A末层也改1；不放宽策略、不新增原语，不重复提交共享NEW点。当前固定Cu run **f8896e83127e4a78ab170df0701f41c0**：38原件/16响应/3.043s，A/D/H1/H2/H3原:10/15/20/25/30为12/13/11/18/28点，层0→1/1→0/1→1/1→1/1→1、source562.slot8/consumer884.slot11、2011/filter1104/nativeOk/span2、四seam差0。root **6ecb41f6d02e481e993810bde8112146:1**，SHA-256 **A0CEC0926F5E9AEC29C3E4975097D8D09BA316326FA81C6419292FEF7503225B**，核原J与背包、零额外Game调用。

raw核验修正旧摘要：D原310f1006…为A0→3/H13→3/H23→3/D3→0，Container原aa0bae…为A0→2/三H2→2/D2→0，内部本来同层，未重验或重做。仅Coal旧H2为1→3，不能充作混层actual-ID cover。保留A/D/H1、5187.slot4/5334.slot7/filter1109；把H2的新完整native请求终点选在已观察level3网格(-119.909668,-8.97829,-165.0415)，留约4m水平3→3 H3 gap。不是裁剪旧返回点/使用旧token，也不是第三次附近出口猜测。将来整案获准后A/D/H2先建空隔离free坡道，H1/H3再fresh同层双cover；每步真实ID、空路径、槽位/材料/连接仍待现场证明。

Coal run **b5dfbbe29aeb4f0393d8922fa9241902**：38原件/16响应/**3.258s**，A/D/H1/H2/H3原:10/15/20/25/30分别12/12/23/19/5点，full_path_stage1全通过、端点Ok/span2、四seam差0，首次失败和closureFailure均null；普通内部token未使用。root **11efa85885484138bb86c14de2d43478:1**，SHA-256 **EDE00FC3DA578EB2D5540DFB6301F3B48415CA487EEE1159C1173CFCFB1FB8F2**，核38逐件SHA、准确响应序列、源/端/层/预算与原97条J；不是实际ID合接通过。

复用其他七条原点、用当前Cu/Coal替换原方案，九路**554中心预测/≤0.25m命中0**；root **1fc50fcf2fa34eb2bceaa240c781eda3:1**，SHA-256 **239FBB83BB4518DEAC47F4FD174E3C6175997EE6990FD434DD74CD6808E40BAD**。Graphene/Cu最近中心0.6666906m；中心投影不是native联合OBB或精确NEW账。571仍仅规划上限，actual-ID cover成本未猜定。

当前源/功率固定包 **2d6558da03a54c98a754227cee00c0cc:2–38**：39原件/37响应/**5.381s**，30明确ID，无prepare/写入。原:5 get_foundry_plan有意在已建5334原点提1112/r60/4min单设备，site.blocked预期，只复用power组件：N3容量**1,894,000**、完整既有峰值保留需求**1,797,800**、export0 J/t，新增当前峰值前空余**96,200 J/t**；不是当帧idle。若未来20个2011按300J/t且真实覆盖N3，额外6000后余90200J/t=**5.412MW**；dummy炉未施工，已建核心负载不重复记。真实新sorter覆盖、持续燃料/源可分配余量仍未证。root **6e16b7c2a25c4e279d9c7f058f12b39e:1**，SHA-256 **74FE9FF4AE610ED1A59D354E72DD2C8900F6D80358AC9AB9506C02B41947398A**，核39原件/全部方法/准确30ID/session/tick/玩家/J闭合。

当帧Fe/Turbo各3000、562Cu1900/magnets1931、Graphene1856，仅有限库存。869/r31 graphite0、870 acid100/graphite0但filters1116×5/1109×25；Graphene较早1438→1856是库存增长线索，不推为连续供给/归因/过滤故障；5187、5171确working。保留旧D燃料分配与独立600窗证据，不从D空仓推永久停机或新链可分配余量。

离线Luna按32指定原件提20个sorter：18个tokenless endpoint附件＋2个direct plan，缺项0，舍入0.001位置无重复。私有派生索引SHA-256 **F84F66093F5731FE7B8F4EFDDA473799023A18F45B8C1D19182478FF83EF10B1**；不是actual sorter IDs/实际互逆清单，plannedBeltIndex是每条路径局部索引，不能当全局唯一ID或原生联合通过。当前Coal/Cu地面端点未改，该索引地面位置仍适用设计，写前必须fresh。

最新Coal闭合S**86574178**/J**86574183**、R1/save85236882、97/durable97/pendingfalse/errornull；external3/10/lifetime63不变、acceptedΔ0/本轮在途action新增0/unknown0。摘要未提供单独pending-action字段，不靠该摘要重置或推断旧未决状态。root本地summary输出过大/字段site.feasibility误读从同一原件修正，无Game重放，不算原生拒绝。完整模型准备/审计/文档墙钟/provider usage未齐，caller秒数不冒充总提速。整案仍false；下一唯一收敛边界为实际同层合接/精确NEW预算、sorter现场覆盖与源分配，不追加支线、不拆普通只读commit、不发行，DSP继续运行。

### 2026-10-03：必要供电位置核销，Codex与游戏生命周期独立

原DSP PID22052仍运行，父进程steam.exe；MCP Program仅stdio RunAsync，Plugin OnDestroy/StopHost仅Dispose自身host/安装lease，没有主动关游戏路径。真实Codex退出重启尚未实测；本轮没有save/close/load/deploy。根规则、包内playbook及当前快照继续要求重连fresh核身份/J/action/accepted，不清十写、不重载健康运行世界。

fresh目录/129generator＋85node包 **b7ed9b5f8b1747a7ad7ba5cabba61c0f:1–10**，10读/3.331s。原生半径/连接规则及214位置的有界派生图对应N3 210节点/127generator、N4 4/2，与原生summary一致、无边界不确定；普通pole回执network=null仍未知，派生成员不伪装原字段。20计划sorter按真实source power点而非视觉中点评估，17点既有覆盖，Coal/D/Container消费端3点缺电。

只读明确选owned5327导出单2201，再检查两个固定site；run **0c722c6649f44d3997748c05daee6f23**：10读/**1.722s**，`:5/:7`均nativeCheckPerformed/nativeCheckPassed=true、Ok、无occupiedObjectId、technology满足；仅whole_blueprint_inventory_insufficient（2201 required1/package0/missing1）。原生返回pole位置分别(-110.404633,5.03104544,-166.929764)与(-143.691742,27.5856171,-136.644623)，不是请求位置照抄；采用这些位置，未来20/20source点派生覆盖，三个缺口距对应pole为8.4334/8.4255/9.5688m。两塔未建，逐consumer实际网络仍待施工读回，不把只读executable=false说成可commit。

两个site power均N3 capacity1,894,000/reserved1,797,800/export0/headroom96,200 J/t；2201新增负载0，20基本sorter另增6,000后余5.412MW。原生r8需要2Fe＋1coil；r6磁铁2＋Cu1产2coil；新增2塔递归预算Fe4/磁铁2/Cu1，当前背包可负担，无需取材/Move。尚未prepare手搓、未施工，整案false不执行；持续燃料/源可分配率不由容量推证。

私有20sorter索引已纠正实际Cu/Coal provenance，33原件、最新各5份SHA一致；新SHA **91066B798208AD38BFC25B7FFC696459C0526A2404B0A2C281BCAA2CB1385227**，取代上一段旧索引引用，历史文件不删。root独立核10最新原响应/逐件SHA、完整J97与背包连续、原生返回pose及预算，proof **42a0f2ec3718457593f79ab0002fab2c:1**，SHA **B2B85C1B39881BD97935F8408955803120A0025249443A25B83D722600DE608D**，0额外Game调用。closingS86609861/J86609864，R1/save85236882/durable97/pendingfalse/errornull，external3/10/lifetime63/acceptedΔ0。root本地索引顺序/路线名称误配从同原件修正，无Game重放、原生拒绝0；模型端到端墙钟/provider usage仍unknown。源分配继续复核；actual-ID合接与持续产出明确留作建成后验收，普通只读不拆commit。

### 2026-10-03：完整源—路—端计划获准；有界中段替代未证明续接

当前source main **81a670a**、installed **3fe31d1**、DSP **0.10.35.29104**；仍同一owned/R1/normal-save85236882、external3/10/lifetime63。Codex/Host退出重启、断线或上下文压缩不保存/关DSP/重载；原游戏由Steam独立运行，未做真实Codex退出试验。

fresh完整图`70b9ed47db7d4c4d992c6cd31e0610c5:2–55`：54页/5365实体/0prebuild/10450互逆边，root`d8e76b0533fa40ceb3a634a836d43a60:1`核全页（SHA`567DFF3B5E9650BE2B3D7F09537D06D0EA0BEFA4FC5FB728CBEEA99707A80EE9`）。相对十写基线仅1213移除矿点39；fresh`49685175dd064aa692cb7b6d61ed19c6:2/3`证明矿点已不存在（不能写成数量0），矿机仍工作/5节点/铁矿50。root`d72a4363db94438185c379f94f21cb3d:1`（SHA`8315DFEE5B36DAE5513A6CA47159D832EA7EA7C2113F1C1B07860C4B340977D5`）复用同图核自动源有向路径；没有重采历史或重置外部accepted。

| 自动源→消费者 | 路线/端口/配方及预算 |
|---|---|
| Cu2440→10/r3→26→562 | 562.slot8/filter1104→884.slot11；78带/2sorter |
| motor724＋coil725→814/r98→827→3467 | tap3467→884.slot1/filter1204；1direct sorter，保留旧燃料分支 |
| acid861＋graphite3084/3966→870→869/r31→871 | 871.slot8/filter1123→884.slot10；63带/2sorter；另870.slot6→869.slot2/filter1109的1direct sorter |
| 884→883/r99→885 | 复用887/888/889和886；r99需Cu2＋Turbo2＋Graphene2→1206，不改无过滤且唯一输出的886 |
| 885.slot2/filter1206→5326.slot5 | 132带/2sorter |
| iron1496→1500/r1→1511→5326 | 52带/2sorter；源端/消费端困难接口原dde89f…正例 |
| 3964/r16及四r58氢源→3347→3074→3073/r40→3074 | 3074.slot2/filter1121→5326.slot4；98带/2sorter；保留3405→3403旧D燃料分配 |
| 5326/r104→5333/r101 | 1206×2＋Fe×2＋D×10→1127；5326.slot8/filter1127→5333.slot11，51带/2sorter |
| coal5171→5187/r17→5334/r60 | 5187.slot4/filter1109→5334.slot7；67带/2sorter；保留5196 |
| 5334/r60→5333/r101 | graphite→1112；5334.slot4/filter1112→5333.slot10，5带/2sorter；r101需Diamond4＋1127→1209 |
| 5333/r101→5329/r78 | 5333.slot1/filter1209→5329.slot8，8带/2sorter；r78需1209→1210 |
| 5329→5331补给端 | 既建29带＋5364/5365，slot7→slot11，已正常保存，仍无1210货物正例 |

完整新施工上限571带/20sorter/2个2201，当前预测554带；背包582带/28sorter，不重购已建核心或29条输出带。两个必要供电点native正例仍为`0c722c…:5/7`，递归材料Fe4＋magnets2＋Cu1可从背包正常手搓；未手搓/建塔。完整既有峰值预算N3 capacity1,894,000/reserved1,797,800/export0 J/t，20sorter追加6,000后剩5.412MW；214节点＋两塔原生返回位置派生覆盖20/20，实际新consumer网络/覆盖及持续燃料尚待建成fresh核。

现有free-end代码`aux.Snap(...,onTerrain:true)`不证明高层source-only续接，故不新增原语、不冷更新。多H统一为A/D/middle先独立空建，再左右同层actual-ID双cover；free请求保持1.5–30m/4–64点。单段超过30m的离线候选未发Bridge；不是放宽策略或原生失败。Graphene run`4575e711f1e244659f5aa5d0c69f195c`原`:15/25/20/30/10`为A/H1/H2/H3/D，12/16/9/17/13点；D run`532927dc163749fe83e7802040ed097c`原`:15/20/25/30/10`为20/26/9/27/20点。两路全部full_path_stage1、端点Ok/span2/精确槽位过滤、四seam0；A/D仅tokenless endpoint preview，不能commit该preview。Graphene调用方曾把施工顺序当路径顺序，root直接核成功原件，不重跑native。

root`5e70f887e3244f2a892a7e8a701b643f:1`（SHA`10D0CE3C8C8FA7191CEC5F216B17A0BB4AE94714AEFD08A49FCF06FB42D00EF3`）核76原件/32响应、全部五段路径/端口/材料/玩家与原97条durableJ；替换当前Cu/Coal/G/D后九路554中心、≤0.25m重合0。中心投影不是联合native OBB，未来actual-ID合接和精确NEW账明确待fresh prepare/readback；持续可分配率留作真实接通后验收，不拿未来36000窗作为无法提前满足的施工条件。

据此root批准完整有限计划`executable=true / wholeNativePreflightPassed=false / wholeSustainedRatePassed=false`。首窗仅Graphene A/D/middlefree、两段actual双cover、D.last→884.slot10/filter1123，再normalSave：预计63带/1sorter/7accepted，3→10即冻结；871源sorter留下一独立审计窗再接，不挤掉保存槽。每笔仍fresh native prepare/精确校验/唯一commit/同action终态/材料与互逆读回，偏差即停，已接受不重放。此批准不是已施工或整链验收通过。

最新只读S86663190/J86663195，R1/save85236882/J97durable/pendingfalse/errornull、external3/10/lifetime63；本段Gameaccepted0/新action0/unknown0，没有施工/扣料/取材/手搓/保存/关闭/加载/部署。G/D各16请求、caller2.848s/2.990s；root复用原件，本地字段/数组顺序错误未触发Game重放，端到端用量/模型墙钟仍unknown。完成门仍为完整自动供料→1210非零→normalSave→protected restart→再输出，之前不进联合长窗/燃料/远征/最终包。上述阶段事实并入同一Gate证据，未另拆只读commit，未tag/release/发布。

### 2026-10-03：首个Graphene施工窗正常保存并独立核销，游戏保持运行

以下是上节批准之后的真实施工，不回改当时prepare-only事实。source main `81a670a`、installed `3fe31d1`、native29104不变；同一owned session正常运行，本窗没有关闭、重载、部署、Move、取材、手搓、拆改或源端接入。Codex/Host生命周期不触发DSP退出；真实Codex退出测试仍未做。

| 原施工caller/终态 | 实际NEW/保留端点 | accepted / 材料 | 有界caller与物理等待 |
|---|---|---|---|
| `d8fc889be1374961b4fd3019639126c8:5/7`；同action终态 `3a7195decbe64b8db1e8dc87ac4b81d2:1` | A12，实际5366–5377 | 3→4；582→570 | 180秒caller等待先到期；真正完工3942 game ticks，完整墙钟unknown |
| `5519959432cb4bbdae70610628896508:7/9/195` | D13，末5388/首5390 | 4→5；570→557 | total383.3s，terminal observation380.1s，6298 game ticks |
| `5811c2deb5924487b36355e0cff684e8:7/9/101` | middle9，首5395/末5399 | 5→6；557→548 | total187.5s，observation184.8s，3169 game ticks |
| `6da08a31e74e4179a872eeb061ff4ab4:9/11/160` | left14 NEW；原5377→5395双cover | 6→7；548→534 | total306.4s，observation302.9s，5243 game ticks |
| `b1f3591eef1041868e272cbb0c0f4141:9/11/160` | right15 NEW；原5399→5390双cover | 7→8；534→519 | total306.3s，observation302.7s，5878 game ticks |
| `8bb1859e532647e6a58cc2876db5fe94:6/8/29` | 2011实体5429；5388→884/filter1123 | 8→9；28→27 | sorter observation37.3s；与save共total39.7s |
| 同run `:36/38/39`，normal save action `0bb6cd96-19d0-4d16-b47b-687b7734d652` | saved tick86729135，R14 | 9→10；无材料扣料 | 同一原action成功终态；没有随后restart |

A的180秒超时不是未执行、native failure或unknown：原accepted已扣12带、建12普通prebuild。较早06:37:41 pending query不能与晚162秒的空prebuild页拼成observer故障。root按原action取真终态、12实际ID/11互逆边及材料核销，未换键/重放；proof `3725f914e7e04816bb6590b5048c660e:1`，SHA `07DC39AEEA90100C6D0B5DC28172FBFBF929488ABCACD91FEB4B2EBF0DF162EB`。后续沿用成熟共享caller、显式600秒有界观察，**不修改native watchdog**。原estimated10800是game ticks，不是180秒墙钟。

旧资格R1误作新执行guard的本地假阻塞在新Game请求前停止；`e11ca482482c40168efa084117ea24be:2/4/5`分别给出fresh R3、Walk/低速/库存与durable97。修正后每笔用真实revision/hash，不改为另一固定R3、不以accepted推算。一次缺import也在D调用前停止；没有因此重跑成功原件。dual-cover的NEW子集最低2点与free完整路径最低4点分别沿用原生规则，不裁剪native路径。

root逐原件独立核D、middle、left、right的native点、唯一commit/同action终态、全部63 NEW及材料、双cover新边和旧边、871/884/5326/5329静态；补充证明分别 `f02446df05164ab6b79cf4397f783f3f:1`（SHA `1AE5A345857706E40EC8F6935B6F7EDFA9E7A1400111F8B0090E4631FBF68F80`）、`373f7f602207494f91988f8da0967e70:1/2`、`561985b2f1b6455b85d0eab621edfd63:1`。目标端真实prepare为exact_slots/source−1/destination10/filter1123/2011×1；读回 `8bb185…:30–32`证明实际belt槽为**4**，sorter.input1与5388.slot4、sorter.output0与884.slot10互持，N3/serve1。虚拟−1不能猜成实际0。

正常save覆盖63条带＋1sorter。fresh捕获 `3bf6a498feaa4ed789422d84d2f2b71a:1–71`：built页`:2–56`为**55页/5429实体/同一snapshot tick86730410**，P`:57`、J`:58`（tick86730652、97/durable97/pendingfalse/errornull）、power`:59`、空prebuild`:60`、9detail`:61–69`、closing S`:70`（86730665/R14/save86729135、healthy/owned/peaceful）、summary`:71`。采页14.943s，不是模型端到端耗时；原5365/54页baseline保持不动。

root全窗证明 `5f7a97fc9ac54461a4a1a7092d217525:1`，SHA **`1EB99710B362943707BD3C1AB3AA858550088B538A8ED93D9416B14457580E26`**：十个原accepted成功终态（含此前维护save＋两个protected resume）、0replay/unknown/在途，external10/lifetime70、冻结；5429 built/0prebuild/**10578互逆边**。相对 `6477dc…` 仅新增63个2001和1个2011，无删除；旧884只加本次slot10边、1213只移除先前已证明耗尽39，其他静态无未解释变化。303对象在显式动态字段变化，未掩盖配置差异。库存582→519/28→27、其余count/inc不变，Walk/低速/能量正/无排队。旧baseline跨恢复的玩家y比当前差2微米；改用本session首笔施工原P `d8fc889…:4`，它与封窗P完全相同，未放宽工厂pose、ownership或hash要求。根程序本地输出/解析和此比较锚点错误从相同不可变原件修正，0新Game调用、0Game重放。

whole actual有向空路径为A→left→middle→right→D→5429→884；**871.slot8源端未提交**。没有Graphene到货、1206启动、1210产出、保存后restart或持续生产正例，不能宣称整链已完成。下一业务边界在十写文档/commit/push/准确SHA绿CI与root交接后，先fresh接871→5366并观察883/r99及885真实1206启动，再连续推进同一全案。保存不会触发游戏关闭；原DSP继续运行。

离线Luna最小Release测试59/59＋8/8已通过；本地共享caller薄入口0Game smoke及18个schema正负例通过，不冒充native验收。各entry到首次prepare约1.1s，后四段和目标/save的caller时间主要为上述物理等待；不据此编造相对旧流程提速比例。完整root/Luna模型调用、缓存/非缓存token、文档/Git/CI阶段墙钟尚缺provider数据，均unknown。本窗只形成同一Gate事件/封窗交付，不给每一段另造commit；无tag/release/Thunderstore发布。

本次流式外审在有界期限内未产生terminal，故记录为未完成而非通过；依据可见轮询估计会话约220秒，准确启动时间未保留，达到预算后停止。未因`unrecognized_model`重试或更改模型/API/baseURL。root另行核验了四份文档的事实与提交范围，但该独立核验不等同于外审通过。

### 同一运行现场续接：源端5430与1206启动（当时未正常保存；后由下节覆盖）

封窗单目的commit **dfc28df9880799ec61b1ab6a6989ef86bd6cf6a3** 已push，远端main匹配，精确Windows Core CI **37079721645 completed/success**。root确认上一writer停止、无未核销或在途后仅重开external0/lifetime70，未改游戏R/tick/J，也未保存、关闭、重载或部署DSP。游戏由Steam持有，不以Codex/Host生命周期作为退出或恢复触发。

普通共享caller的同一薄入口新增方向参数与可省略阶段save，0Game的两个真实pwsh smoke、24个真实ValidatePrepared回调schema正负例通过；这些不是native验收。实际源端run `f926d832bf364d74b5f33180170a3946`：prepare`:6`为exact_slots/source871.slot8/destination5366.virtual−1/filter1123/2011×1，唯一commit`:8` acceptedtrue/replayfalse，action **e1bc59cf-8e1b-4805-b6ed-ced2aa4a324a**，同action终态`:25`成功，tick86765754→86766249、target **5430**、2011 **27→26**。`:26–29`核实际`871.slot8→5430.input1 / 5430.output0→5366.slot4`、N3/serve1、旧边/静态/玩家与其余库存不变；closing S/J`:31–32`为tick86766258/R16/save86729135、J采集86766259/durable97/无pending/error、无在途。root独立证明 `134623e16abe4cd2ac716b1592152d3f:1`，SHA **7B263C5249D878AE47E709F58192191F6DBA436828445177E5A894ED66863BB7**，0新Game调用。

预声明有界采样复用 `Invoke-SpherewrightProductionExperiment`，entities871/869/5366/884/883/885/5388/5429，items1123/1206，3个独立600-tick窗、interval606、max3samples/100requests/300s。run `1be51a62796b42d48816babe486970b9`，事件`:21/40/59`分别索引原响应`:10–20/29–39/48–58`：

| window ticks | 1123 produced/consumed | 1206 produced/consumed | 1206实测/min | 885库存 |
|---|---|---|---|---|
| 86768567–86769166 | 0/4 | 2/0 | 12 | 4 |
| 86769203–86769802 | 2/4 | 2/0 | 12 | 6 |
| 86769822–86770421 | 2/4 | 2/0 | 12 | 7 |

runtime身份为 **1206/粒子容器**，883/item2303/r99三次均working、N3/serve1，1204/1104/1123输入均正。不能把口头误称“碳纳米管”当作物品身份；实际动作一直按上述native ID/recipe执行。原`:61`记录56请求、110.160s（读取4.371s、预定等待105s）、循环内0模型决策；`:60` closing S为86770423/R16/save86729135/healthy owned。root独立核所有三窗原生产响应、selected实体、输入与功率，证明 `67ca019cf6be4bb9aa8b1eb1f4dd6734:1`，SHA **CF132B3A21A33410F376BB86D8A96003D1E531384DB8D6BC29166524D5B050BB**。一次本地审计排序表达式解析错误从同一原件修正，0额外Game调用、0重放，不是native blocker。

当前external **1/10**、lifetime **71**，成功源端5430 **尚未被正常save覆盖**，无unknown/在途。三窗有间隔，continuous credit **0**，只证明1206启动和重复活动；未证明完整自动供给余量、1210、restart或36000ticks，也未由这些样本定位唯一缺料。下一阶段仍是整案内剩余自动源—路—端与必要供电，不为单对象另commit，不重放5430或63条带。DSP保持运行；正式Gate重启留到全链输出与正常保存之后。

### 2026-10-03：Cu/Turbo自动接口完整施工窗保存并独立核销（无游戏重启）

source main **dfc28df**、installed **3fe31d1**、DSP **0.10.35.29104**，同一owned session。延续已批准的完整1210全案，上一源端5430占external1，本批只增加五段Cu带、两端Cu分拣器、既有涡轮带取料分拣器和正常save：**1→10冻结 / lifetime71→80**。没有Move、取材、手搓、拆改、旁路、部署或关闭游戏；Codex/Host重启仍不触发DSP退出、重载或计数归零。

| 原run（唯一commit / 同action终态） | 实际结果 | 背包2001 / external |
|---|---|---|
| `bd1bafbe5a3947bdaf8048ee14011343:9/124` | A12，首5431/末5442 | 519→507 / 1→2 |
| `32cbee1ca51740b5a5c56a7401c4a218` | D13，首5452/末5455 | 507→494 / 2→3 |
| `b06811f317e54f0188bd1ab7e4bb50fd:9/155` | middle18，首5467/末5473 | 494→476 / 3→4 |
| `1b3529f65ece45a5bb036731991e86f7` | left9，5442→5467双cover | 476→467 / 4→5 |
| `588ba6d4e61645ec8f140abb243af532:11/295` | right26，5473→5452双cover | 467→441 / 5→6 |
| `0aaa334c7acb4d078a76a7b5752a658f:8/32` | 5455.actual4→5509→884.11，filter1104 | 2011 26→25 / 6→7 |
| `da0f7a29a3a14f2698e5339bf176a5a2:8/26` | 562.8→5510→5431.actual4，filter1104 | 2011 25→24 / 7→8 |
| `b0fb9c1ed8454769b6e49a8d42dd5112:8/31;40/41` | 3467.actual4→5511→884.1，filter1204；随后fresh normalSave | 2011 24→23 / 8→9→10 |

每个路径fresh原生计划与固定资格坐标/数量/高度/cover保留模式核验，唯一commit、原action终态、材料和actual有序实体/双向连接读回。完整有向Cu路A→left→middle→right→D **78个NEW2001**；不按ID大小排序，不重放成功前缀。3467旧边、884原输入slot10/输出和过滤配置保留，四只新sorter（含5430）均N3/serve1。

正常save原action `3526b3d5-673d-4120-b144-67399fb25d80`终态成功，tick **86835012**，覆盖本窗及先前5430。一次封窗capture **3e64a18b990147b89410d31d000dc6d6:2–57** 为56页/5511唯一built/tick86836183；`:61`证明0prebuild，`:62–70`为相关端点detail，`:71` closing S **86836420/R33/healthy owned/saved/resumeAvailable**，durableJ97/pendingfalse/errornull。十笔原accepted全部同action成功，原索引无replay、unknown、未决或在途。

root直接核原件并与`3bf6a4…`完整基线比对：只增78条带/4sorter，背包2001 **519→441**、2011 **27→23**，增产点数及其它库存不变，玩家位置不变；**10748边唯一互逆**，旧实体无删改或未解释静态漂移，J97原97条连续，N3 required=served **467265 J/t / ratio1**。独立证明 **44306845f3d14191aa140f85d26c6cf7:1**，SHA-256 **D49AF63FF468B24C5317FA28B7D878B70272AAD57D001668AE9827B7671B37F2**，21.04s、0新Game调用。审计本地一次缺少Hashtable键检查在同一原件上修正；不是游戏失败，也未重采或重放。

封窗`:65–66`为瞬时点证据：883/r99 working，1204/1104/1123各3、1206输出0；885库存1206/粒子容器12。`:68–69`两只Cu sorter实际持铜；这不是额外采样窗、稳定供给或1210非零证明。先前三个600-tick启动窗仍仅短窗，continuous credit0；本窗保存已证明，**restart未做**。

效率边界：五段带入口→首prepare **0.99–1.34s**；prepare＋精确校验＋commit合计 **2.23s**，同action终态观察合计 **1620.61s**，五段完整执行 **1637.49s**；三只sorter阶段含末次save **122.66s**。主要等待在原生无人机施工/同action轮询，不能把全部观察墙钟当纯模拟或纯模型用时。Luna两次零accepted调用方错误（Journal摘要字段、Hashtable参数绑定）均从原数据修正；原生拒绝0、无额外commit。0Game真实pwsh smoke通过、sorter-tap AST离线正负例 **9/9**，不冒充native验收。无每动作文档/Git/CI、没有新的模型逐窗采样；root＋Luna provider token/完整委派与GitCI墙钟仍unknown。外审若无terminal仍未完成，不记通过。

整案已施工供料141带/5sorter，预测剩413带/15sorter/2塔；尚须Fe/D/1206/Graphite→Diamond→Lens及1127→Lens→1210实际连接、现场供电和真实产出。完整Gate结束仍为自动供料→1210非零→normalSave→protected restart→恢复后再次非零；本窗docs/push/准确SHA绿CI/root交接之前保持external10冻结，DSP保持运行。

### 2026-10-03：Graphite→Diamond自动接口保存、独立审计与实际启动（无重启）

source main **deb6394ad472bd2f3de0bf69d2ce25246c19e733**，准确SHA的Windows Core CI **37085219613 success** 后root仅重开外部审计计数0/lifetime80；没有重置Game revision/tick/J或identity。installed仍 **3fe31d1**、DSP **0.10.35.29104**，同一owned session/原DSP进程。施工仅属已批准完整1210案：五段Graphite带、正常手搓两塔、固定位置建一塔、两端分拣器及normalSave；**0→10冻结 / lifetime80→90**。没有Move、拆除、注入、旁路、部署或关游戏。

| 原run（唯一commit / 同action终态） | 实际结果 | external |
|---|---|---|
| `eb5530dd87bb4b4ab560fd2b3547f981:9/89` | A12，首5523/末5512；2001 441→429 | 0→1 |
| `95a89e7bcbf64752ae0e765dfc5d46d8:9/66` | D12，首5524/末5535；429→417 | 1→2 |
| `07e0d87e376749b29623c817c6925fe3:9/52` | H2 19，首5538/末5554；417→398 | 2→3 |
| `899ce4188177437888140c44989d6c8a:11/72` | H1 21 NEW，5512→5538双cover；398→377 | 3→4 |
| `8724993f686b4add95c971044d9efc52:11/21` | H3 3 NEW，5554→5524双cover；377→374 | 4→5 |
| `d042133e9eca44068b84296f274f59b0:7/13` | recipe8/count2正常递归手搓：Fe−4、磁铁−2、Cu−1、2201+2 | 5→6 |
| 同run`:25/37;38` | 唯一5579/item2201，扣塔1，精确pose `(-110.404633,5.03104544,-166.929764)` | 6→7 |
| `c20c36977f5d4baa86da833ecaf60c16:8/20` | 5535.actual4→5580→5334.slot7，filter1109；2011 23→22 | 7→8 |
| `3a2403b63fd0464ebcce6b885be803f9:9/26;35/36` | 5187.slot4→5581→5523.actual4，filter1109；2011 22→21；随后normalSave | 8→9→10 |

完整有向路 **A→H1→H2→H3→D / 67条NEW2001**，实际ID/方向来自原计划和完成实体，不按ID排序。两次cover仅续接明确空的成功端点、保留其配置与旧边；每笔fresh原生prepare、精确预算/坐标/高度/绑定校验、唯一commit、同action终态、材料和实际双边slot读回。新sorter均N3/serve1；normalSave原action **4d7cb2ae-60d9-4e7e-a989-6c448959d309** 成功于 **86883661**，覆盖全部本窗施工，签发最新protected resume能力，**未restart**。

仅一次完整capture **8b3a9202460242179005c87381f00b17:2–57**：56页/5581唯一built/同snapshot tick **86884221**；`:58` P、`:59` J、`:60` power、`:61` 0prebuild、`:62–69` 八个固定detail，`:70` closing S **86884471/R52/healthy owned/saved**。root复用这一不可变图，与`3e64a1…`完整基线逐对象比较；十笔原accepted均成功/无replay或未知，仅增67带/2sorter/1塔，无删除、未解释静态变化或整图重放，**10888边唯一互逆**。净料2001−67/2011−2/2201+1/Fe−4/磁铁−2/Cu−1，其它库存、增产点数、玩家位置守恒；J97原97条durable连续、pendingfalse/errornull，空队列/0pending drones。N3节点210→211、consumer523→525、gen127/cap1894000不变，required=served236694/ratio1；N4静态不变且满供电。

独立证明 **8ba5bed2ca774de79f51361af3852412:1**，SHA-256 **FB3137C4EFB439DFCF2DB20988AE22C14167E1185DDAA048846C747AC5C5DE05**，**24.25s / 0新Game调用**。审计复用既有实现，仅参数化基线、路径、单塔/手搓净料和save所属run；原件权威、不是执行者口头通过。配置fixture11/11、实际AST有序扁平路径fixture1/1，均零Game，不冒充live。

初次封窗`:64`的5334/r60仍为石墨input0/金刚石output0，不能据此宣布送达或稳定。唯一后续有界启动探针 **56f2a4a0250f4ba3875100a7f65745be:1–5** 首点立即得到正例，未使用等待或第二组实体调用：`:2` tick **86891785**，5334/r60 **isWorking=true / 1109 input2 / 1112 output55 / progress500000 / N3 serve1**；`:3` 5187/r17 **isWorking=true / coal4 / graphite output51**，原旧边保留，新增slot4→5581；`:4` N3 required=served286821/ratio1；`:5` closing **86892637/R52/save86883661/healthy owned**。5334只有本窗新输入5580；root直接核原DTO与完整有向路，确认真实Graphite送达及Diamond生产。该后续动态库存不声称已被之前save覆盖；本点不是重复600-tick产率窗或持续供给，continuous credit仍0。

调用方经验与边界：5187实际上是item2302/r17生产设备，不是storage。既有共享caller的薄sorter入口增加显式生产输出绑定，精确检查proto/recipe/runtime目录/输出buffer和单位，移除只适用于仓库的重复守卫；默认storage及既有belt tap保护保持。实际AST/live guard正负例 **16/16**、tap **9/9** 零Game通过。电塔5579的entity.powerNetworkId为**不可观测/null**，旧5327同样null；现行CapturePower仅给consumer/generator填该字段。错误caller在原生成功后要求N3而停止；没有把成功判未执行，也未重建。仅fresh readback **f78e7a5098394472963e32f432b28c51:1–10** 核唯一N3节点+1/其它网络与旧配置不变/扣料后剩塔1，root原件比对通过后才续接；相应node语义实际AST **37/37** 零Game通过，不向Plugin新增观察字段或工具。

三个实机调用方故障分别是上述post-terminal node字段、只读摘要访问不存在的Payload.objectId、启动摘要误用`working`而非实际`isWorking`。均保留原accepted和已保护的响应，未重放任何commit或重复已完成请求；**原生拒绝0、unknown/quarantine0**。不要为摘要再造reader/label；共享read caller已经保存原回执。离线审计的recipe单位/扁平路径错误在执行原审计前修正，不计作游戏故障，也没有新Game采集。

效率原件：五段带入口→首业务prepare **1.04–1.31s**；prepare＋精确plan校验＋commit合计 **2.23s**，同action终态观察 **490.23s**，五段总执行 **507.37s**。两塔native prefix **28.13s**（6.58s手搓/18.63s施工终态观察），两只sorter含save **52.72s**；读回故障处84秒间隙和额外主会话处理不隐去。主要施工观察、调用方故障、独立审计与文档/Git/CI不可混算；root＋Luna完整委派/模型调用量/provider token仍unknown，不给出同类任务提速比例。本窗不逐动作提交/等待CI，不重采全图、不在生产循环唤醒模型。

自动供料累计 **208带/7sorter/1塔**，整案预测剩 **346带/13sorter/1塔**，背包374带/21sorter/1塔。Diamond→Lens及Fe/D/1206→1127→Lens→1210未完成，未证明全链1210、持续供给或restart。docs/push/准确SHA绿CI/root交接前external10冻结；后续仍同一1210全案，不进燃料、远征或双包支线。DSP保持原进程；**Codex/Host结束不触发游戏关闭、重载或外部计数清零**，真实Codex关闭试验未做。

### 2026-10-03 宿主重启后Steam/DSP消失：独立启动资格，不冒充修复闭环

Graphite阶段`f3b001a88a4179a7334770fc77a64c448f1782b7`已push、准确Windows Core CI **37088675397 success**，root仅重开external0/lifetime90。接续只读组 **0045868420fd4333912884533c3ab682:1–7** 在10:16:28–29完成：same owned/healthy、tick86913712/R52/save86883661、J97durable/pendingfalse/errornull、库存374/21/1、1511铁3000/空slot0、5326空slot7/N3满供；3074当帧空仓。批准Fe全路与D部分前缀，但执行者随后明确确认**尚无本轮Game请求/prepare/commit意图/accepted/在途**。

用户报告关闭后，root在10:24:58以OS核Steam及DSP均不存在；新宿主进程10:19:42启动。退出精确时刻未捕获，Windows未得相关崩溃/终止事件，Steam没有新增本次正常退出记录。最后正常save86883661已覆盖全部67带/5579/5580/5581；后续生产buffer仍不冒充已保存。external保持0/lifetime90但冻结，新Fe/D未施工，不重放任何已保存前缀。当前健康恢复票据仍为minimum86883661/planet104/J97、非quarantine且未过期；只作离线凭据检查，不是恢复通过。

根因边界：当前托管命令的原生Windows Job查询为 **0x2800 / KillOnJobClose=true / BreakawayAllowed=true**；旧本地启动方式使用普通Start-Process，父进程是Steam不能证明脱离宿主。该机制与宿主重启后双进程消失高度一致，**旧Steam/DSP已经退出，不能声称直接读到了其旧Job归属或已取得确切终止原因**。前次工作规则/代码检查没有完成真实Codex退出存活实验，旧“独立运行”判断必须撤回。

有界无害资格：现有Explorer桌面dispatch启动45秒测试进程，原生成结果 **912db2b4a0654a71bda7c2cfb6f86a70**，SHA **AD8BE6014178FEE2266A9CC99C4A4B8C1B0DA86E427F5E045FB75EF9C5D898A0**；父进程确为启动于宿主之前的Explorer、相同交互session，子进程自身Job **0x1800 / KillOnJobClose=false**。它仍属于非清理Job，不能把`inJob=true`误当失败。普通Shell.Application进程内dispatch仍落入托管清理Job，不能替代获取既有desktop.Document.Application。探针无Game/Steam启动/读写，自动到期，无改Codex配置或安全设置。

已补本地固定Steam app1366540的一次桌面broker入口：现有Steam/DSP拒绝重发、唯一incident先记intent、响应不确定只核销、不退回托管Start-Process；不保存/载入/关闭/部署。真实pwsh smoke **0Game/0启动**、三份入口parse通过。**实际Steam启动、真实Codex退出后游戏存活、同档protected resume仍待**，不宣布完整修好或恢复生产；主线仍完整1210与原0.4验收门。

### 同次独立启动与主档恢复已核销（2026-10-03）

上述“仍待”为启动前证据边界，后续只派发一次incident `9c57185688594a77ab6734f2ade6498f`：Steam由既有Explorer启动、DSP由该Steam启动，两者不在当前命令的KillOnJobClose组。fresh菜单native29104/healthy；root再核既有`450b1bc7…:5`完整cohort的4 Plugin/224 MCP/2 native均一致，未部署。第一准备资格窗结束时确实0prepare/commit/在途，不拿超时当重发许可；之后直接复用已审`resume-safe-runner`，没有重写执行器。

恢复原件 **5e9e2203413b4ac9a5fca38314b1b7e3**：`:3` fresh healthy/default exact-primary prepare，`:4–5`唯一commit意图/响应，`:7–23`全核同action，`:23`成功终态（action **7d5118c6-d9d0-49aa-943a-a64c523e345d**），`:24–26` S/J/P，`:27`完成。source/target **0.10.35.29104**、planet104、minimum86883661、无blocker/另行确认模式；default计划的candidate/identity-preview字段不作为额外身份正例。最终owned adoption、正常保存 **86883692/R1**与J97连续性才是恢复证据。action终态tick为null，不能写成save tick。无重放/新施工/额外save；恢复仅 **1 accepted，external1/lifetime91**，未重开或清零窗口。

恢复后固定8次只读 **e6e12b97d9c3496fae365f99b2806d33:1–8 / 2.9s**：S/P/J、5579/5580/5581/5334、closing S；latest **86902474/R1/save86883692/healthy owned**，J97 durable/pendingfalse/errornull，97条原entries与退出前`004586…:2`完全相同。玩家Walk/速度0/3无人机idle/0 pending与手搓队列；材料374带/21sorter/1塔不变。5579仍2201且network null不可观测；5580为5535.slot4→5334.slot7、5581为5187.slot4→5523.slot4，filter1109/N3/serve1。5334/r60输入1109=2、输出1112=100、isWorking=false；未接完的下游不算持续产量，本次关键节点检查也不是十写完整工厂审计。

root直接核上述原回执、唯一commit/同action终态、97条日记、静止玩家/材料与新健康票据，证明 **d32f1abd12e149a896b1da3e235c7a10:1**，SHA-256 **E06DE14B16A889D9D95A82CE1C1E3F250DB79B761EF14D7A608EBB39A1AA9589**，**0新Game调用**。旧宿主退出的确切终止原因、真实Codex退出存活仍未证明；用户明确无需专门做该关闭测试，不把它设为主线门或主动关闭游戏。保持同一已恢复DSP运行，下一Fe/D计划须使用新session并扣除本次恢复accepted、保留save槽；完整1210/nonzero/save/protected restart/恢复后nonzero要求不变。

### 2026-10-03：Fe A-H-D施工与保存审计（D及1210整链未完成）

本窗只继续同一Gate 2有限计划；DSP保持运行，未关闭、重载或部署。root以capture **`3fce122b339f46d19dd966388f699e2e:1–82`**独立审计完整封窗：**57页 / 5684 built / 0 prebuild / 11090唯一互逆边**，与基线`8b3a9202460242179005c87381f00b17`相比只新增101条带和2个分拣器，无实体删除或未解释静态配置变化。独立proof **`44dbb59cb37a4e639fe86d784a1c6f03:1`**，SHA-256 **`31D65C76DFD511CCD8136BFFA8333265DA5A0F0BD0485455F55439EA1383DA16`**；root审计34.98s、0新Game调用。材料净额2001 −101、2011 −2；增产点数、其它背包数量和玩家位置守恒。N3为211节点/527消费者/127发电机/1,894,000 J/t，required=served 410,992 J/t、ratio 1。closing为tick **87125503 / R18**，正常save为tick **87119420**，J97 durable/pending=false/error=null；本十笔窗口10/10 accepted均completed/succeeded，0 replay、unknown、在途或未决，external10/lifetime100冻结。

代码与运行库边界：source HEAD `ac0e434`；installed `3fe31d1`沿用此前已验证的同批构建，64 tools/1 resource资格引用既有原件，本窗没有部署。十个accepted原run（按业务类别）为：resume `5e9e2203413b4ac9a5fca38314b1b7e3`；Fe A/D/H `2cd145bea1ab4cf4899adfb77ee82f83` / `752d8873308848d8904ecd42e512546c` / `2c64d12fd6054e0b976cc5b2941e7894`；Fe destination/source sorter `bf124624ddb84b50ba7373dd30773f22` / `b39d01864dcc4bee868c7210f66dd76a`；D A/D/H2 `8f9cbe4818a1429097e88a57c4d0395a` / `4d9af32afc474105be4a269bafb22e04` / `88e21aa69ce745c48a4329e3400b30c8`；normalSave `2d908a1c143c42eaa6fb04826c23ca59`，action `4af5d821-2953-4c6f-ac60-ae4b47ad2515`成功终结于tick **87119420**。

Fe当前A-H-D实际路径为52条带，过滤物品1101，两端分拣器均N3/serve ratio 1：源`1511.slot0 → 5635 → 5593.actual4`；末端`5605.actual4 → 5634 → 5326.slot6`，slot7仍空。新鲜detail在`:63–64/:68–69`；source 1511铁缓冲2884。5326/r104当时真实收到1101输入4，但1206/1121输入均0、1127输出0、`isWorking=false`；接入铁块不等于持续生产。capture`:79`中的883/r99是生产设备，working=true且各输入为4；capture`:80`中的885是r0仓库，持有915个1206，未连接5326。不得把885库存说成r99运行或送达该consumer。

D仍仅有DA20、DD20、DH2 free 9段；DH1/H3和D两端分拣器未建，不宣称自动D恢复。当前累计309带/9 sorter/1 tower；余量245带/11 sorter/1 tower是计划预测。1210非零、完整自动供料和连续产率均未证明；本窗没有restart，也没有把快照验收说成protected恢复或持续率测试。

本窗调用方曾遇到若干commit前问题。无效Hashtable绑定在任何Bridge调用前失败；旧SID guard不是“过期票据”：run `4477…:1`有1次successful `get_session_state`，`:2`的fresh_reads guard随后拒绝，没有prepare、commit-intent或commit。另一次原生prepare为`prepared/commitAllowed`；调用方原来要求历史slot 7，但fresh native prepare选择slot 6，严格校验因此在commit前拒绝0 commit（`ce641661024d43cfac009bfa4b6ae81d:6`）。root核当前slot 6为空且没有D4/Container5冲突后，才更新到slot 6并继续严格匹配，未移动目标或放宽matcher。D-A请求另因嵌套PowerShell参数展开在Bridge前停止，之后改为同一PowerShell进程内显式splat。原生拒绝0、重放0；不能将这些已说明的调用方问题写成票据过期、游戏漂移、隔离或unknown。

可复用结论：历史原生资格固定的是几何和历史上下文，不是当前运行时session或实际端口绑定；施工前仍要把同一session的新鲜原生计划、当前空槽与端点读回逐项对应。复杂PowerShell参数不要经嵌套shell二次展开。D前缀、Fe实际入料和保存审计仍不等于Gate完成：完整D链/未来actual-ID联合预检仍待fresh证明，且本窗0 restart；全链自动供料→1210非零→正常保存→protected restart→恢复后再次非零继续保留为结束门。

冻结后只读诊断`234c5f7bc1cd46cba5b50913459b6896:1–5`另有一个本地读回包装问题：3074的`inspect_factory_entity`原响应`success=true/error=null`，H20/D580只是该时点库存。把整数objectId `3074`用于`[ordered]`字典下标赋值时，被当成位置索引而抛`ArgumentOutOfRange`，因此没有继续读完其余对象或取得closing S。它不是Bridge/native/版本/隔离失败；不得重做已经成功并持久化的3074读取。root的零Game fixture重现此异常，普通Hashtable的9个integer key通过；随后授权仅续未读对象与closing S，external10继续冻结。

续读run `03e7d55ef141412387e1c51a9871c3fb:1–9`返回其余8个对象和closing S：tick **87204103 / R18 / save87119420 / healthy owned**，0 prepare/commit。它复用首run已成功的3074与catalog读数，但两run不是同一快照。续读run的九条native响应（8个对象＋closing S）均已保存；随后仅摘要因从`sorterEndpoints`而非entity顶层读取`filterItemId`失败，无需重读。3073读数为H20、D输出99，3403为D40；这些仅是时点库存，不证明长期供给、精确buffer容量或停机原因。该续读不是完整工厂审计，不改变本窗capture **3fce122b…** 的审计数字或closing tick。

### 2026-10-03：氘路径与粒子容器局部前缀（电力写门仍关闭）

source审计HEAD `21096a0`；installed `3fe31d1`与native `29104`保持已核同批，未部署。capture **`b0a67d710c744189a0db205a22372361:1–100`**：**59页 / 5837 built / 0 prebuild / 11398互逆有向边**，factory tick 87394107、closing observation 87396048；较基线`3fce122b339f46d19dd966388f699e2e`新增150带、2分拣器、1塔5734，无删除或未解释静态配置漂移。root proof **`ef588417d6d546f2b4b724363066894a:1`**，SHA-256 **`91DB78937BCEB515B8864778654752F7E6B7458F2619E3EF3629DA3782889FD6`**，独立审计43.44s、0 Game calls。normalSave run `dd84de550d594898a813b5f7ab96bab3` action `01ee5739-6a87-45fd-a2f9-4d6b3df428c7`成功终结于tick **87389880 / R37 / J97**；closing与save分列。十个唯一accepted均completed/succeeded、无replay/unknown/在途或未决；external **10/10**、lifetime **110**保持冻结。

十个action run按完成tick顺序：24 belt `31e880d6b2e64d438439b7ec86b0890b`、25 belt `391028ce064f4553babd0240f3869cd0`、tower5734 `5955bdfabde3409e9cdc439f9250e233`、sorter5735 `63f219375bc3402fbd0a81bb7262b03a`、sorter5736 `9183abaa2115430ca489859340cecc2e`、24 belt `7c71617257224865b7aa5fa35195d77a`、13 belt `29295d67919c4f1bb6d1acdc340e34f1`、33 belt `bfb55727bec1495b9f6672fb28174c40`、31 belt `f56cd4a4deb741e9bc17d024109931f7`、normal save `dd84de550d594898a813b5f7ab96bab3`。总增量150 belt、2 sorter、1 tower；背包为123 belt/17 sorter/0 tower，累计已建459 belt/11 sorter/2 tower，余95/9/0仅是计划预测。

重氢路径已完整接到5326/r104：`3074.slot2 → 5736(filter1121) → 5655 → 98条带路径 → 5657 → 5735(filter1121) → 5326.slot4`。这一路复用既有A/D/H2来源。粒子容器仅完成A24、D13、H2 free 33、H1 NEW 31；route completeness为A-H1-H2与D部分，H3和两个粒子容器端点附件未提交。5326 capture detail（`:73`）为D20/Fe4/particle-container1206=0/output1127=0/not working，故接线和已建前缀不等于消费或产出。完整details和attachments见本capture，未重放动作。

proof原生旋转索引中的四项exact pre/post均由installed native校验通过：object 5636、5684来自run `31e880d6b2e64d438439b7ec86b0890b`（ticks 87311593→87320399、87311599→87320407）；object 5679、5675来自run `391028ce064f4553babd0240f3869cd0`（87328641→87336096、87328647→87336106）。校验使用`ProvesCompletedEmptyJoin`、`TryEmptyBeltMembersHash`和`ProvesNativeBeltRotation`；原始前后读数与保存快照一致，无角度容差或宽泛旋转忽略。该proof记录的native DLL SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`与运行时匹配。5734为item2201/power-node；capture N3计数已核实节点增加1。`CapturePower`仅填consumer/generator的网络字段，不提供power-node网络链接；因此detail的`powerNetworkId=null`、`connectionCount=0`是不可用的node-network观测，不是断开证据，也不证明全案20个计划sorter均被覆盖。

供电不能按固定安装额定值推断：native `EnergyCap_Fuel()`派生capacity受当前tick/燃料状态影响。capture的N3读数为212 nodes/529 consumers/127 generators、capacity **1,858,000 J/t**、required=served **219,831 J/t**、ratio 1。独立只读跟进`1d75f049f852442881f729a9c47a641a`（S1/3/5/8、Power2/4/6，133-tick span）读到capacity **1,786,000 J/t**，各power sample仍ratio 1，required=served **208,893 / 189,143 / 185,693 J/t**。按declared existing peak **1,797,800**加whole-plan 20-sorter allowance **6,000**，对最低观测capacity的条件余量是**−17,800 J/t**；`powerBudgetPassed=false`、`writeReopeningBlocked=true`。这证明预算门失败，不证明当帧供电失败。发电机准确原因、燃料库存、持续power均未知；native dll SHA如上，root核与运行game一致。

后续只读generator续查run `64637a358f7843a5ae9e266476369df9`（S1、inspect134:2）及`f2db38aab0cf4a3d93a80389c50daa86`（inspect ord1–8：183/2516/3058/3059/3060/3061/3062/3063，Power9，closing S10）：closing **87538726 / R37 / save87389880**，0写入。所有generator为item2204/N3；capacity仍 **1,786,000 J/t**、required=served **183,925 J/t**、ratio 1。3059/3061/3063当帧输出0且燃料身份为1109，其余有正输出；这不证明燃料库存或停机原因。该closing只是局部诊断，不是新保存、完整工厂审计或持续供电证明；J97以完整capture已核值为准。

随后只读source pack `750912e47def4d14a1951025cee12c59`：S1；inspect 3083/3084/3308/3309/3399/3123/3122/3098/3096 为ord2–10；Power11；closing S12为 **87565358 / R37 / save87389880**。root零Game source proof `faaac627dedd41c9adfd823a449c2ff1:1`，SHA-256 `1DA933FB5470B09E333217859586AE69D112E6A46FE338ECFC0F134972483197`，核实原件与native29104哈希。runtime catalog的r58配方输入oil1/H2、输出H3/graphite1；3083、3084均有oil2/H4、progress complete、serve1、working=false，graphite output=0而H output分别60/59。按当前DLL中的Assembler Refine guard条件`produced[j] > productCounts[j] * 19`，这两个读数均超过57，满足原生输出阻塞条件；本窗没有instrumented函数返回trace。该条件与观测状态说明当前两处r58石墨输出受氢输出门阻挡，但不能据此确定发电机capacity/fuel根因、下游氢sink根因或声称修复已完成。immutable graph确认输入sorter3308/3309/3399回接r58 refinery3083/3084，尚无超出这些局部读数的全链结论。该source pack仅为局部只读诊断，不改变capture `b0a67d710c744189a0db205a22372361`的closing、save或图统计；N3 +1 node与两端实际D sorter full serve可按原件引用，但不外推全部20个计划sorter覆盖。

两次本地formatter问题发生在原响应已保存后：full-pack缺少`session.inFlightActionIds`字段；成功的generator inspect134随后因摘要读取不存在的`connection.filterItemId`而出错。均非Game/native失败，没有写入或重放；保护原件已恢复，仅续读尚未取得的对象。写门仍关闭，external10/lifetime110冻结；不宣称全1210、粒子容器完整供料、持续功率或保存后restart已通过。

### 2026-10-03：粒子容器路线、Diamond地面前缀与风力预算（整链仍未完成）

source HEAD `776fee1`；installed `3fe31d1`、native `29104`未变，未部署。root完整capture `682d8ec634ae45389ecfcf15e94098ad`的open/pages/P/J/Power/prebuild/details/closing依次为`:1`, `:2–60`, `:61`, `:62`, `:63`, `:64`, `:65–86`, `:87`；共**59页 / 5879 built / 0 prebuild / 11478互逆边**，closing tick **88288293**，不是新保存。root proof `2ec425408a624395a19f65100e0d79f6:1`，SHA-256 **`D42B8AB445F8DA4289A772CC6539F63BDC5AE4EFA1EDBE838038ACF713C82677`**，独立审计35.406秒、0新Game调用。相对上一完整capture `b0a67d710c744189a0db205a22372361`，只新增36条带、2个分拣器和4台风力涡轮机，无删除或未解释静态漂移；仅两项保留cover旋转（5794、5773）获精确原生核验。

本十动作窗口的protected原始记录按动作顺序索引如下；每组均为唯一accepted、同action completed/succeeded、replay=false。序号表示prepare/commit/terminal原件ordinal，动作游戏tick取startedAtGameTick→completedAtGameTick：

| 原始run | 原件序号 | 动作与结果 | terminal游戏tick |
|---|---|---|---|
| `d82636fa9adf4b319b4a05adb0679071` | `:5 / :7 / :14` | recipe7手搓×2，实际产出item2203×2；铁−14、磁铁−6、铜−3 | 88129816→88130336 |
| 同上 | `:20 / :22 / :29` | 风机5838建成 | 88130406→88130889 |
| `5e20d699281441b985e6176f1f090734` | `:9 / :11 / :17` | 风机5839建成 | 88220084→88220448 |
| 同上 | `:32 / :34 / :42` | 风机5840建成 | 88220829→88221398 |
| 同上 | `:62 / :64 / :71` | 风机5841建成 | 88222005→88222465 |
| `b94394a9728d41aa891b9dc04d9b6b76` | `:7 / :9 / :59` | PCH3新增31带，实际有序目标从5843、5842开始，随后5844–5872 | 88253848→88259702 |
| `63c216c7125843ae8eecb7a89e74c58d` | `:6 / :8 / :14` | 粒子容器目的端分拣器5873 | 88265406→88265734 |
| `4f855e8eb00e414da00f5a2acb454ba0` | `:6 / :8 / :18` | 粒子容器源端分拣器5874 | 88268309→88269131 |
| `8b710be45c77475dbdb2732d86565fa3` | `:3 / :5 / :17` | Diamond地面带5875–5879，共5带；尚未接入端点 | 88271739→88272844 |
| `8e8653b0b5ac4f2c86002179643443ae` | `:4 / :6 / :7` | 正常保存，action `b7a9e58b-d792-4c64-986a-d662eb9312d7` | 88283027→88283027 |

保存点为 **88283027 / R56 / J98**；root核十个动作均成功终结、无replay/unknown/在途，external **10/10**、lifetime **120**仍冻结。全窗净变化为铁−14、磁铁−6、铜−3、风机−2（手搓2、建造4）、带−36、分拣器−2；其余库存及玩家位置保持。没有重启、重载或部署；Steam/DSP仍运行。

粒子容器的132带有向路`5760→5761`及两端均由capture核实：`885.slot2 → 5874 → 5760`与`5761 → 5873 → 5326.slot5`，过滤item1206、N3、full serve。5326 detail`:68`当帧output1127=20；这只是capture时点读数，不证明持续供给。J98在`:62`记录一条`production_line_item_first`：item1127/奇异物质，gameTick **88272996**，actualTime **2026-10-03T17:23:54.7304524+08:00**；这证明一次首次自动产出事件，不是持续产率。Diamond带5875–5879仍是地面前缀。5333 detail`:69`未见Diamond/Strange且Lens输出为0；5334 detail`:70`读到Graphite 2、Diamond 100。最近Lens/Strange端点记录仅是tokenless preview，须fresh取得实际ID并走正常原生资格，不能把旧preview当成可提交计划。

N3读数为216 nodes、531 consumers、131 generators，动态capacity **1,806,000 J/t**、required=served **227,586 J/t**、ratio 1。四台风机带来的实际wind credit为**20,000 J/t**，不是铭牌或持续供电。相对声明的整案峰值**1,803,800 J/t**，该帧只有**+2,200 J/t条件余量**；这既不证明长时余量，也不证明1210已产出。Gate 2仍未完成：Diamond/Strange/Lens/Graphite剩余接口、完整1210自动供料、持续窗口及保存后protected restart尚待证明。

### 2026-10-03：Diamond、Strange与Lens有限前缀保存并独立审计

本阶段source HEAD **`69c157ccc488c10b8fecac582ee6ae1bcf851937`**，installed `3fe31d1` / DSP `0.10.35.29104`保持不变，未部署。

root核验的窗口汇总 `134d02c3c37444a1a53803dd5ee36698` 完成10个accepted步骤，耗时233.021秒；正常保存为**88501586 / R75 / J99**，capture closing observation **88516528**，不是新保存。external 10/10、lifetime 130维持冻结；10项均无重放、unknown或在途，本阶段没有关闭、重启、重载或部署。

新增59条带、5个分拣器：Diamond两端为`5880: 5879→5333.slot10`及`5881: 5334.slot4→5875`；Strange A段`5893→5882`新增12带、D段`5894→5905`新增12带、H段`5882→5894`新增27带，NEW对象5906–5932，另有`5933: 5905→5333.slot11`及`5934: 5326.slot8→5893`；Lens地面8带按`5937, 5935, 5936, 5938–5942`铺设，末端`5943: 5942→5329.slot8`。

完整capture `02a5c9f4867840228a29fdcc645cb35f`原件索引为open `:1`、pages `:2–61`、P `:62`、J `:63`、Power `:64`、prebuild `:65`、details `:66–90`、closing `:91`、summary `:92`，共**60页 / 5943 built / 0 prebuild / 11612互逆边**。root proof `3bffcf5f6c6d4fd79a17ae2670abcfde:1`，SHA-256 **`243CF3A6385910EE31E7A1D6074A112B749BFE4813CC87A67D0107055288655A`**，独立审计33.621秒、0次新Game调用；相对前图只有59带和5个分拣器的预期增量，无删除或未解释旧配置漂移。5项旧设备姿态补核见proof `78f528489e254d2da4060f1f12d197f4:1`，SHA-256 **`A8480435EAF3815A9554E7CA3CAAF15E40410A8422A8CB28CDB7C351A2254E78`**。

J99记录`production_line_item_first`，item1209 / **引力透镜**、observed 1、source `factory-production-register`，gameTick **88500867**、gameTime **017d01:43:34**、actualTime **2026-10-03T18:27:51.7537198+08:00**；名称取原DTO的`.name`字段。这是首次自动产出事件，不是持续产率。详情`5333/r101`为Diamond输入8、Strange输入2、Lens输出10且因输出满notWorking；`5329/r78`的Lens/1210输出为0；`5326/r104`的PC/Fe/D输入为4/4/6、Strange输出0。869/r31当帧Graphite 0、acid 2、output1123=0、working=true；870储仓acid 100、Graphite 0。不要把capture时点存量外推为持续供给。

N3当帧216 nodes、536 consumers、131 generators，capacity **1,806,000 J/t**，required=served、ratio 1；相对声明峰值的**+2,200 J/t**只是条件余量，不证明持续供电。剩余施工仅Lens源端和已批准的Graphite并行出口；现有输出→5331路线已完成，无需新增1210下游施工。1210启动正例、正常保存与protected restart后恢复输出仍待验证；本阶段不宣称全链或持续产率通过。

调用经验：固定参数入口完成6次普通prepare/22次请求用时3.58秒，另3次tokenless endpoint preview用时2.575秒；preview不可提交。按原值分别处理`commitAllowedNow`与缺失的`commitAllowed`，从真实Journal/session DTO字段核对，不依赖摘要器猜字段。native终态后施工无人机返航不意味着动作未执行；在下一个prepare前做有界只读idle等待。若终态后的formatter失败，只续读原件缺项，不把accepted重解释为未执行、不重放。

### 2026-10-03：Tail连接、恢复前启动短窗与快照审计

本段只记录有限tail、三个独立启动短窗及其后完整快照，不把短窗当作持续供料/36,000-tick验收或完整自动链。Source HEAD `d8c4fdb`；installed `3fe31d1` / native `29104`及既有64 tools / 1 resource同批能力未变，本阶段未部署。

Tail窗口3个accepted后计数为lifetime133；新增分拣器5944（`5333.slot1 → 5937`，filter1209）和5945（`870.slot6 → 869.slot2`，filter1109），root按原件核实了四端点双向关系及精确2×2011材料变化。tail原件/审计为`698173615fec40c5b3f186f52b385545:1`，SHA-256 `3D72E2BEBCE45FE185DA219C12628F775D20BEC8450DE824FD0947DFF3F75F05`。tail正常保存为 **88629662 / R80 / J99**；cover save后为第4个accepted、lifetime134。

恢复前独立启动run `4fdf834e19bf4a2c9b064e9850759a76`包含三个互相独立的600-tick观察窗；每窗1210 P/C=1/0，5331读数分别为158、159、160，N3 full-serve。startup原件/审计为`1e04fa36c3bd476ab4d6f6d30c3279d8:1`，SHA-256 `F23E897E2A24A8699287C70B917F03C5909B6528F4BCFCBDD267D25C1C844CBC`。这些观察不替代36,000-tick持续验收。

Cover save原件`54797e750c1f4d41b5f8bec457284591:4–10`对应action `5375d5ba-2712-4b32-8f4d-be23a2a5a5a2`，正常保存 **88808715 / R81 / J100**，观察tick **88808731**。其后完整capture `46df142f70ae4fdaa6b1dc11e02db445:1–90`为 **60页 / 5945 built / 0 prebuild / 11620互逆边**，closing observation **88828682**；root审计proof `e497ddfafb35462bb051870945187d1e:1`，SHA-256 `DB14E40E9D5DEDCBF0CD7B9D909B3161FAD0A9485A451D016C54B788C96EB050`，审计25.951秒、0次新Game调用。当前计数external **4** / lifetime **134**，已接受业务动作无unknown、重放或在途，新施工未授权。

当时保存后的protected restart/recovery结果尚未提供；后续核验见下节。最新capture closing是观察值而非新保存；本阶段未部署。完整自动链及36,000-tick持续验收仍未由这些短窗证明。

### 2026-10-03：必要normal restart与healthy owned-primary恢复

必要Gate 2 normal restart原件 `fa50732eff79462fa24e6fc56882d8ad:4–5`记录一次Explorer桌面broker dispatch；既有Steam未关闭，旧DSP进程结束后由Steam启动了新DSP。228项文件哈希全部匹配，没有部署。首次菜单轮询 `239acb6d88b44b5797dd7526738b978d`显示unloaded/unowned ready；随后只恢复已验证的healthy default primary。

恢复原件 `fd917799f37048eea17532e717a14bdc`：prepare `:3`、commit `:5`、terminal `:21`、S/J/P `:22–24`、complete `:25`；action `790e74da-f674-4382-86bf-7f98dc0ecb8f`。恢复保存为 **88808747 / R1 / J100**，观察tick **88808759**。root独立审计 `c810269cb2ee4623be7071c03f920631:1`，SHA-256 `8E98F8E531BD566F516D966C151F49DAB20DF4D68024562B212D581DAE60BE68`，1.055秒、0次新Game调用：确认唯一healthy default primary terminal、Journal身份与条目连续、玩家/库存/位置守恒，无unknown、在途或重放；不记录恢复会话ID。

这证明上述必要normal restart后的owned主档恢复，不证明真实Codex关闭后游戏存活；该专门测试已取消，未做。恢复后1210短窗及完整快照当时尚待采集。当前external **5** / lifetime **135**，无unknown或inflight；新写入因有限阶段停止而被阻止，并非旧十项冻结门。

调用方另曾因包含仅BeltEndpointPreview碰撞诊断文字差异的source-hash mismatch，在任何Game/Bridge之前误拦；root核对228项live文件哈希匹配后改为固定reviewed HEAD与文件哈希权威，保留native版本及全部transaction guards（0 Game/Bridge效果、0 accepted；这是调用方拒绝，不是native拒绝或游戏unknown）。

### 2026-10-03：恢复后1210短窗与完整快照审计

恢复后只读sampler原件 `599570b88f2549919f5fee21d45f6fb9:1–53`：S/J/catalog为`:1–3`，三窗事件为`:20/:34/:48`，对应读取范围`:6–19/:21–33/:35–47`，结果`:50`、closing S/J `:51–52`、summary `:53`。三窗分别为 **88828149–88828748、88828844–88829443、88829528–88830127**；每窗1210 produced/consumed=**1/0**，5331库存 **248/249/250**，N3 full ratio 1、capacity **1,914,000 J/t**。sampler耗时37.717秒，41 sampler reads、46次只读读取；continuous credit为0。原件中的PC三窗读数为 **0/1/0**；883/r99仍为间歇运行，不能据此声称来源稳定或持续供料。

恢复后完整capture `a0a3ce0a2351425ca68142a88c0a07a5`：open `:1`、pages `:2–61`、P `:62`、J `:63`、Power `:64`、prebuild `:65`、details `:66–88`、closing S `:89`、summary `:90`；共 **60页 / 5945 built / 0 prebuild / 11620互逆边**，factory tick **88831552**、观察 **88833228**，R1/save **88808747**、J100、external5/lifetime135。N3为216 nodes / 538 consumers / 131 generators，capacity **1,914,000 J/t**、ratio 1；5331库存254。root独立proof `53d4bb2b10134bf395f5e6adfa52a487:1`，SHA-256 `8944A575EB057597A147BD19F36653538FE94F30651749CE408FFFB6FC121C8F`，审计26.282秒、0次新Game调用：5945项旧pose/config/IDs/连接无漂移，inventory/J连续，无duplicate、unknown、inflight或replay。

至此，normal save → necessary protected restart → healthy owned-primary恢复 → 恢复后1210实际非零产出的垂直生命周期门已闭合；不等于完整持续自动供料或Gate 2完成。三窗均消费0、没有continuous credit，883/r99仍间歇；还需厘清持续来源/库存供料，并按每分钟至少1件、连续36,000 game ticks验收。真实Codex关闭后游戏存活测试仍取消、未证明；下一工作仍限定在1210，不转向燃料、远征或最终包。

### 2026-10-03：恢复档连续1210窗口的非重叠产量下界

本次只读实验沿用源码HEAD `5917b13`、installed `3fe31d1`与DSP native `29104`，未部署；primary save仍为**88808747 / R1 / J100**，最新观察tick **89381782**。Steam与DSP保持运行，用户取消了Codex关闭存活测试；本阶段不作该项声明。external **5** / lifetime **135**维持，有限阶段结束后写入门被阻止（不同于旧十项冻结）。

前一次独立连续尝试 `20e7a8696036493fa9892ab5f293b2f5` 的第41样本遇到347-tick缺口，重置后结束只有33,924 ticks信用，未达连续门；其信用未与本次拼接。旧root proof为 `8ddaef91f2ad45f3968b299e1003e744:1`。本次固定声明 `1e7be92b`对应一次93样本实验，包含1498 sampler reads、共1502次native请求，耗时605.75秒。完整合格连续段为 game ticks **89345606–89381766（36,161 ticks，reset=0）**。run `a109041db94d4e05948b1b1fde4dd060`原件：首末sample `:26/:1594`、production `:25/:1593`、N3 `:24/:1592`、experiment result `:1596`、closing S/J `:1597/:1598`、summary `:1599`。root proof `ed6bab599099432b8bc3f8e68bafaf00:1`，SHA-256 **`0DFD09DCCD2C8B39A43B9D2E4CBF0F25DBCA14D8D3F2952E669D53592D642D0F`**；独立核验19.840秒、0次新Game调用。

同一连续段内，对1210的非重叠闭区间计算给出**16件保守下界**，而“每分钟至少1件”在36,161 ticks上要求至少`ceil(36161/3600)=11`件。该16件是下界、不是精确总量。12种被查询物料均在本实验窗口内出现新产出；item1121即使首末读数都为0，非重叠下界仍为**160**，故不得用端点快照判成整段氘产出为0。粒子容器相关缓冲读数885为**2782→2777**，Warper 5331为**469→485**；N3采样ratio为1/full-serve、动态capacity **1,914,000 J/t**。这些同窗数据不能单独排除库存缓冲影响，也不证明供需配平或无界持续供给。

完整静态capture仍沿用旧原件 `a0a3ce0a2351425ca68142a88c0a07a5:1–90`（60页 / 5945 built / 0 prebuild / 11620互逆边），并非新连续实验快照。对应采样实现的35项PowerShell检查及commit `5917b13` 的Windows Core CI `37131314183`已通过；46项私有算术fixture只验证区间计算，不是Game证据。保存/恢复后的启动、连续覆盖和最低新产量分别已证明；全链供料配平、缓冲排除和Gate 2均未通过。

同一阶段随后完成固定14实体Fe→Motor→Turbo→粒子容器来源包 `38d23ac9043744d1ab4e24d9006b09cb`，20次只读请求、4.11秒：opening S/J/catalog/power `:2–5`，14详情 `:6–19`，closing S/J `:20–21`，summary `:22`。闭合**89481382 / R1 / save88808747 / durableJ100 / external5 / lifetime135**；N3 required=served210045、capacity1914000、ratio1。1500/r1当帧铁矿4、铁块输出100、notWorking；1511铁1820、723铁0、Motor727与Turbo827为空、PC885为2754；814/r98和883/r99当帧Motor/Turbo输入为0但正在加工，不能把单帧零buffer等同不生产。此读数不证明哪条分支正在抢料、唯一传输瓶颈或升级收益。

root静态读回proof `8fb811c0a9bb4a2b86c2dca088835743:1`，SHA-256 **`FB5F6D47B0E4CFDE90939A30238686AB67F9334DC65B7FD3158AA0E9B376DA3C`**，14对象旧配置/连接一致；边界proof `57d0ffcbc7364d7c84892fcb03f6e2f3:1`，SHA-256 **`8B229858A77FD86E4B3F17D5988F361AB6B5A76F00B2EA48943D324EA53B291B`**，独立核20原始响应/owned身份/Journal与功率，均0次Game调用。源包开头一次非终止调用方错误未阻止原响应落盘，没有重发；root首次离线摘要误读不存在的嵌套inserter字段，修正后仅复用原件，没有新Game读取。另修私有dot-source helper的同名load-only参数覆盖，避免主审计提前零证据返回；不是native失败或游戏unknown。下一项只核现有1512/1535/2317铁输送实际容量及分支分配，先完整预算和fresh native资格；未批准新施工、升级、材料准备或重复长实验，不转向其他Gate。

### 2026-10-04：铁线原生升级、连续采样结束与十写封窗

源码HEAD `386feb63e7328b43948ef3a10eb1344877743812`；installed `3fe31d1`、DSP native `29104`与既有64 tools / 1 resource保持不变，未部署。Steam/DSP保持运行，本阶段没有关闭、重启或重载。

两次手搓原件 `3d5ae9e670554d8984cd6dd48163b9bb`：recipe r97×1、r88×1，耗时16.6秒；递归库存核对为Fe−3、Mag−2、Cu−1、Coil+1、basic−2、fast+2，Motor最终0。proof `0420f16838484126b456ccf5af897a10:1`，SHA-256 `617AEF3C50AAB78B8B7BD4D594801C6F99A774CD717AD76E5092F08BB817BD60`。

原生升级run `fb1291322f394076a20a9768882576fb`：prepare `:8`、commit `:10`、terminal `:11`、readback `:12–15`，action `b56c3d79-1f0e-4452-9670-ae225e76137c`。entity 1512从proto 2011升级至2012，原生循环时序参数`progressRequired`由600000降至300000（不是功率）；循环进度比例、货物与过滤保留。既有1500.slot1→1512→1511.slot2与1535/5635分支不变。材料fast−1/basic+1；没有新增实体、带或移动库存。整段调用与读回4.638秒；prepare→终态调用666.535ms（不是纯游戏等待）。proof `22a281654b634ee08c44742bf967872c:1`，SHA-256 `35C198D8C7EAB704715C6363198073B29A6E64B5EF79DEF346668B338959958D`。

升级后只读三窗原件 `0fb3f2d350114f1ba54a6619cfee8221`，样本范围`:14–28 / :37–51 / :60–74`、窗事件`:29 / :52 / :75`，68 sampler reads、73次native请求，118.274秒。窗口分别为89736674–89737273、89737321–89737920、89737963–89738562，中间缺口48与43 ticks，因此continuous credit=0。Fe1511库存1660/1665/1669，PC885为2700/2700/2699，1210产出0/0/1，Warper 5331库存647/647/647，N3 ratio=1。proof `caae1e84eae349598deb2b1868cd6285:1`，SHA-256 `27A8CA73C8AA29F2097E130F8A47AA214BA14AD7762CFEF46D216B9A411D6F15`。这些短窗不证明升级提高持续供料、Warper已送入库存或全链配平。

正常保存run `842bb0bd052447b7956510ac4c28a153`：prepare `:4`、commit `:6`、terminal `:7`、S/P/J `:8–10`，action `a660a977-367b-4521-8950-829616bf5849`，保存 **89785733 / R8 / J100**，最新独立观察 **89785736**。external **9** / lifetime **139**；保存覆盖9个accepted，全部已核销，无unknown、在途或重放，保护票据可用。root proof `c30822c984b047dba5aeb929f079cdae:1`，SHA-256 `0982B16CEA9F57A1727317DA9A5CA2B033F0517EC34F7F859A40448B800469E1`。

采样预算修复位于commit `386feb63`：初始单次timeout上限为3600秒，以覆盖约15–17 ticks/s下36000 ticks约需的40分钟；原180秒默认、12物料、120样本、4096请求、600-tick窗口与只读约束不变，运行后不得续期或重放。41项离线检查通过，Windows Core CI `37146080169`成功；这不是长窗通过证据。

只读采样run `db555450498b4ed79a316dd465950ed1`已终态：97样本、3054 sampler reads、3058次native请求、墙钟2177207ms。第8个样本触发sample_gap重置，最终有效连续段为 **89799586–89835686 / 36101 ticks**。按非重叠区间核出的1210新产量下界为17，超过该窗口按每分钟至少1件所需的11；17是保守下界，不是精确总数。PC库存全程2685→2678，有效连续段内2686→2678，故该窗仍净降8。root原件proof `4fde41bf31b742fb8601fc86d98dda01:1`，SHA-256 `96FF13817F977027BAA1A216DBC220EDC2D733795EE4A0BC235C3CFB13F0A60D`。此结果不证明来源配平或完整供料，Gate 2保持false。

最终正常保存原件 `cf93e8dde9dc4a859df5f5ad561e9222:1–11`：prepare `:4`、commit `:6`、terminal `:7`；覆盖全部10个实际accepted。保存 **89850963 / R9 / J100**，external **10** / lifetime **140**，十写冻结。其后完整capture `44ff771d072a46e4bf3b16e56bd26d64:1–89`为60页 / 5945 built / 0 prebuild / 22项details / 11620互逆边，snapshot tick **89851206**、capture close **89851625**；全厂其他静态配置一致。root十写审计proof `5f6adea51edd4a41a6f2d590f3004db2:1`，SHA-256 `09C7FED9838B2F6683EA8075A69006A8A260784F2522A041F3CF6BAD16787F3F`，0新Game调用。206项动态buffer差异保留在原审计，不在此逐项列出。最新只读run `9de2b23384f24bc6bf8d358565fc85f8:1–7`于89867928闭合，仍为R9/save89850963/J100，无unknown、inflight或replay。

本窗唯一批准的设备升级是1512从2011到2012；`progressRequired` 600000→300000是原生运输周期参数，不是功率。1535/5635分支保留未变。铁链问题仍以`1511→1535→2317→2351→723/724`为待核路段：1511在累积、723铁块库存为空、Motor缺铁并传导至Turbo/粒子容器缺供；现有证据没有唯一指向某个分拣器，不授权旁路扩建。矿机1496覆盖由8变7；node48的原生`INVALID_ENTITY`证明对象不存在，不能记作amount=0；node44仍有17939铁矿且矿机继续生产。当前DLL的`MinerComponent`在amount≤0时调用`RemoveVeinWithComponents` / `NotifyVeinExhausted`，可解释这项有界耗尽变化，但没有观测精确耗尽瞬间。本次upgrade尚无protected restart验收，DSP/Steam保持运行；专门Codex关闭存活测试已取消。本阶段继续冻结，须另经root交接后才开始后续工作。

### 2026-10-04：既有peer升级、独立短窗与连续采样核验

源码HEAD `036611f6471d71e3c79abb3d77b2a45ae4cc9b7d`；installed `3fe31d1`与DSP native `29104`不变，64 tools / 1 resource能力沿用已核版本，本阶段未部署。DSP与Steam保持运行，没有关闭、重启或加载；Codex退出存活专项测试已取消。

原件 `fc8f0f0e907d4f51a50b6e1ddde97332:1–73`记录两次手搓（recipe r97/r88各1）、1535与2317各一次2011→2012原生升级、以及正常保存，共5个unique accepted；五动作执行入口耗时15.159秒。root proof `b0eb2e90939b4469a132d061e9722fa6:1`，SHA-256 `C23368E9698C27BDA55A6C008ED378D18F2826112AC08EE263EA40D2F7F0CE54`，独立审计3.906秒、0次Game调用。后续连续采样耗时另列，不计入该五动作入口耗时。

净库存变化仅Fe/item1101 −3、coil/item1202 −1，其余物品不变（包括basic、fast）。r97使用了已有coil，不能套用旧的“零coil补料”差量。两项升级的native cargo、循环进度比例、filter 0/1101、peer双向连接与全配置均保留；本次返回的object ID仍是1535/2317，但不将ID恒定作为保证。原动作索引无build commit，本阶段也未批准新增实体；这里是目标与peer读回，不冒充完整全厂快照。

正常保存为 **89919581 / R18 / J100**，保存后观察 **89919585**；后续三窗closing观察 **89930835**，仍为同一save/Journal。external **5** / lifetime **145**，无unknown、inflight或未保存结果。三窗只读run `4617f3783f314703b433e80c82c264cf`含82 native请求、125.206秒、0 writes。root proof `70009949864a4fdd862b961f036447db:1`，SHA-256 `58FDF17441C1992AA6EED7F50E99759D2D32F04CF325DADBDAF8FBDFD5164EFF`，0 Game调用。

三个彼此不重叠的600-tick窗口为：89928880–89929479、89929516–89930115、89930228–89930827，间隔36与112 ticks。这是独立样本模式，不累计连续信用；这些间隔也不能拼接成连续实验。按窗口读到的生产/消费（P/C）值如下；单窗正值、P/C相等或短周期波动均不是持续供给通过，单窗零产出也不直接判长周期失败：

| Game tick窗口 | Motor 1203 P/C | Turbo 1204 P/C | 粒子容器 1206 P/C | Warper 1210 P/C | D 1121 P/C | Graphene 1123 P/C |
|---|---:|---:|---:|---:|---:|---:|
| 89928880–89929479 | 2/2 | 1/2 | 0/0 | 1/0 | 5/0 | 2/2 |
| 89929516–89930115 | 3/2 | 1/0 | 1/0 | 0/0 | 0/0 | 0/0 |
| 89930228–89930827 | 3/2 | 2/2 | 1/2 | 0/0 | 5/10 | 0/2 |

同三窗buffer序列：PC885 2660→2661→2660，Warper5331 734→735→735，Fe1511 2990→2990→2988。723的Fe读数三窗均为0，但724输入与生产有实读，因此不能以仓缓冲为空认定没有铁运输。N3每窗采样ratio=1；另行核对的16个实体静态配置（含grade）一致；声明峰值容量预算是独立核对项，结果满足。以上只支持这些短窗时点事实，不能外推整链供给或长期比例；首次离线摘取误用了不存在的`cargoItemId`投影，移除无用投影后从同一已持久化原件完成核验，没有重读Game、重放或新写。

此前有界连续run `db555450498b4ed79a316dd465950ed1`的信用不复用于后续实验。新只读连续run `64251c2a8f0a419e97b5f0d59e6c2f46`按声明原件`:3`、UUID `a0797419-b49e-4997-b5ad-f5cefeef1f43`运行：25实体/12物料，cadence 330、native窗口600，目标至少36000 ticks，最多120样本/4096 native请求/3300秒，poll间隔3秒，0 writes。终态覆盖 **89940003–89976009（36007 ticks）**，87个有效样本、0次重置；sampler读取3008 native requests，墙钟1988.885秒。root汇总的原始请求（含开、闭各两次）共3012 native；原件proof `ff639bc12b774b248eacf60ea4a8200d:1`，SHA-256 `635C6056B926EDD8F2DCBCD432A7B05831ED3F838E0C33AF462748BBE9F52D1C`，离线审计40.748秒、0 Game调用。连续模式按`covered`判完成；`qualifyingWindows=0`仅是独立样本模式计数，不是本次连续实验失败。此87样本窗口未采870石墨/酸库存、5187石墨上游库存或3343上游氢库存，因此有限上游库存排除仍缺原证据；这是证据未覆盖，不表示这些节点停产。

按非重叠区间计算的1210新产量保守下界为17，超过该窗口按最低1件/分钟所需的11；17不是精确总产量。Warper5331库存739→756，PC885库存2662→2665（净增3）。这是连续产出及PC净库存正例，不证明Governor、全源供料归因或已排除所有上游有限库存；不把PC库存短窗增量解释为全链配平。root仍需核sourceSupply与finiteBufferExclusion的上游证据。窗口结束观察为 **89976016**，save仍为 **89919581 / R18 / J100**，external **5** / lifetime **145**，0 writes；无新restart。之前独立通过的恢复后输出正例仍有效，但本阶段sourceSupply、finiteBufferExclusion、full-source与protected restart均未通过。

### 2026-10-04：32实体范围的连续1210采样与来源归因边界

本阶段sampler source HEAD `9be6b5fa5355162dcd2496a6539e07a88f983f22`包含只读采样caller物料范围修复：唯一`ItemIds`参数上限由12改为16；原生600-tick窗口、120样本/4096请求及只读/停止规则不变。对应`test-production-sampling.ps1` 46项与`ProductionWindowGuidanceTests` 2项离线测试通过，0 Game调用；Windows Core CI `37161579848`针对该SHA成功。采样helper改动没有部署；运行中的DSP仍是installed `3fe31d1` / native `29104`，64 tools / 1 resource。以上为代码与离线验证，不是游戏采样结论。

此前caller尝试`3cb0eaac:1–4`仅成功读取开场S/J两个响应，采样尚未开始；原声明35实体、15物料超过当时32实体、12物料的caller边界。该尝试是2次native读取，不是零Game请求；无样本、accepted、write或可复用的连续信用。新实验使用固定32实体/15物料范围，所有预算在开始前声明。

连续只读run `0ec62d89ef9d410bb468d5c08c492ea3`：声明`:3`、结果`:3502`；82个样本、reset=0，覆盖 **90066166–90102290（36125 ticks）**；3412 sampler reads、3416 native请求，墙钟2075.794秒（非纯等待时间）。root proof `2abcbc714cd94daebf69bdb636f17634:1`，SHA-256 **`8F75A06F027BF82F9F55EBB778B5DE724BBD5F91CACBFDA859491464254EF2A8`**，离线审计94.7935023秒、0 Game调用。该段1210非重叠新产量保守下界为**16**，超过最低1件/分钟在36125 ticks内所需的`ceil(36125/3600)=11`；16不是精确产量。Warper 5331库存798→814，PC 885为2677→2680；N3采样满供且容量满足本次声明。

此连续段还观察到：870石墨端点库存0→0、酸100→100，863酸600→600，3348氢0→0，3074氢/氘0→0；Fe 1511为3000→2999（段内最小2998、最大3000）。这些是有限采样窗口的端点/区间读数；其他上游生产buffer可周期波动，不能据此认定没有流量或完成来源归因，也不能证明已排除有限上游库存。窗口closing观察 **90102297**，当前save仍为 **89919581 / R18 / J100**，external **5** / lifetime **145**；无Game写入、unknown或inflight。Steam/DSP保持运行，本轮没有restart，专门Codex退出存活测试已取消。

随后以原完整静态snapshot核对既有source候选：catalog中Acid 861、Graphene 869、D 3073、PC 883等各自唯一。fresh只读原件`65e83ad530dd42f98ea3950e1a4f7635:1–7`显示707/r16单帧氢output=20、oil=0且`isWorking=false`；163/784当次点读只观察到item1120氢库存（296/130），未见精炼油库存。这只纠正当时库存标签，不否定候选输油配置或油曾经流经该处的可能，也不证明707持续停产。原完整静态snapshot中，709/906所在以及784→3715/3125候选路径上的所有分拣器均filter=1114；3964→4188→3966候选链也全部filter=1114。因此这些静态候选在过滤物料上与精炼油item1114一致，但不证明实际流量或持续率。93/r4输出石材，26是混料仓，不能按未核物品的BFS认定它们是所需原料来源。fresh partial proof `8dd872d6e1d04729be7db80b1045d8d8:1`，SHA-256 **`DD89CCC5792F2808C5659732FECA0FD38112F5F08E5EA4FAE3177F0192CD3F4C`**，0 Game调用；仅证明当次点读，后文静态filter结论来自原完整snapshot，不属于该point proof；不代表实际供给通过。

连续原生计数的unionCountBounds（不把重叠窗口相加）为Acid P44–68、Graphene P80–136、D P165–230、PC P37–50、1210 P16–23；这些是非精确边界，不等于产量分配。油、氢与石墨均有多个producer，氢还包含r58回用。本窗没有记录Oil 163/784及旧支路95/862/753的动态趋势，因此仍不能排除有限上游库存或定位停产原因。来源proof `07902087fae1435485f52c1b5985e2d7:1`，SHA-256 **`313803B9B355B396DEB0D28794F6726887725B565DAE57B0BFED4C38335874D5`**，0 Game调用。

因此`sourceSupply`、`finiteBufferExclusion`与Governor验收仍为false；静态路径与本窗buffer边界不足以证明完整供料归属或有限库存排除。此段不构成完整供料配平、无界持续率或protected restart通过。此处记录的partial原件当时仅含7个native响应；后续只读续读及source-point结果见下段，不将两个原件误称同一快照。

#### 2026-10-04 来源点包续读与实物过滤边界

root独立核验的point proof `015fec99132443baab4a9ebf412851a0:1`，SHA-256 **`7140FDA76DA4511928AC4B86AA2A1FAB3BE673D8F39B7C8D12E42DFD13D1577A`**，0 Game调用。原partial `65e83ad530dd42f98ea3950e1a4f7635:1–7`后，续读run `ae9a2db302924e95b32d178d47872592:1–15`只读尚未读取的对象；两份原件合计22 native响应、14个唯一实体。所有只读审批均关闭，0 native拒绝、0 writes、无unknown/inflight；一次本地摘要字段失败未影响原生响应，不作重读或重放。closing **90141554 / R18 / save89919581 / J100**，external **5** / lifetime **145**；本续读没有新长窗或restart。

逐项runtime库存纠正了静态BFS物料假设：163的item1120氢库存296、784的item1120氢库存130；862有item1005石矿3000；95有item1005石矿100及item1108石材2900；753有item1000水600；26有item1104铜100、item1102磁铁2400、item1301电路板3000、item6001蓝矩阵400。六个采集器86石矿、752水、2802原油、5171煤、1496铁、2440铜在各自点读时均`isWorking`且输出非零，节点数依序为1/0/1/6/7/8；这是瞬时状态，不是持续供给验证。水泵读到空的离散节点列表不等于缺水。1496的缺失矿点48与旧十写proof `5f6adea51edd4a41a6f2d590f3004db2`所证自然耗尽相符，其余配置严格比较无新增变化。

因此应将结论限定为：163/784的当时库存标签为氢而非油，既有filter=1114的静态路径候选物料一致，但实际油流、持续来源和上游有限缓存排除仍未证明。油的零端点和707单帧idle都不能证明没有油流或持续停产。下一项仅按过滤方向和runtime输出核验真实精炼油到861/3084，并排除上游有限缓存。point结果不使`sourceSupply`、`finiteBufferExclusion`、Governor或grade restart验收通过；不得据此推进Gate 2。

### 2026-10-04：Gate 2保存、必要重启、恢复与恢复后短窗

本阶段源码 `0b3dd2f` 的Windows Core CI `37176748646`成功；运行中的Plugin/MCP未更换，仍为installed `3fe31d1` / native `0.10.35.29104`（64 tools / 1 resource），没有重新部署。较早的连续只读sampler `8d3f2a538b5b40119f64ded602b95b61`覆盖 **90392580–90428970 / 36391连续ticks**，85样本、43次完整实体观察、reset=0、2816 sampler + 4开闭读取=2820 native，墙钟 **2092353.8333 ms**；root proof `e6eab6f392014500ae82b3fa49fa45da:1`，SHA-256 `DFB7E4FA8AF6CFC1A5333C7E4B63F4C2ECB9E2E2ED2AF60A305F83F1886FF1EB`。独立minimum-source资格只覆盖最低1件/分钟门：1210非重叠下界 **16** 对需求 **11**，PC **2711→2716**、Warper **950→966**、净氢下界 **290** 对需求 **220**；source proof `44c6537a889a44888de962e3a262d898:1`，SHA-256 `0F665EE1B5AE9C085F973CDB6386EA9B972BEF07F1F649CC9AE0CA613210C58C`。二者都不证明精确产量、完整sourceSupply或排除有限缓存。

此前正常保存原件 `1193a2656645437d88c6434ba88d079a:1–10`，action `932fa70a-0ca4-4cca-ada3-a1ac691e6148`，保存 **90474698 / R19 / J100**，external **6** / lifetime **146**；root审计proof `9b7fc90d4bc24eb48cf07ba9070f3a5d:1`，SHA-256 `23D507C63258B4173F278BDDABB726529CD2A4DCA88A3F964B2AB87BD03B4B1A`。后续手持/库存/位置核验 `359affff4b90454e80c442bf236b0570:1`保持一致。保存返回成功并已持久化；随后本地格式化误读`player.revision`，未重发或重放。

获准的Gate 2必要DSP重启原件 `0ade:1–5`，菜单轮询 `1dcf:1`，恢复run `a9f30:1–25`。Steam进程保持打开，228项安装文件哈希一致，没有部署；此处是必要DSP重启和primary恢复，不是Host/Codex关闭存活测试。恢复唯一终态为 `:21`，随后同一owned primary进入健康状态并正常保存为 **90474729 / R1 / J100**，external **7** / lifetime **147**。root审计proof `ac67514532ab4c0688e468038a9a6135:1`，SHA-256 `7798090E23FF30E8090DE4695B4167C73A9C6F0150EF71F4CE37739630DC359F`。prepare字段`exactEmbeddedIdentityVerified=false`，因此不声称加载前已完全读回内嵌身份；native terminal adoption/readback之后才核实同一primary。位置Y差1微米，处于既有1毫米容差内；库存、手持物与位置守恒，不推断差异成因。用户取消的Codex退出存活专项测试没有执行，Steam未关闭。

恢复后完整capture `183568:1–84`为 **60页 / 5945 built / 0 prebuild / 17项details / 11620互逆边**，未新增或删除实体；本阶段将1535、2317从2011升级到2012，既有1512在baseline已为2012并保持，其余静态配置一致。仅已取证的2440/node168耗尽差异许可，198项动态buffer变化仍属动态读数。root proof `d7fc1a41af3a4e67a06e5e259c618d34:1`，SHA-256 `64D0B819291E6E69BEED440DACD10D8443C9812A9C974878B13E60B7C7105B0C`，审计29.223秒、0次新Game调用。

恢复后六个互不重叠的600-tick只读窗原件 `c2c6ae9c3199460380d5b6a359ac79f4:1–143`，marker为`:28/:50/:72/:94/:116/:138`；共133 native请求（128 sampler），77.276秒。1210各窗生产/消费为 **1/0、0/0、0/0、0/0、0/0、0/0**，六窗合计原生实产1；`coveredContinuousTicks=0`。PC库存序列2728/2729/2727/2728/2729/2728，1210库存1011/1012/1012/1013/1013/1013；库存净增2含窗间未采样生产，不能解释为六窗产出3。N3均full-serve，capacity **1,878,000–1,914,000 J/t**，不低于本次声明峰值 **1,804,700 J/t**。root proof `45a1756ad0844cb2b93e172832a77837:1`，SHA-256 `AF988AC6A02B44EA05AA32C705793F609A35B173A6487AC0F0A4E465F2443713`，离线审计3.218秒、0次新Game调用。

最新原件观察 **S90530020 / save90474729 / R1 / durableJ100 / external7 / lifetime147**；无unknown、inflight或未核销动作。恢复后出现过一次1210非零原生生产，因此“保存→必要受保护重启→healthy primary恢复→再次非零”这一窄垂直门通过；这不是持续率证明。不得把最低资格写成gross产量或P=C。`finiteBufferExclusion`仍false：在途货物及141/707窗口内buffer未完整界定；Governor与完整sourceSupply仍false。六短窗不累计连续信用，也不证明无界供给、36,000 ticks持续产出或Gate 2全链完成。Steam/DSP保持运行；用户取消的Codex退出存活专项测试不属于本次验收。只读阶段批准已消费，Game writes仍blocked。

### 2026-10-04：完整物料前沿与粗等待造成的采样缺口

从同一5945对象不可变快照和runtime配方逐级反查1210物料前沿，离线投影定位22个生产设备、21个仓储前沿、8个原料源，未解析输入为0；补入旧范围遗漏的298/884/3064库存与707来源。投影仅为方向/过滤后的静态候选，不冒充实际供料。配置binding `778def25da8b4c9b8a576fd0aa54d9ff:2`，SHA-256 `533B0215768D8BA36E05407E0F08B3CF7C81C6082EB1F7A00CE4B708B1F74A47`。固定48个关键生产/库存对象、24项物料；8个原料源仅留开闭原件，不写成逐样本新观察。

原件 `53edbdb6c5f64b2782994d296644c876:1–3623` 在source `0761dcf`、installed `3fe31d1` 上只执行一次：120样本/60次完整实体观察、3499 native（3479 sampler+20开闭）、墙钟1159085.8331 ms，0 writes。root原件审计 `7e2c4400e9084371ae09aa0339906698:1`，SHA-256 `52D68649B6B134ED47E85CEC6170E47FF4A809B80875BB92CA918840B96D9551`；120次健康/静态配置检查有效，但42次`sample_gap`重置，最后仅1133连续ticks，结论`not_proven`。closing **91240764 / R1 / save90474729 / durableJ100 / external7 / lifetime147**；无unknown/inflight，审批已消费、执行已停止、禁止重放。

root独立等待诊断 `0f142ec7f1fb48a3a7cd85a50d010d91:1`，SHA-256 `2546F86F98A73ACF64A422D6A1D37E53B4028D60EB56441EA7C9157EEB42A056`：`:124`距目标还差39tick，调用方却继续睡满4秒；`:127`与前窗相距603tick，形成真实3tick采样缺口。全部42次重置均为缺口（41次完整观察、1次counter-only），不是产线健康检查失败；不能将其写成产线停产，也不能补插缺口或拼接信用。首末完整观察的1210库存1313→1343、PC2800→2807、所有选定非零库存无净下降，是分离现场趋势，不是连续验收或完整有限缓存排除。

必要最小修复只改已有共享采样器等待：复用已有session回执，以单调接收时钟测native tick速度，临近目标时缩短睡眠；`PollSeconds`仍为上限，原deadline/read/sample cap、fresh校验和严格gap/reset不变。默认cadence1/independent不改，无游戏写入、自动重试或新planner。离线60TPS/120ms native延迟fixture对比仅证明调用方调度：旧4秒等待120样本、59次gap、未完成；新等待可在相同上限内覆盖≥36000ticks且gap0，不冒充实机或token节省。Claude流式外审terminal为BLOCK（`94e647ba297e465eb460bb564dbb5b7f:1`），所疑`PollSeconds>10`经root核实际未改入口`ValidateRange(1,10)`和11秒零native调用负例，不适用于有效参数；不将外审改称APPROVE。root核销 `75b528dc47894e0aa5122659573e4821:1`，SHA-256 `9CB506D4673A908F0EF7CBBB49424CB3F55817EFE35214B38B4568AA1DE51A22`。新实机须另行固定声明、从零计连续信用。

修复精确三文件提交并push为 `ff362e0a130b1b6ba3477bbdc3dbfb29a82d4a53`，最终离线sampler85/85、MCP指南2/2通过，均0 Game调用；root核远端相同SHA及Windows Core CI `37195880987` completed/success。原research及两项test dirty保留，未混入提交。运行Plugin/MCP与228项安装文件仍为既有cohort，未冷部署；共享caller修复不等于包内已安装实现更新。另一个从零计信用的固定只读声明 `551ae66a-478d-4797-a7f3-d83a9881ad65` 保持48对象/24物料/8开闭来源、120样本/4096读取/3300秒原上限；旧53ed不能重放或累计信用。真实Codex退出存活专项测试取消，不执行；不为观察主动关闭Steam/DSP。

#### 同一会话的新适应等待实机正例

新声明唯一执行 `5e1cf80452674139b528c67cf47b30ef:1–2879`，原handle31164正常exit0；无worker在途。89样本/45次完整48对象观察，2786 native（2766 sampler+20开闭），墙钟619332.6737 ms；连续 **91464990–91501193 / 36204 ticks / reset0**，0 writes/0 accepted增量。root直接核原件与静态配置、窗口/覆盖/供电、45组buffer及8开闭来源：proof `ad89fb2801274af68d9d520014afac1a:1`，SHA-256 `A6B7024893380D21501888BD026422743B85E39AF2430FA0AF4835B228909DEC`，独立离线审计全程56088.0418 ms、0 Game调用。新鲜1210非重叠下界 **18≥需求11**；PC2832→2835。1210仓1448→1465的净增17与native下界18不是同一测量区间，不能相加或改写成精确产量。

root复用同一原件运行既有非负区间等式算法，不相加重叠窗：material proof `9802245ba8aa4c7d850bc2258fd87ce3:1`，SHA-256 `D25577742F382097E4E621E25DBFBA130F1259366463714629094B3D02CEBDAD`。24项输入/中间产物/1210计数均保留上下界；1210总生产可行界18–24，不是精确值。所有选定非零库存无首末净下降，变化仅PC+3、871石墨烯+12、163氢+8、1210+17；各对象实际观察时间保留。原油/精炼油/氢等净产量界跨零，不得据此宣布净下降、缺供或配平；氢gross含裂解循环，不能当外部净供给。

同一48/24/8范围、390目标及原上限，实机观察从旧1159085.8331 ms/120样本/3499 native/42次gap且未完成，变为新619332.6737 ms/89样本/2786 native/0次gap且完成；这是一次阶段实验结果，不是全部开发流程提速比例，也不证明token或价格减少。新旧世界进度不同，产量变化不能只归因等待修复。source仍`ff362e0`、installed仍`3fe31d1`，未部署、未save/restart/关闭游戏，未做取消的Host测试。

closing **91501262 / R1 / save90474729 / durableJ100 / external7 / lifetime147**；无unknown/inflight/未核销accepted。新声明已消费，禁止重放。最低持续率门及此前保存恢复正例保留；`sampling_completed`不自动使`sourceSupplyAcceptance`、`finiteBufferExclusion`、Gate2或Governor通过。关键141/707窗口buffer缺口已补为45次实际观察，但未界定运输中的有限缓存和完整来源归因；下一项只能设计最小物料/在途cut核验，不再次盲跑整厂长窗，不新增永久施工、未来库存或其它Gate。
