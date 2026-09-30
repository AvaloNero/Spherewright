# 矿机材料手搓、正常保存与业务预检时限

日期：2026-09-30（Asia/Singapore）。在[已封存且 CI 通过的十写窗口](coal-miner-materials-ten-write-audit.md)之后，同一个 owned/healthy 母星 `104` 开始新外部 accepted 窗口。本阶段只正常手搓和保存，不移动、施工、改配置或换档。

原始运行时配方已在上一阶段核对：齿轮 recipe5 `count=2` 用铁2；磁线圈 recipe6 `count=1` 用磁铁2+铜1产线圈2；采矿机 recipe48 `count=1` 用铁4+电路板2+线圈2+齿轮2。每步 fresh player/session hash 后用现有 `Invoke-SpherewrightNormalAction` 做正常 prepare/commit，同 action 轮询至 terminal，再核队列和背包。动作回执：

| 新窗口 # | 动作 | 受保护回执、terminal 与读回 |
|---|---|---|
| 1 | 齿轮2 | `671097694c24419c8998b3c5a2abde18/0010`，`8e345ec0-3d79-4bb1-bfb6-5d2be72a9c10` completed/succeeded；0→2、队列0 |
| 2 | 磁线圈2 | 同 run `/0019`，`6546451f-f448-449f-8767-5f4bc554a7ae` completed/succeeded；0→2、队列0 |
| 3 | 采矿机1 | 同 run `/0029`，`7eb62ecb-414e-419b-9aee-323d44edf1ab` completed/succeeded；0→1、队列0 |
| 4 | 正常保存 | `810f4a60b2d04ee6b1a212ebfee92f9b/0008`，`897198ae-cab0-4c62-808d-de3a1f5db980` completed/succeeded；主档 tick `79763037 / R25`、healthy、protected resume available、Journal `95/95` durable/no pending |

保存后独立受保护读 `d1d41dfe1b62470cb138a94fead7a732/0002`：玩家 Walk/0、手搓队列0、矿机2301×1；铁1101、电路板1301、磁铁1102、铜1104、齿轮1201、线圈1202均为0。证明材料守恒与一台矿机在背包，**不**证明可在候选煤脉放置、已有电力覆盖、出矿输送或持续石墨。

执行前，Luna 的受保护 run `5c649d13158443a281094b8c7cc936df` 只有 `0001–0002` 会话读取和 `0003` 玩家读取，超过一分钟没有 `prepare_handcraft`、`commit_handcraft` 或 action。主会话中断该委派并核原始序号，再 fresh 读同一 owned/healthy 会话、revision18、材料齐备和空手搓队列后接手。没有可重放的 Luna 动作。主会话三次手搓从首请求到最终终态/读回约 `8.014s`，保存约 `1.992s`；这不含先前的委派等待、业务设计、独立审计、文档与 CI。仅用 session 心跳作为“已开始执行”会掩盖对话层等待，因此 AGENTS.md 的时限现在明确绑定首个**业务 prepare**。本阶段 accepted=4，尚未达到十写冻结。

下一条件是靠近磁环附近煤组并对明确的边缘矿点做一次原生 `prepare_build`；当前结构代码会有限枚举该点周围位置并择覆盖较多的有效候选，但静态煤量与矿机在背包都不能替代真实落点预检。两次相同原生拒绝后按既有规则停下重设计，不盲试第三点。
