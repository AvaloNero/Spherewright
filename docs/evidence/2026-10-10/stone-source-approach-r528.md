# R528 石料来源接近资格

本事件记录R520的Move原action核销、R526保存和R528石料矿机候选只读资格。它不代表矿源已建成、已开采或持续供料通过。

R520受保护原件 `c3721457410249bd8934faef57fbb8e9:36`（SHA-256 `1D0242EFA73D548BBA7E19E0DB861DACB4F29B870BABC01C9BD4747A37E4B56F`）在固定30请求预算用尽时，Move已accepted；该次阶段保持失败状态，不重放。R524对同一原action只读查询 `c29e6adf2afd4b3c8ebd3e77afb8a840:8`（SHA-256 `3F800ADC7442AFB2826A96A1F81856447CFAB32C9EF3DD9AA0ADEF9E97A6EC28`）核实Native终态成功，R525 `e75cc68c757645059152dfbb371f45d4:1`（SHA-256 `E6446ADEA7EC8D5BA607E75220C73065D1B3EBAA7205B3287B3F0806416D684F`）独立核销；无重放。玩家正常落地，共经历1549个Native ticks。

R526 `84a67747e7794d318e5df31a5d28f280:22`（SHA-256 `1AC911875900C0438EA1EE229D06DC0F80E94B89FBBF3F173B7EDC8ECC241787`）执行一笔普通Save，16次请求、4.586秒。保存至 `105072201`/J102，R7健康，external 6/20、lifetime 405；当前无intent或unknown，窗口仍冻结、未关闭、未新开。

root R528 `199fd47fa8b440acbf2cdf7983bb1598:1`（SHA-256 `B24F8E8A0DA5DB5D017C769DFA6D2B4F96324F3440AD47C0507A3388A5D9B4F3`）对不同group 19的节点277核验矿机 `2301×1`候选；Native列出的节点262、264、267、271、273、277均有正余量。该步骤未提交建造或开采。全厂最近完整捕获仍为R519的6314 built/0 prebuild；不把历史快照当成R528新捕获。

已核得的矿机候选资格不代表自动石料供给或持续来源。`wholeSupplyPassed=false`、continuous credit为0；Warper与Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包验收仍未通过。下一有限工作为矿机Native建造与实际出口资格；相关动作尚未在本事件中执行。
