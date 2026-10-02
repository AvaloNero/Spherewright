# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本文为覆盖式快照；本次受限施工封窗的原始回执与独立审计见[Warper 自动供料与建造准备](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前运行与写窗

- 本窗从 0/10、lifetime accepted 33 开始；现为 **10/10 冻结**、lifetime 43。两个 stage runs 合计 10 个唯一 completed action，0 replay、unknown、unresolved 或 in-flight。Root 独立审计 PASS；须等本文档提交的远端 CI 通过及 root 明确 handoff 后才可开新窗。
- 最近 normal save tick `84915771` / R56，durable Journal J97。完整 capture tick `84916675`，closing observed tick `84917231`；Journal identity 与 97 条原记录完全连续。
- 全图 5331 实体、0 prebuild。原 5325 个实体静态配置和 10386 条互逆边全部保持；新增 6 个实体无物流连接引用，其中 collider recipe 104、assembler recipe 78。net3 为 209 nodes / 514 consumers（较前态 +3 / +2），发电能力与容量不变，consumer ratio=1。
- 本窗物料净额：Fe/item 1101 −360、belt/item 2001 +360、collider/item 2310 −1、assembler/item 2303 −1、Tesla/item 2201 −3、stock/item 2101 −1；其余 inventory count/inc 不变。

## Gate 与版本边界

- Gate 1 的 native 启动正例已通过；持续 ≥1/min、保存后真实 restart 尚未证明，Gate 3 的 36000-tick 联合目标也未证明。
- 当前只完成 Gate 2 的有限未来接口施工 qualification；不代表完整 executable 方案、自动供料、全链供电/覆盖或持续 throughput 已通过。
- 当前源码 pin `5f58779016b4c63471ba2b8347b7c366dbef9d83`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。本摘要不代表部署或最终验收。
