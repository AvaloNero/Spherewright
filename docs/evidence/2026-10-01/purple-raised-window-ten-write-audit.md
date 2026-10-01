# 紫糖主干十写独立审计

日期：2026-10-01（Asia/Singapore）。root 已完成当前10/10窗口的独立审计。审计完成不自动开放下一写窗口；仍须本事件提交/CI绿及 root 明确交接。

## 原生完工与朝向差异核销

原完整比较 `bfb19d92f78d4286a2dcc2f459f4ac4b` ord1（SHA-256 `E8FFCB807A95A8084C7F39430DCD26F38EA5913494BF7DD774A424D746BA47A5`）保留其原判定 `false`，差异仅列 `5205` / `5219` rotation。补充原生对照 `8351cf53dd7640da92c4120552793478` ord1（SHA-256 `FF7096162FE1AB7D11509C18DD55D109DFA44215264F29B78509FC88AC003D02`）独立确认该差异由已批准完工路径解释，结论 `true`；不是通用忽略 rotation。

对照使用已批准动作 `e6c457b0f9044c5880fa268a0c6a6c69`：ord20 prepare 指明 `native whole_path_native_rotation_v1` / `empty_open_path_native_geometry_v1` 的双端 `non_removing_belt_cover`，ord27 terminal tick81944295。`5205/5219` 完工前分别读 ord9/10，原生完工后读 ord33/34；两处 quaternion 变化分别为43.5189757°、0.10135915°，位置及旧边保留，只新增 `5205→5229`、`5228→5219`。离线6个故意修改 rotation、position 或 connection 的fixture均被拒，game calls=0。只有与精确批准原生动作及前后证据一致的变化可被解释；不放宽全局静态比较。

## 十写与完整快照

十个原 accepted 动作均唯一，且有成功的同 action terminal；原始轮询共123次。封存基线 `a4e1a2f337d143908ad30d7844e11ac8` 与完整快照相比，built实体5227→5267（新增40：38 belts、2 sorters；删除0）；有向边10186→10266，均互返。快照含53页、5267实体；53项关键详情齐全，prebuild=0、无人机 pending=0。

背包仅 `2001` 减少38、最终305，`2011` 减少2、最终4；其余数量及增量 exact。J96 durable、无 pending/error。同一 owned identity，healthy、peaceful、sandbox disabled、1×。正常保存tick82280505；快照tick82295726；最终closing tick82348777/R34。`resumeAvailable` 不等于实际重启验证。

完整快照 `679a95640dea47a2a648c78560e3dca8` ord2–54 已保留53页；ord55–58含玩家、Journal、电力与prebuild读回。只读调用方把 PowerShell `-File` array 参数从53项折叠成1项，触发本地 guard；这是调用边界问题，不是游戏故障。后续 `11ad8a939a564de4a229776846ae106b` 仅 fresh 补53项详情、progression和closing，未重采53页或4项已有观察，game writes=0。未把快照字节量当作模型token，也不据此宣称总耗时或节省比例。

## 窗口与后续边界

外部写窗口仍 **10/10 FROZEN**。十写独立审计通过，但本次文档提交和对应CI之后仍须 root 明确交接，才可开新窗口；此处不改报OPEN、不新增游戏写入。

尚未证明的物流边界为源尾 `5227→5243`、East1尾 `5266→5230`，以及仓库 `3051` 供料。紫糖到货、科研推进、持续生产和真实重启均未证明。
