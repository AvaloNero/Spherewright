# Storage 格 item count caller 修复

日期：2026-09-30（Asia/Singapore）。仅记录已核现场边界与离线 caller 校验；不访问或修改运行态，不据此宣称紫糖输送或科研通过。

## 已核现场事实

- run `2e501713...` ord1/3/4/5：五次 fresh 读取；没有 prepare 或 commit；原命令 exit1，外部 accepted 仍为8。储仓3051有12个格，itemId 6004 合计2324，配置输入仅为5113。lab84 的紫糖科研点为0，native 槽状态为 `observed`，不是 `ready`。
- 后续 run `67ed5197...` 也是五次读取、零 prepare/commit，命令 exit1。私有 caller guard 错把 `observed` 当作 `ready` 后停止；这是 caller 错误，不是原生拒绝。两次运行均没有新增 accepted。

## 修复与离线覆盖

`Get-SpherewrightStorageItemCount` 将同一物品在多个已观察 storage buffer 格中的 item count 逐格汇总；要求 storage/entity、已观察 buffers、buffer role=`storage`、countUnit=`items`、unitsPerItem=1，并拒绝未知、科研点、非法 itemId/count、负数和 Int64 溢出。只有确实观察到的空 buffers 或零 count 才返回0。

`scripts/test-stage-tools.ps1` 通过 fixture 覆盖12格（11×200+124=2324）及不同物品类型；零/空缓冲、缺失/null 缓冲、research-matrix points、未知 component、错 role/unit/multiplier、null/string/零/负数/超 Int32 itemId、null 行、负数/null/小数/布尔/缺失 count 和 Int64 求和溢出。PowerShell 7 真实 `-File` 结果：`passed=26` 原阶段断言不变，新增 `storageChecks=28`、`successfulFixtureRequests=14`、`gameCalls=0`。原成功 fixture 的两个唯一 commit、accepted 计数和不重放负例保持原样。

本修复只让已观察 storage 格库存能被安全汇总，不让 `observed` 变成 `ready`，也不等于 lab84 已接通、科研消耗已发生或产线通过。
