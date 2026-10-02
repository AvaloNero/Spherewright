# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本页为覆盖式快照；当前石材来源与受限写窗的唯一阶段证据见[Warper 自动供料与建造准备](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前运行与写窗

- 当前窗口 **7/10 冻结**，lifetime accepted 50；包括一次受保护恢复与六个常规 accepted。无 replay、unknown 或在途动作。完整独立审计通过；剩余槽位不补写。须待本文件提交对应 CI 通过且 root 明确 handoff 后才可开新窗。
- 最近 normal save 为 tick `84935126` / R10 / durable J97。它覆盖本窗 7 个 accepted 写入。之后 capture tick `84939047`、closing observed tick `84940013`；保存早于这些观察，不能据此称后续生产缓冲也已保存。
- 完整工厂 5331 实体、0 prebuild。仅 object 95 的 `storageConfiguration` 是获准静态变化；其余实体静态配置与 10386 条互逆边均不变。原三条连接保持。供电拓扑和容量不变，consumer ratio=1。Journal identity 与97条历史记录连续。
- 玩家 Brick 净增158；其他 inventory count/inc 不变。独立审计 proof SHA-256：`AFC767A8D85ED6E9637818EAEB6B35FEBA97317D3E0658A2E56838EF2DB0BA5B`（0 Game calls）。

## 目标与边界

- 中途受保护重启/捕获子门已 PASS；这不表示最新保存后的 restart 已验证。Gate 1 的稳定启动正例已存在，但持续 ≥1/min 尚未证明；Gate 2 仍未整体完成，自动供料、全链供给/覆盖和 Gate 3 连续 36000 ticks 均未证明。
- 三个互不重叠的 600-game-tick 采样窗只证明观察到局部顺序流动。按窗计数（非速率）：Stone 产出 `5/5/5`、消耗 `0/0/8`；酸产出 `0/4/0`、消耗 `0/0/2`；Graph item 1123 产出 `0/0/4`、消耗 `0/0/0`。在 runtime 1123 输出配方31/32中，唯一实际 producer 为869/r31；未证明自动送达883、每窗持续非零或稳定产率。
- 下一唯一阶段：沿既有固定入口完成 Diamond/Lens 配方及输出端资格，再补自动供料；此前已施工的6个实体及石矿/酸链不重做。
- 审核基线 main `e7a1e6ef0b6e972b9c48139e9331b9b3c6d596a2`；本次仅文档。installed runtime/cohort source 仍为 `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP `0.10.35.29104`；228 runtime files 与2个 native hashes匹配）；未部署。
