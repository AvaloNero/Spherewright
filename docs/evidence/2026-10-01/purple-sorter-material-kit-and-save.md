# 紫糖分拣器备料与正常保存

日期：2026-10-01（Asia/Singapore）。同一 owned session、planet 104、DSP 0.10.35.29104。本事件记录原生取料、手搓和保存，不代表六个分拣器已安装或紫糖供给链已通。

## 材料闭环

受保护 run `f7ba99c53f97448181de6ec8699c79eb`。ord3/4 为转移前玩家与仓储读回；仓储实体按 `componentKind` 确认为 `1511`。转移 action `addfd484-f7e6-4a2d-b350-9c8ff6057829`：ord5 prepare、ord7 accepted、ord8 terminal（tick 81634317）。该 action 的同步两端读回严格守恒：仓内普通铁块 `3000→2994`、玩家 `0→6`，静态配置和连接不变（包括 slot 2←1512）。稍后 ord10 的新鲜仓储读回为 `2995`，比该 action 的 post-transfer 值多 1；来源尚未归因。因此不把后续动态读数改写为原动作失败/未知，也不声称当前仓的净变化恰为 −6。

同 run ord12 prepare 后，recipe 85 手搓 action `10a2e594-3f76-4545-b639-ea737b36e4d1` 于 ord14 accepted、ord20 completed（tick 81634612）；ord21 玩家读回确认铁块 `6→0`、电路板 `999→993`、分拣器 `2011: 0→6`、带 `2001` 为 352，手搓队列空，其余 15 种物品的数量和增量不变。组件类型识别纠正了较早的只读仓储定位误差；没有额外施工、原生拒绝或重放。

模板 `Invoke-SpherewrightMaterialHandcraftAndSave` 的内部计时为 10.62 秒，不包括委派、验收或 CI。`timeSpend60` 指游戏 tick 参数；六件预计约 360 game ticks，不是 360 秒。

## 保存与边界

正常保存 action `2409ff1d-c28a-43e3-af32-ab356dd43709`：ord23 prepare、ord25 accepted、ord26 terminal completed（tick 81634733）；ord27 fresh session tick 81634743/R16/healthy/resumeAvailable；ord28 J96 durable、pending=false、无 error。外部 accepted 从 6 增至 9/10，全部已接受动作均有终态，无在途或未核销，尚未触发十写冻结。

本次保存覆盖材料闭环，但真实重启尚未验证。下游既有路径 `5217→…→5218→5198→5197→5199→Lab84` 保持；新得的六个 `2011` 只是分拣器备料，尚未安装。上游仍未接通；没有紫糖到货、科研推进或持续吞吐的证据。
