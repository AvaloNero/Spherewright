# 紫糖源端西侧带段覆盖与正常保存

日期：2026-10-01（Asia/Singapore）。同一 owned session、planet 104、DSP 0.10.35.29104。本事件记录 2001 带段覆盖与保存闭环，不宣称完整供应链或全厂状态通过。

## 施工与保存

施工 run `e6c457b0f9044c5880fa268a0c6a6c69`：ord20 prepare、ord22 commit、ord27 terminal；action `5402b3d1-4b65-43b1-8d91-76cd6686cbfb` completed/succeeded，tick 81944295。使用非移除式双带覆盖 `5205→5219` 间的几何：新建 `5229→5228`，接入既有 `5219–5227` 带段。

正常保存 run `4e8d5b0d35aa40c1a7e4c0903fc78930`：ord4 prepare、ord6 commit、ord7 terminal；action `819285a7-6347-45f0-823d-12e1e0d97e55`，save tick 81944570。ord8 为同一 owned identity、healthy、peaceful confirmed、sandbox confirmed_disabled、1×，`resumeAvailable=true`；观察 tick 81944582/R22。Journal J96 durable，96条 exact unchanged、无 pending/error；这不等于真实重启已验证。新外部窗口 accepted **3/10 OPEN**，无在途。

## 选定对象和材料差异

root 审计 run `22bf91570e0946f89d70517177b6b251` ord1 `source-west-cover-root-audit`，证明 SHA-256 `A8584783423BC0EBA27A90B54AB0981DF67A48B71DC9DD59CEC3A32957BE2F42`。这是对17个选定对象（15旧、2新）的读回审计，不是整厂一致快照；所选拓扑含32条有向连接，均成对互返。局部链为：

`5202→5201→5200→5204→5203→5205→5229→5228→5219→5220→5221→5222→5223→5224→5225→5226→5227`

唯一获准的静态差异是 `5205`、`5219` 上经原生核验的 connections/rotation 变化；带段 `5219–5227` 的 belt `pathId 202→198` 是单独记录的运行时路径变化，不属于静态字段差异，也不是对静态字段的通用豁免。背包 `2001` 为 `343→341`，其他物品数量与增量无变化。`5202` 当前没有供料，尾端 `5227` 为 free；这段路径未证明有物料流。

## 接口边界与调用方核验

只读预检 run `00788a35f3f745c081b7bd080bda84e0` 对 `5203→5219`、sorter item `2011` / filter `6004` 返回 `BUILD_CONNECTION_INVALID/TooSkew`：16 seeds、nativeChecks=0、admittedSeeds=0、bestFacing 未知，无 plan/commit。这仍是有效负例。与之不同的是，本次 `5205→5219` 的 item `2001` 双带非移除式覆盖通过并已保存；不能用它冒充 `2011` sorter 接口通过。旧边 `5204→5203→5205` 与 `5219→5220` 保留。孤立高层前缀 `5206→5208→5207→5209` 与消费者坡道保存前缀未重做。

本阶段已核的调用方注意点：`PreparedAction` 不回显私有 endpoint hashes；`confirmed_disabled` / `confirmed_peaceful` 是字符串枚举，不可用 truthiness 伪造 boolean blocker；缺失子 `saveJSON` 的 accepted 状态是未知，不能计作0。相关 `pwsh -File` smoke 与9个离线 plan fixtures 已由 root 验证；本次17对象审计为0游戏调用，不等于全套测试。

入口至 prepare 为 2.282 秒、action 阶段 4.845 秒、save 子调用 1.502 秒，均是本次 caller 计时，不包括委派、总验收、Git 或 CI。

仓库 `3051` 向 `5202` 供料、抬高主干、紫糖到货、科研推进、持续吞吐及真实重启仍未证明。下一工作是 root 重新设计上游供料方案；不重做成功前缀，也不把已拒绝的 `2011` sorter 候选改角度重试。
