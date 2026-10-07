# Spherewright 当前快照

更新：2026-10-07（Asia/Singapore）。本页覆盖当前事实；本阶段权威回执索引见[持续授权与油路十写审计及续接](evidence/2026-10-07/continuing-oil-supply.md)，较早材料/恢复原索引见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。阶段进度不等于 Gate 2 通过。

## 当前 owned 状态

- 本阶段从源码 `d744b8e6eb5b898b9e9484b2209c30380e40ddda` 构建并部署同批维护 cohort；精确 SHA 的 Windows Core CI `37451961117` 成功。离线候选回归实际计数为 2574；4 个 Plugin 与 224 个 MCP 文件（共 228）同批哈希匹配，MCP source metadata 为 64 tools/1 resource，embedded guide 精确匹配。native `0.10.35.29104` 未变；没有 ZIP、发布或热替换。
- 部署前正常保存的 durable 点为 `97255748/R35/J100`。后续仅关闭 DSP，事务安装同批文件，保留 Steam，再恢复同一个健康的 owned primary；没有做用户已取消的 Host 退出存活专项测试，也未做版本迁移。
- 最新正常保存 99483674/R117/J100；当前窗口 10/10 FROZEN、lifetime 240，无在途或unknown。root独立十写审计 7a0c87061d884e8ca8b623c5b1425a65:1（SHA-256 D6009D7633D4F7EF68C184CE402B8D942358BCA289A34F82FEB385E2C39F1F62）核销10笔唯一accepted全部completed、0 replay/unknown/unpaired；Save覆盖全部10笔，Journal 100/100 durable。窗口已封存。
- 最新全厂capture c38240a45bc74992982d1b3eea4afdf3:1–86（SHA-256 FC833C929CA2836F31DAA2E590FA71C4D59B302A9E37F898E8862E333649B76E）为6188 built/0 prebuild、snapshot tick 99484808。相对6160基线新增6161–6188共28项：pole 6161及belt 6162–6188共27条；0移除、12076条互惠边、0非互反。除预声明的源覆盖外，旧静态差异仅为6154的rotation/connections；6156原输入连接保留且本次rotation未变。
- 同tick物料cut d80925eaf8844d9b89ba4e06b537e37f:1–13（SHA-256 F26BBAE886F7D165B69E001B8624BC0812EEF558D52748AC5FF13362B49D9A90）于tick 99485210读109对象（105 belts、4个既有G sorter）和11条完整Native路径，货物均为空。D族累计105条实际dry belt（上限106）、4个实际sorter（上限14）、3座thermal及2座pole（各自上限3和2；14个sorter累计工作需求上限为4500 J/t（13普通×300、1快速×600））。N3为224 nodes、545 consumers、134 generators且点读full-serve；三台新增thermal仍空燃料、实际capacity/generation均0。source 4450未admit，旧油路5949→3964、H filters、煤源5580及既有generator配置保留。当前库存Fe0/gear0/belt1/normal sorter9/fast sorter1/pole0/thermal0/circuit938/brick144/magnetic coil1，inc0、held null。

## Gate 2 边界

- 整案仍 `executable=false`。1210 既有启动、正常保存、受保护重启及恢复后非零正例仍有效。冷部署 cohort 后已完成一次 fuelPowerState 只读详情核验（13/13 state 为 observed）；这是分时点的库存/发电状态，不是同 tick 物料 cut，也不证明燃烧率、持续功率或净氢/石墨/旧燃料配平。5949→3964现已有短读原生货物流入证据（原件 `a7d27e5a5dc346019e974655f2fb9286:1–15`），但共享供需、有限缓存排除、稳定速率与长窗仍未通过；详见阶段证据。
- 未解决的技术条件仍是净氢/石墨/旧燃料分配、来源接入、共享供需与连续供给验收，整案 executable=false。到油后的三段间隔600-tick诊断仍是continuous credit 0；补充单点读回不能补成连续证据，批次输出接纳堵塞也不是Overseer finding。source 4450仍未admit，三台新thermal燃料与实际capacity/generation均为0；旧配置保留。九个普通接缝已通过prepare-only Native资格（`8886c579d68747ebb08f7d60239596b9:1–53`，9/9 positive、0 commit）；准确SHA绿CI并由root重开后，下一有限阶段为九普通接缝施工并将正常Save留作第10笔封窗动作。fast source admission留待后续窗口并先核下游；实际供料和持续验收仍未通过。
- 2026-10-07目标范围持续授权已写入[当前规范](../AGENTS.md#阶段规划与证据)：root可批准直接服务0.4的有限取材/制作、合法输送片、过滤分配、候选重设计、最小修复与必要同批维护，并在准确SHA绿CI及独立十写审计后重开下一窗口。每片仍须fresh原生prepare及全部硬检查；不扩大任意恢复、另一世界或正式发布权限。
- 点库存、短窗P/C、单台机器状态、额定能力或单个原生prepare正例，都不能替代稳定产率及材料/功率预算。R16b三thermal及一pole的单体Native正例未验证其同时存在时无碰撞；R29主干与三tap的实际施工及拓扑核销也不证明source 4450接入或持续供料。原油实际到达、共享供需与≥36000-tick验收分别记录，不合并为Gate通过；历史候选、最新阶段差异和下一阶段边界见本阶段事件。
- Steam 保留运行。除本阶段正常保存与必要 DSP-only 维护恢复外，没有关闭 Steam、热替换、重启其他进程或运行 Host 专项测试。
