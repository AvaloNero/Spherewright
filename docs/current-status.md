# Spherewright 当前快照

更新：2026-10-07（Asia/Singapore）。本页覆盖当前事实；本阶段权威回执索引见[持续授权与油路十写审计及续接](evidence/2026-10-07/continuing-oil-supply.md)，较早材料/恢复原索引见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。阶段进度不等于 Gate 2 通过。

## 当前 owned 状态

- 本阶段从源码 `d744b8e6eb5b898b9e9484b2209c30380e40ddda` 构建并部署同批维护 cohort；精确 SHA 的 Windows Core CI `37451961117` 成功。离线候选回归实际计数为 2574；4 个 Plugin 与 224 个 MCP 文件（共 228）同批哈希匹配，MCP source metadata 为 64 tools/1 resource，embedded guide 精确匹配。native `0.10.35.29104` 未变；没有 ZIP、发布或热替换。
- 部署前正常保存的 durable 点为 `97255748/R35/J100`。后续仅关闭 DSP，事务安装同批文件，保留 Steam，再恢复同一个健康的 owned primary；没有做用户已取消的 Host 退出存活专项测试，也未做版本迁移。
- 最新正常保存 `98676748/R28/J100`；封窗后只读观察 `98726758/R28`。external `10/10 FROZEN`、lifetime `190`；本窗十个唯一accepted均completed、无replay/unknown/在途，正常Save覆盖全部十笔。受保护健康恢复票据可用；只读资格检查新增accepted为0，未重置游戏计数。
- 电塔 `5946/5947/5948` 与采油器 `5949` 保留，无重建。本窗新增91条油路belt，累计111/保守上限164，玩家余13；111条belt空货来自各施工阶段逐项实体读回，封窗后的fresh全厂快照只核了拓扑与位置。路径为已核104点主路与独立7点下降。5949仍未连接或放料至3964；新sorter forecast为2，但实际实体ID和fresh prepare尚未输出。旧3964/4193及其它静态配置保留。
- 最新全厂捕获 `c4b50b48c0b1424590f90b40d2e9f8c9:1–82`：snapshot `98680938`，61页/6060 built/0 prebuild/11838互惠边。相对5969基线恰新增5970–6060共91项、无删除、0坏边；三项静态差异为2440铜矿成员子集减少167以及5950 native cover旋转和新增输出6045。完整67节点铜矿目录保留162/164/165/171、167已消失；与正常矿竭相容，不归因历史操作者，未解释差异为0。root独立十写审计通过；详见本阶段事件记录。

## Gate 2 边界

- 整案仍 `executable=false`。1210 既有启动、正常保存、受保护重启及恢复后非零正例仍有效。冷部署 cohort 后已完成一次 fuelPowerState 只读详情核验（13/13 state 为 observed）；这是分时点的库存/发电状态，不是同 tick 物料 cut，也不证明燃烧率、持续功率或净氢/石墨/旧燃料配平。新油源 `5949` 尚未连接，整链持续供料与完整有限缓存排除仍未通过；详见阶段证据。
- 未解决的技术条件是新油源5949到3964的实际连接、净氢/石墨/旧燃料分配与完整整链预算，整案仍 `executable=false`；现有 cut、prepare-only 资格结果及预算边界见[2026-10-07 事件记录](evidence/2026-10-04/material-inventory-cuts.md#2026-10-07-net-hydrogen-and-graphite-budget-blocker-prepare-only-close)。这表示尚未验收，不表示没有用户授权。
- 2026-10-07目标范围持续授权已写入[当前规范](../AGENTS.md#阶段规划与证据)：root可批准直接服务0.4的有限取材/制作、合法输送片、过滤分配、候选重设计、最小修复与必要同批维护，并在准确SHA绿CI及独立十写审计后重开下一窗口。每片仍须fresh原生prepare及全部硬检查；不扩大任意恢复、另一世界或正式发布权限。
- 点库存、短窗 P/C、单台机器状态、额定能力或单个原生 prepare 正例，都不能替代完整 source-to-consumer 连接、稳定产率及材料/功率预算。原件 `1f72839a89e342ef9e2d830361ea2b36:9,:12,:15` 分别保留6042→6054的2带gap、6060尾延4带和3964 consumer tokenless preview正例；preview完整请求4个NEW belt点，原生 `nativeSpan=2`。focused continuation `8339955b569e4f2da16b8183afc8cb0a:1–11`只新fresh准备了5949 source-port 6带并复核健康/consumer边界，同时离线核销此前三项结果和调用方重吸附差异，没有重跑已通过候选。差异为0.0000161236m，小于调用方几何对应容差0.0001m；原生check通过，该数值不是DSP声明的原生容差。当前12带仍是forecast，0新accepted。下一阶段须在文档提交、准确SHA CI成功和root重开后，施工2带gap、4带尾延及实际consumer sorter；source-port 6带与source sorter最后处理，两个sorter的实际ID与各自fresh prepare仍待当阶段输出，SourceSort实际能力未证明。随后才可最后放料并核验原油实际进入3964；共享供需和≥36000-tick长窗证据仍待实际验证。
- Steam 保留运行。除本阶段正常保存与必要 DSP-only 维护恢复外，没有关闭 Steam、热替换、重启其他进程或运行 Host 专项测试。
