# Drive Engine IV 科研选择与正常保存

日期：2026-10-01（Asia/Singapore）。本事件记录科研目标选择及保存读回；不代表科技已解锁、持续科研或实机重启通过。完整受保护回执留在证据库，仅列索引和核验结果。

## 科研选择与保存

初始状态 run `50f6293c4b504c5a89a1cf23b2bc3d82` ordinals 1–7 成功：tick 83237500/R2/J96；1704、2104、2903 已解锁，Drive Engine IV（tech 2904）仍锁定、0/720000，三种科研糖各 2000。

之后选择 2904 的唯一 accepted action `f5820b58-13e5-4529-b916-d49212640144`：run `ae6b5599c8a94606939cad538a23b7c4` prepare 7、commit 9、terminal 10，completed tick 83307632。fresh progression ordinal 11 显示队列仅 `[2904]`、currentTech 2904、hash 15/720000，仍为 locked；选择目标不等于解锁或科研产出。

正常保存 action `6c64e223-5c5e-4e5c-afcd-4fdd49bdd848` 在同 run prepare 13、commit 15、terminal 16 成功，completed tick 83307746。closing ordinal 17 为 tick 83307763/R5，同一 owned identity、healthy、保存 tick 83307746；Journal ordinal 18 为 durable 97/97、无 pending/error，原 J96 全部保持前缀一致。新增第 97 条 `upgrade_first_selected` 记录 tech 2904、tick 83307635。root 独立 proof `38bc70f00b764ba5a475d24fed0b3ab8` / SHA-256 `B1980663266C42105290CCDBE18160DB573495C4A0B7E2CB22A7E39F6746CBF7` 核对原选择、保存、closing 与 Journal。

本阶段外部 accepted 从 4/10 到 **6/10 OPEN**：两笔均为唯一成功 action/terminal，无 unknown 或在途写。两个 action 各自观察到的 elapsed ticks 均为 0；本事件不把选择或保存当作科研实验时长、解锁或持续产出的证据。`restartAvailable` 不等于保存后的真实 restart/resume。没有新施工、库存转移或 Move。

## 计时与剩余边界

helper dispatch 到首个 prepare 为 4444.860 ms，总计 5337.661 ms；这是该入口片段，不是委派、模型或整阶段墙钟，也不能据此声称 token 节省。安装 runtime 仍为 `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`（228 files / 64 tools / 1 resource，DSP 29104），本阶段未部署。

仍未证明持续燃料/曲速供给、Foundry Level 3 整厂施工或最终双包验收，也没有本次保存后的 restart。旧石料/紫糖问题只见[前阶段关键供给链事件](purple-critical-path-source-route-demand.md)，不在此重述。下一准备 blocker 是既有 3403 的氘燃料入口；只读诊断仍在进行，当前没有结论，不提前写成通过或失败。
