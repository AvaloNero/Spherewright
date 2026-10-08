# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页记录R321联合供需连续观察的中断与独立核销；详见[观察缺口事件](evidence/2026-10-08/joint-observation-gap-r321.md)。先前Rod carry与R320七写阶段见[对应事件](evidence/2026-10-08/departure-rod-carry-seven-r320.md)，R312铜节点164自然消失观察见[便携套件与十写封窗](evidence/2026-10-08/departure-small-kit-ten-r312.md)。

## 当前 owned 状态

- 固定20写窗口已用7笔，external `7`、lifetime `347`、R41；最近已核对普通Save为`101620229`/J102。R321为只读采样，0 accepted、0 commit；最后观察tick `101713130`仍报告R41及同一Save，但本次caller未取得终态Game closure，不能把J102称为fresh最终读回。root独立核验无intent/unknown，写入保持冻结。
- R321尝试6个各600-tick观察窗。前两窗分别为`101709857–101710456`及`101710481–101711080`，中间缺24 ticks；最长连续段为2634 ticks。采样点读到的电力均full-serve，但continuous credit仍为`0`，不构成长窗或自动供给证明。
- 上次已保存套件与Rod carry事实详见R320事件；R321没有建造、拆除或accepted动作。本次只读caller被root停止，未产生新的生产/库存终态证据。
- 当前执行源码基准为`e5d95d34297a468e11d61509d03f04a38c70aaf4`（Windows CI `37763171765` success）；已安装二进制仍为cohort `863d35546f6cb49fcdaec5b5814869d1e13af42b`。

## Gate 2 边界

- R322独立审计：`cdd7aeda694b402da8a585f9aae241dd:1`，SHA-256 `BA0B604B7B8CCDF49206C9E2AAD8E81FFD7E8AB9F01F2028DA6D3E79A5FF2D37`，确认本轮continuous credit为`0`。R321原记录：`826fac57e8e5485190455ee6974b0185:1–111`，SHA-256 `6F3FE7C601BD4CF63720BC0E02E5E14A78B07006B0873194E2B96AD8C1903FC7`；caller SHA-256 `908256DB76FF749E9101C60833D8C9036D6B8399CD779ABB0DC49560667F113F`。本轮不具备终态seal，不应解读为通过。
- H/D/Fe自动共享来源、双自动补给、完整清单速率与运输供电余量、远端Ti、连续至少36,000 ticks、整合保存恢复及最终同SHA双候选包仍未验收；`wholeSupplyPassed=false`。既有蓝图生命周期与Governor 2×门保持先前通过范围，不重开或扩大；整案仍为`executable=false`。
- 2026-10-03：恢复档独立连续采样覆盖36,161 game ticks且无gap，1210非重叠产量下界16件，高于每分钟至少1件在该窗口所需的11件；不等于精确总量或Gate 2通过。详见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。
- 2026-10-04：1210只读采样最终取得36,101连续ticks、17件非重叠产量保守下界；粒子容器库存仍净降8，故供料/配平未闭合。正常保存至89850963/R9/J100并通过十写审计与完整快照核验；铁链分配仍待预算和fresh native资格。详见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。
- 2026-10-04：既有peer升级与正常保存后，预声明只读连续run覆盖36,007 ticks，1210非重叠产量下界17，PC库存净增3；原窗口未采870石墨/酸、5187上游石墨或3343上游氢库存，有限库存排除仍缺证据（不表示停产），不代表Governor或全链供给通过。详见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。
- 2026-10-04：固定32实体/15物料的连续只读采样覆盖36,125 ticks，1210非重叠产量下界16；随后22项runtime source-point reads纠正了163/784的当时库存标签（氢而非油）。原静态filter=1114路径是物料一致的候选，不证明实际油流或持续来源；有限缓存排除仍未证明。save维持89919581/R18/J100、最新观察90141554、0写入。详见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。
- 2026-10-08：R282十笔核销及健康恢复后保存完成；铁源与全物料短窗已记录，持续供需、36,000-tick长窗和双补给仍未通过，详见[事件证据](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。
- 2026-10-08：R303十笔已由正常保存覆盖，ILS准备与全厂/物料快照已封存；持续共享供需和36,000-tick门仍未通过，详见[事件证据](evidence/2026-10-08/departure-ils-ten-r303.md)。
- 2026-10-08：R312十笔由保存覆盖，便携套件与铜节点变化完成封窗核销；全局供需、Rod carry和36,000-tick门仍未通过，详见[事件证据](evidence/2026-10-08/departure-small-kit-ten-r312.md)。
- 2026-10-08：R320核销Rod carry五件与两分拣器filter恢复；共享供需、36,000-tick连续门和整合恢复仍未通过，详见[事件证据](evidence/2026-10-08/departure-rod-carry-seven-r320.md)。
