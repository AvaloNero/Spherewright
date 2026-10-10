# R839：有限移动停止、窗口封存与流程核验

更新：2026-10-11（Asia/Singapore）。本页记录一个固定 20 写窗口的收尾与其流程时间，不扩写普通只读里程碑，也不改变既定来源验收门。

## 封窗结果

R839 独立审计确认，窗口由 lifetime `454` 开始，最终为 `16/20` 笔 accepted、lifetime `470`。15 笔原动作成功，另 1 笔 accepted Move 以 `position_stalled` 结束并被唯一核销；未用 4 槽退役，不转给后续窗口。普通 Save tick `107238380`、revision `79`、durable Journal `102` 覆盖全部 16 笔原动作。当前无在途或未知动作，写入状态健康且冻结。

完整工厂为 `6492 built / 0 prebuild`。与 R802 基线相比，新增、移除、静态差异和非互惠连接均为 `0`。审计只覆盖 P104 本地三个电网并确认满供。玩家包中的 Ti Ore `50`、Ti Ingot `0`、Warp `400`、Rod `5`、Stone `6` 保持；Ti Ore、Ti Ingot、Warp 取料均为 `0`，短采样未启动。

## 阶段经过

- R827 对超过 32 m 的 Native 地表预览在零 accepted 时停止；R830 对水面字段的本地解析问题也在零 accepted 时停止。
- R834 的普通 Move accepted 一次，之后以 `position_stalled` 终止。R835 对原动作只读，R836 完成独立核销。该失败动作计入窗口，不重放。
- R837 完成普通 Save 和 65 页完整工厂读取，R839 独立核验窗口封闭。失败目标及此前 orthogonal/microshift 候选族维持退役。真实 Native DTO fixture 和零游戏调用 `-File` smoke 核验了调用入口的字段解析与边界；这不代表现场失败动作成功。

批准事实摘要 `r839-approved-facts.json` 的 SHA256 为 `E2F1B3E65C1C2F9931AE1433B8A3387805371EA8BF729206DA75BCDF85B0A359`。独立审计引用：`0299d7dfe49841f0b44eefb14c697743:28`，SHA256 `A7CDA57410C4FE20156DA3DEFC08A2F27ACA0EB118BFA48CBB2361E8EBB9389E`。原 Save 引用：`0ec134df70c042bba1167f1c127939bb:87`，SHA256 `3DDA011709D6D6E073BCD608623ABEE82DCA4F08F9D27934BA17766B9D35FB52`。原 Move reconciliation 引用：`0299d7dfe49841f0b44eefb14c697743:26`，SHA256 `EA9D239638926E0C4D68955987BB9D3EE2275F354D6E1E0F66B5EF5BE2E807D5`。

## 流程计时与剩余门槛

不同端点的阶段墙钟间隔分别为：R813 审计结束至下一次消费者读取声明 `901.7697889 s`；最近 fresh audit 至首个业务 prepare `1665.2289724 s`；批准至首个 prepare `86.5060243 s`。它们不是命令运行时长，也不构成严格同比。单项计时为 fresh 读取 `3879.7172 ms`、Save 与完整工厂读取 `27976.2434 ms`、独立审计 `27642.6247 ms`。单 writer、固定窗口、拓扑缓存复用、采样等待无模型决策、事实 CI 不阻挡未变 cohort 的并行阶段均保留；但端到端提速目标未达到，准备和交接仍耗时。本阶段没有启动短采样。

`sourceConditionsPassed=false`、`wholeSupplyPassed=false`，continuous credit 为 `0`。R783 连续来源长窗未通过；R807 的短窗仍只是有限供料观察，不能替代来源门。相关失败与未通过边界保留，不能将移动、储存库存或本次封窗当作来源/持续供给通过。下一阶段须基于已核验几何和实际可用的正常移动接口 fresh 资格化结构不同的有界路线；本页不表示本地 flight 能力存在，也不预先批准任何施工。完整持续来源、钛链、Warper/Rod 门、双自动补给、整合保存恢复和最终同 SHA 双候选仍待完成。
