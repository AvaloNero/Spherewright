# 短移成功与调用方 revision 误判

日期：2026-09-30（Asia/Singapore）。当前安装仍为已实机验证的 DSP `0.10.35.29104` 同批 Plugin/MCP，没有冷部署、产品代码变更或新存档。此前十写审计提交 `1aa36f0` 已推送、CI `36593552043` 通过，主会话随后只读确认同一 owned/healthy 世界并封存外部 accepted 窗口；封窗受保护证明 SHA-256 `960817F049630096BE196CD2C7AC1E5B22AA044ADEE37D8175811B04B8350138`。该封窗不重置游戏 revision。

主会话先对不同于卡住目标的两个约4米局部切向候选做只读预览，各有5个 observed、0 unknown、`shoreRisk=not_detected`；只批准其一。Luna 在同一星球 `104`、原档保存点 `77676475` 下 fresh prepare，唯一 `commit_move` 被接受，action `9671205c-f79d-4d07-8574-2e8fab39e363` 于 tick `78053739` 终态 `completed`。后读玩家位置 `(-94.96958,-66.34441,-163.29895)`，距目标约0.90米，`Walk`、速度0、核心能量约799.79MJ。原 Journal 为91条、durableThrough91、无 pending/error；owned session healthy、无 write blocker/checkpoint。外部新窗口 accepted `0→1`。没有第二目标、升级或保存；主档仍是 `77676475`，故当前站位尚未持久化。

私有调用方在上述成功读回后抛出 `READBACK_MISMATCH`，唯一起因是自行加入的 `post revision == 3` 断言；实际从 pre `2` 变为 post `4`。主会话独立从受保护回执核原 action 与 fresh player/Journal/session，而没有重新提交 Move。受保护终态/player/Journal/session 回执的 SHA-256 分别为 `8DD13806B6AB1B16833F8CB4ADD951C0024D4910AB85202B21265DD274B8AAE5`、`26DC9FCFEE52D9B84F6B4849AB9B82D8921C642074523819CBD68E42BBA14F6C`、`6F066F8F5B05D764A6E8DF4610C8D257ABA6EFCE3350068CA2A016F24B221CF5`、`73307E5BB7FA2F48AD854A1E1AA472669E8FF2D916336DB8065EA58D6C7EF9BA`。这些是本机受保护证据索引，不包含原始回执或存档名。源码普通动作路径在执行与完成时分别可能 `IncrementRevisionOnMainThread()`；调用方只能读取实际 revision，不能按预期的 `+1` 校验成功。

结论仅限一次避障短移和误报定位：游戏动作成功，误报不构成 native failure 或第二次试错；它尚不证明能到达目标分拣器、煤/石墨线路接通或持续生产。下一次只在 fresh 现场下继续不同的有界目标，仍逐 action 预检、终态和读回；不重放先前失败的约20米目标，也不把本次表面采样视作全路径无碰撞证明。
