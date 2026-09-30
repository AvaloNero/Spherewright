# 石墨入口两条分拣器与正常保存

2026-09-30（Asia/Singapore）。同一 `owned-world-001`、母星104、DSP `0.10.35.29104`。上一十写审计已由 `e3fdbce` 推送并通过 CI；外部 accepted 窗口随后明确从0开始。本记录仅含脱敏索引，原始回执仍在本机受保护的 `8418a1e5bef240b5a436314ec877842a` run 中，不提交凭据、计划或真实存档名。

1. 为覆盖上轮未保存的带5193/5194，普通保存 action `6a5f5827-387f-40f4-ae1c-496ac99d0fe8` 在 ordinal10 terminal/succeeded，保存 tick80006447，J95 durable。
2. fresh 原生预检后，5193→旧带3365 的石墨1109过滤分拣器正常建成5195。唯一 action `3ec210d7-ed63-475a-8800-73cdff814d44` 在 ordinal46 terminal/succeeded、tick80008003；带两端 `-1→-1` 互返，分拣器库存2→1。
3. fresh 原生预检后，新炉5187→带5189 的石墨1109过滤分拣器正常建成5196。唯一 action `5daf7777-fc51-4da4-8f4c-255058888e8a` 在 ordinal85 terminal/succeeded、tick80010505；槽位 `7→-1` 互返，分拣器库存1→0。
4. 原批准阶段末普通保存 action `610a668d-a6be-46bf-8d82-2e08880fa2a0` 在 ordinal100 terminal/succeeded，保存 tick80012591。主会话独立从受保护 ordinal10/46/85/100 原终态与 ordinal101–103 复核：tick80012594/R22、owned/saved/healthy、protected resume available、J95 durable/pending=false、tick80012597 查询预建筑数0。四个 action 均已明确终态，未见重放；本窗 accepted **4/10**，不冻结。

末次读回中，炉5187配方17的煤输入缓冲2、石墨输出缓冲84；带5189单次见石墨1，带5193单次见石墨3，旧带3365该次见0。它们来自不同 tick 的瞬时观察，只证明部分物流已经启动，不证明石墨已进入旧带、磁环持续获料或 0.4 的持续产出门。尚未进行此次保存后的正常退出/protected resume，也未做连续生产实验。后续先从旧消费者反向核真实石墨需求与旧带接点，再做预先固定窗口的有限观察；本次流程优化不授权继续游戏写入。
