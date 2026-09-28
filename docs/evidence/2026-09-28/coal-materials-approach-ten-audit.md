# 煤区材料接近与十写审计

日期：2026-09-28（Asia/Singapore）

记录源码基线：`408c6cc`；安装基线：`034300e` / `DSP 0.10.35.29088`。本批没有新的 DLL 或发行产物。

## 本窗收尾

- `action-533432ff32ea4d7ba0375fb9f9b504c9-` 的唯一 Move terminal（`0018`）于 tick `77510253` 成功；唯一 normal save terminal（`0024`）于 `77510362` 成功。末尾为 `R93 / save77510362 / J91 / accepted10`，完整背包守卫保持，且这两个动作均无物品 delta。
- 该 Move 只采用已核的历史 Walk 例外：fresh prepare 的 surface preview 是 unavailable/span-outside，shoreRisk 为 unknown、样本为零；这不是水面安全、路线、煤交付或吞吐结论。本次未新建或拆除工厂对象；后续审计确认旧静态配置与连接保持。
- 本窗十个已接受动作由既有受保护原件分组覆盖：电塔/矿机/save `3`（[矿机阶段](coal-node319-power-miner-save-audit.md)）、两段短带/save `3`（[短带阶段](coal-short-ends-save-audit.md)）、分拣器/save `2`（[分拣器阶段](coal-short-ends-sorter-save-audit.md)），以及本次 Move/save `2`。本次不重放或重采历史 terminal。

## 一次严格审计

- 证明为 `action-918c67bd7cbd4cdfbd829cc73aa7f216-0060-coal-materials-approach-ten-audit.json`，SHA-256 `4566C8D48B9F3BE6C87526B7ED936E57A017B4BE483626824863BC734DFF109B`。
- 52 页实体快照（`0002…0053`）固定在 tick `77515149`；独立 prebuild 检查为 `0`，收尾 session 是较后 tick `77516237`，不混作同 tick。
- 密封的 `5160` 个旧实体静态配置与 `10062` 条旧边逐项保持；仅允许 `5161…5170` 和既有 16 条声明新边，总数 `5170 / 10078`。矿机 `5162` 仍精确覆盖九个节点、network `3`、serve ratio `1`；Journal 为 durable `J91`、无 pending/error。

accepted10 现为关闭边界而非重置。本阶段没有新炉、升级、材料转移、煤/石墨交付、实际吞吐、持续生产或重启恢复证明；后续材料、`5170` 升级和单炉模块均须在封窗后重新 fresh 核准。
