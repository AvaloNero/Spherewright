# 核心材料正常保存回执

日期：2026-09-30（Asia/Singapore）。记录主会话已核验的一次正常保存及保存后读回；不含凭据、真实存档名或本机路径。

## 保存与持久性读回

| 证据 | 已核事实 |
|---|---|
| run | `52ccd4fc2e86454c88a17483b8ffe62e` |
| 唯一写动作 | ord5 `commit_save` accepted；action `8fdebc08-c842-4b67-809b-cef1c1edc59c` |
| 原生终态 | ord6 同 action `completed` / `succeeded=true`，tick 80425709 |
| 保存后 fresh | ord7 tick 80425712 / revision 31；`lastSaved=80425709`，healthy |
| Journal | ord2、ord8 均 durable J96；无 pending、无 error |
| 索引 | 1 个唯一 accepted；无 replay、unknown 或 intent |

此前记录的电路板与紫糖两笔普通转移已由本次正常保存覆盖。保存成功不等于重启恢复；本次未测试重启。

## 计时口径

入口 stage timing 约 0.466 秒，命令约 1.325 秒，索引 `receiptWallMs=86.390 ms`。这些分别是不同边界的计时；不含委派、主会话工作或 CI，不据此声称端到端提速。

## 当前下一接口

外部窗口由 root 明确重新开放，当前 accepted 为 1/10，唯一写动作为上述 save；当前无在途或未核销动作。下一步仍是先做 lab84 的最小消费者端口预检，再据结果决定上游；本次保存不证明紫糖已实际送达科研站或研究已解锁。
