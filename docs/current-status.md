# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。本文只覆盖当前截面；阶段原件索引见[Gate 2 证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。数值以受保护原件和外部 accepted 台账为准。

## 运行、保存与写入边界

- DSP 保持同一 owned 世界运行，版本 `0.10.35.29104`。最近一次正常保存为 tick **87119420**；完整审计的 closing observation 为 **87125503 / R18**。之后独立只读诊断的 closing 为 **87204103 / R18 / healthy owned**（见阶段证据），不是新的完整工厂审计或保存。durable Journal **J97**、pending=false、error=null。source HEAD `ac0e434`；installed `3fe31d1`沿用已验证的同批构建，64 tools/1 resource资格引用既有原件，本窗没有部署。Host/Codex 退出、重启或断线不关闭、保存、重载或重置游戏。旧Host退出的确切原因未直接证明；用户不要求专门关闭Codex验证存活，本次未测不冒充通过，也不设为Gate 2 blocker；DSP保持运行。
- 本十笔窗口 **10/10 accepted 均 completed/succeeded**，0 replay、unknown、在途或未决；external **10/10**、lifetime **100**，目前冻结。完整封窗和独立审计见 capture `3fce122b339f46d19dd966388f699e2e:1–82` 与 root proof `44dbb59cb37a4e639fe86d784a1c6f03:1`（SHA-256 `31D65C76DFD511CCD8136BFFA8333265DA5A0F0BD0485455F55439EA1383DA16`）。该审计耗时34.98s，0新Game调用。

## Gate 2 进展与未完成项

**目标仍是1210完整自动三级链；wholeNativePreflightPassed=false、wholeSustainedRatePassed=false。** 本窗完成并保存Fe A-H-D的52条带及两端分拣器，filter 1101、N3/serve ratio 1：`1511.slot0 → 5635 → 5593.actual4`，末端为`5605.actual4 → 5634 → 5326.slot6`；5326.slot7仍空。新鲜实体读回见阶段证据`:63–64/:68–69`。真实铁已到5326，但其item1101输入为4、item1206/1121输入为0、item1127输出为0且r104当时未工作；这不是持续供给或目标产出证明。

- 完整图为 **57页 / 5684 built / 0 prebuild / 11090唯一互逆边**。相对`8b3a9202460242179005c87381f00b17`只增加101条带和2个分拣器，无删除或未解释静态配置漂移。材料净变化为2001 −101、2011 −2；增产点数及其它背包/玩家位置守恒。N3为211节点、527消费者、127发电机、容量1,894,000 J/t，required=served 410,992 J/t、ratio 1。快照玩家Walk、速度0、核心能量1,589,482,062、pending build targets 0；背包273条带/19个分拣器/1个电塔。
- D仅完成DA20、DD20及DH2 free 9段；DH1/H3和两端分拣器尚未建立，不能称为自动D供给或完整D路。后续D路线仍需fresh原生预检与实际端点核验，完整D连接和自动供料尚未证明。实际储料点5326仍未加工；883/r99是生产设备（capture`:79`为working=true、各输入4），885是r0仓库（`:80`存有915个1206），且未接5326；这些读数不证明送达该consumer或持续供料。
- 累计使用309条带、9个分拣器、1个电塔；计划预测余245条带、11个分拣器、1个电塔。预测不是完整路线或原生联合通过。自动供料→1210非零→正常保存→protected restart→恢复后再次非零仍未完成；不进入联合36000-tick、燃料长窗、远征或最终包。

## 可复用的调用边界

历史几何资格不能代替当前session的原生计划、准确空槽和当前端点读回。一次旧slot选择不匹配被严格校验挡在commit前；root检查当前slot 6为空且无冲突后才更新精确绑定，未放宽匹配条件、移动实体或重放。复杂PowerShell参数避免经嵌套shell二次展开；具体过程与边界见阶段证据。
