# 紫糖源仓出口分拣器与正常保存

日期：2026-10-01。本事件记录源仓至主干的单只分拣器施工与保存，不代表全线持续供料或科研完成。

## 出口连接施工

施工 run `e51af7173aa24dcd8968d396867a28b5`：prepare ordinal 7、commit ordinal 9、terminal ordinal 14；new entity ordinal 15、source 16、destination 17、玩家库存 18、state 19。唯一 action `c08306b4-3c73-4349-872c-fefd77134e8b` 于 tick **82725565** 完成。

新增 `5315`，item `2011`、filter `6004`、network 3 / ratio 1。实际连接为 storage `3051` slot1 → `5315` input slot1，及 `5315` output slot0 → `5202` slot4。旧 `3051` slot4 ← `5113` output slot0、`5202` output slot0 → `5201` input slot1，以及相关静态配置保持。玩家 `2011` 从3减至2，带 `2001` 保持260，其他库存数量与增量不变。

root proof `5749ab3a068245e7a40aa446f412bb2b` ordinal 1，SHA-256 `E04CCD6DE6FDAD1B589CDFCBEE90088A830F9FBD00896D4EC9304EB70B67DA8C`，独立核验施工回执、槽位、静态保留与物料。

## 正常保存闭环

保存 run `58521fabf043471da2ab6bd96c24b327`：Journal 前读 ordinal 3、commit ordinal 6、terminal ordinal 7、fresh state ordinal 8、Journal 后读 ordinal 9。action `f3771a2c-e6ed-4222-990f-a49feea0f65a` 保存于 tick **82726418**；后续观察 **82726431 / R49**。J96精确96条记录 durable、无 pending/error；`resumeAvailable=true` 不等于真实重启验证。此保存覆盖分拣器 `5315` 与本次连接。

外部写窗口 accepted **9/10 OPEN**，无在途。原始 receipt 已由 root 独立核验；真实重启未进行。

## 采样所见与因果边界

后续只读 run `dfe04f5f181347e682357b9ba740b501` ordinal 1–7 记录：`3051` 的 `6004` 读数为1776，先前快照曾为1824，但这个跨快照差值不能唯一归因；`5315` 持有 `6004×1` 且工作中，`5202` 读到×1、`5197` 读到×2；Lab84处于工作状态，`6004` 的 `research_matrix_points` 为37824。科研点按3600点/物品折算；37824是点数，不能写作37824件物品，也不能单凭该快照证明源仓至实验室的连续物流或点数变化归因。该观察同时记录科技2104。

当前待核的是 `4743` 上游的有界样本与持续生产/消耗归因。仓库出口和下游连接已建立，但紫糖持续送达、研究进展归因、稳态产量和保存后真实重启仍未证明。
