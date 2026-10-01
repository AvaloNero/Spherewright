# 石料源短缺诊断与空矿机回收

日期：2026-10-01。本文记录 root 已核实的生产诊断、空矿机回收与正常保存，以及组15矿机原生选址负例；不声称持续采出或真实重启。

## 已核实的生产与供料证据

原生生产结果 run `f089e5e92afa474eac98a1144835ab36` ordinal 7 以顶层 `planets` 返回，不是 `planetId`。其中 planet104 的 `1124` / item `1402` 实际生产和消耗均为0；其原生上游路径追至 factory 实体 `861`。在 `861` 配置的硫酸 recipe24 中，raw-stone `1005` 可用5、单周期需求8；5/8是该 recipe 的供需对照，不是1124直接需要石料8。

只读 run `639b98d0c21a491fb96244e546eaf864` ordinal 1–2 补证：`3050` 有 graphene×2、titanium×2，CNT output为0；其读回 `powerNetwork=3` / ratio 1。factory实体 `861` 读到 oil 12、water 8、stone 5、acid 0；861不是资源节点。

fresh 只读 run `d8fcfbf5cd7640b0b05b8daed27bf187` closing tick **82943275 / R50**，最近正常保存仍为 **82810176**；该阶段没有读取 Journal。实体读回显示：`86` 的 nodeIds 与 buffers 均空，仅 output0→`87` input1；`95` stone为0、bricks item `1108` 为2198；`128` input←`92`、output→`127`，slot4→`859`，该处的带局部 raw-stone `1005×1` 不属于 sorter held；`859` filter为native ANY（null/0）、held为空。缓存和标称容量不是持续产量证据。

空矿机回收前，root 复用已有完整快照、未增加游戏调用的拓扑核验 `6d8ea12383d94146802a59d676c94b08`：51个对象、50组互返边，核实路径 `86→87→…→128→859→95→3754→…→862→866→861`。这是当时拓扑证据，不代表回收后的完整工厂 census，也不替代库存或生产速率读数。

## 空矿机回收与正常保存

施工 run `9161aa6c7e4f4d1086a7cda1651358bf` 内仅有两个 unique accepted 动作，均 succeeded 且无 unknown/在途：dismantle action `e3ea6f27-12fd-4708-8cd4-2847345d3eec` terminal tick **82996601**；随后 save action `531d4045-3e7a-433d-9144-f24bb25210ca` terminal tick **82996662**。回收读回确认 `86` 为 `INVALID_ENTITY`，玩家 `2301` 从0增至1；其他背包count/inc及位置保持，`87` 仅删除指向 `86` 的入边，其他已核静态字段和连接保持。

正常保存 **82996662** 后 closing session 为 **82996677 / R53**，同一 owned identity、原生版本29104、healthy，`resumeAvailable=true`；J96完整前缀 durable、无 pending/error。最近另一次单资源读数是 node196 在 tick **83000303**，remaining46310、miner0；它不是新 session/完整工厂快照。本次保存读回不等于实际重启验证。proof `d71b086225ea43c89919a7d396bb9ab0`，SHA-256 `B41BDA21414812FAC632619256F73A0046E7CE30513580BCA482098A57A53803`，由root复核21条原 response，未增加Game调用。

本阶段没有新的完整工厂 census，不推算回收后的built总数。当前 external accepted **2/10 OPEN**；R53、游戏tick和Journal均未因窗口交接而重置。

## 矿机选址负例与当前阻塞

此前紫糖十写审计提交 `f09787a166984e21f5acd62f8d1c8eafad36e8a9` 及其 Windows Core CI **36811190833** 已确认成功。随后 root 通过交接索引 `9fdf20bdd79b433fb32bb75dd1f462d2` 开启新 external 窗口 **0/10 OPEN**；已核两个动作后为 **2/10 OPEN**，游戏 revision、tick、Journal 均未重置。

第一项矿机预检 run `9161aa6c7e4f4d1086a7cda1651358bf` ordinal 23，`BUILD_LOCATION_INVALID`，native拒绝末尾报告与实体101 overlap；其 fresh node188 读数 tick82996698、remaining24824/miner0。第二项 run `3c6c3d78b01d4576884a27d61fc0b560` ordinal 3 `prepare_build`，同为 `BUILD_LOCATION_INVALID`，末尾拒绝报告与实体4920 overlap；其 fresh node196 读数 tick83000303、remaining46310/miner0。两次均未产生 plan/commit，已停止并交root重新设计组15位置。native覆盖搜索保留last rejection；这两条消息不证明所有候选位置都只被这两个实体阻挡。

目录 run `8bad90d2158c44ec883012865849be2c` ordinal 1 成功返回含 `planetId` / `revision` 的BuildCatalog；公开DTO没有 `sessionId`。先前摘要读取失败是caller解析问题，不是目录能力缺失或新Native拒绝；不重复读取。

当前唯一施工阻塞是组15矿机位置需root重新设计；配方24的石料缺口仍待上游核验和后续有界验证。本文不声称持续采出、硫酸或紫糖生产恢复、回收后完整工厂 census、真实重启成功或已通过0.4发行验收。
