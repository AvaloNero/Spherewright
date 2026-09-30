# Spherewright 当前快照

更新：2026-09-30（Asia/Singapore）。本文件是覆盖式状态摘要，不是机器状态源；身份、Journal、accepted 与终态以 fresh 状态及受保护原回执为准。历史见[游戏时间线](gameplay-timeline.md)。

## 保存与写窗口

| 项 | 最近已核证值 | 边界 |
|---|---|---|
| 游戏 | owned-world-001 / planet104；DSP 0.10.35.29104 | 本次不代表重启恢复或发行验证 |
| 最新读回 | run `52ccd4fc2e86454c88a17483b8ffe62e` ord7：tick 80425712 / revision 31；healthy | 本次是保存后状态读回 |
| 保存与日志 | lastSaved tick 80425709；Journal durable J96，无 pending/error | 电路板与紫糖两笔转移已被正常保存覆盖 |
| 外部写窗口 | root 已明确重新开放；accepted **1/10**，唯一写动作为正常保存 | 无在途或未核销动作；保存回执不代表重启恢复 |
| Git / 发行 | 本文档批次前 main 为 `8fa8cec` 且其 CI 已绿 | 本文档提交需按准确 SHA 验 CI；不代表已安装或发布 |

run `52ccd4fc2e86454c88a17483b8ffe62e` ord5 的唯一 `commit_save` action `8fdebc08-c842-4b67-809b-cef1c1edc59c`，ord6 成功完成于 tick 80425709；ord7 确认 lastSaved 与健康状态。原回执索引无 replay、unknown 或 intent。细节及计时边界见[正常保存事件](evidence/2026-09-30/core-materials-normal-save.md)。

## 下一消费者接口

下一步先对 lab84 做最小消费者端口预检，再据结果决定上游。此前只读证据显示 lab84 紫糖科研点为0，储仓3051有紫糖2288、仅有输入5113而无输出；这说明待核真实输送接口，不证明当前仍无生产或已经供料。参见[升级后三窗诊断](evidence/2026-09-30/hydrogen-upgrade-three-window-diagnostic.md)。

两笔转移现已保存，但本次没有测试重启恢复、持续生产、科研实际供料或2104解锁。不得将正常保存当作这些验收的替代。

证据入口：[正常保存事件](evidence/2026-09-30/core-materials-normal-save.md) · [第十写材料与工厂审计](evidence/2026-09-30/core-materials-ten-write-audit.md) · [Roadmap](../ROADMAP.md)。
