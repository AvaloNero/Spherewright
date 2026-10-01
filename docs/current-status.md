# Spherewright 当前快照

更新：2026-10-02（Asia/Singapore）。本文为覆盖式快照；本阶段 receipts 与独立审计见[Warper 自动供料与建造准备](evidence/2026-10-02/warper-automatic-source-and-build.md)。本阶段基线 `35eb81b968499c77b7b35c3d4fcf3c5b5fd3f90c` 已推送，Windows Core CI `36928001431` 成功。

## 当前运行与写窗

- 新窗口从 0/10、lifetime 23 开始；当前 **10/10 冻结**、lifetime 33。十项均已核销为唯一 resolved/completed，0 replay、unknown 或 in-flight。root 独立审计 PASS；需等本次文档提交远端 CI 通过及 root 明确 handoff 后才可开新窗口。
- 最新 normal save tick 84852841 / R37，durable Journal J97。完整 capture tick 84853236，closing observed tick 84853679；无待保存写入。
- 全图 5325 实体、10386 条边均唯一互逆，无实体新增/删除。唯一配置变化是 object 884 的两个输入格（1204、1104），其余配置与 power topology/capacity 保持；consumer ratio 均为 1，prebuild=0。玩家物品变化与原生 action deltas 一致，Journal 身份及97条记录连续。

## Gate 与下一阻塞

- Gate 1 的 native 启动正例已通过；尚未证明持续 ≥1/min、保存后真实 restart 或 Gate 3 的 36000-tick 联合目标。
- Gate 2 当前只完成材料/入口准备，未完成三级模块与全链施工；不能称完整 executable、自动供料或持续 throughput 已通过。已验证的 prepare-only / NativeOk 位置不等于施工，NeedGround 项仍需现场重排和后续验证。
- 源码基线 pin `35eb81b968499c77b7b35c3d4fcf3c5b5fd3f90c`；installed runtime/cohort source `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（DSP 0.10.35.29104）。本摘要不代表部署或最终验收。
