# Spherewright 当前快照

更新：2026-10-07（Asia/Singapore）。本页覆盖当前事实；本阶段权威回执索引见[持续授权与油路十写审计及续接](evidence/2026-10-07/continuing-oil-supply.md)，较早材料/恢复原索引见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。阶段进度不等于 Gate 2 通过。

## 当前 owned 状态

- 本阶段从源码 `d744b8e6eb5b898b9e9484b2209c30380e40ddda` 构建并部署同批维护 cohort；精确 SHA 的 Windows Core CI `37451961117` 成功。离线候选回归实际计数为 2574；4 个 Plugin 与 224 个 MCP 文件（共 228）同批哈希匹配，MCP source metadata 为 64 tools/1 resource，embedded guide 精确匹配。native `0.10.35.29104` 未变；没有 ZIP、发布或热替换。
- 部署前正常保存的 durable 点为 `97255748/R35/J100`。后续仅关闭 DSP，事务安装同批文件，保留 Steam，再恢复同一个健康的 owned primary；没有做用户已取消的 Host 退出存活专项测试，也未做版本迁移。
- 最新正常保存 99543357/R136/J100；当前窗口 10/10 FROZEN、lifetime 250，无在途或unknown。root独立十写审计 `6a0e6fbadb66424383db65752bac1723:1`（SHA-256 `995F795247D36627C09D769A1AF368CD7BDA2E4D2C16D3EADC7A2D78A265DF63`）核销10笔唯一accepted全部completed、0 replay/unknown/unpaired；Save覆盖全部10笔，Journal 100/100 exact。窗口已封存。
- 最新完整capture `69a80ac0b06047debea4896def063c17:1–86`（完成记录SHA-256 `3FC0570C98B01183570A35CAEB75B834BE4EF21C78A6AFC611250AE8092C630E`）为6197 built/0 prebuild、tick `99544765`。相对6188基线新增 sorter `6189–6197` 共9项，无移除；共12112条有向边、0非互反，旧拓扑只有预期的18个互惠端点连接新增。
- 同tick物料cut `86ad1e80b7214366ab53825cdc0b0ea4:1–13`（SHA-256 `66B19B4364FA4A182F047F54D605CFFE0157B320522442641B1E7428C0CD7344`）于tick `99545987`读118对象（105 belts、13个normal G sorter），11条完整Native路径均为空货。D族累计105条实际dry belt（上限106）、13个normal sorter/14个sorter总上限（13普通×300、1快速×600，累计工作需求上限4500 J/t）、3座thermal及2座pole；N3为224 nodes、554 consumers、134 generators且点读full-serve。三台新增thermal仍空燃料、实际capacity/generation均0。source `4450`未admit，旧油路5949→3964、H filters、煤源5580及既有generator配置保留；当前库存Fe0/gear0/belt1/normal sorter0/fast sorter1/pole0/thermal0/circuit938/brick144/magnetic coil1，inc0、held null。

## Gate 2 边界

- 整案仍 `executable=false`。1210 既有启动、正常保存、受保护重启及恢复后非零正例仍有效。冷部署 cohort 后已完成一次 fuelPowerState 只读详情核验（13/13 state 为 observed）；这是分时点的库存/发电状态，不是同 tick 物料 cut，也不证明燃烧率、持续功率或净氢/石墨/旧燃料配平。5949→3964现已有短读原生货物流入证据（原件 `a7d27e5a5dc346019e974655f2fb9286:1–15`），但共享供需、有限缓存排除、稳定速率与长窗仍未通过；详见阶段证据。
- 未解决的技术条件仍是净氢/石墨/旧燃料分配、来源接入、共享供需与连续供给验收，整案 `executable=false`。到油后的三段间隔600-tick诊断仍是continuous credit 0；补充单点读回不能补成连续证据，批次输出接纳堵塞也不是Overseer finding。source `4450`仍未admit，三台新thermal燃料与实际capacity/generation均为0；旧配置保留。新建的9个sorter及11条同tick Native路径仍全空。root的prepare-only run `c93a6c1242e146cd89288f1c70348a28:1–15`（seal SHA-256 `A2AFB631B8D9DF41FC7EE4F9A8099B84C195721AE0FD3B2796CCF8D5682752FE`）为`4450→6090` fast `2012/filter1109`取得1个positive Native prepare、0 commit；source/destination槽位与端点offset均精确匹配，原source `4449/4451`连接保留，`6090→6089`仍为空、开放端。source admission尚未批准或执行。准确SHA绿CI并由root重开后，下一有限阶段是fresh施工/读回该source接入，再核真实G到料；共享供需与连续≥36000-tick验收仍未通过。
- 2026-10-07目标范围持续授权已写入[当前规范](../AGENTS.md#阶段规划与证据)：root可批准直接服务0.4的有限取材/制作、合法输送片、过滤分配、候选重设计、最小修复与必要同批维护，并在准确SHA绿CI及独立十写审计后重开下一窗口。每片仍须fresh原生prepare及全部硬检查；不扩大任意恢复、另一世界或正式发布权限。
- 点库存、短窗P/C、单台机器状态、额定能力或单个原生prepare正例，都不能替代稳定产率及材料/功率预算。R16b三thermal及一pole的单体Native正例未验证其同时存在时无碰撞；R29主干、三tap及本窗9个sorter的施工与拓扑核销也不证明source `4450`接入或持续供料。原油实际到达、共享供需与≥36000-tick验收分别记录，不合并为Gate通过。另，R35只读观察到1657的Ti1004槽为0且无订单、530 Ti为0；其Ti出口belt映射正确，但planet102工厂未载入，来源仍待正常同星系飞行后复核。阶段差异和下一阶段边界见本阶段事件。
- Steam 保留运行。除本阶段正常保存与必要 DSP-only 维护恢复外，没有关闭 Steam、热替换、重启其他进程或运行 Host 专项测试。
