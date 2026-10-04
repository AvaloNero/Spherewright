# Spherewright 当前快照

更新：2026-10-04（Asia/Singapore）。本阶段原件与边界见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前已核状态

- 本阶段sampler source HEAD为`9be6b5fa5355162dcd2496a6539e07a88f983f22`，包含只读采样caller的16物料范围修复；对应Windows Core CI `37161579848`成功。运行中的DSP仍为已安装`3fe31d1` / native `29104`（64 tools / 1 resource），本修复未部署。
- 当前owned primary仍为 **89919581 / R18 / J100**；随后只读来源点包closing观察 **90141554**。Steam与DSP保持运行，未做restart或Codex退出存活测试；该专项测试已取消。external **5** / lifetime **145**，只读核验无Game写入、unknown或inflight。
- 连续sampler run `0ec62d89ef9d410bb468d5c08c492ea3`在32实体/15物料固定范围内完成82个样本、0次reset，覆盖 **90066166–90102290（36125 ticks）**；3412 sampler reads、3416 native请求，墙钟2075.794秒。root proof `2abcbc714cd94daebf69bdb636f17634:1`，SHA-256 `8F75A06F027BF82F9F55EBB778B5DE724BBD5F91CACBFDA859491464254EF2A8`，离线审计94.7935023秒、0 Game调用。
- 1210非重叠新产量保守下界为 **16**，高于该窗口按最低1件/分钟所需的`ceil(36125/3600)=11`；这是下界而非精确总产量。Warper 5331库存798→814、PC 885为2677→2680。N3采样满供，容量满足本次声明；不能据此证明全源供料配平或排除有限上游库存。
- 窗口端点读数：870石墨0→0、酸100→100；863酸为600→600、3348氢为0→0、3074氢/氘为0→0。上游buffer会周期变化，端点零值不等于没有流动。Fe 1511为3000→2999，区间最小2998、最大3000，也不是零变化。
- 来源点包只读核验已闭合：22 native响应、14个唯一实体，closing仍为R18/save89919581/J100。点读只纠正163/784当时的库存标签（氢而非油）；原静态snapshot中item-aware的1114过滤候选成立，但不证明实际油流、持续来源或有限库存排除。瞬时采集器状态与逐项读数见阶段证据。

## 尚未闭合的边界

`sourceSupply`、`finiteBufferExclusion`与Governor验收仍为false。下一项按runtime库存物品与分拣过滤方向核验真实精炼油到861/3084的实际流量、持续来源及上游有限缓存；当前静态过滤候选无item矛盾，但不等于实流。现行调用端允许最多16物料；本次连续采样使用15物料并将实体范围固定为32。

本次采样不是来源归因、持续供料配平或protected restart通过。此前保存/恢复事实保持不变；本轮不施工、不重启、不部署或发布。
