# Spherewright 当前快照

更新：2026-10-05（Asia/Singapore）。本文件覆盖当前状态；本轮唯一权威阶段证据见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。

## 当前 owned 状态

- 最近正常保存：`66ad75c2f8f34e32a9e5be0e400dec14`，终态 tick `94656863`，约1.781秒；当前仍是已验证安装 cohort `b1557bb`、native `0.10.35.29104`（4 Plugin+224 MCP文件匹配，64 tools/1 resource）。本轮未部署。
- 现有1210链启动、正常保存、protected restart及恢复后再次非零输出正例已在此前阶段完成；不代表全源配平、有限缓存排除或持续供料已通过。此次十写窗口闭合后 `R2/J100`，external accepted `10`、lifetime `150`；最新只读观察 `94691576/R2`，保存仍为 `94656863`，Journal `pending=false/error=null`。没有 unknown、in-flight 或未保存 accepted 动作。DSP/Steam 保持运行；用户取消的 Host 退出存活测试未做。
- 十个本窗口 accepted 动作来自 `fc8f0f0e907d4f51a50b6e1ddde97332`（5项）、`1193a2656645437d88c6434ba88d079a`、`a9f30a480263417f97da208330dcd903`、`5cce6a8ddf754c89a4be6565b7086f3e`、`920e3b7d46a246dda218fa960fb85b4c`、`66ad75c2f8f34e32a9e5be0e400dec14`（各1项）。原件汇总 `5babc932811040f3b5b311502f8fb3ef:1`：10 unique succeeded，无重放/未知/在途；Fe `1101` 净−3、coil `1202` 净−1，其余物料净变化0，与玩家库存核对相符。

## 本轮全厂核对与边界

- 新完整工厂快照 `dabf773d60f0472baa9d1bc61ec0b806`：factory tick `94667688`，60页、5945实体、0 prebuild、18 detail、11620 reciprocal edges；采集17.844秒。与基线 `183568336ec7495d87fe7c0137873b70`（tick `90521995`）结构计数一致。root独立审计 `54e57431ac4343f89c454f702bbeb195:1`，SHA-256 `A1C8AF4D7077AC7F597E9161ABDC4CA13523053D43E802A1EE9C347E12A762FB`，0 Game calls。
- resource membership 仅见精确删除子集：`1213` 的 `35,42`，`1496` 的 `45,49`，`2440` 的 `163`。Iron `99` 与 Copper `69` 核对证明这些 ID 不在匹配的 active product 枚举中；与自然耗尽相容，但不证明所有节点均不存在，也未归因具体历史耗尽时刻。材料净变化以本轮十动作摘要为准。
- 本轮没有新油源 native 正例、1210 全来源供料证明或连续窗口信用。已有目标 `1210≥1/min`、氢 `20/min`、重氢 `10/min` 仍是目标，不是此窗口实测；旧燃料 `1802` 的实际需求率仍未知。整案/全源配平/有限缓存排除仍为 false。
- root 于 `AGENTS.md` 明确新增了2026-10-05窄资格例外。即使进入资格准备，仍须等待本次文档提交对应 CI 为绿且 root 明确重开；当前没有施工、移动、取材、长窗或 restart 授权。不得因对话结束关闭 DSP/Steam。
