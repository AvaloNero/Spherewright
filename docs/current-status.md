# Spherewright 当前快照

更新：2026-10-07（Asia/Singapore）。本页覆盖当前事实；本阶段权威回执索引见[持续授权与油路十写审计及续接](evidence/2026-10-07/continuing-oil-supply.md)，较早材料/恢复原索引见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。阶段进度不等于 Gate 2 通过。

## 当前 owned 状态

- 本阶段从源码 `d744b8e6eb5b898b9e9484b2209c30380e40ddda` 构建并部署同批维护 cohort；精确 SHA 的 Windows Core CI `37451961117` 成功。离线候选回归实际计数为 2574；4 个 Plugin 与 224 个 MCP 文件（共 228）同批哈希匹配，MCP source metadata 为 64 tools/1 resource，embedded guide 精确匹配。native `0.10.35.29104` 未变；没有 ZIP、发布或热替换。
- 部署前正常保存的 durable 点为 `97255748/R35/J100`。后续仅关闭 DSP，事务安装同批文件，保留 Steam，再恢复同一个健康的 owned primary；没有做用户已取消的 Host 退出存活专项测试，也未做版本迁移。
- 最新正常保存 `99154541/R62/J100`；最新全厂读回末观察 `99155715/R62`。当前窗口external为10/10 FROZEN、lifetime为210。root独立十写主审 `b89f5ff0426a4a52877381b79affc667:1`（SHA-256 `42FAE7F05D0B03F592E848E7090AF2C9AE635547D342660602B5E2610F2D9C04`）确认10个唯一accepted均completed、无replay/unknown，Journal 100/100 durable，Save覆盖全10笔；7条新G端带均为空货、旧配置和电网成员保留，库存/inc/held守恒。当前窗封存，后续写入仍等准确SHA CI及root明确重开。
- 电塔 `5946/5947/5948` 与采油器 `5949` 保留，无重建。新源现已通过source sorter `6074`（`2011/filter1007`）接入既有`5960`，油路经原有主路及新增带到consumer sorter `6067`（`2011/filter1007`），接入`3964/r16`的slot 1。油路累计123条belt（保守上限164，余41），两个sorter预算均已使用；源端首次接通时玩家余1条belt、5个sorter。最新R6材料包后玩家库存为Fe0、gear0、item1102为0、Cu0、coil1、stone144、belt60、2011×5、2012×1、2201×2、2204×3；旧`4193`及其它静态配置保留。
- 最新全厂捕获 `139066f11dec4025ae706ac364138148:1–85`，seal SHA-256 `35538FDEB5547F97C065101FEA8358363ED33F38D2CAAE955614FCD44AB44CEB`：61页、6081 built、0 prebuild；snapshot `99155347`，末观察 `99155715/R62`，Save `99154541/J100`。root投影 `f83521a2de7543979ace10b6b50d2fab:1`（SHA-256 `252C39B0A580EEBDB12D0E36C7455B319E3E1C4E23B85E67A33A48FEECD0ECB3`）相对6074-built旧基线核销新增6075–6081共7项、0移除、0静态变化、175项正常动态变化、11882条连接/0非互反。旧N3/N4电网成员保持且full-serve；完整库存/inc/held均已核对。详见阶段事件。

## Gate 2 边界

- 整案仍 `executable=false`。1210 既有启动、正常保存、受保护重启及恢复后非零正例仍有效。冷部署 cohort 后已完成一次 fuelPowerState 只读详情核验（13/13 state 为 observed）；这是分时点的库存/发电状态，不是同 tick 物料 cut，也不证明燃烧率、持续功率或净氢/石墨/旧燃料配平。5949→3964现已有短读原生货物流入证据（原件 `a7d27e5a5dc346019e974655f2fb9286:1–15`），但共享供需、有限缓存排除、稳定速率与长窗仍未通过；详见阶段证据。
- 未解决的技术条件是净氢/石墨/旧燃料分配、完整整链预算与连续供给验收，整案仍 `executable=false`。root独立短诊断run `a2820be3fd314980aeb65232a9605483` 原件索引 `:1–70`，完成记录 `:70` 文件SHA-256 `BD4C168AA82C7B799847C00BE0DA58DF564B0E018271CDBDD1C967646B79B470`；审计 `69d1142aca204befa042d8a460fcacc2:1` SHA-256 `24D69F2718B6B36D1F813D23CAB3EDE27A9EF8FAE3A7B82012207A5E2E978DD8`。诊断覆盖3个有间隙的600-tick窗口，continuous credit为0；暖机后的补充run `e5241bdbdf7544c1add160ca5db56bc4` 原件索引 `:1–22`，完成记录 `:22` 文件SHA-256 `17DA7F35B62EE64E745FDAFF0A92C4308062BEAF421508F6EBC5AD39EAD99D74`，未改变Save/Journal/窗口计数。各物料P/C、28对象同tick库存cut和分时fuel读数见[阶段事件](evidence/2026-10-07/continuing-oil-supply.md)。3965/3966 r58原生批次输出接纳堵塞判断来自root核验，不是Overseer finding。D族已建source端3条和consumer端4条空带；主干、到料、新发电及完整Native供需验收未完成，continuous ≥36000 ticks仍未通过。最新备料/审计边界见阶段事件。
- 2026-10-07目标范围持续授权已写入[当前规范](../AGENTS.md#阶段规划与证据)：root可批准直接服务0.4的有限取材/制作、合法输送片、过滤分配、候选重设计、最小修复与必要同批维护，并在准确SHA绿CI及独立十写审计后重开下一窗口。每片仍须fresh原生prepare及全部硬检查；不扩大任意恢复、另一世界或正式发布权限。
- 点库存、短窗 P/C、单台机器状态、额定能力或单个原生prepare正例，都不能替代稳定产率及材料/功率预算。历史原件 `1f72839a89e342ef9e2d830361ea2b36:9,:12,:15` 保留6042→6054的2带gap、6060尾延4带和3964 consumer tokenless preview正例；preview完整请求4个NEW belt点，原生 `nativeSpan=2`。focused continuation `8339955b569e4f2da16b8183afc8cb0a:1–11`当时仅新fresh准备了5949 source-port 6带并复核健康/consumer边界，同时离线核销此前三项结果和调用方重吸附差异，没有重跑已通过候选。差异为0.0000161236m，小于调用方几何对应容差0.0001m；原生check通过，该数值不是DSP声明的原生容差。其后施工、实际到油与短诊断见本阶段事件记录；诊断窗口有间隙且continuous credit为0，不替代≥36000-tick验收。备料后的候选已收敛至有界D族但尚无新G施工、filter调整、完整Native资格或持续验收；后续fresh资格与供需门仍按阶段事件记录。
- Steam 保留运行。除本阶段正常保存与必要 DSP-only 维护恢复外，没有关闭 Steam、热替换、重启其他进程或运行 Host 专项测试。
