# 石墨出炉短带：已建并保存，旧石墨带并入口尚未通过

日期：2026-09-30（Asia/Singapore）。沿用同一 owned-world-001 / planet104 / DSP 0.10.35.29104。上一阶段主档为 tick 79954353 / J95，本阶段不载入其它存档，不拆改旧带或重放已成功动作。原始敏感回执只留在本机受保护证据目录；本文不含真实存档名或计划凭据。

## 需求与位置

新炉5187在配方17下已见煤输入4、石墨输出33，但无出料；旧磁环3404的石墨输入为0，其现有过滤分拣器3436从旧石墨带取1109。全厂已封存快照证明旧带3362→3365→…→3436→3404的有向路径。此阶段只建炉南侧短带，不把尚未接入旧线的库存计为磁环供给。

主会话先只读试探炉5187→旧带3362的2012直连，run `25586837733142f4892797ba059069f9`：`BUILD_CONNECTION_INVALID / TooSkew`，零提交。随后精确4点自由带原生预检 run `b353381907714705bafaede3869cef34` 为 `full_path_stage1 / native_grid`，新4带、2001×4、零blocker。预期点为 `(-163.040924,-13.8258171,-115.353973)` 至 `(-165.1862,-13.8258171,-112.260422)`，中间两点按原生结果固定。

## 唯一施工、负例和保存

Luna 在受保护 run `a30f086a528f44ec802988aa79457dd5` 重新取得 fresh 状态。第一次本地展示误查不存在的顶层 `beltPathMode`，但已收到的原生正例位于 `plannedBeltPath.routingMode`；零提交。修正后 fresh prepare 与原四点、预算和模式一致；唯一 `commit_build` action `066bd53d-cb21-4ff9-a319-01c663805f53` 于 tick 79980021 terminal/succeeded，实体5189–5192逐段读回互连，带库存378→374。原始 prepare→commit 约0.25秒，commit→terminal约2分15秒；不能把无人机施工时间算成接口准备时间。

施工后原生预检炉5187→首段5189的普通2011正例：`exact_slots` 炉slot7→带虚拟槽-1、过滤1109、成本2011×1，但**未提交**。末段5192→旧带3362被 `BUILD_CONNECTION_INVALID / TooSkew` 拒绝，stage `no_admissible_seed`，零原生placement checks。主会话审视姿态后发现3362是南北转东西的转弯段，另 fresh 试探同一直线旧带3365，run `01bfdd521b5a48a2a9edc59b03f619ba`，仍被 `BUILD_CONNECTION_INVALID / TooSkew` 拒绝，但阶段是 `no_finite_projection`：16种种子通过，零有限路径投影、零原生placement checks。两次旧带接口候选失败后停止同类试探；“平行且中心距离约2.5m”不构成原生分拣器可装证明。新短带终点与旧直线段的可投影区间尚未对齐，需主会话重设计，不能暗中跨越朝向门。

为保全唯一成功前缀，正常保存 action `d2134c98-e402-4dfb-ae3a-55d6bf6efe8e` 于 tick **79986033** terminal/succeeded；随后 R14、owned/saved/healthy、J95 durableThroughSequence95、persistencePending=false、0 prebuild。只有短带消耗4×2001；无分拣器消耗。此窗从 accepted7 到 accepted9，尚未第十写。短带仍为空的中继，不声称持续石墨/磁环/燃料棒生产或保存后重启已验。

本阶段从首次受保护 Bridge 读 11:02:04 到保存后读 11:14:00 约12分钟；其中建造约2分15秒，局部字段核验及两端几何诊断、交接占其余时间。下一步应先基于现有5192自由尾和旧带直线段的原生 path interval 设计**追加**的有限带段，并先核可投影端点；不得修改已成功5189–5192或连续盲试第三个同类旧带接点。任何新增带仍须 fresh 完整预检、唯一提交与逐对象读回，第十写后立即冻结审计。
