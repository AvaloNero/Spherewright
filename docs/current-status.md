# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新读回 | run `32b2cc3657084ae5b833d714d3a94348` ord18：tick80442890 / revision33；healthy | 配置读回，不等于生产窗口 |
| 保存 | lastSaved tick80425709 | 蓝色后备分拣器868本次配置尚未保存 |
| 外部写窗口 | accepted **2/10**，无在途动作 | 包含此前正常保存及本次一次配置动作；不得重放 |
| Git / 发布 | 本批次前 main 为 `1c2300a` 且对应CI已绿 | 本批次须按准确SHA验CI；不代表安装或发布 |

accepted action `24ede7d8-5d6b-4dc3-8a7f-eda4afd14fc9` 成功将868过滤由1202改为1301，连接26↔868↔76互返；`targetObjectIds=[]` 与 `itemDeltas=[]` 是此配置动作的合法回执。请求绑定使用Sorter配置hash，不要求建造回显字段；玩家库存未变。详情及离线复核见[蓝色后备分拣器事件](evidence/2026-09-30/blue-backup-sorter-binding.md)。

## 下一消费者接口与边界

Lab76采样显示线圈6、电路6、blue output 10、working=false（输出buffer满）；这是采样边界，不证明868实际投递或持续供给。下一步仍是对Lab84做最小紫糖消费者端口预检，再据结果决定上游。Lab84紫糖科研点最后一次fresh读数为此前run `2e501713…` 中的0；本次未重读Lab84，结论不越过该样本边界。详见[Storage caller现场边界](evidence/2026-09-30/storage-buffer-caller-fix.md)。

本次未测试重启恢复、紫糖实际科研供料或2104解锁；正常配置成功与采样点读数均不能替代这些验证。

证据入口：[蓝色后备分拣器事件](evidence/2026-09-30/blue-backup-sorter-binding.md) · [上次正常保存](evidence/2026-09-30/core-materials-normal-save.md) · [Roadmap](../ROADMAP.md)。
