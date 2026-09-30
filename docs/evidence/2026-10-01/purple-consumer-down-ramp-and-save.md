# 紫糖消费者下坡带、分拣器与正常保存

日期：2026-10-01（Asia/Singapore）。同一 owned session、planet 104、DSP 0.10.35.29104。本事件只记录已核实施工与保存，不代表紫糖已送达或科研生产已恢复。

## 消费端路径

受保护施工 run `e6c4a685ae2f47bf8acdf9ec72754639`：ord5 prepare、ord7 accepted、ord43 terminal completed/succeeded（tick 81504289）；ord44–51 新建的八段 `2001` 带为 `5217→5216→5215→5214→5213→5212→5211→5210`，坡道从 L1 到 L0。实体及连接由 root 独立核验。

后续分拣器 run `bee62d67a4a240c8b66483ee526076cc`：ord5 prepare、ord7 accepted、ord17 completed（tick 81513618）；新实体 `5218` 配置为 sorter item `2011`、filter `6004`。实际端口为 `5210` slot 4 OUTPUT → `5218` slot 1 INPUT，以及 `5218` slot 0 OUTPUT → `5198` slot 4 INPUT；既有 `5198→5197` 连接保留。合并既有下游后，物理路径为 `5217→…→5210→5218→5198→5197→5199→Lab84`。

玩家库存独立读回 run `b360595b15424c13aa9162a4769192b6` ord2：分拣器耗用 `2011` 一件（1→0；零库存行在回执中省略）；其余 15 种物品的数量与增量不变。行走/速度为 0、能量 800 MJ、无人机数 0。曾出现的 virtual slot 与实际槽位不一致、零库存行省略两项调用方摘要误报，均由同一成功 action 的原始读回核正；没有重放或原生失败。

## 保存与边界

正常保存 run `dff2724a6d4f42fa8e960e521858c76b`：ord4 prepare、ord6 accepted、ord7 completed（save tick 81554959）；ord8 fresh session tick 81554973 / R12，healthy；ord9 Journal 为 J96 durable、无 pending/error。当前外部窗口 accepted 6/10，所有已接受动作均有终态，无在途或未核销。

新路径已由正常保存覆盖，新的受保护恢复凭据可用，但尚未做真实重启恢复检查。上游供料仍未接通/验证；没有证据证明紫糖到达 Lab84、科研推进或持续吞吐。八段带与分拣器成功不等于整条供给链验收通过。
