# Spherewright 当前快照

更新：2026-10-01（Asia/Singapore）。本文件为覆盖式状态摘要，不是机器状态源；身份、accepted 和原生终态以 fresh 状态与受保护回执为准。

## 当前配置与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet 104，DSP 0.10.35.29104 | 已从精确 owned primary 恢复；和平、非沙盒、1×。不代表 0.4 发行验收。 |
| 最新完整 closing | tick 83113691 / R2 | 保存后 player/session 两份原回执已独立核验，同一 owned identity；玩家 Walk、speed=0。 |
| 正常保存与 Journal | save 83066935；J96 / 96 条完整 durable | 无 pending/error；`resumeAvailable=true` 不等于真实重启测试。 |
| 外部写窗口 | accepted **4/10 OPEN** | 无 unknown 或在途写；R/tick/J 不重置。 |
| 阶段基线与安装 | pinned source `0f8a9005a23143d98074021b1811acd6632769dd`；installed runtime `6bf35b7b81a2e50c8e9f42feebbc1f15552096de` | 228 files / 64 tools / 1 resource；DSP 29104，未新部署。 |

## 石料源与紫糖关键链

当前唯一权威阶段记录为[紫糖关键供给链、原料路径与需求核对](evidence/2026-10-01/purple-critical-path-source-route-demand.md)。此前外部窗口为 2/10；本阶段 exact owned-primary 恢复及一笔正常保存后为 4/10。保存 action 已核唯一成功终态和同一 action 的 session/J96 连续性；菜单只读曾有一次 timeout，之后 fresh 健康核验成功，没有重复启动。从诊断到保存前，6004 的背包 +500 与 mecha 科研缓存 500 whole / 1,800,000 points→0 守恒；保存后全部背包 count/inc 与保存前相同，不把它当新产量。2104已完成，机甲核心容量800→1600 MJ；不将容量等同当前电量。

硬阻塞是原料路径：861 配方24每周期需原石1005×8，现有5；1124/1402 在 planet104 的原生生产统计均为0，4743 的1402输入为0、6004输出为0。静态线路投影只能定位既有连接，不能证明实时送货或全链通过。一个600 tick观察窗里1005、1116、1124、1402均0产0耗，6004为0产1耗；没有1123独立速率，不补算。95的2198是1108石材而非1005原石；网络3/4 ratio=1不代表源端有输出。

原拟正常手动采集64件1005石矿并转存95的有限试验，以及B多窗/C 36000 tick实验，均因2104已完成、科研队列为空而取消；未生成采集/转存plan。本阶段没有新采集、转移、建造、配置写或Move。既往两个候选点的prepare-only碰撞已记录在阶段事件；不重试旧坐标、不盲换朝向、不改动成功线路。尚未证明持续采石、硫酸/紫糖生产、端到端入料或本次保存后的真实重启。现阶段不另造紫糖路线；后续须先有真实消费需求和满足石矿输入的有界方案。

## 安全与验收边界

恢复仅用于精确核验的 owned primary；保持 prepare→唯一 commit→同 action terminal/readback、durable Journal 与正常保存链。accepted 不因 revision、tick 或 Journal 变化清零；断线、解析错误或读回不稳都不是重放授权。unknown、quarantine、身份/版本漂移或材料无法核销时冻结新写并交 root。正常保存只作用于当前 owned identity；`resumeAvailable` 不是已恢复证明。凭据、token、真实存档名、绝对私有路径和 raw body 不入库。
