# 紫糖消费者分拣器接口与正常保存

日期：2026-10-01（Asia/Singapore）。同一 owned session、planet 104、DSP 0.10.35.29104。本阶段把已建消费者六带接入既有消费者端，并正常保存；不宣称紫糖已送达、科研已推进或真实重启已验证。

## 接口施工与选定对象读回

sorter 只读预检 `a401d2974df94749946ea134b2ae6dad` ord6 返回 prepared/allowed、`exact_slots`，虚拟槽位 `-1→-1`、偏移0、filter `6004` / item `2011×1`。预检结果本身不是施工。

施工 run `6d84fe3eb57f40439719a7701e3417d3`：ord5 prepare、ord7 commit、ord17 terminal；action `cc3bc662-2292-4798-b2c4-c7ecb24edd2c` completed/succeeded，tick 82109235。ord18 新 sorter `5236` 配置为 item `2011` / filter `6004`，连接 `5235`→`5217`，network3 / ratio1。实际 slot 读回确认 `5235` slot4 与 `5217` slot4 双向唯一连接，共4条新增有向连接且互返；`5234→5235`、`5217→5216` 原边保留。ord19–20 两个旧端点与基线配置一致，仅各自增加本次接口边。

玩家库存 ord4→21：item `2011` 为 `6→5`，`2001` 保持335；其他物品数量及增量均未变化。root 的独立审计 `e4f33199fe6c40cb8d22f0a50feb0a4c` ord1（SHA-256 `D57354AE4420EE6FBFDA103550C2F2A1001B65692664BBE08374E7CC3C16EB5C`）覆盖3个选定对象，不是整厂 census。

## 供电边界

新鲜只读证据 `50bca349dec3411db7bcfa88f26fe929` ord1–4 记录风机 `2203`、network3，距实际源点6.911m、原生 cover 7.7m；这是风机，不是热电。Foundry advisory `1a6de0203eee45edb00d5e9f3b02b327` ord1 使用代理目标 `1101` / 铁炉 `2302`，因与既有 `303` 碰撞且缺炉而标记 `executable=false`，不是实际炉、分拣器或全链 preflight。其整网采样 peak 1,580,700、capacity 1,894,000、export0；预留剩余6个 sorter×300=1,800 后算术余量311,500。root power proof `b6dbfd6a2b7c474886710b56a4a24ae1`（SHA-256 `5443C4BD0FF7668CC788744F3C73AB0BB8DBB347EF2EFDAE9CA0467499316B31`）仍只是采样证据，不证明持续燃料或持续供电。

## 正常保存与写窗

正常保存 run `b08ae5b1bf7d4cb9a724fec2baf3f870`：ord4/6/7/8/9；action `588deb6f-b3dd-4163-a382-e731bc90d056` 保存 tick 82109940。ord8 最新观察 tick 82109952/R27，同一 owned identity、healthy、peaceful、sandbox disabled、1×、`resumeAvailable=true`；该标志不等于真实重启验证。J96 durable，96条 exact unchanged，无 pending/error。当前外部 accepted **6/10 OPEN**，无在途。

本入口复用既有 NormalAction 模板，参数化 belt destination、最后读取 fresh hash 并扫描实际 belt slot；AST、实际 `-File` smoke 与11个原有 plan fixtures 已核，game calls=0。异常时保留 accepted/unknown，不重放；本段不改公开 MCP surface。

下一 blocker 是尚未连接已保存的源端尾 `5227` 与消费者头 `5230` 的 raised 主干；其后才是仓库 `3051→5202` 供料。紫糖到货、科研推进、持续产出和恢复回归仍未证明；不得重做已保存的成功前缀。
