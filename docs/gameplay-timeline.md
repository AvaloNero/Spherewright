# 存档日记 001：从落地到当前的决策、科技与首次产出

更新时间：2026-09-05（Asia/Singapore）
公开存档 ID：`owned-world-001`（真实存档名不进入仓库）
当前截面：同一 Spherewright-owned 普通和平 1× 非沙盒世界已恢复到母星 planet `104`。v0.3.3 隔离验证遗留的 active ticket 先正确载入早期测试副本；关闭该副本后，长期档只通过已归档的精确 owned proof 和 header 恢复，未枚举或依赖存档名前缀。首读发现同档 Journal 被重建为 0-entry 后立即停止所有游戏写入；DSP 停止后保留证据并以 journal/owned/game-version 三重身份恢复唯一 49-entry 备份。修复版 `7e44e48` 已把新恢复票据绑定到该 Journal 的 tracking tick `4428079` 和 minimum durable sequence `49`：正常恢复通过，缺失与连续 sequence `1..48` 截断两种负例都在 prepare 阶段 fail-closed、没有启动加载，恢复原件后同一票据于 tick `18291377` 成功复归。clean `8c49bcb` 的实际候选包安装后又一次恢复到 tick `18319586`，安装态 MCP 的 3/3-factory bundle 到 tick `18334303`。Luna Max 随后只改正蓝矩阵输入 sorter `573` 的过滤值，再把已有自动高能石墨从源仓经玩家守恒转运到钻石线输入仓；最终 bundle tick `18489233` 证明蓝/红/黄矩阵分别 `12/6/12 min⁻¹`、全部 finding 为 0。`3401` 于 tick `18593844` 自然完成并保存到 `18639872`；完整目录审计后正常选择 `1608 配送物流系统`，Journal sequence `50` durable。`1608` 于 tick `18747873` 自然完成，普通保存到 `18750214` 后完成本安装 session 的严格十写审计：fresh revision `13`、owned/saved/healthy、Journal `50/50` durable、0 pending/error、无 blocker/checkpoint，十个 accepted writes 均已由 terminal 与 fresh 状态核销。recipe `122/123` 已只读确认分别产出物流配送器 `2107` 与配送运输机 `5003`；下一步只读盘点现有物料与可复用设备后，优先落第一条可持续配送物流产线。自动连接 `114 -> 716` 的有界候选仍因旧带/设备占位或安全净空不足 blocked，不能把此前 2000 件人工守恒补料冒充自动化。v0.4 候选等待 owner review，未 tag/release；真实 carrier 连续 600 tick 完全静止仍是普通游戏接口无法安全制造的明确 live 覆盖限制。

## 结论与证据边界

- 这里的“存档日记”是仓库内的人类可读整理；“运行时 Journal”是逐存档自动落盘的机器可读原始首次事件，两者不是同一个文件。逐档约定与登记见 [save-diaries/README.md](./save-diaries/README.md)。
- 记录仍在。本局受保护 Journal 共有 `49` 条，已经持久化到 sequence `49`，`persistencePending=false`，`persistenceError=null`；仓库中的经验账本当前共有 `182` 条决策/经验，完整保留了每条的状态、证据和复验条件。
- 这是 Spherewright 在这台机器上从普通新档创建并从落地开始推进的同一世界，不是接手或枚举得到的既有存档。后来更换 Steam 账号不改变归属证明；Steam/Windows 身份从未被当作存档所有权依据。
- 首次事件日记是在既有世界运行到 tick `4428079`、本局 `000d 20:30:01` 时挂接的，字段明确为 `historicalCoverageComplete=false`。因此：
  - 从 sequence `1` 起的首次手搓、首次流水线产出、首次点科技/升级，拥有精确实际时间、tick 和本局时间；
  - 更早的科技完成时间可以由游戏运行态的 `unlockTick` 精确重建，但更早的“点击时间”和实际时钟时间已经无法诚实恢复；
  - 更早的首次产出只能给出结构化读回、保存 tick 或 Git 里程碑形成的可靠上界，不能伪造为精确首次事件。
- 本文使用四种证据：`J` = 持久化运行时 Journal；`R` = 当前存档运行态；`M` = 产线读回、普通保存与 Git 里程碑；`D` = 经验账本。首次问题及代码修复另见 [incident-fix-log.md](./incident-fix-log.md)，现行规则与复验证据见 [experience-ledger.md](./experience-ledger.md)，版本门见 [ROADMAP.md](../ROADMAP.md)。

## 从落地到当前的主时间线

| 本局时间 / tick | 实际时间 | 事件与决策 | 证据 |
|---|---|---|---|
| `000d 00:00:00` / `0` | 未保留 | Spherewright 创建普通和平、1×、非沙盒新档并落地；从此只操作这个 owned world，不读取用户其他存档。 | R/M |
| `000d 00:02:00`–`02:21:59` / `7208`–`511154` | 未保留 | 完成电磁学、蓝糖、基础制造、自动冶金、基础物流、原油精炼、钢材、冶炼提纯、红糖和火力发电等早期科技；建立矿物、冶炼、蓝糖、石墨、原油/氢和红糖基础链。 | R/M |
| 不晚于 `000d 11:34:20` / save `2499658` | 里程碑提交于 2026-08-31 15:09:53 | 研究站 `256` 的红糖输出 `0 -> 3 -> 6`，完成 M0 第一颗自动能量矩阵；正常保存、冻结 M0，并转入 post-M0 物流目标。 | M |
| `000d 16:34:37`–`18:34:54` / `3580638`–`4013644` | 未保留 | 解锁动力引擎、机甲核心 1/2、驱动引擎 1/2；自动动力引擎仓达到 `9 -> 30`。决定把核心能量、无线充电、移动停滞与碰撞恢复纳入长期安全约束。 | R/M/D |
| `000d 20:24:47` / save `4409247` | 2026-08-31 | 正常保存并关闭游戏，指定此同档为下一次唯一续玩点；不强杀进程。 | M/D |
| `000d 20:30:01` / `4428079` | 2026-09-01 00:40:09 | 在既有同档挂接受保护的逐存档首次事件日记；历史覆盖标记为不完整。 | J |
| `000d 21:22:41` / checkpoint `4617708` | 2026-09-01 | 星际飞行前创建独立检查点；真实飞行 `104 -> 102` 成功，不用传送或位置写入。 | M/D |
| `000d 22:15:40` / checkpoint `4808424` | 2026-09-01 | 返航 `102 -> 104` 两次明确失败后，严格重载同一检查点；第三次稳定落地。飞行成功并覆盖保存后，旧 checkpoint 必须退役。 | M/D |
| `000d 22:18:39` / save `4819163` | 2026-09-01 | 带回 `1000` 钛矿并稳定保存母星主档；跨星球资源搬运成立，但还不是自动星际物流。 | M |
| `001d 00:22:01`–`07:56:06` | 2026-09-01 06:19–16:15 | 依次完成塑料、钛块、金刚石、齿轮、电动机、水、有机晶体、钛晶石和结构矩阵首次自动产出；每条产品线均有自动输出、普通保存和 Git 里程碑。 | J/M |
| `001d 07:58:05` / save `6905142` | 2026-09-01 16:21 前 | 黄糖线正式闭环。结论限定为“最后转换段自动化”；钛原料仍来自同档跨星球人工搬运。 | M/D |
| `001d 09:19:29` / save `7198197` 后 | 2026-09-01 18:01 | 修复并部署飞行 checkpoint 生命周期、journal durable 语义、planned/quarantine 恢复分流、一次性票据 tombstone、移动恢复窗口等问题；Windows CI 和 76 项测试通过。 | M/D |
| `001d 10:19:28`–`13:30:21` | 2026-09-01 18:58–22:09 | 依次完成电磁涡轮、高纯硅、微晶元件、硫酸、处理器、石墨烯和推进器首次自动产出；补建 5 台风机后网络恢复满供电。 | J/M |
| `001d 13:36:35` / save `8123715` | 2026-09-01 22:22 前 | 推进器里程碑正常保存并提交推送；这是当时的产品里程碑边界，后来由最终关机保存覆盖。 | M |
| `001d 13:45:28` / `8155733` | 2026-09-01 22:24:19 | 首次点击升级“垂直建造”；这是日记功能上线后的首个真实 upgrade 事件。 | J |
| `001d 14:09:53` / `8243611` | 未单独记录 | 垂直建造升级完成。 | R |
| `001d 14:10:08` / `8244528` | 2026-09-01 22:49:32 | 首次点击“粒子磁力阱”。关机前 tick `8337127` 时进度 `92929/288000`；粒子容器线已经空载预建，等待科技解锁后单次启用。 | J/R/M |
| `001d 14:36:46` / save `8340400` | 2026-09-01 23:17 前 | 普通保存持久化垂直建造完成、粒子磁力阱研究进度、三套后续产线预建/备料及混料整理，revision `676 -> 677`；受保护续玩票据签发成功，有效期到 `2026-09-02T23:16:11.8559867+08:00`。随后正常关闭 DSP，进程退出、descriptor 清零且未强杀。 | M/D |
| `001d 14:37:26` 起 / `8342796` 起 | 2026-09-02 10:25 起 | 新 DLL 同批部署后只消费受保护票据；prepare 绑定 minimum tick `8340400` 与 planet `104`，exact primary 经正常加载、归属复核和自动重新保存后完成。复读确认 `confirmed_peaceful`、`confirmed_disabled`、1×、healthy、日记 durable through `36`；三台预建制造台 `883/891/898` 均 recipe `0`、网络 1 满供电。`1703` 继续自然研究，没有直接解锁。 | R/M/D |
| `001d 14:59:29` 前 / `8436964` | 2026-09-02 10:53 前 | `1703` 自然推进到 `183940/288000`。尝试把 `1604` 追加到队列时，旧完整哈希因研究上传持续变化而在写入前连续 81 次 stale；没有 action/commit 或队列副作用。源码已新增稳定 selection hash 并通过 83 项测试，当前进程不热替换 DLL，继续优先等待产品线。 | R/D |
| `001d 15:32:01` / save `8474115` | 2026-09-02 11:03 前 | 既有处理器线经 20 自动电路板、20 自动铜块补料自然再产 10 个处理器；它们与自动钛仓的 40 钛块分别双端守恒进入物流站预备仓。玩家往返使用已验证 `82 -> 133 -> 143 -> 182 -> 165 外缘 -> 713` 路线，全程 Walk/停稳且未触发卡脚。仓 `899` 现有钢/处理器/钛块各 40，只欠科技解锁后的 20 粒子容器；普通保存写入健康。 | R/M/D |
| `001d 16:16:11` / `8698306`; save `8699182` | 2026-09-02 12:34:53 | 蓝糖供料修复后，粒子磁力阱在 tick `8696391` 正常完成；预建制造台 `883` 才启用 recipe `99`。涡轮/铜块/石墨烯各下降 10，专用仓 `885` 出现 2 个粒子容器；日记 sequence `38` 已持久化，随后同档普通保存为 revision `52`。 | J/R/M/D |
| `001d 19:16:19` / `9346766`; save `9369181` | 2026-09-02 20:02:42 | 行星物流在 tick `8836460` 正常解锁后，制造台 `891` 才单次启用 recipe `94`；过滤输入仓的铁块/处理器/推进器均被真实消耗，输出 sorter `894` 携带 item `5001`，专用仓 `893` 最终达到 10 个物流运输机。日记 sequence `41` 已持久化，普通保存后 revision `112`、写健康正常。 | J/R/M/D |
| `001d 19:34:06` / `9410766`; save `9413535` | 2026-09-02 20:20:29 | 行星物流站组的四个 sorter 已分别过滤钢材/钛块/处理器/粒子容器，备料仓 `899` 的 `40/40/40/20` 原料全部经普通设备输入耗尽；制造台 `898(recipe 93)` 满供电完成一轮，输出仓 `900` 得到 1 座 item `2103`。日记 sequence `42` 已持久化，普通保存后 revision `115`、写健康正常。 | J/R/M/D |
| `001d 21:17:39` / save `9783554` | 2026-09-02 22:15 前 | 为第二批站体，本地处理器线自动补足 40 个处理器；钢材/处理器/粒子容器先守恒装入，随后从母星钛仓 `531` 取 40 钛块（`160 -> 120`）并送入仓 `899`。制造台 `898` 的四种输入完整消耗，进度实际经过 `4245000/12000000` 并完成，仓 `900` 再得到 1 座 item `2103`。这是同一产线的第二批，故不新增“首次产线物品”日记；普通保存后 revision `55`、写健康正常。 | R/M/D |
| `001d 21:39:36` / save `9862572` | 2026-09-02 22:35 前 | 第二座站体原生施工为 `918`，电塔 `919` 使其接入 network 4；槽 0 配钛块/100/本地供应，限充 6 MW。18 段带与 sorter `938` 从仓 `531` 装入 120 钛，首塔 `916` 的 10 架无人机实际出现 working/order，并以四批各 25 件把目标库存送到 100；源塔余 20、源仓清零、无人机归队、双方订单归零，完整守恒后普通保存。 | R/M/D |
| `001d 22:13:49` / `9985749`; save `9997352` | 2026-09-02 23:13:42 | 第二次资源航行先在独立检查点 tick `9895450` 后明确起飞失败，严格重载同一检查点后再次起飞并在 planet `102` 稳定落地。600 硅石、100 铁矿和 8 石矿均由普通手采守恒取得；本地原料与随身铜递归手搓出缺少的矿机、风机、仓、带、sorter 和电塔。首条远端钛矿线由两台风机以 `660 kW` 容量满供电，仓内自动钛石 `6 -> 26`；journal sequence `43` 记录首次产线钛石，随后正常保存到 tick `9997352`。 | J/R/M/D |
| `001d 22:53:01` / save `10126918` | 2026-09-02 23:56 前 | 用户发现首个硅矿机姿态只覆盖节点 `252/256`，拒绝把“合法但低效”当作完成。源码从“首个合法候选”改为比较全部原生合法姿态并最大化覆盖，完整构建 0 warning/0 error、105 tests passed；保存后正常重启并精确恢复同档。矿机 `17` 由正常拆除回收 1 台矿机及 50 硅石，再以 yaw `150°` 重建，计划/实际均覆盖 `245/249/252/256`。两台风机、电塔 `27/42`、新带 `30…39` 与旧带 `18…24`、sorter `26`、仓 `25` 形成满供电闭环，仓 `16 -> 27 -> 50` 后正常保存。日记仍为 `43/43`，因为该旧档的硅石生产寄存器早已有历史；没有伪造第二个“首次产线”事件。 | R/M/D |
| `001d 23:08:26` / save `10182419` | 2026-09-03 | 返航携带 1100 钛石和 651 硅石；落点实际是 planet `104` 海面而不是未抵达。修复后的控制器选择 24.6 m 外干燥邻域，以一次原生 Drift MoveTo 上岸并稳定 Walk 600 tick，随后普通保存并退役独立 flight checkpoint。 | R/M/D |
| `001d 23:44:30` / save `10312259` 后 | 2026-09-03 | 35 段黄糖专线把仓 `778` 接入同时具有蓝/红输入的研究站 `84`，高强度钛合金 `1414` 从 0 开始正常上传；返航钛石守恒接入仓 `259` 并恢复钛块冶炼。后续修复电路板/蓝矩阵输入，研究继续推进。 | R/M/D |
| `002d 00:09:49` / `10403369` | 2026-09-03 | 研究达到 `54494/144000` 后因红矩阵耗尽暂止。PLS `916` 的 36 段 Output belt `1015…998` 和 `998 -> sorter 1016 -> storage 768` 完工，但端口 raw `storageIndex=0` 仍表示 None，故 100 钛块未流出；该现场直接触发安全输出选择器实现，尚未冒充产线恢复。 | R/D |
| `002d 00:24:00` / save `10456408` | 2026-09-03 | 正常保存/关闭/同批部署后只恢复 tick `10449537` 的 exact primary。动作 `26a83d4b-ebf2-49b8-9d06-5e6c1321b78f` 把 PLS port 0 raw 选择器 `0 -> 1`；钛块真实流过 36 段带和 sorter，制造台 `767` 恢复，钛晶石仓 `769` 达到 9，需求塔无人机自动补回出料槽。114-test 实现由普通保存覆盖。 | R/M/D |
| `002d 01:14:58` / save `10637902` | 2026-09-03 | 86 格钛晶石专线与 94 格金刚石支线分别从仓 `769/717` 自动汇入黄糖输入仓 `775`；有向链、双端 sorter、仓过滤、设备输入和最终科研消费者全部复读。金刚石源仓 `300 -> 145`，目标仓保有 88 钛晶石/11 金刚石，lab `774` 以 `6/6` 输入工作，研究站 `84` 保有 8124 黄矩阵点，科技 `1414` 从 `90000 -> 93121/144000`。普通保存覆盖该里程碑，但上游源仓仍是有限缓存。 | R/M/D |
| `002d 03:14:13` / `11001789+` | 2026-09-03 | 有限磁铁用尽后，科技 `1414` 停在 `130937/144000`。新建六节点铁矿机、磁铁熔炉和专用仓，满电自动积累 1594 磁铁；从仓 `1217` 经 sorter `1241` 延伸的 238 格主干经全链复读无分支/无意外外接，自由末端 `1471` 距旧蓝链仓 `30` 约 20.4 m。中途浅海 Drift 已以有界 MoveTo 正常上岸；后续施工始终由独立陆地点完成，并用只读候选扫描避开设备中心和旧带回折。当前尚未达到产线保存/提交门。 | R/D |
| tick `11075068+` | 2026-09-03 | 主干继续到 `1479`，分页复读为连续 246 格；独立 5 格中继和三只 sorter 已把路径设计为 `1479 -> 1489 -> 1482…1486 -> 1488(filter magnet) -> old belt 63…48 -> 1487(filter magnet) -> assembler 73`。两只下游 sorter 满电，但 `1489` 为 `network=null`，所以线圈/蓝糖/科技尚未恢复；第 10 写只手采 2 铁用于补塔，随后健康审计并落盘 EXP-126，没有误报里程碑。 | R/D |
| `002d 03:23:13` / save `11099621` | 2026-09-03 | 普通手搓并原生建成电塔 `1490` 后，桥接 sorter `1489` 从无电变为 network 1、满供电并实际携带磁铁；`1488/1487`、磁线圈制造台 `73`、蓝矩阵站 `76` 和研究站 `84` 依次恢复。研究站蓝矩阵点从 0 增至 12340，高强度钛合金 `1414` 从 `130937` 连续推进并于 tick `11098574` 完成；普通保存覆盖持续磁铁→蓝糖供料与科技完成边界。 | R/M/D |
| tick `11148734+` | 2026-09-03 | 星际物流系统 `1605` 在 tick `11122095` 正常选择并写入 durable journal sequence `44`。研究开始后单线蓝矩阵缓冲清空，上传暂为 `1337/216000`，但供磁 sorter 仍满电携货，故分类为吞吐不足而非断线。等待期间普通手采/手搓备好基础设施，并在仓 `899/900` 之间落下满电熔炉 `1491`、输出 sorter `1492` 与首条输入 sorter `1493`；recipe 仍为 0，未提前宣称钛合金产线。第 10 写后健康审计完成。 | J/R/D |
| tick `11166447+` | 2026-09-03 | 熔炉 `1491` 的三条输入先后在空载窗口锁定钛块/钢材/硫酸，随后才启用 recipe `66`；20 硫酸由仓 `863` 经玩家守恒装入仓 `899`。去无线塔的 Move 前进约 27 m 后在密集处理器区被 181-tick 看门狗明确终止，未重放；fresh 停点为 Walk/0 且已进入钛/钢仓范围，下一动作直接守恒取得 100 钛块。第二组第 10 写健康审计完成，产线仍缺钢、尚未报首产。 | R/D |
| `002d 03:43:30` / `11172619`; save `11175248` | 2026-09-03 | 各 100 钛块/钢材经守恒转运进入过滤多料仓；钛合金熔炉满电工作，独立输出仓 `900` 从 0 增至 8。journal sequence `45` 持久化首次自动钛合金，随后普通保存 revision `261`、write health healthy。共享仓的旧 PLS 支路也按其合法 filter 预装钛/钢，因此后续配料必须区分“防串料”和“分配优先级”；硫酸有限批已耗尽，尚不冒充 ILS 总物料完成。 | J/R/M/D |
| save `11388372` | 2026-09-03 | 新 8 点铁矿机、熔炉、专仓和长距离铁带通过独立带段与两只满电 sorter 绕开了既有磁铁线的重复占位，永久接回旧电路板/蓝矩阵链。两端实际携铁、蓝矩阵输出 sorter 实际携货，科技 `1605` 跨窗由 `12317 -> 18101/216000` 后正常保存；新增 EXP-133，并记录 action result 有界保留的 EXP-134。 | R/M/D |
| save `11439545` | 2026-09-03 | 原硫酸线按两座 ILS 预算投入 240 精炼油、320 石矿、160 水并自然产出 160 硫酸；68 钛块/68 钢材与分批 140 硫酸经守恒进入已过滤钛合金线，加上设备原有 4 酸恰好完成 18 轮生产。专仓由 8 增至 80 钛合金，另余 20 硫酸；journal 不新增事件，因为首次钛合金早已由 sequence `45` 记录。正常保存后 revision `362`、科技至少 `39677/216000`、写健康正常。 | R/M/D |
| save `11525780` | 2026-09-03 | 永久铁源和蓝矩阵继续工作，科技 `1605` 推进至 `90992/216000`。在提交前复核运行时配方，将“双站+两船”合金预算由漏计加力推进器的 100 纠正为 120。原钛合金线用额外 40 酸/20 钛/20 钢完成五轮，专仓 `100 -> 120`；PLS 制造台的 80 钢/80 钛不变，共享仓只剩 651 硅石。普通保存后 revision `394`、journal `45/45` durable、三张电网满供电、无 checkpoint；8 个本窗口写动作主动提前审计全部 terminal/completed/succeeded。 | R/M/D |
| save `11721771` | 2026-09-03 | 运行时 `1605` 的黄矩阵需求为 120 件、`pointsPerHash=2`；据此纠正“剩余 90000 hash 只需 25 黄糖”的误算，真实还需 50。两批各 50 自动塑料、25 自动水、25 既有精炼油依次产出 25 有机晶体，再经钛晶石长带、黄糖实验室和统一研究站自动供给；保存审计时科技已由 `126000 -> 170155/216000` 且仍在上传，完整预算已进入链路。同期粒子容器仓增至 41，输入仓补齐铜/石墨烯、仅缺涡轮；钛合金仍为 120。普通保存后 revision `508`、journal `45/45` durable、三张电网满供电、无 checkpoint。新增 EXP-136。 | R/M/D |
| save `11775970` | 2026-09-03 | 从自动涡轮仓分两轮投入 51 与 28 个电磁涡轮；其间从自动铁仓守恒补入 174 铁块、向四过滤电机共享仓补铜，齿轮→磁线圈→电机→涡轮链恢复。粒子容器制造台 `883(recipe 99)` 满电自然消耗涡轮/铜/石墨烯，专仓 `885` 从 41 增至精确 80；涡轮仓仍有 23，覆盖四个加力推进器所需 20。十写审计为和平/非沙盒/1×、Walk/0、三网满供电、journal `45/45` durable、无 checkpoint；科技同步推进到至少 `198077/216000`。随后正常保存 revision `521`。 | R/M/D |
| save `11811914` | 2026-09-03 | 星际物流系统在 tick `11808407` 精确完成并解锁配方 `95/96`。为复用既有自动电路板线，先把满载回收仓 `26` 的 900 铜守恒暂存到仓 `136`；第一次 300 件迁移没有释放格子，追加 600 后非空格才由 30 降到 26。钢材支线与蓝糖两处消费者只在 sorter 零携货窗口临时隔离，电路板因此自动回收到仓并增至 47。审计 revision `532`、9/9 动作成功、三网满供电、journal `45/45` durable、无 checkpoint；新增 EXP-137，复验 EXP-102/103/136。 | R/M/D |
| save `11830296` | 2026-09-03 | 复用空载基础推进器单元：制造台 `876` 改为 recipe `21`，输入 sorter `880/881` 改为钛合金/电磁涡轮。20+20 原料经四次守恒 transfer 进入仓 `877` 并被自动配方完整耗尽，仓 `878` 新增 4 个加力推进器；journal sequence `46` 在 tick `11827563` durable 记录首次产线产出。审计 revision `543`、8/8 动作成功、三网满供电、无 checkpoint；电路板同期增至 186。 | J/R/M/D |
| save `11863820` | 2026-09-03 | 从仍自动补货的混合仓 `26` 精确取得 200 电路板并送入仓 `849`；处理器制造台 `853(recipe 51)` 满电消耗 200 电路板和 200 微晶元件，输出仓 `854` 达到精确 100 个处理器，另余 20 微晶元件。40 粒子容器已被 PLS 制造台 `898` 预取；运输船制造台 `891` 启用 recipe `96`，20 钛合金已预取。journal 没有新增，因为处理器首次产线事件早已由 sequence `29` 记录。十写审计 revision `556`、10/10 动作成功、三网满供电、journal `46/46` durable、无 checkpoint。 | R/M/D |
| save `11896243` | 2026-09-03 | 80 个处理器经玩家守恒进入 PLS 线；制造台 `898(recipe 93)` 在渐进预取完整两批后耗尽 80 钢/80 钛/80 处理器/40 粒子容器，仓 `900` 在保留 80 钛合金外新增 2 座 PLS。余下 20 处理器与 4 加力推进器又经守恒进入已缓存 20 钛合金的制造台 `891(recipe 96)`，仓 `893` 得到 2 艘星际物流运输船；sequence `47` 在 tick `11890591` durable 记录首产。主动七写审计 revision `563`、7/7 动作成功、三网满供电、研究队列空、无 checkpoint。 | J/R/M/D |
| save `11926992` | 2026-09-03 | 空载制造台 `898` 切为 recipe `95`，sorter `902/903` 改为 PLS/钛合金、粒子容器 sorter 保持；2 座 PLS、80 合金、40 粒子容器经六次守恒 transfer 全部进入并自动耗尽，输出仓 `900` 得到精确 2 座 ILS。sequence `48` 在 tick `11921722` durable 记录首次产线 ILS。第十写保存后的审计 revision `576`、10/10 动作成功且无 reconciliation，玩家满电 Walk/0、三网满供电、研究队列空、无 checkpoint；双船仍在仓 `893`。 | J/R/M/D |
| save `11974991` | 2026-09-03 | 一座 ILS 从仓 `900` 守恒取出并在高净空站址施工为 `1657`；两座新电塔把它接入 network `1`，60 MW 默认充电降到 ILS 原生最低 30 MW。钛石/硅石两槽各配 `100` 远程需求，仓 `893` 的一艘船经玩家守恒装入 fleet。严格十写审计 revision `593`，10/10 terminal/completed/succeeded；充能期 network `1` 仅约 20.37% 供电。随后正常保存，fresh revision `594`、healthy、journal `48/48` durable、无 checkpoint，站能量继续增至约 `1.463 GJ`。 | R/M/D |
| tick `12052859`–`12222093` | 2026-09-03 | 母星飞前主档正常保存后，两次被中间气态巨行星捕获的失败飞行都只重载同一绑定 checkpoint。确定性中间天体绕行修复部署后，`104 -> 102` 成功稳定落地，立即正常保存到 tick `12071759` 并撤销 checkpoint。远端无线塔 `43` 合并两张旧电网并实测伊卡洛斯自动回充；ILS `44` 接入 network `1`、降至 30 MW，配钛/硅各 100 远程供应并守恒装入 1 船。76 格钛带与 sorter `123` 已连接到 port 0；发现 4 风机的 4.46% 供电不足后，手采/递归手搓 5 风机和 36 带，已先并网风机 `124/125`。第四组十写审计 revision `100`，125 built/0 prebuild、单网 6 风机/`33000 J/tick`、journal `48/48` durable、healthy/无 checkpoint；0.07152 供电比下钛仓/站仍为 `3000/0`，因此尚未报投产。 | R/M/D |
| save `12278450` | 2026-09-03 | 风机 `126…129` 逐台并入 network `1`，总容量达 `55000 J/tick`，钛 sorter `123` 终于开始往返且 ILS 钛槽从 0 增长。硅仓直连 ILS 的 54 带方案因 planned point 与钛带 `102` 只差 0.00393 m 而在 commit 前丢弃；改为零重叠的 44 带 `173 -> … -> 148`，用受电 sorter `174/175` 形成 `storage 25 -> 硅带 -> belt 102 -> ILS`。实际观察中硅槽 `2 -> 9 -> 14 -> 19`、钛槽 `40 -> 49`，三只 sorter 均实际工作；里程碑普通保存后 revision `115`、175 built/0 prebuild、核心满电、healthy、journal `48/48` durable、无 checkpoint。 | R/M/D |
| save `12384194` | 2026-09-03 | 远端钛槽达到 100 后出现订单并降至 23，硅在钛满槽期间继续增到 100、随后也出现订单并降至 16；两次源端取货均完成。返航前正常保存并创建独立 flight checkpoint，动作 `ee51b297-e749-4da4-adf3-0db58cd86f25` 稳定落到 planet `104`；母星 ILS 钛/硅均为 100、双订单归零、运输船归队。成功后 checkpoint capability 消失，母星验收点再次普通保存。 | R/M/D |
| save `12841158` | 2026-09-03 | 母星硅路从 ILS `1657` port `1` 经长带、sorter bridge `1981/2022` 和末端 sorter `2030` 接入高纯硅熔炉 `842`。第二座桥最初为 network `0`，电塔 `2031` 使其接入 network `1` 后立即携带硅石；旧石转硅 sorter `844` 在零携货窗口改为硅石过滤并停止。ILS 硅槽 `100 -> 0`，末端 sorter 实际携货，熔炉连续运行，成品仓 `2806 -> 2820`。保存后 revision `205`、healthy、无 checkpoint；当前窗口 9 个已接受写动作。 | R/M/D |
| save `12841190` | 2026-09-03 | 硅线保存后 DSP 正常窗口关闭，进程退出、descriptor 清零且未强杀；重开后只消费 protected planned-restart ticket，exact primary 通过 minimum tick/planet/模式/owned 校验并自动重存。严格审计为 2031 built/0 prebuild、三网满供电、玩家满电 Walk/0、3/3 施工机 idle、Journal `48/48` durable、无 blocker/checkpoint。恢复后 ILS 保留双需求和正确端口选择，唯一运输船继续硅订单，桥接 sorter 携硅且熔炉工作；黄糖站停机收敛为有机晶体上游缺塑料/水。 | R/M/D |
| tick `12910631` / revision `18` | 2026-09-03 | 恢复后首组十写先从自动水仓守恒取得 50 水并投入有机晶体仓；旧 `141 -> 183` 直线已被新布局占据，只读重算局部侧绕后全程 Walk/0 到达 `183`，再以 `713` 为唯一跨水终点稳定落地。自动塑料仓随后守恒输出 100 塑料到玩家；两个已接受动作仅被本地结果展示错误遮住，均由 fresh 状态唯一核销且未重放。十写审计仍为 2031 built/0 prebuild、三网满供电、Journal `48/48` durable、healthy、无 blocker/checkpoint；有机晶体设备已预取水/油各 2，钛输入仓自然增至 980，下一写只投塑料。 | R/D |
| tick `12959635` / revision `31` | 2026-09-03 | 100 塑料投入后，化工厂完整产出 50 有机晶体；它们分三批守恒接入钛晶石仓，随后金刚石总量 `150 -> 100`、黄糖持续经旧带补入研究站。新 sorter `2032` 已把有机晶体输出仓 `762` 接上原有钛块输入带 `986 -> … -> 998 -> 1016 -> 768`，双端、供电和零预建筑均通过，尚待下一批做携货复验。第十写正常选择运输船引擎，Journal sequence `49` durable；研究到 `1928/36000`，站 `84` 的黄矩阵点在消费中仍由 `36000 -> 38080`。审计为 2032 built、三网满供电、healthy、无 blocker/checkpoint。 | J/R/D |
| save `12996056`; audit `12998411` / revision `38` | 2026-09-03 | 用自动塑料/水与既有精炼油组成 10 件有机晶体复验批。两次连续采样抓到 sorter `2032` 携带 item `1117`；钛晶石制造台取得有机晶体并运行，批次最终耗尽，黄糖 lab 同步运行且金刚石仓 `94 -> 84`。全程玩家未搬运有机晶体，普通保存后审计为 2032 built/0 prebuild、三网满供电、Journal `49/49` durable、healthy、无 blocker/checkpoint。该证据闭合了有机晶体输出桥，但三种本地上游料仍经玩家中转，因此尚不满足 v0.3“不再依赖伊卡洛斯人工携货”门槛。 | R/M/D |
| tick `13126837` / revision `57` | 2026-09-03 | 从纯塑料仓 `558` 建成 sorter `2038` 和 `2037 -> … -> 2085` 的 52 格无环单链。多组原生可建直达方案与旧带重合 4–21 格，全部在 commit 前丢弃；实际施工分为 5/14/23/10 格四段并逐段复读唯一自由末端。第十写审计为 2071 built/0 prebuild、玩家满电 Walk/0、3/3 施工机 idle、Journal `49/49` durable、healthy、无 blocker/checkpoint。三料未接到仓 `761`，不抢先报完成。 | R/D |
| tick `13246759` / revision `77` | 2026-09-03 | 主线先经 11/10 格试探到 `2098`，5.64 m 外的首条独立短带因 sorter `TooFar` 保留为不接入试验段；在 3.77 m 处重建 4 格独立带后，sorter `2115` 成功跨线并实际携带塑料。新侧又施工 10/9/9 格安全段到 `2135`，最后一段在 planned index `10/11` 的旧带 `2062/1695` 前停下。中间用现有铁/铜递归手搓 72 条带与 3 sorter；手搓队列清空。审计为 2135 built/0 prebuild、三网满供电、Journal `49/49` durable、healthy、无 blocker/checkpoint。 | R/D |
| tick `13310101` / revision `95` | 2026-09-03 | 从 `2135` 先到双旧带墙前的 `2144`，再用独立 3 格带和 sorter `2149` 跨到 `2148…2146`；手搓并建成电塔 `2150` 后，`2149` 接入 network `2` 并实际携带塑料。随后以 27/16 格两段到 `2193`，在旧带 `433` 远侧建 9 格独立带 `2194…2202`，sorter `2203` 完成双端拓扑。由于 fresh 复读确认 `2203` 仍为 network `0`、Picking/0，主动在第 9 写提前审计，没有把连接成功冒充通料。审计为 2203 built/0 prebuild（1835 belt、188 inserter、38 power-node），玩家满电 Walk/0、余 75 belt/3 sorter、3/3 施工机 idle，三网 ratio 均 `1.0`，Journal `49/49` durable、healthy、无 blocker/checkpoint。 | R/D |
| tick `13364570` / revision `113` | 2026-09-03 | 两段正常移动已把玩家带到旧电塔区；第三段在距目标 0.87 m 时由 181-tick 停滞看门狗明确终止，随后仍可在范围内从自动仓守恒取得 100 铁。递归手搓 2 电塔/3 sorter 后，电塔 `2204` 使 sorter `2203` 满电携塑料。主线从 `2202` 侧移到 `2206`，再以独立 5 格带 `2212…2208` 与 sorter `2213` 跨过旧带；`2213` 同样满电携塑料。第十写审计为 2213 built/0 prebuild（1843 belt、189 inserter、39 power-node），玩家满电 Walk/0、余 67 belt/5 sorter/1 power-node，三网 ratio 均 `1.0`，Journal `49/49` durable、healthy、无 blocker/checkpoint。 | R/D |
| tick `13405919` / revision `131` | 2026-09-03 | 主路贴近油中继仓 `784` 后，复读推翻其“仍为纯油仓”的旧假设：`163/784` 分别残留 12/55 氢；两次 transfer 将 67 氢守恒隔离到玩家，随后两仓均稳定为 600 精炼油。sorter `2218` 把油汇入主干，10 格续线到水仓旁后 sorter `2229` 实际携水；再经 8 格外绕、6 格独立带和满电桥 `2244` 跨过旧线，最后在黄糖旧带远侧建 `2245/2246`。第十写审计为 2246 built/0 prebuild（1873 belt、192 inserter），玩家满电 Walk/0、余 37 belt/2 sorter/1 power-node，三网 ratio 均 `1.0`，Journal `49/49` durable、healthy、无 blocker/checkpoint；目标仓此刻仍空，未提前报闭环。 | R/D |
| tick `13432430` / revision `150` | 2026-09-03 | sorter `2247/2248` 完成最后跨带和入仓连接，但 50 秒内仓 `761` 只有塑料 `35 -> 75`、油/水仍为 0；源 sorter 满电携货，故定位为单 sorter 下游桥被上游塑料满流量饿死。三个瓶颈各补两只并行 sorter `2249…2254` 后，仓内三料达到 `270/40/25`，化工厂 `760` 与钛晶石制造台 `767` 恢复连续工作。隔离的 67 氢同时守恒归入专用氢仓 `136`。第十写审计为 2254 built/0 prebuild（1873 belt、200 inserter）、写健康正常。 | R/D |
| `002d 14:14:40` / save `13444822`; audit `13446315` / revision `151` | 2026-09-03 | 全自动三料继续增长到 `304/86/72`；有机晶体输出 sorter `2032`、结构矩阵输出 sorter `779` 与下游 sorter `977` 都被连续采样抓到携带正确物品。钛晶石制造台和黄糖 lab 持续工作，金刚石输入由 `84 -> 74`，保存后进一步到 `55`；玩家背包没有水、油、氢、有机晶体、钛晶石或结构矩阵，因此不是人工搬运。普通保存后 owned save 精确为 tick `13444822`，审计确认 peaceful/non-sandbox/1×、healthy、Journal `49/49` durable、0 prebuild、无 blocker/checkpoint、planned restart 可用。v0.3 游戏内容门正式闭合。 | R/M/D |
| save `13494061`; protected resume/save `13494092`; audit `13504262` | 2026-09-03 | 首个 `0.3.0` clean 包虽然文件/MCP 校验通过，live Plugin 却暴露仍为 `0.1.0`，因此停止发布而不创建 tag。统一产品版本源后再次普通保存、正常关闭、把旧 Plugin/MCP 整体移到可恢复备份并 clean install；新 Bridge 精确报告 `0.3.0`，错误 token 被拒绝，安装版 MCP `0.3.0.0` 通过 stdio 成功调用 live status。受保护票据只恢复同一 planet `104` 普通世界并自动重存；配置/物流保留，0 prebuild、Journal `49/49` durable。黄糖最后一件仍被 sorter `977` 携带，但 lab 已因本批金刚石耗尽正常停机，未冒充无限原料。 | R/M/D |
| save `13516383`; protected resume/save `13516415`; audit `13520330` | 2026-09-03 | 从最终 clean commit `a52ff44` 重打工件后，发现其 payload 与已测 preview 并非完全相同，故没有继承候选包结论。正常关闭后重新 clean install 最终 ZIP，228 个运行文件逐一比对 mismatch `0`；受保护票据恢复同一 planet `104` 世界并自动重存，119 项测试、wrong-token 拒绝、live Plugin `0.3.0` 和安装版 MCP `0.3.0.0` 调用全部通过。annotated tag 与 GitHub Release `v0.3.0` 精确指向该 commit，线上 ZIP digest 为 `705081710b7061c6a00c4c8836a7d2869b13bd8b8fb6f42bfb24b7f0d62783c1`。v0.3 正式关闭，后续同档用于 v0.4 Overseer 诊断验收。 | R/M/D |
| no game write after audit `13520330` | 2026-09-03 | v0.4 首个只读基础切片定义连续游戏 tick 窗口、实际/理论速率和五类首因；跨 session 可按同一受保护存档身份衔接，但回档、计数回退、同 tick 异常增量或采样断层会使窗口失效。故障分类在至少一个完整配方周期前保持沉默。135 项 Core/Contracts/MCP 测试与完整 solution 构建通过；没有部署新 Plugin、重启 DSP 或改变本档。 | D |
| saves `13617247` / `13621729` / `13626113`; final protected resume/save `13626145`; audit `13630162+` | 2026-09-03 | 三次都先走普通 save、正常关窗，再只消费当次 exact-primary protected ticket 恢复同一 planet `104` 世界；没有开新档或载入其他档。`0.4.0` 开发 Plugin 新增有界多星球原生生产窗口，实机分页完整返回 planet `104/102/103` 三个既有 factory，远端两厂无需加载显示。最后一次保存前红糖窗口计数为 2；恢复后仅 16 tick 首读仍为 2，直接证明 600-tick 环随档恢复且离线不计时。重复 item、17-planet 页大小和错绑 filter 的 cursor 分别以 `INVALID_REQUEST/STALE_CURSOR` 无副作用拒绝。142 项测试、完整 solution、49-tool MCP initialize/list/live call 均通过；最终审计 healthy、Journal `49/49` durable、无 blocker/checkpoint，玩家 Walk/0 且核心满电。 | R/M/D |
| saves `13665285` / `13696182` / `13725278` / `13767062`; final protected resume/save `13767093`; audit `13775095+` | 2026-09-03 | 继续仅用同一受保护世界完成 Overseer 第二个纵向切片：`limit=1` 的三页共享同一 snapshot/tick，返回 planet `104/102/103` 的有界电网和物流聚合，以及唯一全局科研 `3401`。首次读数揭示旧电力 DTO 把防御场 `energyExport` 误当发电量；源码改为逐发电组件汇总 `generateCurrentTick` 并单列 exported，同时修正 vein collector 互斥分类、空槽一致性、科技队列身份与全域扫描预算。最终源码重新完整构建后，先普通保存 tick `13767062`、正常关窗，再将 7 个 Debug 文件以零哈希差异部署；受保护票据只恢复 exact primary 并自动重存 tick `13767093`。最终三页共享 tick `13773036`，母星 generated/exported 为 `94688/0`，远端 `102` 为 `4050/0`，队列 `[3401]` 及蓝/红/黄矩阵需求正常。旧生产窗仍为 ready；错页大小 cursor/17-planet 分别以 `STALE_CURSOR/INVALID_REQUEST` 拒绝。150 项测试、完整 solution 0 warning/error、50-tool MCP live call 全部通过。最终审计 healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、无 blocker/checkpoint；没有打 tag 或发布。 | R/M/D |
| saves `13827983` / `13831872`; final protected resume/save `13831903`; audit `13861096+` | 2026-09-03 | Overseer 第三个切片按当前程序集逐组件重算理论产能，不读写 UI 缓存。首轮 live 被三台合法耗尽矿机安全挡下；修正 `veinCount=0` 为零容量后，7 个部署文件与源码零哈希差异，最终 Plugin SHA-256 `5AC257D5AB8013E7D088A8609D08A9FA7FD83A633D4D2DA2F0F549BA53815DC1`，exact-primary 仍只恢复同一 planet `104` 世界。母星矩阵理论值为 `20/10/7.5 min⁻¹`，铁/铜/石/煤矿覆盖点数闭合 `420/60/90/210 min⁻¹`，水/油为 `50/133.15919494628906 min⁻¹`，远端未显示 factory 的硅/钛为 `120/60 min⁻¹`；三页共享 tick `13837732`，游标/页长边界继续拒绝。160 项测试、完整构建和 50-tool MCP `initialize/list/live call` 通过。最终审计为 confirmed peaceful/sandbox disabled/1×、healthy、Journal `49/49` durable、Walk/0、满核心、3/3 施工机 idle、无 blocker/checkpoint、restart 可用；没有开新档、隔离、打 tag 或发布。 | R/M/D |
| saves `13931724` / `13938529` / `13943810`; final protected resume/save `13943842`; final read-only audit `13990990+` | 2026-09-03 | Overseer 第四个切片把直接故障分类接到真实 assembler/lab/miner 缓冲、电网、矿脉和物流塔有向拓扑。首轮反向路径在 storage `259` 停止；补入 storage/tank/inserter 中继后，assembler `530` 的硅缺料 finding 精确串起母星 demand station `1657` 与 planet `102` supply station `44`，返回 source inventory `28`、carrier `2`，但因单快照没有停滞证据仍保持 `material_shortage`。最终自然现场还闭合铁矿机 `1213/1496` 和熔炉 `10` 的 `output_blocked`、lab `774` 的缺 item `1112`、三台身份已清空的 `vein_exhausted`；较早快照在制造台 `715` 捕获约 `0.94038` 的 `insufficient_power`，供电恢复后按 fresh tick 变为缺 item `1109`。三次均普通保存、正常关窗、7 文件零差异部署并只恢复 exact primary；最终 Plugin hash `D40D6BEA4E76697EB14C5F1DE3B0CC61532E4BF634125E1A9488D5024FDF59E1`。最终三页生产快照共享 tick `13986388` 并安全拒绝错绑 cursor；源码 MCP `0.4.0.0` 完成 50-tool initialize/list/live call。174 项测试与完整构建通过；没有执行生产写、开新档、隔离、tag 或 Release。 | R/M/D |
| saves `14027876` / `14042622` / `14059914` / `14109460`; final protected resume/save `14109491`; final read-only audit `14119093+` | 2026-09-03 | Overseer 第五个切片把缺料 finding 递归到过滤允许、物理可达的同星球生产者。首个候选在 tick `14028962` 将 yellow matrix lab `774` 的缺金刚石追到 assembler `715` 的缺高能石墨；复核后改为每个输入 item 独立遍历，先要求全部 sorter filter 匹配，又按当前程序集为 splitter 保留精确输出 slot/belt：优先口只允许过滤物，其余口反向排除过滤物，身份不一致则 fail-closed。Core 以 8 层/64 producer/环路 guard 限界，深度、访问量、环路或 resolver 身份导致的截断都会写 `upstream_trace_stop_reason`。最终 Plugin/Core hash 为 `46E62CC930CAD0756BBFB06625C9585F04A074B25FEDC15C4D7DCE2A322F4B70` / `CA8B33DD66330211ECD78E535CBD05932933278AF6E640BE67AF7AFD6301E7C5`，部署文件与源码构建一致；Bridge tick `14111293` 和最终构建后 MCP tick `14138110` 的四节点 live 路径均无截断。188 项测试、完整构建、50-tool MCP live call、三厂生产/摘要分页和错 filter cursor 拒绝全部通过。四次都只普通保存、正常关窗和 exact-primary 同档恢复；没有生产写、开新档、隔离、tag 或 Release。 | R/M/D |
| save `14290235`; protected resume/save `14290266`; final read-only audit `14308785+` | 2026-09-03 | Overseer 第六个切片为物理命中的行星/星际物流路线增加按档保护的时间窗。最终审查又把同次读取的全部路线改为单次原子持久化，只有落盘成功的 analysis 才进入公共 DTO；消费者必须真缺料且需求端 reservation 为正才计时，供料恢复会重置旧基线。无人机/运输船状态、送达库存、订单缩减或 active-carrier 数变化同样重置停滞基线，连续 600 游戏 tick 静止才给 suspected `logistics_blocked`，回档、路线变化、同 tick 突变或超过 3600 tick 的采样缺口会失效。最终四 DLL 与源码逐文件相等，Plugin/Core hash 为 `A66033BFC60DBCAC8B2E798F815E7A22E635AAFCBFDD7F604E5256F191E3CDC5` / `EE9F5519C23A1EC9BC21987D78D29D90A79E561EBEE80F72702A74678B8E492E`；exact-primary 只恢复同一 planet `104` 主档。保护文档为 3 条哈希路线、2942 bytes、current-user-only DACL、无原始存档身份，并实机出现 `consumerInputMissing=false/true` 两种样本；现场三路均无订单/active carrier，因此保持“持久化已验证、活动/停滞样本待闭环”。生产/摘要三页分别共享 tick `14304692/14304700` 并拒绝错 filter cursor；源码 MCP `0.4.0.0` 以协议 `2025-06-18` 列出 50 tools、公开正 reservation/供足重置语义并 live 返回三厂。204 项测试、完整构建、0 prebuild 和最终健康审计通过；没有生产写、开新档、隔离、tag 或 Release。 | R/M/D |
| save `14413801`; protected resume/save `14413832`; final read-only audit `14418919+` | 2026-09-03 | Overseer 第七个切片把 item-aware 反向货运图扩展到每个精确 supply endpoint 的 Input belt，并在全部 owned factory 都深复制后按 planet/object/item 绑定生产者。当前程序集的 `StationComponent.UpdateNeeds/UpdateInputSlots` 证明 input `storageIdx` 是成功取货后写回的动态槽位，不能当作固定 selector；实现因此逐条 Input belt 追踪 sorter/splitter 允许的目标物料。最终审查把多 supply 的聚合库存/机队证据与一条公开主路径分开，只允许主路径 supply 的候选继续递归；存在 demand route 时也不再跳入消费者侧无关的本地候选。普通保存、正常关窗、源码相等部署和 exact-primary 恢复均留在同一 planet `104` 世界。最终 Plugin/Contracts/Core hash 为 `344614FE3B827BE8397D5D6DC77C3CCB90C8991C01D088E3108B6F473AB11869` / `8E2FB3205B54972180540D6A6C9B08F62B028453DA5E44BBC36CA96E04F56991` / `9507C3AAEE729ACF13693573C2AB53466B8043F53B762B054F2DEEBD59AAF412`。live tick `14414535` 将母星钛块熔炉 `104:530` 经 demand `104:1657` / supply `102:44` 追到未显示远端 factory 的钛矿机 `102:1`，并确认其 50/50 输出堵塞；独立 item `1004`、原黄糖同星路径、三页同 tick 游标和错误 filter 拒绝均通过。源码 MCP hash `E86BE095EA8FDF10D7487C65876EEFD534CE3E4684F9EC31C378F3A737A4E70E`，协议 `2025-06-18`、版本 `0.4.0.0`、50 tools，live 返回同一路径。205 项测试、完整构建和最终健康审计通过；活动/停滞 shipment 与受控故障门仍开放，没有打 tag 或发布。 | R/M/D |
| remote save `14535735`; return save `14575384` | 2026-09-03 | 为获取真实物流活动正例，伊卡洛斯正常补氢、飞至 planet `102`，把远端 ILS `44` 的钛/硅远程供应上限由 `100/100` 调整为 `200/300`。这同时揭示单条动态 needs 输入带的头部阻塞：硅满槽会挡住钛；初始无订单快照不能归因于 200 件船容量，因为既有 EXP-144 已证明当前原生调度会把阈值收紧到槽上限以下。钛达到 200 后源/需订单成为 `-200/+200`，船移动超过 2100 tick 时 Overseer 始终没有误报；取货使源钛 `200 -> 79`，送达后订单归零、船归队，母星钛块恢复 `12 min⁻¹`，远端矿机为 `30 min⁻¹`。远端配置/活动结果先保存，返航动作 `3515d9f4-8a65-404b-b7bd-79f75ed7a7bc` 再稳定落到 planet `104`，checkpoint 撤销后普通保存主档。硅随后满到 300，故扩容只算临时缓解，长期仍需拆分输入或保持实际需求。 | R/M/D |
| protected resume/save `14575416`; live audit `14585723+` | 2026-09-03 | 高频轮询活动状态时发现 3-factory 完整首屏虽无 cursor，旧 store 仍保留 60 秒并最终返回 `SERVER_BUSY`。修复后只让真正多页快照占 continuation 容量；206 项测试与 Release 完整构建通过。游戏在 tick `14575384` 正常保存/关窗，四 DLL source/deployed 一致；直接 EXE 的短命包装完成一次恢复/自动重存后退出，新一代票据仍有效，随后改由 Steam `rungameid` 稳定启动并只恢复同一 exact primary。live 连续 16 个 `limit=16` 完整页全部成功，8 个 `limit=1` 首屏占满容量，第 9 个正确 `SERVER_BUSY`；满载时完整首屏和既有 continuation 仍成功。最终审计为 planet `104`、peaceful/non-sandbox/1×、healthy、2254 built/0 prebuild、所有有负载电网满供电、Walk/0、3/3 drone idle、Journal `49/49` durable、无 blocker/checkpoint/BepInEx error。 | R/M/D |
| live audit `14874643+` | 2026-09-03 | 下一组十写以返航补给、返航/保存、两次 exact-primary 恢复和新的 20 煤采集/加注组成。最后一次采集已完成后，客户端只因读取不存在的展示字段退出；节点 `402` 精确减少 20、玩家先得到 20 煤，随后唯一 refuel 动作又使玩家归零、燃料舱出现 19 且反应堆消耗第 20 件，没有重放。严格审计保持同一 owned planet `104`、peaceful/non-sandbox/1×、healthy、2254 built/0 prebuild、三张电网满供电、Walk/0、3/3 施工机 idle、Journal `49/49` durable、无 blocker/checkpoint/BepInEx error；三厂及双星 ILS 拓扑未变。用户随后明确要求增加“人工加载私人存档后显式授权导入/认领，再由 Agent 继续”的安全流程，并进一步定稿为 Agent 先无副作用预检、在对话里单独询问、收到下一条明确确认后才提交；无需快捷键或授权码。实现必须保留原档、另存 owned 副本且不补造历史日记。 | R/M/D |
| save `17048233`; protected resume/save `17048265`; live bundle `17051000`; audit `17059827+` | 2026-09-04 | 为部署第 53 个只读 MCP 工具，先普通保存并正常关窗，再将 Release Plugin/Contracts/Core 以 source-equal 哈希部署；exact-primary 只恢复同一 planet `104` 世界并自动重存。`public_allowlist_v1` 完整页在同一 tick 返回三座 factory，三页 continuation 共享 snapshot/tick；错物品过滤和错页大小 cursor 均以 `STALE_CURSOR` 拒绝。12,156-byte 公共 JSON 未出现受保护存档字段、绝对路径、认证/写计划凭据或内部存档标记；源码 MCP `0.4.0.0` / 53 tools 成功调用 live endpoint。最终健康审计保持 2254 built/0 prebuild、三网满服务、Walk/0、满核心、3/3 施工机 idle、Journal `49/49` durable、无 blocker/checkpoint/BepInEx error。该批只含普通保存/受保护恢复，没有生产写；下一门是受控物流停滞与修复。 | R/M/D |
| bundle `17130476`; save `17136808` / revision `8` | 2026-09-04 | 为恢复远端共享带，先把母星 ILS 硅需求上限从 100 正常调到 300；原生 `+200` 订单使硅 `133 -> 333`，远端入口释放后又真实送回 189 钛石。本地钛块 PLS `918` 开始积货后，在订单归零窗口用正常 UI 路径暂时把精确供应槽从 Supply 改为 None；钛晶石制造台 `767` 在完整 600-tick 窗口被 Overseer 诊断为 `logistics_blocked / confirmed`、`logistics_configured=false`。随后把同槽恢复为 Supply，原生无人机派单、取货和送达，finding 清零，钛晶石实际速率恢复至 `12 min⁻¹`。普通保存的本地展示读取了不存在的字段而报错，但 fresh `LastOwnedSaveGameTick`、revision、healthy 和新 restart ticket 唯一核销成功，未重放。 | R/M/D |
| actions `17175929–17189787`; bundle `17177994`; audit `17196640+` | 2026-09-04 | 在唯一连续工作的精炼厂 `141` 上，把空载输入 sorter `162` 从无过滤临时设为源带不存在的铁矿；原有 4 原油耗尽后，Overseer 以原油 `0/需2` 和 600-tick 精炼油 `0 min⁻¹` 返回 `material_shortage / confirmed`。清回 filter 0 后 sorter 真实搬入原油，finding 清零，后续完整窗口恢复生产/消耗各 `12 min⁻¹`。随后把满电 ILS 充电上限 30→150 MW、钛需求 100→300；因无可用远端发货，塔仍满电、60 kW idle request、0 order，故没有把预期当成断电结果。累计十写审计核销七个 action ID 和三个保存/恢复状态：同一 owned 和平非沙盒 1× 世界、2254/0、三网 ratio 1、Walk/0、满核心、3/3 idle、Journal `49/49` durable、healthy、0 blocker/checkpoint/BepInEx error；下一写先恢复塔充电上限。 | R/M/D |
| silicon delivery `333 -> 533`; audit `17272033+` | 2026-09-04 | 第一轮扩硅需求延迟形成 `+200` 订单并真实完成往返，但连续采样证明母塔 energy 始终 12 GJ、request 始终 60 kW、network ratio 始终 1，不能从 `working vessel` 反推母塔付能。第二轮先关需求、把充电上限升至 150 MW、让火电 sorter 停料直至机组 output 0，再开放 `533/700` 需求；远端无现货，仍未派单或低压。第十写恢复火电后审计核销九个 action 与一个 fresh 槽状态：2254/0、三网 ratio 1、Walk/0、满核心、3/3 idle、Journal `49/49` durable、healthy、0 blocker/checkpoint/error。该两轮是明确负例，不冒充 `insufficient_power` 门。 | R/M/D |
| flight `2209a388-9f77-41f7-bd31-d32f7d9e6066`; save `17305571`; audit `17309480+` | 2026-09-04 | 首次 18 煤飞行预检以约 449 MJ 低于 600 MJ 安全拒绝，未创建 checkpoint。煤节点 `402` 共原生采集 80 并守恒加注；其中最后 20+20 两动作完成后汇总脚本因空集合 `.Sum` 报错，fresh 节点、背包、燃料仓核销且没有重放。飞前保存 `17296462` 后，同一原生飞行动作使距离持续下降、速度最终到 1000 m/s；煤功率不足使核心中段为 0，但航迹仍正常推进，最终在 `102` 连续 600 tick Walk/0 完成，checkpoint 撤销。第十项落地保存后审计：175 built/0 prebuild、network 1 ratio 1、Journal `49/49` durable、healthy、0 blocker/checkpoint/error；ILS `44` 持钛 200、硅 109 和 1 艘 idle vessel。 | R/M/D |
| vessel transfer/save `17333987/17334563`; audit `17338179+` | 2026-09-04 | 从落地点到远端 ILS 的 217.8 m 计划路程拆成八段约 27 m 球面短弧，全部正常 completed；一次残余速度导致下一 prepare `STALE_STATE`，等待 Walk/0 fresh 后继续，未重放。fleet transfer 使站内 idle vessel `1 -> 0`、玩家 `0 -> 1`，working 0、站能量 12 GJ 和 configuration hash 不变。第十项保存后审计核销 10/10 action：175 built/0 prebuild、单网 ratio 1、Journal `49/49` durable、healthy、0 blocker/checkpoint/error；供给塔钛 200、硅 109、Remote Supply、0 order、0/0 fleet。 | R/M/D |
| audit `17371613+` / revision `90` | 2026-09-04 | 取船后以 10 段受控短弧走到煤节点 `379` 旁；10 个 action 全部 unique/terminal/completed/succeeded，无 stall/recovery/reconciliation。玩家 Walk/0、核心约 `213.6/400 MJ`、空燃料仓、背包仍持 1 船，节点距离 `7.444 m`且剩余 `31788`煤。严格审计同时保持 175 built/0 prebuild、单网 `4050/4050` ratio 1、Journal `49/49` durable、healthy、0 blocker/checkpoint/BepInEx error；远端塔仍满电、钛 200/硅 109、Remote Supply、0 order、0/0 fleet。账本更新后写计数归零，允许开始采煤。 | R/D |
| saves `17409786/17420046`; return `17418292`; audit `17442891+` | 2026-09-04 | 煤节点 `379` 正常减少 200，背包先增 200；采集已完成但本地回显丢失，以 fresh 双端状态唯一核销并未重放。第一批 100 煤边充边烧时，另一次 100 预检正确按单格实际容量无副作用拒绝；核心充满后以 `71 + 29` 守恒加注，形成 129 煤返航储备。飞前保存后，返航 action `efabaa9f-7e11-49f6-8f4a-27d018239b67` 从受保护 ticket 恢复丢失的回显身份，稳定落到 `104` 并撤销 checkpoint，落地后再保存。母站上限 150 MW、火电 sorter 过滤并等待输出自然归零后，开钛需求使母站唯一船发出，订单 200。第十写审计核销 9 个 action ID 与采煤 fresh 状态：2254 built/0 prebuild、Journal `49/49` durable、healthy、0 blocker/checkpoint/error。当时低频读数仍为 12 GJ/60 kW、三网 ratio 1，后续证明是漏过了短启程窗口，不是运输无扣能。 | R/M/D |
| logistics/power `17471345–17512817` | 2026-09-04 | 硅需求 `533/700 -> 533/900` 后原生发船，tick `17472191` 高频读到母站约 59.7 MJ 扣能和 8.30 MW 请求，证明旧轮询只是漏过短窗口；该批硅送达 `533 -> 733`。再扩容至 1200 后，下一批硅送达 `733 -> 933`，唯一船随即切入 200 钛订单。tick `17505966–17505969` 同时抓到 ILS `11.873/12 GJ`、request `9.14 MW`、network `1` required/served/capacity `197786/115000/115000`、ratio 约 `0.5815`，bundle 对六台真实生产设备返回 `insufficient_power / confirmed`。正常清回火电 sorter filter 和 ILS 30 MW 上限后，tick `17512817` 已回到满供电、underpowered station 0、power finding 0。 | R/M/D |
| active-route saves `17572610/17579665`; resumes `17572642/17579696`; final save `17584412`; audit `17585687–17600202` | 2026-09-04 | 硅需求正常扩至 1600 后，真实运输在普通保存/正常退出/exact-primary 恢复之间继续；硅最终送达至 1533。下一笔钛运输又在活动状态保存并恢复，新 session 首个受保护样本 tick `17580795` 把 `stagnantSinceGameTick` 重置为同一当前 tick，而不是把退出期间墙钟时间算成 600-tick 停滞；200 订单、消费者缺料、源库存 60、fleet 1/active 1 均存在但 finding 为 0。随后钛送达 90、订单清零，钛块保持 `12 min⁻¹`，最终普通保存到 `17584412`。第七次十写审计确认 2254 built/0 prebuild、Journal `49/49` durable、三网 ratio 1、ILS 12 GJ/30 MW、火电链恢复、healthy、0 blocker/checkpoint/BepInEx error；玩家 Walk/0、满核心、空手搓、3/3 施工机 idle，仍守恒持有远端取回的 1 船。 | R/M/D |
| candidate preflight save `17635167`; installed-package resume `17635198`; live MCP `17640449/17647128` | 2026-09-04 | 第七次审计后先普通保存并正常关闭，再从 clean commit `f43c8ce` 生成首个 v0.4.0 预演包。manifest 232 entries、ZIP SHA-256 `b586a79452c50b94282e08f4ea09adc6766abbe08a7b0386ef6a0a2a493392a3`；包内 4 个 Plugin 运行 DLL 和 224 个自包含 MCP 文件实装哈希 mismatch 0。exact-primary 只恢复同一 planet `104` 世界并自动重存；安装版 Plugin 拒绝错误 token，安装版 MCP `0.4.0.0` / 53 tools 返回 ready 的三星球诊断包，三页共享 tick，错 item/limit cursor 都以 `STALE_CURSOR` 拒绝，公共 JSON 无禁止字段或绝对路径。最终复读发现包内安装说明仍把自己称为 v0.3.0，故该 ZIP 被明确降级为预演而非最终候选；IFX-023 已修正文档，等待从下一 clean commit 重打。 | R/M/D |
| preflight save `18143540`; recovered resumes `18144741/18145258`; audit `18160377+` | 2026-09-05 | v0.3.3 隔离兼容性验证后，active ticket 按设计恢复了最后测试副本，没有猜测长期档。只对该副本普通保存/正常关闭，再以已归档的精确归属证明和 header 重建一次短时恢复；长期世界回到 planet `104`。首读发现运行时 Journal 被重建为 0-entry，因此没有生产/科技/移动写入并再次正常关闭。只有一份 49-entry 备份同时匹配 journal ID、owned hash 和 game version；保留空文档证据后恢复它，两份文件和目录都复读 current-user-only DACL。Luna Max 后续 protected resume 确认 `49/49` durable、0 pending/error、healthy、无 blocker/checkpoint；随后只读盘点三张工厂与 Overseer，不执行游戏写入。新代恢复票据开始绑定 Journal 最小 durable sequence，未完成 live 复归前不回到发布审核态。 | R/M/D |
| saves `18274543/18283562/18290246`; checkpoint resumes `18283593/18290278`; final audit `18291377` | 2026-09-05 | `7e44e48` 先用旧票据兼容恢复同一 planet `104`，自动升级为绑定 `attached_existing_save`、tracking tick `4428079`、minimum durable sequence `49` 的新票据；runtime/handoff 两副本一致，票据和 Journal 均为 current-user-only。新票据正常保存/关闭/恢复后仍为 `49/49`。随后在 DSP 停止时保留精确受保护副本：移走 Journal 的 prepare 返回 unavailable；放回连续但只到 sequence `48` 的文档后 prepare 返回 missing/truncated/mismatch。两次均无 plan/commit/action/load，主菜单状态和同一 token 保持。逐字节恢复原 `49/49` 后，同一票据按 minimum tick `18290246` terminal 恢复；最终 owned/saved/healthy、无 blocker/checkpoint。旧 token 两处 tombstone、新票据重签通过，四个临时敏感备份文件与测试目录已删除。全程游戏内写只由 Luna Max 执行，没有生产/移动/科技/建造。 | R/M/D |
| clean candidate `8c49bcb`; installed resume `18318530`; bundle `18334303` | 2026-09-05 | 打包前 Luna Max 普通保存 tick `18318499` 并正常关窗。clean Release build 0 warning/0 error，262 tests 和 Windows CI 成功。手动包为 234 files / 233 manifest entries / 53 tools / 1 resource，SHA-256 `001323b108d76b0de4fdc9102312a8db2c8ba85d555392e7b8b572ad32542ee8`；Thunderstore 包 12 entries，SHA-256 `c8e53d60b37940cca9fe229dbaecaf4f72f4c71c599e5a63d685ae2a7a5c33ed`。实际手动包安装后 4/4 Plugin、224/224 MCP 文件 hash mismatch 0，MCP 0 extra；3 个旧 PDB 不在 manifest。protected resume 到 planet `104`，fresh `18319586` 为 owned/saved/healthy、Journal `49/49`、无 blocker/checkpoint；安装态 MCP `0.4.0.0`、53 tools、playbook 和 `public_allowlist_v1` 的 3/3 factories / 3 planets 同 tick bundle 通过。没有生产/移动/科技/建造。 | R/M/D |
| blue-line repair bundle `18410193–18434641`; save `18438905` | 2026-09-05 | clean 候选包运行态中，Overseer 先把实验室 `76` 的蓝矩阵 `0 min⁻¹` 定位为缺电路板 `1301`，同时把唯一电路板制造台 `36` 定位为输出堵塞 `20/20`。端点复读确认 `36 -> 714 -> 571` 和 `565 -> 573 -> 76` 都存在，实际断点是入口 sorter `573` 误过滤铜块 `1104`。Luna Max 只通过正常 sorter 配置动作改为电路板 `1301`；fresh 立即看到 sorter 携带 1 件电路板。完整原生 600-tick 窗口随后返回电路板 `24 min⁻¹`、蓝矩阵 `18 min⁻¹`、两者 0 finding，科技 `3401` 的 hash uploaded `3728 -> 4584`。普通保存 terminal/completed/succeeded，fresh `lastOwnedSaveGameTick=18438905`、revision `4`、owned/saved/healthy、Journal `49/49` durable、无 blocker。没有手工搬运或新增建筑。 | R/M/D |
| graphite transfers `18467403/18469135`; yellow save `18487805`; final bundle `18489233` | 2026-09-05 | 蓝线保存后的 fresh bundle 先确认红矩阵 `6 min⁻¹`，并把黄矩阵停产定位到制造台 `715` 缺高能石墨；自动源仓 `114` 有 3000 石墨，输入仓 `716` 为空且无自动入边。端点复读确认石墨源 `113 -> 116 -> 114`、钻石段 `716 -> 720 -> 715 -> 719 -> 717` 和既有 `717 -> 1212 -> belts -> 1116 -> 775 -> 777 -> 774` 均闭合。Luna Max 正常接近两座相距 18.41 m 的仓，用两次 terminal/completed transfer 守恒搬运 2000 石墨：源仓 `3000 -> 1000`、玩家 `0 -> 2000 -> 0`、目标仓动作内 `0 -> 2000`，fresh `1998` 已由生产自然取走。没有新增建筑或配置。完整窗口 tick `18484804` 为石墨 `12`、金刚石 `30`、黄矩阵 `6 min⁻¹`，最终 tick `18489233` 为蓝/红/黄 `12/6/12 min⁻¹`、石墨 `18`、金刚石 `30 min⁻¹`，全部 finding 0；三座矩阵 lab 均工作，科技 `3401` 增至 `16380/36000`。保存 terminal/completed/succeeded，fresh revision `9`、owned/saved/healthy、Journal `49/49` durable、无 blocker/checkpoint。该结果证明有界补料后的既有链恢复，不声称 `114 -> 716` 已自动化。 | R/M/D |
| graphite-route preflight `18524236+` | 2026-09-05 | 为消除 `114 -> 716` 的人工补料边界，Luna Max 先核销当前安装包 session 仅有 7 个 accepted writes，再只做有界预检。仓到仓直连被原生 `BUILD_CONNECTION_INVALID` 无副作用拒绝，确认必须使用自由带和两端 sorter。多条普通 belt prepare 虽返回合法 plan，但全路径复读后均被丢弃：最短约 20 格路线与旧 belt `246/1706` 重叠并穿过实体；南北平移分别撞仓 `717` 或旧带墙；北端两段候选又与 `644/649/1118/1121/1688` 等链重叠，唯一无重叠候选距最近实体约 1.22 m，低于当前厂区采用的安全净空。全程 0 commit/action、revision 保持 `9`、healthy、Journal `49/49`。该自动线在当前有界普通路线集合内明确 blocked，不继续无界枚举，也不以额外 transfer 冒充修复。 | R/D |
| upgrade completion `18593844`; save `18639872`; tech selection `18653067/18653068` | 2026-09-05 | 三色矩阵持续生产期间，升级 `3401 运输船引擎` 自然达到 `36000/36000` 并在本局 `003d 14:04:57` 完成。普通保存 terminal/completed/succeeded；fresh tick `18639912`、revision `10`、healthy、Journal `49/49`。随后只读完整 314 条 progression state，而不是把 40 条已完成 unlocked 项误当成无科技可选；274 条未完成/未满级中有 30 条前置均已完成。运行时 `CanEnqueueTech` 允许物流方向的 `1608 配送物流系统`（前置 `1602/1702`，需蓝 600、红 300，解锁 recipe `122/123`），唯一选择动作 terminal/completed/succeeded。Journal sequence `50` 在 tick `18653068`（实际 `2026-09-05T18:47:34.1336132+08:00`、本局 `003d 14:21:24`）durable 记录首次科技选择；fresh 队列仅 `[1608]`、revision `12`。完整窗口 tick `18658636` 为蓝/红/黄 `24/12/6 min⁻¹` 且全部 finding 0；当前 installed session accepted count 为 9。 | J/R/D |
| tech completion `18747873`; save `18750214`; audit `18750253–18753452` | 2026-09-05 | `1608 配送物流系统` 依靠既有蓝/红矩阵链自然达到 `108000/108000`，精确 unlock tick `18747873`（本局 `003d 14:47:44`）；稍后 fresh 在 tick `18748916` 观察到 completed/unlocked、current tech `0`、队列为空。作为本安装 session 的第 10 个 accepted write，普通保存 terminal/completed/succeeded 到 tick `18750214`；fresh tick `18750253`、revision `13`、owned/saved/healthy、Journal `50/50` durable、无 blocker/checkpoint。严格复核 #1 protected resume、#2 sorter `573` filter 修复、#3/#7/#8/#10 四次保存、#4 移动、#5/#6 两次石墨守恒转运、#9 选择 `1608`，全部有 terminal/fresh 闭环，无重放、unknown outcome 或未解释差量。最终只读 bundle tick `18753452` 为石墨/蓝/红/黄 `18/18/12/6 min⁻¹`、三种矩阵 finding 0；钻石本窗口为 0，符合输入仓 `716` 再次耗尽且 `114 -> 716` 尚未自动连接的已知边界。recipe `122` 为物流配送器 `2107`（铁块×8、电浆激发器×4、处理器×4，8 s），recipe `123` 为配送运输机 `5003`（铁块×2、动力引擎×1、处理器×1，2 s），均已解锁且可手搓/制造台生产；本轮只读未启用配方或施工。 | R/M/D |

## 科技树与升级

### 日记挂接前：已完成科技/升级

以下 `unlockTick` 和本局时间来自当前世界运行态，能够证明完成顺序；“首次点击”的实际时间与 tick 当时尚未记录。

| 类型 | ID | 名称 | 解锁 tick | 本局时间 |
|---|---:|---|---:|---|
| 根科技 | 1 | 戴森球计划 | 0 | `000d 00:00:00` |
| 科技 | 1001 | 电磁学 | 7208 | `000d 00:02:00` |
| 科技 | 1002 | 电磁矩阵 | 11782 | `000d 00:03:16` |
| 科技 | 1201 | 基础制造 | 79246 | `000d 00:22:00` |
| 科技 | 1401 | 自动化冶金 | 96122 | `000d 00:26:42` |
| 科技 | 1601 | 基础物流系统 | 111872 | `000d 00:31:04` |
| 科技 | 1101 | 高效电浆控制 | 346608 | `000d 01:36:16` |
| 科技 | 1120 | 流体储存封装 | 356976 | `000d 01:39:09` |
| 科技 | 1102 | 等离子萃取精炼 | 394121 | `000d 01:49:28` |
| 科技 | 1411 | 钢材冶炼 | 428708 | `000d 01:59:05` |
| 科技 | 1402 | 冶炼提纯 | 456629 | `000d 02:06:50` |
| 科技 | 1111 | 能量矩阵 | 505693 | `000d 02:20:28` |
| 科技 | 1412 | 火力发电 | 511154 | `000d 02:21:59` |
| 科技 | 1805 | 动力引擎 | 3580638 | `000d 16:34:37` |
| 升级 | 2101 | 机甲核心 1 | 3744134 | `000d 17:20:02` |
| 升级 | 2901 | 驱动引擎 1 | 3821275 | `000d 17:41:27` |
| 升级 | 2102 | 机甲核心 2 | 3932513 | `000d 18:12:21` |
| 升级 | 2902 | 驱动引擎 2 | 4013644 | `000d 18:34:54` |

### 日记挂接后：首次点击与最终完成

“点击实际时间/本局时间”均为持久化日记中的首次选择事件；“完成”来自当前世界的原生 `unlockTick`。当前仍在研究的项目明确标为未完成。

| J序号 | 类型 | ID / 名称 | 首次点击实际时间 | 点击 tick / 本局时间 | 完成 tick / 本局时间 |
|---:|---|---|---|---|---|
| 1 | 科技 | 1121 基础化工 | 2026-09-01 00:49:36.0107101 +08:00 | 4462081 / `000d 20:39:28` | 4643972 / `000d 21:29:59` |
| 2 | 科技 | 1122 高分子化工 | 2026-09-01 04:07:34.8973401 +08:00 | 4843047 / `000d 22:25:17` | 5543236 / `001d 01:39:47` |
| 3 | 科技 | 1413 钛矿冶炼 | 2026-09-01 05:24:18.3528138 +08:00 | 5064764 / `000d 23:26:52` | 5692950 / `001d 02:21:22` |
| 4 | 科技 | 1403 晶体冶炼 | 2026-09-01 05:24:36.1337534 +08:00 | 5065831 / `000d 23:27:10` | 6081424 / `001d 04:09:17` |
| 7 | 科技 | 1701 电磁驱动 | 2026-09-01 07:06:58.1108970 +08:00 | 5434156 / `001d 01:09:29` | 6090437 / `001d 04:11:47` |
| 10 | 科技 | 1123 高强度晶体 | 2026-09-01 12:34:21.3647316 +08:00 | 6102006 / `001d 04:15:00` | 6509179 / `001d 06:08:06` |
| 17 | 科技 | 1124 结构矩阵 | 2026-09-01 14:29:27.6313687 +08:00 | 6516327 / `001d 06:10:05` | 6894549 / `001d 07:55:09` |
| 19 | 科技 | 1602 改良物流系统 | 2026-09-01 16:22:15.2660757 +08:00 | 6922333 / `001d 08:02:52` | 6931331 / `001d 08:05:22` |
| 20 | 科技 | 1702 磁悬浮 | 2026-09-01 16:29:37.9987804 +08:00 | 6948897 / `001d 08:10:14` | 7371252 / `001d 10:07:34` |
| 21 | 科技 | 1603 高效物流系统 | 2026-09-01 18:49:00.7587296 +08:00 | 7381605 / `001d 10:10:26` | 7443355 / `001d 10:27:35` |
| 23 | 科技 | 1311 半导体材料 | 2026-09-01 19:06:25.2950713 +08:00 | 7444263 / `001d 10:27:51` | 7479265 / `001d 10:37:34` |
| 25 | 科技 | 1302 处理器 | 2026-09-01 19:19:10.4118291 +08:00 | 7490161 / `001d 10:40:36` | 7702430 / `001d 11:39:33` |
| 30 | 科技 | 1131 应用型超导体 | 2026-09-01 20:19:32.3382353 +08:00 | 7706937 / `001d 11:40:48` | 7745153 / `001d 11:51:25` |
| 31 | 科技 | 1112 氢燃料棒 | 2026-09-01 20:45:07.4347057 +08:00 | 7798967 / `001d 12:06:22` | 7834800 / `001d 12:16:20` |
| 33 | 科技 | 1113 推进器 | 2026-09-01 21:00:10.7854428 +08:00 | 7853160 / `001d 12:21:26` | 8098237 / `001d 13:29:30` |
| 35 | 升级 | 3701 垂直建造 | 2026-09-01 22:24:19.1590085 +08:00 | 8155733 / `001d 13:45:28` | 8243611 / `001d 14:09:53` |
| 36 | 科技 | 1703 粒子磁力阱 | 2026-09-01 22:49:32.8062890 +08:00 | 8244528 / `001d 14:10:08` | 8696391 / `001d 16:15:39` |
| 37 | 科技 | 1604 行星物流系统 | 2026-09-02 11:57:19.5398641 +08:00 | 8651736 / `001d 16:03:15` | 8836460 / `001d 16:54:34` |
| 39 | 科技 | 1114 加力推进器 | 2026-09-02 12:48:46.7314534 +08:00 | 8714995 / `001d 16:20:49` | 10088609 / `001d 22:42:23` |
| 40 | 科技 | 1414 高强度钛合金 | 2026-09-02 12:48:47.0102461 +08:00 | 8715000 / `001d 16:20:50` | 11098574 / `002d 03:22:56` |
| 44 | 科技 | 1605 星际物流系统 | 2026-09-03 05:08:03.9709304 +08:00 | 11122095 / `002d 03:29:28` | 11808407 / `002d 06:40:13` |
| 49 | 升级 | 3401 运输船引擎 | 2026-09-03 14:11:00.9597999 +08:00 | 12956512 / `002d 11:59:01` | 18593844 / `003d 14:04:57` |
| 50 | 科技 | 1608 配送物流系统 | 2026-09-05 18:47:34.1336132 +08:00 | 18653068 / `003d 14:21:24` | 18747873 / `003d 14:47:44` |

## 第一次手搓与第一次流水线产出

### 日记覆盖期内：第一次手搓

手搓与流水线使用两个独立的游戏原生计数域，不互相覆盖。

| J序号 | 物品 | 首次完成数量 | 实际时间 | tick | 本局时间 | 来源 |
|---:|---|---:|---|---:|---|---|
| 5 | 2309 化工厂 | 1 | 2026-09-01 05:55:20.9818785 +08:00 | 5176512 | `000d 23:57:55` | mecha forge feature counter |
| 13 | 2306 抽水站 | 1 | 2026-09-01 13:12:59.3055037 +08:00 | 6241068 | `001d 04:53:37` | mecha forge feature counter |
| 24 | 2012 高速分拣器 | 2 | 2026-09-01 19:12:00.3596429 +08:00 | 7464363 | `001d 10:33:26` | mecha forge feature counter |

### 日记覆盖期内：第一次流水线产出

| J序号 | 物品 | 首次观测数量 | 实际时间 | tick | 本局时间 | 对应里程碑保存 |
|---:|---|---:|---|---:|---|---:|
| 6 | 1115 塑料 | 1 | 2026-09-01 06:19:28.0599282 +08:00 | 5263306 | `001d 00:22:01` | 5265117 |
| 8 | 1106 钛块 | 1 | 2026-09-01 08:21:33.1640020 +08:00 | 5702401 | `001d 02:24:00` | 5705293 |
| 9 | 1112 金刚石 | 1 | 2026-09-01 12:29:17.0019946 +08:00 | 6083748 | `001d 04:09:55` | 6090507 |
| 11 | 1201 齿轮 | 1 | 2026-09-01 13:04:07.1868574 +08:00 | 6209146 | `001d 04:44:45` | 6221009（电动机线） |
| 12 | 1203 电动机 | 1 | 2026-09-01 13:04:11.9629982 +08:00 | 6209433 | `001d 04:44:50` | 6221009 |
| 14 | 1000 水 | 1 | 2026-09-01 13:14:06.2593487 +08:00 | 6245078 | `001d 04:54:44` | 6267723 |
| 15 | 1117 有机晶体 | 1 | 2026-09-01 13:32:52.5005411 +08:00 | 6312637 | `001d 05:13:30` | 6315704 |
| 16 | 1118 钛晶石 | 1 | 2026-09-01 14:28:07.1667461 +08:00 | 6511499 | `001d 06:08:44` | 6518917 |
| 18 | 6003 结构矩阵 | 1 | 2026-09-01 16:15:29.8733116 +08:00 | 6898014 | `001d 07:56:06` | 6905142 |
| 22 | 1204 电磁涡轮 | 1 | 2026-09-01 18:58:02.9267906 +08:00 | 7414129 | `001d 10:19:28` | 7419065 |
| 26 | 1105 高纯硅块 | 1 | 2026-09-01 19:25:48.3212096 +08:00 | 7514032 | `001d 10:47:13` | 7517473 |
| 27 | 1302 微晶元件 | 1 | 2026-09-01 19:33:48.6147297 +08:00 | 7542841 | `001d 10:55:14` | 7545277 |
| 28 | 1116 硫酸 | 4 | 2026-09-01 20:06:51.7681504 +08:00 | 7661594 | `001d 11:28:13` | 7663628 |
| 29 | 1303 处理器 | 1 | 2026-09-01 20:18:50.8692405 +08:00 | 7704459 | `001d 11:40:07` | 7707489 |
| 32 | 1123 石墨烯 | 2 | 2026-09-01 20:59:13.2038998 +08:00 | 7849705 | `001d 12:20:28` | 7854029 |
| 34 | 1405 推进器 | 1 | 2026-09-01 22:09:09.3196038 +08:00 | 8101277 | `001d 13:30:21` | 8123715 |
| 38 | 1206 粒子容器 | 1 | 2026-09-02 12:34:53.5090990 +08:00 | 8698306 | `001d 16:16:11` | 8699182 |
| 41 | 5001 物流运输机 | 1 | 2026-09-02 20:02:42.8990067 +08:00 | 9346766 | `001d 19:16:19` | 9369181 |
| 42 | 2103 行星内物流运输站 | 1 | 2026-09-02 20:20:29.8130148 +08:00 | 9410766 | `001d 19:34:06` | 9413535 |
| 45 | 1107 钛合金 | 4 | 2026-09-03 05:22:18.6349206 +08:00 | 11172619 | `002d 03:43:30` | 11175248 |
| 46 | 1406 加力推进器 | 1 | 2026-09-03 08:29:09.1992358 +08:00 | 11827563 | `002d 06:45:26` | 11830296 |
| 47 | 5002 星际物流运输船 | 1 | 2026-09-03 08:47:27.6896746 +08:00 | 11890591 | `002d 07:02:56` | 11896243 |
| 48 | 2104 星际物流运输站 | 1 | 2026-09-03 08:56:28.0954174 +08:00 | 11921722 | `002d 07:11:35` | 11926992 |

### 日记挂接前：可证明但无法恢复精确首次时间的产物

| 产物/产线 | 已有可靠证据 | 能诚实给出的时间边界 |
|---|---|---|
| 铁矿、铜矿、石矿、煤及其基础冶炼产物 | 普通采集、矿机、熔炉、储仓和后续复用拓扑均有结构化读回 | 在早期自动化阶段已经运行；逐物品首次 tick 和实际时间未记录 |
| 磁线圈、电路板、电磁矩阵（蓝糖） | 早期蓝糖生产与后续研究供料、修复后的输入/输出增长均有证据 | 在第一颗红糖之前已自动生产；精确首次时间未记录 |
| 高能石墨、原油、精炼油、氢 | 红糖链上游的矿机/油井、精炼厂、分流和储存读回完整 | 在第一颗红糖之前已自动生产；精确首次时间未记录 |
| 能量矩阵（红糖） | 研究站 `256` 输出 `0 -> 3 -> 6`，随后普通保存 | 首次产出不晚于 save tick `2499658` / `000d 11:34:20`；Git 在 2026-08-31 15:09:53 记录里程碑，但这不是产出实际时刻 |
| 动力引擎 | 制造台连续输出，仓 `287` 由 `9 -> 30`，随后普通保存 | 首次产出不晚于 save tick `3746997` / `000d 17:20:49`；Git 在 2026-08-31 19:52:34 记录里程碑 |
| 钢材及其他早期建筑物手搓 | 早期工厂和后续钢线续采证明物品存在且按正常规则制造 | 日记挂接前的逐物品首次手搓事件不可恢复；不把“第一次看到”冒充“第一次做出” |

## 已固化的用户级决策

这些是贯穿本局、后续继续开发时仍然生效的高层决策。

1. 只要世界仍健康，就持续使用同一个存档；隔离、卡路或失败都不等于开新档。
2. “安全隔离”只表示写入能力因无法证明安全而 fail-closed；先读状态并恢复证据，不能把隔离当成删档或流程失败。
3. 需要清理进程状态时，先正常保存，再正常关闭并通过 Steam 正式启动链重开；不强杀游戏。
4. 不凭猜测判断卡住：移动使用位移与剩余距离看门狗，尽早结束卡脚订单；能量不足则自动前往已验证无线塔充电。
5. 玩家被基座卡住时，使用局部切平面/有界四向短探测脱困；若业务对象已在交互范围内，不继续撞向目标。
6. 所有游戏控制只调用 Spherewright 结构化 Bridge/MCP；不用键鼠、Computer Use、视觉识别或直接内存改物品。
7. 每次写操作必须 fresh read → 非写 prepare → 校验 → 单次幂等 commit → terminal/readback；结果不明时先协调证据，绝不盲目重放。
8. 优先建可持续生产线；等待科研或产量时才做文档、代码、安全修复和次要搬运。
9. 基础设施可以正常手搓，但产品里程碑必须由生产设备自动产出，不能用手搓产物冒充流水线。
10. M0 在第一颗红糖后冻结；当前阶段是 post-M0 行星/星际物流，不再受旧 M0“禁止飞行/物流塔”的阶段限制。
11. 黄糖成立的范围是最后转换段自动；钛矿跨星球手动搬回不等于跨星球物流自动化。
12. 实际星际飞行前必须单独保存 checkpoint；失败只反复加载同一个 checkpoint，成功稳定落地并保存主档后立即退役旧能力。
13. checkpoint、planned restart 和 quarantine 各有独立生命周期与唯一载入源；不能让旧票据把新世界回滚。
14. 每完成一种新产品流水线，必须有自动输出证据、普通同档保存、一次 Git commit 和 push。
15. 每个新档独立维护日记；第一次手搓、第一次流水线产出分别计数，第一次点科技和升级也记录实际时间与本局时间。
16. 经验不只“追加”：所有新经验立即落盘，后续持续复验；旧结论可以升级为 validated，也可以 invalidated 或 superseded。
17. 混料仓和多原料设备先在空载时锁定全部 sorter filter，再装料；科技门控配方先以 recipe `0` 预建，解锁后只启用一次。
18. 产线验收必须包括供电容量、设备工作、输入下降、输出至少两次增长和最终保存，不能只看“配方已设置”。
19. Steam 账号、Windows 账号或可见存档名都不是 owned-world 身份；只信受保护的高熵主档身份、ticket、tick、星球和模式校验。
20. 仓库公开资料、README/About 与执行规范保持同步，许可证使用 MIT；CI 覆盖不依赖 DSP DLL 的工程。

## 完整决策/经验索引

当前共 `163` 条：`validated=110`、`observed=50`、`invalidated=2`、`superseded=1`。`observed` 表示已有样本但仍需复验；`invalidated` 和 `superseded` 不能继续作为现行规则。每条的适用范围、当前结论、直接证据与复验触发在 [experience-ledger.md](./experience-ledger.md) 中完整保存。

| ID | 状态 | 决策/经验 |
|---|---|---|
| EXP-001 | validated | 部署前显式构建完整解决方案 |
| EXP-002 | validated | DSP 应通过 Steam 启动链启动 |
| EXP-003 | validated | 运行时描述文件使用 `bridge-*.json` |
| EXP-004 | validated | 跨进程恢复票据需要可见且受保护的固定交接目录 |
| EXP-005 | validated | 只有严格绑定的 LastExit 恢复才能延续同一 owned world |
| EXP-006 | observed | 主菜单 demo 状态没有暴露仍可用的恢复票据 |
| EXP-007 | validated | 客户端包装失败不代表游戏动作未执行 |
| EXP-008 | validated | 施工无人机会使玩家状态哈希短时变化 |
| EXP-009 | validated | 移动 action 终态不等于物理速度已经归零 |
| EXP-010 | invalidated | 三台风机只够覆盖当前油井的紧负载 |
| EXP-011 | validated | 当前储仓/热电姿态在约 6.41 m 成功、8.90 m 与 12.8 m 失败 |
| EXP-012 | validated | 同位置旧分拣器必须从新实体归属候选中排除 |
| EXP-013 | validated | outcome unknown 后只允许证据化协调，不允许猜测重试 |
| EXP-014 | validated | PowerShell helper 要避开保留名、别名并显式处理空聚合 |
| EXP-015 | validated | M0 执行顺序以可持续生产线为主 |
| EXP-016 | validated | 孤立热电站不能依靠无电分拣器完成冷启动 |
| EXP-017 | validated | 当前油井与基础分拣器负载远低于三台风机容量 |
| EXP-018 | validated | 批量手搓原料会进入复制队列缓冲，终态才代表整批完成 |
| EXP-019 | validated | 当前两座电线杆约 22.57 m 不会自动连线 |
| EXP-020 | validated | 油井到精炼厂不能把两台建筑都直接绑定为传送带端口 |
| EXP-021 | validated | 自动燃料输入仓的旧余量不能保留给后续手工预算 |
| EXP-022 | validated | 长距离施工前可从已验证主仓正常补充机甲燃料 |
| EXP-023 | validated | 无线输电必须用独立于燃料的核心能量与电网差量验收 |
| EXP-024 | validated | 建筑有电不代表相邻分拣器处于电塔覆盖范围 |
| EXP-025 | validated | 精炼链启动后必须重新按整网峰值校核容量 |
| EXP-026 | observed | 建造完成后的首次单体查询仍应允许一次只读重读 |
| EXP-027 | invalidated | 新分拣器验收必须证明端点既有连接未被覆盖 |
| EXP-028 | validated | 分拣器运行拓扑以目标字段和物料流为准 |
| EXP-029 | observed | 储液罐的公开工厂快照此前未采集流体缓冲 |
| EXP-030 | validated | 当前仓库使用本地 SDK，程序集字段研究优先 Mono.Cecil |
| EXP-031 | observed | 范围内 harvest 会通过正常玩家动作接近资源点 |
| EXP-032 | validated | 从活跃货带末端接续路径会把端点货物回收到玩家 |
| EXP-033 | validated | 首个自动红矩阵必须以同一研究站的 0→正数闭环验收 |
| EXP-034 | validated | 红矩阵运行态会暴露电网容量瓶颈，新增风机后已恢复满供电 |
| EXP-035 | observed | 长途移动前必须自动做能量预算并保留回充余量 |
| EXP-036 | observed | 移动必须用位移和剩余距离双窗口提前判停 |
| EXP-037 | validated | 产线里程碑必须同时保存游戏并提交推送工程经验 |
| EXP-038 | observed | 主菜单加载动作必须在副作用前证明幂等容量并收敛票据副本 |
| EXP-039 | validated | player order 必须用对象引用证明归属并在动作终态精确终止 |
| EXP-040 | observed | factory objectId 与 resource nodeId 是独立命名空间，harvest 必须限距 |
| EXP-041 | validated | 无燃料机甲仍有低速原生基础发电，但只可作为应急等待 |
| EXP-042 | validated | 原始桥 DTO 与 MCP 参数形状不同，精确建造必须先核对 plannedPosition |
| EXP-043 | validated | 枯竭矿机不能阻断旧生产设备，应用新矿带侧向续入 |
| EXP-044 | validated | 混料公共带的背压必须先隔离上游灌入源 |
| EXP-045 | validated | 建筑落点合法不代表后续分拣器可达 |
| EXP-046 | validated | 自动回充必须绑定真实无线塔，并在动作终态检查残留移动 |
| EXP-047 | validated | 星际航行必须先落独立检查点，失败只重复加载同一票据 |
| EXP-048 | observed | 首次手搓与首次产线产出必须使用两个原生计数域 |
| EXP-049 | validated | 互为必需的两种原料不能无约束共用短带 |
| EXP-050 | observed | 原生 Sail 切换与离开母星必须分成两个受控阶段 |
| EXP-051 | observed | 长距离地表移动使用有界球面分段，并在每段后等待惯性归零 |
| EXP-052 | validated | 飞行动作不能把瞬时 Walk 当成稳定着陆 |
| EXP-053 | validated | 地表低速不等于落地，Drift 必须转向已验证的陆地锚点 |
| EXP-054 | observed | 跨区长带必须先扫起点方向，再按施工前沿分段并从陆地等待 |
| EXP-055 | observed | 核心建筑合法落位不代表分拣器可达，短带可以守恒中继 |
| EXP-056 | validated | 多产物设备会被任一满仓出口反压，先守恒腾位再判断上游故障 |
| EXP-057 | observed | 贴住建筑基座时沿局部切平面背离障碍脱困 |
| EXP-058 | observed | 混料仓的无过滤输出会污染专线，过滤应在进料前绑定或改走纯源旁路 |
| EXP-059 | observed | 扩研究消费者后必须重测原料斜率，交替工作通常是供料而非研究站数量瓶颈 |
| EXP-060 | observed | 双产物设备扩容要先过滤空出口、再通原料，并把端点供电与下游电网分开验收 |
| EXP-061 | observed | 多基座夹缝脱困使用有界四向短探测，不把单障碍背离法无限外推 |
| EXP-062 | validated | 锁定配方的预建产线只在科技解锁后激活，里程碑以自动产出、日记和普通保存三重验收 |
| EXP-063 | observed | 进度哈希包含实时上传量，活跃研究时追加队列会安全 stale |
| EXP-064 | observed | 主菜单恢复票据可见性与最新交接 tick 必须分层验证 |
| EXP-065 | validated | 双产物混料仓下游必须先纯化，短期目标流量不等于永久专线 |
| EXP-066 | observed | 已验证陆地锚点不证明中间弧段可通行，首次 Drift 即停路线并返回精确落点 |
| EXP-067 | observed | 历史产出不代表恢复后拓扑仍完整，研究停滞先追到上游满缓冲和端点 |
| EXP-068 | validated | 非带建筑的分拣器端口必须排除已占槽位，否则后建连接会覆盖旧端点 |
| EXP-069 | validated | 健康同档重启应在正常保存时签发最新 tick 的受保护一次性交接票据 |
| EXP-070 | validated | 传送带分拣器附着方位是虚拟 slot，完工反查必须扫描实际连接槽 |
| EXP-071 | superseded | LastExit 未刷新时只能回到票据绑定的最新健康主档，不能伪造关闭证据 |
| EXP-072 | validated | Plugin 引用的新 Core 类型要求同批部署所有 Spherewright 程序集 |
| EXP-073 | observed | 上游修复不能以首个局部流量为终点，必须逐层复读到最终消费者 |
| EXP-074 | validated | 混合备料仓必须在空载时先完成全部出口过滤，再按物料逐项装仓 |
| EXP-075 | validated | 抽水站由原生水面校验放置，并从专用泵口先接带再接仓 |
| EXP-076 | observed | 移动被基座提前终止时，若业务目标已在操作范围内就不要继续撞目标 |
| EXP-077 | observed | 紧凑产线施工验收还要包含玩家撤离空间，原生放置合法不等于不会夹脚 |
| EXP-078 | validated | 混合副产物仓无法安全直出时，用空纯源中转仓恢复长带并追到最终科研增长 |
| EXP-079 | validated | 锁定科技的黄矩阵线先空载过滤预建，解锁后以输入下降、输出增长、日记和保存验收 |
| EXP-080 | observed | 活跃仓并发补货会掩盖 transfer 的源端净差量 |
| EXP-081 | observed | 正常保存重启清理进程状态，但不会把玩家移出已保存的碰撞夹缝 |
| EXP-082 | validated | 星际飞行失败只能重载同一绑定检查点，失败分类必须结构化 |
| EXP-083 | observed | 新主档时间线一旦覆盖飞行 checkpoint，旧回档能力必须立即失效 |
| EXP-084 | observed | 健康重启与隔离恢复必须选择不同的唯一载入源 |
| EXP-085 | observed | 满供电矿机没有资源节点时先判定矿脉耗尽，再用独立出料侧接存量主干 |
| EXP-086 | observed | 制造设备没有原生带口时，用自由短带和两端分拣器完成独立输入支路 |
| EXP-087 | observed | 扩建资源链前先追踪现有矿机和有向带，优先复用真实自由端 |
| EXP-088 | validated | 混合输入仓先锁定双过滤再装第二种物料，可安全驱动两原料制造台 |
| EXP-089 | validated | 三原料化工线应先完成空仓过滤和输出，再守恒装入自动来源 |
| EXP-090 | validated | 旧混料带堵塞时，可用混合仓到目标设备的原生需求直供恢复关键链 |
| EXP-091 | validated | 受科技门控的双原料产线可保持 recipe 0 完成全套预建，解锁后只启用一次 |
| EXP-092 | validated | 科技门控的双原料化工线可先锁过滤，再在交互半径内远程守恒装料 |
| EXP-093 | observed | 未知地形长途先用非带实体构造陆地锚点链，跨水只把已验证对岸作为连续终点 |
| EXP-094 | validated | 配方可在科技门控期预建，但未解锁物品的 sorter filter 必须延后 |
| EXP-095 | validated | 新设备投产后要把电网容量纳入产线验收 |
| EXP-096 | observed | 满仓是槽位语义；新 sorter 可能在过滤前预取混料 |
| EXP-097 | validated | 物流塔观察交叉绑定实体、站点池和星球并拆分实时/配置指纹 |
| EXP-098 | validated | 物流塔槽位配置只采用 SetStationStorage 不换品子集 |
| EXP-099 | validated | 物流塔 energyPerTick 是实时请求，充电上限位于 PowerConsumer |
| EXP-100 | validated | 选科技使用排除自然上传量但保留队列/解锁/前置的专用哈希 |
| EXP-101 | validated | 物流塔最大充电功率绑定 prefab UI 刻度和 consumer 身份 |
| EXP-102 | validated | 活跃分拣器过滤绑定零携货配置指纹，不绑定返程进度 |
| EXP-103 | validated | 自动管理研究物品会先把背包矩阵保留到 MechaLab |
| EXP-104 | validated | 自包含发布包需通过干净提交、清单哈希和 MCP 协议三层复验 |
| EXP-105 | validated | 物流塔载具装载绑定工作中数量、原型容量与增产点损失边界 |
| EXP-106 | validated | 物流运输机以科技门控、过滤输入、自动首产和普通保存闭环 |
| EXP-107 | validated | 多数量建筑配方以完整输入批次、产出仓、durable journal 和普通保存验收 |
| EXP-108 | validated | 本地 PLS 的 StationComponent.planetId 使用 0 哨兵，身份策略须与星际站分流 |
| EXP-109 | validated | 首座 PLS 投运拆分产物、站体、供电、配置、机队和真实路线六道证据门 |
| EXP-110 | validated | PLS 可直接接输入带，带口存储编号为一基，需求侧 fleet 可独立取货 |
| EXP-111 | validated | 阶段转入持续跨星物流时，资源航行应携带或就地补齐完整远端矿站包 |
| EXP-112 | validated | 资源矿机选址比较全部原生合法姿态并最大化覆盖，低效矿机只经正常回收重建 |
| EXP-113 | observed | 飞行 prepare 能量下限只是准入门，零能接触表面不等于稳定着陆 |
| EXP-114 | validated | 海面 Drift 接触后用原生 MoveTo 走向最近且已证明的干燥邻域 |
| EXP-115 | validated | 多矩阵科技必须把全部必需矩阵送入同一座独立研究站 |
| EXP-116 | observed | 普通 Move 落水后只重试未提交的状态竞争并退回已验证陆地点 |
| EXP-117 | validated | 物流站输出带连接与货物选择是两个独立状态 |
| EXP-118 | validated | 双原料专用支线可经 sorter 注入共享主干并由过滤仓安全分流 |
| EXP-119 | observed | 已验证锚点链会被后续扩建改变，反向通行不能继承旧结论 |
| EXP-120 | validated | 复用休眠纯料仓和既有有向带可最小化恢复下游，但有限补料不等于永久上游 |
| EXP-121 | observed | 原生可建不等于玩家净空，带路应按完整 plannedPath 复核建筑距离 |
| EXP-122 | observed | 传送带可跨水，沿带移动仍必须逐段验证 Walk |
| EXP-123 | observed | 带路实体 ID 顺序不是拓扑顺序，自由末端必须以连接和位置复读 |
| EXP-124 | validated | 原生带路 prepare/commit 可能与既有带重叠，提交前必须全厂排除旧带占位 |
| EXP-125 | observed | 无副作用 prepare 仍占短期计划容量，密集候选扫描必须分批等待过期 |
| EXP-126 | validated | 长带连通不代表末端分拣器有电，通料前必须逐个复读供电 |
| EXP-127 | observed | 供料链持续工作不等于具备下一科技的吞吐余量 |
| EXP-128 | observed | Move 目标未达不等于已取得的安全位移必须作废 |
| EXP-129 | observed | 复用过滤多料仓能防串料，但不能保证同物料的产线分配优先级 |
| EXP-130 | observed | 单个矿点锚定的最优姿态不等于整簇矿脉的最优姿态 |
| EXP-131 | observed | 普通矿机原生出料先接传送带，不能假设可直接接分拣器 |
| EXP-132 | observed | 未取得动作终态时，单次 fresh 实体扫描仍可能处于预建筑收尾中 |
| EXP-133 | observed | 既有带占位无法绕开时，可用独立带段和受电分拣器做显式拓扑桥 |
| EXP-134 | observed | 十写审计不能假定较早动作结果仍由运行时保留 |
| EXP-135 | observed | 为范围型业务移动时，可在交互半径内优化短弧的非带实体净空 |
| EXP-136 | validated | 科研剩余矩阵数必须按科技的 pointsPerHash 换算 |
| EXP-137 | observed | 按数量取仓库物品不保证立刻释放物品格 |
| EXP-138 | validated | 空载物流载具单元可在科技解锁后重配为星际运输船线 |
| EXP-139 | validated | 星际物流站升级线必须把站体、合金和粒子容器作为独立守恒输入 |
| EXP-140 | observed | ILS 接入既有小电网后应立即降到本机型原生最低充电档 |
| EXP-141 | observed | 星际直线航向必须避开中间天体的原生 1000 m 捕获层 |
| EXP-142 | validated | 电网连通与建筑受电是两个独立条件 |
| EXP-143 | observed | 带路交叉时在重叠点前用受电分拣器显式汇流 |
| EXP-144 | validated | 跨星取货必须用货槽订单与源库存下降共同验收 |
| EXP-145 | validated | 密集旧厂区长带必须做全路径占位排除并分段提交 |
| EXP-146 | validated | 运行时 Journal、逐档日记和工程事故簿必须分层 |
| EXP-147 | validated | 专用输出仓可经受电分拣器汇入带过滤消费者的既有混合入料带 |
| EXP-148 | validated | 路由候选器空输出必须区分重叠、材料和计划容量拒绝 |
| EXP-149 | observed | 同坐标传送带必须用拓扑区分原生转角和游离重复层 |
| EXP-150 | validated | 满吞吐 sorter 无可配置空窗时须改走有证据的源链纯化 |
| EXP-151 | observed | 多源共享带的单 sorter 后置桥会被上游满流量饿死 |
| EXP-152 | validated | 正式包必须统一产品版本源并经实时 Plugin/MCP 握手校验 |
| EXP-153 | validated | 候选包实机通过不能替代最终 clean 工件本体复验 |
| EXP-154 | validated | 诊断速率只按连续游戏 tick 计算且根因分类须等待完整周期 |
| EXP-155 | validated | 自动产线实际速率优先复用 DSP 随档持久化的 600-tick 原生环 |
| EXP-156 | validated | 跨星球摘要必须共享一个有界快照并区分真实发电与防御场导出 |
| EXP-157 | validated | 理论产能必须从当前组件公式重算，耗尽矿机是合法零容量 |
| EXP-158 | validated | 输出堵塞必须复现当前组件允许下一周期的原生缓冲门 |
| EXP-159 | validated | 物流根因只能沿消费者的真实有向货运拓扑绑定到塔 |
| EXP-160 | validated | 活跃周期和物品级正产量必须阻止瞬时空输入误报 |
| EXP-161 | observed | 故障 finding 是 captured tick 的快照，不是可跨 tick 固化的根因 |
| EXP-162 | validated | 递归根因只沿物料兼容的物理上游，图边界必须显式可见 |
| EXP-163 | validated | 分流器过滤必须按精确输出口双向约束递归路径 |
| EXP-164 | observed | 物流停滞只能由按档保护的连续游戏 tick 窗口给出 suspected 结论 |
| EXP-165 | validated | 跨星生产者只能从精确供应塔的真实输入带继续证明 |
| EXP-166 | validated | 无 continuation 的完整首屏不占分页快照容量 |
| EXP-167 | observed | 混合输入带的满槽物料会造成头部阻塞；起送阈值沿用 EXP-144 |
| EXP-168 | validated | 人工读档交接必须在无副作用预检后由用户另行对话确认 |
| EXP-169 | validated | 跨域诊断只在同一主线程 tick 经完整身份匹配后合并 |
| EXP-170 | validated | 受控物流故障用正常路由配置制造并以真实送达/产量恢复 |
| EXP-171 | validated | 受控缺料用空载输入过滤制造并以真实补料/速率恢复 |
| EXP-172 | validated | 物流塔充电上限不是负载；断电试验先证明真实能量缺口 |
| EXP-173 | validated | 飞行预算与燃料功率分维；终态由持续航迹和 600-tick 落地证明 |
| EXP-174 | validated | 需求端耗能归因前先建立供给端有货、无船、零订单条件 |
| EXP-175 | validated | 受控断电需同 tick 证明负载/电网/分类，并按原配置恢复 |
| EXP-176 | validated | 活跃运输跨保存/恢复时从新 session 游戏 tick 重建连续窗并排除离线墙钟 |
| EXP-177 | validated | 当前普通接口不能安全冻结在途 carrier，停滞实机证据不得用直接字段伪造 |
| EXP-178 | validated | 候选包必须复读包内人类文档，二进制与 manifest 版本一致仍不够 |
| EXP-179 | validated | 开局移动恢复规则必须随 MCP 和发行包交付 |
| EXP-180 | observed | GitHub 手动包与 Thunderstore 包同版本同源码，但安装布局必须分开 |
| EXP-181 | validated | 存档模式证据不应与动作授权混为同一门禁 |
| EXP-182 | validated | 隔离验证必须把恢复票据和逐档 Journal 作为同一边界备份/恢复 |
| EXP-183 | validated | 下一科技候选必须从完整 progression catalog 按前置条件筛选 |
| EXP-184 | observed | 多条终端产线共享中间品时必须按并发满速总需求预算 |

## 当前短期任务与关机续玩边界

- DSP 当前在同一受保护 `owned-world-001` 的母星 planet `104` 运行；`1608 配送物流系统` 已于 tick `18747873` 完成，普通保存到 tick `18750214`；fresh 为 revision `13`、owned/saved/healthy、Journal `50/50` durable、0 pending/error、无 blocker/checkpoint。继续使用同一世界，不开新档。
- `v0.3.3` 已由 clean release commit `f0cd111` 正式发布；0.3.x release 分支不混入 v0.4 Overseer，后续 0.4 发布仍以 owner 审核后的最新 clean `main` 为唯一来源。
- 当前主线：v0.4 Overseer 功能门、IFX-024 补充回归、clean 双包、实装恢复、安装态 MCP/Overseer 和 Windows CI 均完成，候选等待 owner review；审核前不 tag/release。`1608` 已完成，第十写保存及严格审计通过。只读预算后下一条产线优先 recipe `123` 配送运输机：现有自动仓有铁块 6000、动力引擎 4435、电路板 400、微晶元件 116，可复用处理器制造台 `853`，但必须先证明三种输入自动端点与长期供给，不能用玩家搬运或库存存量冒充可持续自动化；accepted-write 计数从 0 重新开始。recipe `122` 仍缺电浆激发器及其上游证据，排在其后。高能石墨 `114 -> 716` 的有限安全路线仍 blocked；后续自动化需要新的可证明施工策略或接口能力。游戏写仍仅由 Luna Max 子 Agent 执行。真实 600-tick carrier stall 继续作为普通玩法无法安全制造的已知 live 覆盖限制。
