# Spherewright 当前快照

更新：2026-10-04（Asia/Singapore）。原件与边界见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前状态

- 源码 `ff362e0` 已push且Windows Core CI `37195880987`通过；共享caller按实测tick速度缩短临近目标的等待，默认cadence1/independent、原预算与gap/reset不变。离线85项及MCP指南2项通过。运行Plugin/MCP未更换，仍为installed `3fe31d1` / native `0.10.35.29104`（64 tools / 1 resource），未部署。
- 本阶段连续采样 `8d3f2a538b5b40119f64ded602b95b61`覆盖36391 ticks、85样本、43次完整实体观察、reset0、2820 native；root proof `e6eab6f392014500ae82b3fa49fa45da:1` / `DFB7E4FA8AF6CFC1A5333C7E4B63F4C2ECB9E2E2ED2AF60A305F83F1886FF1EB`。最低来源资格proof `44c6537a889a44888de962e3a262d898:1` / `0F665EE1B5AE9C085F973CDB6386EA9B972BEF07F1F649CC9AE0CA613210C58C`：1210保守下界16≥需求11，PC 2711→2716、Warper 950→966、净氢下界290≥需求220；仅最低1件/分钟门，不是全源配平或有限缓存排除。
- 最近正常保存 **90474698 / R19 / J100** 后，必要DSP重启与healthy owned-primary恢复已完成；保存点 **90474729 / R1 / J100**，最新观察 **S91501262**，external **7** / lifetime **147**（未达十写冻结）。本次只读观察无新save/restart；最新生产进度不能称作已保存覆盖。Steam/DSP保持运行，既有228项安装文件核验不作本次重新部署。prepare的`exactEmbeddedIdentityVerified=false`，故只在native terminal adoption/readback之后核实同一primary；恢复库存、手持物及位置守恒，Y差1微米在既有1毫米容差内，不推断原因。
- 重启后完整capture为 **60页 / 5945 built / 0 prebuild / 17 details / 11620互逆边**，无新增/删除实体；本阶段将1535、2317从2011升级至2012，既有1512在baseline已为2012并保持；仅2440/node168耗尽差异许可，其余静态配置一致。Gate2垂直门“正常保存→必要受保护重启→恢复同一healthy primary→再次出现1210非零实产”通过。专门Codex退出存活测试已由用户取消，未执行。
- 恢复后六个分离600-tick只读窗仅有1件1210原生实产；`coveredContinuousTicks=0`，不计连续信用。1210库存1011→1013含窗间未采样生产，不能按六窗实产3件解释。N3各窗full-serve，容量1878000–1914000 J/t，高于声明峰值1804700 J/t。

## 尚未闭合

旧`53ed`120样本的42次重置全是粗等待造成的`sample_gap`，不是产线健康检查失败。新`5e1cf80452674139b528c67cf47b30ef`已结束：89样本/45次48对象完整观察、24物料、8来源开闭见证、2786 native，619332.6737 ms，**91464990–91501193 / 36204连续ticks / reset0**。root proof `ad89fb2801274af68d9d520014afac1a:1` / `A6B7024893380D21501888BD026422743B85E39AF2430FA0AF4835B228909DEC`：新鲜1210非重叠下界18≥需求11，PC2832→2835；只证明声明的最低持续率。旧实验信用未复用。

141/707等关键生产buffer已纳入本次45次实际观察；所有选定库存无首末净下降。material union proof `9802245ba8aa4c7d850bc2258fd87ce3:1` / `D25577742F382097E4E621E25DBFBA130F1259366463714629094B3D02CEBDAD`保留24项native计数区间；不少净量区间跨零，不能推断已配平或缺供。`finiteBufferExclusion`、完整sourceSupply、Gate2和Governor仍false：未界定在途有限物料及完整来源归因。库存观察与native窗不同步，不能用库存增量当精确实产。

当前无unknown、inflight或未核销accepted动作；只读worker已停止、审批已消费，禁止重放；Game writes仍blocked。下一唯一边界是设计最小在途/物料cut的有界核验，复用既有观察能力，不盲目再跑整厂长窗、不扩建、不进其它Gate。用户取消的Codex退出存活专项测试不做，游戏保持运行。
