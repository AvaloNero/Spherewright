# 紫糖北向原生覆盖带段

日期：2026-10-01。本文记录一个已独立核验的施工截面；不是整厂完整清点，也不表示物流、科研或恢复闭环。

## 原生施工与读回

施工 run `ea11ee1e03794faca032f55065df1f11`：prepare ordinal 9、commit ordinal 11、terminal ordinal 40。唯一 action `8cc9aa08-b7a3-4d4e-8346-ae1123d4eef1` 以 `completed` / `succeeded=true` 结束，完成于 game tick **82470611**。同 run 的 accepted 索引为 1，replay、未配对终态及未决 action 均为 0。

原生 `native_grid` / `full_path_stage1` 建成 `5227→5243` 两个端点间的双侧 non-removing cover，共新增16条带，顺序 ID 为 `5268..5283`。所选对象读回显示新增34条有向互返连接；守卫 `5202`、`5247`、`5267`、`5257` 与批准值一致。两个端点的朝向变化由明确的原生策略 `whole_path_native_rotation_v1` 与 `empty_open_path_native_geometry_v1` 解释；这不是可普遍忽略朝向差异的规则。既有端点位置、配置和旧连接保留。

玩家库存中 `2001` 从305减至289；其他物品数量及增量不变。root 的独立审计 `89214c6bdd584d07a4acf53953937f82` ordinal 1（SHA-256 `93A20E8FCB30DEF8B2C3FB47DB1A7289B5C7E78817541CC478CD196B1EFC102D`）核对了16个新对象、两个端点和四个守卫对象；该有限审计不是整厂 census。先前只读原生能力预检 `28f2dc1cb8f148a099c254643052b688` 验证了允许的路径能力；计划未提交。

## 窗口与边界

root 已在前一窗口审计后明确交接，新外部窗口从 accepted `0/10` 开始。本次施工后为 **1/10 OPEN**，无在途动作。最新观察 tick **82470758 / R36** 为 healthy；J96 精确条目 durable、无 pending/error。最近正常保存仍为 **82280505**，早于本次施工，因此不能说本段已保存或恢复验证通过。施工入口总计约57.9秒，其中约54.5秒是终态观察时间，不代表纯游戏等待或模型耗时。

本阶段复用了成熟带段调用器的私有双覆盖参数分支；native plan fixtures、既有 elevated fixtures、AST 与实际 `pwsh -File` smoke 均通过且为0游戏调用。未改公共 Plugin/tool surface，也未重新运行无关 C# 全套。

尚未证明：East1 到消费者的剩余接口、仓库3051供料、紫糖到货、科研推进、持续产量、正常保存覆盖本段或真实重启恢复。后续施工需另行独立核验；本文不记录其尚未完成的结果。
