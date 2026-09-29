# 普通保存快路径与第十写审计

日期：2026-09-30（Asia/Singapore）。同一 owned 世界、母星 `104`、已安装 DSP `0.10.35.29104`；没有冷部署、换档、施工或额外游戏写入。上次封窗后本批外部 accepted `#1–#9` 分别在[煤区接近与升级](coal-historical-approach-sorter-upgrade-save.md)、[科研需求测试](coal-backpressure-and-idle-research.md)、[保存与同档恢复](research-demand-save-resume-two-windows.md)、[科技1126与保存](research-1126-caller-and-save.md)记录，均有唯一终态；两次手拼 commit 明确拒绝，计数为零。第十笔之后立即冻结写入。

## 第十笔：正常保存

Luna 复用已验证的普通动作客户端而没有另写 caller。fresh 同档身份、healthy、J93 durable 且无在途异常后，仅一次 `prepare_save → commit_save → 同 action terminal`；action `cffde2f6-7834-4956-9b7b-0d5939e8044f` 在 tick `79200989` 为 `completed/succeeded`。主会话独立解析受保护 run `5c06d2428d3741f08e09d4a837842589` 的 `0006–0008`：主档保存 tick `79200989 / R5`，仍 owned/healthy，protected resume available，Journal `93/93` durable、无 pending/error。终态原记录 SHA-256 `3C1DF9A135875B7FBDFF8977BB86E17F05FDD36E5F47DEEF0A3B1A9F3D0074C1`。没有第十一笔或重放。

调用方记录首个 Bridge 请求 `485 ms`，身份/Journal 预检合计 `644 ms`，prepare/commit/terminal helper 合计 `433 ms`，fresh 读回 `232 ms`，总计约 `1.327 s`；helper 内三阶段未单独计时。这个数只覆盖保存调用方，不含此前诊断、主会话决策、十写审计、文档、Git 或 CI，不可当作端到端工序用时。

## 独立严格审计

主会话从受保护原回执核对本窗口十个唯一动作：`#1` Move、`#2` sorter upgrade、`#3` save、`#4` tech1125 selection、`#5` save、`#6` save、`#7` exact-primary protected resume、`#8` tech1126 selection、`#9` save、`#10` 本次 save。前九笔的终态分别见上列已提交的阶段证据；本次未重跑旧动作或重新采集封存历史。没有 outcome unknown、quarantine、重复 accepted 或未核销的 commit-intent。

审计开场为同一 owned/和平/非沙盒/1×世界，planet `104`、saved tick `79200989 / R5`、健康、无 write blocker 或 flight checkpoint；玩家 `Walk/0`、核心 `800 MJ`、手搓队列空、施工无人机 `3 idle / 0 working / 0 pending`。两张电网 consumer ratio 均 `1`。J93 全部 durable、无 pending/error；与较早 tick `79123151` 的保护玩家读数相比，背包格数和各格物品/数量/inc 不变。

同一不可变工厂 snapshot tick `79223331` 的 52 页读得 `5170` 个唯一 built、`10078` 条有向连接，反向端点缺失 `0`；另一次 prebuild 读得 `0`，该开场/收尾审计没有工厂写入。数量与前次封存总数相同，但这**不是**逐对象静态哈希零差异声明。目标装置全部满供电：裂解厂 `3083/3084` 氢输出仍为 `60/58`，石墨烯厂 `869` 石墨输入 `0`，磁环厂 `3404` 石墨输入 `0`，氘棒厂 `3403` 磁环输入 `0`，宽带厂 `2255` 纳米管输入 `0`，紫矩阵研究站 `4743` 宽带输入 `0`。审计证明结构与安全状态，没有核销持续供料或 0.4 产线门。原工厂页和 prebuild 回执同属受保护 run `f1e3667ca3ae41a585ebe85996812716`，首/末记录 SHA-256 分别为 `DCCA4E898319D808D3C3C5E4A1DDC8DD9EB87C40351FBC9DA6C69DDEB36D6324`、`B280B70080A6B2E455DBDF304BC83E20367A27A2FC936C5D25CE1A4BB7EBD603`；直接采集/逐边核算约 `22.184 s`，不含阅读历史或撰写证据。

审计收尾 tick `79229282` 仍为同一 saved tick `79200989 / R5`、healthy、零 blocker，J93 durable；科技1126仍在队列，hash `236162/240000`，尚未解锁。审计通过后仍须完成本证据的单一目的提交、推送与绿 CI，由主会话明确封窗，之后才可启动新 accepted 计数。

## 同一快照的离线追源与下一缺口

第十笔前的有界只读追踪发现磁环输入带 `3428` 仅偶见一件石墨，连接 `3428→3436→3404` 与供电虽正确，却未证明持续送料；附近 `4223` 实为火电燃料支路，不是独立供源。逐带定点复读到 `3424` 后，Luna 按候选时间界限停止，没有盲造带、清氢仓或重复失败目标。

主会话随后**没有再请求游戏读取**，而是复用本次十写审计的 52 页同一不可变工厂 snapshot，在本地沿 `3428` 所在路径的有向入边遍历 151 个对象、核反向连接，到达四个非带生产源：`3083`、`3084`、`3965`、`3966`。四者全是通电的 recipe `58` X 射线裂解厂，石墨输出均 `0`，氢输出分别 `60/58/58/59`；没有发现这一路径上的独立煤石墨源。解析并追图约 `8.2 s`，而非对每一根带逐次 Bridge 往返。这证实磁环现有石墨输入仍在氢背压的循环依赖内；同 tick 拓扑证据不等于对另一候选的 fresh 原生施工许可，也不能单凭一件带货判断停滞时长。

下一业务缺口缩成：为磁环 `3404` 建立经真实原生预检的**独立**石墨来源，或先证明另一可持续氢消费路径，打破裂解厂氢背压；两者都要核下游真实需求、完整路线、供电、材料与保存后持续产出。已有煤石墨炉 `2986/2989` 各有 `100` 石墨输出、仓 `114` 在该快照有 `3000` 石墨，只是可用物料证据，不是现有路线已接通或自动化修复完成。下一先做有限源/端口/路线预算，不能把本次十写审计称作持续供料通过。
