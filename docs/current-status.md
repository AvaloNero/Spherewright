# Spherewright 当前快照

更新：2026-10-04（Asia/Singapore）。本阶段原件与审计边界见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前已核状态

- 本轮执行源码 `386feb63e7328b43948ef3a10eb1344877743812`；installed `3fe31d1`、DSP native `29104`及64 tools / 1 resource沿用已核能力，本阶段未部署。当前owned primary正常保存 **89850963 / R9 / J100**，最新只读closing观察 **89867928**。本次升级后尚未protected restart验收，不撤销此前恢复输出正例；Steam/DSP保持运行，未做Codex关闭存活测试。
- 最终连续采样run `db555450498b4ed79a316dd465950ed1`已结束：97样本、3054 sampler reads、3058 native请求、墙钟2177207ms。一次sample-gap后重置，最终有效连续段为**89799586–89835686，36101 ticks**。1210非重叠新产量保守下界17，高于该时段至少1件/分钟所需的11；不是精确总量。PC全实验库存2685→2678，连续有效段2686→2678，下降8；因此全源供料与Gate 2仍未通过。
- 正常保存run `cf93e8dde9dc4a859df5f5ad561e9222`覆盖全部10个accepted：external **10** / lifetime **140**；十写冻结。独立完整capture为60页、5945 built、0 prebuild、22对象细节、11620互逆边；除已批准1512升级与矿机1496移除矿点48的有界例外，其余静态配置一致。最后只读包仍为R9/save89850963/J100，无unknown、inflight或replay。root审计proof `5f6adea51edd4a41a6f2d590f3004db2:1`，SHA-256 `09C7FED9838B2F6683EA8075A69006A8A260784F2522A041F3CF6BAD16787F3F`，0 Game调用。
- 唯一批准的升级是1512从2011至2012；`progressRequired`由600000降至300000是原生运输周期参数，不是功率。现有铁输送仍待预算与fresh资格核验，不能归因于某一个分拣器。矿机1496覆盖读数8→7；node48返回原生`INVALID_ENTITY`证明该对象不存在，不是amount=0。node44还有17939铁矿，矿机继续产矿；当前DLL的耗尽回调解释有界覆盖变化，但没有观测精确耗尽瞬间。
- 十写原审计及全capture通过不等于完整自动供料：采样的PC库存净下降，1210下界也不是产量总数。详细窗口、动作、保存和原件索引见阶段证据。

## 尚未闭合的边界

下一阻塞是现有铁链`1511→1535→2317→2351→723/724`的供给与分配：1511累积、723铁空，Motor缺铁并传导至Turbo与粒子容器缺供。先完成整路端到端预算和fresh native资格；尚未唯一定位单个分拣器，不扩建旁路。新upgrade尚无protected restart验收；全源供料配平、缓冲排除和Gate 2保持false。当前不做施工、部署或发布；external10 / lifetime140冻结，需root后续明确交接。
