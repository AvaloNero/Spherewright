# 新煤入炉、配方17、保存及首批石墨

日期：2026-09-30（Asia/Singapore）。继续同一 owned-world-001 / planet104 / 原生29104 主档。前阶段 f01d392 已推送，Windows Core CI 36660080593 成功。此处没有改变 Plugin/MCP、载入其它世界、直接写库存或拆除旧对象。受保护原始回执保留在本机；仓库仅记录脱敏索引，不含真实存档名、planToken 或恢复凭据。

## 原生预检与已接受动作

主会话只读 run 3a0106ad5d7e4d2a954f1799d8feafcc/0005–0006 首先确认：开放带端5172→空炉5187的普通2011分拣器允许提交，过滤煤1006、预算2011×1、exact_slots 源虚拟槽-1/炉槽10；炉配方17也允许提交，正常原生配方预算为煤1006×2输入/高能石墨1109×1输出。两个旧预检 token 都没有用于实际提交。

Luna 在 fresh session/player/端点读回后重新预检，受保护 action run aa6bb49d02344c80bb99100118dfd7d9 记录三个互异、唯一的已接受动作：

| 动作 | 原回执终态 | 直接证据 |
| --- | --- | --- |
| 分拣器 build | ea5bb7ba-4b4f-4782-934e-7e8b10a328e5，tick79950017 completed/succeeded | 唯一新实体5188；原生 exact_slots、煤过滤1006、2011×1；背包普通分拣器3→2。5172 slot4↔5188 slot1，5188 slot0↔炉5187 slot10。施工 commit→terminal 约46.8秒。 |
| 炉配方 configure-building | 7c46df64-a328-485d-82bb-cedfd1e13b5b，tick79952792 completed/succeeded | 目标5187，recipeId17、预算煤2输入/石墨1输出；配置未扣背包物品。prepare→terminal 约0.24秒。 |
| 正常保存 | cb7300bc-fa83-44ce-a7f5-cada530ab8c3，tick79954353 completed/succeeded | session revision10→11，owned/saved/healthy、protected resume available，Journal durableThroughSequence95、无 pending/error；prepare→terminal 约0.38秒。 |

在保存前，一次本地 guard 将 session.localPlanetId 误作 session.planetId，字段读取即失败，没有发出 prepare_save 或 commit_save。执行者核原已接受前缀不重放，改用真实字段后 fresh 读取并完成唯一保存。这不是游戏保存失败，也不改变 sorter/configure 两个已完成 action。

## 主会话独立读回

主会话先从同一受保护 run 的原始 terminal 回执核三种 actionKind、ID、唯一目标及成功 tick，再在只读 run b2b8315e952641948a8ca386a8d5c4ea 于 tick79956902 复读相关实体、玩家、Journal、预建筑：

- 分拣器5188 item2011、filter1006；带5172、分拣器5188与炉5187的连接在实际两侧互返。玩家普通分拣器2。sorterEndpoints 详情单独报告 unavailable/native_sorter_slots_unavailable_or_over_limit；本阶段不把它写作可用证据，采用预检槽位、成功终态及实体 connections 互证。
- 炉5187仍在电网3，powerServeRatio1、recipeId17、isWorking=true；当时 input 煤1006=4、output 高能石墨1109=33。新炉初建与配置即时读回的输出均为0，这个后续正缓冲证明正常配方已实际运行，但不是多窗口产率。
- owned/healthy，主档最后保存79954353 / R11，J95 durable且无 pending，0 prebuild。本窗累计 accepted7（前阶段恢复/两建/保存4，本阶段 sorter/配方/保存3），尚未到第十写冻结。

本批没有新建出炉带、仓或石墨消费者。输出缓冲最多是有限容量，后续可能堵住；不能因为33件瞬时库存就宣称持续石墨、磁环、燃料棒或黄/紫矩阵恢复。下一设计必须从炉5187实际出料槽、可用材料、电力、旧石墨需求及周围完整占位出发，给出 fresh 原生合法路径，并在连接后通过独立生产窗口及普通保存/恢复验证。旧煤不能直接并进过滤石墨的带3373。
