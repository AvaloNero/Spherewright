# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本阶段离线读取维护见[同星仿真驻留工厂与材料切片读取维护](evidence/2026-10-08/simulation-resident-material-read-maintenance.md)。整案验收仍未通过。

## 当前 owned 状态

- 当前为P104/R36/J100，normal Save `100342332`覆盖同星飞行；source端Save `100335320`覆盖P102两项新建。root R127独立核对 `55bed3e8e821483fafc86a4f6df1b72d:8`（SHA-256 `CEA66B92D5A32D92E783758FA9E24DD3BAF53899D3C913968F3DC4350DAE79F0`）确认三项原生动作及保存覆盖终态唯一闭合、J100完整。external `5/10 FROZEN`、lifetime `285`，无unknown或在途；normal Save槽仍保留。
- P102源端由R122新建分拣器16（`2012/filter1004`，pick185→insert61）和电塔187；185→16→61互惠连接成立。root R123读回确认来源几何和供电有效，Ti货物已进入完整原生路径；连续验收credit为0。P102矿机1及分拣器16在N1 full-serve。
- R127同tick读回覆盖9个支持对象、2条完整Native货物流路径与station1657库存锚点。P104需求站1657短期观察到workingVessel 1、remoteOrder `200`、槽内count `113`；factory530当时working。这证明近期运输/进料活动，不证明持续自动补给或长窗配平。当前均已停在P104；下一次远端库存结论须依新的fresh读取。
- 玩家完整库存/inc/held保持一致；source/destination两次保存分别覆盖新建和飞行。当前安装仍为提交`f6694ee11e17a5b32c52c8499c495cbf2a97d301`（Windows Core CI `37675299409` success）对应的同源4+224 cohort，已冷部署并核对228个哈希，MCP为64 tools/1 resource；本阶段没有重新部署或打包。
- 新源码离线扩展只读检查到同星simulation-resident且display未加载的factory，并允许显式same-tick cut读物流站槽存量与普通thermal/fusion缓冲燃料；orders、loaded heat和J/t电力buffer不混作item stock。身份/tick/单位必须完整，原有cut预算不变；所有normal-action prepare/commit仍限本地星球，动作哈希、64 tools/1 resource均未改变。root封存 `30504f6b8fae4db9be99bdda9cae92ec:1`（SHA-256 `5E18ED43075B1721FDF7AF7D35367C07BAEFCCFE24082546FDC0D9BDD3515C51`），相关Core52/52、MCP6/6测试和Native29104 Plugin Release构建0 warnings/0 errors通过；修改仍未部署或实机验证。

## Gate 2 边界

- 原油5949→3964持续保留。G源4450经快速分拣器6198接至6090；R47读回证明11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C只是短窗诊断，没有连续验收credit。
- Ti需求端刚有短期运输活动，但P104 station1657的单次有货读数及factory530 working不是持续产出或供需平衡证据。R126停止的partial cut未用于证明库存；后续由root检查完整H/D/G/副产物与旧燃料竞争，再决定是否需要有界修复。
- R118被动供给仍是离线条件模型：2台矿机、7台分拣器、station44闲置负载与充电器估算 `19800 J/t`，低于N1的 `55000 J/t`容量；这不证明远端运输或长窗稳定供电。`597300 J/t`全负荷理论需求仍高于容量，Foundry full-load baseline为false。
- 连续credit仍为0。下一步是在完整共享供需复核后，必要时做有限修复，再预声明连续≥36000-tick窗口；燃料/翘曲器双自动补给、净库存守恒、保存恢复整合和最终同SHA双候选包仍未通过，整案保持 `executable=false`。
