# 联合供需连续观察缺口（R321/R322）

日期：2026-10-08（Asia/Singapore）。本事件记录一次只读长窗观察中断及独立核销，不把间断样本拼成连续证据。

R321原记录为`826fac57e8e5485190455ee6974b0185:1–111`，记录SHA-256 `6F3FE7C601BD4CF63720BC0E02E5E14A78B07006B0873194E2B96AD8C1903FC7`；caller SHA-256 `908256DB76FF749E9101C60833D8C9036D6B8399CD779ABB0DC49560667F113F`。本轮为只读，0 accepted、0 commit intent，进行了6个各600-tick采样窗。首两窗为`101709857–101710456`和`101710481–101711080`，相隔24 ticks；最长连续段为2634 ticks，未达到36,000-tick门。

各采样点电力均读到full-serve。首轮完整静态/库存比对在计数读回之后执行，带来额外开销；这使采样证据不足，不能据此判定产线供需不足，也不能认定生产源通过。不得跨间隙累计连续credit。

root主动停止唯一观察进程（exit code 1），保留已使用的原once，不重放。记录未取得最终Game closure或seal；最后观察tick `101713130`仍报告R41与原Save `101620229`，窗口开场J102，但这不是fresh终态J核验。固定窗口仍为20写，external `7`、lifetime `347`；root核验无intent/unknown，后续写入保持冻结。

R322零Game独立审计`cdd7aeda694b402da8a585f9aae241dd:1`，SHA-256 `BA0B604B7B8CCDF49206C9E2AAD8E81FFD7E8AB9F01F2028DA6D3E79A5FF2D37`，确认continuous credit为`0`。下一次有界观察应沿用完整采集范围与36,000-tick门，把纯比对延后到观察结束、逐次保存完整原始DTO，并在首个间隙处停止；需要新的root声明，尚未执行或通过。
