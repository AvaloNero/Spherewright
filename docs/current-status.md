# Spherewright 当前快照

更新：2026-10-04（Asia/Singapore）。原件与边界见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前状态

- 源码 `0b3dd2f` 的只读采样修复已由Windows Core CI `37176748646`通过；显式`EntitySampleEvery=2`，默认仍为1。运行中的Plugin/MCP未更换，仍为installed `3fe31d1` / native `0.10.35.29104`（64 tools / 1 resource），未部署。
- 本阶段连续采样 `8d3f2a538b5b40119f64ded602b95b61`覆盖36391 ticks、85样本、43次完整实体观察、reset0、2820 native；root proof `e6eab6f392014500ae82b3fa49fa45da:1` / `DFB7E4FA8AF6CFC1A5333C7E4B63F4C2ECB9E2E2ED2AF60A305F83F1886FF1EB`。最低来源资格proof `44c6537a889a44888de962e3a262d898:1` / `0F665EE1B5AE9C085F973CDB6386EA9B972BEF07F1F649CC9AE0CA613210C58C`：1210保守下界16≥需求11，PC 2711→2716、Warper 950→966、净氢下界290≥需求220；仅最低1件/分钟门，不是全源配平或有限缓存排除。
- 最近正常保存 **90474698 / R19 / J100** 后，必要DSP重启与healthy owned-primary恢复已完成；当前为 **90474729 / R1 / J100**，S **90530020**，external **7** / lifetime **147**。Steam未关闭，228项安装文件哈希匹配，无重新部署。prepare的`exactEmbeddedIdentityVerified=false`，故不声称加载前已完全读回内嵌身份；native terminal adoption/readback之后才核实同一primary。恢复库存、手持物与位置守恒，Y差1微米在既有1毫米容差内，不推断原因。
- 重启后完整capture为 **60页 / 5945 built / 0 prebuild / 17 details / 11620互逆边**，无新增/删除实体；本阶段将1535、2317从2011升级至2012，既有1512在baseline已为2012并保持；仅2440/node168耗尽差异许可，其余静态配置一致。Gate2垂直门“正常保存→必要受保护重启→恢复同一healthy primary→再次出现1210非零实产”通过。专门Codex退出存活测试已由用户取消，未执行。
- 恢复后六个分离600-tick只读窗仅有1件1210原生实产；`coveredContinuousTicks=0`，不计连续信用。1210库存1011→1013含窗间未采样生产，不能按六窗实产3件解释。N3各窗full-serve，容量1878000–1914000 J/t，高于声明峰值1804700 J/t。

## 尚未闭合

`finiteBufferExclusion`、Governor与完整sourceSupply仍false：在途货物及141/707窗口内buffer未完整界定。不得据六短窗推断无界/36,000-tick持续供给或Gate2全链完成。当前无unknown、inflight或未核销动作；只读阶段批准已消费且Game writes仍blocked。下一唯一边界是核清有限上游/在途buffer来源，不进入支线施工。
