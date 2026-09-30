# 最小流程优化：离线证据与边界

日期：2026-09-30（Asia/Singapore）。起始源码 `5fa0a2c`，工作树干净；不新增施工、加载、部署或发行。只保留 root 与既有 gpt-6-luna/max，后者已停止：新增 accepted=0、无在途/未核销动作、无活动命令；原写窗口仍为4/10。

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

后续批次仅补同一优化范围内的快照复用和固定采样，结果写在本文件，不向日记、Roadmap、账本复制同一事实。
