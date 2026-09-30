# 紫糖北向0→1坡道候选占位拒绝

日期：2026-10-01（Asia/Singapore）。仅记录一个北向0→1候选的首点占位拒绝，不泛化为所有坡道失败，也不证明整条路线已完成或已受native Stage 1检查。

## 首点拒绝

受保护预检run `dc25b434b21249d89246718cc9a6c88f` ord5在planned point1返回 `BUILD_LOCATION_INVALID` / `belt_path_existing_overlap`，冲突对象为已有实体4880。未创建对象、accepted增量为0，plan token已丢弃。这个回执只说明首个planned point受占位阻挡；不能写成整条 `native Stage 1` 通过、完整检查或可施工方案。

root复用不可变全厂快照run `6207d5a744894c11b60f654163aee38b` page50/ord50确认4880/4881为起点北侧的东西向旧带。它仅提供纸面定位，不是本候选的fresh preflight，也不证明动态cargo；本次没有新增工厂采集。

该run closing ord6为tick **81255203/R7/healthy**；最近正常保存仍是 **81208004**，J96 durable只由保存run `8b55f9337ce34fda89d5b2a1e784518b` ord9证明。当前外部窗口仍为**3/10**，无在途或未核销；已建高层前缀 `5206→5208→5207→5209` 保留，不重做。

## 后续边界

这个候选不能以原点盲目重试，也不能据单点拒绝概括所有坡道。下一步接口位置尚未核定；在审查既有布局并取得新的有界方案之前，不把纸面位置当作通过，也不报告整条坡道状态。
