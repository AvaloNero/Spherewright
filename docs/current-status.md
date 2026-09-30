# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新观察 | run `062cd8fce1024bbf805f67a0381da62a` ord1：tick80548608 / R39；同一owned planet104、healthy | 新建短带尚未保存，不代表重启恢复 |
| 保存 | lastSaved tick80526460；ord3 Journal J96 durable、无pending/error | 5197/5198仍未被保存覆盖 |
| 外部写窗口 | accepted **6/10**，无在途或未决 | 新短带施工已有唯一成功终态；caller exit1不改变其accepted结果，不得重放 |
| Git / 发布 | 文档提交前基线 `fa636b4` 的Windows Core CI已绿 | 本批提交按准确SHA另验CI；不代表安装或发布 |

run `bacc806ec604461491e462b5ad5e399f` 已建5198远端→5197近端两段belt，各自仅一条互返连接、头尾空；inventory item2001为372→370，其余15种不变。action `2da4f06d-abb0-4c0d-a68c-eeb98c97a043` 已在tick80538928成功终态；命令后续因`Wait-SpherewrightPlayerSettled`包装误当玩家DTO而exit1，不能据此重跑。21条原始索引、独立只读快照和接口契约见[科研消费者短带事件](evidence/2026-09-30/science-consumer-stub.md)。此前源1511取料/recipe85手搓保存仍见[紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md)。

## 下一消费者接口与边界

独立只读run `062cd8fce1024bbf805f67a0381da62a` ord2显示玩家Walk、speed0、core energy800 MJ、3架无人机idle、0 working/0 pending；ord3为J96 durable；ord4功率仅单次采样（network3 ratio1，network4 ratio1），不代表持续供电。当前下一唯一阻塞为近端5197→Lab84 slot2原生分拣器附件预检。之前run `0abcbd57d7544a8aaba610ca788cf916` ord5只证明两个`native_grid`连接2001×2可放，0 commit/token discarded；目前仍未证明附件成功、完整紫糖通路或科研吞吐。此前Lab84紫糖科研点最后fresh读数仍止于run `2e501713…` 的0，本次未重读；详见[Storage caller现场边界](evidence/2026-09-30/storage-buffer-caller-fix.md)。

本次未保存新带、未测试重启恢复、sorter attachment、完整紫糖输送或持续科研；短带施工成功与单次功率采样均不能替代这些验证。

证据入口：[科研消费者短带事件](evidence/2026-09-30/science-consumer-stub.md) · [紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md) · [蓝色后备分拣器事件](evidence/2026-09-30/blue-backup-sorter-binding.md) · [Roadmap](../ROADMAP.md)。
