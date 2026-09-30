# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、accepted 与原生终态以 fresh 状态及受保护回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新观察 | run `37217b5f0c7645e9ae039d4ee646aa91` ord17：tick80608692 / R43；同一owned planet104、healthy | 新三段belt stub尚未保存 |
| 保存 | lastSaved tick80526460；J96 durable仅见于此前只读run `062cd8fce1024bbf805f67a0381da62a` ord3 | 本run未fresh读取Journal；新短带未保存 |
| 外部写窗口 | accepted **8/10**，无在途或未决 | 三段stub已有唯一成功终态；不得重放 |
| Git / 发布 | 文档提交前基线 `bb31796` 的Windows Core CI已绿 | 本批提交按准确SHA另验CI；不代表安装或发布 |

run `37217b5f0c7645e9ae039d4ee646aa91` 建成5202→5201→5200三段item2001 belt，两端开放、互返边齐全；源3051静态/5113旧输入不变。背包item2001为370→367、2011仍1，其余15种物品数量及累计增量不变。action `9d5a95b0-f97b-4e36-8f7f-e74fbcaa5e07` 在80608639成功终态；fresh R43为80608692、lastSaved仍80526460。完整索引及边界见[紫糖仓库出口短带事件](evidence/2026-09-30/purple-source-stub.md)。此前消费者分拣器5199附件读回见[科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md)。

## 下一消费者接口与边界

源端现有5202→5201→5200三段短带，两端自由；不代表3051出料或5198主干已连。此前run `561fc…` ord4的三点native预检为0 commit，仅是旧计划读回。下一唯一阻塞是fresh核验5200→5198双空端主干完整原生预检；最后两写槽留给主干和正常保存，源仓出口分拣器依root计划待十写审计后再建。未证明紫糖实际送达Lab84、科研吞吐、保存后恢复；J96仍只引用较早run `062cd8fce1024bbf805f67a0381da62a` ord3，不是本run新读数。

本次未保存新短带、未测试重启恢复或持续科研；新建主干、仓库出料及Lab84实际收料仍未证明。

证据入口：[紫糖仓库出口短带事件](evidence/2026-09-30/purple-source-stub.md) · [科研消费者分拣器事件](evidence/2026-09-30/science-consumer-sorter.md) · [紫糖路线材料事件](evidence/2026-09-30/purple-route-sorter-materials.md) · [Roadmap](../ROADMAP.md)。
