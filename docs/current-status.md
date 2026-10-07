# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本阶段离线读取维护见[同星仿真驻留工厂与材料切片读取维护](evidence/2026-10-08/simulation-resident-material-read-maintenance.md)。整案验收仍未通过。

## 当前 owned 状态

- 当前为P104/R1/J100，健康主档恢复后的normal Save为 `100406366`；external `7/10 FROZEN`、lifetime `287`，无unknown或在途。root R98独立核验确认当前owned状态、保存与完整J100。
- P102源端由R122新建分拣器16（`2012/filter1004`，pick185→insert61）和电塔187；185→16→61互惠连接成立。root R123读回确认来源几何和供电有效，Ti货物已进入完整原生路径；连续验收credit为0。P102矿机1及分拣器16在N1 full-serve。
- 源码提交 `863d35546f6cb49fcdaec5b5814869d1e13af42b`（Windows Core CI `37698616442` success）已完成同源冷部署：locked restore/Native29104 Release build-test-publish证据为 `2441d64ed9064d06810bfcfd10acad36:5`，SHA-256 `A65BD874A05A0EE1C84588C2CA27E3D305EAC4D56690CECCAE07147E9EB8CC08`，2672 tests、228 files、64 tools/1 resource。R130普通保存 `100406334/R37/J100` 经R132独立核验；R133确认正常关闭、228哈希匹配、保留Steam 36408，仅一次桌面启动分发，没有强制关闭、Host测试或手动加载。
- 冷部署后已健康恢复同一主档。一次旧Gate参数在Native请求前被拒绝（0 accepted），修正调用方后primary resume完成并停止：P104/R1/J100，Save `100406366`，external7/lifetime287。R98独立复核当前状态及完整库存/inc/held与J100；本次没有游戏写入或重新部署。
- 新增只读能力已在部署后实机验证：P104本地同tick切片覆盖46对象、station1657与16台普通fuel generator；P102远端切片覆盖8对象与3条完整Native路径，读取到Ti200/Si500。两个星球的切片各自保留Native tick，不跨tick拼库存。订单、loaded heat与J/t电力buffer不计作item stock。unsupported或身份缺失保持unobserved；过量对象请求和异星陈旧索引明确拒绝。所有normal-action prepare/commit仍严格限本地星球，原cut预算与64 tools/1 resource不变。

## Gate 2 边界

- 原油5949→3964持续保留。G源4450经快速分拣器6198接至6090；R47读回证明11条新G路径有货、3台新thermal已进燃料并发电。新diamond支路5334实际供料仍未证明；旧煤源5580保留。R50的600-tick、30-item P/C只是短窗诊断，没有连续验收credit。
- 新实机切片验证了本地P104站点/普通fuel缓冲与远端P102物料读取，不构成持续供给或供需平衡证据；两地切片不是同一tick。此前P104 station1657短期有船有货只代表一次运输/进料活动。共享H/D/G/副产物与旧燃料竞争仍待完整复核。
- R118被动供给仍是离线条件模型：2台矿机、7台分拣器、station44闲置负载与充电器估算 `19800 J/t`，低于N1的 `55000 J/t`容量；这不证明远端运输或长窗稳定供电。`597300 J/t`全负荷理论需求仍高于容量，Foundry full-load baseline为false。
- 连续credit仍为0。下一步是在完整共享供需复核后，必要时做有限修复，再预声明连续≥36000-tick窗口；燃料/翘曲器双自动补给、净库存守恒、最终保存恢复整合及同SHA双候选包仍未通过，整案保持 `executable=false`。
