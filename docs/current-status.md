# Spherewright 当前快照

更新：2026-10-11（Asia/Singapore）。执行 cohort 仍为源码 `369f35c0d7ea352d505ec686cc22a52294d5601a`；最新 R820 是只读消费者/物料诊断（9 RO、0 accepted），保存、玩家库存与 Journal 未变。来源条件仍未通过。

## 当前保存与窗口

- 最新保存为 R798 普通 Save `106821990`，durable Journal 102。固定 20 写窗口从 lifetime 454 开始，当前 external `14/20`、整案 lifetime `468`；本阶段 7 个唯一 accepted 均已保存覆盖，无在途或未知动作，窗口保持冻结，计数未重置。
- R798 的有限准备、移动和分拣器升级只造成玩家库存 1301 `−1`、2012 `+1`；Warp 400、Rod 5、Stone 6 未变。库存转移和设备升级不作为持续生产证明。
- 完整工厂为 6492 built/0 prebuild。相较阶段前唯一静态变化是分拣器 6492 的 itemId 从 2011 升至 2012；没有新增、移除或非互惠连接。其 filter 1001、端点 6477→1220、持货与双向连接保持；Native 周期要求从 600000 降为 300000，N3 满供电。

## 来源与持续供需

- R783 的 R778 连续观察已证明 Warp 下界 4.5/min、Rod 下界 1.8/min，但 P102 钛矿（1004）与 P104 钛块（1106）为 0/min，且六项库存超过单批容差；这些失败保持。R798 的有限准备与 sorter 升级没有替代来源门。
- R807 三个有间隔的 600-tick 窗口中，1102 磁铁均为 30/min，6492 升级后的吞吐未见改善；1101 铁块为 60/0/0（均值 20/min），铁矿为 42/min，高能石墨 1109 为 30/24/18（均值 24/min、消耗 26/min），原油产出 24/min、消耗 20/min。1004 钛矿、1106 钛块、1127 奇异物质、1210 曲速器与 1802 氘核燃料棒产出均为 0/min。三个本地电网满供，五个当前铁矿节点首尾余量为正；合计仅观察 1800 tick，不构成连续门证据，continuous credit 仍为 0。详见 [R807 磁铁供料短窗观察](evidence/2026-10-11/magnet-feed-short-r807.md)。
- R803 对 1216 的三次 fresh 读取均为 input 3、output 100，末次 `isWorking=false`；6492 分拣器第一个样本为 `Returning` 且未持货，后两个样本为 `Inserting` 且各持铁矿 1，周期为 300000。R820 读到 1216 input 铁矿 2/output 磁铁 100；磁铁库1217为 5827/6000、仍有 173 空位，三个磁铁消费者及电动机/涡轮消费者均已备料但停工。铁炉1500与钛炉530也有在制库存且停工；站916/1657当前需求与订单均为0、无人机/货船空闲。以上截面不证明分拣器速度是瓶颈，也不证明运输阈值是唯一原因。root 暂停 1234/1241 升级，下一步先核正常需求与自动运输如何传至实际来源，再决定有界需求/预热或必要修复；不靠增加容量/库存冒充来源或重跑长窗。R785 仍只是缺件预算，不代表已执行或完整准备通过。详见 [R820 消费端与流程核验](evidence/2026-10-11/consumer-output-and-process-r820.md)。


## 验收边界

- `sourceConditionsPassed=false`、`wholeSupplyPassed=false`，continuous credit 为 0。Warper 与 Rod 各至少 1/min、连续 36,000 ticks、双自动补给、完整来源竞争和材料供需、整合保存恢复及最终同 SHA 双候选包验收仍未通过。
- 已通过的蓝图/Governor 等历史门保持有效。R508 双包检查仅为旧源码 SHA 上的离线预检，不是最终候选、真实 Mod Manager 安装或发行验收；执行 cohort 和已安装 DLL 未因本阶段事实改变。

阶段索引：[磁铁供料周期升级与保存核验（R798–R802）](evidence/2026-10-11/one-magnet-feed-upgrade-r802.md)、[R778 连续完整来源观察与 R783 审计](evidence/2026-10-10/current-full-source-long-r783.md)、[R770 Warp 取料与此前来源截面](evidence/2026-10-10/warp-demand-and-current-source-r777.md)、[R763 窗口关闭和出发材料截面](evidence/2026-10-10/rod-carry-and-window-closure-r763.md)。
