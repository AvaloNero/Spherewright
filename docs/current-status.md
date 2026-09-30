# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新读回 | run `f0bea78c9032414f993bec3a12ccdbfa` ord25：tick80526462 / revision37；同一owned planet104、healthy、saved、resume available | 保存读回，不等于重启恢复 |
| 保存 | lastSaved tick80526460；ord26 Journal durable J96/96，无pending/error | 已覆盖868过滤配置；本次未验证重启 |
| 外部写窗口 | accepted **5/10**，无在途动作 | 本阶段transfer、handcraft、save三项均有唯一成功终态；不得重放 |
| Git / 发布 | 文档提交前基线 `73401fc` 的Windows Core CI已绿 | 本次文档提交按准确SHA另验CI；不代表安装或发布 |

run `f0bea78c9032414f993bec3a12ccdbfa` 中，源1511铁3000→2999、背包0→1；recipe85手搓一次，铁1→0、电路板2011由1→2、C1000→999，其余14种库存不变。保存动作成功且fresh读回R37/J96持久；完整索引、动作终态和计时边界见[紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md)。蓝色后备分拣器868先前过滤1202→1301，连接26↔868↔76互返；其请求绑定和合法空目标/差量回执见[蓝色后备分拣器事件](evidence/2026-09-30/blue-backup-sorter-binding.md)。

## 下一消费者接口与边界

Lab76此前采样显示线圈6、电路6、blue output 10、working=false（输出buffer满），不证明868持续供给。当前下一硬阻塞为Lab84紫糖消费者分拣器原生附件尚未验证：run `0abcbd57d7544a8aaba610ca788cf916` ord5仅证明slot2朝消费者方向的两段`native_grid`连接（2001×2）可放，未commit且plan token已丢弃；不代表整线接通。Lab84紫糖科研点最后fresh读数仍止于此前run `2e501713…` 的0，本次未重读。先最小验证消费者端口，再决定上游；详见[Storage caller现场边界](evidence/2026-09-30/storage-buffer-caller-fix.md)。

本次未测试重启恢复、紫糖实际科研供料或2104解锁；正常配置成功与采样点读数均不能替代这些验证。

证据入口：[紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md) · [蓝色后备分拣器事件](evidence/2026-09-30/blue-backup-sorter-binding.md) · [上次正常保存](evidence/2026-09-30/core-materials-normal-save.md) · [Roadmap](../ROADMAP.md)。
