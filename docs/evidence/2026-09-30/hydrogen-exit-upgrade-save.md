# 重氢链分拣器升级与保存

日期：2026-09-30（Asia/Singapore）。同一 `owned-world-001`、母星104、DSP `0.10.35.29104`。阶段 run：`eb17b3db04fa44deaabfebafb144e2fd`。本文件汇总一次分拣器升级、正常保存及其明确边界；不是持续产量、重启恢复或科研完成验收。

## 升级前提与 accepted #7

升级前 fresh 读回显示3064有满仓600氢；旧分拣器3068使用普通型号2011，向3338输送。3068之前和3338入口对应的带段曾为空；另一重氢来源3073的氢输入不足。此操作只提升3068的局部周期，不宣称修复整条吞吐。

- 唯一升级 action `abd72651-afb7-4a66-9d6d-96c665e81a23`：原生 prepare / intent / commit / terminal 分别为 ord7/8/9/10；终态 `completed`，tick80273779。
- 即时 upgrade readback 保持实体3068，型号2011→2012，recipe0、filter1120、`verifiedConnectionCount=2`。周期策略为 `basic_sorter_cycle_fraction_retained`，进度380000/600000→190000/300000，保留原有周期比例。
- 即时材料差量为2012 −1（1→0）、2011 +1（0→1）。本次 buffersBefore 与 buffersAfter 均为空；不把本次当作携货中的升级正例。
- fresh ord11–13 分别读到3068为2012、源仓3064 slot8→3068 slot1、目标3338 slot4←3068 slot0，连接互返；相邻带实体3337/3339的原有连接保留。

## accepted #8：正常保存与终态

- 保存 action `0b6bf765-8c84-4386-bbe1-d4623a9e8fdc`：fresh ord14 为 R27；prepare / intent / commit / terminal 为 ord15/16/17/18；唯一 commit 成功，完成 tick80273796。
- fresh ord19：tick80273799、R28、owned/saved/healthy；`lastOwnedSave=80273796`，`restartResumeAvailable=true`。这只是恢复能力已通告，未实测本次保存后的重启。
- Journal ord20：tick80273801，`durableThroughSequence=96`、无 pending、无错误。

## 独立科研只读状态与未证明项

另一个已核只读 run `d82c1ba...` 的 ord6 查询显示 lab84 蓝/红/黄科研点数分别39160/39570/36000，紫色为0；这些是科研点数，3600点/件不能直接当作物品库存。储仓3051实际有紫糖2288，输入为5113、无输出。2104仍在队列，`hashUploaded=0`；当前科研阻塞是紫糖未连接lab84，不是没有生产紫糖。

本阶段只有两个唯一、非 replay 的 accepted（升级#7、保存#8），累计8/10，尚余两个写槽；旧执行正常退出，无在途或未核销动作。未测升级后的连续产量，也未验证重启恢复。升级效果留待游戏 Luna 的有界只读三窗观察；此处不将其标成通过。
