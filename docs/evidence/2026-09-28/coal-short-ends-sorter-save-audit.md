# 煤矿短带分拣器与正常保存审计

日期：2026-09-28（Asia/Singapore）

源码基线为 `ad41961`，安装同批为 `034300e`、DSP `0.10.35.29088`；本批没有新的 DLL 或发行。

## 已完成范围

- `action-a091349893cf4be089c60c6cd38ed909-` 的 `0023` 是唯一成功的普通 `2011` 建造 terminal：tick `77413972` 形成 `5170`，玩家 `2011` 为 `5→4`。`0024…0032` 读回该对象及受影响端点，`0033` 为零 prebuild。
- `0034` 时只有 construction drone 正在回航（`working=1`）；pending build/repair 均为零，玩家仍 Walk、低速、空手搓且能量充足。调用方因此未保存；这不是 native 失败、unknown 或可重放的建造。复验既有 [EXP-242](../../experience-ledger.md#exp-242--施工终态无人机空闲与下一对象准入分别核验)：施工终态与无人机空闲分开核验，只在归位后继续未提交后缀。
- 保存后缀 `action-f59301fcf61f4ed8901435302d55a9ba-` 只续未提交的 normal save。`0012` 的 terminal 为成功、无物品 delta，tick `77449842`；收尾为 `R90 / save77449842 / J91 / accepted8`。保护证明 `0016-coal-sorter5170-saved.json` 的 SHA-256 是 `9BABF7B677CA6AF49138CE7A5925E6E8D3F338D386DE09581219888FDF532771`。

## 一次阶段审计

- `action-ec56c89f11a74708a7c5f10ff5e65f8d-0060-coal-short-ends-sorter-save-audit.json` 的 SHA-256 是 `2DEF2DAFF76212A479DD8B8B0E9A81EE4DD61F0ECB7475973E2F0E96C7B29AC5`。
- 52 页实体快照（`0002…0053`）固定在 tick `77458286`；独立 prebuild 检查为 `0`，收尾 session 是较后的 tick `77459534`，两者不混作同 tick。
- 密封的 `5160` 旧实体静态配置与 `10062` 条旧有向边逐项保持；仅允许 `5161…5170`。16 条声明的新有向边使总数为 `10078`；`5170` 是 `2011/inserter`，`5162` 仍精确覆盖九个节点、network `3`、serve ratio `1`，Journal 保持 durable `J91`。

## 容量与未证明项

- `action-d76aac7432214900b4e1007caa76f680-` 的单选导出只证明 `5170` 当前 basic span 为 `3`，额定 `30` coal/min；未来 fast 升级额定 `60` coal/min，二者均非实测吞吐。
- 现设备理论负荷为 `29.0625` graphite/min，即 `58.125` coal/min；`60` 仅比它高 `1.875`，仍低于名义 `6+7.5` 所需的 `61.5`。离线配方核算已确认 `r17` 单炉额定 `30` graphite/min，先施工一炉可覆盖现设备理论负荷、余 `0.9375`；它不满足名义 `30.75`，更不是实测。6/min（±10%）的两段各 `36000` tick 验收目标不降低，实际不足再扩。
- `action-1c5656630a3d462a8577136a68b20cd8-` 中仅 `5164→5168` 的第二并行候选因 `TooSkew/no_finite_projection` 在零 native checks、零 commit 时被拒绝；这不推广为所有并行或地表路线不可行。

本阶段不证明煤已送达、石墨模块已施工、实际吞吐、持续产出、煤供给充足或重启恢复。下一步只在材料、普通升级和模块方案分别 fresh 核准后进行。
