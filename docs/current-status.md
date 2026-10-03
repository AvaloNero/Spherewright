# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。本文覆盖当前截面；阶段原件索引见[Gate 2证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## Owned世界与保存边界

- DSP仍为同一owned世界，版本`0.10.35.29104`；source HEAD `776fee1`，installed `3fe31d1`及native `29104`未变、未部署。Steam/DSP保持运行。本窗没有退出、重启、重载或额外保存；Codex Host的关闭、重启或断线不应触发游戏生命周期动作，专门的Host退出存活测试仍未做。
- 本窗正常保存tick **88283027 / R56 / J98**。之后完整工厂capture的closing observation为**88288293**，不是新保存。root proof `2ec425408a624395a19f65100e0d79f6:1`，SHA-256 **`D42B8AB445F8DA4289A772CC6539F63BDC5AE4EFA1EDBE838038ACF713C82677`**；独立审计35.406秒、0次新Game调用。
- 十个accepted动作均有completed/succeeded终态；replay、unknown、在途均为0。external **10/10**、lifetime **120**保持冻结；此审计和CI不会自动开启下一写入窗口。

## Gate 2当前边界

**1210完整自动链、整链新鲜actual-ID联合资格、持续供料/功率、以及保存后protected restart仍未完成。** 本次完整capture `682d8ec634ae45389ecfcf15e94098ad`（open `:1`、pages `:2–60`、P `:61`、J `:62`、Power `:63`、prebuild `:64`、details `:65–86`、closing `:87`）为**59页 / 5879 built / 0 prebuild / 11478互逆边**。相对前一完整图只增加36条带、2个分拣器和4个风力涡轮机，无删除或未解释的静态漂移；仅有两项保留cover旋转（5794、5773）通过精确原生核验。

- 粒子容器132条带有向路`5760→5761`及两端均在capture中核实：`885.slot2 → 5874 → 5760`、`5761 → 5873 → 5326.slot5`，物品1206、N3、full serve。5326 detail `:68`当帧output1127=20；这不是持续供料证明。Diamond新增地面带5875–5879尚未接入。5333 detail `:69`尚无Diamond/Strange且Lens输出为0；5334 detail `:70`读到Graphite 2、Diamond 100。
- N3当帧为216 nodes / 531 consumers / 131 generators，动态capacity **1,806,000 J/t**、required=served **227,586 J/t**、ratio 1。四台新增风机的实际wind credit为**20,000 J/t**，不是热电铭牌或持续输出。对声明的整案峰值**1,803,800 J/t**，该截面仅有**+2,200 J/t条件余量**；不据此声称持续功率余量或门已永久通过。
- 当前剩余还包括Diamond、Strange、Lens与Graphite接口；最近Lens/Strange端点资格只是tokenless preview，不是可提交计划或actual-ID原生资格，须按新的正常流程fresh核验。没有全1210产出、36000-tick窗口或protected restart通过结论。阶段动作索引与原件见上述唯一阶段证据。
