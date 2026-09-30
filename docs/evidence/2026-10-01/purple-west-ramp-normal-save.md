# 西侧九带正常保存与源端接口边界

日期：2026-10-01（Asia/Singapore）。同一 owned session、planet 104、DSP 0.10.35.29104。此事件接续已独立审计的十写窗口；只记录正常保存闭环和仍未通过的源端接口，不表示紫糖供给或科研恢复。

## 正常保存闭环

受保护 run `e0a7ad3dc0f24fd7bd42455271580d70`：ord6 为唯一 commit accepted、非 replay；同 action `cf916948-32f6-465b-a193-27e0c696479d` 于 ord7 terminal completed/succeeded，save tick 81820689。ord8 session 为同一 owned identity、healthy、`lastSave=81820689`、`restartAvailable=true`；最新观察 tick 81820702/R19。该次正常保存覆盖先前未保存的西侧九个带实体 `5219–5227`。这表示其已保存，不表示真实重启恢复已验证。

Journal 前后 identity 匹配；96 条记录 exact unchanged、durable96、无 pending/error。root 在上一十写审计、文档提交和绿 CI 后明确交接开启新外部窗口；当前 accepted 为 **1/10 OPEN**，无在途。保存入口耗时约 1.53 秒（其中 action 内约 480 ms）；这是本次保存调用口径，不包括委派、独立验收、文档或 CI，也不证明实际重启。

## 尚未闭合的源端接口

只读预检 run `00788a35f3f745c081b7bd080bda84e0` ord1–8（prepare ord6）对 `5203→5219`、filter `6004` 返回 `BUILD_CONNECTION_INVALID` / `TooSkew`。16 个 seed、nativeChecks=0、admittedSeeds=0；bestFacing 未知；没有 plan、commit 或 accepted。原连接 `5204→5203→5205` 与带段 `5219→5220` 均保持，但这不构成两者已连通。西侧九带仍是一段两端 free/free 的带段；不得放宽原生角度、重试同一候选或声称全线路通过。root 正在重新设计源端接口。

因此，当前只证明西侧带段被正常保存；还未证明 `5203` 源端接入、紫糖到货、科研推进、持续吞吐或真实重启恢复。此前完整十写差异仍见[西侧九带施工与十写独立审计](purple-west-ramp-ten-write-audit.md)。
