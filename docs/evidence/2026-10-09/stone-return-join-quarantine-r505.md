# R505 石料返回接缝的未知终态与原生证明拒绝

更新：2026-10-09（Asia/Singapore）。当前执行代码仍为 `d6517834d4d8c55f48f841525008b4cde0a5f6e5`。本页记录一个 accepted Build 的原始未知终态、独立物理证据、进程内核销拒绝及对应代码修复；不把建成提升为原动作成功。

## 原动作与当前边界

[R492 固定恢复](fixed-quarantine-recovery-r492.md)已独立核清并封存旧7/20窗口、lifetime399。新固定20窗口保留普通保存槽位，R495先取得两段返回带和过滤分拣器的原生正例；R496在业务prepare前因只读调用参数缺失停止，零accepted。root核实后仅修正调用参数；R498使用fresh prepare接受一次两段空带接入5321的Build，终态为 `outcome_unknown`，当场停止，分拣器和保存均未尝试。

- 原action：`30d8ca72-480b-4986-af81-c3ae694bbad1`；原幂等键：`0a065715-ec84-4d61-af09-9ae9657f2d2c`。Native terminal tick为 `104817993`，`terminal=true`、`succeeded=false`、目标ID列表为空，2001库存35→33。原意图、accepted和未知终态完整保留，没有重放或换键。
- 当前为同一和平、非沙盒、1×的owned P104/R2；`writeHealth=quarantined`、`writesAllowed=false`，隔离绑定该原action。新窗口1/20、lifetime400，没有归零或转移旧窗未用额度。最近正常主档保存仍为 `103307910` / Journal durable102，未覆盖本次Build。
- R502共72次只读请求，64页完整工厂快照6313 built / 0 prebuild；R503独立核原始14条执行记录及74条观察记录。新增仅5323、5322，无移除实体或非互惠边；精确有向连接为5323→5322→5321→5320→5319→5318→5317→5316。完整Native路径153格、8段带、空载、无外部cargo path输入/输出；另四条完整货物路径的成员、长度、输入/输出路径ID及旧实体静态几何保持。玩家静止，材料count/inc/held除两段带外精确一致，施工无人机和手搓队列为空，Journal完整历史/版本链保持102，当前本地网络满供。
- 旧6311实体有三项明确静态差异，原差异报告仍保留 `allowed=false`：5321增加唯一输入5322并发生Native渲染旋转变化；矿机1213的resourceNodeIds由[37]变为空。R504 fresh Native对37返回 `INVALID_ENTITY` 且明确节点已不存在；正常采掘耗尽是有证据支持的推断，不据此宣称铁来源持续通过。

## 进程内证明没有通过

R504只运行4个只读/preview请求、1次精确原action的 `prepare_quarantine_reconciliation`，零commit、零accepted。Native返回 `ACTION_OUTCOME_UNKNOWN`：保留动作缺少完整建造计划或隔离时库存快照。两次session读回均仍为原隔离，外部计数1/400。

R505直接核原5条记录的ACL、顺序、seal、摘要与原始失败响应，并复用R503完整快照。该入口当前无法证明这笔保留动作，不能重试来补造丢失的记录，不能以物理正确清除隔离。此前用户确认的R492固定候选不授权本次新隔离；普通健康冷部署授权也排除quarantine。当前未关闭DSP、未加载或安装新代码，下一步仅准备可审核的固定受保护恢复方案，恢复本身需要新的后续确认。

## 代码修复及验证层级

`NormalGameActionCoordinator.BeltDestinationReuse.cs`原后验证在Native已把最后一段新negative prebuild接入目标带头后，再调用只接受空输入的路径捕获；该条件与合法Native后态矛盾。修复只在post-create阶段先核目标全部16槽的唯一输入变化、prebuild身份与互惠边，再允许该精确negative ID用于旧路径绑定复读。prepare仍拒绝已有输入、货物或非独立路径；全路径绑定、旧输出、全部新prebuild的有向拓扑及最终建成证明均保留。现场原动作只报告异常类型，无调用栈，实际抛出调用点仍未由运行日志证明。

`StructuredActions.cs`把完整不可变prepared步骤在Native创建前留存，并在finally记录实际存活的合格prebuild ID；`NormalGameActionCoordinator.cs`在start异常触发隔离时立即留存库存。旧逻辑在post-create异常之前尚未写入完整ExpectedBuildEntities，异常分支的DTO库存展示又只是实时fallback，不等于保留的AfterInventory。修复不回填当前旧进程记录，不改变unknown规则；未来核销仍要求精确材料差量、全部prebuild消失、唯一新实体归属、完整有向拓扑和同原action证明。

新增回归覆盖合法target pending input、旧输出保全，以及错误槽、错误带头、错误/未绑定ID、正数、无效ID等拒绝边界。直接相关六组Core策略测试202/202通过；本机完整solution Release构建（含Plugin和当前DLL引用）通过，0警告、0错误。新代码未冷部署或实机验证，原隔离没有解除。

## 受保护原件索引

| 原件 | 引用 | SHA-256 |
|---|---|---|
| R495两接口Native正例 | `a9ea4b66f77b4c46a18132ef8be0beb8:1-10` | `FFF80C9826421EFC952EE13C155BC80C56E68297813F3B3E60C4E1E71E6950EB` |
| R496零accepted停止 | `6daf62ba7a45492aa2fedc657c3532fe:1-5` | `9C7D54113367D107B917B9C25222D0256A7418D4BEA8FF4A270756236D7B2C26` |
| R498原意图/accepted/未知终态 | `5c76f88265344d3fbb3b22264e9d20de:1-14` | `72DB9DC09F024FBDBEEF09B7E074E3D7BCBC04AF59DE2EC727F8F20E4D9EF0BF` |
| R502完整只读观察 | `9f3690f5697646b8bafa0d2f2fc747a9:1-74` | `AD626D410B95C41CAA99B91D2E9200457A68B18E8332E0D2F63B2999425F443C` |
| R503独立事实审计 | `ab8e5fd9cd834cf9b3b4c0f34dc9d073:1` | `D8095D2C3B678AEE6BA34C785FF3A86AA5826BB5C0C1EC55B72F798E8904EAC2` |
| R504原生证明拒绝/节点37缺失 | `62c463de914b442587b2104a16879f0c:1-5` | `2CC622288FFE040340D9395970749348CA68AA99C2DF1C60D1820BF6BC69325F` |
| R505独立拒绝审计 | `2f3b95f840ee41b092ec4afc27cd498c:1` | `150490316377C8FA0B8CED242B41A62B8B7DC0BCA1D00F1EA24C47BFCB796AF1` |

以上不改变R438/R441的来源失败和continuous credit0：新增返回带仍未接入新矿源，`wholeSupplyPassed=false`。29物料、Warper/Rod至少1/min及连续≥36,000 ticks、双自动补给、整合保存恢复、最终同提交双候选包仍待完成；不重开已通过门或封闭的H侧候选。
